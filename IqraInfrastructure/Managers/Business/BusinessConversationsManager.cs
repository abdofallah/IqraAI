using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.Conversations;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessConversationsManager
    {
        private readonly BusinessManager _parentBusinessManager;

        private readonly InboundCallQueueRepository _inboundCallQueueRepository;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepository;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly ConversationAudioRepository _conversationAudioRepository;

        public BusinessConversationsManager(
            BusinessManager businessManager,
            InboundCallQueueRepository callQueueRepository,
            OutboundCallQueueRepository outboundCallQueueRepository,
            ConversationStateRepository conversationStateRepository,
            ConversationAudioRepository conversationAudioRepository
        )
        {
            _parentBusinessManager = businessManager;

            _inboundCallQueueRepository = callQueueRepository;
            _outboundCallQueueRepository = outboundCallQueueRepository;
            _conversationStateRepository = conversationStateRepository;
            _conversationAudioRepository = conversationAudioRepository;
        }

        public async Task<FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>> GetInboundConversationsMetaDataListAsync(
            long businessId,
            int limit = 20,
            string? nextCursor = null,
            string? previousCursor = null
        )
        {
            var result = new FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>();
            var paginatedResult = new PaginatedResult<InboundConversationMetadataModel>();

            // Validate limit
            limit = Math.Clamp(limit, 1, 100);
            paginatedResult.PageSize = limit;

            bool fetchNext = string.IsNullOrWhiteSpace(previousCursor);
            string? currentCursor = fetchNext ? nextCursor : previousCursor;
            PaginationCursor? decodedCursor = PaginationCursor.Decode(currentCursor);

            // 1. Fetch Call Queue Data Page 
            var (callQueueItems, hasMore) = await _inboundCallQueueRepository.GetInboundCallQueuesForBusinessPaginatedAsync(
                businessId,
                limit,
                decodedCursor,
                fetchNext
            );

            if (callQueueItems == null || !callQueueItems.Any())
            {
                paginatedResult.HasNextPage = false;
                paginatedResult.HasPreviousPage = decodedCursor != null && !fetchNext;
                result.SetSuccessResult(paginatedResult);
                return result;
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
                conversationStates = await _conversationStateRepository.GetByQueueIdsAsync(queueIdsWithSession);
            }

            // 3. Map to Metadata Model
            paginatedResult.Items = callQueueItems.Select(cq =>
            {
                var metadata = new InboundConversationMetadataModel
                {
                    QueueId = cq.Id,
                    Status = cq.Status,
                    Logs = cq.Logs,
                    EnqueuedAt = cq.CreatedAt,
                    ProcessingStartedAt = cq.ProcessingStartedAt,
                    CompletedAt = cq.CompletedAt,
                    NumberId = cq.RouteNumberId,
                    RouteId = cq.RouteId,
                    CallerNumber = cq.CallerNumber,
                    SessionId = null,
                    SessionStatus = null
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
                paginatedResult.NextCursor = hasMore
                    ? new PaginationCursor { Timestamp = callQueueItems.Last().CreatedAt, Id = callQueueItems.Last().Id }.Encode()
                    : null;

                paginatedResult.PreviousCursor = (decodedCursor != null || callQueueItems.Count == limit)
                    ? new PaginationCursor { Timestamp = callQueueItems.First().CreatedAt, Id = callQueueItems.First().Id }.Encode()
                    : null;

                paginatedResult.HasPreviousPage = decodedCursor != null;
            }
            else // We fetched 'previous'
            {
                paginatedResult.HasPreviousPage = hasMore;
                paginatedResult.PreviousCursor = hasMore
                    ? new PaginationCursor { Timestamp = callQueueItems.First().CreatedAt, Id = callQueueItems.First().Id }.Encode()
                    : null;

                paginatedResult.NextCursor = new PaginationCursor { Timestamp = callQueueItems.Last().CreatedAt, Id = callQueueItems.Last().Id }.Encode();
                paginatedResult.HasNextPage = true;
            }

            result.SetSuccessResult(paginatedResult);
            return result;
        }

        public async Task<FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>> GetOutboundConversationsMetaDataListAsync(
            long businessId,
            int limit = 20,
            string? nextCursor = null,
            string? previousCursor = null
        )
        {
            var result = new FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>();
            var paginatedResult = new PaginatedResult<OutboundConversationMetadataModel>();

            limit = Math.Clamp(limit, 1, 100);
            paginatedResult.PageSize = limit;

            bool fetchNext = string.IsNullOrWhiteSpace(previousCursor);
            string? currentCursor = fetchNext ? nextCursor : previousCursor;
            PaginationCursor? decodedCursor = PaginationCursor.Decode(currentCursor);

            // 1. Fetch Call Queue Data Page from our new merged method
            var (callQueueItems, hasMore) = await _outboundCallQueueRepository.GetOutboundCallQueuesForBusinessPaginatedAsync(
                businessId,
                limit,
                decodedCursor,
                fetchNext
            );

            if (callQueueItems == null || !callQueueItems.Any())
            {
                paginatedResult.HasNextPage = false;
                paginatedResult.HasPreviousPage = decodedCursor != null && !fetchNext;
                result.SetSuccessResult(paginatedResult);
                return result;
            }

            // 2. Enrich with Conversation State (same as inbound)
            var queueIdsWithSession = callQueueItems
                .Where(cq => !string.IsNullOrEmpty(cq.SessionId))
                .Select(cq => cq.Id)
                .Distinct()
                .ToList();

            Dictionary<string, ConversationState> conversationStates = new Dictionary<string, ConversationState>();
            if (queueIdsWithSession.Any())
            {
                conversationStates = await _conversationStateRepository.GetByQueueIdsAsync(queueIdsWithSession);
            }

            // 3. Map to Metadata Model
            paginatedResult.Items = callQueueItems.Select(cq =>
            {
                var metadata = new OutboundConversationMetadataModel
                {
                    QueueId = cq.Id,
                    Status = cq.Status,
                    Logs = cq.Logs,
                    EnqueuedAt = cq.CreatedAt,
                    ProcessingStartedAt = cq.ProcessingStartedAt,
                    CompletedAt = cq.CompletedAt,
                    CampaignId = cq.CampaignId,
                    NumberId = cq.CallingNumberId,
                    RecipientNumber = cq.RecipientNumber,
                    SessionId = null,
                    SessionStatus = null
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
                paginatedResult.NextCursor = hasMore
                    ? new PaginationCursor { Timestamp = callQueueItems.Last().CreatedAt, Id = callQueueItems.Last().Id }.Encode()
                    : null;

                paginatedResult.PreviousCursor = (decodedCursor != null || callQueueItems.Count == limit)
                    ? new PaginationCursor { Timestamp = callQueueItems.First().CreatedAt, Id = callQueueItems.First().Id }.Encode()
                    : null;

                paginatedResult.HasPreviousPage = decodedCursor != null;
            }
            else // We fetched 'previous'
            {
                paginatedResult.HasPreviousPage = hasMore;
                paginatedResult.PreviousCursor = hasMore
                    ? new PaginationCursor { Timestamp = callQueueItems.First().CreatedAt, Id = callQueueItems.First().Id }.Encode()
                    : null;

                paginatedResult.NextCursor = new PaginationCursor { Timestamp = callQueueItems.Last().CreatedAt, Id = callQueueItems.Last().Id }.Encode();
                paginatedResult.HasNextPage = true;
            }

            result.SetSuccessResult(paginatedResult);
            return result;
        }


        public async Task<FunctionReturnResult<ConversationStateViewModel?>> GetConversationStateViewModelByIdAsync(long businessId, string sessionId)
        {
            var result = new FunctionReturnResult<ConversationStateViewModel?>();
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

                ConversationStateViewModel resultModel = new ConversationStateViewModel()
                {
                    Id = state.Id,
                    QueueId = state.QueueId,
                    Status = state.Status,
                    StartTime = state.StartTime,
                    EndTime = state.EndTime,
                    Clients = new List<ConversationStateClientViewModel>(),
                    Agents = new List<ConversationStateAgentViewModel>(),
                    Messages = new List<ConversationStateMessageViewModel>(),
                    Logs = new List<ConversationStateLogViewModel>()
                };

                int audioUrlExpirySeconds = (int)TimeSpan.FromMinutes(30).TotalSeconds;

                foreach (var client in state.Clients)
                {
                    string? audioUrl = null;
                    if (client.AudioInfo.AudioCompilationStatus == ConversationMemberAudioCompilationStatus.Compiled)
                    {
                        audioUrl = await _conversationAudioRepository.GeneratePresignedUrlAsync($"{sessionId}/compiled/{client.ClientId}.wav", audioUrlExpirySeconds);
                    }

                    var clientModel = new ConversationStateClientViewModel()
                    {
                        ClientId = client.ClientId,
                        ClientType = client.ClientType,
                        JoinedAt = client.JoinedAt,
                        LeftAt = client.LeftAt,
                        LeaveReason = client.LeaveReason,
                        AudioCompilationStatus = client.AudioInfo.AudioCompilationStatus,
                        AudioUrl = audioUrl,
                        AudioFailedReason = client.AudioInfo.FailedReason
                    };

                    resultModel.Clients.Add(clientModel);
                }

                foreach (var agent in state.Agents)
                {
                    string? audioUrl = null;
                    if (agent.AudioInfo.AudioCompilationStatus == ConversationMemberAudioCompilationStatus.Compiled)
                    {
                        audioUrl = await _conversationAudioRepository.GeneratePresignedUrlAsync($"{sessionId}/compiled/{agent.AgentId}.wav", audioUrlExpirySeconds);
                    }

                    var clientModel = new ConversationStateAgentViewModel()
                    {
                        AgentId = agent.AgentId,
                        AgentType = agent.AgentType,
                        JoinedAt = agent.JoinedAt,
                        LeftAt = agent.LeftAt,
                        LeaveReason = agent.LeaveReason,
                        AudioCompilationStatus = agent.AudioInfo.AudioCompilationStatus,
                        AudioUrl = audioUrl,
                        AudioFailedReason = agent.AudioInfo.FailedReason
                    };

                    resultModel.Agents.Add(clientModel);
                }

                foreach (var message in state.Messages)
                {
                    var messageModel = new ConversationStateMessageViewModel()
                    {
                        SenderId = message.SenderId,
                        Role = message.Role,
                        Content = message.Content,
                        Timestamp = message.Timestamp,
                    };

                    resultModel.Messages.Add(messageModel);
                }

                foreach (var log in state.Logs)
                {
                    var logModel = new ConversationStateLogViewModel()
                    {
                        Level = log.Level,
                        Timestamp = log.Timestamp,
                        Message = log.Message
                    };

                    resultModel.Logs.Add(logModel);
                }

                result.SetSuccessResult(resultModel);
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
