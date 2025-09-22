using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.API.v1.Business
{
    [ApiController]
    [Route("api/v1/business/{businessId}/campaigns")]
    public class APIv1BusinessCampaignsController : Controller
    {
        private readonly UserAPIValidationHelper _userAPIValidationHelper;
        private readonly BusinessManager _businessManager;

        public APIv1BusinessCampaignsController(UserAPIValidationHelper userAPIValidationHelper, BusinessManager businessManager)
        {
            _userAPIValidationHelper = userAPIValidationHelper;
            _businessManager = businessManager;
        }

        // Get Outbound Campaigns
        // Get Outbound Campaign by Id

        // Get Inbound Campaigns
        // Get Inbound Campaign by Id

        // Get Web Campaigns
        // Get Web Campaign by Id

        // IN FUTURE
        // Add/Edit Outbound Campaign
        // Add/Edit Inbound Campaign
        // Add/Edit Web Campaign
        // Delete Outbound Campaign
        // Delete Inbound Campaign
        // Delete Web Campaign
    }
}
