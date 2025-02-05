/** Dynamic Variables **/
let ManageRouteType = null; // edit or new
let ManageCurrentRouteData = null;

let currentRouteAgentSelectedId = null;

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
const editRouteDescriptionInput = routingTab.find("#editRouteDescriptionInput");

// Language Tab
const editRouteDefaultLanguageSelect = routingTab.find("#editRouteDefaultLanguageSelect");

const editRouteMultiLanguageCheck = routingTab.find("#editRouteMultiLanguageCheck");

const editRouteAddMultiLanguageEnabledSelect = routingTab.find("#editRouteAddMultiLanguageEnabledSelect");
const routeMultiLanguagesEnabledList = routingTab.find("#routeMultiLanguagesEnabledList");

// Number Tab
let editChangeRouteNumberModal = null;
const saveChangeRouteNumberButton = $("#editChangeRouteNumberModal #saveChangeRouteNumberButton");

const routeNumbersList = routingTab.find("#routeNumbersList");

// Configuration Tab
const editRouteRegionSelect = routingTab.find("#editRouteRegionSelect");
const editRouteNumberPickupDelay = routingTab.find("#editRouteNumberPickupDelay");
const editRouteNumberSilenceNotify = routingTab.find("#editRouteNumberSilenceNotify");
const editRouteNumberSilenceEnd = routingTab.find("#editRouteNumberSilenceEnd");
const editRouteNumberTotalCallTime = routingTab.find("#editRouteNumberTotalCallTime");

// Agent Tab
let editChangeRouteAgentModal = null;
const routingManagerSelectAgentModalList = $("#editChangeRouteAgentModal #routing-manager-select-agent-modal-list");
const saveChangeRouteAgentButton = $("#editChangeRouteAgentModal #saveChangeRouteAgentButton");

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
			selectedRegionId: "",
			PickUpDelayMS: 0,
			notifyOnSilenceMS: 10000,
			endCallOnSilenceMS: 30000,
			MaxCallTimeS: 600,
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
				selectedToolId: "",
				arguements: null,
			},
			pickedTool: {
				selectedToolId: "",
				arguements: null,
			},
			endedTool: {
				selectedToolId: "",
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

	// Region
	editRouteRegionSelect.empty();
	editRouteRegionSelect.append(`<option value="" disabled selected>Select Region</option>`);
	SpecificationRegionsListData.forEach((region) => {
		if (region.disabledAt !== null) return;
		editRouteRegionSelect.append(`<option region-id="${region.id}">${region.countryCode}-${region.countryRegion}</option>`);
	});

	// Agents Tab
	routingManagerSelectAgentModalList.empty();
	BusinessFullData.businessApp.agents.forEach((agent) => {
		routingManagerSelectAgentModalList.append($(createRouteAgentModalListElement(agent)));
	});

	// Numbers
	routeNumbersList.empty();
	$("#editChangeRouteNumberModal #routing-manager-assign-number-modal-list").each((index, element) => {
		$(element).empty();

		// TODO ADD NUMBERS
	});

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
}

function checkRoutingTabHasChanges(enableDisableButton = true) {
	// TO IMPLEMENT
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
function createRouteNumberModalListElement(numberData) {
	const element = `
		<button type="button" class="list-group-item list-group-item-action" number-id="${numberData.id}" number-provider="${numberData.provider.value}">
			+968 724912671
		</button>
	`;

	return element;
}

/** Init **/
function initRoutingTab() {
	$(document).ready(() => {
		/** INIT **/
		editChangeRouteNumberModal = new bootstrap.Modal($("#editChangeRouteNumberModal"));
		editChangeRouteAgentModal = new bootstrap.Modal($("#editChangeRouteAgentModal"));

		/** Event Handlers */
		addNewRoutingButton.on("click", (event) => {
			event.preventDefault();

			ManageCurrentRouteData = createDefaultRouteObject();
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
			routingManagerAssignNumberModalTab.on("click", "button.nav-link", (event) => {
				event.preventDefault();

				const currentElement = $(event.currentTarget);
				const numberProvider = currentElement.attr("number-provider");
			});
		}
		initNumberTabHandlers();

		/** Agents Tab **/
		function initAgentTabHandlers() {
			$(editChangeRouteAgentModal._element).on("show.bs.modal", (event) => {
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
	});
}
