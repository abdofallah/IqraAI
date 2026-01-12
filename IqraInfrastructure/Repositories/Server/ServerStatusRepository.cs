using IqraCore.Entities.Server;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Server
{
    public class ServerStatusRepository
    {
        private readonly IMongoCollection<ServerStatusData> _serverStatusCollection;
        private readonly ILogger<ServerStatusRepository> _logger;

        private readonly string DatabaseName = "IqraServerStatus";
        private readonly string CollectionName = "ServerHistoricalStatus";

        public ServerStatusRepository(ILogger<ServerStatusRepository> logger, IMongoClient client)
        {
            _logger = logger;

            var database = client.GetDatabase(DatabaseName);
            _serverStatusCollection = database.GetCollection<ServerStatusData>(CollectionName);        

            CreateIndexes();
        }

        private void CreateIndexes()
        {
            try
            {
                // 1. Compound Index for efficient Range Queries (NodeId + LastUpdated)
                // This is critical for graphs: "Give me Node X from Date A to Date B"
                var compoundIndexKeys = Builders<ServerStatusData>.IndexKeys
                    .Ascending(s => s.NodeId)
                    .Ascending(s => s.LastUpdated);

                _serverStatusCollection.Indexes.CreateOne(
                    new CreateIndexModel<ServerStatusData>(compoundIndexKeys, new CreateIndexOptions { Name = "NodeId_LastUpdated" }));

                // 2. TTL Index (Auto-delete old data)
                var ttlIndexKeys = Builders<ServerStatusData>.IndexKeys
                    .Ascending(s => s.LastUpdated);

                var ttlOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(30), Name = "TTL_30Days" };

                _serverStatusCollection.Indexes.CreateOne(
                    new CreateIndexModel<ServerStatusData>(ttlIndexKeys, ttlOptions));
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
                await _serverStatusCollection.InsertOneAsync(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording historical status for node {NodeId}", status.NodeId);
            }
        }

        public async Task<List<ServerStatusData>> GetRawServerHistoryAsync(string nodeId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var filter = Builders<ServerStatusData>.Filter.And(
                    Builders<ServerStatusData>.Filter.Eq(s => s.NodeId, nodeId),
                    Builders<ServerStatusData>.Filter.Gte(s => s.LastUpdated, startTime),
                    Builders<ServerStatusData>.Filter.Lte(s => s.LastUpdated, endTime)
                );

                var sort = Builders<ServerStatusData>.Sort.Ascending(s => s.LastUpdated);

                return await _serverStatusCollection.Find(filter).Sort(sort).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting raw history for node {NodeId}", nodeId);
                return new List<ServerStatusData>();
            }
        }
    }
}