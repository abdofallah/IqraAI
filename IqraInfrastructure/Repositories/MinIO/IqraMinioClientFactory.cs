using Minio;

namespace IqraInfrastructure.Repositories.MinIO
{
    public class IqraMinioClientFactory
    {
        private readonly Dictionary<string, MinioPrivatePublicClient> _clients;

        public IqraMinioClientFactory(Dictionary<string, MinioPrivatePublicClient> minioClients)
        {
            _clients = minioClients;
        }

        public IMinioClient? GetLocalClientForRegion(string region)
        {
            return _clients.TryGetValue(region, out var client) ? client.PrivateClient : null;
        }

        public IMinioClient? GetPublicUrlClientForRegion(string region)
        {
            return _clients.TryGetValue(region, out var client) ? client.PublicClient : null;
        }
    }
}
