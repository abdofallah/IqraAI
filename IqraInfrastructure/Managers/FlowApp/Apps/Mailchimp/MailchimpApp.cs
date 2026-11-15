using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Mailchimp.Actions;
using IqraInfrastructure.Managers.FlowApp.Apps.Mailchimp.Fetchers;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Mailchimp
{
    public class MailchimpApp : IFlowApp
    {
        public string AppKey => "mailchimp";
        public string Name => "Mailchimp";
        public string IconUrl => "https://cdn.simpleicons.org/mailchimp";
        public string? IntegrationType => "mailchimp"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MailchimpApp> _logger;

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public MailchimpApp(IHttpClientFactory httpClientFactory, ILogger<MailchimpApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new AddSubscriberAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>
            {
                new ListAudiencesFetcher(this)
            };
        }

        public HttpClient CreateClient(string apiKey)
        {
            var dashIndex = apiKey.LastIndexOf('-');
            if (dashIndex < 0 || dashIndex == apiKey.Length - 1)
            {
                throw new InvalidOperationException("Invalid Mailchimp API key: missing data-center suffix (e.g. '-us21').");
            }

            var dataCenter = apiKey.Substring(dashIndex + 1);

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri($"https://{dataCenter}.api.mailchimp.com/");
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"anystring:{apiKey}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            return client;
        }
    }
}
