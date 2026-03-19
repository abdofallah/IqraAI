/** Global Static Variables **/
const inboundRouteCallRingingArguments = [
	{ "id": "call_queue_id", "Name": "Call Queue Id", "Type": "string", "group": "Call Queue Data", "Description": "The unique identifier of the call queue entry." },
	{ "id": "call_queue_created_at", "Name": "Call Queue Created At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call queue entry was first created." },
	{ "id": "call_queue_enqueued_at", "Name": "Call Queue Enqueued At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call was officially placed in the queue." },
	{ "id": "call_queue_processing_started_at", "Name": "Call Queue Processing Started At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the system started processing the call." },
	{ "id": "call_queue_completed_at", "Name": "Call Queue Completed At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call was completed." },
	{ "id": "call_queue_status", "Name": "Call Queue Status", "Type": "string", "group": "Call Queue Data", "Description": "The current status of the call in the queue." },
	{ "id": "call_queue_route_id", "Name": "Call Queue Route Id", "Type": "string", "group": "Call Queue Data", "Description": "The ID of the route this call belongs to." },
	{ "id": "call_queue_route_number_id", "Name": "Call Queue Route Number Id", "Type": "string", "group": "Call Queue Data", "Description": "The ID of the phone number used to receive the call." },
	{ "id": "call_queue_route_number_provider", "Name": "Call Queue Route Number Provider", "Type": "string", "group": "Call Queue Data", "Description": "The telephony provider of the route number." },
	{ "id": "call_queue_provider_call_id", "Name": "Call Queue Provider Call Id", "Type": "string", "group": "Call Queue Data", "Description": "The unique call identifier from the telephony provider." },
	{ "id": "call_queue_caller_number", "Name": "Call Queue Caller Number", "Type": "string", "group": "Call Queue Data", "Description": "The phone number of the caller." }
];

const inboundRouteCallInitiationFailureArguments = [
	{ "id": "call_queue_id", "Name": "Call Queue Id", "Type": "string", "group": "Call Queue Data", "Description": "The unique identifier of the call queue entry." },
	{ "id": "call_queue_created_at", "Name": "Call Queue Created At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call queue entry was first created." },
	{ "id": "call_queue_enqueued_at", "Name": "Call Queue Enqueued At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call was officially placed in the queue." },
	{ "id": "call_queue_processing_started_at", "Name": "Call Queue Processing Started At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the system started processing the call." },
	{ "id": "call_queue_completed_at", "Name": "Call Queue Completed At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call was completed." },
	{ "id": "call_queue_status", "Name": "Call Queue Status", "Type": "string", "group": "Call Queue Data", "Description": "The current status of the call in the queue." },
	{ "id": "call_queue_route_id", "Name": "Call Queue Route Id", "Type": "string", "group": "Call Queue Data", "Description": "The ID of the route this call belongs to." },
	{ "id": "call_queue_route_number_id", "Name": "Call Queue Route Number Id", "Type": "string", "group": "Call Queue Data", "Description": "The ID of the phone number used to receive the call." },
	{ "id": "call_queue_route_number_provider", "Name": "Call Queue Route Number Provider", "Type": "string", "group": "Call Queue Data", "Description": "The telephony provider of the route number." },
	{ "id": "call_queue_provider_call_id", "Name": "Call Queue Provider Call Id", "Type": "string", "group": "Call Queue Data", "Description": "The unique call identifier from the telephony provider." },
	{ "id": "call_queue_caller_number", "Name": "Call Queue Caller Number", "Type": "string", "group": "Call Queue Data", "Description": "The phone number of the caller." },
	{ "id": "call_queue_session_id", "Name": "Call Queue Session Id", "Type": "string", "group": "Call Queue Data", "Description": "The telephony session ID." },
	{ "id": "call_queue_initiation_error", "Name": "Call Queue Initiation Error", "Type": "string", "group": "Call Queue Data", "Description": "Error message of the call initiation failure." }
];

const inboundRouteCallPickedArguments = [
	{ "id": "call_queue_id", "Name": "Call Queue Id", "Type": "string", "group": "Call Queue Data", "Description": "The unique identifier of the call queue entry." },
	{ "id": "call_queue_created_at", "Name": "Call Queue Created At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call queue entry was first created." },
	{ "id": "call_queue_enqueued_at", "Name": "Call Queue Enqueued At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call was officially placed in the queue." },
	{ "id": "call_queue_processing_started_at", "Name": "Call Queue Processing Started At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the system started processing the call." },
	{ "id": "call_queue_completed_at", "Name": "Call Queue Completed At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call was completed." },
	{ "id": "call_queue_status", "Name": "Call Queue Status", "Type": "string", "group": "Call Queue Data", "Description": "The current status of the call in the queue." },
	{ "id": "call_queue_route_id", "Name": "Call Queue Route Id", "Type": "string", "group": "Call Queue Data", "Description": "The ID of the route this call belongs to." },
	{ "id": "call_queue_route_number_id", "Name": "Call Queue Route Number Id", "Type": "string", "group": "Call Queue Data", "Description": "The ID of the phone number used to receive the call." },
	{ "id": "call_queue_route_number_provider", "Name": "Call Queue Route Number Provider", "Type": "string", "group": "Call Queue Data", "Description": "The telephony provider of the route number." },
	{ "id": "call_queue_provider_call_id", "Name": "Call Queue Provider Call Id", "Type": "string", "group": "Call Queue Data", "Description": "The unique call identifier from the telephony provider." },
	{ "id": "call_queue_caller_number", "Name": "Call Queue Caller Number", "Type": "string", "group": "Call Queue Data", "Description": "The phone number of the caller." },
	{ "id": "conversation_id", "Name": "Conversation Id", "Type": "string", "group": "Conversation Data", "Description": "Id of the conversation." },
	{ "id": "conversation_start_time", "Name": "Conversation Start Time", "Type": "datetime", "group": "Conversation Data", "Description": "Date and time when the conversation was started." }
];

const inboundRouteCallEndedArguments = [
	{ "id": "call_queue_id", "Name": "Call Queue Id", "Type": "string", "group": "Call Queue Data", "Description": "The unique identifier of the call queue entry." },
	{ "id": "call_queue_created_at", "Name": "Call Queue Created At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call queue entry was first created." },
	{ "id": "call_queue_enqueued_at", "Name": "Call Queue Enqueued At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call was officially placed in the queue." },
	{ "id": "call_queue_processing_started_at", "Name": "Call Queue Processing Started At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the system started processing the call." },
	{ "id": "call_queue_completed_at", "Name": "Call Queue Completed At", "Type": "datetime", "group": "Call Queue Data", "Description": "Date and time when the call was completed." },
	{ "id": "call_queue_status", "Name": "Call Queue Status", "Type": "string", "group": "Call Queue Data", "Description": "The current status of the call in the queue." },
	{ "id": "call_queue_route_id", "Name": "Call Queue Route Id", "Type": "string", "group": "Call Queue Data", "Description": "The ID of the route this call belongs to." },
	{ "id": "call_queue_route_number_id", "Name": "Call Queue Route Number Id", "Type": "string", "group": "Call Queue Data", "Description": "The ID of the phone number used to receive the call." },
	{ "id": "call_queue_route_number_provider", "Name": "Call Queue Route Number Provider", "Type": "string", "group": "Call Queue Data", "Description": "The telephony provider of the route number." },
	{ "id": "call_queue_provider_call_id", "Name": "Call Queue Provider Call Id", "Type": "string", "group": "Call Queue Data", "Description": "The unique call identifier from the telephony provider." },
	{ "id": "call_queue_caller_number", "Name": "Call Queue Caller Number", "Type": "string", "group": "Call Queue Data", "Description": "The phone number of the caller." },
	{ "id": "conversation_id", "Name": "Conversation Id", "Type": "string", "group": "Conversation Data", "Description": "Id of the conversation." },
	{ "id": "conversation_start_time", "Name": "Conversation Start Time", "Type": "datetime", "group": "Conversation Data", "Description": "Date and time when the conversation was started." },
	{ "id": "conversation_end_type", "Name": "Conversation End Type", "Type": "string", "group": "Conversation Data", "Description": "Type the conversation was ended with." },
	{ "id": "conversation_end_time", "Name": "Conversation End Time", "Type": "datetime", "group": "Conversation Data", "Description": "Date and time when the conversation was ended." },
	{ "id": "conversation_turns", "Name": "Conversation Turns", "Type": "object", "group": "Conversation Data", "Description": "Complete System/Agent/User turns data of the conversation." },
	{ "id": "conversation_turns_simplified", "Name": "Conversation Turns Simplified", "Type": "string", "group": "Conversation Data", "Description": "Simplified compiled `<role>: <content>` string." }
];

