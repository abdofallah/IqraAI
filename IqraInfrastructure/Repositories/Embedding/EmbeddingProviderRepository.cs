using IqraCore.Entities.Embedding;
using IqraCore.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.Embedding
{
    public class EmbeddingProviderRepository
    {
        private readonly ILogger<EmbeddingProviderRepository> _logger;
        private readonly string CollectionName = "EmbeddingProvider";
        private readonly IMongoCollection<EmbeddingProviderData> _embeddingProviderCollection;

        public EmbeddingProviderRepository(ILogger<EmbeddingProviderRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase(databaseName);
            _embeddingProviderCollection = database.GetCollection<EmbeddingProviderData>(CollectionName);
        }

        public async Task AddProviderAsync(EmbeddingProviderData providerData)
        {
            await _embeddingProviderCollection.InsertOneAsync(providerData);
        }

        public async Task<EmbeddingProviderData?> GetProviderAsync(InterfaceEmbeddingProviderEnum id)
        {
            return await _embeddingProviderCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<ReplaceOneResult> UpdateProviderAsync(EmbeddingProviderData providerData)
        {
            var filter = Builders<EmbeddingProviderData>.Filter.Eq(x => x.Id, providerData.Id);
            return await _embeddingProviderCollection.ReplaceOneAsync(filter, providerData);
        }

        public async Task<UpdateResult> DisableProviderAsync(InterfaceEmbeddingProviderEnum id)
        {
            var filter = Builders<EmbeddingProviderData>.Filter.Eq(x => x.Id, id);
            var update = Builders<EmbeddingProviderData>.Update.Set(x => x.DisabledAt, DateTime.UtcNow);
            return await _embeddingProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> EnableProviderAsync(InterfaceEmbeddingProviderEnum id)
        {
            var filter = Builders<EmbeddingProviderData>.Filter.Eq(x => x.Id, id);
            var update = Builders<EmbeddingProviderData>.Update.Set(x => x.DisabledAt, null);
            return await _embeddingProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<List<EmbeddingProviderData>> GetAllProvidersAsync()
        {
            return await _embeddingProviderCollection.Find(_ => true).ToListAsync();
        }

        public async Task<List<EmbeddingProviderData>?> GetProviderListAsync(int page, int pageSize)
        {
            return await _embeddingProviderCollection
                .Find(_ => true)
                .SortByDescending(x => x.Id)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<UpdateResult> AddModelAsync(InterfaceEmbeddingProviderEnum providerId, EmbeddingProviderModelData modelData)
        {
            var filter = Builders<EmbeddingProviderData>.Filter.Eq(x => x.Id, providerId);
            var update = Builders<EmbeddingProviderData>.Update.Push(x => x.Models, modelData);
            return await _embeddingProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> UpdateModelAsync(InterfaceEmbeddingProviderEnum providerId, EmbeddingProviderModelData modelData)
        {
            var filter = Builders<EmbeddingProviderData>.Filter.And(
                Builders<EmbeddingProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<EmbeddingProviderData>.Filter.ElemMatch(x => x.Models, m => m.Id == modelData.Id)
            );
            var update = Builders<EmbeddingProviderData>.Update.Set(x => x.Models.FirstMatchingElement(), modelData);
            return await _embeddingProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> DisableModelAsync(InterfaceEmbeddingProviderEnum providerId, string modelId)
        {
            var filter = Builders<EmbeddingProviderData>.Filter.And(
                Builders<EmbeddingProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<EmbeddingProviderData>.Filter.ElemMatch(x => x.Models, m => m.Id == modelId)
            );
            var update = Builders<EmbeddingProviderData>.Update.Set(x => x.Models.FirstMatchingElement().DisabledAt, DateTime.UtcNow);
            return await _embeddingProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<EmbeddingProviderData?> GetProviderDataByIntegration(string integrationType)
        {
            var filter = Builders<EmbeddingProviderData>.Filter.Eq(x => x.IntegrationId, integrationType);
            return await _embeddingProviderCollection.Find(filter).FirstOrDefaultAsync();
        }
    }
}
