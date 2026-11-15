using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.GoogleCalendar.Actions;
using IqraInfrastructure.Managers.FlowApp.Apps.GoogleCalendar.Fetchers;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.GoogleCalendar
{
    public class GoogleCalendarApp : IFlowApp
    {
        public string AppKey => "google_calendar";
        public string Name => "Google Calendar";
        public string IconUrl => "https://cdn.simpleicons.org/googlecalendar";
        public string? IntegrationType => "google_calendar";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GoogleCalendarApp> _logger;

        private const string BaseUrl = "https://www.googleapis.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public GoogleCalendarApp(IHttpClientFactory httpClientFactory, ILogger<GoogleCalendarApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new CreateEventAction(this),
                new ListEventsAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>
            {
                new ListCalendarsFetcher(this)
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
