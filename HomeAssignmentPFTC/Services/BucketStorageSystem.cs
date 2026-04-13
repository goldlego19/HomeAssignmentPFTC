using HomeAssignmentPFTC.Interfaces;
using Google.Cloud.Storage.V1;

namespace HomeAssignmentPFTC.Services;

public class BucketStorageService: IBucketStorageService
{
    private readonly ILogger<BucketStorageService> _logger;
    private readonly string _bucketName;
    private readonly StorageClient _storageClient;
    
    public BucketStorageService(ILogger<BucketStorageService> logger, IConfiguration config)
    {
        _logger = logger;
        _bucketName = config.GetValue<string>("Storage:Google:BucketName");
        _storageClient = StorageClient.Create();
    }
    
    public Task DeleteFileAsync(string fileName)
    {
        throw new NotImplementedException();
    }
    
    public async Task<string> UploadFileAsync(IFormFile file, string fileNameForStorage)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentNullException($"File {nameof(file)} cannot be null or empty.");
        }
        try
        {
            //return the url
            if (string.IsNullOrWhiteSpace(fileNameForStorage))
            {
                fileNameForStorage = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            }
            string contentType = file.ContentType;
            using (var memorystream = new MemoryStream())
            {
                await file.CopyToAsync(memorystream);
                memorystream.Position = 0; // Reset the stream position to the beginning

                //Upload the file to Google Cloud Storage
                UploadObjectOptions options = new UploadObjectOptions();
                var storageObject = await _storageClient.UploadObjectAsync(_bucketName, fileNameForStorage, contentType, memorystream, options);
                _logger.LogInformation($"File {fileNameForStorage} uploaded to bucket {_bucketName} successfully.");
                return $"https://storage.googleapis.com/{_bucketName}/{fileNameForStorage}";
            }
        }
        catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
        {
            _logger.LogError(ex, $"Google API Error during file upload: {ex.Message}");
            throw new ApplicationException($"Google API Error during file upload: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading file {fileNameForStorage} to bucket {_bucketName}");
            throw new ApplicationException($"Error uploading file {fileNameForStorage} to bucket {_bucketName}", ex);

        }
    }
    
}