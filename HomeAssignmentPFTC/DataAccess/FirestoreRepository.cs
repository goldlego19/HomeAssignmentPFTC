
using Google.Cloud.Firestore;
using HomeAssignmentPFTC.Models;

namespace HomeAssignmentPFTC.DataAccess;


public class FirestoreRepository
{
    private readonly ILogger<FirestoreRepository> _logger;
    private FirestoreDb _db;

    public FirestoreRepository(ILogger<FirestoreRepository> logger, IConfiguration configuration)
    {
        _logger = logger;
        _db = FirestoreDb.Create(configuration["Authentication:Google:ProjectId"]);
    }
}