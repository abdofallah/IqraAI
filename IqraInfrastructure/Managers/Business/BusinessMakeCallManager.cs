using IqraCore.Entities.Business;
using IqraCore.Entities.Call.Outbound;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Outbound;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.MakeCalls;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Repositories.Call;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using nietras.SeparatedValues;
using PhoneNumbers;
using System.Globalization;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessMakeCallManager
    {
        private readonly ILogger<BusinessMakeCallManager> _logger;
        private readonly BusinessManager _parentBusinessManager;
        private readonly RegionManager _regionManager;
        private readonly OutboundCallQueueGroupRepository _outboundCallQueueGroupRepository;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepository;
        private readonly IntegrationConfigurationManager _integrationConfigurationManager;

        public BusinessMakeCallManager(
            ILogger<BusinessMakeCallManager> logger,
            BusinessManager parentBusinessManager,
            RegionManager regionManager,
            OutboundCallQueueGroupRepository outboundCallQueueGroupRepository,
            OutboundCallQueueRepository outboundCallQueueRepository,
            IntegrationConfigurationManager integrationConfigurationManager
        )
        {
            _logger = logger;
            _parentBusinessManager = parentBusinessManager;
            _regionManager = regionManager;
            _outboundCallQueueGroupRepository = outboundCallQueueGroupRepository;
            _outboundCallQueueRepository = outboundCallQueueRepository;
            _integrationConfigurationManager = integrationConfigurationManager;
        }

        public async Task<FunctionReturnResult<List<string?>?>> QueueCallInitiationRequestAsync(BusinessData businessData, IFormCollection formData)
        {
            var result = new FunctionReturnResult<List<string?>?>();

            long businessId = businessData.Id;
            string businessDefaultLanguage = businessData.DefaultLanguage;
            List<string> businessLanguages = businessData.Languages;

            BusinessAppTelephonyCampaign telephonyCampaignData;
            var callConfigData = new MakeCallRequestDto();
            if (!formData.TryGetValue("config", out var configStringValue))
            {
                return result.SetFailureResult(
                    "QueueCallInitiationRequestAsync:FORM_DATA_CONFIG_NOT_FOUND",
                    "Config not found in form data."
                );
            }
            else
            {
                var configString = configStringValue.FirstOrDefault();
                if (string.IsNullOrEmpty(configString))
                {
                    return result.SetFailureResult(
                        "QueueCallInitiationRequestAsync:FORM_DATA_CONFIG_EMPTY",
                        "Config not found in form data."
                    );
                }

                JsonDocument? callRequestDocElement = null;
                try
                {
                    callRequestDocElement = JsonSerializer.Deserialize<JsonDocument>(configString);
                }
                catch (Exception ex) {
                    return result.SetFailureResult(
                        "QueueCallInitiationRequestAsync:CONFIG_DESERIALIZATION_ERROR",
                        $"Invalid config data format: {ex.Message}"
                    );
                }
                if (callRequestDocElement == null)
                {
                    return result.SetFailureResult(
                        "QueueCallInitiationRequestAsync:CONFIG_DESERIALIZATION_ERROR",
                        "Invalid config data format."
                    );
                }
                var callRequestElement = callRequestDocElement.RootElement;

                // Campaign Id
                if (!callRequestElement.TryGetProperty("campaignId", out var campaignIdElement)
                    || campaignIdElement.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(campaignIdElement.GetString()))
                {
                    return result.SetFailureResult(
                        "QueueCallInitiationRequestAsync:CONFIG_CAMPAIGN_ID_NOT_FOUND",
                        "Campaign ID not found in config data."
                    );
                }
                else
                {
                    var campaignIdValue = campaignIdElement.GetString();
                    
                    var campaignDataResult = await _parentBusinessManager.GetCampaignManager().GetTelephonyCampaignById(businessId, campaignIdValue);
                    if (!campaignDataResult.Success && campaignDataResult.Data != null)
                    {
                        return result.SetFailureResult(
                            "QueueCallInitiationRequestAsync:CAMPAIGN_NOT_FOUND",
                            "Campaign not found in business."
                        );
                    }

                    telephonyCampaignData = campaignDataResult.Data!;
                    callConfigData.CampaignId = campaignIdValue!;
                }

                // Number
                if (!callRequestElement.TryGetProperty("number", out var numberElement)
                    || numberElement.ValueKind != JsonValueKind.Object)
                {
                    return result.SetFailureResult(
                        "QueueCallInitiationRequestAsync:CONFIG_NUMBER_NOT_FOUND",
                        "Number not found in config data."
                    );
                }
                else
                {
                    if (!numberElement.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.Number)
                    {
                        return result.SetFailureResult(
                            "QueueCallInitiationRequestAsync:CONFIG_NUMBER_TYPE_NOT_FOUND",
                            "Number type not found in config data."
                        );
                    }
                    var typeIntValue = typeElement.GetInt32();
                    if (!Enum.IsDefined(typeof(OutboundCallNumberType), typeIntValue))
                    {
                        return result.SetFailureResult(
                            "QueueCallInitiationRequestAsync:CONFIG_NUMBER_TYPE_UNDEFINED",
                            "Number type enum not defined."
                        );
                    }
                    callConfigData.Number.Type = ((OutboundCallNumberType)typeIntValue);

                    // Single Call
                    if (callConfigData.Number.Type == OutboundCallNumberType.Single)
                    {
                        // To Number
                        if (
                            !numberElement.TryGetProperty("toNumber", out var toNumberElement) ||
                            toNumberElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(toNumberElement.GetString())
                        ) {
                            return result.SetFailureResult(
                                "QueueCallInitiationRequestAsync:CONFIG_NUMBER_TO_NUMBER_NOT_FOUND",
                                "to number not found in config data."
                            );
                        }
                        var toNumberValue = toNumberElement.GetString()!;
                        if (!toNumberValue.StartsWith("+"))
                        {
                            if (toNumberValue.StartsWith("00"))
                            {
                                toNumberValue = toNumberValue.Substring(2);
                            }

                            toNumberValue = "+" + toNumberValue;
                        }

                        var toNumberInstance = PhoneNumberUtil.GetInstance().Parse(toNumberValue, "ZZ");
                        if (!PhoneNumberUtil.GetInstance().IsValidNumber(toNumberInstance))
                        {
                            return result.SetFailureResult(
                                "QueueCallInitiationRequestAsync:CONFIG_NUMBER_TO_NUMBER_INVALID",
                                "To number is invalid. Use E.164 format (+1234567890)."
                            );
                        }

                        callConfigData.Number.ToNumber = toNumberValue;
                    }
                }

                // Schedule
                if (!callRequestElement.TryGetProperty("schedule", out var scheduleElement)
                    || scheduleElement.ValueKind != JsonValueKind.Object)
                {
                    return result.SetFailureResult(
                        "QueueCallInitiationRequestAsync:CONFIG_SCHEDULE_NOT_FOUND",
                        "Schedule not found in config data."
                    );
                }
                else
                {
                    if (!scheduleElement.TryGetProperty("type", out var scheduleTypeElement) || scheduleTypeElement.ValueKind != JsonValueKind.Number)
                    {
                        return result.SetFailureResult(
                            "QueueCallInitiationRequestAsync:CONFIG_SCHEDULE_TYPE_NOT_FOUND",
                            "Schedule type not found in config data."
                        );
                    }
                    var scheduleTypeIntValue = scheduleTypeElement.GetInt32();
                    if (!Enum.IsDefined(typeof(OutboundCallScheduleType), scheduleTypeIntValue))
                    {
                        return result.SetFailureResult(
                            "QueueCallInitiationRequestAsync:CONFIG_SCHEDULE_TYPE_UNDEFINED",
                            "Schedule type enum not defined."
                        );
                    }
                    callConfigData.Schedule.Type = ((OutboundCallScheduleType)scheduleTypeIntValue);

                    if (callConfigData.Schedule.Type == OutboundCallScheduleType.Now)
                    {
                        callConfigData.Schedule.DateTimeUTC = DateTime.UtcNow;
                    }
                    else if (callConfigData.Schedule.Type == OutboundCallScheduleType.Scheduled)
                    {
                        if (
                            !scheduleElement.TryGetProperty("dateTimeUTC", out var dateTimeUTCElement) ||
                            dateTimeUTCElement.ValueKind != JsonValueKind.String ||
                            string.IsNullOrWhiteSpace(dateTimeUTCElement.GetString())
                        ) {
                            return result.SetFailureResult(
                                "QueueCallInitiationRequestAsync:CONFIG_SCHEDULE_DATETIMEUTC_NOT_FOUND",
                                "Date time UTC not found in config data."
                            );
                        }
                        var dateTimeUTCValue = dateTimeUTCElement.GetString()!;
                        if (!DateTime.TryParse(dateTimeUTCValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeUTC))
                        {
                            return result.SetFailureResult(
                                "QueueCallInitiationRequestAsync:CONFIG_SCHEDULE_DATETIMEUTC_PARSE_ERROR",
                                "Failed to parse schedule date time UTC."
                            );
                        }
                        callConfigData.Schedule.DateTimeUTC = dateTimeUTC;
                    }
                }

                // DynamicVariables
                if (callRequestElement.TryGetProperty("dynamicVariables", out var dynamicVariablesElement)
                    && dynamicVariablesElement.ValueKind == JsonValueKind.Object)
                {
                    callConfigData.DynamicVariables = new Dictionary<string, string>();
                    foreach (var dynamicVariableItem in dynamicVariablesElement.EnumerateObject())
                    {
                        if (
                            dynamicVariableItem.Value.ValueKind != JsonValueKind.String ||
                            string.IsNullOrWhiteSpace(dynamicVariableItem.Value.GetString())
                        ) {
                            return result.SetFailureResult(
                                "QueueCallInitiationRequestAsync:CONFIG_DYNAMIC_VARIABLES_VALUE_INVALID",
                                $"Dynamic variable value not found or invalid in config data for {dynamicVariableItem.Name}. Must be string."
                            );
                        }
                        callConfigData.DynamicVariables.Add(dynamicVariableItem.Name, dynamicVariableItem.Value.GetString()!);
                    }

                    if (callConfigData.Number.Type == OutboundCallNumberType.Single)
                    {
                        if (telephonyCampaignData.Variables.DynamicVariables.Count > 0)
                        {
                            foreach (var variableData in telephonyCampaignData.Variables.DynamicVariables)
                            {
                                var dynamicVariableItem = callConfigData.DynamicVariables.FirstOrDefault(x => x.Key == variableData.Key);

                                if (dynamicVariableItem.Key == null)
                                {
                                    if (variableData.IsRequired)
                                    {
                                        return result.SetFailureResult(
                                            "QueueCallInitiationRequestAsync:CONFIG_DYNAMIC_VARIABLES_REQUIRED_NOT_FOUND",
                                            $"Dynamic variable required not found in config data for {variableData.Key}. Telephony campaign rule."
                                        );
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(dynamicVariableItem.Value) && !variableData.IsEmptyOrNullAllowed)
                                    {
                                        return result.SetFailureResult(
                                            "QueueCallInitiationRequestAsync:CONFIG_DYNAMIC_VARIABLES_REQUIRED_NOT_FOUND",
                                            $"Dynamic variable cannot be empty in config data for {variableData.Key}. Telephony campaign rule."
                                        );
                                    }
                                }
                            }
                        }
                    }
                }

                // Metadata
                if (callRequestElement.TryGetProperty("metadata", out var metadataElement)
                    && metadataElement.ValueKind == JsonValueKind.Object)
                {
                    callConfigData.Metadata = new Dictionary<string, string>();
                    foreach (var metadataItem in metadataElement.EnumerateObject())
                    {
                        if (
                            metadataItem.Value.ValueKind != JsonValueKind.String ||
                            string.IsNullOrWhiteSpace(metadataItem.Value.GetString())
                        ) {
                            return result.SetFailureResult(
                                "QueueCallInitiationRequestAsync:CONFIG_METADATA_VALUE_INVALID",
                                $"Metadata value not found or invalid in config data for {metadataItem.Name}. Must be string."
                            );
                        }
                        callConfigData.Metadata.Add(metadataItem.Name, metadataItem.Value.GetString()!);
                    }

                    if (callConfigData.Number.Type == OutboundCallNumberType.Single)
                    {
                        if (telephonyCampaignData.Variables.Metadata.Count > 0)
                        {
                            foreach (var variableData in telephonyCampaignData.Variables.Metadata)
                            {
                                var metadataItem = callConfigData.Metadata.FirstOrDefault(x => x.Key == variableData.Key);

                                if (metadataItem.Key == null)
                                {
                                    if (variableData.IsRequired)
                                    {
                                        return result.SetFailureResult(
                                            "QueueCallInitiationRequestAsync:CONFIG_METADATA_REQUIRED_NOT_FOUND",
                                            $"Metadata required not found in config data for {variableData.Key}. Telephony campaign rule."
                                        );
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(metadataItem.Value) && !variableData.IsEmptyOrNullAllowed)
                                    {
                                        return result.SetFailureResult(
                                            "QueueCallInitiationRequestAsync:CONFIG_METADATA_REQUIRED_NOT_FOUND",
                                            $"Metadata cannot be empty in config data for {variableData.Key}. Telephony campaign rule."
                                        );
                                    }
                                }
                            }
                        }
                    }
                }
            }

            BusinessAppAgent? campaignAgent = await _parentBusinessManager.GetAgentsManager().GetAgentById(businessData.Id, telephonyCampaignData.Agent.SelectedAgentId);
            if (campaignAgent == null)
            {
                return result.SetFailureResult(
                    "QueueCallInitiationRequestAsync:CAMPAIGN_AGENT_NOT_FOUND",
                    "Campaign agent not found in business."
                );
            }

            // first check if bulk data converted/valid
            FunctionReturnResult<List<OutboundBulkCallRowData>?>? bulkCallFileResult = null;
            if (callConfigData.Number.Type == OutboundCallNumberType.Bulk)
            {
                var bulkCsvFile = formData.Files.FirstOrDefault(f => f.Name == "bulk_file");
                if (bulkCsvFile == null || bulkCsvFile.Length == 0)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:BULK_FILE_NOT_FOUND",
                        "Bulk file not found in form data."
                    );
                }

                bulkCallFileResult = await ValidateAndBuildBulkCSVCallFile(businessData, telephonyCampaignData, bulkCsvFile!, callConfigData);
                if (!bulkCallFileResult.Success || bulkCallFileResult.Data == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:" + bulkCallFileResult.Code,
                        bulkCallFileResult.Message
                    );
                }
            }

            // Create Outbound Call Queue Group
            var callQueueGroupData = new OutboundCallQueueGroupData()
            {
                Id = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                BusinessId = businessData.Id,
                CallRequestData = callConfigData,
                IsBulkCall = callConfigData.Number.Type == OutboundCallNumberType.Bulk
            };
            var callQueueGroupAddResult = await _outboundCallQueueGroupRepository.CreateOutboundCallQueueGroupAsync(callQueueGroupData);
            if (!callQueueGroupAddResult)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:OUTBOUND_CALL_QUEUE_GROUP_ADD_ERROR",
                    "Failed to add outbound call queue group."
                );
            }

            // Forward the Call To Proxy
            if (callConfigData.Number.Type == OutboundCallNumberType.Single)
            {
                var singleNumberBusinessId = GetBusinessNumberIdForToNumber(callConfigData.Number.ToNumber!, telephonyCampaignData);
                if (!singleNumberBusinessId.Success)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:" + singleNumberBusinessId.Code,
                        singleNumberBusinessId.Message
                    );
                }
                var singleBusinessNumberData = await _parentBusinessManager.GetNumberManager().GetBusinessNumberById(businessData.Id, singleNumberBusinessId.Data!);
                if (singleBusinessNumberData == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:BUSINESS_NUMBER_NOT_FOUND",
                        "Business number not found for single call."
                    );
                }

                var singleForwardResult = await QueueSingleCall(callConfigData, businessData, telephonyCampaignData, singleBusinessNumberData, callQueueGroupData.Id);
                if (!singleForwardResult.Success)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:" + singleForwardResult.Code,
                        singleForwardResult.Message
                    );
                }

                return result.SetSuccessResult(singleForwardResult.Data);
            }
            else if (callConfigData.Number.Type == OutboundCallNumberType.Bulk)
            {
                var bulkForwardResult = await QueueBulkCalls(callConfigData, businessData, telephonyCampaignData, bulkCallFileResult!.Data, callQueueGroupData.Id);
                if (!bulkForwardResult.Success)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:" + bulkForwardResult.Code,
                        bulkForwardResult.Message
                    );
                }

                return result.SetSuccessResult(bulkForwardResult.Data);
            }
            else
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:INVALID_NUMBER_TYPE",
                    "Invalid number type."
                );
            }
        }

        private async Task<FunctionReturnResult<List<string?>?>> QueueSingleCall(MakeCallRequestDto callConfig, BusinessData businessData, BusinessAppTelephonyCampaign telephonyCampaignData, BusinessNumberData businessNumberData, string queueGroupId)
        {
            var result = new FunctionReturnResult<List<string?>?>();

            OutboundCallQueueData outboundCallQueueData = BuildOutboundCallQueueData(callConfig, businessData, telephonyCampaignData, businessNumberData, null, queueGroupId);

            // Enqueue outbound call queue
            string? callQueueIdResult = await _outboundCallQueueRepository.EnqueueOutboundCallAsync(outboundCallQueueData);
            if (callQueueIdResult == null)
            {
                return result.SetFailureResult(
                    "ForwardSingleCallToRegionProxy:OUTBOUND_CALL_QUEUE_ENQUEUE_ERROR",
                    "Failed to enqueue outbound call queue."
                );
            }

            // Add queue to campaign
            var queueToQueueGroupResult = await _outboundCallQueueGroupRepository.AddQueueToQueueGroupAsync(outboundCallQueueData.Id, queueGroupId);
            if (!queueToQueueGroupResult)
            {
                return result.SetFailureResult(
                    "ForwardSingleCallToRegionProxy:2",
                    "Failed to add outbound call queue to campaign."
                );
            }

            return result.SetSuccessResult(new List<string?>() { callQueueIdResult });
        }

        private async Task<FunctionReturnResult<List<string?>?>> QueueBulkCalls(MakeCallRequestDto callConfig, BusinessData businessData, BusinessAppTelephonyCampaign campaignData, List<OutboundBulkCallRowData> callsRows, string queueGroupId)
        {
            var result = new FunctionReturnResult<List<string?>?>();

            var businessNumberDataCache = new Dictionary<string, BusinessNumberData>();

            // Enqueue outbound call queues
            var callQueueIds = new List<string?>();
            var errors = new List<string>();
            for (int i = 0; i < callsRows.Count; i++)
            {
                var outboundCallRow  = callsRows[i];

                var businessNumberId = GetBusinessNumberIdForToNumber(outboundCallRow.ToNumber, campaignData);
                if (!businessNumberId.Success)
                {
                    callQueueIds.Add(null);
                    errors.Add("Failed to get business number id for to number " + outboundCallRow.ToNumber + " at row " + (i + 1) + ".");
                    continue;
                }

                BusinessNumberData businessNumberData;
                if (businessNumberDataCache.TryGetValue(businessNumberId.Data!, out var cachedBusinessNumberData))
                {
                    businessNumberData = cachedBusinessNumberData;
                }
                else
                {
                    var businessNumberResult = await _parentBusinessManager.GetNumberManager().GetBusinessNumberById(businessData.Id, businessNumberId.Data!);
                    if (businessNumberResult == null)
                    {
                        callQueueIds.Add(null);
                        errors.Add("Business number not found for to number " + outboundCallRow.ToNumber + " at row " + (i + 1) + ".");
                        continue;
                    }

                    businessNumberData = businessNumberResult;
                    businessNumberDataCache.Add(businessNumberId.Data!, businessNumberData);
                }

                OutboundCallQueueData outboundCallQueueData = BuildOutboundCallQueueData(callConfig, businessData, campaignData, businessNumberData, outboundCallRow, queueGroupId);

                // Enqueue outbound call queue
                string? callQueueIdResult = await _outboundCallQueueRepository.EnqueueOutboundCallAsync(outboundCallQueueData);
                if (callQueueIdResult == null)
                {
                    callQueueIds.Add(null);
                    errors.Add("Failed to enqueue outbound call queue at row " + (i + 1) + ".");
                    continue;
                }
                outboundCallQueueData.Id = callQueueIdResult;
                callQueueIds.Add(callQueueIdResult);

                // Add queue to queue group
                var queueToQueueGroupResult = await _outboundCallQueueGroupRepository.AddQueueToQueueGroupAsync(outboundCallQueueData.Id, queueGroupId);
                if (!queueToQueueGroupResult)
                {
                    await _outboundCallQueueRepository.UpdateCallStatusAsync(
                        outboundCallQueueData.Id,
                        CallQueueStatusEnum.Failed,
                        new CallQueueLog() {
                            Type = CallQueueLogTypeEnum.Error,
                            Message = "Failed to add outbound call queue to campaign at row " + (i + 1) + "."
                        },
                        completedAt: DateTime.UtcNow
                    );
                    errors.Add("Failed to add outbound call queue to queue group at row " + (i + 1) + ".");
                    continue;
                }
            }

            // todo what if no call is queued?

            if (errors.Count > 0)
            {
                var addErrorResult = await _outboundCallQueueGroupRepository.AddErrorLogs(campaignData.Id, errors);
                // ignore add error result for now, we need to figure out how to do this better (we will see if any major fails happen that can not notify the user)
            }

            return result.SetSuccessResult(callQueueIds);
        }

        private OutboundCallQueueData BuildOutboundCallQueueData(MakeCallRequestDto callConfig, BusinessData businessData, BusinessAppTelephonyCampaign telephonyCampaignData, BusinessNumberData businessNumberData, OutboundBulkCallRowData? bulkCallRowData, string queueGroupId)
        {
            string RecipientNumber;
            if (bulkCallRowData == null || string.IsNullOrWhiteSpace(bulkCallRowData.ToNumber))
            {
                RecipientNumber = callConfig.Number.ToNumber!;
            }
            else
            {
                RecipientNumber = bulkCallRowData!.ToNumber;
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
                CampaignId = telephonyCampaignData.Id,
                QueueGroupId = queueGroupId,
                CallingNumberId = businessNumberData.Id,
                ProviderCallId = null,
                CallingNumberProvider = businessNumberData.Provider,
                RecipientNumber = RecipientNumber,
                ScheduledForDateTime = DateTime.UtcNow.AddMinutes(1),
            };
            if (callConfig.Schedule.Type == OutboundCallScheduleType.Scheduled)
            {
                outboundCallQueueData.ScheduledForDateTime = callConfig.Schedule.DateTimeUTC!.Value;
            }
            if (callConfig.Number.Type == OutboundCallNumberType.Single)
            {
                outboundCallQueueData.DynamicVariables = callConfig.DynamicVariables;
                outboundCallQueueData.Metadata = callConfig.Metadata;
            }
            else if (callConfig.Number.Type == OutboundCallNumberType.Bulk)
            {
                outboundCallQueueData.DynamicVariables = bulkCallRowData!.DynamicVariables;
                outboundCallQueueData.Metadata = bulkCallRowData!.Metadata;
            }

            return outboundCallQueueData;
        }

        // Bulk CSV Row Builder
        private async Task<FunctionReturnResult<List<OutboundBulkCallRowData>?>> ValidateAndBuildBulkCSVCallFile(BusinessData businessData, BusinessAppTelephonyCampaign telephonyCampaignData, IFormFile formFile, MakeCallRequestDto callConfig)
        {
            var result = new FunctionReturnResult<List<OutboundBulkCallRowData>?>();
            long businessId = businessData.Id;

            try
            {
                var rowsDataList = new List<OutboundBulkCallRowData>();

                using (var reader = Sep.Reader(o => o with { HasHeader = true, Sep = Sep.New(','), DisableQuotesParsing = false}).From(formFile.OpenReadStream()))
                {
                    var header = reader.Header;
                    if (header.ColNames.Count != 3)
                    {
                        return result.SetFailureResult(
                            "ValidateAndBuildBulkCSVCallFile:INVAID_COLUMN_COUNT",
                            "Invalid number of columns in CSV file."
                        );
                    }

                    foreach (var readRow in reader)
                    {
                        OutboundBulkCallRowData currentOutboundCallRow = new OutboundBulkCallRowData();

                        // Build Base Variables
                        foreach (var dynamicVariableItem in callConfig.DynamicVariables)
                        {
                            currentOutboundCallRow.DynamicVariables.Add(dynamicVariableItem.Key, dynamicVariableItem.Value);
                        }
                        foreach (var metadataItem in callConfig.Metadata)
                        {
                            currentOutboundCallRow.Metadata.Add(metadataItem.Key, metadataItem.Value);
                        }

                        var currentRowLine = readRow.LineNumberFrom;

                        try
                        {
                            string? to_number = readRow["to_number"].ToString();
                            string? dynamic_variables = readRow["dynamic_variables"].ToString().Replace("\"\"", "\"").TrimStart('"').TrimEnd('"');
                            string? metadata = readRow["metadata"].ToString().Replace("\"\"", "\"").TrimStart('"').TrimEnd('"');
                            string? override_agent_language_code = readRow["override_agent_language_code"].ToString();

                            if (string.IsNullOrWhiteSpace(to_number))
                            {
                                return result.SetFailureResult(
                                    "ValidateAndBuildBulkCSVCallFile:4",
                                    $"Missing 'to_number' in row {currentRowLine}."
                                );
                            }
                            else
                            {
                                if (!to_number.StartsWith("+"))
                                {
                                    if (to_number.StartsWith("00"))
                                    {
                                        to_number = to_number.Substring(2);
                                    }

                                    to_number = $"+{to_number}";
                                }

                                PhoneNumber parsedPhoneNumber;
                                try
                                {
                                    parsedPhoneNumber = PhoneNumberUtil.GetInstance().Parse(to_number, "ZZ");
                                }
                                catch (Exception ex)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:TO_NUMBER_PARSE_FAILED",
                                        $"Error parsing 'to_number' in row {currentRowLine}. Make sure number start with +countrycode."
                                    );
                                }
                                if (!PhoneNumberUtil.GetInstance().IsValidNumber(parsedPhoneNumber))
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:INVALID_TO_NUMBER",
                                        $"Number validation failed for 'to_number' in row {currentRowLine}."
                                    );
                                }
                                currentOutboundCallRow.ToNumber = to_number;
                            }

                            Dictionary<string, string>? dynamicVariablesDictionary = null;
                            if (!string.IsNullOrWhiteSpace(dynamic_variables))
                            {
                                try
                                {
                                    dynamicVariablesDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(dynamic_variables);
                                    if (dynamicVariablesDictionary == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:DYNAMIC_VARIABLES_DESERIALIZATION_FAILED",
                                            $"Error deserializing dynamic variables for row {currentRowLine}."
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:DYNAMIC_VARIABLES_DESERIALIZATION_FAILED",
                                        $"Error deserializing dynamic variables for row {currentRowLine}: {ex.Message}"
                                    );
                                }
                            }
                            if (dynamicVariablesDictionary != null)
                            {
                                foreach (var dynamicVariableItem in dynamicVariablesDictionary)
                                {
                                    if (currentOutboundCallRow.DynamicVariables.ContainsKey(dynamicVariableItem.Key))
                                    {
                                        currentOutboundCallRow.DynamicVariables[dynamicVariableItem.Key] = dynamicVariableItem.Value;
                                    }
                                    else
                                    {
                                        currentOutboundCallRow.DynamicVariables.Add(dynamicVariableItem.Key, dynamicVariableItem.Value);
                                    }
                                }
                            }

                            Dictionary<string, string>? metadataDictionary = null;
                            if (!string.IsNullOrWhiteSpace(metadata))
                            {
                                try
                                {
                                    metadataDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(metadata);
                                    if (metadataDictionary == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:METADATA_DESERIALIZATION_FAILED",
                                            $"Error deserializing metadata for row {currentRowLine}."
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:METADATA_DESERIALIZATION_FAILED",
                                        $"Error deserializing metadata for row {currentRowLine}: {ex.Message}"
                                    );
                                }
                            }
                            if (metadataDictionary != null)
                            {
                                foreach (var metadataItem in metadataDictionary)
                                {
                                    if (currentOutboundCallRow.Metadata.ContainsKey(metadataItem.Key))
                                    {
                                        currentOutboundCallRow.Metadata[metadataItem.Key] = metadataItem.Value;
                                    }
                                    else
                                    {
                                        currentOutboundCallRow.Metadata.Add(metadataItem.Key, metadataItem.Value);
                                    }
                                }
                            }

                            rowsDataList.Add(currentOutboundCallRow);
                        }
                        catch (Exception ex)
                        {
                            return result.SetFailureResult(
                                "ValidateAndBuildBulkCSVCallFile:CSV_FILE_EXCEPTION",
                                $"Error reading row {currentRowLine}: {ex.Message}"
                            );
                        }

                        // Validate Variables based on Campaign
                        if (telephonyCampaignData.Variables.DynamicVariables.Count > 0)
                        {
                            foreach (var variableData in telephonyCampaignData.Variables.DynamicVariables)
                            {
                                var dynamicVariableItem = currentOutboundCallRow.DynamicVariables.FirstOrDefault(x => x.Key == variableData.Key);
                                if (dynamicVariableItem.Key == null)
                                {
                                    if (variableData.IsRequired)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:REQUIRED_DYNAMIC_VARIABLE_NOT_FOUND",
                                            $"Required dynamic variable '{variableData.Key}' not found in row {currentRowLine}. Telephony campaign rule."
                                        );
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrWhiteSpace(dynamicVariableItem.Value) && !variableData.IsEmptyOrNullAllowed)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:REQUIRED_DYNAMIC_VARIABLE_NOT_FOUND",
                                            $"Required dynamic variable '{variableData.Key}' cannot be empty in row {currentRowLine}. Telephony campaign rule."
                                        );
                                    }
                                }
                            }
                        }
                        if (telephonyCampaignData.Variables.Metadata.Count > 0)
                        {
                            foreach (var variableData in telephonyCampaignData.Variables.Metadata)
                            {
                                var metadataItem = currentOutboundCallRow.Metadata.FirstOrDefault(x => x.Key == variableData.Key);
                                if (metadataItem.Key == null)
                                {
                                    if (variableData.IsRequired)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:REQUIRED_METADATA_NOT_FOUND",
                                            $"Required metadata '{variableData.Key}' not found in row {currentRowLine}."
                                        );
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrWhiteSpace(metadataItem.Value) && !variableData.IsEmptyOrNullAllowed)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:REQUIRED_METADATA_NOT_FOUND",
                                            $"Required metadata '{variableData.Key}' cannot be empty in row {currentRowLine}."
                                        );
                                    }
                                }
                            }
                        }

                    }
                }

                if (rowsDataList.Count == 0)
                {
                    return result.SetFailureResult(
                        "ValidateAndBuildBulkCSVCallFile:NO_ROWS_FOUND",
                        "No rows found in CSV file or were converted."
                    );
                }

                return result.SetSuccessResult(rowsDataList);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ValidateAndBuildBulkCSVCallFile:EXCEPTION",
                    $"Error reading CSV file: {ex.Message}"
                );
            }
        }
    
        private FunctionReturnResult<string?> GetBusinessNumberIdForToNumber(string toNumber, BusinessAppTelephonyCampaign campaignData)
        {
            var result = new FunctionReturnResult<string?>();

            var phoneNumber = PhoneNumberUtil.GetInstance().Parse(toNumber, "ZZ");
            if (phoneNumber == null)
            {
                return result.SetFailureResult(
                    "GetBusinessNumberIdForToNumber:TO_NUMBER_PARSE_FAILED",
                    "Error parsing 'to_number'. Make sure number start with +countrycode."
                );
            }

            var phoneNumberRegion = PhoneNumberUtil.GetInstance().GetRegionCodeForNumber(phoneNumber);
            if (phoneNumberRegion == null)
            {
                return result.SetFailureResult(
                    "GetBusinessNumberIdForToNumber:TO_NUMBER_GET_REGION_FAILED",
                    "Error parsing 'to_number'. Make sure number start with +countrycode."
                );
            }

            if (campaignData.NumberRoute.RouteNumberList.TryGetValue(phoneNumberRegion, out var businessNumberId))
            {
                return result.SetSuccessResult(businessNumberId);
            }

            return result.SetSuccessResult(campaignData.NumberRoute.DefaultNumberId);
        }
    }
}
