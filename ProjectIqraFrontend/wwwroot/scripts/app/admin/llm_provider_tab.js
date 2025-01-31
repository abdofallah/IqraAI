/** Dynamic Variables **/
let CurrentManageLLMProviderType = null;
let CurrentManageLLMProviderData = null;

let CurrentManageLLMProviderModelType = null;
let CurrentManageLLMProviderModelData = null;
let CurrentManageLLMProviderModelPromptLanguageNewData = null;

let IsSavingLLMProviderTab = false;

/** Elements Variables **/
const LLMProviderTab = $("#llm-provider-tab");

const llmProviderInnerTab = LLMProviderTab.find("#llm-provider-inner-tab");
const llmProviderManageBreadcrumb = LLMProviderTab.find("#llm-provider-manage-breadcrumb");

const switchBackToLLMProviderListTabFromManageTab = llmProviderManageBreadcrumb.find("#switchBackToLLMProviderListTabFromManageTab");
const currentManageLLMProviderName = llmProviderManageBreadcrumb.find("#currentManageLLMProviderName");

const LLMProviderListTableTab = LLMProviderTab.find("#llmProviderListTableTab");
const LLMProviderListTable = LLMProviderListTableTab.find("#llmProviderListTable");

const LLMProviderManageTab = LLMProviderTab.find("#llmProviderManageTab");
const llmProviderModelListTable = LLMProviderManageTab.find("#llmProviderModelListTable");
const addNewLLMProviderModelButton = LLMProviderManageTab.find("#addNewLLMProviderModelButton");

const llmProviderManagerInnerTabContainer = LLMProviderManageTab.find("#llm-provider-manager-inner-tab-container");
const llmProviderManagerInnerTab = llmProviderManagerInnerTabContainer.find("#llm-provider-manager-inner-tab");
const llmProviderModelManagerBreadcrumb = LLMProviderTab.find("#llm-provider-model-manager-breadcrumb");
const saveManageLLMProviderModelButton = llmProviderModelManagerBreadcrumb.find("#saveManageLLMProviderModelButton");
const saveManageLLMProviderButton = llmProviderManagerInnerTabContainer.find("#saveManageLLMProviderButton");

const currentManageModelLLMProviderName = llmProviderModelManagerBreadcrumb.find("#currentManageModelLLMProviderName");
const currentManageLLMProviderModelName = llmProviderModelManagerBreadcrumb.find("#currentManageLLMProviderModelName");
const switchBackToLLMProviderManagerModelsListTabFromModelTab = llmProviderModelManagerBreadcrumb.find("#switchBackToLLMProviderManagerModelsListTabFromModelTab");

const llmProviderManagerGeneral = LLMProviderManageTab.find("#llm-provider-manager-general");

const manageLLMProviderIdInput = llmProviderManagerGeneral.find("#manageLLMProviderIdInput");
const manageLLMProviderDisabledInput = llmProviderManagerGeneral.find("#manageLLMProviderDisabledInput");

const manageLLMProviderIntegrationSelect = llmProviderManagerGeneral.find("#manageLLMProviderIntegrationSelect");

const llmProviderManagerModelsListTab = LLMProviderManageTab.find("#llmProviderManagerModelsListTab");

const llmProviderManagerModelManageTab = LLMProviderManageTab.find("#llmProviderManagerModelManageTab");

const llmProviderManagerGeneralTab = LLMProviderManageTab.find("#llm-provider-manager-general-tab");

const manageLLMProviderModelIdInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelIdInput");
const manageLLMProviderModelNameInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelNameInput");

const manageLLMProviderModelInputPriceInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelInputPriceInput");
const manageLLMProviderModelInputTokensInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelInputTokensInput");

const manageLLMProviderModelOutputPriceInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelOutputPriceInput");
const manageLLMProviderModelOutputTokensInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelOutputTokensInput");

const manageLLMProviderModelInputLengthInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelInputLengthInput");
const manageLLMProviderModelOutputLengthInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelOutputLengthInput");

const manageLLMProviderModelDisabledInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelDisabledInput");

