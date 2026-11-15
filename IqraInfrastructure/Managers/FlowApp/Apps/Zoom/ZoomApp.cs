using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Zoom.Actions;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Zoom
{
    public class ZoomApp : IFlowApp
    {
        public string AppKey => "zoom";
        public string Name => "Zoom";
        public string IconUrl => "https://cdn.simpleicons.org/zoom";
        public string? IntegrationType => "zoom";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ZoomApp> _logger;

        private const string BaseUrl = "https://api.zoom.us/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public ZoomApp(IHttpClientFactory httpClientFactory, ILogger<ZoomApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new CreateMeetingAction(this)
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
