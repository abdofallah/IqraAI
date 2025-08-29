using Minio;

namespace IqraInfrastructure.Repositories.MinIO
{
    public class MinioPrivatePublicClient
    {
        private readonly IMinioClient _privateClient;
        private readonly IMinioClient _publicClient;
        public MinioPrivatePublicClient(
            string privateEndpoint,
            int privateEndpointPort,
            bool isPrivateEndpointSecure,
            string publicEndpoint,
            int publicEndpointPort,
            bool isPublicEndpointSecure,
            string accessKey,
            string secretKey
        )
        {
            _privateClient = new MinioClient()
                    .WithEndpoint(privateEndpoint, privateEndpointPort)
                    .WithCredentials(accessKey, secretKey)
                    .WithSSL(isPrivateEndpointSecure)
                    .Build();

            _publicClient = new MinioClient()
                    .WithEndpoint(publicEndpoint, publicEndpointPort)
                    .WithCredentials(accessKey, secretKey)
                    .WithSSL(isPublicEndpointSecure)
                    .Build();
        }

        public IMinioClient PrivateClient => _privateClient;
        public IMinioClient PublicClient => _publicClient;
    }
}
