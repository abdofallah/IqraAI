using IqraCore.Entities.Languages;
using IqraInfrastructure.Repositories.App;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Languages
{
    public class LanguagesRepository
    {
        private readonly ILogger<LanguagesRepository> _logger;

        private readonly string CollectionName = "Languages";

        private readonly IMongoCollection<LanguagesData> _languagesCollection;

        public LanguagesRepository(ILogger<LanguagesRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase(databaseName);
            _languagesCollection = database.GetCollection<LanguagesData>(CollectionName);
        }

        public async Task<List<LanguagesData>?> GetLanguagesList(int page, int pageSize)
        {
            return await _languagesCollection
                .Find(_ => true)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<List<LanguagesData>?> GetAllLanguagesList()
        {
            return await _languagesCollection
                .Find(_ => true)
                .ToListAsync();
        }

        public async Task<LanguagesData?> GetLanguageByCode(string languageCode)
        {
            var filter = Builders<LanguagesData>.Filter.Eq(d => d.Id, languageCode);

            return await _languagesCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> AddNewLanguage(LanguagesData newLanguagesData)
        {
            await _languagesCollection.InsertOneAsync(newLanguagesData);

            return true;
        }

        public async Task<bool> ReplaceLanguage(LanguagesData newLanguagesData)
        {
            var filter = Builders<LanguagesData>.Filter.Eq(d => d.Id, newLanguagesData.Id);

            var result = await _languagesCollection.ReplaceOneAsync(filter, newLanguagesData);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
    }
}
