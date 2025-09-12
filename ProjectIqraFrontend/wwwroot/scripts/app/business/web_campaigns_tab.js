/** Dynamic Variables **/
let manageWebCampaignType = null; // 'new' or 'edit'
let currentWebCampaignData = null;

let currentWebCampaignAgentSelectedId = "";

let isSavingWebCampaign = false;

const webCampaignsTooltipTriggerList = document.querySelectorAll('#web-campaigns-tab [data-bs-toggle="tooltip"]');
[...webCampaignsTooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

/** Element Variables **/
const webCampaignsTab = $("#web-campaigns-tab");

// Header Elements
const webCampaignsHeaderContainer = webCampaignsTab.find("#web-campaigns-header-container");
// Manager Header
const webCampaignManagerNameBreadcrumb = webCampaignsHeaderContainer.find("#web-campaign-manager-name-breadcrumb");
const backToWebCampaignsListButton = webCampaignsHeaderContainer.find("#back-to-web-campaigns-list");
const saveWebCampaignButton = webCampaignsHeaderContainer.find("#save-web-campaign-button");

// List View Elements
const webCampaignsListView = webCampaignsTab.find("#web-campaigns-list-view");
const addNewWebCampaignButton = webCampaignsListView.find("#add-new-web-campaign-button");
const webCampaignsListContainer = webCampaignsListView.find("#web-campaigns-list-container");

// Manager View Elements
const webCampaignsManagerView = webCampaignsTab.find("#web-campaigns-manager-view");

// General Tab
const webCampaignIconInput = webCampaignsManagerView.find("#web-campaign-icon-input");
const webCampaignNameInput = webCampaignsManagerView.find("#web-campaign-name-input");
const webCampaignDescriptionInput = webCampaignsManagerView.find("#web-campaign-description-input");

// Agent Tab
const webCampaignAgentIconSpan = webCampaignsManagerView.find("#web-campaign-agent-icon-span");
const webCampaignAgentNameInput = webCampaignsManagerView.find("#web-campaign-agent-name-input");
const webCampaignAgentScriptSelect = webCampaignsManagerView.find("#web-campaign-agent-script-select");
const webCampaignAgentLanguageSelect = webCampaignsManagerView.find("#web-campaign-agent-language-select");
const webCampaignAgentTimezoneSelect = webCampaignsManagerView.find("#web-campaign-agent-timezone-select");

// Region Tab
const webCampaignRegionPolicyRadios = webCampaignsManagerView.find('input[name="web-campaign-region-policy-radio"]');
const webCampaignFixedRegionOptionsContainer = webCampaignsManagerView.find('#web-campaign-fixed-region-options-container');
const webCampaignFixedRegionSelect = webCampaignsManagerView.find('#web-campaign-fixed-region-select');

// Configuration Tab
const webCampaignSilenceNotifyInput = webCampaignsManagerView.find("#web-campaign-silence-notify-input");
const webCampaignSilenceEndInput = webCampaignsManagerView.find("#web-campaign-silence-end-input");
const webCampaignMaxConversationTimeInput = webCampaignsManagerView.find("#web-campaign-max-conversation-time-input");

// Actions Tab
const webCampaignActionsTab = webCampaignsManagerView.find("#web-campaign-manager-actions");
const webCampaignActionToolConversationInitiationFailureSelect = webCampaignActionsTab.find("#web-campaign-action-tool-conversation-initiation-failure-select");
const webCampaignActionToolConversationInitiatedSelect = webCampaignActionsTab.find("#web-campaign-action-tool-conversation-initiated-select");
const webCampaignActionToolConversationEndedSelect = webCampaignActionsTab.find("#web-campaign-action-tool-conversation-ended-select");

// Modals
const webCampaignSelectAgentModalElement = webCampaignsTab.find("#web-campaign-select-agent-modal");
let webCampaignSelectAgentModal = null;
const webCampaignsManagerSelectAgentModalList = webCampaignSelectAgentModalElement.find(".modal-body");
const webCampaignSaveAgentButton = webCampaignSelectAgentModalElement.find("#web-campaign-save-agent-button");

/** API FUNCTIONS **/
function saveWebCampaign(formData, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/campaign/web/save`,
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
function showWebCampaignsListView() {
    webCampaignsManagerView.removeClass("show");
    webCampaignsHeaderContainer.removeClass("d-none");
    setTimeout(() => {
        webCampaignsHeaderContainer.addClass("d-none");
        webCampaignsManagerView.addClass("d-none");

        webCampaignsListView.removeClass("d-none");
        setTimeout(() => {
            webCampaignsListView.addClass("show");
            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function showWebCampaignsManagerView() {
    webCampaignsListView.removeClass("show");
    setTimeout(() => {
        webCampaignsListView.addClass("d-none");

        webCampaignsHeaderContainer.removeClass("d-none");
        webCampaignsManagerView.removeClass("d-none");
        setTimeout(() => {
            webCampaignsHeaderContainer.addClass("show");
            webCampaignsManagerView.addClass("show");
            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function createWebCampaignListElement(campaignData) {
    const agentData = BusinessFullData.businessApp.agents.find((agent) => agent.id === campaignData.agent.selectedAgentId);
    const agentName = agentData ? `Agent: ${agentData.general.emoji} ${agentData.general.name[BusinessDefaultLanguage]}` : 'No Agent Assigned';

    return `
        <div class="col-lg-4 col-md-6 col-12">
            <div class="campaign-card web-campaign-card d-flex flex-column align-items-start justify-content-center" data-campaign-id="${campaignData.id}">
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

function fillWebCampaignsList() {
    webCampaignsListContainer.empty();
    const webCampaigns = BusinessFullData.businessApp.webCampaigns;
    if (!webCampaigns || webCampaigns.length === 0) {
        webCampaignsListContainer.append('<div class="col-12"><h6 class="text-center mt-5">No web campaigns created yet...</h6></div>');
    } else {
        webCampaigns.forEach(campaign => {
            webCampaignsListContainer.append($(createWebCampaignListElement(campaign)));
        });
    }
}

function createDefaultWebCampaignObject() {
    return {
        general: {
            emoji: "🌐",
            name: "",
            description: ""
        },
        agent: {
            selectedAgentId: "",
            openingScriptId: "",
            language: "",
            timezones: []
        },
        configuration: {
            timeouts: {
                notifyOnSilenceMS: 10000,
                endOnSilenceMS: 30000,
                maxConversationTimeS: 600
            }
        },
        regionRoute: {
            policy: 'automatic',
            fixedRegion: null
        },
        actions: {
            conversationInitiationFailureTool: {
                toolId: null,
                arguments: null
            },
            conversationInitiatedTool: {
                toolId: null,
                arguments: null
            },
            conversationEndedTool: {
                toolId: null,
                arguments: null
            }
        }
    };
}

function resetWebCampaignManager() {
    webCampaignsManagerView.find(".is-invalid").removeClass("is-invalid");

    // General
    webCampaignIconInput.text("🌐");
    webCampaignNameInput.val("");
    webCampaignDescriptionInput.val("");

    // Agent
    webCampaignAgentIconSpan.text("-");
    webCampaignAgentNameInput.val("");
    webCampaignAgentScriptSelect.empty().append('<option value="" disabled selected>Select Agent First</option>').prop("disabled", true);
    webCampaignAgentLanguageSelect.empty().append('<option value="" disabled selected>Select Language</option>');
    BusinessFullData.businessData.languages.forEach(lang => {
        const langData = SpecificationLanguagesListData.find(l => l.id === lang);
        webCampaignAgentLanguageSelect.append(`<option value="${lang}">${lang} | ${langData.name}</option>`);
    });
    webCampaignAgentTimezoneSelect.val("");

    // Region
    webCampaignRegionPolicyRadios.filter('[value="automatic"]').prop('checked', true).change();
    webCampaignFixedRegionSelect.empty().append($(`<option value="" disabled selected>Select Region</option>`));
    if (typeof SpecificationRegionsListData !== 'undefined') {
        SpecificationRegionsListData.forEach((regionData) => {
            const countryData = CountriesList[regionData.countryCode.toUpperCase()];
            webCampaignFixedRegionSelect.append($(`<option value="${regionData.countryRegion}">${countryData.Country} (${regionData.countryRegion})</option>`));
        });
    }

    // Configuration
    webCampaignSilenceNotifyInput.val(10000);
    webCampaignSilenceEndInput.val(30000);
    webCampaignMaxConversationTimeInput.val(600);

    // Actions
    const actionSelects = [
        webCampaignActionToolConversationInitiationFailureSelect,
        webCampaignActionToolConversationInitiatedSelect,
        webCampaignActionToolConversationEndedSelect
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
    $("#web-campaign-manager-general-tab").click();
    saveWebCampaignButton.prop("disabled", true);
    currentWebCampaignAgentSelectedId = "";
}

function fillWebCampaignManager() {
    const data = currentWebCampaignData;

    // General
    webCampaignIconInput.text(data.general.emoji);
    webCampaignNameInput.val(data.general.name);
    webCampaignDescriptionInput.val(data.general.description);

    // Agent
    if (data.agent.selectedAgentId) {
        const agentData = BusinessFullData.businessApp.agents.find(a => a.id === data.agent.selectedAgentId);
        if (agentData) {
            currentWebCampaignAgentSelectedId = agentData.id;
            webCampaignAgentIconSpan.text(agentData.general.emoji);
            webCampaignAgentNameInput.val(agentData.general.name[BusinessDefaultLanguage]);
            webCampaignAgentScriptSelect.prop("disabled", false).empty().append('<option value="" disabled>Select Script</option>');
            agentData.scripts.forEach(script => {
                webCampaignAgentScriptSelect.append(`<option value="${script.id}">${script.general.name[BusinessDefaultLanguage]}</option>`);
            });
            webCampaignAgentScriptSelect.val(data.agent.openingScriptId);
        }
    }
    webCampaignAgentLanguageSelect.val(data.agent.language);
    if (data.agent.timezones && data.agent.timezones.length > 0) webCampaignAgentTimezoneSelect.val(data.agent.timezones[0]);

    // Region
    //if (data.regionRoute && data.regionRoute.policy === 'fixed') {
    //    webCampaignRegionPolicyRadios.filter('[value="fixed"]').prop('checked', true).change();
    //    webCampaignFixedRegionSelect.val(data.regionRoute.fixedRegion);
    //} else {
    //    webCampaignRegionPolicyRadios.filter('[value="automatic"]').prop('checked', true).change();
    //}

    // Configuration
    webCampaignSilenceNotifyInput.val(data.configuration.timeouts.notifyOnSilenceMS);
    webCampaignSilenceEndInput.val(data.configuration.timeouts.endOnSilenceMS);
    webCampaignMaxConversationTimeInput.val(data.configuration.timeouts.maxConversationTimeS);

    // Actions
    function fillWebActionTool(actionToolData, actionToolSelectElement) {
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
                        var element = $(createWebCampaignActionArgumentListElement(argumentData));
                        element.find('input').val(value);

                        argumentsList.append(element);
                        selectElement.find(`option[value="${argId}"]`).remove();
                    }
                });
            }
        }
    }
    fillWebActionTool(data.actions.conversationInitiationFailureTool, webCampaignActionToolConversationInitiationFailureSelect);
    fillWebActionTool(data.actions.conversationInitiatedTool, webCampaignActionToolConversationInitiatedSelect);
    fillWebActionTool(data.actions.conversationEndedTool, webCampaignActionToolConversationEndedSelect);
}

