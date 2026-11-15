using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Webhook.Actions;
using IqraInfrastructure.Services;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Webhook
{
    public class WebhookApp : IFlowApp
    {
        public const string HttpClientName = "FlowAppWebhookClient";

        public string AppKey => "webhook";
        public string Name => "Webhook (Make / Zapier / n8n)";
        public string IconUrl => "https://cdn.simpleicons.org/webhooks/000000";

        public string? IntegrationType => null;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WebhookApp> _logger;
        private readonly EndpointSecurityService? _endpointSecurityService;

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public WebhookApp(IHttpClientFactory httpClientFactory, ILogger<WebhookApp> logger, EndpointSecurityService? endpointSecurityService = null)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _endpointSecurityService = endpointSecurityService;

            Actions = new List<IFlowAction>
            {
                new SendRequestAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>();
        }

        public HttpClient CreateClient()
        {
            return _httpClientFactory.CreateClient(HttpClientName);
        }

        public Task<(bool isAllowed, string? reason)> IsEndpointAllowedAsync(string url)
        {
            if (_endpointSecurityService == null)
            {
                return Task.FromResult<(bool isAllowed, string? reason)>((false, "Webhook requests are not available on this server."));
            }

            return _endpointSecurityService.IsEndpointAllowedAsync(url);
        }
    }
}
