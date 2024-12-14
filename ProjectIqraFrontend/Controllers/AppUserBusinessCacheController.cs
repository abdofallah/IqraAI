using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ProjectIqraFrontend.Controllers
{
    public class AppUserBusinessCacheController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;

        public AppUserBusinessCacheController(UserManager userManager, BusinessManager businessManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/cache/messagegroups/save")]
        public async Task<FunctionReturnResult<BusinessAppCacheMessageGroup?>> SaveBusinessMessageGroup(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCacheMessageGroup?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessMessageGroup:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveBusinessMessageGroup:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessMessageGroup:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessMessageGroup:4";
                result.Message = "User does not have permission to edit businesses";

                if (user.Permission.Business.DisableBusinessesAt != null && !string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                if (!string.IsNullOrEmpty(user.Permission.Business.EditBusinessDisableReason))
                {
                    result.Message += ": " + user.Permission.Business.EditBusinessDisableReason;
                }

                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "SaveBusinessMessageGroup:5";
                result.Message = "User does not own this business";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessMessageGroup:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessMessageGroup:8";
                result.Message = "Business is currently disabled";

                if (businessResult.Data.Permission.DisabledFullAt != null && !string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledEditingReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledEditingReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Cache.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessMessageGroup:9";
                result.Message = "Business does not have permission to access cache";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Cache.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Cache.DisabledFullReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Cache.MessageGroup.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessMessageGroup:10";
                result.Message = "Business does not have permission to access message groups";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Cache.MessageGroup.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Cache.MessageGroup.DisabledFullReason;
                }

                return result;
            }

            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || (postType != "new" && postType != "edit"))
            {
                result.Code = "SaveBusinessMessageGroup:11";
                result.Message = "Invalid post type";
                return result;
            }

            formData.TryGetValue("existingGroupId", out StringValues existingGroupIdValue);
            string? existingGroupId = existingGroupIdValue.ToString();

            if (postType == "edit")
            {
                if (businessResult.Data.Permission.Cache.MessageGroup.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessMessageGroup:12";
                    result.Message = "Business does not have permission to edit message groups";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Cache.MessageGroup.DisabledEditingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Cache.MessageGroup.DisabledEditingReason;
                    }

                    return result;
                }          

                if (string.IsNullOrWhiteSpace(existingGroupId))
                {
                    result.Code = "SaveBusinessMessageGroup:13";
                    result.Message = "Missing existing group id";
                    return result;
                }

                bool groupExists = await _businessManager.CheckBusinessCacheMessageGroupExists(businessId, existingGroupId);
                if (!groupExists)
                {
                    result.Code = "SaveBusinessMessageGroup:14";
                    result.Message = "Group not found";
                    return result;
                }
            }
            else if (postType == "new")
            {
                if (businessResult.Data.Permission.Cache.MessageGroup.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessMessageGroup:15";
                    result.Message = "Business does not have permission to add message groups";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Cache.MessageGroup.DisabledAddingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Cache.MessageGroup.DisabledAddingReason;
                    }

                    return result;
                }
            }

            var updateResult = await _businessManager.AddOrUpdateMessageGroup(businessId, formData, postType, existingGroupId);
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

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessMessageGroupMessage:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveBusinessMessageGroupMessage:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessMessageGroupMessage:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessMessageGroupMessage:5";
                result.Message = "User does not have permission to edit businesses";

                if (user.Permission.Business.DisableBusinessesAt != null && !string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                if (!string.IsNullOrEmpty(user.Permission.Business.EditBusinessDisableReason))
                {
                    result.Message += ": " + user.Permission.Business.EditBusinessDisableReason;
                }

                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "SaveBusinessMessageGroupMessage:6";
                result.Message = "User does not own this business";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessMessageGroupMessage:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessMessageGroupMessage:7";
                result.Message = "Business is currently disabled";

                if (businessResult.Data.Permission.DisabledFullAt != null && !string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledEditingReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledEditingReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Cache.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessMessageGroupMessage:8";
                result.Message = "Business does not have permission to access cache";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Cache.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Cache.DisabledFullReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Cache.MessageGroup.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessMessageGroupMessage:9";
                result.Message = "Business does not have permission to access message groups";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Cache.MessageGroup.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Cache.MessageGroup.DisabledFullReason;
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

            bool groupExists = await _businessManager.CheckBusinessCacheMessageGroupExists(businessId, groupId);
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

            if (!businessResult.Data.Languages.Contains(language))
            {
                result.Code = "SaveBusinessMessageGroupMessage:13";
                result.Message = "Language not found for business.";
                return result;
            }

            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || (postType != "new" && postType != "edit"))
            {
                result.Code = "SaveBusinessMessageGroupMessage:14";
                result.Message = "Invalid post type";
                return result;
            }

            formData.TryGetValue("existingCacheId", out StringValues existingCacheIdValue);
            string? existingCacheId = existingCacheIdValue.ToString();
            if (postType == "edit")
            {
                if (businessResult.Data.Permission.Cache.MessageGroup.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessMessageGroupMessage:15";
                    result.Message = "Business does not have permission to edit messages";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Cache.MessageGroup.DisabledEditingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Cache.MessageGroup.DisabledEditingReason;
                    }

                    return result;
                }    

                if (string.IsNullOrWhiteSpace(existingCacheId))
                {
                    result.Code = "SaveBusinessMessageGroupMessage:16";
                    result.Message = "Missing existing cache id";
                    return result;
                }

                bool messageExists = await _businessManager.CheckBusinessCacheMessageGroupMessageExists(
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
                if (businessResult.Data.Permission.Cache.MessageGroup.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessMessageGroupMessage:18";
                    result.Message = "Business does not have permission to add messages";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Cache.MessageGroup.DisabledAddingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Cache.MessageGroup.DisabledAddingReason;
                    }

                    return result;
                }
            }

            var updateResult = await _businessManager.AddOrUpdateMessageGroupMessage(
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
    }
}
