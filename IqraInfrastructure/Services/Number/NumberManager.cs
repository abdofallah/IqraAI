using IqraCore.Entities.Helper.Number;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Number;
using IqraInfrastructure.Repositories.Number;
using Serilog;

namespace IqraInfrastructure.Services.Number
{
    public class NumberManager
    {
        public readonly NumberRepository _numberRepository;

        public NumberManager(NumberRepository numberRepository) {
            _numberRepository = numberRepository;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetUserNumberByIds(List<string> numberIds, string userEmail)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            if (numberIds.Count == 0)
            {
                result.Success = true;
                result.Data = new List<NumberData>();

                return result;
            }

            var numberResults = await _numberRepository.GetUserNumberByIdsAsync(numberIds, userEmail);

            if (numberResults == null) {
                result.Code = "GetUserNumberByIds:1";

                result.Message = "Null - Numbers not found for user: " + userEmail;
                Log.Logger.Error("[NumberManager] " + result.Message);

                return result;
            }

            if (numberResults.Count != numberIds.Count)
            {
                result.Code = "GetUserNumberByIds:2";

                result.Message = "Not all numbers found for user: " + userEmail;
                Log.Logger.Error("[NumberManager] " + result.Message);

                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetBusinessNumberByIds(List<string> numberIds, long businessId)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            if (numberIds.Count == 0)
            {
                result.Success = true;
                result.Data = new List<NumberData>();

                return result;
            }

            var numberResults = await _numberRepository.GetBusinessNumberByIdsAsync(numberIds, businessId);

            if (numberResults == null)
            {
                result.Code = "GetBusinessNumberByIds:1";

                result.Message = "Null - Numbers not found for business: " + businessId;
                Log.Logger.Error("[NumberManager] " + result.Message);

                return result;
            }

            if (numberResults.Count != numberIds.Count)
            {
                result.Code = "GetBusinessNumberByIds:2";

                result.Message = "Not all numbers found for business: " + businessId;
                Log.Logger.Error("[NumberManager] " + result.Message);

                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetNumbers(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            var numberResults = await _numberRepository.GetNumbersAsync(page, pageSize);

            if (numberResults == null)
            {
                result.Code = "GetNumbers:1";

                result.Message = "Null - Numbers not found";
                Log.Logger.Error("[NumberManager] " + result.Message);
                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetNumbersByProvider(NumberProviderEnum provider, int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            var numberResults = await _numberRepository.GetNumbersByProviderAsync(provider, page, pageSize);

            if (numberResults == null)
            {
                result.Code = "GetNumbersByProvider:1";

                result.Message = "Null - Numbers not found";
                Log.Logger.Error("[NumberManager] " + result.Message);
                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetUserNumbersByProvider(NumberProviderEnum provider, string email, int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            var numberResults = await _numberRepository.GetUserNumbersByProvider(provider, email, page, pageSize);

            if (numberResults == null)
            {
                result.Code = "GetUserNumbersByProvider:1";

                result.Message = "Null - Numbers not found";
                Log.Logger.Error("[NumberManager] " + result.Message);
                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetUserNumbers(string email)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            var numberResults = await _numberRepository.GetUserNumbers(email);

            if (numberResults == null)
            {
                result.Code = "GetUserNumbers:1";

                result.Message = "Null - Numbers not found";
                Log.Logger.Error("[NumberManager] " + result.Message);
                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<bool> CheckUserNumberExists(string exisitingNumberId, string userEmail)
        {
            return await _numberRepository.CheckUserNumberExists(exisitingNumberId, userEmail);
        }
    }
}
