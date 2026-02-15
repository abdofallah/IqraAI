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
        private readonly IMongoCollection<EmbeddingProviderData> _embeddingProviderCollection;

        private readonly string DatabaseName = "IqraEmbeddingProvider";
        private readonly string CollectionName = "EmbeddingProvider";

        public EmbeddingProviderRepository(ILogger<EmbeddingProviderRepository> logger, IMongoClient client)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase(DatabaseName);
            _embeddingProviderCollection = database.GetCollection<EmbeddingProviderData>(CollectionName);
        }

        public async Task<bool> AddProviderAsync(EmbeddingProviderData providerData)
        {
            try
            {
                await _embeddingProviderCollection.InsertOneAsync(providerData);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding provider {ProviderId}", providerData.Id);
                return false;
            }
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

        public async Task<EmbeddingProviderData?> GetProviderDataByIntegration(string integrationType)
        {
            var filter = Builders<EmbeddingProviderData>.Filter.Eq(x => x.IntegrationId, integrationType);
            return await _embeddingProviderCollection.Find(filter).FirstOrDefaultAsync();
        }
    }
}