function checkWebCampaignChanges(enableDisableButton = true) {
    if (manageWebCampaignType === null) {
        return {
            hasChanges: false
        };
    }

    const changes = {};
    let hasChanges = false;
    const original = currentWebCampaignData;

    function checkGeneralTab() {
        changes.general = {
            emoji: webCampaignIconInput.text(),
            name: webCampaignNameInput.val().trim(),
            description: webCampaignDescriptionInput.val().trim(),
        };
        if (changes.general.emoji !== original.general.emoji ||
            changes.general.name !== original.general.name ||
            changes.general.description !== original.general.description) {
            hasChanges = true;
        }
    }

    function checkAgentTab() {
        var timezoneValue = webCampaignAgentTimezoneSelect.find(":selected").val();
        changes.agent = {
            selectedAgentId: currentWebCampaignAgentSelectedId,
            openingScriptId: webCampaignAgentScriptSelect.find(":selected").val(),
            language: webCampaignAgentLanguageSelect.find(":selected").val(),
            timezones: (timezoneValue && timezoneValue.trim() !== "") ? [timezoneValue] : [],
        };
        if (changes.agent.selectedAgentId !== original.agent.selectedAgentId ||
            changes.agent.openingScriptId !== original.agent.openingScriptId ||
            changes.agent.language !== original.agent.language ||
            JSON.stringify(changes.agent.timezones) !== JSON.stringify(original.agent.timezones || [])) {
            hasChanges = true;
        }
    }

    function checkRegionTab() {
        changes.regionRoute = {
            policy: webCampaignRegionPolicyRadios.filter(':checked').val(),
            fixedRegion: webCampaignRegionPolicyRadios.filter(':checked').val() === 'fixed' ? webCampaignFixedRegionSelect.val() : null
        };
        if (changes.regionRoute.policy !== original.regionRoute.policy ||
            changes.regionRoute.fixedRegion !== original.regionRoute.fixedRegion) {
            hasChanges = true;
        }
    }

    function checkConfigurationTab() {
        changes.configuration = {
            timeouts: {
                notifyOnSilenceMS: parseInt(webCampaignSilenceNotifyInput.val()),
                endOnSilenceMS: parseInt(webCampaignSilenceEndInput.val()),
                maxConversationTimeS: parseInt(webCampaignMaxConversationTimeInput.val()),
            }
        };
        if (changes.configuration.timeouts.notifyOnSilenceMS !== original.configuration.timeouts.notifyOnSilenceMS ||
            changes.configuration.timeouts.endOnSilenceMS !== original.configuration.timeouts.endOnSilenceMS ||
            changes.configuration.timeouts.maxConversationTimeS !== original.configuration.timeouts.maxConversationTimeS) {
            hasChanges = true;
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
            if (!originalTool) originalTool = {
                toolId: null,
                arguments: null
            };
            if (newTool.toolId !== originalTool.toolId) return true;
            if (JSON.stringify(newTool.arguments) !== JSON.stringify(originalTool.arguments)) return true;
            return false;
        }

        changes.actions = {
            conversationInitiationFailureTool: {
                toolId: webCampaignActionToolConversationInitiationFailureSelect.val() === 'none' ? null : webCampaignActionToolConversationInitiationFailureSelect.val(),
                arguments: collectToolArguments(webCampaignActionToolConversationInitiationFailureSelect)
            },
            conversationInitiatedTool: {
                toolId: webCampaignActionToolConversationInitiatedSelect.val() === 'none' ? null : webCampaignActionToolConversationInitiatedSelect.val(),
                arguments: collectToolArguments(webCampaignActionToolConversationInitiatedSelect)
            },
            conversationEndedTool: {
                toolId: webCampaignActionToolConversationEndedSelect.val() === 'none' ? null : webCampaignActionToolConversationEndedSelect.val(),
                arguments: collectToolArguments(webCampaignActionToolConversationEndedSelect)
            }
        };

        if (compareToolData(changes.actions.conversationInitiationFailureTool, original.actions.conversationInitiationFailureTool) ||
            compareToolData(changes.actions.conversationInitiatedTool, original.actions.conversationInitiatedTool) ||
            compareToolData(changes.actions.conversationEndedTool, original.actions.conversationEndedTool)) {
            hasChanges = true;
        }
    }

    // Execute all checks
    checkGeneralTab();
    checkAgentTab();
    //checkRegionTab();
    checkConfigurationTab();
    checkActionsTab();

    if (enableDisableButton) {
        saveWebCampaignButton.prop("disabled", !hasChanges);
    }

    return {
        hasChanges,
        changes
    };
}

