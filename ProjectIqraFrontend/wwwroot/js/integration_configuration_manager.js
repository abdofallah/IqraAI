/**
 * IntegrationConfigurationManager Class
 * A reusable component for selecting and configuring integrations like LLM, STT, and TTS.
 *
 * @version 1.1.0
 */
class IntegrationConfigurationManager {
	static _modalInstance = null;
	static _modalElement = null;
	static _activeManager = null;

	/**
	 * Creates an instance of IntegrationConfigurationManager.
	 * @param {string} containerSelector - The CSS selector for the container div where the UI will be rendered.
	 * @param {object} options - Configuration options for this manager instance.
	 * @param {string} options.integrationType - The type of integration to manage ('LLM', 'STT', 'TTS').
	 * @param {boolean} options.allowMultiple - If true, the user can add multiple integrations.
	 * @param {boolean} options.isLanguageBound - If true, data is structured by language codes.
	 * @param {object|null} options.languageDropdown - A reference to a language dropdown object that has an onLanguageChange method.
	 * @param {Array} options.allIntegrations - The global array of all available business integrations.
	 * @param {Array} options.providersData - The global array of provider-specific data (userIntegrationFields, models).
	 * @param {string} options.modalSelector - The CSS selector for the shared configuration modal.
	 * @param {function} [options.onSaveSuccessful] - Optional callback function to execute when data is successfully changed or saved.
	 * @param {function} [options.onIntegrationChange] - Optional callback function to execute when integration changes.
	 */
	constructor(containerSelector, options) {
		this.container = $(containerSelector);
		if (this.container.length === 0) {
			console.error(`IntegrationConfigurationManager: Container with selector "${containerSelector}" not found.`);
			return;
		}

		// --- Default options and merging with provided options ---
		this.options = {
			onSaveSuccessful: () => { },
            onIntegrationChange: () => { },
			...options,
		};

		// --- State Variables ---
		this.data = this.options.isLanguageBound ? {} : this.options.allowMultiple ? [] : null;
		this.filteredIntegrations = this.options.allIntegrations.filter((integration) => {
			const typeData = SpecificationIntegrationsListData.find((it) => it.id === integration.type);
			const type = this.options.integrationType.toUpperCase();
			// Handle aliases like SPEECH2TEXT for STT
			return typeData.type.includes(type) || (type === "STT" && typeData.type.includes("SPEECH2TEXT")) || (type === "TTS" && typeData.type.includes("TEXT2SPEECH"));
		});

		// --- Modal State Variables ---
		this.currentConfig = {
			integration: null, // The integration object from this.data being configured
			provider: null, // The provider data for the integration
		};

		if (!IntegrationConfigurationManager._modalInstance) {
			IntegrationConfigurationManager._modalElement = $(this.options.modalSelector);
			if (IntegrationConfigurationManager._modalElement.length) {
				IntegrationConfigurationManager._modalInstance = new bootstrap.Modal(IntegrationConfigurationManager._modalElement[0]);
				this._attachSharedModalEventListeners(); // Attach listeners only ONCE
			} else {
				console.error(`Integration Modal with selector "${this.options.modalSelector}" not found.`);
			}
		}

		this._init();
	}

	// =================================================================
	//  Public API Methods
	// =================================================================

	/**
	 * Loads initial integration data into the manager and renders the UI.
	 * @param {object|Array|null} data - The initial configuration data.
	 */
	load(data) {
		this.data = structuredClone(data); // Deep copy to prevent reference issues
		this._render();
	}

	/**
	 * Retrieves the current, structured data from the manager.
	 * @returns {object|Array|null} The current configuration data.
	 */
	getData() {
		return this.data;
	}

	/**
	 * Resets the manager to its initial, empty state and clears the UI.
	 */
	reset() {
		this.data = this.options.isLanguageBound ? {} : this.options.allowMultiple ? [] : null;
		this._render();
	}

	/**
	 * Disable the manager, making it read-only and uneditable.
	 */
    disable() {
        this._disable();
    }

