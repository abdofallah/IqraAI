/** Dynamic Variables **/
let CurrentTTSProviderType = null;
let CurrentTTSProviderData = null;

let CurrentTTSProviderSpeakerType = null;
let CurrentTTSProviderSpeakerData = null;

let IsSavingTTSProviderTab = false;

/** Elements Variables **/
const TTSProviderTab = $("#tts-provider-tab");

const ttsProviderInnerTab = TTSProviderTab.find("#tts-provider-inner-tab");
const ttsProviderManageBreadcrumb = TTSProviderTab.find("#tts-provider-manage-breadcrumb");

const switchBackToTTSProviderListTabFromManageTab = ttsProviderManageBreadcrumb.find("#switchBackToTTSProviderListTabFromManageTab");
const currentManageTTSProviderName = ttsProviderManageBreadcrumb.find("#currentManageTTSProviderName");

// Provider List Elements
const TTSProviderListTableTab = TTSProviderTab.find("#ttsProviderListTableTab");
const TTSProviderListTable = TTSProviderListTableTab.find("#ttsProviderListTable");
const searchTTSProviderInput = TTSProviderListTableTab.find("input[aria-label='TTS Provider Name or Id']");
const searchTTSProviderButton = TTSProviderListTableTab.find("#searchTTSProviderButton");

// Provider Manage Elements
const TTSProviderManageTab = TTSProviderTab.find("#ttsProviderManageTab");
const ttsProviderManagerInnerTabContainer = TTSProviderManageTab.find("#tts-provider-manager-inner-tab-container");
const ttsProviderManagerInnerTab = ttsProviderManagerInnerTabContainer.find("#tts-provider-manager-inner-tab");
const saveManageTTSProviderButton = ttsProviderManagerInnerTabContainer.find("#saveManageTTSProviderButton");

// Provider General Tab Elements
const manageTTSProviderIdInput = TTSProviderManageTab.find("#manageTTSProviderIdInput");
const manageTTSProviderIntegrationSelect = TTSProviderManageTab.find("#manageTTSProviderIntegrationSelect");
const manageTTSProviderDisabledInput = TTSProviderManageTab.find("#manageTTSProviderDisabledInput");

// Speaker Management Elements
const ttsProviderManagerSpeakersListTab = TTSProviderManageTab.find("#ttsProviderManagerSpeakersListTab");
const ttsProviderSpeakerListTable = ttsProviderManagerSpeakersListTab.find("#ttsProviderSpeakerListTable");
const addNewTTSProviderSpeakerButton = ttsProviderManagerSpeakersListTab.find("#addNewTTSProviderSpeakerButton");
const searchTTSProviderSpeakerInput = ttsProviderManagerSpeakersListTab.find("input[aria-label='Speaker Name or Id']");
const searchTTSProviderSpeakerButton = ttsProviderManagerSpeakersListTab.find("#searchTTSProviderSpeakerButton");

// Speaker Manage Elements
const ttsProviderSpeakerManagerBreadcrumb = TTSProviderTab.find("#tts-provider-speaker-manager-breadcrumb");
const currentManageSpeakerTTSProviderName = ttsProviderSpeakerManagerBreadcrumb.find("#currentManageSpeakerTTSProviderName");
const currentManageTTSProviderSpeakerName = ttsProviderSpeakerManagerBreadcrumb.find("#currentManageTTSProviderSpeakerName");
const switchBackToTTSProviderManagerSpeakersListTabFromSpeakerTab = ttsProviderSpeakerManagerBreadcrumb.find("#switchBackToTTSProviderManagerSpeakersListTabFromSpeakerTab");
const saveManageTTSProviderSpeakerButton = ttsProviderSpeakerManagerBreadcrumb.find("#saveManageTTSProviderSpeakerButton");

