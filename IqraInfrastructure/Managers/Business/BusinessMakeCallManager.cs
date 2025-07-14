using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.MakeCalls;
using IqraInfrastructure.Managers.Region;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using PhoneNumbers;
using IqraCore.Entities.Helper.Call.Outbound;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Utilities;
using nietras.SeparatedValues;
using IqraCore.Entities.Call.Outbound;
using Microsoft.Extensions.Logging;
using IqraInfrastructure.Repositories.Call;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessMakeCallManager
    {
        private readonly ILogger<BusinessMakeCallManager> _logger;
        private readonly BusinessManager _parentBusinessManager;
        private readonly RegionManager _regionManager;
        private readonly OutboundCallCampaignRepository _outboundCallCampaignRepository;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepository;

        public BusinessMakeCallManager(
            ILogger<BusinessMakeCallManager> logger,
            BusinessManager parentBusinessManager,
            RegionManager regionManager,
            OutboundCallCampaignRepository outboundCallCampaignRepository,
            OutboundCallQueueRepository outboundCallQueueRepository
        )
        {
            _logger = logger;
            _parentBusinessManager = parentBusinessManager;
            _regionManager = regionManager;
            _outboundCallCampaignRepository = outboundCallCampaignRepository;
            _outboundCallQueueRepository = outboundCallQueueRepository;
        }

        public async Task<FunctionReturnResult> QueueCallInitiationRequestAsync(BusinessData businessData, MakeCallRequestDto callConfig, IFormFile? bulkCsvFile)
        {
            var result = new FunctionReturnResult();

            long businessId = businessData.Id;
            string businessDefaultLanguage = businessData.DefaultLanguage;
            List<string> businessLanguages = businessData.Languages;

            // General
            if (callConfig.General == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:1",
                    "Missing 'General' in configuration."
                );
            }
            if (string.IsNullOrEmpty(callConfig.General.Identifier))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:2",
                    "Missing 'General.Identifier' in configuration."
                );
            }
            if (string.IsNullOrEmpty(callConfig.General.Description))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:3",
                    "Missing 'General.Description' in configuration."
                );
            }

            // Number Details
            if (callConfig.NumberDetails == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:4",
                    "Missing 'NumberDetails' in configuration."
                );
            }
            if (callConfig.NumberDetails.Type == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:5",
                    "Invalid 'NumberDetails.Type' in configuration."
                );
            }
            if (string.IsNullOrWhiteSpace(callConfig.NumberDetails.FromNumberId))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:6",
                    "Missing 'NumberDetails.FromNumberId' in configuration."
                );
            }
            BusinessNumberData? defaultFromCallNumberData = await _parentBusinessManager.GetNumberManager().GetBusinessNumberById(businessId, callConfig.NumberDetails.FromNumberId);
            if (defaultFromCallNumberData == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:7",
                    "Unable to find 'NumberDetails.FromNumberId' in business data."
                );
            }
            if (callConfig.NumberDetails.Type == OutboundCallNumberType.Single)
            {
                if (string.IsNullOrWhiteSpace(callConfig.NumberDetails.ToNumber))
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:8",
                        "Missing 'NumberDetails.ToNumber' in configuration for single call type."
                    );
                }

                PhoneNumber parsedPhoneNumber = PhoneNumberUtil.GetInstance().Parse(callConfig.NumberDetails.ToNumber, "ZZ");
                if (!PhoneNumberUtil.GetInstance().IsValidNumber(parsedPhoneNumber))
                {
                    result.Code = "ForwardCallInitiationRequestAsync:9";
                    result.Message = "Number validation failed for 'NumberDetails.ToNumber'.";
                    return result;
                }
            }
            else if (callConfig.NumberDetails.Type == OutboundCallNumberType.Bulk)
            {
                if (bulkCsvFile == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:10",
                        "Missing 'bulk_file' for bulk call type."
                    );
                }
                // we validate the bulk calls at the end of the class validation
            }
            // Configuration
            if (callConfig.Configuration == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:12",
                    "Missing 'Configuration' in configuration."
                );
            }
            // Configuration.Schedule
            if (callConfig.Configuration.Schedule == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:13",
                    "Missing 'Configuration.Schedule' in configuration."
                );
            }
            if (callConfig.Configuration.Schedule.Type == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:14",
                    "Missing 'Configuration.Schedule.Type' in configuration."
                );
            }
            if (callConfig.Configuration.Schedule.Type == OutboundCallScheduleType.Scheduled)
            {
                if (callConfig.Configuration.Schedule.DateTimeUTC == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:15",
                        "Missing 'Configuration.Schedule.DateTimeUTC' in configuration."
                    );
                }

                if (callConfig.Configuration.Schedule.DateTimeUTC.Value.AddMinutes(10) < DateTime.UtcNow)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:16",
                        "Cannot schedule call in the past (must be atleast 10 minutes in the future)."
                    );
                }
            }
            // Configuration.RetryDecline
            if (callConfig.Configuration.RetryDecline == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:17",
                    "Missing 'Configuration.RetryDecline' in configuration."
                );
            }
            if (callConfig.Configuration.RetryDecline.Enabled == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:18",
                    "Missing 'Configuration.RetryDecline.Enabled' in configuration."
                );
            }
            if (callConfig.Configuration.RetryDecline.Enabled == true)
            {
                if (callConfig.Configuration.RetryDecline.Count == null || callConfig.Configuration.RetryDecline.Count < 1 || callConfig.Configuration.RetryDecline.Count > 5) // todo can have retires max count based on user/business
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:19",
                        "Missing or invalid 'Configuration.RetryDecline.Count' in configuration. Must be between 1 and 5."
                    );
                }
                if (callConfig.Configuration.RetryDecline.Delay == null || callConfig.Configuration.RetryDecline.Delay < 1)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:20",
                        "Missing or invalid 'Configuration.RetryDecline.Delay' in configuration. Must be greater than 0."
                    );
                }
                if (callConfig.Configuration.RetryDecline.Unit == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:21",
                        "Missing or invalid 'Configuration.RetryDecline.Unit' in configuration."
                    );
                }
            }
            // Configuration.RetryMiss
            if (callConfig.Configuration.RetryMiss == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:22",
                    "Missing 'Configuration.RetryMiss' in configuration."
                );
            }
            if (callConfig.Configuration.RetryMiss.Enabled == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:23",
                    "Missing 'Configuration.RetryMiss.Enabled' in configuration."
                );
            }
            if (callConfig.Configuration.RetryMiss.Enabled == true)
            {
                if (callConfig.Configuration.RetryMiss.Count == null || callConfig.Configuration.RetryMiss.Count < 1 || callConfig.Configuration.RetryMiss.Count > 5) // todo can have retires max count based on user/business, used below in a function too
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:24",
                        "Missing or invalid 'Configuration.RetryMiss.Count' in configuration. Must be between 1 and 5."
                    );
                }
                if (callConfig.Configuration.RetryMiss.Delay == null || callConfig.Configuration.RetryMiss.Delay < 1)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:25",
                        "Missing or invalid 'Configuration.RetryMiss.Delay' in configuration. Must be greater than 0."
                    );
                }
                if (callConfig.Configuration.RetryMiss.Unit == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:26",
                        "Missing or invalid 'Configuration.RetryMiss.Unit' in configuration."
                    );
                }
            }
            // Configuration.Timeouts
            if (callConfig.Configuration.Timeouts == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:27",
                    "Missing 'Configuration.Timeouts' in configuration."
                );
            }
            if (callConfig.Configuration.Timeouts.NotifyOnSilenceMS == null || callConfig.Configuration.Timeouts.NotifyOnSilenceMS < 0)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:28",
                    "Missing or invalid 'Configuration.Timeouts.NotifyOnSilenceMS' in configuration. Set 0 for disabled."
                );
            }
            if (callConfig.Configuration.Timeouts.EndOnSilenceMS == null || callConfig.Configuration.Timeouts.EndOnSilenceMS < 0)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:29",
                    "Missing or invalid 'Configuration.Timeouts.EndOnSilenceMS' in configuration. Set 0 for disabled."
                );
            }
            if (callConfig.Configuration.Timeouts.MaxCallTimeS == null || callConfig.Configuration.Timeouts.MaxCallTimeS < 1 || callConfig.Configuration.Timeouts.MaxCallTimeS > 1800) // this is also used in BusinessRoutings so we should make this a const
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:30",
                    "Missing or invalid 'Configuration.Timeouts.CallTimeoutMS' in configuration. Must be between 1 and 1800 seconds."
                );
            }
            // AgentSettings
            if (callConfig.AgentSettings == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:31",
                    "Missing 'AgentSettings' in configuration."
                );
            }
            if (string.IsNullOrWhiteSpace(callConfig.AgentSettings.AgentId))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:32",
                    "Missing or invalid 'AgentSettings.AgentId' in configuration."
                );
            }
            BusinessAppAgent? defaultAgentData = await _parentBusinessManager.GetAgentsManager().GetAgentById(businessId, callConfig.AgentSettings.AgentId);
            if (defaultAgentData == null) {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:33",
                    $"Default selected agent with id {callConfig.AgentSettings.AgentId} not found in business data."
                );
            }
            if (string.IsNullOrWhiteSpace(callConfig.AgentSettings.ScriptId))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:34",
                    "Missing or invalid 'AgentSettings.ScriptId' in configuration."
                );
            }
            BusinessAppAgentScript? selectedDefaultAgentScriptData = defaultAgentData.Scripts.Find(s => s.Id == callConfig.AgentSettings.ScriptId);
            if (selectedDefaultAgentScriptData == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:35",
                    $"Default selected agent selected script with id {callConfig.AgentSettings.ScriptId} not found in business data."
                );
            }
            if (string.IsNullOrEmpty(callConfig.AgentSettings.LanguageCode))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:36",
                    "Missing or invalid 'AgentSettings.LanguageCode' in configuration."
                );
            }
            if (businessLanguages.Contains(callConfig.AgentSettings.LanguageCode) == false)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:37",
                    $"Language code '{callConfig.AgentSettings.LanguageCode}' not found in business languages."
                );
            }
            if (callConfig.AgentSettings.Timezones == null || callConfig.AgentSettings.Timezones.Count == 0)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:38",
                    "Missing or invalid 'AgentSettings.Timezones' in configuration."
                );
            }
            foreach (var timezone in callConfig.AgentSettings.Timezones)
            {
                if (TimeZoneHelper.ValidateOffsetString(timezone) == false)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:38.1",
                        $"Failed to validate 'AgentSettings.Timezones' in configuration. Timezone: {timezone}"
                    );
                }
            }    
            if (callConfig.AgentSettings.IncludeFromNumberInContext == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:39",
                    "Missing or invalid 'AgentSettings.IncludeFromNumberInContext' in configuration."
                );
            }
            if (callConfig.AgentSettings.IncludeToNumberInContext == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:40",
                    "Missing or invalid 'AgentSettings.IncludeToNumberInContext' in configuration."
                );
            }
            // AgentSettings.Interruption
            if (callConfig.AgentSettings.Interruption == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:41",
                    "Missing 'AgentSettings.Interruption' in configuration."
                );
            }
            if (callConfig.AgentSettings.Interruption.Type == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:42",
                    "Missing or invalid 'AgentSettings.Interruption.Type' in configuration."
                );
            }
            if (callConfig.AgentSettings.Interruption.Type == AgentInterruptionTypeENUM.TurnByTurn)
            {
                if (callConfig.AgentSettings.Interruption.UseInterruptedResponseInNextTurn == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:43",
                        "Missing or invalid 'AgentSettings.Interruption.UseInterruptedResponseInNextTurn' in configuration for intertuption type 'Turn by Turn'."
                    );
                }
            }
            else if (callConfig.AgentSettings.Interruption.Type == AgentInterruptionTypeENUM.InterruptibleViaVAD)
            {
                if (callConfig.AgentSettings.Interruption.VadDurationMS == null || callConfig.AgentSettings.Interruption.VadDurationMS < 100) // todo this is also set in BusinessRoutingsmanager, make it const
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:44",
                        "Missing or invalid 'AgentSettings.Interruption.VadDurationMS' in configuration. Must be at least 100 ms."
                    );
                }
            }
            else if (callConfig.AgentSettings.Interruption.Type == AgentInterruptionTypeENUM.InterruptibleViaAI)
            {
                if (callConfig.AgentSettings.Interruption.UseAgentLLM == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:45",
                        "Missing or invalid 'AgentSettings.Interruption.UseAgentLLM' in configuration for intertuption type 'Via AI'."
                    );
                }
                if (callConfig.AgentSettings.Interruption.UseAgentLLM == false)
                {
                    if (string.IsNullOrWhiteSpace(callConfig.AgentSettings.Interruption.LLMIntegrationId))
                    {
                        return result.SetFailureResult(
                            "ForwardCallInitiationRequestAsync:46",
                            "Missing or invalid 'AgentSettings.Interruption.LlmIntegrationId' in configuration for intertuption type 'Via AI' when 'UseAgentLLM' is false."
                        );
                    }

                    // todo check if llm integration exists
                    // validate integration config

                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:47",
                        "Interruption type 'Via AI' not implemented yet."
                    );
                }
            }
            // Actions
            if (callConfig.Actions == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:48",
                    "Missing 'Actions' in configuration."
                );
            }
            // Actions.Declines
            if (callConfig.Actions.Declined == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:49",
                    "Missing 'Actions.Declined' in configuration."
                );
            }
            var ringingToolValidationResult = await ValidateActionData(businessId, businessDefaultLanguage, callConfig.Actions.Declined, "Declined");
            if (ringingToolValidationResult.Success == false)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:" + ringingToolValidationResult.Code,
                    ringingToolValidationResult.Message
                );
            }
            // Actions.Missed
            if (callConfig.Actions.Missed == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:50",
                    "Missing 'Actions.Missed' in configuration."
                );
            }
            ringingToolValidationResult = await ValidateActionData(businessId, businessDefaultLanguage, callConfig.Actions.Missed, "Missed");
            if (ringingToolValidationResult.Success == false)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:" + ringingToolValidationResult.Code,
                    ringingToolValidationResult.Message
                );
            }
            // Actions.Answered
            if (callConfig.Actions.Answered == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:51",
                    "Missing 'Actions.Answered' in configuration."
                );
            }
            ringingToolValidationResult = await ValidateActionData(businessId, businessDefaultLanguage, callConfig.Actions.Answered, "Answered");
            if (ringingToolValidationResult.Success == false)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:" + ringingToolValidationResult.Code,
                    ringingToolValidationResult.Message
                );
            }
            // Actions.Ended
            if (callConfig.Actions.Ended == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:52",
                    "Missing 'Actions.Ended' in configuration."
                );
            }
            ringingToolValidationResult = await ValidateActionData(businessId, businessDefaultLanguage, callConfig.Actions.Ended, "Ended");
            if (ringingToolValidationResult.Success == false)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:" + ringingToolValidationResult.Code,
                    ringingToolValidationResult.Message
                );
            }

            // first check if bulk data converted/valid
            FunctionReturnResult<(List<OutboundBulkCallRowData> callsRows, Dictionary<string, string> numberRegions)?>? bulkCallFileResult = null;
            if (callConfig.NumberDetails.Type == OutboundCallNumberType.Bulk)
            {
                bulkCallFileResult = await ValidateAndBuildBulkCSVCallFile(businessData, bulkCsvFile!, callConfig, defaultFromCallNumberData, defaultAgentData);
                if (!bulkCallFileResult.Success || bulkCallFileResult.Data == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:" + bulkCallFileResult.Code,
                        bulkCallFileResult.Message
                    );
                }
            }

            // Create a campaign
            OutboundCallCampaignData outboundCallCampaignData = new OutboundCallCampaignData()
            {
                Id = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                CallRequestData = callConfig,
                IsBulkCall = (callConfig.NumberDetails.Type == OutboundCallNumberType.Bulk),
                CallQueueIds = new List<string>()
            };
            var insertCampaignResult = await _outboundCallCampaignRepository.CreateOutboundCallCampaignAsync(outboundCallCampaignData);
            if (!insertCampaignResult)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:53",
                    "Failed to create outbound call campaign."
                );
            }

            // Forward the Call To Proxy
            if (callConfig.NumberDetails.Type == OutboundCallNumberType.Single)
            {
                var singleForwardResult = await QueueSingleCall(businessData, callConfig, defaultAgentData, defaultFromCallNumberData, outboundCallCampaignData.Id);
                if (!singleForwardResult.Success)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:" + singleForwardResult.Code,
                        singleForwardResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            else if (callConfig.NumberDetails.Type == OutboundCallNumberType.Bulk)
            {
                var bulkForwardResult = await QueueBulkCalls(businessData, callConfig, defaultAgentData, defaultFromCallNumberData, bulkCallFileResult!.Data!.Value.callsRows, bulkCallFileResult!.Data!.Value.numberRegions, outboundCallCampaignData.Id);
                if (!bulkForwardResult.Success)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:" + bulkForwardResult.Code,
                        bulkForwardResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            else
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:54",
                    "Invalid number type."
                );
            }
        }

        private async Task<FunctionReturnResult> QueueSingleCall(BusinessData businessData, MakeCallRequestDto callConfig, BusinessAppAgent businessAppAgent, BusinessNumberData businessNumberData, string outboundCallCampaignId)
        {
            var result = new FunctionReturnResult();

            OutboundCallQueueData outboundCallQueueData = BuildOutboundCallQueueData(businessData, callConfig, businessAppAgent, businessNumberData, outboundCallCampaignId, null, null);

            // Enqueue outbound call queue
            string? callQueueIdResult = await _outboundCallQueueRepository.EnqueueOutboundCallAsync(outboundCallQueueData);
            if (callQueueIdResult == null)
            {
                return result.SetFailureResult(
                    "ForwardSingleCallToRegionProxy:1",
                    "Failed to enqueue outbound call queue."
                );
            }

            // Add queue to campaign
            var queueToCampaignResult = await _outboundCallCampaignRepository.AddQueueToCampaignAsync(outboundCallQueueData.Id, outboundCallCampaignId);
            if (!queueToCampaignResult)
            {
                return result.SetFailureResult(
                    "ForwardSingleCallToRegionProxy:2",
                    "Failed to add outbound call queue to campaign."
                );
            }

            var selectedRegion = businessNumberData.RegionId;
            var regionData = await _regionManager.GetRegionById(selectedRegion);
            if (regionData == null)
            {
                await _outboundCallQueueRepository.UpdateCallStatusAsync(outboundCallQueueData.Id, CallQueueStatusEnum.Failed, new CallQueueLog() { Type = CallQueueLogTypeEnum.Error, Message = "Phone number region not found." });
                return result.SetFailureResult(
                    "ForwardSingleCallToRegionProxy:1",
                    "Phone number region not found."
                );
            }

            var anyProxyServerForRegion = regionData.Servers.Find(s => s.Type == ServerTypeEnum.Proxy);
            if (anyProxyServerForRegion == null)
            {
                await _outboundCallQueueRepository.UpdateCallStatusAsync(outboundCallQueueData.Id, CallQueueStatusEnum.Failed, new CallQueueLog() { Type = CallQueueLogTypeEnum.Error, Message = "No proxy server found for phone number region." });
                return result.SetFailureResult(
                    "ForwardSingleCallToRegionProxy:2",
                    "No proxy server found for phone number region."
                );
            }

            return result.SetSuccessResult();
        }

        private async Task<FunctionReturnResult> QueueBulkCalls(BusinessData businessData, MakeCallRequestDto callConfig, BusinessAppAgent businessAppAgent, BusinessNumberData businessNumberData, List<OutboundBulkCallRowData> callsRows, Dictionary<string, string> numberRegions, string outboundCallCampaignId)
        {
            var result = new FunctionReturnResult();


            // Enqueue outbound call queues
            var errors = new List<string>();
            for (int i = 0; i < callsRows.Count; i++)
            {
                var outboundCallRow  = callsRows[i];

                OutboundCallQueueData outboundCallQueueData = BuildOutboundCallQueueData(businessData, callConfig, businessAppAgent, businessNumberData, outboundCallCampaignId, numberRegions, outboundCallRow);

                // Enqueue outbound call queue
                string? callQueueIdResult = await _outboundCallQueueRepository.EnqueueOutboundCallAsync(outboundCallQueueData);
                if (callQueueIdResult == null)
                {
                    errors.Add("Failed to enqueue outbound call queue at row " + (i + 1) + ".");
                    continue;
                }
                outboundCallQueueData.Id = callQueueIdResult;

                // Add queue to campaign
                var queueToCampaignResult = await _outboundCallCampaignRepository.AddQueueToCampaignAsync(outboundCallQueueData.Id, outboundCallCampaignId);
                if (!queueToCampaignResult)
                {
                    await _outboundCallQueueRepository.UpdateCallStatusAsync(outboundCallQueueData.Id, CallQueueStatusEnum.Failed, new CallQueueLog() { Type = CallQueueLogTypeEnum.Error, Message = "Failed to add outbound call queue to campaign at row " + (i + 1) + "." });
                    errors.Add("Failed to add outbound call queue to campaign at row " + (i + 1) + ".");
                    continue;
                }
            }

            // todo what if no call is queued?

            if (errors.Count > 0)
            {
                var addErrorResult = await _outboundCallCampaignRepository.AddErrorLogs(outboundCallCampaignId, errors);
                // ignore add error result for now, we need to figure out how to do this better (we will see if any major fails happen that can not notify the user)
            }

            return result.SetSuccessResult();
        }

        private OutboundCallQueueData BuildOutboundCallQueueData(BusinessData businessData, MakeCallRequestDto callConfig, BusinessAppAgent businessAppAgent, BusinessNumberData businessNumberData, string outboundCallCampaignId, Dictionary<string, string>? numberRegions, OutboundBulkCallRowData? bulkCallRowData)
        {
            string CallingNumberId;
            bool isDefaultNumberData = true;
            if (bulkCallRowData == null || string.IsNullOrWhiteSpace(bulkCallRowData.FromNumberId))
            {
                CallingNumberId = callConfig.NumberDetails!.FromNumberId!;
            }
            else
            {
                CallingNumberId = bulkCallRowData!.FromNumberId;
                isDefaultNumberData = false;
            }

            string RecipientNumber;
            if (bulkCallRowData == null || string.IsNullOrWhiteSpace(bulkCallRowData.ToNumber))
            {
                RecipientNumber = callConfig.NumberDetails!.ToNumber!;
            }
            else
            {
                RecipientNumber = bulkCallRowData!.ToNumber;
            }

            Dictionary<string, string> DynamicVariables;
            if (bulkCallRowData == null || bulkCallRowData.DynamicVariables == null)
            {
                DynamicVariables = new Dictionary<string, string>(); // todo get from callConfig
            }
            else
            {
                DynamicVariables = bulkCallRowData.DynamicVariables;
            }

            OutboundCallRetryData RetryDeclineData = new OutboundCallRetryData() { Enabled = false };
            if (bulkCallRowData == null || bulkCallRowData.OverrideRetryCallDeclinedData == null)
            {
                if (callConfig.Configuration!.RetryDecline!.Enabled! == true)
                {
                    RetryDeclineData = new()
                    {
                        Enabled = true,
                        RetryCount = callConfig.Configuration!.RetryDecline!.Count!,
                        RetryDelay = callConfig.Configuration!.RetryDecline!.Delay!,
                        RetryUnit = callConfig.Configuration!.RetryDecline!.Unit!,
                        TimesTried = 0,
                        LastTried = null,
                    };
                }
            }
            else
            {
                if (bulkCallRowData.OverrideRetryCallDeclinedData!.Enabled! == true)
                {
                    RetryDeclineData = new()
                    {
                        Enabled = true,
                        RetryCount = bulkCallRowData.OverrideRetryCallDeclinedData.Count!,
                        RetryDelay = bulkCallRowData.OverrideRetryCallDeclinedData.Delay!,
                        RetryUnit = bulkCallRowData.OverrideRetryCallDeclinedData.Unit!,
                        TimesTried = 0,
                        LastTried = null,
                    };
                }
            }

            OutboundCallRetryData RetryMissData = new OutboundCallRetryData() { Enabled = false };
            if (bulkCallRowData == null || bulkCallRowData.OverrideRetryCallMissedData == null)
            {
                if (callConfig.Configuration!.RetryMiss!.Enabled! == true)
                {
                    RetryMissData = new()
                    {
                        Enabled = true,
                        RetryCount = callConfig.Configuration!.RetryMiss!.Count!,
                        RetryDelay = callConfig.Configuration!.RetryMiss!.Delay!,
                        RetryUnit = callConfig.Configuration!.RetryMiss!.Unit!,
                        TimesTried = 0,
                        LastTried = null,
                    };
                }
            }
            else
            {
                if (bulkCallRowData.OverrideRetryCallMissedData!.Enabled! == true)
                {
                    RetryMissData = new()
                    {
                        Enabled = true,
                        RetryCount = bulkCallRowData.OverrideRetryCallMissedData.Count!,
                        RetryDelay = bulkCallRowData.OverrideRetryCallMissedData.Delay!,
                        RetryUnit = bulkCallRowData.OverrideRetryCallMissedData.Unit!,
                        TimesTried = 0,
                        LastTried = null,
                    };
                }
            }

            string AgentId;
            if (bulkCallRowData == null || string.IsNullOrWhiteSpace(bulkCallRowData.OverrideAgentId))
            {
                AgentId = callConfig.AgentSettings!.AgentId!;
            }
            else
            {
                AgentId = bulkCallRowData.OverrideAgentId;
            }

            string AgentScriptId;
            if (bulkCallRowData == null || string.IsNullOrWhiteSpace(bulkCallRowData.OverrideSelectedAgentScriptId))
            {
                AgentScriptId = callConfig.AgentSettings!.ScriptId!;
            }
            else
            {
                AgentScriptId = bulkCallRowData.OverrideSelectedAgentScriptId;
            }

            string AgentLanguageCode;
            if (bulkCallRowData == null || string.IsNullOrWhiteSpace(bulkCallRowData.OverrideAgentLanguageCode))
            {
                AgentLanguageCode = callConfig.AgentSettings!.LanguageCode!;
            }
            else
            {
                AgentLanguageCode = bulkCallRowData.OverrideAgentLanguageCode;
            }

            List<string> AgentTimezones;
            if (bulkCallRowData == null || bulkCallRowData.OverrideAgentTimezones == null)
            {
                AgentTimezones = callConfig.AgentSettings!.Timezones!;
            }
            else
            {
                AgentTimezones = bulkCallRowData.OverrideAgentTimezones;
            }

            OutboundCallQueueData outboundCallQueueData = new OutboundCallQueueData()
            {
                CreatedAt = DateTime.UtcNow,
                Type = CallQueueTypeEnum.Outbound,
                Status = CallQueueStatusEnum.Queued,
                BusinessId = businessData.Id,
                RegionId = businessNumberData.RegionId,
                SessionId = null,
                Logs = new List<CallQueueLog>(),
                ProviderMetadata = new Dictionary<string, string>(),
                // outbound related
                CampaignId = outboundCallCampaignId,
                CallingNumberId = CallingNumberId,
                ProviderCallId = null,
                CallingNumberProvider = businessNumberData.Provider,
                RecipientNumber = RecipientNumber,
                ScheduledForDateTime = DateTime.UtcNow.AddMinutes(1),
                CallRetryOnDeclineData = RetryDeclineData,
                CallRetryOnMissedData = RetryMissData,
                DynamicVariables = DynamicVariables,
                AgentId = AgentId,
                AgentScriptId = AgentScriptId,
                AgentLanguageCode = AgentLanguageCode,
                AgentTimeZone = AgentTimezones
            };
            if (callConfig.Configuration!.Schedule!.Type! == OutboundCallScheduleType.Scheduled)
            {
                outboundCallQueueData.ScheduledForDateTime = callConfig.Configuration!.Schedule!.DateTimeUTC!.Value;
            }
            if (!isDefaultNumberData && numberRegions != null && numberRegions.TryGetValue(CallingNumberId, out string callingNumberRegion))
            {
                // numberRegions should never be null in case of bulk call tho
                outboundCallQueueData.RegionId = callingNumberRegion;
            }

            return outboundCallQueueData;
        }

        private async Task<FunctionReturnResult<(List<OutboundBulkCallRowData> callsRows, Dictionary<string, string> numberRegions)?>> ValidateAndBuildBulkCSVCallFile(BusinessData businessData, IFormFile formFile, MakeCallRequestDto callConfig, BusinessNumberData defaultCallNumber, BusinessAppAgent defaultCallAgent)
        {
            var result = new FunctionReturnResult<(List<OutboundBulkCallRowData> callsRows, Dictionary<string, string> numberRegions)?>();

            long businessId = businessData.Id;

            var cachedBusinessNumberRegionData = new Dictionary<string, string>();
            // add default number and region to cache
            cachedBusinessNumberRegionData.Add(defaultCallNumber.Id.ToString(), defaultCallNumber.RegionId);

            var innerCachedBusinessAgentsScriptsData = new Dictionary<string, List<string>>();
            // add default agent and scripts ids to cache
            innerCachedBusinessAgentsScriptsData.Add(defaultCallAgent.Id.ToString(), defaultCallAgent.Scripts.Select(s => s.Id).ToList<string>());

            try
            {
                var rowsDataList = new List<OutboundBulkCallRowData>();

                using (var reader = Sep.Reader(o => o with { HasHeader = true, Sep = Sep.New(','), DisableQuotesParsing = false}).From(formFile.OpenReadStream()))
                {
                    var header = reader.Header;
                    if (header.ColNames.Count != 9)
                    {
                        return result.SetFailureResult(
                            "ValidateAndBuildBulkCSVCallFile:1",
                            "Invalid number of columns in CSV file."
                        );
                    }

                    foreach (var readRow in reader)
                    {
                        var currentOutboundCallRow = new OutboundBulkCallRowData();
                        var currentRowLine = readRow.LineNumberFrom;

                        try
                        {
                            string? from_number_id = readRow["from_number_id"].ToString();
                            string? to_number = readRow["to_number"].ToString();
                            string? dynamic_variables = readRow["dynamic_variables"].ToString().Replace("\"\"", "\"").TrimStart('"').TrimEnd('"');
                            string? override_retry_on_call_declined = readRow["override_retry_on_call_declined"].ToString().Replace("\"\"", "\"").TrimStart('"').TrimEnd('"');
                            string? override_retry_on_missed_call = readRow["override_retry_on_missed_call"].ToString().Replace("\"\"", "\"").TrimStart('"').TrimEnd('"');
                            string? override_agent_id = readRow["override_agent_id"].ToString();
                            string? override_agent_script_id = readRow["override_agent_script_id"].ToString();
                            string? override_agent_language_code = readRow["override_agent_language_code"].ToString();
                            string? override_agent_timezones = readRow["override_agent_timezones"].ToString().TrimStart('"').TrimEnd('"');

                            if (!string.IsNullOrWhiteSpace(from_number_id))
                            {
                                if (!cachedBusinessNumberRegionData.ContainsKey(from_number_id))
                                {
                                    BusinessNumberData? defaultFromCallNumberData = await _parentBusinessManager.GetNumberManager().GetBusinessNumberById(businessId, from_number_id);
                                    if (defaultFromCallNumberData == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:3",
                                            $"Unable to find number {from_number_id} in row {currentRowLine} in business data."
                                        );
                                    }
                                    cachedBusinessNumberRegionData.Add(from_number_id, defaultFromCallNumberData.RegionId);
                                }
                                currentOutboundCallRow.FromNumberId = from_number_id;
                            }

                            if (string.IsNullOrWhiteSpace(to_number))
                            {
                                return result.SetFailureResult(
                                    "ValidateAndBuildBulkCSVCallFile:4",
                                    $"Missing 'to_number' in row {currentRowLine}."
                                );
                            }
                            PhoneNumber parsedPhoneNumber;
                            try
                            {
                                parsedPhoneNumber = PhoneNumberUtil.GetInstance().Parse(to_number, "ZZ");
                            }
                            catch (Exception ex)
                            {
                                return result.SetFailureResult(
                                    "ValidateAndBuildBulkCSVCallFile:5",
                                    $"Error parsing 'to_number' in row {currentRowLine}. Make sure number start with +countrycode."
                                );
                            }
                            if (!PhoneNumberUtil.GetInstance().IsValidNumber(parsedPhoneNumber))
                            {
                                return result.SetFailureResult(
                                    "ValidateAndBuildBulkCSVCallFile:5.1",
                                    $"Number validation failed for 'to_number' in row {currentRowLine}."
                                );
                            }
                            currentOutboundCallRow.ToNumber = to_number;

                            Dictionary<string, string>? dynamicVariablesDictionary = null;
                            if (!string.IsNullOrWhiteSpace(dynamic_variables))
                            {
                                try
                                {
                                    dynamicVariablesDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(dynamic_variables);
                                    if (dynamicVariablesDictionary == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:6",
                                            $"Error deserializing dynamic variables for row {currentRowLine}."
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:7",
                                        $"Error deserializing dynamic variables for row {currentRowLine}: {ex.Message}"
                                    );
                                }
                            }
                            currentOutboundCallRow.DynamicVariables = dynamicVariablesDictionary;

                            OutboundBulkCallRowDataRetryData? outboundBulkCallRowDataRetryDeclinedData = null;
                            if (!string.IsNullOrWhiteSpace(override_retry_on_call_declined))
                            {
                                try
                                {
                                    outboundBulkCallRowDataRetryDeclinedData = JsonSerializer.Deserialize<OutboundBulkCallRowDataRetryData>(override_retry_on_call_declined);
                                    if (outboundBulkCallRowDataRetryDeclinedData == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:8",
                                            $"Error deserializing retry declined data for row {currentRowLine}."
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:9",
                                        $"Error deserializing retry declined data for row {currentRowLine}: {ex.Message}"
                                    );
                                }
                                if (outboundBulkCallRowDataRetryDeclinedData.Enabled == null)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:10",
                                        $"Missing 'Enabled' in retry declined data for row {currentRowLine}."
                                    );
                                }
                                if (outboundBulkCallRowDataRetryDeclinedData.Enabled == true)
                                {
                                    if (outboundBulkCallRowDataRetryDeclinedData.Count == null || outboundBulkCallRowDataRetryDeclinedData.Count < 1 || outboundBulkCallRowDataRetryDeclinedData.Count > 5) // todo can have retires max count based on user/business, used below in a function too
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:11",
                                            $"Missing or invalid 'Count' in retry declined data for row {currentRowLine}. Should be between 1 and 5."
                                        );
                                    }
                                    if (outboundBulkCallRowDataRetryDeclinedData.Delay == null || outboundBulkCallRowDataRetryDeclinedData.Delay < 1)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:12",
                                            $"Missing or invalid 'Delay' in retry declined data for row {currentRowLine}. Should be greater than 0."
                                        );
                                    }
                                    if (outboundBulkCallRowDataRetryDeclinedData.Unit == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:13",
                                            $"Missing or invalid 'Unit' in retry declined data for row {currentRowLine}."
                                        );
                                    }
                                }
                            }
                            currentOutboundCallRow.OverrideRetryCallDeclinedData = outboundBulkCallRowDataRetryDeclinedData;

                            OutboundBulkCallRowDataRetryData? outboundBulkCallRowDataRetryMissedData = null;
                            if (!string.IsNullOrWhiteSpace(override_retry_on_missed_call))
                            {
                                try
                                {
                                    outboundBulkCallRowDataRetryMissedData = JsonSerializer.Deserialize<OutboundBulkCallRowDataRetryData>(override_retry_on_missed_call);
                                    if (outboundBulkCallRowDataRetryMissedData == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:14",
                                            $"Error deserializing retry missed call data for row {currentRowLine}."
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:15",
                                        $"Error deserializing retry missed call data for row {currentRowLine}: {ex.Message}"
                                    );
                                }
                                if (outboundBulkCallRowDataRetryMissedData.Enabled == null)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:16",
                                        $"Missing 'Enabled' in retry missed call data for row {currentRowLine}."
                                    );
                                }
                                if (outboundBulkCallRowDataRetryMissedData.Enabled == true)
                                {
                                    if (outboundBulkCallRowDataRetryMissedData.Count == null || outboundBulkCallRowDataRetryMissedData.Count < 1 || outboundBulkCallRowDataRetryMissedData.Count > 5) // todo can have retires max count based on user/business, used below in a function too
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:11",
                                            $"Missing or invalid 'Count' in retry missed data for row {currentRowLine}. Should be between 1 and 5."
                                        );
                                    }
                                    if (outboundBulkCallRowDataRetryMissedData.Delay == null || outboundBulkCallRowDataRetryMissedData.Delay < 1)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:12",
                                            $"Missing or invalid 'Delay' in retry missed data for row {currentRowLine}. Should be greater than 0."
                                        );
                                    }
                                    if (outboundBulkCallRowDataRetryMissedData.Unit == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:13",
                                            $"Missing or invalid 'Unit' in retry missed data for row {currentRowLine}."
                                        );
                                    }
                                }
                            }
                            currentOutboundCallRow.OverrideRetryCallMissedData = outboundBulkCallRowDataRetryMissedData;
                           
                            if (!string.IsNullOrWhiteSpace(override_agent_id))
                            {
                                if (!innerCachedBusinessAgentsScriptsData.ContainsKey(override_agent_id))
                                {
                                    BusinessAppAgent? agent = await _parentBusinessManager.GetAgentsManager().GetAgentById(businessId, override_agent_id);
                                    if (agent == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:17",
                                            $"Agent with id {override_agent_id} not found in business for row {currentRowLine}."
                                        );
                                    }
                                    innerCachedBusinessAgentsScriptsData.Add(override_agent_id, agent.Scripts.Select(s => s.Id).ToList<string>());
                                }
                                currentOutboundCallRow.OverrideAgentId = override_agent_id;
                            }
                  
                            if (!string.IsNullOrWhiteSpace(override_agent_script_id))
                            {
                                string agentIdToCheckAgainst = string.IsNullOrWhiteSpace(override_agent_id) ? defaultCallAgent.Id : override_agent_id;
                                if (!innerCachedBusinessAgentsScriptsData.TryGetValue(agentIdToCheckAgainst, out List<string>? overrideAgentScripts) || !overrideAgentScripts.Contains(override_agent_script_id))
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:18",
                                        $"Agent {agentIdToCheckAgainst} does not have script with id {override_agent_script_id} not found in business for row {currentRowLine}."
                                    );
                                }
                                currentOutboundCallRow.OverrideSelectedAgentScriptId = override_agent_script_id;
                            }

                            if (!string.IsNullOrEmpty(override_agent_language_code))
                            {
                                if (!businessData.Languages.Contains(override_agent_language_code))
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:19",
                                        $"Agent language code {override_agent_language_code} not found in business for row {currentRowLine}."
                                    );
                                }
                                currentOutboundCallRow.OverrideAgentLanguageCode = override_agent_language_code;
                            }
                        
                            if (!string.IsNullOrEmpty(override_agent_timezones))
                            {
                                List<string> timezonesSplit = override_agent_timezones.Split(',').ToList();
                                foreach (string zone in timezonesSplit)
                                {
                                    if (!TimeZoneHelper.ValidateOffsetString(zone))
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:20",
                                            $"Agent timezone {zone} validation failed for row {currentRowLine}. Must start with + or - followed by HH:MM format."
                                        );
                                    }
                                }
                                currentOutboundCallRow.OverrideAgentTimezones = timezonesSplit;
                            }

                            rowsDataList.Add(currentOutboundCallRow);
                        }
                        catch (Exception ex)
                        {
                            return result.SetFailureResult(
                                "ValidateAndBuildBulkCSVCallFile:",
                                $"Error reading row {currentRowLine}: {ex.Message}"
                            );
                        }

                    }
                }

                if (rowsDataList.Count == 0)
                {
                    return result.SetFailureResult(
                        "ValidateAndBuildBulkCSVCallFile:21",
                        "No rows found in CSV file or were converted."
                    );
                }

                return result.SetSuccessResult(
                    (rowsDataList, cachedBusinessNumberRegionData)
                );
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ValidateAndBuildBulkCSVCallFile:-1",
                    $"Error reading CSV file: {ex.Message}"
                );
            }
        }

        private async Task<FunctionReturnResult> ValidateActionData(long businessId, string businessDefaultLanguage, MakeCallActionToolConfigDto data, string actionType)
        {
            var result = new FunctionReturnResult();

            string? selectedToolId = data.ToolId;
            if (selectedToolId == null) return result.SetSuccessResult();

            var selectedToolData = await _parentBusinessManager.GetToolsManager().GetBusinessAppTool(businessId, selectedToolId);
            if (selectedToolData == null)
            {
                result.Code = "ValidateActionData:1";
                result.Message = $"{actionType} tool not found in business.";
                return result;
            }

            Dictionary<string, object>? argumentsList = data.Arguments;
            if (argumentsList == null) argumentsList = new Dictionary<string, object>();

            foreach (var toolInputArgument in selectedToolData.Configuration.InputSchemea)
            {
                bool foundProperty = argumentsList.TryGetValue(toolInputArgument.Id, out var argumentValueProperty);

                if (!foundProperty && toolInputArgument.IsRequired)
                {
                    return result.SetFailureResult(
                        "ValidateActionData:2",
                        $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} not found but is required."
                    );
                }
                else if (foundProperty)
                {
                    // Handle Array Type
                    if (toolInputArgument.IsArray)
                    {
                        if (argumentValueProperty.GetType() != typeof(object[]))
                        {
                            return result.SetFailureResult(
                                "ValidateActionData:4",
                                $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} should be an array."
                            );
                        }

                        var arrayValuesCount = 0;
                        foreach (var arrayElement in argumentValueProperty as object[])
                        {
                            var validationResult = BusinessAppToolPropertyValidator.ValidateArgumentValue(businessDefaultLanguage, arrayElement, toolInputArgument, actionType);
                            if (!validationResult.Success)
                            {
                                return result.SetFailureResult(
                                    "ValidateActionData:" + validationResult.Code,
                                    validationResult.Message
                                );
                            }
                            arrayValuesCount++;
                        }

                        if (toolInputArgument.IsRequired && arrayValuesCount == 0)
                        {
                            return result.SetFailureResult(
                                "ValidateActionData:5",
                                $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} array cannot be empty as it is required."
                            );
                        }
                    }
                    // Handle Single Value
                    else
                    {
                        var validationResult = BusinessAppToolPropertyValidator.ValidateArgumentValue(businessDefaultLanguage, argumentValueProperty, toolInputArgument, actionType);
                        if (!validationResult.Success)
                        {
                            return result.SetFailureResult(
                                "ValidateActionData:" + validationResult.Code,
                                validationResult.Message
                            );
                        }
                    }
                }
            }

            return result.SetSuccessResult();
        }
    }
}
