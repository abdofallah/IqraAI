using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using IqraCore.Entities.TTS;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.TTS
{
    public class TTSProviderRepository
    {
        private readonly string CollectionName = "TTSProvider";
        private readonly IMongoCollection<TTSProviderData> _ttsProviderCollection;

        public TTSProviderRepository(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _ttsProviderCollection = database.GetCollection<TTSProviderData>(CollectionName);
        }

        public async Task AddProviderAsync(TTSProviderData providerData)
        {
            await _ttsProviderCollection.InsertOneAsync(providerData);
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

        public async Task<UpdateResult> AddSpeakerAsync(InterfaceTTSProviderEnum providerId, TTSProviderSpeakerData speakerData)
        {
            var filter = Builders<TTSProviderData>.Filter.Eq(x => x.Id, providerId);
            var update = Builders<TTSProviderData>.Update.Push(x => x.Models, speakerData);
            return await _ttsProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> UpdateSpeakerAsync(InterfaceTTSProviderEnum providerId, TTSProviderSpeakerData speakerData)
        {
            var filter = Builders<TTSProviderData>.Filter.And(
                Builders<TTSProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<TTSProviderData>.Filter.ElemMatch(x => x.Models, s => s.Id == speakerData.Id)
            );

            var update = Builders<TTSProviderData>.Update.Set(x => x.Models.FirstMatchingElement(), speakerData);
            return await _ttsProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> DisableSpeakerAsync(InterfaceTTSProviderEnum providerId, string speakerId)
        {
            var filter = Builders<TTSProviderData>.Filter.And(
                Builders<TTSProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<TTSProviderData>.Filter.ElemMatch(x => x.Models, s => s.Id == speakerId)
            );

            var update = Builders<TTSProviderData>.Update
                .Set(x => x.Models.FirstMatchingElement().DisabledAt, DateTime.UtcNow);
            return await _ttsProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> EnableSpeakerAsync(InterfaceTTSProviderEnum providerId, string speakerId)
        {
            var filter = Builders<TTSProviderData>.Filter.And(
                Builders<TTSProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<TTSProviderData>.Filter.ElemMatch(x => x.Models, s => s.Id == speakerId)
            );

            var update = Builders<TTSProviderData>.Update
                .Set(x => x.Models.FirstMatchingElement().DisabledAt, null);
            return await _ttsProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<TTSProviderSpeakerData?> GetSpeakerAsync(InterfaceTTSProviderEnum providerId, string speakerId)
        {
            var provider = await _ttsProviderCollection
                .Find(x => x.Id == providerId)
                .FirstOrDefaultAsync();

            return provider?.Models.FirstOrDefault(s => s.Id == speakerId);
        }

        public async Task<UpdateResult> UpdateSpeakerLanguagesAsync(
            InterfaceTTSProviderEnum providerId,
            string speakerId,
            List<string> supportedLanguages,
            bool isMultilingual)
        {
            var filter = Builders<TTSProviderData>.Filter.And(
                Builders<TTSProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<TTSProviderData>.Filter.ElemMatch(x => x.Models, s => s.Id == speakerId)
            );

            var update = Builders<TTSProviderData>.Update
                .Set(x => x.Models.FirstMatchingElement().SupportedLanguages, supportedLanguages)
                .Set(x => x.Models.FirstMatchingElement().IsMultilingual, isMultilingual);

            return await _ttsProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> UpdateSpeakingStylesAsync(
            InterfaceTTSProviderEnum providerId,
            string speakerId,
            List<TTSProviderSpeakingStyleData> speakingStyles)
        {
            var filter = Builders<TTSProviderData>.Filter.And(
                Builders<TTSProviderData>.Filter.Eq(x => x.Id, providerId),
                Builders<TTSProviderData>.Filter.ElemMatch(x => x.Models, s => s.Id == speakerId)
            );

            var update = Builders<TTSProviderData>.Update
                .Set(x => x.Models.FirstMatchingElement().SpeakingStyles, speakingStyles);

            return await _ttsProviderCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> RemoveSpeakerAsync(InterfaceTTSProviderEnum providerId, string speakerId)
        {
            var filter = Builders<TTSProviderData>.Filter.Eq(x => x.Id, providerId);
            var update = Builders<TTSProviderData>.Update
                .PullFilter(x => x.Models, s => s.Id == speakerId);

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