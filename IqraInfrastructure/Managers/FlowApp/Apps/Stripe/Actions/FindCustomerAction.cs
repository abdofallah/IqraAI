using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Helpers.Integrations;
using System.Text.Json;
using System.Web;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Stripe.Actions
{
    public partial class FindCustomerAction : IFlowAction
    {
        private readonly StripeApp _app;

        public FindCustomerAction(StripeApp app)
        {
            _app = app;
        }

        public string ActionKey => "FindCustomer";
        public string Name => "Find Customer";
        public string Description => "Looks up a Stripe customer by email so the agent can confirm or reference their account.";

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "found", Label = "Found" },
                new ActionOutputPort { Key = "not_found", Label = "Not Found" },
                new ActionOutputPort { Key = "error", Label = "Error" }
            };
        }

        public async Task<ActionExecutionResult> ExecuteAsync(JsonElement input, BusinessAppIntegrationDecryptedModel? integration)
        {
            try
            {
                var secretKey = integration!.DecryptedFields["SecretKey"];
                var client = _app.CreateClient(secretKey);

                var email = input.GetProperty("email").GetString();
                if (string.IsNullOrWhiteSpace(email))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Customer email is required.");
                }

                var query = HttpUtility.ParseQueryString(string.Empty);
                query["email"] = email;
                query["limit"] = "1";

                var response = await client.GetAsync($"/v1/customers?{query}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ProviderReplyHelper.ActionFailure("Stripe Error", response.StatusCode, content);
                }

                using var doc = JsonDocument.Parse(content);
                var data = doc.RootElement.GetProperty("data");

                if (data.GetArrayLength() == 0)
                {
                    return ActionExecutionResult.SuccessPort("not_found", new { email });
                }

                var customer = data[0];
                return ActionExecutionResult.SuccessPort("found", new
                {
                    id = customer.TryGetProperty("id", out var id) ? id.GetString() : null,
                    name = customer.TryGetProperty("name", out var name) ? name.GetString() : null,
                    email = customer.TryGetProperty("email", out var em) ? em.GetString() : null,
                    phone = customer.TryGetProperty("phone", out var phone) ? phone.GetString() : null
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}
