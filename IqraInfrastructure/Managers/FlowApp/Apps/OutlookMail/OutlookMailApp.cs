using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.OutlookMail.Actions;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.OutlookMail
{
    public class OutlookMailApp : IFlowApp
    {
        public string AppKey => "outlook_mail";
        public string Name => "Outlook Mail";
        public string IconUrl => "https://cdn.simpleicons.org/microsoftoutlook";
        public string? IntegrationType => "outlook_mail";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OutlookMailApp> _logger;

        private const string BaseUrl = "https://graph.microsoft.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public OutlookMailApp(IHttpClientFactory httpClientFactory, ILogger<OutlookMailApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new SendEmailAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>();
        }

        public HttpClient CreateClient(string accessToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }
    }
}
