using Minio;

namespace IqraInfrastructure.Repositories.MinIO
{
    public class IqraMinioClientFactory
    {
        private readonly Dictionary<string, (IMinioClient localClient, IMinioClient publicUrlClient)> _clients;

        public IqraMinioClientFactory(Dictionary<string, (IMinioClient localClient, IMinioClient publicUrlClient)> minioClients)
        {
            _clients = minioClients;
        }

        public IMinioClient? GetLocalClientForRegion(string region)
        {
            return _clients.TryGetValue(region, out var client) ? client.localClient : null;
        }

        public IMinioClient? GetPublicUrlClientForRegion(string region)
        {
            return _clients.TryGetValue(region, out var client) ? client.publicUrlClient : null;
        }
    }
}
