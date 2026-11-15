using IqraCore.Entities.FlowApp;
using System.Net;
using System.Text.Json;

namespace IqraInfrastructure.Helpers.Integrations
{
    public static class ProviderReplyHelper
    {
        public const int LoggedReplyMaxLength = 4000;
        private const int MaxDepth = 5;
        private static readonly string[] ReasonKeys = { "error", "code", "title", "message", "error_description", "description", "detail", "error_info" };

        public static string Describe(HttpStatusCode statusCode, string? content)
        {
            var reason = ExtractReason(content);
            return string.IsNullOrEmpty(reason)
                ? $"{(int)statusCode} {statusCode}"
                : $"{(int)statusCode} {statusCode} - {reason}";
        }

        public static ActionExecutionResult ActionFailure(string label, HttpStatusCode statusCode, string? content)
        {
            var result = ActionExecutionResult.Failure("API_ERROR", $"{label}: {Describe(statusCode, content)}");
            result.ProviderReply = CapForLog(content);
            return result;
        }

        public static string CapForLog(string? content)
        {
            return Cap(content ?? string.Empty, LoggedReplyMaxLength);
        }

        public static string Cap(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 1) + "…";
        }

        public static string? ExtractReason(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var trimmed = content.Trim();
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                var reasons = new List<string>();
                CollectReasons(document.RootElement, reasons, 0);
                return reasons.Count == 0 ? null : string.Join(" - ", reasons);
            }
            catch (JsonException)
            {
                return trimmed.Contains('<') ? null : trimmed;
            }
        }

        private static void CollectReasons(JsonElement element, List<string> reasons, int depth)
        {
            if (depth > MaxDepth)
            {
                return;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    CollectReasons(item, reasons, depth + 1);
                }
                return;
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(value) && ReasonKeys.Contains(property.Name, StringComparer.OrdinalIgnoreCase) && !reasons.Contains(value))
                    {
                        reasons.Add(value);
                    }
                }
                else if (property.Value.ValueKind == JsonValueKind.Object || property.Value.ValueKind == JsonValueKind.Array)
                {
                    CollectReasons(property.Value, reasons, depth + 1);
                }
            }
        }
    }
}
