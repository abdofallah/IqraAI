using IqraCore.Entities.Call.Outbound;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Call
{
    public class OutboundCallQueueGroupRepository
    {
        private readonly IMongoCollection<OutboundCallQueueGroupData> _callQueueGroupCollection;
        private readonly ILogger<OutboundCallQueueGroupRepository> _logger;

        private readonly string DatabaseName = "IqraOutboundCallCampaign";
        private const string CollectionName = "OutboundCallQueueGroup";

        public OutboundCallQueueGroupRepository(IMongoClient client, ILogger<OutboundCallQueueGroupRepository> logger)
        {
            var database = client.GetDatabase(DatabaseName);
            _callQueueGroupCollection = database.GetCollection<OutboundCallQueueGroupData>(CollectionName);
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
