/** Global Variables & ENUMs **/
const OutboundCallNumberType = {
	Single: 0,
	Bulk: 1,
};

/** Dynamic State Variables **/
let IsInitiatingCall = false;
let CurrentMakeCallType = OutboundCallNumberType.Single;
let SelectedCampaignId = null;
let SelectedBulkFromFileObject = null;
let makeCallFormInitialState = {};

/** Element Variables **/
const makeCallsTab = $("#make-calls-tab");
// Header
const initiateCallButton = makeCallsTab.find("#makeCallInitiateButton");
const initiateCallButtonSpinner = initiateCallButton.find(".spinner-border");
const resetButton = makeCallsTab.find("#makeCallResetButton");
// Configuration
const makeCallTypeSingleBox = makeCallsTab.find(".make-call-type-box-choose[box-type='single']");
const makeCallTypeBulkBox = makeCallsTab.find(".make-call-type-box-choose[box-type='bulk']");
// Campaign Selection
const editSelectedMakeCallCampaignIcon = makeCallsTab.find("#editSelectedMakeCallCampaignIcon");
const editSelectedMakeCallCampaignInput = makeCallsTab.find("#editSelectedMakeCallCampaign");
const editChangeMakeCallCampaignButton = makeCallsTab.find("#editChangeMakeCallCampaignButton");
// Single Number Container
const makeCallNumberSingleContainer = makeCallsTab.find("#make-call-number-single-container");
const makeCallToNumberInput = makeCallsTab.find("#makeCallToNumberInput");
// Bulk Number Container
const makeCallNumberBulkContainer = makeCallsTab.find("#make-call-number-bulk-container");
const makeCallToNumberBulkInput = makeCallsTab.find("#makeCallToNumberBulkInput");
// Schedule
const makeCallScheduleDateTimeInput = makeCallsTab.find("#makeCallScheduleDateTimeInput");
const makeCallMaxScheduleDateTimeInput = makeCallsTab.find("#makeCallMaxScheduleDateTimeInput");
// Dynamic Variables
const makeCallDynamicVariablesSelectLabel = makeCallsTab.find("#makeCallDynamicVariablesSelectLabel");
const makeCallDynamicVariablesSelect = makeCallsTab.find("#makeCallDynamicVariablesSelect");
const makeCallDynamicVariablesSelectAddButton = makeCallsTab.find("#makeCallDynamicVariablesSelectAddButton");
const makeCallDynamicVariablesList = makeCallsTab.find("#makeCallDynamicVariablesList");
// Metadata
const makeCallMetadataSelectLabel = makeCallsTab.find("#makeCallMetadataSelectLabel");
const makeCallMetadataSelect = makeCallsTab.find("#makeCallMetadataSelect");
const makeCallMetadataSelectAddButton = makeCallsTab.find("#makeCallMetadataSelectAddButton");
const makeCallMetadataList = makeCallsTab.find("#makeCallMetadataList");

// Campaign Selection Modal
const editChangeMakeCallCampaignModalElement = makeCallsTab.find("#editChangeMakeCallCampaignModal");
let editChangeMakeCallCampaignModal = null;
const inputMakeCallSelectCampaignSearch = editChangeMakeCallCampaignModalElement.find("#inputMakeCallSelectCampaignSearch");
const searchMakeCallSelectCampaignListButton = editChangeMakeCallCampaignModalElement.find("#searchMakeCallSelectCampaignList");
const makeCallSelectCampaignModalList = editChangeMakeCallCampaignModalElement.find("#make-call-select-campaign-modal-list");
const saveMakeCallCampaignButton = editChangeMakeCallCampaignModalElement.find("#saveMakeCallCampaignButton");

/** API FUNCTIONS **/
function InitiateCall(data, successCallback, errorCallback) {
	$.ajax({
		url: `/app/user/business/${CurrentBusinessId}/calls/initiate`,
		type: "POST",
		data: data,
		processData: false,
		contentType: false,
		dataType: "json",
		success: (response) => {
			successCallback(response);
		},
		error: (xhr, status, error) => {
			errorCallback({ message: `Error: ${status || "Request Failed"}` }, false);
		},
	});
}

