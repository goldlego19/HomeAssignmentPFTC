namespace HomeAssignmentPFTC.Interfaces;

public interface IGoogleSecretManagerService
{
    Task<string> GetSecretAsync(string secretName);
    Task LoadSecretsIntoConfigurationAsync(IConfiguration config);
}