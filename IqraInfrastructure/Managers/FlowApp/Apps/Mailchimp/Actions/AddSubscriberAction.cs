using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Mailchimp.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Mailchimp.Actions
{
    public partial class AddSubscriberAction : IFlowAction
    {
        private readonly MailchimpApp _app;

        public AddSubscriberAction(MailchimpApp app)
        {
            _app = app;
        }

        public string ActionKey => "AddSubscriber";
        public string Name => "Add or Update Subscriber";
        public string Description => "Adds a contact to a Mailchimp audience, or updates them if they already exist (idempotent upsert by email).";

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
                var apiKey = integration!.DecryptedFields["ApiKey"];
                var client = _app.CreateClient(apiKey);

                var listId = input.GetProperty("listId").GetString();
                var email = input.GetProperty("email").GetString();

                if (string.IsNullOrWhiteSpace(listId) || string.IsNullOrWhiteSpace(email))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Audience and email are required.");
                }

                var request = new UpsertMemberRequest
                {
                    EmailAddress = email,
                    StatusIfNew = input.TryGetProperty("doubleOptIn", out var optIn) && optIn.ValueKind == JsonValueKind.True
                        ? "pending"
                        : "subscribed"
                };

                var mergeFields = new Dictionary<string, object?>();
                if (input.TryGetProperty("firstName", out var fn) && !string.IsNullOrWhiteSpace(fn.GetString()))
                    mergeFields["FNAME"] = fn.GetString();
                if (input.TryGetProperty("lastName", out var ln) && !string.IsNullOrWhiteSpace(ln.GetString()))
                    mergeFields["LNAME"] = ln.GetString();
                if (input.TryGetProperty("mergeFields", out var extra) && extra.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in extra.EnumerateObject())
                    {
                        mergeFields[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.GetRawText();
                    }
                }
                if (mergeFields.Count > 0) request.MergeFields = mergeFields;

                var subscriberHash = Md5Hex(email.Trim().ToLowerInvariant());

                var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/3.0/lists/{Uri.EscapeDataString(listId)}/members/{subscriberHash}")
                {
                    Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Mailchimp Error: {response.StatusCode} - {content}");
                }

                var result = JsonSerializer.Deserialize<MemberResponse>(content);
                return ActionExecutionResult.SuccessPort("success", new
                {
                    id = result?.Id,
                    email = result?.EmailAddress,
                    status = result?.Status
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }

        private static string Md5Hex(string value)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