/** CORE FUNCTIONS **/
function resetMakeCallForm() {
	// Reset State Variables
	CurrentMakeCallType = OutboundCallNumberType.Single;
	SelectedCampaignId = null;
	SelectedBulkFromFileObject = null;

	// Reset Form Elements
	editSelectedMakeCallCampaignIcon.text("-");
	editSelectedMakeCallCampaignInput.val("").attr("placeholder", "Select Campaign");

	makeCallToNumberInput.val("");

	makeCallToNumberBulkInput.val(null); // Resets the file input

	// Schedule
	makeCallScheduleDateTimeInput.val(getCurrentLocalISOString());
	makeCallMaxScheduleDateTimeInput.val("");

	makeCallDynamicVariablesSelect.empty();
	makeCallDynamicVariablesSelect.append("<option value='' is-custom='true' selected>Custom Variable</option>");
	makeCallDynamicVariablesList.empty();
	makeCallMetadataSelect.empty();
	makeCallMetadataSelect.append("<option value='' is-custom='true' selected>Custom Variable</option>");
    makeCallMetadataList.empty();

	// Reset validation and buttons
	makeCallsTab.find(".is-invalid").removeClass("is-invalid");
	initiateCallButton.prop("disabled", false);
	initiateCallButtonSpinner.addClass("d-none");
	resetButton.prop("disabled", false);

	// Set initial call type display
	handleCallTypeChange(OutboundCallNumberType.Single);

	// Capture the state AFTER resetting to establish the baseline for changes
	captureInitialFormState();
}

function validateMakeCallConfig(onlyRemoveErrors = false) {
	let isValid = true;
	const errors = [];
	const addError = (message, element) => {
		isValid = false;
		errors.push(message);
		if (!onlyRemoveErrors && element) element.addClass("is-invalid");
	};
	const removeError = (element) => {
		if (element) element.removeClass("is-invalid");
	};

	if (!onlyRemoveErrors) makeCallsTab.find(".is-invalid").removeClass("is-invalid");

	// Campaign Validation
	removeError(editSelectedMakeCallCampaignInput);
	if (!SelectedCampaignId) addError("A Campaign must be selected.", editSelectedMakeCallCampaignInput);

	// Number Validation
	if (CurrentMakeCallType === OutboundCallNumberType.Single) {
		removeError(makeCallToNumberInput);
		const toNumber = makeCallToNumberInput.val().trim();
		if (!toNumber) addError("'Call To' number cannot be empty.", makeCallToNumberInput);
		else if (!/^\+?[1-9]\d{1,14}$/.test(toNumber.replace(/[\s()-]/g, ""))) {
			addError("'Call To' number format is invalid (e.g., +12223334444).", makeCallToNumberInput);
		}
	} else {
		// Bulk Validation
		removeError(makeCallToNumberBulkInput);
		if (!SelectedBulkFromFileObject) addError("A CSV file must be selected for bulk calls.", makeCallToNumberBulkInput);
		else if (!SelectedBulkFromFileObject.name.toLowerCase().endsWith(".csv")) {
			addError("Invalid file type. Please select a .csv file.", makeCallToNumberBulkInput);
		}
	}

	// Schedule Validation
	removeError(makeCallScheduleDateTimeInput);
	removeError(makeCallMaxScheduleDateTimeInput);
	const scheduleValue = makeCallScheduleDateTimeInput.val();
	const maxScheduleValue = makeCallMaxScheduleDateTimeInput.val();
	if (!scheduleValue) {
		addError("Schedule date and time must be set.", makeCallScheduleDateTimeInput);
	}
	if (!maxScheduleValue) {
        addError("Max schedule date and time must be set.", makeCallMaxScheduleDateTimeInput);
	}
	if (scheduleValue && maxScheduleValue) {
		try {
			let scheduleDate = new Date(scheduleValue);
			let maxScheduleDate = new Date(maxScheduleValue);

			if (scheduleDate > maxScheduleDate) {
				addError("Max schedule date and time must be greater than or equal to schedule date and time.", makeCallMaxScheduleDateTimeInput);
			}
		}
		catch (e) {
			addError("Schedule date and time is invalid.", makeCallScheduleDateTimeInput);
			addError("Max Schedule date and time is invalid.", makeCallMaxScheduleDateTimeInput);
		}
	}

	// VARIBALES TAB
	function validateVariableElement(variablesList, variableSelectElement, variableListName, campaignVariablesData = null) {
		var addedKeys = [];

		variablesList.find(".input-group").each((index, element) => {
			var keyElement = $(element).find('[data-type="key"]');

			const key = keyElement.val()?.trim();

			if (!key || key === "") {
				addError(`${variableListName} item (${index + 1}) key must be set.`, keyElement);
				return true;
			}

			if (addedKeys.includes(key)) {
				addError(`${variableListName} item key '${key}' must be unique.`, keyElement);
				return true;
			}

			addedKeys.push(key);

			const value = $(element).find('[data-type="value"]').val()?.trim();

			if (campaignVariablesData != null) {
				var keyData = campaignVariablesData.find((v) => v.key === key);
				if (keyData && keyData != null) {
					if (!keyData.isEmptyOrNullAllowed &&
						(!value || value === "")
					) {
						addError(`${variableListName} item '${key}' value can not be empty.`, $(element).find('[data-type="value"]'));
						return true;
					}
				}
			}
		});

		if (campaignVariablesData != null && CurrentMakeCallType == OutboundCallNumberType.Single) {
			campaignVariablesData.forEach((variableData) => {
				if (!addedKeys.includes(variableData.key)) {
					addError(`${variableListName} item key '${variableData.key}' is required.`, variableSelectElement);
                }
			});
		}
	}

	removeError(makeCallDynamicVariablesSelect);
	makeCallDynamicVariablesList.find(".is-invalid").removeClass("is-invalid");

    removeError(makeCallMetadataSelect);
	makeCallMetadataList.find(".is-invalid").removeClass("is-invalid");	

	if (SelectedCampaignId) {
		const campaignData = BusinessFullData.businessApp.telephonyCampaigns.find((c) => c.id === SelectedCampaignId);

		validateVariableElement(makeCallDynamicVariablesList, makeCallDynamicVariablesSelect,  "Dynamic Variables", campaignData.variables.dynamicVariables);
		validateVariableElement(makeCallMetadataList, makeCallMetadataSelect, "Metadata", campaignData.variables.metadata);
	}
	else {
		validateVariableElement(makeCallDynamicVariablesList, makeCallDynamicVariablesSelect, "Dynamic Variables");
		validateVariableElement(makeCallMetadataList, makeCallMetadataSelect, "Metadata");
	}

	return { validated: isValid, errors: errors };
}

