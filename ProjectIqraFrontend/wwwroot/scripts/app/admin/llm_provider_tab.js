/** Dynamic Variables **/
let CurrentManageLLMProviderType = null;
let CurrentManageLLMProviderData = null;

let CurrentManageLLMProviderModelType = null;
let CurrentManageLLMProviderModelData = null;

let IsSavingLLMProviderTab = false;

let llmFieldsHelper = null;

/** Elements Variables **/
const LLMProviderTab = $("#llm-provider-tab");

// Headers

// List Header
const llmProviderInnerHeader = LLMProviderTab.find("#llm-provider-inner-header");

// Provider Manager Header
const llmProviderManageInnerHeader = LLMProviderTab.find("#llm-provider-manage-inner-header");
const switchBackToLLMProviderListTabFromManageTab = llmProviderManageInnerHeader.find("#switchBackToLLMProviderListTabFromManageTab");
const currentManageLLMProviderName = llmProviderManageInnerHeader.find("#currentManageLLMProviderName");
const saveManageLLMProviderButton = llmProviderManageInnerHeader.find("#saveManageLLMProviderButton");
const llmProviderManagerInnerTab = llmProviderManageInnerHeader.find("#llm-provider-manager-inner-tab");
const llmProviderManagerGeneralTabButton = llmProviderManagerInnerTab.find("#llm-provider-manager-general-tab");

// Provider Model Manager Header
const llmProviderModelManagerInnerHeader = LLMProviderTab.find("#llm-provider-model-manager-inner-header");
const saveManageLLMProviderModelButton = llmProviderModelManagerInnerHeader.find("#saveManageLLMProviderModelButton");
const currentManageModelLLMProviderName = llmProviderModelManagerInnerHeader.find("#currentManageModelLLMProviderName");
const currentManageLLMProviderModelName = llmProviderModelManagerInnerHeader.find("#currentManageLLMProviderModelName");
const switchBackToLLMProviderManagerModelsListTabFromModelTab = llmProviderModelManagerInnerHeader.find("#switchBackToLLMProviderManagerModelsListTabFromModelTab");
const llmProviderModelManagerGeneralTabButton = llmProviderModelManagerInnerHeader.find("#llm-provider-model-manager-general-tab");

// List Tab
const LLMProviderListTableTab = LLMProviderTab.find("#llmProviderListTableTab");
const LLMProviderListTable = LLMProviderListTableTab.find("#llmProviderListTable");

// Provider Manager Tab
const LLMProviderManageTab = LLMProviderTab.find("#llmProviderManageTab");
const llmProviderModelListTable = LLMProviderManageTab.find("#llmProviderModelListTable");
const addNewLLMProviderModelButton = LLMProviderManageTab.find("#addNewLLMProviderModelButton");

// Provider Manager > General Tab
const llmProviderManagerGeneral = LLMProviderManageTab.find("#llm-provider-manager-general");

const manageLLMProviderIdInput = llmProviderManagerGeneral.find("#manageLLMProviderIdInput");
const manageLLMProviderDisabledInput = llmProviderManagerGeneral.find("#manageLLMProviderDisabledInput");

const manageLLMProviderIntegrationSelect = llmProviderManagerGeneral.find("#manageLLMProviderIntegrationSelect");

// Provider Manager > Models List Tab
const llmProviderManagerModelsListTab = LLMProviderManageTab.find("#llmProviderManagerModelsListTab");


// Provider Model Manager
const llmProviderManagerModelManageTab = LLMProviderManageTab.find("#llmProviderManagerModelManageTab");

const manageLLMProviderModelIdInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelIdInput");
const manageLLMProviderModelNameInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelNameInput");

const manageLLMProviderModelInputPriceInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelInputPriceInput");
const manageLLMProviderModelInputTokensInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelInputTokensInput");

const manageLLMProviderModelOutputPriceInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelOutputPriceInput");
const manageLLMProviderModelOutputTokensInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelOutputTokensInput");

const manageLLMProviderModelInputLengthInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelInputLengthInput");
const manageLLMProviderModelOutputLengthInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelOutputLengthInput");

const manageLLMProviderModelDisabledInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelDisabledInput");

