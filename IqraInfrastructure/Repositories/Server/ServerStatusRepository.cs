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

            _serverStatusCollection = database.GetCollection<ServerStatusData>("ServerStatus");
            _historicalStatusCollection = database.GetCollection<ServerHistoricalStatusData>("ServerHistoricalStatus");

            _logger = logger;

            CreateIndexes();
        }

        private void CreateIndexes()
        {
            try
            {
                // Create index for region ID on server status
                var regionIndex = Builders<ServerStatusData>.IndexKeys
                    .Ascending(s => s.RegionId);

                _serverStatusCollection.Indexes.CreateOne(new CreateIndexModel<ServerStatusData>(regionIndex));

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

        public async Task UpdateServerStatusAsync(ServerStatusData status)
        {
            try
            {
                var filter = Builders<ServerStatusData>.Filter.Eq(s => s.ServerId, status.ServerId);

                var options = new ReplaceOptions { IsUpsert = true };
                await _serverStatusCollection.ReplaceOneAsync(filter, status, options);

                _logger.LogDebug("Updated status for server {ServerId}", status.ServerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for server {ServerId}", status.ServerId);
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

        public async Task<ServerStatusData?> GetServerStatusAsync(string serverId)
        {
            try
            {
                var filter = Builders<ServerStatusData>.Filter.Eq(s => s.ServerId, serverId);
                return await _serverStatusCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for server {ServerId}", serverId);
                return null;
            }
        }

        public async Task<List<ServerStatusData>> GetAllServerStatusesAsync()
        {
            try
            {
                return await _serverStatusCollection.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all server statuses");
                return new List<ServerStatusData>();
            }
        }

        public async Task<List<ServerStatusData>> GetRegionServerStatusesAsync(string regionId)
        {
            try
            {
                var filter = Builders<ServerStatusData>.Filter.Eq(s => s.RegionId, regionId);
                return await _serverStatusCollection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting server statuses for region {RegionId}", regionId);
                return new List<ServerStatusData>();
            }
        }

        public async Task SetMaintenanceModeAsync(string serverId, bool maintenanceMode)
        {
            try
            {
                var filter = Builders<ServerStatusData>.Filter.Eq(s => s.ServerId, serverId);

                var update = Builders<ServerStatusData>.Update
                    .Set(s => s.MaintenanceMode, maintenanceMode)
                    .Set(s => s.MaintenanceModeStartedAt, maintenanceMode ? DateTime.UtcNow as DateTime? : null);

                await _serverStatusCollection.UpdateOneAsync(filter, update);

                _logger.LogInformation("Server {ServerId} maintenance mode set to {MaintenanceMode}",
                    serverId, maintenanceMode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting maintenance mode for server {ServerId}", serverId);
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

        public async Task<int> CleanupStaleServerStatusesAsync(TimeSpan threshold)
        {
            try
            {
                var thresholdTime = DateTime.UtcNow.Subtract(threshold);

                var filter = Builders<ServerStatusData>.Filter.Lt(s => s.LastUpdated, thresholdTime);
                var result = await _serverStatusCollection.DeleteManyAsync(filter);

                if (result.DeletedCount > 0)
                {
                    _logger.LogWarning("Cleaned up {Count} stale server statuses", result.DeletedCount);
                }

                return (int)result.DeletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up stale server statuses");
                return 0;
            }
        }
    }
}