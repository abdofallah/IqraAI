using Amazon.Auth.AccessControlPolicy;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using IqraCore.Entities.Region;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

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
        public static RegionS3StorageServerData GetS3ClientServerData(S3StorageClientFactory factory, string? region)
        {
            var client = string.IsNullOrEmpty(region)
                ? factory.GetServerForCurrentRegion()
                : factory.GetServerForRegion(region);

            if (client == null)
            {
                throw new InvalidOperationException($"S3 Server not found for region: {region ?? "Current"}");
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
