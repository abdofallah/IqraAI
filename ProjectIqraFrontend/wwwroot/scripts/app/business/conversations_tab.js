/** Dynamic Variables **/

// Inbound
let CurrentInboundFilters = null;
let CurrentInboundConversationsData = [];
let CurrentInboundNextCursor = null;
let CurrentInboundPrevCursor = null;
let IsLoadingInboundConversations = false;
const InboundConversationsPageSize = 12;
let currentInboundPageNumber = 1;

// Outbound
let CurrentOutboundFilters = null;
let CurrentOutboundConversationsData = [];
let CurrentOutboundNextCursor = null;
let CurrentOutboundPrevCursor = null;
let IsLoadingOutboundConversations = false;
const OutboundConversationsPageSize = 12;
let currentOutboundPageNumber = 1;

// Web Session
let CurrentWebSessionFilters = null;
let CurrentWebSessionData = [];
let CurrentWebSessionNextCursor = null;
let CurrentWebSessionPrevCursor = null;
let IsLoadingWebSessionConversations = false;
const WebSessionConversationsPageSize = 12;
let currentWebSessionPageNumber = 1;

// Conversation Manager
let CurrentViewStateData = null;
let CurrentViewQueueData = null;
let IsLoadingManageView = false;
let waveSurferAgent = null;
let waveSurferClient = null;

// Session Logs Pagination
let sessionLogsCurrentPage = 1;
let sessionLogsTotalCount = 0;
const sessionLogsPageSize = 30;
let isLoadingSessionLogs = false;

// Audio Turn Sync (F5)
let isSyncAudioEnabled = true;
let sessionStartTimeMs = null;
let currentActiveTurnId = null;
let lastHighlightUpdate = 0;

// Enums mirroring backend
const CallQueueStatusEnum = {
    Queued: 0,
    ProcessingProxy: 1,
    ProcessedProxy: 2,
    ProcessingBackend: 3,
    ProcessedBackend: 4,
    Failed: 5,
    Cancelled: 6,
    Expired: 7
};
const ConversationSessionState = {
    Created: 0,
    WaitingForPrimaryClient: 1,
    Starting: 2,
    Active: 3,
    Paused: 4,
    Ending: 5,
    Ended: 6,
    Failed: 7,
};
const ConversationMemberAudioCompilationStatus = {
    WaitingForSessionEnd: 0,
    Compiling: 1,
    Compiled: 2,
    Failed: 3
};
const ConversationSenderRole = {
    System: 0,
    Client: 1,
    Agent: 2
};
const WebSessionStatusEnum = {
    Queued: 0,
    ProcessingQueue: 1,
    ProcessingBackend: 2,
    ProcessedBackend: 3,
    Failed: 4,
    Canceled: 5,
    Expired: 6
};

/** Element Variables **/
const conversationsTab = $("#conversations-tab");

// List View Elements
const conversationListTab = conversationsTab.find("#conversationListTab");

// Inbound
const conversationInboundTable = conversationListTab.find("#conversationInboundTable");
const conversationInboundTableBody = conversationInboundTable.find("tbody");
const inboundPaginationControls = conversationsTab.find("#inboundPaginationControls");
const inboundPrevButton = conversationsTab.find("#inboundPrevButton");
const inboundNextButton = conversationsTab.find("#inboundNextButton");
const inboundPageInfo = conversationsTab.find("#inboundPageInfo"); // Optional page info element
const inboundFilterForm = $("#inboundFilterForm");
const inboundFilterApplyButton = $("#inboundFilterApplyButton");
const inboundFilterResetButton = $("#inboundFilterResetButton");
const clearInboundFiltersButton = $("#clearInboundFiltersButton");
const filterInboundCallButton = $("#filterInboundCallButton");

// Outbound
const conversationOutboundTable = conversationsTab.find("#conversationOutboundTable");
const conversationOutboundTableBody = conversationOutboundTable.find("tbody");
const outboundPaginationControls = conversationsTab.find("#outboundPaginationControls");
const outboundPrevButton = conversationsTab.find("#outboundPrevButton");
const outboundNextButton = conversationsTab.find("#outboundNextButton");
const outboundPageInfo = conversationsTab.find("#outboundPageInfo");
const outboundFilterForm = $("#outboundFilterForm");
const outboundFilterApplyButton = $("#outboundFilterApplyButton");
const outboundFilterResetButton = $("#outboundFilterResetButton");
const clearOutboundFiltersButton = $("#clearOutboundFiltersButton");
const filterOutboundCallButton = $("#filterOutboundCallButton");

// Websocket (Web Session) Elements
const conversationWebsocketTable = conversationsTab.find("#conversationWebsocketTable");
const conversationWebsocketTableBody = conversationWebsocketTable.find("tbody");
const websocketPaginationControls = conversationsTab.find("#websocketPaginationControls");
const websocketPrevButton = conversationsTab.find("#websocketPrevButton");
const websocketNextButton = conversationsTab.find("#websocketNextButton");
const websocketPageInfo = conversationsTab.find("#websocketPageInfo");

const websocketFilterForm = $("#websocketFilterForm");
const websocketFilterApplyButton = $("#websocketFilterApplyButton");
const websocketFilterResetButton = $("#websocketFilterResetButton");
const clearWebsocketFiltersButton = $("#clearWebsocketFiltersButton");
const filterWebsocketButton = $("#filterWebsocketButton");
const websocketSearchInput = $("#websocketSearchInput");
const searchConversationWebsocketButton = $("#searchConversationWebsocketButton");

// Manage View Elements
const conversationManageTab = conversationsTab.find("#conversationManageTab");
const manageViewLoader = conversationsTab.find("#manageViewLoader"); // Loader overlay
const switchBackToConversationsListTabButton = conversationsTab.find("#switchBackToConversationsListTab");
const currentConversationTypeName = conversationManageTab.find("#currentConversationTypeName");
const currentConversationName = conversationManageTab.find("#currentConversationName");

// Manage View - General Tab Elements
const manageViewQueueId = $("#manageViewQueueId");
const manageViewQueueStatus = $("#manageViewQueueStatus");
const manageViewEnqueuedAt = $("#manageViewEnqueuedAt");
const manageViewFromNumber = $("#manageViewFromNumber");
const manageViewToNumber = $("#manageViewToNumber");
const manageViewSessionId = $("#manageViewSessionId");
const manageViewSessionStatus = $("#manageViewSessionStatus");
const manageViewStartTime = $("#manageViewStartTime");
const manageViewEndTime = $("#manageViewEndTime");
const manageViewRouting = $("#manageViewRouting");

// Manage View - Metrics Tab Elements
const conversationsManageMetricsTab = $("#conversationsManage-metrics-tab");

const metricsTabItem = $("#conversationsManage-metrics-tab-item");
const metricsTabPane = $("#conversationsManage-metrics");
const metricDurationSeconds = $("#metricDurationSeconds");
const metricClientMessageCount = $("#metricClientMessageCount");
const metricAgentMessageCount = $("#metricAgentMessageCount");
const metricAverageAgentResponseTimeMs = $("#metricAverageAgentResponseTimeMs");
const metricClientWordCount = $("#metricClientWordCount");
const metricAgentWordCount = $("#metricAgentWordCount");
const metricClientInterruptionCount = $("#metricClientInterruptionCount");
const metricAgentInterruptionCount = $("#metricAgentInterruptionCount");
const metricSilenceCount = $("#metricSilenceCount");
const metricTotalSilenceDurationSeconds = $("#metricTotalSilenceDurationSeconds");
const metricSttAverageLatencyMs = $("#metricSttAverageLatencyMs");
const metricLlmAverageLatencyMs = $("#metricLlmAverageLatencyMs");
const metricTtsAverageLatencyMs = $("#metricTtsAverageLatencyMs");
const metricAdditionalMetricsContainer = $("#metricAdditionalMetricsContainer");

// Manage View - Conversation Tab Elements
const conversationsManageConversationTab = $("#conversationsManage-conversation-tab");

const agentAudioContainer = $("#agentAudioContainer");
const waveformAgentAudio = $("#waveform-agent-audio");
const agentAudioLoader = waveformAgentAudio.find(".audio-loader");
const agentAudioPlayBtn = agentAudioContainer.find(".agent-audio-play");
const agentAudioDownloadBtn = agentAudioContainer.find(".agent-audio-download");
const agentAudioError = agentAudioContainer.find(".agent-audio-error");

const clientAudioContainer = $("#clientAudioContainer");
const waveformClientAudio = $("#waveform-client-audio");
const clientAudioLoader = waveformClientAudio.find(".audio-loader");
const clientAudioPlayBtn = clientAudioContainer.find(".client-audio-play");
const clientAudioDownloadBtn = clientAudioContainer.find(".client-audio-download");
const clientAudioError = clientAudioContainer.find(".client-audio-error");

const syncPlayPauseBtn = $("#syncPlayPauseBtn");
const syncStopBtn = $("#syncStopBtn");
const conversationTurnsContainer = $("#conversationTurnsContainer");

// Manage View - Logs Tab Elements
const manageViewRoutingContainer = $("#manageViewRoutingContainer");
const logsTabButton = $("#conversationsManage-logs-tab");
const queueLogsContainer = $("#queueLogsContainer");
const sessionLogsContainer = $("#sessionLogsContainer");
const sessionLogSearchInput = $("#sessionLogSearchInput");
const sessionLogLevelFilter = $("#sessionLogLevelFilter");
const sessionLogSearchBtn = $("#sessionLogSearchBtn");
const sessionLogsInfo = $("#sessionLogsInfo");
const sessionLogsPrevBtn = $("#sessionLogsPrevBtn");
const sessionLogsNextBtn = $("#sessionLogsNextBtn");

/** API Functions **/
function FetchInboundConversationsMetaDataFromAPI(requestPayload, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/conversations/inbound/metadata`,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(requestPayload),
        dataType: "json",
        success: (response) => {
            if (!response || !response.success) {
                errorCallback(response || { message: "Unknown error occurred." });
                return;
            }
            successCallback(response.data);
        },
        error: (jqXHR, textStatus, errorThrown) => {
            let errorMsg = "An error occurred while fetching inbound conversations.";
            if (jqXHR.responseJSON && jqXHR.responseJSON.message) {
                errorMsg = jqXHR.responseJSON.message;
            }
            console.error("Fetch Inbound Conversations Error:", jqXHR.responseJSON || textStatus || errorThrown);
            errorCallback({ message: errorMsg, code: jqXHR.status });
        },
    });
}
function FetchOutboundConversationsMetaDataFromAPI(requestPayload, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/conversations/outbound/metadata`,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(requestPayload),
        dataType: "json",
        success: (response) => {
            if (!response || !response.success) {
                errorCallback(response || { message: "Unknown error occurred." });
                return;
            }
            successCallback(response.data);
        },
        error: (jqXHR, textStatus, errorThrown) => {
            let errorMsg = "An error occurred while fetching outbound conversations.";
            if (jqXHR.responseJSON && jqXHR.responseJSON.message) {
                errorMsg = jqXHR.responseJSON.message;
            }
            console.error("Fetch Outbound Conversations Error:", jqXHR.responseJSON || textStatus || errorThrown);
            errorCallback({ message: errorMsg, code: jqXHR.status });
        },
    });
}
function FetchConversationStateBySessionId(sessionId, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/conversations/state/${sessionId}`,
        type: "GET",
        dataType: "json",
        success: (response) => {
            if (!response || !response.success) {
                errorCallback(response || { message: "Unknown error fetching conversation state." });
                return;
            }
            successCallback(response.data);
        },
        error: (jqXHR, textStatus, errorThrown) => {
            let errorMsg = "An error occurred fetching conversation details.";
            if (jqXHR.responseJSON && jqXHR.responseJSON.message) {
                errorMsg = jqXHR.responseJSON.message;
            }
            console.error("Fetch Conversation State Error:", jqXHR.responseJSON || textStatus || errorThrown);
            errorCallback({ message: errorMsg, code: jqXHR.status });
        },
    });
}
function FetchWebSessionConversationsMetaDataFromAPI(requestPayload, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/conversations/websession/metadata`,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(requestPayload),
        dataType: "json",
        success: (response) => {
            if (!response || !response.success) {
                errorCallback(response || { message: "Unknown error occurred." });
                return;
            }
            successCallback(response.data);
        },
        error: (jqXHR, textStatus, errorThrown) => {
            let errorMsg = "An error occurred while fetching web sessions.";
            if (jqXHR.responseJSON && jqXHR.responseJSON.message) {
                errorMsg = jqXHR.responseJSON.message;
            }
            console.error("Fetch Web Session Error:", jqXHR.responseJSON || textStatus || errorThrown);
            errorCallback({ message: errorMsg, code: jqXHR.status });
        },
    });
}
function FetchTemporaryAudioUrl(sessionId, memberType, memberId, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/conversations/state/${sessionId}/audio_url`,
        type: "POST",
        data: JSON.stringify({ memberType: memberType, memberId: memberId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: (response) => {
            if (!response || !response.success || !response.data || !response.data.url) {
                errorCallback(response || { message: "Invalid audio URL response." });
                return;
            }
            successCallback(response.data); // Assuming response.data = { url: "..." }
        },
        error: (jqXHR, textStatus, errorThrown) => {
            // ... error handling ...
            errorCallback({ message: errorMsg, code: jqXHR.status });
        },
    });
}

function FetchQueueLogs(queueId, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/conversations/queue/${queueId}/logs`,
        type: "GET", dataType: "json",
        success: (response) => { if (!response || !response.success) { errorCallback(response || { message: "Unknown error." }); return; } successCallback(response.data); },
        error: (jqXHR) => { errorCallback({ message: jqXHR.responseJSON?.message || "Failed to fetch queue logs." }); },
    });
}
function FetchSessionLogsPaginated(sessionId, page, pageSize, search, levelFilter, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/conversations/session/${sessionId}/logs`,
        type: "POST", contentType: "application/json; charset=utf-8",
        data: JSON.stringify({ page, pageSize, search: search || null, levelFilter: levelFilter }),
        dataType: "json",
        success: (response) => { if (!response || !response.success) { errorCallback(response || { message: "Unknown error." }); return; } successCallback(response.data); },
        error: (jqXHR) => { errorCallback({ message: jqXHR.responseJSON?.message || "Failed to fetch session logs." }); },
    });
}
function FetchWebSessionLogs(webSessionId, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/conversations/websession/${webSessionId}/logs`,
        type: "GET", dataType: "json",
        success: (response) => { if (!response || !response.success) { errorCallback(response || { message: "Unknown error." }); return; } successCallback(response.data); },
        error: (jqXHR) => { errorCallback({ message: jqXHR.responseJSON?.message || "Failed to fetch web session logs." }); },
    });
}

