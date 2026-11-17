using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helper.Tool;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Helper.Business;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.User.Business;
using IqraCore.Models.S3Storage;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.API.v1.Business
{
    [ApiController]
    [Route("api/v1/business/{businessId}")]
    public class APIv1BusinessController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly BusinessManager _businessManager;

        public APIv1BusinessController(
            ISessionValidationAndPermissionHelper sessionValidationAndPermissionHelper,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = sessionValidationAndPermissionHelper;
            _businessManager = businessManager;
        }

        [HttpGet]
        public async Task<FunctionReturnResult<GetUserBusinessFullReturnModel?>> GetBusiness(long businessId)
        {
            var result = new FunctionReturnResult<GetUserBusinessFullReturnModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userSessionValidationAndPermissionHelper.ValidateUserAPIAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    checkAPIKeyBusinessRestriction: true,
                    // User Permissions
                    checkUserDisabled: true,
                    // User Business Permissions
                    checkUserBusinessesDisabled: true,
                    // Business Permissions
                    checkBusinessIsDisabled: true
                );
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusiness:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var userData = apiKeyValidaiton.Data!.userData!;
                var businessData = apiKeyValidaiton.Data!.businessData!;

                FunctionReturnResult<BusinessApp?> businessAppResult = await _businessManager.GetUserBusinessAppById(businessId, userData.Email);
                if (!businessAppResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusiness:{businessAppResult.Code}",
                        businessAppResult.Message
                    );
                }

                var businessMetaDataModel = _businessManager.CreateBusinessMetaDataModel(businessData);

                var businessAppModel = _businessManager.CreateBusinessAppModel(businessAppResult.Data!);

                var fullBusinessReturnModel = new GetUserBusinessFullReturnModel()
                {
                    BusinessData = businessMetaDataModel,
                    BusinessApp = businessAppModel
                };

                return result.SetSuccessResult(fullBusinessReturnModel);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetBusiness:EXCEPTION",
                    $"Internal Server Error: {ex.Message}"
                );
            }
        }

        [HttpGet("signed-file/{kind}")]
        public async Task<FunctionReturnResult<S3SignedFileModel?>> GetBusinessSignedFile(long businessId, BusinessSignedFileKindENUM kind)
        {
            var result = new FunctionReturnResult<S3SignedFileModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userSessionValidationAndPermissionHelper.ValidateUserAPIAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    checkAPIKeyBusinessRestriction: true,
                    // User Permissions
                    checkUserDisabled: true,
                    // User Business Permissions
                    checkUserBusinessesDisabled: true,
                    // Business Permissions
                    checkBusinessIsDisabled: true
                );
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessSignedFile:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var businessData = apiKeyValidaiton.Data!.businessData!;

                var signedFileResult = _businessManager.CreateBusinessSignedFile(businessData, kind);
                if (!signedFileResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessSignedFile:{signedFileResult.Code}",
                        signedFileResult.Message
                    );
                }

                return result.SetSuccessResult(signedFileResult.Data!);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetBusinessSignedFile:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpGet("agents/{agentId}/signed-file/{kind}")]
        public async Task<FunctionReturnResult<S3SignedFileModel?>> GetAgentSignedFile(long businessId, string agentId, BusinessAppAgentSignedFileKindENUM kind)
        {
            var result = new FunctionReturnResult<S3SignedFileModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userSessionValidationAndPermissionHelper.ValidateUserAPIAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    checkAPIKeyBusinessRestriction: true,
                    // User Permissions
                    checkUserDisabled: true,
                    // User Business Permissions
                    checkUserBusinessesDisabled: true,
                    // Business Permissions
                    checkBusinessIsDisabled: true
                );
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetAgentSignedFile:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }

                var signedFileResult = await _businessManager.GetAgentsManager().GetAgentSignedFile(businessId, agentId, kind);
                if (!signedFileResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetAgentSignedFile:{signedFileResult.Code}",
                        signedFileResult.Message
                    );
                }

                return result.SetSuccessResult(signedFileResult.Data!);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetAgentSignedFile:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpGet("tools/{toolId}/signed-file/{kind}")]
        public async Task<FunctionReturnResult<S3SignedFileModel?>> GetToolSignedFile(long businessId, string toolId, BusinessAppToolSignedFileKindENUM kind)
        {
            var result = new FunctionReturnResult<S3SignedFileModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userSessionValidationAndPermissionHelper.ValidateUserAPIAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    checkAPIKeyBusinessRestriction: true,
                    // User Permissions
                    checkUserDisabled: true,
                    // User Business Permissions
                    checkUserBusinessesDisabled: true,
                    // Business Permissions
                    checkBusinessIsDisabled: true
                );
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetToolSignedFile:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }

                var signedFileResult = await _businessManager.GetToolsManager().GetToolSignedFile(businessId, toolId, kind);
                if (!signedFileResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetToolSignedFile:{signedFileResult.Code}",
                        signedFileResult.Message
                    );
                }

                return result.SetSuccessResult(signedFileResult.Data!);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetToolSignedFile:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }
    }
}
