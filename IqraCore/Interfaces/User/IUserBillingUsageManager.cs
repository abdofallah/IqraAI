using IqraCore.Entities.User.Usage.Enums;

namespace IqraCore.Interfaces.User
{
    public interface IUserBillingUsageManager
    {
        Task<bool> ProcessAndBillUsageAsync(string masterUserEmail, long businessId, List<ConsumedFeatureInput> consumedFeatures, UserUsageSourceTypeEnum sourceType, string sourceId, string description);
        public record ConsumedFeatureInput(string FeatureKey, decimal Quantity);
    }
}
