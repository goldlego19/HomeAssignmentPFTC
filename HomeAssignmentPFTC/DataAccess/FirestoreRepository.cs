using Google.Cloud.Firestore;

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
}