const ttsProviderManagerSpeakerManageTab = TTSProviderManageTab.find("#ttsProviderManagerSpeakerManageTab");
const manageTTSProviderSpeakerIdInput = ttsProviderManagerSpeakerManageTab.find("#manageTTSProviderSpeakerIdInput");
const manageTTSProviderSpeakerNameInput = ttsProviderManagerSpeakerManageTab.find("#manageTTSProviderSpeakerNameInput");
const manageTTSProviderSpeakerPriceInput = ttsProviderManagerSpeakerManageTab.find("#manageTTSProviderSpeakerPriceInput");
const manageTTSProviderSpeakerPriceUnitInput = ttsProviderManagerSpeakerManageTab.find("#manageTTSProviderSpeakerPriceUnitInput");
const manageTTSProviderSpeakerGenderSelect = ttsProviderManagerSpeakerManageTab.find("#manageTTSProviderSpeakerGenderSelect");
const manageTTSProviderSpeakerAgeGroupSelect = ttsProviderManagerSpeakerManageTab.find("#manageTTSProviderSpeakerAgeGroupSelect");
const manageTTSProviderSpeakerPersonalitySelect = ttsProviderManagerSpeakerManageTab.find("#manageTTSProviderSpeakerPersonalitySelect");
const manageTTSProviderSpeakerMultilingualInput = ttsProviderManagerSpeakerManageTab.find("#manageTTSProviderSpeakerMultilingualInput");
const manageTTSProviderSpeakerLanguagesContainer = ttsProviderManagerSpeakerManageTab.find("#manageTTSProviderSpeakerLanguagesContainer");
const manageTTSProviderSpeakerStylesContainer = ttsProviderManagerSpeakerManageTab.find("#manageTTSProviderSpeakerStylesContainer");
const addNewSpeakingStyleButton = ttsProviderManagerSpeakerManageTab.find("#addNewSpeakingStyleButton");
const manageTTSProviderSpeakerDisabledInput = ttsProviderManagerSpeakerManageTab.find("#manageTTSProviderSpeakerDisabledInput");

// Integration Fields Elements
const ttsProviderIntegrationsTab = $("#tts-provider-manager-integrations");
const addNewTTSProviderIntegrationFieldButton = ttsProviderIntegrationsTab.find("#addNewTTSProviderIntegrationFieldButton");
const ttsProviderIntegrationFieldsList = ttsProviderIntegrationsTab.find("#ttsProviderIntegrationFieldsList");
const searchTTSProviderIntegrationFieldInput = ttsProviderIntegrationsTab.find("input[aria-label='Search Field']");
const searchTTSProviderIntegrationFieldButton = ttsProviderIntegrationsTab.find("#searchTTSProviderIntegrationFieldButton");

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
	ttsProviderInnerTab.removeClass("show");

	setTimeout(() => {
		TTSProviderListTableTab.addClass("d-none");
		ttsProviderInnerTab.addClass("d-none");

		TTSProviderManageTab.removeClass("d-none");
		ttsProviderManageBreadcrumb.removeClass("d-none");

		setTimeout(() => {
			TTSProviderManageTab.addClass("show");
			ttsProviderManageBreadcrumb.addClass("show");
		}, 10);
	}, 300);
}

function ShowTTSProviderListTab() {
	TTSProviderManageTab.removeClass("show");
	ttsProviderManageBreadcrumb.removeClass("show");

	setTimeout(() => {
		TTSProviderManageTab.addClass("d-none");
		ttsProviderManageBreadcrumb.addClass("d-none");

		TTSProviderListTableTab.removeClass("d-none");
		ttsProviderInnerTab.removeClass("d-none");

		setTimeout(() => {
			TTSProviderListTableTab.addClass("show");
			ttsProviderInnerTab.addClass("show");
		}, 10);
	}, 300);
}

/** Provider Management Functions **/
function FillTTSProviderManageTab(providerData) {
	manageTTSProviderIdInput.val(providerData.id.name);
	manageTTSProviderDisabledInput.prop("checked", providerData.disabledAt != null);

	// Fill integration select
	fillTTSProviderIntegrationSelect();

	// Fill speakers table
	if (providerData.models.length != 0) {
		providerData.models.forEach((speakerData) => {
			ttsProviderSpeakerListTable.find("tbody").append(CreateTTSProviderSpeakerListTableElement(speakerData));
		});
	} else {
		ttsProviderSpeakerListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="8">No speakers</td></tr>');
	}

	// Fill integration fields
	fillTTSProviderIntegrationFields();
}

function ResetAndEmptyTTSProvidersManageTab() {
	manageTTSProviderIdInput.val("");
	manageTTSProviderDisabledInput.prop("checked", false).change();
	ttsProviderSpeakerListTable.find("tbody").empty();
	manageTTSProviderIntegrationSelect.val("").change();

	// Reset integration fields
	ttsProviderIntegrationFieldsList.empty();

	// Reset any validation states
	TTSProviderManageTab.find(".is-invalid").removeClass("is-invalid");
}

