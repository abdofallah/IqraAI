/** Constants **/
const DeleteNumberNoteMessage = "<br><br><b>Note</b> You must remove any references (script send sms node references or routes) to this number before deleting and wait or cancel any ongoing call queues or conversations.";

/** Dynamic Variables **/
const NumberProviderEnum = {
	ModemTel: 1,
	Twilio: 2,
	Vonage: 3,
	Telnyx: 4,
	SIP: 10
};

// ModemTel
const ModemTelNumbersState = {
	CurrentManageData: null,
	CurrentManageType: null,
	IsSaving: false,
	IsDeleting: false
}

// Twilio
const TwilioNumbersState = {
	CurrentManageData: null,
	CurrentManageType: null,
	IsSaving: false,
	IsDeleting: false
}

// SIP
const SipNumbersState = {
	CurrentManageData: null,
	CurrentManageType: null,
	IsSaving: false,
	IsDeleting: false
}

/** Element Variables **/
const numbersTabTooltipTriggerList = document.querySelectorAll('#phone-numbers-tab [data-bs-toggle="tooltip"]');
const numbersTabTooltipList = [...numbersTabTooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

const numbersTab = $("#phone-numbers-tab");

// ModemTel
const physicalSimNumbersTable = numbersTab.find("#physicalSimNumbersTable");
const addNewCustomSimNumberModalButton = numbersTab.find("#addNewCustomSimNumberButton");
const addNewCustomSimNumberModalElement = $("#addNewCustomSimNumberModal");
let addNewCustomSimNumberModal = null;
const physicalNumberModalIntegrationSelect = addNewCustomSimNumberModalElement.find("#physicalNumberModalIntegrationSelect");
const physicalNumberModalCountrySelect = addNewCustomSimNumberModalElement.find("#physicalNumberModalCountrySelect");
const physicalNumberModalNumberInput = addNewCustomSimNumberModalElement.find("#physicalNumberModalNumberInput");
const physicalNumberModalVoiceEnabledCheck = addNewCustomSimNumberModalElement.find("#physicalNumberModalVoiceEnabledCheck");
const physicalNumberModalSmsEnabledCheck = addNewCustomSimNumberModalElement.find("#physicalNumberModalSmsEnabledCheck");
const physicalNumberModalRegionSelect = addNewCustomSimNumberModalElement.find("#physicalNumberModalRegionSelect");
const addNewPhysicalNumberButton = addNewCustomSimNumberModalElement.find("#addNewPhysicalNumberButton");

// Twilio
const twilioNumbersTable = numbersTab.find("#twilioNumbersTable");
const addNewTwilioNumberModalButton = numbersTab.find("#addNewTwilioNumberButton");
const addNewTwilioNumberModalElement = $("#addNewTwilioNumberModal");
let addNewTwilioNumberModal = null;
const twilioNumberModalIntegrationSelect = addNewTwilioNumberModalElement.find("#twilioNumberModalIntegrationSelect");
const twilioNumberModalCountrySelect = addNewTwilioNumberModalElement.find("#twilioNumberModalCountrySelect");
const twilioNumberModalNumberInput = addNewTwilioNumberModalElement.find("#twilioNumberModalNumberInput");
const twilioNumberModalVoiceEnabledCheck = addNewTwilioNumberModalElement.find("#twilioNumberModalVoiceEnabledCheck");
const twilioNumberModalSmsEnabledCheck = addNewTwilioNumberModalElement.find("#twilioNumberModalSmsEnabledCheck");
const twilioNumberModalRegionSelect = addNewTwilioNumberModalElement.find("#twilioNumberModalRegionSelect");
const addNewTwilioNumberButton = addNewTwilioNumberModalElement.find("#addNewTwilioNumberButton");
const addNewTwilioNumberButtonSpinner = addNewTwilioNumberButton.find(".save-button-spinner");

// Vonage
const vonageNumbersTable = numbersTab.find("#vonageNumbersTable");

// Telnyx
const telnyxNumbersTable = numbersTab.find("#telnyxNumbersTable");

// SIP Elements
const sipNumbersTable = numbersTab.find("#sipNumbersTable");
const addNewSipNumberModalButton = numbersTab.find("#addNewSipNumberButton");
const addNewSipNumberModalElement = $("#addNewSipNumberModal");
let addNewSipNumberModal = null; 
const sipNumberModalIntegrationSelect = addNewSipNumberModalElement.find("#sipNumberModalIntegrationSelect");
const sipNumberModalIsE164 = addNewSipNumberModalElement.find("#sipNumberModalIsE164");
const sipNumberModalNumberInput = addNewSipNumberModalElement.find("#sipNumberModalNumberInput");
const sipNumberModalVoiceEnabledCheck = addNewSipNumberModalElement.find("#sipNumberModalVoiceEnabledCheck");
const sipNumberModalSmsEnabledCheck = addNewSipNumberModalElement.find("#sipNumberModalSmsEnabledCheck");
const sipNumberModalCountryContainer = addNewSipNumberModalElement.find("#sipNumberModalCountryContainer");
const sipNumberModalCountrySelect = addNewSipNumberModalElement.find("#sipNumberModalCountrySelect");
const sipNumberModalNumberHelp = addNewSipNumberModalElement.find("#sipNumberModalNumberHelp");
const sipNumberModalAllowedIpsInput = addNewSipNumberModalElement.find("#sipNumberModalAllowedIpsInput");
const sipNumberModalRegionSelect = addNewSipNumberModalElement.find("#sipNumberModalRegionSelect");
const addNewSipNumberSaveButton = addNewSipNumberModalElement.find("#addNewSipNumberSaveButton");

/** API Functions **/
function SaveBusinessNumberToAPI(formData, successCallback, errorCallback) {
	return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/numbers/save`,
		type: "POST",
		data: formData,
		dataType: "json",
		processData: false,
		contentType: false,
		success: (response) => {
			if (!response.success) {
				errorCallback(response);
				return;
			}

			successCallback(response.data);
		},
		error: (error) => {
			errorCallback(error);
		},
	});
}
function DeleteBusinessNumberFromAPI(numberId, successCallback, errorCallback) {
	return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/numbers/${numberId}/delete`,
		type: "POST",
		contentType: "application/json",
		success: (response) => {
			if (!response.success) {
				errorCallback(response);
				return;
			}

			successCallback(response.data);
		},
		error: (error) => {
			errorCallback(error);
		},
    });
}
/** Functions **/

