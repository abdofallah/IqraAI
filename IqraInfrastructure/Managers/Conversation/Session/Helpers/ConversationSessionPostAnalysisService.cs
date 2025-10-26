using IqraCore.Entities.Business;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.PostAnalysis;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WebSession;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Helpers;
using IqraInfrastructure.Helpers.Conversation;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Session.Logger;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Conversation.Session.Helpers
{
    public class ConversationSessionPostAnalysisService
    {
        private readonly SessionLoggerFactory _loggerFactory;
        private readonly ILogger<ConversationSessionPostAnalysisService> _logger;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly BusinessManager _businessManager;
        private readonly LLMProviderManager _llmProviderManager;

        public ConversationSessionPostAnalysisService(
            SessionLoggerFactory loggerFactory,
            ConversationStateRepository conversationStateRepository,
            BusinessManager businessManager,
            LLMProviderManager llmProviderManager
        ) {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<ConversationSessionPostAnalysisService>();
            _conversationStateRepository = conversationStateRepository;
            _businessManager = businessManager;
            _llmProviderManager = llmProviderManager;
        }

        public async Task PerformTelephonyOutboundSessionPostCallAnalysis(string sessionId, BusinessApp businessAppData, OutboundCallQueueData callQueueData, BusinessAppTelephonyCampaign campaignData)
        {
            try
            {
                // Initital Checks
                if (campaignData.PostAnalysis.PostAnalysisId == null)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.NotSet);

                    _logger.LogDebug("Post analysis ID is not set for campaign ID {CampaignId}. Skipping.", campaignData.Id);
                    return;
                }
                string postAnalysisId = campaignData.PostAnalysis.PostAnalysisId;
                var postAnalysisData = businessAppData.PostAnalysis.Find(p => p.Id == postAnalysisId);
                if (postAnalysisData == null)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Post analysis data not found for post analysis ID {PostAnalysisId}", postAnalysisId);
                    return;
                }
                if (!postAnalysisData.Summary.IsActive && !postAnalysisData.Tagging.IsActive && !postAnalysisData.Extraction.IsActive)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.NothingToProcess);

                    _logger.LogDebug("Post analysis data is not active for post analysis ID {PostAnalysisId}. Skipping.", postAnalysisId);
                    return;
                }

                await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Processing);
                _logger.LogDebug("Processing post call analysis for post analysis ID {PostAnalysisId}", postAnalysisId);

                // Generate Context using Variables
                var conversationStateData = await _conversationStateRepository.GetByIdAsync(sessionId);
                if (conversationStateData == null)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Conversation state data not found for session {SessionId} while processing post call analysis", sessionId);
                    return;
                }
                var contextVariableArguementsResult = GetTelephonyOutboundArguements(callQueueData, conversationStateData);
                if (!contextVariableArguementsResult.Success)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Failed to generate context variables for post call analysis for post analysis ID {PostAnalysisId}", postAnalysisId);
                    return;
                }
                var contextVariableArguements = contextVariableArguementsResult.Data!;
                var finalContextArguementsVariables = new Dictionary<string, object?>();
                var configuredArguments = campaignData.PostAnalysis.ContextVariables;
                if (configuredArguments != null)
                {
                    foreach (var configuredArg in configuredArguments)
                    {
                        var argumentName = configuredArg.Name;
                        var argumentTemplate = configuredArg.Value;

                        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                            argumentTemplate.ToString()!,
                            contextVariableArguements
                        );

                        finalContextArguementsVariables[argumentName] = processedValue;
                    }
                }
                var promptContextResult = GeneratePromptContext(finalContextArguementsVariables, configuredArguments);
                if (!promptContextResult.Success)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Failed to generate prompt context for post call analysis for post analysis ID {PostAnalysisId} with error: [{ErrorCode}] {ErrorMessage}", postAnalysisId, promptContextResult.Code, promptContextResult.Message);
                    return;
                }
                var promptContext = promptContextResult.Data!;

                await ProcessPostAnalysisTasks(sessionId, businessAppData, postAnalysisData, promptContext);
            }
            catch (Exception ex)
            {
                await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);
                _logger.LogError(ex, "Error performing session post call analysis for session {SessionId}", sessionId);
            }
        }
        public async Task PerformTelephonyInboundSessionPostCallAnalysis(string sessionId, BusinessApp businessAppData, InboundCallQueueData callQueueData, BusinessAppRoute routeData)
        {
            try
            {
                // Initital Checks
                if (routeData.PostAnalysis.PostAnalysisId == null)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.NotSet);

                    _logger.LogDebug("Post analysis ID is not set for route ID {RouteId}. Skipping.", routeData.Id);
                    return;
                }
                string postAnalysisId = routeData.PostAnalysis.PostAnalysisId;
                var postAnalysisData = businessAppData.PostAnalysis.Find(p => p.Id == postAnalysisId);
                if (postAnalysisData == null)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Post analysis data not found for post analysis ID {PostAnalysisId}", postAnalysisId);
                    return;
                }
                if (!postAnalysisData.Summary.IsActive && !postAnalysisData.Tagging.IsActive && !postAnalysisData.Extraction.IsActive)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.NothingToProcess);

                    _logger.LogDebug("Post analysis data is not active for post analysis ID {PostAnalysisId}. Skipping.", postAnalysisId);
                    return;
                }

                await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Processing);
                _logger.LogDebug("Processing post call analysis for post analysis ID {PostAnalysisId}", postAnalysisId);

                // Generate Context using Variables
                var conversationStateData = await _conversationStateRepository.GetByIdAsync(sessionId);
                if (conversationStateData == null)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Conversation state data not found for session {SessionId} while processing post call analysis", sessionId);
                    return;
                }
                var contextVariableArguementsResult = GetRouteInboundArguements(callQueueData, conversationStateData);
                if (!contextVariableArguementsResult.Success)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Failed to generate context variables for post call analysis for post analysis ID {PostAnalysisId}", postAnalysisId);
                    return;
                }
                var contextVariableArguements = contextVariableArguementsResult.Data!;
                var finalContextArguementsVariables = new Dictionary<string, object?>();
                var configuredArguments = routeData.PostAnalysis.ContextVariables;
                if (configuredArguments != null)
                {
                    foreach (var configuredArg in configuredArguments)
                    {
                        var argumentName = configuredArg.Name;
                        var argumentTemplate = configuredArg.Value;

                        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                            argumentTemplate.ToString()!,
                            contextVariableArguements
                        );

                        finalContextArguementsVariables[argumentName] = processedValue;
                    }
                }
                var promptContextResult = GeneratePromptContext(finalContextArguementsVariables, configuredArguments);
                if (!promptContextResult.Success)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Failed to generate prompt context for post call analysis for post analysis ID {PostAnalysisId} with error: [{ErrorCode}] {ErrorMessage}", postAnalysisId, promptContextResult.Code, promptContextResult.Message);
                    return;
                }
                var promptContext = promptContextResult.Data!;

                await ProcessPostAnalysisTasks(sessionId, businessAppData, postAnalysisData, promptContext);
            }
            catch (Exception ex)
            {
                await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);
                _logger.LogError(ex, "Error performing session post call analysis for session {SessionId}", sessionId);
            }
        }
        public async Task PerformWebSessionPostCallAnalysis(string sessionId, BusinessApp businessAppData, WebSessionData sessionData, BusinessAppWebCampaign campaignData)
        {
            try
            {
                // Initital Checks
                if (campaignData.PostAnalysis.PostAnalysisId == null)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.NotSet);

                    _logger.LogDebug("Post analysis ID is not set for web campaign ID {WebCampaignId}. Skipping.", campaignData.Id);
                    return;
                }
                string postAnalysisId = campaignData.PostAnalysis.PostAnalysisId;
                var postAnalysisData = businessAppData.PostAnalysis.Find(p => p.Id == postAnalysisId);
                if (postAnalysisData == null)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Post analysis data not found for post analysis ID {PostAnalysisId}", postAnalysisId);
                    return;
                }
                if (!postAnalysisData.Summary.IsActive && !postAnalysisData.Tagging.IsActive && !postAnalysisData.Extraction.IsActive)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.NothingToProcess);

                    _logger.LogDebug("Post analysis data is not active for post analysis ID {PostAnalysisId}. Skipping.", postAnalysisId);
                    return;
                }

                await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Processing);
                _logger.LogDebug("Processing post call analysis for post analysis ID {PostAnalysisId}", postAnalysisId);

                // Generate Context using Variables
                var conversationStateData = await _conversationStateRepository.GetByIdAsync(sessionId);
                if (conversationStateData == null)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Conversation state data not found for session {SessionId} while processing post call analysis", sessionId);
                    return;
                }
                var contextVariableArguementsResult = GetWebSessionArgurments(sessionData, conversationStateData);
                if (!contextVariableArguementsResult.Success)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Failed to generate context variables for post call analysis for post analysis ID {PostAnalysisId}", postAnalysisId);
                    return;
                }
                var contextVariableArguements = contextVariableArguementsResult.Data!;
                var finalContextArguementsVariables = new Dictionary<string, object?>();
                var configuredArguments = campaignData.PostAnalysis.ContextVariables;
                if (configuredArguments != null)
                {
                    foreach (var configuredArg in configuredArguments)
                    {
                        var argumentName = configuredArg.Name;
                        var argumentTemplate = configuredArg.Value;

                        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                            argumentTemplate.ToString()!,
                            contextVariableArguements
                        );

                        finalContextArguementsVariables[argumentName] = processedValue;
                    }
                }
                var promptContextResult = GeneratePromptContext(finalContextArguementsVariables, configuredArguments);
                if (!promptContextResult.Success)
                {
                    await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);

                    _logger.LogError("Failed to generate prompt context for post call analysis for post analysis ID {PostAnalysisId} with error: [{ErrorCode}] {ErrorMessage}", postAnalysisId, promptContextResult.Code, promptContextResult.Message);
                    return;
                }
                var promptContext = promptContextResult.Data!;

                await ProcessPostAnalysisTasks(sessionId, businessAppData, postAnalysisData, promptContext);
            }
            catch (Exception ex)
            {
                await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);
                _logger.LogError(ex, "Error performing session post call analysis for session {SessionId}", sessionId);
            }
        }

        private async Task ProcessPostAnalysisTasks(string sessionId, BusinessApp businessAppData, BusinessAppPostAnalysis postAnalysisData, string promptContext)
        {
            // Run Processing Tasks
            Task<FunctionReturnResult<string?>>? summaryGenerationTask = null;
            Task<FunctionReturnResult<List<ConversationPostAnalsysisTaggingResultData>?>>? taggingTask = null;
            Task<FunctionReturnResult<List<ConversationPostAnalsysisExtractionFieldResultData>?>>? extractionTask = null;
            if (postAnalysisData.Summary.IsActive)
            {
                summaryGenerationTask = PerformConversationSummaryGeneration(sessionId, businessAppData, postAnalysisData, promptContext);
                _logger.LogDebug("Generating summary for post analysis ID {PostAnalysisId}", postAnalysisData.Id);
            }
            if (postAnalysisData.Tagging.IsActive)
            {
                taggingTask = PerformConversationTagging(sessionId, businessAppData, postAnalysisData, promptContext);
                _logger.LogDebug("Generating tagging for post analysis ID {PostAnalysisId}", postAnalysisData.Id);
            }
            if (postAnalysisData.Extraction.IsActive)
            {
                extractionTask = PerformConversationExtraction(sessionId, businessAppData, postAnalysisData, promptContext);
                _logger.LogDebug("Generating extraction for post analysis ID {PostAnalysisId}", postAnalysisData.Id);
            }
            await Task.WhenAll(summaryGenerationTask ?? Task.CompletedTask, taggingTask ?? Task.CompletedTask, extractionTask ?? Task.CompletedTask);

            // Get Tasks Results
            FunctionReturnResult<string?>? summaryGenerationResult = null;
            FunctionReturnResult<List<ConversationPostAnalsysisTaggingResultData>?>? taggingResult = null;
            FunctionReturnResult<List<ConversationPostAnalsysisExtractionFieldResultData>?>? extractionResult = null;
            if (summaryGenerationTask != null)
            {
                summaryGenerationResult = await summaryGenerationTask;
            }
            if (taggingTask != null)
            {
                taggingResult = await taggingTask;
            }
            if (extractionTask != null)
            {
                extractionResult = await extractionTask;
            }

            // Set Post Analysis
            ConversationPostAnalysis postAnalysis = new ConversationPostAnalysis()
            {
                Status = ConversationPostAnalysisStatusEnum.Success
            };
            if (postAnalysisData.Summary.IsActive)
            {
                if (summaryGenerationResult != null && !summaryGenerationResult.Success)
                {
                    _logger.LogError("Error generating conversation summary for session {SessionId}: [{ErrorCode}] {ErrorMessage}", sessionId, summaryGenerationResult.Code, summaryGenerationResult.Message);
                }
                else
                {
                    postAnalysis.Summary = summaryGenerationResult!.Data;
                }
            }
            if (postAnalysisData.Tagging.IsActive)
            {
                if (taggingResult != null && !taggingResult.Success)
                {
                    _logger.LogError("Error tagging conversation for session {SessionId}: [{ErrorCode}] {ErrorMessage}", sessionId, taggingResult.Code, taggingResult.Message);
                }
                else
                {
                    postAnalysis.Tags = taggingResult!.Data;
                }
            }
            if (postAnalysisData.Extraction.IsActive)
            {
                if (extractionResult != null && !extractionResult.Success)
                {
                    _logger.LogError("Error extracting conversation for session {SessionId}: [{ErrorCode}] {ErrorMessage}", sessionId, extractionResult.Code, extractionResult.Message);
                }
                else
                {
                    postAnalysis.ExtractedFields = extractionResult!.Data;
                }
            }

            // Update Database
            var updateResult = await _conversationStateRepository.UpdatePostAnalysisAsync(sessionId, postAnalysis);
            if (!updateResult)
            {
                await _conversationStateRepository.UpdatePostAnalaysisStatusAsync(sessionId, ConversationPostAnalysisStatusEnum.Failed);
                _logger.LogError("Error updating post analysis for session {SessionId} {PostAnalysisResult}", sessionId, postAnalysisData);
            }
        }

        // Processing
        private async Task<FunctionReturnResult<string?>> PerformConversationSummaryGeneration(string sessionId, BusinessApp businessAppData, BusinessAppPostAnalysis postAnalysisData, string context)
        {
            var result = new FunctionReturnResult<string?>();

            try
            {
                var llmServiceResult = await BuildLLMIntegrationService(sessionId, businessAppData, postAnalysisData.Configuration.LLMIntegration);
                if (!llmServiceResult.Success)
                {
                    return result.SetFailureResult(
                        $"PerformConversationSummaryGeneration:{llmServiceResult.Code}" ,
                        llmServiceResult.Message
                    );
                }
                var llmService = llmServiceResult.Data!;

                llmService.SetSystemPrompt($"You job is to generate a summary of the conversation.\n\nFollow the guidelines for constructing the summary as follow:\n{postAnalysisData.Summary.Prompt}");
                llmService.AddUserMessage($"{context}\n\nGenerate the summary.");

                StringBuilder summaryResponseBuilder = new StringBuilder();
                llmService.MessageStreamed += (sender, args) => {
                    FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(args.ResponseObject, llmService.GetProviderType());
                    if (!chunkExtractResult.Success || !chunkExtractResult.Data.HasValue)
                    {
                        // todo log error
                        return;
                    }
                    (string? deltaText, bool isEndOfResponse) = chunkExtractResult.Data.Value;

                    if (!string.IsNullOrEmpty(deltaText))
                    {
                        summaryResponseBuilder.Append(deltaText);
                    }

                    if (isEndOfResponse)
                    {
                        // needed?
                    }
                };
                await llmService.ProcessInputAsync(CancellationToken.None);

                try
                {
                    llmService.ClearMessages();
                    llmService.ClearMessageStreamed();
                    llmService.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing Post Analysis Summary Generation LLM service for session {SessionId}", sessionId);
                }

                string summaryResponse = summaryResponseBuilder.ToString(); // TODO transform the response to the expected format/extract sumamry only
                return result.SetSuccessResult(summaryResponse);
            }
            catch (Exception ex) {
                return result.SetFailureResult(
                    "PerformConversationSummaryGeneration:EXCEPTION",
                    $"Error generating conversation summary for session {sessionId}: {ex.Message}"
                );
            }
        }
        private async Task<FunctionReturnResult<List<ConversationPostAnalsysisTaggingResultData>?>> PerformConversationTagging(string sessionId, BusinessApp businessAppData, BusinessAppPostAnalysis postAnalysisData, string context)
        {
            var result = new FunctionReturnResult<List<ConversationPostAnalsysisTaggingResultData>?>();

            try
            {


                return result.SetSuccessResult(null);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "PerformConversationTagging:EXCEPTION",
                    $"Error tagging conversation for session {sessionId}: {ex.Message}"
                );
            }
        }
        private async Task<FunctionReturnResult<List<ConversationPostAnalsysisExtractionFieldResultData>?>> PerformConversationExtraction(string sessionId, BusinessApp businessAppData, BusinessAppPostAnalysis postAnalysisData, string context)
        {
            var result = new FunctionReturnResult<List<ConversationPostAnalsysisExtractionFieldResultData>?>();

            try
            {


                return result.SetSuccessResult(null);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "PerformConversationExtraction:EXCEPTION",
                    $"Error extracting conversation for session {sessionId}: {ex.Message}"
                );
            }
        }

        // Helpers
        private async Task<FunctionReturnResult<ILLMService?>> BuildLLMIntegrationService(string sessionId, BusinessApp businessAppData, BusinessAppAgentIntegrationData llmIntegration)
        {
            var result = new FunctionReturnResult<ILLMService?>();

            try
            {
                var businessLLMIntegrationData = businessAppData.Integrations.Find(i => i.Id == llmIntegration.Id);
                if (businessLLMIntegrationData == null)
                {
                    return result.SetFailureResult(
                        "BuildLLMIntegrationService:NOT_FOUND",
                        $"Business app LLM integration {llmIntegration.Id} not found"
                    );
                }

                var llmServiceBuildResult = await _llmProviderManager.BuildProviderServiceByIntegration(_loggerFactory, businessLLMIntegrationData, llmIntegration, new Dictionary<string, string> { });
                if (!llmServiceBuildResult.Success || llmServiceBuildResult.Data == null)
                {
                    return result.SetFailureResult(
                        $"BuildLLMIntegrationService:{llmServiceBuildResult.Code}",
                        llmServiceBuildResult.Message
                    );
                }

                return result.SetSuccessResult(llmServiceBuildResult.Data!);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "BuildLLMIntegrationService:EXCEPTION",
                    $"Error building LLM integration service for session {sessionId}: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<string?> GeneratePromptContext(Dictionary<string, object?> arguements, List<BusinessAppCampaignPostAnalysisContextVariable> contextVariables)
        {
            var result = new FunctionReturnResult<string?>();

            try
            {
                string prompt = "Here is the context of the conversation:\n\n```";

                if (arguements.Count == 0)
                {
                    prompt += "There is no context provided for the conversation\n```";

                    return result.SetSuccessResult(prompt);
                }

                for (int i = 0; i < arguements.Count; i++)
                {
                    var arguement = arguements.ElementAt(i);

                    var contextVariable = contextVariables.Find(c => c.Name == arguement.Key);
                    if (contextVariable == null)
                    {
                        prompt += $"{arguement.Key}:\n{ConvertContextVariableArguementValueForPrompt(arguement.Value)}\n---\n";
                        continue;
                    }

                    prompt += $"{arguement.Key} ({contextVariable.Description}):\n{ConvertContextVariableArguementValueForPrompt(arguement.Value)}\n{(i < arguements.Count - 1 ? "---\n" : "")}";
                }

                prompt += "```";

                return result.SetSuccessResult(prompt);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GeneratePromptContext:EXCEPTION",
                    $"Error generating prompt context: {ex.Message}"
                );
            }
        }
        private string ConvertContextVariableArguementValueForPrompt(object? value)
        {
            try
            {
                var valueType = value?.GetType();

                if (value == null)
                {
                    return "Value is not defined (null).";
                }

                if (valueType == typeof(string))
                {
                    return (string)value;
                }

                if (valueType == typeof(int) || valueType == typeof(long) || valueType == typeof(float) || valueType == typeof(double) || valueType == typeof(decimal))
                {
                    return value.ToString()!;
                }

                if (valueType == typeof(bool))
                {
                    return (bool)value ? "true" : "false";
                }

                if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listValue = (List<object?>)value!;
                    var convertedList = new List<string>();

                    foreach ( var item in listValue ) {
                        convertedList.Add(ConvertContextVariableArguementValueForPrompt(item));
                    }

                    return string.Join("\n- ", convertedList);
                }

                if (valueType == typeof(DateTime))
                {
                    return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
                }

                if (valueType.IsEnum)
                {
                    return Enum.GetName(valueType, value)!;
                }

                if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var dictionaryValue = (Dictionary<string, object?>)value!;
                    var convertedDictionary = new Dictionary<string, string>();

                    foreach ( var item in dictionaryValue ) {
                        convertedDictionary.Add(item.Key, ConvertContextVariableArguementValueForPrompt(item.Value));
                    }

                    return string.Join("\n- ", convertedDictionary);
                }

                return JsonSerializer.Serialize(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConvertContextVariableArguementValueForPrompt:EXCEPTION");

                return "Unable to process value for the given context variable.";
            }
        }

        // Argument Builder
        private FunctionReturnResult<Dictionary<string, object?>?> GetTelephonyOutboundArguements(OutboundCallQueueData callQueueData, ConversationState conversationStateData)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>?>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    // --- Call Queue Data ---
                    { "call_queue_id", callQueueData.Id },
                    { "call_queue_created_at", callQueueData.CreatedAt },
                    { "call_queue_enqueued_at", callQueueData.EnqueuedAt },
                    { "call_queue_processing_started_at", callQueueData.ProcessingStartedAt },
                    { "call_queue_completed_at", callQueueData.CompletedAt },
                    { "call_queue_status", callQueueData.Status.ToString() },
                    { "call_queue_campaign_id", callQueueData.CampaignId },
                    { "call_queue_calling_number_id", callQueueData.CallingNumberId },
                    { "call_queue_calling_number_provider", callQueueData.CallingNumberProvider.ToString() },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_recipient_number", callQueueData.RecipientNumber },
                    { "call_queue_scheduled_for_date_time", callQueueData.ScheduledForDateTime },
                    { "call_queue_dynamic_variables", callQueueData.DynamicVariables },
                    { "call_queue_metadata", callQueueData.Metadata },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", conversationStateData.EndType.ToString() },
                    { "conversation_end_time", conversationStateData.EndTime },
                    { "conversation_turns", conversationStateData.Turns },
                    { "conversation_turns_simplified", ConversationTurnsCompiler.SimplifyConversationTurns(conversationStateData.Turns) }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetTelephonyOutboundArguements:EXCEPTION",
                    $"Error getting telephony outbound call arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>?> GetRouteInboundArguements(InboundCallQueueData callQueueData, ConversationState conversationStateData)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>?>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    // --- Call Queue Data ---
                    { "call_queue_id", callQueueData.Id },
                    { "call_queue_created_at", callQueueData.CreatedAt },
                    { "call_queue_enqueued_at", callQueueData.EnqueuedAt },
                    { "call_queue_processing_started_at", callQueueData.ProcessingStartedAt },
                    { "call_queue_completed_at", callQueueData.CompletedAt },
                    { "call_queue_status", callQueueData.Status.ToString() },
                    { "call_queue_route_id", callQueueData.RouteId },
                    { "call_queue_route_number_id", callQueueData.RouteNumberId },
                    { "call_queue_route_number_provider", callQueueData.RouteNumberProvider.ToString() },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_caller_number", callQueueData.CallerNumber },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", conversationStateData.EndType.ToString() },
                    { "conversation_end_time", conversationStateData.EndTime },
                    { "conversation_turns", conversationStateData.Turns },
                    { "conversation_turns_simplified", ConversationTurnsCompiler.SimplifyConversationTurns(conversationStateData.Turns) }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetRouteInboundArguements:EXCEPTION",
                    $"Error getting route inbound call arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>?> GetWebSessionArgurments(WebSessionData webSessionData, ConversationState conversationStateData)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>?>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    // --- Call Queue Data ---
                    { "web_session_id", webSessionData.Id },
                    { "web_session_created_at", webSessionData.CreatedAt },
                    { "web_session_status", webSessionData.Status.ToString() },
                    { "web_session_web_campaign_id", webSessionData.WebCampaignId },
                    { "web_session_client_identifier", webSessionData.ClientIdentifier },
                    { "web_session_dynamic_variables", webSessionData.DynamicVariables },
                    { "web_session_metadata", webSessionData.Metadata },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", conversationStateData.EndType.ToString() },
                    { "conversation_end_time", conversationStateData.EndTime },
                    { "conversation_turns", conversationStateData.Turns },
                    { "conversation_turns_simplified", ConversationTurnsCompiler.SimplifyConversationTurns(conversationStateData.Turns) }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetWebSessionArgurments:EXCEPTION",
                    $"Error getting web session call arguements: {ex.Message}"
                );
            }
        }
    }
}
