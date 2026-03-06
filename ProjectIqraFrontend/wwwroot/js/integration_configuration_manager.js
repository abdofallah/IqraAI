/**
 * IntegrationConfigurationManager Class
 * A reusable component for selecting and configuring integrations like LLM, STT, and TTS.
 *
 * @version 2.0.0
 */
class IntegrationConfigurationManager {
	static _modalInstance = null;
	static _modalElement = null;
	static _activeManager = null;

	/**
	 * Creates an instance of IntegrationConfigurationManager.
	 * @param {string} containerSelector - The CSS selector for the container div where the UI will be rendered.
	 * @param {object} options - Configuration options for this manager instance.
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
		this.updateAllIntegrations(this.options.allIntegrations);

		// --- Modal State Variables ---
		this.currentConfig = {
			integration: null,
			provider: null,
		};

		if (!IntegrationConfigurationManager._modalInstance) {
			IntegrationConfigurationManager._modalElement = $(this.options.modalSelector);
			if (IntegrationConfigurationManager._modalElement.length) {
				IntegrationConfigurationManager._modalInstance = new bootstrap.Modal(IntegrationConfigurationManager._modalElement[0]);
				this._attachSharedModalEventListeners();
			} else {
				console.error(`Integration Modal with selector "${this.options.modalSelector}" not found.`);
			}
		}

		this._init();
	}

	// =================================================================
	//  Public API Methods
	// =================================================================

	load(data, businessIntegrations = undefined) {
		this.data = structuredClone(data);
		if (businessIntegrations) {
			this.updateAllIntegrations(businessIntegrations);
		}
		this._render();
	}

	getData() {
		return this.data;
	}

	reset() {
		this.data = this.options.isLanguageBound ? {} : this.options.allowMultiple ? [] : null;
		this._render();
	}

	disable() {
		this._disable();
	}

	validate() {
		const allErrors = [];
		let isAllValid = true;

		const processIntegration = (integration, context) => {
			if (!integration || !integration.id) return;

			// Pass the data object directly to validation
			const validationResult = this._validateConfigurationData(integration);
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
		return { isValid: isAllValid, errors: allErrors };
	}

	getSelectElements() {
		return this.container.find('select');
	}

	updateAllIntegrations(allIntegrations) {
		this.options.allIntegrations = allIntegrations;

		this.filteredIntegrations = this.options.allIntegrations.filter((integration) => {
			const typeData = SpecificationIntegrationsListData.find((it) => it.id === integration.type);
			const type = this.options.integrationType;
			return typeData.type.includes(type) || (type === "STT" && typeData.type.includes("SPEECH2TEXT")) || (type === "TTS" && typeData.type.includes("TEXT2SPEECH"));
		});
	}

	// =================================================================
	//  Private Initialization & Rendering Methods
	// =================================================================

	_init() {
		this._attachContainerEventListeners();
		this._render();
	}

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
			const addButton = $(`<button class="btn btn-light mt-2"><i class="fa-regular fa-plus me-2"></i><span>Add Integration</span></button>`);
			this.container.append(addButton);
		} else {
			const element = this._createIntegrationElement(currentDataSet, 0);
			this.container.append(element);
		}

		const tooltipTriggerList = this.container.find('[data-bs-toggle="tooltip"]');
		[...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
	}

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

			if (field.type === "model_vector_dimensions") {
				this._populateModelVectorDimensionsField(fieldElement);
			}
		});

		// Initialize Visibility Logic based on current defaults/values
		this._updateFieldVisibility();

		// Init tooltips inside modal
		const tooltips = fieldsContainer.find('[data-bs-toggle="tooltip"]');
		tooltips.each((index, element) => new bootstrap.Tooltip(element));
	}

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
				const options = field.options?.map(opt => `<option value="${opt.key}" ${currentValue.toString() === opt.key.toString() ? "selected" : ""}>${opt.value}</option>`).join("") || "";
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

			case "model_vector_dimensions":
				fieldHtml = `
                    <div class="mb-3 config-field" data-field-id="${field.id}">
                        ${labelHtml}
                        <select class="form-select config-field-input">
                            <!-- Dynamically Generated on Models Change -->
                        </select>
                    </div>`;
				break;

			case "boolean":
				const isChecked = (currentValue === true || currentValue === "true" || currentValue === "on");
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

	_populateModelsField(field, fieldElement) {
		const provider = this.currentConfig.provider;
		if (!provider?.models) return;

		const selectElement = fieldElement.find('select');
		const currentValue = this.currentConfig.integration.fieldValues?.[field.id] ?? field.defaultValue;

		const enabledModels = provider.models.filter(model => model.disabledAt === null);
		enabledModels.forEach(model => {
			selectElement.append(`<option value="${model.id}" ${currentValue === model.id ? "selected" : ""}>${model.name}</option>`);
		});
	}

	_populateModelVectorDimensionsField(fieldElement) {
		const provider = this.currentConfig.provider;
		if (!provider?.models) return;

		const fieldId = fieldElement.data("field-id");
		var currentSelected = this.currentConfig.integration.fieldValues?.[fieldId];

		const selectElement = fieldElement.find('select');
		selectElement.empty();

		// Find the model field value from UI
		const currentValue = IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields'] .config-field[data-field-id='model'] select").val()
			|| IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields'] .config-field[data-field-id='model_id'] select").val();

		if (!currentValue) {
			selectElement.append(`<option disabled selected>Select Model First</option>`);
		} else {
			const currentModel = provider.models.find(model => model.id === currentValue);
			if (!currentModel || !currentModel.availableVectorDimensions) return;

			selectElement.append(`<option ${!currentSelected ? "selected" : ""} disabled>Select Vector Dimension</option>`);
			currentModel.availableVectorDimensions.forEach(vectorDimension => {
				selectElement.append(`<option value="${vectorDimension}" ${currentSelected === vectorDimension.toString() ? "selected" : ""}>${vectorDimension}</option>`);
			});
		}
	}

	// =================================================================
	//  Private Event Handling Methods
	// =================================================================

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

		if (this.options.isLanguageBound && this.options.languageDropdown) {
			this.options.languageDropdown.onLanguageChange(() => this._render());
		}
	}

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

		modalEl.find(".modal-body div[div-type='integration-configuration-fields']").on("input change", ".config-field-input", (event) => {
			if (IntegrationConfigurationManager._activeManager) {
				const activeManager = IntegrationConfigurationManager._activeManager;

				// Handle specific re-population logic (e.g., Vector Dimensions linked to Model)
				const parentField = $(event.currentTarget).parent();
				const fieldId = parentField.attr("data-field-id");

				if (fieldId === "model" || fieldId === "model_id") {
					const vectorField = modalEl.find(".modal-body div[div-type='integration-configuration-fields'] .config-field[data-field-id='model_vector_dimensions']");
					if (vectorField.length) {
						activeManager._populateModelVectorDimensionsField(vectorField);
					}
				}

				// Update visibility of dependent fields
				activeManager._updateFieldVisibility();

				// Evaluate changes
				const changes = activeManager._getModalChanges();
				saveButton.prop("disabled", !changes.hasChanges);

				// Re-validate UI to clear/show errors instantly (optional, enhances UX)
				activeManager._validateModalUI();
			}
		});
	}

	_handleIntegrationSelect(e) {
		const select = $(e.currentTarget);
		const integrationId = select.val();
		const itemElement = select.closest(".integration-item");
		const index = itemElement.data("index");

		itemElement.find('button[button-type="configure"]').prop("disabled", !integrationId);

		let currentDataSet = this._getCurrentDataSet();

		const newIntegrationData = {
			id: integrationId,
			fieldValues: {},
		};

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
			if (!currentDataSet) {
				currentDataSet = [];
				if (this.options.isLanguageBound) {
					const langKey = this.options.languageDropdown.getSelectedLanguage().id;
					this.data[langKey] = currentDataSet;
				}
			}
			currentDataSet[index] = newIntegrationData;
		} else {
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

		const currentDataSet = this._getCurrentDataSet();
		this.currentConfig.integration = this.options.allowMultiple ? currentDataSet[index] : currentDataSet;

		const integrationDefinition = this.options.allIntegrations.find(i => i.id === integrationId);
		if (!integrationDefinition) return;

		this.currentConfig.provider = this.options.providersData.find(p => p.integrationId === integrationDefinition.type);
		if (!this.currentConfig.provider) {
			AlertManager.createAlert({
				type: "danger",
				message: `Provider configuration not found for integration type: ${integrationDefinition.type}`,
				timeout: 4000
			});
			return;
		}

		IntegrationConfigurationManager._activeManager = this;

		this._renderModalFields();

		const saveButton = IntegrationConfigurationManager._modalElement.find(".modal-footer button[button-type='save-integration-configuration']");
		saveButton.prop('disabled', true);

		IntegrationConfigurationManager._modalInstance.show();
	}

	_handleSaveConfiguration() {
		// 1. Validate the UI state first
		const validation = this._validateModalUI();
		if (!validation.isValid) {
			AlertManager.createAlert({
				type: 'danger',
				message: `Validation Failed:<br>${validation.errors.join('<br>')}`,
				timeout: 6000,
			});
			return false;
		}

		// 2. Get the clean, compiled dataset containing ONLY visible fields
		const changes = this._getModalChanges();

		// 3. Completely replace old field values with the clean compiled set
		this.currentConfig.integration.fieldValues = changes.compiledData;

		this.options.onSaveSuccessful();
		return true;
	}

	_handleModalClose() {
		this.currentConfig = { integration: null, provider: null };
		IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first().empty();
		IntegrationConfigurationManager._activeManager = null;
	}

	_addIntegration() {
		let currentDataSet = this._getCurrentDataSet();
		if (currentDataSet === null || currentDataSet === undefined) {
			currentDataSet = [];
			if (this.options.isLanguageBound) {
				const langKey = this.options.languageDropdown.getSelectedLanguage().id;
				this.data[langKey] = currentDataSet;
			} else {
				this.data = currentDataSet;
			}
		}
		currentDataSet.push({ id: null, fieldValues: {} });
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
	//  Private Logic, Validation & Visibility Methods
	// =================================================================

	/**
	 * Scrapes all raw values from the UI currently, regardless of visibility.
	 * Used internally for condition evaluation.
	 */
	_getRawUIValues() {
		const rawValues = {};
		const fieldsContainer = IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first();

		fieldsContainer.find(".config-field").each((_, el) => {
			const fieldId = $(el).data("field-id");
			const input = $(el).find(".config-field-input");

			if (input.is(':checkbox')) {
				rawValues[fieldId] = input.is(':checked') ? "true" : "false";
			} else {
				rawValues[fieldId] = input.val() || "";
			}
		});

		return rawValues;
	}

