/** Dynamic Variables **/
let CurrentTTSProviderType = null;
let CurrentTTSProviderData = null;

let CurrentTTSProviderModelType = null;
let CurrentTTSProviderModelData = null;

let IsSavingTTSProviderTab = false;

let ttsFieldsHelper = null;

/** Elements Variables **/
const TTSProviderTab = $("#tts-provider-tab");

// Headers

// List Header
const ttsProviderListHeader = TTSProviderTab.find("#tts-provider-list-inner-header");

// Manager Header
const ttsProviderManagerHeader = TTSProviderTab.find("#tts-provider-manager-inner-header");
const switchBackToTTSProviderListTabFromManageTab = ttsProviderManagerHeader.find("#switchBackToTTSProviderListTabFromManageTab");
const currentManageTTSProviderName = ttsProviderManagerHeader.find("#currentManageTTSProviderName");
const saveManageTTSProviderButton = ttsProviderManagerHeader.find("#saveManageTTSProviderButton");

// Manager Model Header
const ttsProviderManagerModelHeader = TTSProviderTab.find("#tts-provider-manager-model-inner-header");
const currentManageModelTTSProviderName = ttsProviderManagerModelHeader.find("#currentManageModelTTSProviderName");
const currentManageTTSProviderModelName = ttsProviderManagerModelHeader.find("#currentManageTTSProviderModelName");
const switchBackToTTSProviderManagerModelsListTabFromModelTab = ttsProviderManagerModelHeader.find("#switchBackToTTSProviderManagerModelsListTabFromModelTab");
const saveManageTTSProviderModelButton = ttsProviderManagerModelHeader.find("#saveManageTTSProviderModelButton");

// Provider List Elements
const TTSProviderListTableTab = TTSProviderTab.find("#ttsProviderListTableTab");
const TTSProviderListTable = TTSProviderListTableTab.find("#ttsProviderListTable");
const searchTTSProviderInput = TTSProviderListTableTab.find("input[aria-label='TTS Provider Name or Id']");
const searchTTSProviderButton = TTSProviderListTableTab.find("#searchTTSProviderButton");

// Provider Manage Elements
const TTSProviderManageTab = TTSProviderTab.find("#ttsProviderManageTab");

// Provider General Tab Elements
const manageTTSProviderIdInput = TTSProviderManageTab.find("#manageTTSProviderIdInput");
const manageTTSProviderIntegrationSelect = TTSProviderManageTab.find("#manageTTSProviderIntegrationSelect");
const manageTTSProviderDisabledInput = TTSProviderManageTab.find("#manageTTSProviderDisabledInput");

// Model Management Elements
const ttsProviderManagerModelsListTab = TTSProviderManageTab.find("#ttsProviderManagerModelsListTab");
const ttsProviderModelListTable = ttsProviderManagerModelsListTab.find("#ttsProviderModelListTable");
const addNewTTSProviderModelButton = ttsProviderManagerModelsListTab.find("#addNewTTSProviderModelButton");
const searchTTSProviderModelInput = ttsProviderManagerModelsListTab.find("input[aria-label='Model Name or Id']");
const searchTTSProviderModelButton = ttsProviderManagerModelsListTab.find("#searchTTSProviderModelButton");

// Model Manage Elements
const ttsProviderManagerModelManageTab = TTSProviderManageTab.find("#ttsProviderManagerModelManageTab");
const manageTTSProviderModelIdInput = ttsProviderManagerModelManageTab.find("#manageTTSProviderModelIdInput");
const manageTTSProviderModelNameInput = ttsProviderManagerModelManageTab.find("#manageTTSProviderModelNameInput");
const manageTTSProviderModelPriceInput = ttsProviderManagerModelManageTab.find("#manageTTSProviderModelPriceInput");
const manageTTSProviderModelPriceUnitInput = ttsProviderManagerModelManageTab.find("#manageTTSProviderModelPriceUnitInput");
const manageTTSProviderModelMultilingualInput = ttsProviderManagerModelManageTab.find("#manageTTSProviderModelMultilingualInput");
const manageTTSProviderModelLanguagesContainer = ttsProviderManagerModelManageTab.find("#manageTTSProviderModelLanguagesContainer");
const addNewSpeakingStyleButton = ttsProviderManagerModelManageTab.find("#addNewSpeakingStyleButton");
const manageTTSProviderModelDisabledInput = ttsProviderManagerModelManageTab.find("#manageTTSProviderModelDisabledInput");

