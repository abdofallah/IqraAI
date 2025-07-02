using IqraCore.Entities.Call.Outbound;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Call
{
    public class OutboundCallCampaignRepository
    {
        private readonly IMongoCollection<OutboundCallCampaignData> _callQueueCollection;
        private readonly ILogger<OutboundCallCampaignRepository> _logger;

        public OutboundCallCampaignRepository(IMongoClient client, string databaseName, ILogger<OutboundCallCampaignRepository> logger)
        {
            var database = client.GetDatabase(databaseName);
            _callQueueCollection = database.GetCollection<OutboundCallCampaignData>("OutboundCallCampaign");
            _logger = logger;
        }

        public async Task<OutboundCallCampaignData?> GetOutboundCallCampaignByIdAsync(string campaignId)
        {
            try
            {
                var filter = Builders<OutboundCallCampaignData>.Filter.Eq(c => c.Id, campaignId);
                return await _callQueueCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outbound call campaign by ID {CampaignId}", campaignId);
                return null;
            }
        }

        public async Task<bool> CreateOutboundCallCampaignAsync(OutboundCallCampaignData outboundCallCampaignData)
        {
            try
            {
                await _callQueueCollection.InsertOneAsync(outboundCallCampaignData);
                return true;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error inserting outbound call campaign");
                return false;
            }
        }

        public async Task<bool> AddQueueToCampaignAsync(string callQueueId, string outboundCallCampaignId)
        {
            try
            {
                var filter = Builders<OutboundCallCampaignData>.Filter.Eq(c => c.Id, outboundCallCampaignId);
                var update = Builders<OutboundCallCampaignData>.Update.AddToSet(c => c.CallQueueIds, callQueueId);
                var result = await _callQueueCollection.UpdateOneAsync(filter, update);
                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding queue to campaign");
                return false;
            }
        }

        public async Task<bool> AddErrorLogs(string outboundCallCampaignId, List<string> errors)
        {
            try
            {
                var filter = Builders<OutboundCallCampaignData>.Filter.Eq(c => c.Id, outboundCallCampaignId);
                var update = Builders<OutboundCallCampaignData>.Update.AddToSetEach(c => c.ErrorLogs, errors);
                var result = await _callQueueCollection.UpdateOneAsync(filter, update);
                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding error logs to campaign");
                return false;
            }
        }
    }
}