// Common Functions
async function deleteBusinessNumberHandler(event) {
	event.preventDefault();
	const button = $(event.currentTarget);
	const numberId = button.attr("number-id");
	const numberData = BusinessFullData.businessApp.numbers.find((n) => n.id === numberId);
	if (!numberData) return;

	const numberProviderState = getNumberProviderState(numberData.provider.value);
	const numberProviderName = getNumberProviderName(numberData.provider.value);

	if (SipNumbersState.IsDeleting) {
		AlertManager.createAlert({
			type: "warning",
			message: `A delete operation for ${numberProviderName} number is already in progress. Please try again once the operation is complete.`,
			timeout: 6000,
		});
        return;
	}

	const confirmDialog = new BootstrapConfirmDialog({
		title: `Delete ${numberProviderName} Number`,
		message: `Are you sure you want to delete this ${numberProviderName} Trunking number?${DeleteNumberNoteMessage}`,
		confirmText: "Delete",
		confirmButtonClass: "btn-danger"
	});

	if (await confirmDialog.show()) {
		showHideButtonSpinnerWithDisableEnable(button, true);
		numberProviderState.IsDeleting = true;

		DeleteBusinessNumberFromAPI(
			numberId,
			() => {
				const numberIndex = BusinessFullData.businessApp.numbers.findIndex(n => n.id === numberId);
				if (numberIndex === -1) return;
				BusinessFullData.businessApp.numbers.splice(numberIndex, 1);

				const numbersProviderTable = getNumberProviderTable(numberData.provider.value);
				numbersProviderTable.find("tbody").find(`tr[number-id="${numberId}"]`).remove();

                AlertManager.createAlert({
                    type: "success",
                    message: `${numberProviderName} number deleted successfully.`,
                    timeout: 6000,
                });
			},
			(errorResult) => {
				var resultMessage = "Check console logs for more details.";
				if (errorResult && errorResult.message) resultMessage = errorResult.message;

				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while deleting business number.",
					resultMessage: resultMessage,
					timeout: 6000,
				});

				console.log("Error occured while deleting business number: ", errorResult);
			}
		).always(() => {
			showHideButtonSpinnerWithDisableEnable(button, false);
			numberProviderState.IsDeleting = false;
		});
	}
}
function saveBusinessNumberHandler(event, numberProviderType) {
	event.preventDefault();

	const numberProviderState = getNumberProviderState(numberProviderType);
	const numberProviderName = getNumberProviderName(numberProviderType);

	if (numberProviderState.IsSaving) {
		AlertManager.createAlert({
			type: "warning",
			message: `A save operation for ${numberProviderName} number is already in progress.`,
			timeout: 6000,
		});
        return;
	}

	const button = $(event.currentTarget);

	const numberProviderValidateModalFunction = getNumberProviderValidateModalFunction(numberProviderType);
	const validationResult = numberProviderValidateModalFunction(false);
	if (!validationResult.validated) {
		AlertManager.createAlert({
			type: "danger",
			message: `${numberProviderName} Saving Validation failed.`,
			resultMessage: validationResult.errors.join("<br>"),
			timeout: 6000,
		});
		return;
	}

	const numberProviderCheckChangesFunction = getNumberProviderCheckModalHasChangesFunction(numberProviderType);
	const changes = numberProviderCheckChangesFunction(false);
	if (!changes.hasChanges) {
		AlertManager.createAlert({
			type: "warning",
			message: `There are no changes to save for ${numberProviderName} number.`,
			timeout: 6000,
		});
        return;
	}

	numberProviderState.IsSaving = true;

	showHideButtonSpinnerWithDisableEnable(button, true);

	const formData = new FormData();
	formData.append("postType", numberProviderState.CurrentManageType);
	formData.append("changes", JSON.stringify(changes.changes));

	if (numberProviderState.CurrentManageType === "edit") {
		formData.append("numberId", numberProviderState.CurrentManageData.id);
	}

	SaveBusinessNumberToAPI(
		formData,
		(responseResult) => {
			const numberProviderTable = getNumberProviderTable(numberProviderType);
			const numberProviderTableElementFunc = getNumberProviderCreateTableElementFunction(numberProviderType);
			const numberProviderModal = getNumberProviderModal(numberProviderType);

			if (numberProviderState.CurrentManageType === "new") {
				BusinessFullData.businessApp.numbers.push(responseResult);

				numberProviderTable.find("tbody").prepend(numberProviderTableElementFunc(responseResult));

				numberProviderTable.find('tbody tr[tr-type="none-notice"]').remove();
			} else {
				const exisitingIndex = BusinessFullData.businessApp.numbers.findIndex((numberData) => numberData.id === numberProviderState.CurrentManageData.id);
				BusinessFullData.businessApp.numbers[exisitingIndex] = responseResult;

				const exisitingUserPhysicalNumbersTableElement = numberProviderTable.find(`tbody tr[number-id="${numberProviderState.CurrentManageData.id}"]`);
				exisitingUserPhysicalNumbersTableElement.replaceWith(numberProviderTableElementFunc(responseResult));
			}

			AlertManager.createAlert({
				type: "success",
				message: `Successfully ${numberProviderState.CurrentManageType === "new" ? "added" : "updated"} business ${numberProviderName} number.`,
				timeout: 6000,
			});

			numberProviderState.IsSaving = false; // needed in order to hide the modal
			numberProviderModal.hide();
		},
		(errorResult) => {
			var resultMessage = "Check console logs for more details.";
			if (errorResult && errorResult.message) resultMessage = errorResult.message;

			AlertManager.createAlert({
				type: "danger",
				message: `Error occured while saving ${numberProviderName} business number.`,
				resultMessage: resultMessage,
				timeout: 6000,
			});
		},
	).always(() => {
		showHideButtonSpinnerWithDisableEnable(button, false);
		numberProviderState.IsSaving = false;
	})
}

// Common Helpers
function getNumberProviderName(numberProvider) {
	var numberProviderName = "Unknown";
	switch (numberProvider) {
		case NumberProviderEnum.ModemTel:
			numberProviderName = "ModemTel";
			break;
		case NumberProviderEnum.Twilio:
			numberProviderName = "Twilio";
			break;
		case NumberProviderEnum.Vonage:
			numberProviderName = "Vonage";
			break;
		case NumberProviderEnum.Telnyx:
			numberProviderName = "Telnyx";
			break;
		case NumberProviderEnum.SIP:
			numberProviderName = "SIP";
			break;
		default:
			break;
	}

    return numberProviderName;
}
function getNumberProviderTable(numberProvider) {
	switch (numberProvider) {
		case NumberProviderEnum.ModemTel:
			return modemTelNumbersTable;
		case NumberProviderEnum.Twilio:
			return  twilioNumbersTable;
		case NumberProviderEnum.Vonage:
			return  vonageNumbersTable;
		case NumberProviderEnum.Telnyx:
			return  telnyxNumbersTable;
		case NumberProviderEnum.SIP:
			return sipNumbersTable;
		default:
			break;
	}

	return null;
}
function getNumberProviderState(numberProvider) {
	switch (numberProvider) {
		case NumberProviderEnum.ModemTel:
			return ModemTelNumbersState;
		case NumberProviderEnum.Twilio:
			return TwilioNumbersState;
		case NumberProviderEnum.Vonage:
			return VonageNumbersState;
		case NumberProviderEnum.Telnyx:
			return TelnyxNumbersState;
		case NumberProviderEnum.SIP:
			return SipNumbersState;
		default:
			break;
	}

    return null;
}
function getNumberProviderValidateModalFunction(numberProvider) {
	switch (numberProvider) {
		case NumberProviderEnum.ModemTel:
			return ValidateModemTelNumberModalData;
		case NumberProviderEnum.Twilio:
			return ValidateTwilioNumberModalData;
		case NumberProviderEnum.Vonage:
			return ValidateVonageNumberModalData;
		case NumberProviderEnum.Telnyx:
			return ValidateTelnyxNumberModalData;
		case NumberProviderEnum.SIP:
			return ValidateSipNumberModalData;
		default:
			break;
	}

	return null;
}
function getNumberProviderCheckModalHasChangesFunction(numberProvider) {
	switch (numberProvider) {
		case NumberProviderEnum.ModemTel:
			return CheckModemTelNumberModalHasChanges;
		case NumberProviderEnum.Twilio:
			return CheckTwilioNumberModalHasChanges;
		case NumberProviderEnum.Vonage:
			return CheckVonageNumberModalHasChanges;
		case NumberProviderEnum.Telnyx:
			return CheckTelnyxNumberModalHasChanges;	
		case NumberProviderEnum.SIP:
			return CheckSipNumberModalHasChanges;
		default:
			break;
	}

    return null;
}
function getNumberProviderCreateTableElementFunction(numberProvider) {
	switch (numberProvider) {
		case NumberProviderEnum.ModemTel:
			return CreateBusinessModemTelNumbersTableElement;
		case NumberProviderEnum.Twilio:
			return CreateBusinessTwilioNumbersTableElement;
		case NumberProviderEnum.Vonage:
			return CreateBusinessVonageNumbersTableElement;
		case NumberProviderEnum.Telnyx:
			return CreateBusinessTelnyxNumbersTableElement;
		case NumberProviderEnum.SIP:
			return CreateBusinessSipNumbersTableElement;
		default:
			break;
	}

    return null;
}
function getNumberProviderModal(numberProvider) {
	switch (numberProvider) {
		case NumberProviderEnum.ModemTel:
			return addNewCustomSimNumberModal;
		case NumberProviderEnum.Twilio:
			return addNewTwilioNumberModal;
		case NumberProviderEnum.Vonage:
			return addNewVonageNumberModal;
		case NumberProviderEnum.Telnyx:
			return addNewTelnyxNumberModal;
		case NumberProviderEnum.SIP:
			return addNewSipNumberModal;
		default:
			break;
	}

    return null;
}

