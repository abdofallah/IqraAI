using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.User.Business;
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
    }
}
