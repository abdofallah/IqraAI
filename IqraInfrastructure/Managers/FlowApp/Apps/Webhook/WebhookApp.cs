using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Webhook.Actions;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Webhook
{
    public class WebhookApp : IFlowApp
    {
        public string AppKey => "webhook";
        public string Name => "Webhook (Make / Zapier / n8n)";
        public string IconUrl => "https://cdn.simpleicons.org/webhooks/000000";

        public string? IntegrationType => null;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WebhookApp> _logger;

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public WebhookApp(IHttpClientFactory httpClientFactory, ILogger<WebhookApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new SendRequestAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>();
        }

        public HttpClient CreateClient()
        {
            return _httpClientFactory.CreateClient();
        }
    }
}
