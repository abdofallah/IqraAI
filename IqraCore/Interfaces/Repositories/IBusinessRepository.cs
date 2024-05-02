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
    }
}