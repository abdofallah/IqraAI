using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.Conversations;
using IqraCore.Models.Business.Queues;
using IqraCore.Models.Business.Queues.Inbound;
using IqraCore.Models.Business.Queues.Outbound;
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

        /**
         * 
         * Inbound
         * 
        **/
        public async Task<FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>> GetInboundConversationsMetaDataListAsync(long businessId, GetBusinessInboundCallQueuesRequestModel modelData)
        {
            var result = new FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>();
            var paginatedResult = new PaginatedResult<InboundConversationMetadataModel>();

            modelData.Limit = Math.Clamp(modelData.Limit, 1, 100);
            paginatedResult.PageSize = modelData.Limit;

            // --- NEW: Validation Logic ---
            if (!string.IsNullOrEmpty(modelData.PreviousCursor) && !string.IsNullOrEmpty(modelData.NextCursor))
            {
                return result.SetFailureResult(
                    "GetInboundConversationsMetaDataListAsync:INVALID_CURSOR",
                    "Cannot provide both nextCursor and previousCursor."
                );
            }

            if ((!string.IsNullOrEmpty(modelData.PreviousCursor) || !string.IsNullOrEmpty(modelData.NextCursor)) && modelData.Filter != null)
            {
                return result.SetFailureResult(
                    "GetInboundConversationsMetaDataListAsync:INVALID_REQUEST",
                    "Cannot provide both a cursor and a new filter. The cursor already contains the filter context."
                );
            }

            // --- End of Validation ---

            bool fetchNext = string.IsNullOrWhiteSpace(modelData.PreviousCursor);
            string? currentCursorString = fetchNext ? modelData.NextCursor : modelData.PreviousCursor;

            var decodedCursor = PaginationCursor<GetBusinessInboundCallQueuesRequestFilterModel>.Decode(currentCursorString);

            // Determine the active filter: from the cursor if it exists, otherwise from the request.
            GetBusinessInboundCallQueuesRequestFilterModel activeFilter =
                decodedCursor?.Filter ?? modelData.Filter ?? new GetBusinessInboundCallQueuesRequestFilterModel();

            // 1. Fetch Call Queue Data Page using the new repository method
            var (callQueueItems, hasMore, totalCount) = await _inboundCallQueueRepository.GetInboundCallQueuesForBusinessPaginatedAsync(
                businessId,
                activeFilter,
                modelData.Limit,
                decodedCursor,
                fetchNext
            );

            paginatedResult.TotalCount = (int)totalCount;

            if (callQueueItems == null || !callQueueItems.Any())
            {
                paginatedResult.HasNextPage = false;
                paginatedResult.HasPreviousPage = decodedCursor != null && !fetchNext;
                result.SetSuccessResult(paginatedResult);
                return result;
            }

            // --- UNCHANGED: Enrichment and Mapping logic remains identical ---
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
            // --- End of Unchanged Logic ---

            // 4. Set Cursors and Flags using the new generic, filter-aware cursor
            if (fetchNext)
            {
                paginatedResult.HasNextPage = hasMore;
                paginatedResult.NextCursor = hasMore
                    ? new PaginationCursor<GetBusinessInboundCallQueuesRequestFilterModel> { Timestamp = callQueueItems.Last().CreatedAt, Id = callQueueItems.Last().Id, Filter = activeFilter }.Encode()
                    : null;

                paginatedResult.PreviousCursor = (decodedCursor != null || callQueueItems.Count == modelData.Limit)
                    ? new PaginationCursor<GetBusinessInboundCallQueuesRequestFilterModel> { Timestamp = callQueueItems.First().CreatedAt, Id = callQueueItems.First().Id, Filter = activeFilter }.Encode()
                    : null;

                paginatedResult.HasPreviousPage = decodedCursor != null;
            }
            else // We fetched 'previous'
            {
                paginatedResult.HasPreviousPage = hasMore;
                paginatedResult.PreviousCursor = hasMore
                    ? new PaginationCursor<GetBusinessInboundCallQueuesRequestFilterModel> { Timestamp = callQueueItems.First().CreatedAt, Id = callQueueItems.First().Id, Filter = activeFilter }.Encode()
                    : null;

                paginatedResult.NextCursor = new PaginationCursor<GetBusinessInboundCallQueuesRequestFilterModel> { Timestamp = callQueueItems.Last().CreatedAt, Id = callQueueItems.Last().Id, Filter = activeFilter }.Encode();
                paginatedResult.HasNextPage = true;
            }

            result.SetSuccessResult(paginatedResult);
            return result;
        }

        public async Task<FunctionReturnResult<long?>> GetInboundCallQueuesCountAsync(long businessId, GetBusinessInboundCallQueuesCountRequestModel modelData)
        {
            var result = new FunctionReturnResult<long?>();

            try
            {
                var count = await _inboundCallQueueRepository.GetInboundCallQueuesCountAsync(businessId, modelData);

                if (count == null)
                {
                    return result.SetFailureResult(
                        "GetInboundCallQueuesCountAsync:DATABASE_COUNT_ERROR",
                        "Unable to fetch count from the database."
                    );
                }

                return result.SetSuccessResult(count);
            }
            catch (Exception ex)
            {
                result.SetFailureResult(
                    "GetInboundCallQueuesCountAsync:EXCEPTION",
                    "An internal server error occurred while fetching the call queue count."
                );
                return result;
            }
        }

        public async Task<FunctionReturnResult<InboundConversationMetadataModel?>> GetInboundConversationsMetaDataAsync(long businessId, string queueId)
        {
            var result = new FunctionReturnResult<InboundConversationMetadataModel?>();

            try
            {
                var callQueueItem = await _inboundCallQueueRepository.GetInboundCallQueueByIdAsync(businessId, queueId);
                if (callQueueItem == null)
                {
                    return result.SetFailureResult(
                        "GetInboundConversationsMetaDataAsync:NOT_FOUND",
                        $"Inbound conversation with Queue ID '{queueId}' not found."
                    );
                }

                ConversationState? conversationState = null;
                if (!string.IsNullOrEmpty(callQueueItem.SessionId))
                {
                    conversationState = await _conversationStateRepository.GetByIdAsync(callQueueItem.SessionId);
                }

                var metadata = new InboundConversationMetadataModel
                {
                    QueueId = callQueueItem.Id,
                    Status = callQueueItem.Status,
                    Logs = callQueueItem.Logs,
                    EnqueuedAt = callQueueItem.CreatedAt,
                    ProcessingStartedAt = callQueueItem.ProcessingStartedAt,
                    CompletedAt = callQueueItem.CompletedAt,
                    NumberId = callQueueItem.RouteNumberId,
                    RouteId = callQueueItem.RouteId,
                    CallerNumber = callQueueItem.CallerNumber,
                    SessionId = conversationState?.Id,
                    SessionStatus = conversationState?.Status
                };

                return result.SetSuccessResult(metadata);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetInboundConversationsMetaDataAsync:EXCEPTION",
                    $"An internal server error occurred: {ex.Message}"
                );
            }
        }

        /**
         * 
         * Outbound
         * 
        **/
        public async Task<FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>> GetOutboundConversationsMetaDataListAsync(long businessId, GetBusinessOutboundCallQueuesRequestModel modelData)
        {
            var result = new FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>();
            var paginatedResult = new PaginatedResult<OutboundConversationMetadataModel>();

            modelData.Limit = Math.Clamp(modelData.Limit, 1, 100);
            paginatedResult.PageSize = modelData.Limit;

            if (!string.IsNullOrEmpty(modelData.PreviousCursor) && !string.IsNullOrEmpty(modelData.NextCursor))
            {
                return result.SetFailureResult(
                    "GetOutboundConversationsMetaDataListAsync:INVALID_CURSOR",
                    "Cannot provide both nextCursor and previousCursor."
                );
            }

            if (
                (!string.IsNullOrEmpty(modelData.PreviousCursor) || !string.IsNullOrEmpty(modelData.NextCursor))
                &&
                modelData.Filter != null
            )
            {
                return result.SetFailureResult(
                    "GetOutboundConversationsMetaDataListAsync:INVALID_REQUEST",
                    "Cannot provide both cursor and filter. The cursor already contains the filter context."
                );
            }

            bool fetchNext = string.IsNullOrWhiteSpace(modelData.PreviousCursor);
            string? currentCursorString = fetchNext ? modelData.NextCursor : modelData.PreviousCursor;

            var decodedCursor = PaginationCursor<GetBusinessOutboundCallQueuesRequestFilterModel>.Decode(currentCursorString);

            GetBusinessOutboundCallQueuesRequestFilterModel activeFilter =
                decodedCursor?.Filter ?? modelData.Filter ?? new GetBusinessOutboundCallQueuesRequestFilterModel();

            var (callQueueItems, hasMore, totalCount) = await _outboundCallQueueRepository.GetOutboundCallQueuesForBusinessPaginatedAsync(
                businessId,
                activeFilter,
                modelData.Limit,
                decodedCursor,
                fetchNext
            );

            paginatedResult.TotalCount = totalCount;

            if (callQueueItems == null || !callQueueItems.Any())
            {
                paginatedResult.HasNextPage = false;
                paginatedResult.HasPreviousPage = decodedCursor != null && !fetchNext;

                return result.SetSuccessResult(paginatedResult);
            }

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

            paginatedResult.Items = callQueueItems.Select(cq =>
            {
                var metadata = new OutboundConversationMetadataModel {
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

            if (fetchNext)
            {
                paginatedResult.HasNextPage = hasMore;
                paginatedResult.NextCursor = hasMore
                    ? new PaginationCursor<GetBusinessOutboundCallQueuesRequestFilterModel> { Timestamp = callQueueItems.Last().CreatedAt, Id = callQueueItems.Last().Id, Filter = activeFilter }.Encode()
                    : null;

                paginatedResult.PreviousCursor = (decodedCursor != null || callQueueItems.Count == modelData.Limit)
                    ? new PaginationCursor<GetBusinessOutboundCallQueuesRequestFilterModel> { Timestamp = callQueueItems.First().CreatedAt, Id = callQueueItems.First().Id, Filter = activeFilter }.Encode()
                    : null;

                paginatedResult.HasPreviousPage = decodedCursor != null;
            }
            else // We fetched 'previous'
            {
                paginatedResult.HasPreviousPage = hasMore;
                paginatedResult.PreviousCursor = hasMore
                    ? new PaginationCursor<GetBusinessOutboundCallQueuesRequestFilterModel> { Timestamp = callQueueItems.First().CreatedAt, Id = callQueueItems.First().Id, Filter = activeFilter }.Encode()
                    : null;

                paginatedResult.NextCursor = new PaginationCursor<GetBusinessOutboundCallQueuesRequestFilterModel> { Timestamp = callQueueItems.Last().CreatedAt, Id = callQueueItems.Last().Id, Filter = activeFilter }.Encode();
                paginatedResult.HasNextPage = true;
            }

            return result.SetSuccessResult(paginatedResult);
        }

        public async Task<FunctionReturnResult<long?>> GetOutboundCallQueuesCountAsync(long businessId, GetBusinessOutboundCallQueuesCountRequestModel modelData)
        {
            var result = new FunctionReturnResult<long?>();

            try
            {
                var count = await _outboundCallQueueRepository.GetOutboundCallQueuesCountAsync(businessId, modelData);
                if (count == null)
                {
                    return result.SetFailureResult(
                        "GetOutboundCallQueuesCountAsync:DATABASE_COUNT_ERROR",
                        "Unable to fetch count from database."
                    );
                }
 
                return result.SetSuccessResult(count);
            }
            catch (Exception ex)
            {
                result.SetFailureResult(
                    "GetOutboundCallQueuesCountAsync:EXCEPTION",
                    $"Internal Server Error: {ex.Message}"
                );
                return result;
            }
        }

        public async Task<FunctionReturnResult<OutboundConversationMetadataModel?>> GetOutboundConversationsMetaDataAsync(long businessId, string queueId)
        {
            var result = new FunctionReturnResult<OutboundConversationMetadataModel?>();

            try
            {
                var callQueueItem = await _outboundCallQueueRepository.GetOutboundCallQueueByIdAsync(businessId, queueId);
                if (callQueueItem == null)
                {
                    return result.SetFailureResult(
                        "GetOutboundConversationsMetaDataAsync:NOT_FOUND",
                        $"Outbound conversation with Queue ID '{queueId}' not found."
                    );
                }

                ConversationState? conversationState = null;
                if (!string.IsNullOrEmpty(callQueueItem.SessionId))
                {
                    conversationState = await _conversationStateRepository.GetByIdAsync(callQueueItem.SessionId);
                }

                var metadata = new OutboundConversationMetadataModel
                {
                    QueueId = callQueueItem.Id,
                    Status = callQueueItem.Status,
                    Logs = callQueueItem.Logs,
                    EnqueuedAt = callQueueItem.CreatedAt,
                    ProcessingStartedAt = callQueueItem.ProcessingStartedAt,
                    CompletedAt = callQueueItem.CompletedAt,
                    CampaignId = callQueueItem.CampaignId,
                    NumberId = callQueueItem.CallingNumberId,
                    RecipientNumber = callQueueItem.RecipientNumber,
                    SessionId = conversationState?.Id,
                    SessionStatus = conversationState?.Status
                };

                return result.SetSuccessResult(metadata);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetOutboundConversationsMetaDataAsync:EXCEPTION",
                    $"An internal server error occurred: {ex.Message}"
                );
            }
        }

        /**
         * 
         * Conversation 
         * 
        **/
        public async Task<FunctionReturnResult<ConversationStateViewModel?>> GetConversationState(long businessId, string sessionId)
        {
            var result = new FunctionReturnResult<ConversationStateViewModel?>();

            try
            {
                var state = await _conversationStateRepository.GetByIdAsync(businessId, sessionId);
                if (state == null)
                {
                    return result.SetFailureResult(
                        "GetConversationStateByIdAsync:NOT_FOUND",
                        $"Conversation state with Session ID '{sessionId}' not found."
                    );
                }

                var resultModel = await MapConversationStateToViewModelAsync(state);
                
                return result.SetSuccessResult(resultModel);
            }
            catch (Exception ex)
            {
                result.SetFailureResult(
                    "GetConversationStateByIdAsync:EXCEPTION",
                    $"An error occurred while fetching conversation state: {ex.Message}"
                );
                return result;
            }
        }

        public async Task<FunctionReturnResult<PaginatedResult<ConversationStateViewModel>?>> GetConversationStatesAsync(long businessId, GetBusinessConversationsRequestModel modelData)
        {
            var result = new FunctionReturnResult<PaginatedResult<ConversationStateViewModel>?>();
            var paginatedResult = new PaginatedResult<ConversationStateViewModel>();

            modelData.Limit = Math.Clamp(modelData.Limit, 1, 100);
            paginatedResult.PageSize = modelData.Limit;

            if (!string.IsNullOrEmpty(modelData.PreviousCursor) && !string.IsNullOrEmpty(modelData.NextCursor))
            {
                return result.SetFailureResult(
                    "GetConversationStatesAsync:INVALID_CURSOR",
                    "Cannot provide both nextCursor and previousCursor."
                );
            }

            if ((!string.IsNullOrEmpty(modelData.PreviousCursor) || !string.IsNullOrEmpty(modelData.NextCursor)) && modelData.Filter != null)
            {
                return result.SetFailureResult(
                    "GetConversationStatesAsync:INVALID_REQUEST",
                    "Cannot provide both a cursor and a new filter."
                );
            }

            bool fetchNext = string.IsNullOrWhiteSpace(modelData.PreviousCursor);
            string? currentCursorString = fetchNext ? modelData.NextCursor : modelData.PreviousCursor;

            var decodedCursor = PaginationCursor<GetBusinessConversationsRequestFilterModel>.Decode(currentCursorString);

            GetBusinessConversationsRequestFilterModel activeFilter =
                decodedCursor?.Filter ?? modelData.Filter ?? new GetBusinessConversationsRequestFilterModel();

            var (conversationStates, hasMore, totalCount) = await _conversationStateRepository.GetConversationStatesPaginatedAsync(
                businessId,
                activeFilter,
                modelData.Limit,
                decodedCursor,
                fetchNext
            );

            paginatedResult.TotalCount = (int)totalCount;

            if (conversationStates == null || !conversationStates.Any())
            {
                paginatedResult.HasNextPage = false;
                paginatedResult.HasPreviousPage = decodedCursor != null && !fetchNext;
                return result.SetSuccessResult(paginatedResult);
            }

            // Asynchronously map all ConversationState objects to ConversationStateViewModels in parallel
            var mappingTasks = conversationStates.Select(state => MapConversationStateToViewModelAsync(state));
            var viewModels = await Task.WhenAll(mappingTasks);
            paginatedResult.Items = viewModels.ToList();

            // Create and encode cursors, using StartTime as the primary timestamp
            if (fetchNext)
            {
                paginatedResult.HasNextPage = hasMore;
                paginatedResult.NextCursor = hasMore
                    ? new PaginationCursor<GetBusinessConversationsRequestFilterModel> { Timestamp = conversationStates.Last().StartTime, Id = conversationStates.Last().Id, Filter = activeFilter }.Encode()
                    : null;
                paginatedResult.PreviousCursor = (decodedCursor != null || conversationStates.Count == modelData.Limit)
                    ? new PaginationCursor<GetBusinessConversationsRequestFilterModel> { Timestamp = conversationStates.First().StartTime, Id = conversationStates.First().Id, Filter = activeFilter }.Encode()
                    : null;
                paginatedResult.HasPreviousPage = decodedCursor != null;
            }
            else // Fetched 'previous'
            {
                paginatedResult.HasPreviousPage = hasMore;
                paginatedResult.PreviousCursor = hasMore
                    ? new PaginationCursor<GetBusinessConversationsRequestFilterModel> { Timestamp = conversationStates.First().StartTime, Id = conversationStates.First().Id, Filter = activeFilter }.Encode()
                    : null;
                paginatedResult.NextCursor = new PaginationCursor<GetBusinessConversationsRequestFilterModel> { Timestamp = conversationStates.Last().StartTime, Id = conversationStates.Last().Id, Filter = activeFilter }.Encode();
                paginatedResult.HasNextPage = true;
            }

            return result.SetSuccessResult(paginatedResult);
        }

        private async Task<ConversationStateViewModel> MapConversationStateToViewModelAsync(ConversationState state)
        {
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
                    audioUrl = await _conversationAudioRepository.GeneratePresignedUrlAsync($"{state.Id}/compiled/{client.ClientId}.wav", audioUrlExpirySeconds);
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
                    audioUrl = await _conversationAudioRepository.GeneratePresignedUrlAsync($"{state.Id}/compiled/{agent.AgentId}.wav", audioUrlExpirySeconds);
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

            // TODO stop using the message view model and use turn view model
            foreach (var turn in state.Turns)
            {
                var userMessageModel = new ConversationStateMessageViewModel()
                {
                    SenderId = turn.UserInput.SenderId,
                    Role = ConversationSenderRole.Client,
                    Content = turn.UserInput.TranscribedText ?? "",
                    Timestamp = turn.UserInput.StartedSpeakingAt,
                };
                resultModel.Messages.Add(userMessageModel);

                var agentMessageModel = new ConversationStateMessageViewModel()
                {
                    SenderId = turn.Response.AgentId,
                    Role = ConversationSenderRole.Agent,
                    Content = "",
                    Timestamp = turn.Response.LLMStreamingStartedAt
                };
                if (turn.Response.Type == IqraCore.Entities.Conversation.Turn.ConversationTurnAgentResponseType.Speech)
                {
                    turn.Response.SpokenSegments.ForEach(segment => agentMessageModel.Content += segment.Text + " ");
                }
                else if (turn.Response.Type == IqraCore.Entities.Conversation.Turn.ConversationTurnAgentResponseType.CustomTool || turn.Response.Type == IqraCore.Entities.Conversation.Turn.ConversationTurnAgentResponseType.SystemTool)
                {
                    if (turn.Response.ToolExecution != null)
                    {
                        agentMessageModel.Content = turn.Response.ToolExecution.RawLLMInput;
                    }
                    else
                    {
                        agentMessageModel.Content = "ERROR: Conversation Turn Response Tool execution not found";
                    }
                }
                else
                {
                    agentMessageModel.Content = "ERROR: Conversation Turn Response Type not found";
                }
                resultModel.Messages.Add(agentMessageModel);
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

            return resultModel;
        }
    }
}
