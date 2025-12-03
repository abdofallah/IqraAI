/** Dynamic Variables **/
let CurrentIntegrationData = null;
let ManageIntegrationType = null; // new or edit
let IsSavingIntegrationTab = false;

/** Elements Variables **/
const integrationsTab = $("#integrations-tab");
const integrationsHeader = integrationsTab.find("#integrations-header");

// List view elements
const integrationsListBreadcrumb = integrationsTab.find("#integrationsListBreadcrumb");
const integrationsListTab = integrationsTab.find("#integrationsListTab");
const integrationsListContainer = integrationsTab.find("#integrationsListContainer");
const addNewIntegrationButton = integrationsTab.find("#addNewIntegrationButton");

// Manager view elements
const integrationManagerBreadcrumb = integrationsTab.find("#integrationManagerBreadcrumb");
const integrationManagerTab = integrationsTab.find("#integrationManagerTab");
const currentIntegrationName = integrationsTab.find("#currentIntegrationName");
const saveIntegrationButton = integrationsTab.find("#saveIntegrationButton");
const switchBackToIntegrationsListTab = integrationsTab.find("#switchBackToIntegrationsListTab");

// Form elements
const integrationIdInput = $("#integrationIdInput");
const integrationNameInput = $("#integrationNameInput");
const integrationDescriptionInput = $("#integrationDescriptionInput");
const integrationLogoInput = $("#integrationLogoInput");
const integrationLogoPreview = $("#integrationLogoPreview");
const integrationTypesInput = $("#integrationTypesInput");
const integrationTypesList = $("#integrationTypesList");
const addTypeButton = $("#addTypeButton");
const addNewFieldButton = $("#addNewFieldButton");
const integrationFieldsList = $("#integrationFieldsList");
const integrationHelpTextInput = $("#integrationHelpTextInput");
const integrationHelpUrlInput = $("#integrationHelpUrlInput");
const integrationDisabledCheck = $("#integrationDisabledCheck");

/** API Functions **/
function FetchIntegrationsFromAPI(successCallback, errorCallback) {
	$.ajax({
		url: '/app/admin/integrations',
		type: 'GET',
		dataType: "json",
		success: (response) => {
			if (!response.success) {
				errorCallback(response);
				return;
			}
			successCallback(response.data);
		},
		error: (error) => {
			errorCallback(error);
		}
	});
}

function SaveIntegrationToAPI(formData, successCallback, errorCallback) {
	$.ajax({
		url: '/app/admin/integrations/save',
		type: 'POST',
		data: formData,
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
		}
	});
}

/** Core Functions **/
function createIntegrationCardElement(integration) {
	const typesBadges = integration.type.map((type) => `<span class="badge border border-dark text-dark me-1 mb-1">${type}</span>`).join("");

	return `
                <div class="col-lg-4 col-md-6 col-12 mb-3">
                    <div class="business-card d-flex flex-column align-items-start justify-content-center"
                        data-integration-id="${integration.id}">
                        <div class="d-flex flex-row align-items-center justify-content-between w-100 mb-3">
                            <div class="d-flex flex-row align-items-center">
                                <img src="${integration.logoUrl}">
                                <div>
                                    <h4>${integration.name}</h4>
                                </div>
                            </div>
                        </div>
                        <div>${typesBadges}</div>
                    </div>
                </div>
            `;
}

