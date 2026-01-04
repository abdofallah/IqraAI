using System.Text.Json.Serialization;

namespace IqraCore.Entities.FlowApp
{
    public class DynamicOption
    {
        /// <summary>
        /// The text displayed to the user in the dropdown.
        /// </summary>
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// The actual value to be stored/sent (String, Int, or Boolean).
        /// </summary>
        [JsonPropertyName("value")]
        public object Value { get; set; } = default!;

        /// <summary>
        /// Optional helper text displayed below the label (e.g., slug or email).
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Optional icon URL to display next to the option.
        /// </summary>
        [JsonPropertyName("iconUrl")]
        public string? IconUrl { get; set; }
    }
}