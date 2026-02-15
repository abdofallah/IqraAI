/** Dynamic Variables **/
let CurrentSTTProviderType = null;
let CurrentSTTProviderData = null;

let CurrentSTTProviderModelType = null;
let CurrentSTTProviderModelData = null;

let IsSavingSTTProviderTab = false;

let sttFieldsHelper = null;

/** Elements Variables **/
const STTProviderTab = $("#stt-provider-tab");

// Headers
const sttProviderInnerHeader = STTProviderTab.find("#stt-provider-inner-header");

// Manager Header
const sttProviderManagerInnerHeader = STTProviderTab.find("#stt-provider-manager-inner-header");
const switchBackToSTTProviderListTabFromManageTab = sttProviderManagerInnerHeader.find("#switchBackToSTTProviderListTabFromManageTab");
const currentManageSTTProviderName = sttProviderManagerInnerHeader.find("#currentManageSTTProviderName");
const saveManageSTTProviderButton = sttProviderManagerInnerHeader.find("#saveManageSTTProviderButton");

// Manager Model Header
const sttProviderManagerModelInnerHeader = STTProviderTab.find("#stt-provider-manager-model-inner-header");
const currentManageModelSTTProviderName = sttProviderManagerModelInnerHeader.find("#currentManageModelSTTProviderName");
const currentManageSTTProviderModelName = sttProviderManagerModelInnerHeader.find("#currentManageSTTProviderModelName");
const switchBackToSTTProviderManagerModelsListTabFromModelTab = sttProviderManagerModelInnerHeader.find("#switchBackToSTTProviderManagerModelsListTabFromModelTab");
const saveManageSTTProviderModelButton = sttProviderManagerModelInnerHeader.find("#saveManageSTTProviderModelButton");

// List Tab
const STTProviderListTableTab = STTProviderTab.find("#sttProviderListTableTab");
const STTProviderListTable = STTProviderListTableTab.find("#sttProviderListTable");

// Manager Tab
const STTProviderManageTab = STTProviderTab.find("#sttProviderManageTab");
const sttProviderModelListTable = STTProviderManageTab.find("#sttProviderModelListTable");
const addNewSTTProviderModelButton = STTProviderManageTab.find("#addNewSTTProviderModelButton");

const sttProviderManagerGeneral = STTProviderManageTab.find("#stt-provider-manager-general");

const manageSTTProviderIdInput = sttProviderManagerGeneral.find("#manageSTTProviderIdInput");
const manageSTTProviderDisabledInput = sttProviderManagerGeneral.find("#manageSTTProviderDisabledInput");
const manageSTTProviderIntegrationSelect = sttProviderManagerGeneral.find("#manageSTTProviderIntegrationSelect");

const sttProviderManagerModelsListTab = STTProviderManageTab.find("#sttProviderManagerModelsListTab");
const sttProviderManagerModelManageTab = STTProviderManageTab.find("#sttProviderManagerModelManageTab");

const manageSTTProviderModelIdInput = sttProviderManagerModelManageTab.find("#manageSTTProviderModelIdInput");
const manageSTTProviderModelNameInput = sttProviderManagerModelManageTab.find("#manageSTTProviderModelNameInput");
const manageSTTProviderModelPriceInput = sttProviderManagerModelManageTab.find("#manageSTTProviderModelPriceInput");
const manageSTTProviderModelPriceUnitInput = sttProviderManagerModelManageTab.find("#manageSTTProviderModelPriceUnitInput");
const manageSTTProviderModelLanguagesContainer = sttProviderManagerModelManageTab.find("#manageSTTProviderModelLanguagesContainer");
const manageSTTProviderModelDisabledInput = sttProviderManagerModelManageTab.find("#manageSTTProviderModelDisabledInput");

// Integration Elements
const sttProviderIntegrationsTab = $("#stt-provider-manager-integrations");
const addNewSTTProviderIntegrationFieldButton = sttProviderIntegrationsTab.find("#addNewSTTProviderIntegrationFieldButton");
const sttProviderIntegrationFieldsList = sttProviderIntegrationsTab.find("#sttProviderIntegrationFieldsList");
const searchSTTProviderIntegrationFieldInput = sttProviderIntegrationsTab.find("input[aria-label='Search Field']");
const searchSTTProviderIntegrationFieldButton = sttProviderIntegrationsTab.find("#searchSTTProviderIntegrationFieldButton");

