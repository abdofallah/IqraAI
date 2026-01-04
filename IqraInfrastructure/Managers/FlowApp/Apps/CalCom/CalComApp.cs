using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Actions;
using IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Fetchers;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.CalCom
{
    public class CalComApp : IFlowApp
    {
        public string AppKey => "cal_com";
        public string Name => "Cal.com";
        public string IconUrl => "https://cal.com/favicon.ico";
        public string? IntegrationType => "cal_com"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CalComApp> _logger;

        private const string BaseUrl = "https://api.cal.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public CalComApp(IHttpClientFactory httpClientFactory, ILogger<CalComApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            // Initialize Actions
            Actions = new List<IFlowAction>
            {
                new AddGuestsAction(this),
                new BookMeetingAction(this),
                new CancelBookingAction(this),
                new GetAllBookingsAction(this),
                new GetBookingAction(this),
                new GetSlotsAction(this),
                new MarkAbsentAction(this),
                new RescheduleBookingAction(this)
            };

            // Initialize Fetchers
            DataFetchers = new List<IFlowDataFetcher>
            {
                new GetEventTypesByIdFetcher(this)
            };
        }

        /// <summary>
        /// Creates an HttpClient with standard Cal.com headers
        /// </summary>
        public HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);

            return client;
        }
    }
}