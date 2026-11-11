using IqraCore.Entities.WebSession;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.WebSession
{
    public class WebSessionLogsRepository
    {
        private readonly string DatabaseName = "IqraWebSession";
        private const string CollectionName = "WebSessionLogs";

        private readonly IMongoCollection<WebSessionLogsData> _webSessionLogsCollection;
        private readonly ILogger<WebSessionLogsRepository> _logger;

        public WebSessionLogsRepository(IMongoClient client, ILogger<WebSessionLogsRepository> logger)
        {
            var database = client.GetDatabase(DatabaseName);
            _webSessionLogsCollection = database.GetCollection<WebSessionLogsData>(CollectionName);
            _logger = logger;

            // Ensure index on _id for efficient lookups
            var indexKeysDefinition = Builders<WebSessionLogsData>.IndexKeys.Ascending(c => c.Id);
            _webSessionLogsCollection.Indexes.CreateOne(new CreateIndexModel<WebSessionLogsData>(indexKeysDefinition));
        }

        public async Task<WebSessionLogsData?> GetLogsByIdAsync(string webSessionId)
        {
            try
            {
                var filter = Builders<WebSessionLogsData>.Filter.Eq(c => c.Id, webSessionId);
                return await _webSessionLogsCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs for web session {WebSessionId}", webSessionId);
                return null;
            }
        }

        public async Task<bool> AddLogAsync(string webSessionid, WebSessionLogEntry log)
        {
            try
            {
                var filter = Builders<WebSessionLogsData>.Filter.Eq(c => c.Id, webSessionid);
                var update = Builders<WebSessionLogsData>.Update.Push(c => c.Logs, log);

                var options = new UpdateOptions { IsUpsert = true };

                var result = await _webSessionLogsCollection.UpdateOneAsync(filter, update, options);
                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding log for web session {WebSessionid}", webSessionid);
                return false;
            }
        }
    }
}