function validateWebCampaign(onlyRemove = true) {
    if (manageWebCampaignType === null) return {
        validated: true,
        errors: []
    };

    const errors = [];
    let validated = true;
    webCampaignsManagerView.find('.is-invalid').removeClass('is-invalid');

    // General
    function validateGeneralTab() {
        if (!webCampaignNameInput.val().trim()) {
            validated = false;
            errors.push("Campaign name is required.");
            if (!onlyRemove) webCampaignNameInput.addClass('is-invalid');
        }
        if (!webCampaignDescriptionInput.val().trim()) {
            validated = false;
            errors.push("Campaign description is required.");
            if (!onlyRemove) webCampaignDescriptionInput.addClass('is-invalid');
        }
    }

    // Agent
    function validateAgentTab() {
        if (!currentWebCampaignAgentSelectedId) {
            validated = false;
            errors.push("An agent must be selected.");
            if (!onlyRemove) webCampaignAgentNameInput.addClass('is-invalid');
        }
        if (!webCampaignAgentScriptSelect.val()) {
            validated = false;
            errors.push("An opening script is required.");
            if (!onlyRemove) webCampaignAgentScriptSelect.addClass('is-invalid');
        }
        if (!webCampaignAgentLanguageSelect.val()) {
            validated = false;
            errors.push("A language must be selected.");
            if (!onlyRemove) webCampaignAgentLanguageSelect.addClass('is-invalid');
        }
        if (!webCampaignAgentTimezoneSelect.val()) {
            validated = false;
            errors.push("A timezone must be selected.");
            if (!onlyRemove) webCampaignAgentTimezoneSelect.addClass('is-invalid');
        }
    }

    // Region
    function validateRegionTab() {
        const policy = webCampaignRegionPolicyRadios.filter(':checked').val();
        if (policy === 'fixed' && !webCampaignFixedRegionSelect.val()) {
            validated = false;
            errors.push("A fixed region must be selected when the policy is set to 'Fixed Region'.");
            if (!onlyRemove) webCampaignFixedRegionSelect.addClass('is-invalid');
        }
    }

    // Configuration
    function validateConfigurationTab() {
        const silenceNotifyValue = parseInt(webCampaignSilenceNotifyInput.val());
        if (isNaN(silenceNotifyValue) || silenceNotifyValue < 0) {
            validated = false;
            errors.push("Notify on silence must be a valid number.");
            if (!onlyRemove) webCampaignSilenceNotifyInput.addClass("is-invalid");
        }
        const silenceEndValue = parseInt(webCampaignSilenceEndInput.val());
        if (isNaN(silenceEndValue) || silenceEndValue < 0) {
            validated = false;
            errors.push("End conversation on silence must be a valid number.");
            if (!onlyRemove) webCampaignSilenceEndInput.addClass("is-invalid");
        }
        const maxConvoTimeValue = parseInt(webCampaignMaxConversationTimeInput.val());
        if (isNaN(maxConvoTimeValue) || maxConvoTimeValue < 0) {
            validated = false;
            errors.push("Max conversation time must be a valid number.");
            if (!onlyRemove) webCampaignMaxConversationTimeInput.addClass("is-invalid");
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
        validateToolArguments(webCampaignActionToolConversationInitiationFailureSelect, "Conversation Initiation Failure tool");
        validateToolArguments(webCampaignActionToolConversationInitiatedSelect, "Conversation Initiated tool");
        validateToolArguments(webCampaignActionToolConversationEndedSelect, "Conversation Ended tool");
    }

    // Execute all validation checks
    validateGeneralTab();
    validateAgentTab();
    //validateRegionTab();
    validateConfigurationTab();
    validateActionsTab();

    return {
        validated,
        errors
    };
}

async function canLeaveWebCampaignsManager(leaveMessage = "") {
    if (isSavingWebCampaign) {
        AlertManager.createAlert({
            type: "warning",
            message: "Campaign is currently being saved. Please wait."
        });
        return false;
    }
    const {
        hasChanges
    } = checkWebCampaignChanges(false);
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

function handleWebCampaignRouting(subPath) {
    if (manageWebCampaignType === 'new' || manageWebCampaignType === 'edit') {
        let correctPath;
        if (manageWebCampaignType === 'new') {
            correctPath = 'webcampaigns/new';
        } else {
            correctPath = `webcampaigns/${currentWebCampaignData.id}`;
        }

        replaceUrlForTab(correctPath);
        return;
    }

    if (!subPath || subPath.length === 0) {
        if (webCampaignsManagerView.hasClass('show') && !webCampaignsListView.hasClass('show')) {
            showWebCampaignsListView();
        }
        replaceUrlForTab('webcampaigns');
        return;
    }

    const action = subPath[0];
    const campaignCard = webCampaignsListContainer.find(`.campaign-card[data-campaign-id="${action}"]`);

    if (action === 'new') {
        if (!webCampaignsManagerView.hasClass('show')) {
            addNewWebCampaignButton.click();
        }
    } else if (campaignCard.length > 0) {
        if (!webCampaignsManagerView.hasClass('show')) {
            campaignCard.click();
        }
    } else {
        showWebCampaignsListView();
        replaceUrlForTab('webcampaigns');
    }
}

function SetWebCampaignCardDynamicWidth() {
    if (!webCampaignsTab.hasClass("show")) return;

    const anyWebCampaignCard = webCampaignsListContainer.find(".web-campaign-card");
    if (anyWebCampaignCard.length > 0) {
        const firstWebCampaignCard = anyWebCampaignCard.first();

        const webCampaignCardWidth = firstWebCampaignCard.innerWidth();

        const webCampaignCardLeftRightPadding = parseInt(firstWebCampaignCard.css("padding-left")) + parseInt(firstWebCampaignCard.css("padding-right"));
        const webCampaignCardIconWidthAndPadding = firstWebCampaignCard.find(".route-icon").innerWidth();

        // .campaign-card h4
        const marginLeftForH4 = 20; // .campaign-card h4 in style.css

        const currentUsedUpSpace = webCampaignCardLeftRightPadding + webCampaignCardIconWidthAndPadding + marginLeftForH4;

        let availableH4Space = webCampaignCardWidth - currentUsedUpSpace;

        if (availableH4Space < 5) {
            availableH4Space = 5;
        }

        // .campaign-card h5-info
        let availableH5Space = webCampaignCardWidth - webCampaignCardLeftRightPadding;

        // FINAL
        $("#dynamicWebCampaignCardCSS").html(`
            .web-campaign-card .card-data {
				width: ${availableH4Space}px;
			}

            .web-campaign-card .h5-info {
                width: ${availableH5Space}px;
            }
		`);
    }
}

/** HELPER FUNCTIONS **/
function createWebCampaignAgentModalListElement(agentData) {
    return `<button type="button" class="list-group-item list-group-item-action" data-agent-id="${agentData.id}"><span>${agentData.general.emoji} ${agentData.general.name[BusinessDefaultLanguage]}</span></button>`;
}

function handleWebCampaignActionToolChange(event) {
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
    checkWebCampaignChanges();
    validateWebCampaign(true);
}

function createWebCampaignActionArgumentListElement(argumentData) {
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

function handleWebCampaignActionAddArgument(event) {
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
        argumentsList.append(createWebCampaignActionArgumentListElement(argumentData));
        selectElement.find(`option[value="${selectedArgumentId}"]`).remove();
        selectElement.val("");
    }
    checkWebCampaignChanges();
    validateWebCampaign(true);
}

function handleWebCampaignActionRemoveArgument(event) {
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
    checkWebCampaignChanges();
    validateWebCampaign(true);
}

/** EVENT HANDLER INITIALIZERS **/
function initWebAgentEventHandlers() {
    const selectAgentButton = webCampaignsManagerView.find('button[data-bs-target="#web-campaign-select-agent-modal"]');

    selectAgentButton.on('click', () => {
        webCampaignsManagerSelectAgentModalList.empty();
        const listGroup = $('<div class="list-group"></div>');
        BusinessFullData.businessApp.agents.forEach(agent => {
            const element = $(createWebCampaignAgentModalListElement(agent));
            if (agent.id === currentWebCampaignAgentSelectedId) {
                element.addClass('active');
            }
            listGroup.append(element);
        });
        webCampaignsManagerSelectAgentModalList.append(listGroup);
        webCampaignSaveAgentButton.prop('disabled', true);
    });

    webCampaignsManagerSelectAgentModalList.on("click", "button", (event) => {
        event.preventDefault();
        const clickedButton = $(event.currentTarget);
        if (clickedButton.hasClass("active")) return;
        webCampaignsManagerSelectAgentModalList.find("button.active").removeClass("active");
        clickedButton.addClass("active");
        const selectedAgentId = clickedButton.data("agent-id");
        webCampaignSaveAgentButton.prop("disabled", selectedAgentId === currentWebCampaignAgentSelectedId);
    });

    webCampaignSaveAgentButton.on("click", (event) => {
        event.preventDefault();
        const selectedAgentButton = webCampaignsManagerSelectAgentModalList.find("button.active");
        if (selectedAgentButton.length === 0) return;

        const newAgentId = selectedAgentButton.data("agent-id");
        if (newAgentId === currentWebCampaignAgentSelectedId) return;

        currentWebCampaignAgentSelectedId = newAgentId;
        const agentData = BusinessFullData.businessApp.agents.find(agent => agent.id === newAgentId);

        webCampaignAgentIconSpan.text(agentData.general.emoji);
        webCampaignAgentNameInput.val(agentData.general.name[BusinessDefaultLanguage]);

        webCampaignAgentScriptSelect.prop("disabled", false).empty();
        webCampaignAgentScriptSelect.append(`<option value="" disabled selected>Select Script</option>`);
        agentData.scripts.forEach(script => {
            webCampaignAgentScriptSelect.append(`<option value="${script.id}">${script.general.name[BusinessDefaultLanguage]}</option>`);
        });

        webCampaignSelectAgentModal.hide();
        checkWebCampaignChanges();
        validateWebCampaign(true);
    });
}

function initWebRegionEventHandlers() {
    webCampaignRegionPolicyRadios.on('change', function () {
        if ($(this).val() === 'fixed') {
            webCampaignFixedRegionOptionsContainer.removeClass('d-none');
        } else {
            webCampaignFixedRegionOptionsContainer.addClass('d-none');
        }
    });
}

function initWebActionsEventHandlers() {
    // Main tool selection change handler
    webCampaignActionToolConversationInitiationFailureSelect.on('change', handleWebCampaignActionToolChange);
    webCampaignActionToolConversationInitiatedSelect.on('change', handleWebCampaignActionToolChange);
    webCampaignActionToolConversationEndedSelect.on('change', handleWebCampaignActionToolChange);

    // Add argument dropdown change handler
    webCampaignsManagerView.on('change', '.custom-tool-input-arguments > select', handleWebCampaignActionAddArgument);

    // Remove argument button click handler
    webCampaignActionsTab.on('click', '[btn-action="remove-campaign-action-tool-argument"]', handleWebCampaignActionRemoveArgument);
}


/** INIT **/
function initWebCampaignsTab() {
    $(document).ready(() => {
        /** INIT MODALS **/
        webCampaignSelectAgentModal = new bootstrap.Modal(webCampaignSelectAgentModalElement);

        /** INIT EMOJI PICKER **/
        new EmojiPicker({
            trigger: [{
                selector: "#web-campaign-icon-input",
                insertInto: "#web-campaign-icon-input"
            }],
            closeButton: true,
            closeOnInsert: true
        });

        /** Event Handlers **/
        $(window).resize(() => {
            SetWebCampaignCardDynamicWidth();
        });

        $(document).on("containerResizeProgress", (event) => {
            SetWebCampaignCardDynamicWidth();
        })

        $(document).on("tabShowing", function (event, data) {
            if (data.tabId === 'web-campaigns-tab') {
                handleWebCampaignRouting(data.urlSubPath);
            }
        });

        $(document).on("tabShown", function (event, data) {
            if (data.tabId === 'web-campaigns-tab') {
                SetWebCampaignCardDynamicWidth();
            }
        });

        addNewWebCampaignButton.on("click", (e) => {
            e.preventDefault();
            currentWebCampaignData = createDefaultWebCampaignObject();
            webCampaignManagerNameBreadcrumb.text("New Web Campaign");
            resetWebCampaignManager();
            manageWebCampaignType = "new";
            showWebCampaignsManagerView();
            updateUrlForTab("webcampaigns/new");
        });

        backToWebCampaignsListButton.on("click", async (e) => {
            e.preventDefault();
            if (await canLeaveWebCampaignsManager(" Discard changes?")) {
                showWebCampaignsListView();
                manageWebCampaignType = null;
                updateUrlForTab("webcampaigns");
            }
        });

        webCampaignsListContainer.on("click", ".campaign-card", (e) => {
            e.preventDefault();
            const campaignId = $(e.currentTarget).attr("data-campaign-id");
            const campaignData = BusinessFullData.businessApp.webCampaigns.find(c => c.id === campaignId);
            if (!campaignData) return;
            currentWebCampaignData = JSON.parse(JSON.stringify(campaignData));
            webCampaignManagerNameBreadcrumb.text(currentWebCampaignData.general.name);
            resetWebCampaignManager();
            fillWebCampaignManager();
            manageWebCampaignType = "edit";
            showWebCampaignsManagerView();
            updateUrlForTab(`webcampaigns/${campaignId}`);
        });

        webCampaignsManagerView.on('input change', 'input, select, textarea', () => {
            if (manageWebCampaignType) {
                checkWebCampaignChanges();
                validateWebCampaign(true);
            }
        });

        saveWebCampaignButton.on("click", async (e) => {
            e.preventDefault();
            if (isSavingWebCampaign) return;
            const validation = validateWebCampaign(false);
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
            } = checkWebCampaignChanges(false);
            if (!hasChanges) return;
            isSavingWebCampaign = true;
            saveWebCampaignButton.prop("disabled", true);
            const formData = new FormData();
            formData.append("postType", manageWebCampaignType);
            formData.append("changes", JSON.stringify(changes));
            if (manageWebCampaignType === "edit") {
                formData.append("existingWebCampaignId", currentWebCampaignData.id);
            }
            saveWebCampaign(formData,
                (response) => {
                    currentWebCampaignData = response.data;
                    const existingIndex = BusinessFullData.businessApp.webCampaigns.findIndex(c => c.id === response.data.id);
                    if (existingIndex > -1) {
                        BusinessFullData.businessApp.webCampaigns[existingIndex] = response.data;
                    } else {
                        BusinessFullData.businessApp.webCampaigns.push(response.data);
                    }
                    fillWebCampaignsList(); // todo instead of this, just update the list item
                    isSavingWebCampaign = false;
                    saveWebCampaignButton.prop("disabled", true);
                    AlertManager.createAlert({
                        type: "success",
                        message: "Campaign saved successfully.",
                        timeout: 3000
                    });
                    manageWebCampaignType = "edit";
                    webCampaignManagerNameBreadcrumb.text(currentWebCampaignData.general.name);
                    updateUrlForTab(`webcampaigns/${currentWebCampaignData.id}`);
                },
                (error) => {
                    isSavingWebCampaign = false;
                    saveWebCampaignButton.prop("disabled", false);
                    AlertManager.createAlert({
                        type: "danger",
                        message: "Failed to save campaign. Check console for more details.",
                        timeout: 3000
                    });
                    console.error("Failed to save campaign:", error);
                }
            );
        });

        // Init All Handlers
        initWebAgentEventHandlers();
        //initWebRegionEventHandlers();
        initWebActionsEventHandlers();

        // Initial population
        fillWebCampaignsList();
    });
}