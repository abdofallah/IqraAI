using Amazon.Runtime;
using Amazon.S3;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Entities.S3Storage;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Repositories.S3Storage
{
    public class S3StorageClientFactory
    {
        private ILogger<S3StorageClientFactory> _logger;

        private (IAmazonS3 client, S3StorageConfigData config) _defaultClient;
        private readonly Dictionary<string, (IAmazonS3? client, S3StorageConfigData? config, bool useDefault)> _regionClients;

        public S3StorageClientFactory()
        {
            _regionClients = new Dictionary<string, (IAmazonS3? client, S3StorageConfigData? config, bool useDefault)>();
        }

        public void SetLogger(ILogger<S3StorageClientFactory> logger) => _logger = logger;

        public async Task<FunctionReturnResult> Initalize(S3StorageConfigData defaultS3Config, List<RegionData> regionsData)
        {
            var result = new FunctionReturnResult();

            try
            {
                // Default Client
                var defaultS3ClientResult = await CreateAmazonS3Client(defaultS3Config);
                if (!defaultS3ClientResult.Success)
                {
                    throw new Exception($"S3 client failed for default region: [{defaultS3ClientResult.Code}] {defaultS3ClientResult.Message}");
                }
                _defaultClient = (defaultS3ClientResult.Data!, defaultS3Config);

                // Region Clients
                foreach (var region in regionsData)
                {
                    if (region.DisabledAt != null)
                    {
                        continue;
                    }

                    if (region.UseDefaultS3)
                    {
                        _regionClients.Add(region.RegionId, (_defaultClient.client, _defaultClient.config, true));
                        continue;
                    }

                    var s3StorageServer = region.S3Server;
                    if (s3StorageServer == null)
                    {
                        throw new Exception($"S3 Server Config not found (null) for region: {region.RegionId}");
                    }

                    if (s3StorageServer.DisabledAt != null)
                    {
                        continue;
                    }

                    var s3ClientResult = await CreateAmazonS3Client(s3StorageServer);
                    if (!s3ClientResult.Success)
                    {
                        throw new Exception($"S3 client failed for region {region.RegionId}: [{s3ClientResult.Code}] {s3ClientResult.Message}");
                    }

                    _regionClients.Add(region.RegionId, (s3ClientResult.Data!, s3StorageServer, false));
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Initalize:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        public static async Task<FunctionReturnResult<IAmazonS3?>> CreateAmazonS3Client(S3StorageConfigData s3Config)
        {
            var result = new FunctionReturnResult<IAmazonS3?>();

            var protocol = s3Config.UseSSL ? "https" : "http";
            var serviceUrl = $"{protocol}://{s3Config.Endpoint}";

            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
                UseHttp = !s3Config.UseSSL
            };

            var credentials = new BasicAWSCredentials(s3Config.AccessKey, s3Config.SecretKey);
            var s3Client = new AmazonS3Client(credentials, config);

            try
            {
                await s3Client.ListBucketsAsync();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Initalize:CONNECTION_LIST_FAILED",
                    $"Exception: {ex.Message}"
                );
            }

            return result.SetSuccessResult(s3Client);
        }

        public IAmazonS3? GetDefaultClient() => _defaultClient.client;
        public IAmazonS3? GetClientForRegion(string region)
        {
            var regionClient = _regionClients.TryGetValue(region, out var data);
            if (!regionClient)
            {
                _logger.LogError("S3 Client not found for region: {Region}", region);
                return null;
            }

            if (data.useDefault)
            {
                return GetDefaultClient();
            }

            return data.client;
        }

        public S3StorageConfigData? GetDefaultConfig() => _defaultClient.config;
        public S3StorageConfigData? GetConfigForRegion(string region)
        {
            var regionClient = _regionClients.TryGetValue(region, out var data);
            if (!regionClient)
            {
                _logger.LogError("S3 Config not found for region: {Region}", region);
                return null;
            }

            if (data.useDefault) {
                return GetDefaultConfig();
            }

            return data.config;
        }
    }
}
