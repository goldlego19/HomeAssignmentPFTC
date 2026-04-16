using Google.Cloud.Firestore;
using HomeAssignmentPFTC.Models;

namespace HomeAssignmentPFTC.DataAccess;

public class FirestoreRepository
{
    private readonly FirestoreDb _firestoreDb;

    public FirestoreRepository(IConfiguration config)
    {
        // Grabs the ProjectId you already set in your user-secrets
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

        // Find all menus that have been processed by the Cron Job
        Query readyMenusQuery = _firestoreDb.CollectionGroup("menus").WhereEqualTo("status", "ready");
        QuerySnapshot snapshot = await readyMenusQuery.GetSnapshotAsync();

        foreach (var document in snapshot.Documents)
        {
            if (document.TryGetValue("items", out List<Dictionary<string, object>> items))
            {
                string restId = document.Reference.Parent.Parent?.Id ?? "Unknown";
            
                foreach (var item in items)
                {
                    string name = item.ContainsKey("itemName") ? item["itemName"].ToString() ?? "" : "";
                    string priceStr = item.ContainsKey("price") ? item["price"].ToString() ?? "" : "";
                
                    // Strip out currency symbols so we can convert the string into a mathematical decimal for sorting
                    decimal numericPrice = 0;
                    string cleanPrice = System.Text.RegularExpressions.Regex.Replace(priceStr, @"[^\d\.,]", "").Replace(",", ".");
                    decimal.TryParse(cleanPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out numericPrice);

                    catalogItems.Add(new CatalogItemViewModel
                    {
                        RestaurantId = restId,
                        MenuId = document.Id,
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