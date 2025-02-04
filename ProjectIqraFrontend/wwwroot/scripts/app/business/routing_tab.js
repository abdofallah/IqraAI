/** Dynamic Variables **/
let ManageRouteType = null; // edit or new
let ManageCurrentRouteData = null;

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
const editRouteAgentConversationTypeSelect = routingTab.find("#editRouteAgentConversationTypeSelect");
const editRouteAgentConversationTypeInterruptibleMaxWords = routingTab.find("#editRouteAgentConversationTypeInterruptibleMaxWords");
const editRouteNumberTimezoneSelect = routingTab.find("#editRouteNumberTimezoneSelect");
const editRouteAgentCallerNumberInContextCheck = routingTab.find("#editRouteAgentCallerNumberInContextCheck");
const editRouteAgentRouteNumberInContextCheck = routingTab.find("#editRouteAgentRouteNumberInContextCheck");

// Actions Tab
const editRouteActionToolRinging = routingTab.find("#editRouteActionToolRinging");
const editRouteActionToolRingingInputArgumentsSelect = routingTab.find("#editRouteActionToolRingingInputArgumentsSelect");
const editRouteActionToolRingingInputArgumentsList = routingTab.find("#editRouteActionToolRingingInputArgumentsList");

const editRouteActionToolPicked = routingTab.find("#editRouteActionToolPicked");
const editRouteActionToolPickedInputArgumentsSelect = routingTab.find("#editRouteActionToolPickedInputArgumentsSelect");
const editRouteActionToolPickedInputArgumentsList = routingTab.find("#editRouteActionToolPickedInputArgumentsList");

const editRouteActionToolEnded = routingTab.find("#editRouteActionToolEnded");
const editRouteActionToolEndedInputArgumentsSelect = routingTab.find("#editRouteActionToolEndedInputArgumentsSelect");
const editRouteActionToolEndedInputArgumentsList = routingTab.find("#editRouteActionToolEndedInputArgumentsList");

/** API FUNCTIONS **/

/** Functions **/
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

function ResortMultiLanugageEnabledListNumbers() {
	const tbodyChild = $(routeMultiLanguagesEnabledList.find("tbody")[0]).children();

	tbodyChild.each((index, element) => {
		$(element)
			.find("td:nth-child(2)")
			.text(index + 1);
	});
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

	$("#routing-manager-general-tab").click();
	saveRouteButton.prop("disabled", true);
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
	});
}
