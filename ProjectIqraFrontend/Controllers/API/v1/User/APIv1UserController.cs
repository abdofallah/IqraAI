using IqraCore.Attributes;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.User.MasterUserDataModel;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace ProjectIqraFrontend.Controllers.API.v1.User
{
    [ApiController]
    [Route("api/v1/user")]
    public class APIv1UserController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;

        public APIv1UserController(
            ISessionValidationAndPermissionHelper sessionValidationAndPermissionHelper
        ) {
            _userSessionValidationAndPermissionHelper = sessionValidationAndPermissionHelper;
        }

        /// <summary>
        /// Retrieve Master User Data
        /// </summary>
        /// <remarks>
        /// This endpoint fetches the complete profile of the authenticated user. 
        /// It includes:
        /// - Personal account details
        /// - White-label configuration (Logos, Icons, and Custom Domains)
        /// - S3 Pre-signed URLs for branding assets
        /// 
        /// **Note:** All responses return HTTP 200. Check the `success` field in the response body to determine if the operation actually succeeded.
        /// </remarks>
        /// <returns>A result object containing the User Data or error details.</returns>
        [HttpGet]
        [OpenSourceOnly]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(FunctionReturnResult<GetMasterUserDataModel>), 200)]
        public async Task<FunctionReturnResult<GetMasterUserDataModel?>> GetUserData()
        {
            var result = new FunctionReturnResult<GetMasterUserDataModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userSessionValidationAndPermissionHelper.ValidateUserAPIWithPermissions(
                    Request: Request,
                    checkUserDisabled: true
                );
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetConversations:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var userData = apiKeyValidaiton.Data!.userData!;

                GetMasterUserDataModel userDataModel = new GetMasterUserDataModel(userData);

                return result.SetSuccessResult(userDataModel);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUserData:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }
    }
}
