using IqraCore.Entities.Archived;
using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.S3Storage;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Scriban;

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
            return result.IsAcknowledged;
        }

        public async Task<bool> AddBusinessContextBranch(long businessId, BusinessAppContextBranch newBusinessContextBranch)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Context.Branches, newBusinessContextBranch);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessContextBranch(long businessId, BusinessAppContextBranch newBusinessContextBranch)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Context.Branches, t => t.Id == newBusinessContextBranch.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Context.Branches.FirstMatchingElement(), newBusinessContextBranch);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
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
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessContextService(long businessId, BusinessAppContextService newBusinessContextService)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Context.Services, t => t.Id == newBusinessContextService.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Context.Services.FirstMatchingElement(), newBusinessContextService);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddBusinessContextProduct(long businessId, BusinessAppContextProduct newBusinessContextProduct)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Context.Products, newBusinessContextProduct);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessContextProduct(long businessId, BusinessAppContextProduct newBusinessContextProduct)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Context.Products, t => t.Id == newBusinessContextProduct.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Context.Products.FirstMatchingElement(), newBusinessContextProduct);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
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
         * Tool Tab
         * 
        **/

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

        public async Task<bool> UpdateBusinessAppToolExceptReferences(long businessId, BusinessAppTool newBusinessAppToolData)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                 Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                 Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == newBusinessAppToolData.Id)
            );

            var update = Builders<BusinessApp>.Update
                .Set(b => b.Tools.FirstMatchingElement().General, newBusinessAppToolData.General)
                .Set(b => b.Tools.FirstMatchingElement().Configuration, newBusinessAppToolData.Configuration)
                .Set(b => b.Tools.FirstMatchingElement().Response, newBusinessAppToolData.Response)
                .Set(b => b.Tools.FirstMatchingElement().Audio, newBusinessAppToolData.Audio);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddBusinessAppTool(long businessId, BusinessAppTool newBusinessAppToolData)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Tools, newBusinessAppToolData);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> DeleteBusinessAppTool(long businessId, string toolId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == toolId)
            );
            var update = Builders<BusinessApp>.Update.PullFilter(b => b.Tools, t => t.Id == toolId);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddScriptExecuteCustomToolNodeReferenceToCustomTool(long businessId, string toolId, BusinessAppToolScriptExecuteCustomToolNodeReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, a => a.Id == toolId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.Tools.FirstMatchingElement().ScriptExecuteCustomToolNodeReferences, reference);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveScriptExecuteCustomToolNodeReferenceFromCustomTool(long businessId, string toolId, BusinessAppToolScriptExecuteCustomToolNodeReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, a => a.Id == toolId)
            );

            var update = Builders<BusinessApp>.Update.PullFilter(
                d => d.Tools.FirstMatchingElement().ScriptExecuteCustomToolNodeReferences,
                r => r.ScriptId == reference.ScriptId && r.NodeId == reference.NodeId
            );

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> AddToolInboundRouteReference(long businessId, string toolId, BusinessAppToolInboundRouteReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == toolId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(b => b.Tools.FirstMatchingElement().InboundRouteReferences, reference);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveToolInboundRouteReference(long businessId, string toolId, BusinessAppToolInboundRouteReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == toolId)
            );

            var update = Builders<BusinessApp>.Update.PullFilter(
                b => b.Tools.FirstMatchingElement().InboundRouteReferences,
                r => r.RouteId == reference.RouteId && r.ActionType == reference.ActionType
            );

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddToolTelephonyCampaignReference(long businessId, string toolId, BusinessAppToolTelephonyCampaignReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == toolId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(b => b.Tools.FirstMatchingElement().TelephonyCampaignReferences, reference);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveToolTelephonyCampaignReference(long businessId, string toolId, BusinessAppToolTelephonyCampaignReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == toolId)
            );

            var update = Builders<BusinessApp>.Update.PullFilter(
                b => b.Tools.FirstMatchingElement().TelephonyCampaignReferences,
                r => r.CampaignId == reference.CampaignId && r.ActionType == reference.ActionType
            );

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddToolWebCampaignReference(long businessId, string toolId, BusinessAppToolWebCampaignReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == toolId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(b => b.Tools.FirstMatchingElement().WebCampaignReferences, reference);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveToolWebCampaignReference(long businessId, string toolId, BusinessAppToolWebCampaignReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Tools, t => t.Id == toolId)
            );

            var update = Builders<BusinessApp>.Update.PullFilter(
                b => b.Tools.FirstMatchingElement().WebCampaignReferences,
                r => r.CampaignId == reference.CampaignId && r.ActionType == reference.ActionType
            );

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
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
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateMessageGroupName(long businessId, string messageGroupId, string newMessageGroupName)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.MessageGroups, t => t.Id == messageGroupId)
            );

            var update = Builders<BusinessApp>.Update.Set(b => b.Cache.MessageGroups.FirstMatchingElement().Name, newMessageGroupName);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddMessageToGroup(long businessId, string groupId, string language, BusinessAppCacheMessage newMessage)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.MessageGroups, t => t.Id == groupId)
            );
            var update = Builders<BusinessApp>.Update.Push(b => b.Cache.MessageGroups.FirstMatchingElement().Messages[language], newMessage);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
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

        public async Task<bool> AddAgentReferenceToMessageCacheGroup(long businessId, string groupId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(c => c.Cache.MessageGroups, Builders<BusinessAppCacheMessageGroup>.Filter.Eq(x => x.Id, groupId))
            );
            var update = Builders<BusinessApp>.Update.AddToSet(c => c.Cache.MessageGroups.FirstMatchingElement().AgentReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }
        public async Task<bool> RemoveAgentReferenceFromMessageCacheGroup(long businessId, string groupId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(c => c.Cache.MessageGroups, Builders<BusinessAppCacheMessageGroup>.Filter.Eq(x => x.Id, groupId))
            );
            var update = Builders<BusinessApp>.Update.Pull(c => c.Cache.MessageGroups.FirstMatchingElement().AgentReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
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
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateAudioGroupName(long businessId, string audioGroupId, string newAudioGroupName)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.AudioGroups, t => t.Id == audioGroupId)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Cache.AudioGroups.FirstMatchingElement().Name, newAudioGroupName);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddAudioToGroup(long businessId, string groupId, string language, BusinessAppCacheAudio newAudio)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.AudioGroups, t => t.Id == groupId)
            );
            var update = Builders<BusinessApp>.Update.Push(b => b.Cache.AudioGroups.FirstMatchingElement().Audios[language], newAudio);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
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

        public async Task<bool> AddAgentReferenceToAudioCacheGroup(long businessId, string groupId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(c => c.Cache.AudioGroups, Builders<BusinessAppCacheAudioGroup>.Filter.Eq(x => x.Id, groupId))
            );
            var update = Builders<BusinessApp>.Update.AddToSet(c => c.Cache.AudioGroups.FirstMatchingElement().AgentReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }
        public async Task<bool> RemoveAgentReferenceFromAudioCacheGroup(long businessId, string groupId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(c => c.Cache.AudioGroups, Builders<BusinessAppCacheAudioGroup>.Filter.Eq(x => x.Id, groupId))
            );
            var update = Builders<BusinessApp>.Update.Pull(c => c.Cache.AudioGroups.FirstMatchingElement().AgentReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }

        public async Task<bool> AddAgentAutoCacheReferenceToAudioCacheGroup(long businessId, string groupId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(c => c.Cache.AudioGroups, Builders<BusinessAppCacheAudioGroup>.Filter.Eq(x => x.Id, groupId))
            );
            var update = Builders<BusinessApp>.Update.AddToSet(c => c.Cache.AudioGroups.FirstMatchingElement().AgentAutoCacheReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }
        public async Task<bool> RemoveAgentAutoCacheReferenceFromAudioCacheGroup(long businessId, string groupId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(c => c.Cache.AudioGroups, Builders<BusinessAppCacheAudioGroup>.Filter.Eq(x => x.Id, groupId))
            );
            var update = Builders<BusinessApp>.Update.Pull(c => c.Cache.AudioGroups.FirstMatchingElement().AgentAutoCacheReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
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
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateEmbeddingGroupName(long businessId, string embeddingGroupId, string newEmbeddingGroupName)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.EmbeddingGroups, t => t.Id == embeddingGroupId)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Cache.EmbeddingGroups.FirstMatchingElement().Name, newEmbeddingGroupName);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddEmbeddingEntryToGroup(long businessId, string groupId, string language, BusinessAppCacheEmbedding newEmbedding)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Cache.EmbeddingGroups, t => t.Id == groupId)
            );
            var update = Builders<BusinessApp>.Update.Push(b => b.Cache.EmbeddingGroups.FirstMatchingElement().Embeddings[language], newEmbedding);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
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

        public async Task<bool> AddAgentReferenceToEmbeddingCacheGroup(long businessId, string groupId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(c => c.Cache.EmbeddingGroups, Builders<BusinessAppCacheEmbeddingGroup>.Filter.Eq(x => x.Id, groupId))
            );
            var update = Builders<BusinessApp>.Update.AddToSet(c => c.Cache.EmbeddingGroups.FirstMatchingElement().AgentReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }
        public async Task<bool> RemoveAgentReferenceFromEmbeddingCacheGroup(long businessId, string groupId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(c => c.Cache.EmbeddingGroups, Builders<BusinessAppCacheEmbeddingGroup>.Filter.Eq(x => x.Id, groupId))
            );
            var update = Builders<BusinessApp>.Update.Pull(c => c.Cache.EmbeddingGroups.FirstMatchingElement().AgentReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }

        public async Task<bool> AddAgentAutoCacheReferenceToEmbeddingCacheGroup(long businessId, string groupId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(c => c.Cache.EmbeddingGroups, Builders<BusinessAppCacheEmbeddingGroup>.Filter.Eq(x => x.Id, groupId))
            );
            var update = Builders<BusinessApp>.Update.AddToSet(c => c.Cache.EmbeddingGroups.FirstMatchingElement().AgentAutoCacheReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }
        public async Task<bool> RemoveAgentAutoCacheReferenceFromEmbeddingCacheGroup(long businessId, string groupId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(c => c.Cache.EmbeddingGroups, Builders<BusinessAppCacheEmbeddingGroup>.Filter.Eq(x => x.Id, groupId))
            );
            var update = Builders<BusinessApp>.Update.Pull(c => c.Cache.EmbeddingGroups.FirstMatchingElement().AgentAutoCacheReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
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
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessIntegration(long businessId, BusinessAppIntegration newIntegration)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, g => g.Id == newIntegration.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Integrations.FirstMatchingElement(), newIntegration);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> DeleteBusinessIntegration(long businessId, string integrationId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, g => g.Id == integrationId)
            );
            var update = Builders<BusinessApp>.Update.PullFilter(b => b.Integrations, g => g.Id == integrationId);

            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddBusinessNumberReferenceToIntegration(long businessId, string integrationId, string businessNumberId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(b => b.Integrations.FirstMatchingElement().BusinessNumberReferences, businessNumberId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveBusinessNumberReferenceFromIntegration(long businessId, string integrationId, string businessNumberId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.Pull(b => b.Integrations.FirstMatchingElement().BusinessNumberReferences, businessNumberId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddKBEmbeddingModelReferenceToIntegration(long businessId, string integrationId, string kbId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(b => b.Integrations.FirstMatchingElement().KnowledgeBaseEmbeddingModelReferences, kbId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveKBEmbeddingModelReferenceFromIntegration(long businessId, string integrationId, string kbId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.Pull(b => b.Integrations.FirstMatchingElement().KnowledgeBaseEmbeddingModelReferences, kbId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddKBRerankReferenceToIntegration(long businessId, string integrationId, string kbId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(b => b.Integrations.FirstMatchingElement().KnowledgeBaseRerankReferences, kbId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveKBRerankReferenceFromIntegration(long businessId, string integrationId, string kbId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.Pull(b => b.Integrations.FirstMatchingElement().KnowledgeBaseRerankReferences, kbId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddPostAnalysisLLMReferenceToIntegration(long businessId, string integrationId, string analysisId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(b => b.Integrations.FirstMatchingElement().PostAnalysisLLMReferences, analysisId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }
        public async Task<bool> RemovePostAnalysisLLMReferenceFromIntegration(long businessId, string integrationId, string analysisId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.Pull(b => b.Integrations.FirstMatchingElement().PostAnalysisLLMReferences, analysisId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddAgentInterruptionTurnEndRefToIntegration(long businessId, string integrationId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(b => b.Integrations.FirstMatchingElement().AgentInterruptionTurnEndViaAILLMReferences, agentId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveAgentInterruptionTurnEndRefFromIntegration(long businessId, string integrationId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.Pull(b => b.Integrations.FirstMatchingElement().AgentInterruptionTurnEndViaAILLMReferences, agentId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddAgentInterruptionVerificationRefToIntegration(long businessId, string integrationId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(b => b.Integrations.FirstMatchingElement().AgentInterruptionVerificationLLMReferences, agentId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveAgentInterruptionVerificationRefFromIntegration(long businessId, string integrationId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.Pull(b => b.Integrations.FirstMatchingElement().AgentInterruptionVerificationLLMReferences, agentId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddAgentSTTReferenceToIntegration(long businessId, string integrationId, string language, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );
            var update = Builders<BusinessApp>.Update.AddToSet($"Integrations.$.AgentSTTReferences.{language}", agentId);

            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }
        public async Task<bool> RemoveAgentSTTReferenceFromIntegration(long businessId, string integrationId, string language, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );
            var update = Builders<BusinessApp>.Update.Pull($"Integrations.$.AgentSTTReferences.{language}", agentId);

            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }

        public async Task<bool> AddAgentLLMReferenceToIntegration(long businessId, string integrationId, string language, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );
            var update = Builders<BusinessApp>.Update.AddToSet($"Integrations.$.AgentLLMReferences.{language}", agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }
        public async Task<bool> RemoveAgentLLMReferenceFromIntegration(long businessId, string integrationId, string language, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );
            var update = Builders<BusinessApp>.Update.Pull($"Integrations.$.AgentLLMReferences.{language}", agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }

        public async Task<bool> AddAgentTTSReferenceToIntegration(long businessId, string integrationId, string language, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );
            var update = Builders<BusinessApp>.Update.AddToSet($"Integrations.$.AgentTTSReferences.{language}", agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }
        public async Task<bool> RemoveAgentTTSReferenceFromIntegration(long businessId, string integrationId, string language, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );
            var update = Builders<BusinessApp>.Update.Pull($"Integrations.$.AgentTTSReferences.{language}", agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }

        public async Task<bool> AddAgentKBQueryRefinementRefToIntegration(long businessId, string integrationId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(b => b.Integrations.FirstMatchingElement().AgentKnowledgeBaseQueryAIRefinementLLMReferences, agentId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveAgentKBQueryRefinementRefFromIntegration(long businessId, string integrationId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );

            var update = Builders<BusinessApp>.Update.Pull(b => b.Integrations.FirstMatchingElement().AgentKnowledgeBaseQueryAIRefinementLLMReferences, agentId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddAgentKBSearchStrategyRefToIntegration(long businessId, string integrationId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );
            var update = Builders<BusinessApp>.Update.AddToSet(b => b.Integrations.FirstMatchingElement().AgentKnowledgeBaseSearchStrategyLLMReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }
        public async Task<bool> RemoveAgentKBSearchStrategyRefFromIntegration(long businessId, string integrationId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Integrations, i => i.Id == integrationId)
            );
            var update = Builders<BusinessApp>.Update.Pull(b => b.Integrations.FirstMatchingElement().AgentKnowledgeBaseSearchStrategyLLMReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
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

        public async Task<bool> AddAgent(long businessId, BusinessAppAgent agent, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.Not(
                    Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, a => a.Id == agent.Id)
                )
            );

            var update = Builders<BusinessApp>.Update.Push(b => b.Agents, agent);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateAgentDataExceptReferences(long businessId, BusinessAppAgent agent, IClientSessionHandle session)
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

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> DeleteAgent(long businessId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, g => g.Id == agentId)
            );

            var update = Builders<BusinessApp>.Update.PullFilter(b => b.Agents, a => a.Id == agentId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> AddInboundRoutingReferenceToAgent(long businessId, string agentId, string inboundRoutingId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, a => a.Id == agentId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.Agents.FirstMatchingElement().InboundRoutingReferences, inboundRoutingId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveInboundRoutingReferenceFromAgent(long businessId, string agentId, string inboundRoutingId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, a => a.Id == agentId)
            );

            var update = Builders<BusinessApp>.Update.Pull(d => d.Agents.FirstMatchingElement().InboundRoutingReferences, inboundRoutingId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> AddTelephonyCampaignReferenceToAgent(long businessId, string agentId, string telephonyCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, a => a.Id == agentId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.Agents.FirstMatchingElement().TelephonyCampaignReferences, telephonyCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveTelephonyCampaignReferenceFromAgent(long businessId, string agentId, string telephonyCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, a => a.Id == agentId)
            );

            var update = Builders<BusinessApp>.Update.Pull(d => d.Agents.FirstMatchingElement().TelephonyCampaignReferences, telephonyCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> AddWebCampaignReferenceToAgent(long businessId, string agentId, string webCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, a => a.Id == agentId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.Agents.FirstMatchingElement().WebCampaignReferences, webCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveWebCampaignReferenceFromAgent(long businessId, string agentId, string webCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, a => a.Id == agentId)
            );

            var update = Builders<BusinessApp>.Update.Pull(d => d.Agents.FirstMatchingElement().WebCampaignReferences, webCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> AddScriptTransferToAgentNodeReferenceToAgent(long businessId, string agentId, BusinessAppAgentScriptTransferToAgentNodeReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, a => a.Id == agentId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.Agents.FirstMatchingElement().ScriptTransferToAgentNodeReferences, reference);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveScriptTransferToAgentNodeReferenceFromAgent(long businessId, string agentId, BusinessAppAgentScriptTransferToAgentNodeReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Agents, a => a.Id == agentId)
            );

            var update = Builders<BusinessApp>.Update.PullFilter(
                d => d.Agents.FirstMatchingElement().ScriptTransferToAgentNodeReferences,
                r => r.ScriptId == reference.ScriptId && r.NodeId == reference.NodeId
            );

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        /**
         * 
         * Scripts
         * 
        **/
        public async Task<bool> CheckScriptExists(long businessId, string scriptId)
        {
            var query = _businessAppCollection.AsQueryable()
                .Where(b => b.Id == businessId)
                .SelectMany(b => b.Scripts)
                .AnyAsync(script => script.Id == scriptId);

            return await query;
        }

        public async Task<BusinessAppScript?> GetScriptById(long businessId, string scriptId)
        {
            var scriptQuery = _businessAppCollection.AsQueryable()
                .Where(b => b.Id == businessId)
                .SelectMany(b => b.Scripts)
                .Where(script => script.Id == scriptId);

            return await scriptQuery.FirstOrDefaultAsync();
        }

        public async Task<bool> AddScript(long businessId, BusinessAppScript newScriptData, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);

            var update = Builders<BusinessApp>.Update.Push(d => d.Scripts, newScriptData);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }
        
        public async Task<bool> UpdateScriptExceptReferences(long businessId, BusinessAppScript updatedScriptData, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Scripts, s => s.Id == updatedScriptData.Id)
            );

            var update = Builders<BusinessApp>.Update
                .Set(d => d.Scripts.FirstMatchingElement().General, updatedScriptData.General)
                .Set(d => d.Scripts.FirstMatchingElement().Nodes, updatedScriptData.Nodes)
                .Set(d => d.Scripts.FirstMatchingElement().Edges, updatedScriptData.Edges);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> DeleteScript(long businessId, string scriptId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Scripts, s => s.Id == scriptId)
            );

            var update = Builders<BusinessApp>.Update.PullFilter(d => d.Scripts, s => s.Id == scriptId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddInboundRoutingReferenceToScript(long businessId, string scriptId, string inboundRoutingId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Scripts, s => s.Id == scriptId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.Scripts.FirstMatchingElement().InboundRoutingReferences, inboundRoutingId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> RemoveInboundRoutingReferenceFromScript(long businessId, string scriptId, string inboundRoutingId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Scripts, s => s.Id == scriptId)
            );

            var update = Builders<BusinessApp>.Update.Pull(d => d.Scripts.FirstMatchingElement().InboundRoutingReferences, inboundRoutingId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddTelephonyCampaignReferenceToScript(long businessId, string scriptId, string telephonyCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Scripts, s => s.Id == scriptId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.Scripts.FirstMatchingElement().TelephonyCampaignReferences, telephonyCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> RemoveTelephonyCampaignReferenceFromScript(long businessId, string scriptId, string telephonyCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Scripts, s => s.Id == scriptId)
            );

            var update = Builders<BusinessApp>.Update.Pull(d => d.Scripts.FirstMatchingElement().TelephonyCampaignReferences, telephonyCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddWebCampaignReferenceToScript(long businessId, string scriptId, string webCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Scripts, s => s.Id == scriptId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.Scripts.FirstMatchingElement().WebCampaignReferences, webCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> RemoveWebCampaignReferenceFromScript(long businessId, string scriptId, string webCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Scripts, s => s.Id == scriptId)
            );

            var update = Builders<BusinessApp>.Update.Pull(d => d.Scripts.FirstMatchingElement().WebCampaignReferences, webCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddScriptToContextNodeReferenceToScript(long businessId, string scriptId, BusinessAppScriptAddScriptToContextNodeReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Scripts, s => s.Id == scriptId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.Scripts.FirstMatchingElement().ScriptAddScriptNodeReferences, reference);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> RemoveAddScriptToContextReferenceFromScript(long businessId, string scriptId, BusinessAppScriptAddScriptToContextNodeReference reference, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Scripts, s => s.Id == scriptId)
            );

            var update = Builders<BusinessApp>.Update.PullFilter(
                d => d.Scripts.FirstMatchingElement().ScriptAddScriptNodeReferences,
                r => r.ScriptId == reference.ScriptId && r.NodeId == reference.NodeId
            );

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
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

        public async Task<bool> AddBusinessNumber(long businessId, BusinessNumberData newNumberData, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Numbers, newNumberData);
            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessNumberExceptReferences(long businessId, BusinessNumberData newNumberData, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Numbers, g => g.Id == newNumberData.Id)
            );
            var update = Builders<BusinessApp>.Update
                .Set(b => b.Numbers.FirstMatchingElement().CountryCode, newNumberData.CountryCode)
                .Set(b => b.Numbers.FirstMatchingElement().Number, newNumberData.Number)
                .Set(b => b.Numbers.FirstMatchingElement().RouteId, newNumberData.RouteId)
                .Set(b => b.Numbers.FirstMatchingElement().RegionId, newNumberData.RegionId)
                .Set(b => b.Numbers.FirstMatchingElement().RegionServerId, newNumberData.RegionServerId)
                .Set(b => b.Numbers.FirstMatchingElement().IntegrationId, newNumberData.IntegrationId)
                .Set(b => b.Numbers.FirstMatchingElement().VoiceEnabled, newNumberData.VoiceEnabled)
                .Set(b => b.Numbers.FirstMatchingElement().SmsEnabled, newNumberData.SmsEnabled)
                .Set(b => b.Numbers.FirstMatchingElement().Provider, newNumberData.Provider);

            if (newNumberData is BusinessNumberModemTelData modemTelData)
            {
                update = update.Set(b => ((BusinessNumberModemTelData)b.Numbers.FirstMatchingElement()).ModemTelPhoneNumberId, modemTelData.ModemTelPhoneNumberId);
            }
            else if (newNumberData is BusinessNumberTwilioData twilioData)
            {
                update = update.Set(b => ((BusinessNumberTwilioData)b.Numbers.FirstMatchingElement()).TwilioPhoneNumberId, twilioData.TwilioPhoneNumberId);
            }
            else if (newNumberData is BusinessNumberVonageData vonageData)
            {
                // todo
                //update = update.Set(b => ((BusinessNumberVonageData)b.Numbers.FirstMatchingElement()).VonagePhoneNumberId, vonageData.VonagePhoneNumberId);
            }
            else if (newNumberData is BusinessNumberTelnyxData telnyxData)
            {
                // todo
                //update = update.Set(b => ((BusinessNumberTelnyxData)b.Numbers.FirstMatchingElement()).TelnyxPhoneNumberId, telnyxData.TelnyxPhoneNumberId);
            }
            else if (newNumberData is BusinessNumberSipData sipData)
            {
                update = update
                    .Set(b => ((BusinessNumberSipData)b.Numbers.FirstMatchingElement()).IsE164Number, sipData.IsE164Number)
                    .Set(b => ((BusinessNumberSipData)b.Numbers.FirstMatchingElement()).OverrideSipUsername, sipData.OverrideSipUsername)
                    .Set(b => ((BusinessNumberSipData)b.Numbers.FirstMatchingElement()).OverrideSipPassword, sipData.OverrideSipPassword)
                    .Set(b => ((BusinessNumberSipData)b.Numbers.FirstMatchingElement()).AllowedSourceIps, sipData.AllowedSourceIps);
            }

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddAgentScriptSMSNodeReferenceToBusinessNumber(long businessId, string phoneNumberId, BusinessNumberScriptSMSNodeReference data, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Numbers, t => t.Id == phoneNumberId)
            );
            var update = Builders<BusinessApp>.Update.AddToSet(d => d.Numbers.FirstMatchingElement().ScriptSMSNodeReferences, data);
            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> RemoveAgentScriptSMSNodeReferenceFromBusinessNumber(long businessId, string phoneNumberId, BusinessNumberScriptSMSNodeReference data, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Numbers, t => t.Id == phoneNumberId)
            );
            var update = Builders<BusinessApp>.Update.PullFilter(
                d => d.Numbers.FirstMatchingElement().ScriptSMSNodeReferences,
                f => f.ScriptId == data.ScriptId && f.NodeReference == data.NodeReference
            );
            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> DeleteBusinessNumber(long businessId, string numberId)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Numbers, t => t.Id == numberId)
            );
            var update = Builders<BusinessApp>.Update.PullFilter(b => b.Numbers, t => t.Id == numberId);
            var result = await _businessAppCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessNumberRoute(long businessId, string numberId, string? routeId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Numbers, g => g.Id == numberId)
            );

            var update = Builders<BusinessApp>.Update.Set(b => b.Numbers.FirstMatchingElement().RouteId, routeId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
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

        public async Task<bool> AddBusinessAppRoute(long businessId, BusinessAppRoute newBusinessAppRouteData, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.Routings, newBusinessAppRouteData);
            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessAppRoute(long businessId, BusinessAppRoute newBusinessAppRouteData, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.Routings, g => g.Id == newBusinessAppRouteData.Id)
            );
            var update = Builders<BusinessApp>.Update.Set(b => b.Routings.FirstMatchingElement(), newBusinessAppRouteData);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        /**
        * 
        * Knowledge Base
        * 
        **/
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
        public async Task<bool> AddKnowledgeBase(long businessId, BusinessAppKnowledgeBase kb, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.KnowledgeBases, kb);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
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
        public async Task<bool> UpdateKnowledgeBaseExceptDocumentsAndReferences(long businessId, BusinessAppKnowledgeBase kb, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.KnowledgeBases, k => k.Id == kb.Id)
            );

            var update = Builders<BusinessApp>.Update
                .Set(b => b.KnowledgeBases.FirstMatchingElement().General, kb.General)
                .Set(b => b.KnowledgeBases.FirstMatchingElement().Configuration, kb.Configuration);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveKnowledgeBase(long businessId, string kbId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.PullFilter(b => b.KnowledgeBases, k => k.Id == kbId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> AddDocumentIdToKnowledgeBaseAsync(long businessId, string kbId, long documentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.KnowledgeBases, k => k.Id == kbId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(b => b.KnowledgeBases.FirstMatchingElement().Documents, documentId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveDocumentIdFromKnowledgeBaseAsync(long businessId, string kbId, long documentId, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.KnowledgeBases, k => k.Id == kbId)
            );

            var update = Builders<BusinessApp>.Update.Pull(b => b.KnowledgeBases.FirstMatchingElement().Documents, documentId);

            var result = session != null
                ? await _businessAppCollection.UpdateOneAsync(session, filter, update)
                : await _businessAppCollection.UpdateOneAsync(filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> AddAgentReferenceToKB(long businessId, string kbId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.KnowledgeBases, k => k.Id == kbId)
            );
            var update = Builders<BusinessApp>.Update.AddToSet(b => b.KnowledgeBases.FirstMatchingElement().AgentReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
        }
        public async Task<bool> RemoveAgentReferenceFromKB(long businessId, string kbId, string agentId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.KnowledgeBases, k => k.Id == kbId)
            );
            var update = Builders<BusinessApp>.Update.Pull(b => b.KnowledgeBases.FirstMatchingElement().AgentReferences, agentId);
            return (await _businessAppCollection.UpdateOneAsync(session, filter, update)).IsAcknowledged;
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

        public async Task<bool> AddBusinessAppTelephonyCampaign(long businessId, BusinessAppTelephonyCampaign newBusinessAppCampaignTelephonyData, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.TelephonyCampaigns, newBusinessAppCampaignTelephonyData);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessAppTelephonyCampaign(long businessId, BusinessAppTelephonyCampaign newBusinessAppCampaignTelephonyData, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.TelephonyCampaigns, t => t.Id == newBusinessAppCampaignTelephonyData.Id)
            );

            var update = Builders<BusinessApp>.Update
                .Set(b => b.TelephonyCampaigns.FirstMatchingElement(), newBusinessAppCampaignTelephonyData);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

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

        public async Task<bool> AddBusinessAppWebCampaign(long businessId, BusinessAppWebCampaign newBusinessAppCampaignWebData, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.WebCampaigns, newBusinessAppCampaignWebData);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessAppWebCampaign(long businessId, BusinessAppWebCampaign newBusinessAppCampaignWebData, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.WebCampaigns, t => t.Id == newBusinessAppCampaignWebData.Id)
            );

            var update = Builders<BusinessApp>.Update
                .Set(b => b.WebCampaigns.FirstMatchingElement(), newBusinessAppCampaignWebData);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

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

        public async Task<bool> AddBusinessAppPostAnalysisTemplate(long businessId, BusinessAppPostAnalysis newTemplate, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessApp>.Update.Push(b => b.PostAnalysis, newTemplate);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateBusinessAppPostAnalysisTemplateExceptReferences(long businessId, BusinessAppPostAnalysis updatedTemplate, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.PostAnalysis, t => t.Id == updatedTemplate.Id)
            );
            var update = Builders<BusinessApp>.Update
                .Set(b => b.PostAnalysis.FirstMatchingElement().General, updatedTemplate.General)
                .Set(b => b.PostAnalysis.FirstMatchingElement().Configuration, updatedTemplate.Configuration)
                .Set(b => b.PostAnalysis.FirstMatchingElement().Summary, updatedTemplate.Summary)
                .Set(b => b.PostAnalysis.FirstMatchingElement().Tagging, updatedTemplate.Tagging)
                .Set(b => b.PostAnalysis.FirstMatchingElement().Extraction, updatedTemplate.Extraction);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> DeletePostAnalysis(long businessId, string templateId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.PostAnalysis, t => t.Id == templateId)
            );

            var update = Builders<BusinessApp>.Update.PullFilter(b => b.PostAnalysis, t => t.Id == templateId);
            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddInboundRoutingReferenceToPostAnalysis(long businessId, string templateId, string inboundRoutingId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.PostAnalysis, a => a.Id == templateId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.PostAnalysis.FirstMatchingElement().InboundRoutingReferences, inboundRoutingId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveInboundRoutingReferenceFromPostAnalysis(long businessId, string templateId, string inboundRoutingId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.PostAnalysis, a => a.Id == templateId)
            );

            var update = Builders<BusinessApp>.Update.Pull(d => d.PostAnalysis.FirstMatchingElement().InboundRoutingReferences, inboundRoutingId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> AddTelephonyCampaignReferenceToPostAnalysis(long businessId, string templateId, string telephonyCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.PostAnalysis, a => a.Id == templateId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.PostAnalysis.FirstMatchingElement().TelephonyCampaignReferences, telephonyCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveTelephonyCampaignReferenceFromPostAnalysis(long businessId, string templateId, string telephonyCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.PostAnalysis, a => a.Id == templateId)
            );

            var update = Builders<BusinessApp>.Update.Pull(d => d.PostAnalysis.FirstMatchingElement().TelephonyCampaignReferences, telephonyCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> AddWebCampaignReferenceToPostAnalysis(long businessId, string templateId, string webCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.PostAnalysis, a => a.Id == templateId)
            );

            var update = Builders<BusinessApp>.Update.AddToSet(d => d.PostAnalysis.FirstMatchingElement().WebCampaignReferences, webCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }
        public async Task<bool> RemoveWebCampaignReferenceFromPostAnalysis(long businessId, string templateId, string webCampaignId, IClientSessionHandle session)
        {
            var filter = Builders<BusinessApp>.Filter.And(
                Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId),
                Builders<BusinessApp>.Filter.ElemMatch(b => b.PostAnalysis, a => a.Id == templateId)
            );

            var update = Builders<BusinessApp>.Update.Pull(d => d.PostAnalysis.FirstMatchingElement().WebCampaignReferences, webCampaignId);

            var result = await _businessAppCollection.UpdateOneAsync(session, filter, update);

            return result.IsAcknowledged;
        }
    }
}