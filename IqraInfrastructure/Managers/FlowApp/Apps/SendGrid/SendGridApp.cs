using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.SendGrid.Actions;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.SendGrid
{
    public class SendGridApp : IFlowApp
    {
        public string AppKey => "sendgrid";
        public string Name => "SendGrid";
        public string IconUrl => "https://cdn.simpleicons.org/sendgrid";
        public string? IntegrationType => "sendgrid"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SendGridApp> _logger;

        private const string BaseUrl = "https://api.sendgrid.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public SendGridApp(IHttpClientFactory httpClientFactory, ILogger<SendGridApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new SendEmailAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>();
        }

        public HttpClient CreateClient(string apiKey)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            return client;
        }
    }
}
