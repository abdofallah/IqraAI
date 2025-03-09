using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.Region;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminRegionsController : Controller
    {
        public readonly RegionManager _regionManager;
        public readonly UserManager _userManager;

        public AppAdminRegionsController(RegionManager regionManager, UserManager userManager)
        {
            _regionManager = regionManager;
            _userManager = userManager;
        }

        [HttpPost("/app/admin/regions")]
        public async Task<FunctionReturnResult<List<RegionData>?>> GetRegions(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<RegionData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetRegions:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetRegions:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetRegions:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetRegions:4";
                result.Message = "User is not an admin";
                return result;
            }

            var regionsResult = await _regionManager.GetRegions(page, pageSize);
            if (!regionsResult.Success)
            {
                result.Code = "GetRegions:" + regionsResult.Code;
                result.Message = regionsResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = regionsResult.Data;

            return result;
        }

    }
}
