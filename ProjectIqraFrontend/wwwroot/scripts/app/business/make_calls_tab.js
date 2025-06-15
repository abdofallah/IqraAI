/** Global Variables & ENUMs **/
// const AgentInterruptionTypeENUM is loaded from routing_tab.js

const OutboundCallNumberType = {
    Single: 0,
    Bulk: 1
}

const OutboundCallScheduleType = {
    Now: 0,
    Later: 1
}

const OutboundCallRetryDelayUnitType = {
    Seconds: 0,
	Minutes: 1,
	Hours: 2,
    Days: 3
}

/** Dynamic State Variables **/
let IsInitiatingCall = false;
let CurrentMakeCallType = OutboundCallNumberType.Single;
let SelectedFromNumberId = null;
let SelectedBulkFromFileObject = null;
let SelectedAgentId = null;
let SelectedAgentData = null;
let makeCallFormInitialState = {};

/** Element Variables **/
const makeCallsTab = $("#make-calls-tab");
// Header
const initiateCallButton = makeCallsTab.find("#makeCallInitiateButton");
const initiateCallButtonSpinner = initiateCallButton.find(".spinner-border");
const resetButton = makeCallsTab.find("#makeCallResetButton");
// General Tab
const makeCallIdentifierInput = makeCallsTab.find("#makeCallIdentifierInput");
const makeCallDescriptionInput = makeCallsTab.find("#makeCallDescriptionInput");
// Number Tab
const makeCallTypeSingleBox = makeCallsTab.find(".make-call-type-box-choose[box-type='single']");
const makeCallTypeBulkBox = makeCallsTab.find(".make-call-type-box-choose[box-type='bulk']");
const makeCallNumberSingleContainer = makeCallsTab.find("#make-call-number-single-container");
const makeCallNumberBulkContainer = makeCallsTab.find("#make-call-number-bulk-container");
const makeCallSelectedFromNumberInput = makeCallsTab.find("#makeCallSelectedFromNumber");
const makeCallChangeFromNumberButton = makeCallsTab.find("#makeCallChangeFromNumberButton");
const makeCallToNumberInput = makeCallsTab.find("#makeCallToNumberInput");
const makeCallSelectedFromNumberBulkInput = makeCallsTab.find("#makeCallSelectedFromNumberBulk");
const makeCallChangeFromNumberButtonBulk = makeCallsTab.find("#makeCallChangeFromNumberButtonBulk");
const makeCallToNumberBulkInput = makeCallsTab.find("#makeCallToNumberBulkInput");
// Number Selection Modal
const makeCallChangeFromNumberModalElement = makeCallsTab.find("#makeCallChangeFromNumberModal");
let makeCallChangeFromNumberModal = null;
const makeCallAssignNumberModalLists = makeCallChangeFromNumberModalElement.find(".make-call-assign-number-modal-list");
const inputMakeCallModalSearchNumberInput = makeCallChangeFromNumberModalElement.find("#inputMakeCallModalSearchNumberInput");
const searchMakeCallAssignNumberModalListButton = makeCallChangeFromNumberModalElement.find("#searchMakeCallAssignNumberModalList");
const saveMakeCallFromNumberButton = makeCallChangeFromNumberModalElement.find("#saveMakeCallFromNumberButton");
// Configuration Tab
const makeCallScheduleTypeNowRadio = makeCallsTab.find("#makeCallScheduleTypeNow");
const makeCallScheduleTypeLaterRadio = makeCallsTab.find("#makeCallScheduleTypeLater");
const makeCallScheduleDateTimeContainer = makeCallsTab.find("#makeCallScheduleDateTimeContainer");
const makeCallScheduleDateTimeInput = makeCallsTab.find("#makeCallScheduleDateTimeInput");
const makeCallRetryOnDeclineCheck = makeCallsTab.find("#makeCallRetryOnDeclineCheck");
const makeCallRetryOnDeclineOptionsContainer = makeCallsTab.find("#makeCallRetryOnDeclineOptionsContainer");
const makeCallRetryDeclineCountInput = makeCallsTab.find("#makeCallRetryDeclineCountInput");
const makeCallRetryDeclineDelayInput = makeCallsTab.find("#makeCallRetryDeclineDelayInput");
const makeCallRetryDeclineUnitSelect = makeCallsTab.find("#makeCallRetryDeclineUnitSelect");
const makeCallRetryOnMissCheck = makeCallsTab.find("#makeCallRetryOnMissCheck");
const makeCallRetryOnMissOptionsContainer = makeCallsTab.find("#makeCallRetryOnMissOptionsContainer");
const makeCallRetryMissCountInput = makeCallsTab.find("#makeCallRetryMissCountInput");
const makeCallRetryMissDelayInput = makeCallsTab.find("#makeCallRetryMissDelayInput");
const makeCallRetryMissUnitSelect = makeCallsTab.find("#makeCallRetryMissUnitSelect");
const makeCallNumberSilenceNotifyInput = makeCallsTab.find("#makeCallNumberSilenceNotify");
const makeCallNumberSilenceEndInput = makeCallsTab.find("#makeCallNumberSilenceEnd");
const makeCallNumberTotalCallTimeInput = makeCallsTab.find("#makeCallNumberTotalCallTime");
// Agent Tab
const makeCallSelectedAgentIcon = makeCallsTab.find("#makeCallSelectedAgentIcon");
const makeCallSelectedAgentNameInput = makeCallsTab.find("#makeCallSelectedAgentName");
const makeCallChangeAgentButton = makeCallsTab.find("#makeCallChangeAgentButton");
const makeCallAgentDefaultScriptSelect = makeCallsTab.find("#makeCallAgentDefaultScriptSelect");
const makeCallAgentLanguageSelect = makeCallsTab.find("#makeCallAgentLanguageSelect");
const makeCallAgentInterruptionTypeSelect = makeCallsTab.find("#makeCallAgentInterruptionTypeSelect");
const makeCallAgentTurnByTurnBox = makeCallsTab.find('.make-call-conversation-type-box[box-type="turnbyturn"]');
const makeCallAgentTurnByTurnUseInterruptedResponseInNextTurn = makeCallAgentTurnByTurnBox.find("#makeCallAgentTurnByTurnUseInterruptedResponseInNextTurn");
const makeCallAgentInterruptViaVadBox = makeCallsTab.find('.make-call-conversation-type-box[box-type="interruptibleviavad"]');
const makeCallAgentConversationTypeInterruptibleAudioActivityDuration = makeCallAgentInterruptViaVadBox.find("#makeCallAgentConversationTypeInterruptibleAudioActivityDuration");
const makeCallAgentInterruptViaAIBox = makeCallsTab.find('.make-call-conversation-type-box[box-type="interruptibleviaai"]');
const makeCallAgentInterruptViaAIUseAgentLLM = makeCallAgentInterruptViaAIBox.find("#makeCallAgentInterruptViaAIUseAgentLLM");
const agentMakeCallInterruptionViaLLMIntegrationSelectBox = makeCallAgentInterruptViaAIBox.find("#agentMakeCallInterruptionViaLLMIntegrationSelectBox");
const agentMakeCallInterruptionViaLLMIntegrationSelect = agentMakeCallInterruptionViaLLMIntegrationSelectBox.find('select[select-type="interrupt-integration-llm-makecall"]');
const makeCallAgentInterruptViaResponseBox = makeCallsTab.find('.make-call-conversation-type-box[box-type="interruptibleviaresponse"]');
const makeCallNumberTimezoneSelect = makeCallsTab.find("#makeCallNumberTimezoneSelect");
const makeCallAgentFromNumberInContextCheck = makeCallsTab.find("#makeCallAgentFromNumberInContextCheck");
const makeCallAgentToNumberInContextCheck = makeCallsTab.find("#makeCallAgentToNumberInContextCheck");
// Agent Selection Modal
const makeCallChangeAgentModalElement = makeCallsTab.find("#makeCallChangeAgentModal");
let makeCallChangeAgentModal = null;
const inputMakeCallSelectAgentSearch = makeCallChangeAgentModalElement.find("#inputMakeCallSelectAgentSearch");
const searchMakeCallSelectAgentListButton = makeCallChangeAgentModalElement.find("#searchMakeCallSelectAgentList");
const makeCallSelectAgentModalList = makeCallChangeAgentModalElement.find("#make-call-select-agent-modal-list");
const saveMakeCallAgentButton = makeCallChangeAgentModalElement.find("#saveMakeCallAgentButton");
// Actions Tab
const makeCallActionToolDeclinedSelect = makeCallsTab.find("#makeCallActionToolDeclined");
const makeCallActionToolDeclinedArgsContainer = makeCallsTab.find("#makeCallActionToolDeclinedContainer .custom-tool-input-arguments");
const makeCallActionToolDeclinedArgsSelect = makeCallsTab.find("#makeCallActionToolDeclinedInputArgumentsSelect");
const makeCallActionToolDeclinedArgsList = makeCallsTab.find("#makeCallActionToolDeclinedInputArgumentsList");
const makeCallActionToolMissSelect = makeCallsTab.find("#makeCallActionToolMiss");
const makeCallActionToolMissArgsContainer = makeCallsTab.find("#makeCallActionToolMissContainer .custom-tool-input-arguments");
const makeCallActionToolMissArgsSelect = makeCallsTab.find("#makeCallActionToolMissInputArgumentsSelect");
const makeCallActionToolMissArgsList = makeCallsTab.find("#makeCallActionToolMissInputArgumentsList");
const makeCallActionToolPickedUpSelect = makeCallsTab.find("#makeCallActionToolPickedUp");
const makeCallActionToolPickedUpArgsContainer = makeCallsTab.find("#makeCallActionToolPickedUpContainer .custom-tool-input-arguments");
const makeCallActionToolPickedUpArgsSelect = makeCallsTab.find("#makeCallActionToolPickedUpInputArgumentsSelect");
const makeCallActionToolPickedUpArgsList = makeCallsTab.find("#makeCallActionToolPickedUpInputArgumentsList");
const makeCallActionToolEndedSelect = makeCallsTab.find("#makeCallActionToolEnded");
const makeCallActionToolEndedArgsContainer = makeCallsTab.find("#makeCallActionToolEndedContainer .custom-tool-input-arguments");
const makeCallActionToolEndedArgsSelect = makeCallsTab.find("#makeCallActionToolEndedInputArgumentsSelect");
const makeCallActionToolEndedArgsList = makeCallsTab.find("#makeCallActionToolEndedInputArgumentsList");

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
			if (response.success) {
				if (successCallback) successCallback(response);
			} else {
				if (errorCallback) errorCallback(response, true);
			}
		},
		error: (xhr, status, error) => {
			console.error("Initiate Call AJAX Error:", status, error, xhr.responseText);
			if (errorCallback) errorCallback({ message: `Error: ${status || "Request Failed"}` }, false);
		},
	});
}

