using IqraCore.Entities.Business;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessAppRepository
    {
        private readonly string CollectionName = "BusinessApp";

        private readonly IMongoCollection<BusinessApp> _businessAppCollection;

        public BusinessAppRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _businessAppCollection = database.GetCollection<BusinessApp>(CollectionName);
        }

        public Task<List<BusinessApp>> GetBusinessesAppAsync()
        {
            return _businessAppCollection.Find(_ => true).ToListAsync();
        }

        public Task<BusinessApp?> GetBusinessAppAsync(long businessId)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            return _businessAppCollection.Find(filter).FirstOrDefaultAsync();
        }

        public Task AddBusinessAppAsync(BusinessApp businessApp)
        {
            return _businessAppCollection.InsertOneAsync(businessApp);
        }

        public async Task<bool> DeleteBusinessAppAsync(long businessId)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessAppCollection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public async Task<bool> UpdateBusinessAppAsync(long businessId, UpdateDefinition<BusinessApp> updateDefinition)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessAppCollection.UpdateOneAsync(filter, updateDefinition);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> ReplaceBusinessAppAsync(BusinessApp? businessApp)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessApp.Id);
            var result = await _businessAppCollection.ReplaceOneAsync(filter, businessApp);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> CheckBusinessAppToolExists(long businessId, string toolId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == toolId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> UpdateBusinessAppTool(long businessId, BusinessAppTool newBusinessAppToolData)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                 Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                 Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == newBusinessAppToolData.Id)
            );
                
            var update = Builders<BusinessApp>.Update.Set(b => b.Tools.FirstMatchingElement(), newBusinessAppToolData);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddBusinessAppTool(long businessId, BusinessAppTool newBusinessAppToolData)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Tools, newBusinessAppToolData);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> CheckBusinessAppBranchExists(long businessId, string branchId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Context.Branches, t => t.Id == branchId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> UpdateBusinessContextBranding(long businessId, BusinessAppContextBranding newBusinessContextBranding)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Set(b => b.Context.Branding, newBusinessContextBranding);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddBusinessContextBranch(long businessId, BusinessAppContextBranch newBusinessContextBranch)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Context.Branches, newBusinessContextBranch);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessContextBranch(long businessId, BusinessAppContextBranch newBusinessContextBranch)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Context.Branches, t => t.Id == newBusinessContextBranch.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Context.Branches.FirstMatchingElement(), newBusinessContextBranch);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
    }
}