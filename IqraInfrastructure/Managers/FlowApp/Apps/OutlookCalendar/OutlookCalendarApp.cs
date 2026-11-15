using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.OutlookCalendar.Actions;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.OutlookCalendar
{
    public class OutlookCalendarApp : IFlowApp
    {
        public string AppKey => "outlook_calendar";
        public string Name => "Outlook Calendar";
        public string IconUrl => "https://cdn.simpleicons.org/microsoftoutlook";
        public string? IntegrationType => "outlook_calendar";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OutlookCalendarApp> _logger;

        private const string BaseUrl = "https://graph.microsoft.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public OutlookCalendarApp(IHttpClientFactory httpClientFactory, ILogger<OutlookCalendarApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new CreateEventAction(this)
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
