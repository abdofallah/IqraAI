/** Dynamic Variables **/
let CurrentManageEmbeddingProviderType = null;
let CurrentManageEmbeddingProviderData = null;

let CurrentManageEmbeddingProviderModelType = null;
let CurrentManageEmbeddingProviderModelData = null;

let IsSavingEmbeddingProviderTab = false;

let embeddingFieldsHelper = null;

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
    manageEmbeddingProviderIntegrationSelect.val("").change();

    embeddingProviderManagerGeneralTab.click();

    // Reset Helper
    embeddingProviderIntegrationFieldsList.empty();
    embeddingFieldsHelper = null;
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

    // Initialize Integration Fields Helper
    embeddingFieldsHelper = new ProviderIntegrationsFieldHelper(
        embeddingProviderIntegrationFieldsList,
        addNewEmbeddingProviderIntegrationFieldButton,
        providerData.userIntegrationFields,
        () => {
            // Callback when fields change
            CheckEmbeddingProviderManageTabHasChanges(true);
        }
    );
    embeddingFieldsHelper.render();
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
    changes.userIntegrationFields = embeddingFieldsHelper.getData();
    if (embeddingFieldsHelper.hasChanges()) {
        hasChanges = true;
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

    // Integration Tab via Helper
    if (embeddingFieldsHelper) {
        const fieldValidation = embeddingFieldsHelper.validate(onlyRemove);
        if (!fieldValidation.validated) {
            validated = false;
            errors.push(...fieldValidation.errors);
        }
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

                    if (embeddingFieldsHelper) {
                        embeddingFieldsHelper.updateInitialData(CurrentManageEmbeddingProviderData.userIntegrationFields);
                    }

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