/** Helper Functions **/
function formatDateTime(dateString) {
    if (!dateString) return "N/A";
    try {
        const date = new Date(dateString);
        // Example: 06/25/2024, 10:30 AM - Adjust options for your locale/preference
        return date.toLocaleString(undefined, { // Use browser's default locale
            year: 'numeric',
            month: 'numeric',
            day: 'numeric',
            hour: 'numeric',
            minute: '2-digit',
            hour12: true // Use true for AM/PM, false for 24-hour
        });
    } catch (e) {
        console.error("Error formatting date:", dateString, e);
        return "Invalid Date";
    }
}
function formatDuration(totalSeconds) {
    if (isNaN(totalSeconds) || totalSeconds < 0) return "-";
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = Math.floor(totalSeconds % 60);

    const pad = (num) => String(num).padStart(2, '0');

    return `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`;
}
function getStatusBadgeElement(statusType, statusValue, sessionEndType = null, includeText = true) {
    let iconClass = "fa-regular fa-question-circle";
    let badgeClass = "bg-secondary";
    let statusText = "Unknown";

    // Handle N/A case first
    if (statusValue === null || statusValue === undefined || (typeof statusValue === 'object' && statusValue.value === null)) {
        return `<span class="badge bg-light text-muted" title="Not Applicable">N/A</span>`;
    }

    const numericValue = (typeof statusValue === 'object') ? statusValue.value : statusValue;

    if (statusType === 'queue') {
        switch (numericValue) { // Use numericValue here
            case CallQueueStatusEnum.Queued:
                iconClass = "fa-regular fa-clock";
                badgeClass = "bg-warning text-dark";
                statusText = "Queued";
                break;
            case CallQueueStatusEnum.ProcessingProxy:
            case CallQueueStatusEnum.ProcessedProxy:
            case CallQueueStatusEnum.ProcessingBackend:
                iconClass = "fa-solid fa-spinner fa-spin";
                badgeClass = "bg-info";
                statusText = "Processing";
                break;
            case CallQueueStatusEnum.ProcessedBackend:
                iconClass = "fa-regular fa-check-circle";
                badgeClass = "bg-success";
                statusText = "Completed"; // Simplified text
                break;
            case CallQueueStatusEnum.Failed:
                iconClass = "fa-regular fa-times-circle";
                badgeClass = "bg-danger";
                statusText = "Failed"; // Simplified text
                break;
            case CallQueueStatusEnum.Cancelled:
                iconClass = "fa-regular fa-ban";
                badgeClass = "bg-secondary";
                statusText = "Cancelled";
                break;
            case CallQueueStatusEnum.Expired:
                iconClass = "fa-regular fa-hourglass-end";
                badgeClass = "bg-light text-muted";
                statusText = "Expired";
                break;
            default: // Handle unknown numeric value
                statusText = `Unknown (${numericValue})`;
                break;
        }
    }
    else if (statusType === 'websession-queue') {
        switch (numericValue) {
            case WebSessionStatusEnum.Queued:
                iconClass = "fa-regular fa-clock"; badgeClass = "bg-warning text-dark"; statusText = "Queued"; break;
            case WebSessionStatusEnum.ProcessingQueue:
            case WebSessionStatusEnum.ProcessingBackend:
                iconClass = "fa-solid fa-spinner fa-spin"; badgeClass = "bg-info"; statusText = "Processing"; break;
            case WebSessionStatusEnum.ProcessedBackend:
                iconClass = "fa-regular fa-check-circle"; badgeClass = "bg-success"; statusText = "Connected"; break;
            case WebSessionStatusEnum.Failed:
                iconClass = "fa-regular fa-times-circle"; badgeClass = "bg-danger"; statusText = "Failed"; break;
            case WebSessionStatusEnum.Canceled:
                iconClass = "fa-regular fa-ban"; badgeClass = "bg-secondary"; statusText = "Canceled"; break;
            case WebSessionStatusEnum.Expired:
                iconClass = "fa-regular fa-hourglass-end"; badgeClass = "bg-light text-muted"; statusText = "Expired"; break;
            default: statusText = `Unknown (${numericValue})`; break;
        }
    }
    else if (statusType === 'session') {
        switch (numericValue) { // Use numericValue here
            case ConversationSessionState.Created:
                iconClass = "fa-regular fa-file-lines";
                badgeClass = "bg-secondary text-muted";
                statusText = "Created";
                break;
            case ConversationSessionState.WaitingForPrimaryClient:
                iconClass = "fa-regular fa-rocket-launch";
                badgeClass = "bg-info";
                statusText = "Waiting for Client";
                break;
            case ConversationSessionState.Starting:
                iconClass = "fa-solid fa-rocket-launch";
                badgeClass = "bg-info";
                statusText = "Starting";
                break;
            case ConversationSessionState.Active:
                iconClass = "fa-solid fa-phone-volume";
                badgeClass = "bg-primary";
                statusText = "Active";
                break;
            case ConversationSessionState.Paused:
                iconClass = "fa-regular fa-pause-circle";
                badgeClass = "bg-warning text-dark";
                statusText = "Paused";
                break;
            case ConversationSessionState.Ending:
                iconClass = "fa-solid fa-flag-checkered";
                badgeClass = "bg-info";
                statusText = "Ending";
                break;
            case ConversationSessionState.Ended:
                iconClass = "fa-regular fa-circle-check";
                badgeClass = "bg-success";
                statusText = `Ended (${sessionEndType.name})`;
                break;
            case ConversationSessionState.Failed:
                iconClass = "fa-regular fa-circle-xmark";
                badgeClass = "bg-danger";
                statusText = "Failed";
                break;
            default: // Handle unknown numeric value
                statusText = `Unknown (${numericValue})`;
                break;
        }
    }

    const badgeContent = includeText ? `<i class="${iconClass} me-1"></i>${statusText}` : `<i class="${iconClass}"></i>`;
    const styleAttribute = includeText ? 'style="text-align: start;"' : '';
    return `<span class="badge ${badgeClass}" title="${statusText}" ${styleAttribute}>${badgeContent}</span>`;
}

function getPostAnalysisStatusBadge(statusEnum) {
    if (!statusEnum) return '';
    const val = statusEnum.value ?? statusEnum;
    switch (val) {
        case 4: return '<span class="badge bg-success ms-1" title="Analysis Complete"><i class="fa-regular fa-brain-circuit"></i></span>';
        case 5: return '<span class="badge bg-danger ms-1" title="Analysis Failed"><i class="fa-regular fa-brain-circuit"></i></span>';
        case 1: case 2: return '<span class="badge bg-warning text-dark ms-1" title="Analysis Pending"><i class="fa-regular fa-brain-circuit"></i></span>';
        case 3: return '<span class="badge bg-secondary ms-1" title="Nothing to Analyze"><i class="fa-regular fa-brain-circuit"></i></span>';
        default: return '';
    }
}