// Functions
function captureInitialFormState() {
	makeCallFormInitialState = gatherMakeCallConfig(true); // Use gather config, but maybe simplify it
	// We need a simpler comparison, maybe just capture raw values
	makeCallFormInitialState.identifier = makeCallIdentifierInput.val();
	makeCallFormInitialState.description = makeCallDescriptionInput.val();
	makeCallFormInitialState.callType = CurrentMakeCallType;
	makeCallFormInitialState.fromNumberId = SelectedFromNumberId;
	makeCallFormInitialState.toNumber = makeCallToNumberInput.val();
	makeCallFormInitialState.bulkFileSelected = !!SelectedBulkFromFileObject; // Just track if a file was selected initially (likely false)
	makeCallFormInitialState.scheduleType = makeCallScheduleTypeLaterRadio.is(":checked") ? 'later' : 'now';
	makeCallFormInitialState.scheduleDateTime = makeCallScheduleDateTimeInput.val();
	// Simplified Retry Capture
	makeCallFormInitialState.retryDeclineEnabled = makeCallRetryOnDeclineCheck.is(":checked");
	makeCallFormInitialState.retryDeclineCount = makeCallRetryDeclineCountInput.val();
	makeCallFormInitialState.retryDeclineDelay = makeCallRetryDeclineDelayInput.val();
	makeCallFormInitialState.retryDeclineUnit = makeCallRetryDeclineUnitSelect.val();
	makeCallFormInitialState.retryMissEnabled = makeCallRetryOnMissCheck.is(":checked");
	makeCallFormInitialState.retryMissCount = makeCallRetryMissCountInput.val();
	makeCallFormInitialState.retryMissDelay = makeCallRetryMissDelayInput.val();
	makeCallFormInitialState.retryMissUnit = makeCallRetryMissUnitSelect.val();
	// Timeouts
	makeCallFormInitialState.silenceNotify = makeCallNumberSilenceNotifyInput.val();
	makeCallFormInitialState.silenceEnd = makeCallNumberSilenceEndInput.val();
	makeCallFormInitialState.maxTime = makeCallNumberTotalCallTimeInput.val();
	// Agent
	makeCallFormInitialState.agentId = SelectedAgentId;
	makeCallFormInitialState.scriptId = makeCallAgentDefaultScriptSelect.val();
	makeCallFormInitialState.languageCode = makeCallAgentLanguageSelect.val(); // Added Language
	makeCallFormInitialState.interruptionType = makeCallAgentInterruptionTypeSelect.val();
	// Capture specific interruption sub-settings based on type... (can get complex, maybe simplify change check)
	// Timezone & Context
	var timezoneValue = makeCallNumberTimezoneSelect.val();
	var timezones = [];
	if (timezoneValue && timezoneValue !== null) {
		timezones = [timezoneValue];
	}
	makeCallFormInitialState.timezones = timezones;
	makeCallFormInitialState.includeFromNumberInContext = makeCallAgentFromNumberInContextCheck.is(":checked");
	makeCallFormInitialState.includeToNumberInContext = makeCallAgentToNumberInContextCheck.is(":checked");
	// Actions (Just track if *any* tool is selected for simplicity)
	makeCallFormInitialState.actionDeclinedTool = makeCallActionToolDeclinedSelect.val();
	makeCallFormInitialState.actionMissTool = makeCallActionToolMissSelect.val();
	makeCallFormInitialState.actionAnsweredTool = makeCallActionToolPickedUpSelect.val();
	makeCallFormInitialState.actionEndedTool = makeCallActionToolEndedSelect.val();
	// Note: Checking arguments deeply might be too complex for a simple "has changes" check.
}

function checkMakeCallTabHasChanges() {
	// Compare current form values to makeCallFormInitialState
	if (makeCallFormInitialState.identifier !== makeCallIdentifierInput.val()) return true;
	if (makeCallFormInitialState.description !== makeCallDescriptionInput.val()) return true;
	if (makeCallFormInitialState.callType !== CurrentMakeCallType) return true;
	if (makeCallFormInitialState.fromNumberId !== SelectedFromNumberId) return true;
	if (makeCallFormInitialState.toNumber !== makeCallToNumberInput.val() && CurrentMakeCallType === OutboundCallNumberType.Single) return true;
	if (makeCallFormInitialState.bulkFileSelected !== !!SelectedBulkFromFileObject && CurrentMakeCallType === OutboundCallNumberType.Bulk) return true;
	const currentScheduleType = makeCallScheduleTypeLaterRadio.is(":checked") ? 'later' : 'now';
	if (makeCallFormInitialState.scheduleType !== currentScheduleType) return true;
	if (makeCallFormInitialState.scheduleDateTime !== makeCallScheduleDateTimeInput.val() && currentScheduleType === 'later') return true;
	if (makeCallFormInitialState.retryDeclineEnabled !== makeCallRetryOnDeclineCheck.is(":checked")) return true;
	if (makeCallRetryOnDeclineCheck.is(":checked")) { // Only check sub-values if enabled
		if (makeCallFormInitialState.retryDeclineCount !== makeCallRetryDeclineCountInput.val()) return true;
		if (makeCallFormInitialState.retryDeclineDelay !== makeCallRetryDeclineDelayInput.val()) return true;
		if (makeCallFormInitialState.retryDeclineUnit !== makeCallRetryDeclineUnitSelect.val()) return true;
	}
	if (makeCallFormInitialState.retryMissEnabled !== makeCallRetryOnMissCheck.is(":checked")) return true;
	if (makeCallRetryOnMissCheck.is(":checked")) { // Only check sub-values if enabled
		if (makeCallFormInitialState.retryMissCount !== makeCallRetryMissCountInput.val()) return true;
		if (makeCallFormInitialState.retryMissDelay !== makeCallRetryMissDelayInput.val()) return true;
		if (makeCallFormInitialState.retryMissUnit !== makeCallRetryMissUnitSelect.val()) return true;
	}
	if (makeCallFormInitialState.silenceNotify !== makeCallNumberSilenceNotifyInput.val()) return true;
	if (makeCallFormInitialState.silenceEnd !== makeCallNumberSilenceEndInput.val()) return true;
	if (makeCallFormInitialState.maxTime !== makeCallNumberTotalCallTimeInput.val()) return true;
	if (makeCallFormInitialState.agentId !== SelectedAgentId) return true;
	if (makeCallFormInitialState.scriptId !== makeCallAgentDefaultScriptSelect.val()) return true;
	if (makeCallFormInitialState.languageCode !== makeCallAgentLanguageSelect.val()) return true; // Added Language Check
	if (makeCallFormInitialState.interruptionType !== makeCallAgentInterruptionTypeSelect.val()) return true;
	// Add checks for interruption sub-settings if needed for more precise change detection
	var timezoneValue = makeCallNumberTimezoneSelect.val();
	var timezones = [];
	if (timezoneValue && timezoneValue !== null) {
		timezones = [timezoneValue];
	}
	const timezonesEqual = timezones.length === makeCallFormInitialState.timezones.length && timezones.every((value, index) => value === makeCallFormInitialState.timezones[index]);
	if (!timezonesEqual) return true;
	if (makeCallFormInitialState.includeFromNumberInContext !== makeCallAgentFromNumberInContextCheck.is(":checked")) return true;
	if (makeCallFormInitialState.includeToNumberInContext !== makeCallAgentToNumberInContextCheck.is(":checked")) return true;
	if (makeCallFormInitialState.actionDeclinedTool !== makeCallActionToolDeclinedSelect.val()) return true;
	if (makeCallFormInitialState.actionMissTool !== makeCallActionToolMissSelect.val()) return true;
	if (makeCallFormInitialState.actionAnsweredTool !== makeCallActionToolPickedUpSelect.val()) return true;
	if (makeCallFormInitialState.actionEndedTool !== makeCallActionToolEndedSelect.val()) return true;
	// Note: Changes within tool arguments are NOT detected by this simplified check

	return false; // No changes detected
}

