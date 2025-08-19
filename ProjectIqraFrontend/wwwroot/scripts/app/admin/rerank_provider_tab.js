/** Dynamic Variables **/
let CurrentManageRerankProviderType = null;
let CurrentManageRerankProviderData = null;

let CurrentManageRerankProviderModelType = null;
let CurrentManageRerankProviderModelData = null;

let IsSavingRerankProviderTab = false;

/** Elements Variables **/
const RerankProviderTab = $("#rerank-provider-tab");

const rerankProviderInnerTab = RerankProviderTab.find("#rerank-provider-inner-tab");
const rerankProviderManageBreadcrumb = RerankProviderTab.find("#rerank-provider-manage-breadcrumb");

const switchBackToRerankProviderListTabFromManageTab = rerankProviderManageBreadcrumb.find("#switchBackToRerankProviderListTabFromManageTab");
const currentManageRerankProviderName = rerankProviderManageBreadcrumb.find("#currentManageRerankProviderName");

const RerankProviderListTableTab = RerankProviderTab.find("#rerankProviderListTableTab");
const RerankProviderListTable = RerankProviderListTableTab.find("#rerankProviderListTable");

const RerankProviderManageTab = RerankProviderTab.find("#rerankProviderManageTab");
const rerankProviderModelListTable = RerankProviderManageTab.find("#rerankProviderModelListTable");
const addNewRerankProviderModelButton = RerankProviderManageTab.find("#addNewRerankProviderModelButton");

const rerankProviderManagerInnerTabContainer = RerankProviderManageTab.find("#rerank-provider-manager-inner-tab-container");
const rerankProviderManagerInnerTab = rerankProviderManagerInnerTabContainer.find("#rerank-provider-manager-inner-tab");
const rerankProviderModelManagerBreadcrumb = RerankProviderTab.find("#rerank-provider-model-manager-breadcrumb");
const saveManageRerankProviderModelButton = rerankProviderModelManagerBreadcrumb.find("#saveManageRerankProviderModelButton");
const saveManageRerankProviderButton = rerankProviderManagerInnerTabContainer.find("#saveManageRerankProviderButton");

const currentManageModelRerankProviderName = rerankProviderModelManagerBreadcrumb.find("#currentManageModelRerankProviderName");
const currentManageRerankProviderModelName = rerankProviderModelManagerBreadcrumb.find("#currentManageRerankProviderModelName");
const switchBackToRerankProviderManagerModelsListTabFromModelTab = rerankProviderModelManagerBreadcrumb.find("#switchBackToRerankProviderManagerModelsListTabFromModelTab");

const rerankProviderManagerGeneral = RerankProviderManageTab.find("#rerank-provider-manager-general");
const manageRerankProviderIdInput = rerankProviderManagerGeneral.find("#manageRerankProviderIdInput");
const manageRerankProviderDisabledInput = rerankProviderManagerGeneral.find("#manageRerankProviderDisabledInput");
const manageRerankProviderIntegrationSelect = rerankProviderManagerGeneral.find("#manageRerankProviderIntegrationSelect");

const rerankProviderManagerModelsListTab = RerankProviderManageTab.find("#rerankProviderManagerModelsListTab");
const rerankProviderManagerModelManageTab = RerankProviderManageTab.find("#rerankProviderManagerModelManageTab");
const rerankProviderManagerGeneralTab = RerankProviderManageTab.find("#rerank-provider-manager-general-tab");
const rerankProviderModelManagerGeneralTab = rerankProviderManagerModelManageTab.find("#rerank-provider-model-manager-general-tab");

// Model Manage Tab Inputs
const manageRerankProviderModelIdInput = rerankProviderManagerModelManageTab.find("#manageRerankProviderModelIdInput");
const manageRerankProviderModelNameInput = rerankProviderManagerModelManageTab.find("#manageRerankProviderModelNameInput");
const manageRerankProviderModelPriceInput = rerankProviderManagerModelManageTab.find("#manageRerankProviderModelPriceInput");
const manageRerankProviderModelPriceTokensInput = rerankProviderManagerModelManageTab.find("#manageRerankProviderModelPriceTokensInput");
const manageRerankProviderModelDisabledInput = rerankProviderManagerModelManageTab.find("#manageRerankProviderModelDisabledInput");