function gatherMakeCallConfig() {
	function getVariableData(elementList) {
		const data = {};

		elementList.find(".input-group").each((index, element) => {
			const key = $(element).find('[data-type="key"]').val()?.trim();
			var value = $(element).find('[data-type="value"]').val()?.trim();

			if (!key) return;

			if (!value || value == null) value = "";

			data[key] = value;
		});

		return data;
	}

	const config = {
		campaignId: SelectedCampaignId,
		number: {
			type: CurrentMakeCallType,
			toNumber: CurrentMakeCallType === OutboundCallNumberType.Single ? makeCallToNumberInput.val().trim() : null,
		},
		schedule: {
			dateTimeUTC: makeCallScheduleDateTimeInput.val() ? new Date(makeCallScheduleDateTimeInput.val()).toISOString() : "",
			maxDateTimeUTC: makeCallMaxScheduleDateTimeInput.val() ? new Date(makeCallMaxScheduleDateTimeInput.val()).toISOString() : "",
		},
		dynamicVariables: getVariableData(makeCallDynamicVariablesList),
		metadata: getVariableData(makeCallMetadataList)
	};

	return config;
}

/** UNSAVED CHANGES LOGIC **/
function captureInitialFormState() {
	makeCallFormInitialState = {
		campaignId: SelectedCampaignId,
		callType: CurrentMakeCallType,
		toNumber: makeCallToNumberInput.val(),
		bulkFileSelected: !!SelectedBulkFromFileObject,
		scheduleDateTime: makeCallScheduleDateTimeInput.val(),
		maxScheduleDateTime: makeCallMaxScheduleDateTimeInput.val(),
		dynamicVariables: {},
		metadata: {}
	};
}

