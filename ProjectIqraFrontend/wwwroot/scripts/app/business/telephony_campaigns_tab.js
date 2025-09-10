/** Dynamic Variables **/
let manageTelephonyCampaignType = null; // 'new' or 'edit'
let currentTelephonyCampaignData = null;

let currentTelephonyCampaignRouteNumberList = {};
let currentTelephonyCampaignDefaultNumberId = "";
let currentTelephonyCampaignAgentSelectedId = "";

let isSavingTelephonyCampaign = false;

// Integration Managers for Voicemail Detection
let telephonyCampaignVoicemailSTTIntegrationManager = null;
let telephonyCampaignVoicemailLLMIntegrationManager = null;
let currentTelephonyCampaignVoicemailMessageToLeaveMultiLangData = {};

const telephonyCampaignsTooltipTriggerList = document.querySelectorAll('#telephony-campaigns-tab [data-bs-toggle="tooltip"]');
[...telephonyCampaignsTooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

/** Element Variables **/
const telephonyCampaignsTab = $("#telephony-campaigns-tab");

// List View Elements
const telephonyCampaignsListView = telephonyCampaignsTab.find("#telephony-campaigns-list-view");
const addNewTelephonyCampaignButton = telephonyCampaignsListView.find("#add-new-telephony-campaign-button");
const telephonyCampaignsListContainer = telephonyCampaignsListView.find("#telephony-campaigns-list-container");

// Manager View Elements
const telephonyCampaignsManagerView = telephonyCampaignsTab.find("#telephony-campaigns-manager-view");
const telephonyCampaignManagerNameBreadcrumb = telephonyCampaignsManagerView.find("#telephony-campaign-manager-name-breadcrumb");
const backToTelephonyCampaignsListButton = telephonyCampaignsManagerView.find("#back-to-telephony-campaigns-list");
const saveTelephonyCampaignButton = telephonyCampaignsManagerView.find("#save-telephony-campaign-button");

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
const telephonyCampaignVoicemailMLCheckDurationMSRange = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-ml-check-duration-ms-range");
const telephonyCampaignVoicemailMLCheckDurationMSValue = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-ml-check-duration-ms-value");
const telephonyCampaignVoicemailMaxMLCheckTriesInput = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-max-ml-check-tries-input");
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
const telephonyCampaignVoicemailWaitAfterMessageInput = telephonyCampaignsManagerView.find("#telephony-campaign-voicemail-wait-after-message-input");

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
    setTimeout(() => {
        telephonyCampaignsManagerView.addClass("d-none");
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
        telephonyCampaignsManagerView.removeClass("d-none");
        setTimeout(() => {
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
            <div class="campaign-card d-flex flex-column align-items-start justify-content-center" data-campaign-id="${campaignData.id}">
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
            mlCheckDurationMS: 1000,
            maxMLCheckTries: 2,
            waitForVADSpeechForMLCheck: true,
            voiceMailMessageVADSilenceThresholdMS: 1000,
            voiceMailMessageVADMaxSpeechDurationMS: 4000,
            onVoiceMailMessageDetectVerifySTTAndLLM: false,
            transcribeVoiceMessageSTT: null,
            verifyVoiceMessageLLM: null,
            stopSpeakingAgentAfterXMlCheckSuccess: true,
            stopSpeakingAgentAfterVadSilence: false,
            stopSpeakingAgentAfterLLMConfirm: false,
            stopSpeakingAgentDelayAfterMatchMS: 1000,
            endOrLeaveMessageAfterXMLCheckSuccess: true,
            endOrLeaveMessageAfterVadSilence: false,
            endOrLeaveMessageAfterLLMConfirm: false,
            endOrLeaveMessageDelayAfterMatchMS: 1000,
            endCallOnDetect: true,
            leaveMessageOnDetect: false,
            messageToLeave: {},
            waitXMSAfterLeavingMessageToEndCall: 1000
        },
        numberRoute: {
            routeNumberList: {},
            defaultNumberId: ""
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

function resetTelephonyManager() {
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
    telephonyCampaignRetryDeclineUnitSelect.val(1); // Assuming 1 is minutes TODO

    telephonyCampaignRetryOnMissCheck.prop("checked", false).change();
    telephonyCampaignRetryMissCountInput.val(3);
    telephonyCampaignRetryMissDelayInput.val(10);
    telephonyCampaignRetryMissUnitSelect.val(1); // Assuming 1 is minutes TODO

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
        container.find('[id$="InputArgumentsList"]').empty();
    });

    // Reset state
    $("#telephony-campaign-manager-general-tab").click();
    saveTelephonyCampaignButton.prop("disabled", true);
    currentTelephonyCampaignAgentSelectedId = "";
}

function fillTelephonyManager() {
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

    // Actions
    function fillTelephonyActionTool(toolData, selectElement) {
        const container = selectElement.closest('div');
        const argumentsContainer = container.find('.custom-tool-input-arguments');
        const argumentsList = argumentsContainer.find('[id$="InputArgumentsList"]');
        selectElement.val("none");
        argumentsList.empty();
        argumentsContainer.addClass('d-none');
        if (toolData && toolData.toolId) {
            selectElement.val(toolData.toolId).change(); // Trigger change to show arguments
            if (toolData.arguments) {
                Object.entries(toolData.arguments).forEach(([argId, value]) => {
                    argumentsList.find(`input[input_arguement="${argId}"]`).val(value);
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
        changes.agent = {
            selectedAgentId: currentTelephonyCampaignAgentSelectedId,
            openingScriptId: telephonyCampaignAgentScriptSelect.val() || "",
            language: telephonyCampaignAgentLanguageSelect.val(),
            timezones: telephonyCampaignAgentTimezoneSelect.val() ? [telephonyCampaignAgentTimezoneSelect.val()] : [],
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
            changes.voicemailDetection.mlCheckDurationMS = parseInt(telephonyCampaignVoicemailMLCheckDurationMSRange.val(), 10);
            changes.voicemailDetection.maxMLCheckTries = parseInt(telephonyCampaignVoicemailMaxMLCheckTriesInput.val(), 10);
            changes.voicemailDetection.voiceMailMessageVADSilenceThresholdMS = parseInt(telephonyCampaignVoicemailVADSilenceThresholdMSInput.val(), 10);
            changes.voicemailDetection.voiceMailMessageVADMaxSpeechDurationMS = parseInt(telephonyCampaignVoicemailVADMaxSpeechDurationMSInput.val(), 10);
            changes.voicemailDetection.onVoiceMailMessageDetectVerifySTTAndLLM = telephonyCampaignVoicemailAdvancedVerificationCheck.is(":checked");
            if (changes.voicemailDetection.onVoiceMailMessageDetectVerifySTTAndLLM) {
                changes.voicemailDetection.transcribeVoiceMessageSTT = telephonyCampaignVoicemailSTTIntegrationManager.getData();
                changes.voicemailDetection.verifyVoiceMessageLLM = telephonyCampaignVoicemailLLMIntegrationManager.getData();
            }
            changes.voicemailDetection.stopSpeakingAgentAfterXMlCheckSuccess = telephonyCampaignStopAgentOnMLCheck.is(':checked');
            changes.voicemailDetection.stopSpeakingAgentAfterVadSilence = telephonyCampaignStopAgentOnVADCheck.is(':checked');
            changes.voicemailDetection.stopSpeakingAgentAfterLLMConfirm = telephonyCampaignStopAgentOnLLMCheck.is(':checked');
            changes.voicemailDetection.stopSpeakingAgentDelayAfterMatchMS = parseInt(telephonyCampaignVoicemailStopSpeakingDelayInput.val(), 10);
            changes.voicemailDetection.endOrLeaveMessageAfterXMLCheckSuccess = telephonyCampaignEndLeaveOnMLCheck.is(':checked');
            changes.voicemailDetection.endOrLeaveMessageAfterVadSilence = telephonyCampaignEndLeaveOnVADCheck.is(':checked');
            changes.voicemailDetection.endOrLeaveMessageAfterLLMConfirm = telephonyCampaignEndLeaveOnLLMCheck.is(':checked');
            changes.voicemailDetection.endOrLeaveMessageDelayAfterMatchMS = parseInt(telephonyCampaignVoicemailEndLeaveDelayInput.val(), 10);
            const finalAction = telephonyCampaignFinalActionRadios.filter(":checked").val();
            changes.voicemailDetection.endCallOnDetect = finalAction === 'end';
            changes.voicemailDetection.leaveMessageOnDetect = finalAction === 'leave';
            if (changes.voicemailDetection.leaveMessageOnDetect) {
                changes.voicemailDetection.waitXMSAfterLeavingMessageToEndCall = parseInt(telephonyCampaignVoicemailWaitAfterMessageInput.val(), 10);
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

    function checkActionsTab() {
        function collectToolArguments(selectElement) {
            const args = {};
            const argumentsList = selectElement.closest('div').find('.custom-tool-input-arguments [id$="InputArgumentsList"]');
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
    if (!subPath || subPath.length === 0) {
        showTelephonyCampaignsListView();
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

/** HELPER FUNCTIONS **/

// -- Agent Tab Helpers --
function createTelephonyCampaignAgentModalListElement(agentData) {
    return `<button type="button" class="list-group-item list-group-item-action" data-agent-id="${agentData.id}"><span>${agentData.general.emoji} ${agentData.general.name[BusinessDefaultLanguage]}</span></button>`;
}

// -- Numbers Tab Helpers --
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

// -- Voicemail Tab Helpers --
function fillTelephonyCampaignVoicemailTab() {
    const data = currentTelephonyCampaignData.voicemailDetection;

    telephonyCampaignVoicemailIsEnabledCheck.prop('checked', data.isEnabled).change();
    telephonyCampaignVoicemailInitialCheckDelayMSInput.val(data.initialCheckDelayMS);
    telephonyCampaignVoicemailMLCheckDurationMSRange.val(data.mlCheckDurationMS).trigger('input');
    telephonyCampaignVoicemailMaxMLCheckTriesInput.val(data.maxMLCheckTries);
    telephonyCampaignVoicemailVADSilenceThresholdMSInput.val(data.voiceMailMessageVADSilenceThresholdMS);
    telephonyCampaignVoicemailVADMaxSpeechDurationMSInput.val(data.voiceMailMessageVADMaxSpeechDurationMS);

    telephonyCampaignVoicemailAdvancedVerificationCheck.prop('checked', data.onVoiceMailMessageDetectVerifySTTAndLLM).change();
    if (telephonyCampaignVoicemailSTTIntegrationManager) telephonyCampaignVoicemailSTTIntegrationManager.load(data.transcribeVoiceMessageSTT);
    if (telephonyCampaignVoicemailLLMIntegrationManager) telephonyCampaignVoicemailLLMIntegrationManager.load(data.verifyVoiceMessageLLM);

    telephonyCampaignStopAgentOnMLCheck.prop('checked', data.stopSpeakingAgentAfterXMlCheckSuccess);
    telephonyCampaignStopAgentOnVADCheck.prop('checked', data.stopSpeakingAgentAfterVadSilence);
    telephonyCampaignStopAgentOnLLMCheck.prop('checked', data.stopSpeakingAgentAfterLLMConfirm);
    telephonyCampaignVoicemailStopSpeakingDelayInput.val(data.stopSpeakingAgentDelayAfterMatchMS);

    telephonyCampaignEndLeaveOnMLCheck.prop('checked', data.endOrLeaveMessageAfterXMLCheckSuccess);
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
    telephonyCampaignVoicemailWaitAfterMessageInput.val(data.waitXMSAfterLeavingMessageToEndCall);
}

// -- Actions Tab Helpers --
function handleTelephonyCampaignActionToolChange(event) {
    const selectElement = $(event.currentTarget);
    const selectedToolId = selectElement.val();
    const container = selectElement.closest('div');
    const argumentsContainer = container.find('.custom-tool-input-arguments');
    const argumentsSelect = argumentsContainer.find('select');
    const argumentsList = argumentsContainer.find('[id$="InputArgumentsList"]');

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

function handleTelephonyCampaignActionAddArgument(event) {
    const selectElement = $(event.currentTarget);
    const selectedArgumentId = selectElement.val();
    if (!selectedArgumentId) return;

    const container = selectElement.closest('.custom-tool-input-arguments');
    const mainToolSelect = container.closest('div').find('select').first();
    const selectedToolId = mainToolSelect.val();
    const argumentsList = container.find('[id$="InputArgumentsList"]');

    const toolData = BusinessFullData.businessApp.tools.find(tool => tool.id === selectedToolId);
    const argumentData = toolData.configuration.inputSchemea.find(arg => arg.id === selectedArgumentId);

    if (argumentData) {
        argumentsList.append(`
            <div class="input-group mb-1">
                <span class="input-group-text">${argumentData.name[BusinessDefaultLanguage]}${argumentData.isRequired ? "*" : ""}</span>
                <input type="text" class="form-control" input_arguement="${argumentData.id}" placeholder="Enter ${argumentData.type.name} value" value="">
                <button class="btn btn-danger" btn-action="remove-campaign-action-tool-argument" input_arguement="${argumentData.id}">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </div>
        `);
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
    const mainToolSelect = container.closest('div').find('select').first();
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
);

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
        telephonyCampaignRetryOnDeclineOptions.toggleClass('show', $(this).is(':checked'));
    });
    telephonyCampaignRetryOnMissCheck.on('change', function () {
        telephonyCampaignRetryOnMissOptions.toggleClass('show', $(this).is(':checked'));
    });
}

function initTelephonyVoicemailDetectionEventHandlers() {
    telephonyCampaignVoicemailIsEnabledCheck.on('change', function () {
        const isEnabled = $(this).is(':checked');
        telephonyCampaignVoicemailSettingsContainer.toggleClass('disabled-container', !isEnabled);
    });

    telephonyCampaignVoicemailAdvancedVerificationCheck.on('change', function () {
        telephonyCampaignVoicemailAdvancedVerificationContainer.toggleClass('d-none', !$(this).is(':checked'));
    });

    telephonyCampaignFinalActionRadios.on('change', function () {
        telephonyCampaignVoicemailLeaveMessageContainer.toggleClass('d-none', $(this).val() !== 'leave');
    });

    telephonyCampaignVoicemailMLCheckDurationMSRange.on('input', function () {
        telephonyCampaignVoicemailMLCheckDurationMSValue.text($(this).val());
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

    // Add argument dropdown change handler
    telephonyCampaignsManagerView.on('change', '.custom-tool-input-arguments > select', handleTelephonyCampaignActionAddArgument);

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
        $(document).on("tabShown", function (event, data) {
            if (data.tabId === 'telephonycampaigns') {
                handleTelephonyCampaignRouting(data.urlSubPath);
            }
        });

        addNewTelephonyCampaignButton.on("click", (e) => {
            e.preventDefault();
            currentTelephonyCampaignData = createDefaultTelephonyCampaignObject();
            telephonyCampaignManagerNameBreadcrumb.text("New Telephony Campaign");
            resetTelephonyManager();
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
            resetTelephonyManager();
            fillTelephonyManager();
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
                formData.append("existingCampaignId", currentTelephonyCampaignData.id);
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
                    fillTelephonyCampaignsList();
                    isSavingTelephonyCampaign = false;
                    saveTelephonyCampaignButton.prop("disabled", true);
                    AlertManager.createAlert({
                        type: "success",
                        message: "Campaign saved successfully."
                    });
                    manageTelephonyCampaignType = "edit";
                    updateUrlForTab(`telephonycampaigns/${currentTelephonyCampaignData.id}`);
                },
                (error) => {
                    isSavingTelephonyCampaign = false;
                    saveTelephonyCampaignButton.prop("disabled", false);
                    AlertManager.createAlert({
                        type: "danger",
                        message: "Failed to save campaign."
                    });
                }
            );
        });

        // Init All Handlers
        initTelephonyConfigurationEventHandlers();
        initTelephonyVoicemailDetectionEventHandlers();
        initTelephonyAgentEventHandlers();
        initTelephonyNumbersEventHandlers();
        initTelephonyActionsEventHandlers();

        // Initial population
        fillTelephonyCampaignsList();
    });
}