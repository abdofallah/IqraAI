using CommunityToolkit.HighPerformance;
using Minio;
using Minio.DataModel.Args;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessToolAudioRepository
    {
        private IMinioClient MinioClient;
        public string BucketName;

        public BusinessToolAudioRepository(string endpoint, int port, string accessKey, string secretKey, string bucketName, bool isSecure)
        {
            BucketName = bucketName;

            MinioClient = new MinioClient()
            .WithEndpoint(endpoint, port)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(isSecure)
            .Build();

            bool bucketExists = MinioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName)).GetAwaiter().GetResult();
            if (!bucketExists)
            {
                throw new ArgumentException("Bucket " + bucketName + " does not exist");
            }
        }

        public async Task PutFileAsByteData(string fileId, ReadOnlyMemory<byte> fileBytes, Dictionary<string, string> metaData)
        {
            using var filestream = fileBytes.AsStream();

            await PutFileAsStreamData(fileId, filestream, metaData);
        }

        public async Task PutFileAsStreamData(string fileId, Stream fileStream, Dictionary<string, string> metaData)
        {
            var args = new PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(fileId)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType("application/octet-stream")
                .WithHeaders(metaData);

            await MinioClient.PutObjectAsync(args);
        }

        public async Task<MemoryStream> GetFileAtPath(string fileId, string filePath)
        {
            MemoryStream stream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(fileId)
                .WithFile(filePath);

            await MinioClient.GetObjectAsync(args);

            return stream;
        }

        public async Task<bool> FileExists(string fileId)
        {
            try
            {
                var args = new StatObjectArgs()
                    .WithBucket(BucketName)
                    .WithObject(fileId);

                await MinioClient.StatObjectAsync(args);
                return true;
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                return false;
            }
        }

        public async Task<MemoryStream> GetFileAsMemoryStream(string fileId)
        {
            MemoryStream stream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(fileId)
                .WithCallbackStream(async s =>
                {
                    await s.CopyToAsync(stream);
                });

            await MinioClient.GetObjectAsync(args);

            return stream;
        }

        public async Task<ReadOnlyMemory<byte>> GetFileAsByteArray(string fileId)
        {
            return new ReadOnlyMemory<byte>((await GetFileAsMemoryStream(fileId)).ToArray());
        }
    }
}