	/**
	 * Evaluates Model and Field conditions to determine if a field should be shown.
	 */
	_isFieldVisible(fieldSchema, selectedModelId, rawValues) {
		// Helper to handle custom JSON writer Enum format { name: "...", value: X }
		const getEnumValue = (val) => (typeof val === 'object' && val !== null && 'value' in val) ? val.value : val;

		// 1. Model Condition Check
		if (fieldSchema.modelCondition && fieldSchema.modelCondition.models && fieldSchema.modelCondition.models.length > 0) {
			const contains = fieldSchema.modelCondition.models.includes(selectedModelId);
			const conditionType = getEnumValue(fieldSchema.modelCondition.type);

			if (conditionType === 0 && !contains) return false; // Include 
			if (conditionType === 1 && contains) return false;  // Exclude
		}

		// 2. Field Conditions Check
		if (fieldSchema.fieldConditions && fieldSchema.fieldConditions.length > 0) {
			for (const cond of fieldSchema.fieldConditions) {
				const depVal = String(rawValues[cond.fieldId] || "").trim();
				const condVal = String(cond.value || "").trim();

				const condType = getEnumValue(cond.type);
				const condVis = getEnumValue(cond.visibility);

				let isMatch = false;

				// Parse numbers for math operators, fallback to NaN if string (like "on")
				const depNum = parseFloat(depVal);
				const condNum = parseFloat(condVal);

				switch (condType) {
					case 0: isMatch = (depVal.toLowerCase() === condVal.toLowerCase()); break; // Equal
					case 1: isMatch = (depVal.toLowerCase() !== condVal.toLowerCase()); break; // NotEqual
					case 2: isMatch = (depVal.toLowerCase().includes(condVal.toLowerCase())); break; // Include
					case 3: isMatch = (!depVal.toLowerCase().includes(condVal.toLowerCase())); break; // Exclude
					case 4: isMatch = (!isNaN(depNum) && !isNaN(condNum) && depNum > condNum); break; // GreaterThan
					case 5: isMatch = (!isNaN(depNum) && !isNaN(condNum) && depNum < condNum); break; // LessThan
					case 6: isMatch = (!isNaN(depNum) && !isNaN(condNum) && depNum >= condNum); break; // GreaterThanOrEqual
					case 7: isMatch = (!isNaN(depNum) && !isNaN(condNum) && depNum <= condNum); break; // LessThanOrEqual
					case 8: isMatch = (depVal.toLowerCase().startsWith(condVal.toLowerCase())); break; // StartsWith
					case 9: isMatch = (depVal.toLowerCase().endsWith(condVal.toLowerCase())); break; // EndsWith
				}

				if (condVis === 0 && !isMatch) return false; // Needs match to show
				if (condVis === 1 && isMatch) return false;  // Hide if match
			}
		}

		return true;
	}