function getOrCreateWaveSurfer(containerId, options) {
    let wavesurfer = null;
    if (containerId === '#waveform-agent-audio') wavesurfer = waveSurferAgent;
    if (containerId === '#waveform-client-audio') wavesurfer = waveSurferClient;

    if (wavesurfer) {
        wavesurfer.empty(); // Clear previous waveform if reusing instance
    } else {
        wavesurfer = WaveSurfer.create({
            container: containerId,
            waveColor: options.waveColor || "#6c757d", // Default grey
            progressColor: options.progressColor || "#0d6efd", // Default blue
            height: 70,
            barWidth: 2,
            barHeight: 0.7,
            fillParent: true,
            // plugins: [ WaveSurfer.Hover.create({ ... }) ] // Add plugins if needed
        });

        // Store the instance
        if (containerId === '#waveform-agent-audio') waveSurferAgent = wavesurfer;
        if (containerId === '#waveform-client-audio') waveSurferClient = wavesurfer;
    }
    return wavesurfer;
}
/** Filters Functions **/
function setupTagInput(inputSelector, tagsContainerSelector) {
    const input = $(inputSelector);
    const tagsContainer = $(tagsContainerSelector);

    input.on('keydown', function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            const value = input.val().trim();
            if (value) {
                const tag = $(`
                    <span class="tag">
                        ${$('<div>').text(value).html()}
                        <span class="tag-close" title="Remove">&times;</span>
                    </span>
                `);
                tagsContainer.append(tag); // Append to the dedicated tags container
                input.val('');
            }
        }
    });

    // Use event delegation on the parent container
    input.closest('.tag-input-container').on('click', '.tag-close', function () {
        $(this).parent('.tag').remove();
    });
}
function getInboundFilterObject() {
    const filter = {};
    const hasFilter = (value) => value !== null && value !== undefined && value !== '';

    const startCreated = $('#inboundFilterStartCreatedDate').val();
    if (hasFilter(startCreated)) filter.StartCreatedDate = new Date(startCreated).toISOString();

    const endCreated = $('#inboundFilterEndCreatedDate').val();
    if (hasFilter(endCreated)) filter.EndCreatedDate = new Date(endCreated).toISOString();

    const startCompleted = $('#inboundFilterStartCompletedAtDate').val();
    if (hasFilter(startCompleted)) filter.StartCompletedAtDate = new Date(startCompleted).toISOString();

    const endCompleted = $('#inboundFilterEndCompletedAtDate').val();
    if (hasFilter(endCompleted)) filter.EndCompletedAtDate = new Date(endCompleted).toISOString();

    const statuses = $('#inboundFilterQueueStatus').next('.dropdown-menu').find('input:checked').map((_, el) => parseInt($(el).val())).get();
    if (statuses.length > 0) filter.QueueStatusTypes = statuses;

    const providers = $('#inboundFilterProvider').next('.dropdown-menu').find('input:checked').map((_, el) => parseInt($(el).val())).get();
    if (providers.length > 0) filter.RouteNumberProviders = providers;

    const getTags = (id) => $(id).find('.tag').map((_, el) => $(el).clone().children().remove().end().text().trim()).get();

    const routeIds = getTags('#inboundFilterRouteIdsTags');
    if (routeIds.length > 0) filter.RouteIds = routeIds;

    const callingNumbers = getTags('#inboundFilterCallingNumbersTags');
    if (callingNumbers.length > 0) filter.CallingNumbers = callingNumbers;

    const routeNumberIds = getTags('#inboundFilterRouteNumberIdsTags');
    if (routeNumberIds.length > 0) filter.RouteNumberIds = routeNumberIds;

    return Object.keys(filter).length > 0 ? filter : null;
}
function getOutboundFilterObject() {
    const filter = {};
    const hasFilter = (value) => value !== null && value !== undefined && value !== '';

    const startCreated = $('#outboundFilterStartCreatedDate').val();
    if (hasFilter(startCreated)) filter.StartCreatedDate = new Date(startCreated).toISOString();

    const endCreated = $('#outboundFilterEndCreatedDate').val();
    if (hasFilter(endCreated)) filter.EndCreatedDate = new Date(endCreated).toISOString();

    const startCompleted = $('#outboundFilterStartCompletedAtDate').val();
    if (hasFilter(startCompleted)) filter.StartCompletedAtDate = new Date(startCompleted).toISOString();

    const endCompleted = $('#outboundFilterEndCompletedAtDate').val();
    if (hasFilter(endCompleted)) filter.EndCompletedAtDate = new Date(endCompleted).toISOString();

    const startScheduled = $('#outboundFilterStartScheduledDate').val();
    if (hasFilter(startScheduled)) filter.StartScheduledDate = new Date(startScheduled).toISOString();

    const endScheduled = $('#outboundFilterEndScheduledDate').val();
    if (hasFilter(endScheduled)) filter.EndScheduledDate = new Date(endScheduled).toISOString();

    const statuses = $('#outboundFilterQueueStatus').next('.dropdown-menu').find('input:checked').map((_, el) => parseInt($(el).val())).get();
    if (statuses.length > 0) filter.QueueStatusTypes = statuses;

    const providers = $('#outboundFilterProvider').next('.dropdown-menu').find('input:checked').map((_, el) => parseInt($(el).val())).get();
    if (providers.length > 0) filter.CallingNumberProviders = providers;

    const getTags = (id) => $(id).find('.tag').map((_, el) => $(el).clone().children().remove().end().text().trim()).get();

    const campaignIds = getTags('#outboundFilterCampaignIdsTags');
    if (campaignIds.length > 0) filter.CampaignIds = campaignIds;

    const callingNumberIds = getTags('#outboundFilterCallingNumberIdsTags');
    if (callingNumberIds.length > 0) filter.CallingNumberIds = callingNumberIds;

    const recipientNumbers = getTags('#outboundFilterRecipientNumbersTags');
    if (recipientNumbers.length > 0) filter.RecipientNumbers = recipientNumbers;

    return Object.keys(filter).length > 0 ? filter : null;
}
function getWebSessionFilterObject() {
    const filter = {};
    const hasFilter = (value) => value !== null && value !== undefined && value !== '';

    const startCreated = $('#websocketFilterStartCreatedDate').val();
    if (hasFilter(startCreated)) filter.StartCreatedDate = new Date(startCreated).toISOString();

    const endCreated = $('#websocketFilterEndCreatedDate').val();
    if (hasFilter(endCreated)) filter.EndCreatedDate = new Date(endCreated).toISOString();

    const statuses = $('#websocketFilterQueueStatus').next('.dropdown-menu').find('input:checked').map((_, el) => parseInt($(el).val())).get();
    if (statuses.length > 0) filter.QueueStatusTypes = statuses;

    const getTags = (id) => $(id).find('.tag').map((_, el) => $(el).clone().children().remove().end().text().trim()).get();

    const clientIdentifiers = getTags('#websocketFilterClientIdentifiersTags');
    if (clientIdentifiers.length > 0) filter.ClientIdentifiers = clientIdentifiers;

    const campaignIds = getTags('#websocketFilterCampaignIdsTags');
    if (campaignIds.length > 0) filter.WebCampaignIds = campaignIds;

    return Object.keys(filter).length > 0 ? filter : null;
}
function updateFilterButtonUI(type, isActive) {
    let filterButton, clearButton;

    if (type === 'inbound') {
        filterButton = filterInboundCallButton; clearButton = clearInboundFiltersButton;
    } else if (type === 'outbound') {
        filterButton = filterOutboundCallButton; clearButton = clearOutboundFiltersButton;
    } else if (type === 'websocket') {
        filterButton = filterWebsocketButton; clearButton = clearWebsocketFiltersButton;
    }

    if (isActive) {
        filterButton.removeClass('btn-primary').addClass('btn-success').removeClass('rounded-2')
        clearButton.removeClass('d-none');
    } else {
        filterButton.removeClass('btn-success').addClass('btn-primary').addClass('rounded-2');
        clearButton.addClass('d-none');
    }
}
function resetFilterForm(formElement) {
    formElement[0].reset(); // Standard form reset
    formElement.find('.tag').remove(); // Clear all tags
    formElement.find('input[type="checkbox"]').prop('checked', false); // Uncheck all boxes
}
/** DOM Manipulation Functions **/
function CreateInboundConversationRow(item) {
    const queueStatusBadge = getStatusBadgeElement('queue', item.status);
    const sessionStatusBadge = getStatusBadgeElement('session', item.sessionStatus, item.sessionEndType);
    const queuedAtFormatted = formatDateTime(item.enqueuedAt);

    let routeDisplay = item.routeId || "N/A";
    if (BusinessFullData && BusinessFullData.businessApp && BusinessFullData.businessApp.routings) {
        const routeData = BusinessFullData.businessApp.routings.find(r => r.id === item.routeId);
        if (routeData) routeDisplay = (routeData.general.emoji || '') + " " + (routeData.general.name || item.routeId);
    }

    let numberDisplay = item.numberId || "N/A";
    if (BusinessFullData && BusinessFullData.businessApp && BusinessFullData.businessApp.numbers) {
        const numberData = BusinessFullData.businessApp.numbers.find(n => n.id === item.numberId);
        if (numberData) numberDisplay = (numberData.countryCode || '') + " " + (numberData.number || item.numberId);
    }

    const hasSession = item.sessionId !== null && item.sessionId !== "";

    const viewButtonData = `
        data-call-type="inbound"
        data-queue-id="${item.queueId}"
        data-queue-status-val="${item.status ? item.status.value : ''}"
        data-enqueued-at="${item.enqueuedAt || ''}"
        data-route-display="${$('<div>').text(routeDisplay).html()}"
        data-caller-number="${item.callerNumber || ''}"
        data-to-number-display="${$('<div>').text(numberDisplay).html()}"
        data-session-id="${item.sessionId || ''}"
        data-logs='${JSON.stringify(item.logs || [])}'
    `;

    const element = $(`
        <tr>
            <td><input type="text" class="form-control form-control-sm copy-on-click" value="${item.queueId}" readonly title="Click to copy"></td>
            <td>${queuedAtFormatted}</td>
            <td class="text-center">${queueStatusBadge}</td>
            <td>${$('<div>').text(routeDisplay).html()}</td>
            <td><b>${$('<div>').text(item.callerNumber).html()}</b></td>
            <td>${$('<div>').text(numberDisplay).html()}</td>
             <td>${hasSession ? `<input type="text" class="form-control form-control-sm copy-on-click" value="${item.sessionId}" readonly title="Click to copy">` : '-'}</td>
            <td class="text-center">${hasSession ? sessionStatusBadge : '-'}${getPostAnalysisStatusBadge(item.postAnalysisStatus)}</td>
            <td>
                <button class="btn btn-info btn-sm view-conversation-button" title="View Details" ${viewButtonData}>
                    <i class="fa-regular fa-eye"></i>
                </button>
                <button class="btn btn-danger btn-sm delete-conversation-button" title="Delete Conversation" data-queue-id="${item.queueId}" disabled>
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `);

    element.find('.copy-on-click').on('click', function () {
        navigator.clipboard.writeText($(this).val()).then(() => {
            AlertManager.createAlert({ type: 'success', message: 'Copied to clipboard!', timeout: 1500 });
        }).catch(err => {
            console.error('Failed to copy:', err);
            AlertManager.createAlert({ type: 'danger', message: 'Failed to copy.', timeout: 2000 });
        });
    });


    return element;
}
function CreateOutboundConversationRow(item) {
    const queueStatusBadge = getStatusBadgeElement('queue', item.status);
    const sessionStatusBadge = getStatusBadgeElement('session', item.sessionStatus, item.sessionEndType);
    const queuedAtFormatted = formatDateTime(item.enqueuedAt);

    let fromNumberDisplay = item.numberId || "N/A";
    if (BusinessFullData && BusinessFullData.businessApp && BusinessFullData.businessApp.numbers) {
        const numberData = BusinessFullData.businessApp.numbers.find(n => n.id === item.numberId);
        if (numberData) fromNumberDisplay = (numberData.countryCode || '') + " " + (numberData.number || item.numberId);
    }

    const hasSession = item.sessionId !== null && item.sessionId !== "";

    const viewButtonData = `
        data-call-type="outbound"
        data-queue-id="${item.queueId}"
        data-queue-status-val="${item.status ? item.status.value : ''}"
        data-enqueued-at="${item.enqueuedAt || ''}"
        data-from-number-display="${$('<div>').text(fromNumberDisplay).html()}"
        data-to-number="${item.recipientNumber || ''}"
        data-session-id="${item.sessionId || ''}"
        data-logs='${JSON.stringify(item.logs || [])}'
    `;

    const element = $(`
        <tr>
            <td><input type="text" class="form-control form-control-sm copy-on-click" value="${item.queueId}" readonly title="Click to copy"></td>
            <td>${queuedAtFormatted}</td>
            <td class="text-center">${queueStatusBadge}</td>
            <td>${$('<div>').text(fromNumberDisplay).html()}</td>
            <td><b>${$('<div>').text(item.recipientNumber).html()}</b></td>
            <td>${hasSession ? `<input type="text" class="form-control form-control-sm copy-on-click" value="${item.sessionId}" readonly title="Click to copy">` : '-'}</td>
            <td class="text-center">${hasSession ? sessionStatusBadge : '-'}${getPostAnalysisStatusBadge(item.postAnalysisStatus)}</td>
            <td>
                <button class="btn btn-info btn-sm view-conversation-button" title="View Details" ${viewButtonData}>
                    <i class="fa-regular fa-eye"></i>
                </button>
                <button class="btn btn-danger btn-sm delete-conversation-button" title="Delete Conversation" data-queue-id="${item.queueId}" disabled>
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `);

    element.find('.copy-on-click').on('click', function () {
        navigator.clipboard.writeText($(this).val()).then(() => {
            AlertManager.createAlert({ type: 'success', message: 'Copied to clipboard!', timeout: 1500 });
        }).catch(err => {
            console.error('Failed to copy:', err);
            AlertManager.createAlert({ type: 'danger', message: 'Failed to copy.', timeout: 2000 });
        });
    });

    return element;
}
function CreateWebSessionConversationRow(item) {
    const queueStatusBadge = getStatusBadgeElement('websession-queue', item.status);
    const sessionStatusBadge = getStatusBadgeElement('session', item.sessionStatus, item.sessionEndType);
    const createdAtFormatted = formatDateTime(item.createdAt);

    const hasSession = item.sessionId !== null && item.sessionId !== "";

    // Data attributes for the "View" button (Manage Tab)
    const viewButtonData = `
        data-call-type="websession"
        data-queue-id="${item.queueId}"
        data-queue-status-val="${item.status}" 
        data-enqueued-at="${item.createdAt || ''}"
        data-client-identifier="${item.clientIdentifier || ''}"
        data-campaign-id="${item.webCampaignId || ''}"
        data-session-id="${item.sessionId || ''}"
    `;

    const element = $(`
        <tr>
            <td><input type="text" class="form-control form-control-sm copy-on-click" value="${item.queueId}" readonly title="Click to copy"></td>
            <td>${createdAtFormatted}</td>
            <td class="text-center">${queueStatusBadge}</td>
            <td><b>${$('<div>').text(item.clientIdentifier).html()}</b></td>
            <td>${$('<div>').text(item.webCampaignId).html()}</td>
            <td>${hasSession ? `<input type="text" class="form-control form-control-sm copy-on-click" value="${item.sessionId}" readonly title="Click to copy">` : '-'}</td>
            <td class="text-center">${hasSession ? sessionStatusBadge : '-'}${getPostAnalysisStatusBadge(item.postAnalysisStatus)}</td>
            <td>
                <button class="btn btn-info btn-sm view-conversation-button" title="View Details" ${viewButtonData}>
                    <i class="fa-regular fa-eye"></i>
                </button>
            </td>
        </tr>
    `);

    // Attach copy-to-clipboard logic
    element.find('.copy-on-click').on('click', function () {
        navigator.clipboard.writeText($(this).val()).then(() => {
            AlertManager.createAlert({ type: 'success', message: 'Copied to clipboard!', timeout: 1500 });
        }).catch(err => { console.error('Failed to copy:', err); });
    });

    return element;
}

function RenderInboundConversationsTable(items) {
    conversationInboundTableBody.empty();

    if (!items || items.length === 0) {
        conversationInboundTableBody.append(`
            <tr>
                <td colspan="9" class="text-center p-4 text-muted">No inbound conversations found.</td> <!-- Adjusted colspan -->
            </tr>
        `);
        return;
    }

    items.forEach(item => {
        const rowElement = CreateInboundConversationRow(item);
        conversationInboundTableBody.append(rowElement);
    });
}
function RenderOutboundConversationsTable(items) {
    conversationOutboundTableBody.empty();
    if (!items || items.length === 0) {
        conversationOutboundTableBody.append(`
            <tr>
                <td colspan="8" class="text-center p-4 text-muted">No outbound conversations found.</td>
            </tr>
        `);
        return;
    }
    items.forEach(item => {
        const rowElement = CreateOutboundConversationRow(item);
        conversationOutboundTableBody.append(rowElement);
    });
}
function RenderWebSessionConversationsTable(items) {
    conversationWebsocketTableBody.empty();
    if (!items || items.length === 0) {
        conversationWebsocketTableBody.append(`
            <tr><td colspan="8" class="text-center p-4 text-muted">No web sessions found.</td></tr>
        `);
        return;
    }
    items.forEach(item => {
        conversationWebsocketTableBody.append(CreateWebSessionConversationRow(item));
    });
}

