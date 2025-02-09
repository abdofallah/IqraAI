/** Dynamic Variables **/
let ManageRouteType = null; // edit or new
let ManageCurrentRouteData = null;

let currentRouteNumbersList = [];
let currentRouteAgentSelectedId = "";

let IsSavingRouteManageTab = false;

/** Element Variables  **/
const tooltipTriggerList = document.querySelectorAll('#routing-tab [data-bs-toggle="tooltip"]');
const tooltipList = [...tooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

const routingTab = $("#routing-tab");

const routingHeader = routingTab.find("#routing-header");

// List Tab
const routingListTab = routingTab.find("#routingListTab");

const addNewRoutingButton = routingListTab.find("#addNewRouteButton");

// Manager Tab
const currentRouteName = routingHeader.find("#currentRouteName");
const switchBackToRoutingTabButton = routingHeader.find("#switchBackToRoutingTab");

const saveRouteButton = routingHeader.find("#saveRouteButton");
const saveRouteButtonSpinner = routingHeader.find(".save-button-spinner");

const routingManagerTab = routingTab.find("#routingManagerTab");

// Genral Tab
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

const editRouteIconInput = routingTab.find("#editRouteIconInput");
const editRouteNameInput = routingTab.find("#editRouteNameInput");
const editRouteDescriptionInput = routingTab.find("#editRouteDescriptionInput");

// Language Tab
const editRouteDefaultLanguageSelect = routingTab.find("#editRouteDefaultLanguageSelect");

const editRouteMultiLanguageCheck = routingTab.find("#editRouteMultiLanguageCheck");

const editRouteAddMultiLanguageEnabledSelect = routingTab.find("#editRouteAddMultiLanguageEnabledSelect");
const routeMultiLanguagesEnabledList = routingTab.find("#routeMultiLanguagesEnabledList");

// Number Tab
const editChangeRouteNumberButton = routingTab.find("#editChangeRouteNumberButton");

const editChangeRouteNumberModalElement = $("#editChangeRouteNumberModal");
let editChangeRouteNumberModal = null;
const saveChangeRouteNumberButton = editChangeRouteNumberModalElement.find("#saveChangeRouteNumberButton");

const routeNumbersList = routingTab.find("#routeNumbersList");

// Configuration Tab
const editRouteNumberPickupDelay = routingTab.find("#editRouteNumberPickupDelay");
const editRouteNumberSilenceNotify = routingTab.find("#editRouteNumberSilenceNotify");
const editRouteNumberSilenceEnd = routingTab.find("#editRouteNumberSilenceEnd");
const editRouteNumberTotalCallTime = routingTab.find("#editRouteNumberTotalCallTime");

// Agent Tab
const editChangeRouteAgentModalElement = $("#editChangeRouteAgentModal");
let editChangeRouteAgentModal = null;
const routingManagerSelectAgentModalList = editChangeRouteAgentModalElement.find("#routing-manager-select-agent-modal-list");
const saveChangeRouteAgentButton = editChangeRouteAgentModalElement.find("#saveChangeRouteAgentButton");

const editSelectedRouteAgentIcon = routingTab.find("#editSelectedRouteAgentIcon");
const editSelectedRouteAgentName = routingTab.find("#editSelectedRouteAgentName");

const editRouteAgentDefaultScriptSelect = routingTab.find("#editRouteAgentDefaultScriptSelect");

const editRouteAgentConversationTypeSelect = routingTab.find("#editRouteAgentConversationTypeSelect");
const editRouteAgentConversationTypeInterruptibleMaxWords = routingTab.find("#editRouteAgentConversationTypeInterruptibleMaxWords");

const editRouteNumberTimezoneSelect = routingTab.find("#editRouteNumberTimezoneSelect");

const editRouteAgentCallerNumberInContextCheck = routingTab.find("#editRouteAgentCallerNumberInContextCheck");
const editRouteAgentRouteNumberInContextCheck = routingTab.find("#editRouteAgentRouteNumberInContextCheck");

// Actions Tab
const editRouteActionToolRinging = routingTab.find("#editRouteActionToolRinging");
const editRouteActionToolRingingInputArguementContainer = routingTab.find("#editRouteActionToolRingingContainer .custom-tool-input-arguments");
const editRouteActionToolRingingInputArgumentsSelect = routingTab.find("#editRouteActionToolRingingInputArgumentsSelect");
const editRouteActionToolRingingInputArgumentsList = routingTab.find("#editRouteActionToolRingingInputArgumentsList");

const editRouteActionToolPicked = routingTab.find("#editRouteActionToolPicked");
const editRouteActionToolPickedInputArguementContainer = routingTab.find("#editRouteActionToolPickedContainer .custom-tool-input-arguments");
const editRouteActionToolPickedInputArgumentsSelect = routingTab.find("#editRouteActionToolPickedInputArgumentsSelect");
const editRouteActionToolPickedInputArgumentsList = routingTab.find("#editRouteActionToolPickedInputArgumentsList");

const editRouteActionToolEnded = routingTab.find("#editRouteActionToolEnded");
const editRouteActionToolEndedInputArguementContainer = routingTab.find("#editRouteActionToolEndedContainer .custom-tool-input-arguments");
const editRouteActionToolEndedInputArgumentsSelect = routingTab.find("#editRouteActionToolEndedInputArgumentsSelect");
const editRouteActionToolEndedInputArgumentsList = routingTab.find("#editRouteActionToolEndedInputArgumentsList");

/** API FUNCTIONS **/
function SaveBusinessRoute(formData, successCallback, errorCallback) {
	$.ajax({
		url: `/app/user/business/${BusinessId}/routes/save`,
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

/** Agent List Tab **/
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

function createRouteListElement(routeData) {
	let numberText = "No number assigned";
	if (routeData.numbers.length > 0) {
		const firstNumber = BusinessFullData.businessApp.numbers.find((num) => num.id === routeData.numbers[0]);
		if (firstNumber) {
			numberText = `+${CountriesList[firstNumber.countryCode.toUpperCase()].phone_code} ${firstNumber.number}`;
		}
	}

	return `
        <div class="col-lg-4 col-md-6 col-12">
            <div class="agent-card routing-card d-flex flex-column align-items-start justify-content-center" data-route-id="${routeData.id}">
                <div class="d-flex flex-row align-items-center justify-content-start mb-4">
                    <span class="agent-icon">${routeData.general.emoji}</span>
                    <div class="card-data">
                        <h4>${routeData.general.name}</h4>
                        <h6>${numberText}</h6>
                    </div>
                </div>
                <div>
                    <h5 class="h5-info agent-description">
                        <span>${routeData.general.description}</span>
                    </h5>
                </div>
            </div>
        </div>
    `;
}

/** Agent Manager Tab **/
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
			conversationType: {
				value: 0,
			},
			interruptibleConversationTypeWords: 3,
			timezones: [],
			callerNumberInContext: true,
			routeNumberInContext: true,
		},
		actions: {
			ringingTool: {
				selectedToolId: null,
				arguements: null,
			},
			pickedTool: {
				selectedToolId: null,
				arguements: null,
			},
			endedTool: {
				selectedToolId: null,
				arguements: null,
			},
		},
	};

	return object;
}

function resetAndEmptyRouteManagerTab() {
	// Langauge
	editRouteAddMultiLanguageEnabledSelect.empty();
	editRouteDefaultLanguageSelect.empty();
	editRouteAddMultiLanguageEnabledSelect.append(`<option value="" disabled selected>Add Language</option>`);
	editRouteDefaultLanguageSelect.append(`<option value="" disabled selected>Select Language</option>`);
	BusinessFullData.businessData.languages.forEach((language) => {
		const currentLanguageData = SpecificationLanguagesListData.find((l) => l.id === language);
		editRouteAddMultiLanguageEnabledSelect.append(`<option value="${language}">${language} | ${currentLanguageData.name}</option>`);
		editRouteDefaultLanguageSelect.append(`<option value="${language}">${language} | ${currentLanguageData.name}</option>`);
	});

	// Agents Tab
	routingManagerSelectAgentModalList.empty();
	BusinessFullData.businessApp.agents.forEach((agent) => {
		routingManagerSelectAgentModalList.append($(createRouteAgentModalListElement(agent)));
	});

	// Numbers
	routeNumbersList.find("tbody").empty();
	routeNumbersList.find("tbody").append(`<tr tr-type="none-notice"><td colspan="4">No numbers added yet...</td></tr>`);

	// Actions
	editRouteActionToolRinging.empty();
	editRouteActionToolRingingInputArgumentsSelect.empty();
	editRouteActionToolRingingInputArgumentsList.empty();
	editRouteActionToolRinging.append(`<option value="none" selected>None</option>`);
	editRouteActionToolRingingInputArgumentsSelect.append(`<option value="" disabled selected>Add Input Argument</option>`);

	editRouteActionToolPicked.empty();
	editRouteActionToolPickedInputArgumentsSelect.empty();
	editRouteActionToolPickedInputArgumentsList.empty();
	editRouteActionToolPicked.append(`<option value="none" selected>None</option>`);
	editRouteActionToolPickedInputArgumentsSelect.append(`<option value="" disabled selected>Add Input Argument</option>`);

	editRouteActionToolEnded.empty();
	editRouteActionToolEndedInputArgumentsSelect.empty();
	editRouteActionToolEndedInputArgumentsList.empty();
	editRouteActionToolEnded.append(`<option value="none" selected>None</option>`);
	editRouteActionToolEndedInputArgumentsSelect.append(`<option value="" disabled selected>Add Input Argument</option>`);

	BusinessFullData.businessApp.tools.forEach((tool) => {
		editRouteActionToolRinging.append(`<option value="${tool.id}">${tool.general.name[BusinessDefaultLanguage]}</option>`);

		editRouteActionToolPicked.append(`<option value="${tool.id}">${tool.general.name[BusinessDefaultLanguage]}</option>`);

		editRouteActionToolEnded.append(`<option value="${tool.id}">${tool.general.name[BusinessDefaultLanguage]}</option>`);
	});

	$("#routing-manager-general-tab").click();
	saveRouteButton.prop("disabled", true);

	// Dynamic Variables
	currentRouteAgentSelectedId = "";
}

function checkRoutingTabHasChanges(enableDisableButton = true) {
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

		if (
			changes.language.defaultLanguageCode !== ManageCurrentRouteData.language.defaultLanguageCode ||
			changes.language.multiLanguageEnabled !== ManageCurrentRouteData.language.multiLanguageEnabled
		) {
			hasChanges = true;
		}

		if (changes.language.multiLanguageEnabled) {
			if (!ManageCurrentRouteData.language.enabledMultiLanguages && changes.language.enabledMultiLanguages.length > 0) {
				hasChanges = true;
			} else if (ManageCurrentRouteData.language.enabledMultiLanguages) {
				if (JSON.stringify(changes.language.enabledMultiLanguages) !== JSON.stringify(ManageCurrentRouteData.language.enabledMultiLanguages)) {
					hasChanges = true;
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

		if (JSON.stringify(changes.numbers) !== JSON.stringify(ManageCurrentRouteData.numbers)) {
			hasChanges = true;
		}
	}

	// Agent Tab
	function checkAgentTab() {
		changes.agent = {
			selectedAgentId: currentRouteAgentSelectedId,
			openingScriptId: editRouteAgentDefaultScriptSelect.find("option:selected").val(),
			conversationType: editRouteAgentConversationTypeSelect.val() === "interruptible" ? 0 : 1,
			interruptibleConversationTypeWords: parseInt(editRouteAgentConversationTypeInterruptibleMaxWords.val()),
			timezones: editRouteNumberTimezoneSelect.val() ? [editRouteNumberTimezoneSelect.val()] : [],
			callerNumberInContext: editRouteAgentCallerNumberInContextCheck.is(":checked"),
			routeNumberInContext: editRouteAgentRouteNumberInContextCheck.is(":checked"),
		};

		if (
			changes.agent.selectedAgentId !== ManageCurrentRouteData.agent.selectedAgentId ||
			changes.agent.openingScriptId !== ManageCurrentRouteData.agent.openingScriptId ||
			changes.agent.conversationType !== ManageCurrentRouteData.agent.conversationType.value ||
			changes.agent.interruptibleConversationTypeWords !== ManageCurrentRouteData.agent.interruptibleConversationTypeWords ||
			JSON.stringify(changes.agent.timezones) !== JSON.stringify(ManageCurrentRouteData.agent.timezones) ||
			changes.agent.callerNumberInContext !== ManageCurrentRouteData.agent.callerNumberInContext ||
			changes.agent.routeNumberInContext !== ManageCurrentRouteData.agent.routeNumberInContext
		) {
			hasChanges = true;
		}
	}

	// Actions Tab
	function checkActionsTab() {
		changes.actions = {
			ringingTool: {
				selectedToolId: editRouteActionToolRinging.val() === "none" ? null : editRouteActionToolRinging.val(),
				arguements: null,
			},
			pickedTool: {
				selectedToolId: editRouteActionToolPicked.val() === "none" ? null : editRouteActionToolPicked.val(),
				arguements: null,
			},
			endedTool: {
				selectedToolId: editRouteActionToolEnded.val() === "none" ? null : editRouteActionToolEnded.val(),
				arguements: null,
			},
		};

		// Check Ringing Tool Arguments
		if (changes.actions.ringingTool.selectedToolId) {
			changes.actions.ringingTool.arguements = {};
			editRouteActionToolRingingInputArgumentsList.find(".input-group").each((idx, element) => {
				const input = $(element).find("input");
				changes.actions.ringingTool.arguements[input.attr("input_arguement")] = input.val().trim();
			});
		}

		// Check Picked Tool Arguments
		if (changes.actions.pickedTool.selectedToolId) {
			changes.actions.pickedTool.arguements = {};
			editRouteActionToolPickedInputArgumentsList.find(".input-group").each((idx, element) => {
				const input = $(element).find("input");
				changes.actions.pickedTool.arguements[input.attr("input_arguement")] = input.val().trim();
			});
		}

		// Check Ended Tool Arguments
		if (changes.actions.endedTool.selectedToolId) {
			changes.actions.endedTool.arguements = {};
			editRouteActionToolEndedInputArgumentsList.find(".input-group").each((idx, element) => {
				const input = $(element).find("input");
				changes.actions.endedTool.arguements[input.attr("input_arguement")] = input.val().trim();
			});
		}

		// Compare with original data
		if (JSON.stringify(changes.actions) !== JSON.stringify(ManageCurrentRouteData.actions)) {
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

		if (!editRouteAgentDefaultScriptSelect.val() && !editRouteAgentDefaultScriptSelect.prop("disabled")) {
			validated = false;
			errors.push("Opening script must be selected");

			if (!onlyRemove) {
				editRouteAgentDefaultScriptSelect.addClass("is-invalid");
			}
		} else {
			editRouteAgentDefaultScriptSelect.removeClass("is-invalid");
		}

		if (editRouteAgentConversationTypeSelect.val() === "interruptible") {
			const wordsValue = parseInt(editRouteAgentConversationTypeInterruptibleMaxWords.val());
			if (isNaN(wordsValue) || wordsValue < 1) {
				validated = false;
				errors.push("Words to interrupt must be a positive number");

				if (!onlyRemove) {
					editRouteAgentConversationTypeInterruptibleMaxWords.addClass("is-invalid");
				}
			} else {
				editRouteAgentConversationTypeInterruptibleMaxWords.removeClass("is-invalid");
			}
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
		// Validate Ringing Tool Arguments
		if (editRouteActionToolRinging.val() !== "none") {
			const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === editRouteActionToolRinging.val());
			const requiredArguments = toolData.configuration.inputSchemea.filter((arg) => arg.isRequired);
			const currentArguments = editRouteActionToolRingingInputArgumentsList.find(".input-group input");

			requiredArguments.forEach((reqArg) => {
				const argInput = currentArguments.filter(`[input_arguement="${reqArg.id}"]`);
				if (argInput.length === 0 || !argInput.val().trim()) {
					validated = false;
					errors.push(`Ringing tool: ${reqArg.name[BusinessDefaultLanguage]} is required`);

					if (!onlyRemove && argInput.length > 0) {
						argInput.addClass("is-invalid");
					}
				} else {
					argInput.removeClass("is-invalid");
				}
			});
		}

		// Validate Picked Tool Arguments
		if (editRouteActionToolPicked.val() !== "none") {
			const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === editRouteActionToolPicked.val());
			const requiredArguments = toolData.configuration.inputSchemea.filter((arg) => arg.isRequired);
			const currentArguments = editRouteActionToolPickedInputArgumentsList.find(".input-group input");

			requiredArguments.forEach((reqArg) => {
				const argInput = currentArguments.filter(`[input_arguement="${reqArg.id}"]`);
				if (argInput.length === 0 || !argInput.val().trim()) {
					validated = false;
					errors.push(`Picked tool: ${reqArg.name[BusinessDefaultLanguage]} is required`);

					if (!onlyRemove && argInput.length > 0) {
						argInput.addClass("is-invalid");
					}
				} else {
					argInput.removeClass("is-invalid");
				}
			});
		}

		// Validate Ended Tool Arguments
		if (editRouteActionToolEnded.val() !== "none") {
			const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === editRouteActionToolEnded.val());
			const requiredArguments = toolData.configuration.inputSchemea.filter((arg) => arg.isRequired);
			const currentArguments = editRouteActionToolEndedInputArgumentsList.find(".input-group input");

			requiredArguments.forEach((reqArg) => {
				const argInput = currentArguments.filter(`[input_arguement="${reqArg.id}"]`);
				if (argInput.length === 0 || !argInput.val().trim()) {
					validated = false;
					errors.push(`Ended tool: ${reqArg.name[BusinessDefaultLanguage]} is required`);

					if (!onlyRemove && argInput.length > 0) {
						argInput.addClass("is-invalid");
					}
				} else {
					argInput.removeClass("is-invalid");
				}
			});
		}
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
	const countryData = CountriesList[numberData.countryCode.toUpperCase()];

	const element = `
		<tr>
			<td>${countryData["Alpha-2 code"]} ${countryData.phone_code}</td>
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
	const countryData = CountriesList[numberData.countryCode.toUpperCase()];

	// TODO CHANGE
	const isNumberActiveInRoute = currentRouteNumbersList.findIndex((number) => number === numberData.id) !== -1;
	const isUsedByOtherRoute = numberData.routeId !== null && numberData.routeId !== ManageCurrentRouteData.id;

	const element = `
		<button type="button" class="list-group-item list-group-item-action ${isUsedByOtherRoute || isNumberActiveInRoute ? "disabled" : ""}" button-type="add-number-to-route" number-id="${numberData.id}" number-provider="${numberData.provider.value}">
			${countryData.phone_code} ${numberData.number} ${isUsedByOtherRoute ? "(Used by another route)" : ""} ${isNumberActiveInRoute ? "(Already added)" : ""}
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
		});

		switchBackToRoutingTabButton.on("click", (event) => {
			event.preventDefault();

			showRoutingListTab();
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
			},
		});

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
			});
		}
		initNumberTabHandlers();

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

				editRouteAgentDefaultScriptSelect.prop("disabled", false);

				editRouteAgentDefaultScriptSelect.empty();
				editRouteAgentDefaultScriptSelect.append(`<option value="" disabled selected>Select Script</option>`);
				agentData.scripts.forEach((script) => {
					editRouteAgentDefaultScriptSelect.append(`<option value="${script.id}">${script.general.name[BusinessDefaultLanguage]}</option>`);
				});

				editChangeRouteAgentModal.hide();
			});

			editRouteAgentConversationTypeSelect.on("change", (event) => {
				const selectedValue = editRouteAgentConversationTypeSelect.val();

				if (!selectedValue) return;

				const interruptibleBox = $('.route-conversation-type-box[box-type="interruptible"]');

				if (selectedValue === "interruptible") {
					interruptibleBox.removeClass("d-none");
				} else if (selectedValue === "turnbyturn") {
					interruptibleBox.addClass("d-none");
				}
			});
		}
		initAgentTabHandlers();

		/** Action Tab Events **/
		function initActionTabHandlers() {
			// Ringing Event
			editRouteActionToolRinging.on("change", (event) => {
				const selectedValue = editRouteActionToolRinging.val();

				editRouteActionToolRingingInputArgumentsSelect.empty();
				editRouteActionToolRingingInputArgumentsList.empty();
				editRouteActionToolRingingInputArgumentsSelect.append(`<option value="" disabled selected>Add Input Argument</option>`);

				if (selectedValue === "none") {
					editRouteActionToolRingingInputArguementContainer.addClass("d-none");
					return;
				}

				editRouteActionToolRingingInputArguementContainer.removeClass("d-none");

				const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === selectedValue);

				toolData.configuration.inputSchemea.forEach((inputArguement) => {
					editRouteActionToolRingingInputArgumentsSelect.append(`<option value="${inputArguement.id}">${inputArguement.name[BusinessDefaultLanguage]}${inputArguement.isRequired ? "*" : ""}</option>`);
				});
			});
			editRouteActionToolRingingInputArgumentsSelect.on("change", (event) => {
				const selectedValue = editRouteActionToolRingingInputArgumentsSelect.val();

				if (selectedValue === "") return;

				const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === editRouteActionToolRinging.val());
				const inputArguementData = toolData.configuration.inputSchemea.find((inputArguement) => inputArguement.id === selectedValue);

				editRouteActionToolRingingInputArgumentsList.append(`
					<div class="input-group mb-1">
						<span class="input-group-text">${inputArguementData.name[BusinessDefaultLanguage]}</span>
						<input type="text" class="form-control" input_arguement="${inputArguementData.id}" placeholder="Enter ${inputArguementData.type.name} variable" value="">
						<button class="btn btn-danger" btn-action="remove-route-action-tool-arguement" input_arguement="${inputArguementData.id}">
							<i class="fa-regular fa-trash"></i>
						</button>
					</div>
				`);

				editRouteActionToolRingingInputArgumentsSelect.find(`option[value="${selectedValue}"]`).remove();

				editRouteActionToolRingingInputArgumentsSelect.val("");
			});
			editRouteActionToolRingingInputArgumentsList.on("click", '[btn-action="remove-route-action-tool-arguement"]', (event) => {
				event.preventDefault();
				event.stopPropagation();
				event.stopImmediatePropagation();

				const currentElement = $(event.currentTarget);
				const inputArguementId = currentElement.attr("input_arguement");

				const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === editRouteActionToolRinging.val());
				const inputArguementData = toolData.configuration.inputSchemea.find((inputArguement) => inputArguement.id === inputArguementId);

				editRouteActionToolRingingInputArgumentsSelect.append(`<option value="${inputArguementData.id}">${inputArguementData.name[BusinessDefaultLanguage]}</option>`);

				currentElement.parent().remove();
			});

			// Tool Picked Up Event
			editRouteActionToolPicked.on("change", (event) => {
				const selectedValue = editRouteActionToolPicked.val();

				editRouteActionToolPickedInputArgumentsSelect.empty();
				editRouteActionToolPickedInputArgumentsList.empty();
				editRouteActionToolPickedInputArgumentsSelect.append(`<option value="" disabled selected>Add Input Argument</option>`);

				if (selectedValue === "none") {
					editRouteActionToolPickedInputArguementContainer.addClass("d-none");
					return;
				}

				editRouteActionToolPickedInputArguementContainer.removeClass("d-none");

				const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === selectedValue);

				toolData.configuration.inputSchemea.forEach((inputArguement) => {
					editRouteActionToolPickedInputArgumentsSelect.append(`<option value="${inputArguement.id}">${inputArguement.name[BusinessDefaultLanguage]}${inputArguement.isRequired ? "*" : ""}</option>`);
				});
			});
			editRouteActionToolPickedInputArgumentsSelect.on("change", (event) => {
				const selectedValue = editRouteActionToolPickedInputArgumentsSelect.val();

				if (selectedValue === "") return;

				const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === editRouteActionToolPicked.val());
				const inputArguementData = toolData.configuration.inputSchemea.find((inputArguement) => inputArguement.id === selectedValue);

				editRouteActionToolPickedInputArgumentsList.append(`
			<div class="input-group mb-1">
				<span class="input-group-text">${inputArguementData.name[BusinessDefaultLanguage]}</span>
				<input type="text" class="form-control" input_arguement="${inputArguementData.id}" placeholder="Enter ${inputArguementData.type.name} variable" value="">
				<button class="btn btn-danger" btn-action="remove-route-action-tool-arguement" input_arguement="${inputArguementData.id}">
					<i class="fa-regular fa-trash"></i>
				</button>
			</div>
		`);

				editRouteActionToolPickedInputArgumentsSelect.find(`option[value="${selectedValue}"]`).remove();
				editRouteActionToolPickedInputArgumentsSelect.val("");
			});
			editRouteActionToolPickedInputArgumentsList.on("click", '[btn-action="remove-route-action-tool-arguement"]', (event) => {
				event.preventDefault();
				event.stopPropagation();
				event.stopImmediatePropagation();

				const currentElement = $(event.currentTarget);
				const inputArguementId = currentElement.attr("input_arguement");

				const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === editRouteActionToolPicked.val());
				const inputArguementData = toolData.configuration.inputSchemea.find((inputArguement) => inputArguement.id === inputArguementId);

				editRouteActionToolPickedInputArgumentsSelect.append(`<option value="${inputArguementData.id}">${inputArguementData.name[BusinessDefaultLanguage]}</option>`);

				currentElement.parent().remove();
			});

			// Tool Ended Event
			editRouteActionToolEnded.on("change", (event) => {
				const selectedValue = editRouteActionToolEnded.val();

				editRouteActionToolEndedInputArgumentsSelect.empty();
				editRouteActionToolEndedInputArgumentsList.empty();
				editRouteActionToolEndedInputArgumentsSelect.append(`<option value="" disabled selected>Add Input Argument</option>`);

				if (selectedValue === "none") {
					editRouteActionToolEndedInputArguementContainer.addClass("d-none");
					return;
				}

				editRouteActionToolEndedInputArguementContainer.removeClass("d-none");

				const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === selectedValue);

				toolData.configuration.inputSchemea.forEach((inputArguement) => {
					editRouteActionToolEndedInputArgumentsSelect.append(`<option value="${inputArguement.id}">${inputArguement.name[BusinessDefaultLanguage]}${inputArguement.isRequired ? "*" : ""}</option>`);
				});
			});
			editRouteActionToolEndedInputArgumentsSelect.on("change", (event) => {
				const selectedValue = editRouteActionToolEndedInputArgumentsSelect.val();

				if (selectedValue === "") return;

				const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === editRouteActionToolEnded.val());
				const inputArguementData = toolData.configuration.inputSchemea.find((inputArguement) => inputArguement.id === selectedValue);

				editRouteActionToolEndedInputArgumentsList.append(`
			<div class="input-group mb-1">
				<span class="input-group-text">${inputArguementData.name[BusinessDefaultLanguage]}</span>
				<input type="text" class="form-control" input_arguement="${inputArguementData.id}" placeholder="Enter ${inputArguementData.type.name} variable" value="">
				<button class="btn btn-danger" btn-action="remove-route-action-tool-arguement" input_arguement="${inputArguementData.id}">
					<i class="fa-regular fa-trash"></i>
				</button>
			</div>
		`);

				editRouteActionToolEndedInputArgumentsSelect.find(`option[value="${selectedValue}"]`).remove();
				editRouteActionToolEndedInputArgumentsSelect.val("");
			});
			editRouteActionToolEndedInputArgumentsList.on("click", '[btn-action="remove-route-action-tool-arguement"]', (event) => {
				event.preventDefault();
				event.stopPropagation();
				event.stopImmediatePropagation();

				const currentElement = $(event.currentTarget);
				const inputArguementId = currentElement.attr("input_arguement");

				const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === editRouteActionToolEnded.val());
				const inputArguementData = toolData.configuration.inputSchemea.find((inputArguement) => inputArguement.id === inputArguementId);

				editRouteActionToolEndedInputArgumentsSelect.append(`<option value="${inputArguementData.id}">${inputArguementData.name[BusinessDefaultLanguage]}</option>`);

				currentElement.parent().remove();
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
					// Update current route data with saved data
					ManageCurrentRouteData = saveResponse.data;

					// Update route name in header
					currentRouteName.text(ManageCurrentRouteData.general.name);

					if (ManageRouteType === "edit") {
						// Update existing route in business data
						const existingDataIndex = BusinessFullData.businessApp.routes.findIndex((route) => route.id === ManageCurrentRouteData.id);
						BusinessFullData.businessApp.routes[existingDataIndex] = ManageCurrentRouteData;

						// Update route in list
						const routeListElement = routingListTab.find(`[data-route-id="${ManageCurrentRouteData.id}"]`);
						routeListElement.replaceWith(createRouteListElement(ManageCurrentRouteData));
					} else if (ManageRouteType === "new") {
						// Add new route to business data
						BusinessFullData.businessApp.routes.push(ManageCurrentRouteData);

						// Add new route to list
						const newRouteElement = $(createRouteListElement(ManageCurrentRouteData));
						routingListTab.find(".row").append(newRouteElement);
					}

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
				},
				(saveError, isUnsuccessful) => {
					// Show error message
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving route data. Check browser console for logs.",
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
	});
}