// ModemTel
function resetOrClearModemTelModal() {
	physicalNumberModalIntegrationSelect.empty();

	const modemTelIntegrations = BusinessFullData.businessApp.integrations.filter((integration) => integration.type === "modemtel");
	if (modemTelIntegrations.length == 0) {
		physicalNumberModalIntegrationSelect.append($(`<option value="" disabled selected>No ModemTel integrations found</option>`));
	}
	else {
        physicalNumberModalIntegrationSelect.append($(`<option value="" disabled selected>Select Integration</option>`));
		modemTelIntegrations.forEach((integration) => {
			physicalNumberModalIntegrationSelect.append($(`<option value="${integration.id}">${integration.friendlyName}</option>`));
		});
	}
}
function createDefaultModemTelNumberObject() {
	const object = {
		integrationId: "",
		countryCode: "",
		number: "",
		voiceEnabled: false,
        smsEnabled: false,
		routeId: null,
		regionId: "",
		regionWebhookEndpoint: "",
		provider: {
			value: NumberProviderEnum.ModemTel,
		},
	};

	return object;
}
function CreateBusinessModemTelNumbersTableElement(numberData) {
	const countryData = CountriesList[numberData.countryCode.toUpperCase()];
	const regionData = SpecificationRegionsListData.find((regionData) => regionData.countryRegion === numberData.regionId);
	const routeData = BusinessFullData.businessApp.routings.find((route) => route.id === numberData.routeId);

	let routeName = "-";
	if (routeData) {
		routeName = routeData.general.emoji + " " + routeData.general.name;
    }

	const element = $(`<tr number-id="${numberData.id}" provider-type="${numberData.provider.value}">
                <td>${countryData["Alpha-2 code"]}</td>
                <td>${numberData.number}</td>
				<td>${routeName}</td>
                <td>${regionData.countryRegion}</td>
                <td>
					<button class="btn btn-light btn-sm" number-id="${numberData.id}" button-type="view-webhook-physical-number">
                        <i class="fa-regular fa-webhook"></i>
                    </button>
                    <button class="btn btn-info btn-sm" number-id="${numberData.id}" button-type="edit-physical-number">
                        <i class="fa-regular fa-pen-to-square"></i>
                    </button>
                    <button class="btn btn-danger btn-sm" number-id="${numberData.id}" button-type="delete-physical-number">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}
function FillBusinessModemTelList() {
	physicalSimNumbersTable.find("tbody").empty();

	const physicalSimNumbersList = BusinessFullData.businessApp.numbers.filter((numberData) => numberData.provider.value === NumberProviderEnum.ModemTel);

	if (physicalSimNumbersList.length === 0) {
		physicalSimNumbersTable.find("tbody").append(`<tr tr-type="none-notice"><td colspan="5">No ModemTel numbers found</td></tr>`);
		return;
	}

	physicalSimNumbersList.forEach((numberData) => {
		physicalSimNumbersTable.find("tbody").append(CreateBusinessModemTelNumbersTableElement(numberData));
	});
}
function FillInitialModemTelModal() {
	// Fill Country Code Select
	physicalNumberModalCountrySelect.append($(`<option value="" disabled selected>Select Country Code</option>`));
	Object.keys(CountriesList).forEach((countryCode) => {
		const countryData = CountriesList[countryCode];
		physicalNumberModalCountrySelect.append($(`<option value="${countryCode}">${countryData.phone_code} - ${countryData.Country}</option>`));
	});

	// Fill Region Select
	physicalNumberModalRegionSelect.append($(`<option value="" disabled selected>Select Region</option>`));
	SpecificationRegionsListData.forEach((regionData) => {
		const countryData = CountriesList[regionData.countryCode.toUpperCase()];
		physicalNumberModalRegionSelect.append($(`<option value="${regionData.countryRegion}">${countryData.Country} (${regionData.countryRegion})</option>`));
	});
}
function CheckModemTelNumberModalHasChanges(enableDisableButton = true) {
	let hasChanges = false;
	const changes = {
		provider: NumberProviderEnum.ModemTel,
	};

	// Integration
	changes.integrationId = physicalNumberModalIntegrationSelect.find("option:selected").val();
	if (ModemTelNumbersState.CurrentManageData.integrationId !== changes.integrationId) {
        hasChanges = true;
    }

	// Country
	changes.countryCode = physicalNumberModalCountrySelect.find("option:selected").val();
	if (ModemTelNumbersState.CurrentManageData.countryCode !== changes.countryCode) {
		hasChanges = true;
	}

	// Number
	changes.number = physicalNumberModalNumberInput.val();
	if (ModemTelNumbersState.CurrentManageData.number !== changes.number) {
		hasChanges = true;
	}

	// Voice Enabled
    const voiceEnabled = physicalNumberModalVoiceEnabledCheck.is(":checked");
    if (ModemTelNumbersState.CurrentManageData.voiceEnabled !== voiceEnabled) {
        hasChanges = true;
    }
	changes.voiceEnabled = voiceEnabled;

	// Sms Enabled
    const smsEnabled = physicalNumberModalSmsEnabledCheck.is(":checked");
    if (ModemTelNumbersState.CurrentManageData.smsEnabled !== smsEnabled) {
        hasChanges = true;
    }

	// Region
	changes.regionId = physicalNumberModalRegionSelect.find("option:selected").val();
	if (ModemTelNumbersState.CurrentManageData.regionId !== changes.regionId) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		addNewPhysicalNumberButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}
function ValidateModemTelNumberModalData(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Validate Integration
    const integrationId = physicalNumberModalIntegrationSelect.find("option:selected").val();
    if (!integrationId || integrationId === "" || integrationId === null) {
        validated = false;
        errors.push("Integration is required");
        if (!onlyRemove) {
            physicalNumberModalIntegrationSelect.addClass("is-invalid");
        }
    } else {
        physicalNumberModalIntegrationSelect.removeClass("is-invalid");
    }

	// Validate Country Code
	const countryCode = physicalNumberModalCountrySelect.find("option:selected").val();
	if (!countryCode || countryCode === "" || countryCode === null) {
		validated = false;
		errors.push("Country Code is required");
		if (!onlyRemove) {
			physicalNumberModalCountrySelect.addClass("is-invalid");
		}
	} else {
		physicalNumberModalCountrySelect.removeClass("is-invalid");
	}

	// Validate Number
	const number = physicalNumberModalNumberInput.val();
	if (!number || number === "" || number === null) {
		validated = false;
		errors.push("Number is required");
		if (!onlyRemove) {
			physicalNumberModalNumberInput.addClass("is-invalid");
		}
	} else {
		physicalNumberModalNumberInput.removeClass("is-invalid");
	}

	// Validate Region
	const regionId = physicalNumberModalRegionSelect.find("option:selected").val();
	if (!regionId || regionId === "" || regionId === null) {
		validated = false;
		errors.push("Region is required");
		if (!onlyRemove) {
			physicalNumberModalRegionSelect.addClass("is-invalid");
		}
	} else {
		physicalNumberModalRegionSelect.removeClass("is-invalid");
	}

	return {
		validated: validated,
		errors: errors,
	};
}
function initModemtelNumberEvents() {
	addNewCustomSimNumberModalButton.on("click", () => {
		ModemTelNumbersState.CurrentManageData = createDefaultModemTelNumberObject();

		ModemTelNumbersState.CurrentManageType = "new";

		addNewCustomSimNumberModal.show();
	});

	addNewCustomSimNumberModalElement.on("show.bs.modal", () => {
		resetOrClearModemTelModal();

		physicalNumberModalIntegrationSelect.val(ModemTelNumbersState.CurrentManageData.integrationId);
		physicalNumberModalNumberInput.val(ModemTelNumbersState.CurrentManageData.number);
        physicalNumberModalVoiceEnabledCheck.prop("checked", ModemTelNumbersState.CurrentManageData.voiceEnabled);
        physicalNumberModalSmsEnabledCheck.prop("checked", ModemTelNumbersState.CurrentManageData.smsEnabled);

		physicalNumberModalCountrySelect.val(ModemTelNumbersState.CurrentManageData.countryCode);
		physicalNumberModalRegionSelect.val(ModemTelNumbersState.CurrentManageData.regionId);

		const shouldDisableFields = ModemTelNumbersState.CurrentManageType === "edit";

		physicalNumberModalNumberInput.prop("disabled", shouldDisableFields);
		physicalNumberModalCountrySelect.prop("disabled", shouldDisableFields);
		physicalNumberModalIntegrationSelect.prop("disabled", shouldDisableFields);
	});

	addNewCustomSimNumberModalElement.on("hide.bs.modal", (event) => {
		if (ModemTelNumbersState.IsSaving) {
			AlertManager.createAlert({
				type: "warning",
				message: "Please wait while saving changes before closing the ModemTel number modal...",
				timeout: 6000,
			});

			event.preventDefault();
			return false;
		}

		ModemTelNumbersState.CurrentManageData = null;
		ModemTelNumbersState.CurrentManageType = null;

		addNewCustomSimNumberModalElement.find(".is-invalid").removeClass("is-invalid");

		addNewPhysicalNumberButton.prop("disabled", true);
	});

	addNewCustomSimNumberModalElement.on("change, input", "input, textarea, select", () => {
		ValidateModemTelNumberModalData();
		CheckModemTelNumberModalHasChanges();
	});

	addNewPhysicalNumberButton.on("click", (event) => {
		saveBusinessNumberHandler(event, NumberProviderEnum.ModemTel);
	});

	physicalSimNumbersTable.on("click", 'button[button-type="edit-physical-number"]', (event) => {
		event.preventDefault();
		event.stopPropagation();

		const currentElement = $(event.currentTarget);

		const numberId = currentElement.attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find((number) => number.id === numberId);

		ModemTelNumbersState.CurrentManageData = numberData;
		ModemTelNumbersState.CurrentManageType = "edit";

		addNewCustomSimNumberModal.show();
	});

	physicalSimNumbersTable.on("click", 'button[button-type="view-webhook-physical-number"]', async (event) => {
		event.preventDefault();
		event.stopPropagation();

		const currentElement = $(event.currentTarget);

		const numberId = currentElement.attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find((number) => number.id === numberId);

		const regionData = SpecificationRegionsListData.find((r) => r.countryRegion === numberData.regionId);
		if (!regionData) {
			AlertManager.createAlert({
				type: "danger",
				message: `Could not find region data for: ${numberData.regionId}`,
				timeout: 6000,
			});

			return;
		}

		const regionProxyServerData = regionData.servers.find((p) => p.id === numberData.regionServerId);
		if (!regionProxyServerData) {
			AlertManager.createAlert({
				type: "danger",
				message: `Could not find proxy server data for: ${numberData.regionServerId}`,
				timeout: 6000,
			});

			return;
		}

		const proxyHost = regionProxyServerData.endpoint;
		const proxyIsSSL = regionProxyServerData.isSSL;

		const webhookURI = `${proxyIsSSL ? "https" : "http"}://${proxyHost}/api/modemtel/webhook/incoming/${CurrentBusinessId}/${numberId}`;

		const webhookDialog = new BootstrapConfirmDialog({
			title: `Number (${numberData.countryCode}-${numberData.number})  Webhook`,
			message: `
					<div class="mb-3">
						<label class="form-label">Webhook URI</label>
						<input type="text" class="form-control" value="${webhookURI}" readonly>
					</div>

					<a href="https://www.modemtel.com" target="_blank" class="text-decoration-none">
						<i class="fa-regular fa-circle-question me-1"></i>
						How to set your Phone Number Webhook URL?
					</a>
				`,
			confirmText: "Copy & Close",
			cancelText: "Cancel",
			confirmButtonClass: "btn-success",
			modalClass: "modal-lg",
		});

		const confirmResult = await webhookDialog.show();
		if (confirmResult) {
			navigator.clipboard.writeText(webhookURI);

			AlertManager.createAlert({
				type: "success",
				message: "Copied webhook URI to clipboard.",
				timeout: 2000,
			});
		}
	});

	// Table Action: Delete ModemTel Number
	physicalSimNumbersTable.on("click", 'button[button-type="delete-physical-number"]', async (event) => {
		await deleteBusinessNumberHandler(event);
	});
}

