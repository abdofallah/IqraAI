using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.GoogleSheets.Actions;
using IqraInfrastructure.Managers.FlowApp.Apps.GoogleSheets.Fetchers;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.GoogleSheets
{
    public class GoogleSheetsApp : IFlowApp
    {
        public string AppKey => "google_sheets";
        public string Name => "Google Sheets";
        public string IconUrl => "https://cdn.simpleicons.org/googlesheets";
        public string? IntegrationType => "google_sheets";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GoogleSheetsApp> _logger;

        private const string BaseUrl = "https://www.googleapis.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public GoogleSheetsApp(IHttpClientFactory httpClientFactory, ILogger<GoogleSheetsApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new AppendRowAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>
            {
                new ListSpreadsheetsFetcher(this)
            };
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
