using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomeAssignmentPFTC.Models;
using HomeAssignmentPFTC.Interfaces;
using HomeAssignmentPFTC.DataAccess;
namespace HomeAssignmentPFTC.Controllers;

public class MenuController : Controller
{
    private readonly ILogger<MenuController> _logger;
    private readonly IBucketStorageService _bucketStorageService;
    private readonly FirestoreRepository _firestoreRepository;
    // Inject the IBucketStorageService via the constructor
    public MenuController(ILogger<MenuController> logger, IBucketStorageService bucketStorageService, FirestoreRepository firestoreRepository)
    {
        _logger = logger;
        _bucketStorageService = bucketStorageService;
        _firestoreRepository = firestoreRepository;
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        // Pass an empty model to the view initially
        return View(new MenuViewModel());
    }
    
    [HttpPost]
    public async Task<IActionResult> UploadImageAjax(List<IFormFile> menuImages)
    {
        if (menuImages != null && menuImages.Count > 0)
        {
            try
            {
                // For testing purposes, generate some IDs. 
                // Later, you might pass these in from the front-end form.
                string restaurantId = "TestRestaurant_" + Guid.NewGuid().ToString().Substring(0, 5);
                string menuId = "Menu_" + Guid.NewGuid().ToString().Substring(0, 5);

                foreach (var image in menuImages)
                {
                    if (image.Length > 0)
                    {
                        // 1. Upload to Cloud Storage
                        string fileUrl = await _bucketStorageService.UploadFileAsync(image, null);
                    
                        // 2. Save reference to Firestore Database
                        await _firestoreRepository.SaveMenuImageAsync(restaurantId, menuId, fileUrl);
                    }
                }
            
                return Json(new { success = true, message = "Successfully uploaded and saved to database!" });
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