function CheckTTSProviderManageTabHasChanges(enableDisableButton = true) {
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
	changes.disabled = manageTTSProviderDisabledInput.prop("checked");
	if (changes.disabled === (CurrentTTSProviderData.disabledAt == null)) {
		hasChanges = true;
	}

	// Check integration selection
	changes.integrationId = manageTTSProviderIntegrationSelect.val();
	if (changes.integrationId !== CurrentTTSProviderData.integrationId) {
		hasChanges = true;
	}

	// Check integration fields
	const integrationFieldsData = [];
	ttsProviderIntegrationFieldsList.find(".integration-field").each(function () {
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
	if (integrationFieldsData.length !== CurrentTTSProviderData.userIntegrationFields.length) {
		hasChanges = true;
	} else {
		for (let i = 0; i < integrationFieldsData.length; i++) {
			const newField = integrationFieldsData[i];
			const oldField = CurrentTTSProviderData.userIntegrationFields[i];

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
	const integrationValidation = ValidateTTSProviderIntegrationFieldsTab(onlyRemove);
	if (!integrationValidation.validated) {
		validated = false;
		errors.push(...integrationValidation.errors);
	}

	return {
		validated: validated,
		errors: errors,
	};
}

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

function fillTTSProviderIntegrationFields() {
	ttsProviderIntegrationFieldsList.empty();

	if (!CurrentTTSProviderData.userIntegrationFields || CurrentTTSProviderData.userIntegrationFields.length === 0) {
		ttsProviderIntegrationFieldsList.append(`
            <div class="text-center p-5">
                <p class="text-muted mb-0">No integration fields defined</p>
            </div>
        `);
		return;
	}

	CurrentTTSProviderData.userIntegrationFields.forEach((field) => {
		ttsProviderIntegrationFieldsList.append($(createTTSProviderIntegrationFieldElement(field)));
	});
}

function createTTSProviderIntegrationFieldElement(fieldData = null) {
	const fieldId = fieldData?.id;

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
                            <option value="string" ${fieldData?.type === "string" ? "selected" : ""}>String</option>
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
                        ${fieldData?.options?.map((option) => createTTSIntegrationFieldOptionElement(option)).join("") || ""}
                    </div>
                    <button type="button" class="btn btn-outline-primary btn-sm mt-2 add-option-button">
                        <i class="fa-regular fa-plus"></i> Add Option
                    </button>
                </div>
            </div>
        </div>
    `;
}

function createTTSIntegrationFieldOptionElement(optionData = null) {
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

function ValidateTTSProviderIntegrationFieldsTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Get all fields
	ttsProviderIntegrationFieldsList.find(".integration-field").each(function (index) {
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
			const duplicateFields = ttsProviderIntegrationFieldsList
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

		// Optional: Validate tooltip and placeholder (if required)
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

/** Speaker Management Functions **/
function CreateTTSProviderSpeakerListTableElement(speakerData) {
	let disabledData = speakerData.disabledAt ? `<span class="badge bg-danger">${speakerData.disabledAt}</span>` : "-";

	let languagesCount = speakerData.supportedLanguages ? `${speakerData.supportedLanguages.length} ${speakerData.isMultilingual ? "(Multilingual)" : ""}` : "0";

	let stylesCount = speakerData.speakingStyles ? speakerData.speakingStyles.length : 0;

	let element = $(`
        <tr>
            <td>${speakerData.id}</td>
            <td>${speakerData.name}</td>
            <td>${languagesCount}</td>
            <td>${speakerData.gender || "-"}</td>
            <td>${speakerData.ageGroup || "-"}</td>
            <td>${stylesCount} styles</td>
            <td>${disabledData}</td>
            <td>
                <button class="btn btn-info btn-sm" speaker-id="${speakerData.id}" button-type="edit-tts-provider-speaker">
                    <i class="fa-regular fa-eye"></i>
                </button>
            </td>
        </tr>
    `);

	return element;
}

function CreateDefaultTTSProviderSpeakerObject() {
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

function ShowTTSProviderSpeakerManageTab() {
	ttsProviderManagerSpeakersListTab.removeClass("show");
	ttsProviderManagerInnerTabContainer.removeClass("show");
	ttsProviderManageBreadcrumb.removeClass("show");

	setTimeout(() => {
		ttsProviderManagerSpeakersListTab.addClass("d-none");
		ttsProviderManagerInnerTabContainer.addClass("d-none");
		ttsProviderManageBreadcrumb.addClass("d-none");

		ttsProviderManagerSpeakerManageTab.removeClass("d-none");
		ttsProviderSpeakerManagerBreadcrumb.removeClass("d-none");

		setTimeout(() => {
			ttsProviderManagerSpeakerManageTab.addClass("show");
			ttsProviderSpeakerManagerBreadcrumb.addClass("show");
		}, 10);
	}, 300);
}

function ShowTTSProviderSpeakerListTab() {
	ttsProviderManagerSpeakerManageTab.removeClass("show");
	ttsProviderSpeakerManagerBreadcrumb.removeClass("show");

	setTimeout(() => {
		ttsProviderManagerSpeakerManageTab.addClass("d-none");
		ttsProviderSpeakerManagerBreadcrumb.addClass("d-none");

		ttsProviderManagerSpeakersListTab.removeClass("d-none");
		ttsProviderManagerInnerTabContainer.removeClass("d-none");
		ttsProviderManageBreadcrumb.removeClass("d-none");

		setTimeout(() => {
			ttsProviderManagerSpeakersListTab.addClass("show");
			ttsProviderManagerInnerTabContainer.addClass("show");
			ttsProviderManageBreadcrumb.addClass("show");
		}, 10);
	}, 300);
}

function FillTTSProviderSpeakerManageTab(speakerData) {
	function GenerateLanguageCheckboxes(selectedLanguages = []) {
		manageTTSProviderSpeakerLanguagesContainer.empty();

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

			// Add change handler for single language mode
			checkbox.find("input").on("change", () => {
				const isMultilingual = manageTTSProviderSpeakerMultilingualInput.is(":checked");

				if (!isMultilingual && $(this).is(":checked")) {
					// Uncheck all other checkboxes
					manageTTSProviderSpeakerLanguagesContainer.find('input[type="checkbox"]').not(this).prop("checked", false);
				}
				CheckTTSProviderSpeakerManageTabHasChanges(true);
			});

			manageTTSProviderSpeakerLanguagesContainer.append(checkbox);
		});
	}

	manageTTSProviderSpeakerIdInput.val(speakerData.id);
	manageTTSProviderSpeakerNameInput.val(speakerData.name);
	manageTTSProviderSpeakerPriceInput.val(speakerData.pricePerUnit);
	manageTTSProviderSpeakerPriceUnitInput.val(speakerData.priceUnit);
	manageTTSProviderSpeakerGenderSelect.val(speakerData.gender);
	manageTTSProviderSpeakerAgeGroupSelect.val(speakerData.ageGroup);

	// Handle personality multi-select
	manageTTSProviderSpeakerPersonalitySelect.val(speakerData.personality || []);

	manageTTSProviderSpeakerMultilingualInput.prop("checked", speakerData.isMultilingual);
	manageTTSProviderSpeakerDisabledInput.prop("checked", speakerData.disabledAt != null);

	// Generate language checkboxes
	GenerateLanguageCheckboxes(speakerData.supportedLanguages || []);

	// Fill speaking styles
	manageTTSProviderSpeakerStylesContainer.empty();
	if (speakerData.speakingStyles && speakerData.speakingStyles.length > 0) {
		speakerData.speakingStyles.forEach((style) => {
			AddSpeakingStyle(style);
		});
	}
}

function ResetAndEmptyTTSProviderSpeakerManageTab() {
	manageTTSProviderSpeakerIdInput.val("").removeClass("is-invalid");
	manageTTSProviderSpeakerNameInput.val("").removeClass("is-invalid");
	manageTTSProviderSpeakerPriceInput.val("").removeClass("is-invalid");
	manageTTSProviderSpeakerPriceUnitInput.val("").removeClass("is-invalid");
	manageTTSProviderSpeakerGenderSelect.val("").removeClass("is-invalid");
	manageTTSProviderSpeakerAgeGroupSelect.val("").removeClass("is-invalid");
	manageTTSProviderSpeakerPersonalitySelect.val([]).removeClass("is-invalid");
	manageTTSProviderSpeakerMultilingualInput.prop("checked", false);
	manageTTSProviderSpeakerDisabledInput.prop("checked", false);

	manageTTSProviderSpeakerLanguagesContainer.empty();
	manageTTSProviderSpeakerStylesContainer.empty();

	saveManageTTSProviderSpeakerButton.prop("disabled", true);
}

function CheckTTSProviderSpeakerManageTabHasChanges(enableDisableButton = true) {
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
	changes.id = manageTTSProviderSpeakerIdInput.val().trim();
	if (changes.id !== CurrentTTSProviderSpeakerData.id) {
		hasChanges = true;
	}

	changes.name = manageTTSProviderSpeakerNameInput.val().trim();
	if (changes.name !== CurrentTTSProviderSpeakerData.name) {
		hasChanges = true;
	}

	// Check price settings
	changes.pricePerUnit = manageTTSProviderSpeakerPriceInput.val();
	if (changes.pricePerUnit !== CurrentTTSProviderSpeakerData.pricePerUnit) {
		hasChanges = true;
	}

	changes.priceUnit = manageTTSProviderSpeakerPriceUnitInput.val().trim();
	if (changes.priceUnit !== CurrentTTSProviderSpeakerData.priceUnit) {
		hasChanges = true;
	}

	// Check characteristics
	changes.gender = manageTTSProviderSpeakerGenderSelect.val();
	if (changes.gender !== CurrentTTSProviderSpeakerData.gender) {
		hasChanges = true;
	}

	changes.ageGroup = manageTTSProviderSpeakerAgeGroupSelect.val();
	if (changes.ageGroup !== CurrentTTSProviderSpeakerData.ageGroup) {
		hasChanges = true;
	}

	changes.personality = manageTTSProviderSpeakerPersonalitySelect.val();
	if (!arraysEqual(changes.personality, CurrentTTSProviderSpeakerData.personality)) {
		hasChanges = true;
	}

	// Check language settings
	changes.isMultilingual = manageTTSProviderSpeakerMultilingualInput.is(":checked");
	if (changes.isMultilingual !== CurrentTTSProviderSpeakerData.isMultilingual) {
		hasChanges = true;
	}

	changes.supportedLanguages = [];
	manageTTSProviderSpeakerLanguagesContainer.find('input[type="checkbox"]:checked').each(function () {
		changes.supportedLanguages.push($(this).val());
	});
	if (!arraysEqual(changes.supportedLanguages, CurrentTTSProviderSpeakerData.supportedLanguages)) {
		hasChanges = true;
	}

	// Check speaking styles
	changes.speakingStyles = [];
	manageTTSProviderSpeakerStylesContainer.find(".speaking-style").each(function () {
		const style = $(this);
		const styleData = {
			id: style.find(".style-id-input").val().trim(),
			name: style.find(".style-name-input").val().trim(),
			previewUrl: style.find(".style-preview-url-input").val().trim(),
			isDefault: style.find(".style-default-check").is(":checked"),
		};
		changes.speakingStyles.push(styleData);
	});

	if (!speakingStylesAreEqual(changes.speakingStyles, CurrentTTSProviderSpeakerData.speakingStyles)) {
		hasChanges = true;
	}

	// Check disabled state
	changes.disabled = manageTTSProviderSpeakerDisabledInput.prop("checked");
	if (changes.disabled === (CurrentTTSProviderSpeakerData.disabledAt === null)) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveManageTTSProviderSpeakerButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function ValidateTTSProviderSpeakerManageTabFields(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Validate ID
	const speakerId = manageTTSProviderSpeakerIdInput.val().trim();
	if (!speakerId) {
		validated = false;
		errors.push("Speaker ID is required");
		if (!onlyRemove) manageTTSProviderSpeakerIdInput.addClass("is-invalid");
	} else {
		manageTTSProviderSpeakerIdInput.removeClass("is-invalid");
	}

	// Validate Name
	const speakerName = manageTTSProviderSpeakerNameInput.val().trim();
	if (!speakerName) {
		validated = false;
		errors.push("Speaker name is required");
		if (!onlyRemove) manageTTSProviderSpeakerNameInput.addClass("is-invalid");
	} else {
		manageTTSProviderSpeakerNameInput.removeClass("is-invalid");
	}

	// Validate Price
	const price = manageTTSProviderSpeakerPriceInput.val();
	if (!price || isNaN(price) || parseFloat(price) <= 0) {
		validated = false;
		errors.push("Valid price is required");
		if (!onlyRemove) manageTTSProviderSpeakerPriceInput.addClass("is-invalid");
	} else {
		manageTTSProviderSpeakerPriceInput.removeClass("is-invalid");
	}

	// Validate Price Unit
	const priceUnit = manageTTSProviderSpeakerPriceUnitInput.val().trim();
	if (!priceUnit) {
		validated = false;
		errors.push("Price unit is required");
		if (!onlyRemove) manageTTSProviderSpeakerPriceUnitInput.addClass("is-invalid");
	} else {
		manageTTSProviderSpeakerPriceUnitInput.removeClass("is-invalid");
	}

	// Validate Gender
	const gender = manageTTSProviderSpeakerGenderSelect.val();
	if (!gender) {
		validated = false;
		errors.push("Gender selection is required");
		if (!onlyRemove) manageTTSProviderSpeakerGenderSelect.addClass("is-invalid");
	} else {
		manageTTSProviderSpeakerGenderSelect.removeClass("is-invalid");
	}

	// Validate Age Group
	const ageGroup = manageTTSProviderSpeakerAgeGroupSelect.val();
	if (!ageGroup) {
		validated = false;
		errors.push("Age group selection is required");
		if (!onlyRemove) manageTTSProviderSpeakerAgeGroupSelect.addClass("is-invalid");
	} else {
		manageTTSProviderSpeakerAgeGroupSelect.removeClass("is-invalid");
	}

	// Validate Languages
	const selectedLanguages = manageTTSProviderSpeakerLanguagesContainer.find('input[type="checkbox"]:checked').length;
	if (selectedLanguages === 0) {
		validated = false;
		errors.push("At least one language must be selected");
		if (!onlyRemove) manageTTSProviderSpeakerLanguagesContainer.addClass("is-invalid");
	} else {
		manageTTSProviderSpeakerLanguagesContainer.removeClass("is-invalid");
	}

	// Validate Speaking Styles
	const speakingStyles = manageTTSProviderSpeakerStylesContainer.find(".speaking-style");
	if (speakingStyles.length === 0) {
		validated = false;
		errors.push("At least one speaking style is required");
	} else {
		let hasDefault = false;
		speakingStyles.each(function (index) {
			const style = $(this);
			const styleId = style.find(".style-id-input").val().trim();
			const styleName = style.find(".style-name-input").val().trim();
			const previewUrl = style.find(".style-preview-url-input").val().trim();

			if (!styleId || !styleName || !previewUrl) {
				validated = false;
				errors.push(`Speaking style ${index + 1}: All fields are required`);
				if (!onlyRemove) {
					if (!styleId) style.find(".style-id-input").addClass("is-invalid");
					if (!styleName) style.find(".style-name-input").addClass("is-invalid");
					if (!previewUrl) style.find(".style-preview-url-input").addClass("is-invalid");
				}
			} else {
				style.find("input").removeClass("is-invalid");
			}

			if (style.find(".style-default-check").is(":checked")) {
				hasDefault = true;
			}
		});

		if (!hasDefault) {
			validated = false;
			errors.push("At least one speaking style must be set as default");
		}
	}

	return {
		validated: validated,
		errors: errors,
	};
}

/** Speaking Styles Functions **/
function AddSpeakingStyle(styleData = null) {
	const styleElement = $(`
        <div class="card mb-3 speaking-style">
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-start mb-3">
                    <h6 class="card-title mb-0">Speaking Style</h6>
                    <button type="button" class="btn btn-danger btn-sm remove-style-button">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </div>
                <div class="row mb-3">
                    <div class="col-md-6">
                        <label class="form-label">Style ID</label>
                        <input type="text" class="form-control style-id-input" placeholder="Style ID" 
                               value="${styleData?.id || ""}">
                    </div>
                    <div class="col-md-6">
                        <label class="form-label">Style Name</label>
                        <input type="text" class="form-control style-name-input" placeholder="Style Name" 
                               value="${styleData?.name || ""}">
                    </div>
                </div>
                <div class="mb-3">
                    <label class="form-label">Preview URL</label>
                    <input type="url" class="form-control style-preview-url-input" placeholder="Preview URL" 
                           value="${styleData?.previewUrl || ""}">
                </div>
                <div class="form-check">
                    <input class="form-check-input style-default-check" type="checkbox" 
                           ${styleData?.isDefault ? "checked" : ""}>
                    <label class="form-check-label">Default Style</label>
                </div>
            </div>
        </div>
    `);

	manageTTSProviderSpeakerStylesContainer.append(styleElement);
	CheckTTSProviderSpeakerManageTabHasChanges(true);
}

/** Helper Functions **/

function speakingStylesAreEqual(styles1, styles2) {
	if (!styles1 || !styles2) return false;
	if (styles1.length !== styles2.length) return false;

	for (let i = 0; i < styles1.length; i++) {
		const style1 = styles1[i];
		const style2 = styles2[i];

		if (style1.id !== style2.id || style1.name !== style2.name || style1.previewUrl !== style2.previewUrl || style1.isDefault !== style2.isDefault) {
			return false;
		}
	}

	return true;
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

	// Speaker List Events
	addNewTTSProviderSpeakerButton.on("click", (event) => {
		event.preventDefault();

		CurrentTTSProviderSpeakerData = CreateDefaultTTSProviderSpeakerObject();

		currentManageSpeakerTTSProviderName.text(CurrentTTSProviderData.id.name);
		currentManageTTSProviderSpeakerName.text("New Speaker");

		ResetAndEmptyTTSProviderSpeakerManageTab();
		FillTTSProviderSpeakerManageTab(CurrentTTSProviderSpeakerData);

		CurrentTTSProviderSpeakerType = "new";
		ShowTTSProviderSpeakerManageTab();
	});

	ttsProviderSpeakerListTable.on("click", "button[button-type=edit-tts-provider-speaker]", (event) => {
		event.preventDefault();

		let speakerId = $(event.currentTarget).attr("speaker-id");
		CurrentTTSProviderSpeakerData = CurrentTTSProviderData.models.find((speaker) => speaker.id === speakerId);

		currentManageSpeakerTTSProviderName.text(CurrentTTSProviderData.id.name);
		currentManageTTSProviderSpeakerName.text(CurrentTTSProviderSpeakerData.name);

		ResetAndEmptyTTSProviderSpeakerManageTab();
		FillTTSProviderSpeakerManageTab(CurrentTTSProviderSpeakerData);

		CurrentTTSProviderSpeakerType = "edit";
		ShowTTSProviderSpeakerManageTab();
	});

	// Speaker Form Events
	switchBackToTTSProviderManagerSpeakersListTabFromSpeakerTab.on("click", (event) => {
		event.preventDefault();
		CurrentTTSProviderSpeakerType = null;
		ShowTTSProviderSpeakerListTab();
	});

	// Form input change handlers
	ttsProviderManagerSpeakerManageTab.on("input change", "input, select", () => {
		if (CurrentTTSProviderSpeakerType === null) return;
		CheckTTSProviderSpeakerManageTabHasChanges(true);
	});

	// Multilingual toggle handler
	manageTTSProviderSpeakerMultilingualInput.on("change", function () {
		const isMultilingual = $(this).is(":checked");
		const languageCheckboxes = manageTTSProviderSpeakerLanguagesContainer.find('input[type="checkbox"]');

		if (!isMultilingual) {
			// If switching to single language, uncheck all except the first checked one
			const checkedBoxes = languageCheckboxes.filter(":checked");
			if (checkedBoxes.length > 1) {
				checkedBoxes.not(":first").prop("checked", false);
			}
		}

		CheckTTSProviderSpeakerManageTabHasChanges(true);
	});

	// Speaking Styles Events
	addNewSpeakingStyleButton.on("click", (event) => {
		event.preventDefault();
		AddSpeakingStyle();
	});

	manageTTSProviderSpeakerStylesContainer.on("click", ".remove-style-button", function (event) {
		event.preventDefault();
		$(this).closest(".speaking-style").remove();
		CheckTTSProviderSpeakerManageTabHasChanges(true);
	});

	// Handle default style selection
	manageTTSProviderSpeakerStylesContainer.on("change", ".style-default-check", function () {
		if ($(this).is(":checked")) {
			// Uncheck other default checkboxes
			manageTTSProviderSpeakerStylesContainer.find(".style-default-check").not(this).prop("checked", false);
		}
		CheckTTSProviderSpeakerManageTabHasChanges(true);
	});

	// Preview URL audio player
	manageTTSProviderSpeakerStylesContainer.on("change", ".style-preview-url-input", function () {
		function isValidUrl(string) {
			try {
				new URL(string);
				return true;
			} catch (_) {
				return false;
			}
		}

		const url = $(this).val().trim();
		const audioPlayer = $(this).siblings(".audio-preview");

		if (url && isValidUrl(url)) {
			if (audioPlayer.length === 0) {
				$(this).after(`
                    <audio controls class="audio-preview mt-2 w-100">
                        <source src="${url}" type="audio/mpeg">
                        Your browser does not support the audio element.
                    </audio>
                `);
			} else {
				audioPlayer.find("source").attr("src", url);
				audioPlayer[0].load();
			}
		} else {
			audioPlayer.remove();
		}
	});

	// Save Speaker Button Handler
	saveManageTTSProviderSpeakerButton.on("click", (event) => {
		event.preventDefault();
		if (IsSavingTTSProviderTab) return;

		const validationResult = ValidateTTSProviderSpeakerManageTabFields(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
				timeout: 6000,
			});
			return;
		}

		const changes = CheckTTSProviderSpeakerManageTabHasChanges(false);
		if (!changes.hasChanges) return;

		IsSavingTTSProviderTab = true;
		saveManageTTSProviderSpeakerButton.prop("disabled", true);

		const formData = new FormData();
		formData.append("providerId", CurrentTTSProviderData.id.value);
		formData.append("speakerId", changes.changes.id);
		formData.append("postType", CurrentTTSProviderSpeakerType);
		formData.append("changes", JSON.stringify(changes.changes));

		SaveTTSProviderSpeakerData(
			formData,
			(saveResponse) => {
				if (saveResponse.success) {
					// Update the current speaker data
					CurrentTTSProviderSpeakerData = saveResponse.data;

					// Update the speakers list in the provider data
					const speakerIndex = CurrentTTSProviderData.models.findIndex((s) => s.id === CurrentTTSProviderSpeakerData.id);

					if (speakerIndex !== -1) {
						CurrentTTSProviderData.models[speakerIndex] = CurrentTTSProviderSpeakerData;
					} else {
						CurrentTTSProviderData.models.push(CurrentTTSProviderSpeakerData);
					}

					// Update the speakers table
					const speakerRow = ttsProviderSpeakerListTable.find(`tr button[speaker-id="${CurrentTTSProviderSpeakerData.id}"]`).closest("tr");

					if (speakerRow.length) {
						speakerRow.replaceWith($(CreateTTSProviderSpeakerListTableElement(CurrentTTSProviderSpeakerData)));
					} else {
						ttsProviderSpeakerListTable.find("tbody tr[tr-type='none-notice']").remove();
						ttsProviderSpeakerListTable.find("tbody").append($(CreateTTSProviderSpeakerListTableElement(CurrentTTSProviderSpeakerData)));
					}

					AlertManager.createAlert({
						type: "success",
						message: "TTS provider speaker saved successfully.",
						timeout: 6000,
					});

					ShowTTSProviderSpeakerListTab();
				} else {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving TTS provider speaker.",
						timeout: 6000,
					});
				}

				saveManageTTSProviderSpeakerButton.prop("disabled", false);
				IsSavingTTSProviderTab = false;
			},
			(error, isUnsuccessful) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occurred while saving TTS provider speaker.",
					timeout: 6000,
				});
				console.error("Save error:", error);

				saveManageTTSProviderSpeakerButton.prop("disabled", false);
				IsSavingTTSProviderTab = false;
			},
		);
	});

	// Search functionality
	searchTTSProviderSpeakerButton.on("click", (event) => {
		event.preventDefault();
		const searchTerm = searchTTSProviderSpeakerInput.val().toLowerCase().trim();

		ttsProviderSpeakerListTable.find("tbody tr").each(function () {
			const row = $(this);
			if (row.attr("tr-type") === "none-notice") return;

			const speakerId = row.find("td:first").text().toLowerCase();
			const speakerName = row.find("td:eq(1)").text().toLowerCase();

			if (speakerId.includes(searchTerm) || speakerName.includes(searchTerm)) {
				row.show();
			} else {
				row.hide();
			}
		});
	});

	// Add new integration field
	addNewTTSProviderIntegrationFieldButton.on("click", (event) => {
		event.preventDefault();
		ttsProviderIntegrationFieldsList.find(".text-center").remove();
		ttsProviderIntegrationFieldsList.append($(createTTSProviderIntegrationFieldElement()));
		CheckTTSProviderManageTabHasChanges(true);
	});

	// Handle field type changes
	ttsProviderIntegrationsTab.on("change", ".field-type-select", function () {
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

		CheckTTSProviderManageTabHasChanges(true);
	});

	// Handle field removal
	ttsProviderIntegrationsTab.on("click", ".remove-field-button", function () {
		$(this).closest(".integration-field").remove();
		if (ttsProviderIntegrationFieldsList.children().length === 0) {
			fillTTSProviderIntegrationFields();
		}
		CheckTTSProviderManageTabHasChanges(true);
	});

	// Handle option management
	ttsProviderIntegrationsTab.on("click", ".add-option-button", function () {
		$(this).siblings(".field-options-list").append($(createTTSIntegrationFieldOptionElement()));
		CheckTTSProviderManageTabHasChanges(true);
	});

	ttsProviderIntegrationsTab.on("click", ".remove-option-button", function () {
		$(this).closest(".field-option").remove();
		CheckTTSProviderManageTabHasChanges(true);
	});

	// Handle input changes
	ttsProviderIntegrationsTab.on("input change", "input, select", () => {
		CheckTTSProviderManageTabHasChanges(true);
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