/** API Functions **/
function SaveSTTProviderData(formData, successCallback, errorCallback) {
	$.ajax({
		url: "/app/admin/sttproviders/save",
		type: "POST",
		data: formData,
		processData: false,
		contentType: false,
		success: function (response) {
			if (response.success) {
				successCallback(response);
			} else {
				errorCallback(response, true);
			}
		},
		error: function (xhr, status, error) {
			errorCallback(error, false);
		}
	});
}

function SaveSTTProviderModelData(formData, successCallback, errorCallback) {
	$.ajax({
		url: "/app/admin/sttproviders/model/save",
		type: "POST",
		data: formData,
		processData: false,
		contentType: false,
		success: function (response) {
			if (response.success) {
				successCallback(response);
			} else {
				errorCallback(response, true);
			}
		},
		error: function (xhr, status, error) {
			errorCallback(error, false);
		}
	});
}

function FetchSTTProvidersFromAPI(page, pageSize, successCallback, errorCallback) {
	$.ajax({
		url: "/app/admin/sttproviders",
		type: "POST",
		data: {
			page: page,
			pageSize: pageSize
		},
		success: function (response) {
			if (response.success) {
				successCallback(response.data);
			} else {
				errorCallback(response, true);
			}
		},
		error: function (xhr, status, error) {
			errorCallback(error, false);
		}
	});
}

/** Core Functions **/

// Provider List Functions
function CreateSTTProviderListTableElement(sttProviderData) {
	let disabledData = "";
	if (sttProviderData.disabledAt == null) {
		disabledData = "-";
	} else {
		disabledData = `<span class="badge bg-danger">${sttProviderData.disabledAt}</span>`;
	}

	const element = $(`
        <tr>
            <td>${sttProviderData.id.value}</td>
            <td>${sttProviderData.id.name}</td>
            <td>${disabledData}</td>
            <td>${sttProviderData.models.length}</td>
            <td>
                <button class="btn btn-info btn-sm" provider-id="${sttProviderData.id.value}" button-type="edit-stt-provider">
                    <i class="fa-regular fa-eye"></i>
                </button>
            </td>
        </tr>
    `);

	return element;
}

