using Microsoft.AspNetCore.Http;

namespace HomeAssignmentPFTC.Models;

public class MenuViewModel
{
    // Captures multiple files uploaded via the HTML form
    public List<IFormFile> MenuImages { get; set; } = new List<IFormFile>();
    
    // Stores the Google Cloud Storage URLs after successful uploads
    public List<string> UploadedImageUrls { get; set; } = new List<string>();
}