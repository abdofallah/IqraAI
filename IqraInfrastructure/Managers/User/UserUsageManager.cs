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

        public async Task<FunctionReturnResult<GetUsageSummaryModel>> GetUsageSummaryAsync(string masterUserEmail, UsageTimeRange timeRange)
        {
            var result = new FunctionReturnResult<GetUsageSummaryModel>();
            var summary = new GetUsageSummaryModel();

            DateTime startDate;
            string groupByFormat;
            string chartTitlePrefix;

            switch (timeRange)
            {
                case UsageTimeRange.Last7Days:
                    startDate = DateTime.UtcNow.Date.AddDays(-6);
                    groupByFormat = "%Y-%m-%d"; // Group by Year-Month-Day
                    chartTitlePrefix = "Usage in the Last 7 Days";
                    break;
                case UsageTimeRange.Today:
                    startDate = DateTime.UtcNow.Date;
                    groupByFormat = "%H"; // Group by Hour
                    chartTitlePrefix = "Usage Today";
                    break;
                case UsageTimeRange.CurrentMonth:
                default:
                    startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                    groupByFormat = "%Y-%m-%d"; // Group by Year-Month-Day
                    chartTitlePrefix = $"Usage for {DateTime.UtcNow:MMMM yyyy}";
                    break;
            }

            try
            {
                var aggregatedData = await _conversationUsageRepository.GetAggregatedUsageAsync(masterUserEmail, startDate, groupByFormat);

                // Now, we need to fill in the gaps for days/hours with no usage.
                var usageDict = aggregatedData.ToDictionary(d => d.Id, d => d.TotalMinutes);

                if (timeRange == UsageTimeRange.Today)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        var hourKey = i.ToString("D2"); // "00", "01", ..., "23"

                        int displayHour = i % 12;
                        if (displayHour == 0) displayHour = 12;
                        string ampm = i < 12 ? "AM" : "PM";
                        string finalLabel = $"{displayHour} {ampm}";

                        summary.Labels.Add(finalLabel);
                        summary.Data.Add(usageDict.TryGetValue(hourKey, out var value) ? value : 0);
                    }
                }
                else // Month or Week
                {
                    DateTime endDate = DateTime.UtcNow.Date;
                    for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        var dateKey = date.ToString("yyyy-MM-dd");
                        summary.Labels.Add(date.ToString("MMM d")); // "Oct 26"
                        summary.Data.Add(usageDict.TryGetValue(dateKey, out var value) ? value : 0);
                    }
                }

                summary.ChartTitle = chartTitlePrefix;
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