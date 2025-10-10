using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessCacheController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly BusinessManager _businessManager;

        public UserBusinessCacheController(
            UserSessionValidationHelper userSessionValidationHelper,
            BusinessManager businessManager
        )
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/cache/messagegroups/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheMessageGroup?>> SaveBusinessMessageGroup(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheMessageGroup?>();

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserAndBusinessSessionAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkBusinessesDisabled: true,
                checkBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessMessageGroup:{userSessionAndBusinessValidationResult.Code}",
                    userSessionAndBusinessValidationResult.Message
                );
            }
            var userData = userSessionAndBusinessValidationResult.Data!.userData!;
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            // Business Cache Permissions
            if (businessData.Permission.Cache.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessMessageGroup:9";
                result.Message = "Business does not have permission to access cache";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.DisabledFullReason;
                }

                return result;
            }
            if (businessData.Permission.Cache.MessageGroup.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessMessageGroup:10";
                result.Message = "Business does not have permission to access message groups";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.MessageGroup.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.MessageGroup.DisabledFullReason;
                }

                return result;
            }

            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
            {
                result.Code = "SaveBusinessMessageGroup:11";
                result.Message = "Invalid post type";
                return result;
            }

            formData.TryGetValue("existingGroupId", out StringValues existingGroupIdValue);
            string? existingGroupId = existingGroupIdValue.ToString();

            if (postType == "edit")
            {
                if (businessData.Permission.Cache.MessageGroup.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessMessageGroup:12";
                    result.Message = "Business does not have permission to edit message groups";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.MessageGroup.DisabledEditingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.MessageGroup.DisabledEditingReason;
                    }

                    return result;
                }

                if (string.IsNullOrWhiteSpace(existingGroupId))
                {
                    result.Code = "SaveBusinessMessageGroup:13";
                    result.Message = "Missing existing group id";
                    return result;
                }

                bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheMessageGroupExists(businessId, existingGroupId);
                if (!groupExists)
                {
                    result.Code = "SaveBusinessMessageGroup:14";
                    result.Message = "Group not found";
                    return result;
                }
            }
            else if (postType == "new")
            {
                if (businessData.Permission.Cache.MessageGroup.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessMessageGroup:15";
                    result.Message = "Business does not have permission to add message groups";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.MessageGroup.DisabledAddingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.MessageGroup.DisabledAddingReason;
                    }

                    return result;
                }
            }

            var updateResult = await _businessManager.GetCacheManager().AddOrUpdateMessageGroup(businessId, formData, postType, existingGroupId);
            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessMessageGroup:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = updateResult.Data;
            return result;
        }

        [HttpPost("/app/user/business/{businessId}/cache/messagegroups/messages/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheMessage?>> SaveBusinessMessageGroupMessage(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheMessage?>();

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserAndBusinessSessionAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkBusinessesDisabled: true,
                checkBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessMessageGroupMessage:{userSessionAndBusinessValidationResult.Code}",
                    userSessionAndBusinessValidationResult.Message
                );
            }
            var userData = userSessionAndBusinessValidationResult.Data!.userData!;
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            if (businessData.Permission.Cache.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessMessageGroupMessage:8";
                result.Message = "Business does not have permission to access cache";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.DisabledFullReason;
                }

                return result;
            }

            if (businessData.Permission.Cache.MessageGroup.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessMessageGroupMessage:9";
                result.Message = "Business does not have permission to access message groups";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.MessageGroup.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.MessageGroup.DisabledFullReason;
                }

                return result;
            }

            formData.TryGetValue("groupId", out StringValues groupIdValue);
            string? groupId = groupIdValue.ToString();

            if (string.IsNullOrWhiteSpace(groupId))
            {
                result.Code = "SaveBusinessMessageGroupMessage:10";
                result.Message = "Missing group id";
                return result;
            }

            bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheMessageGroupExists(businessId, groupId);
            if (!groupExists)
            {
                result.Code = "SaveBusinessMessageGroupMessage:11";
                result.Message = "Group not found";
                return result;
            }

            if (!formData.TryGetValue("language", out var languageValue))
            {
                result.Code = "SaveBusinessMessageGroupMessage:12";
                result.Message = "Language not specified.";
                return result;
            }
            string language = languageValue.ToString();

            if (!businessData.Languages.Contains(language))
            {
                result.Code = "SaveBusinessMessageGroupMessage:13";
                result.Message = "Language not found for business.";
                return result;
            }

            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
            {
                result.Code = "SaveBusinessMessageGroupMessage:14";
                result.Message = "Invalid post type";
                return result;
            }

            formData.TryGetValue("existingCacheId", out StringValues existingCacheIdValue);
            string? existingCacheId = existingCacheIdValue.ToString();
            if (postType == "edit")
            {
                if (businessData.Permission.Cache.MessageGroup.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessMessageGroupMessage:15";
                    result.Message = "Business does not have permission to edit messages";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.MessageGroup.DisabledEditingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.MessageGroup.DisabledEditingReason;
                    }

                    return result;
                }

                if (string.IsNullOrWhiteSpace(existingCacheId))
                {
                    result.Code = "SaveBusinessMessageGroupMessage:16";
                    result.Message = "Missing existing cache id";
                    return result;
                }

                bool messageExists = await _businessManager.GetCacheManager().CheckBusinessCacheMessageGroupMessageExists(
                    businessId,
                    groupId,
                    language,
                    existingCacheId
                );

                if (!messageExists)
                {
                    result.Code = "SaveBusinessMessageGroupMessage:17";
                    result.Message = "Message not found";
                    return result;
                }
            }
            else if (postType == "new")
            {
                if (businessData.Permission.Cache.MessageGroup.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessMessageGroupMessage:18";
                    result.Message = "Business does not have permission to add messages";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.MessageGroup.DisabledAddingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.MessageGroup.DisabledAddingReason;
                    }

                    return result;
                }
            }

            var updateResult = await _businessManager.GetCacheManager().AddOrUpdateMessageGroupMessage(
                businessId,
                groupId,
                formData,
                postType,
                language,
                existingCacheId
            );
            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessMessageGroupMessage:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = updateResult.Data;
            return result;
        }

        [HttpPost("/app/user/business/{businessId}/cache/audiogroups/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheAudioGroup?>> SaveBusinessAudioGroup(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheAudioGroup?>();

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserAndBusinessSessionAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkBusinessesDisabled: true,
                checkBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessAudioGroup:{userSessionAndBusinessValidationResult.Code}",
                    userSessionAndBusinessValidationResult.Message
                );
            }
            var userData = userSessionAndBusinessValidationResult.Data!.userData!;
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            if (businessData.Permission.Cache.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessAudioGroup:9";
                result.Message = "Business does not have permission to access cache";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.DisabledFullReason;
                }

                return result;
            }

            if (businessData.Permission.Cache.AudioGroup.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessAudioGroup:10";
                result.Message = "Business does not have permission to access audio groups";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.AudioGroup.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.AudioGroup.DisabledFullReason;
                }

                return result;
            }

            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
            {
                result.Code = "SaveBusinessAudioGroup:11";
                result.Message = "Invalid post type";
                return result;
            }

            formData.TryGetValue("existingGroupId", out StringValues existingGroupIdValue);
            string? existingGroupId = existingGroupIdValue.ToString();

            if (postType == "edit")
            {
                if (businessData.Permission.Cache.AudioGroup.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessAudioGroup:12";
                    result.Message = "Business does not have permission to edit audio groups";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.AudioGroup.DisabledEditingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.AudioGroup.DisabledEditingReason;
                    }

                    return result;
                }

                if (string.IsNullOrWhiteSpace(existingGroupId))
                {
                    result.Code = "SaveBusinessAudioGroup:13";
                    result.Message = "Missing existing group id";
                    return result;
                }

                bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheAudioGroupExists(businessId, existingGroupId);
                if (!groupExists)
                {
                    result.Code = "SaveBusinessAudioGroup:14";
                    result.Message = "Group not found";
                    return result;
                }
            }
            else if (postType == "new")
            {
                if (businessData.Permission.Cache.AudioGroup.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessAudioGroup:15";
                    result.Message = "Business does not have permission to add audio groups";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.AudioGroup.DisabledAddingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.AudioGroup.DisabledAddingReason;
                    }

                    return result;
                }
            }

            var updateResult = await _businessManager.GetCacheManager().AddOrUpdateAudioGroup(businessId, formData, postType, existingGroupId);
            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessAudioGroup:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = updateResult.Data;
            return result;
        }

        [HttpPost("/app/user/business/{businessId}/cache/audiogroups/audios/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheAudio?>> SaveBusinessAudioGroupAudio(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheAudio?>();

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserAndBusinessSessionAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkBusinessesDisabled: true,
                checkBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessAudioGroupAudio:{userSessionAndBusinessValidationResult.Code}",
                    userSessionAndBusinessValidationResult.Message
                );
            }
            var userData = userSessionAndBusinessValidationResult.Data!.userData!;
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            if (businessData.Permission.Cache.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessAudioGroupAudio:8";
                result.Message = "Business does not have permission to access cache";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.DisabledFullReason;
                }

                return result;
            }

            if (businessData.Permission.Cache.AudioGroup.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessAudioGroupAudio:9";
                result.Message = "Business does not have permission to access audio groups";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.AudioGroup.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.AudioGroup.DisabledFullReason;
                }

                return result;
            }

            formData.TryGetValue("groupId", out StringValues groupIdValue);
            string? groupId = groupIdValue.ToString();

            if (string.IsNullOrWhiteSpace(groupId))
            {
                result.Code = "SaveBusinessAudioGroupAudio:10";
                result.Message = "Missing group id";
                return result;
            }

            bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheAudioGroupExists(businessId, groupId);
            if (!groupExists)
            {
                result.Code = "SaveBusinessAudioGroupAudio:11";
                result.Message = "Group not found";
                return result;
            }

            if (!formData.TryGetValue("language", out var languageValue))
            {
                result.Code = "SaveBusinessAudioGroupAudio:12";
                result.Message = "Language not specified.";
                return result;
            }
            string language = languageValue.ToString();

            if (!businessData.Languages.Contains(language))
            {
                result.Code = "SaveBusinessAudioGroupAudio:13";
                result.Message = "Language not found for business.";
                return result;
            }

            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
            {
                result.Code = "SaveBusinessAudioGroupAudio:14";
                result.Message = "Invalid post type";
                return result;
            }

            formData.TryGetValue("existingCacheId", out StringValues existingCacheIdValue);
            string? existingCacheId = existingCacheIdValue.ToString();
            if (postType == "edit")
            {
                if (businessData.Permission.Cache.AudioGroup.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessAudioGroupAudio:15";
                    result.Message = "Business does not have permission to edit audios";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.AudioGroup.DisabledEditingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.AudioGroup.DisabledEditingReason;
                    }

                    return result;
                }

                if (string.IsNullOrWhiteSpace(existingCacheId))
                {
                    result.Code = "SaveBusinessAudioGroupAudio:16";
                    result.Message = "Missing existing cache id";
                    return result;
                }

                bool audioExists = await _businessManager.GetCacheManager().CheckBusinessCacheAudioGroupAudioExists(
                    businessId,
                    groupId,
                    language,
                    existingCacheId
                );

                if (!audioExists)
                {
                    result.Code = "SaveBusinessAudioGroupAudio:17";
                    result.Message = "Audio not found";
                    return result;
                }
            }
            else if (postType == "new")
            {
                if (businessData.Permission.Cache.AudioGroup.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessAudioGroupAudio:18";
                    result.Message = "Business does not have permission to add audios";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.AudioGroup.DisabledAddingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.AudioGroup.DisabledAddingReason;
                    }

                    return result;
                }
            }

            var updateResult = await _businessManager.GetCacheManager().AddOrUpdateAudioGroupAudio(
                businessId,
                groupId,
                formData,
                postType,
                language,
                existingCacheId
            );
            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessAudioGroupAudio:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = updateResult.Data;
            return result;
        }

        [HttpPost("/app/user/business/{businessId}/cache/embeddinggroups/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheEmbeddingGroup?>> SaveBusinessEmbeddingGroup(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheEmbeddingGroup?>();

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserAndBusinessSessionAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkBusinessesDisabled: true,
                checkBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessEmbeddingGroup:{userSessionAndBusinessValidationResult.Code}",
                    userSessionAndBusinessValidationResult.Message
                );
            }
            var userData = userSessionAndBusinessValidationResult.Data!.userData!;
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            if (businessData.Permission.Cache.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessEmbeddingGroup:9";
                result.Message = "Business does not have permission to access cache";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.DisabledFullReason;
                }

                return result;
            }

            if (businessData.Permission.Cache.EmbeddingGroup.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessEmbeddingGroup:10";
                result.Message = "Business does not have permission to access embedding groups";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.EmbeddingGroup.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.EmbeddingGroup.DisabledFullReason;
                }

                return result;
            }

            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
            {
                result.Code = "SaveBusinessEmbeddingGroup:11";
                result.Message = "Invalid post type";
                return result;
            }

            formData.TryGetValue("existingGroupId", out StringValues existingGroupIdValue);
            string? existingGroupId = existingGroupIdValue.ToString();

            if (postType == "edit")
            {
                if (businessData.Permission.Cache.EmbeddingGroup.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessEmbeddingGroup:12";
                    result.Message = "Business does not have permission to edit embedding groups";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.EmbeddingGroup.DisabledEditingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.EmbeddingGroup.DisabledEditingReason;
                    }

                    return result;
                }

                if (string.IsNullOrWhiteSpace(existingGroupId))
                {
                    result.Code = "SaveBusinessEmbeddingGroup:13";
                    result.Message = "Missing existing group id";
                    return result;
                }

                bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheEmbeddingGroupExists(businessId, existingGroupId);
                if (!groupExists)
                {
                    result.Code = "SaveBusinessEmbeddingGroup:14";
                    result.Message = "Group not found";
                    return result;
                }
            }
            else if (postType == "new")
            {
                if (businessData.Permission.Cache.EmbeddingGroup.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessEmbeddingGroup:15";
                    result.Message = "Business does not have permission to add embedding groups";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.EmbeddingGroup.DisabledAddingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.EmbeddingGroup.DisabledAddingReason;
                    }

                    return result;
                }
            }

            var updateResult = await _businessManager.GetCacheManager().AddOrUpdateEmbeddingGroup(businessId, formData, postType, existingGroupId);
            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessEmbeddingGroup:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            return result.SetSuccessResult(updateResult.Data);
        }

        [HttpPost("/app/user/business/{businessId}/cache/embeddinggroups/embeddings/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheEmbedding?>> SaveBusinessEmbeddingGroupEmbedding(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheEmbedding?>();

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserAndBusinessSessionAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkBusinessesDisabled: true,
                checkBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessEmbeddingGroupEmbedding:{userSessionAndBusinessValidationResult.Code}",
                    userSessionAndBusinessValidationResult.Message
                );
            }
            var userData = userSessionAndBusinessValidationResult.Data!.userData!;
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            if (businessData.Permission.Cache.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessEmbeddingGroupEmbedding:8";
                result.Message = "Business does not have permission to access cache";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.DisabledFullReason;
                }

                return result;
            }

            if (businessData.Permission.Cache.EmbeddingGroup.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessEmbeddingGroupEmbedding:9";
                result.Message = "Business does not have permission to access embedding groups";

                if (!string.IsNullOrEmpty(businessData.Permission.Cache.EmbeddingGroup.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Cache.EmbeddingGroup.DisabledFullReason;
                }

                return result;
            }

            formData.TryGetValue("groupId", out StringValues groupIdValue);
            string? groupId = groupIdValue.ToString();

            if (string.IsNullOrWhiteSpace(groupId))
            {
                result.Code = "SaveBusinessEmbeddingGroupEmbedding:10";
                result.Message = "Missing group id";
                return result;
            }

            bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheEmbeddingGroupExists(businessId, groupId);
            if (!groupExists)
            {
                result.Code = "SaveBusinessEmbeddingGroupEmbedding:11";
                result.Message = "Group not found";
                return result;
            }

            if (!formData.TryGetValue("language", out var languageValue))
            {
                result.Code = "SaveBusinessEmbeddingGroupEmbedding:12";
                result.Message = "Language not specified.";
                return result;
            }
            string language = languageValue.ToString();

            if (!businessData.Languages.Contains(language))
            {
                result.Code = "SaveBusinessEmbeddingGroupEmbedding:13";
                result.Message = "Language not found for business.";
                return result;
            }

            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
            {
                result.Code = "SaveBusinessEmbeddingGroupEmbedding:14";
                result.Message = "Invalid post type";
                return result;
            }

            formData.TryGetValue("existingCacheId", out StringValues existingCacheIdValue);
            string? existingCacheId = existingCacheIdValue.ToString();
            if (postType == "edit")
            {
                if (businessData.Permission.Cache.EmbeddingGroup.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessEmbeddingGroupEmbedding:15";
                    result.Message = "Business does not have permission to edit embeddings";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.EmbeddingGroup.DisabledEditingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.EmbeddingGroup.DisabledEditingReason;
                    }

                    return result;
                }

                if (string.IsNullOrWhiteSpace(existingCacheId))
                {
                    result.Code = "SaveBusinessEmbeddingGroupEmbedding:16";
                    result.Message = "Missing existing cache id";
                    return result;
                }

                bool embeddingExists = await _businessManager.GetCacheManager().CheckBusinessCacheEmbeddingGroupEmbeddingExists(
                    businessId,
                    groupId,
                    language,
                    existingCacheId
                );

                if (!embeddingExists)
                {
                    result.Code = "SaveBusinessEmbeddingGroupEmbedding:17";
                    result.Message = "Embedding not found";
                    return result;
                }
            }
            else if (postType == "new")
            {
                if (businessData.Permission.Cache.EmbeddingGroup.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessEmbeddingGroupEmbedding:18";
                    result.Message = "Business does not have permission to add embeddings";

                    if (!string.IsNullOrEmpty(businessData.Permission.Cache.EmbeddingGroup.DisabledAddingReason))
                    {
                        result.Message += ": " + businessData.Permission.Cache.EmbeddingGroup.DisabledAddingReason;
                    }

                    return result;
                }
            }

            var updateResult = await _businessManager.GetCacheManager().AddOrUpdateEmbeddingGroupEmbedding(
                businessId,
                groupId,
                formData,
                postType,
                language,
                existingCacheId
            );
            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessEmbeddingGroupEmbedding:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            return result.SetSuccessResult(updateResult.Data);
        }

    }
}
