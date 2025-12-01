using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System.Net;

namespace IqraInfrastructure.Repositories.S3Storage
{
    public static class S3StorageHelpers
    {
        /// <summary>
        /// Resolves the correct S3 Client based on the provided region or defaults to the current region.
        /// </summary>
        public static IAmazonS3 GetS3Client(S3StorageClientFactory factory, string? region)
        {
            var client = string.IsNullOrEmpty(region)
                ? factory.GetClientForCurrentRegion()
                : factory.GetClientForRegion(region);

            if (client == null)
            {
                throw new InvalidOperationException($"S3 Client not found for region: {region ?? "Current"}");
            }

            return client;
        }

        /// <summary>
        /// Ensures the specified bucket exists. If not, it attempts to create it.
        /// </summary>
        public static async Task EnsureBucketExistsAsync(IAmazonS3 client, string bucketName, ILogger logger)
        {
            try
            {
                var listBucketsResponse = await client.ListBucketsAsync();
                if (listBucketsResponse.Buckets.Any(b => b.BucketName == bucketName))
                {
                    return;
                }

                await client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // Bucket already exists/owned by you. Safe to ignore.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ensuring bucket exists: {BucketName}", bucketName);
                throw;
            }
        }

        /// <summary>
        /// Generates a presigned URL for an object.
        /// </summary>
        public static string? GeneratePresignedUrl(IAmazonS3 client, string bucketName, string key, int expiresInSeconds, ILogger logger)
        {
            if (string.IsNullOrEmpty(key) || expiresInSeconds <= 0) return null;

            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    Expires = DateTime.UtcNow.AddSeconds(expiresInSeconds),
                    Verb = HttpVerb.GET
                };

                return client.GetPreSignedURL(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating presigned URL for reference {Key}", key);
                return null;
            }
        }
    }
}