async function canLeaveMakeCallTab(leaveMessage = "") {
	if (IsInitiatingCall) {
		AlertManager.createAlert({ type: "warning", message: "Cannot leave while initiating a call.", timeout: 6000 });
		return false;
	}

	if (checkMakeCallTabHasChanges()) {
		const confirmDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes",
			message: `You have unsaved changes in the Make Call form.${leaveMessage}`,
			confirmText: "Discard Changes",
			cancelText: "Stay",
			confirmButtonClass: "btn-danger",
			cancelButtonClass: "btn-secondary",
			modalSize: "modal-lg",
		});
		const confirmResult = await confirmDialog.show();
		if (!confirmResult) {
			return false;
		}
	}
	return true;
}

async function confirmInitiateCall() {
	// Basic summary, can be enhanced
	let summary = `Initiate call?\n`;
	summary += `Type: ${CurrentMakeCallType}\n`;
	if (CurrentMakeCallType === OutboundCallNumberType.Single) {
		summary += `From: ${makeCallSelectedFromNumberInput.val() || 'Not Selected'}\n`;
		summary += `To: ${makeCallToNumberInput.val() || 'Not Entered'}\n`;
	} else {
		summary += `From (Default): ${makeCallSelectedFromNumberBulkInput.val() || SelectedFromNumberId || 'Not Selected'}\n`;
		summary += `File: ${SelectedBulkFromFileObject?.name || 'Not Selected'}\n`;
	}
	summary += `Agent: ${makeCallSelectedAgentNameInput.val() || 'Not Selected'}\n`;
	if (makeCallScheduleTypeLaterRadio.is(':checked') && makeCallScheduleDateTimeInput.val()) {
		try {
			summary += `Scheduled for: ${new Date(makeCallScheduleDateTimeInput.val()).toLocaleString()}\n`;
		} catch (e) {
			summary += `Scheduled for: Invalid Date\n`;
		}
	} else {
		summary += `Scheduled: Now\n`;
	}

	// Ensure BootstrapConfirmDialog class is available globally
	if (typeof BootstrapConfirmDialog === 'undefined') {
		console.error("BootstrapConfirmDialog is not defined. Skipping confirmation.");
		return true; // Skip confirmation if dialog is missing
	}

	const confirmDialog = new BootstrapConfirmDialog({
		title: "Confirm Call Initiation",
		message: `<pre>${summary}</pre>`, // Use pre for basic formatting
		confirmText: "Initiate",
		cancelText: "Cancel",
		confirmButtonClass: "btn-success",
		cancelButtonClass: "btn-secondary",
		modalSize: "modal-lg",
	});

	return await confirmDialog.show(); // Returns true if Initiate clicked, false if Cancelled
}

