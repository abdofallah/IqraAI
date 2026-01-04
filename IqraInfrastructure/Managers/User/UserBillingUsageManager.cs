using IqraCore.Entities.User.Usage.Enums;
using IqraCore.Interfaces.User;

namespace IqraInfrastructure.Managers.User
{
    public class UserBillingUsageManager : IUserBillingUsageManager
    {
        public async Task<bool> ProcessAndBillUsageAsync(string masterUserEmail, long businessId, List<IUserBillingUsageManager.ConsumedFeatureInput> consumedFeatures, UserUsageSourceTypeEnum sourceType, string sourceId, string description)
        {
            return true;
        }
    }
}
