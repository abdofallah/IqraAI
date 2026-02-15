/** Dynamic Variables **/
let CurrentManageEmbeddingProviderType = null;
let CurrentManageEmbeddingProviderData = null;

let CurrentManageEmbeddingProviderModelType = null;
let CurrentManageEmbeddingProviderModelData = null;

let IsSavingEmbeddingProviderTab = false;

/** Elements Variables **/
const EmbeddingProviderTab = $("#embedding-provider-tab");

// Header
const embeddingProviderInnerHeader = EmbeddingProviderTab.find("#embedding-provider-inner-header");

// Manager Header
const embeddingProviderManageInnerHeader = EmbeddingProviderTab.find("#embedding-provider-manager-inner-header");
const switchBackToEmbeddingProviderListTabFromManageTab = embeddingProviderManageInnerHeader.find("#switchBackToEmbeddingProviderListTabFromManageTab");
const currentManageEmbeddingProviderName = embeddingProviderManageInnerHeader.find("#currentManageEmbeddingProviderName");
const saveManageEmbeddingProviderButton = embeddingProviderManageInnerHeader.find("#saveManageEmbeddingProviderButton");

// Manager Model Header
const embeddingProviderModelManagerInnerHeader = EmbeddingProviderTab.find("#embedding-provider-model-manager-inner-header");
const currentManageModelEmbeddingProviderName = embeddingProviderModelManagerInnerHeader.find("#currentManageModelEmbeddingProviderName");
const currentManageEmbeddingProviderModelName = embeddingProviderModelManagerInnerHeader.find("#currentManageEmbeddingProviderModelName");
const switchBackToEmbeddingProviderManagerModelsListTabFromModelTab = embeddingProviderModelManagerInnerHeader.find("#switchBackToEmbeddingProviderManagerModelsListTabFromModelTab");
const saveManageEmbeddingProviderModelButton = embeddingProviderModelManagerInnerHeader.find("#saveManageEmbeddingProviderModelButton");

// List Tab
const EmbeddingProviderListTableTab = EmbeddingProviderTab.find("#embeddingProviderListTableTab");
const EmbeddingProviderListTable = EmbeddingProviderListTableTab.find("#embeddingProviderListTable");

// Manager Tab
const EmbeddingProviderManageTab = EmbeddingProviderTab.find("#embeddingProviderManageTab");
const embeddingProviderModelListTable = EmbeddingProviderManageTab.find("#embeddingProviderModelListTable");
const addNewEmbeddingProviderModelButton = EmbeddingProviderManageTab.find("#addNewEmbeddingProviderModelButton");

const embeddingProviderManagerGeneral = EmbeddingProviderManageTab.find("#embedding-provider-manager-general");
const manageEmbeddingProviderIdInput = embeddingProviderManagerGeneral.find("#manageEmbeddingProviderIdInput");
const manageEmbeddingProviderDisabledInput = embeddingProviderManagerGeneral.find("#manageEmbeddingProviderDisabledInput");
const manageEmbeddingProviderIntegrationSelect = embeddingProviderManagerGeneral.find("#manageEmbeddingProviderIntegrationSelect");

const embeddingProviderManagerModelsListTab = EmbeddingProviderManageTab.find("#embeddingProviderManagerModelsListTab");
const embeddingProviderManagerModelManageTab = EmbeddingProviderManageTab.find("#embeddingProviderManagerModelManageTab");
const embeddingProviderManagerGeneralTab = EmbeddingProviderManageTab.find("#embedding-provider-manager-general-tab");
const embeddingProviderModelManagerGeneralTab = embeddingProviderManagerModelManageTab.find("#embedding-provider-model-manager-general-tab");

// Model Manage Tab Inputs
const manageEmbeddingProviderModelIdInput = embeddingProviderManagerModelManageTab.find("#manageEmbeddingProviderModelIdInput");
const manageEmbeddingProviderModelNameInput = embeddingProviderManagerModelManageTab.find("#manageEmbeddingProviderModelNameInput");
const manageEmbeddingProviderModelPriceInput = embeddingProviderManagerModelManageTab.find("#manageEmbeddingProviderModelPriceInput");
const manageEmbeddingProviderModelPriceTokensInput = embeddingProviderManagerModelManageTab.find("#manageEmbeddingProviderModelPriceTokensInput");
const manageEmbeddingProviderModelDimensionsInput = embeddingProviderManagerModelManageTab.find("#manageEmbeddingProviderModelDimensionsInput");
const manageEmbeddingProviderModelDisabledInput = embeddingProviderManagerModelManageTab.find("#manageEmbeddingProviderModelDisabledInput");

