/** Dynamic Variables **/
let ManageCampaignType = null; // 'new' or 'edit'
let ManageCurrentCampaignData = null;

let currentCampaignNumbersList = []; // Default 'call from' numbers
let currentCampaignAgentSelectedId = "";

let IsSavingCampaignManageTab = false;

// Integration Managers for Voicemail Detection
let campaignVoicemailSTTIntegrationManager = null;
let campaignVoicemailLLMIntegrationManager = null;
let CurrentCampaignVoicemailMessageToLeaveMultiLangData = {};

/** Element Variables **/
const campaignsTab = $("#campaigns-tab");
const campaignsHeader = campaignsTab.find("#campaigns-header");
const campaignsListTab = campaignsTab.find("#campaignsListTab");
const addNewCampaignButton = campaignsListTab.find("#addNewCampaignButton");
const campaignsListTable = campaignsListTab.find("#campaignsListTable");
const currentCampaignName = campaignsHeader.find("#currentCampaignName");
const switchBackToCampaignsTabButton = campaignsHeader.find("#switchBackToCampaignsTab");
const saveCampaignButton = campaignsHeader.find("#saveCampaignButton");
const campaignManagerTab = campaignsTab.find("#campaignManagerTab");

// General Tab
const editCampaignIconInput = campaignManagerTab.find("#editCampaignIconInput");
const editCampaignNameInput = campaignManagerTab.find("#editCampaignNameInput");
const editCampaignDescriptionInput = campaignManagerTab.find("#editCampaignDescriptionInput");

// Agent Tab
const editSelectedCampaignAgentIcon = campaignManagerTab.find("#editSelectedCampaignAgentIcon");
const editSelectedCampaignAgentName = campaignManagerTab.find("#editSelectedCampaignAgentName");
const editCampaignAgentDefaultScriptSelect = campaignManagerTab.find("#editCampaignAgentDefaultScriptSelect");
const editCampaignAgentLanguageSelect = campaignManagerTab.find("#editCampaignAgentLanguageSelect");
const editCampaignAgentInterruptionTypeSelect = campaignManagerTab.find("#editCampaignAgentInterruptionTypeSelect");
const editCampaignNumberTimezoneSelect = campaignManagerTab.find("#editCampaignNumberTimezoneSelect");
const editCampaignAgentFromNumberInContextCheck = campaignManagerTab.find("#editCampaignAgentFromNumberInContextCheck");
const editCampaignAgentToNumberInContextCheck = campaignManagerTab.find("#editCampaignAgentToNumberInContextCheck");

// Numbers Tab
const editChangeCampaignNumberButton = campaignManagerTab.find("#editChangeCampaignNumberButton");
const campaignNumbersListTable = campaignManagerTab.find("#campaignNumbersList");

// Configuration Tab
const editCampaignRetryOnDeclineCheck = campaignManagerTab.find("#editCampaignRetryOnDeclineCheck");
const editCampaignRetryOnDeclineOptionsContainer = campaignManagerTab.find("#editCampaignRetryOnDeclineOptionsContainer");
const editCampaignRetryDeclineCountInput = campaignManagerTab.find("#editCampaignRetryDeclineCountInput");
const editCampaignRetryDeclineDelayInput = campaignManagerTab.find("#editCampaignRetryDeclineDelayInput");
const editCampaignRetryDeclineUnitSelect = campaignManagerTab.find("#editCampaignRetryDeclineUnitSelect");
const editCampaignRetryOnMissCheck = campaignManagerTab.find("#editCampaignRetryOnMissCheck");
const editCampaignRetryOnMissOptionsContainer = campaignManagerTab.find("#editCampaignRetryOnMissOptionsContainer");
const editCampaignRetryMissCountInput = campaignManagerTab.find("#editCampaignRetryMissCountInput");
const editCampaignRetryMissDelayInput = campaignManagerTab.find("#editCampaignRetryMissDelayInput");
const editCampaignRetryMissUnitSelect = campaignManagerTab.find("#editCampaignRetryMissUnitSelect");
const editCampaignNumberPickupDelay = campaignManagerTab.find("#editCampaignNumberPickupDelay");
const editCampaignNumberSilenceNotify = campaignManagerTab.find("#editCampaignNumberSilenceNotify");
const editCampaignNumberSilenceEnd = campaignManagerTab.find("#editCampaignNumberSilenceEnd");
const editCampaignNumberTotalCallTime = campaignManagerTab.find("#editCampaignNumberTotalCallTime");

// Voicemail Tab
const editCampaignVoicemailIsEnabled = campaignManagerTab.find("#editCampaignVoicemailIsEnabled");
const editCampaignVoicemailSettingsContainer = campaignManagerTab.find("#editCampaignVoicemailSettingsContainer");
const editCampaignVoicemailInitialCheckDelayMS = campaignManagerTab.find("#editCampaignVoicemailInitialCheckDelayMS");
const editCampaignVoicemailMLCheckDurationMS = campaignManagerTab.find("#editCampaignVoicemailMLCheckDurationMS");
const editCampaignVoicemailMLCheckDurationMSValue = campaignManagerTab.find("#editCampaignVoicemailMLCheckDurationMSValue");
const editCampaignVoicemailMaxMLCheckTries = campaignManagerTab.find("#editCampaignVoicemailMaxMLCheckTries");
const editCampaignVoicemailVADSilenceThresholdMS = campaignManagerTab.find("#editCampaignVoicemailVADSilenceThresholdMS");
const editCampaignVoicemailVADMaxSpeechDurationMS = campaignManagerTab.find("#editCampaignVoicemailVADMaxSpeechDurationMS");
const editCampaignVoicemailEnableAdvancedVerification = campaignManagerTab.find("#editCampaignVoicemailEnableAdvancedVerification");
const editCampaignVoicemailAdvancedVerificationContainer = campaignManagerTab.find("#editCampaignVoicemailAdvancedVerificationContainer");
const editCampaignStopAgentOnML = campaignManagerTab.find("#editCampaignStopAgentOnML");
const editCampaignStopAgentOnVAD = campaignManagerTab.find("#editCampaignStopAgentOnVAD");
const editCampaignStopAgentOnLLM = campaignManagerTab.find("#editCampaignStopAgentOnLLM");
const editCampaignVoicemailStopSpeakingDelay = campaignManagerTab.find("#editCampaignVoicemailStopSpeakingDelay");
const editCampaignEndLeaveOnML = campaignManagerTab.find("#editCampaignEndLeaveOnML");
const editCampaignEndLeaveOnVAD = campaignManagerTab.find("#editCampaignEndLeaveOnVAD");
const editCampaignEndLeaveOnLLM = campaignManagerTab.find("#editCampaignEndLeaveOnLLM");
const editCampaignVoicemailEndLeaveDelay = campaignManagerTab.find("#editCampaignVoicemailEndLeaveDelay");
const editCampaignFinalActionRadios = campaignManagerTab.find('input[name="editCampaignFinalAction"]');
const editCampaignVoicemailLeaveMessageContainer = campaignManagerTab.find("#editCampaignVoicemailLeaveMessageContainer");
const editCampaignVoicemailMessageToLeave = campaignManagerTab.find("#editCampaignVoicemailMessageToLeave");
const editCampaignVoicemailWaitAfterMessage = campaignManagerTab.find("#editCampaignVoicemailWaitAfterMessage");

