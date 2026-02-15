/**
 * ProviderIntegrationsFieldHelper
 * A robust class to manage dynamic Integration Fields for STT, TTS, and LLM providers.
 * Handles rendering, validation, dynamic visibility, and data extraction.
 */

// Enums for Field Conditions
const FIELD_CONDITION_OPS = {
    0: 'Equals (=)',
    1: 'Not Equals (!=)',
    2: 'Contains (Include)',
    3: 'Does Not Contain',
    4: 'Greater Than (>)',
    5: 'Less Than (<)',
    6: 'Greater or Equal (>=)',
    7: 'Less or Equal (<=)',
    8: 'Starts With',
    9: 'Ends With'
};

const FIELD_CONDITION_VISIBILITY = {
    0: 'Show This Field',
    1: 'Hide This Field'
};

class ProviderIntegrationsFieldHelper {
    /**
     * @param {string|jQuery} containerSelector - The DOM element to render fields into
     * @param {string|jQuery} addButtonSelector - The button that adds a new field
     * @param {Array} initialData - The UserIntegrationFields array from the DB
     * @param {Function} onChangeCallback - Function to call when any input changes (to enable Save button)
     */
    constructor(containerSelector, addButtonSelector, initialData, onChangeCallback) {
        this.container = $(containerSelector);
        this.addButton = $(addButtonSelector);
        // Deep copy initial data to ensure clean comparison state
        this.initialData = initialData ? JSON.parse(JSON.stringify(initialData)) : [];
        this.onChangeCallback = onChangeCallback;

        // Bind internal events immediately
        this._bindEvents();
    }

    // ================= PUBLIC METHODS =================

    /** Renders the fields based on the current data state */
    render() {
        this.container.empty();
        if (!this.initialData || this.initialData.length === 0) {
            this._renderEmptyState();
        } else {
            this.initialData.forEach(field => {
                this.container.append(this._createFieldElement(field));
            });
        }
        // Trigger UI logic (visibility of specific sections) for all rendered fields
        this.container.find('.field-type-select').trigger('change');
        this.container.find('.field-is-array-check').trigger('change');
    }

    /** Scrapes the DOM and returns the clean array of ProviderFieldBase objects */
    getData() {
        const data = [];
        const _this = this;

        this.container.find('.integration-field').each(function () {
            const card = $(this);
            const type = card.find('.field-type-select').val();
            const isArray = card.find('.field-is-array-check').is(':checked');

            const field = {
                id: card.find('.field-id-input').val().trim(),
                name: card.find('.field-name-input').val().trim(),
                type: type,
                tooltip: card.find('.field-tooltip-input').val().trim(),
                placeholder: card.find('.field-placeholder-input').val().trim(),
                defaultValue: card.find('.field-default-value-input').val().trim(),
                required: card.find('.field-required-check').is(':checked'),
                isEncrypted: card.find('.field-encrypted-check').is(':checked'),
                isArray: isArray,
                // Initialize nullable fields as null
                options: null,
                minNumberValue: null,
                maxNumberValue: null,
                decimalPlaces: null,
                stringRegex: null,
                minArrayCount: null,
                maxArrayCount: null,
                modelCondition: null,
                fieldConditions: null
            };

            // 1. Handle Options (Select/Models)
            if (type === 'select') {
                field.options = [];
                card.find('.field-option-row').each(function () {
                    const row = $(this);
                    field.options.push({
                        key: row.find('.option-key-input').val().trim(),
                        value: row.find('.option-value-input').val().trim(),
                        isDefault: row.find('.option-default-check').is(':checked')
                    });
                });
            }

            // 2. Handle Number Validation
            if (type === 'number' || type === 'double_number') {
                const min = card.find('.field-min-num-input').val();
                const max = card.find('.field-max-num-input').val();
                if (min !== '') field.minNumberValue = parseFloat(min);
                if (max !== '') field.maxNumberValue = parseFloat(max);

                if (type === 'double_number') {
                    const dec = card.find('.field-decimal-input').val();
                    if (dec !== '') field.decimalPlaces = parseInt(dec);
                }
            }

            // 3. Handle String Validation
            if (type === 'text') {
                const regex = card.find('.field-regex-input').val();
                if (regex) field.stringRegex = regex;
            }

            // 4. Handle Array Validation
            if (isArray) {
                const minArr = card.find('.field-min-array-input').val();
                const maxArr = card.find('.field-max-array-input').val();
                if (minArr !== '') field.minArrayCount = parseInt(minArr);
                if (maxArr !== '') field.maxArrayCount = parseInt(maxArr);
            }

            // 5. Handle Model Condition
            const modelCondType = card.find('.model-condition-type').val();
            const modelCondList = card.find('.model-condition-list').val().trim();

            // Only create object if specific models are entered
            if (modelCondList.length > 0) {
                field.modelCondition = {
                    type: parseInt(modelCondType), // 0 Include, 1 Exclude
                    models: modelCondList.split(',').map(s => s.trim()).filter(s => s !== "")
                };
            }

            // 6. Handle Field Conditions (Advanced Logic)
            const conditions = [];
            card.find('.field-condition-row').each(function () {
                const row = $(this);
                const targetId = row.find('.condition-field-id').val().trim();

                if (targetId) {
                    conditions.push({
                        fieldId: targetId,
                        type: parseInt(row.find('.condition-operator').val()),
                        visibility: parseInt(row.find('.condition-visibility').val()),
                        value: row.find('.condition-value').val().trim()
                    });
                }
            });

            if (conditions.length > 0) {
                field.fieldConditions = conditions;
            }

            data.push(field);
        });

        return data;
    }

