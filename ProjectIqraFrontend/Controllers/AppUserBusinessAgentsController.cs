using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.Integrations;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers
{
    public class AppUserBusinessAgentsController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppUserBusinessAgentsController(
            UserManager userManager,
            BusinessManager businessManager,
            IntegrationsManager integrationsManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
            _integrationsManager = integrationsManager;
        }
    }
}
