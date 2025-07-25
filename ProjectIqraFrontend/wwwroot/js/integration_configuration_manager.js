/**
 * IntegrationConfigurationManager Class
 * A reusable component for selecting and configuring integrations like LLM, STT, and TTS.
 *
 * @version 1.0.0
 */
class IntegrationConfigurationManager {
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
	 * @param {function} [options.onChange] - Optional callback function to execute when data changes.
	 * @param {function} [options.onValidate] - Optional callback function to execute for validation checks.
	 */
	constructor(containerSelector, options) {
		this.container = $(containerSelector);
		if (this.container.length === 0) {
			console.error(`IntegrationConfigurationManager: Container with selector "${containerSelector}" not found.`);
			return;
		}

		// --- Default options and merging with provided options ---
		this.options = {
			onChange: () => { },
			onValidate: () => { },
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
		this.modalElement = $(this.options.modalSelector);
		this.modal = new bootstrap.Modal(this.modalElement[0]);
		this.modalFieldsContainer = this.modalElement.find(".modal-body > div").first(); // Assumes a standard structure
		this.modalSaveButton = this.modalElement.find(".modal-footer .btn-primary");
		this.currentConfig = {
			integration: null, // The integration object from this.data being configured
			provider: null, // The provider data for the integration
		};

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

	// =================================================================
	//  Private Initialization & Rendering Methods
	// =================================================================

	/**
	 * Initializes event listeners.
	 * @private
	 */
	_init() {
		this._attachEventListeners();
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
		this.modalFieldsContainer.empty();
		if (!this.currentConfig.provider?.userIntegrationFields) return;

		this.currentConfig.provider.userIntegrationFields.forEach(field => {
			const fieldElement = this._createModalFieldElement(field);
			this.modalFieldsContainer.append(fieldElement);

			if (field.type === "models") {
				this._populateModelsField(field, fieldElement);
			}
		});

		// Init tooltips inside modal
		const tooltips = this.modalFieldsContainer.find('[data-bs-toggle="tooltip"]');
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
	 * Attaches all necessary event listeners using delegation.
	 * @private
	 */
	_attachEventListeners() {
		// --- Main Container Listeners ---
		this.container.on("change", "select", this._handleIntegrationSelect.bind(this));
		this.container.on("click", 'button[button-type="configure"]', this._handleConfigureClick.bind(this));
		this.container.on("click", ".btn-light", (e) => { // 'Add' button
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

		// --- Modal Listeners ---
		this.modalSaveButton.on("click", this._handleSaveConfiguration.bind(this));
		this.modalElement.on("hide.bs.modal", this._handleModalClose.bind(this));
		this.modalFieldsContainer.on("input change", ".config-field-input", () => {
			const changes = this._getModalChanges();
			this.modalSaveButton.prop("disabled", !changes.hasChanges);
		});

		// --- External Language Dropdown Listener ---
		if (this.options.isLanguageBound && this.options.languageDropdown) {
			this.options.languageDropdown.onLanguageChange(() => this._render());
		}
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
				if (field.defaultValue !== undefined) {
					newIntegrationData.fieldValues[field.id] = field.defaultValue;
				}
			});
		}

		if (this.options.allowMultiple) {
			currentDataSet[index] = newIntegrationData;
		} else {
			// For single integration, the data itself is the object
			this.data = newIntegrationData;
		}

		this.options.onChange();
		this.options.onValidate();
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
			console.error("Provider data not found for integration type:", integrationDefinition.type);
			return;
		}

		// --- Render and show modal ---
		this._renderModalFields();
		this.modalSaveButton.prop('disabled', true); // Disable save initially
		this.modal.show();
	}

	_handleSaveConfiguration() {
		// Here you can add validation logic for the modal fields if needed
		const changes = this._getModalChanges();

		// Update the fieldValues of the integration being configured
		Object.assign(this.currentConfig.integration.fieldValues, changes.changes);

		this.modal.hide();
		this.options.onChange();
		this.options.onValidate();
	}

	_handleModalClose() {
		// Reset temporary state
		this.currentConfig = { integration: null, provider: null };
		this.modalFieldsContainer.empty();
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
		currentDataSet.push({ id: null, fieldValues: {} });
		this._render();
		this.options.onChange();
	}

	_removeIntegration(index) {
		let currentDataSet = this._getCurrentDataSet();
		currentDataSet.splice(index, 1);
		this._render();
		this.options.onChange();
		this.options.onValidate();
	}

	// =================================================================
	//  Private Helper Methods
	// =================================================================

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
	 * Checks for changes within the configuration modal.
	 * @private
	 * @returns {{hasChanges: boolean, changes: object}}
	 */
	_getModalChanges() {
		const changes = {};
		let hasChanges = false;

		this.modalFieldsContainer.find(".config-field").each((_, el) => {
			const fieldElement = $(el);
			const fieldId = fieldElement.data("field-id");
			const value = fieldElement.find(".config-field-input").val();
			const currentValue = this.currentConfig.integration.fieldValues?.[fieldId] ?? "";

			changes[fieldId] = value;
			if (String(value) !== String(currentValue)) {
				hasChanges = true;
			}
		});

		return { hasChanges, changes };
	}
}