// Integration Variables
const rerankProviderIntegrationsTab = RerankProviderManageTab.find("#rerank-provider-manager-integrations");
const addNewRerankProviderIntegrationFieldButton = rerankProviderIntegrationsTab.find("#addNewRerankProviderIntegrationFieldButton");
const rerankProviderIntegrationFieldsList = rerankProviderIntegrationsTab.find("#rerankProviderIntegrationFieldsList");

/** API Functions **/
function FetchRerankProvidersFromAPI(page, pageSize, successCallback, errorCallback) {
    $.ajax({
        url: '/app/admin/rerankproviders',
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

function SaveRerankProviderData(formData, successCallback, errorCallback) {
    $.ajax({
        type: "POST",
        url: "/app/admin/rerankproviders/save",
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

function SaveRerankProviderModelData(formData, successCallback, errorCallback) {
    $.ajax({
        type: "POST",
        url: "/app/admin/rerankproviders/model/save",
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
function CreateRerankProviderListTableElement(providerData) {
    let disabledData = providerData.disabledAt == null ? "-" : `<span class="badge bg-danger">${providerData.disabledAt}</span>`;
    return $(`
        <tr>
            <td>${providerData.id.value}</td>
            <td>${providerData.id.name}</td>
            <td>${disabledData}</td>
            <td>${providerData.models.length}</td>
            <td>
                <button class="btn btn-info btn-sm" provider-id="${providerData.id.value}" button-type="edit-rerank-provider">
                    <i class="fa-regular fa-eye"></i>
                </button>
            </td>
        </tr>`);
}

function ResetAndEmptyRerankProvidersManageTab() {
    manageRerankProviderIdInput.val("");
    manageRerankProviderDisabledInput.prop("checked", false).change();
    rerankProviderModelListTable.find("tbody").empty();
    rerankProviderManagerGeneralTab.click();
}

function ShowRerankProviderManageTab() {
    RerankProviderListTableTab.removeClass("show");
    rerankProviderInnerTab.removeClass("show");
    setTimeout(() => {
        RerankProviderListTableTab.addClass("d-none");
        rerankProviderInnerTab.addClass("d-none");
        RerankProviderManageTab.removeClass("d-none");
        rerankProviderManageBreadcrumb.removeClass("d-none");
        setTimeout(() => {
            RerankProviderManageTab.addClass("show");
            rerankProviderManageBreadcrumb.addClass("show");
        }, 10);
    }, 300);
}

function ShowRerankProviderListTab() {
    RerankProviderManageTab.removeClass("show");
    rerankProviderManageBreadcrumb.removeClass("show");
    setTimeout(() => {
        RerankProviderManageTab.addClass("d-none");
        rerankProviderManageBreadcrumb.addClass("d-none");
        RerankProviderListTableTab.removeClass("d-none");
        rerankProviderInnerTab.removeClass("d-none");
        setTimeout(() => {
            RerankProviderListTableTab.addClass("show");
            rerankProviderInnerTab.addClass("show");
        }, 10);
    }, 300);
}

function CreateRerankProviderModelListTableElement(modelData) {
    let disabledData = modelData.disabledAt == null ? "-" : `<span class="badge bg-danger">${modelData.disabledAt}</span>`;
    return $(`<tr model-id="${modelData.id}">
        <td>${modelData.id}</td>
        <td>${modelData.name}</td>
        <td>${disabledData}</td>
        <td>
            <button class="btn btn-info btn-sm" model-id="${modelData.id}" button-type="edit-rerank-provider-model">
                <i class="fa-regular fa-eye"></i>
            </button>
        </td>
    </tr>`);
}

function FillRerankProviderManageTab(providerData) {
    manageRerankProviderIdInput.val(providerData.id.name);
    manageRerankProviderDisabledInput.prop("checked", providerData.disabledAt != null);
    fillRerankProviderIntegrationSelect();
    rerankProviderModelListTable.find("tbody").empty();
    if (providerData.models.length > 0) {
        providerData.models.forEach((modelData) => {
            rerankProviderModelListTable.find("tbody").append(CreateRerankProviderModelListTableElement(modelData));
        });
    } else {
        rerankProviderModelListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="4">No models</td></tr>');
    }
}

function ShowRerankProviderModelManageTab() {
    rerankProviderManagerModelsListTab.removeClass("show");
    rerankProviderManagerInnerTabContainer.removeClass("show");
    rerankProviderManageBreadcrumb.removeClass("show");
    setTimeout(() => {
        rerankProviderManagerModelsListTab.addClass("d-none");
        rerankProviderManagerInnerTabContainer.addClass("d-none");
        rerankProviderManageBreadcrumb.addClass("d-none");
        rerankProviderManagerModelManageTab.removeClass("d-none");
        rerankProviderModelManagerBreadcrumb.removeClass("d-none");
        setTimeout(() => {
            rerankProviderManagerModelManageTab.addClass("show");
            rerankProviderModelManagerBreadcrumb.addClass("show");
        }, 10);
    }, 300);
}

function ShowRerankProviderModelListTab() {
    rerankProviderManagerModelManageTab.removeClass("show");
    rerankProviderModelManagerBreadcrumb.removeClass("show");
    setTimeout(() => {
        rerankProviderManagerModelManageTab.addClass("d-none");
        rerankProviderModelManagerBreadcrumb.addClass("d-none");
        rerankProviderManagerModelsListTab.removeClass("d-none");
        rerankProviderManagerInnerTabContainer.removeClass("d-none");
        rerankProviderManageBreadcrumb.removeClass("d-none");
        setTimeout(() => {
            rerankProviderManagerModelsListTab.addClass("show");
            rerankProviderManagerInnerTabContainer.addClass("show");
            rerankProviderManageBreadcrumb.addClass("show");
        }, 10);
    }, 300);
}

function CheckRerankProviderManageTabHasChanges(enableDisableButton = true) {
    let changes = {};
    let hasChanges = false;

    // Check disabled state
    changes.disabled = manageRerankProviderDisabledInput.prop("checked");
    if (changes.disabled === (CurrentManageRerankProviderData.disabledAt == null)) {
        hasChanges = true;
    }

    // Check integration selection
    changes.integrationId = manageRerankProviderIntegrationSelect.val();
    if (changes.integrationId !== CurrentManageRerankProviderData.integrationId) {
        hasChanges = true;
    }

    // Check integration fields
    const integrationFieldsChanges = CheckRerankProviderIntegrationFieldsTabHasChanges();
    if (integrationFieldsChanges.hasChanges) {
        hasChanges = true;
        changes.userIntegrationFields = integrationFieldsChanges.changes;
    }

    if (enableDisableButton) {
        saveManageRerankProviderButton.prop("disabled", !hasChanges);
    }

    return {
        hasChanges: hasChanges,
        changes: changes,
    };
}

function ValidateRerankProviderManageTab(onlyRemove = true) {
    const errors = [];
    let validated = true;

    // General Tab
    const selectedIntegration = manageRerankProviderIntegrationSelect.val();
    if (!selectedIntegration) {
        validated = false;
        errors.push("Integration selection is required");
        if (!onlyRemove) {
            manageRerankProviderIntegrationSelect.addClass("is-invalid");
        }
    } else {
        manageRerankProviderIntegrationSelect.removeClass("is-invalid");
    }

    // Integration Tab
    const integrationValidation = ValidateRerankProviderIntegrationFieldsTab(onlyRemove);
    if (!integrationValidation.validated) {
        validated = false;
        errors.push(...integrationValidation.errors);
    }

    return {
        validated: validated,
        errors: errors,
    };
}

function fillRerankProviderIntegrationSelect() {
    manageRerankProviderIntegrationSelect.empty().append('<option value="">Select Integration</option>');
    // Filter for integrations of type "Rerank"
    const rerankIntegrations = CurrentIntegrationsList.filter((integration) => integration.type.includes("Rerank"));
    rerankIntegrations.forEach((integration) => {
        manageRerankProviderIntegrationSelect.append(
            `<option value="${integration.id}" ${CurrentManageRerankProviderData.integrationId === integration.id ? "selected" : ""}>
                ${integration.name}
            </option>`
        );
    });
}

/** Integration Functions (Adapted from LLM Provider) **/

function createRerankProviderIntegrationFieldElement(fieldData = null) {
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
                        <input type="text" class="form-control field-id-input" placeholder="Field ID" value="${fieldData?.id || ""}">
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label">Name</label>
                        <input type="text" class="form-control field-name-input" placeholder="Field Name" value="${fieldData?.name || ""}">
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
                        <input type="text" class="form-control field-tooltip-input" placeholder="Field Tooltip" value="${fieldData?.tooltip || ""}">
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6 mb-3">
                        <label class="form-label">Placeholder</label>
                        <input type="text" class="form-control field-placeholder-input" placeholder="Field Placeholder" value="${fieldData?.placeholder || ""}">
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label">Default Value</label>
                        <input type="text" class="form-control field-default-value-input" placeholder="Default Value" value="${fieldData?.defaultValue || ""}"
                            ${fieldData?.type === "select" || fieldData?.type === "models" ? "disabled" : ""}>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-check">
                            <input class="form-check-input field-required-check" type="checkbox" ${fieldData?.required ? "checked" : ""}>
                            <label class="form-check-label">Required</label>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-check">
                            <input class="form-check-input field-encrypted-check" type="checkbox" ${fieldData?.isEncrypted ? "checked" : ""}>
                            <label class="form-check-label">Encrypted</label>
                        </div>
                    </div>
                </div>
                <div class="field-options-container ${fieldData?.type === "select" ? "" : "d-none"} mt-3">
                    <label class="form-label">Options</label>
                    <div class="field-options-list">
                        ${fieldData?.options?.map((option) => createRerankIntegrationFieldOptionElement(option)).join("") || ""}
                    </div>
                    <button type="button" class="btn btn-outline-primary btn-sm mt-2 add-option-button">
                        <i class="fa-regular fa-plus"></i> Add Option
                    </button>
                </div>
            </div>
        </div>`;
}

function createRerankIntegrationFieldOptionElement(optionData = null) {
    return `
        <div class="input-group mb-2 field-option">
            <input type="text" class="form-control option-key-input" placeholder="Option Key" value="${optionData?.key || ""}">
            <input type="text" class="form-control option-value-input" placeholder="Option Value" value="${optionData?.value || ""}">
            <div class="input-group-text">
                <input class="form-check-input option-default-check mt-0" type="radio" name="defaultOption" ${optionData?.isDefault ? "checked" : ""}>
                <label class="ms-2">Default?</label>
            </div>
            <button class="btn btn-outline-danger remove-option-button" type="button">
                <i class="fa-regular fa-trash"></i>
            </button>
        </div>`;
}

function fillRerankIntegrationFields() {
    rerankProviderIntegrationFieldsList.empty();
    if (CurrentManageRerankProviderData.userIntegrationFields.length === 0) {
        rerankProviderIntegrationFieldsList.append(`<div class="text-center p-5"><p class="text-muted mb-0">No integration fields defined</p></div>`);
        return;
    }
    CurrentManageRerankProviderData.userIntegrationFields.forEach((field) => {
        rerankProviderIntegrationFieldsList.append($(createRerankProviderIntegrationFieldElement(field)));
    });
}

function CheckRerankProviderIntegrationFieldsTabHasChanges() {
    let changes = [];
    let hasChanges = false;
    rerankProviderIntegrationFieldsList.find(".integration-field").each(function () {
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
    if (changes.length !== CurrentManageRerankProviderData.userIntegrationFields.length) {
        hasChanges = true;
    } else {
        for (let i = 0; i < changes.length; i++) {
            const newField = changes[i];
            const oldField = CurrentManageRerankProviderData.userIntegrationFields[i];
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

function ValidateRerankProviderIntegrationFieldsTab(onlyRemove = true) {
    const errors = [];
    let validated = true;
    rerankProviderIntegrationFieldsList.find(".integration-field").each(function (index) {
        const field = $(this);
        const fieldId = field.find(".field-id-input").val().trim();
        if (!fieldId) {
            validated = false;
            errors.push(`Field ${index + 1}: ID is required`);
            if (!onlyRemove) field.find(".field-id-input").addClass("is-invalid");
        } else {
            field.find(".field-id-input").removeClass("is-invalid");
        }
        const fieldName = field.find(".field-name-input").val().trim();
        if (!fieldName) {
            validated = false;
            errors.push(`Field ${index + 1}: Name is required`);
            if (!onlyRemove) field.find(".field-name-input").addClass("is-invalid");
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
                    if (option.find(".option-default-check").is(":checked")) hasDefault = true;
                });
                if (!hasDefault) {
                    validated = false;
                    errors.push(`Field ${index + 1}: Select type must have a default option selected`);
                }
            }
        }
        if (fieldType !== "select" && fieldType !== "models") {
            const defaultValue = field.find(".field-default-value-input").val().trim();
            const isRequired = field.find(".field-required-check").is(":checked");
            if (isRequired && !defaultValue) {
                validated = false;
                errors.push(`Field ${index + 1}: Default value is required for required fields`);
                if (!onlyRemove) field.find(".field-default-value-input").addClass("is-invalid");
            } else {
                field.find(".field-default-value-input").removeClass("is-invalid");
            }
            if (fieldType === "number" && defaultValue) {
                if (isNaN(defaultValue)) {
                    validated = false;
                    errors.push(`Field ${index + 1}: Default value must be a valid number`);
                    if (!onlyRemove) field.find(".field-default-value-input").addClass("is-invalid");
                } else {
                    field.find(".field-default-value-input").removeClass("is-invalid");
                }
            }
        }
        const currentId = field.find(".field-id-input").val().trim();
        if (currentId) {
            const duplicateFields = rerankProviderIntegrationFieldsList.find(".integration-field").not(field).filter(function () {
                return $(this).find(".field-id-input").val().trim() === currentId;
            });
            if (duplicateFields.length > 0) {
                validated = false;
                errors.push(`Field ${index + 1}: Duplicate Field ID "${currentId}"`);
                if (!onlyRemove) field.find(".field-id-input").addClass("is-invalid");
            }
        }
    });
    return {
        validated: validated,
        errors: errors
    };
}


/** Model Specific Functions **/

function CreateDefaultRerankProviderModelObject() {
    return {
        id: "",
        name: "",
        disabledAt: true, // Interpreted as checked/disabled
        price: "",
        priceTokenUnit: "",
    };
}

function ResetAndEmptyRerankProviderModelManageTab() {
    rerankProviderManagerModelManageTab.find("input").val("").change();
    rerankProviderManagerModelManageTab.find(".is-invalid").removeClass("is-invalid");
    manageRerankProviderModelDisabledInput.prop("checked", true);
    saveManageRerankProviderModelButton.prop("disabled", true);
    rerankProviderModelManagerGeneralTab.click();
}

function FillRerankProviderModelManageTab(modelData) {
    manageRerankProviderModelIdInput.val(modelData.id);
    manageRerankProviderModelNameInput.val(modelData.name);
    manageRerankProviderModelPriceInput.val(modelData.price);
    manageRerankProviderModelPriceTokensInput.val(modelData.priceTokenUnit);
    manageRerankProviderModelDisabledInput.prop("checked", modelData.disabledAt != null);
}

function CheckRerankProviderModelManageTabHasChanges(enableDisableButton = true) {
    let changes = {};
    let hasChanges = false;

    changes.id = manageRerankProviderModelIdInput.val();
    if (CurrentManageRerankProviderModelData.id != changes.id) hasChanges = true;

    changes.name = manageRerankProviderModelNameInput.val();
    if (CurrentManageRerankProviderModelData.name != changes.name) hasChanges = true;

    changes.disabled = manageRerankProviderModelDisabledInput.prop("checked");
    if (changes.disabled == (CurrentManageRerankProviderModelData.disabledAt == null)) hasChanges = true;

    changes.price = manageRerankProviderModelPriceInput.val();
    if (CurrentManageRerankProviderModelData.price != changes.price) hasChanges = true;

    changes.priceTokenUnit = manageRerankProviderModelPriceTokensInput.val();
    if (CurrentManageRerankProviderModelData.priceTokenUnit != changes.priceTokenUnit) hasChanges = true;

    if (enableDisableButton) {
        saveManageRerankProviderModelButton.prop("disabled", !hasChanges);
    }

    return {
        hasChanges: hasChanges,
        changes: changes
    };
}

function ValidateRerankProviderModelManageTabFields(onlyRemove = false) {
    let errors = [];
    let validated = true;

    let modelId = manageRerankProviderModelIdInput.val().trim();
    if (modelId === "") {
        validated = false;
        errors.push("Model id is required.");
        if (!onlyRemove) manageRerankProviderModelIdInput.addClass("is-invalid");
    } else manageRerankProviderModelIdInput.removeClass("is-invalid");

    let modelName = manageRerankProviderModelNameInput.val().trim();
    if (modelName === "") {
        validated = false;
        errors.push("Model name is required.");
        if (!onlyRemove) manageRerankProviderModelNameInput.addClass("is-invalid");
    } else manageRerankProviderModelNameInput.removeClass("is-invalid");

    let modelPrice = manageRerankProviderModelPriceInput.val().trim();
    if (modelPrice === "" || isNaN(modelPrice)) {
        validated = false;
        errors.push("Price is required and must be a number.");
        if (!onlyRemove) manageRerankProviderModelPriceInput.addClass("is-invalid");
    } else manageRerankProviderModelPriceInput.removeClass("is-invalid");

    let modelPriceTokenUnit = manageRerankProviderModelPriceTokensInput.val().trim();
    if (modelPriceTokenUnit === "" || isNaN(modelPriceTokenUnit) || !Number.isInteger(Number(modelPriceTokenUnit))) {
        validated = false;
        errors.push("Price token unit is required and must be an integer.");
        if (!onlyRemove) manageRerankProviderModelPriceTokensInput.addClass("is-invalid");
    } else manageRerankProviderModelPriceTokensInput.removeClass("is-invalid");

    return {
        validated: validated,
        errors: errors
    };
}

/** Initalizer **/
$(document).ready(() => {
    // Provider Level Events
    rerankProviderManagerGeneral.on("input change", "input, select", (event) => {
        if (CurrentManageRerankProviderType == null) return;
        CheckRerankProviderManageTabHasChanges(true);
    });
    rerankProviderIntegrationsTab.on("input change", "input, select", (event) => {
        if (CurrentManageRerankProviderType == null) return;
        CheckRerankProviderManageTabHasChanges(true);
    });

    saveManageRerankProviderButton.on("click", (event) => {
        event.preventDefault();
        if (IsSavingRerankProviderTab) return;

        const validationResult = ValidateRerankProviderManageTab(false);
        if (!validationResult.validated) {
            AlertManager.createAlert({
                type: "danger",
                message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
                timeout: 6000
            });
            return;
        }

        const changes = CheckRerankProviderManageTabHasChanges(false);
        if (!changes.hasChanges) return;

        saveManageRerankProviderButton.prop("disabled", true);
        IsSavingRerankProviderTab = true;

        const formData = new FormData();
        formData.append("changes", JSON.stringify(changes.changes));
        formData.append("providerId", CurrentManageRerankProviderData.id.value);

        SaveRerankProviderData(formData,
            (saveResponse) => {
                if (saveResponse.success) {
                    CurrentManageRerankProviderData = saveResponse.data;
                    let providerIndex = CurrentRerankProvidersList.findIndex((p) => p.id.value === CurrentManageRerankProviderData.id.value);
                    if (providerIndex !== -1) CurrentRerankProvidersList[providerIndex] = CurrentManageRerankProviderData;

                    RerankProviderListTable.find(`tr button[provider-id="${CurrentManageRerankProviderData.id.value}"]`).closest("tr").replaceWith($(CreateRerankProviderListTableElement(CurrentManageRerankProviderData)));
                    AlertManager.createAlert({
                        type: "success",
                        message: "Rerank provider data saved successfully.",
                        timeout: 6000
                    });
                    CheckRerankProviderManageTabHasChanges();
                } else {
                    AlertManager.createAlert({
                        type: "danger",
                        message: "Error saving rerank provider data. Check console.",
                        timeout: 6000
                    });
                    console.error("Error saving rerank provider:", saveResponse);
                }
                saveManageRerankProviderButton.prop("disabled", true);
                IsSavingRerankProviderTab = false;
            },
            (saveError) => {
                AlertManager.createAlert({
                    type: "danger",
                    message: "Error saving rerank provider data. Check console.",
                    timeout: 6000
                });
                console.error("Error saving rerank provider:", saveError);
                saveManageRerankProviderButton.prop("disabled", false);
                IsSavingRerankProviderTab = false;
            }
        );
    });

    RerankProviderListTable.on("click", "button[button-type=edit-rerank-provider]", (event) => {
        event.preventDefault();
        let providerId = $(event.currentTarget).attr("provider-id");
        CurrentManageRerankProviderData = CurrentRerankProvidersList.find((p) => p.id.value == providerId);
        currentManageRerankProviderName.text(CurrentManageRerankProviderData.id.name);
        ResetAndEmptyRerankProvidersManageTab();
        FillRerankProviderManageTab(CurrentManageRerankProviderData);
        fillRerankIntegrationFields();
        CurrentManageRerankProviderType = "edit";
        ShowRerankProviderManageTab();
    });

    switchBackToRerankProviderListTabFromManageTab.on("click", (event) => {
        event.preventDefault();
        CurrentManageRerankProviderType = null;
        ShowRerankProviderListTab();
    });

    // Model Level Events
    addNewRerankProviderModelButton.on("click", (event) => {
        event.preventDefault();
        CurrentManageRerankProviderModelData = CreateDefaultRerankProviderModelObject();
        currentManageModelRerankProviderName.text(CurrentManageRerankProviderData.id.name);
        currentManageRerankProviderModelName.text("New Model");
        ResetAndEmptyRerankProviderModelManageTab();
        FillRerankProviderModelManageTab(CurrentManageRerankProviderModelData);
        CurrentManageRerankProviderModelType = "new";
        ShowRerankProviderModelManageTab();
    });

    rerankProviderModelListTable.on("click", "button[button-type=edit-rerank-provider-model]", (event) => {
        event.preventDefault();
        let modelId = $(event.currentTarget).attr("model-id");
        CurrentManageRerankProviderModelData = CurrentManageRerankProviderData.models.find((m) => m.id == modelId);
        currentManageModelRerankProviderName.text(CurrentManageRerankProviderData.id.name);
        currentManageRerankProviderModelName.text(CurrentManageRerankProviderModelData.name);
        ResetAndEmptyRerankProviderModelManageTab();
        FillRerankProviderModelManageTab(CurrentManageRerankProviderModelData);
        CurrentManageRerankProviderModelType = "edit";
        ShowRerankProviderModelManageTab();
    });

    switchBackToRerankProviderManagerModelsListTabFromModelTab.on("click", (event) => {
        event.preventDefault();
        CurrentManageRerankProviderModelType = null;
        ShowRerankProviderModelListTab();
    });

    rerankProviderManagerModelManageTab.on("input change", "input", () => {
        if (CurrentManageRerankProviderModelType == null) return;
        CheckRerankProviderModelManageTabHasChanges(true);
    });

    saveManageRerankProviderModelButton.on("click", (event) => {
        event.preventDefault();
        let validation = ValidateRerankProviderModelManageTabFields(false);
        if (!validation.validated) {
            AlertManager.createAlert({
                type: "danger",
                message: "Validation failed:<br><br>" + validation.errors.join("<br>"),
                timeout: 6000
            });
            return;
        }

        let changes = CheckRerankProviderModelManageTabHasChanges(false);
        if (!changes.hasChanges) return;

        saveManageRerankProviderModelButton.prop("disabled", true);
        let formData = new FormData();
        formData.append("postType", CurrentManageRerankProviderModelType);
        formData.append("providerId", CurrentManageRerankProviderData.id.name);
        formData.append("modelId", changes.changes.id);
        formData.append("changes", JSON.stringify(changes.changes));

        SaveRerankProviderModelData(formData,
            (saveResponse) => {
                if (saveResponse.success) {
                    CurrentManageRerankProviderModelData = saveResponse.data;
                    let newTableElement = CreateRerankProviderModelListTableElement(CurrentManageRerankProviderModelData);

                    if (CurrentManageRerankProviderModelType == "new") {
                        CurrentManageRerankProviderData.models.push(CurrentManageRerankProviderModelData);
                        if (rerankProviderModelListTable.find('tr[tr-type="none-notice"]').length > 0) {
                            rerankProviderModelListTable.find("tbody").empty();
                        }
                        rerankProviderModelListTable.find("tbody").append(newTableElement);
                        CurrentManageRerankProviderModelType = "edit";
                    } else if (CurrentManageRerankProviderModelType == "edit") {
                        let modelIndex = CurrentManageRerankProviderData.models.findIndex(m => m.id === CurrentManageRerankProviderModelData.id);
                        if (modelIndex > -1) CurrentManageRerankProviderData.models[modelIndex] = CurrentManageRerankProviderModelData;

                        rerankProviderModelListTable.find(`tbody tr[model-id="${CurrentManageRerankProviderModelData.id}"]`).replaceWith(newTableElement);
                    }

                    currentManageRerankProviderModelName.text(CurrentManageRerankProviderModelData.name);
                    CheckRerankProviderModelManageTabHasChanges();
                    AlertManager.createAlert({
                        type: "success",
                        message: "Rerank model saved successfully.",
                        timeout: 6000
                    });
                } else {
                    AlertManager.createAlert({
                        type: "danger",
                        message: "Error saving model. Check console.",
                        timeout: 6000
                    });
                    console.error("Error saving rerank model:", saveResponse);
                    saveManageRerankProviderModelButton.prop("disabled", false);
                }
            },
            (saveError) => {
                AlertManager.createAlert({
                    type: "danger",
                    message: "Error saving model. Check console.",
                    timeout: 6000
                });
                console.error("Error saving rerank model:", saveError);
                saveManageRerankProviderModelButton.prop("disabled", false);
            }
        );
    });

    // Integration Event Handlers
    addNewRerankProviderIntegrationFieldButton.on("click", (event) => {
        event.preventDefault();
        rerankProviderIntegrationFieldsList.find(".text-center").remove();
        rerankProviderIntegrationFieldsList.append($(createRerankProviderIntegrationFieldElement()));
        CheckRerankProviderManageTabHasChanges(true);
    });
    rerankProviderIntegrationsTab.on("change", ".field-type-select", function () {
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
    rerankProviderIntegrationsTab.on("click", ".remove-field-button", function () {
        $(this).closest(".integration-field").remove();
        if (rerankProviderIntegrationFieldsList.children().length === 0) {
            fillRerankIntegrationFields();
        }
        CheckRerankProviderManageTabHasChanges(true);
    });


    // INIT
    FetchRerankProvidersFromAPI(
        0, 100,
        (providersData) => {
            CurrentRerankProvidersList = providersData;
            RerankProviderListTable.find("tbody").empty();
            if (providersData.length > 0) {
                providersData.forEach((providerData) => {
                    RerankProviderListTable.find("tbody").append(CreateRerankProviderListTableElement(providerData));
                });
            }
        },
        (error) => {
            AlertManager.createAlert({
                type: "danger",
                message: "Error fetching rerank providers. Check console.",
                timeout: 5000
            });
            console.error("Error fetching rerank providers: ", error);
        }
    );
});