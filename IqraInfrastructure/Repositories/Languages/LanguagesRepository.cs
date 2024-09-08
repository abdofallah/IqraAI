using IqraCore.Entities.Languages;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Languages
{
    public class LanguagesRepository
    {
        private readonly string CollectionName = "Languages";

        private readonly IMongoCollection<LanguagesData> _languagesCollection;

        public LanguagesRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
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
    }
}