const manageLLMProviderModelPromptTemplateInput = llmProviderManagerModelManageTab.find("#manageLLMProviderModelPromptTemplateInput");

var manageLLMProviderModelLanguageDropdown = null;
RunActionAfterLanguagesLoad(() => {
	manageLLMProviderModelLanguageDropdown = new MultiLanguageDropdown("manageLLMProviderModelLanguageContainer", CurrentLanguagesList);
});

var llmProviderModelManagerGeneralTab = llmProviderManagerModelManageTab.find("#llm-provider-model-manager-general-tab");

// Integration Variables
const llmProviderIntegrationsTab = $("#llm-provider-manager-integrations");
const addNewLLMProviderIntegrationFieldButton = llmProviderIntegrationsTab.find("#addNewLLMProviderIntegrationFieldButton");
const llmProviderIntegrationFieldsList = llmProviderIntegrationsTab.find("#llmProviderIntegrationFieldsList");
const searchLLMProviderIntegrationFieldInput = llmProviderIntegrationsTab.find("input[aria-label='Search Field']");
const searchLLMProviderIntegrationFieldButton = llmProviderIntegrationsTab.find("#searchLLMProviderIntegrationFieldButton");

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

	llmProviderManagerGeneralTab.click();
}

function ShowLLMProviderManageTab() {
	LLMProviderListTableTab.removeClass("show");
	llmProviderInnerTab.removeClass("show");

	setTimeout(() => {
		LLMProviderListTableTab.addClass("d-none");
		llmProviderInnerTab.addClass("d-none");

		LLMProviderManageTab.removeClass("d-none");
		llmProviderManageBreadcrumb.removeClass("d-none");

		setTimeout(() => {
			LLMProviderManageTab.addClass("show");
			llmProviderManageBreadcrumb.addClass("show");
		}, 10);
	}, 300);
}

