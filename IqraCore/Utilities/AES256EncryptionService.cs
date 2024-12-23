using System.Security.Cryptography;
using System.Text;

namespace IqraCore.Utilities
{
    public class AES256EncryptionService
    {
        private readonly string _encryptionKey;
        private readonly byte[] _derivedKey;
        private const int KeySize = 32; // 256 bits
        private const int IvSize = 16;  // 128 bits
        private const int SaltSize = 16; // 128 bits

        private static readonly byte[] Salt = new byte[] {
            0x3F, 0x12, 0x67, 0x89, 0xA4, 0xB5, 0xC6, 0xD7,
            0xE8, 0xF9, 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB
        };

        public AES256EncryptionService(string encryptionKey)
        {
            _encryptionKey = encryptionKey;

            using var pbkdf2 = new Rfc2898DeriveBytes(
                _encryptionKey,
                Salt,
                10000,
                HashAlgorithmName.SHA256);

            _derivedKey = pbkdf2.GetBytes(KeySize);
        }

        public string Encrypt(string value)
        {
            try
            {
                byte[] iv = new byte[IvSize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }

                using var aes = Aes.Create();
                aes.Key = _derivedKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                byte[] valueBytes = Encoding.UTF8.GetBytes(value);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(valueBytes, 0, valueBytes.Length);

                byte[] combined = new byte[iv.Length + encryptedBytes.Length];
                Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
                Buffer.BlockCopy(encryptedBytes, 0, combined, iv.Length, encryptedBytes.Length);

                return Convert.ToBase64String(combined);
            }
            catch (Exception ex)
            {
                throw new Exception("Error encrypting value", ex);
            }
        }

        public string Decrypt(string encryptedValue)
        {
            try
            {
                byte[] combined = Convert.FromBase64String(encryptedValue);
                if (combined.Length < IvSize)
                {
                    throw new ArgumentException("Invalid encrypted value");
                }

                byte[] iv = new byte[IvSize];
                byte[] encryptedBytes = new byte[combined.Length - IvSize];
                Buffer.BlockCopy(combined, 0, iv, 0, IvSize);
                Buffer.BlockCopy(combined, IvSize, encryptedBytes, 0, encryptedBytes.Length);

                using var aes = Aes.Create();
                aes.Key = _derivedKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                throw new Exception("Error decrypting value", ex);
            }
        }
    }
}