function ShowTableLoading(isLoading) {
    if (isLoading) {
        conversationInboundTableBody.empty().append(`
            <tr class="loading-row">
                <td colspan="9" class="text-center p-4"> <!-- Adjusted colspan -->
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </td>
            </tr>
        `);
        inboundPaginationControls.addClass('d-none');
    } else {
        conversationInboundTableBody.find('.loading-row').remove();
        inboundPaginationControls.removeClass('d-none');
    }
}
function ShowOutboundTableLoading(isLoading) {
    if (isLoading) {
        conversationOutboundTableBody.empty().append(`
            <tr class="loading-row">
                <td colspan="8" class="text-center p-4">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </td>
            </tr>
        `);
        outboundPaginationControls.addClass('d-none');
    } else {
        conversationOutboundTableBody.find('.loading-row').remove();
        outboundPaginationControls.removeClass('d-none');
    }
}
function ShowWebSessionTableLoading(isLoading) {
    if (isLoading) {
        conversationWebsocketTableBody.empty().append(`
            <tr class="loading-row"><td colspan="8" class="text-center p-4">
                <div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div>
            </td></tr>
        `);
        websocketPaginationControls.addClass('d-none');
    } else {
        conversationWebsocketTableBody.find('.loading-row').remove();
        websocketPaginationControls.removeClass('d-none');
    }
}

function UpdatePaginationButtons(hasNext, hasPrev) {
    inboundNextButton.prop('disabled', !hasNext);
    inboundPrevButton.prop('disabled', !hasPrev);

    // Optional: Update page info display if needed (requires tracking page number)
    // For cursor pagination, page number isn't strictly necessary, but you could estimate it.
    // inboundPageInfo.text(`Page X`); // Update if you implement page tracking
}
function UpdateOutboundPaginationButtons(hasNext, hasPrev) {
    outboundNextButton.prop('disabled', !hasNext);
    outboundPrevButton.prop('disabled', !hasPrev);
    outboundPageInfo.text(`Page ${currentOutboundPageNumber}`);
}
function UpdateWebSessionPaginationButtons(hasNext, hasPrev) {
    websocketNextButton.prop('disabled', !hasNext);
    websocketPrevButton.prop('disabled', !hasPrev);
    websocketPageInfo.text(`Page ${currentWebSessionPageNumber}`);
}

function ClearManageView() {
    CurrentViewStateData = null;
    CurrentViewQueueData = null;
    manageViewLoader.addClass('d-none');
    IsLoadingManageView = false;

    // General Tab
    manageViewQueueId.val('');
    manageViewQueueStatus.html('N/A');
    manageViewEnqueuedAt.val('');
    manageViewFromNumber.val('');
    manageViewToNumber.val('');
    manageViewSessionId.val('');
    manageViewSessionStatus.html('N/A');
    manageViewStartTime.val('');
    manageViewEndTime.val('');
    manageViewRouting.val('');
    manageViewRoutingContainer.removeClass('d-none'); // Show routing by default

    // Metrics Tab
    metricsTabItem.addClass('d-none'); // Hide tab button
    metricsTabPane.addClass('d-none'); // Hide tab content
    metricsTabPane.find('dd').text('-'); // Reset all metric values
    metricAdditionalMetricsContainer.empty(); // Clear additional metrics

    // Conversation Tab
    conversationTurnsContainer.html('<div class="text-center p-3 text-muted">No conversation data found or session not loaded.</div>');
    agentAudioContainer.addClass('d-none');
    clientAudioContainer.addClass('d-none');
    agentAudioError.addClass('d-none').text('');
    clientAudioError.addClass('d-none').text('');
    agentAudioPlayBtn.prop('disabled', true).find('i').removeClass('fa-pause').addClass('fa-play');
    clientAudioPlayBtn.prop('disabled', true).find('i').removeClass('fa-pause').addClass('fa-play');
    agentAudioDownloadBtn.prop('disabled', true).attr('href', '#'); // Reset download link
    clientAudioDownloadBtn.prop('disabled', true).attr('href', '#');
    syncPlayPauseBtn.prop('disabled', true).find('i').removeClass('fa-pause').addClass('fa-play');
    syncPlayPauseBtn.html('<i class="fa-regular fa-play me-1"></i> Play Both');
    syncStopBtn.prop('disabled', true);

    // Audio Turn Sync resets
    sessionStartTimeMs = null;
    currentActiveTurnId = null;
    lastHighlightUpdate = 0;
    isSyncAudioEnabled = true;
    $('#audioTimeDisplay').text('00:00 / 00:00');
    $('#agentVolumeSlider').val(1);
    $('#clientVolumeSlider').val(1);
    $('#syncAudioToggle').prop('checked', true);
    conversationTurnsContainer.find('.turn-active').removeClass('turn-active');

    // Logs Tab
    queueLogsContainer.html('<div class="text-muted">No queue logs found.</div>');
    sessionLogsContainer.html('<div class="text-muted">No session logs found or session not loaded.</div>');
    sessionLogsInfo.text('-');
    sessionLogsPrevBtn.prop('disabled', true);
    sessionLogsNextBtn.prop('disabled', true);
    sessionLogSearchInput.val('');
    sessionLogLevelFilter.val('');

    // Post-Analysis Section
    $('#postAnalysisSection').addClass('d-none');
    $('#postAnalysisSummaryCard').addClass('d-none');
    $('#postAnalysisTagsCard').addClass('d-none');
    $('#postAnalysisFieldsCard').addClass('d-none');
    $('#postAnalysisStatusBadge').empty();
    $('#postAnalysisSummaryText').empty();
    $('#postAnalysisTagsContainer').empty();
    $('#postAnalysisFieldsTableBody').empty();

    // Re-enable tabs that might have been disabled
    //conversationsManageMetricsTab.removeClass('disabled');
    conversationsManageConversationTab.removeClass('disabled');

    // Destroy wavesurfer instances
    if (waveSurferAgent) {
        waveSurferAgent.destroy();
        waveSurferAgent = null;
    }
    if (waveSurferClient) {
        waveSurferClient.destroy();
        waveSurferClient = null;
    }

    // Reset active tab to General
    $('#conversationsManage-general-tab').tab('show');
}

function PopulateManageView(queueData, stateData) {
    const callType = queueData.callType; // 'inbound', 'outbound', or 'websession'

    // Populate General Tab (Part 1: From Queue Data)
    manageViewQueueId.val(queueData.queueId);
    manageViewSessionId.val(queueData.sessionId || 'N/A');

    if (callType === 'websession') {
        manageViewQueueStatus.html(getStatusBadgeElement('websession-queue', parseInt(queueData.queueStatusVal), null, true));
        // Map "Created At" to Enqueued At field for simplicity, or create a new Label in HTML
        manageViewEnqueuedAt.val(formatDateTime(queueData.enqueuedAt)).prev('label').text('Created At');
    } else {
        manageViewQueueStatus.html(getStatusBadgeElement('queue', { value: parseInt(queueData.queueStatusVal) }, null, true));
        manageViewEnqueuedAt.val(formatDateTime(queueData.enqueuedAt)).prev('label').text('Queued At');
    }

    if (callType === 'inbound') {
        manageViewRoutingContainer.removeClass('d-none');
        manageViewFromNumber.closest('.mb-3').removeClass('d-none');
        manageViewToNumber.closest('.mb-3').removeClass('d-none');

        manageViewFromNumber.val(queueData.callerNumber).prev('label').text('From Number');
        manageViewToNumber.val(queueData.toNumberDisplay).prev('label').text('To Number');
        manageViewRouting.val(queueData.routeDisplay);
    }
    else if (callType === 'outbound') {
        manageViewRoutingContainer.addClass('d-none');
        manageViewFromNumber.closest('.mb-3').removeClass('d-none');
        manageViewToNumber.closest('.mb-3').removeClass('d-none');

        manageViewFromNumber.val(queueData.fromNumberDisplay).prev('label').text('From Number');
        manageViewToNumber.val(queueData.toNumber).prev('label').text('To Number');
    }
    else if (callType === 'websession') {
        manageViewRoutingContainer.addClass('d-none');
        // Repurpose From/To fields or Hide them and use specific ones. 
        // For simplicity, let's repurpose "From Number" to "Client Identifier" and "To Number" to "Campaign ID"
        // Alternatively, you can add new HTML fields in Manage Tab for these and toggle d-none.

        manageViewFromNumber.closest('.mb-3').removeClass('d-none');
        manageViewToNumber.closest('.mb-3').removeClass('d-none');

        manageViewFromNumber.val(queueData.clientIdentifier).prev('label').text('Client Identifier');
        manageViewToNumber.val(queueData.campaignId).prev('label').text('Campaign ID');
    }

    CurrentViewQueueData = queueData;
    // Only populate logs when stateData is available to prevent double API fetch
    if (stateData) {
        PopulateLogsTab(queueData, stateData);
    }

    // Assume tabs are enabled unless a reason to disable is found
    //conversationsManageMetricsTab.removeClass('disabled');
    conversationsManageConversationTab.removeClass('disabled');

    // Populate from State Data (if it exists)
    if (stateData) {
        manageViewSessionStatus.html(getStatusBadgeElement('session', stateData.status, null, true));
        manageViewStartTime.val(formatDateTime(stateData.startTime));
        manageViewEndTime.val(stateData.endTime ? formatDateTime(stateData.endTime) : 'N/A');

        PopulateMetricsTab(stateData.metrics);
        RenderConversationTurns(stateData.turns);
        LoadConversationAudio(stateData);

        // Post-Analysis
        if (stateData.postAnalysis) {
            RenderPostAnalysis(stateData.postAnalysis);
        }

        const isSessionEnded = stateData.status && stateData.status.value === ConversationSessionState.Ended;
        if (isSessionEnded) {
            metricsTabItem.removeClass('d-none');
        } else {
            metricsTabItem.addClass('d-none');
        }

        const isSessionFailed = stateData.status && stateData.status.value === ConversationSessionState.Failed;
        if (isSessionFailed) {
            conversationsManageMetricsTab.addClass('disabled').parent().tooltip({ title: 'Metrics are not available for failed sessions.', trigger: 'hover' });
            conversationsManageConversationTab.addClass('disabled').parent().tooltip({ title: 'Conversation is not available for failed sessions.', trigger: 'hover' });
        }

    } else {
        manageViewSessionStatus.html(getStatusBadgeElement('session', null));
        manageViewStartTime.val('N/A');
        manageViewEndTime.val('N/A');
        metricsTabItem.addClass('d-none');
        conversationTurnsContainer.html('<div class="text-center p-3 text-muted">Session data not available.</div>');
        agentAudioContainer.addClass('d-none');
        clientAudioContainer.addClass('d-none');

        conversationsManageMetricsTab.addClass('disabled').parent().tooltip({ title: 'Metrics require a completed session.', trigger: 'hover' });
        conversationsManageConversationTab.addClass('disabled').parent().tooltip({ title: 'Conversation requires an active session.', trigger: 'hover' });

        // Still load queue logs even without session data
        PopulateLogsTab(queueData, null);
    }

    IsLoadingManageView = false;
    manageViewLoader.addClass('d-none');
}

function PopulateMetricsTab(metrics) {
    const defaultValue = '-'; // Default value for empty/null metrics
    if (!metrics) {
        // If no metrics object, reset all fields to default
        metricsTabPane.find('input[type="text"]').val(defaultValue);
        metricAdditionalMetricsContainer.empty();
        return;
    }

    // Use .val() to set input values
    metricDurationSeconds.val(metrics.durationSeconds ? formatDuration(metrics.durationSeconds) : defaultValue);
    metricClientMessageCount.val(metrics.clientMessageCount ?? defaultValue);
    metricAgentMessageCount.val(metrics.agentMessageCount ?? defaultValue);
    metricAverageAgentResponseTimeMs.val(metrics.averageAgentResponseTimeMs ? metrics.averageAgentResponseTimeMs.toFixed(2) : defaultValue);
    metricClientWordCount.val(metrics.clientWordCount ?? defaultValue);
    metricAgentWordCount.val(metrics.agentWordCount ?? defaultValue);
    metricClientInterruptionCount.val(metrics.clientInterruptionCount ?? defaultValue);
    metricAgentInterruptionCount.val(metrics.agentInterruptionCount ?? defaultValue);
    metricSilenceCount.val(metrics.silenceCount ?? defaultValue);
    metricTotalSilenceDurationSeconds.val(metrics.totalSilenceDurationSeconds ? formatDuration(metrics.totalSilenceDurationSeconds) : defaultValue);
    metricSttAverageLatencyMs.val(metrics.sttAverageLatencyMs ? metrics.sttAverageLatencyMs.toFixed(2) : defaultValue);
    metricLlmAverageLatencyMs.val(metrics.llmAverageLatencyMs ? metrics.llmAverageLatencyMs.toFixed(2) : defaultValue);
    metricTtsAverageLatencyMs.val(metrics.ttsAverageLatencyMs ? metrics.ttsAverageLatencyMs.toFixed(2) : defaultValue);

    // Handle additional metrics - populate as label/input pairs
    metricAdditionalMetricsContainer.empty();
    if (metrics.additionalMetrics) {
        for (const key in metrics.additionalMetrics) {
            const uniqueId = `metricAdditional_${key.replace(/[^a-zA-Z0-9]/g, '_')}`; // Create a safe ID
            const label = $(`<label class="form-label" for="${uniqueId}"></label>`).text(key); // Format key if needed
            const input = $(`<input type="text" class="form-control" id="${uniqueId}" readonly>`).val(metrics.additionalMetrics[key].toFixed(2));
            const container = $('<div class="mb-3"></div>').append(label).append(input);
            metricAdditionalMetricsContainer.append(container);
        }
    }
}