	/**
	 * Updates the DOM elements' visibility based on current inputs.
	 */
	_updateFieldVisibility() {
		const rawValues = this._getRawUIValues();

		// Determine selected model
		let selectedModelId = "";
		const modelFields = ["model", "model_id", "voice_id", "speech_model"];
		for (const key of modelFields) {
			if (rawValues[key]) {
				selectedModelId = rawValues[key];
				break;
			}
		}

		const fieldsContainer = IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first();

		this.currentConfig.provider.userIntegrationFields.forEach(fieldSchema => {
			const isVisible = this._isFieldVisible(fieldSchema, selectedModelId, rawValues);
			const fieldElement = fieldsContainer.find(`[data-field-id="${fieldSchema.id}"]`);

			if (isVisible) {
				fieldElement.removeClass("d-none");
			} else {
				fieldElement.addClass("d-none");
			}
		});
	}

	/**
	 * Validates only the currently visible fields in the Modal UI.
	 * Adds/Removes is-invalid classes.
	 */
	_validateModalUI() {
		const rawValues = this._getRawUIValues();

		let selectedModelId = "";
		const modelFields = ["model", "model_id", "voice_id", "speech_model"];
		for (const key of modelFields) {
			if (rawValues[key]) {
				selectedModelId = rawValues[key];
				break;
			}
		}

		const fieldsContainer = IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first();
		fieldsContainer.find('.is-invalid').removeClass('is-invalid');

		const errors = [];
		let isValid = true;

		this.currentConfig.provider.userIntegrationFields.forEach(field => {
			// Skip validation for hidden fields
			if (!this._isFieldVisible(field, selectedModelId, rawValues)) {
				return;
			}

			const fieldElement = fieldsContainer.find(`[data-field-id="${field.id}"]`);
			const input = fieldElement.find(".config-field-input");
			const rawVal = rawValues[field.id];

			// 1. Required Check
			if (field.required && (!rawVal || rawVal.trim() === "")) {
				if (field.type !== "boolean") { // boolean is never technically empty in raw extraction (true/false)
					isValid = false;
					errors.push(`${field.name} is required.`);
					input.addClass('is-invalid');
					return;
				}
			}

			// Skip further validation if empty and not required
			if (!rawVal || rawVal.trim() === "") return;

			// 2. Type & Constraints Check
			switch (field.type) {
				case "number":
					const numVal = parseInt(rawVal, 10);
					if (isNaN(numVal)) {
						isValid = false; errors.push(`${field.name} must be a valid whole number.`); input.addClass('is-invalid');
					} else {
						if (field.minNumberValue !== null && numVal < field.minNumberValue) { isValid = false; errors.push(`${field.name} must be >= ${field.minNumberValue}.`); input.addClass('is-invalid'); }
						if (field.maxNumberValue !== null && numVal > field.maxNumberValue) { isValid = false; errors.push(`${field.name} must be <= ${field.maxNumberValue}.`); input.addClass('is-invalid'); }
					}
					break;

				case "double_number":
					const dVal = parseFloat(rawVal);
					if (isNaN(dVal)) {
						isValid = false; errors.push(`${field.name} must be a valid decimal number.`); input.addClass('is-invalid');
					} else {
						if (field.minNumberValue !== null && dVal < field.minNumberValue) { isValid = false; errors.push(`${field.name} must be >= ${field.minNumberValue}.`); input.addClass('is-invalid'); }
						if (field.maxNumberValue !== null && dVal > field.maxNumberValue) { isValid = false; errors.push(`${field.name} must be <= ${field.maxNumberValue}.`); input.addClass('is-invalid'); }
					}
					break;

				case "text":
				case "string":
					if (field.stringRegex) {
						try {
							const regex = new RegExp(field.stringRegex);
							if (!regex.test(rawVal)) {
								isValid = false; errors.push(`Format for ${field.name} is invalid.`); input.addClass('is-invalid');
							}
						} catch (e) { console.error("Invalid regex in DB schema", e); }
					}
					break;

				case "models":
					const model = this.currentConfig.provider.models.find(m => m.id === rawVal);
					if (!model) { isValid = false; errors.push(`${field.name}: Selected model is invalid.`); input.addClass('is-invalid'); }
					else if (model.disabledAt !== null) { isValid = false; errors.push(`${field.name}: Selected model is disabled.`); input.addClass('is-invalid'); }
					break;

				case "model_vector_dimensions":
					const dimInt = parseInt(rawVal, 10);
					const vectorModel = this.currentConfig.provider.models.find(m => m.id === selectedModelId);
					if (!vectorModel || !vectorModel.availableVectorDimensions || !vectorModel.availableVectorDimensions.includes(dimInt)) {
						isValid = false; errors.push(`${field.name}: Invalid vector dimension for selected model.`); input.addClass('is-invalid');
					}
					break;

				case "select":
					if (field.options && !field.options.some(opt => String(opt.key) === rawVal)) {
						isValid = false; errors.push(`${field.name}: Invalid option selected.`); input.addClass('is-invalid');
					}
					break;
			}
		});

		return { isValid, errors };
	}

