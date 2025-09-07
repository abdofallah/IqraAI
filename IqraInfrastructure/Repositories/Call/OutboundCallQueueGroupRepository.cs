using IqraCore.Entities.Call.Outbound;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Call
{
    public class OutboundCallCampaignRepository
    {
        private readonly IMongoCollection<OutboundCallQueueGroupData> _callQueueGroupCollection;
        private readonly ILogger<OutboundCallCampaignRepository> _logger;

        public OutboundCallCampaignRepository(IMongoClient client, string databaseName, ILogger<OutboundCallCampaignRepository> logger)
        {
            var database = client.GetDatabase(databaseName);
            _callQueueGroupCollection = database.GetCollection<OutboundCallQueueGroupData>("OutboundCallQueueGroup");
            _logger = logger;
        }

        public async Task<OutboundCallQueueGroupData?> GetOutboundCallQueueGroupByIdAsync(string queueGroupId)
        {
            try
            {
                var filter = Builders<OutboundCallQueueGroupData>.Filter.Eq(c => c.Id, queueGroupId);
                return await _callQueueGroupCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outbound call queue group by ID {QueueGroupId}", queueGroupId);
                return null;
            }
        }

        public async Task<bool> CreateOutboundCallQueueGroupAsync(OutboundCallQueueGroupData outboundCallQueueGroupData)
        {
            try
            {
                await _callQueueGroupCollection.InsertOneAsync(outboundCallQueueGroupData);
                return true;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error inserting outbound call campaign");
                return false;
            }
        }

        public async Task<bool> AddQueueToQueueGroupAsync(string callQueueId, string outboundCallQueueGroupId)
        {
            try
            {
                var filter = Builders<OutboundCallQueueGroupData>.Filter.Eq(c => c.Id, outboundCallQueueGroupId);
                var update = Builders<OutboundCallQueueGroupData>.Update.AddToSet(c => c.CallQueueIds, callQueueId);
                var result = await _callQueueGroupCollection.UpdateOneAsync(filter, update);
                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding queue to queue group");
                return false;
            }
        }

        public async Task<bool> AddErrorLogs(string outboundCallQueueGroupId, List<string> errors)
        {
            try
            {
                var filter = Builders<OutboundCallQueueGroupData>.Filter.Eq(c => c.Id, outboundCallQueueGroupId);
                var update = Builders<OutboundCallQueueGroupData>.Update.AddToSetEach(c => c.ErrorLogs, errors);
                var result = await _callQueueGroupCollection.UpdateOneAsync(filter, update);
                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding error logs to queue group");
                return false;
            }
        }
    }
}