    /** 
     * Validates the form data.
     * @param {boolean} onlyRemoveInvalidClasses - If true, only clears errors, doesn't add new ones.
     * @returns {object} { validated: boolean, errors: string[] }
     */
    validate(onlyRemoveInvalidClasses = false) {
        const errors = [];
        let validated = true;
        const idsSeen = new Set();

        this.container.find('.integration-field').each(function (index) {
            const card = $(this);
            const i = index + 1;

            // ID Validation
            const idInput = card.find('.field-id-input');
            const idVal = idInput.val().trim();
            if (!idVal) {
                validated = false;
                errors.push(`Field #${i}: ID is required.`);
                if (!onlyRemoveInvalidClasses) idInput.addClass('is-invalid');
            } else if (idsSeen.has(idVal)) {
                validated = false;
                errors.push(`Field #${i}: Duplicate ID '${idVal}'.`);
                if (!onlyRemoveInvalidClasses) idInput.addClass('is-invalid');
            } else {
                idsSeen.add(idVal);
                idInput.removeClass('is-invalid');
            }

            // Name Validation
            const nameInput = card.find('.field-name-input');
            if (!nameInput.val().trim()) {
                validated = false;
                errors.push(`Field #${i}: Name is required.`);
                if (!onlyRemoveInvalidClasses) nameInput.addClass('is-invalid');
            } else {
                nameInput.removeClass('is-invalid');
            }

            // Select Options Validation
            const type = card.find('.field-type-select').val();
            if (type === 'select') {
                const options = card.find('.field-option-row');
                if (options.length === 0) {
                    validated = false;
                    errors.push(`Field #${i}: Select type must have at least one option.`);
                } else {
                    let hasDefault = false;
                    options.each(function () {
                        const optKey = $(this).find('.option-key-input');
                        const optVal = $(this).find('.option-value-input');

                        // Check empty
                        if (!optKey.val().trim()) {
                            if (!onlyRemoveInvalidClasses) optKey.addClass('is-invalid');
                            validated = false;
                        } else optKey.removeClass('is-invalid');

                        if (!optVal.val().trim()) {
                            if (!onlyRemoveInvalidClasses) optVal.addClass('is-invalid');
                            validated = false;
                        } else optVal.removeClass('is-invalid');

                        if ($(this).find('.option-default-check').is(':checked')) hasDefault = true;
                    });

                    if (!hasDefault && options.length > 0) {
                        validated = false;
                        errors.push(`Field #${i}: Select options must have a default selected.`);
                    }
                }
            }
        });

        return { validated, errors };
    }