	/**
	 * Retrieves structured data from the modal.
	 * Ignores hidden fields entirely to ensure a clean JSON payload.
	 */
	_getModalChanges() {
		const rawUI = this._getRawUIValues();

		let selectedModelId = "";
		const modelFields = ["model", "model_id", "voice_id", "speech_model"];
		for (const key of modelFields) {
			if (rawUI[key]) {
				selectedModelId = rawUI[key];
				break;
			}
		}

		const proposedFieldValues = {};

		this.currentConfig.provider.userIntegrationFields.forEach(fieldSchema => {
			if (!this._isFieldVisible(fieldSchema, selectedModelId, rawUI)) {
				return; // Skip hidden field completely
			}

			const rawVal = rawUI[fieldSchema.id];
			let processedValue;

			switch (fieldSchema.type) {
				case 'boolean':
					processedValue = (rawVal === "true");
					break;

				case 'number':
					const intVal = parseInt(rawVal, 10);
					processedValue = isNaN(intVal) ? rawVal : intVal;
					break;

				case 'double_number':
					const floatVal = parseFloat(rawVal);
					processedValue = isNaN(floatVal) ? rawVal : floatVal;
					break;

				default:
					processedValue = rawVal || "";
					break;
			}

			proposedFieldValues[fieldSchema.id] = processedValue;
		});

		// Deep comparison to detect changes
		const hasChanges = !this._deepEqual(this.currentConfig.integration.fieldValues, proposedFieldValues);

		return { hasChanges, compiledData: proposedFieldValues };
	}

