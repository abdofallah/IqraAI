using IqraCore.Entities.Billing.Usage;
using IqraCore.Entities.Helpers;
using IqraCore.Models.Usage;
using IqraCore.Models.User.Usage;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Globalization;

namespace IqraInfrastructure.Managers.User
{
    public class UserUsageManager
    {
        private readonly ILogger<UserUsageManager> _logger;
        private readonly ConversationUsageRepository _conversationUsageRepository;

        public UserUsageManager(
            ILogger<UserUsageManager> logger,
            ConversationUsageRepository usageRepository
        )
        {
            _logger = logger;
            _conversationUsageRepository = usageRepository;
        }

        public async Task<FunctionReturnResult<GetUserUsageCountResponseModel?>> GetUsageCount(string masterUserEmail, GetUserUsageCountRequestModel request)
        {
            var result = new FunctionReturnResult<GetUserUsageCountResponseModel?>();

            try
            {
                var currentCount = await _conversationUsageRepository.GetConversationsCountAsync(masterUserEmail, request.StartDate, request.EndDate, request.BusinessIds);

                var response = new GetUserUsageCountResponseModel
                {
                    CurrentCount = currentCount,
                    PreviousCount = null
                };

                // If a comparison with the previous period is requested.
                if (request.ComparePrevious)
                {
                    // Calculate the timespan of the current period.
                    var timeSpan = request.EndDate - request.StartDate;

                    // Determine the start and end dates for the previous period.
                    var previousStartDate = request.StartDate - timeSpan;
                    var previousEndDate = request.StartDate;

                    // Fetch the count for the previous period.
                    var previousCount = await _conversationUsageRepository.GetConversationsCountAsync(masterUserEmail, previousStartDate, previousEndDate, request.BusinessIds);
                    response.PreviousCount = (int)previousCount;
                }

                return result.SetSuccessResult(response);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUsageCount:EXCEPTION",
                    $"An unexpected error occurred: {ex.Message}"
                );
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
                case UsageGroupBy.Hour:
                    if (totalDaysInRange > 1)
                    {
                        return result.SetFailureResult("INVALID_GROUPING", "Grouping by hour is only permitted for a single-day range.");
                    }
                    break;
                case UsageGroupBy.Month:
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
                // 2. Get Overall Summary Stats
                var overallStats = await _conversationUsageRepository.GetOverallUsageStatsAsync(masterUserEmail, startDate, endDate);
                if (overallStats != null)
                {
                    summary.TotalCalls = overallStats.TotalCalls;
                    summary.TotalDurationMinutes = overallStats.TotalMinutes;
                    summary.TotalCost = overallStats.TotalCost;
                    summary.AverageDurationSeconds = (overallStats.TotalCalls > 0) ? (overallStats.TotalMinutes * 60) / overallStats.TotalCalls : 0;
                    summary.AverageCallCost = (overallStats.TotalCalls > 0) ? overallStats.TotalCost / overallStats.TotalCalls : 0;
                }

                // 3. Get Aggregated Data for Charts
                string groupByFormat;
                string labelFormat;
                switch (request.GroupBy)
                {
                    case UsageGroupBy.Hour:
                        groupByFormat = "%H";
                        labelFormat = "h tt";
                        break;
                    case UsageGroupBy.Month:
                        groupByFormat = "%Y-%m";
                        labelFormat = "MMM yyyy";
                        break;
                    case UsageGroupBy.Day:
                    default:
                        groupByFormat = "%Y-%m-%d";
                        labelFormat = "MMM d";
                        break;
                }

                var aggregatedData = await _conversationUsageRepository.GetAggregatedUsageByPeriodAsync(masterUserEmail, startDate, endDate, groupByFormat);

                var uniqueBusinessIds = aggregatedData.Select(d => d.BusinessId).Distinct().ToList();
                summary.DurationChart.Datasets = uniqueBusinessIds.Select(id => new StackedBarDataset { BusinessId = id }).ToList();
                summary.CallsChart.Datasets = uniqueBusinessIds.Select(id => new StackedBarDataset { BusinessId = id }).ToList();
                summary.CostChart.Datasets = uniqueBusinessIds.Select(id => new StackedBarDataset { BusinessId = id }).ToList();

                var dataLookup = aggregatedData.ToDictionary(d => $"{d.Period}_{d.BusinessId}");

                var xLabels = new List<string>();

                switch (request.GroupBy)
                {
                    case UsageGroupBy.Hour:
                        for (int i = 0; i < 24; i++)
                        {
                            var currentHour = startDate.AddHours(i);
                            var periodKey = currentHour.Hour.ToString("D2");
                            xLabels.Add(currentHour.ToString(labelFormat, CultureInfo.InvariantCulture));

                            // For each business, find its data for this period
                            for (int j = 0; j < uniqueBusinessIds.Count; j++)
                            {
                                var businessId = uniqueBusinessIds[j];
                                var lookupKey = $"{periodKey}_{businessId}";
                                var dataExists = dataLookup.TryGetValue(lookupKey, out var stats);

                                summary.DurationChart.Datasets[j].Data.Add(dataExists ? stats.TotalMinutes : 0);
                                summary.CallsChart.Datasets[j].Data.Add(dataExists ? stats.TotalCalls : 0);
                                summary.CostChart.Datasets[j].Data.Add(dataExists ? stats.TotalCost : 0);
                            }
                        }
                        break;

                    case UsageGroupBy.Month:
                        for (var currentMonth = new DateTime(startDate.Year, startDate.Month, 1); currentMonth <= inclusiveEndDate; currentMonth = currentMonth.AddMonths(1))
                        {
                            var periodKey = currentMonth.ToString("yyyy-MM");
                            xLabels.Add(currentMonth.ToString(labelFormat, CultureInfo.InvariantCulture));
                            for (int j = 0; j < uniqueBusinessIds.Count; j++)
                            {
                                var businessId = uniqueBusinessIds[j];
                                var lookupKey = $"{periodKey}_{businessId}";
                                var dataExists = dataLookup.TryGetValue(lookupKey, out var stats);

                                summary.DurationChart.Datasets[j].Data.Add(dataExists ? stats.TotalMinutes : 0);
                                summary.CallsChart.Datasets[j].Data.Add(dataExists ? stats.TotalCalls : 0);
                                summary.CostChart.Datasets[j].Data.Add(dataExists ? stats.TotalCost : 0);
                            }
                        }
                        break;
                    case UsageGroupBy.Day:
                    default:
                        for (var currentDay = startDate; currentDay <= inclusiveEndDate; currentDay = currentDay.AddDays(1))
                        {
                            var periodKey = currentDay.ToString("yyyy-MM-dd");
                            xLabels.Add(currentDay.ToString(labelFormat, CultureInfo.InvariantCulture));
                            for (int j = 0; j < uniqueBusinessIds.Count; j++)
                            {
                                var businessId = uniqueBusinessIds[j];
                                var lookupKey = $"{periodKey}_{businessId}";
                                var dataExists = dataLookup.TryGetValue(lookupKey, out var stats);

                                summary.DurationChart.Datasets[j].Data.Add(dataExists ? stats.TotalMinutes : 0);
                                summary.CallsChart.Datasets[j].Data.Add(dataExists ? stats.TotalCalls : 0);
                                summary.CostChart.Datasets[j].Data.Add(dataExists ? stats.TotalCost : 0);
                            }
                        }
                        break;
                }

                summary.DurationChart.Labels = xLabels;
                summary.CallsChart.Labels = xLabels;
                summary.CostChart.Labels = xLabels;

                summary.ChartTitle = $"Usage from {startDate:MMM d, yyyy} to {inclusiveEndDate:MMM d, yyyy}";

                return result.SetSuccessResult(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get usage summary for user {Email}", masterUserEmail);
                return result.SetFailureResult("USAGE_SUMMARY_FAILED", "An error occurred while generating the usage summary.");
            }
        }

