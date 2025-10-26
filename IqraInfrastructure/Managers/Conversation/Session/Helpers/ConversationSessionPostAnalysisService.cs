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
using IqraInfrastructure.Helpers.Json;
using IqraInfrastructure.Managers.Conversation.Session.Logger;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.Conversation.Session.Helpers
{
    public class ConversationSessionPostAnalysisService
    {
        private readonly SessionLoggerFactory _loggerFactory;
        private readonly ILogger<ConversationSessionPostAnalysisService> _logger;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly LLMProviderManager _llmProviderManager;

        public ConversationSessionPostAnalysisService(
            SessionLoggerFactory loggerFactory,
            ConversationStateRepository conversationStateRepository,
            LLMProviderManager llmProviderManager
        ) {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<ConversationSessionPostAnalysisService>();
            _conversationStateRepository = conversationStateRepository;
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
            Task<FunctionReturnResult<ConversationSummaryGenerationResultData?>>? summaryGenerationTask = null;
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
            FunctionReturnResult<ConversationSummaryGenerationResultData?>? summaryGenerationResult = null;
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
        private async Task<FunctionReturnResult<ConversationSummaryGenerationResultData?>> PerformConversationSummaryGeneration(string sessionId, BusinessApp businessAppData, BusinessAppPostAnalysis postAnalysisData, string context)
        {
            var result = new FunctionReturnResult<ConversationSummaryGenerationResultData?>();

            try
            {
                var llmServiceResult = await BuildLLMIntegrationService(sessionId, businessAppData, postAnalysisData.Configuration.LLMIntegration);
                if (!llmServiceResult.Success)
                {
                    return result.SetFailureResult(
                        $"PerformConversationSummaryGeneration:{llmServiceResult.Code}",
                        llmServiceResult.Message
                    );
                }
                var llmService = llmServiceResult.Data!;

                // --- Refined System Prompt ---
                var systemPrompt = $@"You are an expert AI assistant. Your task is to analyze the provided conversation context and generate a concise, accurate summary.

Follow these user-provided guidelines for constructing the summary:
<guidelines>
{postAnalysisData.Summary.Prompt}
</guidelines>

You MUST respond with a JSON object enclosed in a ```json code block.
The JSON object must have two keys: ""Thinking"" and ""Summary"".
- ""Thinking"": Briefly explain your reasoning and the thought process behind the summary you are about to write.
- ""Summary"": The final generated summary based on the conversation and the guidelines.

Example Response Format:
```json
{{
    ""Thinking"": ""The user expressed frustration about a billing error. The agent successfully identified the issue, apologized, and provided a clear resolution path. The summary should capture these key points."",
    ""Summary"": ""The customer called regarding an incorrect charge on their recent invoice. The agent investigated the issue, confirmed a billing system error, and processed a credit for the overcharged amount. The customer was satisfied with the resolution.""
}}
```";

                llmService.SetSystemPrompt(systemPrompt);
                llmService.SetMaxTokens(10000);
                llmService.AddUserMessage($"{context}\n\nGenerate the summary in the specified JSON format.");

                StringBuilder responseBuilder = new StringBuilder();
                bool isStreamingEnded = false;
                bool hasStreamingFailed = false;
                string? streamingFailedMessage = null;

                llmService.MessageStreamed += (sender, args) =>
                {
                    FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(args.ResponseObject, llmService.GetProviderType());
                    if (!chunkExtractResult.Success || !chunkExtractResult.Data.HasValue)
                    {
                        _logger.LogWarning("Could not extract chunk data during summary generation for session {SessionId}.", sessionId);
                        return;
                    }
                    (string? deltaText, bool isEndOfResponse) = chunkExtractResult.Data.Value;

                    if (!string.IsNullOrEmpty(deltaText))
                    {
                        responseBuilder.Append(deltaText);
                    }

                    if (isEndOfResponse)
                    {
                        isStreamingEnded = true;
                    }
                };

                llmService.MessageStreamedCancelled += (sender, args) =>
                {
                    if (hasStreamingFailed) return;
                    hasStreamingFailed = true;
                    streamingFailedMessage = $"Summary Generation LLM Failed: [{Enum.GetName(args.Type)} {args.ResponseCode}] {args.ResponseMessage}";
                };

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                await llmService.ProcessInputAsync(cancellationTokenSource.Token);

                var stopWatch = Stopwatch.StartNew();
                while (!isStreamingEnded && !hasStreamingFailed)
                {
                    if (stopWatch.ElapsedMilliseconds > 60000) // 60 seconds timeout
                    {
                        hasStreamingFailed = true;
                        streamingFailedMessage = "Summary Generation LLM Timed Out after 60 seconds.";
                        cancellationTokenSource.Cancel();
                        break;
                    }
                    await Task.Delay(100);
                }
                stopWatch.Stop();

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

                if (hasStreamingFailed)
                {
                    return result.SetFailureResult(
                        "PerformConversationSummaryGeneration:LLM_STREAM_FAILED",
                        streamingFailedMessage!
                    );
                }

                string rawLlmResponse = responseBuilder.ToString();
                string? jsonBlock = ExtractJsonBlock(rawLlmResponse);

                if (string.IsNullOrWhiteSpace(jsonBlock))
                {
                    return result.SetFailureResult(
                        "PerformConversationSummaryGeneration:JSON_EXTRACTION_FAILED",
                        $"Could not extract a valid JSON block from the LLM response. Raw Response: {rawLlmResponse}"
                    );
                }

                try
                {
                    var summaryData = JsonSerializer.Deserialize<ConversationSummaryGenerationResultData>(jsonBlock, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (summaryData == null)
                    {
                        return result.SetFailureResult(
                            "PerformConversationSummaryGeneration:JSON_DESERIALIZATION_FAILED",
                            "Deserialized JSON object is null."
                        );
                    }

                    return result.SetSuccessResult(summaryData);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse JSON for summary generation on session {SessionId}. JSON Block: {JsonBlock}", sessionId, jsonBlock);
                    return result.SetFailureResult(
                        "PerformConversationSummaryGeneration:JSON_PARSE_EXCEPTION",
                        $"Failed to parse JSON from LLM: {ex.Message}"
                    );
                }
            }
            catch (Exception ex)
            {
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
                if (postAnalysisData.Tagging.Tags == null || !postAnalysisData.Tagging.Tags.Any())
                {
                    _logger.LogInformation("No tags configured for post analysis on session {SessionId}. Skipping tagging.", sessionId);
                    return result.SetSuccessResult(new List<ConversationPostAnalsysisTaggingResultData>());
                }

                var llmServiceResult = await BuildLLMIntegrationService(sessionId, businessAppData, postAnalysisData.Configuration.LLMIntegration);
                if (!llmServiceResult.Success)
                {
                    return result.SetFailureResult(
                        $"PerformConversationTagging:{llmServiceResult.Code}",
                        llmServiceResult.Message
                    );
                }
                var llmService = llmServiceResult.Data!;

                // --- 1. Generate the tag definitions for the prompt ---
                string tagDefinitionsJson = GenerateTagDefinitionsForPrompt(postAnalysisData.Tagging.Tags);

                // --- 2. Construct the detailed system prompt ---
                var systemPrompt = $@"You are an expert conversation classification AI. Your task is to analyze a conversation and classify it by selecting the most appropriate tags from a predefined hierarchical structure.

Here are the available tag categories and their sub-tags, including their descriptions, IDs, and rules, in JSON format:
<available_tags>
{tagDefinitionsJson}
</available_tags>

---
**CRITICAL RULES & LOGIC**
---

**MAIN DIRECTIVE: A parent category should ONLY be included in the final JSON output IF you select one or more of its sub-tags.**

Follow these rules with absolute precision:
1.  **Analyze the Conversation**: First, understand the context of the conversation.
2.  **Select Sub-Tags**: Based on the context, decide which specific sub-tags are relevant from the `<available_tags>`.
3.  **Construct the Output**:
    - For each parent category where you selected at least one sub-tag, create a parent object in your response.
    - Nest the selected sub-tag(s) within that parent's `SubTags` array.
4.  **Handle Required Categories (`IsRequired: true`)**: For these categories, you MUST find and select at least one relevant sub-tag. Therefore, these parent categories will almost always appear in your output.
5.  **Handle Optional Categories (`IsRequired: false`)**: For these categories, you should ONLY include the parent category in your output IF you find a relevant sub-tag to select. If no sub-tags are relevant, **OMIT the entire parent category from your response.** Do not include it with an empty `SubTags` array.
6.  **Single vs. Multiple Choices (`AllowMultiple`)**: If `AllowMultiple: false`, you must select exactly one sub-tag for that level. If `AllowMultiple: true`, you may select one or more. This rule applies at each level of the hierarchy.
7.  **IDs and Thinking**:
    - The `TagId` for the parent object must be the ID of the category. The `Thinking` should justify why this entire category is relevant.
    - The `TagId` for the nested object must be the ID of the specific sub-tag you chose. The `Thinking` should justify why you chose that specific sub-tag.

---
**EXAMPLE SCENARIO**
---

**Example Available Tags:**
```json
[
  {{
    ""Id"": ""call-outcome-id"", ""Name"": ""Call Outcome"", ""Rules"": {{ ""IsRequired"": true, ""AllowMultiple"": false }},
    ""SubTags"": [
      {{ ""Id"": ""resolved-id"", ""Name"": ""Resolved"" }},
      {{ ""Id"": ""escalated-id"", ""Name"": ""Escalated"" }}
    ]
  }},
  {{
    ""Id"": ""features-mentioned-id"", ""Name"": ""Features Mentioned"", ""Rules"": {{ ""IsRequired"": false, ""AllowMultiple"": true }},
    ""SubTags"": [
      {{ ""Id"": ""dashboard-feature-id"", ""Name"": ""Dashboard"" }},
      {{ ""Id"": ""reporting-feature-id"", ""Name"": ""Reporting"" }}
    ]
  }}
]
```

**Example Conversation Context:** ""Hi, I was having trouble with the main dashboard page, but your previous agent fixed it for me. Thanks for the great support!""

**Correct JSON Response:**
```json
{{
  ""appliedTags"": [
    {{
      ""Thinking"": ""The call had a clear resolution, so the 'Call Outcome' category is relevant."",
      ""TagId"": ""call-outcome-id"",
      ""SubTags"": [
        {{
          ""Thinking"": ""The user stated their problem was 'fixed', which directly maps to the 'Resolved' sub-tag."",
          ""TagId"": ""resolved-id"",
          ""SubTags"": []
        }}
      ]
    }},
    {{
      ""Thinking"": ""The user explicitly mentioned a product feature, making the 'Features Mentioned' category relevant."",
      ""TagId"": ""features-mentioned-id"",
      ""SubTags"": [
        {{
          ""Thinking"": ""The user specifically mentioned the 'dashboard page', corresponding to the 'Dashboard' sub-tag."",
          ""TagId"": ""dashboard-feature-id"",
          ""SubTags"": []
        }}
      ]
    }}
  ]
}}
```

**INCORRECT Response (What NOT to do):**
```json
// THIS IS WRONG.
{{
  ""appliedTags"": [
    {{
      ""Thinking"": ""The call was resolved."",
      ""TagId"": ""resolved-id"", // WRONG: This is a sub-tag ID at the top level.
      ""SubTags"": []
    }},
    {{
      ""Thinking"": ""A feature was mentioned."",
      ""TagId"": ""features-mentioned-id"", // Partially correct, but...
      ""SubTags"": [] // WRONG: A relevant sub-tag was available but not selected and nested.
    }}
  ]
}}
```

---
**END OF EXAMPLE**
---

You MUST now analyze the real conversation context and provide the classification in the specified JSON format, following all the rules and the logic demonstrated in the example.
";

                llmService.SetSystemPrompt(systemPrompt);
                llmService.SetMaxTokens(10000);
                llmService.AddUserMessage($"{context}\n\nAnalyze the conversation and provide the classification in the specified JSON format.");

                // --- 3. Full streaming & processing logic ---
                StringBuilder responseBuilder = new StringBuilder();
                bool isStreamingEnded = false;
                bool hasStreamingFailed = false;
                string? streamingFailedMessage = null;

                llmService.MessageStreamed += (sender, args) =>
                {
                    FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(args.ResponseObject, llmService.GetProviderType());
                    if (!chunkExtractResult.Success || !chunkExtractResult.Data.HasValue)
                    {
                        _logger.LogWarning("Could not extract chunk data during tagging for session {SessionId}.", sessionId);
                        return;
                    }
                    (string? deltaText, bool isEndOfResponse) = chunkExtractResult.Data.Value;

                    if (!string.IsNullOrEmpty(deltaText))
                    {
                        responseBuilder.Append(deltaText);
                    }

                    if (isEndOfResponse)
                    {
                        isStreamingEnded = true;
                    }
                };

                llmService.MessageStreamedCancelled += (sender, args) =>
                {
                    if (hasStreamingFailed) return;
                    hasStreamingFailed = true;
                    streamingFailedMessage = $"Tagging LLM Failed: [{Enum.GetName(args.Type)} {args.ResponseCode}] {args.ResponseMessage}";
                    _logger.LogError("Post Analysis Tagging LLM service for session {SessionId} has failed: {FailureMessage}", sessionId, streamingFailedMessage);
                };

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                await llmService.ProcessInputAsync(cancellationTokenSource.Token);

                var stopWatch = Stopwatch.StartNew();
                while (!isStreamingEnded && !hasStreamingFailed)
                {
                    if (stopWatch.ElapsedMilliseconds > 60000) // 60 seconds timeout
                    {
                        hasStreamingFailed = true;
                        streamingFailedMessage = "Tagging LLM Timed Out after 60 seconds.";
                        cancellationTokenSource.Cancel();
                        break;
                    }
                    await Task.Delay(100);
                }
                stopWatch.Stop();

                try
                {
                    llmService.ClearMessages();
                    llmService.ClearMessageStreamed();
                    llmService.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing Post Analysis Tagging LLM service for session {SessionId}", sessionId);
                }

                if (hasStreamingFailed)
                {
                    return result.SetFailureResult(
                        "PerformConversationTagging:LLM_STREAM_FAILED",
                        streamingFailedMessage!
                    );
                }

                string rawLlmResponse = responseBuilder.ToString();


                // --- 4. Extract, Parse, and Return ---
                string? jsonBlock = ExtractJsonBlock(rawLlmResponse);
                if (string.IsNullOrWhiteSpace(jsonBlock))
                {
                    return result.SetFailureResult(
                        "PerformConversationTagging:JSON_EXTRACTION_FAILED",
                        $"Could not extract a valid JSON block from the LLM response for tagging. Raw: {rawLlmResponse}"
                    );
                }

                try
                {
                    var taggingResponse = JsonSerializer.Deserialize<ConversationTaggingLLMResponse>(jsonBlock, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (taggingResponse == null)
                    {
                        return result.SetFailureResult(
                            "PerformConversationTagging:JSON_DESERIALIZATION_FAILED",
                            "Deserialized tagging response object is null."
                        );
                    }
                    return result.SetSuccessResult(taggingResponse.AppliedTags);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse JSON for tagging on session {SessionId}. JSON Block: {JsonBlock}", sessionId, jsonBlock);
                    return result.SetFailureResult(
                        "PerformConversationTagging:JSON_PARSE_EXCEPTION",
                        $"Failed to parse JSON from LLM for tagging: {ex.Message}"
                    );
                }
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "PerformConversationTagging:EXCEPTION",
                    $"Error performing conversation tagging for session {sessionId}: {ex.Message}"
                );
            }
        }
        private async Task<FunctionReturnResult<List<ConversationPostAnalsysisExtractionFieldResultData>?>> PerformConversationExtraction(string sessionId, BusinessApp businessAppData, BusinessAppPostAnalysis postAnalysisData, string context)
        {
            var result = new FunctionReturnResult<List<ConversationPostAnalsysisExtractionFieldResultData>?>();

            try
            {
                if (postAnalysisData.Extraction.Fields == null || !postAnalysisData.Extraction.Fields.Any())
                {
                    _logger.LogInformation("No extraction fields configured for post analysis on session {SessionId}. Skipping extraction.", sessionId);
                    return result.SetSuccessResult(new List<ConversationPostAnalsysisExtractionFieldResultData>());
                }

                var llmServiceResult = await BuildLLMIntegrationService(sessionId, businessAppData, postAnalysisData.Configuration.LLMIntegration);
                if (!llmServiceResult.Success)
                {
                    return result.SetFailureResult(
                        $"PerformConversationExtraction:{llmServiceResult.Code}",
                        llmServiceResult.Message
                    );
                }
                var llmService = llmServiceResult.Data!;

                // --- 1. Generate the field definitions for the prompt ---
                string fieldDefinitionsJson = GenerateFieldDefinitionsForPrompt(postAnalysisData.Extraction.Fields);

                // --- 2. Construct the highly detailed system prompt ---
                var systemPrompt = $@"You are a highly precise AI data extraction engine. Your task is to analyze a conversation and extract specific pieces of information into a structured JSON format based on a provided schema.

Here is the schema of fields you must extract. Adhere to it STRICTLY:
<extraction_schema>
{fieldDefinitionsJson}
</extraction_schema>

Follow these critical instructions for extraction:
1.  **Analyze the Conversation**: Read the entire conversation context to find the values for each field in the schema.
2.  **Data Types**: You MUST respect the `dataType` for each field:
    - `String`: Extract the text as a string.
    - `Boolean`: The value must be either `true` or `false`.
    - `Number`: The value must be a valid integer or decimal number (e.g., 123, 45.6).
    - `DateTime`: The value must be a string in ISO 8601 format: `YYYY-MM-DDTHH:MM:SSZ`.
    - `Enum`: The value MUST be one of the exact strings provided in the `options` array.
3.  **Rules**:
    - `isRequired: true`: You MUST provide a value for this field. If the information is not explicitly present, make a logical inference based on the context.
    - `isEmptyOrNullAllowed: false`: You must provide a value. If `true`, you may use `null` as the `fieldValue` if the information is not found.
4.  **Conditional Extraction**: This is the most important rule. Some fields have `conditionalRules`.
    - After you extract a value for a parent field, check if that value matches the `condition` of any of its `conditionalRules`.
    - If a condition is met (e.g., for a boolean field, the value is `true` and the condition `operator` is `Equals` and `value` is `""true""`), you MUST then proceed to extract all the `fieldsToExtract` listed under that specific rule. This process applies recursively.
5.  **IDs**: ONLY use the `fieldId` provided in the schema. Do not invent your own.

You MUST respond with a JSON object enclosed in a ```json code block.
The root object must have one key: `""ExtractedFields""`, an array of the top-level fields you extracted.

Each object in the array must follow this structure:
- `""Thinking""`: A brief justification for the extracted value.
- `""FieldId""`: The exact ID of the field from the schema.
- `""FieldValue""`: The extracted value, formatted according to its `dataType`. Use `null` if allowed and not found.
- `""ConditionalExtractedFields""`: An array of conditionally extracted fields, following this same structure. If no conditional rules were met, provide an empty array `[]`.

Example Response Format:
```json
{{
  ""extractedFields"": [
    {{
      ""Thinking"": ""The customer confirmed they were the account holder."",
      ""FieldId"": ""is-account-holder-id"",
      ""FieldValue"": true,
      ""ConditionalExtractedFields"": [
        {{
          ""Thinking"": ""Since they are the account holder, I need to extract their name which was mentioned at the start."",
          ""FieldId"": ""account-holder-name-id"",
          ""FieldValue"": ""John Doe"",
          ""ConditionalExtractedFields"": []
        }}
      ]
    }},
    {{
       ""Thinking"": ""The customer mentioned the reason for their call was about billing."",
       ""FieldId"": ""call-reason-id"",
       ""FieldValue"": ""Billing Inquiry"",
       ""ConditionalExtractedFields"": []
    }}
  ]
}}
```";

                llmService.SetSystemPrompt(systemPrompt);
                llmService.SetMaxTokens(10000);
                llmService.AddUserMessage($"{context}\n\nPerform the extraction based on the schema and rules, providing the output in the specified JSON format.");

                // --- 3. Full streaming & processing logic ---
                StringBuilder responseBuilder = new StringBuilder();
                bool isStreamingEnded = false;
                bool hasStreamingFailed = false;
                string? streamingFailedMessage = null;

                llmService.MessageStreamed += (sender, args) =>
                {
                    var chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(args.ResponseObject, llmService.GetProviderType());
                    if (!chunkExtractResult.Success || !chunkExtractResult.Data.HasValue)
                    {
                        _logger.LogWarning("Could not extract chunk data during extraction for session {SessionId}.", sessionId);
                        return;
                    }
                    (string? deltaText, bool isEndOfResponse) = chunkExtractResult.Data.Value;

                    if (!string.IsNullOrEmpty(deltaText))
                    {
                        responseBuilder.Append(deltaText);
                    }

                    if (isEndOfResponse)
                    {
                        isStreamingEnded = true;
                    }
                };

                llmService.MessageStreamedCancelled += (sender, args) =>
                {
                    if (hasStreamingFailed) return;
                    hasStreamingFailed = true;
                    streamingFailedMessage = $"Extraction LLM Failed: [{Enum.GetName(args.Type)} {args.ResponseCode}] {args.ResponseMessage}";
                    _logger.LogError("Post Analysis Extraction LLM service for session {SessionId} has failed: {FailureMessage}", sessionId, streamingFailedMessage);
                };

                var cancellationTokenSource = new CancellationTokenSource();
                await llmService.ProcessInputAsync(cancellationTokenSource.Token);

                var stopWatch = Stopwatch.StartNew();
                while (!isStreamingEnded && !hasStreamingFailed)
                {
                    if (stopWatch.ElapsedMilliseconds > 60000) // 60 seconds timeout
                    {
                        hasStreamingFailed = true;
                        streamingFailedMessage = "Extraction LLM Timed Out after 60 seconds.";
                        cancellationTokenSource.Cancel();
                        break;
                    }
                    await Task.Delay(100);
                }
                stopWatch.Stop();

                try
                {
                    llmService.ClearMessages();
                    llmService.ClearMessageStreamed();
                    llmService.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing Post Analysis Extraction LLM service for session {SessionId}", sessionId);
                }

                if (hasStreamingFailed)
                {
                    return result.SetFailureResult(
                        "PerformConversationExtraction:LLM_STREAM_FAILED",
                        streamingFailedMessage!
                    );
                }

                string rawLlmResponse = responseBuilder.ToString();

                // --- 4. Extract, Parse, and Return ---
                string? jsonBlock = ExtractJsonBlock(rawLlmResponse);
                if (string.IsNullOrWhiteSpace(jsonBlock))
                {
                    return result.SetFailureResult(
                        "PerformConversationExtraction:JSON_EXTRACTION_FAILED",
                        $"Could not extract a valid JSON block from the LLM response for extraction. Raw: {rawLlmResponse}"
                    );
                }

                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new ObjectToPrimitiveConverter() }
                    };

                    var extractionResponse = JsonSerializer.Deserialize<ConversationExtractionLLMResponse>(jsonBlock, options);
                    if (extractionResponse == null)
                    {
                        return result.SetFailureResult(
                            "PerformConversationExtraction:JSON_DESERIALIZATION_FAILED",
                            "Deserialized extraction response object is null."
                        );
                    }
                    return result.SetSuccessResult(extractionResponse.ExtractedFields);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse JSON for extraction on session {SessionId}. JSON Block: {JsonBlock}", sessionId, jsonBlock);
                    return result.SetFailureResult(
                        "PerformConversationExtraction:JSON_PARSE_EXCEPTION",
                        $"Failed to parse JSON from LLM for extraction: {ex.Message}"
                    );
                }
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "PerformConversationExtraction:EXCEPTION",
                    $"Error performing conversation extraction for session {sessionId}: {ex.Message}"
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
        private string? ExtractJsonBlock(string rawResponse)
        {
            try
            {
                const string codeBlockDelimiter = "```";
                string searchScope = rawResponse;

                int startDelimiterIndex = rawResponse.IndexOf(codeBlockDelimiter);
                if (startDelimiterIndex != -1)
                {
                    int endDelimiterIndex = rawResponse.IndexOf(codeBlockDelimiter, startDelimiterIndex + codeBlockDelimiter.Length);
                    if (endDelimiterIndex != -1)
                    {
                        int contentStartIndex = startDelimiterIndex + codeBlockDelimiter.Length;

                        int actualContentStart = contentStartIndex;
                        while (actualContentStart < endDelimiterIndex && (char.IsLetter(rawResponse[actualContentStart]) || char.IsWhiteSpace(rawResponse[actualContentStart])))
                        {
                            actualContentStart++;
                        }

                        searchScope = rawResponse.Substring(actualContentStart, endDelimiterIndex - actualContentStart);
                    }
                }

                int startIndex = searchScope.IndexOf('{');
                if (startIndex == -1)
                {
                    return null;
                }

                int endIndex = searchScope.LastIndexOf('}');
                if (endIndex == -1 || endIndex < startIndex)
                {
                    return null;
                }

                return searchScope.Substring(startIndex, endIndex - startIndex + 1).Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while extracting JSON block from raw LLM response.");
                return null;
            }
        }
        private string GenerateTagDefinitionsForPrompt(List<BusinessAppPostAnalysisTagDefinition> tags)
        {
            var simplifiedTags = tags.Select(tag => ToSimplifiedTag(tag)).ToList();
            return JsonSerializer.Serialize(simplifiedTags, new JsonSerializerOptions { WriteIndented = true });
        }
        private object ToSimplifiedTag(BusinessAppPostAnalysisTagDefinition tag)
        {
            return new
            {
                id = tag.Id,
                name = tag.Name,
                description = tag.Description,
                rules = tag.Rules,
                subTags = tag.SubTags.Select(subTag => ToSimplifiedTag(subTag)).ToList()
            };
        }
        private string GenerateFieldDefinitionsForPrompt(List<BusinessAppPostAnalysisExtractionField> fields)
        {
            var simplifiedFields = fields.Select(field => ToSimplifiedField(field)).ToList();
            return JsonSerializer.Serialize(simplifiedFields, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        }
        private object ToSimplifiedField(BusinessAppPostAnalysisExtractionField field)
        {
            return new
            {
                fieldId = field.Id,
                keyName = field.KeyName,
                description = field.Description,
                dataType = field.DataType.ToString(),
                isRequired = field.IsRequired,
                isEmptyOrNullAllowed = field.IsEmptyOrNullAllowed,
                options = field.DataType == BusinessAppPostAnalysisExtractionFieldDataType.Enum ? field.Options : null,
                validation = field.Validation,
                conditionalRules = field.ConditionalRules.Any() ? field.ConditionalRules.Select(rule => new
                {
                    condition = rule.Condition,
                    fieldsToExtract = rule.FieldsToExtract.Select(subField => ToSimplifiedField(subField)).ToList()
                }).ToList() : null
            };
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

    // LLM Response Models
    internal class ConversationTaggingLLMResponse
    {
        [JsonPropertyName("appliedTags")]
        public List<ConversationPostAnalsysisTaggingResultData> AppliedTags { get; set; } = new List<ConversationPostAnalsysisTaggingResultData>();
    }
    internal class ConversationExtractionLLMResponse
    {
        [JsonPropertyName("extractedFields")]
        public List<ConversationPostAnalsysisExtractionFieldResultData> ExtractedFields { get; set; } = new List<ConversationPostAnalsysisExtractionFieldResultData>();
    }
}