function RenderConversationTurns(turns) {
    conversationTurnsContainer.empty();

    if (!turns || turns.length === 0) {
        conversationTurnsContainer.append('<div class="text-center p-3 text-muted">No conversation turns found.</div>');
        return;
    }

    const sortedTurns = [...turns].sort((a, b) => (a.sequence || 0) - (b.sequence || 0));

    sortedTurns.forEach((turn, idx) => {
        const turnType = turn.type?.name || 'Unknown';
        const turnStatus = turn.status?.name || 'Unknown';
        const isClient = turnType === 'User';
        const isInterrupted = turnStatus === 'Interrupted';
        const isSystem = turnType === 'System';
        const isToolResult = turnType === 'ToolResult';
        const alignClass = isClient ? 'text-end' : (isSystem ? 'text-center' : 'text-start');
        const bubbleClass = isClient ? 'user-message' : (isSystem ? 'system-message' : 'ai-message');
        const iconClass = isClient ? 'fa-regular fa-user' : (isSystem ? 'fa-regular fa-gear' : 'fa-regular fa-headset');
        const senderLabel = isClient ? 'Client' : (isSystem ? 'System' : 'Agent');
        const badgeClass = isClient ? 'bg-info' : (isSystem ? 'bg-secondary' : 'bg-success');

        // Status indicator
        let statusIcon = '';
        if (turnStatus === 'Completed') statusIcon = '<span class="badge bg-success ms-1" title="Completed"><i class="fa-regular fa-check"></i></span>';
        else if (isInterrupted) statusIcon = '<span class="badge bg-warning text-dark ms-1" title="Interrupted"><i class="fa-regular fa-bolt"></i></span>';
        else if (turnStatus === 'Error') statusIcon = '<span class="badge bg-danger ms-1" title="Error"><i class="fa-regular fa-xmark"></i></span>';

        let primaryText = '';
        if (turn.userInput?.transcribedText) {
            primaryText = turn.userInput.transcribedText;
        } else if (turn.response?.spokenSegments && turn.response.spokenSegments.length > 0) {
            primaryText = turn.response.spokenSegments.map(s => s.text || '').join(' ');
        } else if (turn.systemInput?.type) {
            primaryText = `[System: ${turn.systemInput.type}]`;
        } else if (isToolResult && turn.toolResultInput?.resultOfTurnId) {
            primaryText = `[Tool Result for Turn ${turn.toolResultInput.resultOfTurnId}]`;
        }

        let detailsHtml = '';

        // F6: TTFT / Latency metadata row for agent responses
        if (!isClient && !isSystem && turn.response) {
            let metaItems = [];
            if (turn.response.llmProcessLatencyFirstTokenMS)
                metaItems.push(`TTFT: ${turn.response.llmProcessLatencyFirstTokenMS}ms`);
            if (turn.response.spokenSegments?.length)
                metaItems.push(`Segments: ${turn.response.spokenSegments.length}`);
            const cacheHits = turn.response.spokenSegments?.filter(s => s.isCacheHit).length || 0;
            if (cacheHits > 0) metaItems.push(`Cache Hits: ${cacheHits}`);
            const interruptedSegs = turn.response.spokenSegments?.filter(s => s.wasInterrupted).length || 0;
            if (interruptedSegs > 0) metaItems.push(`⚡ ${interruptedSegs} interrupted`);
            if (metaItems.length > 0) {
                detailsHtml += `<div class="mt-1 small text-muted opacity-75">${metaItems.join(' · ')}</div>`;
            }
        }

        if (turn.response?.spokenSegments && turn.response.spokenSegments.length > 0) {
            detailsHtml += `<div class="mt-2"><small class="text-muted fw-bold"><i class="fa-regular fa-volume-high me-1"></i>Spoken Segments (${turn.response.spokenSegments.length})</small>`;
            turn.response.spokenSegments.forEach((seg, i) => {
                const interrupted = seg.wasInterrupted ? ' <span class="badge bg-warning text-dark">Interrupted</span>' : '';
                const cached = seg.isCacheHit ? ' <span class="badge bg-secondary">Cached</span>' : '';
                detailsHtml += `<div class="small text-muted ms-2">#${i + 1}: ${$('<div>').text(seg.text || '').html()}${interrupted}${cached}</div>`;
            });
            detailsHtml += '</div>';
        }

        if (turn.response?.toolExecution) {
            const tool = turn.response.toolExecution;
            const toolName = tool.toolName || 'Unknown Tool';
            const success = tool.wasSuccessful ? '<span class="badge bg-success">Success</span>' : '<span class="badge bg-danger">Failed</span>';
            const turnIdx = idx;
            detailsHtml += `<div class="mt-2"><small class="text-muted fw-bold"><i class="fa-regular fa-wrench me-1"></i>Tool Execution</small>`;
            detailsHtml += `<div class="small ms-2 mb-1 p-2 border rounded d-flex justify-content-between align-items-center">`;
            detailsHtml += `<span><strong>${$('<div>').text(toolName).html()}</strong> ${success}</span>`;
            detailsHtml += `<button class="btn btn-outline-secondary btn-sm view-tool-details-btn" data-turn-idx="${turnIdx}"><i class="fa-regular fa-eye me-1"></i>Details</button>`;
            detailsHtml += `</div></div>`;
        }

        if (turn.response?.knowledgeBaseRetrievalData) {
            const kb = turn.response.knowledgeBaseRetrievalData;
            const kbSuccess = kb.wasSuccessfull ? '<span class="badge bg-success">Success</span>' : '<span class="badge bg-danger">Failed</span>';
            const turnIdx = idx;
            detailsHtml += `<div class="mt-2"><small class="text-muted fw-bold"><i class="fa-regular fa-book me-1"></i>KB Retrieval</small>`;
            detailsHtml += `<div class="small ms-2 d-flex justify-content-between align-items-center">`;
            detailsHtml += `<span>${kbSuccess} (${kb.retrievalLatencyMS || 0}ms)`;
            if (kb.resultMessage) detailsHtml += ` — ${$('<div>').text(kb.resultMessage).html()}`;
            detailsHtml += `</span>`;
            detailsHtml += `<button class="btn btn-outline-secondary btn-sm view-kb-details-btn" data-turn-idx="${turnIdx}"><i class="fa-regular fa-eye me-1"></i>Details</button>`;
            detailsHtml += `</div></div>`;
        }

        const turnElement = $(`
            <div class="each-conversation-message mb-3 ${alignClass}" data-turn-id="${turn.id}" data-sequence="${turn.sequence}">
                <div class="this-conversation-container d-inline-block p-3 rounded shadow-sm ${bubbleClass}" style="max-width: 80%; text-align: left;">
                    <div class="message-header small mb-1 d-flex justify-content-between align-items-center">
                        <span>
                            <span class="badge ${badgeClass} me-1"><i class="${iconClass} me-1"></i>${senderLabel}</span>
                            <span class="badge bg-secondary">#${turn.sequence || idx + 1}</span>
                            <span class="badge bg-dark ms-1">${turnType}</span>
                            ${statusIcon}
                        </span>
                        <span class="text-muted small">${formatDateTime(turn.createdAt)}</span>
                    </div>
                    <div class="message-content mt-1">
                        ${primaryText ? $('<div>').text(primaryText).html() : '<span class="text-muted fst-italic">No text content</span>'}
                    </div>
                    ${detailsHtml}
                </div>
            </div>
        `);
        conversationTurnsContainer.append(turnElement);
    });

    // Bind "View Details" buttons for tool execution modals
    conversationTurnsContainer.find('.view-tool-details-btn').on('click', function() {
        const turnIdx = $(this).data('turn-idx');
        const turn = sortedTurns[turnIdx];
        if (turn?.response?.toolExecution) {
            PopulateToolExecutionModal(turn.response.toolExecution);
            new bootstrap.Modal(document.getElementById('toolExecutionDetailModal')).show();
        }
    });

    // Bind "View Details" buttons for KB retrieval modals
    conversationTurnsContainer.find('.view-kb-details-btn').on('click', function() {
        const turnIdx = $(this).data('turn-idx');
        const turn = sortedTurns[turnIdx];
        if (turn?.response?.knowledgeBaseRetrievalData) {
            PopulateKBRetrievalModal(turn.response.knowledgeBaseRetrievalData);
            new bootstrap.Modal(document.getElementById('kbRetrievalDetailModal')).show();
        }
    });
}

function RenderPostAnalysis(postAnalysis) {
    const section = $('#postAnalysisSection');
    const statusBadge = $('#postAnalysisStatusBadge');
    const summaryCard = $('#postAnalysisSummaryCard');
    const tagsCard = $('#postAnalysisTagsCard');
    const fieldsCard = $('#postAnalysisFieldsCard');

    section.removeClass('d-none');

    const statusValue = postAnalysis.status?.value ?? postAnalysis.status;
    const statusName = postAnalysis.status?.name || 'Unknown';

    // Status badge
    let statusBadgeHtml = '';
    switch (statusValue) {
        case 4: // Success
            statusBadgeHtml = '<span class="badge bg-success"><i class="fa-regular fa-check me-1"></i>Analysis Complete</span>';
            break;
        case 5: // Failed
            statusBadgeHtml = '<span class="badge bg-danger"><i class="fa-regular fa-xmark me-1"></i>Analysis Failed</span>';
            section.removeClass('d-none');
            statusBadge.html(statusBadgeHtml);
            return;
        case 1: // Waiting
        case 2: // Processing
            statusBadgeHtml = `<span class="badge bg-warning text-dark"><i class="fa-regular fa-spinner-third fa-spin me-1"></i>${statusName}</span>`;
            section.removeClass('d-none');
            statusBadge.html(statusBadgeHtml);
            return;
        case 3: // NothingToProcess
            statusBadgeHtml = '<span class="badge bg-secondary"><i class="fa-regular fa-minus me-1"></i>Nothing to Process</span>';
            section.removeClass('d-none');
            statusBadge.html(statusBadgeHtml);
            return;
        default:
            return; // Don't show for NotSet
    }
    statusBadge.html(statusBadgeHtml);

    // Summary
    if (postAnalysis.summaryData?.summary?.summary) {
        summaryCard.removeClass('d-none');
        $('#postAnalysisSummaryText').text(postAnalysis.summaryData.summary.summary);
    }

    // Tags — resolve TagId → name via BusinessFullData
    if (postAnalysis.tagsData?.tags && postAnalysis.tagsData.tags.length > 0) {
        tagsCard.removeClass('d-none');
        const tagsContainer = $('#postAnalysisTagsContainer');
        tagsContainer.empty();

        // Try to resolve tag names from BusinessFullData
        let tagDefinitions = null;
        try {
            tagDefinitions = BusinessFullData?.businessApp?.postAnalysis?.tags;
        } catch (e) { /* BusinessFullData may not be available */ }

        function resolveTagName(tagId) {
            if (!tagDefinitions) return tagId;
            const found = tagDefinitions.find(t => t.id === tagId || t.tagId === tagId);
            return found?.name || found?.tagName || tagId;
        }

        function renderTagPills(tags, container, indent = 0) {
            tags.forEach(tag => {
                const tagName = resolveTagName(tag.tagId);
                const thinkingTooltip = tag.thinking ? `title="${$('<div>').text(tag.thinking).html()}"` : '';
                const marginClass = indent > 0 ? `ms-${indent * 2}` : '';
                container.append(`
                    <span class="badge bg-primary me-1 mb-1 ${marginClass}" ${thinkingTooltip} style="cursor: help;">
                        <i class="fa-regular fa-tag me-1"></i>${$('<div>').text(tagName).html()}
                    </span>
                `);
                if (tag.subTags && tag.subTags.length > 0) {
                    renderTagPills(tag.subTags, container, indent + 1);
                }
            });
        }

        renderTagPills(postAnalysis.tagsData.tags, tagsContainer);
    }

    // Extracted Fields
    if (postAnalysis.extractedFieldsData?.extractedFields && postAnalysis.extractedFieldsData.extractedFields.length > 0) {
        fieldsCard.removeClass('d-none');
        const tbody = $('#postAnalysisFieldsTableBody');
        tbody.empty();

        let fieldDefinitions = null;
        try {
            fieldDefinitions = BusinessFullData?.businessApp?.postAnalysis?.extractedFields;
        } catch (e) { /* BusinessFullData may not be available */ }

        function resolveFieldName(fieldId) {
            if (!fieldDefinitions) return fieldId;
            const found = fieldDefinitions.find(f => f.id === fieldId || f.fieldId === fieldId);
            return found?.name || found?.fieldName || fieldId;
        }

        function renderFields(fields, indent = 0) {
            fields.forEach(field => {
                const fieldName = resolveFieldName(field.fieldId);
                const indentPx = indent * 20;
                const value = field.fieldValue !== null && field.fieldValue !== undefined ? String(field.fieldValue) : '-';
                const thinkingTooltip = field.thinking ? `title="${$('<div>').text(field.thinking).html()}"` : '';
                tbody.append(`
                    <tr ${thinkingTooltip} style="cursor: help;">
                        <td style="padding-left: ${indentPx + 12}px;">${$('<div>').text(fieldName).html()}</td>
                        <td>${$('<div>').text(value).html()}</td>
                    </tr>
                `);
                if (field.conditionalExtractedFields && field.conditionalExtractedFields.length > 0) {
                    renderFields(field.conditionalExtractedFields, indent + 1);
                }
            });
        }

        renderFields(postAnalysis.extractedFieldsData.extractedFields);
    }
}

