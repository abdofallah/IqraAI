using IqraCore.Entities.Helpers;
using IqraCore.Entities.Languages;
using IqraInfrastructure.Repositories.Languages;

namespace IqraInfrastructure.Services.Languages
{
    public class LanguagesManager
    {
        private readonly LanguagesRepository _languagesRepository;
        public LanguagesManager(LanguagesRepository languagesRepository)
        {
            _languagesRepository = languagesRepository;
        }

        public async Task<FunctionReturnResult<List<LanguagesData>?>> GetLanguagesList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<LanguagesData>?>();

            var getResult = await _languagesRepository.GetLanguagesList(page, pageSize);
            if (getResult == null)
            {
                result.Code = "GetLanguagesList:1";
                return result;
            }
            
            result.Success = true;
            result.Data = getResult;
            return result;
        }
    }
}
