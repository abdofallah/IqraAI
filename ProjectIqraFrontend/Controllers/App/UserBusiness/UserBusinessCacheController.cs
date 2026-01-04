using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using IqraCore.Entities.Validation;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessCacheController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;

        public UserBusinessCacheController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _businessManager = businessManager;
            _whiteLabelContext = whiteLabelContext;
        }

        [HttpPost("/app/user/business/{businessId}/cache/messagegroups/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheMessageGroup?>> SaveBusinessCacheMessageGroup(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheMessageGroup?>();

            try
            {
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                ) {
                    return result.SetFailureResult(
                        "SaveBusinessCacheMessageGroup:INVALID_POST_TYPE",
                        "Invalid post type"
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.MessageGroup",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.MessageGroup",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessCacheMessageGroup:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                string? existingGroupId = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("existingGroupId", out StringValues existingGroupIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessCacheMessageGroup:MISSING_EXISTING_GROUP_ID",
                            "Existing cache message group id is required for edit mode."
                        );
                    }
                    existingGroupId = existingGroupIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(existingGroupId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessCacheMessageGroup:INVALID_EXISTING_GROUP_ID",
                            "Existing cache message group id is invalid."
                        );
                    }

                    bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheMessageGroupExists(businessId, existingGroupId);
                    if (!groupExists)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessCacheMessageGroup:GROUP_NOT_FOUND",
                            "Cache message group not found"
                        );
                    }
                }

                var updateResult = await _businessManager.GetCacheManager().AddOrUpdateMessageGroup(businessId, formData, postType, existingGroupId);
                if (!updateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessCacheMessageGroup:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                   "SaveBusinessCacheMessageGroup:Exception",
                   $"Exception: {ex.Message}"
               );
            }
        }

        [HttpPost("/app/user/business/{businessId}/cache/messagegroups/messages/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheMessage?>> SaveBusinessCacheMessageGroupMessageItem(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheMessage?>();

            try
            {
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                )
                {
                    return result.SetFailureResult(
                        "SaveBusinessCacheMessageGroup:INVALID_POST_TYPE",
                        "Invalid post type"
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.MessageGroup",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.MessageGroup",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessCacheMessageGroupMessageItem:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                if (!formData.TryGetValue("groupId", out StringValues groupIdValue))
                {
                    return result.SetFailureResult(
                        "SaveBusinessCacheMessageGroupMessageItem:GROUP_ID_NOT_SPECIFIED",
                        "Cache message group id not specified."
                    );
                }
                string? groupId = groupIdValue.ToString();
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    return result.SetFailureResult(
                        "SaveBusinessCacheMessageGroupMessageItem:INVALID_GROUP_ID",
                        "Cache message group id is invalid or empty."
                    );
                }

                bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheMessageGroupExists(businessId, groupId);
                if (!groupExists)
                {
                    return result.SetFailureResult(
                        "SaveBusinessCacheMessageGroupMessageItem:GROUP_NOT_FOUND",
                        "Cache message group not found."
                    );
                }

                if (!formData.TryGetValue("language", out var languageValue))
                {
                    return result.SetFailureResult(
                        "SaveBusinessCacheMessageGroupMessageItem:LANGUAGE_NOT_SPECIFIED",
                        "Language not specified."
                    );
                }
                string language = languageValue.ToString();
                if (!businessData.Languages.Contains(language))
                {
                    return result.SetFailureResult(
                        "SaveBusinessCacheMessageGroupMessageItem:LANGUAGE_NOT_FOUND",
                        "Language not found for business."
                    );
                }

                string? existingCacheId = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("existingCacheId", out StringValues existingCacheIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessCacheMessageGroupMessageItem:EXISTING_CACHE_ID_NOT_SPECIFIED",
                            "Existing message cache group message item id not specified."
                        );
                    }
                    existingCacheId = existingCacheIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(existingCacheId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessCacheMessageGroupMessageItem:INVALID_EXISTING_CACHE_ID",
                            "Existing message cache group message item id is invalid or empty."
                        );
                    }

                    bool messageExists = await _businessManager.GetCacheManager().CheckBusinessCacheMessageGroupMessageExists(
                        businessId,
                        groupId,
                        language,
                        existingCacheId
                    );

                    if (!messageExists)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessCacheMessageGroupMessageItem:MESSAGE_ITEM_NOT_FOUND",
                            "Cache message group message item not found."
                        );
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
                    return result.SetFailureResult(
                        $"SaveBusinessCacheMessageGroupMessageItem:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessCacheMessageGroupMessageItem:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/cache/audiogroups/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheAudioGroup?>> SaveBusinessAudioGroup(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheAudioGroup?>();

            try
            {
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                )
                {
                    return result.SetFailureResult(
                        "SaveBusinessAudioGroup:INVALID_POST_TYPE",
                        "Invalid post type"
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.AudioGroup",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.AudioGroup",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessAudioGroup:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                string? existingGroupId = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("existingGroupId", out StringValues existingGroupIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAudioGroup:EXISTING_GROUP_ID_NOT_SPECIFIED",
                            "Existing group id not specified."
                        );
                    }
                    existingGroupId = existingGroupIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(existingGroupId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAudioGroup:INVALID_EXISTING_GROUP_ID",
                            "Existing group id is invalid or empty."
                        );
                    }

                    bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheAudioGroupExists(businessId, existingGroupId);
                    if (!groupExists)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAudioGroup:GROUP_NOT_FOUND",
                            "Cache audio group not found"
                        );
                    }
                }

                var updateResult = await _businessManager.GetCacheManager().AddOrUpdateAudioGroup(businessId, formData, postType, existingGroupId);
                if (!updateResult.Success)
                {
                    result.Code = "SaveBusinessAudioGroup:" + updateResult.Code;
                    result.Message = updateResult.Message;
                    return result;
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessAudioGroup:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/cache/audiogroups/audios/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheAudio?>> SaveBusinessAudioGroupAudioItem(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheAudio?>();

            try
            {
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                )
                {
                    return result.SetFailureResult(
                        "SaveBusinessAudioGroupAudioItem:INVALID_POST_TYPE",
                        "Invalid post type"
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.AudioGroup",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.AudioGroup",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessAudioGroupAudioItem:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                if (!formData.TryGetValue("groupId", out StringValues groupIdValue))
                {
                    return result.SetFailureResult(
                        "SaveBusinessAudioGroupAudioItem:GROUP_ID_NOT_SPECIFIED",
                        "Cache audio group id not specified."
                    );
                }
                string? groupId = groupIdValue.ToString();
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    return result.SetFailureResult(
                        "SaveBusinessAudioGroupAudioItem:GROUP_ID_EMPTY",
                        "Cache audio group id empty."
                    );
                }

                bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheAudioGroupExists(businessId, groupId);
                if (!groupExists)
                {
                    return result.SetFailureResult(
                        "SaveBusinessAudioGroupAudioItem:GROUP_NOT_FOUND",
                        "Cache audio group not found"
                    );
                }

                if (!formData.TryGetValue("language", out var languageValue))
                {
                    return result.SetFailureResult(
                        "SaveBusinessAudioGroupAudioItem:LANGUAGE_NOT_SPECIFIED",
                        "Language not specified."
                    );
                }
                string language = languageValue.ToString();
                if (!businessData.Languages.Contains(language))
                {
                    return result.SetFailureResult(
                        "SaveBusinessAudioGroupAudioItem:LANGUAGE_NOT_FOUND",
                        "Language not found in business"
                    );
                }

                string? existingCacheId = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("existingCacheId", out StringValues existingCacheIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAudioGroupAudioItem:AUDIO_ITEM_ID_NOT_FOUND",
                            "Existing audio cache group item id not found"
                        );
                    }
                    existingCacheId = existingCacheIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(existingCacheId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAudioGroupAudioItem:AUDIO_ITEM_ID_EMPTY",
                            "Empty existing audio cache group item id"
                        );
                    }

                    bool audioExists = await _businessManager.GetCacheManager().CheckBusinessCacheAudioGroupAudioExists(
                        businessId,
                        groupId,
                        language,
                        existingCacheId
                    );

                    if (!audioExists)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAudioGroupAudioItem:AUDIO_ITEM_NOT_FOUND",
                            "Audio cache group audio item not found"
                        );
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
                    return result.SetFailureResult(
                        $"SaveBusinessAudioGroupAudioItem:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessAudioGroupAudioItem:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/cache/embeddinggroups/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheEmbeddingGroup?>> SaveBusinessEmbeddingGroup(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheEmbeddingGroup?>();

            try
            {
                // Validation
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                )
                {
                    return result.SetFailureResult(
                        "SaveBusinessEmbeddingGroup:INVALID_POST_TYPE",
                        "Invalid post type"
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.EmbeddingGroup",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.EmbeddingGroup",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessEmbeddingGroup:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                string? existingGroupId = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("existingGroupId", out StringValues existingGroupIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessEmbeddingGroup:EXISTING_GROUP_ID_MISSING",
                            "Missing existing cache embedding group id"
                        );
                    }
                    existingGroupId = existingGroupIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(existingGroupId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessEmbeddingGroup:EXISTING_GROUP_ID_EMPTY",
                            "Empty existing cache embedding group id"
                        );
                    }

                    bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheEmbeddingGroupExists(businessId, existingGroupId);
                    if (!groupExists)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessEmbeddingGroup:GROUP_NOT_FOUND",
                            "Cache embedding group not found"
                        );
                    }
                }

                var updateResult = await _businessManager.GetCacheManager().AddOrUpdateEmbeddingGroup(businessId, formData, postType, existingGroupId);
                if (!updateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessEmbeddingGroup:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessEmbeddingGroup:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/cache/embeddinggroups/embeddings/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheEmbedding?>> SaveBusinessEmbeddingGroupEmbeddingItem(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheEmbedding?>();

            try
            {
                // Validation
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                )
                {
                    return result.SetFailureResult(
                        "SaveBusinessEmbeddingGroup:INVALID_POST_TYPE",
                        "Invalid post type"
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.CachePermissions",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.EmbeddingGroup",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Cache.EmbeddingGroup",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessEmbeddingGroupEmbeddingItem:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                if (!formData.TryGetValue("groupId", out StringValues groupIdValue))
                {
                    return result.SetFailureResult(
                        "SaveBusinessEmbeddingGroupEmbeddingItem:GROUP_ID_MISSING",
                        "Missing cache embedding group id"
                    );
                }
                string? groupId = groupIdValue.ToString();
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    return result.SetFailureResult(
                        "SaveBusinessEmbeddingGroupEmbeddingItem:EMPTY_GROUP_ID",
                        "Empty cache embedding group id"
                    );
                }

                bool groupExists = await _businessManager.GetCacheManager().CheckBusinessCacheEmbeddingGroupExists(businessId, groupId);
                if (!groupExists)
                {
                    return result.SetFailureResult(
                        "SaveBusinessEmbeddingGroupEmbeddingItem:GROUP_NOT_FOUND",
                        "Cache embedding group not found"
                    );
                }

                if (!formData.TryGetValue("language", out var languageValue))
                {
                    return result.SetFailureResult(
                        "SaveBusinessEmbeddingGroupEmbeddingItem:MISSING_LANGUAGE",
                        "Missing language"
                    );
                }
                string language = languageValue.ToString();
                if (!businessData.Languages.Contains(language))
                {
                    return result.SetFailureResult(
                        "SaveBusinessEmbeddingGroupEmbeddingItem:LANGUAGE_NOT_FOUND",
                        "Language not found for business."
                    );
                }

                string? existingCacheId = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("existingCacheId", out StringValues existingCacheIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessEmbeddingGroupEmbeddingItem:MISSING_EXISTING_CACHE_ITEM_ID",
                            "Missing existing cache embedding group embedding item id"
                        );
                    }
                    existingCacheId = existingCacheIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(existingCacheId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessEmbeddingGroupEmbeddingItem:EMPTY_EXISTING_CACHE_ITEM_ID",
                            "Empty existing cache embedding group embedding item id"
                        );
                    }

                    bool embeddingExists = await _businessManager.GetCacheManager().CheckBusinessCacheEmbeddingGroupEmbeddingExists(
                        businessId,
                        groupId,
                        language,
                        existingCacheId
                    );

                    if (!embeddingExists)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessEmbeddingGroupEmbeddingItem:EMBEDDING_ITEM_NOT_FOUND",
                            "Cache embedding group embedding item not found"
                        );
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
                    return result.SetFailureResult(
                        $"SaveBusinessEmbeddingGroupEmbeddingItem:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessEmbeddingGroupEmbeddingItem:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

    }
}
