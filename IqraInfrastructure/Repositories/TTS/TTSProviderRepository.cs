using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using IqraCore.Entities.TTS;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.TTS
{
    public class TTSProviderRepository
    {
        private readonly ILogger<TTSProviderRepository> _logger;
        private readonly IMongoCollection<TTSProviderData> _ttsProviderCollection;

        private readonly string DatabaseName = "IqraTTSProvider";
        private readonly string CollectionName = "TTSProvider";

        public TTSProviderRepository(ILogger<TTSProviderRepository> logger, IMongoClient client)
        {
            _logger = logger;

            var database = client.GetDatabase(DatabaseName);
            _ttsProviderCollection = database.GetCollection<TTSProviderData>(CollectionName);
        }

        public async Task<bool> AddProviderAsync(TTSProviderData providerData)
        {
            try
            {
                await _ttsProviderCollection.InsertOneAsync(providerData);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tts provider");
                return false;
            }
        }

        public async Task<TTSProviderData?> GetProviderAsync(InterfaceTTSProviderEnum id)
        {
            return await _ttsProviderCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<ReplaceOneResult> UpdateProviderAsync(TTSProviderData providerData)
        {
            var filter = Builders<TTSProviderData>.Filter.Eq(x => x.Id, providerData.Id);
            return await _ttsProviderCollection.ReplaceOneAsync(filter, providerData);
        }

        public async Task<List<TTSProviderData>> GetAllProvidersAsync()
        {
            return await _ttsProviderCollection.Find(_ => true).ToListAsync();
        }

        public async Task<List<TTSProviderData>?> GetProviderListAsync(int page, int pageSize)
        {
            return await _ttsProviderCollection
                .Find(_ => true)
                .SortByDescending(x => x.Id)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<UpdateResult> DisableProviderAsync(InterfaceTTSProviderEnum id)
        {
            var filter = Builders<TTSProviderData>.Filter.Eq(x => x.Id, id);
            var update = Builders<TTSProviderData>.Update.Set(x => x.DisabledAt, DateTime.UtcNow);
            return await _ttsProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> EnableProviderAsync(InterfaceTTSProviderEnum id)
        {
            var filter = Builders<TTSProviderData>.Filter.Eq(x => x.Id, id);
            var update = Builders<TTSProviderData>.Update.Set(x => x.DisabledAt, null);
            return await _ttsProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> AddModelAsync(InterfaceTTSProviderEnum providerId, TTSProviderModelData modelData)
        {
            var filter = Builders<TTSProviderData>.Filter.Eq(x => x.Id, providerId);
            var update = Builders<TTSProviderData>.Update.Push(x => x.Models, modelData);
            return await _ttsProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> UpdateModelAsync(InterfaceTTSProviderEnum providerId, TTSProviderModelData modelData)
        {
            var filter = Builders<TTSProviderData>.Filter.And(
                Builders<TTSProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<TTSProviderData>.Filter.ElemMatch(x => x.Models, s => s.Id == modelData.Id)
            );

            var update = Builders<TTSProviderData>.Update.Set(x => x.Models.FirstMatchingElement(), modelData);
            return await _ttsProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> UpdateProviderIntegrationFieldsAsync(
            InterfaceTTSProviderEnum providerId,
            List<ProviderFieldBase> integrationFields)
        {
            var filter = Builders<TTSProviderData>.Filter.Eq(x => x.Id, providerId);
            var update = Builders<TTSProviderData>.Update
                .Set(x => x.UserIntegrationFields, integrationFields);

            return await _ttsProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<TTSProviderData?> GetProviderDataByIntegration(string integrationId)
        {
            return await _ttsProviderCollection
                .Find(x => x.IntegrationId == integrationId)
                .FirstOrDefaultAsync();
        }

        public async Task<BulkWriteResult<TTSProviderData>> UpdateMultipleProvidersAsync(
            List<WriteModel<TTSProviderData>> operations)
        {
            return await _ttsProviderCollection.BulkWriteAsync(operations);
        }
    }
}