function resetMakeCallForm() {
	// ... (Reset State Variables) ...
	CurrentMakeCallType = OutboundCallNumberType.Single;
	SelectedFromNumberId = null;
	SelectedBulkFromFileObject = null;
	SelectedAgentId = null;
	SelectedAgentData = null;

	// ... (Reset General Tab) ...
	makeCallIdentifierInput.val("");
	makeCallDescriptionInput.val("");

	// ... (Reset Number Tab) ...
	makeCallTypeSingleBox.addClass("active");
	makeCallTypeBulkBox.removeClass("active");
	makeCallSelectedFromNumberInput.val("").attr("placeholder", "Select a number...");
	makeCallToNumberInput.val("");
	makeCallSelectedFromNumberBulkInput.val("").attr("placeholder", "Select a default 'from' number...");
	makeCallToNumberBulkInput.val(null);

	// ... (Reset Configuration Tab - Simplified Retry) ...
	makeCallScheduleTypeNowRadio.prop("checked", true);
	makeCallScheduleTypeLaterRadio.prop("checked", false);
	makeCallScheduleDateTimeInput.val("");
	makeCallScheduleDateTimeContainer.addClass("d-none").removeClass("show"); // Hide initially

	makeCallRetryOnDeclineCheck.prop("checked", false);
	makeCallRetryDeclineCountInput.val("3");
	makeCallRetryDeclineDelayInput.val("10");
	makeCallRetryDeclineUnitSelect.val("minutes");
	handleRetryOptionsVisibility("Decline"); // Call handler AFTER setting defaults

	makeCallRetryOnMissCheck.prop("checked", false);
	makeCallRetryMissCountInput.val("3");
	makeCallRetryMissDelayInput.val("10");
	makeCallRetryMissUnitSelect.val("minutes");
	handleRetryOptionsVisibility("Miss"); // Call handler AFTER setting defaults

	makeCallNumberSilenceNotifyInput.val("10000");
	makeCallNumberSilenceEndInput.val("30000");
	makeCallNumberTotalCallTimeInput.val("600");


	// ... (Reset Agent Tab - Added Language) ...
	makeCallSelectedAgentIcon.text("-");
	makeCallSelectedAgentNameInput.val("").attr("placeholder", "Select Agent...");
	makeCallAgentDefaultScriptSelect.empty().append('<option value="" disabled selected>Select Agent First</option>').prop("disabled", true);
	makeCallAgentLanguageSelect.val(""); // Reset language select

	makeCallAgentInterruptionTypeSelect.val(AgentInterruptionTypeENUM.TurnByTurn);
	makeCallAgentTurnByTurnUseInterruptedResponseInNextTurn.prop("checked", false);
	makeCallAgentConversationTypeInterruptibleAudioActivityDuration.val("300");
	makeCallAgentInterruptViaAIUseAgentLLM.prop("checked", true);
	agentMakeCallInterruptionViaLLMIntegrationSelect.val("");
	handleInterruptionTypeChange(); // Call after setting defaults

	makeCallNumberTimezoneSelect.val("");
	makeCallAgentFromNumberInContextCheck.prop("checked", true);
	makeCallAgentToNumberInContextCheck.prop("checked", true);

	// ... (Reset Actions Tab) ...
	[
		{ sel: makeCallActionToolDeclinedSelect, argsCont: makeCallActionToolDeclinedArgsContainer, argsSel: makeCallActionToolDeclinedArgsSelect, argsList: makeCallActionToolDeclinedArgsList },
		{ sel: makeCallActionToolMissSelect, argsCont: makeCallActionToolMissArgsContainer, argsSel: makeCallActionToolMissArgsSelect, argsList: makeCallActionToolMissArgsList },
		{ sel: makeCallActionToolPickedUpSelect, argsCont: makeCallActionToolPickedUpArgsContainer, argsSel: makeCallActionToolPickedUpArgsSelect, argsList: makeCallActionToolPickedUpArgsList },
		{ sel: makeCallActionToolEndedSelect, argsCont: makeCallActionToolEndedArgsContainer, argsSel: makeCallActionToolEndedArgsSelect, argsList: makeCallActionToolEndedArgsList },
	].forEach((action) => { /* ... same reset logic ... */ });


	// ... (Reset validation, buttons) ...
	makeCallsTab.find(".is-invalid").removeClass("is-invalid");
	initiateCallButton.prop("disabled", false);
	initiateCallButtonSpinner.addClass("d-none");
	resetButton.prop("disabled", false); // Ensure reset is enabled after programmatic reset
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

	// General Tab Validation
	removeError(makeCallIdentifierInput);
	removeError(makeCallDescriptionInput);
    if (!makeCallIdentifierInput.val().trim()) addError("Call identifier is required.", makeCallIdentifierInput);
    if (!makeCallDescriptionInput.val().trim()) addError("Call description is required.", makeCallDescriptionInput);

	// --- Number Tab Validation ---
	if (CurrentMakeCallType === OutboundCallNumberType.Single) {
		removeError(makeCallSelectedFromNumberInput);
		removeError(makeCallToNumberInput);
		if (!SelectedFromNumberId) addError("A 'Call From' number must be selected.", makeCallSelectedFromNumberInput);
		const toNumber = makeCallToNumberInput.val().trim();
		if (!toNumber) addError("'Call To' number cannot be empty.", makeCallToNumberInput);
		else if (!/^\+?[1-9]\d{1,14}$/.test(toNumber.replace(/[\s()-]/g, ""))) addError("'Call To' number format is invalid (e.g., +12223334444).", makeCallToNumberInput);
		removeError(makeCallSelectedFromNumberBulkInput);
		removeError(makeCallToNumberBulkInput);
	} else {
		// Bulk
		removeError(makeCallSelectedFromNumberBulkInput);
		removeError(makeCallToNumberBulkInput);
		if (!SelectedFromNumberId) addError("A default 'Call From' number must be selected for bulk calls.", makeCallSelectedFromNumberBulkInput);
		if (!SelectedBulkFromFileObject) addError("A CSV file must be selected for bulk calls.", makeCallToNumberBulkInput);
		else if (!SelectedBulkFromFileObject.name.toLowerCase().endsWith(".csv")) addError("Invalid file type. Please select a .csv file.", makeCallToNumberBulkInput);
		removeError(makeCallSelectedFromNumberInput);
		removeError(makeCallToNumberInput);
	}

	// --- Configuration Tab Validation ---
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

	// Retry Decline
	removeError(makeCallRetryDeclineCountInput);
	removeError(makeCallRetryDeclineDelayInput);
	if (makeCallRetryOnDeclineCheck.is(":checked")) {
		const count = parseInt(makeCallRetryDeclineCountInput.val());
		const delay = parseInt(makeCallRetryDeclineDelayInput.val());
		if (isNaN(count) || count < 1) addError("Decline Retry Count must be > 0.", makeCallRetryDeclineCountInput);
		if (isNaN(delay) || delay < 1) addError("Decline Retry Delay must be > 0.", makeCallRetryDeclineDelayInput);
	}

	// Retry Miss
	removeError(makeCallRetryMissCountInput);
	removeError(makeCallRetryMissDelayInput);
	if (makeCallRetryOnMissCheck.is(":checked")) {
		const count = parseInt(makeCallRetryMissCountInput.val());
		const delay = parseInt(makeCallRetryMissDelayInput.val());
		if (isNaN(count) || count < 1) addError("No Answer Retry Count must be > 0.", makeCallRetryMissCountInput);
		if (isNaN(delay) || delay < 1) addError("No Answer Retry Delay must be > 0.", makeCallRetryMissDelayInput);
	}

	// Timeouts
	removeError(makeCallNumberSilenceNotifyInput);
	removeError(makeCallNumberSilenceEndInput);
	removeError(makeCallNumberTotalCallTimeInput);
	const silenceNotify = parseInt(makeCallNumberSilenceNotifyInput.val());
	const silenceEnd = parseInt(makeCallNumberSilenceEndInput.val());
	const maxTime = parseInt(makeCallNumberTotalCallTimeInput.val());
	if (isNaN(silenceNotify) || silenceNotify < 0) addError("Notify on Silence must be >= 0.", makeCallNumberSilenceNotifyInput);
	if (isNaN(silenceEnd) || silenceEnd < 0) addError("End on Silence must be >= 0.", makeCallNumberSilenceEndInput);
	if (isNaN(maxTime) || maxTime < 1) addError("Max Call Time must be > 0.", makeCallNumberTotalCallTimeInput);
	if (!isNaN(silenceNotify) && !isNaN(silenceEnd) && silenceEnd < silenceNotify) addError("End on Silence cannot be less than Notify time.", makeCallNumberSilenceEndInput);

	// --- Agent Tab Validation ---
	removeError(makeCallSelectedAgentNameInput);
	removeError(makeCallAgentDefaultScriptSelect);
	removeError(makeCallAgentLanguageSelect);
	removeError(makeCallNumberTimezoneSelect);
	removeError(makeCallAgentConversationTypeInterruptibleAudioActivityDuration);
	removeError(agentMakeCallInterruptionViaLLMIntegrationSelect);
	if (!SelectedAgentId) addError("An Agent must be selected.", makeCallSelectedAgentNameInput);
	if (SelectedAgentId && !makeCallAgentDefaultScriptSelect.val()) addError("An Opening Script must be selected.", makeCallAgentDefaultScriptSelect);
	if (!makeCallAgentLanguageSelect.val()) addError("A Language must be selected.", makeCallAgentLanguageSelect);
	if (!makeCallNumberTimezoneSelect.val()) addError("A Timezone must be selected.", makeCallNumberTimezoneSelect);
	const interruptionType = parseInt(makeCallAgentInterruptionTypeSelect.val());
	if (interruptionType === AgentInterruptionTypeENUM.InterruptibleViaVAD) {
		const duration = parseInt(makeCallAgentConversationTypeInterruptibleAudioActivityDuration.val());
		if (isNaN(duration) || duration < 50) addError("VAD duration must be >= 50ms.", makeCallAgentConversationTypeInterruptibleAudioActivityDuration);
	} else if (interruptionType === AgentInterruptionTypeENUM.InterruptibleViaAI) {
		if (!makeCallAgentInterruptViaAIUseAgentLLM.is(":checked") && !agentMakeCallInterruptionViaLLMIntegrationSelect.val())
			addError("An LLM Integration must be selected for AI interruption.", agentMakeCallInterruptionViaLLMIntegrationSelect);
	}

	// --- Actions Tab Validation ---
	const actionsToValidate = [
		{ toolSel: makeCallActionToolDeclinedSelect, argsList: makeCallActionToolDeclinedArgsList, name: "Declined/Busy Action" },
		{ toolSel: makeCallActionToolMissSelect, argsList: makeCallActionToolMissArgsList, name: "No Answer Action" },
		{ toolSel: makeCallActionToolPickedUpSelect, argsList: makeCallActionToolPickedUpArgsList, name: "Call Answered Action" },
		{ toolSel: makeCallActionToolEndedSelect, argsList: makeCallActionToolEndedArgsList, name: "Call Ended Action" },
	];
	actionsToValidate.forEach((action) => {
		const toolId = action.toolSel.val();
		if (toolId && toolId !== "none") {
			const toolData = BusinessFullData.businessApp.tools.find((t) => t.id === toolId);
			toolData?.configuration?.inputSchemea?.forEach((inputSchema) => {
				if (inputSchema.isRequired) {
					const argInput = action.argsList.find(`input[input_arguement="${inputSchema.id}"]`);
					removeError(argInput);
					if (!argInput.length || !argInput.val().trim()) {
						const argName = inputSchema.name[BusinessDefaultLanguage] || inputSchema.id;
						addError(`${action.name}: Required argument '${argName}' missing.`, argInput.length ? argInput : action.toolSel);
					}
				}
			});
		} else {
			action.argsList.find("input").each((i, el) => removeError($(el)));
		}
	});

	return { validated: isValid, errors: errors };
}