function checkMakeCallTabHasChanges() {
	if (makeCallFormInitialState.campaignId !== SelectedCampaignId) return true;
	if (makeCallFormInitialState.callType !== CurrentMakeCallType) return true;

	if (CurrentMakeCallType === OutboundCallNumberType.Single) {
		if (makeCallFormInitialState.toNumber !== makeCallToNumberInput.val()) return true;
	} else {
		if (makeCallFormInitialState.bulkFileSelected !== !!SelectedBulkFromFileObject) return true;
	}

	if (makeCallFormInitialState.scheduleDateTime !== makeCallScheduleDateTimeInput.val()) return true;
	if (makeCallFormInitialState.maxScheduleDateTime !== makeCallMaxScheduleDateTimeInput.val()) return true;

	if (Object.keys(makeCallFormInitialState.dynamicVariables).length != 0) return true;
	if (Object.keys(makeCallFormInitialState.metadata).length != 0) return true;

	return false; // No changes detected
}

/** HELPER & UI FUNCTIONS **/
function handleCallTypeChange(selectedType) {
	if (selectedType === OutboundCallNumberType.Single) {
		CurrentMakeCallType = OutboundCallNumberType.Single;
		makeCallTypeSingleBox.addClass("active");
		makeCallTypeBulkBox.removeClass("active");
		makeCallNumberSingleContainer.removeClass("d-none").addClass("show");
		makeCallNumberBulkContainer.addClass("d-none").removeClass("show");
		makeCallDynamicVariablesSelectLabel.text("Dynamic Variables");
        makeCallMetadataSelectLabel.text("Metadata");
	} else {
		// bulk
		CurrentMakeCallType = OutboundCallNumberType.Bulk;
		makeCallTypeSingleBox.removeClass("active");
		makeCallTypeBulkBox.addClass("active");
		makeCallNumberSingleContainer.addClass("d-none").removeClass("show");
		makeCallNumberBulkContainer.removeClass("d-none").addClass("show");
		makeCallDynamicVariablesSelectLabel.text("Default Dynamic Variables");
        makeCallMetadataSelectLabel.text("Default Metadata");
	}
	validateMakeCallConfig(true);
}

// Campaign Modal Functions
function createMakeCallCampaignModalListElement(campaignData) {
	const campaignName = campaignData.general.name;
	const campaignEmoji = campaignData.general.emoji;
	return `<button type="button" class="list-group-item list-group-item-action" campaign-id="${campaignData.id}"><span>${campaignEmoji} ${campaignName}</span></button>`;
}

function fillMakeCallCampaignModalList() {
	const searchTerm = inputMakeCallSelectCampaignSearch.val().toLowerCase().trim();
	makeCallSelectCampaignModalList.empty();
	const campaigns = BusinessFullData.businessApp.telephonyCampaigns;
	let campaignsFound = false;

	if (campaigns.length === 0) {
		makeCallSelectCampaignModalList.append('<span class="list-group-item">No campaigns created yet.</span>');
		return;
	}

	campaigns.forEach((campaign) => {
		const campaignName = campaign.name;
		if (!searchTerm || campaignName.toLowerCase().includes(searchTerm)) {
			makeCallSelectCampaignModalList.append(createMakeCallCampaignModalListElement(campaign));
			campaignsFound = true;
		}
	});

	if (!campaignsFound) {
		makeCallSelectCampaignModalList.append(`<span class="list-group-item">No campaigns found matching '${searchTerm}'.</span>`);
	}
}

function getCurrentLocalISOString() {
	const now = new Date();
	// Adjust for the local timezone offset
	const timezoneOffset = now.getTimezoneOffset() * 60000; // in milliseconds
	const localISOTime = new Date(now - timezoneOffset).toISOString().slice(0, 16);
	return localISOTime;
}

