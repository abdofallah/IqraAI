using IqraCore.Entities.Conversation;
using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.Conversations;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessConversationsManager
    {
        private readonly BusinessManager _parentBusinessManager;

        private readonly CallQueueRepository _callQueueRepository;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly ConversationAudioRepository _conversationAudioRepository;

        public BusinessConversationsManager(
            BusinessManager businessManager,
            CallQueueRepository callQueueRepository,
            ConversationStateRepository conversationStateRepository,
            ConversationAudioRepository conversationAudioRepository
        )
        {
            _parentBusinessManager = businessManager;

            _callQueueRepository = callQueueRepository;
            _conversationStateRepository = conversationStateRepository;
            _conversationAudioRepository = conversationAudioRepository;
        }

        public async Task<FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>> GetInboundConversationsMetaDataListAsync(
            long businessId,
            int limit = 20, // Default limit
            string? nextCursor = null,
            string? previousCursor = null)
        {
            var result = new FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>();
            var paginatedResult = new PaginatedResult<InboundConversationMetadataModel>();

            // Validate limit
            limit = Math.Clamp(limit, 1, 100); // Enforce min 1, max 100
            paginatedResult.PageSize = limit;

            bool fetchNext = string.IsNullOrWhiteSpace(previousCursor); // Prefer fetching next unless previous is specified
            string? currentCursor = fetchNext ? nextCursor : previousCursor;
            PaginationCursor? decodedCursor = PaginationCursor.Decode(currentCursor);

            // 1. Fetch Call Queue Data Page
            var (callQueueItems, hasMore) = await _callQueueRepository.GetCallQueuesForBusinessPaginatedAsync(
                businessId,
                limit,
                decodedCursor,
                fetchNext
            );

            if (callQueueItems == null || !callQueueItems.Any())
            {
                paginatedResult.HasNextPage = false;
                paginatedResult.HasPreviousPage = decodedCursor != null && !fetchNext; // Rough indicator
                result.SetSuccessResult(paginatedResult);
                return result; // Return empty success result
            }

            // 2. Enrich with Conversation State
            var queueIdsWithSession = callQueueItems
                .Where(cq => !string.IsNullOrEmpty(cq.SessionId))
                .Select(cq => cq.Id)
                .Distinct()
                .ToList();

            Dictionary<string, ConversationState> conversationStates = new Dictionary<string, ConversationState>();
            if (queueIdsWithSession.Any())
            {
                // **** Pass the correct IDs here based on the link ****
                conversationStates = await _conversationStateRepository.GetByQueueIdsAsync(queueIdsWithSession);
            }

            // 3. Map to Metadata Model
            paginatedResult.Items = callQueueItems.Select(cq =>
            {
                var metadata = new InboundConversationMetadataModel
                {
                    QueueId = cq.Id, // Assuming CallQueueData.Id is the QueueId
                    Status = cq.Status,
                    EnqueuedAt = cq.EnqueuedAt,
                    ProcessingStartedAt = cq.ProcessingStartedAt,
                    CompletedAt = cq.CompletedAt,
                    NumberId = "",//cq.NumberId,
                    RouteId = "",//cq.RouteId,
                    CallerNumber = "",//cq.CallerNumber,
                    SessionId = null, // Default to null
                    SessionStatus = null // Default to null
                };

                if (!string.IsNullOrEmpty(cq.Id) && conversationStates.TryGetValue(cq.Id, out var state))
                {
                    metadata.SessionId = state.Id;
                    metadata.SessionStatus = state.Status;
                }


                return metadata;
            }).ToList();


            // 4. Set Cursors and Flags
            if (fetchNext)
            {
                paginatedResult.HasNextPage = hasMore;
                paginatedResult.NextCursor = hasMore ? new PaginationCursor { Timestamp = callQueueItems.Last().EnqueuedAt, Id = callQueueItems.Last().Id }.Encode() : null;
                // If we fetched using 'next', the previous cursor refers to the *first* item on the current page
                paginatedResult.PreviousCursor = (decodedCursor != null || callQueueItems.Count == limit) // Only show previous if not on absolute first page view
                                                  ? new PaginationCursor { Timestamp = callQueueItems.First().EnqueuedAt, Id = callQueueItems.First().Id }.Encode()
                                                  : null;
                paginatedResult.HasPreviousPage = decodedCursor != null; // True if we used a cursor to get here
            }
            else // We fetched 'previous'
            {
                paginatedResult.HasPreviousPage = hasMore;
                paginatedResult.PreviousCursor = hasMore ? new PaginationCursor { Timestamp = callQueueItems.First().EnqueuedAt, Id = callQueueItems.First().Id }.Encode() : null;
                // If we fetched using 'previous', the next cursor refers to the *last* item on the current page
                paginatedResult.NextCursor = new PaginationCursor { Timestamp = callQueueItems.Last().EnqueuedAt, Id = callQueueItems.Last().Id }.Encode();
                paginatedResult.HasNextPage = true; // If we successfully fetched a previous page, there's always a next page (the one we came from)
            }


            result.SetSuccessResult(paginatedResult);
            return result;
        }

        public async Task<FunctionReturnResult<ConversationState?>> GetConversationStateByIdAsync(long businessId, string sessionId)
        {
            var result = new FunctionReturnResult<ConversationState?>();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                result.Code = "GetConvState:1";
                result.Message = "Session ID cannot be empty.";
                return result;
            }

            try
            {
                var state = await _conversationStateRepository.GetByIdAsync(sessionId);
                if (state == null)
                {
                    result.SetFailureResult("GetConversationStateByIdAsync:1", $"Conversation state with Session ID '{sessionId}' not found.");
                    return result;
                }

                if (state.BusinessId != businessId)
                {
                    result.SetFailureResult("GetConversationStateByIdAsync:2", $"Access denied. Conversation does not belong to business ID {businessId}.");
                    return result;
                }

                result.SetSuccessResult(state);
                return result;
            }
            catch (Exception ex)
            {
                result.SetFailureResult("GetConversationStateByIdAsync:-1", "An error occurred while fetching conversation state");
                return result;
                // Consider logging the exception details here via _logger if available/injected
            }
        }
    }
}