	/**
	 * Public method to validate the configuration of all managed integrations.
	 * Note: This checks the *configuration* of selected integrations, not whether an integration *has been* selected.
	 * @returns {{isValid: boolean, errors: Array<string>}} An object containing the overall validation status and a list of all errors found.
	 */
	validate() {
		const allErrors = [];
		let isAllValid = true;

		const processIntegration = (integration, context) => {
			if (!integration || !integration.id) {
				return; // Don't validate empty/unselected integrations
			}
			const validationResult = this._validateConfiguration(integration, false); // No UI errors from this public method
			if (!validationResult.isValid) {
				isAllValid = false;
				validationResult.errors.forEach(err => allErrors.push(`${context}${err}`));
			}
		};

		if (this.options.isLanguageBound) {
			const langKey = this.options.languageDropdown.getSelectedLanguage().id;
			const currentDataSet = this.data[langKey] || [];
			const context = `Language ${langKey}: `;
			if (this.options.allowMultiple) {
				currentDataSet.forEach((integration, index) => {
					const def = this.filteredIntegrations.find(i => i.id === integration.id);
					const name = def ? `'${def.friendlyName}'` : `Integration #${index + 1}`;
					processIntegration(integration, `${context}${name} - `);
				});
			} else {
				processIntegration(currentDataSet, context);
			}
		} else {
			// This is the routing tab scenario
			const currentDataSet = this.data;
			if (this.options.allowMultiple) {
				(currentDataSet || []).forEach((integration, index) => {
					const def = this.filteredIntegrations.find(i => i.id === integration.id);
					const name = def ? `'${def.friendlyName}'` : `Integration #${index + 1}`;
					processIntegration(integration, `${name} - `);
				});
			} else {
				if (currentDataSet && currentDataSet.id) {
					const def = this.filteredIntegrations.find(i => i.id === currentDataSet.id);
					const name = def ? `'${def.friendlyName}'` : `${this.options.integrationType} Integration`;
					processIntegration(currentDataSet, `${name} - `);
				}
			}
		}
		return {
			isValid: isAllValid,
			errors: allErrors
		};
	}

	/**
	 * Publicly exposes the select element(s) managed by this instance.
	 * @returns {jQuery} A jQuery object containing the select element(s).
	 */
	getSelectElements() {
		return this.container.find('select');
	}


	// =================================================================
	//  Private Initialization & Rendering Methods
	// =================================================================

	/**
	 * Initializes event listeners.
	 * @private
	 */
	_init() {
		this._attachContainerEventListeners();
		this._render(); // Initial render
	}