    /** Checks if the current DOM state differs from the initial data */
    hasChanges() {
        const currentData = this.getData();
        return !this._deepEqual(this.initialData, currentData);
    }

    /** Updates the baseline for change detection (call after a successful Save) */
    updateInitialData(newData) {
        this.initialData = JSON.parse(JSON.stringify(newData));
        this.render(); // Re-render to ensure UI consistency
    }

    // ================= PRIVATE METHODS =================

    _bindEvents() {
        const _this = this;

        // Add New Field
        this.addButton.off('click').on('click', (e) => {
            e.preventDefault();
            _this.container.find('.empty-state').remove();
            _this.container.append(_this._createFieldElement());
            _this._triggerChange();
        });

        // --- Event Delegation for Dynamic Elements ---

        // Remove Field
        this.container.on('click', '.remove-field-button', function () {
            $(this).closest('.integration-field').remove();
            if (_this.container.children().length === 0) _this._renderEmptyState();
            _this._triggerChange();
        });

        // Type Change (Toggle Sections)
        this.container.on('change', '.field-type-select', function () {
            _this._handleTypeChange($(this));
            _this._triggerChange();
        });

        // Array Check (Toggle Array Section)
        this.container.on('change', '.field-is-array-check', function () {
            _this._handleArrayCheck($(this));
            _this._triggerChange();
        });

        // Add/Remove Options (Select)
        this.container.on('click', '.add-option-button', function () {
            const list = $(this).siblings('.field-options-list');
            list.append(_this._createOptionElement());
            _this._triggerChange();
        });

        this.container.on('click', '.remove-option-button', function () {
            $(this).closest('.field-option-row').remove();
            _this._triggerChange();
        });

        // Default Option Mutual Exclusivity
        this.container.on('change', '.option-default-check', function () {
            if ($(this).is(':checked')) {
                $(this).closest('.field-options-list').find('.option-default-check').not(this).prop('checked', false);
            }
            _this._triggerChange();
        });

        // Add/Remove Field Conditions
        this.container.on('click', '.add-condition-button', function () {
            const list = $(this).siblings('.field-conditions-list');
            list.append(_this._createConditionRow());
            _this._triggerChange();
        });

        this.container.on('click', '.remove-condition-button', function () {
            $(this).closest('.field-condition-row').remove();
            _this._triggerChange();
        });

        // Generic Input Change (Bubbling)
        this.container.on('input change', 'input:not(.option-default-check), select:not(.field-type-select)', function () {
            _this._triggerChange();
        });
    }

    _triggerChange() {
        if (typeof this.onChangeCallback === 'function') {
            this.onChangeCallback();
        }
    }

    _renderEmptyState() {
        this.container.html(`
            <div class="text-center p-5 empty-state">
                <p class="text-muted mb-0">No integration fields defined.</p>
            </div>
        `);
    }

    _handleTypeChange(selectElement) {
        const type = selectElement.val();
        const cardBody = selectElement.closest('.card-body');

        const optionsSection = cardBody.find('.options-section');
        const numberSection = cardBody.find('.number-validation-section');
        const textSection = cardBody.find('.text-validation-section');
        const defaultValueInput = cardBody.find('.field-default-value-input');

        // Reset
        optionsSection.addClass('d-none');
        numberSection.addClass('d-none');
        textSection.addClass('d-none');
        cardBody.find('.decimal-places-group').addClass('d-none');
        defaultValueInput.prop('disabled', false);

        switch (type) {
            case 'select':
                optionsSection.removeClass('d-none');
                defaultValueInput.prop('disabled', true).val(''); // Default is determined by options
                break;
            case 'models':
                // Models options come from API/Parent, user doesn't define them here
                defaultValueInput.prop('disabled', true).val('');
                break;
            case 'number':
                numberSection.removeClass('d-none');
                break;
            case 'double_number':
                numberSection.removeClass('d-none');
                cardBody.find('.decimal-places-group').removeClass('d-none');
                break;
            case 'text':
                textSection.removeClass('d-none');
                break;
        }
    }