        public async Task<FunctionReturnResult<PaginatedResult<MinuteUsageRecordModel>>> GetUsageHistoryAsync(
            string masterUserEmail,
            int limit,
            string? nextCursor,
            string? previousCursor,
            List<long>? businessIds
        ) {
            var result = new FunctionReturnResult<PaginatedResult<MinuteUsageRecordModel>>();
            var paginatedResult = new PaginatedResult<MinuteUsageRecordModel> { PageSize = limit };

            bool fetchNext = string.IsNullOrWhiteSpace(previousCursor);
            string? currentCursor = fetchNext ? nextCursor : previousCursor;
            var decodedCursor = PaginationCursor<PaginationCursorNoFilterHelper>.Decode(currentCursor);

            try
            {
                // Fetch usage records
                var (usageRecords, hasMore) = await _conversationUsageRepository.GetUsageHistoryPaginatedAsync(masterUserEmail, limit, decodedCursor, fetchNext, businessIds);

                if (usageRecords == null || !usageRecords.Any())
                {
                    return result.SetSuccessResult(new PaginatedResult<MinuteUsageRecordModel>());
                }

                // Map to the final model
                paginatedResult.Items = usageRecords.Select((r) =>
                {
                    MinuteUsageRecordModel returnResult;

                    if (r is FixedPlanMinuteUsageRecord fixedPlanRecord)
                    {
                        returnResult = new FixedPlanMinuteUsageRecordModel()
                        {
                            TotalMinutesDeducted = fixedPlanRecord.TotalPlanMinutesDeducted,
                            TotalOverageMinutesCharged = fixedPlanRecord.TotalOverageMinutesCharged,
                            TotalOverageCost = fixedPlanRecord.TotalOverageCost
                        };
                    }
                    else
                    {
                        returnResult = new MinuteUsageRecordModel();
                    }

                    returnResult.Id = r.Id;
                    returnResult.Timestamp = r.CreatedAt;
                    returnResult.BusinessId = r.BusinessId;
                    returnResult.MinutesUsed = r.TotalMinutesUsed;
                    returnResult.ConversationSessionId = r.ConversationSessionId;
                    returnResult.PlanModel = r.PlanModel;
                    returnResult.TotalCost = r.TotalCost;

                    return returnResult;
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
    }
}