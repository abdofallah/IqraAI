using IqraCore.Entities.Business;
using IqraCore.Entities.Business.WhiteLabelDomain;
using IqraCore.Entities.Helpers;
using IqraCore.Models.User;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.API.v1.Business
{
    [ApiController]
    [Route("api/v1/business/{businessId}")]
    public class APIv1BusinessController : Controller
    {
        private readonly UserAPIValidationHelper _userAPIValidationHelper;
        private readonly BusinessManager _businessManager;

        public APIv1BusinessController(UserAPIValidationHelper userAPIValidationHelper, BusinessManager businessManager)
        {
            _userAPIValidationHelper = userAPIValidationHelper;
            _businessManager = businessManager;
        }

        [HttpGet]
        public async Task<FunctionReturnResult<GetUserBusinessFullReturnModel?>> GetBusiness(long businessId)
        {
            var result = new FunctionReturnResult<GetUserBusinessFullReturnModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateAPIUserAndBusinessSessionAsync(Request, businessId);
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

                FunctionReturnResult<List<BusinessWhiteLabelDomain>?> businessWhiteLabelDomainResult = await _businessManager.GetSettingsManager().GetUserBusinessWhiteLabelDomainByIds(userData.Email, businessId, businessData.WhiteLabelDomainIds);
                if (!businessWhiteLabelDomainResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusiness:{businessWhiteLabelDomainResult.Code}",
                        businessWhiteLabelDomainResult.Message
                    );
                }

                var fullBusinessReturnModel = new GetUserBusinessFullReturnModel()
                {
                    BusinessData = businessData,
                    BusinessApp = businessAppResult.Data!,
                    BusinessWhiteLabelDomain = businessWhiteLabelDomainResult.Data!
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
