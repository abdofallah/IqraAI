using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Models.User.Business;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Repositories.Business;
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
        private readonly BusinessLogoRepository _businessLogoRepository;

        public APIv1BusinessController(
            UserAPIValidationHelper userAPIValidationHelper,
            BusinessManager businessManager,
            BusinessLogoRepository businessLogoRepository
        )
        {
            _userAPIValidationHelper = userAPIValidationHelper;
            _businessManager = businessManager;
            _businessLogoRepository = businessLogoRepository;
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

                var businessMetaDataModel = new GetUseBusinessFullResultMetaDataModel(businessData);
                if (businessData.LogoS3StorageLink != null)
                {
                    businessMetaDataModel.LogoUrl = _businessLogoRepository.GeneratePresignedUrl(businessData.LogoS3StorageLink.ObjectName, 86400, businessData.LogoS3StorageLink.OriginRegion);
                }

                var fullBusinessReturnModel = new GetUserBusinessFullReturnModel()
                {
                    BusinessData = businessMetaDataModel,
                    BusinessApp = businessAppResult.Data!
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
