using IqraCore.Entities.Helpers;
using IqraCore.Models.User;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;

namespace IqraInfrastructure.Managers.User
{
    public class UserUsageManager
    {
        private readonly ILogger<UserUsageManager> _logger;
        private readonly ConversationUsageRepository _conversationUsageRepository;
        private readonly BusinessRepository _businessRepository;

        public UserUsageManager(
            ILogger<UserUsageManager> logger,
            ConversationUsageRepository usageRepository,
            BusinessRepository businessRepository)
        {
            _logger = logger;
            _conversationUsageRepository = usageRepository;
            _businessRepository = businessRepository;
        }

        public async Task<FunctionReturnResult<GetUsageSummaryModel?>> GetUsageSummaryAsync(string masterUserEmail, GetUsageSummaryRequestModel request)
        {
            var result = new FunctionReturnResult<GetUsageSummaryModel?>();

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

            var summary = new GetUsageSummaryModel();
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
                var usageDict = aggregatedData.ToDictionary(d => d.Id, d => d);

                switch (request.GroupBy)
                {
                    case UsageGroupBy.Hour:
                        // Loop 24 times for each hour of the selected day.
                        for (int i = 0; i < 24; i++)
                        {
                            var currentHour = startDate.AddHours(i);
                            var key = currentHour.Hour.ToString("D2"); // "00", "01", ... "23"

                            summary.DurationChart.Labels.Add(currentHour.ToString(labelFormat, CultureInfo.InvariantCulture));
                            summary.CallsChart.Labels.Add(currentHour.ToString(labelFormat, CultureInfo.InvariantCulture));

                            if (usageDict.TryGetValue(key, out var stats))
                            {
                                summary.DurationChart.Data.Add(stats.TotalMinutes);
                                summary.CallsChart.Data.Add(stats.TotalCalls);
                            }
                            else
                            {
                                summary.DurationChart.Data.Add(0);
                                summary.CallsChart.Data.Add(0);
                            }
                        }
                        break;

                    case UsageGroupBy.Month:
                        // Loop from the start month to the end month.
                        var currentMonth = new DateTime(startDate.Year, startDate.Month, 1);
                        while (currentMonth <= inclusiveEndDate)
                        {
                            var key = currentMonth.ToString("yyyy-MM");

                            summary.DurationChart.Labels.Add(currentMonth.ToString(labelFormat, CultureInfo.InvariantCulture));
                            summary.CallsChart.Labels.Add(currentMonth.ToString(labelFormat, CultureInfo.InvariantCulture));

                            if (usageDict.TryGetValue(key, out var stats))
                            {
                                summary.DurationChart.Data.Add(stats.TotalMinutes);
                                summary.CallsChart.Data.Add(stats.TotalCalls);
                            }
                            else
                            {
                                summary.DurationChart.Data.Add(0);
                                summary.CallsChart.Data.Add(0);
                            }

                            // Increment to the next month for the next iteration.
                            currentMonth = currentMonth.AddMonths(1);
                        }
                        break;

                    case UsageGroupBy.Day:
                    default:
                        // Loop from the start day to the end day.
                        for (var currentDay = startDate; currentDay <= inclusiveEndDate; currentDay = currentDay.AddDays(1))
                        {
                            var key = currentDay.ToString("yyyy-MM-dd");

                            summary.DurationChart.Labels.Add(currentDay.ToString(labelFormat, CultureInfo.InvariantCulture));
                            summary.CallsChart.Labels.Add(currentDay.ToString(labelFormat, CultureInfo.InvariantCulture));

                            if (usageDict.TryGetValue(key, out var stats))
                            {
                                summary.DurationChart.Data.Add(stats.TotalMinutes);
                                summary.CallsChart.Data.Add(stats.TotalCalls);
                            }
                            else
                            {
                                summary.DurationChart.Data.Add(0);
                                summary.CallsChart.Data.Add(0);
                            }
                        }
                        break;
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

        public async Task<FunctionReturnResult<PaginatedResult<MinuteUsageRecordModel>>> GetUsageHistoryAsync(
            string masterUserEmail,
            int limit,
            string? nextCursor,
            string? previousCursor)
        {
            var result = new FunctionReturnResult<PaginatedResult<MinuteUsageRecordModel>>();
            var paginatedResult = new PaginatedResult<MinuteUsageRecordModel> { PageSize = limit };

            bool fetchNext = string.IsNullOrWhiteSpace(previousCursor);
            string? currentCursor = fetchNext ? nextCursor : previousCursor;
            var decodedCursor = PaginationCursor.Decode(currentCursor);

            try
            {
                // 1. Fetch usage records
                var (usageRecords, hasMore) = await _conversationUsageRepository.GetUsageHistoryPaginatedAsync(masterUserEmail, limit, decodedCursor, fetchNext);

                if (usageRecords == null || !usageRecords.Any())
                {
                    return result.SetSuccessResult(new PaginatedResult<MinuteUsageRecordModel>());
                }

                // 2. Fetch business names for enrichment
                var businessIds = usageRecords.Select(r => r.BusinessId).Distinct().ToList();
                var businesses = await _businessRepository.GetBusinessesAsync(businessIds);
                var businessNameMap = businesses.ToDictionary(b => b.Id, b => b.Name);

                // 3. Map to the final model
                paginatedResult.Items = usageRecords.Select(r => new MinuteUsageRecordModel
                {
                    Id = r.Id,
                    Timestamp = r.CreatedAt,
                    BusinessId = r.BusinessId,
                    BusinessName = businessNameMap.GetValueOrDefault(r.BusinessId, "Unknown Business"),
                    MinutesUsed = r.TotalMinutesUsed,
                    TotalCost = r.TotalCost,
                    ConversationSessionId = r.ConversationSessionId
                }).ToList();

                // 4. Set cursors (This logic is identical to your reference code)
                if (fetchNext)
                {
                    paginatedResult.HasNextPage = hasMore;
                    paginatedResult.NextCursor = hasMore ? new PaginationCursor { Timestamp = usageRecords.Last().CreatedAt, Id = usageRecords.Last().Id }.Encode() : null;
                    paginatedResult.PreviousCursor = decodedCursor != null ? new PaginationCursor { Timestamp = usageRecords.First().CreatedAt, Id = usageRecords.First().Id }.Encode() : null;
                    paginatedResult.HasPreviousPage = decodedCursor != null;
                }
                else
                {
                    paginatedResult.HasPreviousPage = hasMore;
                    paginatedResult.PreviousCursor = hasMore ? new PaginationCursor { Timestamp = usageRecords.First().CreatedAt, Id = usageRecords.First().Id }.Encode() : null;
                    paginatedResult.NextCursor = new PaginationCursor { Timestamp = usageRecords.Last().CreatedAt, Id = usageRecords.Last().Id }.Encode();
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