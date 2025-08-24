/** Dynamic Variables **/

// Inbound
let CurrentInboundConversationsData = [];
let CurrentInboundNextCursor = null;
let CurrentInboundPrevCursor = null;
let IsLoadingInboundConversations = false;
const InboundConversationsPageSize = 12;
let currentInboundPageNumber = 1;

// Outbound
let CurrentOutboundConversationsData = [];
let CurrentOutboundNextCursor = null;
let CurrentOutboundPrevCursor = null;
let IsLoadingOutboundConversations = false;
const OutboundConversationsPageSize = 12;
let currentOutboundPageNumber = 1;

// Conversation Manager
let CurrentViewStateData = null;
let IsLoadingManageView = false;
let waveSurferAgent = null;
let waveSurferClient = null;

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

// Outbound
const conversationOutboundTable = conversationsTab.find("#conversationOutboundTable");
const conversationOutboundTableBody = conversationOutboundTable.find("tbody");
const outboundPaginationControls = conversationsTab.find("#outboundPaginationControls");
const outboundPrevButton = conversationsTab.find("#outboundPrevButton");
const outboundNextButton = conversationsTab.find("#outboundNextButton");
const outboundPageInfo = conversationsTab.find("#outboundPageInfo");

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

const conversationMessagesContainer = conversationManageTab.find(".conversation-messages");
const messagesPlaceholder = conversationMessagesContainer.find(".messages-placeholder");

// Manage View - Logs Tab Elements
const manageViewRoutingContainer = $("#manageViewRoutingContainer");
const logsTabButton = $("#conversationsManage-logs-tab");
const queueLogsContainer = $("#queueLogsContainer");
const sessionLogsContainer = $("#sessionLogsContainer");

