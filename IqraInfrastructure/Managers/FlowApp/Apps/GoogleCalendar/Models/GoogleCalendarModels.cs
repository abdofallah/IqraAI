using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.FlowApp.Apps.GoogleCalendar.Models
{
    public class EventDateTime
    {
        [JsonPropertyName("dateTime")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DateTime { get; set; }

        [JsonPropertyName("timeZone")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TimeZone { get; set; }
    }

    public class EventAttendee
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    public class CreateEventRequest
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        [JsonPropertyName("location")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Location { get; set; }

        [JsonPropertyName("start")]
        public EventDateTime Start { get; set; } = new();

        [JsonPropertyName("end")]
        public EventDateTime End { get; set; } = new();

        [JsonPropertyName("attendees")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<EventAttendee>? Attendees { get; set; }
    }

    public class CalendarEvent
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("htmlLink")]
        public string? HtmlLink { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("start")]
        public EventDateTime? Start { get; set; }

        [JsonPropertyName("end")]
        public EventDateTime? End { get; set; }
    }

    public class EventListResponse
    {
        [JsonPropertyName("items")]
        public List<CalendarEvent> Items { get; set; } = new();
    }

    public class CalendarListEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }
    }

    public class CalendarListResponse
    {
        [JsonPropertyName("items")]
        public List<CalendarListEntry> Items { get; set; } = new();
    }
}
