using Google.Cloud.SecretManager.V1;
namespace HomeAssignmentPFTC.Services;

public class GoogleSecretManagerService
{
    private readonly SecretManagerServiceClient _client;
    private readonly ILogger<GoogleSecretManagerService> _logger;
    private readonly string _projectId;

    public GoogleSecretManagerService(string projectId, ILogger<GoogleSecretManagerService> logger)
    {
        _logger = logger;
        _projectId = projectId;
        _client = SecretManagerServiceClient.Create();
    }

    public async Task<string> GetSecretAsync(string secretName)
    {
        try
        {
            var secretVersionName = new SecretVersionName(_projectId, secretName, "latest");
            var result = await _client.AccessSecretVersionAsync(secretVersionName);
        
            _logger.LogInformation($"Successfully loaded secret {secretName} from Google Secret Manager");
            return result.Payload.Data.ToStringUtf8();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
        
    }

    public async Task LoadSecretsIntoConfigurationAsync(IConfiguration config)
    {
        _logger.LogInformation($"Loading secrets from Google Secret Manager");

        try
        {
            var secrets = new Dictionary<string, string>
            {
                { "Authentication:Google:ClientId", await GetSecretAsync("Google:ClientId") },
                { "Authentication:Google:ClientSecret", await GetSecretAsync("Google:ClientSecret") }
            };
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to load secrets into configuration {e.Message}");
            throw;
        }
    }
}