/// <reference path="animate-images.umd.min.js" />
/**
 * IntegrationConfigurationManager Class
 * A reusable component for selecting and configuring integrations like LLM, STT, and TTS.
 *
 * @version 2.1.0 (Added dynamic Array support)
 */
class IntegrationConfigurationManager {
	static _modalInstance = null;
	static _modalElement = null;
	static _activeManager = null;

	constructor(containerSelector, options) {
		this.container = $(containerSelector);
		if (this.container.length === 0) {
			console.error(`IntegrationConfigurationManager: Container with selector "${containerSelector}" not found.`);
			return;
		}

		this.options = {
			onSaveSuccessful: () => { },
			onIntegrationChange: () => { },
			...options,
		};

		this.data = this.options.isLanguageBound ? {} : this.options.allowMultiple ? [] : null;
		this.updateAllIntegrations(this.options.allIntegrations);

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
		if (businessIntegrations) this.updateAllIntegrations(businessIntegrations);
		this._render();
	}

	getData() { return this.data; }

	reset() {
		this.data = this.options.isLanguageBound ? {} : this.options.allowMultiple ? [] : null;
		this._render();
	}

	disable() { this._disable(); }

	validate() {
		const allErrors = [];
		let isAllValid = true;

		const processIntegration = (integration, context) => {
			if (!integration || !integration.id) return;
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

	getSelectElements() { return this.container.find('select'); }

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
		return $(`
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
        `);
	}

	_renderModalFields() {
		const fieldsContainer = IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first();
		fieldsContainer.empty();
		if (!this.currentConfig.provider?.userIntegrationFields) return;

		this.currentConfig.provider.userIntegrationFields.forEach(field => {
			const fieldElement = this._createModalFieldElement(field);
			fieldsContainer.append(fieldElement);

			if (field.type === "models") this._populateModelsField(field, fieldElement);
			if (field.type === "model_vector_dimensions") this._populateModelVectorDimensionsField(fieldElement);
		});

		this._updateFieldVisibility();

		const tooltips = fieldsContainer.find('[data-bs-toggle="tooltip"]');
		tooltips.each((index, element) => new bootstrap.Tooltip(element));
	}

	_createModalFieldElement(field) {
		let currentValue = this.currentConfig.integration.fieldValues?.[field.id] ?? field.defaultValue ?? "";

		const labelHtml = `
            <label class="form-label btn-ic-span-align">
                <span>${field.name} ${field.required ? '<span class="text-danger">*</span>' : ""}</span>
                ${field.tooltip ? `
                    <a href="#" class="d-inline-block" data-bs-html="true" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${field.tooltip}">
                        <i class="fa-regular fa-circle-question"></i>
                    </a>` : ""}
            </label>`;

		if (field.isArray) {
			let valuesArray = [];
			if (Array.isArray(currentValue)) valuesArray = currentValue;
			else if (typeof currentValue === 'string' && currentValue.trim() !== '') valuesArray = currentValue.split(',').map(s => s.trim());

			// Pre-fill min required elements if empty
			const minCount = field.minArrayCount || 0;
			if (valuesArray.length < minCount) {
				for (let i = valuesArray.length; i < minCount; i++) valuesArray.push("");
			}

			const itemsHtml = valuesArray.map(val => this._createSingleInputHtml(field, val, true)).join("");
			return $(`
                <div class="mb-3 config-field array-field" data-field-id="${field.id}" data-is-array="true">
                    ${labelHtml}
                    <div class="array-items-container">
                        ${itemsHtml}
                    </div>
                    <button type="button" class="btn btn-sm btn-outline-secondary mt-2 add-array-item-btn">
                        <i class="fa-solid fa-plus me-1"></i> Add Item
                    </button>
                </div>
            `);
		} else {
			return $(`
                <div class="mb-3 config-field" data-field-id="${field.id}" data-is-array="false">
                    ${labelHtml}
                    ${this._createSingleInputHtml(field, currentValue, false)}
                </div>
            `);
		}
	}

	_createSingleInputHtml(field, val, isArrayItem = false) {
		let inputHtml = "";
		switch (field.type) {
			case "text":
			case "string":
			case "number":
			case "double_number":
				inputHtml = `<input type="${field.isEncrypted ? 'password' : (field.type.includes('number') ? 'number' : 'text')}" class="form-control config-field-input" placeholder="${field.placeholder || ""}" value="${val}">`;
				break;
			case "select":
				const options = field.options?.map(opt => `<option value="${opt.key}" ${String(val) === String(opt.key) ? "selected" : ""}>${opt.value}</option>`).join("") || "";
				inputHtml = `<select class="form-select config-field-input"><option value="" disabled ${val === "" ? "selected" : ""}>Select ${field.name}</option>${options}</select>`;
				break;
			case "models":
			case "model_vector_dimensions":
				// Populated later dynamically, using data attribute to remember what should be selected
				inputHtml = `<select class="form-select config-field-input" data-selected-val="${val}"><option value="" disabled ${!val ? "selected" : ""}>Select ${field.name}</option></select>`;
				break;
			case "boolean":
				const isChecked = (val === true || val === "true" || val === "on");
				const uid = Math.random().toString(36).substring(7);
				inputHtml = `
                    <div class="form-check form-switch mt-1">
                        <input type="checkbox" class="form-check-input config-field-input" id="check-${uid}" ${isChecked ? "checked" : ""}>
                        <label class="form-check-label" for="check-${uid}">Enable</label>
                    </div>`;
				break;
		}

		if (isArrayItem) {
			return `
                <div class="input-group mb-2 array-item">
                    ${inputHtml}
                    <button type="button" class="btn btn-outline-danger remove-array-item-btn"><i class="fa-solid fa-trash"></i></button>
                </div>`;
		}
		return inputHtml;
	}

	_populateModelsField(field, fieldElement) {
		const provider = this.currentConfig.provider;
		if (!provider?.models) return;
		const enabledModels = provider.models.filter(model => model.disabledAt === null);

		fieldElement.find('select').each((_, sel) => {
			const selectElement = $(sel);
			const val = selectElement.attr('data-selected-val') || "";
			enabledModels.forEach(model => {
				selectElement.append(`<option value="${model.id}" ${val === model.id ? "selected" : ""}>${model.name}</option>`);
			});
		});
	}

	_populateModelVectorDimensionsField(fieldElement) {
		const provider = this.currentConfig.provider;
		if (!provider?.models) return;

		const modalEl = IntegrationConfigurationManager._modalElement;
		const modelValue = modalEl.find(".modal-body div[div-type='integration-configuration-fields'] .config-field[data-field-id='model'] select").val()
			|| modalEl.find(".modal-body div[div-type='integration-configuration-fields'] .config-field[data-field-id='model_id'] select").val();

		fieldElement.find('select').each((_, sel) => {
			const selectElement = $(sel);
			const currentSelected = selectElement.attr('data-selected-val') || selectElement.val() || "";
			selectElement.empty();

			if (!modelValue) {
				selectElement.append(`<option disabled selected>Select Model First</option>`);
			} else {
				const currentModel = provider.models.find(model => model.id === modelValue);
				if (!currentModel || !currentModel.availableVectorDimensions) return;

				selectElement.append(`<option ${!currentSelected ? "selected" : ""} disabled>Select Vector Dimension</option>`);
				currentModel.availableVectorDimensions.forEach(dim => {
					selectElement.append(`<option value="${dim}" ${currentSelected === dim.toString() ? "selected" : ""}>${dim}</option>`);
				});
			}
		});
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
				if (isSuccess) IntegrationConfigurationManager._modalInstance.hide();
			}
		});

		modalEl.on("hidden.bs.modal", () => {
			if (IntegrationConfigurationManager._activeManager) {
				IntegrationConfigurationManager._activeManager._handleModalClose();
			}
		});

		// Dynamic Array Items (Add)
		modalEl.on("click", ".add-array-item-btn", (e) => {
			if (!IntegrationConfigurationManager._activeManager) return;
			const mgr = IntegrationConfigurationManager._activeManager;

			const btn = $(e.currentTarget);
			const container = btn.siblings(".array-items-container");
			const fieldId = btn.closest(".config-field").data("field-id");
			const fieldSchema = mgr.currentConfig.provider.userIntegrationFields.find(f => f.id === fieldId);

			const newItemHtml = $(mgr._createSingleInputHtml(fieldSchema, "", true));
			container.append(newItemHtml);

			if (fieldSchema.type === "models") mgr._populateModelsField(fieldSchema, newItemHtml);
			if (fieldSchema.type === "model_vector_dimensions") mgr._populateModelVectorDimensionsField(newItemHtml);

			mgr._updateFieldVisibility();
			saveButton.prop("disabled", !mgr._getModalChanges().hasChanges);
		});

		// Dynamic Array Items (Remove)
		modalEl.on("click", ".remove-array-item-btn", (e) => {
			if (!IntegrationConfigurationManager._activeManager) return;
			$(e.currentTarget).closest(".array-item").remove();

			const mgr = IntegrationConfigurationManager._activeManager;
			mgr._updateFieldVisibility();
			saveButton.prop("disabled", !mgr._getModalChanges().hasChanges);
		});

		// Input Changes
		modalEl.find(".modal-body div[div-type='integration-configuration-fields']").on("input change", ".config-field-input", (event) => {
			if (IntegrationConfigurationManager._activeManager) {
				const activeManager = IntegrationConfigurationManager._activeManager;
				const parentField = $(event.currentTarget).closest('.config-field');
				const fieldId = parentField.attr("data-field-id");

				if (fieldId === "model" || fieldId === "model_id") {
					const vectorField = modalEl.find(".modal-body div[div-type='integration-configuration-fields'] .config-field[data-field-id='model_vector_dimensions']");
					if (vectorField.length) activeManager._populateModelVectorDimensionsField(vectorField);
				}

				activeManager._updateFieldVisibility();
				const changes = activeManager._getModalChanges();
				saveButton.prop("disabled", !changes.hasChanges);
				activeManager._validateModalUI(); // Live validation cleanup
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

		const newIntegrationData = { id: integrationId, fieldValues: {} };

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
		const itemElement = $(e.currentTarget).closest('.integration-item');
		const index = itemElement.data('index');
		const integrationId = itemElement.find('select').val();

		if (!integrationId) return;

		const currentDataSet = this._getCurrentDataSet();
		this.currentConfig.integration = this.options.allowMultiple ? currentDataSet[index] : currentDataSet;

		const integrationDefinition = this.options.allIntegrations.find(i => i.id === integrationId);
		if (!integrationDefinition) return;

		this.currentConfig.provider = this.options.providersData.find(p => p.integrationId === integrationDefinition.type);
		if (!this.currentConfig.provider) {
			AlertManager.createAlert({ type: "danger", message: `Provider configuration not found for integration type: ${integrationDefinition.type}`, timeout: 4000 });
			return;
		}

		IntegrationConfigurationManager._activeManager = this;
		this._renderModalFields();

		const saveButton = IntegrationConfigurationManager._modalElement.find(".modal-footer button[button-type='save-integration-configuration']");
		saveButton.prop('disabled', true);

		IntegrationConfigurationManager._modalInstance.show();
	}

	_handleSaveConfiguration() {
		const validation = this._validateModalUI();
		if (!validation.isValid) {
			AlertManager.createAlert({ type: 'danger', message: `Validation Failed:<br>${validation.errors.join('<br>')}`, timeout: 6000 });
			return false;
		}

		const changes = this._getModalChanges();
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

	_getRawUIValues() {
		const rawValues = {};
		const fieldsContainer = IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first();

		fieldsContainer.find(".config-field").each((_, el) => {
			const fieldElement = $(el);
			const fieldId = fieldElement.data("field-id");
			const isArray = fieldElement.data("is-array") === true;

			if (isArray) {
				const vals = [];
				fieldElement.find(".config-field-input").each((_, inputEl) => {
					const input = $(inputEl);
					if (input.is(':checkbox')) vals.push(input.is(':checked') ? "true" : "false");
					else vals.push(input.val() || "");
				});
				rawValues[fieldId] = vals;
			} else {
				const input = fieldElement.find(".config-field-input").first();
				if (input.is(':checkbox')) rawValues[fieldId] = input.is(':checked') ? "true" : "false";
				else rawValues[fieldId] = input.val() || "";
			}
		});

		return rawValues;
	}

	_isFieldVisible(fieldSchema, selectedModelId, rawValues) {
		const getEnumValue = (val) => (typeof val === 'object' && val !== null && 'value' in val) ? val.value : val;

		if (fieldSchema.modelCondition && fieldSchema.modelCondition.models && fieldSchema.modelCondition.models.length > 0) {
			const contains = fieldSchema.modelCondition.models.includes(selectedModelId);
			const conditionType = getEnumValue(fieldSchema.modelCondition.type);

			if (conditionType === 0 && !contains) return false;
			if (conditionType === 1 && contains) return false;
		}

		if (fieldSchema.fieldConditions && fieldSchema.fieldConditions.length > 0) {
			for (const cond of fieldSchema.fieldConditions) {
				// Handle array dependencies gracefully (checks the first item or joins them, though usually conditions are based on scalar fields)
				let depValRaw = rawValues[cond.fieldId];
				if (Array.isArray(depValRaw)) depValRaw = depValRaw[0];
				const depVal = String(depValRaw || "").trim();

				const condVal = String(cond.value || "").trim();
				const condType = getEnumValue(cond.type);
				const condVis = getEnumValue(cond.visibility);

				let isMatch = false;
				const depNum = parseFloat(depVal);
				const condNum = parseFloat(condVal);

				switch (condType) {
					case 0: isMatch = (depVal.toLowerCase() === condVal.toLowerCase()); break;
					case 1: isMatch = (depVal.toLowerCase() !== condVal.toLowerCase()); break;
					case 2: isMatch = (depVal.toLowerCase().includes(condVal.toLowerCase())); break;
					case 3: isMatch = (!depVal.toLowerCase().includes(condVal.toLowerCase())); break;
					case 4: isMatch = (!isNaN(depNum) && !isNaN(condNum) && depNum > condNum); break;
					case 5: isMatch = (!isNaN(depNum) && !isNaN(condNum) && depNum < condNum); break;
					case 6: isMatch = (!isNaN(depNum) && !isNaN(condNum) && depNum >= condNum); break;
					case 7: isMatch = (!isNaN(depNum) && !isNaN(condNum) && depNum <= condNum); break;
					case 8: isMatch = (depVal.toLowerCase().startsWith(condVal.toLowerCase())); break;
					case 9: isMatch = (depVal.toLowerCase().endsWith(condVal.toLowerCase())); break;
				}

				if (condVis === 0 && !isMatch) return false;
				if (condVis === 1 && isMatch) return false;
			}
		}

		return true;
	}

	_updateFieldVisibility() {
		const rawValues = this._getRawUIValues();
		let selectedModelId = "";
		const modelFields = ["model", "model_id", "voice_id", "speech_model"];
		for (const key of modelFields) {
			if (rawValues[key] && !Array.isArray(rawValues[key])) {
				selectedModelId = rawValues[key];
				break;
			}
		}

		const fieldsContainer = IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first();

		this.currentConfig.provider.userIntegrationFields.forEach(fieldSchema => {
			const isVisible = this._isFieldVisible(fieldSchema, selectedModelId, rawValues);
			const fieldElement = fieldsContainer.find(`[data-field-id="${fieldSchema.id}"]`);

			if (isVisible) fieldElement.removeClass("d-none");
			else fieldElement.addClass("d-none");
		});
	}

	_validateModalUI() {
		const rawValues = this._getRawUIValues();
		let selectedModelId = "";
		const modelFields = ["model", "model_id", "voice_id", "speech_model"];
		for (const key of modelFields) {
			if (rawValues[key] && !Array.isArray(rawValues[key])) {
				selectedModelId = rawValues[key];
				break;
			}
		}

		const fieldsContainer = IntegrationConfigurationManager._modalElement.find(".modal-body div[div-type='integration-configuration-fields']").first();
		fieldsContainer.find('.is-invalid').removeClass('is-invalid');
		fieldsContainer.find('.border-danger').removeClass('border-danger'); // for array containers

		const errors = [];
		let isValid = true;

		this.currentConfig.provider.userIntegrationFields.forEach(field => {
			if (!this._isFieldVisible(field, selectedModelId, rawValues)) return;

			const fieldElement = fieldsContainer.find(`[data-field-id="${field.id}"]`);
			const rawVal = rawValues[field.id];

			const valsToValidate = field.isArray ? (Array.isArray(rawVal) ? rawVal : []) : [rawVal];

			// Array Bounds Check
			if (field.isArray) {
				if (field.minArrayCount !== null && valsToValidate.length < field.minArrayCount) {
					isValid = false; errors.push(`${field.name} requires at least ${field.minArrayCount} item(s).`);
					fieldElement.find('.array-items-container').addClass('border border-danger rounded p-1');
				}
				if (field.maxArrayCount !== null && valsToValidate.length > field.maxArrayCount) {
					isValid = false; errors.push(`${field.name} exceeds maximum of ${field.maxArrayCount} item(s).`);
					fieldElement.find('.array-items-container').addClass('border border-danger rounded p-1');
				}
			}

			// Validate Each Value
			valsToValidate.forEach((v, idx) => {
				const inputEl = field.isArray ? fieldElement.find('.config-field-input').eq(idx) : fieldElement.find('.config-field-input');

				// Required and Empty checks
				const isEmpty = (!v || String(v).trim() === "");
				if (isEmpty) {
					if (field.required && field.type !== "boolean") {
						isValid = false;
						errors.push(field.isArray ? `${field.name} item #${idx + 1} is required.` : `${field.name} is required.`);
						inputEl.addClass('is-invalid');
						return;
					}

					if (field.isArray && field.type !== "boolean") {
						isValid = false;
						errors.push(`${field.name} cannot contain empty items.`);
						inputEl.addClass('is-invalid');
						return;
					}
				}

				if (isEmpty) return; // Optional and empty -> valid

				switch (field.type) {
					case "number":
						const numVal = parseInt(v, 10);
						if (isNaN(numVal)) { isValid = false; errors.push(`${field.name} must be a valid whole number.`); inputEl.addClass('is-invalid'); }
						else {
							if (field.minNumberValue !== null && numVal < field.minNumberValue) { isValid = false; errors.push(`${field.name} must be >= ${field.minNumberValue}.`); inputEl.addClass('is-invalid'); }
							if (field.maxNumberValue !== null && numVal > field.maxNumberValue) { isValid = false; errors.push(`${field.name} must be <= ${field.maxNumberValue}.`); inputEl.addClass('is-invalid'); }
						}
						break;

					case "double_number":
						const dVal = parseFloat(v);
						if (isNaN(dVal)) { isValid = false; errors.push(`${field.name} must be a valid decimal number.`); inputEl.addClass('is-invalid'); }
						else {
							if (field.minNumberValue !== null && dVal < field.minNumberValue) { isValid = false; errors.push(`${field.name} must be >= ${field.minNumberValue}.`); inputEl.addClass('is-invalid'); }
							if (field.maxNumberValue !== null && dVal > field.maxNumberValue) { isValid = false; errors.push(`${field.name} must be <= ${field.maxNumberValue}.`); inputEl.addClass('is-invalid'); }
						}
						break;

					case "text":
					case "string":
						if (field.stringRegex) {
							try {
								const regex = new RegExp(field.stringRegex);
								if (!regex.test(v)) { isValid = false; errors.push(`Format for ${field.name} is invalid.`); inputEl.addClass('is-invalid'); }
							} catch (e) { console.error("Invalid regex in DB schema", e); }
						}
						break;

					case "models":
						const model = this.currentConfig.provider.models.find(m => m.id === v);
						if (!model) { isValid = false; errors.push(`${field.name}: Selected model is invalid.`); inputEl.addClass('is-invalid'); }
						else if (model.disabledAt !== null) { isValid = false; errors.push(`${field.name}: Selected model is disabled.`); inputEl.addClass('is-invalid'); }
						break;

					case "model_vector_dimensions":
						const dimInt = parseInt(v, 10);
						const vectorModel = this.currentConfig.provider.models.find(m => m.id === selectedModelId);
						if (!vectorModel || !vectorModel.availableVectorDimensions || !vectorModel.availableVectorDimensions.includes(dimInt)) {
							isValid = false; errors.push(`${field.name}: Invalid vector dimension for selected model.`); inputEl.addClass('is-invalid');
						}
						break;

					case "select":
						if (field.options && !field.options.some(opt => String(opt.key) === String(v))) {
							isValid = false; errors.push(`${field.name}: Invalid option selected.`); inputEl.addClass('is-invalid');
						}
						break;
				}
			});
		});

		return { isValid, errors };
	}

	_getModalChanges() {
		const rawUI = this._getRawUIValues();
		let selectedModelId = "";
		const modelFields = ["model", "model_id", "voice_id", "speech_model"];
		for (const key of modelFields) {
			if (rawUI[key] && !Array.isArray(rawUI[key])) {
				selectedModelId = rawUI[key];
				break;
			}
		}

		const proposedFieldValues = {};

		const processSingleValue = (val, type) => {
			if (type === 'boolean') {
				if (val === true || val === "true" || val === "on" || val === "yes") return true;
				return false;
			}
			if (type === 'number') {
				const intVal = parseInt(val, 10);
				return isNaN(intVal) ? val : intVal;
			}
			if (type === 'double_number') {
				const floatVal = parseFloat(val);
				return isNaN(floatVal) ? val : floatVal;
			}
			return val || "";
		};

		this.currentConfig.provider.userIntegrationFields.forEach(fieldSchema => {
			if (!this._isFieldVisible(fieldSchema, selectedModelId, rawUI)) return;

			const rawVal = rawUI[fieldSchema.id];
			if (rawVal === undefined || rawVal === null) return;

			if (fieldSchema.isArray) {
				// Must map the array, ensure we drop empty elements if they snuck in
				const validArray = Array.isArray(rawVal) ? rawVal : [];
				proposedFieldValues[fieldSchema.id] = validArray.map(v => processSingleValue(v, fieldSchema.type));
			} else {
				proposedFieldValues[fieldSchema.id] = processSingleValue(rawVal, fieldSchema.type);
			}
		});

		const hasChanges = !this._deepEqual(this.currentConfig.integration.fieldValues, proposedFieldValues);
		return { hasChanges, compiledData: proposedFieldValues };
	}

	_validateConfigurationData(integration) {
		const errors = [];
		let isValid = true;

		const businessIntegrationData = this.options.allIntegrations.find(i => i.id === integration.id);
		if (!businessIntegrationData) return { isValid: false, errors: ["Business integration definition not found."] };

		const provider = this.options.providersData.find(p => p.integrationId === businessIntegrationData.type);
		if (!provider) return { isValid: false, errors: [`Provider configuration not found for integration '${businessIntegrationData.friendlyName}'.`] };

		// Format raw values for condition check mapping
		const mockRawValues = {};
		let selectedModelId = "";
		const modelFields = ["model", "model_id", "voice_id", "speech_model"];

		for (const [k, v] of Object.entries(integration.fieldValues)) {
			if (Array.isArray(v)) mockRawValues[k] = v.map(String);
			else mockRawValues[k] = String(v);

			if (modelFields.includes(k)) selectedModelId = String(v);
		}

		provider.userIntegrationFields.forEach(field => {
			if (!this._isFieldVisible(field, selectedModelId, mockRawValues)) return;

			const value = integration.fieldValues[field.id];
			const isValueEmpty = (value === undefined || value === null || value === "" || (Array.isArray(value) && value.length === 0));

			if (field.required && isValueEmpty && field.type !== "boolean") {
				isValid = false; errors.push(`${field.name} is required.`); return;
			}

			if (isValueEmpty) return;

			const valsToValidate = field.isArray ? (Array.isArray(value) ? value : []) : [value];

			if (field.isArray) {
				if (field.minArrayCount !== null && valsToValidate.length < field.minArrayCount) { isValid = false; errors.push(`${field.name} requires at least ${field.minArrayCount} item(s).`); }
				if (field.maxArrayCount !== null && valsToValidate.length > field.maxArrayCount) { isValid = false; errors.push(`${field.name} exceeds max of ${field.maxArrayCount} item(s).`); }
			}

			valsToValidate.forEach(v => {
				if (field.isArray && (!v || String(v).trim() === "") && field.type !== "boolean") {
					isValid = false; errors.push(`${field.name} cannot contain empty items.`); return;
				}

				switch (field.type) {
					case "number":
					case "double_number":
						if (isNaN(v)) { isValid = false; errors.push(`${field.name} must be a valid number.`); }
						else {
							const num = parseFloat(v);
							if (field.minNumberValue !== null && num < field.minNumberValue) { isValid = false; errors.push(`${field.name} must be >= ${field.minNumberValue}.`); }
							if (field.maxNumberValue !== null && num > field.maxNumberValue) { isValid = false; errors.push(`${field.name} must be <= ${field.maxNumberValue}.`); }
						}
						break;

					case "text":
					case "string":
						if (field.stringRegex) {
							try {
								if (!new RegExp(field.stringRegex).test(String(v))) { isValid = false; errors.push(`Format for ${field.name} is invalid.`); }
							} catch (e) { }
						}
						break;

					case "models":
						const model = provider.models.find(m => m.id === String(v));
						if (!model) { isValid = false; errors.push(`${field.name}: Selected model is invalid.`); }
						else if (model.disabledAt !== null) { isValid = false; errors.push(`${field.name}: Selected model is disabled.`); }
						break;

					case "model_vector_dimensions":
						const dimInt = parseInt(v, 10);
						const vectorModel = provider.models.find(m => m.id === selectedModelId);
						if (!vectorModel || !vectorModel.availableVectorDimensions || !vectorModel.availableVectorDimensions.includes(dimInt)) {
							isValid = false; errors.push(`${field.name}: Invalid vector dimension for selected model.`);
						}
						break;
				}
			});
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