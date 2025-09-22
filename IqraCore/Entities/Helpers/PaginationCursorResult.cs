using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraCore.Entities.Helpers
{
    public class PaginationCursor<T>
    {
        // TODO LOAD FROM CONFIG APPSETTINGS.JSON
        private static readonly string SecretKey = "YOUR_SUPER_SECRET_KEY_FROM_CONFIG_256_BITS_LONG";

        public DateTime Timestamp { get; set; }
        public string Id { get; set; }

        public T Filter { get; set; }

        [JsonIgnore] // Don't include the signature in the data to be signed
        public string? Signature { get; set; }

        public string Encode()
        {
            var payloadJson = JsonSerializer.Serialize(this, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            // Create the signature
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
            var signatureBytes = hmac.ComputeHash(payloadBytes);
            var signature = Convert.ToBase64String(signatureBytes);

            // Combine payload and signature
            var combinedString = $"{Convert.ToBase64String(payloadBytes)}.{signature}";

            // Final Base64 encoding for the whole token
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(combinedString));
        }

        public static PaginationCursor<T>? Decode(string? encodedCursor)
        {
            if (string.IsNullOrWhiteSpace(encodedCursor))
            {
                return null;
            }

            try
            {
                var combinedBytes = Convert.FromBase64String(encodedCursor);
                var combinedString = Encoding.UTF8.GetString(combinedBytes);

                var parts = combinedString.Split('.');
                if (parts.Length != 2) return null; // Invalid format

                var payloadBase64 = parts[0];
                var signature = parts[1];

                var payloadBytes = Convert.FromBase64String(payloadBase64);

                // Verify the signature
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
                var expectedSignatureBytes = hmac.ComputeHash(payloadBytes);
                var expectedSignature = Convert.ToBase64String(expectedSignatureBytes);

                if (signature != expectedSignature)
                {
                    // TAMPERING DETECTED! Or the secret key changed.
                    // Log this as a security warning.
                    return null;
                }

                var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                return JsonSerializer.Deserialize<PaginationCursor<T>>(payloadJson);
            }
            catch // Handle potential decoding/deserialization errors
            {
                // Log warning or error if desired
                return null;
            }
        }
    }

    public sealed class PaginationCursorNoFilterHelper;

    // Generic pagination result
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public string? NextCursor { get; set; }
        public string? PreviousCursor { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
    }
}
