/** Global Static Variables **/
const telephonyCampaignPostAnalysisContextVariableArguments = [
    // Call Queue Data
    {
        "id": "call_queue_id",
        "Name": "Call Queue Id",
        "Type": "string",
        "group": "Call Queue Data",
        "Description": "The unique identifier of the call queue entry."
    },
    {
        "id": "call_queue_created_at",
        "Name": "Call Queue Created At",
        "Type": "datetime",
        "group": "Call Queue Data",
        "Description": "Date and time when the call queue entry was first created."
    },
    {
        "id": "call_queue_enqueued_at",
        "Name": "Call Queue Enqueued At",
        "Type": "datetime",
        "group": "Call Queue Data",
        "Description": "Date and time when the call was officially placed in the queue."
    },
    {
        "id": "call_queue_processing_started_at",
        "Name": "Call Queue Processing Started At",
        "Type": "datetime",
        "group": "Call Queue Data",
        "Description": "Date and time when the system started processing the call."
    },
    {
        "id": "call_queue_completed_at",
        "Name": "Call Queue Completed At",
        "Type": "datetime",
        "group": "Call Queue Data",
        "Description": "Date and time when the call was completed."
    },
    {
        "id": "call_queue_status",
        "Name": "Call Queue Status",
        "Type": "string",
        "group": "Call Queue Data",
        "Description": "The current status of the call in the queue (e.g., Queued, Processing, Completed)."
    },
    {
        "id": "call_queue_session_id",
        "Name": "Call Queue Session Id",
        "Type": "string",
        "group": "Call Queue Data",
        "Description": "The session ID associated with the call, if any."
    },
    {
        "id": "call_queue_campaign_id",
        "Name": "Call Queue Campaign Id",
        "Type": "string",
        "group": "Call Queue Data",
        "Description": "The ID of the telephony campaign this call belongs to."
    },
    {
        "id": "call_queue_calling_number_id",
        "Name": "Call Queue Calling Number Id",
        "Type": "string",
        "group": "Call Queue Data",
        "Description": "The ID of the phone number used to make the call."
    },
    {
        "id": "call_queue_calling_number_provider",
        "Name": "Call Queue Calling Number Provider",
        "Type": "string",
        "group": "Call Queue Data",
        "Description": "The telephony provider of the calling number (e.g., Twilio)."
    },
    {
        "id": "call_queue_provider_call_id",
        "Name": "Call Queue Provider Call Id",
        "Type": "string",
        "group": "Call Queue Data",
        "Description": "The unique call identifier from the telephony provider (e.g., Twilio Call SID)."
    },
    {
        "id": "call_queue_recipient_number",
        "Name": "Call Queue Recipient Number",
        "Type": "string",
        "group": "Call Queue Data",
        "Description": "The phone number of the person being called."
    },
    {
        "id": "call_queue_scheduled_for_date_time",
        "Name": "Call Queue Scheduled For",
        "Type": "datetime",
        "group": "Call Queue Data",
        "Description": "The date and time the call is scheduled to be made."
    },
    {
        "id": "call_queue_dynamic_variables",
        "Name": "Call Queue Dynamic Variables",
        "Type": "object",
        "group": "Call Queue Data",
        "Description": "Dynamic variables associated with the call (key-value pairs)."
    },
    {
        "id": "call_queue_metadata",
        "Name": "Call Queue Metadata",
        "Type": "object",
        "group": "Call Queue Data",
        "Description": "Metadata associated with the call (key-value pairs)."
    },  
    // Conversation Data
    {
        "id": "conversation_id",
        "Name": "Conversation Id",
        "Type": "string",
        "group": "Conversation Data",
        "Description": "Id of the conversation"
    },
    {
        "id": "conversation_start_time",
        "Name": "Conversation Start Time",
        "Type": "datetime",
        "group": "Conversation Data",
        "Description": "Date and time when the conversation was started"
    },
    {
        "id": "conversation_end_type",
        "Name": "Conversation End Type",
        "Type": "string",
        "group": "Conversation Data",
        "Description": "Type the conversation was ended with"
    },
    {
        "id": "conversation_end_time",
        "Name": "Conversation End Time",
        "Type": "datetime",
        "group": "Conversation Data",
        "Description": "Date and time when the conversation was ended"
    },
    {
        "id": "conversation_turns",
        "Name": "Conversation Turns",
        "Type": "object",
        "group": "Conversation Data",
        "Description": "Complete System/Agent/User turns data of the conversation"
    },
    {
        "id": "conversation_turns_simplified",
        "Name": "Conversation Turns Simplified",
        "Type": "string",
        "group": "Conversation Data",
        "Description": "Simplified & already compiled `<role>: <content>` string of Conversations Turns"
    }
];

/** Dynamic Variables **/
let manageTelephonyCampaignType = null; // 'new' or 'edit'
let currentTelephonyCampaignData = null;

let currentTelephonyCampaignRouteNumberList = {};
let currentTelephonyCampaignDefaultNumberId = "";
let currentTelephonyCampaignAgentSelectedId = "";

let isSavingTelephonyCampaign = false;

var telephonyCampaignPostAnalysisContextVariablesCustomInput = {};

// Integration Managers for Voicemail Detection
let telephonyCampaignVoicemailSTTIntegrationManager = null;
let telephonyCampaignVoicemailLLMIntegrationManager = null;
let currentTelephonyCampaignVoicemailMessageToLeaveMultiLangData = {};

