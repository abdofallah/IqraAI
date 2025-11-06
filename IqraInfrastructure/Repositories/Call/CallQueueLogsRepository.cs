using IqraCore.Entities.Call.Queue;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Call
{
    public class CallQueueLogsRepository
    {
        private readonly IMongoCollection<CallQueueLogsData> _callQueueLogsCollection;
        private readonly ILogger<CallQueueLogsRepository> _logger;

        public CallQueueLogsRepository(IMongoClient client, string databaseName, ILogger<CallQueueLogsRepository> logger)
        {
            var database = client.GetDatabase(databaseName);
            _callQueueLogsCollection = database.GetCollection<CallQueueLogsData>("CallQueueLogs");
            _logger = logger;
        }

        public async Task<bool> AddCallLogAsync(string queueId, CallQueueLogEntry log)
        {
            try
            {
                var filter = Builders<CallQueueLogsData>.Filter.Eq(c => c.Id, queueId);
                var update = Builders<CallQueueLogsData>.Update.Push(c => c.Logs, log);

                var options = new UpdateOptions { IsUpsert = true };

                var result = await _callQueueLogsCollection.UpdateOneAsync(filter, update, options);
                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding log for outbound call {QueueId}", queueId);
                return false;
            }
        }
    }
}
