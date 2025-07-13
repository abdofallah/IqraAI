/** Dynamic Variables **/

const NumberProviderEnum = {
	MODEMTEL: 1,
	TWILIO: 2,
	VONAGE: 3,
	TELYNX: 4,
};

// ModemTel
let CurrentManageModemTelNumberData = null;
let ManageModemTelNumberType = null; // new or edit
let IsSavingModemTelNumber = false;

// Twilio
let CurrentManageTwilioNumberData = null;
let ManageTwilioNumberType = null; // new or edit
let IsSavingTwilioNumber = false;

// Vonage

/** Element Variables **/
const numbersTabTooltipTriggerList = document.querySelectorAll('#phone-numbers-tab [data-bs-toggle="tooltip"]');
const numbersTabTooltipList = [...numbersTabTooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

const numbersTab = $("#phone-numbers-tab");

// ModemTel
const addNewCustomSimNumberModalButton = numbersTab.find("#addNewCustomSimNumberButton");
const addNewCustomSimNumberModalElement = $("#addNewCustomSimNumberModal");
let addNewCustomSimNumberModal = null;
const physicalNumberModalIntegrationSelect = addNewCustomSimNumberModalElement.find("#physicalNumberModalIntegrationSelect");
const physicalNumberModalCountrySelect = addNewCustomSimNumberModalElement.find("#physicalNumberModalCountrySelect");
const physicalNumberModalNumberInput = addNewCustomSimNumberModalElement.find("#physicalNumberModalNumberInput");
const physicalNumberModalRegionSelect = addNewCustomSimNumberModalElement.find("#physicalNumberModalRegionSelect");
const addNewPhysicalNumberButton = addNewCustomSimNumberModalElement.find("#addNewPhysicalNumberButton");
const addNewPhysicalNumberButtonSpinner = addNewPhysicalNumberButton.find(".save-button-spinner");

const physicalSimNumbersTable = numbersTab.find("#physicalSimNumbersTable");

// Twilio
const addNewTwilioNumberModalButton = numbersTab.find("#addNewTwilioNumberButton");
const addNewTwilioNumberModalElement = $("#addNewTwilioNumberModal");
let addNewTwilioNumberModal = null;
const twilioNumberModalIntegrationSelect = addNewTwilioNumberModalElement.find("#twilioNumberModalIntegrationSelect");
const twilioNumberModalCountrySelect = addNewTwilioNumberModalElement.find("#twilioNumberModalCountrySelect");
const twilioNumberModalNumberInput = addNewTwilioNumberModalElement.find("#twilioNumberModalNumberInput");
const twilioNumberModalRegionSelect = addNewTwilioNumberModalElement.find("#twilioNumberModalRegionSelect");
const addNewTwilioNumberButton = addNewTwilioNumberModalElement.find("#addNewTwilioNumberButton");
const addNewTwilioNumberButtonSpinner = addNewTwilioNumberButton.find(".save-button-spinner");

const twilioNumbersTable = numbersTab.find("#twilioNumbersTable");


// Vonage
const vonageNumbersTable = numbersTab.find("#vonageNumbersTable");

// Telnyx
const telnyxNumbersTable = numbersTab.find("#telnyxNumbersTable");

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

// INIT
function initNumbersTab() {
	$(document).ready(() => {
		addNewCustomSimNumberModal = new bootstrap.Modal(addNewCustomSimNumberModalElement);
		addNewTwilioNumberModal = new bootstrap.Modal(addNewTwilioNumberModalElement);

		/** Event Listeners **/

		// ModemTel
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
		initModemtelNumberEvents();

		// Twilio
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
        initTwilioNumberEvents();

		// Init

		// ModemTel
		FillBusinessModemTelList();
		FillInitialModemTelModal();

		// Twilio
		FillBusinessTwilioList();
        FillInitialTwilioModal();
	});
}
