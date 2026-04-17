using Google.Cloud.Firestore;
using HomeAssignmentPFTC.Models;

namespace HomeAssignmentPFTC.DataAccess;

public class FirestoreRepository
{
    private readonly FirestoreDb _firestoreDb;

    public FirestoreRepository(IConfiguration config)
    {
        string projectId = config.GetValue<string>("Authentication:Google:ProjectId");
        _firestoreDb = FirestoreDb.Create(projectId);
    }

    public async Task SaveMenuImageAsync(string restaurantId, string menuId, string imageUrl)
    {
        // This creates the exact hierarchy required: restaurants -> menus -> images
        CollectionReference imagesCollection = _firestoreDb
            .Collection("restaurants").Document(restaurantId)
            .Collection("menus").Document(menuId)
            .Collection("images");

        // The data we want to save
        var imageData = new Dictionary<string, object>
        {
            { "imageUrl", imageUrl },
            { "uploadedAt", Timestamp.GetCurrentTimestamp() }
        };

        // Add it to the database
        await imagesCollection.AddAsync(imageData);
    }
    
    public async Task<List<CatalogItemViewModel>> GetReadyCatalogItemsAsync()
    {
        var catalogItems = new List<CatalogItemViewModel>();

        //Find all menus that have been processed by the Cron Job
        Query readyMenusQuery = _firestoreDb.CollectionGroup("menus").WhereEqualTo("status", "ready");
        QuerySnapshot snapshot = await readyMenusQuery.GetSnapshotAsync();

        //Group them by Restaurant ID to only keep the newest one
        var latestMenus = new Dictionary<string, DocumentSnapshot>();

        foreach (var document in snapshot.Documents)
        {
            string restId = document.Reference.Parent.Parent?.Id ?? "Unknown";

            if (!latestMenus.ContainsKey(restId))
            {
                latestMenus[restId] = document;
            }
            else
            {
                if (document.TryGetValue("cleanedAt", out Timestamp currentDocTime) && 
                    latestMenus[restId].TryGetValue("cleanedAt", out Timestamp savedDocTime))
                {
                    if (currentDocTime.ToDateTime() > savedDocTime.ToDateTime())
                    {
                        latestMenus[restId] = document; 
                    }
                }
            }
        }

        //Loop through the newest menus and fetch the items
        foreach (var kvp in latestMenus)
        {
            string restId = kvp.Key;
            DocumentSnapshot menuDoc = kvp.Value;

            string restaurantName = "Unknown Restaurant";
            string locality = "Unknown Locality";
            
            if (menuDoc.Reference.Parent.Parent != null)
            {
                DocumentSnapshot restSnapshot = await menuDoc.Reference.Parent.Parent.GetSnapshotAsync();
                if (restSnapshot.Exists)
                {
                    restaurantName = restSnapshot.TryGetValue("name", out string n) ? n : "Unknown Restaurant";
                    locality = restSnapshot.TryGetValue("locality", out string l) ? l : "Unknown Locality";
                }
            }

            if (menuDoc.TryGetValue("items", out List<Dictionary<string, object>> items))
            {
                foreach (var item in items)
                {
                    string name = item.ContainsKey("itemName") ? item["itemName"].ToString() ?? "" : "";
                    string priceStr = item.ContainsKey("price") ? item["price"].ToString() ?? "" : "";
                
                    decimal numericPrice = 0;
                    string cleanPrice = System.Text.RegularExpressions.Regex.Replace(priceStr, @"[^\d\.,]", "").Replace(",", ".");
                    decimal.TryParse(cleanPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out numericPrice);

                    catalogItems.Add(new CatalogItemViewModel
                    {
                        RestaurantId = restId,
                        MenuId = menuDoc.Id,
                        RestaurantName = restaurantName,
                        Locality = locality,
                        ItemName = name,
                        DisplayPrice = priceStr,
                        NumericPrice = numericPrice
                    });
                }
            }
        }
        
        return catalogItems;
    }
}