using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Stripe.Actions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Stripe
{
    public class StripeApp : IFlowApp
    {
        public string AppKey => "stripe";
        public string Name => "Stripe";
        public string IconUrl => "https://cdn.simpleicons.org/stripe";
        public string? IntegrationType => "stripe"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StripeApp> _logger;

        private const string BaseUrl = "https://api.stripe.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public StripeApp(IHttpClientFactory httpClientFactory, ILogger<StripeApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new CreatePaymentLinkAction(this),
                new FindCustomerAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>();
        }

        public HttpClient CreateClient(string secretKey)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{secretKey}:"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            return client;
        }
    }
}