	/**
	 * Renders the entire UI for the component based on the current state.
	 * @private
	 */
	_render() {
		this.container.empty();
		const currentDataSet = this._getCurrentDataSet();

		if (this.options.allowMultiple) {
			if (currentDataSet && currentDataSet.length > 0) {
				currentDataSet.forEach((integration, index) => {
					const element = this._createIntegrationElement(integration, index);
					this.container.append(element);
				});
			}
			// Always show 'Add' button for multiple-allowed contexts
			const addButton = $(`<button class="btn btn-light mt-2"><i class="fa-regular fa-plus me-2"></i><span>Add Integration</span></button>`);
			this.container.append(addButton);
		} else {
			// Single integration (select with configure button)
			const element = this._createIntegrationElement(currentDataSet, 0);
			this.container.append(element);
		}

		// Initialize tooltips for any new elements
		const tooltipTriggerList = this.container.find('[data-bs-toggle="tooltip"]');
		[...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
	}

	/**
	 * Creates the HTML for a single integration selection row.
	 * @private
	 * @param {object|null} integrationData - The data for the integration to render.
	 * @param {number} index - The index of the integration in its array.
	 * @returns {jQuery} A jQuery object representing the new element.
	 */
	_createIntegrationElement(integrationData, index) {
		const selectId = `integration-manager-${this.options.integrationType}-${Date.now()}-${index}`;

		let optionsHtml = '<option value="">Select Integration</option>';
		this.filteredIntegrations.forEach((integration) => {
			optionsHtml += `<option value="${integration.id}" ${integrationData?.id === integration.id ? "selected" : ""}>${integration.friendlyName}</option>`;
		});

		const isConfigureDisabled = !integrationData?.id;

		const elementHtml = `
            <div class="input-group integration-item" data-index="${index}">
                ${this.options.allowMultiple ? `<span class="input-group-text"><i class="fa-regular fa-${index + 1}"></i></span>` : ""}
                <select class="form-select" id="${selectId}">
                    ${optionsHtml}
                </select>
                <button class="btn btn-secondary" button-type="configure" data-bs-toggle="tooltip" data-bs-title="Configure Integration" ${isConfigureDisabled ? "disabled" : ""}>
                    <i class="fa-regular fa-gear"></i>
                </button>
                ${this.options.allowMultiple ? `<button class="btn btn-danger" button-type="remove"><i class="fa-regular fa-trash"></i></button>` : ""}
            </div>
        `;
		return $(elementHtml);
	}

	/**
	 * Renders the configuration fields inside the modal.
	 * @private
	 */
	_renderModalFields() {
		const fieldsContainer = IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first();

		fieldsContainer.empty();
		if (!this.currentConfig.provider?.userIntegrationFields) return;

		this.currentConfig.provider.userIntegrationFields.forEach(field => {
			const fieldElement = this._createModalFieldElement(field);
			fieldsContainer.append(fieldElement);

			if (field.type === "models") {
				this._populateModelsField(field, fieldElement);
			}
		});

		// Init tooltips inside modal
		const tooltips = fieldsContainer.find('[data-bs-toggle="tooltip"]');
		tooltips.each((index, element) => new bootstrap.Tooltip(element));
	}

	/**
	 * Creates the HTML for a single field within the configuration modal.
	 * @private
	 * @param {object} field - The field schema object.
	 * @returns {jQuery} A jQuery object for the field.
	 */
	_createModalFieldElement(field) {
		const currentValue = this.currentConfig.integration.fieldValues?.[field.id] ?? field.defaultValue ?? "";
		let fieldHtml = "";

		const labelHtml = `
            <label class="form-label btn-ic-span-align">
                <span>${field.name} ${field.required ? '<span class="text-danger">*</span>' : ""}</span>
                ${field.tooltip ? `
                    <a href="#" class="d-inline-block" data-bs-html="true" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${field.tooltip}">
                        <i class="fa-regular fa-circle-question"></i>
                    </a>` : ""}
            </label>`;

		switch (field.type) {
			case "text":
			case "string":
			case "number":
			case "double_number":
				fieldHtml = `
					<div class="mb-3 config-field" data-field-id="${field.id}">
						${labelHtml}
						<input type="${field.isEncrypted ? 'password' : (field.type.includes('number') ? 'number' : 'text')}"
							class="form-control config-field-input"
							placeholder="${field.placeholder || ""}"
							value="${currentValue}">
					</div>`;
				break;

			case "select": {
				const options = field.options?.map(opt => `<option value="${opt.key}" ${currentValue === opt.key ? "selected" : ""}>${opt.value}</option>`).join("") || "";
				fieldHtml = `
					<div class="mb-3 config-field" data-field-id="${field.id}">
						${labelHtml}
						<select class="form-select config-field-input">
							<option value="" disabled ${currentValue === "" ? "selected" : ""}>Select ${field.name}</option>
							${options}
						</select>
					</div>`;
				break;
			}

			case "models":
				fieldHtml = `
					<div class="mb-3 config-field" data-field-id="${field.id}">
						${labelHtml}
						<select class="form-select config-field-input">
							<option value="" disabled ${!currentValue ? "selected" : ""}>Select ${field.name}</option>
						</select>
					</div>`;
				break;

			case "boolean":
				const isChecked = currentValue === true;
				fieldHtml = `
                    <div class="mb-3 config-field form-check" data-field-id="${field.id}">
                        <input type="checkbox" class="form-check-input config-field-input" id="check-${field.id}" ${isChecked ? "checked" : ""}>
                        <label class="form-check-label" for="check-${field.id}">
							<span class="btn-ic-span-align">
								<span>${field.name} ${field.required ? '<span class="text-danger">*</span>' : ""}</span>
								${field.tooltip ? `
									<a href="#" class="d-inline-block" data-bs-html="true" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${field.tooltip}">
										<i class="fa-regular fa-circle-question"></i>
									</a>` : ""}
							</span>
                        </label>
                    </div>`;
				break;
		}

		return $(fieldHtml);
	}

	/**
	 * Populates a "models" type select field with available models.
	 * @private
	 */
	_populateModelsField(field, fieldElement) {
		const provider = this.currentConfig.provider;
		if (!provider?.models) return;

		const selectElement = fieldElement.find('select');
		const currentValue = this.currentConfig.integration.fieldValues?.[field.id];

		const enabledModels = provider.models.filter(model => model.disabledAt === null);
		enabledModels.forEach(model => {
			selectElement.append(`<option value="${model.id}" ${currentValue === model.id ? "selected" : ""}>${model.name}</option>`);
		});
	}

	// =================================================================
	//  Private Event Handling Methods
	// =================================================================

	/**
	 * Attaches event listeners for the integration items within this manager's container.
	 */
	_attachContainerEventListeners() {
		this.container.on("change", "select", this._handleIntegrationSelect.bind(this));
		this.container.on("click", 'button[button-type="configure"]', this._handleConfigureClick.bind(this));
		this.container.on("click", ".btn-light", (e) => {
			if (!this.options.allowMultiple) return;
			e.preventDefault();
			this._addIntegration();
		});
		this.container.on("click", 'button[button-type="remove"]', (e) => {
			if (!this.options.allowMultiple) return;
			e.preventDefault();
			const index = $(e.currentTarget).closest(".integration-item").data("index");
			this._removeIntegration(index);
		});

		// Language dropdown listener remains instance-specific
		if (this.options.isLanguageBound && this.options.languageDropdown) {
			this.options.languageDropdown.onLanguageChange(() => this._render());
		}
	}

	/**
	 * This method is called only ONCE by the first instance created.
	 */
	_attachSharedModalEventListeners() {
		const modalEl = IntegrationConfigurationManager._modalElement;
		const saveButton = modalEl.find(".modal-footer button[button-type='save-integration-configuration']");

		saveButton.on("click", () => {
			if (IntegrationConfigurationManager._activeManager) {
				const isSuccess = IntegrationConfigurationManager._activeManager._handleSaveConfiguration();
				if (isSuccess) {
					IntegrationConfigurationManager._modalInstance.hide();
				}
			}
		});

		modalEl.on("hidden.bs.modal", () => {
			if (IntegrationConfigurationManager._activeManager) {
				IntegrationConfigurationManager._activeManager._handleModalClose();
			}
		});

		modalEl.find(".modal-body div[div-type='integration-configuration-fields']").on("input change", ".config-field-input", () => {
			if (IntegrationConfigurationManager._activeManager) {
				const changes = IntegrationConfigurationManager._activeManager._getModalChanges();
				saveButton.prop("disabled", !changes.hasChanges);
			}
		});
	}

	_handleIntegrationSelect(e) {
		const select = $(e.currentTarget);
		const integrationId = select.val();
		const itemElement = select.closest(".integration-item");
		const index = itemElement.data("index");

		// Enable/disable configure button
		itemElement.find('button[button-type="configure"]').prop("disabled", !integrationId);

		// Get the relevant data array/object
		let currentDataSet = this._getCurrentDataSet();

		const newIntegrationData = {
			id: integrationId,
			fieldValues: {},
		};

		// Set default values for the newly selected integration
		if (integrationId) {
			const integrationDefinition = this.options.allIntegrations.find(i => i.id === integrationId);
			const provider = this.options.providersData.find(p => p.integrationId === integrationDefinition.type);
			provider?.userIntegrationFields.forEach(field => {
				if (field.defaultValue !== undefined && field.defaultValue !== null) {
					newIntegrationData.fieldValues[field.id] = field.defaultValue;
				}
			});
		}

		if (this.options.allowMultiple) {
			// Ensure the dataset exists before trying to assign to an index
			if (!currentDataSet) {
				currentDataSet = [];
				if (this.options.isLanguageBound) {
					const langKey = this.options.languageDropdown.getSelectedLanguage().id;
					this.data[langKey] = currentDataSet;
				}
			}
			currentDataSet[index] = newIntegrationData;
		} else {
			// For single integration, the data itself is the object
			this.data = integrationId ? newIntegrationData : null;
		}

		this.options.onIntegrationChange();
	}

	_handleConfigureClick(e) {
		e.preventDefault();
		const button = $(e.currentTarget);
		const itemElement = button.closest('.integration-item');
		const index = itemElement.data('index');
		const integrationId = itemElement.find('select').val();

		if (!integrationId) return;

		// --- Find the integration data and provider ---
		const currentDataSet = this._getCurrentDataSet();
		this.currentConfig.integration = this.options.allowMultiple ? currentDataSet[index] : currentDataSet;

		const integrationDefinition = this.options.allIntegrations.find(i => i.id === integrationId);
		if (!integrationDefinition) {
			console.error("Integration definition not found for ID:", integrationId);
			return;
		}

		this.currentConfig.provider = this.options.providersData.find(p => p.integrationId === integrationDefinition.type);
		if (!this.currentConfig.provider) {
			AlertManager.createAlert({
				type: "error",
				message: `Provider configuration not found for integration type: ${integrationDefinition.type}`,
				timeout: 4000
			});
			return;
		}

		IntegrationConfigurationManager._activeManager = this;

		// --- Render and show modal ---
		this._renderModalFields();
		const saveButton = IntegrationConfigurationManager._modalElement.find(".modal-footer button[button-type='save-integration-configuration']");
		saveButton.prop('disabled', true);

		IntegrationConfigurationManager._modalInstance.show();
	}

	_handleSaveConfiguration() {
		const changes = this._getModalChanges();
		const proposedIntegrationData = structuredClone(this.currentConfig.integration);
		Object.assign(proposedIntegrationData.fieldValues, changes.changes);

		const validation = this._validateConfiguration(proposedIntegrationData, true);

		if (!validation.isValid) {
			AlertManager.createAlert({
				type: 'danger',
				message: `Validation Failed:<br>${validation.errors.join('<br>')}`,
				timeout: 6000,
			});
			return false; // Return failure
		}

		Object.assign(this.currentConfig.integration.fieldValues, changes.changes);
		this.options.onSaveSuccessful();
        
		return true;
	}


	_handleModalClose() {
		// Reset temporary state for THIS instance
		this.currentConfig = { integration: null, provider: null };
		IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first().empty();

		// Crucially, unset the active manager so the modal is "free" again.
		IntegrationConfigurationManager._activeManager = null;
	}

	_addIntegration() {
		let currentDataSet = this._getCurrentDataSet();
		if (currentDataSet === null || currentDataSet === undefined) {
			currentDataSet = [];
			// Update the master data object if language bound
			if (this.options.isLanguageBound) {
				const langKey = this.options.languageDropdown.getSelectedLanguage().id;
				this.data[langKey] = currentDataSet;
			} else {
				this.data = currentDataSet;
			}
		}
		currentDataSet.push({
			id: null,
			fieldValues: {}
		});
		this._render();
		this.options.onIntegrationChange();
	}

	_removeIntegration(index) {
		let currentDataSet = this._getCurrentDataSet();
		currentDataSet.splice(index, 1);
		this._render();
		this.options.onIntegrationChange();
	}

	// =================================================================
	//  Private Helper & Validation Methods
	// =================================================================

	/**
	 * Central validation logic for a single integration's configuration.
	 * @private
	 * @param {object} integration - The integration object to validate (e.g., { id: '...', fieldValues: {...} }).
	 * @param {boolean} [showUIErrors=false] - If true, adds/removes 'is-invalid' classes in the modal.
	 * @returns {{isValid: boolean, errors: Array<string>}}
	 */
	_validateConfiguration(integration, showUIErrors = false) {
		const fieldsContainer = IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first();

		const errors = [];
		let isValid = true;

		const businessIntegrationData = this.options.allIntegrations.find(i => i.id === integration.id);
		const provider = this.options.providersData.find(p => p.integrationId === businessIntegrationData.type);

		if (!provider) {
			return {
				isValid: false,
				errors: [`Provider configuration not found for integration '${businessIntegrationData.friendlyName}'.`]
			};
		}

		// Clear previous errors if showing UI errors
		if (showUIErrors) {
			fieldsContainer.find('.is-invalid').removeClass('is-invalid');
		}

		provider.userIntegrationFields.forEach(field => {
			const value = integration.fieldValues[field.id];
			const fieldElement = fieldsContainer.find(`[data-field-id="${field.id}"]`);
			const input = fieldElement.find(".config-field-input");

			// Required field validation
			if (field.required && (value === undefined || value === null || String(value).trim() === "")) {
				isValid = false;
				errors.push(`${field.name} is required.`);
				if (showUIErrors) input.addClass('is-invalid');
				return;
			}

			// Type-specific validation (only if a value is present)
			if (value !== undefined && value !== null && String(value).trim() !== "") {
				switch (field.type) {
					case "number":
					case "double_number":
						if (isNaN(value)) {
							isValid = false;
							errors.push(`${field.name} must be a valid number.`);
							if (showUIErrors) input.addClass('is-invalid');
						}
						break;

					case "models":
						const model = provider.models.find(m => m.id === value);
						if (!model) {
							isValid = false;
							errors.push(`${field.name}: Selected model is invalid.`);
							if (showUIErrors) input.addClass('is-invalid');
						} else if (model.disabledAt !== null) {
							isValid = false;
							errors.push(`${field.name}: Selected model is disabled.`);
							if (showUIErrors) input.addClass('is-invalid');
						}
						break;

					case "select":
						if (field.options && !field.options.some(opt => opt.key === value)) {
							isValid = false;
							errors.push(`${field.name}: Invalid option selected.`);
							if (showUIErrors) input.addClass('is-invalid');
						}
						break;
				}
			}
		});

		return {
			isValid,
			errors
		};
	}


	/**
	 * Gets the relevant slice of data based on whether the manager is language-bound.
	 * @private
	 * @returns {Array|object|null} The data set for the current context.
	 */
	_getCurrentDataSet() {
		if (this.options.isLanguageBound) {
			if (!this.options.languageDropdown) {
				console.error("IntegrationConfigurationManager is language-bound but no languageDropdown was provided.");
				return [];
			}
			const langKey = this.options.languageDropdown.getSelectedLanguage().id;
			return this.data[langKey] || [];
		}
		return this.data;
	}

	/**
	 * Checks for changes within the configuration modal and performs type conversion.
	 * @private
	 * @returns {{hasChanges: boolean, changes: object}}
	 */
	_getModalChanges() {
		const fieldsContainer = IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first();

		const changes = {};
		let hasChanges = false;

		fieldsContainer.find(".config-field").each((_, el) => {
			const fieldElement = $(el);
			const fieldId = fieldElement.data("field-id");
			const input = fieldElement.find(".config-field-input");

			// Find the schema to know the expected type
			const fieldSchema = this.currentConfig.provider.userIntegrationFields.find(f => f.id === fieldId);
			if (!fieldSchema) return; // Should not happen

			let processedValue;

			// Perform type conversion based on the schema
			switch (fieldSchema.type) {
				case 'boolean':
					processedValue = input.is(':checked');
					break;

				case 'number':
					// For integers. parseFloat is used to handle empty/invalid strings gracefully.
					const intValue = parseInt(input.val(), 10);
					processedValue = isNaN(intValue) ? input.val() : intValue; // If not a valid int, keep original string for validation
					break;

				case 'double_number':
					// For floating-point numbers
					const floatValue = parseFloat(input.val());
					processedValue = isNaN(floatValue) ? input.val() : floatValue; // If not a valid float, keep original string
					break;

				case 'string':
				case 'text':
				case 'select':
				case 'models':
				default:
					// For all others, the value is a string
					processedValue = input.val();
					break;
			}

			// Retrieve the original value for comparison
			const originalValue = this.currentConfig.integration.fieldValues?.[fieldId];
			const defaultValue = fieldSchema.defaultValue ?? (fieldSchema.type === 'boolean' ? false : "");
			const currentValue = originalValue ?? defaultValue;

			// Assign the correctly-typed value to our changes object
			changes[fieldId] = processedValue;

			// Use a strict comparison, which now works correctly due to proper typing
			if (processedValue !== currentValue) {
				hasChanges = true;
			}
		});

		return { hasChanges, changes };
	}

	/**
	 * Disables the manager, making it read-only and uneditable.
     * @private
	 */
	_disable() {
        this.container.find('button').prop('disabled', true);
        this.container.find('select').prop('disabled', true);
	}
}