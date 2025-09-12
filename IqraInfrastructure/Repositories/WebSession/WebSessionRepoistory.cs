using IqraCore.Entities.WebSession;

namespace IqraInfrastructure.Repositories.WebSession
{
    public class WebSessionRepoistory
    {

        public WebSessionRepoistory()
        {

        }

        internal async Task<bool> AddWebSessionAsync(WebSessionData newWebSessionData)
        {
            throw new NotImplementedException();
        }

        internal async Task<WebSessionData?> GetWebSessionByIdAsync(string webSessionId)
        {
            throw new NotImplementedException();
        }

        internal async Task UpdateStatusAndAddLogAsync(string id, WebSessionStatusEnum failed, WebSessionLog webSessionLog)
        {
            throw new NotImplementedException();
        }

        internal async Task UpdateStatusProcessedBackendWithServerIdAndWebhookURL(string webSessionId, string sessionId, string webhookUrl)
        {
            throw new NotImplementedException();
        }

        internal async Task UpdateStatusProcessingBackendWithServerId(string webSessionId, string serverId)
        {
            throw new NotImplementedException();
        }
    }
}
