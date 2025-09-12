using IqraCore.Entities.WebSession;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.WebSession
{
    public class WebSessionRepository
    {
        private readonly IMongoCollection<WebSessionData> _webSessionCollection;
        private readonly ILogger<WebSessionRepository> _logger;

        private const string InboundCollectionName = "WebSession";

        public WebSessionRepository(ILogger<WebSessionRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;

            var database = client.GetDatabase(databaseName);
            _webSessionCollection = database.GetCollection<WebSessionData>(InboundCollectionName);
        }

        public async Task<bool> AddWebSessionAsync(WebSessionData newWebSessionData)
        {
            try
            {
                await _webSessionCollection.InsertOneAsync(newWebSessionData);

                return true;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error in AddWebSessionAsync");
                return false;
            }
        }

        public async Task<WebSessionData?> GetWebSessionByIdAsync(string webSessionId)
        {
            try
            {
                var filter = Builders<WebSessionData>.Filter.Eq(x => x.Id, webSessionId);

                return await _webSessionCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetWebSessionByIdAsync");
                return null;
            }
        }

        public async Task<bool> UpdateStatusAndAddLogAsync(string id, WebSessionStatusEnum failed, WebSessionLog webSessionLog)
        {
            try
            {
                var filter = Builders<WebSessionData>.Filter.Eq(x => x.Id, id);
                var update = Builders<WebSessionData>.Update
                    .Set(x => x.Status, failed)
                    .Push(x => x.Logs, webSessionLog);

                var result = await _webSessionCollection.UpdateOneAsync(filter, update);

                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateStatusAndAddLogAsync");
                return false;
            }
        }

        public async Task<bool> UpdateStatusProcessedBackendWithServerIdAndWebsocketURL(string webSessionId, string sessionId, string websocketUrl)
        {
            try
            {
                var filter = Builders<WebSessionData>.Filter.Eq(x => x.Id, webSessionId);
                var update = Builders<WebSessionData>.Update
                    .Set(x => x.Status, WebSessionStatusEnum.ProcessedBackend)
                    .Set(x => x.SessionId, sessionId)
                    .Set(x => x.SessionWebSocketUrl, websocketUrl);

                var result = await _webSessionCollection.UpdateOneAsync(filter, update);

                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateStatusProcessedBackendWithServerIdAndWebsocketURL");
                return false;
            }
        }

        public async Task<bool> UpdateStatusProcessingBackendWithServerId(string webSessionId, string serverId)
        {
            try
            {
                var filter = Builders<WebSessionData>.Filter.Eq(x => x.Id, webSessionId);
                var update = Builders<WebSessionData>.Update
                    .Set(x => x.Status, WebSessionStatusEnum.ProcessingBackend)
                    .Set(x => x.SessionRegionBackendServerId, serverId);

                var result = await _webSessionCollection.UpdateOneAsync(filter, update);

                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateStatusProcessingBackendWithServerId");
                return false;
            }
        }
    }
}
