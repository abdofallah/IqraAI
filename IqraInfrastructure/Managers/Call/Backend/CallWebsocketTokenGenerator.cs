using System.Security.Cryptography;
using System.Text;

namespace IqraInfrastructure.Managers.Call.Backend
{
    public static class CallWebsocketTokenGenerator
    {
        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=') // Remove padding
                .Replace('+', '-') // 62nd char of encoding
                .Replace('/', '_'); // 63rd char of encoding
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break; // One pad char
                default: throw new ArgumentException("Illegal base64url string!", nameof(input));
            }
            return Convert.FromBase64String(output);
        }

        public static string GenerateHmacToken(
            string sessionId,
            string clientId,
            TimeSpan validityPeriod,
            string privateKey // Your HMAC secret key
        )
        {
            if (string.IsNullOrEmpty(sessionId)) throw new ArgumentNullException(nameof(sessionId));
            if (string.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrEmpty(privateKey)) throw new ArgumentNullException(nameof(privateKey));

            var expirationTimeUnix = DateTimeOffset.UtcNow.Add(validityPeriod).ToUnixTimeSeconds();

            // Payload can be simple or more complex JSON
            // For simplicity here, we'll combine fixed fields.
            // A JSON payload is more extensible: var payloadObject = new { sid = sessionId, cid = clientId, exp = expirationTimeUnix };
            // string payloadJson = JsonSerializer.Serialize(payloadObject);
            // string payloadToSign = payloadJson;

            // Simpler string payload for this example:
            string payloadToSign = $"{sessionId}:{clientId}:{expirationTimeUnix}";
            string base64UrlEncodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadToSign));

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(privateKey)))
            {
                byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(base64UrlEncodedPayload)); // Sign the encoded payload
                string base64UrlEncodedSignature = Base64UrlEncode(signatureBytes);

                return $"{base64UrlEncodedPayload}.{base64UrlEncodedSignature}";
            }
        }

        public static bool ValidateHmacToken(
            string token,
            string expectedSessionId,
            string expectedClientId,
            string privateKey,
            out string? validationError
        )
        {
            validationError = null;
            if (string.IsNullOrEmpty(token))
            {
                validationError = "Token is null or empty.";
                return false;
            }
            if (string.IsNullOrEmpty(privateKey))
            {
                validationError = "Private key is null or empty.";
                return false; // Should throw ideally in a real app if key is missing
            }

            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                validationError = "Token format is invalid.";
                return false;
            }

            string base64UrlEncodedPayload = parts[0];
            string base64UrlEncodedSignatureFromToken = parts[1];

            // Verify signature
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(privateKey)))
            {
                byte[] expectedSignatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(base64UrlEncodedPayload));
                string base64UrlEncodedExpectedSignature = Base64UrlEncode(expectedSignatureBytes);

                if (!TimingSafeEquals(base64UrlEncodedSignatureFromToken, base64UrlEncodedExpectedSignature))
                {
                    validationError = "Token signature is invalid.";
                    return false;
                }
            }

            // Decode and validate payload
            string payloadToSign;
            try
            {
                payloadToSign = Encoding.UTF8.GetString(Base64UrlDecode(base64UrlEncodedPayload));
            }
            catch (Exception ex)
            {
                validationError = $"Failed to decode token payload: {ex.Message}";
                return false;
            }

            var payloadParts = payloadToSign.Split(':');
            if (payloadParts.Length != 3)
            {
                validationError = "Decoded payload format is invalid.";
                return false;
            }

            string actualSessionId = payloadParts[0];
            string actualClientId = payloadParts[1];

            if (!long.TryParse(payloadParts[2], out long expirationTimeUnix))
            {
                validationError = "Invalid expiration time in token payload.";
                return false;
            }

            // Validate expiration
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expirationTimeUnix)
            {
                validationError = "Token has expired.";
                return false;
            }

            // Validate content (sessionId, clientId)
            if (actualSessionId != expectedSessionId)
            {
                validationError = "Session ID mismatch.";
                return false;
            }

            if (actualClientId != expectedClientId)
            {
                validationError = "Client ID mismatch.";
                return false;
            }

            return true;
        }

        // Constant-time string comparison to protect against timing attacks.
        private static bool TimingSafeEquals(string a, string b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }

            int diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