function gatherMakeCallConfig() {
	const config = {
		general: {
			identifier: makeCallIdentifierInput.val().trim() || null,
			description: makeCallDescriptionInput.val().trim() || null,
		},
		numberDetails: {
			type: CurrentMakeCallType,
			fromNumberId: SelectedFromNumberId,
			toNumber: CurrentMakeCallType === OutboundCallNumberType.Single ? makeCallToNumberInput.val().trim() : null,
		},
		configuration: {
			schedule: { type: makeCallScheduleTypeLaterRadio.is(":checked") ? OutboundCallScheduleType.Later : OutboundCallScheduleType.Now, dateTimeUTC: null },
			retryDecline: {
				enabled: makeCallRetryOnDeclineCheck.is(":checked"),
				count: null,
				delay: null,
				unit: null,
			},
			retryMiss: {
				enabled: makeCallRetryOnMissCheck.is(":checked"),
				count: null,
				delay: null,
				unit: null,
			},
			timeouts: {
				notifyOnSilenceMS: parseInt(makeCallNumberSilenceNotifyInput.val()) || 10000,
				endOnSilenceMS: parseInt(makeCallNumberSilenceEndInput.val()) || 30000,
				maxCallTimeS: parseInt(makeCallNumberTotalCallTimeInput.val()) || 600,
			},
		},
		agentSettings: {
			agentId: SelectedAgentId,
			scriptId: makeCallAgentDefaultScriptSelect.val() || null,
			languageCode: makeCallAgentLanguageSelect.val() || null,
			interruption: { type: parseInt(makeCallAgentInterruptionTypeSelect.val()), useInterruptedResponseInNextTurn: null, vadDurationMS: null, useAgentLLM: null, llmIntegrationId: null },
			timezones: [makeCallNumberTimezoneSelect.val()] || null,
			includeFromNumberInContext: makeCallAgentFromNumberInContextCheck.is(":checked"),
			includeToNumberInContext: makeCallAgentToNumberInContextCheck.is(":checked")
		},
		actions: { declined: null, missed: null, answered: null, ended: null },
	};

	if (config.configuration.schedule.type === OutboundCallScheduleType.Later && makeCallScheduleDateTimeInput.val()) {
		try {
			config.configuration.schedule.dateTimeUTC = new Date(makeCallScheduleDateTimeInput.val()).toISOString();
		} catch (e) {
			console.error("Error parsing schedule date");
		}
	}
	if (config.configuration.retryDecline.enabled) {
		config.configuration.retryDecline.count = parseInt(makeCallRetryDeclineCountInput.val()) || 3;
		config.configuration.retryDecline.delay = parseInt(makeCallRetryDeclineDelayInput.val()) || 10;
		config.configuration.retryDecline.unit = makeCallRetryDeclineUnitSelect.val();
	}
	if (config.configuration.retryMiss.enabled) {
		config.configuration.retryMiss.count = parseInt(makeCallRetryMissCountInput.val()) || 3;
		config.configuration.retryMiss.delay = parseInt(makeCallRetryMissDelayInput.val()) || 10;
		config.configuration.retryMiss.unit = makeCallRetryMissUnitSelect.val();
	}
	const intType = config.agentSettings.interruption.type;
	if (intType === AgentInterruptionTypeENUM.TurnByTurn) {
		config.agentSettings.interruption.useInterruptedResponseInNextTurn = makeCallAgentTurnByTurnUseInterruptedResponseInNextTurn.is(":checked");
	} else if (intType === AgentInterruptionTypeENUM.InterruptibleViaVAD) {
		config.agentSettings.interruption.vadDurationMS = parseInt(makeCallAgentConversationTypeInterruptibleAudioActivityDuration.val()) || 300;
	} else if (intType === AgentInterruptionTypeENUM.InterruptibleViaAI) {
		config.agentSettings.interruption.useAgentLLM = makeCallAgentInterruptViaAIUseAgentLLM.is(":checked");
		if (!config.agentSettings.interruption.useAgentLLM) config.agentSettings.interruption.llmIntegrationId = agentMakeCallInterruptionViaLLMIntegrationSelect.val() || null;
	}
	const actionMappings = [
		{ key: "declined", toolSel: makeCallActionToolDeclinedSelect, argsList: makeCallActionToolDeclinedArgsList },
		{ key: "missed", toolSel: makeCallActionToolMissSelect, argsList: makeCallActionToolMissArgsList },
		{ key: "answered", toolSel: makeCallActionToolPickedUpSelect, argsList: makeCallActionToolPickedUpArgsList },
		{ key: "ended", toolSel: makeCallActionToolEndedSelect, argsList: makeCallActionToolEndedArgsList },
	];
	actionMappings.forEach((mapping) => {
		const toolId = mapping.toolSel.val();
		if (toolId && toolId !== "none") {
			const actionData = { toolId: toolId, arguments: {} };
			mapping.argsList.find("input[input_arguement]").each((i, input) => {
				const $input = $(input);
				const argId = $input.attr("input_arguement");
				actionData.arguments[argId] = $input.val();
			});
			config.actions[mapping.key] = actionData;
		} else {
			config.actions[mapping.key] = {
				toolId: null
			};
		}
	});
	return config;
}

// Number Tab Functions
function createMakeCallNumberModalListElement(numberData) {
	const isUsedByRoute = numberData.routeId !== null && numberData.routeId !== undefined;
	const countryData = CountriesList[numberData.countryCode.toUpperCase()];
	const formattedNumber = `(${countryData?.phone_code || numberData.countryCode}) ${numberData.number}`;
	return `<button type="button" class="list-group-item list-group-item-action" number-id="${numberData.id}" number-provider="${numberData.provider.value}" number-formatted="${formattedNumber}">${formattedNumber} ${isUsedByRoute ? "(Used by inbound route)" : ""}</button>`;
}

function fillMakeCallNumberModalNumbersList() {
	const searchTerm = inputMakeCallModalSearchNumberInput.val().toLowerCase().trim();
	let numbersFoundOverall = false;
	makeCallAssignNumberModalLists.empty().append('<span class="list-group-item">Loading numbers...</span>');
	const allNumbers = BusinessFullData?.businessApp?.numbers || [];
	const numbersByProvider = {};

	allNumbers.forEach((number) => {
		// TODO: Add check here if number is capable of OUTBOUND calls based on provider/number capabilities
		const providerValue = number.provider.value;
		const countryData = CountriesList[number.countryCode.toUpperCase()];
		const formattedNumber = `(${countryData?.phone_code || numberData.countryCode}) ${number.number}`;
		if (searchTerm && !formattedNumber.toLowerCase().includes(searchTerm) && !number.number.includes(searchTerm)) return;
		if (!numbersByProvider[providerValue]) numbersByProvider[providerValue] = [];
		numbersByProvider[providerValue].push(number);
		numbersFoundOverall = true;
	});

	makeCallAssignNumberModalLists.each((index, listElement) => {
		const $listElement = $(listElement);
		const providerValue = parseInt($listElement.attr("number-provider"));
		const providerNumbers = numbersByProvider[providerValue] || [];
		$listElement.empty();
		if (providerNumbers.length > 0) providerNumbers.forEach((number) => $listElement.append(createMakeCallNumberModalListElement(number)));
		else if (!searchTerm || !numbersFoundOverall) $listElement.append(`<span class="list-group-item">No suitable numbers found for this provider.</span>`);
		else $listElement.append(`<span class="list-group-item">No numbers match '${searchTerm}' for this provider.</span>`);
	});
}

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

// Configuration Tab Functions
function handleRetryOptionsVisibility(type) {
	const mainCheckbox = type === "Decline" ? makeCallRetryOnDeclineCheck : makeCallRetryOnMissCheck;
	const optionsContainer = type === "Decline" ? makeCallRetryOnDeclineOptionsContainer : makeCallRetryOnMissOptionsContainer;

	if (mainCheckbox.is(":checked")) {
		optionsContainer.removeClass("d-none").addClass("show");
	} else {
		optionsContainer.addClass("d-none").removeClass("show");
	}
	validateMakeCallConfig(true);
}

