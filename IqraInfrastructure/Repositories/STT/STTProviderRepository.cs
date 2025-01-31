using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using IqraCore.Entities.STT;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.STT
{
    public class STTProviderRepository
    {
        private readonly string CollectionName = "STTProvider";
        private readonly IMongoCollection<STTProviderData> _sttProviderCollection;

        public STTProviderRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _sttProviderCollection = database.GetCollection<STTProviderData>(CollectionName);
        }

        public async Task AddProviderAsync(STTProviderData providerData)
        {
            await _sttProviderCollection.InsertOneAsync(providerData);
        }

        public async Task<STTProviderData?> GetProviderAsync(InterfaceSTTProviderEnum id)
        {
            return await _sttProviderCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<ReplaceOneResult> UpdateProviderAsync(STTProviderData providerData)
        {
            var filter = Builders<STTProviderData>.Filter.Eq(x => x.Id, providerData.Id);
            return await _sttProviderCollection.ReplaceOneAsync(filter, providerData);
        }

        public async Task<UpdateResult> DisableProviderAsync(InterfaceSTTProviderEnum id)
        {
            var filter = Builders<STTProviderData>.Filter.Eq(x => x.Id, id);
            var update = Builders<STTProviderData>.Update.Set(x => x.DisabledAt, DateTime.UtcNow);
            return await _sttProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> EnableProviderAsync(InterfaceSTTProviderEnum id)
        {
            var filter = Builders<STTProviderData>.Filter.Eq(x => x.Id, id);
            var update = Builders<STTProviderData>.Update.Set(x => x.DisabledAt, null);
            return await _sttProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<DeleteResult> RemoveProviderAsync(InterfaceSTTProviderEnum id)
        {
            return await _sttProviderCollection.DeleteOneAsync(x => x.Id == id);
        }

        public async Task<List<STTProviderData>> GetAllProvidersAsync()
        {
            return await _sttProviderCollection.Find(_ => true).ToListAsync();
        }

        public async Task<List<STTProviderData>?> GetProviderListAsync(int page, int pageSize)
        {
            return await _sttProviderCollection
                .Find(_ => true)
                .SortByDescending(x => x.Id)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<UpdateResult> AddModelAsync(InterfaceSTTProviderEnum providerId, STTProviderModelData modelData)
        {
            var filter = Builders<STTProviderData>.Filter.Eq(x => x.Id, providerId);
            var update = Builders<STTProviderData>.Update.Push(x => x.Models, modelData);
            return await _sttProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> UpdateModelAsync(InterfaceSTTProviderEnum providerId, STTProviderModelData modelData)
        {
            var filter = Builders<STTProviderData>.Filter.And(
                Builders<STTProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<STTProviderData>.Filter.ElemMatch(x => x.Models, m => m.Id == modelData.Id)
            );
            var update = Builders<STTProviderData>.Update.Set(x => x.Models.FirstMatchingElement(), modelData);
            return await _sttProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> DisableModelAsync(InterfaceSTTProviderEnum providerId, string modelId)
        {
            var filter = Builders<STTProviderData>.Filter.And(
                Builders<STTProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<STTProviderData>.Filter.ElemMatch(x => x.Models, m => m.Id == modelId)
            );
            var update = Builders<STTProviderData>.Update.Set(x => x.Models.FirstMatchingElement().DisabledAt, DateTime.UtcNow);
            return await _sttProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> RemoveModelAsync(InterfaceSTTProviderEnum providerId, string modelId)
        {
            var filter = Builders<STTProviderData>.Filter.Eq(x => x.Id, providerId);
            var update = Builders<STTProviderData>.Update.PullFilter(x => x.Models, m => m.Id == modelId);
            return await _sttProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<STTProviderModelData?> GetModelAsync(InterfaceSTTProviderEnum providerId, string modelId)
        {
            var provider = await _sttProviderCollection
                .Find(x => x.Id == providerId)
                .FirstOrDefaultAsync();

            return provider?.Models.FirstOrDefault(m => m.Id == modelId);
        }

        public async Task<UpdateResult> UpdateModelSupportedLanguagesAsync(
            InterfaceSTTProviderEnum providerId,
            string modelId,
            List<string> supportedLanguages)
        {
            var filter = Builders<STTProviderData>.Filter.And(
                Builders<STTProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<STTProviderData>.Filter.ElemMatch(x => x.Models, m => m.Id == modelId)
            );
            var update = Builders<STTProviderData>.Update
                .Set(x => x.Models.FirstMatchingElement().SupportedLanguages, supportedLanguages);
            return await _sttProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<STTProviderData?> GetProviderDataByIntegration(string integrationType)
        {
            var filter = Builders<STTProviderData>.Filter.Eq(x => x.IntegrationId, integrationType);
            return await _sttProviderCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<UpdateResult> UpdateProviderIntegrationFieldsAsync(
            InterfaceSTTProviderEnum providerId,
            List<ProviderFieldBase> integrationFields)
        {
            var filter = Builders<STTProviderData>.Filter.Eq(x => x.Id, providerId);
            var update = Builders<STTProviderData>.Update
                .Set(x => x.UserIntegrationFields, integrationFields);
            return await _sttProviderCollection.UpdateOneAsync(filter, update);
        }
    }
}