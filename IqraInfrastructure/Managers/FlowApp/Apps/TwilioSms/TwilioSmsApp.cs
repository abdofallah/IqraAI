using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.TwilioSms.Actions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace IqraInfrastructure.Managers.FlowApp.Apps.TwilioSms
{
    public class TwilioSmsApp : IFlowApp
    {
        public string AppKey => "twilio_sms";
        public string Name => "Twilio SMS";
        public string IconUrl => "https://cdn.simpleicons.org/twilio";
        public string? IntegrationType => "twilio_sms"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TwilioSmsApp> _logger;

        private const string BaseUrl = "https://api.twilio.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public TwilioSmsApp(IHttpClientFactory httpClientFactory, ILogger<TwilioSmsApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new SendSmsAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>();
        }

        public HttpClient CreateClient(string accountSid, string authToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            return client;
        }
    }
}
