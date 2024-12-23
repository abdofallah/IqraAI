using IqraCore.Entities.Integrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Integrations
{
    public class IntegrationsRepository
    {
        private readonly string CollectionName = "Integrations";
        private readonly IMongoCollection<IntegrationData> _integrationsCollection;

        public IntegrationsRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _integrationsCollection = database.GetCollection<IntegrationData>(CollectionName);
        }

        public async Task<bool> IsIntegrationIdUniqueAsync(string id)
        {
            var filter = Builders<IntegrationData>.Filter.Eq(x => x.Id, id);
            return !await _integrationsCollection.Find(filter).AnyAsync();
        }

        public async Task AddIntegrationAsync(IntegrationData integrationData)
        {
            await _integrationsCollection.InsertOneAsync(integrationData);
        }

        public async Task<IntegrationData?> GetIntegrationAsync(string id)
        {
            return await _integrationsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<DateTime?> GetIntegrationDisabledAtAsync(string id)
        {
            var result = await _integrationsCollection
                .Find(x => x.Id == id)
                .Project(x => x.DisabledAt)
                .FirstOrDefaultAsync();
            return result;
        }

        public async Task<UpdateResult> UpdateIntegrationAsync(IntegrationData integrationData)
        {
            var filter = Builders<IntegrationData>.Filter.Eq(x => x.Id, integrationData.Id);
            var update = Builders<IntegrationData>.Update
                .Set(x => x.Name, integrationData.Name)
                .Set(x => x.Description, integrationData.Description)
                .Set(x => x.DisabledAt, integrationData.DisabledAt)
                .Set(x => x.Logo, integrationData.Logo)
                .Set(x => x.Type, integrationData.Type)
                .Set(x => x.Fields, integrationData.Fields)
                .Set(x => x.Help, integrationData.Help);

            return await _integrationsCollection.UpdateOneAsync(filter, update);
        }

        public async Task<DeleteResult> DeleteIntegrationAsync(string id)
        {
            return await _integrationsCollection.DeleteOneAsync(x => x.Id == id);
        }

        public async Task<List<IntegrationData>> GetAllIntegrationsAsync()
        {
            return await _integrationsCollection.Find(_ => true).ToListAsync();
        }

        public async Task<List<IntegrationData>> GetIntegrationsListAsync(int page, int pageSize)
        {
            return await _integrationsCollection
                .Find(_ => true)
                .SortBy(x => x.Id)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<bool> IntegrationExistsAsync(string id)
        {
            var filter = Builders<IntegrationData>.Filter.Eq(x => x.Id, id);
            return await _integrationsCollection.Find(filter).AnyAsync();
        }

        public async Task<UpdateResult> UpdateIntegrationFieldsAsync(string integrationId, List<IntegrationFieldData> fields)
        {
            var filter = Builders<IntegrationData>.Filter.Eq(x => x.Id, integrationId);
            var update = Builders<IntegrationData>.Update.Set(x => x.Fields, fields);
            return await _integrationsCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> UpdateIntegrationLogoAsync(string integrationId, string logoPath)
        {
            var filter = Builders<IntegrationData>.Filter.Eq(x => x.Id, integrationId);
            var update = Builders<IntegrationData>.Update.Set(x => x.Logo, logoPath);
            return await _integrationsCollection.UpdateOneAsync(filter, update);
        }

        public async Task<UpdateResult> UpdateIntegrationTypesAsync(string integrationId, List<string> types)
        {
            var filter = Builders<IntegrationData>.Filter.Eq(x => x.Id, integrationId);
            var update = Builders<IntegrationData>.Update.Set(x => x.Type, types);
            return await _integrationsCollection.UpdateOneAsync(filter, update);
        }

        public async Task<long> GetTotalIntegrationsCountAsync()
        {
            return await _integrationsCollection.CountDocumentsAsync(FilterDefinition<IntegrationData>.Empty);
        }

        // Helper methods for searching and filtering
        public async Task<List<IntegrationData>> SearchIntegrationsAsync(string searchTerm, int page, int pageSize)
        {
            var filter = Builders<IntegrationData>.Filter.Or(
                Builders<IntegrationData>.Filter.Regex(x => x.Name, new BsonRegularExpression(searchTerm, "i")),
                Builders<IntegrationData>.Filter.Regex(x => x.Description, new BsonRegularExpression(searchTerm, "i"))
            );

            return await _integrationsCollection
                .Find(filter)
                .SortBy(x => x.Id)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<List<IntegrationData>> GetIntegrationsByTypeAsync(string type, int page, int pageSize)
        {
            var filter = Builders<IntegrationData>.Filter.AnyEq(x => x.Type, type);

            return await _integrationsCollection
                .Find(filter)
                .SortBy(x => x.Id)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        // Methods for checking field usage/validation
        public async Task<bool> IsFieldIdUniqueAsync(string integrationId, string fieldId)
        {
            var filter = Builders<IntegrationData>.Filter.And(
                Builders<IntegrationData>.Filter.Ne(x => x.Id, integrationId),
                Builders<IntegrationData>.Filter.ElemMatch(x => x.Fields, f => f.Id == fieldId)
            );

            return !await _integrationsCollection.Find(filter).AnyAsync();
        }

        public async Task<UpdateResult> ClearIntegrationFieldsAsync(string integrationId)
        {
            var filter = Builders<IntegrationData>.Filter.Eq(x => x.Id, integrationId);
            var update = Builders<IntegrationData>.Update.Set(x => x.Fields, new List<IntegrationFieldData>());
            return await _integrationsCollection.UpdateOneAsync(filter, update);
        }

        // Bulk operations for admin purposes
        public async Task<BulkWriteResult<IntegrationData>> BulkUpdateIntegrationsAsync(List<IntegrationData> integrations)
        {
            var updates = integrations.Select(integration =>
                new ReplaceOneModel<IntegrationData>(
                    Builders<IntegrationData>.Filter.Eq(x => x.Id, integration.Id),
                    integration)
                {
                    IsUpsert = false
                }
            );

            return await _integrationsCollection.BulkWriteAsync(updates);
        }

        public async Task<IntegrationData?> GetIntegrationById(string integrationId)
        {
            var filter = Builders<IntegrationData>.Filter.Eq(x => x.Id, integrationId);
            return await _integrationsCollection.Find(filter).FirstOrDefaultAsync();
        }
    }
}
