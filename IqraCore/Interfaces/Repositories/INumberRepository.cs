using IqraCore.Entities.Number;
using MongoDB.Driver;

namespace IqraCore.Interfaces.Repositories
{
    public interface INumberRepository
    {
        Task<List<NumberData>> GetNumbersAsync();
        Task<List<NumberData>> GetBusinessNumbersAsync(long businessId);
        Task<List<NumberData>> GetNumbersAsync(List<long> numbersId);
        Task<NumberData> GetNumberAsync(string countryCode, string number);
        Task AddNumberAsync(NumberData number);
        Task<bool> DeleteNumberAsync(long numberId);
        Task<bool> UpdateNumberAsync(long numberId, UpdateDefinition<NumberData> updateDefinition);
    }
}