/** Dynamic Variables **/
let ManageRouteType = null; // edit or new
let ManageCurrentRouteData = null;

let currentRouteNumbersList = [];
let currentRouteAgentSelectedId = "";

let IsSavingRouteManageTab = false;
let IsDeletingRoute = false;

let editRouteActionToolCallInitiationFailureCustomInputs = {};
let editRouteActionToolRingingCustomInputs = {};
let editRouteActionToolPickedCustomInputs = {};
let editRouteActionToolEndedCustomInputs = {};

/** Element Variables  **/
const tooltipTriggerList = document.querySelectorAll('#routing-tab [data-bs-toggle="tooltip"]');
const tooltipList = [...tooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

const routingTab = $("#routing-tab");

const routingHeader = routingTab.find("#routing-header");

// List Tab
const routingListTab = routingTab.find("#routingListTab");

const addNewRoutingButton = routingListTab.find("#addNewRouteButton");
const routingListContainer = routingListTab.find("#routingListTable");

// Manager Tab
const currentRouteName = routingHeader.find("#currentRouteName");
const switchBackToRoutingTabButton = routingHeader.find("#switchBackToRoutingTab");

const saveRouteButton = routingHeader.find("#saveRouteButton");
const saveRouteButtonSpinner = routingHeader.find(".save-button-spinner");

const routingManagerTab = routingTab.find("#routingManagerTab");

// Genral Tab
const routeManagerGeneralTab = routingManagerTab.find("#routing-manager-general");

const routeIconPicker = new EmojiPicker({
	trigger: [
		{
			selector: "#editRouteIconInput",
			insertInto: "#editRouteIconInput",
		},
	],
	closeButton: true,
	closeOnInsert: true,
});

const editRouteIconInput = routeManagerGeneralTab.find("#editRouteIconInput");
const editRouteNameInput = routeManagerGeneralTab.find("#editRouteNameInput");
const editRouteDescriptionInput = routeManagerGeneralTab.find("#editRouteDescriptionInput");

// Language Tab
const routeManagerLanguageTab = routingManagerTab.find("#routing-manager-language");

const editRouteDefaultLanguageSelect = routeManagerLanguageTab.find("#editRouteDefaultLanguageSelect");

const editRouteMultiLanguageCheck = routeManagerLanguageTab.find("#editRouteMultiLanguageCheck");

const editRouteAddMultiLanguageEnabledSelect = routeManagerLanguageTab.find("#editRouteAddMultiLanguageEnabledSelect");
const routeMultiLanguagesEnabledList = routeManagerLanguageTab.find("#routeMultiLanguagesEnabledList");

// Number Tab
const editChangeRouteNumberButton = routingTab.find("#editChangeRouteNumberButton");

const editChangeRouteNumberModalElement = $("#editChangeRouteNumberModal");
let editChangeRouteNumberModal = null;
const saveChangeRouteNumberButton = editChangeRouteNumberModalElement.find("#saveChangeRouteNumberButton");

const routeNumbersList = routingTab.find("#routeNumbersList");

// Configuration Tab
const routeManagerConfigurationTab = routingManagerTab.find("#routing-manager-configuration");

const editRouteNumberPickupDelay = routeManagerConfigurationTab.find("#editRouteNumberPickupDelay");
const editRouteNumberSilenceNotify = routeManagerConfigurationTab.find("#editRouteNumberSilenceNotify");
const editRouteNumberSilenceEnd = routeManagerConfigurationTab.find("#editRouteNumberSilenceEnd");
const editRouteNumberTotalCallTime = routeManagerConfigurationTab.find("#editRouteNumberTotalCallTime");

// Agent Tab
const editChangeRouteAgentModalElement = routingTab.find("#editChangeRouteAgentModal");
let editChangeRouteAgentModal = null;
const routingManagerSelectAgentModalList = editChangeRouteAgentModalElement.find("#routing-manager-select-agent-modal-list");
const saveChangeRouteAgentButton = editChangeRouteAgentModalElement.find("#saveChangeRouteAgentButton");

const editSelectedRouteAgentIcon = routingTab.find("#editSelectedRouteAgentIcon");
const editSelectedRouteAgentName = routingTab.find("#editSelectedRouteAgentName");

const editRouteAgentDefaultScriptSelect = routingTab.find("#editRouteAgentDefaultScriptSelect");

const editRouteNumberTimezoneSelect = routingTab.find("#editRouteNumberTimezoneSelect");

const editRouteAgentCallerNumberInContextCheck = routingTab.find("#editRouteAgentCallerNumberInContextCheck");
const editRouteAgentRouteNumberInContextCheck = routingTab.find("#editRouteAgentRouteNumberInContextCheck");

// Actions Tab
const routeActionsTab = routingManagerTab.find("#routing-manager-actions");
const editRouteActionToolCallInitiationFailure = routeActionsTab.find("#editRouteActionToolCallInitiationFailure");
const editRouteActionToolRinging = routeActionsTab.find("#editRouteActionToolRinging");
const editRouteActionToolPicked = routeActionsTab.find("#editRouteActionToolPicked");
const editRouteActionToolEnded = routeActionsTab.find("#editRouteActionToolEnded");

/** API FUNCTIONS **/
function SaveBusinessRoute(formData, successCallback, errorCallback) {
	return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/routes/save`,
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
function DeleteBusinessRoute(routeId, successCallback, errorCallback) {
	return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/routes/${routeId}/delete`,
		type: "POST",
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

/** Routing List Tab **/
function showRoutingListTab() {
	routingManagerTab.removeClass("show");
	routingHeader.removeClass("show");
	setTimeout(() => {
		routingManagerTab.addClass("d-none");
		routingHeader.addClass("d-none");

		routingListTab.removeClass("d-none");
		setTimeout(() => {
			routingListTab.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}
function createRouteListCardElement(routeData) {
	const agentData = BusinessFullData.businessApp.agents.find((agent) => agent.id === routeData.agent.selectedAgentId);
	const actionDropdownHtml = `
        <div class="dropdown action-dropdown dropdown-menu-end">
            <button class="btn action-button dropdown-toggle" type="button" data-bs-toggle="dropdown" data-bs-auto-close="true" aria-expanded="false">
                <i class="fa-solid fa-ellipsis"></i>
            </button>
            <ul class="dropdown-menu">
                <li>
                    <span class="dropdown-item text-danger" data-item-id="${routeData.id}" button-type="delete-route">
                        <i class="fa-solid fa-trash me-2"></i>Delete
                    </span>
                </li>
            </ul>
        </div>
    `;

	return createIqraCardElement({
		id: routeData.id,
		type: 'routing',
		visualHtml: `<span>${routeData.general.emoji}</span>`,
		titleHtml: routeData.general.name,
		subTitleHtml: `
            <h6>${routeData.numbers.length} Number${routeData.numbers.length === 1 ? "" : "s"} Assigned</h6>
            <h6>Agent ${agentData.general.emoji} ${agentData.general.name[BusinessDefaultLanguage]}</h6>
        `,
		descriptionHtml: routeData.general.description,
		actionDropdownHtml: actionDropdownHtml,
	});
}
function fillRouteList() {
	const routes = BusinessFullData.businessApp.routings;

	routingListContainer.empty();
	if (routes.length === 0) {
		routingListContainer.append('<div class="col-12 none-routes-list-notice"><h6 class="text-center mt-5">No routes added yet...</h6></div>');
	} else {
		routes.forEach((route) => {
			const element = createRouteListCardElement(route);
			routingListContainer.append($(element));
		});
	}
}

/** Routing Manager Tab **/
function showRoutingManagerTab() {
	routingListTab.removeClass("show");
	setTimeout(() => {
		routingListTab.addClass("d-none");

		routingManagerTab.removeClass("d-none");
		routingHeader.removeClass("d-none");
		setTimeout(() => {
			routingManagerTab.addClass("show");
			routingHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}
function createDefaultRouteObject() {
	const object = {
		general: {
			emoji: "📞",
			name: "",
			description: "",
		},
		language: {
			defaultLanguageCode: "",
			multiLanguageEnabled: false,
			enabledMultiLanguages: null,
		},
		configuration: {
			pickUpDelayMS: 0,
			notifyOnSilenceMS: 10000,
			endCallOnSilenceMS: 30000,
			maxCallTimeS: 600,
		},
		numbers: [],
		agent: {
			selectedAgentId: "",
			openingScriptId: "",
			timezones: [],
			callerNumberInContext: true,
			routeNumberInContext: true,
		},
		actions: {
			callInitiationFailureTool: { toolId: null, arguments: null },
			ringingTool: { toolId: null, arguments: null },
			callPickedTool: { toolId: null, arguments: null },
			callEndedTool: { toolId: null, arguments: null },
		},
	};

	return object;
}
function resetAndEmptyRouteManagerTab() {
	// General Tab
	editRouteNameInput.val("");
	editRouteDescriptionInput.val("");
	editRouteIconInput.html("📞");

	// Langauge
	editRouteMultiLanguageCheck.prop("checked", false).change();
	routeMultiLanguagesEnabledList.find("tbody").empty();

	editRouteAddMultiLanguageEnabledSelect.empty();
	editRouteAddMultiLanguageEnabledSelect.append(`<option value="" disabled selected>Add Language</option>`);

	editRouteDefaultLanguageSelect.empty();
	editRouteDefaultLanguageSelect.append(`<option value="" disabled selected>Select Language</option>`);

	BusinessFullData.businessData.languages.forEach((language) => {
		const currentLanguageData = SpecificationLanguagesListData.find((l) => l.id === language);

		editRouteAddMultiLanguageEnabledSelect.append(`<option value="${language}">${language} | ${currentLanguageData.name}</option>`);

		editRouteDefaultLanguageSelect.append(`<option value="${language}">${language} | ${currentLanguageData.name}</option>`);
	});

	// Numbers
	routeNumbersList.find("tbody").empty();
	routeNumbersList.find("tbody").append(`<tr tr-type="none-notice"><td colspan="4">No numbers added yet...</td></tr>`);

	// Configuration
	editRouteNumberPickupDelay.val(0);
	editRouteNumberSilenceNotify.val(10000);
	editRouteNumberSilenceEnd.val(30000);
	editRouteNumberTotalCallTime.val(600);

	// Agents Tab
	routingManagerSelectAgentModalList.empty();
	BusinessFullData.businessApp.agents.forEach((agent) => {
		routingManagerSelectAgentModalList.append($(createRouteAgentModalListElement(agent)));
	});
	editSelectedRouteAgentName.val("");
	editSelectedRouteAgentIcon.html("-");

	editRouteAgentDefaultScriptSelect.empty();
	editRouteAgentDefaultScriptSelect.append('<option value="" disabled>Select Script</option>');
	BusinessFullData.businessApp.scripts.forEach((script) => {
		editRouteAgentDefaultScriptSelect.append(`<option value="${script.id}">${script.general.emoji} ${script.general.name[BusinessDefaultLanguage]}</option>`);
	});

	editRouteNumberTimezoneSelect.val("").change();
	editRouteAgentCallerNumberInContextCheck.prop("checked", true);
	editRouteAgentRouteNumberInContextCheck.prop("checked", true);

	// Actions
	const actionSelects = [
		editRouteActionToolCallInitiationFailure,
		editRouteActionToolRinging,
		editRouteActionToolPicked,
		editRouteActionToolEnded
	];
	actionSelects.forEach(select => {
		select.empty().append('<option value="none" selected>None</option>');
		BusinessFullData.businessApp.tools.forEach(tool => {
			select.append(`<option value="${tool.id}">${tool.general.name[BusinessDefaultLanguage]}</option>`);
		});
		const container = select.closest('div.mb-3');
		container.find('.custom-tool-input-arguments').addClass('d-none');
		container.find('[id$="-arguments-list"]').empty();
	});

	const toolArgumentsListObjects = [
		editRouteActionToolCallInitiationFailureCustomInputs,
		editRouteActionToolRingingCustomInputs,
		editRouteActionToolPickedCustomInputs,
		editRouteActionToolEndedCustomInputs
	];
	toolArgumentsListObjects.forEach(toolArgumentsListObject => {
		Object.keys(toolArgumentsListObject).forEach((customInputId) => {
			toolArgumentsListObject[customInputId].destroy();
			delete toolArgumentsListObject[customInputId];
		});
	});

	$("#routing-manager-general-tab").click();
	saveRouteButton.prop("disabled", true);

	// Dynamic Variables
	currentRouteAgentSelectedId = "";
}
function checkRoutingTabHasChanges(enableDisableButton = true) {
	if (ManageRouteType === null) return;

	const changes = {};
	let hasChanges = false;

	// General Tab
	function checkGeneralTab() {
		changes.general = {
			emoji: editRouteIconInput.text(),
			name: editRouteNameInput.val().trim(),
			description: editRouteDescriptionInput.val().trim(),
		};

		if (
			changes.general.emoji !== ManageCurrentRouteData.general.emoji ||
			changes.general.name !== ManageCurrentRouteData.general.name ||
			changes.general.description !== ManageCurrentRouteData.general.description
		) {
			hasChanges = true;
		}
	}

	// Language Tab
	function checkLanguageTab() {
		changes.language = {
			defaultLanguageCode: editRouteDefaultLanguageSelect.find("option:selected").val(),
			multiLanguageEnabled: editRouteMultiLanguageCheck.is(":checked"),
			enabledMultiLanguages: null,
		};

		if (changes.language.multiLanguageEnabled) {
			changes.language.enabledMultiLanguages = [];
			routeMultiLanguagesEnabledList.find("tbody tr").each((idx, element) => {
				const currentElement = $(element);
				if (!currentElement.attr("tr-type")) {
					changes.language.enabledMultiLanguages.push({
						languageCode: currentElement.attr("code"),
						messageToPlay: currentElement.find("input").val().trim(),
					});
				}
			});
		}

		// Check basic properties
		if (
			changes.language.defaultLanguageCode !== ManageCurrentRouteData.language.defaultLanguageCode ||
			changes.language.multiLanguageEnabled !== ManageCurrentRouteData.language.multiLanguageEnabled
		) {
			hasChanges = true;
			return;
		}

		// Check enabled languages
		if (changes.language.multiLanguageEnabled) {
			// Case: New has languages but original doesn't
			if (!ManageCurrentRouteData.language.enabledMultiLanguages && changes.language.enabledMultiLanguages.length > 0) {
				hasChanges = true;
				return;
			}

			// Case: Both have languages
			if (ManageCurrentRouteData.language.enabledMultiLanguages) {
				// Compare lengths first
				if (changes.language.enabledMultiLanguages.length !== ManageCurrentRouteData.language.enabledMultiLanguages.length) {
					hasChanges = true;
					return;
				}

				// Compare each language entry
				for (let i = 0; i < changes.language.enabledMultiLanguages.length; i++) {
					const newLang = changes.language.enabledMultiLanguages[i];
					const originalLang = ManageCurrentRouteData.language.enabledMultiLanguages[i];

					// Compare language codes and messages
					if (newLang.languageCode !== originalLang.languageCode || newLang.messageToPlay !== originalLang.messageToPlay) {
						hasChanges = true;
						return;
					}
				}
			}
		}
	}

	// Configuration Tab
	function checkConfigurationTab() {
		changes.configuration = {
			pickUpDelayMS: parseInt(editRouteNumberPickupDelay.val()),
			notifyOnSilenceMS: parseInt(editRouteNumberSilenceNotify.val()),
			endCallOnSilenceMS: parseInt(editRouteNumberSilenceEnd.val()),
			maxCallTimeS: parseInt(editRouteNumberTotalCallTime.val()),
		};

		if (
			changes.configuration.pickUpDelayMS !== ManageCurrentRouteData.configuration.pickUpDelayMS ||
			changes.configuration.notifyOnSilenceMS !== ManageCurrentRouteData.configuration.notifyOnSilenceMS ||
			changes.configuration.endCallOnSilenceMS !== ManageCurrentRouteData.configuration.endCallOnSilenceMS ||
			changes.configuration.maxCallTimeS !== ManageCurrentRouteData.configuration.maxCallTimeS
		) {
			hasChanges = true;
		}
	}

	// Numbers Tab
	function checkNumbersTab() {
		changes.numbers = [...currentRouteNumbersList];

		// If lengths are different, there are changes
		if (changes.numbers.length !== ManageCurrentRouteData.numbers.length) {
			hasChanges = true;
			return;
		}

		// Sort both arrays for comparison
		const sortedNewNumbers = [...changes.numbers].sort();
		const sortedOriginalNumbers = [...ManageCurrentRouteData.numbers].sort();

		// Compare each number
		for (let i = 0; i < sortedNewNumbers.length; i++) {
			if (sortedNewNumbers[i] !== sortedOriginalNumbers[i]) {
				hasChanges = true;
				return;
			}
		}
	}

	// Agent Tab
	function checkAgentTab() {
		changes.agent = {
			selectedAgentId: currentRouteAgentSelectedId,
			openingScriptId: editRouteAgentDefaultScriptSelect.find("option:selected").val(),
			timezones: editRouteNumberTimezoneSelect.val() ? [editRouteNumberTimezoneSelect.val()] : [],
			callerNumberInContext: editRouteAgentCallerNumberInContextCheck.is(":checked"),
			routeNumberInContext: editRouteAgentRouteNumberInContextCheck.is(":checked"),
		};

		// Compare basic properties
		if (
			changes.agent.selectedAgentId !== ManageCurrentRouteData.agent.selectedAgentId ||
			changes.agent.openingScriptId !== ManageCurrentRouteData.agent.openingScriptId ||
			changes.agent.callerNumberInContext !== ManageCurrentRouteData.agent.callerNumberInContext ||
			changes.agent.routeNumberInContext !== ManageCurrentRouteData.agent.routeNumberInContext
		) {
			hasChanges = true;
			return;
		}

		// Compare timezones
		const newTimezones = new Set(changes.agent.timezones);
		const originalTimezones = new Set(ManageCurrentRouteData.agent.timezones);

		// Check if lengths are different
		if (newTimezones.size !== originalTimezones.size) {
			hasChanges = true;
			return;
		}

		// Check if all timezones in new set exist in original set
		if ([...newTimezones].some((timezone) => !originalTimezones.has(timezone))) {
			hasChanges = true;
		}
	}

	// Actions Tab
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
			if (!originalTool) originalTool = { toolId: null, arguments: null };
			if (newTool.toolId !== originalTool.toolId) return true;
			if (JSON.stringify(newTool.arguments) !== JSON.stringify(originalTool.arguments)) return true;
			return false;
		}

		changes.actions = {
			callInitiationFailureTool: {
				toolId: editRouteActionToolCallInitiationFailure.val() === "none" ? null : editRouteActionToolCallInitiationFailure.val(),
				arguments: collectToolArguments(editRouteActionToolCallInitiationFailure, editRouteActionToolCallInitiationFailureCustomInputs),
			},
			ringingTool: {
				toolId: editRouteActionToolRinging.val() === "none" ? null : editRouteActionToolRinging.val(),
				arguments: collectToolArguments(editRouteActionToolRinging, editRouteActionToolRingingCustomInputs),
			},
			callPickedTool: {
				toolId: editRouteActionToolPicked.val() === "none" ? null : editRouteActionToolPicked.val(),
				arguments: collectToolArguments(editRouteActionToolPicked, editRouteActionToolPickedCustomInputs),
			},
			callEndedTool: {
				toolId: editRouteActionToolEnded.val() === "none" ? null : editRouteActionToolEnded.val(),
				arguments: collectToolArguments(editRouteActionToolEnded, editRouteActionToolEndedCustomInputs),
			},
		};

		if (compareToolData(changes.actions.callInitiationFailureTool, original.actions.callInitiationFailureTool) ||
			compareToolData(changes.actions.ringingTool, original.actions.ringingTool) ||
			compareToolData(changes.actions.callPickedTool, original.actions.callPickedTool) ||
			compareToolData(changes.actions.callEndedTool, original.actions.callEndedTool)) {
			hasChanges = true;
		}
	}

	// Execute all check functions
	checkGeneralTab();
	checkLanguageTab();
	checkConfigurationTab();
	checkNumbersTab();
	checkAgentTab();
	checkActionsTab();

	if (enableDisableButton) {
		saveRouteButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}
function validateRoutingTab(onlyRemove = true) {
	if (ManageRouteType === null) return;

	const errors = [];
	let validated = true;

	// General Tab
	function validateGeneralTab() {
		if (!editRouteNameInput.val().trim()) {
			validated = false;
			errors.push("Route name is required");

			if (!onlyRemove) {
				editRouteNameInput.addClass("is-invalid");
			}
		} else {
			editRouteNameInput.removeClass("is-invalid");
		}

		if (!editRouteDescriptionInput.val().trim()) {
			validated = false;
			errors.push("Route description is required");

			if (!onlyRemove) {
				editRouteDescriptionInput.addClass("is-invalid");
			}
		} else {
			editRouteDescriptionInput.removeClass("is-invalid");
		}
	}

	// Language Tab
	function validateLanguageTab() {
		if (!editRouteDefaultLanguageSelect.val()) {
			validated = false;
			errors.push("Default language is required");

			if (!onlyRemove) {
				editRouteDefaultLanguageSelect.addClass("is-invalid");
			}
		} else {
			editRouteDefaultLanguageSelect.removeClass("is-invalid");
		}

		if (editRouteMultiLanguageCheck.is(":checked")) {
			const enabledLanguages = routeMultiLanguagesEnabledList.find("tbody tr").not('[tr-type="none-notice"]');

			if (enabledLanguages.length === 0) {
				validated = false;
				errors.push("At least one language must be enabled when multi-language is checked");

				if (!onlyRemove) {
					editRouteAddMultiLanguageEnabledSelect.addClass("is-invalid");
				}
			} else {
				editRouteAddMultiLanguageEnabledSelect.removeClass("is-invalid");
			}

			enabledLanguages.each((idx, element) => {
				const messageInput = $(element).find("input");
				if (!messageInput.val().trim()) {
					validated = false;
					errors.push(`Language message for ${$(element).attr("name")} is required`);

					if (!onlyRemove) {
						messageInput.addClass("is-invalid");
					}
				} else {
					messageInput.removeClass("is-invalid");
				}
			});
		}
	}

	// Configuration Tab
	function validateConfigurationTab() {
		// Pickup Delay
		if (editRouteNumberPickupDelay.val() === "" || isNaN(editRouteNumberPickupDelay.val())) {
			validated = false;
			errors.push("Pick up delay must be a valid number");

			if (!onlyRemove) {
				editRouteNumberPickupDelay.addClass("is-invalid");
			}
		} else if (parseInt(editRouteNumberPickupDelay.val()) < 0) {
			validated = false;
			errors.push("Pick up delay cannot be negative");

			if (!onlyRemove) {
				editRouteNumberPickupDelay.addClass("is-invalid");
			}
		} else {
			editRouteNumberPickupDelay.removeClass("is-invalid");
		}

		// Silence Notify
		if (editRouteNumberSilenceNotify.val() === "" || isNaN(editRouteNumberSilenceNotify.val())) {
			validated = false;
			errors.push("Notify on silence must be a valid number");

			if (!onlyRemove) {
				editRouteNumberSilenceNotify.addClass("is-invalid");
			}
		} else if (parseInt(editRouteNumberSilenceNotify.val()) < 0) {
			validated = false;
			errors.push("Notify on silence cannot be negative");

			if (!onlyRemove) {
				editRouteNumberSilenceNotify.addClass("is-invalid");
			}
		} else {
			editRouteNumberSilenceNotify.removeClass("is-invalid");
		}

		// Silence End
		if (editRouteNumberSilenceEnd.val() === "" || isNaN(editRouteNumberSilenceEnd.val())) {
			validated = false;
			errors.push("End call on silence must be a valid number");

			if (!onlyRemove) {
				editRouteNumberSilenceEnd.addClass("is-invalid");
			}
		} else if (parseInt(editRouteNumberSilenceEnd.val()) < 0) {
			validated = false;
			errors.push("End call on silence cannot be negative");

			if (!onlyRemove) {
				editRouteNumberSilenceEnd.addClass("is-invalid");
			}
		} else {
			editRouteNumberSilenceEnd.removeClass("is-invalid");
		}

		// Max Call Time
		if (editRouteNumberTotalCallTime.val() === "" || isNaN(editRouteNumberTotalCallTime.val())) {
			validated = false;
			errors.push("Max call time must be a valid number");

			if (!onlyRemove) {
				editRouteNumberTotalCallTime.addClass("is-invalid");
			}
		} else if (parseInt(editRouteNumberTotalCallTime.val()) < 0) {
			validated = false;
			errors.push("Max call time cannot be negative");

			if (!onlyRemove) {
				editRouteNumberTotalCallTime.addClass("is-invalid");
			}
		} else {
			editRouteNumberTotalCallTime.removeClass("is-invalid");
		}
	}

	// Numbers Tab
	function validateNumbersTab() {
		if (currentRouteNumbersList.length === 0) {
			validated = false;
			errors.push("At least one number must be added to the route");
		}
	}

	// Agent Tab
	function validateAgentTab() {
		if (!currentRouteAgentSelectedId) {
			validated = false;
			errors.push("An agent must be selected");

			if (!onlyRemove) {
				editSelectedRouteAgentName.addClass("is-invalid");
			}
		} else {
			editSelectedRouteAgentName.removeClass("is-invalid");
		}

		if (!editRouteAgentDefaultScriptSelect.val()) {
			validated = false;
			errors.push("Opening script must be selected");

			if (!onlyRemove) {
				editRouteAgentDefaultScriptSelect.addClass("is-invalid");
			}
		} else {
			editRouteAgentDefaultScriptSelect.removeClass("is-invalid");
		}

		if (!editRouteNumberTimezoneSelect.val()) {
			validated = false;
			errors.push("Timezone must be selected");

			if (!onlyRemove) {
				editRouteNumberTimezoneSelect.addClass("is-invalid");
			}
		} else {
			editRouteNumberTimezoneSelect.removeClass("is-invalid");
		}
	}

	// Actions Tab
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

		validateToolArguments(editRouteActionToolCallInitiationFailure, editRouteActionToolCallInitiationFailureCustomInputs, "Call Initiation Failure tool");
		validateToolArguments(editRouteActionToolRinging, editRouteActionToolRingingCustomInputs, "Ringing tool");
		validateToolArguments(editRouteActionToolPicked, editRouteActionToolPickedCustomInputs, "Picked tool");
		validateToolArguments(editRouteActionToolEnded, editRouteActionToolEndedCustomInputs, "Ended tool");
	}

	// Execute all validation functions
	validateGeneralTab();
	validateLanguageTab();
	validateConfigurationTab();
	validateNumbersTab();
	validateAgentTab();
	validateActionsTab();

	return {
		validated: validated,
		errors: errors,
	};
}
function fillRoutingManagerTab() {
	// General Tab
	editRouteIconInput.text(ManageCurrentRouteData.general.emoji);
	editRouteNameInput.val(ManageCurrentRouteData.general.name);
	editRouteDescriptionInput.val(ManageCurrentRouteData.general.description);

	// Language Tab
	editRouteDefaultLanguageSelect.val(ManageCurrentRouteData.language.defaultLanguageCode);
	editRouteMultiLanguageCheck.prop("checked", ManageCurrentRouteData.language.multiLanguageEnabled);

	if (ManageCurrentRouteData.language.multiLanguageEnabled) {
		editRouteAddMultiLanguageEnabledSelect.prop("disabled", false);
		routeMultiLanguagesEnabledList.removeClass("disabled");

		if (ManageCurrentRouteData.language.enabledMultiLanguages) {
			ManageCurrentRouteData.language.enabledMultiLanguages.forEach((language, index) => {
				const languageData = SpecificationLanguagesListData.find((l) => l.id === language.languageCode);
				const element = $(createRouteLanguageMultiTableElement(language.languageCode, `${language.languageCode} | ${languageData.name}`, index + 1));
				element.find("input").val(language.messageToPlay);
				routeMultiLanguagesEnabledList.find("tbody").append(element);

				// Remove from select options
				editRouteAddMultiLanguageEnabledSelect.find(`option[value="${language.languageCode}"]`).remove();
			});
		}
	} else {
		editRouteAddMultiLanguageEnabledSelect.prop("disabled", true);
		routeMultiLanguagesEnabledList.addClass("disabled");
	}

	// Configuration Tab
	editRouteNumberPickupDelay.val(ManageCurrentRouteData.configuration.pickUpDelayMS);
	editRouteNumberSilenceNotify.val(ManageCurrentRouteData.configuration.notifyOnSilenceMS);
	editRouteNumberSilenceEnd.val(ManageCurrentRouteData.configuration.endCallOnSilenceMS);
	editRouteNumberTotalCallTime.val(ManageCurrentRouteData.configuration.maxCallTimeS);

	// Numbers Tab
	routeNumbersList.find("tbody tr[tr-type='none-notice']").remove();
	ManageCurrentRouteData.numbers.forEach((numberId) => {
		const numberData = BusinessFullData.businessApp.numbers.find((n) => n.id === numberId);
		if (numberData) {
			routeNumbersList.find("tbody").append($(createAddedRouteNumberListElement(numberData)));
		}
	});
	if (ManageCurrentRouteData.numbers.length === 0) {
		routeNumbersList.find("tbody").append('<tr tr-type="none-notice"><td colspan="4">No numbers added yet...</td></tr>');
	}

	// Agent Tab
	if (ManageCurrentRouteData.agent.selectedAgentId) {
		const agentData = BusinessFullData.businessApp.agents.find((agent) => agent.id === ManageCurrentRouteData.agent.selectedAgentId);
		if (agentData) {
			currentRouteAgentSelectedId = agentData.id;
			editSelectedRouteAgentIcon.text(agentData.general.emoji);
			editSelectedRouteAgentName.val(agentData.general.name[BusinessDefaultLanguage]);
		}
	}

	if (ManageCurrentRouteData.agent.openingScriptId) {
		editRouteAgentDefaultScriptSelect.val(ManageCurrentRouteData.agent.openingScriptId);
	}

	// Set timezone and context checkboxes
	if (ManageCurrentRouteData.agent.timezones.length > 0) {
		editRouteNumberTimezoneSelect.val(ManageCurrentRouteData.agent.timezones[0]);
	}
	editRouteAgentCallerNumberInContextCheck.prop("checked", ManageCurrentRouteData.agent.callerNumberInContext);
	editRouteAgentRouteNumberInContextCheck.prop("checked", ManageCurrentRouteData.agent.routeNumberInContext);

	// Actions
	function fillRouteActionTool(actionToolData, actionToolSelectElement, customInputArguments, customInputObject) {
		const container = actionToolSelectElement.closest('div.mb-3');
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

						var element = $(createRouteActionArgumentListElement(argumentData));
						argumentsList.append(element);

						const customInput = new CustomVariableInput(
							$(element.find('.variable-input-container')[0]),
							customInputArguments,
							{
								placeholder: `Enter '${argumentData.type.name}' value or select {={variable}=}...`,
								onValueChange: () => {
									checkRoutingTabHasChanges();
									validateRoutingTab(true);
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

	fillRouteActionTool(data.actions.callInitiationFailureTool, editRouteActionToolCallInitiationFailure, inboundRouteCallInitiationFailureArguments, editRouteActionToolCallInitiationFailureCustomInputs);
	fillRouteActionTool(data.actions.ringingTool, editRouteActionToolRinging, inboundRouteCallRingingArguments, editRouteActionToolRingingCustomInputs);
	fillRouteActionTool(data.actions.callPickedTool, editRouteActionToolPicked, inboundRouteCallPickedArguments, editRouteActionToolPickedCustomInputs);
	fillRouteActionTool(data.actions.callEndedTool, editRouteActionToolEnded, inboundRouteCallEndedArguments, editRouteActionToolEndedCustomInputs);
}
async function canLeaveRoutingTab(leaveMessage = "") {
	if (IsSavingRouteManageTab) {
		AlertManager.createAlert({
			type: "warning",
			message: "Route is currently being saved. Please wait for the save to finish.",
			timeout: 6000,
		});
		return false;
	}

	const changes = checkRoutingTabHasChanges(false);
	if (changes.hasChanges) {
		const confirmDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in the route.${leaveMessage}`,
			confirmText: "Discard",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "modal-lg",
		});

		const confirmResult = await confirmDialog.show();
		if (!confirmResult) {
			return false;
		}
	}

	return true;
}
function handleInboundRoutingURLRouting(subPath) {
	if (ManageRouteType === 'new' || ManageRouteType === 'edit') {
		let correctPath;
		if (ManageRouteType === 'new') {
			correctPath = 'routings/new';
		} else {
			correctPath = `routings/${ManageCurrentRouteData.id}`;
		}

		replaceUrlForTab(correctPath);
		return;
	}

	if (!subPath || subPath.length === 0) {
		if (routingManagerTab.hasClass("show") && !routingListTab.hasClass("show")) {
			showRoutingListTab();
		}
		replaceUrlForTab('routings');
		return;
	}

	const action = subPath[0];
	const routingCard = telephonyCampaignsListContainer.find(`.routing-card[data-item-id="${action}"]`);

	if (action === 'new') {
		if (!routingManagerTab.hasClass('show')) {
			addNewRoutingButton.click();
		}
	} else if (routingCard.length > 0) {
		if (!routingManagerTab.hasClass('show')) {
			routingCard.click();
		}
	} else {
		showRoutingListTab();
		replaceUrlForTab('routings');
	}
}

/** Language Tab **/
function ResortMultiLanugageEnabledListNumbers() {
	const tbodyChild = $(routeMultiLanguagesEnabledList.find("tbody")[0]).children();

	tbodyChild.each((index, element) => {
		$(element)
			.find("td:nth-child(2)")
			.text(index + 1);
	});
}
function createRouteLanguageMultiTableElement(langaugeCode, languageName, index) {
	const element = `
        <tr code="${langaugeCode}" name="${languageName}">
            <td class="text-center px-2">
                <button class="btn text-center" button-type="move-enabled-language">
                        <i class="fa-regular fa-arrows-up-down"></i>
                </button>
            </td>
            <td>${index}</td>
            <td>${languageName}</td>
            <td>${langaugeCode}</td>
            <td class="py-2">
                <input class="form-control" style="width: 90%" placeholder="Message to speak to user for language selection" value="Press {number} for {name}">
            </td>
            <td>
                <button class="btn btn-danger" button-type="remove-enabled-language">
                        <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `;
	return element;
}

/** Agent Tab **/
function createRouteAgentModalListElement(agentData) {
	const element = `
		<button type="button" class="list-group-item list-group-item-action" agent-id="${agentData.id}">
			<span>${agentData.general.emoji} ${agentData.general.name[BusinessDefaultLanguage]}</span>
		</button>
	`;

	return element;
}

/** Numbers Tab **/
function createAddedRouteNumberListElement(numberData) {
	const countryData = undefined;
	if (numberData.provider.value !== NumberProviderEnum.SIP || numberData.isE164Number) {
		countryData = CountriesList[numberData.countryCode.toUpperCase()]
	}

	const element = `
		<tr>
			<td>${countryData ? `${countryData["Alpha-2 code"]} ${countryData.phone_code}` : '-'}</td>
			<td>${numberData.number}</td>
			<td>${numberData.provider.name}</td>
			<td>
				<button class="btn btn-danger btn-sm" number-id="${numberData.id}" button-type="remove-number-from-route">
					<i class="fa-regular fa-trash"></i>
				</button>
			</td>
		</tr>
	`;

	return element;
}
function createRouteNumberModalListElement(numberData) {
	var countryData = undefined;
	if (numberData.provider.value !== NumberProviderEnum.SIP || numberData.isE164Number) {
		countryData = CountriesList[numberData.countryCode.toUpperCase()]
	}

	// TODO CHANGE
	const isNumberActiveInRoute = currentRouteNumbersList.findIndex((number) => number === numberData.id) !== -1;
	const isUsedByOtherRoute = numberData.routeId !== null && numberData.routeId !== ManageCurrentRouteData.id;

	const element = `
		<button type="button" class="list-group-item list-group-item-action ${isUsedByOtherRoute || isNumberActiveInRoute ? "disabled" : ""}" button-type="add-number-to-route" number-id="${numberData.id}" number-provider="${numberData.provider.value}">
			${countryData ? `${countryData.phone_code} ` : ""}${numberData.number} ${isUsedByOtherRoute ? "(Used by another route)" : ""} ${isNumberActiveInRoute ? "(Already added)" : ""}
		</button>
	`;

	return element;
}
function fillRouteNumberModalNumbersList() {
	Object.keys(NumberProviderEnum).forEach((providerType) => {
		const providerKey = NumberProviderEnum[providerType];

		const providerNumbers = BusinessFullData.businessApp.numbers.filter((number) => number.provider.value === providerKey);

		const listElement = editChangeRouteNumberModalElement.find(`#routing-manager-assign-number-modal-list[number-provider="${providerKey}"]`);

		listElement.empty();
		if (providerNumbers.length === 0) {
			listElement.append("<span>No numbers found for provider.</span>");
		} else {
			providerNumbers.forEach((number) => {
				listElement.append($(createRouteNumberModalListElement(number)));
			});
		}
	});
}

/** Action Tab Helpers **/
function handleRouteActionToolChange(event) {
	const selectElement = $(event.currentTarget);
	const selectedToolId = selectElement.val();
	const container = selectElement.closest('div.mb-3');
	const argumentsContainer = container.find('.custom-tool-input-arguments');
	const argumentsSelect = argumentsContainer.find('select');
	const argumentsList = argumentsContainer.find('[id$="-arguments-list"]');

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
	checkRoutingTabHasChanges();
	validateRoutingTab(true);
}
function createRouteActionArgumentListElement(argumentData) {
	return `
        <div class="input-group mb-1 route-action-tool-argument">
            <span class="input-group-text">${argumentData.isRequired ? "*" : ""}${argumentData.name[BusinessDefaultLanguage]}</span>
            <div class="variable-input-container" input_arguement="${argumentData.id}"></div>
            <button class="btn btn-danger" btn-action="remove-route-action-tool-argument" input_arguement="${argumentData.id}">
                <i class="fa-regular fa-trash"></i>
            </button>
        </div>
    `;
}
function handleRouteActionAddArgument(event, customInputArguments, customInputObject) {
	const selectElement = $(event.currentTarget);
	const selectedArgumentId = selectElement.val();
	if (!selectedArgumentId) return;

	const container = selectElement.closest('.custom-tool-input-arguments');
	const mainToolSelect = container.parent().find('select').first();
	const selectedToolId = mainToolSelect.val();
	const argumentsList = container.find('[id$="-arguments-list"]');

	const toolData = BusinessFullData.businessApp.tools.find(tool => tool.id === selectedToolId);
	const argumentData = toolData.configuration.inputSchemea.find(arg => arg.id === selectedArgumentId);

	if (argumentData) {
		selectElement.find(`option[value="${selectedArgumentId}"]`).remove();
		selectElement.val("");

		var element = $(createRouteActionArgumentListElement(argumentData));
		argumentsList.append(element);

		const customInput = new CustomVariableInput(
			$(element.find('.variable-input-container')[0]),
			customInputArguments,
			{
				placeholder: `Enter '${argumentData.type.name}' value or select {={variable}=}...`,
				onValueChange: () => {
					checkRoutingTabHasChanges();
					validateRoutingTab(true);
				}
			}
		);

		customInputObject[selectedArgumentId] = customInput;
	}

	checkRoutingTabHasChanges();
	validateRoutingTab(true);
}
function handleRouteActionRemoveArgument(event, customInputObject) {
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

	checkRoutingTabHasChanges();
	validateRoutingTab(true);
}

/** Init **/
function initRoutingTab() {
	$(document).ready(() => {
		/** INIT **/
		editChangeRouteNumberModal = new bootstrap.Modal(editChangeRouteNumberModalElement);
		editChangeRouteAgentModal = new bootstrap.Modal(editChangeRouteAgentModalElement);

		/** Event Handlers */
		addNewRoutingButton.on("click", (event) => {
			event.preventDefault();

			ManageCurrentRouteData = createDefaultRouteObject();
			currentRouteNumbersList = [];
			currentRouteName.text("New Route");

			resetAndEmptyRouteManagerTab();

			showRoutingManagerTab();

			ManageRouteType = "new";
			updateUrlForTab("routings/new");
		});

		switchBackToRoutingTabButton.on("click", async (event) => {
			event.preventDefault();

			if (ManageRouteType !== null) {
				const canLeaveResult = await canLeaveRoutingTab(" Are you sure you want to discard these changes and leave the routes manage tab?");
				if (!canLeaveResult) {
					return false;
				}
			}

			ManageRouteType = null;
			showRoutingListTab();
			updateUrlForTab("routings");
		});

		routingListContainer.on("click", ".routing-card", (event) => {
			event.preventDefault();
			event.stopPropagation();

			// check if target was button or its icon
			if ($(event.target).closest(".dropdown").length != 0) {
				return;
			}

			const routeId = $(event.currentTarget).attr("data-item-id");
			ManageCurrentRouteData = BusinessFullData.businessApp.routings.find((route) => route.id === routeId);
			currentRouteNumbersList = [...ManageCurrentRouteData.numbers];

			currentRouteName.text(ManageCurrentRouteData.general.name);

			resetAndEmptyRouteManagerTab();

			fillRoutingManagerTab();

			showRoutingManagerTab();

			ManageRouteType = "edit";
			updateUrlForTab(`routings/${routeId}`);
		});

		routingListContainer.on("click", ".routing-card span[button-type='delete-route']", async (event) => {
			event.preventDefault();

			const button = $(event.currentTarget);
			const routeId = button.attr("data-item-id");
			const routeIndex = BusinessFullData.businessApp.routings.findIndex(n => n.id === routeId);
			if (routeIndex === -1) return;
			const routeData = BusinessFullData.businessApp.routings[routeIndex];
			if (!routeData) return;
			const routeCard = routingListContainer.find(`.routing-card[data-item-id="${routeId}"]`);

			if (IsDeletingRoute) {
				AlertManager.createAlert({
					type: "warning",
					message: `A delete operation for inbound routes is already in progress. Please try again once the operation is complete.`,
					timeout: 6000,
				});
				return;
			}

			const confirmDialog = new BootstrapConfirmDialog({
				title: `Delete "${routeData.general.name}" Inbound Route`,
				message: `Are you sure you want to delete this inbound route?<br><br><b>Note:</b> You must wait or cancel any ongoing call queues or conversations.`,
				confirmText: "Delete",
				confirmButtonClass: "btn-danger",
				modalClass: "modal-lg"
			});

			if (await confirmDialog.show()) {
				showHideButtonSpinnerWithDisableEnable(button, true);
				IsDeletingRoute = true;
				routeCard.addClass("disabled");

				DeleteBusinessRoute(
					routeId,
					() => {

						BusinessFullData.businessApp.routings.splice(routeIndex, 1);

						routeCard.parent().remove();

						if (BusinessFullData.businessApp.routings.length === 0) {
							routingListContainer.append('<div class="col-12 none-routes-list-notice"><h6 class="text-center mt-5">No routes added yet...</h6></div>');
						}

						AlertManager.createAlert({
							type: "success",
							message: `Inbound Route "${routeData.general.name}" deleted successfully.`,
							timeout: 6000,
						});
					},
					(errorResult) => {
						routeCard.removeClass("disabled");

						var resultMessage = "Check console logs for more details.";
						if (errorResult && errorResult.message) resultMessage = errorResult.message;

						AlertManager.createAlert({
							type: "danger",
							message: "Error occured while deleting business inbound route.",
							resultMessage: resultMessage,
							timeout: 6000,
						});

						console.log("Error occured while deleting business inbound route: ", errorResult);
					}
				).always(() => {
					showHideButtonSpinnerWithDisableEnable(button, false);
					IsDeletingRoute = false;
				});
			}
		});

		$(document).on("tabShowing", function (event, data) {
			if (data.tabId === 'routing-tab') {
				handleInboundRoutingURLRouting(data.urlSubPath);
			}
		});

		/** General Tab **/
		function initGeneralTabHandlers() {
			routeManagerGeneralTab.on("input", "input", (event) => {
				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});

			editRouteIconInput.on("emojiSelected", (event) => {
				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});
		}
		initGeneralTabHandlers();

		/** Language Tab **/
		function initLanguageTabHandlers() {
			editRouteDefaultLanguageSelect.on("change", (event) => {
				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});

			editRouteMultiLanguageCheck.on("change", (event) => {
				const isChecked = $(event.currentTarget).is(":checked");

				editRouteAddMultiLanguageEnabledSelect.prop("disabled", !isChecked);
				if (isChecked) {
					routeMultiLanguagesEnabledList.removeClass("disabled");
				} else {
					routeMultiLanguagesEnabledList.addClass("disabled");
				}

				routeMultiLanguagesEnabledList.find("tr td button, tr td input").each((index, element) => {
					$(element).prop("disabled", !isChecked);
				});

				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});

			editRouteAddMultiLanguageEnabledSelect.on("change", (event) => {
				const selectedValue = $(event.currentTarget).val();
				if (selectedValue === "select" || !selectedValue || selectedValue === "") return;

				const optionElement = editRouteAddMultiLanguageEnabledSelect.find(`option[value="${selectedValue}"]`);
				const optionText = optionElement.text();

				const tbody = $(routeMultiLanguagesEnabledList.find("tbody")[0]);

				tbody.append($(createRouteLanguageMultiTableElement(selectedValue, optionText, tbody.children().length + 1)));

				optionElement.remove();

				editRouteAddMultiLanguageEnabledSelect.val("");
				editRouteAddMultiLanguageEnabledSelect.change();

				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});

			routeMultiLanguagesEnabledList.on("click", '[button-type="remove-enabled-language"]', (event) => {
				event.preventDefault();
				event.stopPropagation();
				event.stopImmediatePropagation();

				const parent = $(event.currentTarget).parent().parent();
				const code = parent.attr("code");
				const name = parent.attr("name");

				editRouteAddMultiLanguageEnabledSelect.append(`<option value="${code}">${name}</option>`);
				parent.remove();

				ResortMultiLanugageEnabledListNumbers();

				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});

			routeMultiLanguagesEnabledList.find("tbody").sortable({
				items: 'tr:not([data-type="nothing-added"])',
				cursor: "pointer",
				axis: "y",
				dropOnEmpty: false,
				forceHelperSize: true,
				forcePlaceholderSize: true,
				handle: 'button[button-type="move-enabled-language"]',
				cancel: "",
				start: (e, ui) => {
					ui.item.addClass("selected");
				},
				stop: (e, ui) => {
					ui.item.removeClass("selected");

					ResortMultiLanugageEnabledListNumbers();

					checkRoutingTabHasChanges();
					validateRoutingTab(true);
				},
			});
		}
		initLanguageTabHandlers();

		/** Number Tab **/
		function initNumberTabHandlers() {
			editChangeRouteNumberButton.on("click", (event) => {
				event.preventDefault();

				fillRouteNumberModalNumbersList();

				editChangeRouteNumberModal.show();

				saveChangeRouteNumberButton.prop("disabled", true);
			});

			editChangeRouteNumberModalElement.on("click", "[button-type=add-number-to-route]", (event) => {
				event.preventDefault();

				const currentElement = $(event.currentTarget);
				const numberId = currentElement.attr("number-id");

				const currentActiveElement = editChangeRouteNumberModalElement.find('[button-type="add-number-to-route"].active');
				if (currentActiveElement.length > 0) {
					const currentActiveNumberId = currentActiveElement.attr("number-id");

					if (currentActiveNumberId === numberId) {
						return;
					}

					currentActiveElement.removeClass("active");
				}

				currentElement.addClass("active");
				saveChangeRouteNumberButton.prop("disabled", false);
			});

			saveChangeRouteNumberButton.on("click", (event) => {
				event.preventDefault();

				const currentActiveElement = editChangeRouteNumberModalElement.find('[button-type="add-number-to-route"].active');
				if (currentActiveElement.length === 0) return;

				const numberId = currentActiveElement.attr("number-id");

				const numberData = BusinessFullData.businessApp.numbers.find((number) => number.id === numberId);

				routeNumbersList.find("tbody").append($(createAddedRouteNumberListElement(numberData)));

				const noneNotice = routeNumbersList.find("tbody tr[tr-type=none-notice]");
				if (noneNotice.length > 0) {
					noneNotice.remove();
				}

				currentRouteNumbersList.push(numberId);

				editChangeRouteNumberModal.hide();

				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});

			routeNumbersList.on("click", '[button-type="remove-number-from-route"]', (event) => {
				event.preventDefault();
				event.stopPropagation();

				const currentElement = $(event.currentTarget);
				const numberId = currentElement.attr("number-id");

				const index = currentRouteNumbersList.indexOf(numberId);
				if (index > -1) {
					currentRouteNumbersList.splice(index, 1);
				}

				currentElement.parent().parent().remove();

				if (routeNumbersList.find("tbody").children().length === 0) {
					routeNumbersList.find("tbody").append('<tr tr-type="none-notice"><td colspan="4">No numbers added yet...</td></tr>');
				}

				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});
		}
		initNumberTabHandlers();

		/** Configuration Tab **/
		function initConfigurationTabHandlers() {
			routeManagerConfigurationTab.on("input", "input", (event) => {
				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});
		}
		initConfigurationTabHandlers();

		/** Agents Tab **/
		function initAgentTabHandlers() {
			editChangeRouteAgentModalElement.on("show.bs.modal", (event) => {
				const activeButton = routingManagerSelectAgentModalList.find("button.active");

				if (activeButton.length > 0) {
					const agentId = activeButton.attr("agent-id");
					saveChangeRouteAgentButton.prop("disabled", agentId === currentRouteAgentSelectedId);
				} else {
					saveChangeRouteAgentButton.prop("disabled", true);
				}
			});

			routingManagerSelectAgentModalList.on("click", "button", (event) => {
				event.preventDefault();

				const currentElement = $(event.currentTarget);

				if (currentElement.hasClass("active")) {
					return;
				}

				const agentId = currentElement.attr("agent-id");

				routingManagerSelectAgentModalList.find("button.active").removeClass("active");
				currentElement.addClass("active");

				const isSameAgent = agentId === currentRouteAgentSelectedId;
				saveChangeRouteAgentButton.prop("disabled", isSameAgent);
				currentRouteAgentSelectedId = agentId;
			});

			saveChangeRouteAgentButton.on("click", (event) => {
				event.preventDefault();

				if (currentRouteAgentSelectedId === null) return;

				const agentData = BusinessFullData.businessApp.agents.find((agent) => agent.id === currentRouteAgentSelectedId);

				editSelectedRouteAgentIcon.text(agentData.general.emoji);
				editSelectedRouteAgentName.val(agentData.general.name[BusinessDefaultLanguage]);

				editChangeRouteAgentModal.hide();

				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});

			editRouteAgentDefaultScriptSelect.on("change", (event) => {
				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});

			editRouteNumberTimezoneSelect.on("change", (event) => {
				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});

			editRouteAgentCallerNumberInContextCheck.on("change", (event) => {
				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});

			editRouteAgentRouteNumberInContextCheck.on("change", (event) => {
				checkRoutingTabHasChanges();
				validateRoutingTab(true);
			});
		}
		initAgentTabHandlers();

		/** Action Tab Events **/
		function initActionTabHandlers() {
			editRouteActionToolCallInitiationFailure.on('change', handleRouteActionToolChange);
			editRouteActionToolRinging.on('change', handleRouteActionToolChange);
			editRouteActionToolPicked.on('change', handleRouteActionToolChange);
			editRouteActionToolEnded.on('change', handleRouteActionToolChange);

			routeActionsTab.on('change', '#editRouteActionToolCallInitiationFailure-arguments-select', (event) => {
				handleRouteActionAddArgument(event, inboundRouteCallInitiationFailureArguments, editRouteActionToolCallInitiationFailureCustomInputs);
			});
			routeActionsTab.on('change', '#editRouteActionToolRinging-arguments-select', (event) => {
				handleRouteActionAddArgument(event, inboundRouteCallRingingArguments, editRouteActionToolRingingCustomInputs);
			});
			routeActionsTab.on('change', '#editRouteActionToolPicked-arguments-select', (event) => {
				handleRouteActionAddArgument(event, inboundRouteCallPickedArguments, editRouteActionToolPickedCustomInputs);
			});
			routeActionsTab.on('change', '#editRouteActionToolEnded-arguments-select', (event) => {
				handleRouteActionAddArgument(event, inboundRouteCallEndedArguments, editRouteActionToolEndedCustomInputs);
			});

			routeActionsTab.on('click', '#editRouteActionToolCallInitiationFailure-arguments-list [btn-action="remove-route-action-tool-argument"]', (event) => {
				handleRouteActionRemoveArgument(event, editRouteActionToolCallInitiationFailureCustomInputs);
			});
			routeActionsTab.on('click', '#editRouteActionToolRinging-arguments-list [btn-action="remove-route-action-tool-argument"]', (event) => {
				handleRouteActionRemoveArgument(event, editRouteActionToolRingingCustomInputs);
			});
			routeActionsTab.on('click', '#editRouteActionToolPicked-arguments-list [btn-action="remove-route-action-tool-argument"]', (event) => {
				handleRouteActionRemoveArgument(event, editRouteActionToolPickedCustomInputs);
			});
			routeActionsTab.on('click', '#editRouteActionToolEnded-arguments-list [btn-action="remove-route-action-tool-argument"]', (event) => {
				handleRouteActionRemoveArgument(event, editRouteActionToolEndedCustomInputs);
			});
		}
		initActionTabHandlers();

		// Save Button Click Handler
		saveRouteButton.on("click", async (event) => {
			event.preventDefault();

			if (IsSavingRouteManageTab) return;

			// Validate the route
			const validationResult = validateRoutingTab(false);
			if (!validationResult.validated) {
				AlertManager.createAlert({
					type: "danger",
					message: `Validation for required route fields failed.<br><br>${validationResult.errors.join("<br>")}`,
					timeout: 6000,
				});
				return;
			}

			// Check for changes
			const routeChanges = checkRoutingTabHasChanges(false);
			if (!routeChanges.hasChanges) {
				return;
			}

			// Disable button and show spinner
			saveRouteButton.prop("disabled", true);
			saveRouteButtonSpinner.removeClass("d-none");

			IsSavingRouteManageTab = true;

			// Create form data
			const formData = new FormData();
			formData.append("postType", ManageRouteType);
			formData.append("changes", JSON.stringify(routeChanges.changes));

			if (ManageRouteType === "edit") {
				formData.append("existingRouteId", ManageCurrentRouteData.id);
			}

			// Call API to save route
			SaveBusinessRoute(
				formData,
				(saveResponse) => {
					// Update Remove Numbers Route
					if (ManageRouteType === "edit") {
						ManageCurrentRouteData.numbers.forEach((number) => {
							const existingIndex = currentRouteNumbersList.findIndex((num) => num === number);
							if (existingIndex === -1) {
								const numberIndex = BusinessFullData.businessApp.numbers.findIndex((num) => num.id === number);
								BusinessFullData.businessApp.numbers[numberIndex].routeId = null;
							}
						});
					}

					// Set New Route Data
					ManageCurrentRouteData = saveResponse.data;
					currentRouteNumbersList = [...ManageCurrentRouteData.numbers];

					// Set New/Current Numbers Route
					currentRouteNumbersList.forEach((number) => {
						const numberIndex = BusinessFullData.businessApp.numbers.findIndex((num) => num.id === number);
						BusinessFullData.businessApp.numbers[numberIndex].routeId = ManageCurrentRouteData.id;
					});

					// Update route name in header
					currentRouteName.text(ManageCurrentRouteData.general.name);

					if (ManageRouteType === "edit") {
						// Update existing route in business data
						const existingDataIndex = BusinessFullData.businessApp.routings.findIndex((route) => route.id === ManageCurrentRouteData.id);
						BusinessFullData.businessApp.routings[existingDataIndex] = ManageCurrentRouteData;

						// Update route in list
						const routeListElement = routingListContainer.find(`[data-item-id="${ManageCurrentRouteData.id}"]`);
						routeListElement.parent().replaceWith(createRouteListCardElement(ManageCurrentRouteData));
					} else if (ManageRouteType === "new") {
						// Add new route to business data
						BusinessFullData.businessApp.routings.push(ManageCurrentRouteData);

						// Add new route to list
						const newRouteElement = $(createRouteListCardElement(ManageCurrentRouteData));
						routingListContainer.append(newRouteElement);
					}

					$(".none-routes-list-notice").remove();

					// Reset save button state
					saveRouteButton.prop("disabled", true);
					saveRouteButtonSpinner.addClass("d-none");

					IsSavingRouteManageTab = false;

					// Show success message
					AlertManager.createAlert({
						type: "success",
						message: "Route saved successfully.",
						timeout: 6000,
					});

					// Update route type to edit mode
					ManageRouteType = "edit";
					updateUrlForTab(`routings/${ManageCurrentRouteData.id}`);
				},
				(saveError, isUnsuccessful) => {
					var resultMessage = "Check console logs for more details.";
					if (saveError && saveError.message) resultMessage = saveError.message;

					// Show error message
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving route data.",
						resultMessage: resultMessage,
						timeout: 6000,
					});

					console.log("Error occurred while saving route data: ", saveError);

					// Reset save button state
					saveRouteButton.prop("disabled", false);
					saveRouteButtonSpinner.addClass("d-none");

					IsSavingRouteManageTab = false;
				},
			);
		});

		// INIT
		fillRouteList();
	});
}