function PopulateToolExecutionModal(toolExecution) {
    $('#toolModalName').text(toolExecution.toolName || 'Unknown Tool');

    // Tool type
    const toolTypeName = toolExecution.toolType?.name || 'Unknown';
    const toolTypeBadge = toolTypeName === 'Custom'
        ? '<span class="badge bg-primary">Custom</span>'
        : '<span class="badge bg-secondary">System</span>';
    $('#toolModalType').html(toolTypeBadge);

    // Status
    const statusBadge = toolExecution.wasSuccessful
        ? '<span class="badge bg-success"><i class="fa-regular fa-check me-1"></i>Success</span>'
        : '<span class="badge bg-danger"><i class="fa-regular fa-xmark me-1"></i>Failed</span>';
    $('#toolModalStatus').html(statusBadge);

    // Node ID
    if (toolExecution.nodeId) {
        $('#toolModalNodeIdContainer').removeClass('d-none');
        $('#toolModalNodeId').text(toolExecution.nodeId);
    } else {
        $('#toolModalNodeIdContainer').addClass('d-none');
    }

    // Reason
    if (toolExecution.reasonForExecution) {
        $('#toolModalReasonContainer').removeClass('d-none');
        $('#toolModalReason').text(toolExecution.reasonForExecution);
    } else {
        $('#toolModalReasonContainer').addClass('d-none');
    }

    // Raw LLM Input — try to parse and pretty-print as JSON
    let rawInput = toolExecution.rawLLMInput || '';
    try {
        rawInput = JSON.stringify(JSON.parse(rawInput), null, 2);
    } catch (e) { /* Not valid JSON, display as-is */ }
    $('#toolModalRawInput').text(rawInput);

    // Result
    if (toolExecution.result) {
        $('#toolModalResultContainer').removeClass('d-none');
        let result = toolExecution.result;
        try {
            result = JSON.stringify(JSON.parse(result), null, 2);
        } catch (e) { /* Not valid JSON */ }
        $('#toolModalResult').text(result);
    } else {
        $('#toolModalResultContainer').addClass('d-none');
    }

    // Completed At
    $('#toolModalCompletedAt').text(toolExecution.completedAt ? formatDateTime(toolExecution.completedAt) : 'N/A');
}

function PopulateKBRetrievalModal(kbData) {
    // Status
    const statusBadge = kbData.wasSuccessfull
        ? '<span class="badge bg-success"><i class="fa-regular fa-check me-1"></i>Success</span>'
        : '<span class="badge bg-danger"><i class="fa-regular fa-xmark me-1"></i>Failed</span>';
    $('#kbModalStatus').html(statusBadge);

    // Latency
    $('#kbModalLatency').text(`${kbData.retrievalLatencyMS || 0}ms`);

    // Result Message
    if (kbData.resultMessage) {
        $('#kbModalResultMessageContainer').removeClass('d-none');
        $('#kbModalResultMessage').text(kbData.resultMessage);
    } else {
        $('#kbModalResultMessageContainer').addClass('d-none');
    }

    // Retrieved Metadata
    if (kbData.retrievedResultsMetaData && kbData.retrievedResultsMetaData.length > 0) {
        $('#kbModalMetadataContainer').removeClass('d-none');
        try {
            $('#kbModalMetadata').text(JSON.stringify(kbData.retrievedResultsMetaData, null, 2));
        } catch (e) {
            $('#kbModalMetadata').text(String(kbData.retrievedResultsMetaData));
        }
    } else {
        $('#kbModalMetadataContainer').addClass('d-none');
    }
}

function PopulateLogDetailModal(logData) {
    const logType = logData.type?.name || logData.level?.name || 'Information';

    // Level badge
    let levelBadge = '<span class="badge bg-info">Info</span>';
    switch (logType) {
        case 'Trace': levelBadge = '<span class="badge bg-secondary">Trace</span>'; break;
        case 'Debug': levelBadge = '<span class="badge bg-primary">Debug</span>'; break;
        case 'Information': levelBadge = '<span class="badge bg-info">Information</span>'; break;
        case 'Warning': levelBadge = '<span class="badge bg-warning text-dark">Warning</span>'; break;
        case 'Error': levelBadge = '<span class="badge bg-danger">Error</span>'; break;
        case 'Critical': levelBadge = '<span class="badge bg-dark">Critical</span>'; break;
    }
    $('#logModalLevel').html(levelBadge);

    // Sender Type
    const senderType = logData.senderType?.name || '-';
    $('#logModalSenderType').text(senderType);

    // Timestamp
    $('#logModalTimestamp').text(formatDateTime(logData.createdAt || logData.timestamp));

    // System Reference
    if (logData.systemSenderReference) {
        $('#logModalSystemRefContainer').removeClass('d-none');
        $('#logModalSystemRef').text(logData.systemSenderReference);
    } else {
        $('#logModalSystemRefContainer').addClass('d-none');
    }

    // Full Message
    $('#logModalMessage').text(logData.message || '');

    // Exception Data
    if (logData.exceptionDataJson) {
        $('#logModalExceptionContainer').removeClass('d-none');
        let exceptionText = logData.exceptionDataJson;
        try {
            exceptionText = JSON.stringify(JSON.parse(exceptionText), null, 2);
        } catch (e) { /* Not valid JSON, display as-is */ }
        $('#logModalException').text(exceptionText);
    } else {
        $('#logModalExceptionContainer').addClass('d-none');
    }
}

// Delegated click handler for expandable log entries
$(document).on('click', '.log-entry-expandable', function() {
    try {
        const logDataStr = decodeURIComponent($(this).data('log-json'));
        const logData = JSON.parse(logDataStr);
        PopulateLogDetailModal(logData);
        new bootstrap.Modal(document.getElementById('logDetailModal')).show();
    } catch (e) {
        console.error('Failed to parse log data for modal:', e);
    }
});

function formatAudioTime(seconds) {
    if (!seconds || isNaN(seconds)) return '00:00';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
}

function initAudioTurnSync(stateData) {
    if (!stateData || !stateData.startTime) return;
    sessionStartTimeMs = new Date(stateData.startTime).getTime();

    // Sort turns for offset lookup
    const turns = stateData.turns || [];
    if (turns.length === 0) return;
    const sortedTurns = [...turns].sort((a, b) => (a.sequence || 0) - (b.sequence || 0));

    // Build turn time ranges for fast lookup
    const turnTimeRanges = sortedTurns.map((turn, idx) => {
        let startMs = null;
        let endMs = null;

        if (turn.userInput?.startedSpeakingAt) {
            startMs = new Date(turn.userInput.startedSpeakingAt).getTime();
            endMs = turn.userInput.finishedSpeakingAt
                ? new Date(turn.userInput.finishedSpeakingAt).getTime()
                : startMs + 5000; // fallback 5s
        } else if (turn.response?.spokenSegments?.length > 0) {
            const segs = turn.response.spokenSegments;
            startMs = new Date(segs[0].startedPlayingAt).getTime();
            const lastSeg = segs[segs.length - 1];
            endMs = lastSeg.finishedPlayingAt
                ? new Date(lastSeg.finishedPlayingAt).getTime()
                : startMs + 5000;
        } else if (turn.createdAt) {
            startMs = new Date(turn.createdAt).getTime();
            endMs = turn.completedAt ? new Date(turn.completedAt).getTime() : startMs + 2000;
        }

        return {
            turnId: turn.id,
            startOffset: startMs ? (startMs - sessionStartTimeMs) / 1000 : null,
            endOffset: endMs ? (endMs - sessionStartTimeMs) / 1000 : null
        };
    }).filter(t => t.startOffset !== null);

    // Register audioprocess callback on agent wavesurfer (throttled to ~4Hz)
    const primaryPlayer = waveSurferAgent || waveSurferClient;
    if (!primaryPlayer) return;

    primaryPlayer.on('audioprocess', (currentTime) => {
        const now = Date.now();
        if (now - lastHighlightUpdate < 250) return; // Throttle to 4Hz
        lastHighlightUpdate = now;

        // Update time display
        const duration = primaryPlayer.getDuration() || 0;
        $('#audioTimeDisplay').text(`${formatAudioTime(currentTime)} / ${formatAudioTime(duration)}`);

        // Find active turn
        let activeTurnId = null;
        for (let i = turnTimeRanges.length - 1; i >= 0; i--) {
            const range = turnTimeRanges[i];
            if (currentTime >= range.startOffset && currentTime <= range.endOffset) {
                activeTurnId = range.turnId;
                break;
            }
        }

        // Fallback: find the most recent turn that started before current time
        if (!activeTurnId) {
            for (let i = turnTimeRanges.length - 1; i >= 0; i--) {
                if (currentTime >= turnTimeRanges[i].startOffset) {
                    activeTurnId = turnTimeRanges[i].turnId;
                    break;
                }
            }
        }

        // Apply highlight if changed
        if (activeTurnId !== currentActiveTurnId) {
            currentActiveTurnId = activeTurnId;
            conversationTurnsContainer.find('.turn-active').removeClass('turn-active');

            if (activeTurnId) {
                const activeEl = conversationTurnsContainer.find(`[data-turn-id="${activeTurnId}"]`);
                activeEl.addClass('turn-active');

                if (isSyncAudioEnabled && activeEl.length) {
                    activeEl[0].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                }
            }
        }
    });

    // Reset highlight on stop/finish
    primaryPlayer.on('finish', () => {
        conversationTurnsContainer.find('.turn-active').removeClass('turn-active');
        currentActiveTurnId = null;
        $('#audioTimeDisplay').text(`${formatAudioTime(primaryPlayer.getDuration())} / ${formatAudioTime(primaryPlayer.getDuration())}`);
    });

    // Click-to-seek on turn bubbles
    conversationTurnsContainer.find('.each-conversation-message').on('click', function (e) {
        // Don't intercept button clicks
        if ($(e.target).closest('button').length) return;

        const turnId = $(this).data('turn-id');
        if (!turnId || !primaryPlayer) return;

        const range = turnTimeRanges.find(t => t.turnId === turnId);
        if (!range || range.startOffset === null) return;

        const duration = primaryPlayer.getDuration();
        if (duration <= 0) return;

        const seekRatio = Math.max(0, Math.min(1, range.startOffset / duration));
        if (waveSurferAgent) waveSurferAgent.seekTo(seekRatio);
        if (waveSurferClient) waveSurferClient.seekTo(seekRatio);
    });
}

function LoadConversationAudio(stateData) {
    // Reset audio states
    agentAudioContainer.addClass('d-none');
    clientAudioContainer.addClass('d-none');
    agentAudioError.addClass('d-none');
    clientAudioError.addClass('d-none');
    // Destroy previous instances if any
    if (waveSurferAgent) { waveSurferAgent.destroy(); waveSurferAgent = null; }
    if (waveSurferClient) { waveSurferClient.destroy(); waveSurferClient = null; }


    // --- Agent Audio ---
    const agentInfo = stateData.agents && stateData.agents.length > 0 ? stateData.agents[0] : null;
    if (agentInfo && agentInfo.audioUrl) {
        agentAudioContainer.removeClass('d-none');
        agentAudioLoader.removeClass('d-none');
        agentAudioPlayBtn.prop('disabled', true);
        agentAudioDownloadBtn.prop('disabled', true).attr('href', '#');
        agentAudioError.addClass('d-none');
        handleAudioUrlSuccess(agentInfo.audioUrl, '#waveform-agent-audio', agentAudioLoader, agentAudioPlayBtn, agentAudioDownloadBtn, agentAudioError);
    } else if (agentInfo) {
        agentAudioContainer.removeClass('d-none');
        agentAudioError.text('Agent audio not yet available. It may still be compiling.').removeClass('d-none');
    }

    // --- Client Audio ---
    const clientInfo = stateData.clients && stateData.clients.length > 0 ? stateData.clients[0] : null;
    if (clientInfo && clientInfo.audioUrl) {
        clientAudioContainer.removeClass('d-none');
        clientAudioLoader.removeClass('d-none');
        clientAudioPlayBtn.prop('disabled', true);
        clientAudioDownloadBtn.prop('disabled', true).attr('href', '#');
        clientAudioError.addClass('d-none');
        handleAudioUrlSuccess(clientInfo.audioUrl, '#waveform-client-audio', clientAudioLoader, clientAudioPlayBtn, clientAudioDownloadBtn, clientAudioError);
    } else if (clientInfo) {
        clientAudioContainer.removeClass('d-none');
        clientAudioError.text('Client audio not yet available. It may still be compiling.').removeClass('d-none');
    }

    // Initialize audio-turn sync after a short delay to allow WaveSurfer to fully load
    setTimeout(() => initAudioTurnSync(stateData), 500);
}