const telephonyCampaignsTooltipTriggerList = document.querySelectorAll('#telephony-campaigns-tab [data-bs-toggle="tooltip"]');
[...telephonyCampaignsTooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

/** Element Variables **/
const telephonyCampaignsTab = $("#telephony-campaigns-tab");

// Header Elements
const telephonyCampaignHeaderContainer = telephonyCampaignsTab.find("#telephony-campaign-header-container");
// Manager Header
const telephonyCampaignManagerNameBreadcrumb = telephonyCampaignHeaderContainer.find("#telephony-campaign-manager-name-breadcrumb");
const backToTelephonyCampaignsListButton = telephonyCampaignHeaderContainer.find("#back-to-telephony-campaigns-list");
const saveTelephonyCampaignButton = telephonyCampaignHeaderContainer.find("#save-telephony-campaign-button");

// List View Elements
const telephonyCampaignsListView = telephonyCampaignsTab.find("#telephony-campaigns-list-view");
const addNewTelephonyCampaignButton = telephonyCampaignsListView.find("#add-new-telephony-campaign-button");
const telephonyCampaignsListContainer = telephonyCampaignsListView.find("#telephony-campaigns-list-container");

// Manager View Elements
const telephonyCampaignsManagerView = telephonyCampaignsTab.find("#telephony-campaigns-manager-view");

// General Tab
const telephonyCampaignIconInput = telephonyCampaignsManagerView.find("#telephony-campaign-icon-input");
const telephonyCampaignNameInput = telephonyCampaignsManagerView.find("#telephony-campaign-name-input");
const telephonyCampaignDescriptionInput = telephonyCampaignsManagerView.find("#telephony-campaign-description-input");

// Agent Tab
const telephonyCampaignAgentIconSpan = telephonyCampaignsManagerView.find("#telephony-campaign-agent-icon-span");
const telephonyCampaignAgentNameInput = telephonyCampaignsManagerView.find("#telephony-campaign-agent-name-input");
const telephonyCampaignAgentScriptSelect = telephonyCampaignsManagerView.find("#telephony-campaign-agent-script-select");
const telephonyCampaignAgentLanguageSelect = telephonyCampaignsManagerView.find("#telephony-campaign-agent-language-select");
const telephonyCampaignAgentTimezoneSelect = telephonyCampaignsManagerView.find("#telephony-campaign-agent-timezone-select");
const telephonyCampaignAgentFromNumberInContextCheck = telephonyCampaignsManagerView.find("#telephony-campaign-agent-from-number-in-context-check");
const telephonyCampaignAgentToNumberInContextCheck = telephonyCampaignsManagerView.find("#telephony-campaign-agent-to-number-in-context-check");

// Numbers Tab
const addTelephonyCampaignRegionRouteButton = telephonyCampaignsManagerView.find("#telephony-campaign-add-region-route-button");
const telephonyCampaignNumbersListTable = telephonyCampaignsManagerView.find("#telephony-campaign-numbers-list-table");

// Configuration Tab
const telephonyCampaignRetryOnDeclineCheck = telephonyCampaignsManagerView.find("#telephony-campaign-retry-on-decline-check");
const telephonyCampaignRetryOnDeclineOptions = telephonyCampaignsManagerView.find("#telephony-campaign-retry-on-decline-options");
const telephonyCampaignRetryDeclineCountInput = telephonyCampaignsManagerView.find("#telephony-campaign-retry-decline-count-input");
const telephonyCampaignRetryDeclineDelayInput = telephonyCampaignsManagerView.find("#telephony-campaign-retry-decline-delay-input");
const telephonyCampaignRetryDeclineUnitSelect = telephonyCampaignsManagerView.find("#telephony-campaign-retry-decline-unit-select");
const telephonyCampaignRetryOnMissCheck = telephonyCampaignsManagerView.find("#telephony-campaign-retry-on-miss-check");
const telephonyCampaignRetryOnMissOptions = telephonyCampaignsManagerView.find("#telephony-campaign-retry-on-miss-options");
const telephonyCampaignRetryMissCountInput = telephonyCampaignsManagerView.find("#telephony-campaign-retry-miss-count-input");
const telephonyCampaignRetryMissDelayInput = telephonyCampaignsManagerView.find("#telephony-campaign-retry-miss-delay-input");
const telephonyCampaignRetryMissUnitSelect = telephonyCampaignsManagerView.find("#telephony-campaign-retry-miss-unit-select");
const telephonyCampaignPickupDelayInput = telephonyCampaignsManagerView.find("#telephony-campaign-pickup-delay-input");
const telephonyCampaignSilenceNotifyInput = telephonyCampaignsManagerView.find("#telephony-campaign-silence-notify-input");
const telephonyCampaignSilenceEndInput = telephonyCampaignsManagerView.find("#telephony-campaign-silence-end-input");
const telephonyCampaignMaxCallTimeInput = telephonyCampaignsManagerView.find("#telephony-campaign-max-call-time-input");

// Voicemail Tab
const telephonyCampaignVoicemailIsEnabledCheck = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-is-enabled-check");
const telephonyCampaignVoicemailSettingsContainer = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-settings-container");
const telephonyCampaignVoicemailInitialCheckDelayMSInput = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-initial-check-delay-ms-input");
const telephonyCampaignVoicemailVADSilenceThresholdMSInput = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-vad-silence-threshold-ms-input");
const telephonyCampaignVoicemailVADMaxSpeechDurationMSInput = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-vad-max-speech-duration-ms-input");
const telephonyCampaignVoicemailAdvancedVerificationCheck = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-advanced-verification-check");
const telephonyCampaignVoicemailAdvancedVerificationContainer = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-advanced-verification-container");
const telephonyCampaignStopAgentOnMLCheck = telephonyCampaignsManagerView.find("#telephony-campaign-stop-agent-on-ml-check");
const telephonyCampaignStopAgentOnVADCheck = telephonyCampaignsManagerView.find("#telephony-campaign-stop-agent-on-vad-check");
const telephonyCampaignStopAgentOnLLMCheck = telephonyCampaignsManagerView.find("#telephony-campaign-stop-agent-on-llm-check");
const telephonyCampaignVoicemailStopSpeakingDelayInput = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-stop-speaking-delay-input");
const telephonyCampaignEndLeaveOnMLCheck = telephonyCampaignsManagerView.find("#telephony-campaign-end-leave-on-ml-check");
const telephonyCampaignEndLeaveOnVADCheck = telephonyCampaignsManagerView.find("#telephony-campaign-end-leave-on-vad-check");
const telephonyCampaignEndLeaveOnLLMCheck = telephonyCampaignsManagerView.find("#telephony-campaign-end-leave-on-llm-check");
const telephonyCampaignVoicemailEndLeaveDelayInput = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-end-leave-delay-input");
const telephonyCampaignFinalActionRadios = telephonyCampaignsManagerView.find('input[name="telephony-campaign-final-action-radio"]');
const telephonyCampaignVoicemailLeaveMessageContainer = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-leave-message-container");
const telephonyCampaignVoicemailMessageToLeaveTextarea = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-message-to-leave-textarea");

// Variables Tab
const addTelephonyCampaignDynamicVariable = telephonyCampaignsManagerView.find("#addTelephonyCampaignDynamicVariable");
const telephonyCampaignDynamicVariablesList = telephonyCampaignsManagerView.find("#telephonyCampaignDynamicVariablesList");
const addTelephonyCampaignMetadata = telephonyCampaignsManagerView.find("#addTelephonyCampaignMetadata");
const telephonyCampaignMetadataList = telephonyCampaignsManagerView.find("#telephonyCampaignMetadataList");

// Post Analysis Tab
const telephonyCampaignPostAnalysisTemplateSelect = telephonyCampaignsManagerView.find("#telephonyCampaignPostAnalysisTemplateSelect");
const addTelephonyCampaignPostAnalysisVariable = telephonyCampaignsManagerView.find("#addTelephonyCampaignPostAnalysisVariable");
const telephonyCampaignPostAnalysisVariablesList = telephonyCampaignsManagerView.find("#telephonyCampaignPostAnalysisVariablesList");

// Actions Tab
const telephonyCampaignActionsTab = telephonyCampaignsManagerView.find("#telephony-campaign-manager-actions");
const telephonyCampaignActionToolCallInitiationFailureSelect = telephonyCampaignActionsTab.find("#telephony-campaign-action-tool-call-initiation-failure-select");
const telephonyCampaignActionToolCallInitiatedSelect = telephonyCampaignActionsTab.find("#telephony-campaign-action-tool-call-initiated-select");
const telephonyCampaignActionToolCallDeclinedSelect = telephonyCampaignActionsTab.find("#telephony-campaign-action-tool-call-declined-select");
const telephonyCampaignActionToolCallMissedSelect = telephonyCampaignActionsTab.find("#telephony-campaign-action-tool-call-missed-select");
const telephonyCampaignActionToolCallAnsweredSelect = telephonyCampaignActionsTab.find("#telephony-campaign-action-tool-call-answered-select");
const telephonyCampaignActionToolCallEndedSelect = telephonyCampaignActionsTab.find("#telephony-campaign-action-tool-call-ended-select");

// Modals
const telephonyCampaignSelectAgentModalElement = $("#telephony-campaign-select-agent-modal");
let telephonyCampaignSelectAgentModal = null;
const telephonyCampaignsManagerSelectAgentModalList = telephonyCampaignSelectAgentModalElement.find(".modal-body");
const telephonyCampaignSaveAgentButton = telephonyCampaignSelectAgentModalElement.find("#telephony-campaign-save-agent-button");

const telephonyCampaignChangeNumberModalElement = $("#telephony-campaign-change-number-modal");
let telephonyCampaignChangeNumberModal = null;

const telephonyCampaignAddRegionModalElement = $("#telephony-campaign-add-region-modal");
let telephonyCampaignAddRegionModal = null;

/** API FUNCTIONS **/
function saveTelephonyCampaign(formData, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/campaign/telephony/save`,
        type: "POST",
        data: formData,
        processData: false,
        contentType: false,
        success: (response) => {
            if (response.success) {
                successCallback(response);
            } else {
                errorCallback(response, true);
            }
        },
        error: (xhr, status, error) => {
            errorCallback(error, false);
        },
    });
}

/** FUNCTIONS **/
function showTelephonyCampaignsListView() {
    telephonyCampaignsManagerView.removeClass("show");
    telephonyCampaignHeaderContainer.removeClass("show");
    setTimeout(() => {
        telephonyCampaignsManagerView.addClass("d-none");
        telephonyCampaignHeaderContainer.addClass("d-none");

        telephonyCampaignsListView.removeClass("d-none");
        setTimeout(() => {
            telephonyCampaignsListView.addClass("show");
            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function showTelephonyCampaignsManagerView() {
    telephonyCampaignsListView.removeClass("show");
    setTimeout(() => {
        telephonyCampaignsListView.addClass("d-none");

        telephonyCampaignHeaderContainer.removeClass("d-none");
        telephonyCampaignsManagerView.removeClass("d-none");
        setTimeout(() => {
            telephonyCampaignHeaderContainer.addClass("show");
            telephonyCampaignsManagerView.addClass("show");
            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function createTelephonyCampaignListElement(campaignData) {
    const agentData = BusinessFullData.businessApp.agents.find((agent) => agent.id === campaignData.agent.selectedAgentId);
    const agentName = agentData ? `Agent: ${agentData.general.emoji} ${agentData.general.name[BusinessDefaultLanguage]}` : 'No Agent Assigned';

    return `
        <div class="col-lg-4 col-md-6 col-12">
            <div class="campaign-card telephony-campaign-card d-flex flex-column align-items-start justify-content-center" data-campaign-id="${campaignData.id}">
                <div class="d-flex flex-row align-items-center justify-content-start mb-4">
                    <span class="route-icon">${campaignData.general.emoji}</span>
                    <div class="card-data">
                        <h4>${campaignData.general.name}</h4>
                        <h6>${agentName}</h6>
                    </div>
                </div>
                <div><h5 class="h5-info agent-description"><span>${campaignData.general.description}</span></h5></div>
            </div>
        </div>
    `;
}

function fillTelephonyCampaignsList() {
    telephonyCampaignsListContainer.empty();
    const telephonyCampaigns = BusinessFullData.businessApp.telephonyCampaigns;
    if (!telephonyCampaigns || telephonyCampaigns.length === 0) {
        telephonyCampaignsListContainer.append('<div class="col-12"><h6 class="text-center mt-5">No telephony campaigns created yet...</h6></div>');
    } else {
        telephonyCampaigns.forEach(campaign => {
            telephonyCampaignsListContainer.append($(createTelephonyCampaignListElement(campaign)));
        });
    }
}

function createDefaultTelephonyCampaignObject() {
    return {
        general: {
            emoji: "📞",
            name: "",
            description: ""
        },
        agent: {
            selectedAgentId: "",
            openingScriptId: "",
            language: "",
            timezones: [],
            fromNumberInContext: true,
            toNumberInContext: true,
        },
        numberRoute: {
            routeNumberList: {},
            defaultNumberId: ""
        },
        configuration: {
            retryOnDecline: {
                enabled: false
            },
            retryOnMiss: {
                enabled: false
            },
            timeouts: {
                pickupDelayMS: 0,
                notifyOnSilenceMS: 10000,
                endOnSilenceMS: 30000,
                maxCallTimeS: 600
            }
        },
        voicemailDetection: {
            isEnabled: false,
            initialCheckDelayMS: 1000,
            waitForVADSpeechForMLCheck: true,
            voiceMailMessageVADSilenceThresholdMS: 1000,
            voiceMailMessageVADMaxSpeechDurationMS: 4000,
            onVoiceMailMessageDetectVerifySTTAndLLM: false,
            transcribeVoiceMessageSTT: null,
            verifyVoiceMessageLLM: null,
            stopSpeakingAgentAfterMlCheckSuccess: true,
            stopSpeakingAgentAfterVadSilence: false,
            stopSpeakingAgentAfterLLMConfirm: false,
            stopSpeakingAgentDelayAfterMatchMS: 1000,
            endOrLeaveMessageAfterMLCheckSuccess: true,
            endOrLeaveMessageAfterVadSilence: false,
            endOrLeaveMessageAfterLLMConfirm: false,
            endOrLeaveMessageDelayAfterMatchMS: 1000,
            endCallOnDetect: true,
            leaveMessageOnDetect: false,
            messageToLeave: {}
        },
        variables: {
            dynamicVariables: [],
            metadata: []
        },
        postAnalysis: {
            postAnalysisId: null,
            contextVariables: null
        },
        actions: {
            callInitiationFailureTool: {
                toolId: null,
                arguments: null
            },
            callInitiatedTool: {
                toolId: null,
                arguments: null
            },
            callDeclinedTool: {
                toolId: null,
                arguments: null
            },
            callMissedTool: {
                toolId: null,
                arguments: null
            },
            callAnsweredTool: {
                toolId: null,
                arguments: null
            },
            callEndedTool: {
                toolId: null,
                arguments: null
            }
        }
    };
}

function resetTelephonyCampaignManager() {
    telephonyCampaignsManagerView.find(".is-invalid").removeClass("is-invalid");
    telephonyCampaignsManagerView.find('.border-danger').removeClass('border-danger');

    // General
    telephonyCampaignIconInput.text("📞");
    telephonyCampaignNameInput.val("");
    telephonyCampaignDescriptionInput.val("");

    // Agent
    telephonyCampaignAgentIconSpan.text("-");
    telephonyCampaignAgentNameInput.val("");
    telephonyCampaignAgentScriptSelect.empty().append('<option value="" disabled selected>Select Agent First</option>').prop("disabled", true);
    telephonyCampaignAgentLanguageSelect.empty().append('<option value="" disabled selected>Select Language</option>');
    BusinessFullData.businessData.languages.forEach(lang => {
        const langData = SpecificationLanguagesListData.find(l => l.id === lang);
        telephonyCampaignAgentLanguageSelect.append(`<option value="${lang}">${lang} | ${langData.name}</option>`);
    });
    telephonyCampaignAgentTimezoneSelect.val(""); // Add timezone options if not static
    telephonyCampaignAgentFromNumberInContextCheck.prop("checked", true);
    telephonyCampaignAgentToNumberInContextCheck.prop("checked", true);

    // Numbers
    updateTelephonyDefaultNumberRowUI(null);
    telephonyCampaignNumbersListTable.find("tbody tr:not([data-region-code='default'])").remove();
    currentTelephonyCampaignRouteNumberList = {};
    currentTelephonyCampaignDefaultNumberId = "";

    // Configuration
    telephonyCampaignRetryOnDeclineCheck.prop("checked", false).change();
    telephonyCampaignRetryDeclineCountInput.val(3);
    telephonyCampaignRetryDeclineDelayInput.val(10);
    telephonyCampaignRetryDeclineUnitSelect.val(1);

    telephonyCampaignRetryOnMissCheck.prop("checked", false).change();
    telephonyCampaignRetryMissCountInput.val(3);
    telephonyCampaignRetryMissDelayInput.val(10);
    telephonyCampaignRetryMissUnitSelect.val(1);

    telephonyCampaignPickupDelayInput.val(0);
    telephonyCampaignSilenceNotifyInput.val(10000);
    telephonyCampaignSilenceEndInput.val(30000);
    telephonyCampaignMaxCallTimeInput.val(600);  

    // Voicemail
    telephonyCampaignVoicemailIsEnabledCheck.prop('checked', false).change();
    telephonyCampaignVoicemailAdvancedVerificationCheck.prop('checked', false).change();
    currentTelephonyCampaignVoicemailMessageToLeaveMultiLangData = {};
    if (telephonyCampaignVoicemailSTTIntegrationManager) telephonyCampaignVoicemailSTTIntegrationManager.reset();
    if (telephonyCampaignVoicemailLLMIntegrationManager) telephonyCampaignVoicemailLLMIntegrationManager.reset();

    // Variables Tab
    telephonyCampaignDynamicVariablesList.empty();
    telephonyCampaignMetadataList.empty();

    // Post Analysis Tab
    telephonyCampaignPostAnalysisTemplateSelect.empty();
    telephonyCampaignPostAnalysisTemplateSelect.append('<option value="" selected>No Post Analysis</option>');
    BusinessFullData.businessApp.postAnalysis.forEach((template) => {
        telephonyCampaignPostAnalysisTemplateSelect.append(`<option value="${template.id}">${template.general.name}</option>`);
    });
    addTelephonyCampaignPostAnalysisVariable.prop("disabled", true);
    telephonyCampaignPostAnalysisVariablesList.empty();
    Object.keys(telephonyCampaignPostAnalysisContextVariablesCustomInput).forEach((customInputId) => {
        telephonyCampaignPostAnalysisContextVariablesCustomInput[customInputId].destroy();
    });
    telephonyCampaignPostAnalysisContextVariablesCustomInput = {};

    // Actions
    const actionSelects = [
        telephonyCampaignActionToolCallInitiationFailureSelect,
        telephonyCampaignActionToolCallInitiatedSelect,
        telephonyCampaignActionToolCallMissedSelect,
        telephonyCampaignActionToolCallDeclinedSelect,
        telephonyCampaignActionToolCallAnsweredSelect,
        telephonyCampaignActionToolCallEndedSelect
    ];
    actionSelects.forEach(select => {
        select.empty().append('<option value="none" selected>None</option>');
        BusinessFullData.businessApp.tools.forEach(tool => {
            select.append(`<option value="${tool.id}">${tool.general.name[BusinessDefaultLanguage]}</option>`);
        });
        const container = select.closest('div');
        container.find('.custom-tool-input-arguments').addClass('d-none');
        container.find('[id$="-arguments-list"]').empty();
    });

    // Reset state
    $("#telephony-campaign-manager-general-tab").click();
    saveTelephonyCampaignButton.prop("disabled", true);
    currentTelephonyCampaignAgentSelectedId = "";
}

function fillTelephonyCampaignManager() {
    const data = currentTelephonyCampaignData;

    // General
    telephonyCampaignIconInput.text(data.general.emoji);
    telephonyCampaignNameInput.val(data.general.name);
    telephonyCampaignDescriptionInput.val(data.general.description);

    // Agent
    if (data.agent.selectedAgentId) {
        const agentData = BusinessFullData.businessApp.agents.find(a => a.id === data.agent.selectedAgentId);
        if (agentData) {
            currentTelephonyCampaignAgentSelectedId = agentData.id;
            telephonyCampaignAgentIconSpan.text(agentData.general.emoji);
            telephonyCampaignAgentNameInput.val(agentData.general.name[BusinessDefaultLanguage]);
            telephonyCampaignAgentScriptSelect.prop("disabled", false).empty().append('<option value="" disabled>Select Script</option>');
            agentData.scripts.forEach(script => {
                telephonyCampaignAgentScriptSelect.append(`<option value="${script.id}">${script.general.name[BusinessDefaultLanguage]}</option>`);
            });
            telephonyCampaignAgentScriptSelect.val(data.agent.openingScriptId);
        }
    }
    telephonyCampaignAgentLanguageSelect.val(data.agent.language);
    if (data.agent.timezones && data.agent.timezones.length > 0) telephonyCampaignAgentTimezoneSelect.val(data.agent.timezones[0]);
    telephonyCampaignAgentFromNumberInContextCheck.prop("checked", data.agent.fromNumberInContext);
    telephonyCampaignAgentToNumberInContextCheck.prop("checked", data.agent.toNumberInContext);

    // Numbers
    currentTelephonyCampaignDefaultNumberId = data.numberRoute.defaultNumberId;
    const defaultNumberData = BusinessFullData.businessApp.numbers.find(n => n.id === currentTelephonyCampaignDefaultNumberId);
    updateTelephonyDefaultNumberRowUI(defaultNumberData);
    currentTelephonyCampaignRouteNumberList = JSON.parse(JSON.stringify(data.numberRoute.routeNumberList || {}));
    telephonyCampaignNumbersListTable.find("tbody tr:not([data-region-code='default'])").remove();
    if (Object.keys(currentTelephonyCampaignRouteNumberList).length > 0) {
        for (const regionCode in currentTelephonyCampaignRouteNumberList) {
            const numberId = currentTelephonyCampaignRouteNumberList[regionCode];
            const numberData = BusinessFullData.businessApp.numbers.find(n => n.id === numberId);
            if (numberData) {
                const row = createTelephonyCampaignRegionRowElement(regionCode, numberData);
                telephonyCampaignNumbersListTable.find("tbody").append(row);
            }
        }
    }

    // Configuration
    telephonyCampaignRetryOnDeclineCheck.prop("checked", data.configuration.retryOnDecline.enabled).change();
    if (data.configuration.retryOnDecline.enabled) {
        telephonyCampaignRetryDeclineCountInput.val(data.configuration.retryOnDecline.count);
        telephonyCampaignRetryDeclineDelayInput.val(data.configuration.retryOnDecline.delay);
        telephonyCampaignRetryDeclineUnitSelect.val(data.configuration.retryOnDecline.unit.value);
    }
    telephonyCampaignRetryOnMissCheck.prop("checked", data.configuration.retryOnMiss.enabled).change();
    if (data.configuration.retryOnMiss.enabled) {
        telephonyCampaignRetryMissCountInput.val(data.configuration.retryOnMiss.count);
        telephonyCampaignRetryMissDelayInput.val(data.configuration.retryOnMiss.delay);
        telephonyCampaignRetryMissUnitSelect.val(data.configuration.retryOnMiss.unit.value);
    }
    telephonyCampaignPickupDelayInput.val(data.configuration.timeouts.pickupDelayMS);
    telephonyCampaignSilenceNotifyInput.val(data.configuration.timeouts.notifyOnSilenceMS);
    telephonyCampaignSilenceEndInput.val(data.configuration.timeouts.endOnSilenceMS);
    telephonyCampaignMaxCallTimeInput.val(data.configuration.timeouts.maxCallTimeS);

    // Voicemail
    fillTelephonyCampaignVoicemailTab();

    // Variables
    data.variables.dynamicVariables.forEach((dynamicVariable) => {
        const row = createTelephonyCampaignVariableElement(dynamicVariable);
        telephonyCampaignDynamicVariablesList.append(row);
    });
    data.variables.metadata.forEach((metaData) => {
        const row = createTelephonyCampaignVariableElement(metaData);
        telephonyCampaignMetadataList.append(row);
    });

    // Post Analysis
    if (data.postAnalysis.postAnalysisId != null) {
        telephonyCampaignPostAnalysisTemplateSelect.val(data.postAnalysis.postAnalysisId).change();
        data.postAnalysis.contextVariables.forEach((contextVariable) => {
            const uniqueGuid = crypto.randomUUID();

            const contextVariableElement = $(createTelephonyCampaignPostAnalysisContextVariableElement(uniqueGuid, contextVariable));
            telephonyCampaignPostAnalysisVariablesList.append(contextVariableElement);

            const customInput = new CustomVariableInput(
                $(contextVariableElement.find('.variable-input-container')[0]),
                telephonyCampaignPostAnalysisContextVariableArguments,
                {
                    placeholder: "Enter information or {={variable}=} for post analysis context...",
                    onValueChange: () => {
                        checkTelephonyCampaignChanges();
                        validateTelephonyCampaign(true);
                    }
                }
            );

            telephonyCampaignPostAnalysisContextVariablesCustomInput[uniqueGuid] = customInput;

            customInput.setValue(contextVariable.value);
        });
    }

    // Actions
    function fillTelephonyActionTool(actionToolData, actionToolSelectElement) {
        const container = actionToolSelectElement.closest('div');
        const argumentsContainer = container.find('.custom-tool-input-arguments');
        const argumentsList = argumentsContainer.find('[id$="-arguments-list"]');
        const selectElement = argumentsContainer.find('select[id$="-arguments-select"]');
        actionToolSelectElement.val("none");
        selectElement.val("");
        argumentsList.empty();
        argumentsContainer.addClass('d-none');
        if (actionToolData && actionToolData.toolId) {
            actionToolSelectElement.val(actionToolData.toolId).change();
            if (actionToolData.arguments) {
                Object.entries(actionToolData.arguments).forEach(([argId, value]) => {
                    const businessToolData = BusinessFullData.businessApp.tools.find(tool => tool.id === actionToolData.toolId);
                    const argumentData = businessToolData.configuration.inputSchemea.find(arg => arg.id === argId);

                    if (argumentData) {
                        var element = $(createTelephonyCampaignActionArgumentListElement(argumentData));
                        element.find('input').val(value);

                        argumentsList.append(element);
                        selectElement.find(`option[value="${argId}"]`).remove();
                    }
                });
            }
        }
    }
    fillTelephonyActionTool(data.actions.callInitiationFailureTool, telephonyCampaignActionToolCallInitiationFailureSelect);
    fillTelephonyActionTool(data.actions.callInitiatedTool, telephonyCampaignActionToolCallInitiatedSelect);
    fillTelephonyActionTool(data.actions.callDeclinedTool, telephonyCampaignActionToolCallDeclinedSelect);
    fillTelephonyActionTool(data.actions.callMissedTool, telephonyCampaignActionToolCallMissedSelect);
    fillTelephonyActionTool(data.actions.callAnsweredTool, telephonyCampaignActionToolCallAnsweredSelect);
    fillTelephonyActionTool(data.actions.callEndedTool, telephonyCampaignActionToolCallEndedSelect);
}

function checkTelephonyCampaignChanges(enableDisableButton = true) {
    if (manageTelephonyCampaignType === null) {
        return {
            hasChanges: false
        };
    }

    const changes = {};
    let hasChanges = false;
    const original = currentTelephonyCampaignData;

    function checkGeneralTab() {
        changes.general = {
            emoji: telephonyCampaignIconInput.text(),
            name: telephonyCampaignNameInput.val().trim(),
            description: telephonyCampaignDescriptionInput.val().trim(),
        };

        if (changes.general.emoji !== original.general.emoji ||
            changes.general.name !== original.general.name ||
            changes.general.description !== original.general.description) {
            hasChanges = true;
        }
    }

    function checkAgentTab() {
        const timezoneValue = telephonyCampaignAgentTimezoneSelect.find(":selected").val();
        changes.agent = {
            selectedAgentId: currentTelephonyCampaignAgentSelectedId,
            openingScriptId: telephonyCampaignAgentScriptSelect.find(":selected").val(),
            language: telephonyCampaignAgentLanguageSelect.find(":selected").val(),
            timezones: (timezoneValue && timezoneValue.trim() !== "") ? [timezoneValue] : [],
            fromNumberInContext: telephonyCampaignAgentFromNumberInContextCheck.is(":checked"),
            toNumberInContext: telephonyCampaignAgentToNumberInContextCheck.is(":checked"),
        };

        if (changes.agent.selectedAgentId !== original.agent.selectedAgentId ||
            changes.agent.openingScriptId !== original.agent.openingScriptId ||
            changes.agent.language !== original.agent.language ||
            JSON.stringify(changes.agent.timezones) !== JSON.stringify(original.agent.timezones) ||
            changes.agent.fromNumberInContext !== original.agent.fromNumberInContext ||
            changes.agent.toNumberInContext !== original.agent.toNumberInContext) {
            hasChanges = true;
        }
    }

    function checkNumbersTab() {
        changes.numberRoute = {
            defaultNumberId: currentTelephonyCampaignDefaultNumberId,
            routeNumberList: currentTelephonyCampaignRouteNumberList
        };
        if (changes.numberRoute.defaultNumberId !== original.numberRoute.defaultNumberId ||
            JSON.stringify(changes.numberRoute.routeNumberList) !== JSON.stringify(original.numberRoute.routeNumberList)) {
            hasChanges = true;
        }
    }

    function checkConfigurationTab() {
        changes.configuration = {
            retryOnDecline: {
                enabled: telephonyCampaignRetryOnDeclineCheck.is(":checked"),
                count: parseInt(telephonyCampaignRetryDeclineCountInput.val()),
                delay: parseInt(telephonyCampaignRetryDeclineDelayInput.val()),
                unit: parseInt(telephonyCampaignRetryDeclineUnitSelect.val())
            },
            retryOnMiss: {
                enabled: telephonyCampaignRetryOnMissCheck.is(":checked"),
                count: parseInt(telephonyCampaignRetryMissCountInput.val()),
                delay: parseInt(telephonyCampaignRetryMissDelayInput.val()),
                unit: parseInt(telephonyCampaignRetryMissUnitSelect.val())
            },
            timeouts: {
                pickupDelayMS: parseInt(telephonyCampaignPickupDelayInput.val()),
                notifyOnSilenceMS: parseInt(telephonyCampaignSilenceNotifyInput.val()),
                endOnSilenceMS: parseInt(telephonyCampaignSilenceEndInput.val()),
                maxCallTimeS: parseInt(telephonyCampaignMaxCallTimeInput.val()),
            }
        };

        if (changes.configuration.retryOnDecline.enabled !== original.configuration.retryOnDecline.enabled) {
            hasChanges = true;
        } else if (changes.configuration.retryOnDecline.enabled === true) {
            if (changes.configuration.retryOnDecline.count !== original.configuration.retryOnDecline.count ||
                changes.configuration.retryOnDecline.delay !== original.configuration.retryOnDecline.delay ||
                changes.configuration.retryOnDecline.unit !== original.configuration.retryOnDecline.unit.value) {
                hasChanges = true;
            }
        }

        if (changes.configuration.retryOnMiss.enabled !== original.configuration.retryOnMiss.enabled) {
            hasChanges = true;
        } else if (changes.configuration.retryOnMiss.enabled === true) {
            if (changes.configuration.retryOnMiss.count !== original.configuration.retryOnMiss.count ||
                changes.configuration.retryOnMiss.delay !== original.configuration.retryOnMiss.delay ||
                changes.configuration.retryOnMiss.unit !== original.configuration.retryOnMiss.unit.value) {
                hasChanges = true;
            }
        }

        if (changes.configuration.timeouts.pickupDelayMS !== original.configuration.timeouts.pickupDelayMS ||
            changes.configuration.timeouts.notifyOnSilenceMS !== original.configuration.timeouts.notifyOnSilenceMS ||
            changes.configuration.timeouts.endOnSilenceMS !== original.configuration.timeouts.endOnSilenceMS ||
            changes.configuration.timeouts.maxCallTimeS !== original.configuration.timeouts.maxCallTimeS) {
            hasChanges = true;
        }
    }

    function checkVoicemailTab() {
        changes.voicemailDetection = {
            isEnabled: telephonyCampaignVoicemailIsEnabledCheck.is(":checked")
        };

        if (changes.voicemailDetection.isEnabled) {
            changes.voicemailDetection.initialCheckDelayMS = parseInt(telephonyCampaignVoicemailInitialCheckDelayMSInput.val(), 10);
            changes.voicemailDetection.voiceMailMessageVADSilenceThresholdMS = parseInt(telephonyCampaignVoicemailVADSilenceThresholdMSInput.val(), 10);
            changes.voicemailDetection.voiceMailMessageVADMaxSpeechDurationMS = parseInt(telephonyCampaignVoicemailVADMaxSpeechDurationMSInput.val(), 10);
            changes.voicemailDetection.onVoiceMailMessageDetectVerifySTTAndLLM = telephonyCampaignVoicemailAdvancedVerificationCheck.is(":checked");
            if (changes.voicemailDetection.onVoiceMailMessageDetectVerifySTTAndLLM) {
                changes.voicemailDetection.transcribeVoiceMessageSTT = telephonyCampaignVoicemailSTTIntegrationManager.getData();
                changes.voicemailDetection.verifyVoiceMessageLLM = telephonyCampaignVoicemailLLMIntegrationManager.getData();
            }
            else {
                changes.voicemailDetection.transcribeVoiceMessageSTT = null;
                changes.voicemailDetection.verifyVoiceMessageLLM = null;
            }
            changes.voicemailDetection.stopSpeakingAgentAfterMlCheckSuccess = telephonyCampaignStopAgentOnMLCheck.is(':checked');
            changes.voicemailDetection.stopSpeakingAgentAfterVadSilence = telephonyCampaignStopAgentOnVADCheck.is(':checked');
            changes.voicemailDetection.stopSpeakingAgentAfterLLMConfirm = telephonyCampaignStopAgentOnLLMCheck.is(':checked');
            changes.voicemailDetection.stopSpeakingAgentDelayAfterMatchMS = parseInt(telephonyCampaignVoicemailStopSpeakingDelayInput.val(), 10);
            changes.voicemailDetection.endOrLeaveMessageAfterMLCheckSuccess = telephonyCampaignEndLeaveOnMLCheck.is(':checked');
            changes.voicemailDetection.endOrLeaveMessageAfterVadSilence = telephonyCampaignEndLeaveOnVADCheck.is(':checked');
            changes.voicemailDetection.endOrLeaveMessageAfterLLMConfirm = telephonyCampaignEndLeaveOnLLMCheck.is(':checked');
            changes.voicemailDetection.endOrLeaveMessageDelayAfterMatchMS = parseInt(telephonyCampaignVoicemailEndLeaveDelayInput.val(), 10);
            const finalAction = telephonyCampaignFinalActionRadios.filter(":checked").val();
            changes.voicemailDetection.endCallOnDetect = finalAction === 'end';
            changes.voicemailDetection.leaveMessageOnDetect = finalAction === 'leave';
            if (changes.voicemailDetection.leaveMessageOnDetect) {
                changes.voicemailDetection.messageToLeave = currentTelephonyCampaignVoicemailMessageToLeaveMultiLangData;
            }
        }

        if (changes.voicemailDetection.isEnabled !== original.voicemailDetection.isEnabled) {
            hasChanges = true;
        } else if (changes.voicemailDetection.isEnabled) {
            if (JSON.stringify(changes.voicemailDetection) !== JSON.stringify(original.voicemailDetection)) {
                hasChanges = true;
            }
        }
    }

    function checkVariablesTab() {
        changes.variables = {
            dynamicVariables: [],
            metadata: []
        };

        changes.variables.dynamicVariables = getTelephonyCampaignVariablesList(telephonyCampaignDynamicVariablesList);
        changes.variables.metadata = getTelephonyCampaignVariablesList(telephonyCampaignMetadataList);

        // Check Changes
        function areArraysOfObjectsEqual(arr1, arr2) {
            if (arr1 === arr2) return true;
            if (!arr1 || !arr2 || arr1.length !== arr2.length) return false;

            for (let i = 0; i < arr1.length; i++) {
                const obj1 = arr1[i];
                const obj2 = arr2[i];
                const keys1 = Object.keys(obj1);
                const keys2 = Object.keys(obj2);
                if (keys1.length !== keys2.length) return false;
                for (const key of keys1) {
                    if (obj1[key] !== obj2[key]) return false;
                }
            }
            return true;
        }

        if (!areArraysOfObjectsEqual(changes.variables.dynamicVariables, original.variables.dynamicVariables) ||
            !areArraysOfObjectsEqual(changes.variables.metadata, original.variables.metadata)) {
            hasChanges = true;
        }
    }

    function checkPostAnalysisTab() {
        changes.postAnalysis = {};

        let postAnalysisId = telephonyCampaignPostAnalysisTemplateSelect.find("option:selected").val();
        if (!postAnalysisId || postAnalysisId == "" || postAnalysisId == null) {
            postAnalysisId = null;
        }
        changes.postAnalysis.postAnalysisId = postAnalysisId;
        if (changes.postAnalysis.postAnalysisId != original.postAnalysis.postAnalysisId) {
            hasChanges = true;
        }

        if (postAnalysisId == null) {
            changes.postAnalysis.contextVariables = null;
        }
        else {
            changes.postAnalysis.contextVariables = [];

            telephonyCampaignPostAnalysisVariablesList.children().each((i, contextVariableElement) => {
                const dataId = $(contextVariableElement).attr("data-id");

                const contextVariableData = {
                    name: $(contextVariableElement).find('.campaign-post-analysis-context-variable-name').val() ?? "",
                    description: $(contextVariableElement).find('.campaign-post-analysis-context-variable-description').val() ?? "",
                    value: telephonyCampaignPostAnalysisContextVariablesCustomInput[dataId].getValue() ?? ""
                };

                changes.postAnalysis.contextVariables.push(contextVariableData);
            });
        }

        if (postAnalysisId != null && original.postAnalysis.postAnalysisId != null) {
            if (changes.postAnalysis.contextVariables.length != original.postAnalysis.contextVariables.length) {
                hasChanges = true;
            }

            if (JSON.stringify(changes.postAnalysis.contextVariables) != JSON.stringify(original.postAnalysis.contextVariables)) {
                hasChanges = true;
            }
        }
    }

    function checkActionsTab() {
        function collectToolArguments(selectElement) {
            const args = {};
            const argumentsList = selectElement.siblings('.custom-tool-input-arguments').find('[id$="-arguments-list"]');
            argumentsList.find(".input-group input").each((_, el) => {
                const input = $(el);
                args[input.attr("input_arguement")] = input.val().trim();
            });
            return Object.keys(args).length > 0 ? args : null;
        }

        function compareToolData(newTool, originalTool) {
            if (newTool.toolId !== originalTool.toolId) return true;
            if (JSON.stringify(newTool.arguments) !== JSON.stringify(originalTool.arguments)) return true;
            return false;
        }

        changes.actions = {
            callInitiationFailureTool: {
                toolId: telephonyCampaignActionToolCallInitiationFailureSelect.val() === 'none' ? null : telephonyCampaignActionToolCallInitiationFailureSelect.val(),
                arguments: collectToolArguments(telephonyCampaignActionToolCallInitiationFailureSelect)
            },
            callInitiatedTool: {
                toolId: telephonyCampaignActionToolCallInitiatedSelect.val() === 'none' ? null : telephonyCampaignActionToolCallInitiatedSelect.val(),
                arguments: collectToolArguments(telephonyCampaignActionToolCallInitiatedSelect)
            },
            callDeclinedTool: {
                toolId: telephonyCampaignActionToolCallDeclinedSelect.val() === 'none' ? null : telephonyCampaignActionToolCallDeclinedSelect.val(),
                arguments: collectToolArguments(telephonyCampaignActionToolCallDeclinedSelect)
            },
            callMissedTool: {
                toolId: telephonyCampaignActionToolCallMissedSelect.val() === 'none' ? null : telephonyCampaignActionToolCallMissedSelect.val(),
                arguments: collectToolArguments(telephonyCampaignActionToolCallMissedSelect)
            },
            callAnsweredTool: {
                toolId: telephonyCampaignActionToolCallAnsweredSelect.val() === 'none' ? null : telephonyCampaignActionToolCallAnsweredSelect.val(),
                arguments: collectToolArguments(telephonyCampaignActionToolCallAnsweredSelect)
            },
            callEndedTool: {
                toolId: telephonyCampaignActionToolCallEndedSelect.val() === 'none' ? null : telephonyCampaignActionToolCallEndedSelect.val(),
                arguments: collectToolArguments(telephonyCampaignActionToolCallEndedSelect)
            },
        };

        if (compareToolData(changes.actions.callInitiationFailureTool, original.actions.callInitiationFailureTool) ||
            compareToolData(changes.actions.callInitiatedTool, original.actions.callInitiatedTool) ||
            compareToolData(changes.actions.callDeclinedTool, original.actions.callDeclinedTool) ||
            compareToolData(changes.actions.callMissedTool, original.actions.callMissedTool) ||
            compareToolData(changes.actions.callAnsweredTool, original.actions.callAnsweredTool) ||
            compareToolData(changes.actions.callEndedTool, original.actions.callEndedTool)) {
            hasChanges = true;
        }
    }

    // Execute all checks
    checkGeneralTab();
    checkAgentTab();
    checkNumbersTab();
    checkConfigurationTab();  
    checkVoicemailTab();
    checkVariablesTab();
    checkPostAnalysisTab()
    checkActionsTab();

    if (enableDisableButton) {
        saveTelephonyCampaignButton.prop("disabled", !hasChanges);
    }

    return {
        hasChanges,
        changes
    };
}

function validateTelephonyCampaign(onlyRemove = true) {
    if (manageTelephonyCampaignType === null) return {
        validated: true,
        errors: []
    };

    const errors = [];
    let validated = true;
    telephonyCampaignsManagerView.find('.is-invalid').removeClass('is-invalid');
    telephonyCampaignsManagerView.find('.border-danger').removeClass('border-danger');
    telephonyCampaignsManagerView.find('.table-danger').removeClass('table-danger');

    // General
    function validateGeneralTab() {
        if (!telephonyCampaignNameInput.val().trim()) {
            validated = false;
            errors.push("Campaign name is required.");
            if (!onlyRemove) telephonyCampaignNameInput.addClass('is-invalid');
        }
        if (!telephonyCampaignDescriptionInput.val().trim()) {
            validated = false;
            errors.push("Campaign description is required.");
            if (!onlyRemove) telephonyCampaignDescriptionInput.addClass('is-invalid');
        }
    }

    // Agent
    function validateAgentTab() {
        if (!currentTelephonyCampaignAgentSelectedId) {
            validated = false;
            errors.push("An agent must be selected.");
            if (!onlyRemove) telephonyCampaignAgentNameInput.addClass('is-invalid');
        }
        if (!telephonyCampaignAgentScriptSelect.val()) {
            validated = false;
            errors.push("An opening script is required.");
            if (!onlyRemove) telephonyCampaignAgentScriptSelect.addClass('is-invalid');
        }
        if (!telephonyCampaignAgentLanguageSelect.val()) {
            validated = false;
            errors.push("A language must be selected.");
            if (!onlyRemove) telephonyCampaignAgentLanguageSelect.addClass('is-invalid');
        }
        if (!telephonyCampaignAgentTimezoneSelect.val()) {
            validated = false;
            errors.push("A timezone must be selected.");
            if (!onlyRemove) telephonyCampaignAgentTimezoneSelect.addClass('is-invalid');
        }
    }

    // Configuration
    function validateConfigurationTab() {
        const pickupDelayValue = parseInt(telephonyCampaignPickupDelayInput.val());
        if (isNaN(pickupDelayValue) || pickupDelayValue < 0) {
            validated = false;
            errors.push("Pick up delay must be a valid number.");
            if (!onlyRemove) telephonyCampaignPickupDelayInput.addClass("is-invalid");
        }

        const silenceNotifyValue = parseInt(telephonyCampaignSilenceNotifyInput.val());
        if (isNaN(silenceNotifyValue) || silenceNotifyValue < 0) {
            validated = false;
            errors.push("Notify on silence must be a valid number.");
            if (!onlyRemove) telephonyCampaignSilenceNotifyInput.addClass("is-invalid");
        }

        const silenceEndValue = parseInt(telephonyCampaignSilenceEndInput.val());
        if (isNaN(silenceEndValue) || silenceEndValue < 0) {
            validated = false;
            errors.push("End call on silence must be a valid number.");
            if (!onlyRemove) telephonyCampaignSilenceEndInput.addClass("is-invalid");
        }

        const maxCallTimeValue = parseInt(telephonyCampaignMaxCallTimeInput.val());
        if (isNaN(maxCallTimeValue) || maxCallTimeValue < 0) {
            validated = false;
            errors.push("Max call time must be a valid number.");
            if (!onlyRemove) telephonyCampaignMaxCallTimeInput.addClass("is-invalid");
        }
    }
    
    // Numbers
    function validateNumbersTab() {
        if (!currentTelephonyCampaignDefaultNumberId) {
            validated = false;
            errors.push("A default calling number must be set.");
            if (!onlyRemove) {
                telephonyCampaignNumbersListTable.find("tr[data-region-code='default']").addClass('table-danger');
            }
        } else {
            telephonyCampaignNumbersListTable.find("tr[data-region-code='default']").removeClass('table-danger');
        }
    }
  
    // Voicemail
    function validateVoicemailTab() {
        if (!telephonyCampaignVoicemailIsEnabledCheck.is(":checked")) return; // No validation needed if disabled

        if (telephonyCampaignVoicemailAdvancedVerificationCheck.is(":checked")) {
            const sttValidation = telephonyCampaignVoicemailSTTIntegrationManager.validate();
            if (!sttValidation.isValid) {
                validated = false;
                errors.push(...sttValidation.errors.map(e => `Voicemail STT: ${e}`));
                if (!onlyRemove) telephonyCampaignVoicemailSTTIntegrationManager.getSelectElements().addClass('is-invalid');
            }
            const llmValidation = telephonyCampaignVoicemailLLMIntegrationManager.validate();
            if (!llmValidation.isValid) {
                validated = false;
                errors.push(...llmValidation.errors.map(e => `Voicemail LLM: ${e}`));
                if (!onlyRemove) telephonyCampaignVoicemailLLMIntegrationManager.getSelectElements().addClass('is-invalid');
            }
        }

        const isStopTriggerSelected = telephonyCampaignStopAgentOnMLCheck.is(':checked') || telephonyCampaignStopAgentOnVADCheck.is(':checked') || telephonyCampaignStopAgentOnLLMCheck.is(':checked');
        if (!isStopTriggerSelected) {
            validated = false;
            errors.push("At least one 'Stop Agent Speaking' trigger must be selected for Voicemail Detection.");
            if (!onlyRemove) telephonyCampaignStopAgentOnMLCheck.closest('.card').addClass('border-danger');
        }
        const isEndLeaveTriggerSelected = telephonyCampaignEndLeaveOnMLCheck.is(':checked') || telephonyCampaignEndLeaveOnVADCheck.is(':checked') || telephonyCampaignEndLeaveOnLLMCheck.is(':checked');
        if (!isEndLeaveTriggerSelected) {
            validated = false;
            errors.push("At least one 'End Call / Leave Message' trigger must be selected for Voicemail Detection.");
            if (!onlyRemove) telephonyCampaignEndLeaveOnMLCheck.closest('.card').addClass('border-danger');
        }

        if (telephonyCampaignFinalActionRadios.filter('[value="leave"]').is(":checked")) {
            let isMessageEmpty = false;
            BusinessFullData.businessData.languages.forEach(language => {
                if (!currentTelephonyCampaignVoicemailMessageToLeaveMultiLangData[language] || currentTelephonyCampaignVoicemailMessageToLeaveMultiLangData[language].trim() === "") {
                    isMessageEmpty = true;
                }
            });
            if (isMessageEmpty) {
                validated = false;
                errors.push("The 'Message to Leave' cannot be empty for any business language.");
                if (!onlyRemove) telephonyCampaignVoicemailMessageToLeaveTextarea.addClass("is-invalid");
            }
        }
    }

    // Variables
    function validateVariablesTab() {
        function checkVariableList(variablesList, listName) {
            var currentAddedKeys = [];

            variablesList.find(".telephony-campaign-variable-box").each((index, variableElement) => {
                var variableKeyElement = $(variableElement).find('input[data-type="key"]');
                var variableKey = variableKeyElement.val();

                if (!variableKey || variableKey == "" || variableKey == null) {
                    validated = false;
                    errors.push(`${listName}: Variable key is required and can not be empty.`);
                    if (!onlyRemove) variableKeyElement.addClass('is-invalid');
                }
                else {
                    variableKey = variableKey.trim();

                    if (currentAddedKeys.includes(variableKey)) {
                        validated = false;
                        errors.push(`${listName}: Variable key must be unique but is duplicate for ${variableKey}`);
                        if (!onlyRemove) variableKeyElement.addClass('is-invalid');
                    }
                    else {
                        currentAddedKeys.push(variableKey);
                    }
                }
            });
        }

        checkVariableList(telephonyCampaignDynamicVariablesList, "Dynamic Variables");
        checkVariableList(telephonyCampaignMetadataList, "Metadata");
    }

    // Post Analysis
    function validatePostAnalysisTab() {
        let postAnalysisId = telephonyCampaignPostAnalysisTemplateSelect.find("option:selected").val();
        if (postAnalysisId && postAnalysisId != "" && postAnalysisId != null) {
            telephonyCampaignPostAnalysisVariablesList.children().each((i, contextVariableElement) => {
                const nameInput = $(contextVariableElement).find('.campaign-post-analysis-context-variable-name');
                const nameValue = nameInput.val();
                if (!nameValue || nameValue == "" || nameValue == null) {
                    validated = false;
                    errors.push("Context variable name is required and can not be empty.");
                    if (!onlyRemove) nameInput.addClass("is-invalid");
                }
                else {
                    nameInput.removeClass("is-invalid");
                }

                const descriptionInput = $(contextVariableElement).find('.campaign-post-analysis-context-variable-description');
                const descriptionValue = descriptionInput.val();
                if (!descriptionValue || descriptionValue == "" || descriptionValue == null) {
                    validated = false;
                    errors.push("Context variable description is required and can not be empty.");
                    if (!onlyRemove) descriptionInput.addClass("is-invalid");
                }
                else {
                    descriptionInput.removeClass("is-invalid");
                }

                const variableInputEditor = $(contextVariableElement).find('.variable-input-container .editor-area.form-control').first();
                const dataId = $(contextVariableElement).attr("data-id");
                const validationResult = telephonyCampaignPostAnalysisContextVariablesCustomInput[dataId].validate();
                if (!validationResult.isValidated) {
                    validated = false;
                    errors.push(validationResult.errors);
                    if (!onlyRemove) variableInputEditor.addClass("is-invalid");
                }
                else {
                    var variableValue = telephonyCampaignPostAnalysisContextVariablesCustomInput[dataId].getValue();
                    if (!variableValue || variableValue == "" || variableValue == null) {
                        validated = false;
                        errors.push("Context variable value is required and can not be empty.");
                        if (!onlyRemove) variableInputEditor.addClass("is-invalid");
                    }
                    else {
                        variableInputEditor.removeClass("is-invalid");
                    }
                }
            });
        }
    }

    // Actions
    function validateActionsTab() {
        function validateToolArguments($toolSelect, errorPrefix) {
            if ($toolSelect.val() === "none") return;

            const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === $toolSelect.val());
            if (!toolData) return;

            const requiredArguments = toolData.configuration.inputSchemea.filter((arg) => arg.isRequired);
            const $argumentsContainer = $toolSelect.closest('div').find('.custom-tool-input-arguments');

            requiredArguments.forEach((reqArg) => {
                const $argInput = $argumentsContainer.find(`input[input_arguement="${reqArg.id}"]`);
                if ($argInput.length === 0 || !$argInput.val().trim()) {
                    validated = false;
                    errors.push(`${errorPrefix}: ${reqArg.name[BusinessDefaultLanguage]} is required.`);
                    if (!onlyRemove && $argInput.length > 0) $argInput.addClass("is-invalid");
                }
            });
        }

        validateToolArguments(telephonyCampaignActionToolCallInitiationFailureSelect, "Call Initiation Failure tool");
        validateToolArguments(telephonyCampaignActionToolCallInitiatedSelect, "Call Initiated tool");
        validateToolArguments(telephonyCampaignActionToolCallDeclinedSelect, "Call Declined tool");
        validateToolArguments(telephonyCampaignActionToolCallMissedSelect, "Call Missed tool");
        validateToolArguments(telephonyCampaignActionToolCallAnsweredSelect, "Call Answered tool");
        validateToolArguments(telephonyCampaignActionToolCallEndedSelect, "Call Ended tool");
    }


    // Execute all validation checks
    validateGeneralTab();
    validateAgentTab();
    validateConfigurationTab();
    validateNumbersTab();
    validateVoicemailTab();
    validateVariablesTab();
    validatePostAnalysisTab()
    validateActionsTab();

    return {
        validated,
        errors
    };
}

async function canLeaveTelephonyCampaignsManager(leaveMessage = "") {
    if (isSavingTelephonyCampaign) {
        AlertManager.createAlert({
            type: "warning",
            message: "Campaign is currently being saved. Please wait."
        });
        return false;
    }
    const {
        hasChanges
    } = checkTelephonyCampaignChanges(false);
    if (hasChanges) {
        const confirmDialog = new BootstrapConfirmDialog({
            title: "Unsaved Changes",
            message: `You have unsaved changes in this campaign.${leaveMessage}`,
            confirmText: "Discard",
            cancelText: "Cancel",
            confirmButtonClass: "btn-danger"
        });
        return await confirmDialog.show();
    }
    return true;
}

function handleTelephonyCampaignRouting(subPath) {
    if (manageTelephonyCampaignType === 'new' || manageTelephonyCampaignType === 'edit') {
        let correctPath;
        if (manageTelephonyCampaignType === 'new') {
            correctPath = 'telephonycampaigns/new';
        } else {
            correctPath = `telephonycampaigns/${currentTelephonyCampaignData.id}`;
        }

        replaceUrlForTab(correctPath);
        return;
    }

    if (!subPath || subPath.length === 0) {
        if (telephonyCampaignsManagerView.hasClass("show") && !telephonyCampaignsListView.hasClass("show")) {
            showTelephonyCampaignsListView();
        }
        replaceUrlForTab('telephonycampaigns');
        return;
    }

    const action = subPath[0];
    const campaignCard = telephonyCampaignsListContainer.find(`.campaign-card[data-campaign-id="${action}"]`);

    if (action === 'new') {
        if (!telephonyCampaignsManagerView.hasClass('show')) {
            addNewTelephonyCampaignButton.click();
        }
    } else if (campaignCard.length > 0) {
        if (!telephonyCampaignsManagerView.hasClass('show')) {
            campaignCard.click();
        }
    } else {
        showTelephonyCampaignsListView();
        replaceUrlForTab('telephonycampaigns');
    }
}

function SetTelephonyCampaignCardDynamicWidth() {
    if (!telephonyCampaignsTab.hasClass("show")) return;

    const anyTelephonyCampaignCard = telephonyCampaignsListContainer.find(".telephony-campaign-card");
    if (anyTelephonyCampaignCard.length > 0) {
        const firstTelephonyCampaignCard = anyTelephonyCampaignCard.first();

        const telephonyCampaignCardWidth = firstTelephonyCampaignCard.innerWidth();

        const telephonyCampaignCardLeftRightPadding = parseInt(firstTelephonyCampaignCard.css("padding-left")) + parseInt(firstTelephonyCampaignCard.css("padding-right"));
        const telephonyCampaignCardIconWidthAndPadding = firstTelephonyCampaignCard.find(".route-icon").innerWidth();

        // .campaign-card h4
        const marginLeftForH4 = 20; // .campaign-card h4 in style.css

        const currentUsedUpSpace = telephonyCampaignCardLeftRightPadding + telephonyCampaignCardIconWidthAndPadding + marginLeftForH4;

        let availableH4Space = telephonyCampaignCardWidth - currentUsedUpSpace;

        if (availableH4Space < 5) {
            availableH4Space = 5;
        }

        // .campaign-card h5-info
        let availableH5Space = telephonyCampaignCardWidth - telephonyCampaignCardLeftRightPadding;

        // FINAL
        $("#dynamicTelephonyCampaignCardCSS").html(`
            .telephony-campaign-card .card-data {
				width: ${availableH4Space}px;
			}

            .telephony-campaign-card .h5-info {
                width: ${availableH5Space}px;
            }
		`);
    }
}

/** HELPER FUNCTIONS **/

// Agent Tab
function createTelephonyCampaignAgentModalListElement(agentData) {
    return `<button type="button" class="list-group-item list-group-item-action" data-agent-id="${agentData.id}"><span>${agentData.general.emoji} ${agentData.general.name[BusinessDefaultLanguage]}</span></button>`;
}

// Numbers Tab
function updateTelephonyDefaultNumberRowUI(numberData) {
    const defaultRow = telephonyCampaignNumbersListTable.find("tr[data-region-code='default']");
    const numberCell = defaultRow.find("td").eq(1); // Second cell
    const actionButton = defaultRow.find("button[data-action='edit-route']");
    defaultRow.find("td[data-col='provider-only']").remove();
    if (numberData) {
        numberCell.removeAttr('colspan');
        numberCell.html(`<span>${numberData.number}</span>`);
        const providerCell = $(`<td data-col="provider-only">${numberData.provider.name}</td>`);
        numberCell.after(providerCell);
        actionButton.find('span').text(" Change");
        actionButton.removeClass('btn-primary').addClass('btn-secondary');
    } else {
        numberCell.attr('colspan', '2');
        numberCell.html(`<span class="text-muted">No default number selected.</span>`);
        actionButton.find('span').text(" Set Number");
        actionButton.removeClass('btn-secondary').addClass('btn-primary');
    }
}

function createTelephonyCampaignRegionRowElement(regionCode, numberData) {
    const countryData = CountriesList[regionCode.toUpperCase()];
    const regionName = countryData ? `${countryData.Country} (${countryData.phone_code})` : regionCode;
    return `
        <tr data-region-code="${regionCode}">
            <td>${regionName}</td>
            <td>${numberData.number}</td>
            <td>${numberData.provider.name}</td>
            <td>
                <button class="btn btn-secondary btn-sm me-1" data-action="edit-route" title="Change Number"><i class="fa-regular fa-pencil"></i></button>
                <button class="btn btn-danger btn-sm" data-action="delete-route" title="Delete Route"><i class="fa-regular fa-trash"></i></button>
            </td>
        </tr>
    `;
}

function populateTelephonyNumberSelectionModal() {
    const modalBody = telephonyCampaignChangeNumberModalElement.find('.modal-body');
    modalBody.empty();

    const listGroup = $('<div class="list-group"></div>');
    const availableNumbers = BusinessFullData.businessApp.numbers;

    if (availableNumbers.length === 0) {
        listGroup.append("<span>No numbers found for your business.</span>");
    } else {
        availableNumbers.forEach((number) => {
            const countryData = CountriesList[number.countryCode.toUpperCase()];
            listGroup.append(`
                <button type="button" class="list-group-item list-group-item-action" data-number-id="${number.id}">
                    ${countryData.phone_code} ${number.number}
                </button>
            `);
        });
    }
    modalBody.append(listGroup);
}

// Variable Tab
function createTelephonyCampaignVariableElement(data) {
    let isRequiredCheckBoxIdUnique = `telephony-campaign-variable-required-${crypto.randomUUID()}`;
    let isEmptyOrNullAllowedCheckBoxIdUnique = `telephony-campaign-variable-emptyOrNull-${crypto.randomUUID()}`;

    return `
        <div class="input-group mt-1 telephony-campaign-variable-box">
			<input type="text" class="form-control" data-type="key" placeholder="Key" value="${data ? data.key : ""}">
            <div class="input-group-text">
                <input class="form-check-input mt-0" type="checkbox" id="${isRequiredCheckBoxIdUnique}" data-type="isRequired" ${data && data.isRequired ? "checked" : ""}>
                <label class="form-check-label ms-1" for="${isRequiredCheckBoxIdUnique}">Required?</label>
            </div>
            <div class="input-group-text">
                <input class="form-check-input mt-0" type="checkbox" id="${isEmptyOrNullAllowedCheckBoxIdUnique}" data-type="isEmptyOrNullAllowed" ${data && data.isEmptyOrNullAllowed ? "checked" : ""}>
                <label class="form-check-label ms-1" for="${isEmptyOrNullAllowedCheckBoxIdUnique}">Empty Allowed?</label>
            </div>
			<button class="btn btn-danger" button-type="removeTelephonyCampaignVariable">
				<i class="fa-regular fa-trash"></i>
			</button>
		</div>
    `;
}

function initTelephonyCampaignVariablesEventHandlers() {
    // Dynamic Variables
    addTelephonyCampaignDynamicVariable.on('click', (event) => {
        var newElement = createTelephonyCampaignVariableElement(null);
        telephonyCampaignDynamicVariablesList.append(newElement);

        checkTelephonyCampaignChanges();
        validateTelephonyCampaign(true);
    });

    telephonyCampaignDynamicVariablesList.on('click', '.btn[button-type="removeTelephonyCampaignVariable"]', onRemoveVariable);

    // Metadata
    addTelephonyCampaignMetadata.on('click', (event) => {
        var newElement = createTelephonyCampaignVariableElement(null);
        telephonyCampaignMetadataList.append(newElement);

        checkTelephonyCampaignChanges();
        validateTelephonyCampaign(true);
    });

    telephonyCampaignMetadataList.on('click', '.btn[button-type="removeTelephonyCampaignVariable"]', onRemoveVariable);

    // Common
    function onRemoveVariable(event) {
        event.preventDefault();

        const currentElement = $(event.currentTarget);
        currentElement.closest('.telephony-campaign-variable-box').remove();

        checkTelephonyCampaignChanges();
        validateTelephonyCampaign(true);
    }
}

function getTelephonyCampaignVariablesList(variablesList) {
    var array = [];

    variablesList.find(".telephony-campaign-variable-box").each((index, variableElement) => {
        var variableKey = $(variableElement).find('input[data-type="key"]').val()?.trim();
        var isRequired = $(variableElement).find('input[data-type="isRequired"]').is(":checked");
        var isEmptyOrNullAllowed = $(variableElement).find('input[data-type="isEmptyOrNullAllowed"]').is(":checked");

        var object = {
            key: variableKey,
            isRequired: isRequired,
            isEmptyOrNullAllowed: isEmptyOrNullAllowed
        };

        array.push(object);
    });

    return array;
}

// Voicemail Tab
function fillTelephonyCampaignVoicemailTab() {
    const data = currentTelephonyCampaignData.voicemailDetection;

    telephonyCampaignVoicemailIsEnabledCheck.prop('checked', data.isEnabled).change();
    telephonyCampaignVoicemailInitialCheckDelayMSInput.val(data.initialCheckDelayMS);
    telephonyCampaignVoicemailVADSilenceThresholdMSInput.val(data.voiceMailMessageVADSilenceThresholdMS);
    telephonyCampaignVoicemailVADMaxSpeechDurationMSInput.val(data.voiceMailMessageVADMaxSpeechDurationMS);

    telephonyCampaignVoicemailAdvancedVerificationCheck.prop('checked', data.onVoiceMailMessageDetectVerifySTTAndLLM).change();
    if (telephonyCampaignVoicemailSTTIntegrationManager) telephonyCampaignVoicemailSTTIntegrationManager.load(data.transcribeVoiceMessageSTT);
    if (telephonyCampaignVoicemailLLMIntegrationManager) telephonyCampaignVoicemailLLMIntegrationManager.load(data.verifyVoiceMessageLLM);

    telephonyCampaignStopAgentOnMLCheck.prop('checked', data.stopSpeakingAgentAfterMlCheckSuccess);
    telephonyCampaignStopAgentOnVADCheck.prop('checked', data.stopSpeakingAgentAfterVadSilence);
    telephonyCampaignStopAgentOnLLMCheck.prop('checked', data.stopSpeakingAgentAfterLLMConfirm);
    telephonyCampaignVoicemailStopSpeakingDelayInput.val(data.stopSpeakingAgentDelayAfterMatchMS);

    telephonyCampaignEndLeaveOnMLCheck.prop('checked', data.endOrLeaveMessageAfterMLCheckSuccess);
    telephonyCampaignEndLeaveOnVADCheck.prop('checked', data.endOrLeaveMessageAfterVadSilence);
    telephonyCampaignEndLeaveOnLLMCheck.prop('checked', data.endOrLeaveMessageAfterLLMConfirm);
    telephonyCampaignVoicemailEndLeaveDelayInput.val(data.endOrLeaveMessageDelayAfterMatchMS);

    if (data.leaveMessageOnDetect) {
        telephonyCampaignFinalActionRadios.filter('[value="leave"]').prop('checked', true).change();
        currentTelephonyCampaignVoicemailMessageToLeaveMultiLangData = {
            ...(data.messageToLeave || {})
        };
        telephonyCampaignVoicemailMessageToLeaveTextarea.val(currentTelephonyCampaignVoicemailMessageToLeaveMultiLangData[BusinessDefaultLanguage] || "");
    } else {
        telephonyCampaignFinalActionRadios.filter('[value="end"]').prop('checked', true).change();
    }
}

// Post Analysis Tab
function createTelephonyCampaignPostAnalysisContextVariableElement(id, data = null) {
    return `
        <div class="input-group mt-1 campaign-post-analysis-context-variable" data-id="${id}">
          <div class="d-flex flex-column" style="width: calc(100% - 41px);">
            <div class="input-group">
                <input type="text" class="form-control campaign-post-analysis-context-variable-name" placeholder="Name" data-type="variable-name" style="max-width: 30%;" value="${data ? data.name : ""}">
                <input type="text" class="form-control campaign-post-analysis-context-variable-description" placeholder="Description" data-type="variable-description" style="max-width: 70%;" value="${data ? data.description : ""}">
            </div>
            <div class="variable-input-container"></div>
          </div>
          <button class="btn btn-danger" type="button" button-type="remove-variable"><i class="fa-regular fa-trash"></i></button>
        </div>
    `;
}

function initTelephonyCampaignPostAnalysisEventHandlers() {
    telephonyCampaignPostAnalysisTemplateSelect.on('change', (e) => {
        const currentElement = $(e.currentTarget);

        const currentSelectedOption = currentElement.find('option:selected');
        const currentValue = currentSelectedOption.val();

        if (!currentValue || currentValue == "") {
            addTelephonyCampaignPostAnalysisVariable.prop("disabled", true);
            Object.keys(telephonyCampaignPostAnalysisContextVariablesCustomInput).forEach((customInputId) => {
                telephonyCampaignPostAnalysisContextVariablesCustomInput[customInputId].destroy();
            });;
            telephonyCampaignPostAnalysisVariablesList.empty();
        }
        else {
            addTelephonyCampaignPostAnalysisVariable.prop("disabled", false);
        }

        checkTelephonyCampaignChanges();
        validateTelephonyCampaign(true);
    });

    addTelephonyCampaignPostAnalysisVariable.on('click', (event) => {
        event.preventDefault();

        const uniqueId = crypto.randomUUID();

        const contextVariableElement = $(createTelephonyCampaignPostAnalysisContextVariableElement(uniqueId, null));
        telephonyCampaignPostAnalysisVariablesList.append(contextVariableElement);

        const customInput = new CustomVariableInput(
            $(contextVariableElement.find('.variable-input-container')[0]),
            telephonyCampaignPostAnalysisContextVariableArguments,
            {
                placeholder: "Enter information or {={variable}=} for post analysis context...",
                onValueChange: () => {
                    checkTelephonyCampaignChanges();
                    validateTelephonyCampaign(true);
                }
            }
        );

        telephonyCampaignPostAnalysisContextVariablesCustomInput[uniqueId] = customInput;

        checkTelephonyCampaignChanges();
        validateTelephonyCampaign(true);
    });

    telephonyCampaignPostAnalysisVariablesList.on('click', '.btn[button-type="remove-variable"]', (event) => {
        event.preventDefault();
        event.stopPropagation();

        const currentElement = $(event.currentTarget);
        const parentContainer = currentElement.closest('.campaign-post-analysis-context-variable');
        const parentId = parentContainer.attr('data-id');

        telephonyCampaignPostAnalysisContextVariablesCustomInput[parentId].destroy();
        delete telephonyCampaignPostAnalysisContextVariablesCustomInput[parentId];

        parentContainer.remove();

        checkTelephonyCampaignChanges();
        validateTelephonyCampaign(true);
    });
}

// Actions Tab Helpers
function handleTelephonyCampaignActionToolChange(event) {
    const selectElement = $(event.currentTarget);
    const selectedToolId = selectElement.val();
    const container = selectElement.closest('div'); // This is the parent div.mb-3
    const argumentsContainer = container.find('.custom-tool-input-arguments');
    const argumentsSelect = argumentsContainer.find('select');
    const argumentsList = argumentsContainer.find('[id$="-arguments-list"]');

    // Reset the arguments section
    argumentsList.empty();
    argumentsSelect.empty().append('<option value="" disabled selected>Add Input Argument</option>');

    if (selectedToolId === 'none') {
        argumentsContainer.addClass('d-none');
    } else {
        argumentsContainer.removeClass('d-none');
        const toolData = BusinessFullData.businessApp.tools.find(tool => tool.id === selectedToolId);
        if (toolData && toolData.configuration.inputSchemea) {
            toolData.configuration.inputSchemea.forEach(inputArgument => {
                argumentsSelect.append(`<option value="${inputArgument.id}">${inputArgument.name[BusinessDefaultLanguage]}${inputArgument.isRequired ? "*" : ""}</option>`);
            });
        }
    }
    checkTelephonyCampaignChanges();
    validateTelephonyCampaign(true);
}

function createTelephonyCampaignActionArgumentListElement(argumentData) {
    return `
            <div class="input-group mb-1">
                <span class="input-group-text">${argumentData.name[BusinessDefaultLanguage]}${argumentData.isRequired ? "*" : ""}</span>
                <input type="text" class="form-control" input_arguement="${argumentData.id}" placeholder="Enter ${argumentData.type.name} value" value="">
                <button class="btn btn-danger" btn-action="remove-campaign-action-tool-argument" input_arguement="${argumentData.id}">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </div>
        `;
}

function handleTelephonyCampaignActionAddArgument(event) {
    const selectElement = $(event.currentTarget);
    const selectedArgumentId = selectElement.val();
    if (!selectedArgumentId) return;

    const container = selectElement.closest('.custom-tool-input-arguments');
    const mainToolSelect = container.parent().find('select').first(); // Go up to parent div and find main tool select
    const selectedToolId = mainToolSelect.val();
    const argumentsList = container.find('[id$="-arguments-list"]');

    const toolData = BusinessFullData.businessApp.tools.find(tool => tool.id === selectedToolId);
    const argumentData = toolData.configuration.inputSchemea.find(arg => arg.id === selectedArgumentId);

    if (argumentData) {
        argumentsList.append(createTelephonyCampaignActionArgumentListElement(argumentData));
        selectElement.find(`option[value="${selectedArgumentId}"]`).remove();
        selectElement.val("");
    }
    checkTelephonyCampaignChanges();
    validateTelephonyCampaign(true);
}

function handleTelephonyCampaignActionRemoveArgument(event) {
    event.preventDefault();
    const removeButton = $(event.currentTarget);
    const argumentIdToRemove = removeButton.attr('input_arguement');
    const inputGroup = removeButton.closest('.input-group');
    const container = removeButton.closest('.custom-tool-input-arguments');
    const mainToolSelect = container.parent().parent().find('select').first();
    const argumentsSelect = container.find('select');
    const selectedToolId = mainToolSelect.val();

    const toolData = BusinessFullData.businessApp.tools.find(tool => tool.id === selectedToolId);
    const argumentData = toolData.configuration.inputSchemea.find(arg => arg.id === argumentIdToRemove);

    if (argumentData) {
        argumentsSelect.append(`<option value="${argumentData.id}">${argumentData.name[BusinessDefaultLanguage]}${argumentData.isRequired ? "*" : ""}</option>`);
    }

    inputGroup.remove();
    checkTelephonyCampaignChanges();
    validateTelephonyCampaign(true);
}

/** EVENT HANDLER INITIALIZERS **/
function initTelephonyAgentEventHandlers() {
    const selectAgentButton = telephonyCampaignsManagerView.find('button[data-bs-target="#telephony-campaign-select-agent-modal"]');

    selectAgentButton.on('click', () => {
        telephonyCampaignsManagerSelectAgentModalList.empty();
        const listGroup = $('<div class="list-group"></div>');
        BusinessFullData.businessApp.agents.forEach(agent => {
            const element = $(createTelephonyCampaignAgentModalListElement(agent));
            if (agent.id === currentTelephonyCampaignAgentSelectedId) {
                element.addClass('active');
            }
            listGroup.append(element);
        });
        telephonyCampaignsManagerSelectAgentModalList.append(listGroup);
        telephonyCampaignSaveAgentButton.prop('disabled', true);
    });

    telephonyCampaignsManagerSelectAgentModalList.on("click", "button", (event) => {
        event.preventDefault();
        const clickedButton = $(event.currentTarget);
        if (clickedButton.hasClass("active")) return;
        telephonyCampaignsManagerSelectAgentModalList.find("button.active").removeClass("active");
        clickedButton.addClass("active");
        const selectedAgentId = clickedButton.data("agent-id");
        telephonyCampaignSaveAgentButton.prop("disabled", selectedAgentId === currentTelephonyCampaignAgentSelectedId);
    });

    telephonyCampaignSaveAgentButton.on("click", (event) => {
        event.preventDefault();
        const selectedAgentButton = telephonyCampaignsManagerSelectAgentModalList.find("button.active");
        if (selectedAgentButton.length === 0) return;

        const newAgentId = selectedAgentButton.data("agent-id");
        if (newAgentId === currentTelephonyCampaignAgentSelectedId) return;

        currentTelephonyCampaignAgentSelectedId = newAgentId;
        const agentData = BusinessFullData.businessApp.agents.find(agent => agent.id === newAgentId);

        telephonyCampaignAgentIconSpan.text(agentData.general.emoji);
        telephonyCampaignAgentNameInput.val(agentData.general.name[BusinessDefaultLanguage]);

        telephonyCampaignAgentScriptSelect.prop("disabled", false).empty();
        telephonyCampaignAgentScriptSelect.append(`<option value="" disabled selected>Select Script</option>`);
        agentData.scripts.forEach(script => {
            telephonyCampaignAgentScriptSelect.append(`<option value="${script.id}">${script.general.name[BusinessDefaultLanguage]}</option>`);
        });

        telephonyCampaignSelectAgentModal.hide();
        checkTelephonyCampaignChanges();
        validateTelephonyCampaign(true);
    });
}

function initTelephonyNumbersEventHandlers() {
    const saveChangeCampaignNumberButton = telephonyCampaignChangeNumberModalElement.find("#telephony-campaign-save-number-button");
    const campaignRegionSelect = telephonyCampaignAddRegionModalElement.find('.modal-body'); // Assuming a select will be here
    const confirmRegionSelectionButton = telephonyCampaignAddRegionModalElement.find('#telephony-campaign-confirm-region-button');

    addTelephonyCampaignRegionRouteButton.on("click", (event) => {
        event.preventDefault();
        const campaignRegionSelectElement = $('<select class="form-select" id="temp-region-select"></select>');
        campaignRegionSelectElement.append('<option value="" disabled selected>Select a country...</option>');
        confirmRegionSelectionButton.prop('disabled', true);

        const existingRegions = Object.keys(currentTelephonyCampaignRouteNumberList);
        Object.keys(CountriesList).forEach(countryCode => {
            if (!existingRegions.includes(countryCode)) {
                const countryData = CountriesList[countryCode];
                campaignRegionSelectElement.append(`<option value="${countryCode}">${countryData.Country} ${countryData.phone_code}</option>`);
            }
        });
        campaignRegionSelect.empty().append(campaignRegionSelectElement);
        telephonyCampaignAddRegionModal.show();
    });

    campaignRegionSelect.on('change', 'select', function () {
        confirmRegionSelectionButton.prop('disabled', !$(this).val());
    });

    confirmRegionSelectionButton.on('click', function (event) {
        event.preventDefault();
        const selectedRegion = campaignRegionSelect.find('select').val();
        if (!selectedRegion) return;

        telephonyCampaignAddRegionModal.hide();
        telephonyCampaignChangeNumberModalElement.data('context-region-code', selectedRegion);
        populateTelephonyNumberSelectionModal();
        saveChangeCampaignNumberButton.prop('disabled', true);
        telephonyCampaignChangeNumberModal.show();
    });

    telephonyCampaignChangeNumberModalElement.on("click", ".list-group-item-action", (event) => {
        event.preventDefault();
        const currentElement = $(event.currentTarget);
        if (currentElement.hasClass('active')) return;
        telephonyCampaignChangeNumberModalElement.find('.active').removeClass('active');
        currentElement.addClass("active");
        saveChangeCampaignNumberButton.prop("disabled", false);
    });

    saveChangeCampaignNumberButton.on("click", (event) => {
        event.preventDefault();
        const selectedNumberButton = telephonyCampaignChangeNumberModalElement.find(".list-group-item-action.active");
        if (selectedNumberButton.length === 0) return;

        const numberId = selectedNumberButton.data("number-id");
        const numberData = BusinessFullData.businessApp.numbers.find(n => n.id === numberId);
        const addRegionCode = telephonyCampaignChangeNumberModalElement.data('context-region-code');
        const editRegionCode = telephonyCampaignChangeNumberModalElement.data('context-edit-region-code');

        if (editRegionCode === 'default') {
            currentTelephonyCampaignDefaultNumberId = numberId;
            updateTelephonyDefaultNumberRowUI(numberData);
        } else if (addRegionCode) {
            currentTelephonyCampaignRouteNumberList[addRegionCode] = numberId;
            const newRow = createTelephonyCampaignRegionRowElement(addRegionCode, numberData);
            telephonyCampaignNumbersListTable.find("tbody").append(newRow);
        } else if (editRegionCode) {
            currentTelephonyCampaignRouteNumberList[editRegionCode] = numberId;
            const updatedRow = createTelephonyCampaignRegionRowElement(editRegionCode, numberData);
            telephonyCampaignNumbersListTable.find(`tr[data-region-code="${editRegionCode}"]`).replaceWith(updatedRow);
        }

        telephonyCampaignChangeNumberModalElement.removeData('context-region-code');
        telephonyCampaignChangeNumberModalElement.removeData('context-edit-region-code');
        telephonyCampaignChangeNumberModal.hide();
        checkTelephonyCampaignChanges();
        validateTelephonyCampaign(true);
    });

    telephonyCampaignNumbersListTable.on("click", "[data-action]", (event) => {
        event.preventDefault();
        const button = $(event.currentTarget);
        const action = button.data("action");
        const row = button.closest('tr');
        const regionCode = row.data("region-code");

        if (action === "edit-route") {
            telephonyCampaignChangeNumberModalElement.data('context-edit-region-code', regionCode);
            populateTelephonyNumberSelectionModal();
            saveChangeCampaignNumberButton.prop('disabled', true);
            telephonyCampaignChangeNumberModal.show();
        } else if (action === "delete-route") {
            delete currentTelephonyCampaignRouteNumberList[regionCode];
            row.remove();
            checkTelephonyCampaignChanges();
        }
    });
}

function initTelephonyConfigurationEventHandlers() {
    telephonyCampaignRetryOnDeclineCheck.on('change', function () {
        const isChecked = $(this).is(':checked');

        if (isChecked) {
            telephonyCampaignRetryOnDeclineOptions.toggleClass('d-none', !isChecked);
            setTimeout(() => {
                telephonyCampaignRetryOnDeclineOptions.toggleClass('show', isChecked);
            }, 10);
        }
        else {
            telephonyCampaignRetryOnDeclineOptions.toggleClass('show', isChecked);
            setTimeout(() => {
                telephonyCampaignRetryOnDeclineOptions.toggleClass('d-none', !isChecked);
            }, 300);
        }
    });
    telephonyCampaignRetryOnMissCheck.on('change', function () {
        const isChecked = $(this).is(':checked');

        if (isChecked) {
            telephonyCampaignRetryOnMissOptions.toggleClass('d-none', !isChecked);
            setTimeout(() => {
                telephonyCampaignRetryOnMissOptions.toggleClass('show', isChecked);
            }, 10);
        }
        else {
            telephonyCampaignRetryOnMissOptions.toggleClass('show', isChecked);
            setTimeout(() => {
                telephonyCampaignRetryOnMissOptions.toggleClass('d-none', !isChecked);
            }, 300);
        }
    });
}

function initTelephonyVoicemailDetectionEventHandlers() {
    telephonyCampaignVoicemailIsEnabledCheck.on('change', function () {
        const isEnabled = $(this).is(':checked');
        telephonyCampaignVoicemailSettingsContainer.css({
            opacity: isEnabled ? 1 : 0.5,
            pointerEvents: isEnabled ? 'auto' : 'none'
        });
        telephonyCampaignVoicemailSettingsContainer.find('input, select, textarea').prop('disabled', !isEnabled);
    });

    telephonyCampaignVoicemailAdvancedVerificationCheck.on('change', function () {
        telephonyCampaignVoicemailAdvancedVerificationContainer.toggleClass('d-none', !$(this).is(':checked'));
    });

    telephonyCampaignFinalActionRadios.on('change', function () {
        telephonyCampaignVoicemailLeaveMessageContainer.toggleClass('d-none', $(this).val() !== 'leave');
    });

    telephonyCampaignVoicemailMessageToLeaveTextarea.on("input", (e) => {
        const currentLang = BusinessDefaultLanguage;
        currentTelephonyCampaignVoicemailMessageToLeaveMultiLangData[currentLang] = $(e.currentTarget).val();
    });
}

function initTelephonyActionsEventHandlers() {
    // Main tool selection change handler
    telephonyCampaignActionToolCallInitiationFailureSelect.on('change', handleTelephonyCampaignActionToolChange);
    telephonyCampaignActionToolCallInitiatedSelect.on('change', handleTelephonyCampaignActionToolChange);
    telephonyCampaignActionToolCallMissedSelect.on('change', handleTelephonyCampaignActionToolChange);
    telephonyCampaignActionToolCallDeclinedSelect.on('change', handleTelephonyCampaignActionToolChange);
    telephonyCampaignActionToolCallAnsweredSelect.on('change', handleTelephonyCampaignActionToolChange);
    telephonyCampaignActionToolCallEndedSelect.on('change', handleTelephonyCampaignActionToolChange);

    // Add argument dropdown change handler (uses event delegation on the tab content)
    telephonyCampaignActionsTab.on('change', '.custom-tool-input-arguments > select', handleTelephonyCampaignActionAddArgument);

    // Remove argument button click handler
    telephonyCampaignActionsTab.on('click', '[btn-action="remove-campaign-action-tool-argument"]', handleTelephonyCampaignActionRemoveArgument);
}

/** INIT **/
function initTelephonyCampaignsTab() {
    $(document).ready(() => {
        /** INIT MODALS **/
        telephonyCampaignSelectAgentModal = new bootstrap.Modal(telephonyCampaignSelectAgentModalElement);
        telephonyCampaignChangeNumberModal = new bootstrap.Modal(telephonyCampaignChangeNumberModalElement);
        telephonyCampaignAddRegionModal = new bootstrap.Modal(telephonyCampaignAddRegionModalElement);

        /** INIT EMOJI PICKER **/
        new EmojiPicker({
            trigger: [{
                selector: "#telephony-campaign-icon-input",
                insertInto: "#telephony-campaign-icon-input"
            }],
            closeButton: true,
            closeOnInsert: true
        });

        /** INIT INTEGRATION MANAGERS **/
        telephonyCampaignVoicemailSTTIntegrationManager = new IntegrationConfigurationManager('#telephony-campaign-voicemail-stt-integration-container', {
            integrationType: 'STT',
            allIntegrations: BusinessFullData.businessApp.integrations,
            providersData: BusinessSTTProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onSaveSuccessful: () => {
                checkTelephonyCampaignChanges();
                validateTelephonyCampaign(true);
            },
            onIntegrationChange: () => {
                checkTelephonyCampaignChanges();
                validateTelephonyCampaign(true);
            },
        });
        telephonyCampaignVoicemailLLMIntegrationManager = new IntegrationConfigurationManager('#telephony-campaign-voicemail-llm-integration-container', {
            integrationType: 'LLM',
            allIntegrations: BusinessFullData.businessApp.integrations,
            providersData: BusinessLLMProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onSaveSuccessful: () => {
                checkTelephonyCampaignChanges();
                validateTelephonyCampaign(true);
            },
            onIntegrationChange: () => {
                checkTelephonyCampaignChanges();
                validateTelephonyCampaign(true);
            },
        });

        /** Event Handlers **/
        $(window).resize(() => {
            SetTelephonyCampaignCardDynamicWidth();
        });

        $(document).on("containerResizeProgress", (event) => {
            SetTelephonyCampaignCardDynamicWidth();
        });

        $(document).on("tabShowing", function (event, data) {
            if (data.tabId === 'telephony-campaigns-tab') {
                handleTelephonyCampaignRouting(data.urlSubPath);
            }
        });

        $(document).on("tabShown", function (event, data) {
            if (data.tabId === 'telephony-campaigns-tab') {
                SetTelephonyCampaignCardDynamicWidth();
            }
        });

        addNewTelephonyCampaignButton.on("click", (e) => {
            e.preventDefault();
            currentTelephonyCampaignData = createDefaultTelephonyCampaignObject();
            telephonyCampaignManagerNameBreadcrumb.text("New Telephony Campaign");
            resetTelephonyCampaignManager();
            manageTelephonyCampaignType = "new";
            showTelephonyCampaignsManagerView();
            updateUrlForTab("telephonycampaigns/new");
        });

        backToTelephonyCampaignsListButton.on("click", async (e) => {
            e.preventDefault();
            if (await canLeaveTelephonyCampaignsManager(" Discard changes?")) {
                showTelephonyCampaignsListView();
                manageTelephonyCampaignType = null;
                updateUrlForTab("telephonycampaigns");
            }
        });

        telephonyCampaignsListContainer.on("click", ".campaign-card", (e) => {
            e.preventDefault();
            const campaignId = $(e.currentTarget).attr("data-campaign-id");
            const campaignData = BusinessFullData.businessApp.telephonyCampaigns.find(c => c.id === campaignId);
            if (!campaignData) return;
            currentTelephonyCampaignData = JSON.parse(JSON.stringify(campaignData)); // Deep copy
            telephonyCampaignManagerNameBreadcrumb.text(currentTelephonyCampaignData.general.name);
            resetTelephonyCampaignManager();
            fillTelephonyCampaignManager();
            manageTelephonyCampaignType = "edit";
            showTelephonyCampaignsManagerView();
            updateUrlForTab(`telephonycampaigns/${campaignId}`);
        });

        telephonyCampaignsManagerView.on('input change', 'input:not(.form-range), select, textarea', () => {
            if (manageTelephonyCampaignType) {
                checkTelephonyCampaignChanges();
                validateTelephonyCampaign(true);
            }
        });
        telephonyCampaignsManagerView.on('mouseup', 'input.form-range', () => { // For range sliders
            if (manageTelephonyCampaignType) {
                checkTelephonyCampaignChanges();
                validateTelephonyCampaign(true);
            }
        });

        saveTelephonyCampaignButton.on("click", async (e) => {
            e.preventDefault();
            if (isSavingTelephonyCampaign) return;
            const validation = validateTelephonyCampaign(false);
            if (!validation.validated) {
                AlertManager.createAlert({
                    type: "danger",
                    message: `Validation failed:<br>${validation.errors.join("<br>")}`
                });
                return;
            }
            const {
                hasChanges,
                changes
            } = checkTelephonyCampaignChanges(false);
            if (!hasChanges) return;
            isSavingTelephonyCampaign = true;
            saveTelephonyCampaignButton.prop("disabled", true);
            const formData = new FormData();
            formData.append("postType", manageTelephonyCampaignType);
            formData.append("changes", JSON.stringify(changes));
            if (manageTelephonyCampaignType === "edit") {
                formData.append("existingTelephonyCampaignId", currentTelephonyCampaignData.id);
            }
            saveTelephonyCampaign(formData,
                (response) => {
                    currentTelephonyCampaignData = response.data;
                    const existingIndex = BusinessFullData.businessApp.telephonyCampaigns.findIndex(c => c.id === response.data.id);
                    if (existingIndex > -1) {
                        BusinessFullData.businessApp.telephonyCampaigns[existingIndex] = response.data;
                    } else {
                        BusinessFullData.businessApp.telephonyCampaigns.push(response.data);
                    }
                    fillTelephonyCampaignsList(); // todo instead of this, update the list item
                    isSavingTelephonyCampaign = false;
                    saveTelephonyCampaignButton.prop("disabled", true);
                    AlertManager.createAlert({
                        type: "success",
                        message: "Campaign saved successfully.",
                        timeout: 3000
                    });
                    manageTelephonyCampaignType = "edit";
                    telephonyCampaignManagerNameBreadcrumb.text(currentTelephonyCampaignData.general.name);
                    updateUrlForTab(`telephonycampaigns/${currentTelephonyCampaignData.id}`);
                },
                (error) => {
                    isSavingTelephonyCampaign = false;
                    saveTelephonyCampaignButton.prop("disabled", false);
                    AlertManager.createAlert({
                        type: "danger",
                        message: "Failed to save campaign. Check console logs for more details.",
                        timeout: 3000
                    });
                    console.error("Failed to save campaign:", error);
                }
            );
        });

        // Init All Handlers
        initTelephonyConfigurationEventHandlers();
        initTelephonyAgentEventHandlers();
        initTelephonyNumbersEventHandlers();
        initTelephonyVoicemailDetectionEventHandlers();
        initTelephonyCampaignVariablesEventHandlers();
        initTelephonyCampaignPostAnalysisEventHandlers();
        initTelephonyActionsEventHandlers();

        // Initial population
        fillTelephonyCampaignsList();
    });
}