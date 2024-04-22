using IqraCore.Entities.Business;
using MongoDB.Driver;

namespace IqraCore.Interfaces.Repositories
{
    public interface IBusinessRepository
    {
        Task<List<Business>> GetBusinessesMetadataAsync();
        Task<List<Business>> GetBusinessesAsync();

        Task<Business?> GetBusinessByPhoneNumberAsync(string phoneNumber);
        Task<Business?> GetBusinessAsync(long businessId);
        Task<bool> AddBusinessAsync(Business business);
        Task<bool> DeleteBusinessAsync(long businessId);

        Task<bool> UpdateBusinessAsync(long businessId, UpdateDefinition<Business> updateDefinition);
        Task<bool> UpdateBusinessPropertyAsync<T>(long businessId, string propertyName, T value);

        Task<bool> UpdateBusinessAzureSettingsAsync(long businessId, BusinessAzureSettings azureSettings);
        Task<bool> UpdateBusinessPromptAsync(long businessId, Dictionary<string, string> businessPrompt, string promptType);
        Task<bool> UpdateBusinessPhoneNumberAsync(long businessId, string phoneNumber);

        Task<Dictionary<string, string>> GetBusinessNameAsync(long businessId);
        Task<Dictionary<string, string>> GetBusinessSystemPromptAsync(long businessId);
        Task<Dictionary<string, string>> GetBusinessInitialMessageAsync(long businessId);
        Task<List<string>> GetBusinessLanguagesEnabledAsync(long businessId);
        Task<BusinessAzureSettings> GetBusinessAzureSettingsAsync(long businessId);
        Task<string> GetBusinessPhoneNumberAsync(long businessId);
    }
}