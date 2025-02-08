using IqraCore.Entities.Business;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Text.RegularExpressions;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessAppRepository
    {
        private readonly string CollectionName = "BusinessApp";

        private readonly IMongoCollection<BusinessApp> _businessAppCollection;

        public BusinessAppRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _businessAppCollection = database.GetCollection<BusinessApp>(CollectionName);
        }

        public Task<List<BusinessApp>> GetBusinessesAppAsync()
        {
            return _businessAppCollection.Find(_ => true).ToListAsync();
        }

        public Task<BusinessApp?> GetBusinessAppAsync(long businessId)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            return _businessAppCollection.Find(filter).FirstOrDefaultAsync();
        }

        public Task AddBusinessAppAsync(BusinessApp businessApp)
        {
            return _businessAppCollection.InsertOneAsync(businessApp);
        }

        public async Task<bool> DeleteBusinessAppAsync(long businessId)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessAppCollection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public async Task<bool> UpdateBusinessAppAsync(long businessId, UpdateDefinition<BusinessApp> updateDefinition)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessAppCollection.UpdateOneAsync(filter, updateDefinition);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> ReplaceBusinessAppAsync(BusinessApp? businessApp)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessApp.Id);
            var result = await _businessAppCollection.ReplaceOneAsync(filter, businessApp);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> CheckBusinessAppToolExists(long businessId, string toolId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == toolId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> UpdateBusinessAppTool(long businessId, BusinessAppTool newBusinessAppToolData)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                 Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                 Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == newBusinessAppToolData.Id)
            );
                
            var update = Builders<BusinessApp>.Update.Set(b => b.Tools.FirstMatchingElement(), newBusinessAppToolData);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddBusinessAppTool(long businessId, BusinessAppTool newBusinessAppToolData)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Tools, newBusinessAppToolData);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> CheckBusinessAppBranchExists(long businessId, string branchId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Context.Branches, t => t.Id == branchId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> UpdateBusinessContextBranding(long businessId, BusinessAppContextBranding newBusinessContextBranding)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Set(b => b.Context.Branding, newBusinessContextBranding);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddBusinessContextBranch(long businessId, BusinessAppContextBranch newBusinessContextBranch)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Context.Branches, newBusinessContextBranch);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessContextBranch(long businessId, BusinessAppContextBranch newBusinessContextBranch)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Context.Branches, t => t.Id == newBusinessContextBranch.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Context.Branches.FirstMatchingElement(), newBusinessContextBranch);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> CheckBusinessAppContextServiceExists(long businessId, string exisitingServiceId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Context.Services, t => t.Id == exisitingServiceId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> CheckBusinessAppProductExists(long businessId, string productId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Context.Products, t => t.Id == productId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> AddBusinessContextService(long businessId, BusinessAppContextService newBusinessContextService)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Context.Services, newBusinessContextService);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessContextService(long businessId, BusinessAppContextService newBusinessContextService)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Context.Services, t => t.Id == newBusinessContextService.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Context.Services.FirstMatchingElement(), newBusinessContextService);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddBusinessContextProduct(long businessId, BusinessAppContextProduct newBusinessContextProduct)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Context.Products, newBusinessContextProduct);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessContextProduct(long businessId, BusinessAppContextProduct newBusinessContextProduct)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Context.Products, t => t.Id == newBusinessContextProduct.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Context.Products.FirstMatchingElement(), newBusinessContextProduct);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /**
         * 
         * Cache Tab
         * Message Group | Message Cache
         * 
        **/

        public async Task<bool> AddCacheMessageGroup(long businessId, BusinessAppCacheMessageGroup newMessageGroup)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Cache.MessageGroups, newMessageGroup);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateMessageGroupName(long businessId, string messageGroupId, string newMessageGroupName)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.MessageGroups, t => t.Id == messageGroupId)
            );

            var update = Builders<BusinessApp>.Update.Set(b => b.Cache.MessageGroups.FirstMatchingElement().Name, newMessageGroupName);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddMessageToGroup(long businessId, string groupId, string language, BusinessAppCacheMessage newMessage)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.MessageGroups, t => t.Id == groupId)
            );
            var update = Builders<BusinessApp>.Update.Push(b => b.Cache.MessageGroups.FirstMatchingElement().Messages[language], newMessage);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateMessageInGroup(long businessId, string groupId, string language, BusinessAppCacheMessage newMessage)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.MessageGroups, g => g.Id == groupId)
            );

            var update = Builders<BusinessApp>.Update.Set(
                $"Cache.MessageGroups.$.Messages.{language}",
                new BsonArray(new[] { newMessage.ToBsonDocument() })
            );

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result != null;
        }

        public async Task<bool> CheckCacheMessageGroupExists(long businessId, string existingGroupId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.MessageGroups, t => t.Id == existingGroupId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> CheckCacheMessageGroupMessageExists(long businessId, string groupId, string language, string existingCacheId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.MessageGroups, g =>
                    g.Id == groupId &&
                    g.Messages[language].Any(m => m.Id == existingCacheId)
                )
            );

            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        /**
         * 
         * Cache Tab
         * Audio Group | Audio Cache
         * 
        **/

        public async Task<bool> AddCacheAudioGroup(long businessId, BusinessAppCacheAudioGroup newAudioGroup)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Cache.AudioGroups, newAudioGroup);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateAudioGroupName(long businessId, string audioGroupId, string newAudioGroupName)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.AudioGroups, t => t.Id == audioGroupId)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Cache.AudioGroups.FirstMatchingElement().Name, newAudioGroupName);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddAudioToGroup(long businessId, string groupId, string language, BusinessAppCacheAudio newAudio)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.AudioGroups, t => t.Id == groupId)
            );
            var update = Builders<BusinessApp>.Update.Push(b => b.Cache.AudioGroups.FirstMatchingElement().Audios[language], newAudio);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateAudioInGroup(long businessId, string groupId, string language, BusinessAppCacheAudio newAudio)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.AudioGroups, g => g.Id == groupId)
            );
            var update = Builders<BusinessApp>.Update.Set(
                $"Cache.AudioGroups.$.Audios.{language}",
                new BsonArray(new[] { newAudio.ToBsonDocument() })
            );
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result != null;
        }

        public async Task<bool> CheckCacheAudioGroupExists(long businessId, string existingGroupId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.AudioGroups, t => t.Id == existingGroupId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> CheckCacheAudioGroupAudioExists(long businessId, string groupId, string language, string existingCacheId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.AudioGroups, g =>
                    g.Id == groupId &&
                    g.Audios[language].Any(a => a.Id == existingCacheId)
                )
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<BusinessAppIntegration?> getBusinessIntegrationById(long businessId, string currentIntegrationId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, t => t.Id == currentIntegrationId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result?.Integrations.FirstOrDefault(t => t.Id == currentIntegrationId);
        }

        public async Task<bool> AddBusinessIntegration(long businessId, BusinessAppIntegration newIntegration)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Integrations, newIntegration);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessIntegration(long businessId, BusinessAppIntegration newIntegration)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, g => g.Id == newIntegration.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(
                $"Integrations.$",
                new BsonDocument(newIntegration.ToBsonDocument())
            );
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        /**
         * 
         * Agents Tab
         * 
        **/

        public async Task<bool> CheckAgentExists(long businessId, string existingAgentId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, t => t.Id == existingAgentId)
            );

            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> AddAgent(long businessId, BusinessAppAgent agent)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Agents, agent);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateAgent(long businessId, BusinessAppAgent agent)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, g => g.Id == agent.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(
                $"Agents.$",
                new BsonDocument(agent.ToBsonDocument())
            );
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<BusinessAppAgent?> GetAgentById(long businessId, string agentId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, t => t.Id == agentId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result?.Agents.FirstOrDefault(t => t.Id == agentId);
        }

        public async Task<bool> AddAgentScript(long businessId, string agentId, BusinessAppAgentScript newScriptData)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, a => a.Id == agentId)
            );

            var update = Builders<BusinessApp>.Update.Push(
                "Agents.$.Scripts",
                newScriptData
            );

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateAgentScript(long businessId, string agentId, BusinessAppAgentScript updatedScriptData)
        {
            // Simpler filter - we just need to match the business ID
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);

            // Update with proper positional operator syntax
            var update = Builders<BusinessApp>.Update.Set(
                "Agents.$[agentElem].Scripts.$[scriptElem]",
                updatedScriptData
            );

            // Array filters with correct syntax
            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument("agentElem._id", agentId)
                ),
                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument("scriptElem._id", updatedScriptData.Id)
                )
            };

            var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };

            var result = await _businessAppCollection.UpdateOneAsync(filter, update, updateOptions);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> CheckAgentScriptExists(long businessId, string agentId, string scriptId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, t => t.Id == agentId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents.FirstMatchingElement().Scripts, t => t.Id == scriptId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        /**
        * 
        * Numbers Tab
        * 
        **/

        public async Task<BusinessNumberData?> GetBusinessNumberById(long businessId, string numberId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Numbers, t => t.Id == numberId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result?.Numbers.FirstOrDefault(t => t.Id == numberId);
        }

        public async Task<bool> CheckBusinessNumberExistsByNumber(string numberCountryCode, string phoneNumber, long businessId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Numbers, t => t.CountryCode == numberCountryCode && t.Number == phoneNumber)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> CheckBusinessNumberExistsById(string exisitingNumberId, long businessId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Numbers, t => t.Id == exisitingNumberId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> AddBusinessNumber(long businessId, BusinessNumberData newNumberData)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Numbers, newNumberData);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessNumber(long businessId, BusinessNumberData newNumberData)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Numbers, g => g.Id == newNumberData.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(
                $"Numbers.$",
                new BsonDocument(newNumberData.ToBsonDocument())
            );
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
    }
}