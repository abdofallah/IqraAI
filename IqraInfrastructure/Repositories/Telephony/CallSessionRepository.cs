using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Telephony.Call;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Telephony
{
    public class CallSessionRepository
    {
        private readonly IMongoCollection<CallSessionData> _callSessionCollection;
        private readonly ILogger<CallSessionRepository> _logger;
        
        public CallSessionRepository(
            string connectionString, 
            string databaseName,
            ILogger<CallSessionRepository> logger)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _callSessionCollection = database.GetCollection<CallSessionData>("CallSessions");
            _logger = logger;
            
            CreateIndexes();
        }
        
        private void CreateIndexes()
        {
            try
            {
                // Create index for queue ID
                var queueIndex = Builders<CallSessionData>.IndexKeys
                    .Ascending(c => c.QueueId);
                    
                _callSessionCollection.Indexes.CreateOne(new CreateIndexModel<CallSessionData>(queueIndex));
                
                // Create index for business ID
                var businessIndex = Builders<CallSessionData>.IndexKeys
                    .Ascending(c => c.BusinessId);
                    
                _callSessionCollection.Indexes.CreateOne(new CreateIndexModel<CallSessionData>(businessIndex));
                
                // Create index for processing server
                var serverIndex = Builders<CallSessionData>.IndexKeys
                    .Ascending(c => c.ProcessingServer);
                    
                _callSessionCollection.Indexes.CreateOne(new CreateIndexModel<CallSessionData>(serverIndex));
                
                // Create TTL index to automatically expire completed sessions after 7 days
                var ttlIndex = Builders<CallSessionData>.IndexKeys
                    .Ascending(c => c.EndedAt);
                    
                var indexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(7) };
                _callSessionCollection.Indexes.CreateOne(new CreateIndexModel<CallSessionData>(ttlIndex, indexOptions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating indexes for call session collection");
            }
        }
        
        public async Task<string> CreateSessionAsync(CallSessionData sessionData)
        {
            try
            {
                await _callSessionCollection.InsertOneAsync(sessionData);
                _logger.LogInformation("Session created: {SessionId} for queue item {QueueId}", 
                    sessionData.Id, sessionData.QueueId);
                return sessionData.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session for queue item {QueueId}", sessionData.QueueId);
                throw;
            }
        }
        
        public async Task<CallSessionData?> GetSessionByIdAsync(string sessionId)
        {
            try
            {
                var filter = Builders<CallSessionData>.Filter.Eq(c => c.Id, sessionId);
                return await _callSessionCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session by ID {SessionId}", sessionId);
                return null;
            }
        }
        
        public async Task<CallSessionData?> GetSessionByQueueIdAsync(string queueId)
        {
            try
            {
                var filter = Builders<CallSessionData>.Filter.Eq(c => c.QueueId, queueId);
                return await _callSessionCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session by queue ID {QueueId}", queueId);
                return null;
            }
        }
        
        public async Task UpdateSessionStatusAsync(string sessionId, CallSessionStatusEnum status)
        {
            try
            {
                var filter = Builders<CallSessionData>.Filter.Eq(c => c.Id, sessionId);
                var update = Builders<CallSessionData>.Update
                    .Set(c => c.Status, status);
                
                if (status == CallSessionStatusEnum.Completed || 
                    status == CallSessionStatusEnum.Failed || 
                    status == CallSessionStatusEnum.Canceled)
                {
                    update = update.Set(c => c.EndedAt, DateTime.UtcNow);
                }
                
                await _callSessionCollection.UpdateOneAsync(filter, update);
                
                _logger.LogInformation("Session {SessionId} status updated to {Status}", sessionId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for session {SessionId}", sessionId);
            }
        }
        
        public async Task UpdateConversationIdAsync(string sessionId, long conversationId)
        {
            try
            {
                var filter = Builders<CallSessionData>.Filter.Eq(c => c.Id, sessionId);
                var update = Builders<CallSessionData>.Update
                    .Set(c => c.ConversationId, conversationId);
                
                await _callSessionCollection.UpdateOneAsync(filter, update);
                
                _logger.LogInformation("Session {SessionId} linked to conversation {ConversationId}", 
                    sessionId, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating conversation ID for session {SessionId}", sessionId);
            }
        }
        
        public async Task AddLogEntryAsync(string sessionId, string message, 
            CallSessionLogLevelEnum level = CallSessionLogLevelEnum.Info, string? component = null)
        {
            try
            {
                var entry = new CallSessionLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Message = message,
                    Level = level,
                    Component = component
                };
                
                var filter = Builders<CallSessionData>.Filter.Eq(c => c.Id, sessionId);
                var update = Builders<CallSessionData>.Update
                    .Push(c => c.Logs, entry);
                
                await _callSessionCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding log entry to session {SessionId}", sessionId);
            }
        }
        
        public async Task UpdateSessionMetricsAsync(string sessionId, CallSessionMetrics metrics)
        {
            try
            {
                var filter = Builders<CallSessionData>.Filter.Eq(c => c.Id, sessionId);
                var update = Builders<CallSessionData>.Update
                    .Set(c => c.Metrics, metrics);
                
                await _callSessionCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metrics for session {SessionId}", sessionId);
            }
        }
        
        public async Task<List<CallSessionData>> GetActiveSessionsForServerAsync(string serverId)
        {
            try
            {
                var filter = Builders<CallSessionData>.Filter.And(
                    Builders<CallSessionData>.Filter.Eq(c => c.ProcessingServer, serverId),
                    Builders<CallSessionData>.Filter.In(c => c.Status, new[] 
                    { 
                        CallSessionStatusEnum.Initializing,
                        CallSessionStatusEnum.Connecting,
                        CallSessionStatusEnum.Active
                    })
                );
                
                return await _callSessionCollection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active sessions for server {ServerId}", serverId);
                return new List<CallSessionData>();
            }
        }
        
        public async Task CleanupOrphanedSessionsAsync(TimeSpan threshold)
        {
            try
            {
                var thresholdTime = DateTime.UtcNow.Subtract(threshold);
                
                var filter = Builders<CallSessionData>.Filter.And(
                    Builders<CallSessionData>.Filter.In(c => c.Status, new[] 
                    { 
                        CallSessionStatusEnum.Initializing,
                        CallSessionStatusEnum.Connecting,
                        CallSessionStatusEnum.Active
                    }),
                    Builders<CallSessionData>.Filter.Lt(c => c.StartedAt, thresholdTime)
                );
                
                var update = Builders<CallSessionData>.Update
                    .Set(c => c.Status, CallSessionStatusEnum.Failed)
                    .Set(c => c.EndedAt, DateTime.UtcNow)
                    .Push(c => c.Logs, new CallSessionLogEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        Message = "Session marked as failed due to inactivity",
                        Level = CallSessionLogLevelEnum.Warning,
                        Component = "SystemCleanup"
                    });
                
                var result = await _callSessionCollection.UpdateManyAsync(filter, update);
                
                if (result.ModifiedCount > 0)
                {
                    _logger.LogWarning("Cleaned up {Count} orphaned sessions", result.ModifiedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up orphaned sessions");
            }
        }
    }
}