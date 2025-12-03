using IqraCore.Entities.Helpers;
using IqraCore.Models.User.GetMasterUserDataModel;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.User
{
    public class UserController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly BusinessLogoRepository _businessLogoRepository;

        public UserController(
            UserSessionValidationHelper userSessionValidationHelper,
            BusinessLogoRepository businessLogoRepository
        )
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _businessLogoRepository = businessLogoRepository;
        }

        [HttpGet("/app/user")]
        public async Task<FunctionReturnResult<GetMasterUserDataModel?>> GetMasterUserDataModel()
        {
            var result = new FunctionReturnResult<GetMasterUserDataModel?>();

            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request, checkUserDisabled: true);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetMasterUserDataModel:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;

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
            catch ( Exception ex ) {
                return result.SetFailureResult(
                    "GetMasterUserDataModel:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }
    }
}
