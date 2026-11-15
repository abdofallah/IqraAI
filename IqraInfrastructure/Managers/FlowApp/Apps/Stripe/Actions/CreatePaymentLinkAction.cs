using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Stripe.Actions
{
    public partial class CreatePaymentLinkAction : IFlowAction
    {
        private readonly StripeApp _app;

        public CreatePaymentLinkAction(StripeApp app)
        {
            _app = app;
        }

        public string ActionKey => "CreatePaymentLink";
        public string Name => "Create Payment Link";
        public string Description => "Creates a shareable Stripe Payment Link for an existing Price (the agent can SMS or email the URL to the caller).";

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "success", Label = "Success" },
                new ActionOutputPort { Key = "error", Label = "Error" }
            };
        }

        public async Task<ActionExecutionResult> ExecuteAsync(JsonElement input, BusinessAppIntegrationDecryptedModel? integration)
        {
            try
            {
                var secretKey = integration!.DecryptedFields["SecretKey"];
                var client = _app.CreateClient(secretKey);

                var priceId = input.GetProperty("priceId").GetString();
                if (string.IsNullOrWhiteSpace(priceId))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "A Stripe Price ID is required.");
                }

                int quantity = 1;
                if (input.TryGetProperty("quantity", out var qtyProp) && qtyProp.ValueKind == JsonValueKind.Number)
                {
                    quantity = Math.Max(1, qtyProp.GetInt32());
                }

                var form = new Dictionary<string, string>
                {
                    ["line_items[0][price]"] = priceId,
                    ["line_items[0][quantity]"] = quantity.ToString()
                };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/payment_links")
                {
                    Content = new FormUrlEncodedContent(form)
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Stripe Error: {response.StatusCode} - {content}");
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                return ActionExecutionResult.SuccessPort("success", new
                {
                    id = root.TryGetProperty("id", out var id) ? id.GetString() : null,
                    url = root.TryGetProperty("url", out var url) ? url.GetString() : null,
                    active = root.TryGetProperty("active", out var active) && active.GetBoolean()
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}
