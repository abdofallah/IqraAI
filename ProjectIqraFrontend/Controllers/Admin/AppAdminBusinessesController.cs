using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminBusinessesController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly BusinessManager _businessManager;

        public AppAdminBusinessesController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _businessManager = businessManager;
        }

        [HttpPost("/app/admin/businesses")]
        public async Task<FunctionReturnResult<List<BusinessData>?>> GetBusinesses(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinesses:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var businessesResult = await _businessManager.GetBusinesses(page, pageSize);
                if (!businessesResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinesses:{businessesResult.Code}",
                        businessesResult.Message
                    );
                }

                return result.SetSuccessResult(businessesResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetBusinesses:EXCEPTION",
                    $"Failed to get businesses. Exception: {ex.Message}"
                );
            }
        }
    }
}
