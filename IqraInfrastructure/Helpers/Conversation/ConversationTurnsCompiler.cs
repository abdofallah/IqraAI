using IqraCore.Entities.Conversation.Turn;

namespace IqraInfrastructure.Helpers.Conversation
{
    public static class ConversationTurnsCompiler
    {
        public static string SimplifyConversationTurns(List<ConversationTurn> turns)
        {
            var stringResult = "";

            foreach (var turn in turns)
            {
                try
                {
                    if (turn.Type == ConversationTurnType.System)
                    {
                        stringResult += $"[{turn.CreatedAt.ToString("G")}] SYSTEM ({turn.SystemInput!.Type})\n\n";
                    }
                    else if (turn.Type == ConversationTurnType.User)
                    {
                        stringResult += $"[{turn.UserInput!.StartedSpeakingAt.ToString("G")}] USER: {turn.UserInput.TranscribedText}\n\n";
                    }
                }
                catch (Exception ex)
                {
                    stringResult += $"-- FAILED TO PARSE TURN TO SIMPLIFICATION FOR TURN ID ({turn.Id}) --\n\n";
                }

                try
                {
                    if (turn.Response.Type == ConversationTurnAgentResponseType.Speech)
                    {
                        stringResult += $"[{(turn.Response.LLMStreamingStartedAt ?? turn.Response.SpokenSegments[0].StartedPlayingAt).ToString("G")}] ASSISTANT: {string.Join(" ", turn.Response.SpokenSegments.Select(d => $"{d.Text}"))}\n\n";
                    }
                    else if (turn.Response.Type == ConversationTurnAgentResponseType.CustomTool || turn.Response.Type == ConversationTurnAgentResponseType.SystemTool)
                    {
                        stringResult += $"[{(turn.Response.LLMStreamingStartedAt ?? turn.Response.LLMStreamingCompletedAt ?? turn.Response.LLMProcessStartedAt)?.ToString("G")}] ASSISTANT: {turn.Response.ToolExecution!.RawLLMInput}\n\n";

                        if (!string.IsNullOrWhiteSpace(turn.Response.ToolExecution!.Result))
                        {
                            stringResult += $"[{(turn.Response.ToolExecution.CompletedAt)?.ToString("G")}] SYSTEM: {turn.Response.ToolExecution!.Result}\n\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    stringResult += $"-- FAILED TO PARSE TURN TO SIMPLIFICATION FOR TURN ID ({turn.Id}) --\n\n";
                }
            }

            return stringResult.TrimEnd();
        }
    }
}