// Actions Tab
const editCampaignActionToolAnswered = campaignManagerTab.find("#editCampaignActionToolAnswered");
const editCampaignActionToolDeclined = campaignManagerTab.find("#editCampaignActionToolDeclined");
const editCampaignActionToolNoAnswer = campaignManagerTab.find("#editCampaignActionToolNoAnswer");
const editCampaignActionToolEnded = campaignManagerTab.find("#editCampaignActionToolEnded");

// Modals
const editChangeCampaignNumberModalElement = $("#editChangeCampaignNumberModal");
let editChangeCampaignNumberModal = null;
const editChangeCampaignAgentModalElement = $("#editChangeCampaignAgentModal");
let editChangeCampaignAgentModal = null;
const campaignsManagerSelectAgentModalList = editChangeCampaignAgentModalElement.find("#campaigns-manager-select-agent-modal-list");
const saveChangeCampaignAgentButton = editChangeCampaignAgentModalElement.find("#saveChangeCampaignAgentButton");


/** API FUNCTIONS **/
function SaveBusinessCampaign(formData, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/campaigns/save`, // New endpoint
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

/** Functions **/

/** View Switching & List Management **/
function showCampaignsListTab() {
    campaignManagerTab.removeClass("show");
    campaignsHeader.removeClass("show");
    setTimeout(() => {
        campaignManagerTab.addClass("d-none");
        campaignsHeader.addClass("d-none");
        campaignsListTab.removeClass("d-none");
        setTimeout(() => {
            campaignsListTab.addClass("show");
        }, 10);
    }, 300);
}

function showCampaignManagerTab() {
    campaignsListTab.removeClass("show");
    setTimeout(() => {
        campaignsListTab.addClass("d-none");
        campaignManagerTab.removeClass("d-none");
        campaignsHeader.removeClass("d-none");
        setTimeout(() => {
            campaignManagerTab.addClass("show");
            campaignsHeader.addClass("show");
        }, 10);
    }, 300);
}

function createCampaignListElement(campaignData) {
    const agentData = BusinessFullData.businessApp.agents.find((agent) => agent.id === campaignData.agent.selectedAgentId);
    const agentName = agentData ? `Agent: ${agentData.general.emoji} ${agentData.general.name[BusinessDefaultLanguage]}` : 'No Agent Assigned';

    return `
        <div class="col-lg-4 col-md-6 col-12">
            <div class="routing-card d-flex flex-column align-items-start justify-content-center" campaign-id="${campaignData.id}">
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

function fillCampaignsList() {
    const campaigns = BusinessFullData.businessApp.campaigns || [];
    campaignsListTable.empty();
    if (campaigns.length === 0) {
        campaignsListTable.append('<div class="col-12"><h6 class="text-center mt-5">No campaigns created yet...</h6></div>');
    } else {
        campaigns.forEach((campaign) => {
            campaignsListTable.append($(createCampaignListElement(campaign)));
        });
    }
}

/** Data Object & Form Management **/
function createDefaultCampaignObject() {
    return {
        general: {
            emoji: "📣",
            name: "",
            description: ""
        },
        agent: {
            selectedAgentId: "",
            openingScriptId: "",
            language: "",
            interruption: {
                type: {
                    value: 0
                }
            },
            timezones: [],
            fromNumberInContext: true,
            toNumberInContext: true,
        },
        numbers: [], // Array of number IDs for 'call from'
        configuration: {
            retryOnDecline: {
                isEnabled: false,
                retryCount: 3,
                delay: 10,
                unit: 'minutes'
            },
            retryOnMiss: {
                isEnabled: false,
                retryCount: 3,
                delay: 10,
                unit: 'minutes'
            },
            pickupDelayMS: 0,
            notifyOnSilenceMS: 10000,
            endCallOnSilenceMS: 30000,
            maxCallTimeS: 600,
        },
        voicemail: {
            isEnabled: false,
            initialCheckDelayMS: 1000,
            mlCheckDurationMS: 1000,
            maxMLCheckTries: 2,
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
        actions: {
            answeredTool: {
                selectedToolId: null,
                arguments: null
            },
            declinedTool: {
                selectedToolId: null,
                arguments: null
            },
            noAnswerTool: {
                selectedToolId: null,
                arguments: null
            },
            endedTool: {
                selectedToolId: null,
                arguments: null
            },
        },
    };
}

function resetAndEmptyCampaignManagerTab() {
    // General
    editCampaignIconInput.text("📣");
    editCampaignNameInput.val("");
    editCampaignDescriptionInput.val("");

    // Agent
    editSelectedCampaignAgentIcon.text("-");
    editSelectedCampaignAgentName.val("");
    editCampaignAgentDefaultScriptSelect.empty().append('<option value="" disabled selected>Select Agent First</option>').prop("disabled", true);
    editCampaignAgentLanguageSelect.empty().append('<option value="" disabled selected>Select Language</option>');
    BusinessFullData.businessData.languages.forEach(lang => {
        const langData = SpecificationLanguagesListData.find(l => l.id === lang);
        editCampaignAgentLanguageSelect.append(`<option value="${lang}">${lang} | ${langData.name}</option>`);
    });
    editCampaignAgentInterruptionTypeSelect.val("0");
    editCampaignNumberTimezoneSelect.val("");
    editCampaignAgentFromNumberInContextCheck.prop("checked", true);
    editCampaignAgentToNumberInContextCheck.prop("checked", true);

    // Numbers
    campaignNumbersListTable.find("tbody").empty().append('<tr tr-type="none-notice"><td colspan="4">No numbers added yet...</td></tr>');
    currentCampaignNumbersList = [];

    // Configuration
    editCampaignRetryOnDeclineCheck.prop("checked", false).change();
    editCampaignRetryDeclineCountInput.val(3);
    editCampaignRetryDeclineDelayInput.val(10);
    editCampaignRetryDeclineUnitSelect.val('minutes');
    editCampaignRetryOnMissCheck.prop("checked", false).change();
    editCampaignRetryMissCountInput.val(3);
    editCampaignRetryMissDelayInput.val(10);
    editCampaignRetryMissUnitSelect.val('minutes');
    editCampaignNumberPickupDelay.val(0);
    editCampaignNumberSilenceNotify.val(10000);
    editCampaignNumberSilenceEnd.val(30000);
    editCampaignNumberTotalCallTime.val(600);

    // Voicemail
    editCampaignVoicemailIsEnabled.prop('checked', false).change();
    // (The change handler will reset the rest of the form)
    CurrentCampaignVoicemailMessageToLeaveMultiLangData = {};
    if (campaignVoicemailSTTIntegrationManager) campaignVoicemailSTTIntegrationManager.reset();
    if (campaignVoicemailLLMIntegrationManager) campaignVoicemailLLMIntegrationManager.reset();

    // Actions
    const actionSelects = [
        editCampaignActionToolAnswered, editCampaignActionToolDeclined,
        editCampaignActionToolNoAnswer, editCampaignActionToolEnded
    ];

    actionSelects.forEach(select => {
        select.empty().append('<option value="none" selected>None</option>');
        BusinessFullData.businessApp.tools.forEach(tool => {
            select.append(`<option value="${tool.id}">${tool.general.name[BusinessDefaultLanguage]}</option>`);
        });
        // Hide and clear argument containers
        const container = select.closest('[id$="Container"]');
        container.find('.custom-tool-input-arguments').addClass('d-none');
        container.find('[id$="InputArgumentsSelect"]').empty().append('<option value="" disabled selected>Add Input Argument</option>');
        container.find('[id$="InputArgumentsList"]').empty();
    });

    // Reset state
    $("#campaigns-manager-general-tab").click();
    saveCampaignButton.prop("disabled", true);
    currentCampaignAgentSelectedId = "";
}

function fillCampaignManagerTab() {
    const data = ManageCurrentCampaignData;
    // General
    editCampaignIconInput.text(data.general.emoji);
    editCampaignNameInput.val(data.general.name);
    editCampaignDescriptionInput.val(data.general.description);

    // Agent
    if (data.agent.selectedAgentId) {
        const agentData = BusinessFullData.businessApp.agents.find(a => a.id === data.agent.selectedAgentId);
        if (agentData) {
            currentCampaignAgentSelectedId = agentData.id;
            editSelectedCampaignAgentIcon.text(agentData.general.emoji);
            editSelectedCampaignAgentName.val(agentData.general.name[BusinessDefaultLanguage]);
            editCampaignAgentDefaultScriptSelect.prop("disabled", false).empty().append('<option value="" disabled>Select Script</option>');
            agentData.scripts.forEach(script => {
                editCampaignAgentDefaultScriptSelect.append(`<option value="${script.id}">${script.general.name[BusinessDefaultLanguage]}</option>`);
            });
            editCampaignAgentDefaultScriptSelect.val(data.agent.openingScriptId);
        }
    }
    editCampaignAgentLanguageSelect.val(data.agent.language);
    editCampaignAgentInterruptionTypeSelect.val(data.agent.interruption.type.value);
    if (data.agent.timezones && data.agent.timezones.length > 0) editCampaignNumberTimezoneSelect.val(data.agent.timezones[0]);
    editCampaignAgentFromNumberInContextCheck.prop("checked", data.agent.fromNumberInContext);
    editCampaignAgentToNumberInContextCheck.prop("checked", data.agent.toNumberInContext);

    // Numbers
    campaignNumbersListTable.find("tbody").empty();
    data.numbers.forEach(numberId => {
        const numberData = BusinessFullData.businessApp.numbers.find(n => n.id === numberId);
        if (numberData) {
            // Assumes createAddedRouteNumberListElement exists and is adaptable
            // campaignNumbersListTable.find("tbody").append($(createAddedCampaignNumberListElement(numberData)));
        }
    });
    if (data.numbers.length === 0) {
        campaignNumbersListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="4">No numbers added yet...</td></tr>');
    }

    // Configuration
    editCampaignRetryOnDeclineCheck.prop("checked", data.configuration.retryOnDecline.isEnabled).change();
    editCampaignRetryDeclineCountInput.val(data.configuration.retryOnDecline.retryCount);
    editCampaignRetryDeclineDelayInput.val(data.configuration.retryOnDecline.delay);
    editCampaignRetryDeclineUnitSelect.val(data.configuration.retryOnDecline.unit);
    editCampaignRetryOnMissCheck.prop("checked", data.configuration.retryOnMiss.isEnabled).change();
    editCampaignRetryMissCountInput.val(data.configuration.retryOnMiss.retryCount);
    editCampaignRetryMissDelayInput.val(data.configuration.retryOnMiss.delay);
    editCampaignRetryMissUnitSelect.val(data.configuration.retryOnMiss.unit);
    editCampaignNumberPickupDelay.val(data.configuration.pickupDelayMS);
    editCampaignNumberSilenceNotify.val(data.configuration.notifyOnSilenceMS);
    editCampaignNumberSilenceEnd.val(data.configuration.endCallOnSilenceMS);
    editCampaignNumberTotalCallTime.val(data.configuration.maxCallTimeS);

    // Voicemail (using adapted function)
    fillCampaignVoicemailTab();

    // Actions
    function fillCampaignActionTool(toolData, selectElement) {
        const container = selectElement.closest('[id$="Container"]');
        const argumentsContainer = container.find('.custom-tool-input-arguments');
        const argumentsSelect = container.find('[id$="InputArgumentsSelect"]');
        const argumentsList = container.find('[id$="InputArgumentsList"]');

        argumentsContainer.addClass('d-none');
        argumentsSelect.empty().append('<option value="" disabled selected>Add Input Argument</option>');
        argumentsList.empty();
        selectElement.val("none");

        if (toolData && toolData.selectedToolId) {
            selectElement.val(toolData.selectedToolId);
            const tool = BusinessFullData.businessApp.tools.find(t => t.id === toolData.selectedToolId);
            if (tool) {
                argumentsContainer.removeClass('d-none');
                const usedArguments = toolData.arguments ? Object.keys(toolData.arguments) : [];
                tool.configuration.inputSchemea.forEach(arg => {
                    if (!usedArguments.includes(arg.id)) {
                        argumentsSelect.append(`<option value="${arg.id}">${arg.name[BusinessDefaultLanguage]}${arg.isRequired ? "*" : ""}</option>`);
                    }
                });

                if (toolData.arguments) {
                    Object.entries(toolData.arguments).forEach(([argId, value]) => {
                        const argData = tool.configuration.inputSchemea.find(a => a.id === argId);
                        if (argData) {
                            argumentsList.append(`
                                <div class="input-group mb-1">
                                    <span class="input-group-text">${argData.name[BusinessDefaultLanguage]}</span>
                                    <input type="text" class="form-control" input_arguement="${argData.id}" placeholder="Enter ${argData.type.name}" value="${value}">
                                    <button class="btn btn-danger" btn-action="remove-campaign-action-tool-arguement" input_arguement="${argData.id}"><i class="fa-regular fa-trash"></i></button>
                                </div>
                            `);
                        }
                    });
                }
            }
        }
    }

    fillCampaignActionTool(data.actions.answeredTool, editCampaignActionToolAnswered);
    fillCampaignActionTool(data.actions.declinedTool, editCampaignActionToolDeclined);
    fillCampaignActionTool(data.actions.noAnswerTool, editCampaignActionToolNoAnswer);
    fillCampaignActionTool(data.actions.endedTool, editCampaignActionToolEnded);
}

/** Voicemail Specific Functions **/
function fillCampaignVoicemailTab() {
    const data = ManageCurrentCampaignData.voicemail;
    editCampaignVoicemailIsEnabled.prop('checked', data.isEnabled).change();
    editCampaignVoicemailInitialCheckDelayMS.val(data.initialCheckDelayMS);
    editCampaignVoicemailMLCheckDurationMS.val(data.mlCheckDurationMS).trigger('input');
    editCampaignVoicemailMaxMLCheckTries.val(data.maxMLCheckTries);
    editCampaignVoicemailVADSilenceThresholdMS.val(data.voiceMailMessageVADSilenceThresholdMS);
    editCampaignVoicemailVADMaxSpeechDurationMS.val(data.voiceMailMessageVADMaxSpeechDurationMS);
    editCampaignVoicemailEnableAdvancedVerification.prop('checked', data.onVoiceMailMessageDetectVerifySTTAndLLM).change();
    campaignVoicemailSTTIntegrationManager.load(data.transcribeVoiceMessageSTT);
    campaignVoicemailLLMIntegrationManager.load(data.verifyVoiceMessageLLM);
    editCampaignStopAgentOnML.prop('checked', data.stopSpeakingAgentAfterXMlCheckSuccess);
    editCampaignStopAgentOnVAD.prop('checked', data.stopSpeakingAgentAfterVadSilence);
    editCampaignStopAgentOnLLM.prop('checked', data.stopSpeakingAgentAfterLLMConfirm);
    editCampaignVoicemailStopSpeakingDelay.val(data.stopSpeakingAgentDelayAfterMatchMS);
    editCampaignEndLeaveOnML.prop('checked', data.endOrLeaveMessageAfterXMLCheckSuccess);
    editCampaignEndLeaveOnVAD.prop('checked', data.endOrLeaveMessageAfterVadSilence);
    editCampaignEndLeaveOnLLM.prop('checked', data.endOrLeaveMessageAfterLLMConfirm);
    editCampaignVoicemailEndLeaveDelay.val(data.endOrLeaveMessageDelayAfterMatchMS);
    if (data.leaveMessageOnDetect) {
        editCampaignFinalActionRadios.filter('[value="leave"]').prop('checked', true).change();
        CurrentCampaignVoicemailMessageToLeaveMultiLangData = { ...data.messageToLeave };
        editCampaignVoicemailMessageToLeave.val(CurrentCampaignVoicemailMessageToLeaveMultiLangData[BusinessDefaultLanguage] || "");
    } else {
        editCampaignFinalActionRadios.filter('[value="end"]').prop('checked', true).change();
    }
    editCampaignVoicemailWaitAfterMessage.val(data.waitXMSAfterLeavingMessageToEndCall);
}

/** Change Detection & Validation **/
function checkCampaignTabHasChanges(enableDisableButton = true) {
    if (ManageCampaignType === null) return { hasChanges: false };

    const changes = {};
    let hasChanges = false;
    const original = ManageCurrentCampaignData;

    function checkGeneralTab() {
        changes.general = {
            emoji: editCampaignIconInput.text(),
            name: editCampaignNameInput.val().trim(),
            description: editCampaignDescriptionInput.val().trim(),
        };
        if (changes.general.emoji !== original.general.emoji ||
            changes.general.name !== original.general.name ||
            changes.general.description !== original.general.description) {
            hasChanges = true;
        }
    }

    function checkAgentTab() {
        changes.agent = {
            selectedAgentId: currentCampaignAgentSelectedId,
            openingScriptId: editCampaignAgentDefaultScriptSelect.val(),
            language: editCampaignAgentLanguageSelect.val(),
            interruption: { type: { value: parseInt(editCampaignAgentInterruptionTypeSelect.val()) } },
            timezones: editCampaignNumberTimezoneSelect.val() ? [editCampaignNumberTimezoneSelect.val()] : [],
            fromNumberInContext: editCampaignAgentFromNumberInContextCheck.is(":checked"),
            toNumberInContext: editCampaignAgentToNumberInContextCheck.is(":checked"),
        };
        if (changes.agent.selectedAgentId !== original.agent.selectedAgentId ||
            changes.agent.openingScriptId !== original.agent.openingScriptId ||
            changes.agent.language !== original.agent.language ||
            changes.agent.interruption.type.value !== original.agent.interruption.type.value ||
            changes.agent.fromNumberInContext !== original.agent.fromNumberInContext ||
            changes.agent.toNumberInContext !== original.agent.toNumberInContext ||
            JSON.stringify(changes.agent.timezones) !== JSON.stringify(original.agent.timezones)) {
            hasChanges = true;
        }
    }

    function checkNumbersTab() {
        changes.numbers = [...currentCampaignNumbersList];
        if (changes.numbers.length !== original.numbers.length ||
            !changes.numbers.every(num => original.numbers.includes(num))) {
            hasChanges = true;
        }
    }

    function checkConfigurationTab() {
        changes.configuration = {
            retryOnDecline: {
                isEnabled: editCampaignRetryOnDeclineCheck.is(":checked"),
                retryCount: parseInt(editCampaignRetryDeclineCountInput.val()),
                delay: parseInt(editCampaignRetryDeclineDelayInput.val()),
                unit: editCampaignRetryDeclineUnitSelect.val()
            },
            retryOnMiss: {
                isEnabled: editCampaignRetryOnMissCheck.is(":checked"),
                retryCount: parseInt(editCampaignRetryMissCountInput.val()),
                delay: parseInt(editCampaignRetryMissDelayInput.val()),
                unit: editCampaignRetryMissUnitSelect.val()
            },
            pickupDelayMS: parseInt(editCampaignNumberPickupDelay.val()),
            notifyOnSilenceMS: parseInt(editCampaignNumberSilenceNotify.val()),
            endCallOnSilenceMS: parseInt(editCampaignNumberSilenceEnd.val()),
            maxCallTimeS: parseInt(editCampaignNumberTotalCallTime.val()),
        };
        if (JSON.stringify(changes.configuration) !== JSON.stringify(original.configuration)) {
            hasChanges = true; // JSON is acceptable for this self-contained, simple object
        }
    }

    function checkVoicemailTab() {
        changes.voicemail = {
            isEnabled: editCampaignVoicemailIsEnabled.is(':checked'),
            initialCheckDelayMS: parseInt(editCampaignVoicemailInitialCheckDelayMS.val()),
            mlCheckDurationMS: parseInt(editCampaignVoicemailMLCheckDurationMS.val()),
            maxMLCheckTries: parseInt(editCampaignVoicemailMaxMLCheckTries.val()),
            voiceMailMessageVADSilenceThresholdMS: parseInt(editCampaignVoicemailVADSilenceThresholdMS.val()),
            voiceMailMessageVADMaxSpeechDurationMS: parseInt(editCampaignVoicemailVADMaxSpeechDurationMS.val()),
            onVoiceMailMessageDetectVerifySTTAndLLM: editCampaignVoicemailEnableAdvancedVerification.is(':checked'),
            transcribeVoiceMessageSTT: campaignVoicemailSTTIntegrationManager.getData(),
            verifyVoiceMessageLLM: campaignVoicemailLLMIntegrationManager.getData(),
            stopSpeakingAgentAfterXMlCheckSuccess: editCampaignStopAgentOnML.is(':checked'),
            stopSpeakingAgentAfterVadSilence: editCampaignStopAgentOnVAD.is(':checked'),
            stopSpeakingAgentAfterLLMConfirm: editCampaignStopAgentOnLLM.is(':checked'),
            stopSpeakingAgentDelayAfterMatchMS: parseInt(editCampaignVoicemailStopSpeakingDelay.val()),
            endOrLeaveMessageAfterXMLCheckSuccess: editCampaignEndLeaveOnML.is(':checked'),
            endOrLeaveMessageAfterVadSilence: editCampaignEndLeaveOnVAD.is(':checked'),
            endOrLeaveMessageAfterLLMConfirm: editCampaignEndLeaveOnLLM.is(':checked'),
            endOrLeaveMessageDelayAfterMatchMS: parseInt(editCampaignVoicemailEndLeaveDelay.val()),
            endCallOnDetect: editCampaignFinalActionRadios.filter('[value="end"]').is(':checked'),
            leaveMessageOnDetect: editCampaignFinalActionRadios.filter('[value="leave"]').is(':checked'),
            waitXMSAfterLeavingMessageToEndCall: parseInt(editCampaignVoicemailWaitAfterMessage.val()),
            messageToLeave: CurrentCampaignVoicemailMessageToLeaveMultiLangData,
        };
        if (JSON.stringify(changes.voicemail) !== JSON.stringify(original.voicemail)) {
            hasChanges = true;
        }
    }

    function checkActionsTab() {
        function collectToolArguments(list) {
            const args = {};
            list.find(".input-group").each((_, el) => {
                const input = $(el).find("input");
                args[input.attr("input_arguement")] = input.val().trim();
            });
            return Object.keys(args).length > 0 ? args : null;
        }

        changes.actions = {
            answeredTool: { selectedToolId: editCampaignActionToolAnswered.val() === 'none' ? null : editCampaignActionToolAnswered.val(), arguments: collectToolArguments(editCampaignActionToolAnswered.closest('.mb-3').find('[id$="InputArgumentsList"]')) },
            declinedTool: { selectedToolId: editCampaignActionToolDeclined.val() === 'none' ? null : editCampaignActionToolDeclined.val(), arguments: collectToolArguments(editCampaignActionToolDeclined.closest('.mb-3').find('[id$="InputArgumentsList"]')) },
            noAnswerTool: { selectedToolId: editCampaignActionToolNoAnswer.val() === 'none' ? null : editCampaignActionToolNoAnswer.val(), arguments: collectToolArguments(editCampaignActionToolNoAnswer.closest('.mb-3').find('[id$="InputArgumentsList"]')) },
            endedTool: { selectedToolId: editCampaignActionToolEnded.val() === 'none' ? null : editCampaignActionToolEnded.val(), arguments: collectToolArguments(editCampaignActionToolEnded.closest('.mb-3').find('[id$="InputArgumentsList"]')) },
        };
        if (JSON.stringify(changes.actions) !== JSON.stringify(original.actions)) {
            hasChanges = true;
        }
    }

    checkGeneralTab();
    checkAgentTab();
    checkNumbersTab();
    checkConfigurationTab();
    checkVoicemailTab();
    checkActionsTab();

    if (enableDisableButton) {
        saveCampaignButton.prop("disabled", !hasChanges);
    }
    return { hasChanges, changes };
}

function validateCampaignTab(onlyRemove = true) {
    if (ManageCampaignType === null) return { validated: true };

    const errors = [];
    let validated = true;
    campaignManagerTab.find('.is-invalid').removeClass('is-invalid');

    // General
    if (!editCampaignNameInput.val().trim()) { validated = false; errors.push("Campaign name is required."); if (!onlyRemove) editCampaignNameInput.addClass('is-invalid'); }

    // Agent
    if (!currentCampaignAgentSelectedId) { validated = false; errors.push("An agent must be selected."); if (!onlyRemove) editSelectedCampaignAgentName.addClass('is-invalid'); }
    if (!editCampaignAgentDefaultScriptSelect.val()) { validated = false; errors.push("An opening script is required."); if (!onlyRemove) editCampaignAgentDefaultScriptSelect.addClass('is-invalid'); }
    if (!editCampaignAgentLanguageSelect.val()) { validated = false; errors.push("A language must be selected."); if (!onlyRemove) editCampaignAgentLanguageSelect.addClass('is-invalid'); }

    // Voicemail
    function validateVoicemailTab() {
        if (!editCampaignVoicemailIsEnabled.is(":checked")) return; // No validation needed if disabled

        // Advanced verification
        if (editCampaignVoicemailEnableAdvancedVerification.is(":checked")) {
            const sttValidation = campaignVoicemailSTTIntegrationManager.validate();
            if (!sttValidation.isValid) {
                validated = false;
                errors.push(...sttValidation.errors.map(e => `Voicemail STT: ${e}`));
                if (!onlyRemove) campaignVoicemailSTTIntegrationManager.getSelectElements().addClass('is-invalid');
            }
            const llmValidation = campaignVoicemailLLMIntegrationManager.validate();
            if (!llmValidation.isValid) {
                validated = false;
                errors.push(...llmValidation.errors.map(e => `Voicemail LLM: ${e}`));
                if (!onlyRemove) campaignVoicemailLLMIntegrationManager.getSelectElements().addClass('is-invalid');
            }
        }

        // Action Triggers
        const isStopTriggerSelected = editCampaignStopAgentOnML.is(':checked') || editCampaignStopAgentOnVAD.is(':checked') || editCampaignStopAgentOnLLM.is(':checked');
        if (!isStopTriggerSelected) {
            validated = false;
            errors.push("At least one 'Stop Agent Speaking' trigger must be selected for Voicemail Detection.");
            if (!onlyRemove) editCampaignStopAgentOnML.closest('.card').addClass('border-danger');
        }
        const isEndLeaveTriggerSelected = editCampaignEndLeaveOnML.is(':checked') || editCampaignEndLeaveOnVAD.is(':checked') || editCampaignEndLeaveOnLLM.is(':checked');
        if (!isEndLeaveTriggerSelected) {
            validated = false;
            errors.push("At least one 'End Call / Leave Message' trigger must be selected for Voicemail Detection.");
            if (!onlyRemove) editCampaignEndLeaveOnML.closest('.card').addClass('border-danger');
        }

        // Final Action
        if (editCampaignFinalActionRadios.filter('[value="leave"]').is(":checked")) {
            let isMessageEmpty = false;
            BusinessFullData.businessData.languages.forEach(language => {
                if (!CurrentCampaignVoicemailMessageToLeaveMultiLangData[language] || CurrentCampaignVoicemailMessageToLeaveMultiLangData[language].trim() === "") {
                    isMessageEmpty = true;
                }
            });
            if (isMessageEmpty) {
                validated = false;
                errors.push("The 'Message to Leave' cannot be empty for any business language.");
                if (!onlyRemove) editCampaignVoicemailMessageToLeave.addClass("is-invalid");
            }
        }
    }
    validateVoicemailTab();

    // (TODO: Validate Actions tab for required arguments)

    return { validated, errors };
}

async function canLeaveCampaignsTab(leaveMessage = "") {
    if (IsSavingCampaignManageTab) {
        AlertManager.createAlert({ type: "warning", message: "Campaign is currently being saved. Please wait." });
        return false;
    }
    const { hasChanges } = checkCampaignTabHasChanges(false);
    if (hasChanges) {
        const confirmDialog = new BootstrapConfirmDialog({
            title: "Unsaved Changes",
            message: `You have unsaved changes in this campaign.${leaveMessage}`,
            confirmText: "Discard", cancelText: "Cancel", confirmButtonClass: "btn-danger"
        });
        return await confirmDialog.show();
    }
    return true;
}

/** Helper Functions for Modals **/
/** Agent Tab **/
function createCampaignAgentModalListElement(agentData) {
    return `
        <button type="button" class="list-group-item list-group-item-action" agent-id="${agentData.id}">
            <span>${agentData.general.emoji} ${agentData.general.name[BusinessDefaultLanguage]}</span>
        </button>
    `;
}

/** Numbers Tab **/
function createAddedCampaignNumberListElement(numberData) {
    const countryData = CountriesList[numberData.countryCode.toUpperCase()];
    return `
        <tr number-id="${numberData.id}">
            <td>${countryData["Alpha-2 code"]} ${countryData.phone_code}</td>
            <td>${numberData.number}</td>
            <td>${numberData.provider.name}</td>
            <td>
                <button class="btn btn-danger btn-sm" button-type="remove-number-from-campaign">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `;
}

function createCampaignNumberModalListElement(numberData) {
    const countryData = CountriesList[numberData.countryCode.toUpperCase()];
    const isNumberActiveInCampaign = currentCampaignNumbersList.includes(numberData.id);

    // Unlike routing, a number can be used in multiple outbound campaigns, so we don't check for other usage.
    const elementClass = isNumberActiveInCampaign ? "disabled" : "";
    const elementText = isNumberActiveInCampaign ? "(Already added)" : "";

    return `
        <button type="button" class="list-group-item list-group-item-action ${elementClass}" button-type="add-number-to-campaign" number-id="${numberData.id}" number-provider="${numberData.provider.value}">
            ${countryData.phone_code} ${numberData.number} ${elementText}
        </button>
    `;
}

function fillCampaignNumberModalNumbersList() {
    // This function can be more complex if you add provider tabs, but for a single list, it's simpler.
    // Let's assume the modal body in the HTML is just a single list-group container for now.
    const modalBody = editChangeCampaignNumberModalElement.find('.modal-body');
    modalBody.empty();

    const listGroup = $('<div class="list-group"></div>');
    const availableNumbers = BusinessFullData.businessApp.numbers;

    if (availableNumbers.length === 0) {
        listGroup.append("<span>No numbers found for your business.</span>");
    } else {
        availableNumbers.forEach((number) => {
            // We only add numbers that are capable of making outbound calls.
            // This might be a property on your number object, e.g., `number.capabilities.outbound === true`
            // For now, we'll assume all numbers are capable.
            listGroup.append($(createCampaignNumberModalListElement(number)));
        });
    }
    modalBody.append(listGroup);
}

/** Helper Function for Actions Tab */

function handleCampaignActionToolChange(event) {
    const selectElement = $(event.currentTarget);
    const selectedToolId = selectElement.val();
    const container = selectElement.closest('[id$="Container"]');
    const argumentsContainer = container.find('.custom-tool-input-arguments');
    const argumentsSelect = container.find('[id$="InputArgumentsSelect"]');
    const argumentsList = container.find('[id$="InputArgumentsList"]');

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
    checkCampaignTabHasChanges();
    validateCampaignTab(true);
}

function handleCampaignActionAddArgument(event) {
    const selectElement = $(event.currentTarget);
    const selectedArgumentId = selectElement.val();
    if (!selectedArgumentId) return;

    const container = selectElement.closest('[id$="Container"]');
    const mainToolSelect = container.find('select').first();
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
        // Remove the option from the select dropdown and reset it
        selectElement.find(`option[value="${selectedArgumentId}"]`).remove();
        selectElement.val("");
    }
    checkCampaignTabHasChanges();
    validateCampaignTab(true);
}

function handleCampaignActionRemoveArgument(event) {
    event.preventDefault();
    const removeButton = $(event.currentTarget);
    const argumentIdToRemove = removeButton.attr('input_arguement');
    const inputGroup = removeButton.closest('.input-group');
    const container = removeButton.closest('[id$="Container"]');
    const mainToolSelect = container.find('select').first();
    const argumentsSelect = container.find('[id$="InputArgumentsSelect"]');
    const selectedToolId = mainToolSelect.val();

    const toolData = BusinessFullData.businessApp.tools.find(tool => tool.id === selectedToolId);
    const argumentData = toolData.configuration.inputSchemea.find(arg => arg.id === argumentIdToRemove);

    if (argumentData) {
        // Add the option back to the select dropdown
        argumentsSelect.append(`<option value="${argumentData.id}">${argumentData.name[BusinessDefaultLanguage]}${argumentData.isRequired ? "*" : ""}</option>`);
    }

    inputGroup.remove();
    checkCampaignTabHasChanges();
    validateCampaignTab(true);
}

/** Init **/
function initCampaignsTab() {
    $(document).ready(() => {
        /** INIT MODALS **/
        editChangeCampaignNumberModal = new bootstrap.Modal(editChangeCampaignNumberModalElement);
        editChangeCampaignAgentModal = new bootstrap.Modal(editChangeCampaignAgentModalElement);

        /** INIT EMOJI PICKER **/
        new EmojiPicker({ trigger: [{ selector: "#editCampaignIconInput", insertInto: "#editCampaignIconInput" }], closeButton: true, closeOnInsert: true });

        /** INIT INTEGRATION MANAGERS **/
        campaignVoicemailSTTIntegrationManager = new IntegrationConfigurationManager('#campaignVoicemailSTTIntegrationContainer', {
            integrationType: 'STT', allowMultiple: false, isLanguageBound: false,
            allIntegrations: BusinessFullData.businessApp.integrations, providersData: BusinessSTTProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onSaveSuccessful: () => { checkCampaignTabHasChanges(); validateCampaignTab(true); },
            onIntegrationChange: () => { checkCampaignTabHasChanges(); validateCampaignTab(true); },
        });
        campaignVoicemailLLMIntegrationManager = new IntegrationConfigurationManager('#campaignVoicemailLLMIntegrationContainer', {
            integrationType: 'LLM', allowMultiple: false, isLanguageBound: false,
            allIntegrations: BusinessFullData.businessApp.integrations, providersData: BusinessLLMProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onSaveSuccessful: () => { checkCampaignTabHasChanges(); validateCampaignTab(true); },
            onIntegrationChange: () => { checkCampaignTabHasChanges(); validateCampaignTab(true); },
        });


        /** Event Handlers **/
        addNewCampaignButton.on("click", (e) => { e.preventDefault(); ManageCampaignType = "new"; ManageCurrentCampaignData = createDefaultCampaignObject(); currentCampaignName.text("New Campaign"); resetAndEmptyCampaignManagerTab(); showCampaignManagerTab(); });
        switchBackToCampaignsTabButton.on("click", async (e) => { e.preventDefault(); if (await canLeaveCampaignsTab(" Discard changes?")) { showCampaignsListTab(); ManageCampaignType = null; } });
        campaignsListTable.on("click", ".routing-card", (e) => {
            e.preventDefault();
            const campaignId = $(e.currentTarget).attr("campaign-id");
            ManageCurrentCampaignData = BusinessFullData.businessApp.campaigns.find(c => c.id === campaignId);
            if (!ManageCurrentCampaignData) return;
            currentCampaignNumbersList = [...ManageCurrentCampaignData.numbers];
            currentCampaignName.text(ManageCurrentCampaignData.general.name);
            ManageCampaignType = "edit";
            resetAndEmptyCampaignManagerTab();
            fillCampaignManagerTab();
            showCampaignManagerTab();
        });

        // Universal handler for simple inputs
        campaignManagerTab.on('input change', 'input, select, textarea', () => {
            if (ManageCampaignType) {
                checkCampaignTabHasChanges();
                validateCampaignTab(true);
            }
        });

        // Specific handlers for UI toggles
        editCampaignRetryOnDeclineCheck.on('change', function () { editCampaignRetryOnDeclineOptionsContainer.toggleClass('d-none', !$(this).is(':checked')); });
        editCampaignRetryOnMissCheck.on('change', function () { editCampaignRetryOnMissOptionsContainer.toggleClass('d-none', !$(this).is(':checked')); });

        // --- Voicemail Handlers ---
        editCampaignVoicemailIsEnabled.on('change', function () {
            const isEnabled = $(this).is(':checked');
            editCampaignVoicemailSettingsContainer.css({ opacity: isEnabled ? 1 : 0.5, pointerEvents: isEnabled ? 'auto' : 'none' });
            editCampaignVoicemailSettingsContainer.find('input, select, textarea').prop('disabled', !isEnabled);
        });
        editCampaignVoicemailEnableAdvancedVerification.on('change', function () {
            editCampaignVoicemailAdvancedVerificationContainer.toggleClass('d-none', !$(this).is(':checked'));
        });
        editCampaignFinalActionRadios.on('change', function () {
            editCampaignVoicemailLeaveMessageContainer.toggleClass('d-none', $(this).val() !== 'leave');
        });
        editCampaignVoicemailMLCheckDurationMS.on('input', function () { editCampaignVoicemailMLCheckDurationMSValue.text($(this).val()); });
        editCampaignVoicemailMessageToLeave.on("input", (e) => {
            // Assumes a language selector for the manager exists, like `manageAgentsLanguageDropdown`
            const currentLang = BusinessDefaultLanguage; // Replace with dynamic language if manager has it
            CurrentCampaignVoicemailMessageToLeaveMultiLangData[currentLang] = $(e.currentTarget).val();
        });

        // --- Agent Tab Handlers ---
        const editChangeCampaignAgentButton = campaignManagerTab.find("#editChangeCampaignAgentButton");
        const saveChangeCampaignAgentButton = editChangeCampaignAgentModalElement.find("#saveChangeCampaignAgentButton");

        editChangeCampaignAgentButton.on('click', () => {
            campaignsManagerSelectAgentModalList.empty();
            BusinessFullData.businessApp.agents.forEach(agent => {
                const element = $(createCampaignAgentModalListElement(agent));
                if (agent.id === currentCampaignAgentSelectedId) {
                    element.addClass('active');
                }
                campaignsManagerSelectAgentModalList.append(element);
            });
            saveChangeCampaignAgentButton.prop('disabled', true);
        });

        campaignsManagerSelectAgentModalList.on("click", "button", (event) => {
            event.preventDefault();
            const clickedButton = $(event.currentTarget);
            if (clickedButton.hasClass("active")) return;

            campaignsManagerSelectAgentModalList.find("button.active").removeClass("active");
            clickedButton.addClass("active");

            const selectedAgentId = clickedButton.attr("agent-id");
            saveChangeCampaignAgentButton.prop("disabled", selectedAgentId === currentCampaignAgentSelectedId);
        });

        saveChangeCampaignAgentButton.on("click", (event) => {
            event.preventDefault();
            const selectedAgentButton = campaignsManagerSelectAgentModalList.find("button.active");
            if (selectedAgentButton.length === 0) return;

            const newAgentId = selectedAgentButton.attr("agent-id");
            if (newAgentId === currentCampaignAgentSelectedId) return; // No changes made

            currentCampaignAgentSelectedId = newAgentId;
            const agentData = BusinessFullData.businessApp.agents.find(agent => agent.id === newAgentId);

            // Update the main form
            editSelectedCampaignAgentIcon.text(agentData.general.emoji);
            editSelectedCampaignAgentName.val(agentData.general.name[BusinessDefaultLanguage]);

            // Populate and enable the scripts dropdown
            editCampaignAgentDefaultScriptSelect.prop("disabled", false).empty();
            editCampaignAgentDefaultScriptSelect.append(`<option value="" disabled selected>Select Script</option>`);
            agentData.scripts.forEach(script => {
                editCampaignAgentDefaultScriptSelect.append(`<option value="${script.id}">${script.general.name[BusinessDefaultLanguage]}</option>`);
            });

            editChangeCampaignAgentModal.hide();
            checkCampaignTabHasChanges();
            validateCampaignTab(true);
        });


        // --- 'Call From' Numbers Tab Handlers ---
        const saveChangeCampaignNumberButton = editChangeCampaignNumberModalElement.find("#saveChangeCampaignNumberButton");

        editChangeCampaignNumberButton.on("click", (event) => {
            event.preventDefault();
            fillCampaignNumberModalNumbersList();
            editChangeCampaignNumberModal.show();
            saveChangeCampaignNumberButton.prop("disabled", true);
        });

        editChangeCampaignNumberModalElement.on("click", "[button-type=add-number-to-campaign]", (event) => {
            event.preventDefault();
            const currentElement = $(event.currentTarget);
            // This allows for multi-select in the future if desired, for now, single select.
            editChangeCampaignNumberModalElement.find('.active').removeClass('active');
            currentElement.addClass("active");
            saveChangeCampaignNumberButton.prop("disabled", false);
        });

        saveChangeCampaignNumberButton.on("click", (event) => {
            event.preventDefault();
            const selectedNumberButton = editChangeCampaignNumberModalElement.find("[button-type=add-number-to-campaign].active");
            if (selectedNumberButton.length === 0) return;

            const numberId = selectedNumberButton.attr("number-id");
            const numberData = BusinessFullData.businessApp.numbers.find(n => n.id === numberId);

            if (numberData && !currentCampaignNumbersList.includes(numberId)) {
                currentCampaignNumbersList.push(numberId);
                campaignNumbersListTable.find("tbody tr[tr-type=none-notice]").remove();
                campaignNumbersListTable.find("tbody").append($(createAddedCampaignNumberListElement(numberData)));
            }

            editChangeCampaignNumberModal.hide();
            checkCampaignTabHasChanges();
            validateCampaignTab(true);
        });

        campaignNumbersListTable.on("click", "[button-type=remove-number-from-campaign]", (event) => {
            event.preventDefault();
            event.stopPropagation();
            const row = $(event.currentTarget).closest('tr');
            const numberId = row.attr("number-id");

            currentCampaignNumbersList = currentCampaignNumbersList.filter(id => id !== numberId);
            row.remove();

            if (campaignNumbersListTable.find("tbody").children().length === 0) {
                campaignNumbersListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="4">No numbers added yet...</td></tr>');
            }
            checkCampaignTabHasChanges();
            validateCampaignTab(true);
        });

        // --- Actions Tab Handlers ---
        const actionsTab = campaignManagerTab.find("#campaigns-manager-actions");

        // Main tool selection change handler
        actionsTab.on('change', 'select[id^="editCampaignActionTool"]', handleCampaignActionToolChange);

        // Add argument dropdown change handler
        actionsTab.on('change', 'select[id$="InputArgumentsSelect"]', handleCampaignActionAddArgument);

        // Remove argument button click handler (delegated)
        actionsTab.on('click', '[btn-action="remove-campaign-action-tool-argument"]', handleCampaignActionRemoveArgument);

        // Save Button Logic
        saveCampaignButton.on("click", async (e) => {
            e.preventDefault();
            if (IsSavingCampaignManageTab) return;

            const validation = validateCampaignTab(false);
            if (!validation.validated) {
                AlertManager.createAlert({ type: "danger", message: `Validation failed:<br>${validation.errors.join("<br>")}` });
                return;
            }
            const { hasChanges, changes } = checkCampaignTabHasChanges(false);
            if (!hasChanges) return;

            IsSavingCampaignManageTab = true;
            saveCampaignButton.prop("disabled", true).find('.spinner-border').removeClass('d-none');

            const formData = new FormData();
            formData.append("postType", ManageCampaignType);
            formData.append("changes", JSON.stringify(changes));
            if (ManageCampaignType === "edit") {
                formData.append("existingCampaignId", ManageCurrentCampaignData.id);
            }

            SaveBusinessCampaign(formData,
                (response) => {
                    ManageCurrentCampaignData = response.data;
                    currentCampaignNumbersList = [...ManageCurrentCampaignData.numbers];
                    currentCampaignName.text(ManageCurrentCampaignData.general.name);

                    const existingIndex = BusinessFullData.businessApp.campaigns.findIndex(c => c.id === response.data.id);
                    if (existingIndex > -1) {
                        BusinessFullData.businessApp.campaigns[existingIndex] = response.data;
                        campaignsListTable.find(`[campaign-id="${response.data.id}"]`).parent().replaceWith(createCampaignListElement(response.data));
                    } else {
                        BusinessFullData.businessApp.campaigns.push(response.data);
                        campaignsListTable.append($(createCampaignListElement(response.data)));
                    }

                    if (campaignsListTable.find('.col-12 h6').length > 0) campaignsListTable.empty().append($(createCampaignListElement(response.data)));

                    IsSavingCampaignManageTab = false;
                    saveCampaignButton.prop("disabled", true).find('.spinner-border').addClass('d-none');
                    AlertManager.createAlert({ type: "success", message: "Campaign saved successfully." });
                    ManageCampaignType = "edit";
                },
                (error) => {
                    console.error("Error saving campaign:", error);
                    IsSavingCampaignManageTab = false;
                    saveCampaignButton.prop("disabled", false).find('.spinner-border').addClass('d-none');
                    AlertManager.createAlert({ type: "danger", message: "Failed to save campaign. Check console for details." });
                }
            );
        });

        // Assuming you have a function to fetch initial data that includes campaigns
        // fillCampaignsList();
    });
}