// Integration Fields Elements
const ttsProviderIntegrationsTab = $("#tts-provider-manager-integrations");
const addNewTTSProviderIntegrationFieldButton = ttsProviderIntegrationsTab.find("#addNewTTSProviderIntegrationFieldButton");
const ttsProviderIntegrationFieldsList = ttsProviderIntegrationsTab.find("#ttsProviderIntegrationFieldsList");
const searchTTSProviderIntegrationFieldInput = ttsProviderIntegrationsTab.find("input[aria-label='Search Field']");
const searchTTSProviderIntegrationFieldButton = ttsProviderIntegrationsTab.find("#searchTTSProviderIntegrationFieldButton");

/** API Functions **/
function SaveTTSProviderData(formData, successCallback, errorCallback) {
	$.ajax({
		url: "/app/admin/ttsproviders/save",
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

function SaveTTSProviderModelData(formData, successCallback, errorCallback) {
	$.ajax({
		url: "/app/admin/ttsproviders/model/save",
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

function FetchTTSProvidersFromAPI(page, pageSize, successCallback, errorCallback) {
	$.ajax({
		url: "/app/admin/ttsproviders",
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

/** Provider List Functions **/
function CreateTTSProviderListTableElement(ttsProviderData) {
	let disabledData = "";
	if (ttsProviderData.disabledAt == null) {
		disabledData = "-";
	} else {
		disabledData = `<span class="badge bg-danger">${ttsProviderData.disabledAt}</span>`;
	}

	let element = $(`
        <tr>
            <td>${ttsProviderData.id.value}</td>
            <td>${ttsProviderData.id.name}</td>
            <td>${disabledData}</td>
            <td>${ttsProviderData.models.length}</td>
            <td>
                <button class="btn btn-info btn-sm" provider-id="${ttsProviderData.id.value}" button-type="edit-tts-provider">
                    <i class="fa-regular fa-eye"></i>
                </button>
            </td>
        </tr>
    `);

	return element;
}

function ShowTTSProviderManageTab() {
	TTSProviderListTableTab.removeClass("show");
	ttsProviderListHeader.removeClass("show");

	setTimeout(() => {
		TTSProviderListTableTab.addClass("d-none");
		ttsProviderListHeader.addClass("d-none");

		TTSProviderManageTab.removeClass("d-none");
		ttsProviderManagerHeader.removeClass("d-none");

		setTimeout(() => {
			TTSProviderManageTab.addClass("show");
			ttsProviderManagerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowTTSProviderListTab() {
	TTSProviderManageTab.removeClass("show");
	ttsProviderManagerHeader.removeClass("show");

	setTimeout(() => {
		TTSProviderManageTab.addClass("d-none");
		ttsProviderManagerHeader.addClass("d-none");

		TTSProviderListTableTab.removeClass("d-none");
		ttsProviderListHeader.removeClass("d-none");

		setTimeout(() => {
			TTSProviderListTableTab.addClass("show");
			ttsProviderListHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

/** Provider Management Functions **/
function FillTTSProviderManageTab(providerData) {
	manageTTSProviderIdInput.val(providerData.id.name);
	manageTTSProviderDisabledInput.prop("checked", providerData.disabledAt != null);

	// Fill integration select
	fillTTSProviderIntegrationSelect();

	// Fill models table
	if (providerData.models.length != 0) {
		providerData.models.forEach((modelData) => {
			ttsProviderModelListTable.find("tbody").append(CreateTTSProviderModelListTableElement(modelData));
		});
	} else {
		ttsProviderModelListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="8">No models</td></tr>');
	}

	// Initialize Helper
	ttsFieldsHelper = new ProviderIntegrationsFieldHelper(
		ttsProviderIntegrationFieldsList,
		addNewTTSProviderIntegrationFieldButton,
		providerData.userIntegrationFields,
		() => {
			CheckTTSProviderManageTabHasChanges(true);
		}
	);
	ttsFieldsHelper.render();
}

function ResetAndEmptyTTSProvidersManageTab() {
	manageTTSProviderIdInput.val("");
	manageTTSProviderDisabledInput.prop("checked", false).change();
	ttsProviderModelListTable.find("tbody").empty();
	manageTTSProviderIntegrationSelect.val("").change();

	// Reset integration fields
	ttsProviderIntegrationFieldsList.empty();
	ttsFieldsHelper = null;

	// Reset any validation states
	TTSProviderManageTab.find(".is-invalid").removeClass("is-invalid");
}

function CheckTTSProviderManageTabHasChanges(enableDisableButton = true) {
	let changes = {};
	let hasChanges = false;

	// Check disabled state
	changes.disabled = manageTTSProviderDisabledInput.prop("checked");
	if (changes.disabled === (CurrentTTSProviderData.disabledAt == null)) {
		hasChanges = true;
	}

	// Check integration selection
	changes.integrationId = manageTTSProviderIntegrationSelect.val();
	if (changes.integrationId !== CurrentTTSProviderData.integrationId) {
		hasChanges = true;
	}

	// Compare integration fields
	changes.userIntegrationFields = ttsFieldsHelper.getData();
	if (ttsFieldsHelper.hasChanges()) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveManageTTSProviderButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function ValidateTTSProviderManageTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// General Tab
	const selectedIntegration = manageTTSProviderIntegrationSelect.val();
	if (!selectedIntegration) {
		validated = false;
		errors.push("Integration selection is required");
		if (!onlyRemove) {
			manageTTSProviderIntegrationSelect.addClass("is-invalid");
		}
	} else {
		manageTTSProviderIntegrationSelect.removeClass("is-invalid");
	}

	// Integration Tab
	const fieldValidation = ttsFieldsHelper.validate(onlyRemove);
	if (!fieldValidation.validated) {
		validated = false;
		errors.push(...fieldValidation.errors);
	}

	return {
		validated: validated,
		errors: errors,
	};
}

/** Integration Functions **/
function fillTTSProviderIntegrationSelect() {
	manageTTSProviderIntegrationSelect.empty();
	manageTTSProviderIntegrationSelect.append('<option value="">Select Integration</option>');

	// Filter available integrations that have TTS in their type
	const ttsIntegrations = CurrentIntegrationsList.filter((integration) => integration.type.includes("TTS") || integration.type.includes("TEXT2SPEECH"));

	ttsIntegrations.forEach((integration) => {
		manageTTSProviderIntegrationSelect.append(`
            <option value="${integration.id}" 
                ${CurrentTTSProviderData.integrationId === integration.id ? "selected" : ""}>
                ${integration.name}
            </option>
        `);
	});
}

/** Model Management Functions **/
function CreateTTSProviderModelListTableElement(modelData) {
	let disabledData = modelData.disabledAt ? `<span class="badge bg-danger">${modelData.disabledAt}</span>` : "-";

	let languagesCount = modelData.supportedLanguages ? `${modelData.supportedLanguages.length} ${modelData.isMultilingual ? "(Multilingual)" : ""}` : "0";

	let element = $(`
        <tr>
            <td>${modelData.id}</td>
            <td>${modelData.name}</td>
            <td>${languagesCount}</td>
            <td>${disabledData}</td>
            <td>
                <button class="btn btn-info btn-sm" model-id="${modelData.id}" button-type="edit-tts-provider-model">
                    <i class="fa-regular fa-eye"></i>
                </button>
            </td>
        </tr>
    `);

	return element;
}

function CreateDefaultTTSProviderModelObject() {
	return {
		id: "",
		name: "",
		disabledAt: null,
		pricePerUnit: "",
		priceUnit: "",
		gender: "",
		ageGroup: "",
		personality: [],
		supportedLanguages: [],
		isMultilingual: false,
		speakingStyles: [],
	};
}

function ShowTTSProviderModelManageTab() {
	ttsProviderManagerModelsListTab.removeClass("show");
	ttsProviderManagerHeader.removeClass("show");

	setTimeout(() => {
		ttsProviderManagerModelsListTab.addClass("d-none");
		ttsProviderManagerHeader.addClass("d-none");

		ttsProviderManagerModelManageTab.removeClass("d-none");
		ttsProviderManagerModelHeader.removeClass("d-none");

		setTimeout(() => {
			ttsProviderManagerModelManageTab.addClass("show");
			ttsProviderManagerModelHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowTTSProviderModelListTab() {
	ttsProviderManagerModelManageTab.removeClass("show");
	ttsProviderManagerModelHeader.removeClass("show");

	setTimeout(() => {
		ttsProviderManagerModelManageTab.addClass("d-none");
		ttsProviderManagerModelHeader.addClass("d-none");

		ttsProviderManagerModelsListTab.removeClass("d-none");
		ttsProviderManagerHeader.removeClass("d-none");

		setTimeout(() => {
			ttsProviderManagerModelsListTab.addClass("show");
			ttsProviderManagerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function FillTTSProviderModelManageTab(modelData) {
	function GenerateLanguageCheckboxes(selectedLanguages = []) {
		manageTTSProviderModelLanguagesContainer.empty();

		CurrentLanguagesList.forEach((language) => {
			const isChecked = selectedLanguages.includes(language.id);
			const checkbox = $(`
				<div class="form-check">
					<input class="form-check-input language-checkbox" type="checkbox" 
						value="${language.id}" id="lang-${language.id}" 
						${isChecked ? "checked" : ""}>
					<label class="form-check-label" for="lang-${language.id}">
						${language.name} (${language.id})
					</label>
				</div>
			`);

			manageTTSProviderModelLanguagesContainer.append(checkbox);
		});
	}

	manageTTSProviderModelIdInput.val(modelData.id);
	manageTTSProviderModelNameInput.val(modelData.name);
	manageTTSProviderModelPriceInput.val(modelData.pricePerUnit);
	manageTTSProviderModelPriceUnitInput.val(modelData.priceUnit);
	manageTTSProviderModelDisabledInput.prop("checked", modelData.disabledAt != null);

	// Generate language checkboxes
	GenerateLanguageCheckboxes(modelData.supportedLanguages || []);
}

function ResetAndEmptyTTSProviderModelManageTab() {
	manageTTSProviderModelIdInput.val("").removeClass("is-invalid");
	manageTTSProviderModelNameInput.val("").removeClass("is-invalid");
	manageTTSProviderModelPriceInput.val("").removeClass("is-invalid");
	manageTTSProviderModelPriceUnitInput.val("").removeClass("is-invalid");
	manageTTSProviderModelMultilingualInput.prop("checked", false);
	manageTTSProviderModelDisabledInput.prop("checked", false);

	manageTTSProviderModelLanguagesContainer.empty();

	saveManageTTSProviderModelButton.prop("disabled", true);
}

function CheckTTSProviderModelManageTabHasChanges(enableDisableButton = true) {
	function arraysEqual(arr1, arr2) {
		if (!arr1 || !arr2) return false;
		if (arr1.length !== arr2.length) return false;

		const sortedArr1 = [...arr1].sort();
		const sortedArr2 = [...arr2].sort();

		return sortedArr1.every((value, index) => value === sortedArr2[index]);
	}

	let changes = {};
	let hasChanges = false;

	// Check basic information
	changes.id = manageTTSProviderModelIdInput.val().trim();
	if (changes.id !== CurrentTTSProviderModelData.id) {
		hasChanges = true;
	}

	changes.name = manageTTSProviderModelNameInput.val().trim();
	if (changes.name !== CurrentTTSProviderModelData.name) {
		hasChanges = true;
	}

	// Check price settings
	changes.pricePerUnit = manageTTSProviderModelPriceInput.val();
	if (changes.pricePerUnit !== CurrentTTSProviderModelData.pricePerUnit) {
		hasChanges = true;
	}

	changes.priceUnit = manageTTSProviderModelPriceUnitInput.val().trim();
	if (changes.priceUnit !== CurrentTTSProviderModelData.priceUnit) {
		hasChanges = true;
	}

	// Check language settings
	changes.supportedLanguages = [];
	manageTTSProviderModelLanguagesContainer.find('input[type="checkbox"]:checked').each(function () {
		changes.supportedLanguages.push($(this).val());
	});
	if (!arraysEqual(changes.supportedLanguages, CurrentTTSProviderModelData.supportedLanguages)) {
		hasChanges = true;
	}

	// Check disabled state
	changes.disabled = manageTTSProviderModelDisabledInput.prop("checked");
	if (changes.disabled === (CurrentTTSProviderModelData.disabledAt === null)) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveManageTTSProviderModelButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function ValidateTTSProviderModelManageTabFields(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Validate ID
	const modelId = manageTTSProviderModelIdInput.val().trim();
	if (!modelId) {
		validated = false;
		errors.push("Model ID is required");
		if (!onlyRemove) manageTTSProviderModelIdInput.addClass("is-invalid");
	} else {
		manageTTSProviderModelIdInput.removeClass("is-invalid");
	}

	// Validate Name
	const modelName = manageTTSProviderModelNameInput.val().trim();
	if (!modelName) {
		validated = false;
		errors.push("Model name is required");
		if (!onlyRemove) manageTTSProviderModelNameInput.addClass("is-invalid");
	} else {
		manageTTSProviderModelNameInput.removeClass("is-invalid");
	}

	// Validate Price
	const price = manageTTSProviderModelPriceInput.val();
	if (!price || isNaN(price) || parseFloat(price) <= 0) {
		validated = false;
		errors.push("Valid price is required");
		if (!onlyRemove) manageTTSProviderModelPriceInput.addClass("is-invalid");
	} else {
		manageTTSProviderModelPriceInput.removeClass("is-invalid");
	}

	// Validate Price Unit
	const priceUnit = manageTTSProviderModelPriceUnitInput.val().trim();
	if (!priceUnit) {
		validated = false;
		errors.push("Price unit is required");
		if (!onlyRemove) manageTTSProviderModelPriceUnitInput.addClass("is-invalid");
	} else {
		manageTTSProviderModelPriceUnitInput.removeClass("is-invalid");
	}

	// Validate Languages
	const selectedLanguages = manageTTSProviderModelLanguagesContainer.find('input[type="checkbox"]:checked').length;
	if (selectedLanguages === 0) {
		validated = false;
		errors.push("At least one language must be selected");
		if (!onlyRemove) manageTTSProviderModelLanguagesContainer.addClass("is-invalid");
	} else {
		manageTTSProviderModelLanguagesContainer.removeClass("is-invalid");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

/** Initialize **/
$(document).ready(() => {
	// Provider List Event Handlers
	TTSProviderListTable.on("click", "button[button-type=edit-tts-provider]", (event) => {
		event.preventDefault();

		let providerId = $(event.currentTarget).attr("provider-id");
		CurrentTTSProviderData = CurrentTTSProvidersList.find((provider) => provider.id.value == providerId);

		currentManageTTSProviderName.text(CurrentTTSProviderData.id.name);

		ResetAndEmptyTTSProvidersManageTab();
		FillTTSProviderManageTab(CurrentTTSProviderData);

		CurrentTTSProviderType = "edit";
		ShowTTSProviderManageTab();
	});

	// Switch back to list event handler
	switchBackToTTSProviderListTabFromManageTab.on("click", (event) => {
		event.preventDefault();
		CurrentTTSProviderType = null;
		ShowTTSProviderListTab();
	});

	// Provider Management Event Handlers
	manageTTSProviderIntegrationSelect.on("change", () => {
		if (CurrentTTSProviderType === null) return;
		CheckTTSProviderManageTabHasChanges(true);
	});

	manageTTSProviderDisabledInput.on("change", () => {
		if (CurrentTTSProviderType === null) return;
		CheckTTSProviderManageTabHasChanges(true);
	});

	// Save Provider Button Handler
	saveManageTTSProviderButton.on("click", (event) => {
		event.preventDefault();
		if (IsSavingTTSProviderTab) return;

		const validationResult = ValidateTTSProviderManageTab(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
				timeout: 6000,
			});
			return;
		}

		const changes = CheckTTSProviderManageTabHasChanges(false);
		if (!changes.hasChanges) return;

		IsSavingTTSProviderTab = true;
		saveManageTTSProviderButton.prop("disabled", true);

		const formData = new FormData();
		formData.append("changes", JSON.stringify(changes.changes));
		formData.append("providerId", CurrentTTSProviderData.id.value);

		SaveTTSProviderData(
			formData,
			(saveResponse) => {
				if (saveResponse.success) {
					CurrentTTSProviderData = saveResponse.data;

					if (ttsFieldsHelper) {
						ttsFieldsHelper.updateInitialData(CurrentTTSProviderData.userIntegrationFields);
					}

					const providerIndex = CurrentTTSProvidersList.findIndex((p) => p.id.value === CurrentTTSProviderData.id.value);
					if (providerIndex !== -1) {
						CurrentTTSProvidersList[providerIndex] = CurrentTTSProviderData;
					}

					TTSProviderListTable.find(`tr button[provider-id="${CurrentTTSProviderData.id.value}"]`)
						.closest("tr")
						.replaceWith($(CreateTTSProviderListTableElement(CurrentTTSProviderData)));

					AlertManager.createAlert({
						type: "success",
						message: "TTS provider data saved successfully.",
						timeout: 6000,
					});

					CheckTTSProviderManageTabHasChanges();
				} else {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving TTS provider data.",
						timeout: 6000,
					});
				}

				saveManageTTSProviderButton.prop("disabled", false);
				IsSavingTTSProviderTab = false;
			},
			(error, isUnsuccessful) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occurred while saving TTS provider data.",
					timeout: 6000,
				});
				console.error("Save error:", error);

				saveManageTTSProviderButton.prop("disabled", false);
				IsSavingTTSProviderTab = false;
			},
		);
	});

	// Model List Events
	addNewTTSProviderModelButton.on("click", (event) => {
		event.preventDefault();

		CurrentTTSProviderModelData = CreateDefaultTTSProviderModelObject();

		currentManageModelTTSProviderName.text(CurrentTTSProviderData.id.name);
		currentManageTTSProviderModelName.text("New Model");

		ResetAndEmptyTTSProviderModelManageTab();
		FillTTSProviderModelManageTab(CurrentTTSProviderModelData);

		CurrentTTSProviderModelType = "new";
		ShowTTSProviderModelManageTab();
	});

	ttsProviderModelListTable.on("click", "button[button-type=edit-tts-provider-model]", (event) => {
		event.preventDefault();

		let modelId = $(event.currentTarget).attr("model-id");
		CurrentTTSProviderModelData = CurrentTTSProviderData.models.find((model) => model.id === modelId);

		currentManageModelTTSProviderName.text(CurrentTTSProviderData.id.name);
		currentManageTTSProviderModelName.text(CurrentTTSProviderModelData.name);

		ResetAndEmptyTTSProviderModelManageTab();
		FillTTSProviderModelManageTab(CurrentTTSProviderModelData);

		CurrentTTSProviderModelType = "edit";
		ShowTTSProviderModelManageTab();
	});

	// Model Form Events
	switchBackToTTSProviderManagerModelsListTabFromModelTab.on("click", (event) => {
		event.preventDefault();
		CurrentTTSProviderModelType = null;
		ShowTTSProviderModelListTab();
	});

	// Form input change handlers
	ttsProviderManagerModelManageTab.on("input change", "input, select", () => {
		if (CurrentTTSProviderModelType === null) return;
		CheckTTSProviderModelManageTabHasChanges(true);
	});

	// Speaking Styles Events
	addNewSpeakingStyleButton.on("click", (event) => {
		event.preventDefault();
		AddSpeakingStyle();
	});

	// Save Model Button Handler
	saveManageTTSProviderModelButton.on("click", (event) => {
		event.preventDefault();
		if (IsSavingTTSProviderTab) return;

		const validationResult = ValidateTTSProviderModelManageTabFields(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
				timeout: 6000,
			});
			return;
		}

		const changes = CheckTTSProviderModelManageTabHasChanges(false);
		if (!changes.hasChanges) return;

		IsSavingTTSProviderTab = true;
		saveManageTTSProviderModelButton.prop("disabled", true);

		const formData = new FormData();
		formData.append("providerId", CurrentTTSProviderData.id.value);
		formData.append("modelId", changes.changes.id);
		formData.append("postType", CurrentTTSProviderModelType);
		formData.append("changes", JSON.stringify(changes.changes));

		SaveTTSProviderModelData(
			formData,
			(saveResponse) => {
				if (saveResponse.success) {
					// Update the current model data
					CurrentTTSProviderModelData = saveResponse.data;

					// Update the models list in the provider data
					const modelIndex = CurrentTTSProviderData.models.findIndex((s) => s.id === CurrentTTSProviderModelData.id);

					if (modelIndex !== -1) {
						CurrentTTSProviderData.models[modelIndex] = CurrentTTSProviderModelData;
					} else {
						CurrentTTSProviderData.models.push(CurrentTTSProviderModelData);
					}

					// Update the models table
					const modelRow = ttsProviderModelListTable.find(`tr button[model-id="${CurrentTTSProviderModelData.id}"]`).closest("tr");

					if (modelRow.length) {
						modelRow.replaceWith($(CreateTTSProviderModelListTableElement(CurrentTTSProviderModelData)));
					} else {
						ttsProviderModelListTable.find("tbody tr[tr-type='none-notice']").remove();
						ttsProviderModelListTable.find("tbody").append($(CreateTTSProviderModelListTableElement(CurrentTTSProviderModelData)));
					}

					AlertManager.createAlert({
						type: "success",
						message: "TTS provider model saved successfully.",
						timeout: 6000,
					});

					ShowTTSProviderModelListTab();
				} else {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving TTS provider model.",
						timeout: 6000,
					});
				}

				saveManageTTSProviderModelButton.prop("disabled", false);
				IsSavingTTSProviderTab = false;
			},
			(error, isUnsuccessful) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occurred while saving TTS provider model.",
					timeout: 6000,
				});
				console.error("Save error:", error);

				saveManageTTSProviderModelButton.prop("disabled", false);
				IsSavingTTSProviderTab = false;
			},
		);
	});

	// Search functionality
	searchTTSProviderModelButton.on("click", (event) => {
		event.preventDefault();
		const searchTerm = searchTTSProviderModelInput.val().toLowerCase().trim();

		ttsProviderModelListTable.find("tbody tr").each(function () {
			const row = $(this);
			if (row.attr("tr-type") === "none-notice") return;

			const modelId = row.find("td:first").text().toLowerCase();
			const modelName = row.find("td:eq(1)").text().toLowerCase();

			if (modelId.includes(searchTerm) || modelName.includes(searchTerm)) {
				row.show();
			} else {
				row.hide();
			}
		});
	});

	// Initialize provider list
	FetchTTSProvidersFromAPI(
		0,
		100,
		(ttsProvidersData) => {
			CurrentTTSProvidersList = ttsProvidersData;
			CurrentTTSProvidersList.forEach((providerData) => {
				TTSProviderListTable.find("tbody").append(CreateTTSProviderListTableElement(providerData));
			});
		},
		(error, isUnsuccessful) => {
			AlertManager.createAlert({
				type: "danger",
				message: "Error occurred while fetching TTS providers. Check browser console for logs.",
				timeout: 5000,
			});
			console.error("Error fetching TTS providers:", error);
		},
	);
});
