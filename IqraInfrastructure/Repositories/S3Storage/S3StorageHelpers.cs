using Amazon.S3;
using Amazon.S3.Model;
using IqraCore.Models.S3Storage;
using Microsoft.Extensions.Logging;
using System.Net;

namespace IqraInfrastructure.Repositories.S3Storage
{
    public static class S3StorageHelpers
    {
        public static IAmazonS3 GetRegionS3Client(S3StorageClientFactory factory, string region)
        {
            var client = factory.GetClientForRegion(region);

            if (client == null)
            {
                throw new InvalidOperationException($"S3 Client not found for region: {region}");
            }

            return client;
        }

        public static IAmazonS3 GetDefaultS3Client(S3StorageClientFactory factory)
        {
            var client = factory.GetDefaultClient();

            if (client == null)
            {
                throw new InvalidOperationException("Default S3 Client not found");
            }

            return client;
        }

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

        public static S3SignedFileModel? GenerateSignedFile(IAmazonS3 client, string bucketName, string key, int expiresInSeconds, ILogger logger)
        {
            if (string.IsNullOrEmpty(key) || expiresInSeconds <= 0) return null;

            try
            {
                var expiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    Expires = expiresAt,
                    Verb = HttpVerb.GET
                };

                return new S3SignedFileModel(client.GetPreSignedURL(request), expiresAt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating presigned URL for reference {Key}", key);
                return null;
            }
        }
    }
}
