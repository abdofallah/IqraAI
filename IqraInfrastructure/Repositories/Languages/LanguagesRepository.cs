using IqraCore.Entities.Languages;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Languages
{
    public class LanguagesRepository
    {
        private readonly ILogger<LanguagesRepository> _logger;
        private readonly IMongoCollection<LanguagesData> _languagesCollection;

        private readonly string DatabaseName = "IqraLanguages";
        private readonly string CollectionName = "Languages";

        public LanguagesRepository(ILogger<LanguagesRepository> logger, IMongoClient client)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase(DatabaseName);
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

        public async Task<List<LanguagesData>?> GetAllLanguagesList(bool withPrompts)
        {
            if (withPrompts)
            {
                return await _languagesCollection.Find(_ => true).ToListAsync();
            }
            else
            {
                var projection = Builders<LanguagesData>.Projection
                    .Exclude(d => d.Prompts);

                return await _languagesCollection.Find(_ => true).Project<LanguagesData>(projection).ToListAsync();
            }
        }

        public async Task<LanguagesData?> GetLanguageByCode(string languageCode, bool withPrompts)
        {
            var filter = Builders<LanguagesData>.Filter.Eq(d => d.Id, languageCode);

            if (withPrompts)
            {
                return await _languagesCollection.Find(filter).FirstOrDefaultAsync();
            }
            else
            {
                var projection = Builders<LanguagesData>.Projection
                    .Exclude(d => d.Prompts);

                return await _languagesCollection.Find(filter).Project<LanguagesData>(projection).FirstOrDefaultAsync();
            }
        }

        public async Task<bool> AddNewLanguage(LanguagesData newLanguagesData)
        {
            try
            {
                await _languagesCollection.InsertOneAsync(newLanguagesData);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add new language");
                return false;
            }
        }

        public async Task<bool> ReplaceLanguage(LanguagesData newLanguagesData)
        {
            var filter = Builders<LanguagesData>.Filter.Eq(d => d.Id, newLanguagesData.Id);

            var result = await _languagesCollection.ReplaceOneAsync(filter, newLanguagesData);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
    }
}
