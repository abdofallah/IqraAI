using System.Text;
using System.Text.Json;

namespace IqraCore.Entities.Helpers
{
    public class PaginationCursor
    {
        public DateTime Timestamp { get; set; }
        public string Id { get; set; }

        public string Encode()
        {
            var json = JsonSerializer.Serialize(this);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static PaginationCursor? Decode(string? encodedCursor)
        {
            if (string.IsNullOrWhiteSpace(encodedCursor))
            {
                return null;
            }

            try
            {
                var jsonBytes = Convert.FromBase64String(encodedCursor);
                var json = Encoding.UTF8.GetString(jsonBytes);
                return JsonSerializer.Deserialize<PaginationCursor>(json);
            }
            catch // Handle potential decoding/deserialization errors
            {
                // Log warning or error if desired
                return null;
            }
        }
    }

    // Generic pagination result
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public string? NextCursor { get; set; }
        public string? PreviousCursor { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