// Twilio
function resetOrClearTwilioNumberModalData() {
	twilioNumberModalIntegrationSelect.empty();

	const twilioIntegrations = BusinessFullData.businessApp.integrations.filter((integration) => integration.type === "twilio");
	if (twilioIntegrations.length == 0) {
		twilioNumberModalIntegrationSelect.append($(`<option value="" disabled selected>No Twilio integrations found</option>`));
	}
	else {
		twilioNumberModalIntegrationSelect.append($(`<option value="" disabled selected>Select Integration</option>`));
		twilioIntegrations.forEach((integration) => {
			twilioNumberModalIntegrationSelect.append($(`<option value="${integration.id}">${integration.friendlyName}</option>`));
		});
	}
}
function createDefaultTwilioNumberObject() {
	const object = {
		integrationId: "",
		countryCode: "",
		number: "",
		voiceEnabled: false,
		smsEnabled: false,
		routeId: null,
		regionId: "",
		regionWebhookEndpoint: "",
		provider: {
			value: NumberProviderEnum.Twilio,
		},
	};

	return object;
}
function CreateBusinessTwilioNumbersTableElement(numberData) {
	const countryData = CountriesList[numberData.countryCode.toUpperCase()];
	const regionData = SpecificationRegionsListData.find((regionData) => regionData.countryRegion === numberData.regionId);
	const routeData = BusinessFullData.businessApp.routings.find((route) => route.id === numberData.routeId);

	let routeName = "-";
	if (routeData) {
		routeName = routeData.general.emoji + " " + routeData.general.name;
	}

	const element = $(`<tr number-id="${numberData.id}" provider-type="${numberData.provider.value}">
                <td>${countryData["Alpha-2 code"]}</td>
                <td>${numberData.number}</td>
				<td>${routeName}</td>
                <td>${regionData.countryRegion}</td>
                <td>
					<button class="btn btn-light btn-sm" number-id="${numberData.id}" button-type="view-webhook-twilio-number">
                        <i class="fa-regular fa-webhook"></i>
                    </button>
                    <button class="btn btn-info btn-sm" number-id="${numberData.id}" button-type="edit-twilio-number">
                        <i class="fa-regular fa-pen-to-square"></i>
                    </button>
                    <button class="btn btn-danger btn-sm" number-id="${numberData.id}" button-type="delete-twilio-number">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}
function FillBusinessTwilioList() {
	twilioNumbersTable.find("tbody").empty();

	const twilioNumbersList = BusinessFullData.businessApp.numbers.filter((numberData) => numberData.provider.value === NumberProviderEnum.Twilio);

	if (twilioNumbersList.length === 0) {
		twilioNumbersTable.find("tbody").append(`<tr tr-type="none-notice"><td colspan="5">No Twilio numbers found</td></tr>`);
		return;
	}

	twilioNumbersList.forEach((numberData) => {
		twilioNumbersTable.find("tbody").append(CreateBusinessTwilioNumbersTableElement(numberData));
	});
}
function FillInitialTwilioModal() {
	twilioNumberModalCountrySelect.append($(`<option value="" disabled selected>Select Country Code</option>`));
	Object.keys(CountriesList).forEach((countryCode) => {
		const countryData = CountriesList[countryCode];
		twilioNumberModalCountrySelect.append($(`<option value="${countryCode}">${countryData.phone_code} - ${countryData.Country}</option>`));
	});

	twilioNumberModalRegionSelect.append($(`<option value="" disabled selected>Select Region</option>`));
	SpecificationRegionsListData.forEach((regionData) => {
		const countryData = CountriesList[regionData.countryCode.toUpperCase()];
		twilioNumberModalRegionSelect.append($(`<option value="${regionData.countryRegion}">${countryData.Country} (${regionData.countryRegion})</option>`));
	});
}
function CheckTwilioNumberModalHasChanges(enableDisableButton = true) {
	let hasChanges = false;
	const changes = {
		provider: NumberProviderEnum.Twilio,
	};

	// Integration
	changes.integrationId = twilioNumberModalIntegrationSelect.find("option:selected").val();
	if (TwilioNumbersState.CurrentManageData.integrationId !== changes.integrationId) {
		hasChanges = true;
	}

	// Country
	changes.countryCode = twilioNumberModalCountrySelect.find("option:selected").val();
	if (TwilioNumbersState.CurrentManageData.countryCode !== changes.countryCode) {
		hasChanges = true;
	}

	// Number
	changes.number = twilioNumberModalNumberInput.val();
	if (TwilioNumbersState.CurrentManageData.number !== changes.number) {
		hasChanges = true;
	}

	// Voice Enabled
    const voiceEnabled = twilioNumberModalVoiceEnabledCheck.is(":checked");
    if (TwilioNumbersState.CurrentManageData.voiceEnabled !== voiceEnabled) {
        hasChanges = true;
    }
	changes.voiceEnabled = voiceEnabled;

	// SMS Enabled
    const smsEnabled = twilioNumberModalSmsEnabledCheck.is(":checked");
    if (TwilioNumbersState.CurrentManageData.smsEnabled !== smsEnabled) {
        hasChanges = true;
    }
    changes.smsEnabled = smsEnabled;

	// Region
	changes.regionId = twilioNumberModalRegionSelect.find("option:selected").val();
	if (TwilioNumbersState.CurrentManageData.regionId !== changes.regionId) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		addNewTwilioNumberButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}
function ValidateTwilioNumberModalData(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Validate Integration
	const integrationId = twilioNumberModalIntegrationSelect.find("option:selected").val();
	if (!integrationId || integrationId === "" || integrationId === null) {
		validated = false;
		errors.push("Integration is required");
		if (!onlyRemove) {
			twilioNumberModalIntegrationSelect.addClass("is-invalid");
		}
	} else {
		twilioNumberModalIntegrationSelect.removeClass("is-invalid");
	}

	// Validate Country Code
	const countryCode = twilioNumberModalCountrySelect.find("option:selected").val();
	if (!countryCode || countryCode === "" || countryCode === null) {
		validated = false;
		errors.push("Country Code is required");
		if (!onlyRemove) {
			twilioNumberModalCountrySelect.addClass("is-invalid");
		}
	} else {
		twilioNumberModalCountrySelect.removeClass("is-invalid");
	}

	// Validate Number
	const number = twilioNumberModalNumberInput.val();
	if (!number || number === "" || number === null) {
		validated = false;
		errors.push("Number is required");
		if (!onlyRemove) {
			twilioNumberModalNumberInput.addClass("is-invalid");
		}
	} else {
		twilioNumberModalNumberInput.removeClass("is-invalid");
	}

	// Validate Region
	const regionId = twilioNumberModalRegionSelect.find("option:selected").val();
	if (!regionId || regionId === "" || regionId === null) {
		validated = false;
		errors.push("Region is required");
		if (!onlyRemove) {
			twilioNumberModalRegionSelect.addClass("is-invalid");
		}
	} else {
		twilioNumberModalRegionSelect.removeClass("is-invalid");
	}

	return {
		validated: validated,
		errors: errors,
	};
}
function initTwilioNumberEvents() {
	addNewTwilioNumberModalButton.on("click", () => {
		TwilioNumbersState.CurrentManageData = createDefaultModemTelNumberObject();

		TwilioNumbersState.CurrentManageType = "new";

		addNewTwilioNumberModal.show();
	});

	addNewTwilioNumberModalElement.on("show.bs.modal", () => {
		resetOrClearTwilioNumberModalData();

		twilioNumberModalIntegrationSelect.val(TwilioNumbersState.CurrentManageData.integrationId);
		twilioNumberModalNumberInput.val(TwilioNumbersState.CurrentManageData.number);
        twilioNumberModalVoiceEnabledCheck.prop("checked", TwilioNumbersState.CurrentManageData.voiceEnabled);
        twilioNumberModalSmsEnabledCheck.prop("checked", TwilioNumbersState.CurrentManageData.smsEnabled);

		twilioNumberModalCountrySelect.val(TwilioNumbersState.CurrentManageData.countryCode);
		twilioNumberModalRegionSelect.val(TwilioNumbersState.CurrentManageData.regionId);

		const shouldDisableFields = TwilioNumbersState.CurrentManageType === "edit";

		twilioNumberModalNumberInput.prop("disabled", shouldDisableFields);
		twilioNumberModalCountrySelect.prop("disabled", shouldDisableFields);
		twilioNumberModalIntegrationSelect.prop("disabled", shouldDisableFields);
	});

	addNewTwilioNumberModalElement.on("hide.bs.modal", (event) => {
		if (TwilioNumbersState.IsSaving) {
			AlertManager.createAlert({
				type: "warning",
				message: "Please wait while saving changes before closing the Twilio number modal...",
				timeout: 6000,
			});

			event.preventDefault();
			return false;
		}

		TwilioNumbersState.CurrentManageData = null;
		TwilioNumbersState.CurrentManageType = null;

		addNewTwilioNumberModalElement.find(".is-invalid").removeClass("is-invalid");

		addNewTwilioNumberButton.prop("disabled", true);
	});

	addNewTwilioNumberModalElement.on("change, input", "input, textarea, select", () => {
		ValidateTwilioNumberModalData();
		CheckTwilioNumberModalHasChanges();
	});

	addNewTwilioNumberButton.on("click", (event) => {
		saveBusinessNumberHandler(event, NumberProviderEnum.Twilio);
	});

	twilioNumbersTable.on("click", 'button[button-type="edit-twilio-number"]', (event) => {
		event.preventDefault();
		event.stopPropagation();

		const currentElement = $(event.currentTarget);

		const numberId = currentElement.attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find((number) => number.id === numberId);

		TwilioNumbersState.CurrentManageData = numberData;
		TwilioNumbersState.CurrentManageType = "edit";

		addNewTwilioNumberModal.show();
	});

	twilioNumbersTable.on("click", 'button[button-type="view-webhook-twilio-number"]', async (event) => {
		event.preventDefault();
		event.stopPropagation();

		const currentElement = $(event.currentTarget);

		const numberId = currentElement.attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find((number) => number.id === numberId);

		const regionData = SpecificationRegionsListData.find((r) => r.countryRegion === numberData.regionId);
		if (!regionData) {
			AlertManager.createAlert({
				type: "danger",
				message: `Could not find region data for: ${numberData.regionId}`,
				timeout: 6000,
			});

			return;
		}

		const regionProxyServerData = regionData.servers.find((p) => p.id === numberData.regionServerId);
		if (!regionProxyServerData) {
			AlertManager.createAlert({
				type: "danger",
				message: `Could not find proxy server data for: ${numberData.regionServerId}`,
				timeout: 6000,
			});

			return;
		}

		const proxyHost = regionProxyServerData.endpoint;
		const proxyIsSSL = regionProxyServerData.isSSL;

		const webhookURI = `${proxyIsSSL ? "https" : "http"}://${proxyHost}/api/twilio/webhook/voice/incoming/${CurrentBusinessId}/${numberId}`;

		const webhookDialog = new BootstrapConfirmDialog({
			title: `Twilio Number (${numberData.countryCode}-${numberData.number})  Webhook`,
			message: `
					<div class="mb-3">
						<label class="form-label">Webhook URI (HTTP Post)</label>
						<input type="text" class="form-control" value="${webhookURI}" readonly>
					</div>

					<a href="https://www.twilio.com/docs/usage/webhooks/getting-started-twilio-webhooks" target="_blank" class="text-decoration-none">
						<i class="fa-regular fa-circle-question me-1"></i>
						How to set your Phone Number Webhook URL?
					</a>
				`,
			confirmText: "Copy & Close",
			cancelText: "Cancel",
			confirmButtonClass: "btn-success",
			modalClass: "modal-lg",
		});

		const confirmResult = await webhookDialog.show();
		if (confirmResult) {
			navigator.clipboard.writeText(webhookURI);

			AlertManager.createAlert({
				type: "success",
				message: "Copied webhook URI to clipboard.",
				timeout: 2000,
			});
		}
	});

	// Table Action: Delete Twilio Number
	twilioNumbersTable.on("click", 'button[button-type="delete-twilio-number"]', async (event) => {
		await deleteBusinessNumberHandler(event);
	});
}

