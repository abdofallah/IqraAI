using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using System.Data.HashFunction.xxHash;

namespace IqraCore.Utilities
{
    public static class ImageHelper
    {
        private static Dictionary<string, List<byte[]>> BusinessLogoSignatures = new Dictionary<string, List<byte[]>>
        {
            { "png", new List<byte[]> {
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            }},
            { "jpeg", new List<byte[]> {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }
            }},
            { "gif", new List<byte[]> {
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 },
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }
            }},
            { "webp", new List<byte[]> {
                new byte[] { 0x52, 0x49, 0x46, 0x46 }
            }}
        };

        private static Dictionary<string, List<byte[]>> BusinessWhiteLabelFaviconSignatures = new Dictionary<string, List<byte[]>>
        {
            { "png", new List<byte[]> {
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            }},
            { "jpeg", new List<byte[]> {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }
            }},
            { "gif", new List<byte[]> {
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 },
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }
            }},
            { "webp", new List<byte[]> {
                new byte[] { 0x52, 0x49, 0x46, 0x46 }
            }},
            { "ico", new List<byte[]> {
                new byte[] { 0x00, 0x00, 0x01, 0x00 },
            }}
        };

        private static Dictionary<string, List<byte[]>> IntegrationLogoSignatures = new Dictionary<string, List<byte[]>>
        {
            { "png", new List<byte[]> {
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            }},
            { "jpeg", new List<byte[]> {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }
            }},
            { "gif", new List<byte[]> {
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 },
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }
            }},
            { "webp", new List<byte[]> {
                new byte[] { 0x52, 0x49, 0x46, 0x46 }
            }}
        };

        private static IxxHash _xxHash = xxHashFactory.Instance.Create(new xxHashConfig { HashSizeInBits = 64 });

        public static int ValidateBusinessLogoFile(IFormFile file)
        {
            if (file.Length > 5 * 1024 * 1024)
            {
                return 0;
            }

            using (var reader = new BinaryReader(file.OpenReadStream()))
            {
                var maxSignatureLength = BusinessLogoSignatures.Values.SelectMany(x => x).Max(x => x.Length);
                var headerBytes = reader.ReadBytes(maxSignatureLength);

                bool result = BusinessLogoSignatures.Values.SelectMany(x => x)
                                 .Any(signature =>
                                     headerBytes.Take(signature.Length)
                                                .SequenceEqual(signature));

                return (result == true ? 200 : 1);
            }
        }

        public static int ValidateIntegrationLogoFile(IFormFile file)
        {
            if (file.Length > 5 * 1024 * 1024)
            {
                return 0;
            }

            using (var reader = new BinaryReader(file.OpenReadStream()))
            {
                var maxSignatureLength = IntegrationLogoSignatures.Values.SelectMany(x => x).Max(x => x.Length);
                var headerBytes = reader.ReadBytes(maxSignatureLength);

                bool result = IntegrationLogoSignatures.Values.SelectMany(x => x)
                                 .Any(signature =>
                                     headerBytes.Take(signature.Length)
                                                .SequenceEqual(signature));

                return (result == true ? 200 : 1);
            }
        }

        public static int ValidateBusinessWhiteLabelLogoFile(IFormFile file)
        {
            if (file.Length > 5 * 1024 * 1024)
            {
                return 0;
            }

            using (var reader = new BinaryReader(file.OpenReadStream()))
            {
                var maxSignatureLength = BusinessLogoSignatures.Values.SelectMany(x => x).Max(x => x.Length);
                var headerBytes = reader.ReadBytes(maxSignatureLength);

                bool result = BusinessLogoSignatures.Values.SelectMany(x => x)
                                 .Any(signature =>
                                     headerBytes.Take(signature.Length)
                                                .SequenceEqual(signature));

                return (result == true ? 200 : 1);
            }
        }

        public static int ValidateBusinessWhiteLabelFaviconFile(IFormFile file)
        {
            if (file.Length > 1 * 1024 * 1024)
            {
                return 0;
            }

            using (var reader = new BinaryReader(file.OpenReadStream()))
            {
                var maxSignatureLength = BusinessWhiteLabelFaviconSignatures.Values.SelectMany(x => x).Max(x => x.Length);
                var headerBytes = reader.ReadBytes(maxSignatureLength);

                bool result = BusinessWhiteLabelFaviconSignatures.Values.SelectMany(x => x)
                                 .Any(signature =>
                                     headerBytes.Take(signature.Length)
                                                .SequenceEqual(signature));

                return (result == true ? 200 : 1);
            }
        }

        public async static Task<(byte[] ImageData, string Hash)> ConvertScaleAndHashToWebp(IFormFile file)
        {
            using var image = await Image.LoadAsync(file.OpenReadStream());

            using var ms = new MemoryStream();
            await image.SaveAsWebpAsync(ms);
            byte[] imageData = ms.ToArray();

            // Calculate xxHash64
            var hashValue = _xxHash.ComputeHash(imageData);
            string hash = BitConverter.ToString(hashValue.Hash).Replace("-", "").ToLowerInvariant();

            return (imageData, hash);
        }

        
    }
}
