/** Dynamic Variables **/
let CurrentSTTProviderType = null;
let CurrentSTTProviderData = null;

let CurrentSTTProviderModelType = null;
let CurrentSTTProviderModelData = null;

let IsSavingSTTProviderTab = false;

/** Elements Variables **/
const STTProviderTab = $("#stt-provider-tab");

const sttProviderInnerTab = STTProviderTab.find("#stt-provider-inner-tab");
const sttProviderManageBreadcrumb = STTProviderTab.find("#stt-provider-manage-breadcrumb");

const switchBackToSTTProviderListTabFromManageTab = sttProviderManageBreadcrumb.find("#switchBackToSTTProviderListTabFromManageTab");
const currentManageSTTProviderName = sttProviderManageBreadcrumb.find("#currentManageSTTProviderName");

const STTProviderListTableTab = STTProviderTab.find("#sttProviderListTableTab");
const STTProviderListTable = STTProviderListTableTab.find("#sttProviderListTable");

const STTProviderManageTab = STTProviderTab.find("#sttProviderManageTab");
const sttProviderModelListTable = STTProviderManageTab.find("#sttProviderModelListTable");
const addNewSTTProviderModelButton = STTProviderManageTab.find("#addNewSTTProviderModelButton");

const sttProviderManagerInnerTabContainer = STTProviderManageTab.find("#stt-provider-manager-inner-tab-container");
const sttProviderManagerInnerTab = sttProviderManagerInnerTabContainer.find("#stt-provider-manager-inner-tab");
const sttProviderModelManagerBreadcrumb = STTProviderTab.find("#stt-provider-model-manager-breadcrumb");
const saveManageSTTProviderModelButton = sttProviderModelManagerBreadcrumb.find("#saveManageSTTProviderModelButton");
const saveManageSTTProviderButton = sttProviderManagerInnerTabContainer.find("#saveManageSTTProviderButton");

const currentManageModelSTTProviderName = sttProviderModelManagerBreadcrumb.find("#currentManageModelSTTProviderName");
const currentManageSTTProviderModelName = sttProviderModelManagerBreadcrumb.find("#currentManageSTTProviderModelName");
const switchBackToSTTProviderManagerModelsListTabFromModelTab = sttProviderModelManagerBreadcrumb.find("#switchBackToSTTProviderManagerModelsListTabFromModelTab");

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
	sttProviderInnerTab.removeClass("show");

	setTimeout(() => {
		STTProviderListTableTab.addClass("d-none");
		sttProviderInnerTab.addClass("d-none");

		STTProviderManageTab.removeClass("d-none");
		sttProviderManageBreadcrumb.removeClass("d-none");

		setTimeout(() => {
			STTProviderManageTab.addClass("show");
			sttProviderManageBreadcrumb.addClass("show");
		}, 10);
	}, 300);
}