// Agent Tab Functions
function createMakeCallAgentModalListElement(agentData) {
	const agentName = agentData.general.name[BusinessDefaultLanguage] || agentData.general.name[Object.keys(agentData.general.name)[0]] || "Unnamed Agent";
	const agentEmoji = agentData.general.emoji || "🤖";
	return `<button type="button" class="list-group-item list-group-item-action" agent-id="${agentData.id}"><span>${agentEmoji} ${agentName}</span></button>`;
}

function fillMakeCallAgentModalList() {
	const searchTerm = inputMakeCallSelectAgentSearch.val().toLowerCase().trim();
	makeCallSelectAgentModalList.empty();
	const agents = BusinessFullData?.businessApp?.agents || [];
	let agentsFound = false;
	if (agents.length === 0) {
		makeCallSelectAgentModalList.append('<span class="list-group-item">No agents created yet.</span>');
		return;
	}
	agents.forEach((agent) => {
		const agentName = agent.general.name[BusinessDefaultLanguage] || agent.general.name[Object.keys(agent.general.name)[0]] || "";
		if (!searchTerm || agentName.toLowerCase().includes(searchTerm)) {
			makeCallSelectAgentModalList.append(createMakeCallAgentModalListElement(agent));
			agentsFound = true;
		}
	});
	if (!agentsFound) makeCallSelectAgentModalList.append(`<span class="list-group-item">No agents found matching '${searchTerm}'.</span>`);
}

function populateAgentScripts(agentData) {
	makeCallAgentDefaultScriptSelect.empty().prop("disabled", true);
	if (!agentData || !agentData.scripts || agentData.scripts.length === 0) {
		makeCallAgentDefaultScriptSelect.append('<option value="" disabled selected>No scripts available</option>');
		return;
	}
	makeCallAgentDefaultScriptSelect.append('<option value="" disabled selected>Select Opening Script</option>');
	agentData.scripts.forEach((script) => {
		const scriptName = script.general.name[BusinessDefaultLanguage] || script.general.name[Object.keys(script.general.name)[0]] || "Unnamed Script";
		makeCallAgentDefaultScriptSelect.append(`<option value="${script.id}">${scriptName}</option>`);
	});
	makeCallAgentDefaultScriptSelect.prop("disabled", false);
}

function fillMakeCallAgentInterruptViaAIIntegrationSelect() {
	agentMakeCallInterruptionViaLLMIntegrationSelect.empty().append(`<option value="" disabled selected>Select LLM Integration</option>`);
	let llmIntegrationsFound = false;
	const integrations = BusinessFullData?.businessApp?.integrations || [];
	const llmIntegrationTypes = SpecificationIntegrationsListData?.filter((i) => i.type.includes("LLM")).map((i) => i.id) || [];
	integrations.forEach((integrationData) => {
		if (llmIntegrationTypes.includes(integrationData.type)) {
			agentMakeCallInterruptionViaLLMIntegrationSelect.append(`<option value="${integrationData.id}">${integrationData.friendlyName || "Unnamed Integration"}</option>`);
			llmIntegrationsFound = true;
		}
	});
	if (!llmIntegrationsFound) {
		agentMakeCallInterruptionViaLLMIntegrationSelect.append(`<option value="" disabled>No LLM Integrations found</option>`);
		agentMakeCallInterruptionViaLLMIntegrationSelect.prop("disabled", true);
	} else {
		agentMakeCallInterruptionViaLLMIntegrationSelect.prop("disabled", false);
	}
}

function handleInterruptionTypeChange() {
	let selectedValue = makeCallAgentInterruptionTypeSelect.val();
	if (!selectedValue) return;
	selectedValue = parseInt(selectedValue);
	makeCallAgentTurnByTurnBox.addClass("d-none");
	makeCallAgentInterruptViaVadBox.addClass("d-none");
	makeCallAgentInterruptViaAIBox.addClass("d-none");
	makeCallAgentInterruptViaResponseBox.addClass("d-none");
	switch (selectedValue) {
		case AgentInterruptionTypeENUM.TurnByTurn:
			makeCallAgentTurnByTurnBox.removeClass("d-none");
			break;
		case AgentInterruptionTypeENUM.InterruptibleViaVAD:
			makeCallAgentInterruptViaVadBox.removeClass("d-none");
			break;
		case AgentInterruptionTypeENUM.InterruptibleViaAI:
			makeCallAgentInterruptViaAIBox.removeClass("d-none");
			makeCallAgentInterruptViaAIUseAgentLLM.trigger("change");
			break;
		case AgentInterruptionTypeENUM.InterruptibleViaResponse:
			makeCallAgentInterruptViaResponseBox.removeClass("d-none");
			break;
	}
	validateMakeCallConfig(true);
}

function populateLanguageSelect() {
	makeCallAgentLanguageSelect.empty();
	makeCallAgentLanguageSelect.append('<option value="" disabled selected>Select Language</option>');


	const languages = SpecificationLanguagesListData || [];
	const businessLanguages = BusinessFullData?.businessData?.languages || [];

	if (businessLanguages.length === 0) {
		makeCallAgentLanguageSelect.append('<option value="" disabled>No languages configured</option>');
		makeCallAgentLanguageSelect.prop('disabled', true);
	} else {
		businessLanguages.forEach(language => {
			const lang = SpecificationLanguagesListData.find((l) => l.id === language);
			makeCallAgentLanguageSelect.append(`<option value="${lang.id}">${lang.name} (${lang.id})</option>`);
		});
		makeCallAgentLanguageSelect.prop('disabled', false);
	}
}


// Action Tab Functions
function populateToolSelect($selectElement) {
	$selectElement.empty().append('<option value="none" selected>None</option>');
	const tools = BusinessFullData?.businessApp?.tools || [];
	if (tools.length > 0)
		tools.forEach((tool) => {
			const toolName = tool.general.name[BusinessDefaultLanguage] || tool.general.name[Object.keys(tool.general.name)[0]] || "Unnamed Tool";
			$selectElement.append(`<option value="${tool.id}">${toolName}</option>`);
		});
}

function handleToolSelectionChange($toolSelect, $argsContainer, $argsSelect, $argsList) {
	const selectedToolId = $toolSelect.val();
	$argsList.empty();
	$argsSelect.empty().append('<option value="" disabled selected>Add Input Argument</option>');
	if (selectedToolId === "none" || !selectedToolId) $argsContainer.addClass("d-none");
	else {
		$argsContainer.removeClass("d-none");
		const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === selectedToolId);
		if (toolData?.configuration?.inputSchemea && toolData.configuration.inputSchemea.length > 0) {
			toolData.configuration.inputSchemea.forEach((inputArgument) => {
				const argName = inputArgument.name[BusinessDefaultLanguage] || inputArgument.name[Object.keys(inputArgument.name)[0]] || inputArgument.id;
				const isRequired = inputArgument.isRequired || false;
				$argsSelect.append(
					`<option value="${inputArgument.id}" data-arg-name="${argName}" data-arg-type="${inputArgument.type.name}" data-arg-required="${isRequired}">${argName}${isRequired ? "*" : ""}</option>`,
				);
			});
			$argsSelect.prop("disabled", false);
		} else {
			$argsSelect.append('<option value="" disabled>Tool has no input arguments</option>');
			$argsSelect.prop("disabled", true);
		}
	}
	validateMakeCallConfig(true);
}

function handleArgumentSelectionChange($argsSelect, $argsList) {
	const $selectedOption = $argsSelect.find("option:selected");
	const argumentId = $selectedOption.val();
	if (!argumentId || argumentId === "") return;
	const argumentName = $selectedOption.data("arg-name");
	const argumentType = $selectedOption.data("arg-type");
	const isRequired = $selectedOption.data("arg-required") === true;
	const inputGroupHtml = `<div class="input-group mb-1" data-arg-id="${argumentId}"><span class="input-group-text" title="${argumentId}${isRequired ? " (Required)" : ""}">${argumentName}${isRequired ? "*" : ""}</span><input type="text" class="form-control" input_arguement="${argumentId}" placeholder="Enter ${argumentType} value" value=""><button class="btn btn-danger" btn-action="remove-makecall-action-tool-arguement" data-arg-id="${argumentId}" title="Remove Argument"><i class="fa-regular fa-trash"></i></button></div>`;
	$argsList.append(inputGroupHtml);
	$selectedOption.remove();
	$argsSelect.val("");
	validateMakeCallConfig(true);
}