/** EVENT HANDLERS **/
function initMakeCallHandlers() {
	// Call Type Selection
	makeCallTypeSingleBox.on("click", () => handleCallTypeChange(OutboundCallNumberType.Single));
	makeCallTypeBulkBox.on("click", () => handleCallTypeChange(OutboundCallNumberType.Bulk));

	// Campaign Selection Modal
	editChangeMakeCallCampaignButton.on("click", (event) => {
		event.preventDefault();
		fillMakeCallCampaignModalList();
		inputMakeCallSelectCampaignSearch.val("");
		makeCallSelectCampaignModalList.find("button.active").removeClass("active");
		if (SelectedCampaignId) {
			makeCallSelectCampaignModalList.find(`button[campaign-id="${SelectedCampaignId}"]`).addClass("active");
		}
		saveMakeCallCampaignButton.prop("disabled", true);
		editChangeMakeCallCampaignModal.show();
	});

	searchMakeCallSelectCampaignListButton.on("click", fillMakeCallCampaignModalList);
	inputMakeCallSelectCampaignSearch.on("keypress", (e) => {
		if (e.which === 13) fillMakeCallCampaignModalList();
	});

	makeCallSelectCampaignModalList.on("click", "button", (event) => {
		const $button = $(event.currentTarget);
		const $activeButton = makeCallSelectCampaignModalList.find("button.active");

		if ($activeButton.length != 0) {
			if ($button[0] == $activeButton[0]) return;
			$activeButton.removeClass("active");
		}

		$button.addClass("active");
		saveMakeCallCampaignButton.prop("disabled", false);
	});

	saveMakeCallCampaignButton.on("click", (event) => {
		event.preventDefault();
		const $activeButton = makeCallSelectCampaignModalList.find("button.active");
		if ($activeButton.length) {
			const campaignId = $activeButton.attr("campaign-id");

			if (campaignId === SelectedCampaignId) {
				editChangeMakeCallCampaignModal.hide();
				return;
			}

			const campaignData = BusinessFullData.businessApp.telephonyCampaigns.find((c) => c.id === campaignId);
			if (campaignData) {
				SelectedCampaignId = campaignId;
				editSelectedMakeCallCampaignIcon.text(campaignData.general.emoji);
				editSelectedMakeCallCampaignInput.val(campaignData.general.name);
				editChangeMakeCallCampaignModal.hide();

				makeCallDynamicVariablesSelect.empty();
				makeCallDynamicVariablesSelect.append("<option value='' is-custom='true' selected>Custom Variable</option>");
				campaignData.variables.dynamicVariables.forEach((variable) => {
					makeCallDynamicVariablesSelect.append(`<option value='${variable.key}' is-custom='false' is-required='${variable.isRequired ? "true" : "false"}'>${(variable.isRequired ? "*" : "")}${variable.key}</option>`);
				});

				makeCallMetadataSelect.empty();
				makeCallMetadataSelect.append("<option value='' is-custom='true' selected>Custom Variable</option>");
                campaignData.variables.metadata.forEach((variable) => {
					makeCallMetadataSelect.append(`<option value='${variable.key}' is-custom='false' is-required='${variable.isRequired ? "true" : "false"}'>${(variable.isRequired ? "*" : "")}${variable.key}</option>`);
                });

				validateMakeCallConfig(true);
			} else {
				AlertManager.createAlert({
					type: "danger",
					message: "Selected campaign data not found.",
					timeout: 6000
				});
			}
		}
	});

	// Input handlers
	makeCallToNumberInput.on("input", () => validateMakeCallConfig(true));
	makeCallToNumberBulkInput.on("change", (event) => {
		SelectedBulkFromFileObject = event.target.files.length > 0 ? event.target.files[0] : null;
		validateMakeCallConfig(true);
	});
	makeCallScheduleDateTimeInput.on("input", () => validateMakeCallConfig(true));
	makeCallMaxScheduleDateTimeInput.on("input", () => validateMakeCallConfig(true));

	// Variables Handlers
	function addVariableListElement(isCustom, key, isRequired) {
		return `
			<div class="input-group mt-1">
			  <input type="text" class="form-control" placeholder="Key" data-type="key" ${(isCustom ? "" : `value="${key}" disabled`)} style="max-width: 300px">
			  <input type="text" class="form-control" placeholder="Value" data-type="value">
			  <button class="btn btn-danger" button-type="remove-variable" is-custom="${isCustom ? "true" : "false"}" ${(isCustom ? "" : `static-key="${key}" is-required="${isRequired ? "true" : "false"}"`)}><i class="fa-regular fa-trash"></i></button>
			</div>
		`;
	}

	function onVariableSelectAddClick(event, variablesList, selectElement) {
		event.preventDefault();

		const selectedElement = selectElement.find("option:selected");
		if (selectedElement.length == 0) return;

		var isCustomAdd = selectedElement.attr("is-custom") === "true";
		if (isCustomAdd) {
			variablesList.append($(addVariableListElement(true, null, null)))
		}
		else {
			const variableKey = selectedElement.val();
            const isRequired = selectedElement.attr("is-required") === "true";

			variablesList.append($(addVariableListElement(false, variableKey, isRequired)))

			selectedElement.remove();
		}
	}

	makeCallDynamicVariablesSelectAddButton.on("click", (event) => {
		onVariableSelectAddClick(event, makeCallDynamicVariablesList, makeCallDynamicVariablesSelect);
		validateMakeCallConfig(true);
	});
	makeCallMetadataSelectAddButton.on("click", (event) => {
		onVariableSelectAddClick(event, makeCallMetadataList, makeCallMetadataSelect);
		validateMakeCallConfig(true);
	});
	function onRemoveVariableClick(event, selectElement) {
        event.preventDefault();

		var currentTarget = $(event.currentTarget);

		var isCustom = currentTarget.attr("is-custom") === "true";
		if (!isCustom) {
            var staticKey = currentTarget.attr("static-key");
			var isRequired = currentTarget.attr("is-required") === "true";

			selectElement.append(`<option value="${staticKey}" is-custom="false" static-key="${staticKey}">${isRequired ? "*" : ""}${staticKey}</option>`);
		}

		currentTarget.parent().remove();
    }

	makeCallDynamicVariablesList.on("click", "button[button-type='remove-variable']", (event) => {
		onRemoveVariableClick(event, makeCallDynamicVariablesSelect);
		validateMakeCallConfig(true);
	});
	makeCallMetadataList.on("click", "button[button-type='remove-variable']", (event) => {
		onRemoveVariableClick(event, makeCallMetadataSelect);
		validateMakeCallConfig(true);
	});

	makeCallDynamicVariablesList.on("input", "input", (event) => {
		validateMakeCallConfig(true);
	});
	makeCallMetadataList.on("input", "input", (event) => {
		validateMakeCallConfig(true);
	});
}

