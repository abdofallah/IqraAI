using IqraCore.Entities.Business;
using IqraCore.Interfaces.Repositories;
using IqraInfrastructure.Services.App;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BusinessController : ControllerBase
    {
        private readonly ApiManager _apiManager;

        private readonly IBusinessRepository _businessRepository;

        public BusinessController(ApiManager apiManager, IBusinessRepository businessRepository)
        {
            _apiManager = apiManager;
            _businessRepository = businessRepository;
        }

        [HttpPost("GetBusinessesMetadata")]
        public async Task<IActionResult> GetBusinessesMetadata()
        {
            string? ApiKey = Request.Headers["ApiKey"];
            if ((await _apiManager.ValidateApiKey(ApiKey)) == false)
            {
                return BadRequest("Invalid Api Key");
            }

            List<Business> businesses = await _businessRepository.GetBusinessesMetadataAsync();

            return new JsonResult(new { data = businesses });
        }

        [HttpPost("GetBusinessData")]
        public async Task<IActionResult> GetBusinessData(long businessId)
        {
            string? ApiKey = Request.Headers["ApiKey"];
            if ((await _apiManager.ValidateApiKey(ApiKey)) == false)
            {
                return BadRequest("Invalid Api Key");
            }

            Business? business = await _businessRepository.GetBusinessAsync(businessId);

            if (business == null)
            {
                return NotFound("Business not found");
            }

            return new JsonResult(new { data = business });
        }
    }
}