function createLogEntryHtml(log) {
    let iconClass = 'fa-regular fa-circle-info';
    let textClass = 'text-muted';
    const logType = log.type?.name || log.level?.name || 'Information';

    switch (logType) {
        case 'Warning': iconClass = 'fa-regular fa-triangle-exclamation'; textClass = 'text-warning'; break;
        case 'Error': case 'Critical': iconClass = 'fa-regular fa-circle-exclamation'; textClass = 'text-danger'; break;
        case 'Debug': case 'Trace': iconClass = 'fa-regular fa-bug'; textClass = 'text-secondary'; break;
    }
    const timestamp = formatDateTime(log.createdAt || log.timestamp);
    const sender = log.senderType?.name ? `<span class="badge bg-dark me-1">${log.senderType.name}</span>` : '';
    const sysRef = log.systemSenderReference ? `<span class="badge bg-secondary me-1">${$('<div>').text(log.systemSenderReference).html()}</span>` : '';
    // Store log data as JSON for modal expansion
    const logDataAttr = encodeURIComponent(JSON.stringify(log));
    const hasException = log.exceptionDataJson ? true : false;
    const exceptionIndicator = hasException ? ' <span class="badge bg-danger" title="Has exception data"><i class="fa-regular fa-bug"></i></span>' : '';
    return `
        <div class="log-entry d-flex align-items-start mb-2 ${textClass} log-entry-expandable" role="button" data-log-json="${logDataAttr}" style="cursor: pointer;">
            <i class="${iconClass} me-2 mt-1"></i>
            <div class="flex-grow-1">
                <strong class="me-2">[${timestamp}]</strong>
                ${sender}${sysRef}${exceptionIndicator}
                <span>${$('<div>').text(log.message).html()}</span>
            </div>
            <i class="fa-regular fa-chevron-right ms-2 mt-1 opacity-50" title="Click to view details"></i>
        </div>
    `;
}

function PopulateLogsTab(queueData, stateData) {
    // --- Queue / WebSession Logs (fetched from API) ---
    queueLogsContainer.html('<div class="text-muted"><div class="spinner-border spinner-border-sm me-2" role="status"></div>Loading...</div>');

    if (queueData && queueData.queueId) {
        if (queueData.callType === 'websession') {
            FetchWebSessionLogs(queueData.queueId,
                (logs) => {
                    queueLogsContainer.empty();
                    if (logs && logs.length > 0) { logs.forEach(log => queueLogsContainer.append(createLogEntryHtml(log))); }
                    else { queueLogsContainer.html('<div class="text-muted">No queue logs found.</div>'); }
                },
                () => { queueLogsContainer.html('<div class="text-danger">Failed to load queue logs.</div>'); }
            );
        } else {
            FetchQueueLogs(queueData.queueId,
                (logs) => {
                    queueLogsContainer.empty();
                    if (logs && logs.length > 0) { logs.forEach(log => queueLogsContainer.append(createLogEntryHtml(log))); }
                    else { queueLogsContainer.html('<div class="text-muted">No queue logs found.</div>'); }
                },
                () => { queueLogsContainer.html('<div class="text-danger">Failed to load queue logs.</div>'); }
            );
        }
    } else {
        queueLogsContainer.html('<div class="text-muted">No queue data available.</div>');
    }

    // --- Session Logs (paginated) ---
    sessionLogsCurrentPage = 1;
    sessionLogsTotalCount = 0;
    sessionLogSearchInput.val('');
    sessionLogLevelFilter.val('');
    if (stateData && stateData.id) {
        LoadSessionLogsPaginated(stateData.id, 1);
    } else {
        sessionLogsContainer.html('<div class="text-muted">No session data available.</div>');
        sessionLogsInfo.text('-');
        sessionLogsPrevBtn.prop('disabled', true);
        sessionLogsNextBtn.prop('disabled', true);
    }
}

function LoadSessionLogsPaginated(sessionId, page) {
    if (isLoadingSessionLogs) return;
    isLoadingSessionLogs = true;
    sessionLogsContainer.html('<div class="text-muted"><div class="spinner-border spinner-border-sm me-2" role="status"></div>Loading...</div>');

    const search = sessionLogSearchInput.val().trim();
    const levelVal = sessionLogLevelFilter.val();
    const levelFilter = levelVal !== '' ? parseInt(levelVal) : null;

    FetchSessionLogsPaginated(sessionId, page, sessionLogsPageSize, search, levelFilter,
        (data) => {
            isLoadingSessionLogs = false;
            sessionLogsCurrentPage = data.page;
            sessionLogsTotalCount = data.totalCount;

            sessionLogsContainer.empty();
            if (data.logs && data.logs.length > 0) {
                data.logs.forEach(log => sessionLogsContainer.append(createLogEntryHtml(log)));
            } else {
                sessionLogsContainer.html('<div class="text-muted">No session logs found matching criteria.</div>');
            }

            const totalPages = Math.ceil(data.totalCount / data.pageSize) || 1;
            sessionLogsInfo.text(`Page ${data.page} of ${totalPages} (${data.totalCount} total)`);
            sessionLogsPrevBtn.prop('disabled', data.page <= 1);
            sessionLogsNextBtn.prop('disabled', data.page >= totalPages);
        },
        (error) => {
            isLoadingSessionLogs = false;
            sessionLogsContainer.html('<div class="text-danger">Failed to load session logs.</div>');
            console.error("Session logs error:", error);
        }
    );
}

/** Event Handlers **/
function handleAudioUrlSuccess(url, containerId, loader, playBtn, downloadBtn, errorEl) {
    try {
        const wavesurfer = getOrCreateWaveSurfer(containerId, {
            waveColor: containerId === '#waveform-agent-audio' ? "#5f6833" : "#33685f", // Different colors
            progressColor: containerId === '#waveform-agent-audio' ? "#CBE54E" : "#4ecbe5",
        });

        wavesurfer.load(url);

        wavesurfer.on('ready', () => {
            loader.addClass('d-none');
            playBtn.prop('disabled', false);
            downloadBtn.prop('disabled', false).attr('href', url).attr('download', `conversation_${containerId.includes('agent') ? 'agent' : 'client'}_audio.wav`); // Set download link
            console.log(`WaveSurfer ready for ${containerId}`);
            // Enable sync controls when at least one player is ready
            syncPlayPauseBtn.prop('disabled', false);
            syncStopBtn.prop('disabled', false);
        });

        wavesurfer.on('error', (err) => {
            console.error(`WaveSurfer error for ${containerId}:`, err);
            handleAudioUrlError({ message: `Error loading audio: ${err}` }, loader, errorEl);
        });

        wavesurfer.on('play', () => {
            playBtn.find('i').removeClass('fa-play').addClass('fa-pause');
        });

        wavesurfer.on('pause', () => {
            playBtn.find('i').removeClass('fa-pause').addClass('fa-play');
        });
        wavesurfer.on('finish', () => {
            playBtn.find('i').removeClass('fa-pause').addClass('fa-play'); // Reset button when finished
        });

        // Make play button toggle play/pause
        playBtn.off('click').on('click', () => {
            wavesurfer.playPause();
        });

    } catch (e) {
        console.error(`Error initializing WaveSurfer for ${containerId}:`, e);
        handleAudioUrlError({ message: "Error initializing audio player." }, loader, errorEl);
    }
}

function handleAudioUrlError(error, loader, errorEl) {
    loader.addClass('d-none');
    errorEl.text(error.message || "Failed to load audio.").removeClass('d-none');
}

function handleFetchSuccess(data, targetPageNumber) {
    CurrentInboundConversationsData = data.items;
    CurrentInboundNextCursor = data.nextCursor;
    CurrentInboundPrevCursor = data.previousCursor;

    currentInboundPageNumber = targetPageNumber;
    if (!data.hasPreviousPage) {
        currentInboundPageNumber = 1;
    }

    inboundPageInfo.text(`Page ${currentInboundPageNumber}`);

    RenderInboundConversationsTable(CurrentInboundConversationsData);
    ShowTableLoading(false);
    UpdatePaginationButtons(data.hasNextPage, data.hasPreviousPage);

    IsLoadingInboundConversations = false;
}
function handleOutboundFetchSuccess(data) {
    CurrentOutboundConversationsData = data.items;
    CurrentOutboundNextCursor = data.nextCursor;
    CurrentOutboundPrevCursor = data.previousCursor;

    if (!data.hasPreviousPage) {
        currentOutboundPageNumber = 1;
    }

    RenderOutboundConversationsTable(CurrentOutboundConversationsData);
    ShowOutboundTableLoading(false);
    UpdateOutboundPaginationButtons(data.hasNextPage, data.hasPreviousPage);

    IsLoadingOutboundConversations = false;
}

function handleFetchError(error, pageNumberBeforeAttempt) {
    console.error("Error fetching inbound conversations:", error);
    AlertManager.createAlert({ /* ... */ });

    currentInboundPageNumber = pageNumberBeforeAttempt; // Revert page number
    inboundPageInfo.text(`Page ${currentInboundPageNumber}`); // Revert display

    IsLoadingInboundConversations = false;
    ShowTableLoading(false);
    UpdatePaginationButtons(CurrentInboundNextCursor !== null, CurrentInboundPrevCursor !== null); // Revert buttons based on *previous* state

    conversationInboundTableBody.empty().append(`
        <tr>
             <td colspan="9" class="text-center p-4 text-danger">Failed to load conversations.</td> <!-- Adjusted colspan -->
        </tr>
    `);
}
function handleOutboundFetchError(error) {
    console.error("Error fetching outbound conversations:", error);
    IsLoadingOutboundConversations = false;
    ShowOutboundTableLoading(false);
    UpdateOutboundPaginationButtons(CurrentOutboundNextCursor !== null, CurrentOutboundPrevCursor !== null);
    conversationOutboundTableBody.empty().append(`
        <tr>
             <td colspan="8" class="text-center p-4 text-danger">Failed to load conversations.</td>
        </tr>
    `);
}

function LoadInboundConversations(cursor = null, direction = 'next') {
    if (IsLoadingInboundConversations) return;
    IsLoadingInboundConversations = true;
    ShowTableLoading(true);

    let nextC = null;
    let prevC = null;
    let targetPageNumber = currentInboundPageNumber;

    if (direction === 'next') {
        nextC = cursor;
        if (cursor) { targetPageNumber++; }
    } else {
        prevC = cursor;
        if (cursor && targetPageNumber > 1) { targetPageNumber--; }
    }

    // On a fresh load (no cursor) or a filter change, ensure page is 1.
    if (!nextC && !prevC) {
        targetPageNumber = 1;
    }

    inboundPageInfo.text(`Page ${targetPageNumber}`);

    // Construct the request payload
    const requestPayload = {
        limit: InboundConversationsPageSize,
        nextCursor: nextC,
        previousCursor: prevC,
        filter: (nextC || prevC) ? null : CurrentInboundFilters
    };

    FetchInboundConversationsMetaDataFromAPI(
        requestPayload,
        (data) => handleFetchSuccess(data, targetPageNumber),
        (error) => handleFetchError(error, currentInboundPageNumber)
    );
}
function LoadOutboundConversations(cursor = null, direction = 'next') {
    if (IsLoadingOutboundConversations) return;
    IsLoadingOutboundConversations = true;
    ShowOutboundTableLoading(true);

    let nextC = null;
    let prevC = null;

    if (direction === 'next') {
        nextC = cursor;
        if (cursor) {
            currentOutboundPageNumber++;
        }
    } else { // 'prev'
        prevC = cursor;
        if (cursor && currentOutboundPageNumber > 1) {
            currentOutboundPageNumber--;
        }
    }

    // On a fresh load or filter change, reset page number.
    if (!nextC && !prevC) {
        currentOutboundPageNumber = 1;
    }

    outboundPageInfo.text(`Page ${currentOutboundPageNumber}`);

    const requestPayload = {
        limit: OutboundConversationsPageSize,
        nextCursor: nextC,
        previousCursor: prevC,
        filter: (nextC || prevC) ? null : CurrentOutboundFilters
    };

    FetchOutboundConversationsMetaDataFromAPI(
        requestPayload,
        handleOutboundFetchSuccess,
        handleOutboundFetchError
    );
}
function LoadWebSessionConversations(cursor = null, direction = 'next') {
    if (IsLoadingWebSessionConversations) return;
    IsLoadingWebSessionConversations = true;
    ShowWebSessionTableLoading(true);

    let nextC = null;
    let prevC = null;

    if (direction === 'next') {
        nextC = cursor;
        if (cursor) currentWebSessionPageNumber++;
    } else {
        prevC = cursor;
        if (cursor && currentWebSessionPageNumber > 1) currentWebSessionPageNumber--;
    }

    if (!nextC && !prevC) currentWebSessionPageNumber = 1; // Reset on fresh load

    websocketPageInfo.text(`Page ${currentWebSessionPageNumber}`);

    const requestPayload = {
        limit: WebSessionConversationsPageSize,
        nextCursor: nextC,
        previousCursor: prevC,
        filter: (nextC || prevC) ? null : CurrentWebSessionFilters
    };

    FetchWebSessionConversationsMetaDataFromAPI(
        requestPayload,
        (data) => {
            CurrentWebSessionData = data.items;
            CurrentWebSessionNextCursor = data.nextCursor;
            CurrentWebSessionPrevCursor = data.previousCursor;

            if (!data.hasPreviousPage) currentWebSessionPageNumber = 1;

            RenderWebSessionConversationsTable(CurrentWebSessionData);
            ShowWebSessionTableLoading(false);
            UpdateWebSessionPaginationButtons(data.hasNextPage, data.hasPreviousPage);
            IsLoadingWebSessionConversations = false;
        },
        (error) => {
            console.error("Error fetching web sessions:", error);
            IsLoadingWebSessionConversations = false;
            ShowWebSessionTableLoading(false);
            conversationWebsocketTableBody.empty().append(`
                <tr><td colspan="8" class="text-center p-4 text-danger">Failed to load conversations.</td></tr>
            `);
        }
    );
}

