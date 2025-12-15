/** Dynamic Variables **/
const NumberProviderEnum = {
	MODEMTEL: 1,
	TWILIO: 2,
	VONAGE: 3,
	TELYNX: 4,
	SIP: 10
};

// ModemTel
let CurrentManageModemTelNumberData = null;
let ManageModemTelNumberType = null;
let IsSavingModemTelNumber = false;

// Twilio
let CurrentManageTwilioNumberData = null;
let ManageTwilioNumberType = null;
let IsSavingTwilioNumber = false;

// SIP Variables
let CurrentManageSipNumberData = null;
let ManageSipNumberType = null;
let IsSavingSipNumber = false;

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
const physicalNumberModalRegionSelect = addNewCustomSimNumberModalElement.find("#physicalNumberModalRegionSelect");
const addNewPhysicalNumberButton = addNewCustomSimNumberModalElement.find("#addNewPhysicalNumberButton");
const addNewPhysicalNumberButtonSpinner = addNewPhysicalNumberButton.find(".save-button-spinner");

// Twilio
const twilioNumbersTable = numbersTab.find("#twilioNumbersTable");
const addNewTwilioNumberModalButton = numbersTab.find("#addNewTwilioNumberButton");
const addNewTwilioNumberModalElement = $("#addNewTwilioNumberModal");
let addNewTwilioNumberModal = null;
const twilioNumberModalIntegrationSelect = addNewTwilioNumberModalElement.find("#twilioNumberModalIntegrationSelect");
const twilioNumberModalCountrySelect = addNewTwilioNumberModalElement.find("#twilioNumberModalCountrySelect");
const twilioNumberModalNumberInput = addNewTwilioNumberModalElement.find("#twilioNumberModalNumberInput");
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
const sipNumberModalCountryContainer = addNewSipNumberModalElement.find("#sipNumberModalCountryContainer");
const sipNumberModalCountrySelect = addNewSipNumberModalElement.find("#sipNumberModalCountrySelect");
const sipNumberModalNumberHelp = addNewSipNumberModalElement.find("#sipNumberModalNumberHelp");
const sipNumberModalAllowedIpsInput = addNewSipNumberModalElement.find("#sipNumberModalAllowedIpsInput");
const sipNumberModalRegionSelect = addNewSipNumberModalElement.find("#sipNumberModalRegionSelect");
const addNewSipNumberSaveButton = addNewSipNumberModalElement.find("#addNewSipNumberSaveButton");
const addNewSipNumberSaveButtonSpinner = addNewSipNumberSaveButton.find(".save-button-spinner");

