/** Dynamic Variables **/

const NumberProviderEnum = {
	PHYSICAL: 1,
	TWILIO: 2,
	VONAGE: 3,
	TELYNX: 4,
};

// Physical Sim
let CurrentManagePhysicalNumberData = null;
let ManagePhysicalNumberType = null; // new or edit
let IsSavingPhysicalNumber = false;

// Twilio

// Vonage

/** Element Variables **/

const numbersTab = $("#phone-numbers-tab");

// Physical Sim
const addNewCustomSimNumberButton = numbersTab.find("#addNewCustomSimNumberButton");
const addNewCustomSimNumberModalElement = $("#addNewCustomSimNumberModal");
let addNewCustomSimNumberModal = null;
const physicalNumberModalCountrySelect = addNewCustomSimNumberModalElement.find("#physicalNumberModalCountrySelect");
const physicalNumberModalNumberInput = addNewCustomSimNumberModalElement.find("#physicalNumberModalNumberInput");
const physicalNumberModalRegionSelect = addNewCustomSimNumberModalElement.find("#physicalNumberModalRegionSelect");
const addNewPhysicalNumberButton = addNewCustomSimNumberModalElement.find("#addNewPhysicalNumberButton");
const addNewPhysicalNumberButtonSpinner = addNewPhysicalNumberButton.find(".save-button-spinner");

const physicalSimNumbersTable = numbersTab.find("#physicalSimNumbersTable");

// Twilio
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

// Physical Sim
function createDefaultPhysicalNumberObject() {
	const object = {
		countryCode: "",
		number: "",
		regionId: "",
		provider: {
			value: 1,
		},
	};

	return object;
}

