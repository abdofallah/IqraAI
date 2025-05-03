using IqraCore.Entities.Server;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Server
{
    public class ServerStatusRepository
    {
        private readonly IMongoCollection<ServerStatusData> _serverStatusCollection;
        private readonly IMongoCollection<ServerHistoricalStatusData> _historicalStatusCollection;
        private readonly ILogger<ServerStatusRepository> _logger;

        public ServerStatusRepository(
            string connectionString,
            string databaseName,
            ILogger<ServerStatusRepository> logger)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);

            _historicalStatusCollection = database.GetCollection<ServerHistoricalStatusData>("ServerHistoricalStatus");

            _logger = logger;

            CreateIndexes();
        }

        private void CreateIndexes()
        {
            try
            {
                // Create indexes for historical status
                var historicalServerIndex = Builders<ServerHistoricalStatusData>.IndexKeys
                    .Ascending(s => s.ServerId);

                _historicalStatusCollection.Indexes.CreateOne(
                    new CreateIndexModel<ServerHistoricalStatusData>(historicalServerIndex));

                var historicalTimestampIndex = Builders<ServerHistoricalStatusData>.IndexKeys
                    .Ascending(s => s.Timestamp);

                var indexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(30) };
                _historicalStatusCollection.Indexes.CreateOne(
                    new CreateIndexModel<ServerHistoricalStatusData>(historicalTimestampIndex, indexOptions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating indexes for server status collections");
            }
        }

        public async Task RecordHistoricalStatusAsync(ServerStatusData status)
        {
            try
            {
                var historicalStatus = new ServerHistoricalStatusData
                {
                    ServerId = status.ServerId,
                    Timestamp = DateTime.UtcNow,
                    ActiveCalls = status.CurrentActiveCallsCount,
                    QueuedCalls = status.QueuedCallsCount,
                    CpuUsagePercent = status.CpuUsagePercent,
                    MemoryUsagePercent = status.MemoryUsagePercent,
                    NetworkDownloadMbps = status.NetworkDownloadMbps,
                    NetworkUploadMbps = status.NetworkUploadMbps
                };

                await _historicalStatusCollection.InsertOneAsync(historicalStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording historical status for server {ServerId}", status.ServerId);
            }
        }

        public async Task<List<ServerHistoricalStatusData>> GetServerHistoryAsync(
            string serverId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var filter = Builders<ServerHistoricalStatusData>.Filter.And(
                    Builders<ServerHistoricalStatusData>.Filter.Eq(s => s.ServerId, serverId),
                    Builders<ServerHistoricalStatusData>.Filter.Gte(s => s.Timestamp, startTime),
                    Builders<ServerHistoricalStatusData>.Filter.Lte(s => s.Timestamp, endTime)
                );

                var sort = Builders<ServerHistoricalStatusData>.Sort.Ascending(s => s.Timestamp);

                return await _historicalStatusCollection.Find(filter).Sort(sort).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history for server {ServerId}", serverId);
                return new List<ServerHistoricalStatusData>();
            }
        }
    }
}