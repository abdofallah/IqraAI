using IqraCore.Entities.FlowApp;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.FlowApp
{
    public class FlowAppRepository
    {
        private readonly IMongoCollection<FlowAppData> _collection;
        private readonly ILogger<FlowAppRepository> _logger;

        private readonly string DatabaseName = "IqraFlowApp";
        private readonly string CollectionName = "FlowApps";

        public FlowAppRepository(IMongoClient client, ILogger<FlowAppRepository> logger)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase(DatabaseName);
            _collection = database.GetCollection<FlowAppData>(CollectionName);

            // Ensure unique index on AppKey
            var indexKeys = Builders<FlowAppData>.IndexKeys.Ascending(x => x.AppKey);
            var indexOptions = new CreateIndexOptions { Unique = true };
            _collection.Indexes.CreateOne(new CreateIndexModel<FlowAppData>(indexKeys, indexOptions));
        }

        public async Task<List<FlowAppData>> GetAllAppDataAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<FlowAppData?> GetAppDataAsync(string appKey)
        {
            return await _collection.Find(x => x.AppKey == appKey).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Updates the App-level permission (Disable/Enable).
        /// Upserts the record if it doesn't exist.
        /// </summary>
        public async Task<bool> UpdateAppPermissionAsync(string appKey, bool isDisabled, string? privateReason, string? publicReason)
        {
            var filter = Builders<FlowAppData>.Filter.Eq(x => x.AppKey, appKey);

            var update = Builders<FlowAppData>.Update
                .Set(x => x.DisabledAt, isDisabled ? DateTime.UtcNow : null)
                .Set(x => x.DisabledPrivateReason, isDisabled ? privateReason : null)
                .Set(x => x.DisabledPublicReason, isDisabled ? publicReason : null)
                .SetOnInsert(x => x.AppKey, appKey); // Ensure Key is set on insert

            var result = await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            return result.IsAcknowledged;
        }

        /// <summary>
        /// Updates permission for a specific Action inside an App.
        /// </summary>
        public async Task<bool> UpdateActionPermissionAsync(string appKey, string actionKey, bool isDisabled, string? privateReason, string? publicReason)
        {
            var filter = Builders<FlowAppData>.Filter.Eq(x => x.AppKey, appKey);

            var permission = new FlowItemPermission
            {
                DisabledAt = isDisabled ? DateTime.UtcNow : null,
                DisabledPrivateReason = isDisabled ? privateReason : null,
                DisabledPublicReason = isDisabled ? publicReason : null
            };

            // We use Dictionary notation for MongoDB update: "actionPermissions.ActionKey"
            var update = Builders<FlowAppData>.Update
                .Set(d => d.ActionPermissions[actionKey], permission)
                .SetOnInsert(x => x.AppKey, appKey);

            var result = await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            return result.IsAcknowledged;
        }

        /// <summary>
        /// Updates permission for a specific Fetcher inside an App.
        /// </summary>
        public async Task<bool> UpdateFetcherPermissionAsync(string appKey, string fetcherKey, bool isDisabled, string? privateReason, string? publicReason)
        {
            var filter = Builders<FlowAppData>.Filter.Eq(x => x.AppKey, appKey);

            var permission = new FlowItemPermission
            {
                DisabledAt = isDisabled ? DateTime.UtcNow : null,
                DisabledPrivateReason = isDisabled ? privateReason : null,
                DisabledPublicReason = isDisabled ? publicReason : null
            };

            var update = Builders<FlowAppData>.Update
                .Set(d => d.FetcherPermissions[fetcherKey], permission)
                .SetOnInsert(x => x.AppKey, appKey);

            var result = await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            return result.IsAcknowledged;
        }
    }
}