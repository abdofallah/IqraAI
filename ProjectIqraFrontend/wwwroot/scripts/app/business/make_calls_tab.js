/** Global Variables & ENUMs **/
const OutboundCallNumberType = {
	Single: 0,
	Bulk: 1,
};

const OutboundCallScheduleType = {
	Now: 0,
	Later: 1,
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
const makeCallScheduleTypeNowRadio = makeCallsTab.find("#makeCallScheduleTypeNow");
const makeCallScheduleTypeLaterRadio = makeCallsTab.find("#makeCallScheduleTypeLater");
const makeCallScheduleDateTimeContainer = makeCallsTab.find("#makeCallScheduleDateTimeContainer");
const makeCallScheduleDateTimeInput = makeCallsTab.find("#makeCallScheduleDateTimeInput");

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

	makeCallScheduleTypeNowRadio.prop("checked", true);
	makeCallScheduleTypeLaterRadio.prop("checked", false);
	makeCallScheduleDateTimeInput.val("");
	makeCallScheduleDateTimeContainer.addClass("d-none").removeClass("show");

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
	if (makeCallScheduleTypeLaterRadio.is(":checked")) {
		const scheduleValue = makeCallScheduleDateTimeInput.val();
		if (!scheduleValue) addError("Schedule date and time must be set.", makeCallScheduleDateTimeInput);
		else {
			try {
				if (new Date(scheduleValue) <= new Date()) addError("Scheduled time must be in the future.", makeCallScheduleDateTimeInput);
			} catch (e) {
				addError("Invalid date/time format for schedule.", makeCallScheduleDateTimeInput);
			}
		}
	}

	return { validated: isValid, errors: errors };
}

function gatherMakeCallConfig() {
	const config = {
		campaignId: SelectedCampaignId,
		number: {
			type: CurrentMakeCallType,
			toNumber: CurrentMakeCallType === OutboundCallNumberType.Single ? makeCallToNumberInput.val().trim() : null,
		},
		schedule: {
			type: makeCallScheduleTypeLaterRadio.is(":checked") ? OutboundCallScheduleType.Later : OutboundCallScheduleType.Now
		},
		dynamicVariables: {
			// keep empty for now
		},
		metadata: {
			// keep empty for now
		}
	};

	if (config.schedule.type === OutboundCallScheduleType.Later && makeCallScheduleDateTimeInput.val()) {
		try {
			config.schedule.dateTimeUTC = new Date(makeCallScheduleDateTimeInput.val()).toISOString();
		} catch (e) {
			console.error("Error parsing schedule date");
		}
	}

	return config;
}

/** UNSAVED CHANGES LOGIC **/
function captureInitialFormState() {
	makeCallFormInitialState = {
		campaignId: SelectedCampaignId,
		callType: CurrentMakeCallType,
		toNumber: makeCallToNumberInput.val(),
		bulkFileSelected: !!SelectedBulkFromFileObject,
		scheduleType: makeCallScheduleTypeLaterRadio.is(":checked") ? "later" : "now",
		scheduleDateTime: makeCallScheduleDateTimeInput.val(),
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

	const currentScheduleType = makeCallScheduleTypeLaterRadio.is(":checked") ? "later" : "now";
	if (makeCallFormInitialState.scheduleType !== currentScheduleType) return true;
	if (currentScheduleType === "later" && makeCallFormInitialState.scheduleDateTime !== makeCallScheduleDateTimeInput.val()) return true;

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
	} else {
		// bulk
		CurrentMakeCallType = OutboundCallNumberType.Bulk;
		makeCallTypeSingleBox.removeClass("active");
		makeCallTypeBulkBox.addClass("active");
		makeCallNumberSingleContainer.addClass("d-none").removeClass("show");
		makeCallNumberBulkContainer.removeClass("d-none").addClass("show");
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
	makeCallScheduleTypeNowRadio.on("change", () => {
		makeCallScheduleDateTimeContainer.addClass("d-none").removeClass("show");
		validateMakeCallConfig(true);
	});
	makeCallScheduleTypeLaterRadio.on("change", () => {
		makeCallScheduleDateTimeContainer.removeClass("d-none").addClass("show");
		validateMakeCallConfig(true);
	});
	makeCallScheduleDateTimeInput.on("input", () => validateMakeCallConfig(true));
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
				timeout: 6000
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

					return;
				}
				else {
					AlertManager.createAlert({
						type: "success",
						message: "Call queued successfully!",
						timeout: 5000
					});

					resetMakeCallForm();
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
					timeout: 6000
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