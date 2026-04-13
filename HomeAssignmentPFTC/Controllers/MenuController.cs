using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomeAssignmentPFTC.Models;
using HomeAssignmentPFTC.Interfaces;

namespace HomeAssignmentPFTC.Controllers;

public class MenuController : Controller
{
    private readonly ILogger<MenuController> _logger;
    private readonly IBucketStorageService _bucketStorageService;

    // Inject the IBucketStorageService via the constructor
    public MenuController(ILogger<MenuController> logger, IBucketStorageService bucketStorageService)
    {
        _logger = logger;
        _bucketStorageService = bucketStorageService;
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
                var uploadedUrls = new List<string>();

                // Loop through and upload each file
                foreach (var image in menuImages)
                {
                    if (image.Length > 0)
                    {
                        string fileUrl = await _bucketStorageService.UploadFileAsync(image, null);
                        uploadedUrls.Add(fileUrl);
                    }
                }
            
                // Return success status and the list of new URLs
                return Json(new { 
                    success = true, 
                    urls = uploadedUrls, 
                    message = $"Successfully uploaded {uploadedUrls.Count} menu image(s)!" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload the menu images.");
                return Json(new { success = false, message = "An error occurred whilst uploading the images." });
            }
        }
    
        return Json(new { success = false, message = "Please select at least one valid image file." });
    }
}