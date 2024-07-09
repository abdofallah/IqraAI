using IqraInfrastructure.Repositories.App;

namespace IqraInfrastructure.Services.App
{
    public class ApiManager
    {
        private readonly AppRepository _appRepository;
        public ApiManager (AppRepository appRepository)
        {
            _appRepository = appRepository;
        }

        public async Task<bool> ValidateApiKey(string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return false;
            return await _appRepository.ApiKeyExists(apiKey);
        }
    }
}