function ShowSTTProviderListTab() {
	STTProviderManageTab.removeClass("show");
	sttProviderManageBreadcrumb.removeClass("show");

	setTimeout(() => {
		STTProviderManageTab.addClass("d-none");
		sttProviderManageBreadcrumb.addClass("d-none");

		STTProviderListTableTab.removeClass("d-none");
		sttProviderInnerTab.removeClass("d-none");

		setTimeout(() => {
			STTProviderListTableTab.addClass("show");
			sttProviderInnerTab.addClass("show");
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

	// Fill integration fields
	fillSTTProviderIntegrationFields();
}

function CheckSTTProviderManageTabHasChanges(enableDisableButton = true) {
	function fieldsAreEqual(field1, field2) {
		if (
			field1.id !== field2.id ||
			field1.name !== field2.name ||
			field1.type !== field2.type ||
			field1.tooltip !== field2.tooltip ||
			field1.placeholder !== field2.placeholder ||
			field1.defaultValue !== field2.defaultValue ||
			field1.required !== field2.required ||
			field1.isEncrypted !== field2.isEncrypted
		) {
			return false;
		}

		// Compare options if type is select
		if (field1.type === "select") {
			if (!field1.options || !field2.options || field1.options.length !== field2.options.length) {
				return false;
			}

			for (let i = 0; i < field1.options.length; i++) {
				const option1 = field1.options[i];
				const option2 = field2.options[i];

				if (option1.key !== option2.key || option1.value !== option2.value || option1.isDefault !== option2.isDefault) {
					return false;
				}
			}
		}

		return true;
	}

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

	// Check integration fields
	const integrationFieldsData = [];
	sttProviderIntegrationFieldsList.find(".integration-field").each(function () {
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

		// Handle options for select type
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

		integrationFieldsData.push(fieldData);
	});

	// Compare integration fields
	if (integrationFieldsData.length !== CurrentSTTProviderData.userIntegrationFields.length) {
		hasChanges = true;
	} else {
		for (let i = 0; i < integrationFieldsData.length; i++) {
			const newField = integrationFieldsData[i];
			const oldField = CurrentSTTProviderData.userIntegrationFields[i];

			if (!fieldsAreEqual(newField, oldField)) {
				hasChanges = true;
				break;
			}
		}
	}

	if (hasChanges) {
		changes.userIntegrationFields = integrationFieldsData;
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
	const integrationValidation = ValidateSTTProviderIntegrationFieldsTab(onlyRemove);
	if (!integrationValidation.validated) {
		validated = false;
		errors.push(...integrationValidation.errors);
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
	sttProviderManagerInnerTabContainer.removeClass("show");
	sttProviderManageBreadcrumb.removeClass("show");

	setTimeout(() => {
		sttProviderManagerModelsListTab.addClass("d-none");
		sttProviderManagerInnerTabContainer.addClass("d-none");
		sttProviderManageBreadcrumb.addClass("d-none");

		sttProviderManagerModelManageTab.removeClass("d-none");
		sttProviderModelManagerBreadcrumb.removeClass("d-none");

		setTimeout(() => {
			sttProviderManagerModelManageTab.addClass("show");
			sttProviderModelManagerBreadcrumb.addClass("show");
		}, 10);
	}, 300);
}

function ShowSTTProviderModelListTab() {
	sttProviderManagerModelManageTab.removeClass("show");
	sttProviderModelManagerBreadcrumb.removeClass("show");

	setTimeout(() => {
		sttProviderManagerModelManageTab.addClass("d-none");
		sttProviderModelManagerBreadcrumb.addClass("d-none");

		sttProviderManagerModelsListTab.removeClass("d-none");
		sttProviderManagerInnerTabContainer.removeClass("d-none");
		sttProviderManageBreadcrumb.removeClass("d-none");

		setTimeout(() => {
			sttProviderManagerModelsListTab.addClass("show");
			sttProviderManagerInnerTabContainer.addClass("show");
			sttProviderManageBreadcrumb.addClass("show");
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

/** Integration Fields Management Functions **/
function createSTTProviderIntegrationFieldElement(fieldData = null) {
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
                        ${fieldData?.options?.map((option) => createSTTIntegrationFieldOptionElement(option)).join("") || ""}
                    </div>
                    <button type="button" class="btn btn-outline-primary btn-sm mt-2 add-option-button">
                        <i class="fa-regular fa-plus"></i> Add Option
                    </button>
                </div>
            </div>
        </div>
    `;
}

function createSTTIntegrationFieldOptionElement(optionData = null) {
	return `
        <div class="field-option mb-2">
            <div class="row">
                <div class="col-5">
                    <input type="text" class="form-control option-key-input" 
                        placeholder="Key" value="${optionData?.key || ""}">
                </div>
                <div class="col-5">
                    <input type="text" class="form-control option-value-input" 
                        placeholder="Value" value="${optionData?.value || ""}">
                </div>
                <div class="col-1">
                    <div class="form-check">
                        <input class="form-check-input option-default-check" type="checkbox"
                            ${optionData?.isDefault ? "checked" : ""}>
                    </div>
                </div>
                <div class="col-1">
                    <button type="button" class="btn btn-danger btn-sm remove-option-button">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </div>
            </div>
        </div>
    `;
}

function fillSTTProviderIntegrationFields() {
	sttProviderIntegrationFieldsList.empty();

	if (CurrentSTTProviderData.userIntegrationFields.length === 0) {
		sttProviderIntegrationFieldsList.append(`
            <div class="text-center p-5">
                <p class="text-muted mb-0">No integration fields defined</p>
            </div>
        `);
		return;
	}

	CurrentSTTProviderData.userIntegrationFields.forEach((field) => {
		sttProviderIntegrationFieldsList.append($(createSTTProviderIntegrationFieldElement(field)));
	});
}

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

function ValidateSTTProviderIntegrationFieldsTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Get all fields
	sttProviderIntegrationFieldsList.find(".integration-field").each(function (index) {
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
			const duplicateFields = sttProviderIntegrationFieldsList
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

		// Validate tooltip and placeholder (optional fields)
		const tooltip = field.find(".field-tooltip-input").val().trim();
		const placeholder = field.find(".field-placeholder-input").val().trim();

		// Clear any previous invalid states for these fields
		field.find(".field-tooltip-input").removeClass("is-invalid");
		field.find(".field-placeholder-input").removeClass("is-invalid");
	});

	return {
		validated: validated,
		errors: errors,
	};
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

	// Add new integration field
	addNewSTTProviderIntegrationFieldButton.on("click", (event) => {
		event.preventDefault();
		sttProviderIntegrationFieldsList.find(".text-center").remove();
		sttProviderIntegrationFieldsList.append($(createSTTProviderIntegrationFieldElement()));
		CheckSTTProviderManageTabHasChanges(true);
	});

	// Handle field type changes
	sttProviderIntegrationsTab.on("change", ".field-type-select", function () {
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
	sttProviderIntegrationsTab.on("click", ".remove-field-button", function () {
		$(this).closest(".integration-field").remove();
		if (sttProviderIntegrationFieldsList.children().length === 0) {
			fillSTTProviderIntegrationFields();
		}
		CheckSTTProviderManageTabHasChanges(true);
	});

	// Handle option management
	sttProviderIntegrationsTab.on("click", ".add-option-button", function () {
		$(this).siblings(".field-options-list").append($(createSTTIntegrationFieldOptionElement()));
		CheckSTTProviderManageTabHasChanges(true);
	});

	sttProviderIntegrationsTab.on("click", ".remove-option-button", function () {
		$(this).closest(".field-option").remove();
		CheckSTTProviderManageTabHasChanges(true);
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