// Integration Variables
const llmProviderIntegrationsTab = $("#llm-provider-manager-integrations");
const addNewLLMProviderIntegrationFieldButton = llmProviderIntegrationsTab.find("#addNewLLMProviderIntegrationFieldButton");
const llmProviderIntegrationFieldsList = llmProviderIntegrationsTab.find("#llmProviderIntegrationFieldsList");
const searchLLMProviderIntegrationFieldInput = llmProviderIntegrationsTab.find("input[aria-label='Search Field']");
const searchLLMProviderIntegrationFieldButton = llmProviderIntegrationsTab.find("#searchLLMProviderIntegrationFieldButton");

/** API Functions **/
function FetchLLMProvidersFromAPI(page, pageSize, successCallback, errorCallback) {
	$.ajax({
		url: '/app/admin/llmproviders',
		type: 'POST',
		dataType: "json",
		data: {
			page: page,
			pageSize: pageSize
		},
		success: (response) => {
			if (!response.success) {
				errorCallback(response, true);
				return;
			}

			successCallback(response.data);
		},
		error: (error) => {
			errorCallback(error, false);
		}
	});
}

function SaveLLMProviderData(formData, successCallback, errorCallback) {
	$.ajax({
		type: "POST",
		url: "/app/admin/llmproviders/save",
		data: formData,
		dataType: "json",
		processData: false,
		contentType: false,
		success: (response) => {
			if (!response.success) {
				errorCallback(response, true);
				return;
			}

			successCallback(response);
		},
		error: (error) => {
			errorCallback(error, false);
		}
	});
}

function SaveLLMProviderModelData(formData, successCallback, errorCallback) {
	$.ajax({
		type: "POST",
		url: "/app/admin/llmproviders/model/save",
		data: formData,
		dataType: "json",
		processData: false,
		contentType: false,
		success: (response) => {
			if (!response.success) {
				errorCallback(response, true);
				return;
			}

			successCallback(response);
		},
		error: (error) => {
			errorCallback(error, false);
		}
	});
}

/** Functions **/
function CreateLLMProviderListTableElement(llmProviderData) {
	let disabledData = "";
	if (llmProviderData.disabledAt == null) {
		disabledData = "-";
	} else {
		disabledData = `<span class="badge bg-danger">${llmProviderData.disabledAt}</span>`;
	}

	let element = $(`
                <tr>
                    <td>${llmProviderData.id.value}</td>
                    <td>${llmProviderData.id.name}</td>
                    <td>${disabledData}</td>
                    <td>${llmProviderData.models.length}</td>
                    <td>
                        <button class="btn btn-info btn-sm" provider-id="${llmProviderData.id.value}" button-type="edit-llm-provider">
                            <i class="fa-regular fa-eye"></i>
                        </button>
                    </td>
                </tr>
            `);

	return element;
}

function ResetAndEmptyLLMProvidersManageTab() {
	manageLLMProviderIdInput.val("");
	manageLLMProviderDisabledInput.prop("checked", false).change();
	llmProviderModelListTable.find("tbody").empty();
	manageLLMProviderIntegrationSelect.val("").change();

	llmProviderManagerGeneralTabButton.click();

	// Reset Helper
	llmProviderIntegrationFieldsList.empty();
	llmFieldsHelper = null;
}