	/**
	 * Validates raw data objects entirely (used for public .validate() without UI elements).
	 */
	_validateConfigurationData(integration) {
		const errors = [];
		let isValid = true;

		const businessIntegrationData = this.options.allIntegrations.find(i => i.id === integration.id);
		if (!businessIntegrationData) return { isValid: false, errors: ["Business integration definition not found."] };

		const provider = this.options.providersData.find(p => p.integrationId === businessIntegrationData.type);
		if (!provider) return { isValid: false, errors: [`Provider configuration not found for integration '${businessIntegrationData.friendlyName}'.`] };

		// Pre-pass for selected model
		let selectedModelId = "";
		const modelFields = ["model", "model_id", "voice_id", "speech_model"];
		for (const key of modelFields) {
			if (integration.fieldValues[key]) {
				selectedModelId = String(integration.fieldValues[key]);
				break;
			}
		}

		provider.userIntegrationFields.forEach(field => {
			// If it's conditionally hidden based on current data, skip validation.
			// Note: For backend data validation, we mock the UI raw string format
			const mockRawValues = {};
			for (const [k, v] of Object.entries(integration.fieldValues)) mockRawValues[k] = String(v);

			if (!this._isFieldVisible(field, selectedModelId, mockRawValues)) {
				return;
			}

			const value = integration.fieldValues[field.id];

			if (field.required && (value === undefined || value === null || String(value).trim() === "")) {
				isValid = false; errors.push(`${field.name} is required.`); return;
			}

			if (value !== undefined && value !== null && String(value).trim() !== "") {
				switch (field.type) {
					case "number":
					case "double_number":
						if (isNaN(value)) { isValid = false; errors.push(`${field.name} must be a valid number.`); }
						else {
							const num = parseFloat(value);
							if (field.minNumberValue !== null && num < field.minNumberValue) { isValid = false; errors.push(`${field.name} must be >= ${field.minNumberValue}.`); }
							if (field.maxNumberValue !== null && num > field.maxNumberValue) { isValid = false; errors.push(`${field.name} must be <= ${field.maxNumberValue}.`); }
						}
						break;

					case "text":
					case "string":
						if (field.stringRegex) {
							try {
								if (!new RegExp(field.stringRegex).test(String(value))) {
									isValid = false; errors.push(`Format for ${field.name} is invalid.`);
								}
							} catch (e) { }
						}
						break;

					case "models":
						const model = provider.models.find(m => m.id === String(value));
						if (!model) { isValid = false; errors.push(`${field.name}: Selected model is invalid.`); }
						else if (model.disabledAt !== null) { isValid = false; errors.push(`${field.name}: Selected model is disabled.`); }
						break;

					case "model_vector_dimensions":
						const dimInt = parseInt(value, 10);
						const vectorModel = provider.models.find(m => m.id === selectedModelId);
						if (!vectorModel || !vectorModel.availableVectorDimensions || !vectorModel.availableVectorDimensions.includes(dimInt)) {
							isValid = false; errors.push(`${field.name}: Invalid vector dimension for selected model.`);
						}
						break;
				}
			}
		});

		return { isValid, errors };
	}

	_getCurrentDataSet() {
		if (this.options.isLanguageBound) {
			if (!this.options.languageDropdown) return [];
			const langKey = this.options.languageDropdown.getSelectedLanguage().id;
			return this.data[langKey] || [];
		}
		return this.data;
	}

	_disable() {
		this.container.find('button').prop('disabled', true);
		this.container.find('select').prop('disabled', true);
	}

	_deepEqual(obj1, obj2) {
		if (obj1 === obj2) return true;
		if (typeof obj1 !== 'object' || obj1 === null || typeof obj2 !== 'object' || obj2 === null) return false;

		const keys1 = Object.keys(obj1);
		const keys2 = Object.keys(obj2);

		if (keys1.length !== keys2.length) return false;

		for (let key of keys1) {
			if (!keys2.includes(key)) return false;
			if (!this._deepEqual(obj1[key], obj2[key])) return false;
		}

		return true;
	}
}