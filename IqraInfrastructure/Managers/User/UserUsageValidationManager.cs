using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.User;

namespace IqraInfrastructure.Managers.User
{
    public class UserUsageValidationManager : IUserUsageValidationManager
    {
        public UserUsageValidationManager() { }

        public async Task<FunctionReturnResult> CheckUsageConcurrency(long businessId, string featureKey)
        {
            var result = new FunctionReturnResult();
            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> DecreaseUsageConcurrency(long businessId, string featureKey, object parentReference, object? childReference)
        {
            var result = new FunctionReturnResult();
            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> DecreaseUsageConcurrency(string userEmail, long businessId, string featureKey, object parentReference, object? childReference)
        {
            var result = new FunctionReturnResult();
            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> TryIncreaseUsageConcurrency(long businessId, string featureKey, object parentReference, object? childReference)
        {
            var result = new FunctionReturnResult();
            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> ValidateCallPermissionAsync(long businessId)
        {
            var result = new FunctionReturnResult();
            return result.SetSuccessResult();
        }
    }
}
