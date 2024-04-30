using IqraCore.Entities.Session;
using IqraCore.Interfaces.Repositories;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly IMongoCollection<Session> _sessionsCollection;

        public SessionRepository(IMongoDatabase database)
        {
            _sessionsCollection = database.GetCollection<Session>("sessions");
        }

        public async Task<Session> CreateSessionAsync()
        {
            var session = new Session
            {
                SessionId = Guid.NewGuid().ToString(),
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.MinValue
            };

            await _sessionsCollection.InsertOneAsync(session);
            return session;
        }

        public async Task<Session> GetSessionAsync(string sessionId)
        {
            var filter = Builders<Session>.Filter.Eq(s => s.SessionId, sessionId);
            return await _sessionsCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task UpdateSessionAsync(Session session)
        {
            var filter = Builders<Session>.Filter.Eq(s => s.SessionId, session.SessionId);
            await _sessionsCollection.ReplaceOneAsync(filter, session);
        }
    }
}