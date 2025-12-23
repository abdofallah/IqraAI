using IqraCore.Entities.Helpers;

namespace IqraCore.Interfaces.User
{
    public interface IUserUsageValidationManager
    {
        Task<FunctionReturnResult> CheckUsageConcurrency(long businessId, string featureKey);
        Task<FunctionReturnResult> DecreaseUsageConcurrency(long businessId, string featureKey, object parentReference, object? childReference);
        Task<FunctionReturnResult> DecreaseUsageConcurrency(string userEmail, long businessId, string featureKey, object parentReference, object? childReference);
        Task<FunctionReturnResult> TryIncreaseUsageConcurrency(long businessId, string featureKey, object parentReference, object? childReference);
        Task<FunctionReturnResult> ValidateCallPermissionAsync(long businessId);
    }
}