// Provider Management Functions
function ShowSTTProviderManageTab() {
	STTProviderListTableTab.removeClass("show");
	sttProviderInnerHeader.removeClass("show");

	setTimeout(() => {
		STTProviderListTableTab.addClass("d-none");
		sttProviderInnerHeader.addClass("d-none");

		STTProviderManageTab.removeClass("d-none");
		sttProviderManagerInnerHeader.removeClass("d-none");

		setTimeout(() => {
			STTProviderManageTab.addClass("show");
			sttProviderManagerInnerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowSTTProviderListTab() {
	STTProviderManageTab.removeClass("show");
	sttProviderManagerInnerHeader.removeClass("show");

	setTimeout(() => {
		STTProviderManageTab.addClass("d-none");
		sttProviderManagerInnerHeader.addClass("d-none");

		STTProviderListTableTab.removeClass("d-none");
		sttProviderInnerHeader.removeClass("d-none");

		setTimeout(() => {
			STTProviderListTableTab.addClass("show");
			sttProviderInnerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ResetAndEmptySTTProvidersManageTab() {
	manageSTTProviderIdInput.val("");
	manageSTTProviderDisabledInput.prop("checked", false).change();
	sttProviderModelListTable.find("tbody").empty();
	manageSTTProviderIntegrationSelect.val("").change();

	// Reset integration fields
	sttProviderIntegrationFieldsList.empty();
	sttFieldsHelper = null;
}

function FillSTTProviderManageTab(sttProviderData) {
	manageSTTProviderIdInput.val(sttProviderData.id.name);
	manageSTTProviderDisabledInput.prop("checked", sttProviderData.disabledAt != null);

	// Fill integration select
	fillSTTProviderIntegrationSelect();

	// Fill models table
	if (sttProviderData.models.length !== 0) {
		sttProviderData.models.forEach((modelData) => {
			sttProviderModelListTable.find("tbody").append(CreateSTTProviderModelListTableElement(modelData));
		});
	} else {
		sttProviderModelListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="5">No models</td></tr>');
	}

	// Initialize the Field Helper Class
	sttFieldsHelper = new ProviderIntegrationsFieldHelper(
		sttProviderIntegrationFieldsList,
		addNewSTTProviderIntegrationFieldButton,
		sttProviderData.userIntegrationFields,
		() => {
			// Callback when fields change
			CheckSTTProviderManageTabHasChanges(true);
		}
	);
	sttFieldsHelper.render();
}

function CheckSTTProviderManageTabHasChanges(enableDisableButton = true) {
	let changes = {};
	let hasChanges = false;

	// Check disabled state
	changes.disabled = manageSTTProviderDisabledInput.prop("checked");
	if (changes.disabled === (CurrentSTTProviderData.disabledAt == null)) {
		hasChanges = true;
	}

	// Check integration selection
	changes.integrationId = manageSTTProviderIntegrationSelect.val();
	if (changes.integrationId !== CurrentSTTProviderData.integrationId) {
		hasChanges = true;
	}

	// Check integration fields using Helper
	changes.userIntegrationFields = sttFieldsHelper.getData();
	if (sttFieldsHelper.hasChanges()) {
		hasChanges = true;		
	}

	if (enableDisableButton) {
		saveManageSTTProviderButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function ValidateSTTProviderManageTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// General Tab
	const selectedIntegration = manageSTTProviderIntegrationSelect.val();
	if (!selectedIntegration) {
		validated = false;
		errors.push("Integration selection is required");
		if (!onlyRemove) {
			manageSTTProviderIntegrationSelect.addClass("is-invalid");
		}
	} else {
		manageSTTProviderIntegrationSelect.removeClass("is-invalid");
	}

	// Integration Tab
	const fieldValidation = sttFieldsHelper.validate(onlyRemove);
	if (!fieldValidation.validated) {
		validated = false;
		errors.push(...fieldValidation.errors);
	}

	return {
		validated: validated,
		errors: errors,
	};
}

/** Model Management Functions **/
function CreateSTTProviderModelListTableElement(modelData) {
	let disabledData = "";
	if (modelData.disabledAt == null) {
		disabledData = "-";
	} else {
		disabledData = `<span class="badge bg-danger">${modelData.disabledAt}</span>`;
	}

	const languagesCount = modelData.supportedLanguages ? modelData.supportedLanguages.length : 0;

	const element = $(`<tr model-id="${modelData.id}">
        <td>${modelData.id}</td>
        <td>${modelData.name}</td>
        <td>${disabledData}</td>
        <td>${languagesCount} languages</td>
        <td>
            <button class="btn btn-info btn-sm" model-id="${modelData.id}" button-type="edit-stt-provider-model">
                <i class="fa-regular fa-eye"></i>
            </button>
        </td>
    </tr>`);

	return element;
}

function ShowSTTProviderModelManageTab() {
	sttProviderManagerModelsListTab.removeClass("show");
	sttProviderManagerInnerHeader.removeClass("show");

	setTimeout(() => {
		sttProviderManagerModelsListTab.addClass("d-none");
		sttProviderManagerInnerHeader.addClass("d-none");

		sttProviderManagerModelManageTab.removeClass("d-none");
		sttProviderManagerModelInnerHeader.removeClass("d-none");

		setTimeout(() => {
			sttProviderManagerModelManageTab.addClass("show");
			sttProviderManagerModelInnerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowSTTProviderModelListTab() {
	sttProviderManagerModelManageTab.removeClass("show");
	sttProviderManagerModelInnerHeader.removeClass("show");

	setTimeout(() => {
		sttProviderManagerModelManageTab.addClass("d-none");
		sttProviderManagerModelInnerHeader.addClass("d-none");

		sttProviderManagerModelsListTab.removeClass("d-none");
		sttProviderManagerInnerHeader.removeClass("d-none");

		setTimeout(() => {
			sttProviderManagerModelsListTab.addClass("show");
			sttProviderManagerInnerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function CreateDefaultSTTProviderModelObject() {
	return {
		id: "",
		name: "",
		disabledAt: null,
		pricePerUnit: "",
		priceUnit: "",
		supportedLanguages: [],
	};
}

function GenerateLanguageCheckboxes() {
	manageSTTProviderModelLanguagesContainer.empty();

	CurrentLanguagesList.forEach((language) => {
		const isChecked = CurrentSTTProviderModelData?.supportedLanguages?.includes(language.id) ? "checked" : "";
		const checkbox = $(`
            <div class="form-check">
                <input class="form-check-input language-checkbox" type="checkbox" 
                       value="${language.id}" id="lang-${language.id}" ${isChecked}>
                <label class="form-check-label" for="lang-${language.id}">
                    ${language.name} (${language.id})
                </label>
            </div>
        `);
		manageSTTProviderModelLanguagesContainer.append(checkbox);
	});
}

function FillSTTProviderModelManageTab(modelData) {
	manageSTTProviderModelIdInput.val(modelData.id);
	manageSTTProviderModelNameInput.val(modelData.name);
	manageSTTProviderModelPriceInput.val(modelData.pricePerUnit);
	manageSTTProviderModelPriceUnitInput.val(modelData.priceUnit);
	manageSTTProviderModelDisabledInput.prop("checked", modelData.disabledAt != null);

	GenerateLanguageCheckboxes();
}

function ResetAndEmptySTTProviderModelManageTab() {
	sttProviderManagerModelManageTab.find("input[type='text'], input[type='number']").val("").change();
	manageSTTProviderModelPriceUnitInput.val("").change();
	sttProviderManagerModelManageTab.find(".is-invalid").removeClass("is-invalid");
	manageSTTProviderModelDisabledInput.prop("checked", false);
	manageSTTProviderModelLanguagesContainer.empty();

	saveManageSTTProviderModelButton.prop("disabled", true);
}

function ValidateSTTProviderModelManageTabFields(onlyRemove = false) {
	const errors = [];
	let validated = true;

	const modelId = manageSTTProviderModelIdInput.val();
	if (!modelId || modelId.trim().length === 0) {
		validated = false;
		errors.push("Model ID is required");
		if (!onlyRemove) manageSTTProviderModelIdInput.addClass("is-invalid");
	} else {
		manageSTTProviderModelIdInput.removeClass("is-invalid");
	}

	const modelName = manageSTTProviderModelNameInput.val();
	if (!modelName || modelName.trim().length === 0) {
		validated = false;
		errors.push("Model name is required");
		if (!onlyRemove) manageSTTProviderModelNameInput.addClass("is-invalid");
	} else {
		manageSTTProviderModelNameInput.removeClass("is-invalid");
	}

	const price = manageSTTProviderModelPriceInput.val();
	if (!price || isNaN(price) || parseFloat(price) <= 0) {
		validated = false;
		errors.push("Valid price is required");
		if (!onlyRemove) manageSTTProviderModelPriceInput.addClass("is-invalid");
	} else {
		manageSTTProviderModelPriceInput.removeClass("is-invalid");
	}

	const selectedLanguages = manageSTTProviderModelLanguagesContainer.find('input[type="checkbox"]:checked').length;
	if (selectedLanguages === 0) {
		validated = false;
		errors.push("At least one language must be selected");
		if (!onlyRemove) manageSTTProviderModelLanguagesContainer.addClass("is-invalid");
	} else {
		manageSTTProviderModelLanguagesContainer.removeClass("is-invalid");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

function CheckSTTProviderModelManageTabHasChanges(enableDisableButton = true) {
	function arraysEqual(arr1, arr2) {
		if (!arr1 || !arr2) return false;
		if (arr1.length !== arr2.length) return false;

		const sortedArr1 = [...arr1].sort();
		const sortedArr2 = [...arr2].sort();

		return sortedArr1.every((value, index) => value === sortedArr2[index]);
	}

	const changes = {};
	let hasChanges = false;

	changes.id = manageSTTProviderModelIdInput.val();
	if (CurrentSTTProviderModelData.id !== changes.id) {
		hasChanges = true;
	}

	changes.name = manageSTTProviderModelNameInput.val();
	if (CurrentSTTProviderModelData.name !== changes.name) {
		hasChanges = true;
	}

	changes.disabled = manageSTTProviderModelDisabledInput.prop("checked");
	if (changes.disabled === (CurrentSTTProviderModelData.disabledAt === null)) {
		hasChanges = true;
	}

	changes.pricePerUnit = manageSTTProviderModelPriceInput.val();
	if (CurrentSTTProviderModelData.pricePerUnit !== changes.pricePerUnit) {
		hasChanges = true;
	}

	changes.priceUnit = manageSTTProviderModelPriceUnitInput.val().trim();
	if (CurrentSTTProviderModelData.priceUnit !== changes.priceUnit) {
		hasChanges = true;
	}

	changes.supportedLanguages = [];
	manageSTTProviderModelLanguagesContainer.find('input[type="checkbox"]:checked').each(function () {
		changes.supportedLanguages.push($(this).val());
	});
	if (!arraysEqual(CurrentSTTProviderModelData.supportedLanguages, changes.supportedLanguages)) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveManageSTTProviderModelButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

/** Integration Functions **/
function fillSTTProviderIntegrationSelect() {
	manageSTTProviderIntegrationSelect.empty();
	manageSTTProviderIntegrationSelect.append('<option value="" disabled selected>Select Integration</option>');

	// Filter available integrations that have STT in their type
	const sttIntegrations = CurrentIntegrationsList.filter((integration) => integration.type.includes("STT") || integration.type.includes("SPEECH2TEXT"));

	sttIntegrations.forEach((integration) => {
		manageSTTProviderIntegrationSelect.append(`
            <option value="${integration.id}" 
                ${CurrentSTTProviderData.integrationId === integration.id ? "selected" : ""}>
                ${integration.name}
            </option>
        `);
	});
}



/** Event Handlers **/
$(document).ready(() => {
	// Provider List Events
	STTProviderListTable.on("click", "button[button-type=edit-stt-provider]", (event) => {
		event.preventDefault();

		const providerId = parseInt($(event.currentTarget).attr("provider-id"));
		CurrentSTTProviderData = CurrentSTTProvidersList.find((provider) => provider.id.value === providerId);

		currentManageSTTProviderName.text(CurrentSTTProviderData.id.name);

		ResetAndEmptySTTProvidersManageTab();
		FillSTTProviderManageTab(CurrentSTTProviderData);

		CurrentSTTProviderType = "edit";
		ShowSTTProviderManageTab();
	});

	// Provider Management Events
	switchBackToSTTProviderListTabFromManageTab.on("click", (event) => {
		event.preventDefault();
		CurrentSTTProviderType = null;
		ShowSTTProviderListTab();
	});

	sttProviderManagerGeneral.on("input change", "input, select", () => {
		if (CurrentSTTProviderType === null) return;
		CheckSTTProviderManageTabHasChanges(true);
	});

	// Model Management Events
	addNewSTTProviderModelButton.on("click", (event) => {
		event.preventDefault();

		CurrentSTTProviderModelData = CreateDefaultSTTProviderModelObject();

		currentManageModelSTTProviderName.text(CurrentSTTProviderData.id.name);
		currentManageSTTProviderModelName.text("New Model");

		ResetAndEmptySTTProviderModelManageTab();
		FillSTTProviderModelManageTab(CurrentSTTProviderModelData);

		CurrentSTTProviderModelType = "new";
		ShowSTTProviderModelManageTab();
	});

	sttProviderModelListTable.on("click", "button[button-type=edit-stt-provider-model]", (event) => {
		event.preventDefault();

		const modelId = $(event.currentTarget).attr("model-id");
		CurrentSTTProviderModelData = CurrentSTTProviderData.models.find((model) => model.id === modelId);

		currentManageModelSTTProviderName.text(CurrentSTTProviderData.id.name);
		currentManageSTTProviderModelName.text(CurrentSTTProviderModelData.name);

		ResetAndEmptySTTProviderModelManageTab();
		FillSTTProviderModelManageTab(CurrentSTTProviderModelData);

		CurrentSTTProviderModelType = "edit";
		ShowSTTProviderModelManageTab();
	});

	switchBackToSTTProviderManagerModelsListTabFromModelTab.on("click", (event) => {
		event.preventDefault();
		CurrentSTTProviderModelType = null;
		ShowSTTProviderModelListTab();
	});

	sttProviderManagerModelManageTab.on("input change", "input, select", () => {
		if (CurrentSTTProviderModelType === null) return;
		CheckSTTProviderModelManageTabHasChanges(true);
	});

	// Save button handler
	saveManageSTTProviderButton.on("click", (event) => {
		event.preventDefault();
		if (IsSavingSTTProviderTab) return;

		const validationResult = ValidateSTTProviderManageTab(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
				timeout: 6000,
			});
			return;
		}

		const changes = CheckSTTProviderManageTabHasChanges(false);
		if (!changes.hasChanges) return;

		IsSavingSTTProviderTab = true;
		saveManageSTTProviderButton.prop("disabled", true);

		const formData = new FormData();
		formData.append("changes", JSON.stringify(changes.changes));
		formData.append("providerId", CurrentSTTProviderData.id.value);

		SaveSTTProviderData(
			formData,
			(saveResponse) => {
				if (saveResponse.success) {
					CurrentSTTProviderData = saveResponse.data;

					const providerIndex = CurrentSTTProvidersList.findIndex((p) => p.id.value === CurrentSTTProviderData.id.value);
					if (providerIndex !== -1) {
						CurrentSTTProvidersList[providerIndex] = CurrentSTTProviderData;
					}

					STTProviderListTable.find(`tr button[provider-id="${CurrentSTTProviderData.id.value}"]`)
						.closest("tr")
						.replaceWith($(CreateSTTProviderListTableElement(CurrentSTTProviderData)));

					AlertManager.createAlert({
						type: "success",
						message: "STT provider data saved successfully.",
						timeout: 6000,
					});

					CheckSTTProviderManageTabHasChanges();
				} else {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving STT provider data.",
						timeout: 6000,
					});
				}

				saveManageSTTProviderButton.prop("disabled", false);
				IsSavingSTTProviderTab = false;
			},
			(error, isUnsuccessful) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occurred while saving STT provider data.",
					timeout: 6000,
				});
				console.error("Save error:", error);

				saveManageSTTProviderButton.prop("disabled", false);
				IsSavingSTTProviderTab = false;
			},
		);
	});

	// Save Model Button Handler
	saveManageSTTProviderModelButton.on("click", (event) => {
		event.preventDefault();
		if (IsSavingSTTProviderTab) return;

		const validationResult = ValidateSTTProviderModelManageTabFields(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
				timeout: 6000,
			});
			return;
		}

		const changes = CheckSTTProviderModelManageTabHasChanges(false);
		if (!changes.hasChanges) return;

		IsSavingSTTProviderTab = true;
		saveManageSTTProviderModelButton.prop("disabled", true);

		const formData = new FormData();
		formData.append("providerId", CurrentSTTProviderData.id.value);
		formData.append("modelId", changes.changes.id);
		formData.append("postType", CurrentSTTProviderModelType);
		formData.append("changes", JSON.stringify(changes.changes));

		SaveSTTProviderModelData(
			formData,
			(saveResponse) => {
				if (saveResponse.success) {
					// Update the current model data
					CurrentSTTProviderModelData = saveResponse.data;

					// Update the models list in the provider data
					const modelIndex = CurrentSTTProviderData.models.findIndex((m) => m.id === CurrentSTTProviderModelData.id);

					if (modelIndex !== -1) {
						CurrentSTTProviderData.models[modelIndex] = CurrentSTTProviderModelData;
					} else {
						CurrentSTTProviderData.models.push(CurrentSTTProviderModelData);
					}

					// Update the models table
					const modelRow = sttProviderModelListTable.find(`tr button[model-id="${CurrentSTTProviderModelData.id}"]`).closest("tr");

					if (modelRow.length) {
						modelRow.replaceWith($(CreateSTTProviderModelListTableElement(CurrentSTTProviderModelData)));
					} else {
						sttProviderModelListTable.find("tbody tr[tr-type='none-notice']").remove();
						sttProviderModelListTable.find("tbody").append($(CreateSTTProviderModelListTableElement(CurrentSTTProviderModelData)));
					}

					AlertManager.createAlert({
						type: "success",
						message: "STT provider model saved successfully.",
						timeout: 6000,
					});

					ShowSTTProviderModelListTab();
				} else {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving STT provider model.",
						timeout: 6000,
					});
				}

				saveManageSTTProviderModelButton.prop("disabled", false);
				IsSavingSTTProviderTab = false;
			},
			(error, isUnsuccessful) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occurred while saving STT provider model.",
					timeout: 6000,
				});
				console.error("Save error:", error);

				saveManageSTTProviderModelButton.prop("disabled", false);
				IsSavingSTTProviderTab = false;
			},
		);
	});

	// Initialize
	FetchSTTProvidersFromAPI(
		0,
		100,
		(sttProvidersData) => {
			CurrentSTTProvidersList = sttProvidersData;
			CurrentSTTProvidersList.forEach((providerData) => {
				STTProviderListTable.find("tbody").append(CreateSTTProviderListTableElement(providerData));
			});
		},
		(error, isUnsuccessful) => {
			AlertManager.createAlert({
				type: "danger",
				message: "Error occurred while fetching STT providers. Check browser console for logs.",
				timeout: 5000,
			});
			console.error("Error fetching STT providers:", error);
		},
	);
});