function createFieldElement(fieldData = null) {
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
                                    <option value="select" ${fieldData?.type === "select" ? "selected" : ""}>Select</option>
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
                                    ${fieldData?.type === "select" ? "disabled" : ""}>
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
                                ${fieldData?.options?.map((option) => createIntegrationFieldOptionElement(option)).join("") || ""}
                            </div>
                            <button type="button" class="btn btn-outline-primary btn-sm mt-2 add-option-button">
                                <i class="fa-regular fa-plus"></i> Add Option
                            </button>
                        </div>
                    </div>
                </div>
            `;
}

function createIntegrationFieldOptionElement(optionData = null) {
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
function createTypeBadgeElement(type) {
	return `
                <span class="badge border me-1 mb-1">
                    ${type}
                    <button type="button" class="btn-close ms-1" aria-label="Remove"></button>
                </span>
            `;
}

function generateUniqueId() {
	return Math.random().toString(36).substr(2, 9);
}

function FillIntegrationsList() {
	integrationsListContainer.empty();

	FetchIntegrationsFromAPI(
		(integrations) => {
			CurrentIntegrationsList = integrations;

			if (integrations.length === 0) {
				integrationsListContainer.append(`
                            <div class="col-12 text-center p-5">
                                <p class="text-muted mb-0">No integrations found</p>
                            </div>
                        `);
				return;
			}

			integrations.forEach((integration) => {
				integrationsListContainer.append($(createIntegrationCardElement(integration)));
			});
		},
		(error) => {
			AlertManager.createAlert({
				type: "danger",
				message: "Error loading integrations. Please try again.",
				timeout: 6000,
			});
			console.error("Error loading integrations:", error);
		},
	);
}

function ShowIntegrationsListTab() {
	integrationManagerBreadcrumb.removeClass("show");
	integrationManagerTab.removeClass("show");
	setTimeout(() => {
		integrationManagerBreadcrumb.addClass("d-none");
		integrationManagerTab.addClass("d-none");

		integrationsListBreadcrumb.removeClass("d-none");
		integrationsListTab.removeClass("d-none");
		setTimeout(() => {
			integrationsListBreadcrumb.addClass("show");
			integrationsListTab.addClass("show");
		}, 10);
	}, 300);
}

function ShowIntegrationManagerTab() {
	integrationsListBreadcrumb.removeClass("show");
	integrationsListTab.removeClass("show");
	setTimeout(() => {
		integrationsListBreadcrumb.addClass("d-none");
		integrationsListTab.addClass("d-none");

		integrationManagerBreadcrumb.removeClass("d-none");
		integrationManagerTab.removeClass("d-none");
		setTimeout(() => {
			integrationManagerBreadcrumb.addClass("show");
			integrationManagerTab.addClass("show");
		}, 10);
	}, 300);
}

function CreateDefaultIntegrationObject() {
	return {
		id: "",
		name: "",
		description: "",
		disabledAt: true,
		logo: null,
		type: [],
		fields: [],
		help: { text: "", uri: "" },
	};
}

/** Validation and Changes Functions **/
function ValidateIntegrationTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Basic info validation
	const id = integrationIdInput.val().trim();
	if (!id) {
		validated = false;
		errors.push("Integration ID is required");
		if (!onlyRemove) integrationIdInput.addClass("is-invalid");
	} else {
		integrationIdInput.removeClass("is-invalid");
	}

	const name = integrationNameInput.val().trim();
	if (!name) {
		validated = false;
		errors.push("Integration name is required");
		if (!onlyRemove) integrationNameInput.addClass("is-invalid");
	} else {
		integrationNameInput.removeClass("is-invalid");
	}

	// Logo validation
	if (ManageIntegrationType === "new" && integrationLogoInput[0].files.length === 0) {
		validated = false;
		errors.push("Integration logo is required");
		if (!onlyRemove) integrationLogoInput.addClass("is-invalid");
	} else {
		integrationLogoInput.removeClass("is-invalid");
	}

	// Type validation
	const types = collectTypes();
	if (types.length === 0) {
		validated = false;
		errors.push("At least one integration type is required");
		if (!onlyRemove) integrationTypesInput.addClass("is-invalid");
	} else {
		integrationTypesInput.removeClass("is-invalid");
	}

	// Fields validation
	const fields = collectFields();
	if (fields.length === 0) {
		validated = false;
		errors.push("At least one field is required");
	}

	// Validate each field
	fields.forEach((field, index) => {
		const fieldElement = $(`.integration-field[data-field-id="${field.id}"]`);

		if (!field.id.trim()) {
			validated = false;
			errors.push(`Field ${index + 1}: ID is required`);
			if (!onlyRemove) fieldElement.find(".field-id-input").addClass("is-invalid");
		}

		if (!field.name.trim()) {
			validated = false;
			errors.push(`Field ${index + 1}: Name is required`);
			if (!onlyRemove) fieldElement.find(".field-name-input").addClass("is-invalid");
		}

		if (field.type === "select" && (!field.options || field.options.length === 0)) {
			validated = false;
			errors.push(`Field ${index + 1}: Select field must have at least one option`);
		}

		if (field.type === "select" && field.options) {
			field.options.forEach((option, optionIndex) => {
				if (!option.key.trim() || !option.value.trim()) {
					validated = false;
					errors.push(`Field ${index + 1}: Option ${optionIndex + 1} must have both key and value`);
				}
			});
		}
	});

	return {
		validated: validated,
		errors: errors,
	};
}

function CheckIntegrationTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Check basic fields
	changes.id = integrationIdInput.val().trim();
	if (CurrentIntegrationData.id !== changes.id) {
		hasChanges = true;
	}

	changes.name = integrationNameInput.val().trim();
	if (CurrentIntegrationData.name !== changes.name) {
		hasChanges = true;
	}

	changes.description = integrationDescriptionInput.val().trim();
	if (CurrentIntegrationData.description !== changes.description) {
		hasChanges = true;
	}

	// Check disabled state
	const isDisabled = integrationDisabledCheck.is(":checked");
	changes.disabled = isDisabled;
	if (isDisabled === (CurrentIntegrationData.disabledAt == null)) {
		hasChanges = true;
	}

	// Check types
	changes.type = collectTypes();
	if (changes.type.length !== CurrentIntegrationData.type.length) {
		hasChanges = true;
	} else {
		for (let i = 0; i < changes.type.length; i++) {
			if (changes.type[i] !== CurrentIntegrationData.type[i]) {
				hasChanges = true;
				break;
			}
		}
	}

	// Check help
	changes.help = {
		text: integrationHelpTextInput.val().trim(),
		uri: integrationHelpUrlInput.val().trim(),
	};

	if (changes.help.text !== CurrentIntegrationData.help.text || changes.help.uri !== CurrentIntegrationData.help.uri) {
		hasChanges = true;
	}

	// Check fields
	changes.fields = collectFields();
	if (changes.fields.length !== CurrentIntegrationData.fields.length) {
		hasChanges = true;
	} else {
		for (let i = 0; i < changes.fields.length; i++) {
			const newField = changes.fields[i];
			const oldField = CurrentIntegrationData.fields[i];

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

			// Check options for select fields
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

	if (enableDisableButton) {
		saveIntegrationButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function collectTypes() {
	const types = [];
	integrationTypesList.find(".badge").each(function () {
		types.push($(this).text().trim());
	});
	return types;
}

function collectFields() {
	const fields = [];
	integrationFieldsList.find(".integration-field").each(function () {
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
			fieldData.defaultValue = ""; // Clear default value for select types
		}

		fields.push(fieldData);
	});
	return fields;
}
function resetOrClearIntegrationManager() {
	integrationIdInput.val("");
	integrationIdInput.prop("disabled", false);
	integrationNameInput.val("");
	integrationDescriptionInput.val("");
	integrationLogoPreview.attr("src", "/img/placeholder.png");
	integrationLogoInput.val("");
	integrationTypesList.empty();
	integrationFieldsList.empty();
	integrationHelpTextInput.val("");
	integrationHelpUrlInput.val("");

	integrationManagerTab.find(".is-invalid").removeClass("is-invalid");
	saveIntegrationButton.prop("disabled", true);
	integrationDisabledCheck.prop("checked", true);
}

function fillIntegrationManager(integrationData) {
	resetOrClearIntegrationManager();

	integrationIdInput.val(integrationData.id);
	integrationIdInput.prop("disabled", true);

	integrationNameInput.val(integrationData.name);
	integrationDescriptionInput.val(integrationData.description);
	if (integrationData.logo) {
		integrationLogoPreview.attr("src", integrationData.logoUrl);
	}

	// Set disabled state
	integrationDisabledCheck.prop("checked", integrationData.disabledAt != null);

	// Fill types
	integrationData.type.forEach((type) => {
		integrationTypesList.append($(createTypeBadgeElement(type)));
	});

	// Fill fields
	integrationData.fields.forEach((field) => {
		integrationFieldsList.append($(createFieldElement(field)));
	});

	// Fill help
	integrationHelpTextInput.val(integrationData.help.text);
	integrationHelpUrlInput.val(integrationData.help.uri);

	saveIntegrationButton.prop("disabled", true);
}

/** Event Handlers **/
function initIntegrationsTab() {
	// Add new integration
	addNewIntegrationButton.on("click", (event) => {
		event.preventDefault();

		ManageIntegrationType = "new";
		CurrentIntegrationData = CreateDefaultIntegrationObject();
		currentIntegrationName.text("New Integration");

		resetOrClearIntegrationManager();
		ShowIntegrationManagerTab();
	});

	// Switch back to list
	switchBackToIntegrationsListTab.on("click", async (event) => {
		event.preventDefault();

		const changes = CheckIntegrationTabHasChanges(false);
		if (changes.hasChanges) {
			const confirmDialog = new BootstrapConfirmDialog({
				title: "Unsaved Changes",
				message: "You have unsaved changes. Are you sure you want to discard them?",
				confirmText: "Discard",
				cancelText: "Stay",
				confirmButtonClass: "btn-danger",
			});

			const confirmed = await confirmDialog.show();
			if (!confirmed) return;
		}

		ShowIntegrationsListTab();
		ManageIntegrationType = null;
		CurrentIntegrationData = null;
	});

	// Edit integration
	integrationsListContainer.on("click", ".business-card", (event) => {
		event.preventDefault();
		const card = $(event.currentTarget);
		const integrationId = card.data("integration-id");

		CurrentIntegrationData = CurrentIntegrationsList.find((i) => i.id === integrationId);
		if (!CurrentIntegrationData) return;

		ManageIntegrationType = "edit";
		currentIntegrationName.text(CurrentIntegrationData.name);

		fillIntegrationManager(CurrentIntegrationData);
		ShowIntegrationManagerTab();
	});

	// Type management
	addTypeButton.on("click", () => {
		const type = integrationTypesInput.val().trim();
		if (!type) return;

		integrationTypesList.append($(createTypeBadgeElement(type)));
		integrationTypesInput.val("");
		CheckIntegrationTabHasChanges();
	});

	integrationTypesInput.on("keypress", (event) => {
		if (event.key === "Enter") {
			event.preventDefault();
			addTypeButton.click();
		}
	});

	integrationTypesList.on("click", ".btn-close", function (event) {
		$(this).closest(".badge").remove();
		CheckIntegrationTabHasChanges();
	});

	// Field management
	addNewFieldButton.on("click", () => {
		integrationFieldsList.append($(createFieldElement()));
		CheckIntegrationTabHasChanges();
	});

	integrationFieldsList.on("click", ".remove-field-button", function () {
		$(this).closest(".integration-field").remove();
		CheckIntegrationTabHasChanges();
	});

	integrationFieldsList.on("change", ".field-type-select", function () {
		const field = $(this).closest(".integration-field");
		const optionsContainer = field.find(".field-options-container");
		const defaultValueInput = field.find(".field-default-value-input");

		if ($(this).val() === "select") {
			optionsContainer.removeClass("d-none");
			defaultValueInput.prop("disabled", true).val("");
		} else {
			optionsContainer.addClass("d-none");
			defaultValueInput.prop("disabled", false);
		}
		CheckIntegrationTabHasChanges();
	});

	// Field options management
	integrationFieldsList.on("click", ".add-option-button", function () {
		$(this).siblings(".field-options-list").append($(createIntegrationFieldOptionElement()));
		CheckIntegrationTabHasChanges();
	});

	integrationFieldsList.on("click", ".remove-option-button", function () {
		$(this).closest(".field-option").remove();
		CheckIntegrationTabHasChanges();
	});

	integrationFieldsList.on("change", ".option-default-check", function () {
		const currentField = $(this).closest(".field-options-container");
		// Uncheck other default options in the same field
		currentField.find(".option-default-check").not(this).prop("checked", false);
		CheckIntegrationTabHasChanges();
	});

	// Form changes
	integrationManagerTab.on("input change", "input, select, textarea", () => {
		CheckIntegrationTabHasChanges();
	});

	// Logo preview
	integrationLogoInput.on("change", function () {
		const file = this.files[0];
		if (file) {
			const reader = new FileReader();
			reader.onload = function (e) {
				integrationLogoPreview.attr("src", e.target.result);
			};
			reader.readAsDataURL(file);
		}
	});

	// Save integration
	saveIntegrationButton.on("click", async (event) => {
		event.preventDefault();

		if (IsSavingIntegrationTab) return;

		const validationResult = ValidateIntegrationTab(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
				timeout: 6000,
			});
			return;
		}

		const changes = CheckIntegrationTabHasChanges(false);
		if (!changes.hasChanges) return;

		saveIntegrationButton.prop("disabled", true);
		const saveButtonSpinner = saveIntegrationButton.find(".spinner-border");
		saveButtonSpinner.removeClass("d-none");

		IsSavingIntegrationTab = true;

		const formData = new FormData();
		formData.append("changes", JSON.stringify(changes.changes));
		formData.append("postType", ManageIntegrationType);
		formData.append("currentIntegrationId", CurrentIntegrationData.id);

		if (integrationLogoInput[0].files.length > 0) {
			formData.append("logo", integrationLogoInput[0].files[0]);
		}

		SaveIntegrationToAPI(
			formData,
			(savedIntegration) => {
				AlertManager.createAlert({
					type: "success",
					message: `Integration ${ManageIntegrationType === "new" ? "added" : "updated"} successfully.`,
					timeout: 6000,
				});

				saveIntegrationButton.prop("disabled", true);
				saveButtonSpinner.addClass("d-none");
				IsSavingIntegrationTab = false;

				ShowIntegrationsListTab();
				FillIntegrationsList();
			},
			(error) => {
				AlertManager.createAlert({
					type: "danger",
					message: error.message || "Error saving integration. Please try again.",
					timeout: 6000,
				});

				console.error("Error saving integration:", error);

				saveIntegrationButton.prop("disabled", false);
				saveButtonSpinner.addClass("d-none");
				IsSavingIntegrationTab = false;
			},
		);
	});

	// Initial load
	FillIntegrationsList();
}

initIntegrationsTab();
