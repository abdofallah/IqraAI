using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IqraCore.Utilities
{
    public static class SubdomainHashGenerator
    {
        public static string GenerateSubdomainHash(long businessId)
        {
            // Combine businessId and ticks
            string combined = $"{businessId}-{DateTime.UtcNow.Ticks}";

            // Create SHA256 hash
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(combined));

                // Convert to hexadecimal string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                // Take the first 16 characters to keep it shorter
                return builder.ToString().Substring(0, 16);
            }
        }
    }
}
