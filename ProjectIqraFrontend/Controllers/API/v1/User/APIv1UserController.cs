using IqraCore.Entities.Helpers;
using IqraCore.Models.User.GetMasterUserDataModel;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.API.v1.User
{
    [ApiController]
    [Route("api/v1/user")]
    public class APIv1UserController : Controller
    {
        private readonly UserAPIValidationHelper _userAPIValidationHelper;
        private readonly BusinessLogoRepository _businessLogoRepository;

        public APIv1UserController(
            UserAPIValidationHelper userAPIValidationHelper,
            BusinessLogoRepository businessLogoRepository
        )
        {
            _userAPIValidationHelper = userAPIValidationHelper;
            _businessLogoRepository = businessLogoRepository;
        }

        [HttpGet]
        public async Task<FunctionReturnResult<GetMasterUserDataModel?>> GetUserData()
        {
            var result = new FunctionReturnResult<GetMasterUserDataModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateUserAPIAsync(Request);
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetConversations:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var userData = apiKeyValidaiton.Data!.userData!;

                GetMasterUserDataModel userDataModel = new GetMasterUserDataModel(userData);

                if (userData.WhiteLabel.ActivatedAt != null)
                {
                    var defaultPlatformLogo = userData.WhiteLabel.DefaultBranding.PlatformLogoS3StorageLink;
                    if (defaultPlatformLogo != null)
                    {
                        userDataModel.WhiteLabel.DefaultBranding.PlatformLogoUrl = _businessLogoRepository.GeneratePresignedUrl(defaultPlatformLogo.ObjectName, 86400, defaultPlatformLogo.OriginRegion);
                    }

                    var defaultPlatformIcon = userData.WhiteLabel.DefaultBranding.PlatformIconS3StorageLink;
                    if (defaultPlatformIcon != null)
                    {
                        userDataModel.WhiteLabel.DefaultBranding.PlatformIconUrl = _businessLogoRepository.GeneratePresignedUrl(defaultPlatformIcon.ObjectName, 86400, defaultPlatformIcon.OriginRegion);
                    }

                    foreach (var domain in userData.WhiteLabel.Domains)
                    {
                        var domainModel = userDataModel!.WhiteLabel.Domains.Find(x => x.CustomDomain == domain.CustomDomain);
                        if (domainModel == null) continue;

                        var domainOverrideLogo = domain.OverrideBranding.PlatformLogoS3StorageLink;
                        if (domainOverrideLogo != null)
                        {
                            domainModel.OverrideBranding.PlatformLogoUrl = _businessLogoRepository.GeneratePresignedUrl(domainOverrideLogo.ObjectName, 86400, domainOverrideLogo.OriginRegion);
                        }

                        var domainOverrideIcon = domain.OverrideBranding.PlatformIconS3StorageLink;
                        if (domainOverrideIcon != null)
                        {
                            domainModel.OverrideBranding.PlatformIconUrl = _businessLogoRepository.GeneratePresignedUrl(domainOverrideIcon.ObjectName, 86400, domainOverrideIcon.OriginRegion);
                        }
                    }
                }

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
