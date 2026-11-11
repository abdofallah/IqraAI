using IqraCore.Entities.Business;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Business
{
    public class EndpointBlacklistRepository
    {
        private readonly ILogger<EndpointBlacklistRepository> _logger;
        private readonly IMongoCollection<EndpointBlacklistEntry> _collection;

        public EndpointBlacklistRepository(ILogger<EndpointBlacklistRepository> logger, IMongoClient client)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase("IqraBusinessApp");
            _collection = database.GetCollection<EndpointBlacklistEntry>("EndpointBlacklist");
        }

        public async Task<List<EndpointBlacklistEntry>> GetAllActiveEntriesAsync()
        {
            var filter = Builders<EndpointBlacklistEntry>.Filter.Eq(e => e.IsActive, true);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<List<EndpointBlacklistEntry>> GetAllEntriesAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<bool> AddEntryAsync(EndpointBlacklistEntry entry)
        {
            try
            {
                await _collection.InsertOneAsync(entry);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding endpoint blacklist entry: {Pattern}", entry.Pattern);
                return false;
            }
        }

        public async Task<bool> UpdateEntryAsync(string entryId, EndpointBlacklistEntry updatedEntry)
        {
            var filter = Builders<EndpointBlacklistEntry>.Filter.Eq(e => e.Id, entryId);
            var update = Builders<EndpointBlacklistEntry>.Update
                .Set(e => e.Pattern, updatedEntry.Pattern)
                .Set(e => e.PatternType, updatedEntry.PatternType)
                .Set(e => e.Reason, updatedEntry.Reason)
                .Set(e => e.IsActive, updatedEntry.IsActive);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteEntryAsync(string entryId)
        {
            var filter = Builders<EndpointBlacklistEntry>.Filter.Eq(e => e.Id, entryId);
            var result = await _collection.DeleteOneAsync(filter);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
    }
}
