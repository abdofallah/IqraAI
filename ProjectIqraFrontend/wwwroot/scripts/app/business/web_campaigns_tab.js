/** Global Static Variables **/
const webCampaignPostAnalysisContextVariableArguments = [
    // Web Session Data
    {
        "id": "web_session_id",
        "Name": "Web Session Id",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Id of the web session that the conversation belongs to"
    },
    {
        "id": "web_session_created_at",
        "Name": "Web Session Created At",
        "Type": "datetime",
        "group": "Web Session Data",
        "Description": "Date and time when the web session was created"
    },
    {
        "id": "web_session_status",
        "Name": "Web Session Status",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Status of the web session"
    },
    {
        "id": "web_session_web_campaign_id",
        "Name": "Web Session Web Campaign Id",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Id of the web campaign the web session is configured with"
    },
    {
        "id": "web_session_client_identifier",
        "Name": "Web Session Client Identifier",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Unique identifier of the client the web session was initiated with"
    },
    {
        "id": "web_session_dynamic_variables",
        "Name": "Web Session Dynamic Variables",
        "Type": "object",
        "group": "Web Session Data",
        "Description": "Dynamic variables the web session was initiated with",
    },
    {
        "id": "web_session_metadata",
        "Name": "Web Session Metadata",
        "Type": "object",
        "group": "Web Session Data",
        "Description": "Metadata the web session was initiated with",
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

const webCampaignOnConversationInitiationFailureActionArgurments = [
    // Web Session Data
    {
        "id": "web_session_id",
        "Name": "Web Session Id",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Id of the web session that the conversation belongs to"
    },
    {
        "id": "web_session_created_at",
        "Name": "Web Session Created At",
        "Type": "datetime",
        "group": "Web Session Data",
        "Description": "Date and time when the web session was created"
    },
    {
        "id": "web_session_status",
        "Name": "Web Session Status",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Status of the web session"
    },
    {
        "id": "web_session_web_campaign_id",
        "Name": "Web Session Web Campaign Id",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Id of the web campaign the web session is configured with"
    },
    {
        "id": "web_session_client_identifier",
        "Name": "Web Session Client Identifier",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Unique identifier of the client the web session was initiated with"
    },
    {
        "id": "web_session_dynamic_variables",
        "Name": "Web Session Dynamic Variables",
        "Type": "object",
        "group": "Web Session Data",
        "Description": "Dynamic variables the web session was initiated with",
    },
    {
        "id": "web_session_metadata",
        "Name": "Web Session Metadata",
        "Type": "object",
        "group": "Web Session Data",
        "Description": "Metadata the web session was initiated with",
    },
    {
        "id": "web_session_initiation_error",
        "Name": "Web Session Initiation Error",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Error message of the web session initiation failure",
    }
];
const webCampaignOnConversationInitiatedActionArgurments = [
    // Web Session Data
    {
        "id": "web_session_id",
        "Name": "Web Session Id",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Id of the web session that the conversation belongs to"
    },
    {
        "id": "web_session_created_at",
        "Name": "Web Session Created At",
        "Type": "datetime",
        "group": "Web Session Data",
        "Description": "Date and time when the web session was created"
    },
    {
        "id": "web_session_status",
        "Name": "Web Session Status",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Status of the web session"
    },
    {
        "id": "web_session_web_campaign_id",
        "Name": "Web Session Web Campaign Id",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Id of the web campaign the web session is configured with"
    },
    {
        "id": "web_session_client_identifier",
        "Name": "Web Session Client Identifier",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Unique identifier of the client the web session was initiated with"
    },
    {
        "id": "web_session_dynamic_variables",
        "Name": "Web Session Dynamic Variables",
        "Type": "object",
        "group": "Web Session Data",
        "Description": "Dynamic variables the web session was initiated with",
    },
    {
        "id": "web_session_metadata",
        "Name": "Web Session Metadata",
        "Type": "object",
        "group": "Web Session Data",
        "Description": "Metadata the web session was initiated with",
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
    }
];
const webCampaignOnConversationEndedActionArgurments = [
    // Web Session Data
    {
        "id": "web_session_id",
        "Name": "Web Session Id",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Id of the web session that the conversation belongs to"
    },
    {
        "id": "web_session_created_at",
        "Name": "Web Session Created At",
        "Type": "datetime",
        "group": "Web Session Data",
        "Description": "Date and time when the web session was created"
    },
    {
        "id": "web_session_status",
        "Name": "Web Session Status",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Status of the web session"
    },
    {
        "id": "web_session_web_campaign_id",
        "Name": "Web Session Web Campaign Id",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Id of the web campaign the web session is configured with"
    },
    {
        "id": "web_session_client_identifier",
        "Name": "Web Session Client Identifier",
        "Type": "string",
        "group": "Web Session Data",
        "Description": "Unique identifier of the client the web session was initiated with"
    },
    {
        "id": "web_session_dynamic_variables",
        "Name": "Web Session Dynamic Variables",
        "Type": "object",
        "group": "Web Session Data",
        "Description": "Dynamic variables the web session was initiated with",
    },
    {
        "id": "web_session_metadata",
        "Name": "Web Session Metadata",
        "Type": "object",
        "group": "Web Session Data",
        "Description": "Metadata the web session was initiated with",
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
let manageWebCampaignType = null; // 'new' or 'edit'
let currentWebCampaignData = null;

let currentWebCampaignAgentSelectedId = "";

let isSavingWebCampaign = false;

const webCampaignsTooltipTriggerList = document.querySelectorAll('#web-campaigns-tab [data-bs-toggle="tooltip"]');
[...webCampaignsTooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

var webCampaignPostAnalysisContextVariablesCustomInput = {};

var webCampaignOnConversationInitiationFailureActionInputArgumentsCustomInput = {};
var webCampaignOnConversationInitiatedActionInputArgumentsCustomInput = {};
var webCampaignOnConversationEndedActionInputArgumentsCustomInput = {};

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

// Configuration Tab
const webCampaignSilenceNotifyInput = webCampaignsManagerView.find("#web-campaign-silence-notify-input");
const webCampaignSilenceEndInput = webCampaignsManagerView.find("#web-campaign-silence-end-input");
const webCampaignMaxConversationTimeInput = webCampaignsManagerView.find("#web-campaign-max-conversation-time-input");

// Variables Tab
const addWebCampaignDynamicVariable = webCampaignsManagerView.find("#addWebCampaignDynamicVariable");
const webCampaignDynamicVariablesList = webCampaignsManagerView.find("#webCampaignDynamicVariablesList");
const addWebCampaignMetadata = webCampaignsManagerView.find("#addWebCampaignMetadata");
const webCampaignMetadataList = webCampaignsManagerView.find("#webCampaignMetadataList");

// Post Analysis Tab
const webCampaignPostAnalysisTemplateSelect = webCampaignsManagerView.find("#webCampaignPostAnalysisTemplateSelect");
const addWebCampaignPostAnalysisVariable = webCampaignsManagerView.find("#addWebCampaignPostAnalysisVariable");
const webCampaignPostAnalysisVariablesList = webCampaignsManagerView.find("#webCampaignPostAnalysisVariablesList");

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
    const actionDropdownHtml = `
        <div class="dropdown action-dropdown dropdown-menu-end">
            <button class="btn action-button dropdown-toggle" type="button" data-bs-toggle="dropdown" data-bs-auto-close="true" aria-expanded="false">
                <i class="fa-solid fa-ellipsis"></i>
            </button>
            <ul class="dropdown-menu">
                <li>
                    <span class="dropdown-item text-danger" data-item-id="${campaignData.id}" button-type="delete-campaign">
                        <i class="fa-solid fa-trash me-2"></i>Delete
                    </span>
                </li>
            </ul>
        </div>
    `;

    return createIqraCardElement({
        id: campaignData.id,
        type: 'web-campaign',
        visualHtml: `<span>${campaignData.general.emoji}</span>`,
        titleHtml: campaignData.general.name,
        subTitleHtml: `<h6>${agentName}</h6>`,
        descriptionHtml: campaignData.general.description,
        actionDropdownHtml: actionDropdownHtml,
    });
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
        variables: {
            dynamicVariables: [],
            metadata: []
        },
        postAnalysis: {
            postAnalysisId: null,
            contextVariables: null
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

    // Configuration
    webCampaignSilenceNotifyInput.val(10000);
    webCampaignSilenceEndInput.val(30000);
    webCampaignMaxConversationTimeInput.val(600);

    // Variables Tab
    webCampaignDynamicVariablesList.empty();
    webCampaignMetadataList.empty();

    // Post Analysis Tab
    webCampaignPostAnalysisTemplateSelect.empty();
    webCampaignPostAnalysisTemplateSelect.append('<option value="" selected>No Post Analysis</option>');
    BusinessFullData.businessApp.postAnalysis.forEach((template) => {
        webCampaignPostAnalysisTemplateSelect.append(`<option value="${template.id}">${template.general.name}</option>`);
    });
    addWebCampaignPostAnalysisVariable.prop("disabled", true);
    webCampaignPostAnalysisVariablesList.empty();
    Object.keys(webCampaignPostAnalysisContextVariablesCustomInput).forEach((customInputId) => {
        webCampaignPostAnalysisContextVariablesCustomInput[customInputId].destroy();
    });
    webCampaignPostAnalysisContextVariablesCustomInput = {};

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
    const toolArgumentsListObjects = [
        webCampaignOnConversationInitiationFailureActionInputArgumentsCustomInput,
        webCampaignOnConversationInitiatedActionInputArgumentsCustomInput,
        webCampaignOnConversationEndedActionInputArgumentsCustomInput
    ];
    toolArgumentsListObjects.forEach(toolArgumentsListObject => {
        Object.keys(toolArgumentsListObject).forEach((customInputId) => {
            toolArgumentsListObject[customInputId].destroy();
            delete toolArgumentsListObject[customInputId];
        });
        toolArgumentsListObject = {};
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

    // Configuration
    webCampaignSilenceNotifyInput.val(data.configuration.timeouts.notifyOnSilenceMS);
    webCampaignSilenceEndInput.val(data.configuration.timeouts.endOnSilenceMS);
    webCampaignMaxConversationTimeInput.val(data.configuration.timeouts.maxConversationTimeS);

    // Variables
    data.variables.dynamicVariables.forEach((dynamicVariable) => {
        const row = createWebCampaignVariableElement(dynamicVariable);
        webCampaignDynamicVariablesList.append(row);
    });
    data.variables.metadata.forEach((metaData) => {
        const row = createWebCampaignVariableElement(metaData);
        webCampaignMetadataList.append(row);
    });

    // Post Analysis
    if (data.postAnalysis.postAnalysisId != null) {
        webCampaignPostAnalysisTemplateSelect.val(data.postAnalysis.postAnalysisId).change();
        data.postAnalysis.contextVariables.forEach((contextVariable) => {
            const uniqueGuid = crypto.randomUUID();

            const contextVariableElement = $(createWebCampaignPostAnalysisContextVariableElement(uniqueGuid, contextVariable));
            webCampaignPostAnalysisVariablesList.append(contextVariableElement);

            const customInput = new CustomVariableInput(
                $(contextVariableElement.find('.variable-input-container')[0]),
                webCampaignPostAnalysisContextVariableArguments,
                {
                    placeholder: "Enter information or {={variable}=} for post analysis context...",
                    onValueChange: () => {
                        checkWebCampaignChanges();
                        validateWebCampaign(true);
                    }
                }
            );

            webCampaignPostAnalysisContextVariablesCustomInput[uniqueGuid] = customInput;
            customInput.setValue(contextVariable.value);
        });
    }   

    // Actions
    function fillWebActionTool(actionToolData, actionToolSelectElement, customInputArguments, customInputObject) {
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
                        selectElement.find(`option[value="${argId}"]`).remove();

                        var element = $(createWebCampaignActionArgumentListElement(argumentData));
                        argumentsList.append(element);

                        const customInput = new CustomVariableInput(
                            $(element.find('.variable-input-container')[0]),
                            customInputArguments,
                            {
                                placeholder: `Enter '${argumentData.type.name}' value or select {={variable}=}...`,
                                onValueChange: () => {
                                    checkWebCampaignChanges();
                                    validateWebCampaign(true);
                                }
                            }
                        );

                        customInputObject[argId] = customInput;
                        customInput.setValue(value);
                    }
                });
            }
        }
    }
    fillWebActionTool(data.actions.conversationInitiationFailureTool, webCampaignActionToolConversationInitiationFailureSelect, webCampaignOnConversationInitiationFailureActionArgurments, webCampaignOnConversationInitiationFailureActionInputArgumentsCustomInput);
    fillWebActionTool(data.actions.conversationInitiatedTool, webCampaignActionToolConversationInitiatedSelect, webCampaignOnConversationInitiatedActionArgurments, webCampaignOnConversationInitiatedActionInputArgumentsCustomInput);
    fillWebActionTool(data.actions.conversationEndedTool, webCampaignActionToolConversationEndedSelect, webCampaignOnConversationEndedActionArgurments, webCampaignOnConversationEndedActionInputArgumentsCustomInput);
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

    function checkVariablesTab() {
        changes.variables = {
            dynamicVariables: [],
            metadata: []
        };

        changes.variables.dynamicVariables = getWebCampaignVariablesList(webCampaignDynamicVariablesList);
        changes.variables.metadata = getWebCampaignVariablesList(webCampaignMetadataList);

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

        let postAnalysisId = webCampaignPostAnalysisTemplateSelect.find("option:selected").val();
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

            webCampaignPostAnalysisVariablesList.children().each((i, contextVariableElement) => {
                const dataId = $(contextVariableElement).attr("data-id");

                const contextVariableData = {
                    name: $(contextVariableElement).find('.campaign-post-analysis-context-variable-name').val() ?? "",
                    description: $(contextVariableElement).find('.campaign-post-analysis-context-variable-description').val() ?? "",
                    value: webCampaignPostAnalysisContextVariablesCustomInput[dataId].getValue() ?? ""
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
        function collectToolArguments(selectElement, inputArguementObject) {
            const args = {};
            const argumentsList = selectElement.siblings('.custom-tool-input-arguments').find('[id$="-arguments-list"]');
            argumentsList.find(".variable-input-container").each((_, el) => {
                const inputArguement = $(el).attr("input_arguement");

                args[inputArguement] = inputArguementObject[inputArguement].getValue();
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
                arguments: collectToolArguments(webCampaignActionToolConversationInitiationFailureSelect, webCampaignOnConversationInitiationFailureActionInputArgumentsCustomInput)
            },
            conversationInitiatedTool: {
                toolId: webCampaignActionToolConversationInitiatedSelect.val() === 'none' ? null : webCampaignActionToolConversationInitiatedSelect.val(),
                arguments: collectToolArguments(webCampaignActionToolConversationInitiatedSelect, webCampaignOnConversationInitiatedActionInputArgumentsCustomInput)
            },
            conversationEndedTool: {
                toolId: webCampaignActionToolConversationEndedSelect.val() === 'none' ? null : webCampaignActionToolConversationEndedSelect.val(),
                arguments: collectToolArguments(webCampaignActionToolConversationEndedSelect, webCampaignOnConversationEndedActionInputArgumentsCustomInput)
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
    checkConfigurationTab();
    checkVariablesTab();
    checkPostAnalysisTab();
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

    // General
    function validateGeneralTab() {
        if (!webCampaignNameInput.val().trim()) {
            validated = false;
            errors.push("Campaign name is required.");
            if (!onlyRemove) webCampaignNameInput.addClass('is-invalid');
        }
        else {
            webCampaignNameInput.removeClass('is-invalid');
        }

        if (!webCampaignDescriptionInput.val().trim()) {
            validated = false;
            errors.push("Campaign description is required.");
            if (!onlyRemove) webCampaignDescriptionInput.addClass('is-invalid');
        }
        else {
            webCampaignDescriptionInput.removeClass('is-invalid');
        }
    }

    // Agent
    function validateAgentTab() {
        if (!currentWebCampaignAgentSelectedId) {
            validated = false;
            errors.push("An agent must be selected.");
            if (!onlyRemove) webCampaignAgentNameInput.addClass('is-invalid');
        }
        else {
            webCampaignAgentNameInput.removeClass('is-invalid');
        }

        if (!webCampaignAgentScriptSelect.val()) {
            validated = false;
            errors.push("An opening script is required.");
            if (!onlyRemove) webCampaignAgentScriptSelect.addClass('is-invalid');
        }
        else {
            webCampaignAgentScriptSelect.removeClass('is-invalid');
        }

        if (!webCampaignAgentLanguageSelect.val()) {
            validated = false;
            errors.push("A language must be selected.");
            if (!onlyRemove) webCampaignAgentLanguageSelect.addClass('is-invalid');
        }
        else {
            webCampaignAgentLanguageSelect.removeClass('is-invalid');
        }

        if (!webCampaignAgentTimezoneSelect.val()) {
            validated = false;
            errors.push("A timezone must be selected.");
            if (!onlyRemove) webCampaignAgentTimezoneSelect.addClass('is-invalid');
        }
        else {
            webCampaignAgentTimezoneSelect.removeClass('is-invalid');
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
        else {
            webCampaignSilenceNotifyInput.removeClass("is-invalid");
        }

        const silenceEndValue = parseInt(webCampaignSilenceEndInput.val());
        if (isNaN(silenceEndValue) || silenceEndValue < 0) {
            validated = false;
            errors.push("End conversation on silence must be a valid number.");
            if (!onlyRemove) webCampaignSilenceEndInput.addClass("is-invalid");
        }
        else {
            webCampaignSilenceEndInput.removeClass("is-invalid");
        }

        const maxConvoTimeValue = parseInt(webCampaignMaxConversationTimeInput.val());
        if (isNaN(maxConvoTimeValue) || maxConvoTimeValue < 0) {
            validated = false;
            errors.push("Max conversation time must be a valid number.");
            if (!onlyRemove) webCampaignMaxConversationTimeInput.addClass("is-invalid");
        }
        else {
            webCampaignMaxConversationTimeInput.removeClass("is-invalid");
        }

    }

    // Variables
    function validateVariablesTab() {
        function checkVariableList(variablesList, listName) {
            var currentAddedKeys = [];

            variablesList.find(".web-campaign-variable-box").each((index, variableElement) => {
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
                        variableKeyElement.removeClass('is-invalid');
                        currentAddedKeys.push(variableKey);
                    }
                }
            });
        }

        checkVariableList(webCampaignDynamicVariablesList, "Dynamic Variables");
        checkVariableList(webCampaignMetadataList, "Metadata");
    }

    // Post Analysis
    function validatePostAnalysisTab() {
        let postAnalysisId = webCampaignPostAnalysisTemplateSelect.find("option:selected").val();
        if (postAnalysisId && postAnalysisId != "" && postAnalysisId != null) {
            webCampaignPostAnalysisVariablesList.children().each((i, contextVariableElement) => {
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
                const validationResult = webCampaignPostAnalysisContextVariablesCustomInput[dataId].validate();
                if (!validationResult.isValidated) {
                    validated = false;
                    errors.push(validationResult.errors);
                    if (!onlyRemove) variableInputEditor.addClass("is-invalid");
                }
                else {
                    var variableValue = webCampaignPostAnalysisContextVariablesCustomInput[dataId].getValue();
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
        function validateToolArguments($toolSelect, inputArguementObject, errorPrefix) {
            if ($toolSelect.val() === "none") return;
            const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === $toolSelect.val());
            if (!toolData) return;
            const requiredArguments = toolData.configuration.inputSchemea.filter((arg) => arg.isRequired);
            const $argumentsContainer = $toolSelect.closest('div').find('.custom-tool-input-arguments');

            $toolSelect.removeClass("is-invalid");
            requiredArguments.forEach((reqArg) => {
                const arguementInput = inputArguementObject[reqArg.id];
                if (!arguementInput) {
                    validated = false;
                    errors.push(`${errorPrefix}: ${reqArg.name[BusinessDefaultLanguage]} is required.`);

                    if (!onlyRemove) $toolSelect.addClass("is-invalid");
                }
                else {
                    const arguementInputEditorField = $argumentsContainer.find(`.variable-input-container[input_arguement="${reqArg.id}"] .editor-area.form-control`);

                    const value = arguementInput.getValue();
                    if (!value || value == "" || value == null) {
                        validated = false;
                        errors.push(`${errorPrefix}: ${reqArg.name[BusinessDefaultLanguage]} is required.`);

                        if (!onlyRemove) arguementInputEditorField.addClass("is-invalid");
                    }
                    else {
                        arguementInputEditorField.removeClass("is-invalid");
                    }
                }
            });
        }

        validateToolArguments(webCampaignActionToolConversationInitiationFailureSelect, webCampaignOnConversationInitiationFailureActionInputArgumentsCustomInput, "Conversation Initiation Failure tool");
        validateToolArguments(webCampaignActionToolConversationInitiatedSelect, webCampaignOnConversationInitiatedActionInputArgumentsCustomInput, "Conversation Initiated tool");
        validateToolArguments(webCampaignActionToolConversationEndedSelect, webCampaignOnConversationEndedActionInputArgumentsCustomInput, "Conversation Ended tool");
    }

    // Execute all validation checks
    validateGeneralTab();
    validateAgentTab();
    validateConfigurationTab();
    validatePostAnalysisTab();
    validateVariablesTab();
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
    const campaignCard = webCampaignsListContainer.find(`.campaign-card[data-item-id="${action}"]`);

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

// Agent Tab Functions
function createWebCampaignAgentModalListElement(agentData) {
    return `<button type="button" class="list-group-item list-group-item-action" data-agent-id="${agentData.id}"><span>${agentData.general.emoji} ${agentData.general.name[BusinessDefaultLanguage]}</span></button>`;
}

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

// Variables Tab Functions
function createWebCampaignVariableElement(data) {
    let isRequiredCheckBoxIdUnique = `web-campaign-variable-required-${crypto.randomUUID()}`;
    let isEmptyOrNullAllowedCheckBoxIdUnique = `web-campaign-variable-emptyOrNull-${crypto.randomUUID()}`;

    return `
        <div class="input-group mt-1 web-campaign-variable-box">
			<input type="text" class="form-control" data-type="key" placeholder="Key" value="${data ? data.key : ""}">
            <div class="input-group-text">
                <input class="form-check-input mt-0" type="checkbox" id="${isRequiredCheckBoxIdUnique}" data-type="isRequired" ${data && data.isRequired ? "checked" : ""}>
                <label class="form-check-label ms-1" for="${isRequiredCheckBoxIdUnique}">Required?</label>
            </div>
            <div class="input-group-text">
                <input class="form-check-input mt-0" type="checkbox" id="${isEmptyOrNullAllowedCheckBoxIdUnique}" data-type="isEmptyOrNullAllowed" ${data && data.isEmptyOrNullAllowed ? "checked" : ""}>
                <label class="form-check-label ms-1" for="${isEmptyOrNullAllowedCheckBoxIdUnique}">Empty Allowed?</label>
            </div>
			<button class="btn btn-danger" button-type="removeWebCampaignVariable">
				<i class="fa-regular fa-trash"></i>
			</button>
		</div>
    `;
}

function initWebCampaignVariablesEventHandlers() {
    // Dynamic Variables
    addWebCampaignDynamicVariable.on('click', (event) => {
        var newElement = createWebCampaignVariableElement(null);
        webCampaignDynamicVariablesList.append(newElement);

        checkWebCampaignChanges();
        validateWebCampaign(true);
    });

    webCampaignDynamicVariablesList.on('click', '.btn[button-type="removeWebCampaignVariable"]', onRemoveVariable);

    // Metadata
    addWebCampaignMetadata.on('click', (event) => {
        var newElement = createWebCampaignVariableElement(null);
        webCampaignMetadataList.append(newElement);

        checkWebCampaignChanges();
        validateWebCampaign(true);
    });

    webCampaignMetadataList.on('click', '.btn[button-type="removeWebCampaignVariable"]', onRemoveVariable);

    // Common
    function onRemoveVariable(event) {
        event.preventDefault();

        const currentElement = $(event.currentTarget);
        currentElement.closest('.web-campaign-variable-box').remove();

        checkWebCampaignChanges();
        validateWebCampaign(true);
    }
}

function getWebCampaignVariablesList(variablesList) {
    var array = [];

    variablesList.find(".web-campaign-variable-box").each((index, variableElement) => {
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

// Post Analysis Tab Functions
function createWebCampaignPostAnalysisContextVariableElement(id, data = null) {
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

function initWebCampaignPostAnalysisEventHandlers() {
    webCampaignPostAnalysisTemplateSelect.on('change', (e) => {
        const currentElement = $(e.currentTarget);

        const currentSelectedOption = currentElement.find('option:selected');
        const currentValue = currentSelectedOption.val();

        if (!currentValue || currentValue == "") {
            addWebCampaignPostAnalysisVariable.prop("disabled", true);
            Object.keys(webCampaignPostAnalysisContextVariablesCustomInput).forEach((customInputId) => {
                webCampaignPostAnalysisContextVariablesCustomInput[customInputId].destroy();
            });;
            webCampaignPostAnalysisVariablesList.empty();
        }
        else {
            addWebCampaignPostAnalysisVariable.prop("disabled", false);
        }

        checkWebCampaignChanges();
        validateWebCampaign(true);
    });

    addWebCampaignPostAnalysisVariable.on('click', (event) => {
        event.preventDefault();

        const uniqueId = crypto.randomUUID();

        const contextVariableElement = $(createWebCampaignPostAnalysisContextVariableElement(uniqueId, null));
        webCampaignPostAnalysisVariablesList.append(contextVariableElement);

        const customInput = new CustomVariableInput(
            $(contextVariableElement.find('.variable-input-container')[0]),
            webCampaignPostAnalysisContextVariableArguments,
            {
                placeholder: "Enter information or {={variable}=} for post analysis context...",
                onValueChange: () => {
                    checkWebCampaignChanges();
                    validateWebCampaign(true);
                }
            }
        );

        webCampaignPostAnalysisContextVariablesCustomInput[uniqueId] = customInput;

        checkWebCampaignChanges();
        validateWebCampaign(true);
    });

    webCampaignPostAnalysisVariablesList.on('click', '.btn[button-type="remove-variable"]', (event) => {
        event.preventDefault();
        event.stopPropagation();

        const currentElement = $(event.currentTarget);
        const parentContainer = currentElement.closest('.campaign-post-analysis-context-variable');
        const parentId = parentContainer.attr('data-id');

        webCampaignPostAnalysisContextVariablesCustomInput[parentId].destroy();
        delete webCampaignPostAnalysisContextVariablesCustomInput[parentId];

        parentContainer.remove();

        checkWebCampaignChanges();
        validateWebCampaign(true);
    });
}

// Actions Tab Functions
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
            <div class="input-group mb-1 campaign-action-tool-argument">
                <span class="input-group-text">${argumentData.isRequired ? "*" : ""}${argumentData.name[BusinessDefaultLanguage]}</span>
                <div class="variable-input-container" input_arguement="${argumentData.id}"></div>
                <button class="btn btn-danger" btn-action="remove-campaign-action-tool-argument" input_arguement="${argumentData.id}">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </div>
        `;
}

function handleWebCampaignActionAddArgument(event, customInputArguments, customInputObject) {
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
        selectElement.find(`option[value="${selectedArgumentId}"]`).remove();
        selectElement.val("");

        var element = $(createWebCampaignActionArgumentListElement(argumentData));
        argumentsList.append(element);

        const customInput = new CustomVariableInput(
            $(element.find('.variable-input-container')[0]),
            customInputArguments,
            {
                placeholder: `Enter '${argumentData.type.name}' value or select {={variable}=}...`,
                onValueChange: () => {
                    checkWebCampaignChanges();
                    validateWebCampaign(true);
                }
            }
        );

        customInputObject[selectedArgumentId] = customInput;  
    }

    checkWebCampaignChanges();
    validateWebCampaign(true);
}

function handleWebCampaignActionRemoveArgument(event, customInputObject) {
    event.preventDefault();
    const removeButton = $(event.currentTarget);
    const argumentIdToRemove = removeButton.attr('input_arguement');
    const inputGroup = removeButton.closest('.input-group');
    const container = removeButton.closest('.custom-tool-input-arguments');
    const mainToolSelect = container.parent().find('select').first();
    const argumentsSelect = container.find('select');
    const selectedToolId = mainToolSelect.val();

    const toolData = BusinessFullData.businessApp.tools.find(tool => tool.id === selectedToolId);
    const argumentData = toolData.configuration.inputSchemea.find(arg => arg.id === argumentIdToRemove);

    if (argumentData) {
        argumentsSelect.append(`<option value="${argumentData.id}">${argumentData.name[BusinessDefaultLanguage]}${argumentData.isRequired ? "*" : ""}</option>`);
    }

    customInputObject[argumentIdToRemove].destroy();
    delete customInputObject[argumentIdToRemove];

    inputGroup.remove();

    checkWebCampaignChanges();
    validateWebCampaign(true);
}

function initWebActionsEventHandlers() {
    // Main tool selection change handler
    webCampaignActionToolConversationInitiationFailureSelect.on('change', handleWebCampaignActionToolChange);
    webCampaignActionToolConversationInitiatedSelect.on('change', handleWebCampaignActionToolChange);
    webCampaignActionToolConversationEndedSelect.on('change', handleWebCampaignActionToolChange);

    // Add argument dropdown change handler
    webCampaignsManagerView.on('change', '#web-campaign-action-tool-conversation-initiation-failure-arguments-select', (event) => {
        handleWebCampaignActionAddArgument(
            event,
            webCampaignOnConversationInitiationFailureActionArgurments,
            webCampaignOnConversationInitiationFailureActionInputArgumentsCustomInput
        );
    });
    webCampaignsManagerView.on('change', '#web-campaign-action-tool-conversation-initiated-arguments-select', (event) => {
        handleWebCampaignActionAddArgument(
            event,
            webCampaignOnConversationInitiatedActionArgurments,
            webCampaignOnConversationInitiatedActionInputArgumentsCustomInput
        );
    });
    webCampaignsManagerView.on('change', '#web-campaign-action-tool-conversation-ended-arguments-select', (event) => {
        handleWebCampaignActionAddArgument(
            event,
            webCampaignOnConversationEndedActionArgurments,
            webCampaignOnConversationEndedActionInputArgumentsCustomInput
        );
    });

    // Remove argument button click handler
    webCampaignActionsTab.on('click', '#web-campaign-action-tool-conversation-initiation-failure-arguments-list [btn-action="remove-campaign-action-tool-argument"]', (event) => {
        handleWebCampaignActionRemoveArgument(event, webCampaignOnConversationInitiationFailureActionInputArgumentsCustomInput);
    });
    webCampaignActionsTab.on('click', '#web-campaign-action-tool-conversation-initiated-arguments-list [btn-action="remove-campaign-action-tool-argument"]', (event) => {
        handleWebCampaignActionRemoveArgument(event, webCampaignOnConversationInitiatedActionInputArgumentsCustomInput);
    });
    webCampaignActionsTab.on('click', '#web-campaign-action-tool-conversation-ended-arguments-list [btn-action="remove-campaign-action-tool-argument"]', (event) => {
        handleWebCampaignActionRemoveArgument(event, webCampaignOnConversationEndedActionInputArgumentsCustomInput);
    });
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
        $(document).on("tabShowing", function (event, data) {
            if (data.tabId === 'web-campaigns-tab') {
                handleWebCampaignRouting(data.urlSubPath);
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
            e.stopPropagation();

            // check if target was button or its icon
            if ($(event.target).closest(".dropdown").length != 0) {
                return;
            }

            const campaignId = $(e.currentTarget).attr("data-item-id");
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
        initWebCampaignVariablesEventHandlers();
        initWebCampaignPostAnalysisEventHandlers();
        initWebActionsEventHandlers();

        // Initial population
        fillWebCampaignsList();
    });
}