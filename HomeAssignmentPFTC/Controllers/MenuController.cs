using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomeAssignmentPFTC.Models;
using HomeAssignmentPFTC.Interfaces;
using HomeAssignmentPFTC.DataAccess;
using Google.Cloud.PubSub.V1; // Added for PubSub
using System.Text.Json;
using Microsoft.AspNetCore.Authorization; // Added for JSON serialization

namespace HomeAssignmentPFTC.Controllers;

[Authorize]
public class MenuController : Controller
{
    private readonly ILogger<MenuController> _logger;
    private readonly IBucketStorageService _bucketStorageService;
    private readonly FirestoreRepository _firestoreRepository;
    private readonly IConfiguration _configuration; // Added to get Project ID

    // Inject IConfiguration along with your other services
    public MenuController(
        ILogger<MenuController> logger, 
        IBucketStorageService bucketStorageService, 
        FirestoreRepository firestoreRepository,
        IConfiguration configuration)
    {
        _logger = logger;
        _bucketStorageService = bucketStorageService;
        _firestoreRepository = firestoreRepository;
        _configuration = configuration;
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        return View(new MenuViewModel());
    }
    
    [HttpPost]
    public async Task<IActionResult> UploadImageAjax(List<IFormFile> menuImages,string restaurantName,string locality)
    {
        if (menuImages != null && menuImages.Count > 0)
        {
            try
            {
                // Retrieve your project ID from configuration
                string projectId = _configuration.GetValue<string>("Authentication:Google:ProjectId");

                // Create predictable IDs using the new input fields
                string safeName = System.Text.RegularExpressions.Regex.Replace(restaurantName.ToLower().Trim(), @"[^a-z0-9]+", "-");
                string safeLocality = System.Text.RegularExpressions.Regex.Replace(locality.ToLower().Trim(), @"[^a-z0-9]+", "-");
                string restaurantId = $"{safeName}-{safeLocality}".Trim('-');
                
                // Creates a unique menu ID incorporating the locality
                string menuId = $"{safeLocality}-menu-" + Guid.NewGuid().ToString().Substring(0, 5);

                foreach (var image in menuImages)
                {
                    if (image.Length > 0)
                    {
                        // 1. Upload to Cloud Storage
                        string fileUrl = await _bucketStorageService.UploadFileAsync(image, null);
                    
                        // 2. Save reference to Firestore Database
                        await _firestoreRepository.SaveMenuImageAsync(restaurantId, menuId, fileUrl);

                        // 3. Publish to Pub/Sub Topic (SE4.6a requirement)
                        TopicName topicName = TopicName.FromProjectTopic(projectId, "menu-uploads-topic");
                        PublisherClient publisher = await PublisherClient.CreateAsync(topicName);
                        
                        // We send the IDs and URL so the background function knows what to process
                        var messageData = new 
                        { 
                            RestaurantId = restaurantId, 
                            MenuId = menuId, 
                            ImageUrl = fileUrl 
                        };
                        
                        string jsonMessage = JsonSerializer.Serialize(messageData);
                        await publisher.PublishAsync(jsonMessage);
                    }
                }
            
                return Json(new { success = true, message = "Successfully uploaded and queued for processing!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload and save the menu images.");
                return Json(new { success = false, message = "An error occurred." });
            }
        }
    
        return Json(new { success = false, message = "Please select at least one valid image file." });
    }
}