function ShowLLMProviderManageTab() {
	LLMProviderListTableTab.removeClass("show");
	llmProviderInnerHeader.removeClass("show");

	setTimeout(() => {
		LLMProviderListTableTab.addClass("d-none");
		llmProviderInnerHeader.addClass("d-none");

		LLMProviderManageTab.removeClass("d-none");
		llmProviderManageInnerHeader.removeClass("d-none");

		setTimeout(() => {
			LLMProviderManageTab.addClass("show");
			llmProviderManageInnerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowLLMProviderListTab() {
	LLMProviderManageTab.removeClass("show");
	llmProviderManageInnerHeader.removeClass("show");

	setTimeout(() => {
		LLMProviderManageTab.addClass("d-none");
		llmProviderManageInnerHeader.addClass("d-none");

		LLMProviderListTableTab.removeClass("d-none");
		llmProviderInnerHeader.removeClass("d-none");

		setTimeout(() => {
			LLMProviderListTableTab.addClass("show");
			llmProviderInnerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function CreateLLMProviderModelListTableElement(modelData) {
	let disabledData = "";
	if (modelData.disabledAt == null) {
		disabledData = "-";
	} else {
		disabledData = `<span class="badge bg-danger">${modelData.disabledAt}</span>`;
	}

	let element = $(`<tr model-id="${modelData.id}">
                <td>${modelData.id}</td>
                <td>${modelData.name}</td>
                <td>${disabledData}</td>
                <td>
                    <button class="btn btn-info btn-sm" model-id="${modelData.id}" button-type="edit-llm-provider-model">
                        <i class="fa-regular fa-eye"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}

function FillLLMProviderManageTab(llmProviderData) {
	manageLLMProviderIdInput.val(llmProviderData.id.name);
	manageLLMProviderDisabledInput.prop("checked", llmProviderData.disabledAt != null);

	// Fill integration select
	fillLLMProviderIntegrationSelect();

	if (llmProviderData.models.length != 0) {
		llmProviderData.models.forEach((modelData) => {
			llmProviderModelListTable.find("tbody").append(CreateLLMProviderModelListTableElement(modelData));
		});
	} else {
		llmProviderModelListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="4">No models</td></tr>');
	}

	// Initialize Integration Fields Helper
	llmFieldsHelper = new ProviderIntegrationsFieldHelper(
		llmProviderIntegrationFieldsList,
		addNewLLMProviderIntegrationFieldButton,
		llmProviderData.userIntegrationFields,
		() => {
			// Callback when fields change
			CheckLLMProviderManageTabHasChanges(true);
		}
	);
	llmFieldsHelper.render();
}

function ShowLLMProviderModelManageTab() {
	llmProviderManagerModelsListTab.removeClass("show");
	llmProviderManageInnerHeader.removeClass("show");

	setTimeout(() => {
		llmProviderManagerModelsListTab.addClass("d-none");
		llmProviderManageInnerHeader.addClass("d-none");

		llmProviderManagerModelManageTab.removeClass("d-none");
		llmProviderModelManagerInnerHeader.removeClass("d-none");

		setTimeout(() => {
			llmProviderManagerModelManageTab.addClass("show");
			llmProviderModelManagerInnerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowLLMProviderModelListTab() {
	llmProviderManagerModelManageTab.removeClass("show");
	llmProviderModelManagerInnerHeader.removeClass("show");

	setTimeout(() => {
		llmProviderManagerModelManageTab.addClass("d-none");
		llmProviderModelManagerInnerHeader.addClass("d-none");

		llmProviderManagerModelsListTab.removeClass("d-none");
		llmProviderManageInnerHeader.removeClass("d-none");

		setTimeout(() => {
			llmProviderManagerModelsListTab.addClass("show");
			llmProviderManageInnerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function CheckLLMProviderModelManageTabHasChanges(enableDisableButton = true) {
	let changes = {};
	let hasChanges = false;

	changes.id = manageLLMProviderModelIdInput.val();
	if (CurrentManageLLMProviderModelData.id != changes.id) {
		hasChanges = true;
	}

	changes.name = manageLLMProviderModelNameInput.val();
	if (CurrentManageLLMProviderModelData.name != changes.name) {
		hasChanges = true;
	}

	changes.disabled = manageLLMProviderModelDisabledInput.prop("checked");
	if (changes.disabled == (CurrentManageLLMProviderModelData.disabledAt == null)) {
		hasChanges = true;
	}

	changes.inputPrice = manageLLMProviderModelInputPriceInput.val();
	if (CurrentManageLLMProviderModelData.inputPrice != changes.inputPrice) {
		hasChanges = true;
	}

	changes.inputPriceTokenUnit = manageLLMProviderModelInputTokensInput.val();
	if (CurrentManageLLMProviderModelData.inputPriceTokenUnit != changes.inputPriceTokenUnit) {
		hasChanges = true;
	}

	changes.outputPrice = manageLLMProviderModelOutputPriceInput.val();
	if (CurrentManageLLMProviderModelData.outputPrice != changes.outputPrice) {
		hasChanges = true;
	}

	changes.outputPriceTokenUnit = manageLLMProviderModelOutputTokensInput.val();
	if (CurrentManageLLMProviderModelData.outputPriceTokenUnit != changes.outputPriceTokenUnit) {
		hasChanges = true;
	}

	changes.maxInputTokenLength = manageLLMProviderModelInputLengthInput.val();
	if (CurrentManageLLMProviderModelData.maxInputTokenLength != changes.maxInputTokenLength) {
		hasChanges = true;
	}

	changes.maxOutputTokenLength = manageLLMProviderModelOutputLengthInput.val();
	if (CurrentManageLLMProviderModelData.maxOutputTokenLength != changes.maxOutputTokenLength) {
		hasChanges = true;
	}

	if (enableDisableButton == true) {
		saveManageLLMProviderModelButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function CheckLLMProviderManageTabHasChanges(enableDisableButton = true) {
	let changes = {};
	let hasChanges = false;

	// Check disabled state
	changes.disabled = manageLLMProviderDisabledInput.prop("checked");
	if (changes.disabled === (CurrentManageLLMProviderData.disabledAt == null)) {
		hasChanges = true;
	}

	// Check integration selection
	changes.integrationId = manageLLMProviderIntegrationSelect.val();
	if (changes.integrationId !== CurrentManageLLMProviderData.integrationId) {
		hasChanges = true;
	}

	// Check integration fields via Helper
	changes.userIntegrationFields = llmFieldsHelper.getData();
	if (llmFieldsHelper.hasChanges()) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveManageLLMProviderButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function ResetAndEmptyLLMProviderModelManageTab() {
	llmProviderManagerModelManageTab.find("input, textarea").val("").change();
	llmProviderManagerModelManageTab.find(".is-invalid").removeClass("is-invalid");
	manageLLMProviderModelDisabledInput.prop("checked", true);

	saveManageLLMProviderModelButton.prop("disabled", true);

	llmProviderModelManagerGeneralTabButton.click();
}

function CreateDefaultLLMProviderModelObject() {
	let object = {
		id: "",
		name: "",
		disabledAt: true,
		inputPrice: "",
		inputPriceTokenUnit: "",
		outputPrice: "",
		outputPriceTokenUnit: "",
		maxInputTokenLength: "",
		maxOutputTokenLength: ""
	};

	return object;
}

function ValidateLLMProviderModelManageTabFields(onlyRemove = false) {
	let errors = [];
	let validated = true;

	// General Tab Fields

	let modelId = manageLLMProviderModelIdInput.val();
	if (!modelId || modelId.trim().length === 0 || modelId === "") {
		validated = false;
		errors.push("Model id is required and can not be empty.");

		if (!onlyRemove) {
			manageLLMProviderModelIdInput.addClass("is-invalid");
		}
	} else {
		manageLLMProviderModelIdInput.removeClass("is-invalid");
	}

	let modelName = manageLLMProviderModelNameInput.val();
	if (!modelName || modelName.trim().length === 0 || modelName === "") {
		validated = false;
		errors.push("Model name is required and can not be empty.");

		if (!onlyRemove) {
			manageLLMProviderModelNameInput.addClass("is-invalid");
		}
	} else {
		manageLLMProviderModelNameInput.removeClass("is-invalid");
	}

	let modelInputPrice = manageLLMProviderModelInputPriceInput.val();
	if (!modelInputPrice || modelInputPrice.trim().length === 0 || modelInputPrice === "") {
		validated = false;
		errors.push("Input price is required and can not be empty.");

		if (!onlyRemove) {
			manageLLMProviderModelInputPriceInput.addClass("is-invalid");
		}
	} else {
		manageLLMProviderModelInputPriceInput.removeClass("is-invalid");
	}

	let modelInputPriceTokenUnit = manageLLMProviderModelInputTokensInput.val();
	if (!modelInputPriceTokenUnit || modelInputPriceTokenUnit.trim().length === 0 || modelInputPriceTokenUnit === "") {
		validated = false;
		errors.push("Input price token unit is required and can not be empty.");

		if (!onlyRemove) {
			manageLLMProviderModelInputTokensInput.addClass("is-invalid");
		}
	} else {
		manageLLMProviderModelInputTokensInput.removeClass("is-invalid");
	}

	let modelOutputPrice = manageLLMProviderModelOutputPriceInput.val();
	if (!modelOutputPrice || modelOutputPrice.trim().length === 0 || modelOutputPrice === "") {
		validated = false;
		errors.push("Output price is required and can not be empty.");

		if (!onlyRemove) {
			manageLLMProviderModelOutputPriceInput.addClass("is-invalid");
		}
	} else {
		manageLLMProviderModelOutputPriceInput.removeClass("is-invalid");
	}

	let modelOutputPriceTokenUnit = manageLLMProviderModelOutputTokensInput.val();
	if (!modelOutputPriceTokenUnit || modelOutputPriceTokenUnit.trim().length === 0 || modelOutputPriceTokenUnit === "") {
		validated = false;
		errors.push("Output price token unit is required and can not be empty.");

		if (!onlyRemove) {
			manageLLMProviderModelOutputTokensInput.addClass("is-invalid");
		}
	} else {
		manageLLMProviderModelOutputTokensInput.removeClass("is-invalid");
	}

	let modelMaxInputTokenLength = manageLLMProviderModelInputLengthInput.val();
	if (!modelMaxInputTokenLength || modelMaxInputTokenLength.trim().length === 0 || modelMaxInputTokenLength === "") {
		validated = false;
		errors.push("Max input token length is required and can not be empty.");

		if (!onlyRemove) {
			manageLLMProviderModelInputLengthInput.addClass("is-invalid");
		}
	} else {
		manageLLMProviderModelInputLengthInput.removeClass("is-invalid");
	}

	let modelMaxOutputTokenLength = manageLLMProviderModelOutputLengthInput.val();
	if (!modelMaxOutputTokenLength || modelMaxOutputTokenLength.trim().length === 0 || modelMaxOutputTokenLength === "") {
		validated = false;
		errors.push("Max output token length is required and can not be empty.");

		if (!onlyRemove) {
			manageLLMProviderModelOutputLengthInput.addClass("is-invalid");
		}
	} else {
		manageLLMProviderModelOutputLengthInput.removeClass("is-invalid");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

function FillLLMProviderModelManageTab(modelData) {
	manageLLMProviderModelIdInput.val(modelData.id);
	manageLLMProviderModelNameInput.val(modelData.name);
	manageLLMProviderModelInputPriceInput.val(modelData.inputPrice);
	manageLLMProviderModelInputTokensInput.val(modelData.inputPriceTokenUnit);
	manageLLMProviderModelOutputPriceInput.val(modelData.outputPrice);
	manageLLMProviderModelOutputTokensInput.val(modelData.outputPriceTokenUnit);
	manageLLMProviderModelInputLengthInput.val(modelData.maxInputTokenLength);
	manageLLMProviderModelOutputLengthInput.val(modelData.maxOutputTokenLength);
	manageLLMProviderModelDisabledInput.prop("checked", modelData.disabledAt != null);
}

function ValidateLLMProviderManageTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// General Tab
	const selectedIntegration = manageLLMProviderIntegrationSelect.val();
	if (!selectedIntegration) {
		validated = false;
		errors.push("Integration selection is required");
		if (!onlyRemove) {
			manageLLMProviderIntegrationSelect.addClass("is-invalid");
		}
	} else {
		manageLLMProviderIntegrationSelect.removeClass("is-invalid");
	}

	// Integration Tab
	const fieldValidation = llmFieldsHelper.validate(onlyRemove);
	if (!fieldValidation.validated) {
		validated = false;
		errors.push(...fieldValidation.errors);
	}

	return {
		validated: validated,
		errors: errors,
	};
}

function fillLLMProviderIntegrationSelect() {
	manageLLMProviderIntegrationSelect.empty();
	manageLLMProviderIntegrationSelect.append('<option value="">Select Integration</option>');

	// Filter available integrations that have LLM in their type
	const llmIntegrations = CurrentIntegrationsList.filter((integration) => integration.type.includes("LLM"));

	llmIntegrations.forEach((integration) => {
		manageLLMProviderIntegrationSelect.append(
			`<option value="${integration.id}" ${CurrentManageLLMProviderData.integrationId === integration.id ? "selected" : ""}>
                        ${integration.name}
                    </option>`,
		);
	});
}

// Integration Functions


/** Initalizer **/

$(document).ready(() => {
	// Event Handlers

	llmProviderManagerGeneral.on("input change", "input, textarea, select", (event) => {
		if (CurrentManageLLMProviderType == null) return;

		CheckLLMProviderManageTabHasChanges(true);
		ValidateLLMProviderIntegrationFieldsTab(true);
	});

	saveManageLLMProviderButton.on("click", async (event) => {
		event.preventDefault();

		if (IsSavingLLMProviderTab) return;

		// Validate fields
		const validationResult = ValidateLLMProviderManageTab(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
				timeout: 6000,
			});
			return;
		}

		// Check for changes
		const changes = CheckLLMProviderManageTabHasChanges(false);
		if (!changes.hasChanges) return;

		// Update UI state
		saveManageLLMProviderButton.prop("disabled", true);
		const saveButtonSpinner = saveManageLLMProviderButton.find(".spinner-border");
		saveButtonSpinner.removeClass("d-none");

		IsSavingLLMProviderTab = true;

		// Prepare form data
		const formData = new FormData();
		formData.append("changes", JSON.stringify(changes.changes));
		formData.append("providerId", CurrentManageLLMProviderData.id.value);

		SaveLLMProviderData(
			formData,
			(saveResponse) => {
				if (saveResponse.success) {
					// Update current data
					CurrentManageLLMProviderData = saveResponse.data;

					if (llmFieldsHelper) {
						llmFieldsHelper.updateInitialData(CurrentManageLLMProviderData.userIntegrationFields);
					}

					// Update providers list
					let providerIndex = CurrentLLMProvidersList.findIndex((p) => p.id.value === CurrentManageLLMProviderData.id.value);
					if (providerIndex !== -1) {
						CurrentLLMProvidersList[providerIndex] = CurrentManageLLMProviderData;
					}

					// Update table row
					LLMProviderListTable.find(`tr button[provider-id="${CurrentManageLLMProviderData.id.value}"]`)
						.closest("tr")
						.replaceWith($(CreateLLMProviderListTableElement(CurrentManageLLMProviderData)));

					// Show success message
					AlertManager.createAlert({
						type: "success",
						message: "LLM provider data saved successfully.",
						timeout: 6000,
					});

					// Reset state
					CheckLLMProviderManageTabHasChanges();
				} else {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving LLM provider data. Check browser console for logs.",
						timeout: 6000,
					});

					console.error("Error occurred while saving LLM provider data: ", saveResponse);
				}

				// Reset UI state
				saveManageLLMProviderButton.prop("disabled", true);
				saveButtonSpinner.addClass("d-none");
				IsSavingLLMProviderTab = false;
			},
			(saveError, isUnsuccessful) => {
				// Handle error
				AlertManager.createAlert({
					type: "danger",
					message: "Error occurred while saving LLM provider data. Check browser console for logs.",
					timeout: 6000,
				});

				console.error("Error occurred while saving LLM provider data: ", saveError);

				// Reset UI state
				saveManageLLMProviderButton.prop("disabled", false);
				saveButtonSpinner.addClass("d-none");
				IsSavingLLMProviderTab = false;
			},
		);
	});

	llmProviderManagerInnerTab.find('button[data-bs-toggle="pill"]').on("shown.bs.tab", (event) => {
		let newTab = event.target;

		if (newTab.id == "llm-provider-manager-models-tab") {
			saveManageLLMProviderButton.addClass("d-none");
		} else {
			saveManageLLMProviderButton.removeClass("d-none");
		}
	});

	saveManageLLMProviderModelButton.on("click", (event) => {
		event.preventDefault();

		let validation = ValidateLLMProviderModelManageTabFields(false);
		if (!validation.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: "Validation for required fields failed.<br><br>" + validation.errors.join("<br>"),
				timeout: 6000,
			});

			return;
		}

		let changes = CheckLLMProviderModelManageTabHasChanges();
		if (!changes.hasChanges) {
			return;
		}

		saveManageLLMProviderModelButton.prop("disabled", true);

		let formData = new FormData();
		formData.append("postType", CurrentManageLLMProviderModelType);
		formData.append("providerId", CurrentManageLLMProviderData.id.name);
		formData.append("modelId", changes.changes.id);
		formData.append("changes", JSON.stringify(changes.changes));

		SaveLLMProviderModelData(
			formData,
			(saveResponse) => {
				if (saveResponse.success) {
					CurrentManageLLMProviderModelData = saveResponse.data;

					let newTableElement = CreateLLMProviderModelListTableElement(CurrentManageLLMProviderModelData);

					if (CurrentManageLLMProviderModelType == "new") {
						CurrentManageLLMProviderData.models.push(CurrentManageLLMProviderModelData);
						CurrentManageLLMProviderModelType = "edit";

						llmProviderModelListTable.find(`tbody tr[model-id="${CurrentManageLLMProviderModelData.id}"]`).append(newTableElement);
					} else if (CurrentManageLLMProviderModelType == "edit") {
						let currentModelIndex = CurrentManageLLMProviderData.models.findIndex((model) => model.id == saveResponse.data.id);
						CurrentManageLLMProviderData.models.splice(currentModelIndex, 0, CurrentManageLLMProviderModelData);

						llmProviderModelListTable.find(`tbody tr[model-id="${CurrentManageLLMProviderModelData.id}"]`).replaceWith(newTableElement);
					}

					currentManageLLMProviderModelName.text(CurrentManageLLMProviderModelData.name);

					CheckLLMProviderModelManageTabHasChanges();

					AlertManager.createAlert({
						type: "success",
						message: "LLM provider model data saved successfully.",
						timeout: 6000,
					});
				} else {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occured while saving LLM provider model data. Check browser console for logs.",
						timeout: 6000,
					});

					console.log("Error occured while saving LLM provider model data: ", saveResponse);
				}
			},
			(saveError, isUnsuccessful) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while saving LLM provider model data. Check browser console for logs.",
					timeout: 6000,
				});

				console.log("Error occured while saving LLM provider model data: ", saveError);

				saveManageLLMProviderModelButton.prop("disabled", false);
			},
		);
	});

	switchBackToLLMProviderManagerModelsListTabFromModelTab.on("click", (event) => {
		event.preventDefault();

		CurrentManageLLMProviderModelType = null;

		ShowLLMProviderModelListTab();
	});

	addNewLLMProviderModelButton.on("click", (event) => {
		event.preventDefault();

		CurrentManageLLMProviderModelData = CreateDefaultLLMProviderModelObject();

		currentManageModelLLMProviderName.text(CurrentManageLLMProviderData.id.name);
		currentManageLLMProviderModelName.text("New Model");

		ResetAndEmptyLLMProviderModelManageTab();

		FillLLMProviderModelManageTab(CurrentManageLLMProviderModelData);

		CurrentManageLLMProviderModelType = "new";

		ShowLLMProviderModelManageTab();
	});

	LLMProviderListTable.on("click", "button[button-type=edit-llm-provider]", (event) => {
		event.preventDefault();

		let providerId = $(event.currentTarget).attr("provider-id");
		CurrentManageLLMProviderData = CurrentLLMProvidersList.find((provider) => provider.id.value == providerId);

		currentManageLLMProviderName.text(CurrentManageLLMProviderData.id.name);

		ResetAndEmptyLLMProvidersManageTab();

		FillLLMProviderManageTab(CurrentManageLLMProviderData);

		CurrentManageLLMProviderType = "edit";

		ShowLLMProviderManageTab();
	});

	switchBackToLLMProviderListTabFromManageTab.on("click", (event) => {
		event.preventDefault();

		CurrentManageLLMProviderType = null;

		ShowLLMProviderListTab();
	});

	llmProviderModelListTable.on("click", "button[button-type=edit-llm-provider-model]", (event) => {
		event.preventDefault();

		let currentModelId = $(event.currentTarget).attr("model-id");

		CurrentManageLLMProviderModelData = CurrentManageLLMProviderData.models.find((model) => model.id == currentModelId);

		currentManageModelLLMProviderName.text(CurrentManageLLMProviderData.id.name);
		currentManageLLMProviderModelName.text(CurrentManageLLMProviderModelData.name);

		ResetAndEmptyLLMProviderModelManageTab();
		FillLLMProviderModelManageTab(CurrentManageLLMProviderModelData);

		CurrentManageLLMProviderModelType = "edit";
		ShowLLMProviderModelManageTab();
	});

	llmProviderManagerModelManageTab.on("input change", "input, textarea", (event) => {
		if (CurrentManageLLMProviderModelType == null) return;

		CheckLLMProviderModelManageTabHasChanges(true);
	});

	// Integration Event Handlers


	// INIT

	FetchLLMProvidersFromAPI(
		0,
		100,
		(llmProvidersData) => {
			console.log("llmProviderData: ", llmProvidersData);
			CurrentLLMProvidersList = llmProvidersData;

			CurrentLLMProvidersList.forEach((llmProviderData) => {
				LLMProviderListTable.append(CreateLLMProviderListTableElement(llmProviderData));
			});
		},
		(llmProviderError, isUnsuccessful) => {
			AlertManager.createAlert({
				type: "danger",
				message: "Error occured while fetching llm providers. Check browser console for logs.",
				timeout: 5000,
			});

			console.log("Error occured while fetching llm providers: ", llmProviderError);
		},
	);
});