/** API Functions **/
function FetchInboundConversationsMetaDataFromAPI(limit, nextCursor, prevCursor, successCallback, errorCallback) {
    let url = `/app/user/business/${CurrentBusinessId}/conversations/inbound/metadata?limit=${limit}`;
    if (nextCursor) {
        url += `&next=${encodeURIComponent(nextCursor)}`;
    } else if (prevCursor) {
        url += `&prev=${encodeURIComponent(prevCursor)}`;
    }

    $.ajax({
        url: url,
        type: "GET", // Changed to GET as per backend modification
        dataType: "json",
        success: (response) => {
            if (!response || !response.success) {
                errorCallback(response || { message: "Unknown error occurred." });
                return;
            }
            successCallback(response.data); // Pass the PaginatedConversationMetadataResult
        },
        error: (jqXHR, textStatus, errorThrown) => {
            // Try to parse JSON error response from backend if available
            let errorMsg = "An error occurred while fetching inbound conversations.";
            if (jqXHR.responseJSON && jqXHR.responseJSON.message) {
                errorMsg = jqXHR.responseJSON.message;
            } else if (typeof errorThrown === 'string' && errorThrown.length > 0) {
                errorMsg = errorThrown;
            }
            console.error("Fetch Inbound Conversations Error:", jqXHR.responseJSON || textStatus || errorThrown);
            errorCallback({ message: errorMsg, code: jqXHR.status });
        },
    });
}
function FetchOutboundConversationsMetaDataFromAPI(limit, nextCursor, prevCursor, successCallback, errorCallback) {
    let url = `/app/user/business/${CurrentBusinessId}/conversations/outbound/metadata?limit=${limit}`;
    if (nextCursor) {
        url += `&next=${encodeURIComponent(nextCursor)}`;
    } else if (prevCursor) {
        url += `&prev=${encodeURIComponent(prevCursor)}`;
    }

    $.ajax({
        url: url,
        type: "GET", // Changed to GET as per backend modification
        dataType: "json",
        success: (response) => {
            if (!response || !response.success) {
                errorCallback(response || { message: "Unknown error occurred." });
                return;
            }
            successCallback(response.data); // Pass the PaginatedConversationMetadataResult
        },
        error: (jqXHR, textStatus, errorThrown) => {
            // Try to parse JSON error response from backend if available
            let errorMsg = "An error occurred while fetching outbound conversations.";
            if (jqXHR.responseJSON && jqXHR.responseJSON.message) {
                errorMsg = jqXHR.responseJSON.message;
            } else if (typeof errorThrown === 'string' && errorThrown.length > 0) {
                errorMsg = errorThrown;
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
function getStatusBadgeElement(statusType, statusValue, includeText = true) {
    let iconClass = "fa-regular fa-question-circle";
    let badgeClass = "bg-secondary";
    let statusText = "Unknown"; // For tooltip AND display text

    // Handle N/A case first
    if (statusValue === null || statusValue === undefined || statusValue.value === null || statusValue.value === undefined) {
        // Keep N/A simple, no icon needed unless desired
        return `<span class="badge bg-light text-muted" title="Not Applicable">N/A</span>`;
    }

    const numericValue = statusValue.value; // Get the numeric value

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
    } else if (statusType === 'session') {
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
                statusText = "Ended";
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

    // Construct the inner HTML based on includeText flag
    const badgeContent = includeText
        ? `<i class="${iconClass} me-1"></i>${statusText}` // Icon + Text
        : `<i class="${iconClass}"></i>`; // Icon Only

    // Return the full badge HTML
    // Added min-width and text-start for better alignment when text is included
    const styleAttribute = includeText ? 'style="text-align: start;"' : '';
    return `<span class="badge ${badgeClass}" title="${statusText}" ${styleAttribute}>${badgeContent}</span>`;
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

/** DOM Manipulation Functions **/
function CreateInboundConversationRow(item) {
    const queueStatusBadge = getStatusBadgeElement('queue', item.status);
    const sessionStatusBadge = getStatusBadgeElement('session', item.sessionStatus);
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
            <td class="text-center">${hasSession ? sessionStatusBadge : '-'}</td>
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
    const sessionStatusBadge = getStatusBadgeElement('session', item.sessionStatus);
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
            <td class="text-center">${hasSession ? sessionStatusBadge : '-'}</td>
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

function ClearManageView() {
    CurrentViewStateData = null;
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
    conversationMessagesContainer.empty().append(messagesPlaceholder.clone().removeClass('d-none')); // Clear messages
    agentAudioContainer.addClass('d-none');
    clientAudioContainer.addClass('d-none');
    agentAudioError.addClass('d-none').text('');
    clientAudioError.addClass('d-none').text('');
    agentAudioPlayBtn.prop('disabled', true).find('i').removeClass('fa-pause').addClass('fa-play');
    clientAudioPlayBtn.prop('disabled', true).find('i').removeClass('fa-pause').addClass('fa-play');
    agentAudioDownloadBtn.prop('disabled', true).attr('href', '#'); // Reset download link
    clientAudioDownloadBtn.prop('disabled', true).attr('href', '#');

    // Logs Tab
    queueLogsContainer.html('<div class="text-muted">No queue logs found.</div>');
    sessionLogsContainer.html('<div class="text-muted">No session logs found or session not loaded.</div>');

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
    const callType = queueData.callType; // 'inbound' or 'outbound'

    // Populate General Tab (Part 1: From Queue Data)
    manageViewQueueId.val(queueData.queueId);
    manageViewQueueStatus.html(getStatusBadgeElement('queue', { value: parseInt(queueData.queueStatusVal) }, true));
    manageViewEnqueuedAt.val(formatDateTime(queueData.enqueuedAt));
    manageViewSessionId.val(queueData.sessionId || 'N/A');

    if (callType === 'inbound') {
        manageViewRoutingContainer.removeClass('d-none');
        manageViewFromNumber.val(queueData.callerNumber);
        manageViewToNumber.val(queueData.toNumberDisplay);
        manageViewRouting.val(queueData.routeDisplay);
    } else { // outbound
        manageViewRoutingContainer.addClass('d-none');
        manageViewFromNumber.val(queueData.fromNumberDisplay);
        manageViewToNumber.val(queueData.toNumber);
        manageViewRouting.val('N/A');
    }

    const queueLogs = queueData.logs;
    PopulateLogsTab(queueLogs, stateData ? stateData.logs : []);

    // Assume tabs are enabled unless a reason to disable is found
    //conversationsManageMetricsTab.removeClass('disabled');
    conversationsManageConversationTab.removeClass('disabled');

    // Populate from State Data (if it exists)
    if (stateData) {
        manageViewSessionStatus.html(getStatusBadgeElement('session', stateData.status, true));
        manageViewStartTime.val(formatDateTime(stateData.startTime));
        manageViewEndTime.val(stateData.endTime ? formatDateTime(stateData.endTime) : 'N/A');

        PopulateMetricsTab(stateData.metrics);
        RenderConversationMessages(stateData.messages);
        LoadConversationAudio(stateData);

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
        conversationMessagesContainer.empty().append(messagesPlaceholder.clone().removeClass('d-none').text('Session data not available.'));
        agentAudioContainer.addClass('d-none');
        clientAudioContainer.addClass('d-none');

        conversationsManageMetricsTab.addClass('disabled').parent().tooltip({ title: 'Metrics require a completed session.', trigger: 'hover' });
        conversationsManageConversationTab.addClass('disabled').parent().tooltip({ title: 'Conversation requires an active session.', trigger: 'hover' });
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

function RenderConversationMessages(messages) {
    conversationMessagesContainer.empty(); // Clear placeholder or old messages

    if (!messages || messages.length === 0) {
        conversationMessagesContainer.append(messagesPlaceholder.clone().removeClass('d-none').text('No messages in this conversation.'));
        return;
    }

    messages.forEach(msg => {
        let iconClass = 'fa-regular fa-message-question'; // System default
        let messageClass = 'system-message';
        let senderName = 'System';
        let textAlign = 'text-start';

        if (msg.role.value === ConversationSenderRole.Client) {
            iconClass = 'fa-regular fa-user';
            messageClass = 'user-message';
            senderName = 'Client'; // Or get from ClientInfo if available
            textAlign = 'text-end'; // Align user right
        } else if (msg.role.value === ConversationSenderRole.Agent) {
            iconClass = 'fa-regular fa-headset'; // Or fa-microchip-ai
            messageClass = 'ai-message';
            senderName = 'Agent'; // Or get from AgentInfo
            textAlign = 'text-start'; // Align agent left
        }

        // Basic structure - adapt based on your message display preference
        const messageElement = $(`
            <div class="each-conversation-message mb-2 ${textAlign}">
                 <div class="this-conversation-container d-inline-block p-2 rounded shadow-sm ${messageClass}" style="max-width: 75%;">
                    <div class="message-header small text-muted mb-1">
                         <i class="${iconClass} me-1"></i>
                         <span>${senderName}</span>
                         <span class="float-end">${formatDateTime(msg.timestamp)}</span>
                    </div>
                    <div class="message-content">
                        ${$('<div>').text(msg.content).html()} <!-- Basic XSS protection -->
                    </div>
                 </div>
             </div>
        `);
        conversationMessagesContainer.append(messageElement);
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
    if (agentInfo && agentInfo.audioCompilationStatus.value === ConversationMemberAudioCompilationStatus.Compiled) {
        agentAudioContainer.removeClass('d-none');
        agentAudioLoader.removeClass('d-none');
        agentAudioPlayBtn.prop('disabled', true);
        agentAudioDownloadBtn.prop('disabled', true).attr('href', '#');
        agentAudioError.addClass('d-none');

        handleAudioUrlSuccess(stateData.agents[0].audioUrl, '#waveform-agent-audio', agentAudioLoader, agentAudioPlayBtn, agentAudioDownloadBtn, agentAudioError)
        //handleAudioUrlError(error, agentAudioLoader, agentAudioError)
    } else if (agentInfo && agentInfo.audioCompilationStatus.value === ConversationMemberAudioCompilationStatus.Failed) {
        agentAudioContainer.removeClass('d-none');
        agentAudioError.text(`Agent audio compilation failed: ${agentInfo.audioInfo.failedReason || 'Unknown reason'}`).removeClass('d-none');
    }
    else {
        agentAudioContainer.removeClass('d-none');
        agentAudioError.text('Agent audio currently compiling. Please wait a while and check later.').removeClass('d-none');
    }
    // Optionally show a message if status is Waiting or Compiling

    // --- Client Audio ---
    const clientInfo = stateData.clients && stateData.clients.length > 0 ? stateData.clients[0] : null;
    if (clientInfo && clientInfo.audioCompilationStatus.value === ConversationMemberAudioCompilationStatus.Compiled) {
        clientAudioContainer.removeClass('d-none');
        clientAudioLoader.removeClass('d-none');
        clientAudioPlayBtn.prop('disabled', true);
        clientAudioDownloadBtn.prop('disabled', true).attr('href', '#');
        clientAudioError.addClass('d-none');

        handleAudioUrlSuccess(stateData.clients[0].audioUrl, '#waveform-client-audio', clientAudioLoader, clientAudioPlayBtn, clientAudioDownloadBtn, clientAudioError)
        //handleAudioUrlError(error, clientAudioLoader, clientAudioError)
    } else if (clientInfo && clientInfo.audioCompilationStatus.value === ConversationMemberAudioCompilationStatus.Failed) {
        clientAudioContainer.removeClass('d-none');
        clientAudioError.text(`Client audio compilation failed: ${clientInfo.audioInfo.failedReason || 'Unknown reason'}`).removeClass('d-none');
    }
    else {
        clientAudioContainer.removeClass('d-none');
        clientAudioError.text('Client audio currently compiling. Please wait a while and check later.').removeClass('d-none');
    }
}

function PopulateLogsTab(queueLogs, sessionLogs) {
    const createLogEntry = (log) => {
        let iconClass = 'fa-regular fa-circle-info';
        let textClass = 'text-muted';
        const logType = log.type?.name || log.level?.name || 'Information';

        switch (logType) {
            case 'Warning':
                iconClass = 'fa-regular fa-triangle-exclamation';
                textClass = 'text-warning';
                break;
            case 'Error':
                iconClass = 'fa-regular fa-circle-exclamation';
                textClass = 'text-danger';
                break;
        }
        const timestamp = formatDateTime(log.createdAt || log.timestamp);
        return `
            <div class="log-entry d-flex align-items-start mb-2 ${textClass}">
                <i class="${iconClass} me-2 mt-1"></i>
                <div>
                    <strong class="me-2">[${timestamp}]</strong>
                    <span>${$('<div>').text(log.message).html()}</span>
                </div>
            </div>
        `;
    };

    queueLogsContainer.empty();
    if (queueLogs && queueLogs.length > 0) {
        queueLogs.forEach(log => queueLogsContainer.append(createLogEntry(log)));
    } else {
        queueLogsContainer.html('<div class="text-muted">No queue logs found.</div>');
    }

    sessionLogsContainer.empty();
    if (sessionLogs && sessionLogs.length > 0) {
        sessionLogs.forEach(log => sessionLogsContainer.append(createLogEntry(log)));
    } else {
        sessionLogsContainer.html('<div class="text-muted">No session logs found or session not loaded.</div>');
    }
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
    ShowTableLoading(true); // Hides pagination controls

    let nextC = null;
    let prevC = null;
    let targetPageNumber = currentInboundPageNumber; // Keep track of the intended page

    if (direction === 'next') {
        nextC = cursor;
        // Only increment if we are *actually* moving forward
        if (CurrentInboundNextCursor || nextC) { // If a next cursor exists or is provided
            targetPageNumber++;
        } else if (currentInboundPageNumber === 1 && !nextC && !prevC) {
            // Initial load, target is 1
            targetPageNumber = 1;
        }
    } else { // direction === 'prev'
        prevC = cursor;
        // Only decrement if we are *actually* moving backward
        if (targetPageNumber > 1) {
            targetPageNumber--;
        }
    }

    // Update the display optimistically *before* the call
    inboundPageInfo.text(`Page ${targetPageNumber}`);

    FetchInboundConversationsMetaDataFromAPI(
        InboundConversationsPageSize,
        nextC,
        prevC,
        (data) => handleFetchSuccess(data, targetPageNumber), // Pass TARGET page number
        (error) => handleFetchError(error, currentInboundPageNumber) // Pass CURRENT page number in case of error
    );
}
function LoadOutboundConversations(cursor = null, direction = 'next') {
    if (IsLoadingOutboundConversations) return;

    IsLoadingOutboundConversations = true;
    ShowOutboundTableLoading(true);

    if (direction === 'next') {
        if (CurrentOutboundNextCursor || cursor) {
            currentOutboundPageNumber++;
        }
    } else { // 'prev'
        if (currentOutboundPageNumber > 1) {
            currentOutboundPageNumber--;
        }
    }

    FetchOutboundConversationsMetaDataFromAPI(
        OutboundConversationsPageSize,
        direction === 'next' ? cursor : null,
        direction === 'prev' ? cursor : null,
        handleOutboundFetchSuccess,
        handleOutboundFetchError
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
    } else {
        currentConversationTypeName.text("Outbound Call");
        currentConversationName.text(`To ${queueData.toNumber}`);
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

    // --- Event Listeners ---

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
}