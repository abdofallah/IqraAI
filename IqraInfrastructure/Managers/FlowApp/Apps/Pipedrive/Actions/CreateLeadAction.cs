using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Pipedrive.Models;
using IqraInfrastructure.Helpers.Integrations;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Pipedrive.Actions
{
    public partial class CreateLeadAction : IFlowAction
    {
        private readonly PipedriveApp _app;

        public CreateLeadAction(PipedriveApp app)
        {
            _app = app;
        }

        public string ActionKey => "CreateLead";
        public string Name => "Create Lead";
        public string Description => "Creates a Person (if name/contact provided) and a Lead linked to them, perfect for capturing an inbound lead during a call.";

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
                var apiToken = integration!.DecryptedFields["ApiToken"];
                var client = _app.CreateClient();

                var title = input.GetProperty("title").GetString();
                if (string.IsNullOrWhiteSpace(title))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Lead title is required.");
                }

                int? personId = null;

                // 1. Optionally create a Person to attach the lead to.
                var personName = input.TryGetProperty("personName", out var nameProp) ? nameProp.GetString() : null;
                if (!string.IsNullOrWhiteSpace(personName))
                {
                    var personRequest = new CreatePersonRequest { Name = personName! };

                    if (input.TryGetProperty("personEmail", out var emailProp) && !string.IsNullOrWhiteSpace(emailProp.GetString()))
                        personRequest.Email = new List<string> { emailProp.GetString()! };
                    if (input.TryGetProperty("personPhone", out var phoneProp) && !string.IsNullOrWhiteSpace(phoneProp.GetString()))
                        personRequest.Phone = new List<string> { phoneProp.GetString()! };

                    var personHttp = new HttpRequestMessage(HttpMethod.Post, PipedriveApp.BuildPath("/v1/persons", apiToken))
                    {
                        Content = new StringContent(JsonSerializer.Serialize(personRequest), Encoding.UTF8, "application/json")
                    };
                    var personResponse = await client.SendAsync(personHttp);
                    var personContent = await personResponse.Content.ReadAsStringAsync();

                    if (!personResponse.IsSuccessStatusCode)
                    {
                        return ProviderReplyHelper.ActionFailure("Pipedrive person error", personResponse.StatusCode, personContent);
                    }

                    var personResult = JsonSerializer.Deserialize<PipedriveResponse<PersonData>>(personContent);
                    personId = personResult?.Data?.Id;
                }

                // 2. Create the Lead.
                var leadRequest = new CreateLeadRequest { Title = title!, PersonId = personId };

                if (input.TryGetProperty("value", out var valueProp) && valueProp.ValueKind == JsonValueKind.Number)
                {
                    var currency = input.TryGetProperty("currency", out var curProp) && !string.IsNullOrWhiteSpace(curProp.GetString())
                        ? curProp.GetString()!
                        : "USD";
                    leadRequest.Value = new LeadValue { Amount = valueProp.GetDecimal(), Currency = currency };
                }

                var leadHttp = new HttpRequestMessage(HttpMethod.Post, PipedriveApp.BuildPath("/v1/leads", apiToken))
                {
                    Content = new StringContent(JsonSerializer.Serialize(leadRequest), Encoding.UTF8, "application/json")
                };
                var leadResponse = await client.SendAsync(leadHttp);
                var leadContent = await leadResponse.Content.ReadAsStringAsync();

                if (!leadResponse.IsSuccessStatusCode)
                {
                    return ProviderReplyHelper.ActionFailure("Pipedrive lead error", leadResponse.StatusCode, leadContent);
                }

                var leadResult = JsonSerializer.Deserialize<PipedriveResponse<LeadData>>(leadContent);
                return ActionExecutionResult.SuccessPort("success", new
                {
                    leadId = leadResult?.Data?.Id,
                    title = leadResult?.Data?.Title,
                    personId
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}
