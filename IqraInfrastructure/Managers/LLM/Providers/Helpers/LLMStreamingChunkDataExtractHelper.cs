
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;

namespace IqraInfrastructure.Managers.LLM.Providers.Helpers
{
    public static class LLMStreamingChunkDataExtractHelper
    {
        public static FunctionReturnResult<(string? deltaText, bool isEndOfRespones)?> GetChunkData(object? responseObject, InterfaceLLMProviderEnum providerType)
        {
            var result = new FunctionReturnResult<(string? deltaText, bool isEndOfRespones)?>();

            string? deltaText = null;
            bool isEndOfResponse = false;

            if (providerType == InterfaceLLMProviderEnum.AnthropicClaude)
            {
                var response = (Anthropic.SDK.Messaging.MessageResponse)responseObject;
                if (response.Delta != null)
                {
                    deltaText = response.Delta.Text;

                    if (
                        response.Delta.StopReason != null
                        //&& (response.Delta.StopReason == Anthropic.SDK.Messaging.MessageStopReason.Length || response.Delta.StopReason == Anthropic.SDK.Messaging.MessageStopReason.Stop)
                    )
                    {
                        isEndOfResponse = true;
                    }
                }
            }
            else if (providerType == InterfaceLLMProviderEnum.OpenAIGPT || providerType == InterfaceLLMProviderEnum.AzureOpenAI)
            {
                var response = (OpenAI.Chat.StreamingChatCompletionUpdate)responseObject;
                var messageContentPart = (OpenAI.Chat.ChatMessageContentPart)response.ContentUpdate.FirstOrDefault();

                deltaText = messageContentPart?.Text ?? "";

                if (
                    response.FinishReason != null 
                    //&& (response.FinishReason == OpenAI.Chat.ChatFinishReason.Stop || response.FinishReason == OpenAI.Chat.ChatFinishReason.Length)
                )
                {
                    isEndOfResponse = true;
                }
            }
            else if (providerType == InterfaceLLMProviderEnum.GoogleAIGemini)
            {
                var response = (GenerativeAI.Types.GenerateContentResponse)responseObject;

                var candidate = response.Candidates.FirstOrDefault(); // Usually only one candidate
                if (candidate?.Content?.Parts?.FirstOrDefault() != null)
                {
                    deltaText = candidate.Content.Parts.First().Text;
                }

                if (
                    candidate?.FinishReason != null
                    //&& candidate.FinishReason != (GenerativeAI.Types.FinishReason.FINISH_REASON_UNSPECIFIED)
                )
                {
                    isEndOfResponse = true;
                }
            }
            else if (providerType == InterfaceLLMProviderEnum.GroqCloud)
            {
                var groqChunk = (IqraCore.Entities.LLM.Providers.GroqCloud.GroqCloudStreamChunk)responseObject;

                var choice = groqChunk.Choices?.FirstOrDefault();
                deltaText = choice?.Delta?.Content;

                if (
                    choice?.FinishReason != null
                    // && todo finish reason
                    )
                {
                    isEndOfResponse = true;
                }
            }
            else if (providerType == InterfaceLLMProviderEnum.AzureAIInference)
            {
                var response = (Azure.AI.Inference.StreamingChatCompletionsUpdate)responseObject;

                deltaText = response.ContentUpdate;

                if (
                    response.FinishReason != null
                    //&& (response.FinishReason == Azure.AI.Inference.CompletionsFinishReason.Stopped || response.FinishReason == Azure.AI.Inference.CompletionsFinishReason.TokenLimitReached)
                )
                {
                    isEndOfResponse = true;
                }
            }
            else
            {
                result.Code = "GetChunkData:1";
                result.Message = "LLM provider type {providerType} not implemented in GetChunkData";
                return result;
            }

            result.Success = true;
            result.Data = (deltaText, isEndOfResponse);

            return result;
        }
    }
}