function CreateBusinessPhysicalNumbersTableElement(numberData) {
	const countryData = CountriesList[numberData.countryCode.toUpperCase()];
	const regionData = SpecificationRegionsListData.find((regionData) => regionData.id === numberData.regionId);

	let statusElement = "";
	if (numberData.status.value === 0) {
		statusElement = `<span class="badge bg-danger">Offline</span>`;
	} else if (numberData.status.value === 1) {
		statusElement = `<span class="badge bg-success">Online</span>`;
	} else {
		statusElement = `<span class="badge bg-warning">Unknown</span>`;
	}

	const element = $(`<tr number-id="${numberData.id}" provider-type="${numberData.provider.value}">
                <td>${statusElement}</td>
                <td>${countryData["Alpha-2 code"]}</td>
                <td>${numberData.number}</td>
                <td>${regionData.id}</td>
                <td>
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

function FillBusinessPhysicalSimList() {
	physicalSimNumbersTable.find("tbody").empty();

	const physicalSimNumbersList = BusinessFullData.businessApp.numbers.filter((numberData) => numberData.provider.value === NumberProviderEnum.PHYSICAL);

	if (physicalSimNumbersList.length === 0) {
		physicalSimNumbersTable.find("tbody").append(`<tr tr-type="none-notice"><td colspan="5">No physical sim numbers found</td></tr>`);
		return;
	}

	physicalSimNumbersList.forEach((numberData) => {
		physicalSimNumbersTable.find("tbody").append(CreateBusinessPhysicalNumbersTableElement(numberData));
	});
}

function FillInitialPhysicalSimModal() {
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
		physicalNumberModalRegionSelect.append($(`<option value="${regionData.id}">${countryData.Country} (${regionData.id})</option>`));
	});
}

function CheckPhysicalNumberModalHasChanges(enableDisableButton = true) {
	let hasChanges = false;
	const changes = {
		provider: NumberProviderEnum.PHYSICAL,
	};

	changes.countryCode = physicalNumberModalCountrySelect.find("option:selected").val();
	if (CurrentManagePhysicalNumberData.countryCode !== changes.countryCode) {
		hasChanges = true;
	}

	changes.number = physicalNumberModalNumberInput.val();
	if (CurrentManagePhysicalNumberData.number !== changes.number) {
		hasChanges = true;
	}

	changes.regionId = physicalNumberModalRegionSelect.find("option:selected").val();
	if (CurrentManagePhysicalNumberData.regionId !== changes.regionId) {
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

function ValidatePhysicalNumberModalData(onlyRemove = true) {
	const errors = [];
	let validated = true;

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
function CreateBusinessTwilioNumbersTableElement(numberData) {
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

function initNumbersTab() {
	$(document).ready(() => {
		addNewCustomSimNumberModal = new bootstrap.Modal(addNewCustomSimNumberModalElement);

		/** Event Listeners **/

		// Physical Sim
		addNewCustomSimNumberButton.on("click", () => {
			CurrentManagePhysicalNumberData = createDefaultPhysicalNumberObject();

			ManagePhysicalNumberType = "new";

			addNewCustomSimNumberModal.show();
		});

		addNewCustomSimNumberModalElement.on("show.bs.modal", () => {
			physicalNumberModalNumberInput.val(CurrentManagePhysicalNumberData.number);

			physicalNumberModalCountrySelect.val(CurrentManagePhysicalNumberData.countryCode);
			physicalNumberModalRegionSelect.val(CurrentManagePhysicalNumberData.regionId);

			const shouldDisableFields = ManagePhysicalNumberType === "edit";

			physicalNumberModalNumberInput.prop("disabled", shouldDisableFields);
			physicalNumberModalCountrySelect.prop("disabled", shouldDisableFields);
		});

		addNewCustomSimNumberModalElement.on("hide.bs.modal", (event) => {
			if (IsSavingPhysicalNumber) {
				AlertManager.createAlert({
					type: "warning",
					message: "Please wait while saving changes before closing the physical sim number modal...",
					timeout: 6000,
				});

				event.preventDefault();
				return false;
			}

			CurrentManagePhysicalNumberData = null;
			ManagePhysicalNumberType = null;

			addNewCustomSimNumberModalElement.find(".is-invalid").removeClass("is-invalid");

			addNewPhysicalNumberButton.prop("disabled", true);
			addNewPhysicalNumberButtonSpinner.addClass("d-none");
		});

		addNewCustomSimNumberModalElement.on("change, input", "input, textarea, select", () => {
			ValidatePhysicalNumberModalData();
			CheckPhysicalNumberModalHasChanges();
		});

		addNewPhysicalNumberButton.on("click", (event) => {
			event.preventDefault();

			if (IsSavingPhysicalNumber) return;

			const validationResult = ValidatePhysicalNumberModalData(false);
			if (!validationResult.validated) {
				AlertManager.createAlert({
					type: "danger",
					message: `Physical Sim Saving Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
					timeout: 6000,
				});
				return;
			}

			const changes = CheckPhysicalNumberModalHasChanges(false);
			if (!changes.hasChanges) return;

			IsSavingPhysicalNumber = true;

			addNewPhysicalNumberButton.prop("disabled", true);
			addNewPhysicalNumberButtonSpinner.removeClass("d-none");

			const formData = new FormData();
			formData.append("postType", ManagePhysicalNumberType);
			formData.append("changes", JSON.stringify(changes.changes));

			if (ManagePhysicalNumberType === "edit") {
				formData.append("numberId", CurrentManagePhysicalNumberData.id);
			}

			SaveBusinessNumberToAPI(
				formData,
				(responseResult) => {
					if (ManagePhysicalNumberType === "new") {
						PhysicalSimNumbersList.push(responseResult);

						physicalSimNumbersTable.find("tbody").prepend(CreateBusinessPhysicalNumbersTableElement(responseResult));

						physicalSimNumbersTable.find('tbody tr[tr-type="none-notice"]').remove();
					} else {
						const exisitingIndex = PhysicalSimNumbersList.findIndex((numberData) => numberData.id === CurrentManagePhysicalNumberData.id);
						PhysicalSimNumbersList[exisitingIndex] = responseResult;

						const exisitingUserPhysicalNumbersTableElement = physicalSimNumbersTable.find(`tbody tr[number-id="${CurrentManagePhysicalNumberData.id}"]`);
						exisitingUserPhysicalNumbersTableElement.replaceWith(CreateBusinessPhysicalNumbersTableElement(responseResult));
					}

					AlertManager.createAlert({
						type: "success",
						message: `Successfully ${ManagePhysicalNumberType === "new" ? "added" : "updated"} business physical sim number.`,
						timeout: 6000,
					});

					IsSavingPhysicalNumber = false;

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

					IsSavingPhysicalNumber = false;
				},
			);
		});

		physicalSimNumbersTable.on("click", 'button[button-type="edit-physical-number"]', (event) => {
			event.preventDefault();
			event.stopPropagation();

			const currentElement = $(event.currentTarget);

			const numberId = currentElement.attr("number-id");
			const numberData = PhysicalSimNumbersList.find((number) => number.id === numberId);

			CurrentManagePhysicalNumberData = numberData;
			ManagePhysicalNumberType = "edit";

			addNewCustomSimNumberModal.show();
		});

		// Init
		FillBusinessPhysicalSimList();
		FillInitialPhysicalSimModal();
	});
}