function handleArgumentRemoval($removeButton, $argsSelect) {
	const $inputGroup = $removeButton.closest(".input-group");
	const argumentId = $inputGroup.data("arg-id");
	const argumentName = $inputGroup.find(".input-group-text").text().replace("*", "");
	const isRequired = $inputGroup.find(".input-group-text").text().includes("*");
	const toolId = $argsSelect.closest('[id*="Container"]').find('select[id*="ActionTool"]').val();
	let argumentType = "value";
	if (toolId && toolId !== "none") {
		const toolData = BusinessFullData.businessApp.tools.find((tool) => tool.id === toolId);
		const argData = toolData?.configuration?.inputSchemea.find((arg) => arg.id === argumentId);
		if (argData) argumentType = argData.type.name;
	}
	$argsSelect.append(
		`<option value="${argumentId}" data-arg-name="${argumentName}" data-arg-type="${argumentType}" data-arg-required="${isRequired}">${argumentName}${isRequired ? "*" : ""}</option>`,
	);
	$inputGroup.remove();
	validateMakeCallConfig(true);
}

// Event Handlers
function initGeneralTabHandlers() {
	makeCallIdentifierInput.on("input", () => validateMakeCallConfig(true));
	makeCallDescriptionInput.on("input", () => validateMakeCallConfig(true));
}

function initNumberTabHandlers() {
	makeCallTypeSingleBox.on("click", () => handleCallTypeChange(OutboundCallNumberType.Single));
	makeCallTypeBulkBox.on("click", () => handleCallTypeChange(OutboundCallNumberType.Bulk));

	const handleOpenNumberModal = (event) => {
		event.preventDefault();
		fillMakeCallNumberModalNumbersList();
		inputMakeCallModalSearchNumberInput.val("");
		makeCallAssignNumberModalLists.find("button.active").removeClass("active");
		saveMakeCallFromNumberButton.prop("disabled", true);
		saveMakeCallFromNumberButton.data("opener-id", event.currentTarget.id);
		makeCallChangeFromNumberModal.show();
	};
	makeCallChangeFromNumberButton.on("click", handleOpenNumberModal);
	makeCallChangeFromNumberButtonBulk.on("click", handleOpenNumberModal);

	makeCallChangeFromNumberModalElement.on("click", ".make-call-assign-number-modal-list button:not(.disabled)", (event) => {
		const $button = $(event.currentTarget);
		makeCallAssignNumberModalLists.find("button.active").removeClass("active");
		$button.addClass("active");
		saveMakeCallFromNumberButton.prop("disabled", false);
	});

	saveMakeCallFromNumberButton.on("click", (event) => {
		event.preventDefault();
		const $activeButton = makeCallAssignNumberModalLists.find("button.active");
		if ($activeButton.length) {
			SelectedFromNumberId = $activeButton.attr("number-id");
			const formattedNumber = $activeButton.attr("number-formatted");
			const openerId = saveMakeCallFromNumberButton.data("opener-id");
			if (openerId === makeCallChangeFromNumberButton.attr("id")) makeCallSelectedFromNumberInput.val(formattedNumber);
			else if (openerId === makeCallChangeFromNumberButtonBulk.attr("id")) makeCallSelectedFromNumberBulkInput.val(formattedNumber);
			makeCallChangeFromNumberModal.hide();
			validateMakeCallConfig(true);
		}
	});

	const triggerNumberSearch = () => {
		fillMakeCallNumberModalNumbersList();
		makeCallAssignNumberModalLists.find("button.active").removeClass("active");
		saveMakeCallFromNumberButton.prop("disabled", true);
	};
	searchMakeCallAssignNumberModalListButton.on("click", triggerNumberSearch);
	inputMakeCallModalSearchNumberInput.on("keypress", (e) => {
		if (e.which === 13) triggerNumberSearch;
	});

	makeCallToNumberInput.on("input", () => validateMakeCallConfig(true));
	makeCallToNumberBulkInput.on("change", (event) => {
		SelectedBulkFromFileObject = event.target.files.length > 0 ? event.target.files[0] : null;
		validateMakeCallConfig(true);
	});
}

function initConfigurationTabHandlers() {
	makeCallScheduleTypeNowRadio.on("change", () => {
		if (makeCallScheduleTypeNowRadio.is(":checked")) {
			makeCallScheduleDateTimeContainer.addClass("d-none").removeClass("show");
			validateMakeCallConfig(true);
		}
	});
	makeCallScheduleTypeLaterRadio.on("change", () => {
		if (makeCallScheduleTypeLaterRadio.is(":checked")) {
			makeCallScheduleDateTimeContainer.removeClass("d-none").addClass("show");
			validateMakeCallConfig(true);
		}
	});
	makeCallScheduleDateTimeInput.on("input", () => validateMakeCallConfig(true));

	makeCallRetryOnDeclineCheck.on("change", () => handleRetryOptionsVisibility("Decline"));
	makeCallRetryDeclineCountInput.on("input", () => validateMakeCallConfig(true));
	makeCallRetryDeclineDelayInput.on("input", () => validateMakeCallConfig(true));
	makeCallRetryDeclineUnitSelect.on("change", () => validateMakeCallConfig(true));

	makeCallRetryOnMissCheck.on("change", () => handleRetryOptionsVisibility("Miss"));
	makeCallRetryMissCountInput.on("input", () => validateMakeCallConfig(true));
	makeCallRetryMissDelayInput.on("input", () => validateMakeCallConfig(true));
	makeCallRetryMissUnitSelect.on("change", () => validateMakeCallConfig(true));

	makeCallNumberSilenceNotifyInput.on("input", () => validateMakeCallConfig(true));
	makeCallNumberSilenceEndInput.on("input", () => validateMakeCallConfig(true));
	makeCallNumberTotalCallTimeInput.on("input", () => validateMakeCallConfig(true));
}

function initAgentTabHandlers() {
	makeCallChangeAgentButton.on("click", (event) => {
		event.preventDefault();
		fillMakeCallAgentModalList();
		inputMakeCallSelectAgentSearch.val("");
		makeCallSelectAgentModalList.find("button.active").removeClass("active");
		if (SelectedAgentId) makeCallSelectAgentModalList.find(`button[agent-id="${SelectedAgentId}"]`).addClass("active");
		saveMakeCallAgentButton.prop("disabled", true);
		makeCallChangeAgentModal.show();
	});

	const triggerAgentSearch = () => {
		fillMakeCallAgentModalList();
		makeCallSelectAgentModalList.find("button.active").removeClass("active");
		saveMakeCallAgentButton.prop("disabled", true);
	};
	searchMakeCallSelectAgentListButton.on("click", triggerAgentSearch);
	inputMakeCallSelectAgentSearch.on("keypress", (e) => {
		if (e.which === 13) triggerAgentSearch;
	});

	makeCallSelectAgentModalList.on("click", "button", (event) => {
		const $button = $(event.currentTarget);
		if ($button.hasClass("active")) {
			saveMakeCallAgentButton.prop("disabled", false);
			return;
		}
		makeCallSelectAgentModalList.find("button.active").removeClass("active");
		$button.addClass("active");
		saveMakeCallAgentButton.prop("disabled", false);
	});

	saveMakeCallAgentButton.on("click", (event) => {
		event.preventDefault();
		const $activeButton = makeCallSelectAgentModalList.find("button.active");
		if ($activeButton.length) {
			const agentId = $activeButton.attr("agent-id");
			const agentData = BusinessFullData.businessApp.agents.find((agent) => agent.id === agentId);
			if (agentData) {
				SelectedAgentId = agentId;
				SelectedAgentData = agentData;
				const agentName = agentData.general.name[BusinessDefaultLanguage] || agentData.general.name[Object.keys(agentData.general.name)[0]] || "Unnamed Agent";
				const agentEmoji = agentData.general.emoji || "🤖";
				makeCallSelectedAgentIcon.text(agentEmoji);
				makeCallSelectedAgentNameInput.val(agentName);
				populateAgentScripts(agentData);
				makeCallChangeAgentModal.hide();
				validateMakeCallConfig(true);
			} else {
				console.error(`Selected agent with ID ${agentId} not found.`);
				AlertManager.createAlert({ type: "danger", message: "Selected agent data not found.", timeout: 5000 });
			}
		}
	});

	makeCallAgentDefaultScriptSelect.on("change", () => validateMakeCallConfig(true));
	makeCallAgentLanguageSelect.on("change", () => validateMakeCallConfig(true));
	makeCallAgentInterruptionTypeSelect.on("change", handleInterruptionTypeChange);
	makeCallAgentTurnByTurnUseInterruptedResponseInNextTurn.on("change", () => validateMakeCallConfig(true));
	makeCallAgentConversationTypeInterruptibleAudioActivityDuration.on("input", () => validateMakeCallConfig(true));
	makeCallAgentInterruptViaAIUseAgentLLM.on("change", (event) => {
		const isChecked = $(event.currentTarget).is(":checked");
		if (isChecked) agentMakeCallInterruptionViaLLMIntegrationSelectBox.addClass("d-none");
		else agentMakeCallInterruptionViaLLMIntegrationSelectBox.removeClass("d-none");
		validateMakeCallConfig(true);
	});
	agentMakeCallInterruptionViaLLMIntegrationSelect.on("change", () => validateMakeCallConfig(true));
	makeCallNumberTimezoneSelect.on("change", () => validateMakeCallConfig(true));
	makeCallAgentFromNumberInContextCheck.on("change", () => validateMakeCallConfig(true));
	makeCallAgentToNumberInContextCheck.on("change", () => validateMakeCallConfig(true));
}