function ShowLLMProviderListTab() {
	LLMProviderManageTab.removeClass("show");
	llmProviderManageBreadcrumb.removeClass("show");

	setTimeout(() => {
		LLMProviderManageTab.addClass("d-none");
		llmProviderManageBreadcrumb.addClass("d-none");

		LLMProviderListTableTab.removeClass("d-none");
		llmProviderInnerTab.removeClass("d-none");

		setTimeout(() => {
			LLMProviderListTableTab.addClass("show");
			llmProviderInnerTab.addClass("show");
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
}

function ShowLLMProviderModelManageTab() {
	llmProviderManagerModelsListTab.removeClass("show");
	llmProviderManagerInnerTabContainer.removeClass("show");
	llmProviderManageBreadcrumb.removeClass("show");

	setTimeout(() => {
		llmProviderManagerModelsListTab.addClass("d-none");
		llmProviderManagerInnerTabContainer.addClass("d-none");
		llmProviderManageBreadcrumb.addClass("d-none");

		llmProviderManagerModelManageTab.removeClass("d-none");
		llmProviderModelManagerBreadcrumb.removeClass("d-none");

		setTimeout(() => {
			llmProviderManagerModelManageTab.addClass("show");
			llmProviderModelManagerBreadcrumb.addClass("show");
		}, 10);
	}, 300);
}

function ShowLLMProviderModelListTab() {
	llmProviderManagerModelManageTab.removeClass("show");
	llmProviderModelManagerBreadcrumb.removeClass("show");

	setTimeout(() => {
		llmProviderManagerModelManageTab.addClass("d-none");
		llmProviderModelManagerBreadcrumb.addClass("d-none");

		llmProviderManagerModelsListTab.removeClass("d-none");
		llmProviderManagerInnerTabContainer.removeClass("d-none");
		llmProviderManageBreadcrumb.removeClass("d-none");

		setTimeout(() => {
			llmProviderManagerModelsListTab.addClass("show");
			llmProviderManagerInnerTabContainer.addClass("show");
			llmProviderManageBreadcrumb.addClass("show");
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

	changes.promptTemplates = {};
	CurrentLanguagesList.forEach((language) => {
		changes.promptTemplates[language.id] = CurrentManageLLMProviderModelPromptLanguageNewData[language.id];

		if (CurrentManageLLMProviderModelPromptLanguageNewData[language.id] != changes.promptTemplates[language.id]) {
			hasChanges = true;
		}
	});

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

	// Check integration fields
	const integrationFieldsChanges = CheckLLMProviderIntegrationFieldsTabHasChanges();
	if (integrationFieldsChanges.hasChanges) {
		hasChanges = true;
		changes.userIntegrationFields = integrationFieldsChanges.changes;
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

	llmProviderModelManagerGeneralTab.click();
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
		maxOutputTokenLength: "",
		promptTemplates: {},
	};

	CurrentLanguagesList.forEach((language) => {
		object.promptTemplates[language.id] = "";
	});

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

	// Prompts Tab Fields

	Object.keys(CurrentManageLLMProviderModelPromptLanguageNewData).forEach((proxyTemplateLanguageDataKey) => {
		let proxyTemplateLanguageData = CurrentManageLLMProviderModelPromptLanguageNewData[proxyTemplateLanguageDataKey];

		if (!proxyTemplateLanguageData || proxyTemplateLanguageData.trim().length === 0 || proxyTemplateLanguageData === "") {
			validated = false;
			errors.push(`Prompt template for language ${proxyTemplateLanguageDataKey} is required and can not be empty.`);

			if (!onlyRemove) {
				manageLLMProviderModelPromptTemplateInput.addClass("is-invalid");
			}
		} else {
			manageLLMProviderModelPromptTemplateInput.removeClass("is-invalid");
		}
	});

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

	Object.keys(modelData.promptTemplates).forEach((proxyTemplateLanguageDataKey) => {
		let value = modelData.promptTemplates[proxyTemplateLanguageDataKey];

		let valueIsInComplete = !value || value == "" || value.trim() == "";
		manageLLMProviderModelLanguageDropdown.setLanguageStatus(proxyTemplateLanguageDataKey, valueIsInComplete ? "incomplete" : "complete");
	});
	manageLLMProviderModelPromptTemplateInput.val(modelData.promptTemplates[manageLLMProviderModelLanguageDropdown.getSelectedLanguage().id]).change();
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
	const integrationValidation = ValidateLLMProviderIntegrationFieldsTab(onlyRemove);
	if (!integrationValidation.validated) {
		validated = false;
		errors.push(...integrationValidation.errors);
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
function createLLMProviderIntegrationFieldElement(fieldData = null) {
	const fieldId = fieldData?.id || generateUniqueId();

	return `
                <div class="card mb-3 integration-field" data-field-id="${fieldId}">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start mb-3">
                            <h6 class="card-title mb-0">Field</h6>
                            <button type="button" class="btn btn-danger btn-sm remove-field-button">
                                <i class="fa-regular fa-trash"></i>
                            </button>
                        </div>
                        <div class="row">
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Field ID</label>
                                <input type="text" class="form-control field-id-input" 
                                    placeholder="Field ID" value="${fieldData?.id || ""}">
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Name</label>
                                <input type="text" class="form-control field-name-input" 
                                    placeholder="Field Name" value="${fieldData?.name || ""}">
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Type</label>
                                <select class="form-select field-type-select">
                                    <option value="text" ${fieldData?.type === "text" ? "selected" : ""}>Text</option>
                                    <option value="number" ${fieldData?.type === "number" ? "selected" : ""}>Number</option>
									<option value="double_number" ${fieldData?.type === "double_number" ? "selected" : ""}>Double Number</option>
                                    <option value="select" ${fieldData?.type === "select" ? "selected" : ""}>Select</option>
                                    <option value="models" ${fieldData?.type === "models" ? "selected" : ""}>Models</option>
                                </select>
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Tooltip</label>
                                <input type="text" class="form-control field-tooltip-input" 
                                    placeholder="Field Tooltip" value="${fieldData?.tooltip || ""}">
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Placeholder</label>
                                <input type="text" class="form-control field-placeholder-input" 
                                    placeholder="Field Placeholder" value="${fieldData?.placeholder || ""}">
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Default Value</label>
                                <input type="text" class="form-control field-default-value-input" 
                                    placeholder="Default Value" value="${fieldData?.defaultValue || ""}"
                                    ${fieldData?.type === "select" || fieldData?.type === "models" ? "disabled" : ""}>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-check">
                                    <input class="form-check-input field-required-check" type="checkbox" 
                                        ${fieldData?.required ? "checked" : ""}>
                                    <label class="form-check-label">Required</label>
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-check">
                                    <input class="form-check-input field-encrypted-check" type="checkbox"
                                        ${fieldData?.isEncrypted ? "checked" : ""}>
                                    <label class="form-check-label">Encrypted</label>
                                </div>
                            </div>
                        </div>
                        <div class="field-options-container ${fieldData?.type === "select" ? "" : "d-none"} mt-3">
                            <label class="form-label">Options</label>
                            <div class="field-options-list">
                                ${fieldData?.options?.map((option) => createLLMIntegrationFieldOptionElement(option)).join("") || ""}
                            </div>
                            <button type="button" class="btn btn-outline-primary btn-sm mt-2 add-option-button">
                                <i class="fa-regular fa-plus"></i> Add Option
                            </button>
                        </div>
                    </div>
                </div>
            `;
}

function createLLMIntegrationFieldOptionElement(optionData = null) {
	return `
                <div class="input-group mb-2 field-option">
                    <input type="text" class="form-control option-key-input" placeholder="Option Key"
                        value="${optionData?.key || ""}">
                    <input type="text" class="form-control option-value-input" placeholder="Option Value"
                        value="${optionData?.value || ""}">
                    <div class="input-group-text">
                        <input class="form-check-input option-default-check mt-0" type="radio" name="defaultOption" ${optionData?.isDefault ? "checked" : ""}>
                        <label class="ms-2">Default?</label>
                    </div>
                    <button class="btn btn-outline-danger remove-option-button" type="button">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </div>
            `;
}

function fillIntegrationFields() {
	llmProviderIntegrationFieldsList.empty();

	if (CurrentManageLLMProviderData.userIntegrationFields.length === 0) {
		llmProviderIntegrationFieldsList.append(`
                    <div class="text-center p-5">
                        <p class="text-muted mb-0">No integration fields defined</p>
                    </div>
                `);
		return;
	}

	CurrentManageLLMProviderData.userIntegrationFields.forEach((field) => {
		llmProviderIntegrationFieldsList.append($(createLLMProviderIntegrationFieldElement(field)));
	});
}

function CheckLLMProviderIntegrationFieldsTabHasChanges() {
	let changes = [];
	let hasChanges = false;

	// Collect all current fields
	llmProviderIntegrationFieldsList.find(".integration-field").each(function () {
		const field = $(this);
		const fieldData = {
			id: field.find(".field-id-input").val().trim(),
			name: field.find(".field-name-input").val().trim(),
			type: field.find(".field-type-select").val(),
			tooltip: field.find(".field-tooltip-input").val().trim(),
			placeholder: field.find(".field-placeholder-input").val().trim(),
			defaultValue: field.find(".field-default-value-input").val().trim(),
			required: field.find(".field-required-check").is(":checked"),
			isEncrypted: field.find(".field-encrypted-check").is(":checked"),
		};

		if (fieldData.type === "select") {
			fieldData.options = [];
			field.find(".field-option").each(function () {
				const option = $(this);
				fieldData.options.push({
					key: option.find(".option-key-input").val().trim(),
					value: option.find(".option-value-input").val().trim(),
					isDefault: option.find(".option-default-check").is(":checked"),
				});
			});
		}

		changes.push(fieldData);
	});

	// Compare with original data
	if (changes.length !== CurrentManageLLMProviderData.userIntegrationFields.length) {
		hasChanges = true;
	} else {
		for (let i = 0; i < changes.length; i++) {
			const newField = changes[i];
			const oldField = CurrentManageLLMProviderData.userIntegrationFields[i];

			if (
				newField.id !== oldField.id ||
				newField.name !== oldField.name ||
				newField.type !== oldField.type ||
				newField.tooltip !== oldField.tooltip ||
				newField.placeholder !== oldField.placeholder ||
				newField.defaultValue !== oldField.defaultValue ||
				newField.required !== oldField.required ||
				newField.isEncrypted !== oldField.isEncrypted
			) {
				hasChanges = true;
				break;
			}

			if (newField.type === "select") {
				if (!oldField.options || newField.options.length !== oldField.options.length) {
					hasChanges = true;
					break;
				}

				for (let j = 0; j < newField.options.length; j++) {
					const newOption = newField.options[j];
					const oldOption = oldField.options[j];

					if (newOption.key !== oldOption.key || newOption.value !== oldOption.value || newOption.isDefault !== oldOption.isDefault) {
						hasChanges = true;
						break;
					}
				}
			}
		}
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function ValidateLLMProviderIntegrationFieldsTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Get all fields
	llmProviderIntegrationFieldsList.find(".integration-field").each(function (index) {
		const field = $(this);

		// Validate Field ID
		const fieldId = field.find(".field-id-input").val().trim();
		if (!fieldId) {
			validated = false;
			errors.push(`Field ${index + 1}: ID is required`);
			if (!onlyRemove) {
				field.find(".field-id-input").addClass("is-invalid");
			}
		} else {
			field.find(".field-id-input").removeClass("is-invalid");
		}

		// Validate Field Name
		const fieldName = field.find(".field-name-input").val().trim();
		if (!fieldName) {
			validated = false;
			errors.push(`Field ${index + 1}: Name is required`);
			if (!onlyRemove) {
				field.find(".field-name-input").addClass("is-invalid");
			}
		} else {
			field.find(".field-name-input").removeClass("is-invalid");
		}

		// Get field type for specific validations
		const fieldType = field.find(".field-type-select").val();

		// Validate Select Options
		if (fieldType === "select") {
			const options = field.find(".field-option");
			if (options.length === 0) {
				validated = false;
				errors.push(`Field ${index + 1}: Select type must have at least one option`);
			} else {
				let hasDefault = false;
				options.each(function (optIndex) {
					const option = $(this);
					const key = option.find(".option-key-input").val().trim();
					const value = option.find(".option-value-input").val().trim();

					if (!key || !value) {
						validated = false;
						errors.push(`Field ${index + 1}, Option ${optIndex + 1}: Key and Value are required`);
						if (!onlyRemove) {
							if (!key) option.find(".option-key-input").addClass("is-invalid");
							if (!value) option.find(".option-value-input").addClass("is-invalid");
						}
					} else {
						option.find(".option-key-input").removeClass("is-invalid");
						option.find(".option-value-input").removeClass("is-invalid");
					}

					if (option.find(".option-default-check").is(":checked")) {
						hasDefault = true;
					}
				});

				if (!hasDefault) {
					validated = false;
					errors.push(`Field ${index + 1}: Select type must have a default option selected`);
				}
			}
		}

		// Validate Default Value for non-select/models types
		if (fieldType !== "select" && fieldType !== "models") {
			const defaultValue = field.find(".field-default-value-input").val().trim();
			const isRequired = field.find(".field-required-check").is(":checked");

			if (isRequired && !defaultValue) {
				validated = false;
				errors.push(`Field ${index + 1}: Default value is required for required fields`);
				if (!onlyRemove) {
					field.find(".field-default-value-input").addClass("is-invalid");
				}
			} else {
				field.find(".field-default-value-input").removeClass("is-invalid");
			}

			// Additional validation for number type
			if (fieldType === "number" && defaultValue) {
				if (isNaN(defaultValue)) {
					validated = false;
					errors.push(`Field ${index + 1}: Default value must be a valid number`);
					if (!onlyRemove) {
						field.find(".field-default-value-input").addClass("is-invalid");
					}
				} else {
					field.find(".field-default-value-input").removeClass("is-invalid");
				}
			}
		}

		// Check for duplicate IDs
		const currentId = field.find(".field-id-input").val().trim();
		if (currentId) {
			const duplicateFields = llmProviderIntegrationFieldsList
				.find(".integration-field")
				.not(field)
				.filter(function () {
					return $(this).find(".field-id-input").val().trim() === currentId;
				});

			if (duplicateFields.length > 0) {
				validated = false;
				errors.push(`Field ${index + 1}: Duplicate Field ID "${currentId}"`);
				if (!onlyRemove) {
					field.find(".field-id-input").addClass("is-invalid");
				}
			}
		}
	});

	return {
		validated: validated,
		errors: errors,
	};
}

/** Initalizer **/

$(document).ready(() => {
	// Event Handlers

	llmProviderManagerGeneral.on("input change", "input, textarea, select", (event) => {
		if (CurrentManageLLMProviderType == null) return;

		CheckLLMProviderManageTabHasChanges(true);
		ValidateLLMProviderIntegrationFieldsTab(true);
	});

	llmProviderIntegrationsTab.on("input change", "input, textarea, select", (event) => {
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
					CurrentManageLLMProviderModelPromptLanguageNewData = saveResponse.data.promptTemplates;

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
		CurrentManageLLMProviderModelPromptLanguageNewData = null;

		ShowLLMProviderModelListTab();
	});

	addNewLLMProviderModelButton.on("click", (event) => {
		event.preventDefault();

		CurrentManageLLMProviderModelData = CreateDefaultLLMProviderModelObject();
		CurrentManageLLMProviderModelPromptLanguageNewData = CreateDefaultLLMProviderModelObject().promptTemplates;

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
		fillIntegrationFields();

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

		CurrentManageLLMProviderModelPromptLanguageNewData = {};
		Object.keys(CurrentManageLLMProviderModelData.promptTemplates).forEach((languageId) => {
			CurrentManageLLMProviderModelPromptLanguageNewData[languageId] = CurrentManageLLMProviderModelData.promptTemplates[languageId];
		});

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

	// Add new integration field
	addNewLLMProviderIntegrationFieldButton.on("click", (event) => {
		event.preventDefault();
		llmProviderIntegrationFieldsList.find(".text-center").remove(); // Remove "no fields" message
		llmProviderIntegrationFieldsList.append($(createLLMProviderIntegrationFieldElement()));

		CheckLLMProviderManageTabHasChanges(true);
	});

	// Handle field type changes
	llmProviderIntegrationsTab.on("change", ".field-type-select", function () {
		const field = $(this).closest(".integration-field");
		const optionsContainer = field.find(".field-options-container");
		const defaultValueInput = field.find(".field-default-value-input");

		const selectedType = $(this).val();
		if (selectedType === "select") {
			optionsContainer.removeClass("d-none");
			defaultValueInput.prop("disabled", true).val("");
		} else if (selectedType === "models") {
			optionsContainer.addClass("d-none");
			defaultValueInput.prop("disabled", true).val("");
		} else {
			optionsContainer.addClass("d-none");
			defaultValueInput.prop("disabled", false);
		}
	});

	// Handle field removal
	llmProviderIntegrationsTab.on("click", ".remove-field-button", function () {
		$(this).closest(".integration-field").remove();
		if (llmProviderIntegrationFieldsList.children().length === 0) {
			fillIntegrationFields(); // This will add the "no fields" message
		}

		CheckLLMProviderManageTabHasChanges(true);
	});

	var checkManageLLMProviderModelLanguageDropdownInterval = setInterval(() => {
		if (manageLLMProviderModelLanguageDropdown != null) {
			manageLLMProviderModelPromptTemplateInput.on("input change", (event) => {
				let currentLanguage = manageLLMProviderModelLanguageDropdown.getSelectedLanguage();

				let value = manageLLMProviderModelPromptTemplateInput.val();

				CurrentManageLLMProviderModelPromptLanguageNewData[currentLanguage.id] = value;

				let valueIsInComplete = !value || value == "" || value.trim() == "";
				manageLLMProviderModelLanguageDropdown.setLanguageStatus(currentLanguage.id, valueIsInComplete ? "incomplete" : "complete");
			});

			manageLLMProviderModelLanguageDropdown.onLanguageChange((language) => {
				let promptTemplate = CurrentManageLLMProviderModelPromptLanguageNewData[language.id];
				manageLLMProviderModelPromptTemplateInput.val(promptTemplate);
			});

			clearInterval(checkManageLLMProviderModelLanguageDropdownInterval);
		}
	}, 100);

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