function SwitchToManageView(buttonElement) {
    const queueData = buttonElement.data();

    ClearManageView();
    manageViewLoader.removeClass('d-none');
    IsLoadingManageView = true;

    if (queueData.callType === 'inbound') {
        currentConversationTypeName.text("Inbound Call");
        currentConversationName.text(`From ${queueData.callerNumber}`);
    } else if (queueData.callType === 'outbound') {
        currentConversationTypeName.text("Outbound Call");
        currentConversationName.text(`To ${queueData.toNumber}`);
    } else if (queueData.callType === 'websession') {
        currentConversationTypeName.text("Web Session");
        currentConversationName.text(queueData.clientIdentifier || 'Unknown Client');
    }

    PopulateManageView(queueData, null);

    conversationListTab.removeClass("show");
    conversationListTab.one('transitionend', () => {
        conversationListTab.addClass("d-none");
        conversationManageTab.removeClass("d-none");
        setTimeout(() => {
            conversationManageTab.addClass("show");

            if (queueData.sessionId) {
                FetchConversationStateBySessionId(queueData.sessionId,
                    (stateData) => {
                        CurrentViewStateData = stateData;
                        PopulateManageView(queueData, stateData);
                    },
                    (error) => {
                        console.error("Failed to fetch conversation state:", error);
                        AlertManager.createAlert({ type: 'danger', message: `Failed to load conversation details: ${error.message}`, timeout: 6000 });
                        IsLoadingManageView = false;
                        manageViewLoader.addClass('d-none');
                    }
                );
            } else {
                IsLoadingManageView = false;
                manageViewLoader.addClass('d-none');
                console.warn("No session ID found for this queue item.");
            }

        }, 10);
    });
}

function SwitchToListView() {
    conversationManageTab.removeClass("show");
    conversationManageTab.one('transitionend', () => {
        conversationManageTab.addClass("d-none");
        conversationListTab.removeClass("d-none");
        setTimeout(() => {
            conversationListTab.addClass("show");
            ClearManageView(); // Clear data and destroy players when switching back
        }, 10);
    });
}

/** Initialization **/
function initConversationsTab() {
    // Initial Load
    LoadInboundConversations(); // Load first page
    LoadOutboundConversations(); // Load first page
    LoadWebSessionConversations(); // Load first page

    // Setup all tag inputs
    setupTagInput('#inboundFilterRouteIdsInput', '#inboundFilterRouteIdsTags');
    setupTagInput('#inboundFilterCallingNumbersInput', '#inboundFilterCallingNumbersTags');
    setupTagInput('#inboundFilterRouteNumberIdsInput', '#inboundFilterRouteNumberIdsTags');
    setupTagInput('#outboundFilterCampaignIdsInput', '#outboundFilterCampaignIdsTags');
    setupTagInput('#outboundFilterCallingNumberIdsInput', '#outboundFilterCallingNumberIdsTags');
    setupTagInput('#outboundFilterRecipientNumbersInput', '#outboundFilterRecipientNumbersTags');

    // --- Event Listeners ---

    // Inbound Filter Actions
    inboundFilterApplyButton.on('click', function () {
        CurrentInboundFilters = getInboundFilterObject();
        updateFilterButtonUI('inbound', CurrentInboundFilters !== null);

        // Reset pagination and reload data from page 1
        CurrentInboundNextCursor = null;
        CurrentInboundPrevCursor = null;
        LoadInboundConversations();

        $('#inboundFilterCollapse').collapse('hide');
    });

    inboundFilterResetButton.on('click', function () {
        resetFilterForm(inboundFilterForm);
    });

    clearInboundFiltersButton.on('click', function () {
        CurrentInboundFilters = null;
        resetFilterForm(inboundFilterForm);
        updateFilterButtonUI('inbound', false);

        // Reset pagination and reload data from page 1
        CurrentInboundNextCursor = null;
        CurrentInboundPrevCursor = null;
        LoadInboundConversations();
    });

    // Outbound Filter Actions
    outboundFilterApplyButton.on('click', function () {
        CurrentOutboundFilters = getOutboundFilterObject();
        updateFilterButtonUI('outbound', CurrentOutboundFilters !== null);

        // Reset pagination and reload data from page 1
        CurrentOutboundNextCursor = null;
        CurrentOutboundPrevCursor = null;
        LoadOutboundConversations();

        $('#outboundFilterCollapse').collapse('hide');
    });

    outboundFilterResetButton.on('click', function () {
        resetFilterForm(outboundFilterForm);
    });

    clearOutboundFiltersButton.on('click', function () {
        CurrentOutboundFilters = null;
        resetFilterForm(outboundFilterForm);
        updateFilterButtonUI('outbound', false);

        // Reset pagination and reload data from page 1
        CurrentOutboundNextCursor = null;
        CurrentOutboundPrevCursor = null;
        LoadOutboundConversations();
    });

    // Websocket (Web Session) Filter Actions
    websocketFilterApplyButton.on('click', function () {
        CurrentWebSessionFilters = getWebSessionFilterObject();
        updateFilterButtonUI('websocket', CurrentWebSessionFilters !== null); // Need to update this helper too
        CurrentWebSessionNextCursor = null;
        CurrentWebSessionPrevCursor = null;
        LoadWebSessionConversations();
        $('#websocketFilterCollapse').collapse('hide');
    });

    websocketFilterResetButton.on('click', function () {
        resetFilterForm(websocketFilterForm);
    });

    clearWebsocketFiltersButton.on('click', function () {
        CurrentWebSessionFilters = null;
        resetFilterForm(websocketFilterForm);
        updateFilterButtonUI('websocket', false); // Need to update this helper too
        CurrentWebSessionNextCursor = null;
        CurrentWebSessionPrevCursor = null;
        LoadWebSessionConversations();
    });

    searchConversationWebsocketButton.on('click', function () {
        const val = websocketSearchInput.val().trim();
        // Simple search implementation: treat as client identifier filter
        if (val) {
            // Reset form first
            resetFilterForm(websocketFilterForm);
            // Add tag
            const tagContainer = $('#websocketFilterClientIdentifiersTags');
            tagContainer.empty().append(`<span class="tag">${val}<span class="tag-close">&times;</span></span>`);

            // Trigger Apply
            websocketFilterApplyButton.click();
        } else {
            // If empty, clear filters
            clearWebsocketFiltersButton.click();
        }
    });

    // Pagination Buttons
    inboundNextButton.on("click", () => {
        if (!inboundNextButton.prop('disabled')) {
            LoadInboundConversations(CurrentInboundNextCursor, 'next');
        }
    });
    inboundPrevButton.on("click", () => {
        if (!inboundPrevButton.prop('disabled')) {
            LoadInboundConversations(CurrentInboundPrevCursor, 'prev');
        }
    });

    outboundNextButton.on("click", () => {
        if (!outboundNextButton.prop('disabled')) {
            LoadOutboundConversations(CurrentOutboundNextCursor, 'next');
        }
    });
    outboundPrevButton.on("click", () => {
        if (!outboundPrevButton.prop('disabled')) {
            LoadOutboundConversations(CurrentOutboundPrevCursor, 'prev');
        }
    });

    websocketNextButton.on("click", () => {
        if (!websocketNextButton.prop('disabled')) LoadWebSessionConversations(CurrentWebSessionNextCursor, 'next');
    });
    websocketPrevButton.on("click", () => {
        if (!websocketPrevButton.prop('disabled')) LoadWebSessionConversations(CurrentWebSessionPrevCursor, 'prev');
    });

    // View Button Click (using event delegation)
    conversationListTab.on("click", ".view-conversation-button", function (event) {
        event.preventDefault();
        SwitchToManageView($(this));
    });

    // Delete Button Click (Placeholder)
    conversationInboundTableBody.on("click", ".delete-conversation-button", function (event) {
        event.preventDefault();
        const queueId = $(this).data("queue-id");
        AlertManager.createAlert({
            type: "warning",
            message: `Deletion for Queue ID ${queueId} is not yet implemented.`,
            timeout: 3000,
        });
        // TODO: Implement deletion logic with confirmation dialog
    });

    switchBackToConversationsListTabButton.on("click", (event) => {
        event.preventDefault();
        SwitchToListView();
    });

    // Session Logs - Search, Filter, Pagination
    sessionLogSearchBtn.on('click', function () {
        if (CurrentViewStateData && CurrentViewStateData.id) {
            sessionLogsCurrentPage = 1;
            LoadSessionLogsPaginated(CurrentViewStateData.id, 1);
        }
    });
    sessionLogSearchInput.on('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); sessionLogSearchBtn.click(); }
    });
    sessionLogLevelFilter.on('change', function () {
        if (CurrentViewStateData && CurrentViewStateData.id) {
            sessionLogsCurrentPage = 1;
            LoadSessionLogsPaginated(CurrentViewStateData.id, 1);
        }
    });
    sessionLogsPrevBtn.on('click', function () {
        if (CurrentViewStateData && CurrentViewStateData.id && sessionLogsCurrentPage > 1) {
            LoadSessionLogsPaginated(CurrentViewStateData.id, sessionLogsCurrentPage - 1);
        }
    });
    sessionLogsNextBtn.on('click', function () {
        if (CurrentViewStateData && CurrentViewStateData.id) {
            const totalPages = Math.ceil(sessionLogsTotalCount / sessionLogsPageSize) || 1;
            if (sessionLogsCurrentPage < totalPages) {
                LoadSessionLogsPaginated(CurrentViewStateData.id, sessionLogsCurrentPage + 1);
            }
        }
    });

    // Synchronized Audio Controls
    syncPlayPauseBtn.on('click', function () {
        const isPlaying = waveSurferAgent?.isPlaying() || waveSurferClient?.isPlaying();
        if (isPlaying) {
            if (waveSurferAgent) waveSurferAgent.pause();
            if (waveSurferClient) waveSurferClient.pause();
            syncPlayPauseBtn.html('<i class="fa-regular fa-play me-1"></i> Play Both');
        } else {
            if (waveSurferAgent) waveSurferAgent.play();
            if (waveSurferClient) waveSurferClient.play();
            syncPlayPauseBtn.html('<i class="fa-regular fa-pause me-1"></i> Pause Both');
        }
    });
    syncStopBtn.on('click', function () {
        if (waveSurferAgent) { waveSurferAgent.stop(); }
        if (waveSurferClient) { waveSurferClient.stop(); }
        syncPlayPauseBtn.html('<i class="fa-regular fa-play me-1"></i> Play Both');
        // Reset turn highlight on stop
        conversationTurnsContainer.find('.turn-active').removeClass('turn-active');
        currentActiveTurnId = null;
        $('#audioTimeDisplay').text('00:00 / 00:00');
    });

    // Volume sliders
    $('#agentVolumeSlider').on('input', function () {
        if (waveSurferAgent) waveSurferAgent.setVolume(parseFloat($(this).val()));
    });
    $('#clientVolumeSlider').on('input', function () {
        if (waveSurferClient) waveSurferClient.setVolume(parseFloat($(this).val()));
    });

    // Sync toggle
    $('#syncAudioToggle').on('change', function () {
        isSyncAudioEnabled = $(this).is(':checked');
    });
}