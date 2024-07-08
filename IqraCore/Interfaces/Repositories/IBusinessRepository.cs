using IqraCore.Entities.Business;
using MongoDB.Driver;

namespace IqraCore.Interfaces.Repositories
{
    public interface IBusinessRepository
    {
        Task<List<BusinessData>> GetBusinessesAsync();
        Task<List<BusinessData>> GetBusinessesAsync(int page, int pageSize);
        Task<List<BusinessData>> GetBusinessesAsync(List<long> businessesId);
        Task<List<BusinessData>> GetBusinessesByMasterUserEmailAsync(string userEmail);

        Task<BusinessData?> GetBusinessAsync(long businessId);
        Task AddBusinessAsync(BusinessData business);
        Task<bool> DeleteBusinessAsync(long businessId);

        Task<bool> UpdateBusinessAsync(long businessId, UpdateDefinition<BusinessData> updateDefinition);
    }
}