    _handleArrayCheck(checkbox) {
        const isArray = checkbox.is(':checked');
        const cardBody = checkbox.closest('.card-body');
        const arraySection = cardBody.find('.array-validation-section');

        if (isArray) arraySection.removeClass('d-none');
        else arraySection.addClass('d-none');
    }

    // --- HTML Generation ---

    _createFieldElement(data = null) {
        const id = data ? data.id : '';
        const type = data ? data.type : 'text';
        const isArray = data ? data.isArray : false;

        const modelCondType = data?.modelCondition?.type ?? 0;
        const modelCondList = data?.modelCondition?.models?.join(', ') ?? '';

        return `
        <div class="card mb-3 integration-field shadow-sm">
            <div class="card-body">
                <!-- Header -->
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <h6 class="card-title mb-0 fw-bold text-dark"><i class="fa-solid fa-cube me-2 text-primary"></i>Field Configuration</h6>
                    <button type="button" class="btn btn-danger btn-sm remove-field-button" title="Remove Field"><i class="fa-regular fa-trash"></i></button>
                </div>

                <!-- Row 1: Basic Info -->
                <div class="row g-3 mb-3">
                    <div class="col-md-4">
                        <label class="form-label small text-muted fw-semibold">Field ID (Internal)</label>
                        <input type="text" class="form-control form-control-sm field-id-input" placeholder="e.g. silence_timeout" value="${id}">
                    </div>
                    <div class="col-md-4">
                        <label class="form-label small text-muted fw-semibold">Display Name</label>
                        <input type="text" class="form-control form-control-sm field-name-input" placeholder="e.g. Silence Timeout" value="${data?.name || ''}">
                    </div>
                    <div class="col-md-4">
                        <label class="form-label small text-muted fw-semibold">Data Type</label>
                        <select class="form-select form-select-sm field-type-select">
                            <option value="text" ${type === 'text' ? 'selected' : ''}>Text</option>
                            <option value="number" ${type === 'number' ? 'selected' : ''}>Number (Integer)</option>
                            <option value="double_number" ${type === 'double_number' ? 'selected' : ''}>Decimal (Double)</option>
                            <option value="select" ${type === 'select' ? 'selected' : ''}>Dropdown Select</option>
                            <option value="models" ${type === 'models' ? 'selected' : ''}>Models List (System)</option>
                        </select>
                    </div>
                </div>

                <!-- Row 2: UX & Default -->
                <div class="row g-3 mb-3">
                    <div class="col-md-4">
                        <label class="form-label small text-muted fw-semibold">Tooltip</label>
                        <input type="text" class="form-control form-control-sm field-tooltip-input" value="${data?.tooltip || ''}">
                    </div>
                    <div class="col-md-4">
                        <label class="form-label small text-muted fw-semibold">Placeholder</label>
                        <input type="text" class="form-control form-control-sm field-placeholder-input" value="${data?.placeholder || ''}">
                    </div>
                    <div class="col-md-4">
                        <label class="form-label small text-muted fw-semibold">Default Value</label>
                        <input type="text" class="form-control form-control-sm field-default-value-input" value="${data?.defaultValue || ''}" ${type === 'select' || type === 'models' ? 'disabled' : ''}>
                    </div>
                </div>

                <!-- Row 3: Flags -->
                <div class="row g-3 mb-3">
                    <div class="col-md-3">
                        <div class="form-check form-switch">
                            <input class="form-check-input field-required-check" type="checkbox" ${data?.required ? 'checked' : ''}>
                            <label class="form-check-label small fw-semibold">Required</label>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="form-check form-switch">
                            <input class="form-check-input field-encrypted-check" type="checkbox" ${data?.isEncrypted ? 'checked' : ''}>
                            <label class="form-check-label small fw-semibold">Is Encrypted</label>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="form-check form-switch">
                            <input class="form-check-input field-is-array-check" type="checkbox" ${isArray ? 'checked' : ''}>
                            <label class="form-check-label small fw-semibold">Is Array (List)</label>
                        </div>
                    </div>
                </div>

                <!-- Dynamic: Number Validation -->
                <div class="number-validation-section mb-3 ${type === 'number' || type === 'double_number' ? '' : 'd-none'}">
                    <h6 class="small text-primary border-bottom pb-1 mb-2 fw-bold">Number Constraints</h6>
                    <div class="row g-2">
                        <div class="col-md-4">
                            <label class="form-label small text-muted">Min Value</label>
                            <input type="number" class="form-control form-control-sm field-min-num-input" placeholder="Min" value="${data?.minNumberValue ?? ''}">
                        </div>
                        <div class="col-md-4">
                            <label class="form-label small text-muted">Max Value</label>
                            <input type="number" class="form-control form-control-sm field-max-num-input" placeholder="Max" value="${data?.maxNumberValue ?? ''}">
                        </div>
                        <div class="col-md-4 decimal-places-group ${type === 'double_number' ? '' : 'd-none'}">
                            <label class="form-label small text-muted">Decimal Places</label>
                            <input type="number" class="form-control form-control-sm field-decimal-input" placeholder="Precision" value="${data?.decimalPlaces ?? ''}">
                        </div>
                    </div>
                </div>

                <!-- Dynamic: Text Validation -->
                <div class="text-validation-section mb-3 ${type === 'text' ? '' : 'd-none'}">
                    <h6 class="small text-primary border-bottom pb-1 mb-2 fw-bold">Text Constraints</h6>
                    <label class="form-label small text-muted">Regex Pattern</label>
                    <input type="text" class="form-control form-control-sm field-regex-input" placeholder="e.g. ^[a-z]+$" value="${data?.stringRegex || ''}">
                </div>

                <!-- Dynamic: Array Validation -->
                <div class="array-validation-section mb-3 ${isArray ? '' : 'd-none'}">
                    <h6 class="small text-primary border-bottom pb-1 mb-2 fw-bold">Array Constraints</h6>
                    <div class="row g-2">
                        <div class="col-md-6">
                            <label class="form-label small text-muted">Min Items</label>
                            <input type="number" class="form-control form-control-sm field-min-array-input" placeholder="0" value="${data?.minArrayCount ?? ''}">
                        </div>
                        <div class="col-md-6">
                            <label class="form-label small text-muted">Max Items</label>
                            <input type="number" class="form-control form-control-sm field-max-array-input" placeholder="10" value="${data?.maxArrayCount ?? ''}">
                        </div>
                    </div>
                </div>

                <!-- Dynamic: Select Options -->
                <div class="options-section mb-3 ${type === 'select' ? '' : 'd-none'}">
                    <h6 class="small text-primary border-bottom pb-1 mb-2 fw-bold">Dropdown Options</h6>
                    <div class="field-options-list">
                        ${data?.options ? data.options.map(opt => this._createOptionElement(opt)).join('') : ''}
                    </div>
                    <button type="button" class="btn btn-outline-secondary btn-sm mt-2 add-option-button"><i class="fa-solid fa-plus me-1"></i> Add Option</button>
                </div>

                <!-- Logic / Conditions -->
                <div class="conditions-section mt-3 pt-2 border-top">
                    
                    <!-- Model Conditions -->
                    <h6 class="small text-primary mb-2 fw-bold">Model Visibility</h6>
                    <div class="row g-2 align-items-center mb-3">
                        <div class="col-auto">
                            <select class="form-select form-select-sm model-condition-type">
                                <option value="0" ${modelCondType === 0 ? 'selected' : ''}>Include Only For Models:</option>
                                <option value="1" ${modelCondType === 1 ? 'selected' : ''}>Exclude From Models:</option>
                            </select>
                        </div>
                        <div class="col">
                            <input type="text" class="form-control form-control-sm model-condition-list" 
                                placeholder="Comma separated Model IDs (e.g. flux-general-en, nova-2)" 
                                value="${modelCondList}">
                        </div>
                    </div>

                    <!-- Field Dependencies -->
                    <h6 class="small text-primary mb-2 fw-bold">Field Dependencies (Advanced)</h6>
                    <div class="alert alert-light p-2 mb-2 small text-muted border">
                        <i class="fa-solid fa-info-circle me-1"></i> 
                        Show or hide this field based on values of other fields.
                    </div>
                    
                    <div class="field-conditions-list">
                        ${data?.fieldConditions ? data.fieldConditions.map(cond => this._createConditionRow(cond)).join('') : ''}
                    </div>
                    
                    <button type="button" class="btn btn-outline-secondary btn-sm mt-2 add-condition-button">
                        <i class="fa-solid fa-code-branch me-1"></i> Add Rule
                    </button>
                </div>

            </div>
        </div>
        `;
    }

