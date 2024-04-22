using IqraCore.Interfaces.Repositories;
using IqraInfrastructure.Services.App;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly ApiManager _apiManager;
        private readonly ModemsManager _modemsManager;

        private readonly IBusinessRepository _businessRepository;

        public AgentController (ApiManager apiManager, ModemsManager modemsManager, IBusinessRepository businessRepository)
        {
            _apiManager = apiManager;
            _modemsManager = modemsManager;

            _businessRepository = businessRepository;
        }


    }
}