/** INITIALIZATION **/
function initMakeCallsTab() {
	// Initialize Bootstrap components
	const tooltipTriggerList = makeCallsTab[0].querySelectorAll('[data-bs-toggle="tooltip"]');
	[...tooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));
	editChangeMakeCallCampaignModal = new bootstrap.Modal(editChangeMakeCallCampaignModalElement[0]);

	// Set Initial Form State
	resetMakeCallForm();

	// Attach Event Handlers
	initiateCallButton.on("click", async (event) => {
		event.preventDefault();
		if (IsInitiatingCall) return;

		const validationResult = validateMakeCallConfig(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `Please fix the errors:<br><br>${validationResult.errors.join("<br>")}`,
				timeout: 3000
			});
			makeCallsTab.find(".is-invalid").first().focus();
			return;
		}

		// Optional: Add a confirmation dialog here if desired
		IsInitiatingCall = true;
		initiateCallButton.prop("disabled", true);
		initiateCallButtonSpinner.removeClass("d-none");
		resetButton.prop("disabled", true);

		const callConfigData = gatherMakeCallConfig();
		const requestData = new FormData();
		requestData.append("config", JSON.stringify(callConfigData));

		if (CurrentMakeCallType === OutboundCallNumberType.Bulk && SelectedBulkFromFileObject) {
			requestData.append("bulk_file", SelectedBulkFromFileObject);
		}

		InitiateCall(
			requestData,
			(response) => {
				if (!response.success) {
					AlertManager.createAlert({
						type: "danger",
						message: "Failed to queue call. Check console logs for more details.",
						timeout: 5000
					});
					console.error("Failed to queue call:", response);
				}
				else {
					AlertManager.createAlert({
						type: "success",
						message: "Call queued successfully!",
						timeout: 5000
					});

					//resetMakeCallForm(); todo make configurable
				}		

				IsInitiatingCall = false;
				initiateCallButton.prop("disabled", false);
				initiateCallButtonSpinner.addClass("d-none");
				resetButton.prop("disabled", false);
			},
			(errorResponse) => {
				AlertManager.createAlert({
					type: "danger",
					message: "An error occurred while initiating calls. Check console logs for more details.",
					timeout: 4000
				});
				console.error("An error occurred while initiating calls:", errorResponse);

				IsInitiatingCall = false;
				initiateCallButton.prop("disabled", false);
				initiateCallButtonSpinner.addClass("d-none");
				resetButton.prop("disabled", false);
			}
		);
	});

	resetButton.on("click", (event) => {
		event.preventDefault();
		resetMakeCallForm();
	});

	initMakeCallHandlers();
}