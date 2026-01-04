using IqraCore.Entities.Interfaces;
using IqraCore.Entities.Rerank;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.Rerank
{
    public class RerankProviderRepository
    {
        private readonly ILogger<RerankProviderRepository> _logger;
        private readonly IMongoCollection<RerankProviderData> _rerankProviderCollection;

        private readonly string DatabaseName = "IqraRerankProvider";
        private readonly string CollectionName = "RerankProvider";

        public RerankProviderRepository(ILogger<RerankProviderRepository> logger, IMongoClient client)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase(DatabaseName);
            _rerankProviderCollection = database.GetCollection<RerankProviderData>(CollectionName);
        }

        public async Task AddProviderAsync(RerankProviderData providerData)
        {
            await _rerankProviderCollection.InsertOneAsync(providerData);
        }

        public async Task<RerankProviderData?> GetProviderAsync(InterfaceRerankProviderEnum id)
        {
            return await _rerankProviderCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<ReplaceOneResult> UpdateProviderAsync(RerankProviderData providerData)
        {
            var filter = Builders<RerankProviderData>.Filter.Eq(x => x.Id, providerData.Id);
            return await _rerankProviderCollection.ReplaceOneAsync(filter, providerData);
        }

        public async Task<UpdateResult> DisableProviderAsync(InterfaceRerankProviderEnum id)
        {
            var filter = Builders<RerankProviderData>.Filter.Eq(x => x.Id, id);
            var update = Builders<RerankProviderData>.Update.Set(x => x.DisabledAt, DateTime.UtcNow);
            return await _rerankProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> EnableProviderAsync(InterfaceRerankProviderEnum id)
        {
            var filter = Builders<RerankProviderData>.Filter.Eq(x => x.Id, id);
            var update = Builders<RerankProviderData>.Update.Set(x => x.DisabledAt, null);
            return await _rerankProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<List<RerankProviderData>?> GetProviderListAsync(int page, int pageSize)
        {
            return await _rerankProviderCollection
                .Find(_ => true)
                .SortByDescending(x => x.Id)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<UpdateResult> AddModelAsync(InterfaceRerankProviderEnum providerId, RerankProviderModelData modelData)
        {
            var filter = Builders<RerankProviderData>.Filter.Eq(x => x.Id, providerId);
            var update = Builders<RerankProviderData>.Update.Push(x => x.Models, modelData);
            return await _rerankProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> UpdateModelAsync(InterfaceRerankProviderEnum providerId, RerankProviderModelData modelData)
        {
            var filter = Builders<RerankProviderData>.Filter.And(
                Builders<RerankProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<RerankProviderData>.Filter.ElemMatch(x => x.Models, m => m.Id == modelData.Id)
            );
            var update = Builders<RerankProviderData>.Update.Set(x => x.Models.FirstMatchingElement(), modelData);
            return await _rerankProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> DisableModelAsync(InterfaceRerankProviderEnum providerId, string modelId)
        {
            var filter = Builders<RerankProviderData>.Filter.And(
                Builders<RerankProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<RerankProviderData>.Filter.ElemMatch(x => x.Models, m => m.Id == modelId)
            );
            var update = Builders<RerankProviderData>.Update.Set(x => x.Models.FirstMatchingElement().DisabledAt, DateTime.UtcNow);
            return await _rerankProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<RerankProviderData?> GetProviderDataByIntegration(string integrationType)
        {
            var filter = Builders<RerankProviderData>.Filter.Eq(x => x.IntegrationId, integrationType);
            return await _rerankProviderCollection.Find(filter).FirstOrDefaultAsync();
        }
    }
}
