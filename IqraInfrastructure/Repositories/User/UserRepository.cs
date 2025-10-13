using IqraCore.Entities.User;
using IqraCore.Entities.User.Billing;
using IqraCore.Entities.User.Notifcation;
using IqraInfrastructure.Helpers.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.User
{
    public class UserRepository
    {
        private readonly ILogger<UserRepository> _logger;

        private readonly string CollectionName = "Users";
        private readonly IMongoCollection<UserData> _usersCollection;

        public UserRepository(ILogger<UserRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;

            IMongoDatabase database = client.GetDatabase(databaseName);
            _usersCollection = database.GetCollection<UserData>(CollectionName);
        }

        public UserRepository(IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<UserData>(CollectionName);
        }

        public async Task<bool> AddUserAsync(UserData user)
        {
            await _usersCollection.InsertOneAsync(user);
            return true;
        }

        public async Task<UserData?> GetFullUserByEmail(string email)
        {
            var filter = Builders<UserData>.Filter.Eq(b => b.Email, email);
            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<UserData?> GetUserDataForLoginValidation(string email)
        {
            var filter = Builders<UserData>.Filter.Eq(b => b.Email, email);

            var project = Builders<UserData>.Projection
                .Include(b => b.Email)
                .Include(b => b.PasswordSHA)
                .Include(b => b.VerifyEmailToken)
                .Include(b => b.Permission);

            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<UserData?> GetUserDataForResetPasswordValidation(string email)
        {
            var filter = Builders<UserData>.Filter.Eq(b => b.Email, email);

            var project = Builders<UserData>.Projection
                .Include(b => b.Email)
                .Include(b => b.ResetPasswordTokens)
                .Include(b => b.VerifyEmailToken)
                .Include(b => b.Permission);

            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<UserData?> GetUserDataForRequestResetPasswordValiation(string email)
        {
            var filter = Builders<UserData>.Filter.Eq(b => b.Email, email);

            var project = Builders<UserData>.Projection
                .Include(b => b.Email)
                .Include(b => b.VerifyEmailToken)
                .Include(b => b.Permission);

            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateUser(string email, UpdateDefinition<UserData> updateDefinition)
        {
            var filter = Builders<UserData>.Filter.Eq(b => b.Email, email);
            return await UpdateUser(filter, updateDefinition);
        }

        public async Task<bool> UpdateUser(FilterDefinition<UserData> filter, UpdateDefinition<UserData> updateDefinition)
        {
            var result = await _usersCollection.UpdateOneAsync(filter, updateDefinition);
            return result.IsAcknowledged && result.ModifiedCount != 0;
        }

        public async Task<bool> UpdateUser(string email, UpdateDefinition<UserData> updateDefinition, IClientSessionHandle session)
        {
            var filter = Builders<UserData>.Filter.Eq(b => b.Email, email);
            var result = await _usersCollection.UpdateOneAsync(session, filter, updateDefinition);
            return result.IsAcknowledged && result.ModifiedCount != 0;
        }

        public async Task<bool> UpdateUser(FilterDefinition<UserData> filter, UpdateDefinition<UserData> updateDefinition, IClientSessionHandle session)
        {
            var result = await _usersCollection.UpdateOneAsync(session, filter, updateDefinition);
            return result.IsAcknowledged && result.ModifiedCount != 0;
        }

        public Task<List<UserData>> GetUsersAsync()
        {
            return _usersCollection.Find(_ => true).ToListAsync();
        }

        public Task<List<UserData>> GetUsersAsync(FilterDefinition<UserData> filter)
        {
            return _usersCollection.Find(filter).ToListAsync();
        }

        public Task<List<UserData>> GetUsersAsync(int page, int pageSize)
        {
            return _usersCollection.Find(_ => true).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }

        public async Task<UserData?> GetUserByEmailHashAsync(string emailHash)
        {
            var filter = Builders<UserData>.Filter.Eq(u => u.EmailHash, emailHash);
            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> CheckUserExistsByEmail(string email)
        {
            var filter = Builders<UserData>.Filter.Eq(u => u.Email, email);
            return await _usersCollection.Find(filter).AnyAsync();
        }

        public async Task<bool> CheckUserIsAdmin(string email)
        {
            var filter = Builders<UserData>.Filter.And(
                Builders<UserData>.Filter.Eq(u => u.Email, email),
                Builders<UserData>.Filter.Eq(u => u.Permission.IsAdmin, true)
            );

            return await _usersCollection.Find(filter).AnyAsync();
        }

        public async Task<UserBillingData?> GetUserBillingData(string email)
        {
            var query = _usersCollection.AsQueryable()
                .Where(u => u.Email == email)
                .Select(u => u.Billing)
                .FirstOrDefaultAsync();

            return await query;
        }

        public async Task<bool> TryIncrementConcurrencyUsageAsync(string userEmail, string featureKey, long maxConcurrency, UserBillingCycleConcurrencyFeatureUsage usageItem)
        {
            string arrayFieldPath = $"Billing.CurrentCycleUsage.CurrentConcurrencyFeatureUsage.{featureKey}";

            var filterBuilder = Builders<UserData>.Filter;

            var userFilter = filterBuilder.Eq(u => u.Email, userEmail);

            var concurrencyLimitFilter = filterBuilder.Not(filterBuilder.Exists($"{arrayFieldPath}.{maxConcurrency - 1}"));

            var finalFilter = filterBuilder.And(userFilter, concurrencyLimitFilter);
            var update = Builders<UserData>.Update.Push(arrayFieldPath, usageItem);
            var result = await _usersCollection.UpdateOneAsync(finalFilter, update);

            return result.IsAcknowledged && result.ModifiedCount != 0;
        }

        public async Task<bool> DecrementConcurrencyUsageAsync(string userEmail, string featureKey, long businessId, object parentReference, object? childReference)
        {
            string arrayFieldPath = $"Billing.CurrentCycleUsage.CurrentConcurrencyFeatureUsage.{featureKey}";

            var userFilter = Builders<UserData>.Filter.Eq(u => u.Email, userEmail);

            var update = Builders<UserData>.Update.PullFilter(
                arrayFieldPath,
                Builders<UserBillingCycleConcurrencyFeatureUsage>.Filter.And(
                    Builders<UserBillingCycleConcurrencyFeatureUsage>.Filter.Eq(i => i.BusinessId, businessId),
                    Builders<UserBillingCycleConcurrencyFeatureUsage>.Filter.Eq(i => i.ParentReference, parentReference),
                    Builders<UserBillingCycleConcurrencyFeatureUsage>.Filter.Eq(i => i.ChildReference, childReference)
                )
            );

            var result = await _usersCollection.UpdateOneAsync(userFilter, update);
            return result.IsAcknowledged && result.ModifiedCount != 0;
        }

        public async Task<bool> TrySetAddonRenewalInProgressAsync(string userEmail, string addonId)
        {
            var filter = Builders<UserData>.Filter.And(
                Builders<UserData>.Filter.Eq(u => u.Email, userEmail),
                Builders<UserData>.Filter.ElemMatch(
                    u => u.Billing.ActiveFeatureAddons,
                    addon => addon.Id == addonId && !addon.IsRenewInProgress
                )
            );

            var update = Builders<UserData>.Update.Set(d => d.Billing.ActiveFeatureAddons.FirstMatchingElement().IsRenewInProgress, true);

            var result = await _usersCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount != 0;
        }

        public async Task<UserBillingFeatureAddon?> GetUserBillingFeatureAddonAsync(string userEmail, string addonId)
        {
            var query = _usersCollection.AsQueryable()
                .Where(u => (u.Email == userEmail && u.Billing.ActiveFeatureAddons.Any(a => a.Id == addonId)))
                .Select(u => u.Billing.ActiveFeatureAddons.FirstOrDefault());

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> UserHasAnyPrimaryPaymentMethod(string userEmail)
        {
            var query = _usersCollection.AsQueryable()
                .Where(u => (u.Email == userEmail && u.PaymentMethods.Any(pm => pm.IsPrimary)))
                .AnyAsync();

            return await query;
        }

        public async Task<bool> AddUserNotification(string userEmail, UserNotificationData userNotificationData)
        {
            var filter = Builders<UserData>.Filter.Eq(u => u.Email, userEmail);
            var update = Builders<UserData>.Update.Push(u => u.Notifications, userNotificationData);
            var result = await _usersCollection.UpdateOneAsync(filter, update);

            return result.IsAcknowledged && result.ModifiedCount != 0;
        }

        public async Task<bool> SetNotificationAsRead(string userEmail, string notificationId)
        {
            var filter = Builders<UserData>.Filter.And(
                Builders<UserData>.Filter.Eq(u => u.Email, userEmail),
                Builders<UserData>.Filter.ElemMatch(u => u.Notifications, n => (n.Id == notificationId && n.ReadOn != null))
            );

            var update = Builders<UserData>.Update.Set(d => d.Notifications.FirstMatchingElement().ReadOn, DateTime.UtcNow);

            var result = await _usersCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount != 0;
        }

        public async Task<bool> SetNotificationActionAsClicked(string userEmail, string notificationId, string actionId)
        {
            const string notificationIdentifier = "notificationElem";
            const string actionIdentifier = "actionElem";
            const string updatePath = $"{nameof(UserData.Notifications)}.$[{notificationIdentifier}].{nameof(UserNotificationData.Actions)}.$[{actionIdentifier}].{nameof(UserNotificationActionData.ClickedOn)}";

            var filter = Builders<UserData>.Filter.And(
                Builders<UserData>.Filter.Eq(u => u.Email, userEmail),
                Builders<UserData>.Filter.ElemMatch(u => u.Notifications,
                    n => n.Id == notificationId && n.Actions.Any(a => a.Id == actionId && a.ClickedOn == null)
                )
            );
            var update = Builders<UserData>.Update.Set(updatePath, DateTime.UtcNow);
            var arrayFilters = new List<ArrayFilterDefinition>
            {
                TypeSafeArrayFilter.Create<UserNotificationData>(notificationIdentifier, notif => notif.Id == notificationId),
                TypeSafeArrayFilter.Create<UserNotificationActionData>(actionIdentifier, action => action.Id == actionId)
            };
            var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };

            var result = await _usersCollection.UpdateOneAsync(filter, update, updateOptions);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<List<UserNotificationData>> GetUnreadNotifications(string userEmail)
        {
            var filter = Builders<UserData>.Filter.Eq(u => u.Email, userEmail);

            var projection = Builders<UserData>.Projection.Expression(u =>
                u.Notifications.Where(n => n.ReadOn == null)
            );

            var unreadNotifications = await _usersCollection
                .Find(filter)
                .Project(projection)
                .FirstOrDefaultAsync();

            return unreadNotifications.ToList() ?? new List<UserNotificationData>();
        }
    }
}
