using IqraCore.Entities.Interfaces;
using IqraCore.Entities.LLM;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.LLM
{
    public class LLMProviderRepository
    {
        private readonly string CollectionName = "LLMProvider";

        private readonly IMongoCollection<LLMProviderData> _llmProviderCollection;

        public LLMProviderRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _llmProviderCollection = database.GetCollection<LLMProviderData>(CollectionName);
        }

        public async Task AddProviderAsync(LLMProviderData providerData)
        {
            await _llmProviderCollection.InsertOneAsync(providerData);
        }

        public async Task<LLMProviderData> GetProviderAsync(InterfaceLLMProviderEnum id)
        {
            return await _llmProviderCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<UpdateResult> UpdateProviderAsync(LLMProviderData providerData)
        {
            var filter = Builders<LLMProviderData>.Filter.Eq(x => x.Id, providerData.Id);
            var update = Builders<LLMProviderData>.Update
                .Set(x => x.DisabledAt, providerData.DisabledAt)
                .Set(x => x.LLMProviderModelData, providerData.LLMProviderModelData);
            return await _llmProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> DisableProviderAsync(InterfaceLLMProviderEnum id)
        {
            var filter = Builders<LLMProviderData>.Filter.Eq(x => x.Id, id);
            var update = Builders<LLMProviderData>.Update.Set(x => x.DisabledAt, DateTime.UtcNow);
            return await _llmProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> EnableProviderAsync(InterfaceLLMProviderEnum id)
        {
            var filter = Builders<LLMProviderData>.Filter.Eq(x => x.Id, id);
            var update = Builders<LLMProviderData>.Update.Set(x => x.DisabledAt, null);
            return await _llmProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<DeleteResult> RemoveProviderAsync(InterfaceLLMProviderEnum id)
        {
            return await _llmProviderCollection.DeleteOneAsync(x => x.Id == id);
        }

        public async Task<List<LLMProviderData>> GetAllProvidersAsync()
        {
            return await _llmProviderCollection.Find(_ => true).ToListAsync();
        }

        public async Task<UpdateResult> AddModelAsync(InterfaceLLMProviderEnum providerId, LLMProviderModelData modelData)
        {
            var filter = Builders<LLMProviderData>.Filter.Eq(x => x.Id, providerId);
            var update = Builders<LLMProviderData>.Update.Push(x => x.LLMProviderModelData, modelData);
            return await _llmProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> UpdateModelAsync(InterfaceLLMProviderEnum providerId, LLMProviderModelData modelData)
        {
            var filter = Builders<LLMProviderData>.Filter.And(
                Builders<LLMProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<LLMProviderData>.Filter.ElemMatch(x => x.LLMProviderModelData, m => m.Id == modelData.Id)
            );
            var update = Builders<LLMProviderData>.Update.Set(x => x.LLMProviderModelData.FirstMatchingElement(), modelData);
            return await _llmProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> DisableModelAsync(InterfaceLLMProviderEnum providerId, string modelId)
        {
            var filter = Builders<LLMProviderData>.Filter.And(
                Builders<LLMProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<LLMProviderData>.Filter.ElemMatch(x => x.LLMProviderModelData, m => m.Id == modelId)
            );
            var update = Builders<LLMProviderData>.Update.Set(x => x.LLMProviderModelData.FirstMatchingElement().DisabledAt, DateTime.UtcNow);
            return await _llmProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> RemoveModelAsync(InterfaceLLMProviderEnum providerId, string modelId)
        {
            var filter = Builders<LLMProviderData>.Filter.Eq(x => x.Id, providerId);
            var update = Builders<LLMProviderData>.Update.PullFilter(x => x.LLMProviderModelData, m => m.Id == modelId);
            return await _llmProviderCollection.UpdateOneAsync(filter, update);
        }
    }
}