// Integration Variables
const embeddingProviderIntegrationsTab = EmbeddingProviderTab.find("#embedding-provider-manager-integrations");
const addNewEmbeddingProviderIntegrationFieldButton = embeddingProviderIntegrationsTab.find("#addNewEmbeddingProviderIntegrationFieldButton");
const embeddingProviderIntegrationFieldsList = embeddingProviderIntegrationsTab.find("#embeddingProviderIntegrationFieldsList");

/** API Functions **/
function FetchEmbeddingProvidersFromAPI(page, pageSize, successCallback, errorCallback) {
    $.ajax({
        url: '/app/admin/embeddingproviders',
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

function SaveEmbeddingProviderData(formData, successCallback, errorCallback) {
    $.ajax({
        type: "POST",
        url: "/app/admin/embeddingproviders/save",
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

function SaveEmbeddingProviderModelData(formData, successCallback, errorCallback) {
    $.ajax({
        type: "POST",
        url: "/app/admin/embeddingproviders/model/save",
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

/** Generic UI & Data Functions **/
function CreateEmbeddingProviderListTableElement(providerData) {
    let disabledData = providerData.disabledAt == null ? "-" : `<span class="badge bg-danger">${providerData.disabledAt}</span>`;
    return $(`
        <tr>
            <td>${providerData.id.value}</td>
            <td>${providerData.id.name}</td>
            <td>${disabledData}</td>
            <td>${providerData.models.length}</td>
            <td>
                <button class="btn btn-info btn-sm" provider-id="${providerData.id.value}" button-type="edit-embedding-provider">
                    <i class="fa-regular fa-eye"></i>
                </button>
            </td>
        </tr>`);
}

function ResetAndEmptyEmbeddingProvidersManageTab() {
    manageEmbeddingProviderIdInput.val("");
    manageEmbeddingProviderDisabledInput.prop("checked", false).change();
    embeddingProviderModelListTable.find("tbody").empty();
    embeddingProviderIntegrationFieldsList.empty();
    embeddingProviderManagerGeneralTab.click();
}

function ShowEmbeddingProviderManageTab() {
    EmbeddingProviderListTableTab.removeClass("show");
    embeddingProviderInnerHeader.removeClass("show");

    setTimeout(() => {
        EmbeddingProviderListTableTab.addClass("d-none");
        embeddingProviderInnerHeader.addClass("d-none");

        EmbeddingProviderManageTab.removeClass("d-none");
        embeddingProviderManageInnerHeader.removeClass("d-none");
        setTimeout(() => {
            EmbeddingProviderManageTab.addClass("show");
            embeddingProviderManageInnerHeader.addClass("show");

            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function ShowEmbeddingProviderListTab() {
    EmbeddingProviderManageTab.removeClass("show");
    embeddingProviderManageInnerHeader.removeClass("show");

    setTimeout(() => {
        EmbeddingProviderManageTab.addClass("d-none");
        embeddingProviderManageInnerHeader.addClass("d-none");

        EmbeddingProviderListTableTab.removeClass("d-none");
        embeddingProviderInnerHeader.removeClass("d-none");
        setTimeout(() => {
            EmbeddingProviderListTableTab.addClass("show");
            embeddingProviderInnerHeader.addClass("show");

            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function CreateEmbeddingProviderModelListTableElement(modelData) {
    let disabledData = modelData.disabledAt == null ? "-" : `<span class="badge bg-danger">${modelData.disabledAt}</span>`;
    return $(`<tr model-id="${modelData.id}">
        <td>${modelData.id}</td>
        <td>${modelData.name}</td>
        <td>${disabledData}</td>
        <td>
            <button class="btn btn-info btn-sm" model-id="${modelData.id}" button-type="edit-embedding-provider-model">
                <i class="fa-regular fa-eye"></i>
            </button>
        </td>
    </tr>`);
}

function FillEmbeddingProviderManageTab(providerData) {
    manageEmbeddingProviderIdInput.val(providerData.id.name);
    manageEmbeddingProviderDisabledInput.prop("checked", providerData.disabledAt != null);
    fillEmbeddingProviderIntegrationSelect();
    embeddingProviderModelListTable.find("tbody").empty();
    if (providerData.models.length > 0) {
        providerData.models.forEach((modelData) => {
            embeddingProviderModelListTable.find("tbody").append(CreateEmbeddingProviderModelListTableElement(modelData));
        });
    } else {
        embeddingProviderModelListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="4">No models</td></tr>');
    }
}

function ShowEmbeddingProviderModelManageTab() {
    embeddingProviderManagerModelsListTab.removeClass("show");
    embeddingProviderManageInnerHeader.removeClass("show");

    setTimeout(() => {
        embeddingProviderManagerModelsListTab.addClass("d-none");
        embeddingProviderManageInnerHeader.addClass("d-none");

        embeddingProviderManagerModelManageTab.removeClass("d-none");
        embeddingProviderModelManagerInnerHeader.removeClass("d-none");
        setTimeout(() => {
            embeddingProviderManagerModelManageTab.addClass("show");
            embeddingProviderModelManagerInnerHeader.addClass("show");

            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function ShowEmbeddingProviderModelListTab() {
    embeddingProviderManagerModelManageTab.removeClass("show");
    embeddingProviderModelManagerInnerHeader.removeClass("show");

    setTimeout(() => {
        embeddingProviderManagerModelManageTab.addClass("d-none");
        embeddingProviderModelManagerInnerHeader.addClass("d-none");

        embeddingProviderManagerModelsListTab.removeClass("d-none");
        embeddingProviderManageInnerHeader.removeClass("d-none");
        setTimeout(() => {
            embeddingProviderManagerModelsListTab.addClass("show");
            embeddingProviderManageInnerHeader.addClass("show");

            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function CheckEmbeddingProviderManageTabHasChanges(enableDisableButton = true) {
    let changes = {};
    let hasChanges = false;

    // Check disabled state
    changes.disabled = manageEmbeddingProviderDisabledInput.prop("checked");
    if (changes.disabled === (CurrentManageEmbeddingProviderData.disabledAt == null)) {
        hasChanges = true;
    }

    // Check integration selection
    changes.integrationId = manageEmbeddingProviderIntegrationSelect.val();
    if (changes.integrationId !== CurrentManageEmbeddingProviderData.integrationId) {
        hasChanges = true;
    }

    // Check integration fields
    const integrationFieldsChanges = CheckEmbeddingProviderIntegrationFieldsTabHasChanges();
    if (integrationFieldsChanges.hasChanges) {
        hasChanges = true;
        changes.userIntegrationFields = integrationFieldsChanges.changes;
    }

    if (enableDisableButton) {
        saveManageEmbeddingProviderButton.prop("disabled", !hasChanges);
    }

    return {
        hasChanges: hasChanges,
        changes: changes,
    };
}

function ValidateEmbeddingProviderManageTab(onlyRemove = true) {
    const errors = [];
    let validated = true;

    // General Tab
    const selectedIntegration = manageEmbeddingProviderIntegrationSelect.val();
    if (!selectedIntegration) {
        validated = false;
        errors.push("Integration selection is required");
        if (!onlyRemove) {
            manageEmbeddingProviderIntegrationSelect.addClass("is-invalid");
        }
    } else {
        manageEmbeddingProviderIntegrationSelect.removeClass("is-invalid");
    }

    // Integration Tab
    const integrationValidation = ValidateEmbeddingProviderIntegrationFieldsTab(onlyRemove);
    if (!integrationValidation.validated) {
        validated = false;
        errors.push(...integrationValidation.errors);
    }

    return {
        validated: validated,
        errors: errors,
    };
}

function fillEmbeddingProviderIntegrationSelect() {
    manageEmbeddingProviderIntegrationSelect.empty().append('<option value="">Select Integration</option>');
    // CRITICAL CHANGE: Filter for "Embedding" type
    const embeddingIntegrations = CurrentIntegrationsList.filter((integration) => integration.type.includes("Embedding"));
    embeddingIntegrations.forEach((integration) => {
        manageEmbeddingProviderIntegrationSelect.append(
            `<option value="${integration.id}" ${CurrentManageEmbeddingProviderData.integrationId === integration.id ? "selected" : ""}>
                ${integration.name}
            </option>`
        );
    });
}

function createEmbeddingProviderIntegrationFieldElement(fieldData = null) {
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
                    <div class="col-md-6 mb-3"><label class="form-label">Field ID</label><input type="text" class="form-control field-id-input" placeholder="Field ID" value="${fieldData?.id || ""}"></div>
                    <div class="col-md-6 mb-3"><label class="form-label">Name</label><input type="text" class="form-control field-name-input" placeholder="Field Name" value="${fieldData?.name || ""}"></div>
                </div>
                <div class="row">
                    <div class="col-md-6 mb-3"><label class="form-label">Type</label>
                    <select class="form-select field-type-select">
                        <option value="text" ${fieldData?.type === "text" ? "selected" : ""}>Text</option>
                        <option value="number" ${fieldData?.type === "number" ? "selected" : ""}>Number</option>
                        <option value="double_number" ${fieldData?.type === "double_number" ? "selected" : ""}>Double Number</option>
                        <option value="select" ${fieldData?.type === "select" ? "selected" : ""}>Select</option>
                        <option value="models" ${fieldData?.type === "models" ? "selected" : ""}>Models</option>
                        <option value="model_vector_dimensions" ${fieldData?.type === "model_vector_dimensions" ? "selected" : ""}>Model Vector Dimensions</option>
                    </select>
                </div>
                <div class="col-md-6 mb-3"><label class="form-label">Tooltip</label><input type="text" class="form-control field-tooltip-input" placeholder="Field Tooltip" value="${fieldData?.tooltip || ""}"></div>
                </div>
                <div class="row">
                    <div class="col-md-6 mb-3"><label class="form-label">Placeholder</label><input type="text" class="form-control field-placeholder-input" placeholder="Field Placeholder" value="${fieldData?.placeholder || ""}"></div>
                    <div class="col-md-6 mb-3"><label class="form-label">Default Value</label><input type="text" class="form-control field-default-value-input" placeholder="Default Value" value="${fieldData?.defaultValue || ""}" ${fieldData?.type === "select" || fieldData?.type === "model_vector_dimensions" || fieldData?.type === "models" ? "disabled" : ""}></div>
                </div>
                <div class="row">
                    <div class="col-md-6"><div class="form-check"><input class="form-check-input field-required-check" type="checkbox" ${fieldData?.required ? "checked" : ""}><label class="form-check-label">Required</label></div></div>
                    <div class="col-md-6"><div class="form-check"><input class="form-check-input field-encrypted-check" type="checkbox" ${fieldData?.isEncrypted ? "checked" : ""}><label class="form-check-label">Encrypted</label></div></div>
                </div>
                <div class="field-options-container ${fieldData?.type === "select" ? "" : "d-none"} mt-3">
                    <label class="form-label">Options</label>
                    <div class="field-options-list">${fieldData?.options?.map((option) => createEmbeddingIntegrationFieldOptionElement(option)).join("") || ""}</div>
                    <button type="button" class="btn btn-outline-primary btn-sm mt-2 add-option-button"><i class="fa-regular fa-plus"></i> Add Option</button>
                </div>
            </div>
        </div>`;
}

function createEmbeddingIntegrationFieldOptionElement(optionData = null) {
    return `
        <div class="input-group mb-2 field-option">
            <input type="text" class="form-control option-key-input" placeholder="Option Key" value="${optionData?.key || ""}">
            <input type="text" class="form-control option-value-input" placeholder="Option Value" value="${optionData?.value || ""}">
            <div class="input-group-text"><input class="form-check-input option-default-check mt-0" type="radio" name="defaultOption" ${optionData?.isDefault ? "checked" : ""}><label class="ms-2">Default?</label></div>
            <button class="btn btn-outline-danger remove-option-button" type="button"><i class="fa-regular fa-trash"></i></button>
        </div>`;
}

function fillEmbeddingProviderIntegrationFields() {
    embeddingProviderIntegrationFieldsList.empty();
    if (CurrentManageEmbeddingProviderData.userIntegrationFields.length === 0) {
        embeddingProviderIntegrationFieldsList.append(`<div class="text-center p-5"><p class="text-muted mb-0">No integration fields defined</p></div>`);
        return;
    }
    CurrentManageEmbeddingProviderData.userIntegrationFields.forEach((field) => {
        embeddingProviderIntegrationFieldsList.append($(createEmbeddingProviderIntegrationFieldElement(field)));
    });
}

function CheckEmbeddingProviderIntegrationFieldsTabHasChanges() {
    let changes = [];
    let hasChanges = false;
    embeddingProviderIntegrationFieldsList.find(".integration-field").each(function () {
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
                    isDefault: option.find(".option-default-check").is(":checked")
                });
            });
        }
        changes.push(fieldData);
    });
    if (changes.length !== CurrentManageEmbeddingProviderData.userIntegrationFields.length) {
        hasChanges = true;
    } else {
        for (let i = 0; i < changes.length; i++) {
            const newField = changes[i];
            const oldField = CurrentManageEmbeddingProviderData.userIntegrationFields[i];
            if (newField.id !== oldField.id || newField.name !== oldField.name || newField.type !== oldField.type || newField.tooltip !== oldField.tooltip || newField.placeholder !== oldField.placeholder || newField.defaultValue !== oldField.defaultValue || newField.required !== oldField.required || newField.isEncrypted !== oldField.isEncrypted) {
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
        changes: changes
    };
}

function ValidateEmbeddingProviderIntegrationFieldsTab(onlyRemove = true) {
    const errors = [];
    let validated = true;
    embeddingProviderIntegrationFieldsList.find(".integration-field").each(function (index) {
        const field = $(this);
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
        const fieldType = field.find(".field-type-select").val();
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
        if (fieldType !== "select" && fieldType !== "models" && fieldType !== "model_vector_dimensions") {
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
        const currentId = field.find(".field-id-input").val().trim();
        if (currentId) {
            const duplicateFields = embeddingProviderIntegrationFieldsList.find(".integration-field").not(field).filter(function () {
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
        errors: errors
    };
}

/** Model Specific Functions **/

function CreateDefaultEmbeddingProviderModelObject() {
    return {
        id: "",
        name: "",
        disabledAt: true,
        price: "",
        priceTokenUnit: "",
        availableVectorDimensions: [],
    };
}

function ResetAndEmptyEmbeddingProviderModelManageTab() {
    embeddingProviderManagerModelManageTab.find("input").val("").change();
    embeddingProviderManagerModelManageTab.find(".is-invalid").removeClass("is-invalid");
    manageEmbeddingProviderModelDisabledInput.prop("checked", true);
    saveManageEmbeddingProviderModelButton.prop("disabled", true);
    embeddingProviderModelManagerGeneralTab.click();
}

function FillEmbeddingProviderModelManageTab(modelData) {
    manageEmbeddingProviderModelIdInput.val(modelData.id);
    manageEmbeddingProviderModelNameInput.val(modelData.name);
    manageEmbeddingProviderModelPriceInput.val(modelData.price);
    manageEmbeddingProviderModelPriceTokensInput.val(modelData.priceTokenUnit);
    manageEmbeddingProviderModelDimensionsInput.val((modelData.availableVectorDimensions || []).join(', '));
    manageEmbeddingProviderModelDisabledInput.prop("checked", modelData.disabledAt != null);
}

function CheckEmbeddingProviderModelManageTabHasChanges(enableDisableButton = true) {
    let changes = {};
    let hasChanges = false;

    changes.id = manageEmbeddingProviderModelIdInput.val();
    if (CurrentManageEmbeddingProviderModelData.id != changes.id) hasChanges = true;

    changes.name = manageEmbeddingProviderModelNameInput.val();
    if (CurrentManageEmbeddingProviderModelData.name != changes.name) hasChanges = true;

    changes.disabled = manageEmbeddingProviderModelDisabledInput.prop("checked");
    if (changes.disabled == (CurrentManageEmbeddingProviderModelData.disabledAt == null)) hasChanges = true;

    changes.price = manageEmbeddingProviderModelPriceInput.val();
    if (CurrentManageEmbeddingProviderModelData.price != changes.price) hasChanges = true;

    changes.priceTokenUnit = manageEmbeddingProviderModelPriceTokensInput.val();
    if (CurrentManageEmbeddingProviderModelData.priceTokenUnit != changes.priceTokenUnit) hasChanges = true;

    const newDims = manageEmbeddingProviderModelDimensionsInput.val().split(',').map(d => parseInt(d.trim())).filter(n => !isNaN(n)).sort((a, b) => a - b);
    const oldDims = (CurrentManageEmbeddingProviderModelData.availableVectorDimensions || []).sort((a, b) => a - b);
    changes.availableVectorDimensions = newDims;
    if (JSON.stringify(newDims) !== JSON.stringify(oldDims)) hasChanges = true;

    if (enableDisableButton) {
        saveManageEmbeddingProviderModelButton.prop("disabled", !hasChanges);
    }

    return {
        hasChanges: hasChanges,
        changes: changes
    };
}

function ValidateEmbeddingProviderModelManageTabFields(onlyRemove = false) {
    let errors = [];
    let validated = true;

    let modelId = manageEmbeddingProviderModelIdInput.val().trim();
    if (modelId === "") {
        validated = false;
        errors.push("Model id is required.");
        if (!onlyRemove) manageEmbeddingProviderModelIdInput.addClass("is-invalid");
    } else {
        manageEmbeddingProviderModelIdInput.removeClass("is-invalid");
    }

    let modelName = manageEmbeddingProviderModelNameInput.val().trim();
    if (modelName === "") {
        validated = false;
        errors.push("Model name is required.");
        if (!onlyRemove) manageEmbeddingProviderModelNameInput.addClass("is-invalid");
    } else {
        manageEmbeddingProviderModelNameInput.removeClass("is-invalid");
    }

    let modelPrice = manageEmbeddingProviderModelPriceInput.val().trim();
    if (modelPrice === "" || isNaN(modelPrice)) {
        validated = false;
        errors.push("Price is required and must be a number.");
        if (!onlyRemove) manageEmbeddingProviderModelPriceInput.addClass("is-invalid");
    } else {
        manageEmbeddingProviderModelPriceInput.removeClass("is-invalid");
    }

    let modelPriceTokenUnit = manageEmbeddingProviderModelPriceTokensInput.val().trim();
    if (modelPriceTokenUnit === "" || isNaN(modelPriceTokenUnit) || !Number.isInteger(Number(modelPriceTokenUnit))) {
        validated = false;
        errors.push("Price token unit is required and must be an integer.");
        if (!onlyRemove) manageEmbeddingProviderModelPriceTokensInput.addClass("is-invalid");
    } else {
        manageEmbeddingProviderModelPriceTokensInput.removeClass("is-invalid");
    }

    let dimsInput = manageEmbeddingProviderModelDimensionsInput.val().trim();
    if (dimsInput !== "") {
        const areAllNumbers = dimsInput.split(',').every(d => d.trim() !== '' && !isNaN(parseInt(d.trim())) && Number.isInteger(Number(d.trim())));
        if (!areAllNumbers) {
            validated = false;
            errors.push("Available Vector Dimensions must be a comma-separated list of valid integers.");
            if (!onlyRemove) manageEmbeddingProviderModelDimensionsInput.addClass("is-invalid");
        } else {
            manageEmbeddingProviderModelDimensionsInput.removeClass("is-invalid");
        }
    } else {
        manageEmbeddingProviderModelDimensionsInput.removeClass("is-invalid");
    }

    return {
        validated: validated,
        errors: errors
    };
}

/** Initalizer **/
$(document).ready(() => {
    // Event Handlers
    embeddingProviderManagerGeneral.on("input change", "input, select", () => {
        if (CurrentManageEmbeddingProviderType == null) return;
        CheckEmbeddingProviderManageTabHasChanges(true);
        ValidateEmbeddingProviderManageTab(true);
    });

    embeddingProviderIntegrationsTab.on("input change", "input, select", () => {
        if (CurrentManageEmbeddingProviderType == null) return;
        CheckEmbeddingProviderManageTabHasChanges(true);
        ValidateEmbeddingProviderIntegrationFieldsTab(true);
    });

    saveManageEmbeddingProviderButton.on("click", (event) => {
        event.preventDefault();
        if (IsSavingEmbeddingProviderTab) return;

        const validationResult = ValidateEmbeddingProviderManageTab(false);
        if (!validationResult.validated) {
            AlertManager.createAlert({
                type: "danger",
                message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
                timeout: 6000,
            });
            return;
        }

        const changes = CheckEmbeddingProviderManageTabHasChanges(false);
        if (!changes.hasChanges) return;

        saveManageEmbeddingProviderButton.prop("disabled", true);
        IsSavingEmbeddingProviderTab = true;

        const formData = new FormData();
        formData.append("changes", JSON.stringify(changes.changes));
        formData.append("providerId", CurrentManageEmbeddingProviderData.id.value);

        SaveEmbeddingProviderData(
            formData,
            (saveResponse) => {
                if (saveResponse.success) {
                    CurrentManageEmbeddingProviderData = saveResponse.data;
                    let providerIndex = CurrentEmbeddingProvidersList.findIndex((p) => p.id.value === CurrentManageEmbeddingProviderData.id.value);
                    if (providerIndex !== -1) {
                        CurrentEmbeddingProvidersList[providerIndex] = CurrentManageEmbeddingProviderData;
                    }
                    EmbeddingProviderListTable.find(`tr button[provider-id="${CurrentManageEmbeddingProviderData.id.value}"]`).closest("tr").replaceWith($(CreateEmbeddingProviderListTableElement(CurrentManageEmbeddingProviderData)));
                    AlertManager.createAlert({
                        type: "success",
                        message: "Embedding provider data saved successfully.",
                        timeout: 6000
                    });
                    CheckEmbeddingProviderManageTabHasChanges();
                } else {
                    AlertManager.createAlert({
                        type: "danger",
                        message: "Error saving embedding provider data. Check console.",
                        timeout: 6000
                    });
                    console.error("Error saving embedding provider data: ", saveResponse);
                }
                saveManageEmbeddingProviderButton.prop("disabled", true);
                IsSavingEmbeddingProviderTab = false;
            },
            (saveError) => {
                AlertManager.createAlert({
                    type: "danger",
                    message: "Error saving embedding provider data. Check console.",
                    timeout: 6000
                });
                console.error("Error saving embedding provider data: ", saveError);
                saveManageEmbeddingProviderButton.prop("disabled", false);
                IsSavingEmbeddingProviderTab = false;
            }
        );
    });

    embeddingProviderManageInnerHeader.find('button[data-bs-toggle="pill"]').on("shown.bs.tab", (event) => {
        let newTab = event.target;
        if (newTab.id == "embedding-provider-manager-models-tab") {
            saveManageEmbeddingProviderButton.addClass("d-none");
        } else {
            saveManageEmbeddingProviderButton.removeClass("d-none");
        }
    });

    saveManageEmbeddingProviderModelButton.on("click", (event) => {
        event.preventDefault();
        let validation = ValidateEmbeddingProviderModelManageTabFields(false);
        if (!validation.validated) {
            AlertManager.createAlert({
                type: "danger",
                message: "Validation failed:<br><br>" + validation.errors.join("<br>"),
                timeout: 6000
            });
            return;
        }

        let changes = CheckEmbeddingProviderModelManageTabHasChanges(false);
        if (!changes.hasChanges) return;

        saveManageEmbeddingProviderModelButton.prop("disabled", true);
        let formData = new FormData();
        formData.append("postType", CurrentManageEmbeddingProviderModelType);
        formData.append("providerId", CurrentManageEmbeddingProviderData.id.name);
        formData.append("modelId", changes.changes.id);
        formData.append("changes", JSON.stringify(changes.changes));

        SaveEmbeddingProviderModelData(formData,
            (saveResponse) => {
                if (saveResponse.success) {
                    CurrentManageEmbeddingProviderModelData = saveResponse.data;
                    let newTableElement = CreateEmbeddingProviderModelListTableElement(CurrentManageEmbeddingProviderModelData);
                    let existingRow = embeddingProviderModelListTable.find(`tbody tr[model-id="${CurrentManageEmbeddingProviderModelData.id}"]`);

                    if (CurrentManageEmbeddingProviderModelType == "new") {
                        CurrentManageEmbeddingProviderData.models.push(CurrentManageEmbeddingProviderModelData);
                        if (embeddingProviderModelListTable.find('tr[tr-type="none-notice"]').length > 0) {
                            embeddingProviderModelListTable.find("tbody").empty();
                        }
                        embeddingProviderModelListTable.find("tbody").append(newTableElement);
                        CurrentManageEmbeddingProviderModelType = "edit";
                    } else if (CurrentManageEmbeddingProviderModelType == "edit") {
                        let modelIndex = CurrentManageEmbeddingProviderData.models.findIndex(m => m.id === saveResponse.data.id);
                        if (modelIndex > -1) CurrentManageEmbeddingProviderData.models[modelIndex] = CurrentManageEmbeddingProviderModelData;
                        existingRow.replaceWith(newTableElement);
                    }

                    currentManageEmbeddingProviderModelName.text(CurrentManageEmbeddingProviderModelData.name);
                    CheckEmbeddingProviderModelManageTabHasChanges();
                    AlertManager.createAlert({
                        type: "success",
                        message: "Embedding model saved successfully.",
                        timeout: 6000
                    });
                } else {
                    AlertManager.createAlert({
                        type: "danger",
                        message: "Error saving model. Check console.",
                        timeout: 6000
                    });
                    console.error("Error saving embedding model:", saveResponse);
                }
                // Only re-enable the button on failure
                if (!saveResponse.success) saveManageEmbeddingProviderModelButton.prop("disabled", false);
            },
            (saveError) => {
                AlertManager.createAlert({
                    type: "danger",
                    message: "Error saving model. Check console.",
                    timeout: 6000
                });
                console.error("Error saving embedding model:", saveError);
                saveManageEmbeddingProviderModelButton.prop("disabled", false);
            }
        );
    });

    switchBackToEmbeddingProviderManagerModelsListTabFromModelTab.on("click", (event) => {
        event.preventDefault();
        CurrentManageEmbeddingProviderModelType = null;
        ShowEmbeddingProviderModelListTab();
    });

    addNewEmbeddingProviderModelButton.on("click", (event) => {
        event.preventDefault();
        CurrentManageEmbeddingProviderModelData = CreateDefaultEmbeddingProviderModelObject();
        currentManageModelEmbeddingProviderName.text(CurrentManageEmbeddingProviderData.id.name);
        currentManageEmbeddingProviderModelName.text("New Model");
        ResetAndEmptyEmbeddingProviderModelManageTab();
        FillEmbeddingProviderModelManageTab(CurrentManageEmbeddingProviderModelData);
        CurrentManageEmbeddingProviderModelType = "new";
        ShowEmbeddingProviderModelManageTab();
    });

    EmbeddingProviderListTable.on("click", "button[button-type=edit-embedding-provider]", (event) => {
        event.preventDefault();
        let providerId = $(event.currentTarget).attr("provider-id");
        CurrentManageEmbeddingProviderData = CurrentEmbeddingProvidersList.find((p) => p.id.value == providerId);
        currentManageEmbeddingProviderName.text(CurrentManageEmbeddingProviderData.id.name);
        ResetAndEmptyEmbeddingProvidersManageTab();
        FillEmbeddingProviderManageTab(CurrentManageEmbeddingProviderData);
        fillEmbeddingProviderIntegrationFields(); // Fill integration fields with data
        CurrentManageEmbeddingProviderType = "edit";
        ShowEmbeddingProviderManageTab();
    });

    switchBackToEmbeddingProviderListTabFromManageTab.on("click", (event) => {
        event.preventDefault();
        CurrentManageEmbeddingProviderType = null;
        ShowEmbeddingProviderListTab();
    });

    embeddingProviderModelListTable.on("click", "button[button-type=edit-embedding-provider-model]", (event) => {
        event.preventDefault();
        let modelId = $(event.currentTarget).attr("model-id");
        CurrentManageEmbeddingProviderModelData = CurrentManageEmbeddingProviderData.models.find((m) => m.id == modelId);
        currentManageModelEmbeddingProviderName.text(CurrentManageEmbeddingProviderData.id.name);
        currentManageEmbeddingProviderModelName.text(CurrentManageEmbeddingProviderModelData.name);
        ResetAndEmptyEmbeddingProviderModelManageTab();
        FillEmbeddingProviderModelManageTab(CurrentManageEmbeddingProviderModelData);
        CurrentManageEmbeddingProviderModelType = "edit";
        ShowEmbeddingProviderModelManageTab();
    });

    embeddingProviderManagerModelManageTab.on("input change", "input", () => {
        if (CurrentManageEmbeddingProviderModelType == null) return;
        CheckEmbeddingProviderModelManageTabHasChanges(true);
    });

    // Integration Event Handlers
    addNewEmbeddingProviderIntegrationFieldButton.on("click", (event) => {
        event.preventDefault();
        embeddingProviderIntegrationFieldsList.find(".text-center").remove();
        embeddingProviderIntegrationFieldsList.append($(createEmbeddingProviderIntegrationFieldElement()));
        CheckEmbeddingProviderManageTabHasChanges(true);
    });

    embeddingProviderIntegrationsTab.on("change", ".field-type-select", function () {
        const field = $(this).closest(".integration-field");
        const optionsContainer = field.find(".field-options-container");
        const defaultValueInput = field.find(".field-default-value-input");
        const selectedType = $(this).val();
        if (selectedType === "select") {
            optionsContainer.removeClass("d-none");
            defaultValueInput.prop("disabled", true).val("");
        } else if (selectedType === "models" || selectedType === "model_vector_dimensions") {
            optionsContainer.addClass("d-none");
            defaultValueInput.prop("disabled", true).val("");
        } else {
            optionsContainer.addClass("d-none");
            defaultValueInput.prop("disabled", false);
        }
    });

    embeddingProviderIntegrationsTab.on("click", ".remove-field-button", function () {
        $(this).closest(".integration-field").remove();
        if (embeddingProviderIntegrationFieldsList.children().length === 0) {
            fillEmbeddingProviderIntegrationFields();
        }
        CheckEmbeddingProviderManageTabHasChanges(true);
    });

    // INIT
    FetchEmbeddingProvidersFromAPI(
        0, 100,
        (providersData) => {
            CurrentEmbeddingProvidersList = providersData;
            EmbeddingProviderListTable.find("tbody").empty();
            if (providersData.length > 0) {
                providersData.forEach((providerData) => {
                    EmbeddingProviderListTable.append(CreateEmbeddingProviderListTableElement(providerData));
                });
            } else {
                EmbeddingProviderListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="5">No providers found</td></tr>');
            }
        },
        (error) => {
            AlertManager.createAlert({
                type: "danger",
                message: "Error fetching embedding providers. Check console.",
                timeout: 5000
            });
            console.error("Error fetching embedding providers: ", error);
        }
    );
});