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
const physicalNumberModalBusinessSelect = addNewCustomSimNumberModalElement.find("#physicalNumberModalBusinessSelect");
const addNewPhysicalNumberButton = addNewCustomSimNumberModalElement.find("#addNewPhysicalNumberButton");
const addNewPhysicalNumberButtonSpinner = addNewPhysicalNumberButton.find(".save-button-spinner");

const physicalSimNumbersTable = numbersTab.find("#physicalSimNumbersTable");

// Twilio
const twilioNumbersTable = numbersTab.find("#twilioNumbersTable");

// Vonage
const vonageNumbersTable = numbersTab.find("#vonageNumbersTable");

// Telnyx
const telenyxNumbersTable = numbersTab.find("#telenyxNumbersTable");

/** API Functions **/
function AddNewUserNumberToAPI(formData, successCallback, errorCallback) {
	$.ajax({
		url: "/app/user/number/add",
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
		assignedToBusinessId: "",
		regionId: "",
		provider: {
			value: 1,
		},
	};

	return object;
}

function CreateUserPhysicalNumbersTableElement(numberData) {
	const countryData = CountriesList[numberData.countryCode.toUpperCase()];
	const businessData = CurrentBusinessesList.find((businessData) => businessData.id === numberData.assignedToBusinessId);
	const regionData = CurrentRegionsList.find((regionData) => regionData.id === numberData.regionId);

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
                <td>${businessData == null ? "-" : businessData.name}</td>
                <td>
                    <button class="btn btn-info btn-sm" number-id="${numberData.id}" button-type="edit-physical-number">
                        <i class="fa-regular fa-eye"></i>
                    </button>
                    <button class="btn btn-danger btn-sm" number-id="${numberData.id}" button-type="delete-physical-number">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>`);

	return element;
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
	const changes = {};

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

	changes.assignedToBusinessId = physicalNumberModalBusinessSelect.find("option:selected").val();
	if (CurrentManagePhysicalNumberData.assignedToBusinessId !== changes.assignedToBusinessId) {
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
function CreateUserTwilioNumbersTableElement(numberData) {
	const countryData = CountriesList[numberData.countryCode.toUpperCase()];
	const businessData = CurrentBusinessesList.find((businessData) => businessData.id === numberData.assignedToBusinessId);

	const element = $(`<tr>
                <td><span class="badge bg-success">Online</span></td>
                <td>${countryData["Alpha-2 code"]}</td>
                <td>${numberData.number}</td>
                <td>${businessData == null ? "-" : businessData.name}</td>
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
function CreateUserVonageNumbersTableElement(numberData) {
	const countryData = CountriesList[numberData.countryCode.toUpperCase()];
	const businessData = CurrentBusinessesList.find((businessData) => businessData.id === numberData.assignedToBusinessId);

	const element = $(`<tr>
                <td><span class="badge bg-success">Online</span></td>
                <td>${countryData["Alpha-2 code"]}</td>
                <td>${numberData.number}</td>
                <td>${businessData == null ? "-" : businessData.name}</td>
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
function CreateUserTelenyxNumbersTableElement(numberData) {
	const element = "TODO";

	// todo

	return element;
}

function InitNumbersTab() {
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
			physicalNumberModalBusinessSelect.empty();
			physicalNumberModalBusinessSelect.append($(`<option value="" ${CurrentManagePhysicalNumberData.assignedToBusinessId === "" ? "selected" : ""}>None</option>`));
			CurrentBusinessesList.forEach((businessData) => {
				physicalNumberModalBusinessSelect.append(
					$(`<option value="${businessData.id}" ${CurrentManagePhysicalNumberData.assignedToBusinessId === businessData.id ? "selected" : ""}>${businessData.name}</option>`),
				);
			});

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
			formData.append("changes", changes.changes);

			if (ManagePhysicalNumberType === "edit") {
				formData.append("numberId", CurrentManagePhysicalNumberData.id);
			}

			AddNewUserNumberToAPI(
				formData,
				(responseResult) => {
					if (!responseResult.success) {
						AlertManager.createAlert({
							type: "danger",
							message: "Error occured while saving user number. Check browser console for logs.",
							timeout: 6000,
						});

						console.log("Error occured while saving user number: ", responseResult);
					} else {
						if (ManagePhysicalNumberType === "new") {
							PhysicalSimNumbersList.push(responseResult.data);

							physicalSimNumbersTable.find("tbody").prepend(CreateUserPhysicalNumbersTableElement(responseResult.data));
						} else {
							const exisitingIndex = PhysicalSimNumbersList.findIndex((numberData) => numberData.id === CurrentManagePhysicalNumberData.id);
							PhysicalSimNumbersList[exisitingIndex] = responseResult.data;

							const exisitingUserPhysicalNumbersTableElement = physicalSimNumbersTable.find(`tbody tr[number-id="${CurrentManagePhysicalNumberData.id}"]`);
							exisitingUserPhysicalNumbersTableElement.replaceWith(CreateUserPhysicalNumbersTableElement(responseResult.data));
						}

						AlertManager.createAlert({
							type: "success",
							message: `Successfully ${ManagePhysicalNumberType === "new" ? "added" : "updated"} user physical sim number.`,
							timeout: 6000,
						});

						addNewCustomSimNumberModal.hide();
					}

					addNewPhysicalNumberButton.prop("disabled", false);
					addNewPhysicalNumberButtonSpinner.addClass("d-none");

					IsSavingPhysicalNumber = false;
				},
				(errorResult) => {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occured while saving user number. Check browser console for logs.",
						timeout: 6000,
					});

					console.log("Error occured while saving user number: ", errorResult);

					addNewPhysicalNumberButton.prop("disabled", false);
					addNewPhysicalNumberButtonSpinner.addClass("d-none");

					IsSavingPhysicalNumber = false;
				},
			);
		});

		/** Init **/

		// Physical Numbers
		FetchUserNumbersFromAPI(
			NumberProviderEnum.PHYSICAL,
			0,
			100,
			(userPhysicalNumbers) => {
				PhysicalSimNumbersList = userPhysicalNumbers;

				if (PhysicalSimNumbersList.length === 0) {
					physicalSimNumbersTable.find("tbody").append($('<tr><td colspan="6">No numbers found</td></tr>'));
				} else {
					PhysicalSimNumbersList.forEach((numberData) => {
						physicalSimNumbersTable.find("tbody").append(CreateUserPhysicalNumbersTableElement(numberData));
					});
				}
			},
			(userPhysicalNumbersError) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while fetching user physical numbers. Check browser console for logs.",
					enableDismiss: false,
				});

				console.log("Error occured while fetching user physical numbers: ", userPhysicalNumbersError);
			},
		);

		// Twilio Numbers
		FetchUserNumbersFromAPI(
			NumberProviderEnum.TWILIO,
			0,
			0,
			(userTwilioNumbers) => {
				TwilioNumbersList = userTwilioNumbers;

				if (TwilioNumbersList.length === 0) {
					twilioNumbersTable.find("tbody").append($('<tr><td colspan="5">No numbers found</td></tr>'));
				} else {
					TwilioNumbersList.forEach((numberData) => {
						twilioNumbersTable.find("tbody").append(CreateUserTwilioNumbersTableElement(numberData));
					});
				}
			},
			(userTwilioNumbersError) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while fetching user twilio numbers. Check browser console for logs.",
					enableDismiss: false,
				});

				console.log("Error occured while fetching user twilio numbers: ", userTwilioNumbersError);
			},
		);

		// Vonage Numbers
		FetchUserNumbersFromAPI(
			NumberProviderEnum.VONAGE,
			0,
			0,
			(userVonageNumbers) => {
				VonageNumbersList = userVonageNumbers;

				if (VonageNumbersList.length === 0) {
					vonageNumbersTable.find("tbody").append($('<tr><td colspan="5">No numbers found</td></tr>'));
				} else {
					VonageNumbersList.forEach((numberData) => {
						vonageNumbersTable.find("tbody").append(CreateUserVonageNumbersTableElement(numberData));
					});
				}
			},
			(userVonageNumbersError) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while fetching user vonage numbers. Check browser console for logs.",
					enableDismiss: false,
				});

				console.log("Error occured while fetching user vonage numbers: ", userVonageNumbersError);
			},
		);

		// Telnyx Numbers
		FetchUserNumbersFromAPI(
			NumberProviderEnum.TELNYX,
			0,
			0,
			(userTelenyxNumbers) => {
				TelenyxNumbersList = userTelenyxNumbers;

				if (TelenyxNumbersList.length === 0) {
					telenyxNumbersTable.find("tbody").append($('<tr><td colspan="5">No numbers found</td></tr>'));
				} else {
					TelenyxNumbersList.forEach((numberData) => {
						telenyxNumbersTable.find("tbody").append(CreateUserTelenyxNumbersTableElement(numberData));
					});
				}
			},
			(userTelenyxNumbersError) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while fetching user telenyx numbers. Check browser console for logs.",
					enableDismiss: false,
				});

				console.log("Error occured while fetching user telenyx numbers: ", userTelenyxNumbersError);
			},
		);

		// Init
		FillInitialPhysicalSimModal();
	});
}