// Vonage
function CreateBusinessVonageNumbersTableElement(numberData) {
	const countryData = CountriesList[numberData.countryCode.toUpperCase()];

	const element = $(`<tr>
                <td><span class="badge bg-success">Online</span></td>
                <td>${countryData["Alpha-2 code"]}</td>
                <td>${numberData.number}</td>
                <td>
                    <button class="btn btn-info btn-sm" number-email="${numberData.id}" button-type="edit-physical-number">
                        <i class="fa-regular fa-eye"></i>
                    </button>
                    <button class="btn btn-danger btn-sm">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}

// Telnyx
function CreateBusinessTelnyxNumbersTableElement(numberData) {
	const element = "TODO";

	// todo

	return element;
}

// SIP
function resetOrClearSipNumberModalData() {
	sipNumberModalIntegrationSelect.empty();

	// Filter for SIP integrations
	const sipIntegrations = BusinessFullData.businessApp.integrations.filter((integration) => integration.type === "sip_trunking");

	if (sipIntegrations.length === 0) {
		sipNumberModalIntegrationSelect.append($(`<option value="" disabled selected>No SIP Trunk integrations found</option>`));
	} else {
		sipNumberModalIntegrationSelect.append($(`<option value="" disabled selected>Select Integration</option>`));
		sipIntegrations.forEach((integration) => {
			sipNumberModalIntegrationSelect.append($(`<option value="${integration.id}">${integration.friendlyName}</option>`));
		});
	}

	sipNumberModalCountrySelect.append($(`<option value="" disabled selected>Select Country Code</option>`));
	Object.keys(CountriesList).forEach((countryCode) => {
		const countryData = CountriesList[countryCode];
		sipNumberModalCountrySelect.append($(`<option value="${countryCode}">${countryData.phone_code} - ${countryData.Country}</option>`));
	});

	// Reset Toggle
	sipNumberModalIsE164.prop('checked', false);
	toggleSipCountrySelect(false);
}
function createDefaultSipNumberObject() {
	return {
		integrationId: "",
		isE164Number: false,
		countryCode: "", // Empty if not E.164
		number: "",
		voiceEnabled: false,
		smsEnabled: false,
		allowedSourceIps: [],
		routeId: null,
		regionId: "",
		provider: { value: NumberProviderEnum.SIP }
	};
}
function CreateBusinessSipNumbersTableElement(numberData) {
	const regionData = SpecificationRegionsListData.find((r) => r.countryRegion === numberData.regionId);
	const routeData = BusinessFullData.businessApp.routings.find((r) => r.id === numberData.routeId);
	let routeName = routeData ? (routeData.general.emoji + " " + routeData.general.name) : "-";
	let ips = (numberData.allowedSourceIps || []).join(", ");
	if (!ips) ips = "<span class='text-warning'>Any (Unsafe)</span>";

	return $(`<tr number-id="${numberData.id}" provider-type="${numberData.provider.value}">
        <td>${numberData.number}</td>
        <td>${routeName}</td>
        <td>${regionData ? regionData.countryRegion : numberData.regionId}</td>
        <td>${ips}</td>
        <td>
            <button class="btn btn-light btn-sm" number-id="${numberData.id}" button-type="view-sip-config">
                <i class="fa-regular fa-circle-info"></i>
            </button>
            <button class="btn btn-info btn-sm" number-id="${numberData.id}" button-type="edit-sip-number">
                <i class="fa-regular fa-pen-to-square"></i>
            </button>
            <button class="btn btn-danger btn-sm" number-id="${numberData.id}" button-type="delete-sip-number">
                <i class="fa-regular fa-trash"></i>
            </button>
        </td>
    </tr>`);
}
function FillBusinessSipList() {
	sipNumbersTable.find("tbody").empty();
	const list = BusinessFullData.businessApp.numbers.filter(n => n.provider.value === NumberProviderEnum.SIP);

	if (list.length === 0) {
		sipNumbersTable.find("tbody").append(`<tr tr-type="none-notice"><td colspan="5">No SIP numbers found</td></tr>`);
		return;
	}
	list.forEach(n => sipNumbersTable.find("tbody").append(CreateBusinessSipNumbersTableElement(n)));
}
function FillInitialSipModal() {
	sipNumberModalRegionSelect.append($(`<option value="" disabled selected>Select Region</option>`));
	SpecificationRegionsListData.forEach((r) => {
		// Use Country list to get full name if needed, or just region code
		sipNumberModalRegionSelect.append($(`<option value="${r.countryRegion}">${r.countryRegion}</option>`));
	});
}
function CheckSipNumberModalHasChanges(enableButton = true) {
	let hasChanges = false;
	const changes = { provider: NumberProviderEnum.SIP };

	// Integration
	const newIntegrationId = sipNumberModalIntegrationSelect.find("option:selected").val();
	if (SipNumbersState.CurrentManageData.integrationId !== newIntegrationId) {
		hasChanges = true;
	}
	changes.integrationId = newIntegrationId;

	// Is E.164 & Country
	const isE164 = sipNumberModalIsE164.is(':checked');
	if (SipNumbersState.CurrentManageData.isE164Number !== isE164) {
		hasChanges = true;
	}
	changes.isE164Number = isE164;

	let newCountryCode = "";
	if (isE164) {
		newCountryCode = sipNumberModalCountrySelect.find("option:selected").val();
		if (SipNumbersState.CurrentManageData.countryCode !== newCountryCode) {
			hasChanges = true;
		}
	}
	changes.countryCode = newCountryCode;

	// Number
	const newNumber = sipNumberModalNumberInput.val().trim();
	if (SipNumbersState.CurrentManageData.number !== newNumber) {
		hasChanges = true;
	}
	changes.number = newNumber;

	// Voice Enabled
    const voiceEnabled = sipNumberModalVoiceEnabledCheck.is(':checked');
    if (SipNumbersState.CurrentManageData.voiceEnabled !== voiceEnabled) {
        hasChanges = true;
    }
	changes.voiceEnabled = voiceEnabled;

	// SMS Enabled
    const smsEnabled = sipNumberModalSmsEnabledCheck.is(':checked');
    if (SipNumbersState.CurrentManageData.smsEnabled !== smsEnabled) {
        hasChanges = true;
    }
    changes.smsEnabled = smsEnabled;

	// Region
	const newRegion = sipNumberModalRegionSelect.find("option:selected").val();
	if (SipNumbersState.CurrentManageData.regionId !== newRegion) {
		hasChanges = true;
	}
	changes.regionId = newRegion;

	// IPs
	const newIpsStr = sipNumberModalAllowedIpsInput.val();
	const newIps = newIpsStr.split(',').map(s => s.trim()).filter(s => s !== "");
	if (JSON.stringify(SipNumbersState.CurrentManageData.allowedSourceIps || []) !== JSON.stringify(newIps)) {
		hasChanges = true;
	}
	changes.allowedSourceIps = newIps;

	if (enableButton) addNewSipNumberSaveButton.prop("disabled", !hasChanges);
	return { hasChanges, changes };
}
function ValidateSipNumberModalData(onlyRemove = true) {
	const errors = [];
	let validated = true;
	const isE164 = sipNumberModalIsE164.is(':checked');

	// Integration
	if (!sipNumberModalIntegrationSelect.val()) {
        errors.push("Integration is required.");
		validated = false;
		if (!onlyRemove) sipNumberModalIntegrationSelect.addClass("is-invalid");
	} else {
		sipNumberModalIntegrationSelect.removeClass("is-invalid");
	}

	// Country Code (Only if E.164)
	if (isE164 && !sipNumberModalCountrySelect.val()) {
        errors.push("Country code is required if E.164 is enabled.");
		validated = false;
		if (!onlyRemove) sipNumberModalCountrySelect.addClass("is-invalid");
	} else {
		sipNumberModalCountrySelect.removeClass("is-invalid");
	}

	// Number
	const num = sipNumberModalNumberInput.val().trim();
	if (!num) {
        errors.push("Number is required.");
		validated = false;
		if (!onlyRemove) sipNumberModalNumberInput.addClass("is-invalid");
	} else {
		// Optional: Regex check for E.164 digits vs Custom string
		if (isE164 && !/^\d+$/.test(num)) {
			validated = false;
			errors.push("E.164 Numbers must contain digits only.");
			if (!onlyRemove) sipNumberModalNumberInput.addClass("is-invalid");
		} else {
			sipNumberModalNumberInput.removeClass("is-invalid");
		}
	}

	// Region
	if (!sipNumberModalRegionSelect.val()) {
        errors.push("Region is required.");
		validated = false;
		if (!onlyRemove) sipNumberModalRegionSelect.addClass("is-invalid");
	} else {
		sipNumberModalRegionSelect.removeClass("is-invalid");
	}

	return { validated, errors };
}
function toggleSipCountrySelect(isE164) {
	if (isE164) {
		sipNumberModalCountryContainer.slideDown(200);
		sipNumberModalNumberInput.attr('placeholder', 'e.g. 501234567');
		sipNumberModalNumberHelp.text("Enter the number without country code.");
	} else {
		sipNumberModalCountryContainer.slideUp(200);
		sipNumberModalNumberInput.attr('placeholder', 'e.g. 1001 or support_line');
		sipNumberModalNumberHelp.text("The user part of the SIP URI (e.g. sip:1001@proxy).");
	}
}
function initSipNumberEvents() {
	// Open "Add New" Modal
	addNewSipNumberModalButton.on("click", () => {
		SipNumbersState.CurrentManageData = createDefaultSipNumberObject();
		SipNumbersState.CurrentManageType = "new";
		addNewSipNumberModal.show();
	});

	// Modal Show Event (Populate Data)
	addNewSipNumberModalElement.on("show.bs.modal", () => {
		// Load Values
		resetOrClearSipNumberModalData(); // Refill integration list

		sipNumberModalIntegrationSelect.val(SipNumbersState.CurrentManageData.integrationId);
		sipNumberModalIsE164.prop('checked', SipNumbersState.CurrentManageData.isE164Number);

        sipNumberModalVoiceEnabledCheck.prop('checked', SipNumbersState.CurrentManageData.voiceEnabled);
        sipNumberModalSmsEnabledCheck.prop('checked', SipNumbersState.CurrentManageData.smsEnabled);

		toggleSipCountrySelect(SipNumbersState.CurrentManageData.isE164Number);

		if (SipNumbersState.CurrentManageData.isE164Number) {
			sipNumberModalCountrySelect.val(SipNumbersState.CurrentManageData.countryCode);
		}

		sipNumberModalNumberInput.val(SipNumbersState.CurrentManageData.number);
		sipNumberModalRegionSelect.val(SipNumbersState.CurrentManageData.regionId);
		sipNumberModalAllowedIpsInput.val((SipNumbersState.CurrentManageData.allowedSourceIps || []).join(", "));
	});

	// Toggle Switch Listener
	sipNumberModalIsE164.on('change', function () {
		const checked = $(this).is(':checked');
		toggleSipCountrySelect(checked);
		ValidateSipNumberModalData(true);
		CheckSipNumberModalHasChanges(true);
	});

	// Modal Hide Event (Cleanup)
	addNewSipNumberModalElement.on("hide.bs.modal", (event) => {
		if (SipNumbersState.IsSaving) {
			AlertManager.createAlert({
				type: "warning",
				message: "Please wait while saving changes before closing the SIP number modal...",
				timeout: 6000,
			});
			event.preventDefault();
			return false;
		}

		SipNumbersState.CurrentManageData = null;
		SipNumbersState.CurrentManageType = null;

		addNewSipNumberModalElement.find(".is-invalid").removeClass("is-invalid");

		addNewSipNumberSaveButton.prop("disabled", true);
	});

	// Input Changes (Validation Trigger)
	addNewSipNumberModalElement.on("change, input", "input, select", () => {
		ValidateSipNumberModalData(true); // Remove error classes on typing
		CheckSipNumberModalHasChanges(true); // Enable/Disable save button
	});

	// Save Button Click
	addNewSipNumberSaveButton.on("click", (event) => {
		saveBusinessNumberHandler(event, NumberProviderEnum.SIP);
	});

	// Table Action: Edit
	sipNumbersTable.on("click", 'button[button-type="edit-sip-number"]', (event) => {
		event.preventDefault();
		const numberId = $(event.currentTarget).attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find(n => n.id === numberId);

		if (numberData) {
			SipNumbersState.CurrentManageData = numberData;
			SipNumbersState.CurrentManageType = "edit";
			addNewSipNumberModal.show();
		}
	});

	// Table Action: View Config (The "How To")
	sipNumbersTable.on("click", 'button[button-type="view-sip-config"]', async (event) => {
		event.preventDefault();
		const numberId = $(event.currentTarget).attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find(n => n.id === numberId);

		// Determine Proxy Endpoint
		const regionData = SpecificationRegionsListData.find((r) => r.countryRegion === numberData.regionId);
		if (!regionData) {
			AlertManager.createAlert({
                type: "danger",
                message: `Could not find region data for: ${numberData.regionId}`,
				timeout: 6000,
			});

            return;
		}

		const regionProxyServerData = regionData.servers.find((p) => p.id === numberData.regionServerId);
		if (!regionProxyServerData) {
			AlertManager.createAlert({
                type: "danger",
				message: `Could not find proxy server data for: ${numberData.regionServerId}`,
                timeout: 6000,
			});

            return;
		}

		const proxyHost = regionProxyServerData.endpoint;
		const proxyPort = regionProxyServerData.sipPort;

		// Construct the SIP URI that the carrier should send to
		// sip:DID@ProxyHost:Port
		const sipUri = `sip:${numberData.number}@${proxyHost}:${proxyPort}`;

		// Construct the Headers needed for optimization
		const headerString = `X-Business-Id=${CurrentBusinessId};X-Phone-Id=${numberData.id}`;

		const configDialog = new BootstrapConfirmDialog({
			title: `SIP Configuration: ${numberData.number}`,
			message: `
                <div class="alert alert-info">
                    <i class="fa-regular fa-info-circle"></i> Configure your Carrier / PBX to route calls to this URI.
                </div>
                <div class="mb-3">
                    <label class="form-label fw-bold">SIP URI (Destination)</label>
                    <div class="input-group">
                        <input type="text" class="form-control" value="${sipUri}" readonly>
                        <button class="btn btn-outline-secondary copy-btn" type="button"><i class="fa-regular fa-copy"></i></button>
                    </div>
                </div>
                <div class="mb-3">
                    <label class="form-label fw-bold">Required Headers / URI Parameters</label>
                    <div class="form-text mb-1">Add these to your Invite Request URI.</div>
                    <div class="input-group">
                        <input type="text" class="form-control" value="${headerString}" readonly>
                         <button class="btn btn-outline-secondary copy-btn" type="button"><i class="fa-regular fa-copy"></i></button>
                    </div>
                </div>
            `,
			confirmText: "Close",
			hideCancel: true,
			modalClass: "modal-lg"
		});

		// Simple copy logic for the dialog
		$(configDialog.getModalElement()).on('click', '.copy-btn', function () {
			const input = $(this).prev('input');
			navigator.clipboard.writeText(input.val());
			const icon = $(this).find('i');
			icon.removeClass('fa-copy').addClass('fa-check');
			setTimeout(() => icon.removeClass('fa-check').addClass('fa-copy'), 1500);
		});

		await configDialog.show();
	});

	// Table Action: Delete SIP Number
	sipNumbersTable.on("click", 'button[button-type="delete-sip-number"]', async (event) => {
		await deleteBusinessNumberHandler(event);
	});
}

// INIT
function initNumbersTab() {
	$(document).ready(() => {
		// ModemTel
		addNewCustomSimNumberModal = new bootstrap.Modal(addNewCustomSimNumberModalElement);
		initModemtelNumberEvents();
		FillBusinessModemTelList();
		FillInitialModemTelModal();

		// Twilio
		addNewTwilioNumberModal = new bootstrap.Modal(addNewTwilioNumberModalElement);
		initTwilioNumberEvents();
		FillBusinessTwilioList();
		FillInitialTwilioModal();

		// SIP
		addNewSipNumberModal = new bootstrap.Modal(addNewSipNumberModalElement);
        initSipNumberEvents();
		FillBusinessSipList();
		FillInitialSipModal();
	});
}