    _createOptionElement(opt = null) {
        return `
        <div class="row g-1 mb-1 align-items-center field-option-row">
            <div class="col-5">
                <input type="text" class="form-control form-control-sm option-key-input" placeholder="Key (saved)" value="${opt?.key || ''}">
            </div>
            <div class="col-5">
                <input type="text" class="form-control form-control-sm option-value-input" placeholder="Value (displayed)" value="${opt?.value || ''}">
            </div>
            <div class="col-auto text-center" style="width: 40px;">
                <input class="form-check-input option-default-check" type="checkbox" title="Set as Default" ${opt?.isDefault ? 'checked' : ''}>
            </div>
            <div class="col-auto">
                <button type="button" class="btn btn-outline-danger btn-sm remove-option-button" style="padding: 0.2rem 0.5rem;"><i class="fa-solid fa-times"></i></button>
            </div>
        </div>
        `;
    }

    _createConditionRow(cond = null) {
        // Generate Options for Operators
        let opsHtml = '';
        for (const [key, label] of Object.entries(FIELD_CONDITION_OPS)) {
            const isSel = (cond?.type?.toString() === key) ? 'selected' : '';
            opsHtml += `<option value="${key}" ${isSel}>${label}</option>`;
        }

        // Generate Options for Visibility
        let visHtml = '';
        for (const [key, label] of Object.entries(FIELD_CONDITION_VISIBILITY)) {
            const isSel = (cond?.visibility?.toString() === key) ? 'selected' : '';
            visHtml += `<option value="${key}" ${isSel}>${label}</option>`;
        }

        return `
        <div class="row g-1 mb-2 align-items-center field-condition-row border rounded p-2 bg-dark-2 shadow-sm">
            <div class="col-md-2">
                <select class="form-select form-select-sm condition-visibility" title="Action">
                    ${visHtml}
                </select>
            </div>
            <div class="col-auto">
                <span class="small fw-bold text-muted">IF</span>
            </div>
            <div class="col-md-3">
                <input type="text" class="form-control form-control-sm condition-field-id" 
                       placeholder="Target Field ID" value="${cond?.fieldId || ''}" title="The ID of the other field to check">
            </div>
            <div class="col-md-3">
                <select class="form-select form-select-sm condition-operator" title="Operator">
                    ${opsHtml}
                </select>
            </div>
            <div class="col-md-3">
                <input type="text" class="form-control form-control-sm condition-value" 
                       placeholder="Target Value" value="${cond?.value || ''}" title="The value to compare against">
            </div>
            <div class="col-auto">
                <button type="button" class="btn btn-outline-danger btn-sm remove-condition-button">
                    <i class="fa-solid fa-times"></i>
                </button>
            </div>
        </div>
        `;
    }

    /** 
     * Recursive helper to check deep equality of objects.
     * Used to detect unsaved changes.
     */
    _deepEqual(obj1, obj2) {
        if (obj1 === obj2) return true;

        if (typeof obj1 !== 'object' || obj1 === null || typeof obj2 !== 'object' || obj2 === null) {
            return false;
        }

        const keys1 = Object.keys(obj1);
        const keys2 = Object.keys(obj2);

        if (keys1.length !== keys2.length) return false;

        for (let key of keys1) {
            if (!keys2.includes(key)) return false;
            // Recursive check
            if (!this._deepEqual(obj1[key], obj2[key])) return false;
        }

        return true;
    }
}