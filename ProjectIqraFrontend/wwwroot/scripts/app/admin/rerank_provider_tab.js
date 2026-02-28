/** Dynamic Variables **/
let CurrentManageRerankProviderType = null;
let CurrentManageRerankProviderData = null;

let CurrentManageRerankProviderModelType = null;
let CurrentManageRerankProviderModelData = null;

let IsSavingRerankProviderTab = false;

let rerankFieldsHelper = null;

/** Elements Variables **/
const RerankProviderTab = $("#rerank-provider-tab");

// Header
const rerankProviderInnerHeader = RerankProviderTab.find("#rerank-provider-inner-header");

// Manager Header
const rerankProviderManagerInnerHeader = RerankProviderTab.find("#rerank-provider-manager-inner-header");
const switchBackToRerankProviderListTabFromManageTab = rerankProviderManagerInnerHeader.find("#switchBackToRerankProviderListTabFromManageTab");
const currentManageRerankProviderName = rerankProviderManagerInnerHeader.find("#currentManageRerankProviderName");
const saveManageRerankProviderButton = rerankProviderManagerInnerHeader.find("#saveManageRerankProviderButton");

// Manager Model Header
const rerankProviderModelManagerModelInnerHeader = RerankProviderTab.find("#rerank-provider-manager-model-inner-header");
const currentManageModelRerankProviderName = rerankProviderModelManagerModelInnerHeader.find("#currentManageModelRerankProviderName");
const currentManageRerankProviderModelName = rerankProviderModelManagerModelInnerHeader.find("#currentManageRerankProviderModelName");
const switchBackToRerankProviderManagerModelsListTabFromModelTab = rerankProviderModelManagerModelInnerHeader.find("#switchBackToRerankProviderManagerModelsListTabFromModelTab");
const saveManageRerankProviderModelButton = rerankProviderModelManagerModelInnerHeader.find("#saveManageRerankProviderModelButton");

// List Tab
const RerankProviderListTableTab = RerankProviderTab.find("#rerankProviderListTableTab");
const RerankProviderListTable = RerankProviderListTableTab.find("#rerankProviderListTable");

// Manager Tab
const RerankProviderManageTab = RerankProviderTab.find("#rerankProviderManageTab");
const rerankProviderModelListTable = RerankProviderManageTab.find("#rerankProviderModelListTable");
const addNewRerankProviderModelButton = RerankProviderManageTab.find("#addNewRerankProviderModelButton");

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
    manageRerankProviderIntegrationSelect.val("").change();

    rerankProviderManagerGeneralTab.click();

    // Reset Helper
    rerankProviderIntegrationFieldsList.empty();
    rerankFieldsHelper = null;
}

function ShowRerankProviderManageTab() {
    RerankProviderListTableTab.removeClass("show");
    rerankProviderInnerHeader.removeClass("show");

    setTimeout(() => {
        RerankProviderListTableTab.addClass("d-none");
        rerankProviderInnerHeader.addClass("d-none");

        RerankProviderManageTab.removeClass("d-none");
        rerankProviderManagerInnerHeader.removeClass("d-none");

        setTimeout(() => {
            RerankProviderManageTab.addClass("show");
            rerankProviderManagerInnerHeader.addClass("show");

            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function ShowRerankProviderListTab() {
    RerankProviderManageTab.removeClass("show");
    rerankProviderManagerInnerHeader.removeClass("show");

    setTimeout(() => {
        RerankProviderManageTab.addClass("d-none");
        rerankProviderManagerInnerHeader.addClass("d-none");

        RerankProviderListTableTab.removeClass("d-none");
        rerankProviderInnerHeader.removeClass("d-none");

        setTimeout(() => {
            RerankProviderListTableTab.addClass("show");
            rerankProviderInnerHeader.addClass("show");

            setDynamicBodyHeight();
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

    // Initialize Integration Fields Helper
    rerankFieldsHelper = new ProviderIntegrationsFieldHelper(
        rerankProviderIntegrationFieldsList,
        addNewRerankProviderIntegrationFieldButton,
        providerData.userIntegrationFields,
        () => {
            // Callback when fields change
            CheckRerankProviderManageTabHasChanges(true);
        }
    );
    rerankFieldsHelper.render();
}

function ShowRerankProviderModelManageTab() {
    rerankProviderManagerModelsListTab.removeClass("show");
    rerankProviderManagerInnerHeader.removeClass("show");

    setTimeout(() => {
        rerankProviderManagerModelsListTab.addClass("d-none");
        rerankProviderManagerInnerHeader.addClass("d-none");

        rerankProviderManagerModelManageTab.removeClass("d-none");
        rerankProviderModelManagerModelInnerHeader.removeClass("d-none");

        setTimeout(() => {
            rerankProviderManagerModelManageTab.addClass("show");
            rerankProviderModelManagerModelInnerHeader.addClass("show");

            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function ShowRerankProviderModelListTab() {
    rerankProviderManagerModelManageTab.removeClass("show");
    rerankProviderModelManagerModelInnerHeader.removeClass("show");

    setTimeout(() => {
        rerankProviderManagerModelManageTab.addClass("d-none");
        rerankProviderModelManagerModelInnerHeader.addClass("d-none");

        rerankProviderManagerModelsListTab.removeClass("d-none");
        rerankProviderManagerInnerHeader.removeClass("d-none");

        setTimeout(() => {
            rerankProviderManagerModelsListTab.addClass("show");
            rerankProviderManagerInnerHeader.addClass("show");

            setDynamicBodyHeight();
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
    changes.userIntegrationFields = rerankFieldsHelper.getData();
    if (rerankFieldsHelper.hasChanges()) {
        hasChanges = true;      
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
    if (rerankFieldsHelper) {
        const fieldValidation = rerankFieldsHelper.validate(onlyRemove);
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

                    if (rerankFieldsHelper) {
                        rerankFieldsHelper.updateInitialData(CurrentManageRerankProviderData.userIntegrationFields);
                    }

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