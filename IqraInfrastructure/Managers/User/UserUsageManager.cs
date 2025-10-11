using IqraCore.Entities.Billing;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Usage;
using IqraCore.Entities.User.Usage.Enums;
using IqraCore.Models.Usage;
using IqraCore.Models.User.Usage;
using IqraCore.Models.User.Usage.Summary;
using IqraInfrastructure.Repositories.User;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Globalization;
using static TorchSharp.torch.utils;

namespace IqraInfrastructure.Managers.User
{
    public class UserUsageManager
    {
        private readonly ILogger<UserUsageManager> _logger;
        private readonly UserUsageRepository _usageRepository;

        public UserUsageManager(
            ILogger<UserUsageManager> logger,
            UserUsageRepository usageRepository
        )
        {
            _logger = logger;
            _usageRepository = usageRepository;
        }

        public async Task<FunctionReturnResult<GetUserUsageCountResponseModel?>> GetUsageCount(string masterUserEmail, GetUserUsageCountRequestModel request)
        {
            var result = new FunctionReturnResult<GetUserUsageCountResponseModel?>();
            try
            {
                var currentCountsDict = await _usageRepository.GetUserUsageSourceTypeCountsAsync(masterUserEmail, request.StartDate, request.EndDate, request.BusinessIds);
                var response = new GetUserUsageCountResponseModel
                {
                    CurrentCounts = currentCountsDict.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value)
                };

                if (request.ComparePrevious)
                {
                    var timeSpan = request.EndDate - request.StartDate;
                    var previousStartDate = request.StartDate - timeSpan;
                    var previousEndDate = request.StartDate;
                    var previousCountsDict = await _usageRepository.GetUserUsageSourceTypeCountsAsync(masterUserEmail, previousStartDate, previousEndDate, request.BusinessIds);
                    response.PreviousCounts = previousCountsDict.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
                }

                return result.SetSuccessResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get usage counts for user {Email}", masterUserEmail);
                return result.SetFailureResult("GET_USAGE_COUNT_FAILED", $"An unexpected error occurred: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult<UserUsageSummaryResponseModel?>> GetUsageSummaryAsync(string masterUserEmail, UserUsageSummaryRequestModel request)
        {
            var result = new FunctionReturnResult<UserUsageSummaryResponseModel?>();

            var startDate = request.StartDate.ToUniversalTime().Date;
            var endDate = request.EndDate.ToUniversalTime().Date.AddDays(1);

            var inclusiveEndDate = request.EndDate.ToUniversalTime().Date;

            var minAllowedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var maxAllowedDate = DateTime.UtcNow.Date;

            if (startDate < minAllowedDate || inclusiveEndDate > maxAllowedDate || startDate >= endDate)
            {
                return result.SetFailureResult("INVALID_DATE_RANGE", "The selected date range is invalid.");
            }

            var totalDaysInRange = (inclusiveEndDate - startDate).TotalDays;
            switch (request.GroupBy)
            {
                case UserUsageGroupBy.Hour:
                    if (totalDaysInRange > 1)
                    {
                        return result.SetFailureResult("INVALID_GROUPING", "Grouping by hour is only permitted for a single-day range.");
                    }
                    break;
                case UserUsageGroupBy.Month:
                    if (startDate.Year == inclusiveEndDate.Year && startDate.Month == inclusiveEndDate.Month)
                    {
                        return result.SetFailureResult("INVALID_GROUPING", "Grouping by month requires a date range that spans across multiple months.");
                    }
                    break;
            }
            // END: VALIDATION

            var response = new UserUsageSummaryResponseModel();
            try
            {
                string groupByFormat;
                string labelFormat;
                switch (request.GroupBy)
                {
                    case UserUsageGroupBy.Hour:
                        groupByFormat = "%H";
                        labelFormat = "h tt";
                        break;
                    case UserUsageGroupBy.Month:
                        groupByFormat = "%Y-%m";
                        labelFormat = "MMM yyyy";
                        break;
                    case UserUsageGroupBy.Day:
                    default:
                        groupByFormat = "%Y-%m-%d";
                        labelFormat = "MMM d";
                        break;
                }

                // STEP 1: Execute all aggregation queries in parallel for maximum efficiency
                var mainStatsTask = _usageRepository.GetUserUsageMainStatsAsync(masterUserEmail, startDate, endDate);
                var sourceCountsTask = _usageRepository.GetUserUsageUniqueSourceCountsAsync(masterUserEmail, startDate, endDate);

                // We can create multiple chart data tasks for different metrics
                var featureTotalUsageChartDataTask = _usageRepository.GetUserUsageAggregatedChartDataAsync(masterUserEmail, startDate, endDate, groupByFormat, "$ConsumedFeatures.TotalUsage");
                var featureQuantityChartDataTask = _usageRepository.GetUserUsageAggregatedChartDataAsync(masterUserEmail, startDate, endDate, groupByFormat, "$ConsumedFeatures.Quantity");
                var callCountChartDataTask = _usageRepository.GetAggregatedSourceCountByPeriodAsync(masterUserEmail, startDate, endDate, groupByFormat, UserUsageSourceTypeEnum.Conversation);

                await Task.WhenAll(mainStatsTask, featureTotalUsageChartDataTask, featureQuantityChartDataTask, callCountChartDataTask);

                var mainStatsResults = await mainStatsTask;
                var featureTotalUsageChartDataResults = await featureTotalUsageChartDataTask;
                var featureQuantityChartDataResults = await featureQuantityChartDataTask;
                var callCountChartDataResults = await callCountChartDataTask;
                var sourceCountResults = await sourceCountsTask;

                // STEP 2: Process Main Stats to populate Overall, ByBusiness, and ByFeature dictionaries
                foreach (var stat in mainStatsResults)
                {
                    // Update Overall totals
                    if (stat.ConsumedType == UserUsageConsumedTypeEnum.PayAsYouGo || stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                    {
                        response.TotalCost += stat.TotalCost;

                        if (stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                        {
                            response.TotalOverageCost += stat.TotalCost;
                        }
                        else if (stat.ConsumedType == UserUsageConsumedTypeEnum.PayAsYouGo)
                        {
                            response.TotalPayAsYouGoCost += stat.TotalCost;
                        }
                    }

                    // Update ByBusiness
                    if (!response.ByBusiness.ContainsKey(stat.BusinessId)) response.ByBusiness[stat.BusinessId] = new UserUsageSummaryBusinessMetricsModel();
                    var businessMetrics = response.ByBusiness[stat.BusinessId];
                    if (stat.ConsumedType == UserUsageConsumedTypeEnum.PayAsYouGo || stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                    {
                        businessMetrics.TotalCost += stat.TotalCost;

                        if (stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                        {
                            businessMetrics.TotalOverageCost += stat.TotalCost;
                        }
                        else if (stat.ConsumedType == UserUsageConsumedTypeEnum.PayAsYouGo)
                        {
                            businessMetrics.TotalPayAsYouGoCost += stat.TotalCost;
                        }
                    }
                    // Business > By Feature
                    if (!businessMetrics.ConsumptionByFeature.ContainsKey(stat.FeatureKey)) businessMetrics.ConsumptionByFeature[stat.FeatureKey] = new UserUsageSummaryBusinessMetricsByFeatureModel();
                    var currentBusinessFeatureConsumption = businessMetrics.ConsumptionByFeature[stat.FeatureKey];
                    currentBusinessFeatureConsumption.TotalCount += stat.Count;
                    currentBusinessFeatureConsumption.TotalQuantity += stat.TotalQuantity;
                    if (stat.ConsumedType == UserUsageConsumedTypeEnum.PayAsYouGo || stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                    {
                        currentBusinessFeatureConsumption.TotalCost += stat.TotalCost;

                        if (stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                        {
                            currentBusinessFeatureConsumption.TotalOverageCost += stat.TotalCost;
                            currentBusinessFeatureConsumption.TotalOverageQuantity += stat.TotalQuantity;
                        }
                        else
                        {
                            currentBusinessFeatureConsumption.TotalPayAsYouGoCost += stat.TotalCost;
                            currentBusinessFeatureConsumption.TotalPayAsYouGoQuantity += stat.TotalQuantity;
                        }
                    }
                    else
                    {
                        currentBusinessFeatureConsumption.TotalIncludedUsage += stat.TotalCost;
                    }
                    // Business > By Source
                    if (!businessMetrics.ConsumptionBySource.ContainsKey(stat.SourceType)) businessMetrics.ConsumptionBySource[stat.SourceType] = new UserUsageSummaryBusinessMetricsBySourceModel();
                    var currentBusinessSourceConsumption = businessMetrics.ConsumptionBySource[stat.SourceType];
                    if (stat.ConsumedType == UserUsageConsumedTypeEnum.PayAsYouGo || stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                    {
                        currentBusinessSourceConsumption.TotalCost += stat.TotalCost;

                        if (stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                        {
                            currentBusinessSourceConsumption.TotalOverageCost += stat.TotalCost;
                        }
                        else
                        {
                            currentBusinessSourceConsumption.TotalPayAsYouGoCost += stat.TotalCost;
                        }
                    }

                    // Update BySource
                    if (!response.BySource.ContainsKey(stat.SourceType)) response.BySource[stat.SourceType] = new UserUsageSummarySourceMetricsModel();
                    var sourceMetrics = response.BySource[stat.SourceType];
                    if (stat.ConsumedType == UserUsageConsumedTypeEnum.PayAsYouGo || stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                    {
                        sourceMetrics.TotalCost += stat.TotalCost;

                        if (stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                        {
                            sourceMetrics.TotalOverageCost += stat.TotalCost;
                        }
                        else if (stat.ConsumedType == UserUsageConsumedTypeEnum.PayAsYouGo)
                        {
                            sourceMetrics.TotalPayAsYouGoCost += stat.TotalCost;
                        }
                    }
                    // BySource > By Feature
                    if (!sourceMetrics.ConsumptionByFeature.ContainsKey(stat.FeatureKey)) sourceMetrics.ConsumptionByFeature[stat.FeatureKey] = new UserUsageSummarySourceMetricsByFeaturesModel();
                    var currentSourceFeatureConsumption = sourceMetrics.ConsumptionByFeature[stat.FeatureKey];
                    currentSourceFeatureConsumption.TotalCount += stat.Count;
                    currentSourceFeatureConsumption.TotalQuantity += stat.TotalQuantity;
                    if (stat.ConsumedType == UserUsageConsumedTypeEnum.PayAsYouGo || stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                    {
                        currentSourceFeatureConsumption.TotalCost += stat.TotalCost;

                        if (stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                        {
                            currentSourceFeatureConsumption.TotalOverageCost += stat.TotalCost;
                            currentSourceFeatureConsumption.TotalOverageQuantity += stat.TotalQuantity;
                        }
                        else
                        {
                            currentSourceFeatureConsumption.TotalPayAsYouGoCost += stat.TotalCost;
                            currentSourceFeatureConsumption.TotalPayAsYouGoQuantity += stat.TotalQuantity;
                        }
                    }
                    else
                    {
                        currentSourceFeatureConsumption.TotalIncludedUsage += stat.TotalCost;
                    }

                    // Update ByFeature
                    if (!response.ByFeature.ContainsKey(stat.FeatureKey)) response.ByFeature[stat.FeatureKey] = new UserUsageSummaryFeatureMetricsModel();
                    var featureMetrics = response.ByFeature[stat.FeatureKey];
                    featureMetrics.TotalCount += stat.Count;
                    featureMetrics.TotalQuantity += stat.TotalQuantity;
                    if (stat.ConsumedType == UserUsageConsumedTypeEnum.PayAsYouGo || stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                    {
                        featureMetrics.TotalCost += stat.TotalCost;

                        if (stat.ConsumedType == UserUsageConsumedTypeEnum.Overage)
                        {
                            featureMetrics.TotalOverageCost += stat.TotalCost;
                            featureMetrics.TotalOverageQuantity += stat.TotalQuantity;
                        }
                        else if (stat.ConsumedType == UserUsageConsumedTypeEnum.PayAsYouGo)
                        {
                            featureMetrics.TotalPayAsYouGoCost += stat.TotalCost;
                            featureMetrics.TotalPayAsYouGoQuantity += stat.TotalQuantity;
                        }
                    }
                    else if (stat.ConsumedType == UserUsageConsumedTypeEnum.Included)
                    {
                        featureMetrics.TotalIncludedUsage += stat.TotalCost;
                    } 
                }

                foreach (var sourceCount in sourceCountResults)
                {
                    if (response.ByBusiness.ContainsKey(sourceCount.BusinessId))
                    {
                        response.ByBusiness[sourceCount.BusinessId].TotalCount += sourceCount.Count;

                        if (response.ByBusiness[sourceCount.BusinessId].ConsumptionBySource.ContainsKey(sourceCount.SourceType))
                        {
                            response.ByBusiness[sourceCount.BusinessId].ConsumptionBySource[sourceCount.SourceType].TotalCount += sourceCount.Count;
                        }
                    }

                    if (response.BySource.ContainsKey(sourceCount.SourceType))
                    {
                        response.BySource[sourceCount.SourceType].TotalCount += sourceCount.Count;
                    }
                }

                // STEP 4: Process and build the charts
                var chargeableData = featureTotalUsageChartDataResults.Where(r => r.ConsumedType != UserUsageConsumedTypeEnum.Included).ToList();
                response.Charts["overallCostChart"] = BuildChart(chargeableData, startDate, inclusiveEndDate, request.GroupBy, labelFormat);

                // For calls duration chart, we need to filter the results to only include the "Call Minutes" feature
                var callDurationData = featureQuantityChartDataResults.Where(d => d.FeatureKey == BillingFeatureKey.CallMinutes).ToList();
                response.Charts["durationChart"] = BuildChart(callDurationData, startDate, inclusiveEndDate, request.GroupBy, labelFormat, isIntValue: true);

                var callCountForChartHelper = callCountChartDataResults.Select(r => new UserUsageAggregatedChartDataResult
                {
                    Period = r.Period,
                    BusinessId = r.BusinessId,
                    Value = r.Count // Convert int Count to decimal Value
                }).ToList();
                response.Charts["callCountChart"] = BuildChart(callCountForChartHelper, startDate, inclusiveEndDate, request.GroupBy, labelFormat, isIntValue: true);

                return result.SetSuccessResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get usage summary for user {Email}", masterUserEmail);
                return result.SetFailureResult("USAGE_SUMMARY_FAILED", "An error occurred while generating the usage summary.");
            }
        }

        public async Task<FunctionReturnResult<PaginatedResult<UserUsageRecordModel>>> GetUsageHistoryAsync(
            string masterUserEmail,
            int limit,
            string? nextCursor,
            string? previousCursor,
            List<long>? businessIds
        ) {
            var result = new FunctionReturnResult<PaginatedResult<UserUsageRecordModel>>();
            var paginatedResult = new PaginatedResult<UserUsageRecordModel> { PageSize = limit };

            bool fetchNext = string.IsNullOrWhiteSpace(previousCursor);
            string? currentCursor = fetchNext ? nextCursor : previousCursor;
            var decodedCursor = PaginationCursor<PaginationCursorNoFilterHelper>.Decode(currentCursor);

            try
            {
                // Fetch usage records
                var (usageRecords, hasMore) = await _usageRepository.GetUserUsageHistoryPaginatedAsync(masterUserEmail, limit, decodedCursor, fetchNext, businessIds);
                if (usageRecords == null || !usageRecords.Any())
                {
                    return result.SetSuccessResult(new PaginatedResult<UserUsageRecordModel>());
                }

                // Map to the final model
                paginatedResult.Items = usageRecords.Select(r => new UserUsageRecordModel
                {
                    Id = r.Id,
                    Timestamp = r.CreatedAt,
                    BusinessId = r.BusinessId,
                    PlanId = r.PlanId,
                    Description = r.Description,
                    SourceType = r.SourceType.ToString(),
                    SourceId = r.SourceId,
                    ConsumedFeatures = r.ConsumedFeatures.Select(cf => new ConsumedFeatureModel
                    {
                        FeatureKey = cf.FeatureKey,
                        Type = cf.Type.ToString(),
                        Quantity = cf.Quantity,
                        AppliedUnitUsage = cf.AppliedUnitUsage,
                        TotalUsage = cf.TotalUsage
                    }).ToList()
                }).ToList();

                // Set cursors
                if (fetchNext)
                {
                    paginatedResult.HasNextPage = hasMore;
                    paginatedResult.NextCursor = hasMore ? new PaginationCursor<PaginationCursorNoFilterHelper> { Timestamp = usageRecords.Last().CreatedAt, Id = usageRecords.Last().Id }.Encode() : null;
                    paginatedResult.PreviousCursor = decodedCursor != null ? new PaginationCursor<PaginationCursorNoFilterHelper> { Timestamp = usageRecords.First().CreatedAt, Id = usageRecords.First().Id }.Encode() : null;
                    paginatedResult.HasPreviousPage = decodedCursor != null;
                }
                else
                {
                    paginatedResult.HasPreviousPage = hasMore;
                    paginatedResult.PreviousCursor = hasMore ? new PaginationCursor<PaginationCursorNoFilterHelper> { Timestamp = usageRecords.First().CreatedAt, Id = usageRecords.First().Id }.Encode() : null;
                    paginatedResult.NextCursor = new PaginationCursor<PaginationCursorNoFilterHelper> { Timestamp = usageRecords.Last().CreatedAt, Id = usageRecords.Last().Id }.Encode();
                    paginatedResult.HasNextPage = true;
                }

                return result.SetSuccessResult(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get usage history for user {Email}", masterUserEmail);
                return result.SetFailureResult("USAGE_HISTORY_FAILED", "An error occurred while fetching usage history.");
            }
        }

        // Helpers
        private IEnumerable<(string PeriodKey, string Label)> GetTimePeriods(DateTime start, DateTime end, UserUsageGroupBy groupBy, string labelFormat)
        {
            switch (groupBy)
            {
                case UserUsageGroupBy.Hour:
                    for (int i = 0; i < 24; i++)
                    {
                        var currentHour = start.AddHours(i);
                        yield return (currentHour.Hour.ToString("D2"), currentHour.ToString(labelFormat, CultureInfo.InvariantCulture));
                    }
                    break;
                case UserUsageGroupBy.Month:
                    for (var currentMonth = new DateTime(start.Year, start.Month, 1); currentMonth <= end; currentMonth = currentMonth.AddMonths(1))
                    {
                        yield return (currentMonth.ToString("yyyy-MM"), currentMonth.ToString(labelFormat, CultureInfo.InvariantCulture));
                    }
                    break;
                default: // Day
                    for (var currentDay = start; currentDay <= end; currentDay = currentDay.AddDays(1))
                    {
                        yield return (currentDay.ToString("yyyy-MM-dd"), currentDay.ToString(labelFormat, CultureInfo.InvariantCulture));
                    }
                    break;
            }
        }

        private UserUsageSummaryStackedChartDataModel BuildChart(List<UserUsageAggregatedChartDataResult> data, DateTime startDate, DateTime inclusiveEndDate, UserUsageGroupBy groupBy, string labelFormat, bool isIntValue = false)
        {
            var chart = new UserUsageSummaryStackedChartDataModel();
            if (!data.Any()) return chart;

            var uniqueBusinessIds = data.Select(d => d.BusinessId).Distinct().ToList();
            chart.Datasets = uniqueBusinessIds.Select(id => new UserUsageSummaryStackedBarDatasetModel { BusinessId = id }).ToList();

            var dataLookup = data.ToLookup(d => $"{d.Period}_{d.BusinessId}");

            IEnumerable<(string PeriodKey, string Label)> timePeriods = GetTimePeriods(startDate, inclusiveEndDate, groupBy, labelFormat);
            foreach (var (periodKey, label) in timePeriods)
            {
                chart.Labels.Add(label);
                for (int i = 0; i < uniqueBusinessIds.Count; i++)
                {
                    var businessId = uniqueBusinessIds[i];
                    var lookupKey = $"{periodKey}_{businessId}";
                    // Sum up values for all features for this business in this period
                    var value = dataLookup[lookupKey].Sum(d => d.Value);
                    chart.Datasets[i].Data.Add(value);
                }
            }
            return chart;
        }
    }
}