using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Pipedrive.Actions;
using IqraInfrastructure.Managers.FlowApp.Apps.Pipedrive.Fetchers;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Pipedrive
{
    public class PipedriveApp : IFlowApp
    {
        public string AppKey => "pipedrive";
        public string Name => "Pipedrive";
        public string IconUrl => "https://cdn.simpleicons.org/pipedrive";
        public string? IntegrationType => "pipedrive"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PipedriveApp> _logger;

        private const string BaseUrl = "https://api.pipedrive.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public PipedriveApp(IHttpClientFactory httpClientFactory, ILogger<PipedriveApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new CreateLeadAction(this),
                new CreateActivityAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>
            {
                new ListPipelinesFetcher(this)
            };
        }

        public HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            return client;
        }

        public static string BuildPath(string path, string apiToken)
        {
            var separator = path.Contains('?') ? "&" : "?";
            return $"{path}{separator}api_token={Uri.EscapeDataString(apiToken)}";
        }
    }
}
