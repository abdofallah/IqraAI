using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Zendesk.Actions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Zendesk
{
    public class ZendeskApp : IFlowApp
    {
        public string AppKey => "zendesk";
        public string Name => "Zendesk";
        public string IconUrl => "https://cdn.simpleicons.org/zendesk";
        public string? IntegrationType => "zendesk"; // Matches Admin Dashboard Definition

        private static readonly Regex SubdomainPattern = new(@"^[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\z", RegexOptions.Compiled);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ZendeskApp> _logger;

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public ZendeskApp(IHttpClientFactory httpClientFactory, ILogger<ZendeskApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new CreateTicketAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>();
        }

        public HttpClient CreateClient(string subdomain, string email, string apiToken)
        {
            var trimmedSubdomain = subdomain.Trim();
            if (!SubdomainPattern.IsMatch(trimmedSubdomain))
            {
                throw new InvalidOperationException("The Zendesk subdomain must be only the first part of your Zendesk address, for example acme for acme.zendesk.com.");
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri($"https://{trimmedSubdomain}.zendesk.com/");
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{email}/token:{apiToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            return client;
        }
    }
}
