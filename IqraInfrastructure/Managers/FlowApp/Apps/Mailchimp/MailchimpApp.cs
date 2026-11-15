using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Mailchimp.Actions;
using IqraInfrastructure.Managers.FlowApp.Apps.Mailchimp.Fetchers;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Mailchimp
{
    public class MailchimpApp : IFlowApp
    {
        public string AppKey => "mailchimp";
        public string Name => "Mailchimp";
        public string IconUrl => "https://cdn.simpleicons.org/mailchimp";
        public string? IntegrationType => "mailchimp"; // Matches Admin Dashboard Definition

        private static readonly Regex DataCenterPattern = new(@"^[A-Za-z0-9]+\z", RegexOptions.Compiled);

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
            var dataCenter = dashIndex < 0 ? string.Empty : apiKey.Substring(dashIndex + 1);
            if (!DataCenterPattern.IsMatch(dataCenter))
            {
                throw new InvalidOperationException("Invalid Mailchimp API key: it must end with its data-center suffix, for example -us21.");
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri($"https://{dataCenter}.api.mailchimp.com/");
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"anystring:{apiKey}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            return client;
        }
    }
}
