using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessCacheManager
    {
        private readonly ILogger<BusinessCacheManager> _logger;

        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;

        public BusinessCacheManager( ILogger<BusinessCacheManager> logger, BusinessManager businessManager, BusinessAppRepository businessAppRepository, BusinessRepository businessRepository)
        {
            _logger = logger;

            _parentBusinessManager = businessManager;

            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;
        }

        /**
         * 
         * Cache Tab
         * Message Group | Message Cache
         * 
        **/

        public async Task<FunctionReturnResult<BusinessAppCacheMessageGroup?>> AddOrUpdateMessageGroup(long businessId, IFormCollection formData, string postType, string? existingGroupId)
        {
            var result = new FunctionReturnResult<BusinessAppCacheMessageGroup?>();

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateMessageGroup:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateMessageGroup:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var newMessageGroup = new BusinessAppCacheMessageGroup();

            // Name validation
            if (!changes.RootElement.TryGetProperty("name", out var nameElement))
            {
                result.Code = "AddOrUpdateMessageGroup:3";
                result.Message = "Name not found.";
                return result;
            }

            string? name = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                result.Code = "AddOrUpdateMessageGroup:4";
                result.Message = "Name is required.";
                return result;
            }
            newMessageGroup.Name = name;

            // Initialize messages dictionary for all business languages
            List<string> businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);
            foreach (var language in businessLanguages)
            {
                newMessageGroup.Messages[language] = new List<BusinessAppCacheMessage>();
            }

            // Saving or Updating
            if (postType == "new")
            {
                newMessageGroup.Id = ObjectId.GenerateNewId().ToString();

                var addResult = await _businessAppRepository.AddCacheMessageGroup(businessId, newMessageGroup);
                if (!addResult)
                {
                    result.Code = "AddOrUpdateMessageGroup:5";
                    result.Message = "Failed to add message group.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                newMessageGroup.Id = existingGroupId;

                var updateResult = await _businessAppRepository.UpdateMessageGroupName(businessId, newMessageGroup.Id, newMessageGroup.Name);
                if (!updateResult)
                {
                    result.Code = "AddOrUpdateMessageGroup:6";
                    result.Message = "Failed to update message group.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newMessageGroup;
            return result;
        }

        public async Task<FunctionReturnResult<BusinessAppCacheMessage?>> AddOrUpdateMessageGroupMessage(long businessId, string groupId, IFormCollection formData, string postType, string language, string? existingMessageCacheId)
        {
            var result = new FunctionReturnResult<BusinessAppCacheMessage?>();

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateMessageGroupMessage:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateMessageGroupMessage:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var newMessage = new BusinessAppCacheMessage();

            // Query validation
            if (!changes.RootElement.TryGetProperty("query", out var queryElement))
            {
                result.Code = "AddOrUpdateMessageGroupMessage:3";
                result.Message = "Query not found.";
                return result;
            }

            string? query = queryElement.GetString();
            if (string.IsNullOrWhiteSpace(query))
            {
                result.Code = "AddOrUpdateMessageGroupMessage:4";
                result.Message = "Query is required.";
                return result;
            }
            newMessage.Query = query;

            // Response validation
            if (!changes.RootElement.TryGetProperty("response", out var responseElement))
            {
                result.Code = "AddOrUpdateMessageGroupMessage:5";
                result.Message = "Response not found.";
                return result;
            }

            string? response = responseElement.GetString();
            if (string.IsNullOrWhiteSpace(response))
            {
                result.Code = "AddOrUpdateMessageGroupMessage:6";
                result.Message = "Response is required.";
                return result;
            }
            newMessage.Response = response;

            // Saving or updating
            if (postType == "new")
            {
                newMessage.Id = ObjectId.GenerateNewId().ToString();

                var addResult = await _businessAppRepository.AddMessageToGroup(
                    businessId,
                    groupId,
                    language,
                    newMessage
                );
                if (!addResult)
                {
                    result.Code = "AddOrUpdateMessageGroupMessage:8";
                    result.Message = "Failed to add message to group.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                newMessage.Id = existingMessageCacheId;

                var updateResult = await _businessAppRepository.UpdateMessageInGroup(
                    businessId,
                    groupId,
                    language,
                    newMessage
                );
                if (!updateResult)
                {
                    result.Code = "AddOrUpdateMessageGroupMessage:9";
                    result.Message = "Failed to update message in group.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newMessage;
            return result;
        }

        public async Task<bool> CheckBusinessCacheMessageGroupExists(long businessId, string existingGroupId)
        {
            var result = await _businessAppRepository.CheckCacheMessageGroupExists(businessId, existingGroupId);
            return result;
        }

        public async Task<bool> CheckBusinessCacheMessageGroupMessageExists(long businessId, string groupId, string language, string existingCacheId)
        {
            var result = await _businessAppRepository.CheckCacheMessageGroupMessageExists(businessId, groupId, language, existingCacheId);
            return result;
        }

        /**
         * 
         * Cache Tab
         * Audio Group | Audio Cache
         * 
        **/

        public async Task<FunctionReturnResult<BusinessAppCacheAudioGroup?>> AddOrUpdateAudioGroup(long businessId, IFormCollection formData, string postType, string? existingGroupId)
        {
            var result = new FunctionReturnResult<BusinessAppCacheAudioGroup?>();

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateAudioGroup:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateAudioGroup:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var newAudioGroup = new BusinessAppCacheAudioGroup();

            // Name validation
            if (!changes.RootElement.TryGetProperty("name", out var nameElement))
            {
                result.Code = "AddOrUpdateAudioGroup:3";
                result.Message = "Name not found.";
                return result;
            }

            string? name = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                result.Code = "AddOrUpdateAudioGroup:4";
                result.Message = "Name is required.";
                return result;
            }
            newAudioGroup.Name = name;

            // Initialize audios dictionary for all business languages
            List<string> businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);
            foreach (var language in businessLanguages)
            {
                newAudioGroup.Audios[language] = new List<BusinessAppCacheAudio>();
            }

            // Saving or Updating
            if (postType == "new")
            {
                newAudioGroup.Id = ObjectId.GenerateNewId().ToString();

                var addResult = await _businessAppRepository.AddCacheAudioGroup(businessId, newAudioGroup);
                if (!addResult)
                {
                    result.Code = "AddOrUpdateAudioGroup:5";
                    result.Message = "Failed to add audio group.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                newAudioGroup.Id = existingGroupId;

                var updateResult = await _businessAppRepository.UpdateAudioGroupName(businessId, newAudioGroup.Id, newAudioGroup.Name);
                if (!updateResult)
                {
                    result.Code = "AddOrUpdateAudioGroup:6";
                    result.Message = "Failed to update audio group.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newAudioGroup;
            return result;
        }

        public async Task<FunctionReturnResult<BusinessAppCacheAudio?>> AddOrUpdateAudioGroupAudio(long businessId, string groupId, IFormCollection formData, string postType, string language, string? existingAudioCacheId)
        {
            var result = new FunctionReturnResult<BusinessAppCacheAudio?>();

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateAudioGroupAudio:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateAudioGroupAudio:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var newAudio = new BusinessAppCacheAudio();

            // Query validation
            if (!changes.RootElement.TryGetProperty("query", out var queryElement))
            {
                result.Code = "AddOrUpdateAudioGroupAudio:3";
                result.Message = "Query not found.";
                return result;
            }

            string? query = queryElement.GetString();
            if (string.IsNullOrWhiteSpace(query))
            {
                result.Code = "AddOrUpdateAudioGroupAudio:4";
                result.Message = "Query is required.";
                return result;
            }
            newAudio.Query = query;

            // Unused expiry hours validation
            if (changes.RootElement.TryGetProperty("unusedExpiryHours", out var expiryElement))
            {
                int expiryHours = expiryElement.GetInt32();
                if (expiryHours < 1)
                {
                    result.Code = "AddOrUpdateAudioGroupAudio:5";
                    result.Message = "Expiry hours must be at least 1.";
                    return result;
                }
                newAudio.UnusedExpiryHours = expiryHours;
            }
            else
            {
                newAudio.UnusedExpiryHours = 24; // Default value
            }

            // Saving or updating
            if (postType == "new")
            {
                newAudio.Id = ObjectId.GenerateNewId().ToString();

                var addResult = await _businessAppRepository.AddAudioToGroup(
                    businessId,
                    groupId,
                    language,
                    newAudio
                );
                if (!addResult)
                {
                    result.Code = "AddOrUpdateAudioGroupAudio:6";
                    result.Message = "Failed to add audio to group.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                newAudio.Id = existingAudioCacheId;

                var updateResult = await _businessAppRepository.UpdateAudioInGroup(
                    businessId,
                    groupId,
                    language,
                    newAudio
                );
                if (!updateResult)
                {
                    result.Code = "AddOrUpdateAudioGroupAudio:7";
                    result.Message = "Failed to update audio in group.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newAudio;
            return result;
        }

        public async Task<FunctionReturnResult<BusinessAppCacheAudio?>> AddAudioCacheGroupAudio(
            long businessId,
            string groupId,
            string language,
            BusinessAppCacheAudio audio
        )
        {
            var result = new FunctionReturnResult<BusinessAppCacheAudio?>();
            if (audio == null)
            {
                result.Code = "AddAudioChacheGroupAudio:1";
                result.Message = "Audio cannot be null.";
                return result;
            }
            audio.Id = ObjectId.GenerateNewId().ToString();
            var addResult = await _businessAppRepository.AddAudioToGroup(
                businessId,
                groupId,
                language,
                audio
            );
            if (!addResult)
            {
                result.Code = "AddAudioChacheGroupAudio:2";
                result.Message = "Failed to add audio to group.";
                return result;
            }
            result.Success = true;
            result.Data = audio;
            return result;
        }

        public async Task<bool> CheckBusinessCacheAudioGroupExists(long businessId, string existingGroupId)
        {
            var result = await _businessAppRepository.CheckCacheAudioGroupExists(businessId, existingGroupId);
            return result;
        }

        public async Task<bool> CheckBusinessCacheAudioGroupAudioExists(long businessId, string groupId, string language, string existingCacheId)
        {
            var result = await _businessAppRepository.CheckCacheAudioGroupAudioExists(businessId, groupId, language, existingCacheId);
            return result;
        }

        /**
         * 
         * Cache Tab
         * Embedding Group | Embedding Cache
         * 
        **/

        public async Task<bool> CheckBusinessCacheEmbeddingGroupExists(long businessId, string embeddingsCacheGroupId)
        {
            var result = await _businessAppRepository.CheckCacheEmbeddingGroupExists(businessId, embeddingsCacheGroupId);
            return result;
        }

        public async Task<List<BusinessAppCacheEmbeddingGroup>> FindCacheEmbeddingGroupsWithCacheQuery(long businessId, string language, string query)
        {
            try
            {
                return await _businessAppRepository.FindCacheEmbeddingGroupsWithCacheQuery(businessId, language, query);
            }
            catch (Exception ex)
            {
                _logger.LogError("[BusinessCacheManager] FindCacheEmbeddingGroupsWithCacheQuery: " + ex.Message);
                return new List<BusinessAppCacheEmbeddingGroup>();
            }
        }

        public async Task<FunctionReturnResult<BusinessAppCacheEmbeddingGroup?>> AddOrUpdateEmbeddingGroup(long businessId, IFormCollection formData, string postType, string? existingGroupId)
        {
            var result = new FunctionReturnResult<BusinessAppCacheEmbeddingGroup?>();

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateEmbeddingGroup:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateEmbeddingGroup:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var newEmbeddingGroup = new BusinessAppCacheEmbeddingGroup();

            // Name validation
            if (!changes.RootElement.TryGetProperty("name", out var nameElement))
            {
                result.Code = "AddOrUpdateEmbeddingGroup:3";
                result.Message = "Name not found.";
                return result;
            }

            string? name = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                result.Code = "AddOrUpdateEmbeddingGroup:4";
                result.Message = "Name is required.";
                return result;
            }
            newEmbeddingGroup.Name = name;

            // Initialize embeddings dictionary for all business languages
            List<string> businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);
            foreach (var language in businessLanguages)
            {
                newEmbeddingGroup.Embeddings[language] = new List<BusinessAppCacheEmbedding>();
            }

            // Saving or Updating
            if (postType == "new")
            {
                newEmbeddingGroup.Id = ObjectId.GenerateNewId().ToString();

                var addResult = await _businessAppRepository.AddCacheEmbeddingGroup(businessId, newEmbeddingGroup);
                if (!addResult)
                {
                    result.Code = "AddOrUpdateEmbeddingGroup:5";
                    result.Message = "Failed to add embedding group.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                newEmbeddingGroup.Id = existingGroupId;

                var updateResult = await _businessAppRepository.UpdateEmbeddingGroupName(businessId, newEmbeddingGroup.Id, newEmbeddingGroup.Name);
                if (!updateResult)
                {
                    result.Code = "AddOrUpdateEmbeddingGroup:6";
                    result.Message = "Failed to update embedding group.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newEmbeddingGroup;
            return result;
        }

        public async Task<bool> CheckBusinessCacheEmbeddingGroupEmbeddingExists(long businessId, string groupId, string language, string existingCacheId)
        {
            var result = await _businessAppRepository.CheckCacheEmbeddingGroupEmbeddingExists(businessId, groupId, language, existingCacheId);
            return result;
        }

        public async Task<FunctionReturnResult<BusinessAppCacheEmbedding?>> AddOrUpdateEmbeddingGroupEmbedding(long businessId, string groupId, IFormCollection formData, string postType, string language, string? existingCacheId)
        {
            var result = new FunctionReturnResult<BusinessAppCacheEmbedding?>();

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateEmbeddingGroupEmbedding:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateEmbeddingGroupEmbedding:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var newEmbedding = new BusinessAppCacheEmbedding();

            // Query validation
            if (!changes.RootElement.TryGetProperty("query", out var queryElement))
            {
                result.Code = "AddOrUpdateEmbeddingGroupEmbedding:3";
                result.Message = "Query not found.";
                return result;
            }

            string? query = queryElement.GetString();
            if (string.IsNullOrWhiteSpace(query))
            {
                result.Code = "AddOrUpdateEmbeddingGroupEmbedding:4";
                result.Message = "Query is required.";
                return result;
            }
            newEmbedding.Query = query;

            // Saving or updating
            if (postType == "new")
            {
                newEmbedding.Id = ObjectId.GenerateNewId().ToString();

                var addResult = await _businessAppRepository.AddEmbeddingEntryToGroup(
                    businessId,
                    groupId,
                    language,
                    newEmbedding
                );
                if (!addResult)
                {
                    result.Code = "AddOrUpdateEmbeddingGroupEmbedding:5";
                    result.Message = "Failed to add embedding to group.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                newEmbedding.Id = existingCacheId;

                var updateResult = await _businessAppRepository.UpdateEmbeddingInGroup(
                    businessId,
                    groupId,
                    language,
                    newEmbedding
                );
                if (!updateResult)
                {
                    result.Code = "AddOrUpdateEmbeddingGroupEmbedding:6";
                    result.Message = "Failed to update embedding in group.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newEmbedding;
            return result;
        }

        public async Task<bool> AddEmbeddingGroupEntry(long businessId, string groupId, string language, BusinessAppCacheEmbedding cacheEntry)
        {
            return await _businessAppRepository.AddEmbeddingEntryToGroup(
                businessId,
                groupId,
                language,
                cacheEntry
            );
        }
    }
}
