using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Models
{
    // --- Shared ---

    public class CalComResponse<T>
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("pagination")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationData? Pagination { get; set; }
    }

    public class PaginationData
    {
        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("itemsPerPage")]
        public int ItemsPerPage { get; set; }

        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }
    }

    // --- Shared Data Entities ---

    public class Attendee
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; } = "UTC";

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }
    }

    public class Guest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; } = "UTC";
    }

    // --- Action: Book Meeting ---

    public class CreateBookingRequest
    {
        [JsonPropertyName("start")]
        public string Start { get; set; } = string.Empty; // ISO 8601

        [JsonPropertyName("attendee")]
        public Attendee Attendee { get; set; } = new();

        [JsonPropertyName("eventTypeId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? EventTypeId { get; set; }

        [JsonPropertyName("eventTypeSlug")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EventTypeSlug { get; set; }

        [JsonPropertyName("username")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Username { get; set; }

        [JsonPropertyName("teamSlug")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TeamSlug { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class BookingResponseData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("start")]
        public string Start { get; set; } = string.Empty;

        [JsonPropertyName("end")]
        public string End { get; set; } = string.Empty;

        [JsonPropertyName("cancellationReason")]
        public string? CancellationReason { get; set; }

        [JsonPropertyName("reschedulingReason")]
        public string? ReschedulingReason { get; set; }

        [JsonPropertyName("attendees")]
        public List<Attendee> Attendees { get; set; } = new();
    }

    // --- Action: Get Slots ---

    public class Slot
    {
        [JsonPropertyName("start")]
        public string Start { get; set; } = string.Empty;

        [JsonPropertyName("end")]
        public string? End { get; set; }
    }

    // --- Action: Cancel Booking ---

    public class CancelBookingRequest
    {
        [JsonPropertyName("cancellationReason")]
        public string Reason { get; set; } = "User requested cancellation via AI Agent";
    }

    // --- Action: Reschedule Booking ---

    public class RescheduleBookingRequest
    {
        [JsonPropertyName("start")]
        public string Start { get; set; } = string.Empty;

        [JsonPropertyName("reschedulingReason")]
        public string Reason { get; set; } = "User requested reschedule via AI Agent";
    }

    // --- Action: Mark Absent ---

    public class MarkAbsentRequest
    {
        [JsonPropertyName("host")]
        public bool IsHost { get; set; }

        [JsonPropertyName("attendees")]
        public List<AbsentAttendeePayload> Attendees { get; set; } = new();
    }

    public class AbsentAttendeePayload
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("absent")]
        public bool Absent { get; set; }
    }

    // --- Action: Add Guests ---

    public class AddGuestsRequest
    {
        [JsonPropertyName("guests")]
        public List<Guest> Guests { get; set; } = new();
    }

    // --- Fetchers ---
    public class EventTypeDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("length")]
        public int Length { get; set; }

        [JsonPropertyName("owner")]
        public EventTypeOwner? Owner { get; set; }

        [JsonPropertyName("team")]
        public EventTypeTeam? Team { get; set; }
    }

    public class EventTypeOwner
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
    }

    public class EventTypeTeam
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}