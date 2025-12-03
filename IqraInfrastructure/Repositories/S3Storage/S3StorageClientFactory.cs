using Amazon.Runtime;
using Amazon.S3;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;

namespace IqraInfrastructure.Repositories.S3Storage
{
    public class S3StorageClientFactory
    {
        private readonly string _currentRegion;
        private readonly Dictionary<string, (IAmazonS3 client, RegionS3StorageServerData server)> _regionClients;

        public S3StorageClientFactory(string currentRegionCode)
        {
            _currentRegion = currentRegionCode;
            _regionClients = new Dictionary<string, (IAmazonS3 client, RegionS3StorageServerData server)>();
        }

        public string GetCurrentRegion() => _currentRegion;

        public async Task<FunctionReturnResult> Initalize(List<RegionData> regionsData)
        {
            var result = new FunctionReturnResult();

            try
            {
                foreach (var region in regionsData)
                {
                    if (region.DisabledAt != null)
                    {
                        continue;
                    }

                    var s3StorageServer = region.S3Server;
                    if (s3StorageServer.DisabledAt != null)
                    {
                        continue;
                    }

                    var protocol = s3StorageServer.UseSSL ? "https" : "http";
                    var serviceUrl = $"{protocol}://{s3StorageServer.Endpoint}";

                    var config = new AmazonS3Config
                    {
                        ServiceURL = serviceUrl,
                        ForcePathStyle = true,
                        UseHttp = !s3StorageServer.UseSSL
                    };

                    var credentials = new BasicAWSCredentials(s3StorageServer.AccessKey, s3StorageServer.SecretKey);
                    var s3Client = new AmazonS3Client(credentials, config);

                    try
                    {
                        await s3Client.ListBucketsAsync();
                    }
                    catch (Exception ex) {
                        return result.SetFailureResult(
                            "Initalize:CONNECTION_LIST_FAILED",
                            $"Exception: {ex.Message}"
                        );
                    }

                    _regionClients.Add(region.CountryRegion, (s3Client, s3StorageServer));
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

        public IAmazonS3? GetClientForCurrentRegion() => GetClientForRegion(_currentRegion);
        public IAmazonS3? GetClientForRegion(string region)
        {
            return _regionClients.TryGetValue(region, out var data) ? data.client : null;
        }

        public RegionS3StorageServerData? GetServerForCurrentRegion() => GetServerForRegion(_currentRegion);
        public RegionS3StorageServerData? GetServerForRegion(string region)
        {
            return _regionClients.TryGetValue(region, out var data) ? data.server : null;
        }
    }
}
