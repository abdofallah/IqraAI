using IqraCore.Entities.Archived;
using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.S3Storage;
using IqraInfrastructure.Helpers.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessAppRepository
    {
        private readonly ILogger<BusinessAppRepository> _logger;

        private readonly string CollectionName = "BusinessApp";
        private readonly string ArchivedCollectionName = "BusinessApp_archived";

        private readonly IMongoClient _client;
        private readonly IMongoCollection<BusinessApp> _businessAppCollection;
        private readonly IMongoCollection<ArchivedRepoObject<BusinessApp>> _businessAppArchivedCollection;

        public BusinessAppRepository(ILogger<BusinessAppRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;
            _client = client;

            IMongoDatabase database = client.GetDatabase(databaseName);
            _businessAppCollection = database.GetCollection<BusinessApp>(CollectionName);
            _businessAppArchivedCollection = database.GetCollection<ArchivedRepoObject<BusinessApp>>(ArchivedCollectionName);
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

        public Task AddBusinessAppAsync(BusinessApp businessApp, IClientSessionHandle mongoSession)
        {
            return _businessAppCollection.InsertOneAsync(mongoSession, businessApp);
        }

        public async Task<bool> DeleteBusinessAppAsync(long businessId)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessAppCollection.DeleteOneAsync(filter);
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessAppAsync(long businessId, UpdateDefinition<BusinessApp> updateDefinition)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessAppCollection.UpdateOneAsync(filter, updateDefinition);
            return result.IsAcknowledged;
        }

        public async Task<bool> ReplaceBusinessAppAsync(BusinessApp? businessApp)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessApp.Id);
            var result = await _businessAppCollection.ReplaceOneAsync(filter, businessApp);
            return result.IsAcknowledged;
        }

        public async Task<BusinessAppTool?> GetBusinessAppTool(long businessId, string selectedToolId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == selectedToolId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();

            if (result == null)
            {
                throw new Exception("Business not found");
            }

            return result.Tools.FirstOrDefault(t => t.Id == selectedToolId);
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

        public async Task<bool> MoveBusinessToArchivedAsync(long businessId, IClientSessionHandle session)
        {
            try
            {
                string businessIdString = businessId.ToString();

                var businessAppFilter = Builders<BusinessApp>.Filter.Eq(c => c.Id, businessId);
                var businessToArchive = await _businessAppCollection.Find(businessAppFilter).FirstOrDefaultAsync();

                if (businessToArchive == null)
                {
                    var archivedBusinessAppFilter = Builders<ArchivedRepoObject<BusinessApp>>.Filter.Eq(c => c.ObjectId, businessIdString);
                    var alreadyArchived = await _businessAppArchivedCollection.Find(archivedBusinessAppFilter).FirstOrDefaultAsync();
                    return alreadyArchived != null;
                }

                var businessAppArchive = new ArchivedRepoObject<BusinessApp>(businessIdString, businessToArchive);
                await _businessAppArchivedCollection.InsertOneAsync(session, businessAppArchive);
                await _businessAppCollection.DeleteOneAsync(session, businessAppFilter);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting business {BusinessId}: {Message}", businessId, ex.Message);
                return false;
            }
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

        public async Task<bool> AddCacheLinkToAudioCacheGroupEntry(long businessId, string groupId, string audioId, string language, BusinessAppCacheAudioCacheLink cacheLinkToAdd)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);

            var update = Builders<BusinessApp>.Update.AddToSet(
                $"Cache.AudioGroups.$[group].Audios.{language}.$[audio].GeneratedCacheLinks",
                cacheLinkToAdd
            );

            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("group._id", groupId)),
                new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("audio._id", audioId))
            };

            var options = new UpdateOptions { ArrayFilters = arrayFilters };

            var result = await _businessAppCollection.UpdateOneAsync(filter, update, options);

            return result.IsAcknowledged;
        }


        /**
         * 
         * Cache Tab
         * Embedding Group | Embedding Cache
         * 
        **/

        public async Task<bool> AddCacheEmbeddingGroup(long businessId, BusinessAppCacheEmbeddingGroup newEmbeddingGroup)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Cache.EmbeddingGroups, newEmbeddingGroup);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateEmbeddingGroupName(long businessId, string embeddingGroupId, string newEmbeddingGroupName)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.EmbeddingGroups, t => t.Id == embeddingGroupId)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Cache.EmbeddingGroups.FirstMatchingElement().Name, newEmbeddingGroupName);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddEmbeddingEntryToGroup(long businessId, string groupId, string language, BusinessAppCacheEmbedding newEmbedding)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.EmbeddingGroups, t => t.Id == groupId)
            );
            var update = Builders<BusinessApp>.Update.Push(b => b.Cache.EmbeddingGroups.FirstMatchingElement().Embeddings[language], newEmbedding);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateEmbeddingInGroup(long businessId, string groupId, string language, BusinessAppCacheEmbedding newEmbedding)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.EmbeddingGroups, g => g.Id == groupId)
            );
            var update = Builders<BusinessApp>.Update.Set(
                $"Cache.EmbeddingGroups.$.Embeddings.{language}",
                new BsonArray(new[] { newEmbedding.ToBsonDocument() })
            );
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result != null;
        }

        public async Task<bool> CheckCacheEmbeddingGroupExists(long businessId, string existingGroupId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.EmbeddingGroups, t => t.Id == existingGroupId)
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<bool> CheckCacheEmbeddingGroupEmbeddingExists(long businessId, string groupId, string language, string existingCacheId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.EmbeddingGroups, g =>
                    g.Id == groupId &&
                    g.Embeddings[language].Any(a => a.Id == existingCacheId)
                )
            );
            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        public async Task<List<BusinessAppCacheEmbeddingGroup>> FindCacheEmbeddingGroupsWithCacheQuery(long businessId, string language, string query)
        {
            var pipeline = _businessAppCollection.Aggregate()
                .Match(b => b.Id == businessId &&
                    b.Cache.EmbeddingGroups.Any(g =>
                        g.Embeddings.ContainsKey(language) &&
                        g.Embeddings[language].Any(e => e.Query == query)
                    )
                )
                .Unwind<BusinessApp, BsonDocument>(b => b.Cache.EmbeddingGroups)
                .Match(new BsonDocument
                {
                    { $"Cache.EmbeddingGroups.Embeddings.{language}.Query", query }
                })
                .Project(new BsonDocument
                {
                    { "_id", 0 },
                    { "Id", "$Cache.EmbeddingGroups.Id" },
                    { "Name", "$Cache.EmbeddingGroups.Name" },
                    {
                        "Embeddings",
                        new BsonDocument(language,
                            new BsonDocument("$filter",
                                new BsonDocument
                                {
                                    { "input", $"$Cache.EmbeddingGroups.Embeddings.{language}" },
                                    { "as", "emb" },
                                    { "cond", new BsonDocument("$eq", new BsonArray { "$$emb.Query", query }) }
                                }
                            )
                        )
                    }
                })
                .As<BusinessAppCacheEmbeddingGroup>();

            return await pipeline.ToListAsync();
        }

        public async Task<bool> AddCacheLinkToEmbeddingCacheGroupEntry(long businessId, string groupId, string embeddingId, string language, BusinessAppCacheEmbeddingCacheLink cacheLinkToAdd)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);

            var update = Builders<BusinessApp>.Update.AddToSet(
                $"Cache.EmbeddingGroups.$[group].Embeddings.{language}.$[embedding].GeneratedCacheLinks",
                cacheLinkToAdd
            );

            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("group.Id", groupId)),
                new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("embedding.Id", embeddingId))
            };

            var options = new UpdateOptions { ArrayFilters = arrayFilters };

            var result = await _businessAppCollection.UpdateOneAsync(filter, update, options);

            return result.IsAcknowledged;
        }

        /**
         * 
         * Integrations
         * 
        **/
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
         * Agents
         * 
        **/

        public async Task<bool> CheckAgentExists(long businessId, string existingAgentId)
        {
            var query = _businessAppCollection.AsQueryable()
                .Where(b => b.Id == businessId)
                .SelectMany(b => b.Agents)
                .AnyAsync(agent => agent.Id == existingAgentId);

            return await query;
        }

        public async Task<BusinessAppAgent?> GetAgentById(long businessId, string agentId)
        {
            var agentQuery = _businessAppCollection.AsQueryable()
                .Where(b => b.Id == businessId)
                .SelectMany(b => b.Agents)
                .Where(agent => agent.Id == agentId);

            return await agentQuery.FirstOrDefaultAsync();
        }

        public async Task<S3StorageFileLink?> GetAgentSettingsBackgroundAudioS3StorageLink(long businessId, string agentId)
        {
            var urlQuery = _businessAppCollection.AsQueryable()
                .Where(b => b.Id == businessId)
                .SelectMany(b => b.Agents)
                .Where(agent => agent.Id == agentId)
                .Select(agent => agent.Settings.BackgroundAudioS3StorageLink);

            return await urlQuery.FirstOrDefaultAsync();
        }

        public async Task<bool> AddAgent(long businessId, BusinessAppAgent agent)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.Not(
                    Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, a => a.Id == agent.Id)
                )
            );

            var update = Builders<BusinessApp>.Update.Push(b => b.Agents, agent);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateAgentDataExceptScripts(long businessId, BusinessAppAgent agent)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, g => g.Id == agent.Id)
            );

            var update = Builders<BusinessApp>.Update
                .Set(d => d.Agents.FirstMatchingElement().General, agent.General)
                .Set(d => d.Agents.FirstMatchingElement().Context, agent.Context)
                .Set(d => d.Agents.FirstMatchingElement().Personality, agent.Personality)
                .Set(d => d.Agents.FirstMatchingElement().Utterances, agent.Utterances)
                .Set(d => d.Agents.FirstMatchingElement().Interruptions, agent.Interruptions)
                .Set(d => d.Agents.FirstMatchingElement().KnowledgeBase, agent.KnowledgeBase)
                .Set(d => d.Agents.FirstMatchingElement().Integrations, agent.Integrations)
                .Set(d => d.Agents.FirstMatchingElement().Cache, agent.Cache)
                .Set(d => d.Agents.FirstMatchingElement().Settings, agent.Settings);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        // Agent Scripts
        public async Task<bool> CheckAgentScriptExists(long businessId, string agentId, string scriptId)
        {
            var query = _businessAppCollection.AsQueryable()
                .Where(b => b.Id == businessId)
                .SelectMany(b => b.Agents)
                .Where(agent => agent.Id == agentId)
                .SelectMany(agent => agent.Scripts)
                .AnyAsync(script => script.Id == scriptId);

            return await query;
        }

        public async Task<bool> AddAgentScript(long businessId, string agentId, BusinessAppAgentScript newScriptData)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, agent =>
                    agent.Id == agentId &&
                    !agent.Scripts.Any(script => script.Id == newScriptData.Id)
                )
            );

            var update = Builders<BusinessApp>.Update.Push(d => d.Agents.FirstMatchingElement().Scripts, newScriptData);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
        
        public async Task<bool> UpdateAgentScript(long businessId, string agentId, BusinessAppAgentScript updatedScriptData)
        {
            const string UpdateAgentScriptAgentPathIdentifier = "agentElem";
            const string UpdateAgentScriptScriptPathIdentifier = "scriptElem";
            const string UpdateAgentScriptUpdatePath = $"{nameof(BusinessApp.Agents)}.$[{UpdateAgentScriptAgentPathIdentifier}].{nameof(BusinessAppAgent.Scripts)}.$[{UpdateAgentScriptScriptPathIdentifier}]";

            try
            {
                var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents,
                        a => a.Id == agentId &&
                        a.Scripts.Any(s => s.Id == updatedScriptData.Id)
                    )
                );

                var update = Builders<BusinessApp>.Update.Set(UpdateAgentScriptUpdatePath, updatedScriptData);

                var arrayFilters = new List<ArrayFilterDefinition>
                {
                    TypeSafeArrayFilter.Create<BusinessAppAgent>(UpdateAgentScriptAgentPathIdentifier, agent => agent.Id == agentId),
                    TypeSafeArrayFilter.Create<BusinessAppAgentScript>(UpdateAgentScriptScriptPathIdentifier, script => script.Id == updatedScriptData.Id)
                };

                var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };

                var result = await _businessAppCollection.UpdateOneAsync(filter, update, updateOptions);
                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }     

        /**
        * 
        * Numbers
        * 
        **/

        public async Task<List<BusinessNumberData>> GetBusinessNumbers(long businessId)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var projection = Builders<BusinessApp>.Projection.Include(b => b.Numbers).Include(b => b.Id);
            var result = await _businessAppCollection.Find(filter).Project<BusinessApp>(projection).FirstOrDefaultAsync();

            if (result == null)
            {
                throw new Exception("Business not found");
            }

            return result.Numbers;
        }

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

            var projection = Builders<BusinessApp>.Projection
                .Include(b => b.Id)
                .Include(b => b.Numbers.FirstMatchingElement());

            var result = await _businessAppCollection.Find(filter).Project<BusinessApp>(projection).FirstOrDefaultAsync();
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

        /**
        * 
        * Routes
        * 
        **/

        public async Task<bool> CheckBusinessRouteExists(long businessId, string existingRouteId)
        {
            return await GetBusinessRoute(businessId, existingRouteId) != null;
        }

        public async Task<BusinessAppRoute?> GetBusinessRoute(long businessId, string existingRouteId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Routings, t => t.Id == existingRouteId)
            );
            var projection = Builders<BusinessApp>.Projection.Include(b => b.Routings).Include(b => b.Id);
            var result = await _businessAppCollection.Find(filter).Project<BusinessApp>(projection).FirstOrDefaultAsync();

            if (result == null) return null;

            return result.Routings.FirstOrDefault(t => t.Id == existingRouteId);
        }

        public async Task<bool> AddBusinessAppRoute(long businessId, BusinessAppRoute newBusinessAppRouteData)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Routings, newBusinessAppRouteData);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessAppRoute(long businessId, BusinessAppRoute newBusinessAppRouteData)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Routings, g => g.Id == newBusinessAppRouteData.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(
                $"Routings.$",
                new BsonDocument(newBusinessAppRouteData.ToBsonDocument())
            );
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessNumberRoute(long businessId, string numberId, string? routeId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Numbers, g => g.Id == numberId)
            );

            var update = Builders<BusinessApp>.Update.Set(
                $"Numbers.$.RouteId",
                routeId
            );
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /**
        * 
        * Knowledge Base
        * 
        **/

        public async Task<bool> AddKnowledgeBaseToArrayAsync(long businessId, BusinessAppKnowledgeBase kb, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.KnowledgeBases, kb);

            var result = session != null
                ? await _businessAppCollection.UpdateOneAsync(session, filter, update)
                : await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateKnowledgeBaseInArrayAsync(long businessId, BusinessAppKnowledgeBase kb, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.KnowledgeBases, k => k.Id == kb.Id)
            );

            var update = Builders<BusinessApp>.Update.Set("KnowledgeBases.$", kb);

            var result = session != null
                ? await _businessAppCollection.UpdateOneAsync(session, filter, update)
                : await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> RemoveKnowledgeBaseFromArrayAsync(long businessId, string kbId, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.PullFilter(b => b.KnowledgeBases, k => k.Id == kbId);

            var result = session != null
                ? await _businessAppCollection.UpdateOneAsync(session, filter, update)
                : await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddDocumentIdToKnowledgeBaseAsync(long businessId, string kbId, long documentId, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.KnowledgeBases, k => k.Id == kbId)
            );

            // Use the positional operator '$' to update the 'Documents' array of the matched knowledge base
            var update = Builders<BusinessApp>.Update.Push("KnowledgeBases.$.Documents", documentId);

            var result = session != null
                ? await _businessAppCollection.UpdateOneAsync(session, filter, update)
                : await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> RemoveDocumentIdFromKnowledgeBaseAsync(long businessId, string kbId, long documentId, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.KnowledgeBases, k => k.Id == kbId)
            );

            var update = Builders<BusinessApp>.Update.Pull("KnowledgeBases.$.Documents", documentId);

            var result = session != null
                ? await _businessAppCollection.UpdateOneAsync(session, filter, update)
                : await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<BusinessAppKnowledgeBase?> GetBusinessAppKnowledgeBaseAsync(long businessId, string existingKbId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.KnowledgeBases, k => k.Id == existingKbId)
            );

            var result = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            return result?.KnowledgeBases.FirstOrDefault(kb => kb.Id == existingKbId);
        }

        public async Task<bool> CheckKnowledgeBaseGroupExistsById(long businessId, string linkedGroupId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.KnowledgeBases, k => k.Id == linkedGroupId)
            );

            var projection = Builders<BusinessApp>.Projection
                .Include(b => b.Id)
                .Include(b => b.KnowledgeBases.FirstMatchingElement());
                
            var result = await _businessAppCollection.Find(filter).Project(projection).FirstOrDefaultAsync();
            return result != null;
        }

        /**
        * 
        * Telephony Campaign
        * 
        **/

        public async Task<BusinessAppTelephonyCampaign?> GetBusinessTelephonyCampaignById(long businessId, string existingCampaignId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.TelephonyCampaigns, k => k.Id == existingCampaignId)
            );

            var projection = Builders<BusinessApp>.Projection
                .Include(f => f.Id)
                .Include(f => f.TelephonyCampaigns.FirstMatchingElement());

            var result = await _businessAppCollection.Find(filter).Project<BusinessApp>(projection).FirstOrDefaultAsync();
            return result?.TelephonyCampaigns.FirstOrDefault();
        }

        public async Task<bool> AddBusinessAppTelephonyCampaign(long businessId, BusinessAppTelephonyCampaign newBusinessAppCampaignTelephonyData)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.TelephonyCampaigns, newBusinessAppCampaignTelephonyData);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessAppTelephonyCampaign(long businessId, BusinessAppTelephonyCampaign newBusinessAppCampaignTelephonyData)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.TelephonyCampaigns, t => t.Id == newBusinessAppCampaignTelephonyData.Id)
            );

            var update = Builders<BusinessApp>.Update
                .Set(b => b.TelephonyCampaigns.FirstMatchingElement(), newBusinessAppCampaignTelephonyData);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> CheckTelephonyCampaignExistsById(long businessId, string? telephonyCampaignIdValue)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.TelephonyCampaigns, k => k.Id == telephonyCampaignIdValue)
            );

            var project = Builders<BusinessApp>.Projection
                .Include(b => b.Id)
                .Include(b => b.TelephonyCampaigns.FirstMatchingElement());
                
            var result = await _businessAppCollection.Find(filter).Project<BusinessApp>(project).FirstOrDefaultAsync();
            return result != null;
        }

        /**
        * 
        * Web Campaign
        * 
        **/

        public async Task<BusinessAppWebCampaign?> GetBusinessWebCampaignById(long businessId, string existingWebCampaignId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.WebCampaigns, k => k.Id == existingWebCampaignId)
            );

            var projection = Builders<BusinessApp>.Projection
                .Include(f => f.Id)
                .Include(f => f.WebCampaigns.FirstMatchingElement());

            var result = await _businessAppCollection.Find(filter).Project<BusinessApp>(projection).FirstOrDefaultAsync();
            return result?.WebCampaigns.FirstOrDefault();
        }

        public async Task<bool> AddBusinessAppWebCampaign(long businessId, BusinessAppWebCampaign newBusinessAppCampaignWebData)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.WebCampaigns, newBusinessAppCampaignWebData);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessAppWebCampaign(long businessId, BusinessAppWebCampaign newBusinessAppCampaignWebData)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.WebCampaigns, t => t.Id == newBusinessAppCampaignWebData.Id)
            );

            var update = Builders<BusinessApp>.Update
                .Set(b => b.WebCampaigns.FirstMatchingElement(), newBusinessAppCampaignWebData);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> CheckWebCampaignExistsById(long businessId, string? telephonyCampaignIdValue)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.WebCampaigns, k => k.Id == telephonyCampaignIdValue)
            );

            var project = Builders<BusinessApp>.Projection
                .Include(b => b.Id)
                .Include(b => b.WebCampaigns.FirstMatchingElement());

            var result = await _businessAppCollection.Find(filter).Project<BusinessApp>(project).FirstOrDefaultAsync();
            return result != null;
        }

        /**
        * 
        * Post Analysis
        * 
        **/

        public async Task<BusinessAppPostAnalysis?> GetBusinessPostAnalysisTemplateById(long businessId, string templateId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.PostAnalysis, t => t.Id == templateId)
            );

            var businessApp = await _businessAppCollection.Find(filter).FirstOrDefaultAsync();
            if (businessApp == null) return null;

            return businessApp.PostAnalysis.FirstOrDefault(t => t.Id == templateId);
        }

        public async Task<bool> AddBusinessAppPostAnalysisTemplate(long businessId, BusinessAppPostAnalysis newTemplate)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.PostAnalysis, newTemplate);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessAppPostAnalysisTemplate(long businessId, BusinessAppPostAnalysis updatedTemplate)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.PostAnalysis, t => t.Id == updatedTemplate.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.PostAnalysis.FirstMatchingElement(), updatedTemplate);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }
    }
}