function initActionsTabHandlers() {
	makeCallActionToolDeclinedSelect.on("change", (event) => {
		handleToolSelectionChange($(event.currentTarget), makeCallActionToolDeclinedArgsContainer, makeCallActionToolDeclinedArgsSelect, makeCallActionToolDeclinedArgsList);
	});
	makeCallActionToolMissSelect.on("change", (event) => {
		handleToolSelectionChange($(event.currentTarget), makeCallActionToolMissArgsContainer, makeCallActionToolMissArgsSelect, makeCallActionToolMissArgsList);
	});
	makeCallActionToolPickedUpSelect.on("change", (event) => {
		handleToolSelectionChange($(event.currentTarget), makeCallActionToolPickedUpArgsContainer, makeCallActionToolPickedUpArgsSelect, makeCallActionToolPickedUpArgsList);
	});
	makeCallActionToolEndedSelect.on("change", (event) => {
		handleToolSelectionChange($(event.currentTarget), makeCallActionToolEndedArgsContainer, makeCallActionToolEndedArgsSelect, makeCallActionToolEndedArgsList);
	});

	makeCallActionToolDeclinedArgsSelect.on("change", (event) => {
		handleArgumentSelectionChange($(event.currentTarget), makeCallActionToolDeclinedArgsList);
	});
	makeCallActionToolMissArgsSelect.on("change", (event) => {
		handleArgumentSelectionChange($(event.currentTarget), makeCallActionToolMissArgsList);
	});
	makeCallActionToolPickedUpArgsSelect.on("change", (event) => {
		handleArgumentSelectionChange($(event.currentTarget), makeCallActionToolPickedUpArgsList);
	});
	makeCallActionToolEndedArgsSelect.on("change", (event) => {
		handleArgumentSelectionChange($(event.currentTarget), makeCallActionToolEndedArgsList);
	});

	makeCallsTab.on("input", "#makeCallActionToolDeclinedInputArgumentsList input", () => validateMakeCallConfig(true));
	makeCallsTab.on("input", "#makeCallActionToolMissInputArgumentsList input", () => validateMakeCallConfig(true));
	makeCallsTab.on("input", "#makeCallActionToolPickedUpInputArgumentsList input", () => validateMakeCallConfig(true));
	makeCallsTab.on("input", "#makeCallActionToolEndedInputArgumentsList input", () => validateMakeCallConfig(true));

	makeCallsTab.on("click", 'button[btn-action="remove-makecall-action-tool-arguement"]', (event) => {
		event.preventDefault();
		const $button = $(event.currentTarget);
		const $container = $button.closest('[id*="Container"]');
		let $argsSelect;
		if ($container.is("#makeCallActionToolDeclinedContainer")) $argsSelect = makeCallActionToolDeclinedArgsSelect;
		else if ($container.is("#makeCallActionToolMissContainer")) $argsSelect = makeCallActionToolMissArgsSelect;
		else if ($container.is("#makeCallActionToolPickedUpContainer")) $argsSelect = makeCallActionToolPickedUpArgsSelect;
		else if ($container.is("#makeCallActionToolEndedContainer")) $argsSelect = makeCallActionToolEndedArgsSelect;
		if ($argsSelect) handleArgumentRemoval($button, $argsSelect);
		else console.error("Could not determine corresponding argument select for removal.");
	});
}

// INIT
function initMakeCallsTab() {
	const tooltipTriggerList = makeCallsTab[0].querySelectorAll('[data-bs-toggle="tooltip"]');
	[...tooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));
	makeCallChangeFromNumberModal = new bootstrap.Modal(makeCallChangeFromNumberModalElement[0]);
	makeCallChangeAgentModal = new bootstrap.Modal(makeCallChangeAgentModalElement[0]);

	// Pre-populate Dropdowns
	populateLanguageSelect();
	fillMakeCallAgentInterruptViaAIIntegrationSelect();
	populateToolSelect(makeCallActionToolDeclinedSelect);
	populateToolSelect(makeCallActionToolMissSelect);
	populateToolSelect(makeCallActionToolPickedUpSelect);
	populateToolSelect(makeCallActionToolEndedSelect);

	// Set Initial Form State
	resetMakeCallForm();

	// Attach Event Handlers
	// Header Buttons
	initiateCallButton.on("click", async (event) => {
		event.preventDefault();
		if (IsInitiatingCall) return;

		const validationResult = validateMakeCallConfig(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({ type: "danger", message: `Please fix the errors:<br><br>${validationResult.errors.join("<br>")}`, timeout: 8000 });
			makeCallsTab.find(".is-invalid").first().focus();
			return;
		}

		const confirmed = await confirmInitiateCall();
		if (!confirmed) {
			return; // User cancelled
		}

		IsInitiatingCall = true;
		initiateCallButton.prop("disabled", true);
		initiateCallButtonSpinner.removeClass("d-none");
		resetButton.prop("disabled", true);

		const callConfigData = gatherMakeCallConfig();

		var requestData = new FormData();
		requestData.append("config", JSON.stringify(callConfigData));

		if (CurrentMakeCallType === OutboundCallNumberType.Bulk) {
			if (!SelectedBulkFromFileObject) {
				if (errorCallback) errorCallback({ success: false, message: "Bulk file is missing." }, true);
				return;
			}

			requestData.append("bulk_file", SelectedBulkFromFileObject);
		}

		InitiateCall(
			requestData,
			(response) => {
				AlertManager.createAlert({ type: "success", message: response.message || "Call campaign initiated successfully!", timeout: 5000 });
				resetMakeCallForm();

				IsInitiatingCall = false;
				initiateCallButton.prop("disabled", false);
				initiateCallButtonSpinner.addClass("d-none");
				resetButton.prop("disabled", false);
			},
			(errorResponse, isUnsuccessful) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while initiating call(s). Check browser console for logs.",
					timeout: 6000,
				});

				console.error("Error occured while initiating call(s): ", errorResponse);

				IsInitiatingCall = false;
				initiateCallButton.prop("disabled", false);
				initiateCallButtonSpinner.addClass("d-none");
				resetButton.prop("disabled", false);
			},
		)
	});

	resetButton.on("click", (event) => {
		event.preventDefault();
		resetMakeCallForm();
	});

	$("#nav-bar").on("tabChange", async (event) => {
		const sourceTabId = event.detail.from;

		if (sourceTabId !== "make-calls-tab") {
			return;
		}

		const canLeave = await canLeaveMakeCallTab(" Are you sure you want to discard these settings and leave?");
		if (!canLeave) {
			event.preventDefault();
			return;
		}

		populateLanguageSelect();
		fillMakeCallAgentInterruptViaAIIntegrationSelect();
		populateToolSelect(makeCallActionToolDeclinedSelect);
		populateToolSelect(makeCallActionToolMissSelect);
		populateToolSelect(makeCallActionToolPickedUpSelect);
		populateToolSelect(makeCallActionToolEndedSelect);

		resetMakeCallForm();
	});

	// Tab Handlers
	initGeneralTabHandlers();
	initNumberTabHandlers();
	initConfigurationTabHandlers();
	initAgentTabHandlers();
	initActionsTabHandlers();
}
