using IqraCore.Interfaces;
using StackExchange.Redis;

namespace IqraInfrastructure.Caching
{
    public class RedisAudioCache : IAudioCache
    {
        private readonly ConnectionMultiplexer _redisConnection;
        private readonly IDatabase _redisDatabase;

        public RedisAudioCache(string redisConnectionString)
        {
            _redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
            _redisDatabase = _redisConnection.GetDatabase();
        }

        public void SetAudioData(ulong textHash, string ttsProvider, string language, string speaker, byte[] audioData)
        {
            var key = $"{ttsProvider}:{language}:{textHash}:{speaker}";
            _redisDatabase.StringSet(key, audioData);
        }

        public byte[]? GetAudioData(ulong textHash, string ttsProvider, string language, string speaker)
        {
            var key = $"{ttsProvider}:{language}:{textHash}:{speaker}";

            RedisValue result = _redisDatabase.StringGet(key);

            if (result.IsNullOrEmpty) return null;

            return result;
        }
    }
}