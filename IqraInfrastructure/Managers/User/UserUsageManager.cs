using IqraCore.Entities.Helpers;
using IqraCore.Models.Usage;
using IqraCore.Models.User.Usage;
using IqraInfrastructure.Repositories.User;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Globalization;

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

        public async Task<FunctionReturnResult<GetUserUsageSummaryModel?>> GetUsageSummaryAsync(string masterUserEmail, GetUserUsageSummaryRequestModel request)
        {
            var result = new FunctionReturnResult<GetUserUsageSummaryModel?>();

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

            var summary = new GetUserUsageSummaryModel();
            try
            {
                // 1. Get the new, detailed Overall Summary Stats
                var overallStats = await _usageRepository.GetOverallUserUsageStatsByTypeAsync(masterUserEmail, startDate, endDate);
                summary.OverallStats = overallStats;
                summary.GrandTotalCost = overallStats.TotalCost;

                // 2. Get Aggregated Data for Charts
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
                var aggregatedData = await _usageRepository.GetAggregatedUserUsageByPeriodAsync(masterUserEmail, startDate, endDate, groupByFormat);

                if (!aggregatedData.Any())
                {
                    summary.ChartTitle = $"No usage data from {startDate:MMM d, yyyy} to {inclusiveEndDate:MMM d, yyyy}";
                    return result.SetSuccessResult(summary);
                }

                // 3. Dynamically discover all features and businesses present in the data
                var uniqueBusinessIds = aggregatedData.Select(d => d.BusinessId).Distinct().ToList();
                var allFeatureKeys = aggregatedData.SelectMany(d => d.UsageByFeature.Keys).Distinct().ToList();

                // 4. Initialize a chart for each discovered feature
                foreach (var key in allFeatureKeys)
                {
                    summary.ChartsByFeature[key] = new StackedChartData
                    {
                        Datasets = uniqueBusinessIds.Select(id => new StackedBarDataset { BusinessId = id }).ToList()
                    };
                }

                var dataLookup = aggregatedData.ToDictionary(d => $"{d.Period}_{d.BusinessId}");
                var xLabels = new List<string>();

                IEnumerable<(string PeriodKey, string Label)> timePeriods = GetTimePeriods(startDate, inclusiveEndDate, request.GroupBy, labelFormat);
                foreach (var (periodKey, label) in timePeriods)
                {
                    xLabels.Add(label);
                    foreach (var featureKey in allFeatureKeys)
                    {
                        var chart = summary.ChartsByFeature[featureKey];
                        for (int j = 0; j < uniqueBusinessIds.Count; j++)
                        {
                            var businessId = uniqueBusinessIds[j];
                            var lookupKey = $"{periodKey}_{businessId}";
                            decimal value = dataLookup.TryGetValue(lookupKey, out var stats)
                                ? stats.UsageByFeature.GetValueOrDefault(featureKey, 0)
                                : 0;
                            chart.Datasets[j].Data.Add(value);
                        }
                    }
                }

                // 6. Assign labels to all generated charts
                foreach (var chart in summary.ChartsByFeature.Values)
                {
                    chart.Labels = xLabels;
                }

                summary.ChartTitle = $"Usage from {startDate:MMM d, yyyy} to {inclusiveEndDate:MMM d, yyyy}";
                return result.SetSuccessResult(summary);
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
                    SourceType = r.SourceType,
                    SourceId = r.SourceId,
                    TotalCost = r.ConsumedFeatures.Sum(cf => cf.TotalUsage),
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
    }
}