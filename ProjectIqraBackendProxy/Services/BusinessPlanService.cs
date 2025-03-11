using IqraInfrastructure.Repositories.Telephony;
using IqraInfrastructure.Managers.Business;

namespace ProjectIqraBackendProxy.Services
{
    public class BusinessPlanService
    {
        private readonly ILogger<BusinessPlanService> _logger;
        private readonly BusinessManager _businessManager;
        private readonly CallQueueRepository _callQueueRepository;

        public BusinessPlanService(
            ILogger<BusinessPlanService> logger,
            BusinessManager businessManager,
            CallQueueRepository callQueueRepository)
        {
            _logger = logger;
            _businessManager = businessManager;
            _callQueueRepository = callQueueRepository;
        }

        public async Task<BusinessPlanValidationResult> ValidateCallLimitsAsync(long businessId, bool isOutbound = false)
        {
            var result = new BusinessPlanValidationResult();

            try
            {
                // Get business data
                var businessResult = await _businessManager.GetUserBusinessById(businessId, "ValidateCallLimitsAsync");
                if (!businessResult.Success || businessResult.Data == null)
                {
                    result.Message = $"Business not found: {businessId}";
                    _logger.LogWarning("Business not found: {BusinessId}", businessId);
                    return result;
                }

                var business = businessResult.Data;

                // Check if business is active
                if (business.Permission.DisabledFullAt != null)
                {
                    result.Message = "Business is disabled";
                    _logger.LogWarning("Business {BusinessId} is disabled", businessId);
                    return result;
                }

                // Check outbound permission if applicable
                if (isOutbound && business.Permission.MakeCall.DisabledCallingAt != null)
                {
                    result.Message = "Outbound calls are disabled for this business";
                    _logger.LogWarning("Outbound calls are disabled for business {BusinessId}", businessId);
                    return result;
                }

                // Determine concurrent call limit based on business plan
                int concurrentCallLimit = GetConcurrentCallLimit(business);

                // Get current active call count
                int activeCallCount = await _callQueueRepository.GetActiveCallCountForBusinessAsync(businessId);

                if (activeCallCount >= concurrentCallLimit)
                {
                    result.Message = $"Business has reached concurrent call limit: {concurrentCallLimit}";
                    _logger.LogWarning("Business {BusinessId} has reached concurrent call limit: {Limit}",
                        businessId, concurrentCallLimit);
                    return result;
                }

                // Validation successful
                result.Success = true;
                result.ConcurrentCallLimit = concurrentCallLimit;
                result.CurrentCallCount = activeCallCount;

                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Error validating business plan: {ex.Message}";
                _logger.LogError(ex, "Error validating business plan for {BusinessId}", businessId);
                return result;
            }
        }

        private int GetConcurrentCallLimit(IqraCore.Entities.Business.BusinessData business)
        {
            // In a real implementation, this would check the business plan tier
            // For now, we'll use a placeholder

            // Default limit
            int limit = 10;

            return limit;
        }
    }

    public class BusinessPlanValidationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ConcurrentCallLimit { get; set; }
        public int CurrentCallCount { get; set; }
    }
}