/** API Functions **/
function SaveBusinessNumberToAPI(formData, successCallback, errorCallback) {
	$.ajax({
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

/** Functions **/

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
		routeId: null,
		regionId: "",
		regionWebhookEndpoint: "",
		provider: {
			value: NumberProviderEnum.MODEMTEL,
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

	const physicalSimNumbersList = BusinessFullData.businessApp.numbers.filter((numberData) => numberData.provider.value === NumberProviderEnum.MODEMTEL);

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
		provider: NumberProviderEnum.MODEMTEL,
	};

	changes.integrationId = physicalNumberModalIntegrationSelect.find("option:selected").val();
	if (CurrentManageModemTelNumberData.integrationId !== changes.integrationId) {
        hasChanges = true;
    }

	changes.countryCode = physicalNumberModalCountrySelect.find("option:selected").val();
	if (CurrentManageModemTelNumberData.countryCode !== changes.countryCode) {
		hasChanges = true;
	}

	changes.number = physicalNumberModalNumberInput.val();
	if (CurrentManageModemTelNumberData.number !== changes.number) {
		hasChanges = true;
	}

	changes.regionId = physicalNumberModalRegionSelect.find("option:selected").val();
	if (CurrentManageModemTelNumberData.regionId !== changes.regionId) {
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
		CurrentManageModemTelNumberData = createDefaultModemTelNumberObject();

		ManageModemTelNumberType = "new";

		addNewCustomSimNumberModal.show();
	});

	addNewCustomSimNumberModalElement.on("show.bs.modal", () => {
		resetOrClearModemTelModal();

		physicalNumberModalIntegrationSelect.val(CurrentManageModemTelNumberData.integrationId);
		physicalNumberModalNumberInput.val(CurrentManageModemTelNumberData.number);

		physicalNumberModalCountrySelect.val(CurrentManageModemTelNumberData.countryCode);
		physicalNumberModalRegionSelect.val(CurrentManageModemTelNumberData.regionId);

		const shouldDisableFields = ManageModemTelNumberType === "edit";

		physicalNumberModalNumberInput.prop("disabled", shouldDisableFields);
		physicalNumberModalCountrySelect.prop("disabled", shouldDisableFields);
		physicalNumberModalIntegrationSelect.prop("disabled", shouldDisableFields);
	});

	addNewCustomSimNumberModalElement.on("hide.bs.modal", (event) => {
		if (IsSavingModemTelNumber) {
			AlertManager.createAlert({
				type: "warning",
				message: "Please wait while saving changes before closing the ModemTel number modal...",
				timeout: 6000,
			});

			event.preventDefault();
			return false;
		}

		CurrentManageModemTelNumberData = null;
		ManageModemTelNumberType = null;

		addNewCustomSimNumberModalElement.find(".is-invalid").removeClass("is-invalid");

		addNewPhysicalNumberButton.prop("disabled", true);
		addNewPhysicalNumberButtonSpinner.addClass("d-none");
	});

	addNewCustomSimNumberModalElement.on("change, input", "input, textarea, select", () => {
		ValidateModemTelNumberModalData();
		CheckModemTelNumberModalHasChanges();
	});

	addNewPhysicalNumberButton.on("click", (event) => {
		event.preventDefault();

		if (IsSavingModemTelNumber) return;

		const validationResult = ValidateModemTelNumberModalData(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `ModemTel Saving Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
				timeout: 6000,
			});
			return;
		}

		const changes = CheckModemTelNumberModalHasChanges(false);
		if (!changes.hasChanges) return;

		IsSavingModemTelNumber = true;

		addNewPhysicalNumberButton.prop("disabled", true);
		addNewPhysicalNumberButtonSpinner.removeClass("d-none");

		const formData = new FormData();
		formData.append("postType", ManageModemTelNumberType);
		formData.append("changes", JSON.stringify(changes.changes));

		if (ManageModemTelNumberType === "edit") {
			formData.append("numberId", CurrentManageModemTelNumberData.id);
		}

		SaveBusinessNumberToAPI(
			formData,
			(responseResult) => {
				if (ManageModemTelNumberType === "new") {
					BusinessFullData.businessApp.numbers.push(responseResult);

					physicalSimNumbersTable.find("tbody").prepend(CreateBusinessModemTelNumbersTableElement(responseResult));

					physicalSimNumbersTable.find('tbody tr[tr-type="none-notice"]').remove();
				} else {
					const exisitingIndex = BusinessFullData.businessApp.numbers.findIndex((numberData) => numberData.id === CurrentManageModemTelNumberData.id);
					BusinessFullData.businessApp.numbers[exisitingIndex] = responseResult;

					const exisitingUserPhysicalNumbersTableElement = physicalSimNumbersTable.find(`tbody tr[number-id="${CurrentManageModemTelNumberData.id}"]`);
					exisitingUserPhysicalNumbersTableElement.replaceWith(CreateBusinessModemTelNumbersTableElement(responseResult));
				}

				AlertManager.createAlert({
					type: "success",
					message: `Successfully ${ManageModemTelNumberType === "new" ? "added" : "updated"} business ModemTel number.`,
					timeout: 6000,
				});

				IsSavingModemTelNumber = false;

				addNewCustomSimNumberModal.hide();
			},
			(errorResult) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while saving business number. Check browser console for logs.",
					timeout: 6000,
				});

				console.log("Error occured while saving business number: ", errorResult);

				addNewPhysicalNumberButton.prop("disabled", false);
				addNewPhysicalNumberButtonSpinner.addClass("d-none");

				IsSavingModemTelNumber = false;
			},
		);
	});

	physicalSimNumbersTable.on("click", 'button[button-type="edit-physical-number"]', (event) => {
		event.preventDefault();
		event.stopPropagation();

		const currentElement = $(event.currentTarget);

		const numberId = currentElement.attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find((number) => number.id === numberId);

		CurrentManageModemTelNumberData = numberData;
		ManageModemTelNumberType = "edit";

		addNewCustomSimNumberModal.show();
	});

	physicalSimNumbersTable.on("click", 'button[button-type="view-webhook-physical-number"]', async (event) => {
		event.preventDefault();
		event.stopPropagation();

		const currentElement = $(event.currentTarget);

		const numberId = currentElement.attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find((number) => number.id === numberId);

		const webhookURI = `https://${numberData.regionWebhookEndpoint}/api/modemtel/webhook/incoming/${CurrentBusinessId}/${numberId}`;

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
		routeId: null,
		regionId: "",
		regionWebhookEndpoint: "",
		provider: {
			value: NumberProviderEnum.TWILIO,
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

	const twilioNumbersList = BusinessFullData.businessApp.numbers.filter((numberData) => numberData.provider.value === NumberProviderEnum.TWILIO);

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
		provider: NumberProviderEnum.TWILIO,
	};

	changes.integrationId = twilioNumberModalIntegrationSelect.find("option:selected").val();
	if (CurrentManageTwilioNumberData.integrationId !== changes.integrationId) {
		hasChanges = true;
	}

	changes.countryCode = twilioNumberModalCountrySelect.find("option:selected").val();
	if (CurrentManageTwilioNumberData.countryCode !== changes.countryCode) {
		hasChanges = true;
	}

	changes.number = twilioNumberModalNumberInput.val();
	if (CurrentManageTwilioNumberData.number !== changes.number) {
		hasChanges = true;
	}

	changes.regionId = twilioNumberModalRegionSelect.find("option:selected").val();
	if (CurrentManageTwilioNumberData.regionId !== changes.regionId) {
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
		CurrentManageTwilioNumberData = createDefaultModemTelNumberObject();

		ManageTwilioNumberType = "new";

		addNewTwilioNumberModal.show();
	});

	addNewTwilioNumberModalElement.on("show.bs.modal", () => {
		resetOrClearTwilioNumberModalData();

		twilioNumberModalIntegrationSelect.val(CurrentManageTwilioNumberData.integrationId);
		twilioNumberModalNumberInput.val(CurrentManageTwilioNumberData.number);

		twilioNumberModalCountrySelect.val(CurrentManageTwilioNumberData.countryCode);
		twilioNumberModalRegionSelect.val(CurrentManageTwilioNumberData.regionId);

		const shouldDisableFields = ManageTwilioNumberType === "edit";

		twilioNumberModalNumberInput.prop("disabled", shouldDisableFields);
		twilioNumberModalCountrySelect.prop("disabled", shouldDisableFields);
		twilioNumberModalIntegrationSelect.prop("disabled", shouldDisableFields);
	});

	addNewTwilioNumberModalElement.on("hide.bs.modal", (event) => {
		if (IsSavingTwilioNumber) {
			AlertManager.createAlert({
				type: "warning",
				message: "Please wait while saving changes before closing the Twilio number modal...",
				timeout: 6000,
			});

			event.preventDefault();
			return false;
		}

		CurrentManageTwilioNumberData = null;
		ManageTwilioNumberType = null;

		addNewTwilioNumberModalElement.find(".is-invalid").removeClass("is-invalid");

		addNewTwilioNumberButton.prop("disabled", true);
		addNewTwilioNumberButtonSpinner.addClass("d-none");
	});

	addNewTwilioNumberModalElement.on("change, input", "input, textarea, select", () => {
		ValidateTwilioNumberModalData();
		CheckTwilioNumberModalHasChanges();
	});

	addNewTwilioNumberButton.on("click", (event) => {
		event.preventDefault();

		if (IsSavingTwilioNumber) return;

		const validationResult = ValidateTwilioNumberModalData(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `Twilio Saving Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
				timeout: 6000,
			});
			return;
		}

		const changes = CheckTwilioNumberModalHasChanges(false);
		if (!changes.hasChanges) return;

		IsSavingTwilioNumber = true;

		addNewTwilioNumberButton.prop("disabled", true);
		addNewTwilioNumberButtonSpinner.removeClass("d-none");

		const formData = new FormData();
		formData.append("postType", ManageTwilioNumberType);
		formData.append("changes", JSON.stringify(changes.changes));

		if (ManageTwilioNumberType === "edit") {
			formData.append("numberId", CurrentManageTwilioNumberData.id);
		}

		SaveBusinessNumberToAPI(
			formData,
			(responseResult) => {
				if (ManageTwilioNumberType === "new") {
					BusinessFullData.businessApp.numbers.push(responseResult);

					twilioNumbersTable.find("tbody").prepend(CreateBusinessTwilioNumbersTableElement(responseResult));

					twilioNumbersTable.find('tbody tr[tr-type="none-notice"]').remove();
				} else {
					const exisitingIndex = BusinessFullData.businessApp.numbers.findIndex((numberData) => numberData.id === CurrentManageTwilioNumberData.id);
					BusinessFullData.businessApp.numbers[exisitingIndex] = responseResult;

					const exisitingUserTwilioNumbersTableElement = twilioNumbersTable.find(`tbody tr[number-id="${CurrentManageTwilioNumberData.id}"]`);
					exisitingUserTwilioNumbersTableElement.replaceWith(CreateBusinessTwilioNumbersTableElement(responseResult));
				}

				AlertManager.createAlert({
					type: "success",
					message: `Successfully ${ManageTwilioNumberType === "new" ? "added" : "updated"} business Twilio number.`,
					timeout: 6000,
				});

				IsSavingTwilioNumber = false;

				addNewTwilioNumberModal.hide();
			},
			(errorResult) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while saving Twilio number. Check browser console for logs.",
					timeout: 6000,
				});

				console.log("Error occured while saving Twilio number: ", errorResult);

				addNewTwilioNumberButton.prop("disabled", false);
				addNewTwilioNumberButtonSpinner.addClass("d-none");

				IsSavingTwilioNumber = false;
			},
		);
	});

	twilioNumbersTable.on("click", 'button[button-type="edit-twilio-number"]', (event) => {
		event.preventDefault();
		event.stopPropagation();

		const currentElement = $(event.currentTarget);

		const numberId = currentElement.attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find((number) => number.id === numberId);

		CurrentManageTwilioNumberData = numberData;
		ManageTwilioNumberType = "edit";

		addNewTwilioNumberModal.show();
	});

	twilioNumbersTable.on("click", 'button[button-type="view-webhook-twilio-number"]', async (event) => {
		event.preventDefault();
		event.stopPropagation();

		const currentElement = $(event.currentTarget);

		const numberId = currentElement.attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find((number) => number.id === numberId);

		const webhookURI = `https://${numberData.regionWebhookEndpoint}/api/twilio/webhook/incoming/${CurrentBusinessId}/${numberId}`;

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

	// 1. Integration
	const newIntegrationId = sipNumberModalIntegrationSelect.val();
	if (CurrentManageSipNumberData.integrationId !== newIntegrationId) {
		hasChanges = true;
	}
	changes.integrationId = newIntegrationId;

	// 2. Is E.164 & Country
	const isE164 = sipNumberModalIsE164.is(':checked');
	if (CurrentManageSipNumberData.isE164Number !== isE164) {
		hasChanges = true;
	}
	changes.isE164Number = isE164;

	let newCountryCode = "";
	if (isE164) {
		newCountryCode = sipNumberModalCountrySelect.val();
		if (CurrentManageSipNumberData.countryCode !== newCountryCode) {
			hasChanges = true;
		}
	}
	changes.countryCode = newCountryCode;

	// 3. Number
	const newNumber = sipNumberModalNumberInput.val().trim();
	if (CurrentManageSipNumberData.number !== newNumber) {
		hasChanges = true;
	}
	changes.number = newNumber;

	// 4. Region
	const newRegion = sipNumberModalRegionSelect.val();
	if (CurrentManageSipNumberData.regionId !== newRegion) {
		hasChanges = true;
	}
	changes.regionId = newRegion;

	// 5. IPs
	const newIpsStr = sipNumberModalAllowedIpsInput.val();
	const newIps = newIpsStr.split(',').map(s => s.trim()).filter(s => s !== "");
	if (JSON.stringify(CurrentManageSipNumberData.allowedSourceIps || []) !== JSON.stringify(newIps)) {
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
		validated = false;
		if (!onlyRemove) sipNumberModalIntegrationSelect.addClass("is-invalid");
	} else {
		sipNumberModalIntegrationSelect.removeClass("is-invalid");
	}

	// Country Code (Only if E.164)
	if (isE164 && !sipNumberModalCountrySelect.val()) {
		validated = false;
		if (!onlyRemove) sipNumberModalCountrySelect.addClass("is-invalid");
	} else {
		sipNumberModalCountrySelect.removeClass("is-invalid");
	}

	// Number
	const num = sipNumberModalNumberInput.val().trim();
	if (!num) {
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
		CurrentManageSipNumberData = createDefaultSipNumberObject();
		ManageSipNumberType = "new";
		addNewSipNumberModal.show();
	});

	// Modal Show Event (Populate Data)
	addNewSipNumberModalElement.on("show.bs.modal", () => {
		resetOrClearSipNumberModalData(); // Refill integration list

		// Load Values
		sipNumberModalIntegrationSelect.val(CurrentManageSipNumberData.integrationId);
		sipNumberModalIsE164.prop('checked', CurrentManageSipNumberData.isE164Number);

		toggleSipCountrySelect(CurrentManageSipNumberData.isE164Number);

		if (CurrentManageSipNumberData.isE164Number) {
			sipNumberModalCountrySelect.val(CurrentManageSipNumberData.countryCode);
		}

		sipNumberModalNumberInput.val(CurrentManageSipNumberData.number);
		sipNumberModalRegionSelect.val(CurrentManageSipNumberData.regionId);
		sipNumberModalAllowedIpsInput.val((CurrentManageSipNumberData.allowedSourceIps || []).join(", "));
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
		if (IsSavingSipNumber) {
			AlertManager.createAlert({
				type: "warning",
				message: "Please wait while saving changes...",
				timeout: 6000,
			});
			event.preventDefault();
			return false;
		}

		CurrentManageSipNumberData = null;
		ManageSipNumberType = null;
		addNewSipNumberSaveButton.prop("disabled", true);
		// addNewSipNumberSaveButtonSpinner.addClass("d-none"); // If spinner exists
	});

	// Input Changes (Validation Trigger)
	addNewSipNumberModalElement.on("change, input", "input, select", () => {
		ValidateSipNumberModalData(true); // Remove error classes on typing
		CheckSipNumberModalHasChanges(true); // Enable/Disable save button
	});

	// Save Button Click
	addNewSipNumberSaveButton.on("click", (event) => {
		event.preventDefault();

		if (IsSavingSipNumber) return;

		// Run Validation
		const validationResult = ValidateSipNumberModalData(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `Validation failed:<br>${validationResult.errors.join("<br>")}`,
				timeout: 6000,
			});
			return;
		}

		// Run Change Check
		const check = CheckSipNumberModalHasChanges(false);
		if (!check.hasChanges) return;

		IsSavingSipNumber = true;
		addNewSipNumberSaveButton.prop("disabled", true);
		// addNewSipNumberSaveButtonSpinner.removeClass("d-none");

		// Prepare Payload
		const formData = new FormData();
		formData.append("postType", ManageSipNumberType);
		// Important: Backend expects JSON string in "changes" field
		formData.append("changes", JSON.stringify(check.changes));

		if (ManageSipNumberType === "edit") {
			formData.append("numberId", CurrentManageSipNumberData.id);
		}

		// Send API Request
		SaveBusinessNumberToAPI(
			formData,
			(responseResult) => {
				// Success
				if (ManageSipNumberType === "new") {
					BusinessFullData.businessApp.numbers.push(responseResult);
					// Prepend logic handled in FillBusinessSipList usually, or manual:
					FillBusinessSipList();
				} else {
					const idx = BusinessFullData.businessApp.numbers.findIndex(n => n.id === CurrentManageSipNumberData.id);
					if (idx > -1) BusinessFullData.businessApp.numbers[idx] = responseResult;
					FillBusinessSipList();
				}

				AlertManager.createAlert({
					type: "success",
					message: `Successfully ${ManageSipNumberType === "new" ? "added" : "updated"} SIP number.`,
					timeout: 6000,
				});

				IsSavingSipNumber = false;
				addNewSipNumberModal.hide();
			},
			(errorResult) => {
				// Error
				console.error("Save Error:", errorResult);
				AlertManager.createAlert({
					type: "danger",
					message: errorResult.message || "Error saving SIP number.",
					timeout: 6000,
				});

				addNewSipNumberSaveButton.prop("disabled", false);
				IsSavingSipNumber = false;
			}
		);
	});

	// Table Action: Edit
	sipNumbersTable.on("click", 'button[button-type="edit-sip-number"]', (event) => {
		event.preventDefault();
		const numberId = $(event.currentTarget).attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find(n => n.id === numberId);

		if (numberData) {
			CurrentManageSipNumberData = numberData;
			ManageSipNumberType = "edit";
			addNewSipNumberModal.show();
		}
	});

	// Table Action: View Config (The "How To")
	sipNumbersTable.on("click", 'button[button-type="view-sip-config"]', async (event) => {
		event.preventDefault();
		const numberId = $(event.currentTarget).attr("number-id");
		const numberData = BusinessFullData.businessApp.numbers.find(n => n.id === numberId);

		// Determine Proxy Endpoint
		// Use the regionWebhookEndpoint stored in data, fallback to current window location if missing (dev mode)
		const proxyHost = numberData.regionWebhookEndpoint || "sip.your-platform.com";
		const proxyPort = 5060;

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
                    <label class="form-label fw-bold">Proxy Address</label>
                     <div class="input-group">
                        <input type="text" class="form-control" value="${proxyHost}" readonly>
                         <button class="btn btn-outline-secondary copy-btn" type="button"><i class="fa-regular fa-copy"></i></button>
                    </div>
                </div>
                <div class="mb-3">
                    <label class="form-label fw-bold">Required Headers / URI Parameters</label>
                    <div class="form-text mb-1">Add these to your Invite Request URI for faster routing.</div>
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

	// Table Action: Delete
	sipNumbersTable.on("click", 'button[button-type="delete-sip-number"]', async (event) => {
		event.preventDefault();
		const numberId = $(event.currentTarget).attr("number-id");

		const confirmDialog = new BootstrapConfirmDialog({
			title: "Delete SIP Number",
			message: "Are you sure you want to delete this SIP Trunking number? Incoming calls will stop working immediately.",
			confirmText: "Delete",
			confirmButtonClass: "btn-danger"
		});

		if (await confirmDialog.show()) {
			// Assume generic delete endpoint exists or create specific one
			// Using jQuery ajax directly as per previous examples structure
			$.ajax({
				url: `/app/user/business/${CurrentBusinessId}/numbers/delete/${numberId}`,
				type: "POST", // or DELETE
				success: (res) => {
					if (res.success) {
						// Remove from Array
						const idx = BusinessFullData.businessApp.numbers.findIndex(n => n.id === numberId);
						if (idx > -1) BusinessFullData.businessApp.numbers.splice(idx, 1);

						// Refresh UI
						FillBusinessSipList();

						AlertManager.createAlert({ type: "success", message: "Number deleted successfully.", timeout: 3000 });
					} else {
						AlertManager.createAlert({ type: "danger", message: res.message || "Failed to delete.", timeout: 3000 });
					}
				},
				error: () => AlertManager.createAlert({ type: "danger", message: "Network error.", timeout: 3000 })
			});
		}
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
