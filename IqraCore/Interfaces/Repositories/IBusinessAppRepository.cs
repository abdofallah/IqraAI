using IqraCore.Entities.BusinessNEW;
using MongoDB.Driver;

namespace IqraCore.Interfaces.Repositories
{
    public interface IBusinessAppRepository
    {
        Task<List<BusinessApp>> GetBusinessesAppAsync();

        Task<BusinessApp?> GetBusinessAppAsync(long businessId);
        Task AddBusinessAppAsync(BusinessApp businessApp);
        Task<bool> DeleteBusinessAppAsync(long businessId);

        Task<bool> UpdateBusinessAppAsync(long businessId, UpdateDefinition<BusinessApp> updateDefinition);
    }
}