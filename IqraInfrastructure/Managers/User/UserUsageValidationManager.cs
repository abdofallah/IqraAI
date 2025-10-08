using IqraCore.Entities.Billing;
using IqraCore.Entities.Billing.Plan;
using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Entities.User.Billing;
using IqraCore.Entities.User.Billing.Enums;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.User;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.User
{
    public class UserUsageValidationManager
    {
        private readonly ILogger<UserUsageValidationManager> _logger;
        private readonly AppRepository _appRepository;
        private readonly BusinessRepository _businessRepository;
        private readonly UserRepository _userRepository;
        private readonly PlanManager _planManager;
        private readonly ConversationStateRepository _conversationStateRepository;

        public UserUsageValidationManager(
            ILogger<UserUsageValidationManager> logger,
            AppRepository appRepository,
            BusinessRepository businessRepository,
            UserRepository userRepository,
            PlanManager planManager,
            ConversationStateRepository conversationStateRepository)
        {
            _logger = logger;
            _appRepository = appRepository;
            _businessRepository = businessRepository;
            _userRepository = userRepository;
            _planManager = planManager;
            _conversationStateRepository = conversationStateRepository;
        }

        public async Task<FunctionReturnResult> ValidateCallPermissionAsync(long businessId)
        {
            var result = new FunctionReturnResult();
            string logPrefix = "ValidateCallPermission";

            // --- Load all necessary data using the new helper method ---
            var (loadResult, businessData, masterUserData, userBillingPlan) = await LoadUserAndPlanAsync(businessId, logPrefix);
            if (!loadResult.Success)
            {
                return loadResult;
            }

            //  --- Validate Credit Balance / Package Limits (now dynamic) ---
            var minutesFeature = userBillingPlan!.GetFeature(BillingFeatureKey.CallMinutes);
            if (minutesFeature == null)
            {
                return result.SetFailureResult($"{logPrefix}:FEATURE_NOT_DEFINED", $"The plan '{userBillingPlan.Name}' does not have the '{BillingFeatureKey.CallMinutes}' feature defined.");
            }

            if (userBillingPlan is FixedPackagePlanDefinition)
            {
                if (masterUserData!.Billing.CurrentCycleUsage.CurrentFeatureUsage.ContainsKey(BillingFeatureKey.CallMinutes) == false)
                {
                    return result.SetFailureResult($"{logPrefix}:NO_CURRENT_MINUTES_USAGE", $"No current usage data found for call minutes.");
                }

                decimal minutesUsed = masterUserData!.Billing.CurrentCycleUsage.CurrentFeatureUsage.GetValueOrDefault(BillingFeatureKey.CallMinutes);

                // If the user is out of included minutes, they must have a positive credit balance for overages.
                if (minutesUsed >= minutesFeature.IncludedLimit && masterUserData.Billing.CreditBalance <= 0)
                {
                    return result.SetFailureResult($"{logPrefix}:EXCEEDED_PACKAGE_AND_CREDIT", "Exceeded plan minutes and insufficient credit balance for overage.");
                }
            }
            else // StandardPayAsYouGo or VolumeBasedTiered
            {
                // User must have a positive credit balance to start a new call.
                if (masterUserData.Billing.CreditBalance <= 0)
                {
                    return result.SetFailureResult($"{logPrefix}:INSUFFICIENT_BALANCE", "Insufficient credit balance to make a call.");
                }
            }

            // --- Validate Business-Level Minute Cap (unchanged) ---
            if (businessData.AllocatedMonthlyMinuteCap.HasValue)
            {
                if (businessData.CurrentMonthlyMinuteUsage >= businessData.AllocatedMonthlyMinuteCap.Value)
                {
                    return result.SetFailureResult($"{logPrefix}:BUSINESS_MONTHLY_MINUTE_CAP", "Business allocated monthly minutes limit reached.");
                }
            }

            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> TryIncreaseUsageConcurrency(long businessId, string featureKey, object parentReference, object? childReference)
        {
            var result = new FunctionReturnResult();
            string logPrefix = "TryIncreaseUsageCocurrency";

            try
            {
                var (loadResult, businessData, masterUserData, userBillingPlan) = await LoadUserAndPlanAsync(businessId, logPrefix);
                if (!loadResult.Success)
                {
                    return loadResult;
                }

                var concurrencyFeature = userBillingPlan.GetFeature(featureKey);
                if (concurrencyFeature == null)
                {
                    return result.SetFailureResult(
                        $"{logPrefix}:FEATURE_NOT_DEFINED",
                        $"The plan '{userBillingPlan.Name}' does not have the '{featureKey}' feature defined."
                    );
                }

                // Calculate total user concurrency from the plan and any active add-ons
                decimal baseConcurrency = concurrencyFeature.IncludedLimit;
                decimal purchasedConcurrency = masterUserData.Billing.ActiveFeatureAddons
                    .Where(a => a.FeatureKey == featureKey && a.Status == UserBillingAddonStatusEnum.Active)
                    .Sum(a => a.Quantity);
                long totalUserConcurrency = (long)(baseConcurrency + purchasedConcurrency);

                if (totalUserConcurrency <= 0)
                {
                    return result.SetFailureResult(
                       $"{logPrefix}:CONCURRENCY_LIMIT_ZERO",
                       $"User has no concurrency allowance for '{featureKey}' feature."
                   );
                }

                var usageItem = new UserBillingCycleConcurrencyFeatureUsage
                {
                    BusinessId = businessId,
                    ParentReference = parentReference,
                    ChildReference = childReference
                };

                bool increased = await _userRepository.TryIncrementConcurrencyUsageAsync(masterUserData.Email, featureKey, totalUserConcurrency, usageItem);
                if (!increased)
                {
                    return result.SetFailureResult(
                        $"{logPrefix}:CONCURRENCY_LIMIT_REACHED",
                        $"Could not increase concurrency for '{featureKey}'. The limit of {totalUserConcurrency} has been reached."
                    );
                }
                return result.SetSuccessResult();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Exception occurred while increasing {FeatureKey} concurrency for {BusinessId}", featureKey, businessId);
                return result.SetFailureResult(
                    "INCREASE_CONCURRENCY:EXCEPTION",
                    $"An unexpected error occurred: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult> DecreaseUsageConcurrency(long businessId, string featureKey, object parentReference, object? childReference)
        {
            var result = new FunctionReturnResult();
            string logPrefix = "DecreaseUsageCocurrency";

            try
            {
                var businessData = await _businessRepository.GetBusinessAsync(businessId);
                if (businessData == null)
                {
                    return result.SetFailureResult(
                        $"{logPrefix}:BUSINESS_NOT_FOUND",
                        "Business not found."
                    );
                }

                return await DecreaseUsageConcurrency(businessData.MasterUserEmail, businessId, featureKey, parentReference, childReference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while decreasing {FeatureKey} concurrency for business {BusinessId}", featureKey, businessId);
                return result.SetFailureResult(
                    $"{logPrefix}:EXCEPTION",
                    $"An unexpected error occurred: {ex.Message}"
                );
            }

        }

        public async Task<FunctionReturnResult> DecreaseUsageConcurrency(string userEmail, long businessId, string featureKey, object parentReference, object? childReference)
        {
            var result = new FunctionReturnResult();
            string logPrefix = "DecreaseUsageCocurrency";

            try
            {
                bool decreased = await _userRepository.DecrementConcurrencyUsageAsync(userEmail, featureKey, businessId, parentReference, childReference);

                if (!decreased)
                {
                    _logger.LogWarning("{Prefix}: Failed to decrease {FeatureKey} concurrency for business {BusinessId}. The specific usage reference was not found.", logPrefix, featureKey, businessId);
                    return result.SetFailureResult(
                        $"{logPrefix}:CONCURRENCY_ITEM_NOT_FOUND",
                        $"The specified {featureKey} concurrency session was not found. It may have already been closed."
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while decreasing {FeatureKey} concurrency for business {BusinessId}", featureKey, businessId);
                return result.SetFailureResult(
                    $"{logPrefix}:EXCEPTION",
                    $"An unexpected error occurred: {ex.Message}"
                );
            }
        }

        private async Task<(FunctionReturnResult result, BusinessData? business, UserData? user, BillingPlanDefinitionBase? plan)> LoadUserAndPlanAsync(long businessId, string logPrefix)
        {
            var result = new FunctionReturnResult();

            var businessData = await _businessRepository.GetBusinessAsync(businessId);
            if (businessData == null)
            {
                return (result.SetFailureResult($"{logPrefix}:BUSINESS_NOT_FOUND", "Business not found."), null, null, null);
            }

            UserData? masterUserData = await _userRepository.GetUserByEmail(businessData.MasterUserEmail);
            if (masterUserData == null)
            {
                return (result.SetFailureResult($"{logPrefix}:USER_NOT_FOUND", "Master user not found."), businessData, null, null);
            }

            var planResult = await _planManager.GetPlanByIdAsync(masterUserData!.Billing.Subscription.PlanId);
            if (!planResult.Success || planResult.Data == null)
            {
                return (result.SetFailureResult($"{logPrefix}:PLAN_NOT_FOUND", $"Plan with ID '{masterUserData!.Billing.Subscription.PlanId}' could not be found."), businessData, masterUserData, null);
            }

            return (result.SetSuccessResult(), businessData, masterUserData, planResult.Data);
        }
    }
}
