// CONSTANTS & ENUMS
const PCA_EXTRACTION_FIELD_DATA_TYPE = {
    String: 0,
    Boolean: 1,
    Number: 2,
    DateTime: 3,
    Enum: 4
};
const PCA_CONDITION_OPERATOR = {
    Equals: 0,
    NotEquals: 1,
    Contains: 2,
    GreaterThan: 3,
    LessThan: 4
};

const MAX_PCA_TAG_LEVELS = 5;
const MAX_PCA_TAGS_PER_LEVEL = 5;
const MAX_PCA_EXTRACTION_LEVELS = 5;
const MAX_PCA_FIELDS_PER_LEVEL = 5;
const MAX_PCA_RULES_PER_FIELD = 5;

// DYNAMIC VARIABLES
let managePCAType = null; // 'new' or 'edit'
let currentPCATemplateData = null; // Stores the original data of the template being edited
let isSavingPCATemplate = false;

// ELEMENT VARIABLES
const postAnalysisTab = $("#post-analysis-tab");
const pcaTooltipTriggerList = document.querySelectorAll('#post-analysis-tab [data-bs-toggle="tooltip"]');
[...pcaTooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

// Header Elements
const pcaManagerHeader = postAnalysisTab.find("#post-analysis-manager-header");
const backToPCAListButton = pcaManagerHeader.find("#back-to-post-analysis-list");
const pcaManagerNameBreadcrumb = pcaManagerHeader.find("#pca-manager-name-breadcrumb");
const savePCATemplateButton = pcaManagerHeader.find("#save-pca-template-button");
const savePCATemplateButtonSpinner = savePCATemplateButton.find(".save-button-spinner");

// List View Elements
const pcaListView = postAnalysisTab.find("#post-analysis-list-view");
const addNewPCATemplateButton = pcaListView.find("#add-new-pca-template-button");
const pcaListContainer = pcaListView.find("#post-analysis-list-container");

// Manager View Elements
const pcaManagerView = postAnalysisTab.find("#post-analysis-manager-view");
const pcaManagerGeneralTabButton = pcaManagerView.find("#pca-manager-general-tab");

// Manager View - General Tab
const pcaIconInput = pcaManagerView.find("#pca-icon-input");
const pcaNameInput = pcaManagerView.find("#pca-name-input");
const pcaDescriptionInput = pcaManagerView.find("#pca-description-input");

// Manager View - Summary Tab
const pcaSummaryActiveToggle = pcaManagerView.find("#pca-summary-active-toggle");
const pcaSummaryConfigContainer = pcaManagerView.find("#pca-summary-config-container");
const pcaSummaryPromptInput = pcaManagerView.find("#pca-summary-prompt-input");
const pcaSummaryMaxLengthInput = pcaManagerView.find("#pca-summary-maxlength-input");
const pcaSummaryFormatSelect = pcaManagerView.find("#pca-summary-format-select");

// Manager View - Conversation Tags Tab
const pcaAddTagSetButton = pcaManagerView.find("#pca-add-tag-set-button");
const pcaTagSetsList = pcaManagerView.find("#pca-tag-sets-list");

// Manager View - Data Extraction Tab
const pcaAddExtractionFieldButton = pcaManagerView.find("#pca-add-extraction-field-button");
const pcaExtractionFieldsList = pcaManagerView.find("#pca-extraction-fields-list");

// API FUNCTIONS
function savePCATemplate(formData, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/postanalysis/save`,
        type: "POST",
        data: formData,
        processData: false,
        contentType: false,
        success: (response) => {
            if (response.success) {
                successCallback(response);
            } else {
                errorCallback(response, true);
            }
        },
        error: (xhr, status, error) => {
            errorCallback(error, false);
        },
    });
}
function deletePCATemplate(templateId, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/postanalysis/delete`,
        type: "POST", data: JSON.stringify({ templateId: templateId }), contentType: "application/json",
        success: (response) => { response.success ? successCallback(response) : errorCallback(response, true); },
        error: (xhr, status, error) => { errorCallback(error, false); },
    });
}

// CORE UI & DATA FUNCTIONS
function showPCAListView() {
    pcaManagerHeader.removeClass("show");
    pcaManagerView.removeClass("show");
    setTimeout(() => {
        pcaManagerHeader.addClass("d-none");
        pcaManagerView.addClass("d-none");
        pcaListView.removeClass("d-none");
        setTimeout(() => {
            pcaListView.addClass("show");
            setDynamicBodyHeight();
        }, 10);
    }, 300);
}
function showPCAManagerView() {
    pcaListView.removeClass("show");
    setTimeout(() => {
        pcaListView.addClass("d-none");
        pcaManagerHeader.removeClass("d-none");
        pcaManagerView.removeClass("d-none");
        setTimeout(() => {
            pcaManagerHeader.addClass("show");
            pcaManagerView.addClass("show");
            setDynamicBodyHeight();
        }, 10);
    }, 300);
}
function fillPCAList() {
    pcaListContainer.empty();
    const templates = BusinessFullData.businessApp.postAnalysisTemplates;
    if (!templates || templates.length === 0) {
        pcaListContainer.append('<div class="col-12"><h6 class="text-center mt-5">No analysis templates created yet...</h6></div>');
    } else {
        templates.forEach(template => {
            pcaListContainer.append($(createPCAListElement(template)));
        });
    }
}
function createDefaultPCATemplateObject() {
    return {
        id: null,
        general: {
            emoji: "📊",
            name: "",
            description: ""
        },
        summary: {
            isActive: true,
            prompt: ""
        },
        tagging: {
            tags: []
        },
        extraction: {
            fields: []
        }
    };
}
function resetPCAManager() {
    pcaManagerView.find(".is-invalid").removeClass("is-invalid");

    // General
    pcaIconInput.text("📊");
    pcaNameInput.val("");
    pcaDescriptionInput.val("");

    // Summary
    pcaSummaryActiveToggle.prop("checked", true).change();
    pcaSummaryPromptInput.val("");

    // Tags & Extraction
    pcaTagSetsList.empty();
    pcaExtractionFieldsList.empty();

    // State
    pcaManagerGeneralTabButton.click();
    savePCATemplateButton.prop("disabled", true);
}
function fillPCAManager(templateData) {
    // General
    pcaIconInput.text(templateData.general.emoji);
    pcaNameInput.val(templateData.general.name);
    pcaDescriptionInput.val(templateData.general.description);

    // Summary
    pcaSummaryActiveToggle.prop("checked", templateData.summary.isActive).change();
    pcaSummaryPromptInput.val(templateData.summary.prompt);

    // Tags
    renderPCATags(pcaTagSetsList, templateData.tagging.tags, 0);

    // Extraction
    renderPCAExtractionFields(pcaExtractionFieldsList, templateData.extraction.fields, 0);
}
function checkPCAChanges(enableDisableButton = true) {
    if (managePCAType === null) return { hasChanges: false, changes: {} };

    let hasChanges = false;
    const original = currentPCATemplateData;
    const findById = (arr, id) => arr.find(item => item.id === id);

    // Build current state from DOM
    const currentState = {
        general: {
            emoji: pcaIconInput.text(),
            name: pcaNameInput.val().trim(),
            description: pcaDescriptionInput.val().trim()
        },
        summary: {
            isActive: pcaSummaryActiveToggle.is(':checked'),
            prompt: pcaSummaryPromptInput.val().trim()
        },
        tagging: {
            tags: getPCATagsFromDOM(pcaTagSetsList)
        },
        extraction: {
            fields: getPCAFieldsFromDOM(pcaExtractionFieldsList)
        }
    };

    // Compare General
    if (currentState.general.name !== original.general.name ||
        currentState.general.description !== original.general.description ||
        currentState.general.emoji !== original.general.emoji)
    {
        hasChanges = true;
    }

    // Compare Summary
    if (currentState.summary.isActive !== original.summary.isActive ||
        currentState.summary.prompt !== original.summary.prompt
    ) {
        hasChanges = true;
    }

    // Compare Tagging recursively
    if (!hasChanges) {
        hasChanges = comparePCATags(currentState.tagging.tags, original.tagging.tags);
    }

    // Compare Extraction recursively
    if (!hasChanges) {
        hasChanges = comparePCAFields(currentState.extraction.fields, original.extraction.fields);
    }

    if (enableDisableButton) {
        savePCATemplateButton.prop("disabled", !hasChanges);
    }
    return { hasChanges, changes: currentState };
}
function validatePCATemplate(onlyRemove = true) {
    const errors = [];
    let validated = true;
    if (!onlyRemove) pcaManagerView.find('.is-invalid').removeClass('is-invalid');

    // General
    if (!pcaNameInput.val().trim()) {
        validated = false; errors.push("Template Name is required.");
        if (!onlyRemove) pcaNameInput.addClass('is-invalid');
    }

    // Tagging
    const tagSetNames = new Set();
    validatePCATagsRecursive(pcaTagSetsList, errors, tagSetNames, onlyRemove);

    // Extraction
    const keyNames = new Set();
    validatePCAFieldsRecursive(pcaExtractionFieldsList, errors, keyNames, onlyRemove);

    return { validated, errors };
}
async function canLeavePCAManager(leaveMessage = "") {
    if (isSavingPCATemplate) {
        AlertManager.createAlert({ type: "warning", message: "Template is currently being saved. Please wait." });
        return false;
    }
    const { hasChanges } = checkPCAChanges(false);
    if (hasChanges) {
        const confirmDialog = new BootstrapConfirmDialog({
            title: "Unsaved Changes",
            message: `You have unsaved changes.${leaveMessage}`,
            confirmText: "Discard",
            cancelText: "Cancel",
            confirmButtonClass: "btn-danger"
        });
        return await confirmDialog.show();
    }
    return true;
}
function handlePCARouting(subPath) {
    if (managePCAType === 'new' || managePCAType === 'edit') {
        const correctPath = managePCAType === 'new' ? 'postanalysis/new' : `postanalysis/${currentPCATemplateData.id}`;
        replaceUrlForTab(correctPath);
        return;
    }

    if (!subPath || subPath.length === 0) {
        if (pcaManagerView.hasClass('show')) showPCAListView();
        replaceUrlForTab('postanalysis');
        return;
    }

    const action = subPath[0];
    if (action === 'new') {
        if (!pcaManagerView.hasClass('show')) addNewPCATemplateButton.click();
    } else {
        const card = pcaListContainer.find(`.pca-template-card[data-template-id="${action}"]`);
        if (card.length > 0) {
            if (!pcaManagerView.hasClass('show')) card.find('.btn-edit').click();
        } else {
            showPCAListView();
            replaceUrlForTab('postanalysis');
        }
    }
}

// RECURSIVE HELPER FUNCTIONS (VALIDATION, UI, DATA GATHERING)
function renderPCATags(container, tags, level) {
    container.empty();
    tags.forEach(tagData => {
        const tagElement = $(createPCATagElement(tagData, level));
        container.append(tagElement);
        const subTagContainer = tagElement.find('.sub-tags-container').first();
        if (tagData.subTags && tagData.subTags.length > 0) {
            renderPCATags(subTagContainer, tagData.subTags, level + 1);
        }
    });
}
function renderPCAExtractionFields(container, fields, level) {
    container.empty();
    fields.forEach(fieldData => {
        const fieldElement = $(createPCAExtractionFieldElement(fieldData, level));
        container.append(fieldElement);
        const rulesContainer = fieldElement.find('.rules-container').first();
        if (fieldData.conditionalRules && fieldData.conditionalRules.length > 0) {
            fieldData.conditionalRules.forEach(ruleData => {
                const ruleElement = $(createPCAConditionalRuleElement(ruleData, fieldData));
                rulesContainer.append(ruleElement);
                const dependentFieldsContainer = ruleElement.find('.dependent-fields-container').first();
                renderPCAExtractionFields(dependentFieldsContainer, ruleData.fieldsToExtract, level + 1);
            });
        }
    });
}
function getPCATagsFromDOM(container) {
    const tags = [];
    container.children('.tag-box').each((_, el) => {
        const $el = $(el);
        const subTagsContainer = $el.find('.sub-tags-container').first();
        tags.push({
            id: $el.data('id'),
            name: $el.find('[data-type="name"]').val().trim(),
            description: $el.find('[data-type="description"]').val().trim(),
            rules: {
                allowMultiple: $el.find('[data-type="allowMultiple"]').is(':checked'),
                isRequired: $el.find('[data-type="isRequired"]').is(':checked')
            },
            subTags: getPCATagsFromDOM(subTagsContainer)
        });
    });
    return tags;
}
function getPCAFieldsFromDOM(container) {
    const fields = [];
    container.children('.extraction-field-box').each((_, el) => {
        const $el = $(el);
        const rulesContainer = $el.find('.rules-container').first();
        const dataType = parseInt($el.find('[data-type="DataType"]').val());
        const field = {
            id: $el.data('id'),
            keyName: $el.find('[data-type="keyName"]').val().trim(),
            description: $el.find('[data-type="description"]').val().trim(),
            isRequired: $el.find('[data-type="isRequired"]').is(':checked'),
            dataType: dataType,
            options: [],
            validation: { pattern: null },
            conditionalRules: getPCARulesFromDOM($el)
        };
        if (dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum) field.options = $el.find('.field-options-container input').val().split(',').map(s => s.trim()).filter(Boolean);
        if (dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.String) field.validation.pattern = $el.find('.field-options-container input').val().trim() || null;
        fields.push(field);
    });
    return fields;
}
function getPCARulesFromDOM(fieldBox) {
    const rules = [];
    fieldBox.find('.rule-box').each((_, el) => {
        const $el = $(el);
        const dependentFieldsContainer = $el.find('.dependent-fields-container').first();
        rules.push({
            id: $el.data('id'),
            condition: {
                operator: parseInt($el.find('[data-type="conditionOperator"]').val()),
                value: $el.find('[data-type="conditionValue"]').val()
            },
            fieldsToExtract: getPCAFieldsFromDOM(dependentFieldsContainer)
        });
    });
    return rules;
}
function validatePCATagsRecursive(container, errors, path = "Tags") {
    container.children('.tag-box').each((i, el) => {
        const $el = $(el);
        const nameInput = $el.find('[data-type="name"]');
        const currentPath = `${path} -> #${i + 1}`;
        if (!nameInput.val().trim()) errors.push(`${currentPath}: Tag Name is required.`);
        const subTagContainer = $el.find('.sub-tags-container').first();
        validatePCATagsRecursive(subTagContainer, errors, currentPath);
    });
}
function validatePCAFieldsRecursive(container, errors, uniqueKeys, path = "Fields") {
    container.children('.extraction-field-box').each((i, el) => {
        const $el = $(el);
        const keyNameInput = $el.find('[data-type="keyName"]');
        const keyName = keyNameInput.val().trim();
        const currentPath = `${path} -> #${i + 1}`;
        if (!keyName) errors.push(`${currentPath}: Key Name is required.`);
        else if (uniqueKeys.has(keyName)) errors.push(`${currentPath}: Key Name "${keyName}" must be unique across the entire template.`);
        else uniqueKeys.add(keyName);
        $el.find('.rule-box').each((j, ruleEl) => {
            validatePCAFieldsRecursive($(ruleEl).find('.dependent-fields-container').first(), errors, uniqueKeys, `${currentPath} -> Rule #${j + 1}`);
        });
    });
}
function comparePCATags(currentTags, originalTags) {
    if (currentTags.length !== originalTags.length) return true;

    for (const currentTag of currentTags) {
        const originalTag = originalTags.find(ot => ot.id === currentTag.id);
        if (!originalTag) return true; // A tag was replaced

        if (currentTag.name !== originalTag.name ||
            currentTag.description !== originalTag.description ||
            currentTag.rules.allowMultiple !== originalTag.rules.allowMultiple ||
            currentTag.rules.isRequired !== originalTag.rules.isRequired) {
            return true;
        }

        // Recursive call for sub-tags
        if (comparePCATags(currentTag.subTags, originalTag.subTags)) {
            return true;
        }
    }
    return false; // No changes found
}
function compareRules(currentRules, originalRules) {
    if (currentRules.length !== originalRules.length) return true;

    for (const currentRule of currentRules) {
        const originalRule = originalRules.find(or => or.id === currentRule.id);
        if (!originalRule) return true; // A rule was replaced

        if (currentRule.condition.operator !== originalRule.condition.operator ||
            currentRule.condition.value !== originalRule.condition.value) {
            return true;
        }

        // Recursive call for nested fields within the rule
        if (comparePCAFields(currentRule.fieldsToExtract, originalRule.fieldsToExtract)) {
            return true;
        }
    }
    return false;
}
function comparePCAFields(currentFields, originalFields) {
    if (currentFields.length !== originalFields.length) return true;

    for (const currentField of currentFields) {
        const originalField = originalFields.find(of => of.id === currentField.id);
        if (!originalField) return true; // A field was replaced

        // Compare simple properties
        if (currentField.keyName !== originalField.keyName ||
            currentField.description !== originalField.description ||
            currentField.isRequired !== originalField.isRequired ||
            currentField.dataType !== originalField.dataType) {
            return true;
        }

        // Compare complex properties
        if (JSON.stringify(currentField.options) !== JSON.stringify(originalField.options) ||
            currentField.validation.pattern !== originalField.validation.pattern) {
            return true;
        }

        // Recursive call for conditional rules
        if (compareRules(currentField.conditionalRules, originalField.conditionalRules)) {
            return true;
        }
    }
    return false;
}

// DYNAMIC ELEMENT CREATORS
function createPCAListElement(templateData) {
    return `
        <div class="col-lg-4 col-md-6 col-12">
            <div class="campaign-card pca-template-card d-flex flex-column" data-template-id="${templateData.id}">
                 <div class="d-flex flex-row align-items-center justify-content-between w-100">
                    <div class="d-flex flex-row align-items-center">
                        <span class="route-icon">${templateData.general.emoji}</span>
                        <div class="card-data">
                            <h4>${templateData.general.name}</h4>
                        </div>
                    </div>
                    <div class="card-actions">
                        <button class="btn btn-info btn-sm btn-edit"><i class="fa-regular fa-pen-to-square"></i></button>
                        <button class="btn btn-danger btn-sm btn-delete"><i class="fa-regular fa-trash"></i></button>
                    </div>
                </div>
                <div class="mt-3"><h5 class="h5-info agent-description"><span>${templateData.general.description}</span></h5></div>
            </div>
        </div>`;
}
function createPCATagElement(tagData, level) {
    const data = tagData || {};
    const id = data.id || crypto.randomUUID();
    const isRequired = data.rules ? data.rules.isRequired : false;
    const allowMultiple = data.rules ? data.rules.allowMultiple : false;
    return `
        <div class="p-3 border rounded mb-2 tag-box" data-id="${id}" data-level="${level}" style="background-color: ${(level == 0 ? '#1a1a1a' : `rgba(255,255,255,${level * 0.03});`)}">
            <div class="d-flex align-items-center mb-2">
                <input type="text" class="form-control me-2" placeholder="Tag Name" data-type="name" value="${data.name || ''}">
                <button class="btn btn-danger btn-sm" button-type="remove-item"><i class="fa-regular fa-trash"></i></button>
            </div>
            <textarea class="form-control form-control-sm mb-2" rows="2" placeholder="Description for AI..." data-type="description">${data.description || ''}</textarea>
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <small class="text-muted me-3">Rules for Sub-Tags:</small>
                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="checkbox" id="tag-required-${id}" data-type="isRequired" ${isRequired ? 'checked' : ''}>
                        <label class="form-check-label" for="tag-required-${id}">Required</label>
                    </div>
                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="checkbox" id="tag-multiple-${id}" data-type="allowMultiple" ${allowMultiple ? 'checked' : ''}>
                        <label class="form-check-label" for="tag-multiple-${id}">Allow Multiple</label>
                    </div>
                </div>
                <button class="btn btn-light btn-sm" button-type="add-sub-tag"><i class="fa-regular fa-plus"></i> Add Sub-Tag</button>
            </div>
            <div class="mt-2 sub-tags-container"></div>
        </div>`;
}
function createPCAExtractionFieldElement(fieldData, level) {
    const data = fieldData || {};
    const id = data.id || crypto.randomUUID();
    const isRequired = data.isRequired || false;

    // Determine the data type, defaulting to String (0) if not provided
    const dataType = (data.dataType !== undefined && data.dataType !== null) ? data.dataType : PCA_EXTRACTION_FIELD_DATA_TYPE.String;

    return `
        <div class="p-3 border rounded mb-2 extraction-field-box" data-id="${id}" data-level="${level}" style="background-color: ${(level == 0 ? '#1a1a1a' : `rgba(255,255,255,${level * 0.03});`)}">
             <div class="d-flex justify-content-between align-items-center mb-2">
                <h6 class="mb-0 text-muted">Field Definition</h6>
                <button class="btn btn-danger btn-sm" button-type="remove-item"><i class="fa-regular fa-trash"></i></button>
            </div>
            <div class="row">
                <div class="col-md-4 mb-3">
                    <label for="field-key-${id}" class="form-label form-label-sm">Key Name</label>
                    <input type="text" class="form-control form-control-sm" id="field-key-${id}" placeholder="e.g., customer_email" data-type="keyName" value="${data.keyName || ''}">
                </div>
                <div class="col-md-8 mb-3">
                    <label for="field-desc-${id}" class="form-label form-label-sm">Description for AI</label>
                    <input type="text" class="form-control form-control-sm" id="field-desc-${id}" placeholder="e.g., Extract the customer's primary contact email." data-type="description" value="${data.description || ''}">
                </div>
            </div>
            <div class="row align-items-center">
                <div class="col-md-3">
                    <label for="field-type-${id}" class="form-label form-label-sm">Data Type</label>
                    <select class="form-select form-select-sm" id="field-type-${id}" data-type="DataType">
                        <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.String}" ${dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.String ? 'selected' : ''}>String</option>
                        <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.Boolean}" ${dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Boolean ? 'selected' : ''}>Boolean</option>
                        <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.Number}" ${dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Number ? 'selected' : ''}>Number</option>
                        <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.DateTime}" ${dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.DateTime ? 'selected' : ''}>DateTime</option>
                        <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.Enum}" ${dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum ? 'selected' : ''}>Enum (Options)</option>
                    </select>
                </div>
                <div class="col-md-7 field-options-container">
                    <!-- This container will be populated by the 'change' event handler based on the Data Type selection -->
                </div>
                <div class="col-md-2 text-end">
                    <div class="form-check form-check-inline mt-3">
                        <input class="form-check-input" type="checkbox" id="field-required-${id}" data-type="isRequired" ${isRequired ? 'checked' : ''}>
                        <label class="form-check-label" for="field-required-${id}">Required</label>
                    </div>
                </div>
            </div>
            <div class="mt-2 rules-container">
                <!-- Conditional rules will be appended here -->
            </div>
            <button class="btn btn-light btn-sm mt-2" button-type="add-conditional-rule">
                <i class="fa-regular fa-plus"></i> Add Conditional Rule
            </button>
        </div>`;
}
function createPCAConditionalRuleElement(ruleData, parentFieldData) {
    const data = ruleData || {};
    const id = data.id || crypto.randomUUID();
    let valueInputHtml = '<input type="text" class="form-control form-control-sm" data-type="conditionValue">';
    if (parentFieldData.dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Boolean) {
        valueInputHtml = `<select class="form-select form-select-sm" data-type="conditionValue"><option value="true">True</option><option value="false">False</option></select>`;
    } else if (parentFieldData.dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum) {
        const optionsHtml = parentFieldData.options.map(opt => `<option value="${opt}">${opt}</option>`).join('');
        valueInputHtml = `<select class="form-select form-select-sm" data-type="conditionValue">${optionsHtml}</select>`;
    }
    return `
        <div class="p-2 mt-2 border rounded rule-box" data-id="${id}">
            <div class="d-flex align-items-center justify-content-between">
                <div class="d-flex align-items-center">
                    <strong class="me-2">IF Value</strong>
                    <select class="form-select form-select-sm me-2" data-type="conditionOperator" style="width: auto;">
                        <option value="${PCA_CONDITION_OPERATOR.Equals}">Equals</option>
                        <option value="${PCA_CONDITION_OPERATOR.NotEquals}">Not Equals</option>
                    </select>
                    <div style="width: 150px;">${valueInputHtml}</div>
                    <strong class="mx-2">THEN Extract:</strong>
                </div>
                <button class="btn btn-danger btn-sm" button-type="remove-item"><i class="fa-regular fa-trash"></i></button>
            </div>
            <div class="mt-2 dependent-fields-container"></div>
            <button class="btn btn-light btn-sm mt-2" button-type="add-dependent-field"><i class="fa-regular fa-plus"></i> Add Field</button>
        </div>`;
}
function updateParentTagDropdowns(tagSetElement, tagSetData = null) {
    const $tagSetElement = $(tagSetElement);
    const tags = [];
    $tagSetElement.find('.pca-tag-def-box').each((_, tagEl) => {
        tags.push({
            id: $(tagEl).data('tag-id'),
            name: $(tagEl).find('[data-type="tagName"]').val() || 'Untitled'
        });
    });

    $tagSetElement.find('select[data-type="parentTag"]').each((_, selectEl) => {
        const $selectEl = $(selectEl);
        const currentTagId = $selectEl.closest('.pca-tag-def-box').data('tag-id');
        let selectedValue = (tagSetData)
            ? tagSetData.tags.find(t => t.id === currentTagId)?.parentTagId || ""
            : $selectEl.val();

        $selectEl.empty().append('<option value="">No Parent</option>');

        tags.forEach(tag => {
            if (tag.id !== currentTagId) { // A tag cannot be its own parent
                $selectEl.append(`<option value="${tag.id}">${tag.name}</option>`);
            }
        });
        $selectEl.val(selectedValue);
    });
}

// EVENT HANDLER INITIALIZERS
function initPCAManagerEventHandlers() {
    new EmojiPicker({ trigger: [{ selector: "#pca-icon-input", insertInto: "#pca-icon-input" }], closeOnInsert: true });

    // Universal change listener
    pcaManagerView.on('input change', 'input, select, textarea', () => { if (managePCAType) checkPCAChanges(); });

    // Top-level "Add" buttons
    pcaAddTagSetButton.on('click', () => {
        if (pcaTagSetsList.children('.tag-box').length >= MAX_PCA_TAGS_PER_LEVEL) {
            AlertManager.createAlert({
                type: 'warning',
                message: `You can only add a maximum of ${MAX_PCA_TAGS_PER_LEVEL} tags at the top level.`,
                timeout: 3000
            });
            return;
        }
        pcaTagSetsList.append(createPCATagElement(null, 0));
    });

    pcaAddExtractionFieldButton.on('click', () => {
        if (pcaExtractionFieldsList.children('.extraction-field-box').length >= MAX_PCA_FIELDS_PER_LEVEL) {
            AlertManager.createAlert({
                type: 'warning',
                message: `You can only add a maximum of ${MAX_PCA_FIELDS_PER_LEVEL} fields at the top level.`,
                timeout: 3000
            });
            return;
        }
        pcaExtractionFieldsList.append(createPCAExtractionFieldElement(null, 0));
    });

    // Universal remove button (delegated)
    pcaManagerView.on('click', '[button-type="remove-item"]', (e) => {
        $(e.currentTarget).closest('.tag-box, .extraction-field-box, .rule-box').remove();
    });

    // --- Tagging specific delegation ---
    pcaTagSetsList.on('click', '[button-type="add-sub-tag"]', (e) => {
        const $parentBox = $(e.currentTarget).closest('.tag-box');
        const subTagContainer = $parentBox.find('.sub-tags-container').first();
        const currentLevel = parseInt($parentBox.data('level') || 0);
        const newLevel = currentLevel + 1;

        if (newLevel >= MAX_PCA_TAG_LEVELS) {
            AlertManager.createAlert({
                type: 'warning',
                message: `Nesting is limited to a maximum of ${MAX_PCA_TAG_LEVELS} levels.`,
                timeout: 3000
            });
            return;
        }

        if (subTagContainer.children('.tag-box').length >= MAX_PCA_TAGS_PER_LEVEL) {
            AlertManager.createAlert({
                type: 'warning',
                message: `You can only add a maximum of ${MAX_PCA_TAGS_PER_LEVEL} sub-tags per level.`,
                timeout: 3000
            });
            return;
        }

        subTagContainer.append(createPCATagElement(null, newLevel));
    });

    // --- Extraction specific delegation ---
    pcaExtractionFieldsList.on('click', '[button-type="add-conditional-rule"]', (e) => {
        const $fieldBox = $(e.currentTarget).closest('.extraction-field-box');
        const rulesContainer = $fieldBox.find('.rules-container').first();

        if (rulesContainer.children('.rule-box').length >= MAX_PCA_RULES_PER_FIELD) {
            AlertManager.createAlert({
                type: 'warning',
                message: `A field can have a maximum of ${MAX_PCA_RULES_PER_FIELD} conditional rules.`,
                timeout: 3000
            });
            return;
        }

        const parentFieldData = {
            dataType: parseInt($fieldBox.find('[data-type="DataType"]').val()),
            options: ($fieldBox.find('[data-type="DataType"]').val() == PCA_EXTRACTION_FIELD_DATA_TYPE.Enum)
                ? $fieldBox.find('.field-options-container input').val().split(',').map(s => s.trim()).filter(Boolean)
                : []
        };
        $fieldBox.find('.rules-container').first().append(createPCAConditionalRuleElement(null, parentFieldData));
    });

    pcaExtractionFieldsList.on('click', '[button-type="add-dependent-field"]', (e) => {
        const $ruleBox = $(e.currentTarget).closest('.rule-box');
        const dependentFieldsContainer = $ruleBox.find('.dependent-fields-container').first();
        const $parentFieldBox = $ruleBox.closest('.extraction-field-box');
        const parentFieldLevel = parseInt($parentFieldBox.data('level') || 0);
        const newLevel = parentFieldLevel + 1;

        if (newLevel >= MAX_PCA_EXTRACTION_LEVELS) {
            AlertManager.createAlert({
                type: 'warning',
                message: `Nesting is limited to a maximum of ${MAX_PCA_EXTRACTION_LEVELS} levels.`,
                timeout: 3000
            });
            return;
        }

        const totalSiblingFields = $parentFieldBox.find(`.extraction-field-box[data-level="${newLevel}"]`).length;

        if (totalSiblingFields >= MAX_PCA_FIELDS_PER_LEVEL) {
            AlertManager.createAlert({
                type: 'warning',
                message: `You can only add a maximum of ${MAX_PCA_FIELDS_PER_LEVEL} fields per level.`,
                timeout: 3000
            });
            return;
        }

        dependentFieldsContainer.append(createPCAExtractionFieldElement(null, newLevel));
    });
}

// MAIN INITIALIZER
function initPostAnalysisTab() {
    initPCAManagerEventHandlers();

    addNewPCATemplateButton.on('click', (e) => {
        managePCAType = 'new';
        currentPCATemplateData = createDefaultPCATemplateObject();
        resetPCAManager();
        pcaManagerNameBreadcrumb.text("New Template");
        showPCAManagerView();
        updateUrlForTab("postanalysis/new");
    });

    backToPCAListButton.on('click', async (e) => {
        e.preventDefault();
        if (await canLeavePCAManager(" Discard changes?")) {
            showPCAListView();
            managePCAType = null;
            updateUrlForTab("postanalysis");
        }
    });

    pcaListContainer.on('click', '.btn-edit', (e) => {
        e.stopPropagation();
        const templateId = $(e.currentTarget).closest('.pca-template-card').data('template-id');
        const templateData = BusinessFullData.businessApp.postAnalysisTemplates.find(t => t.id === templateId);

        managePCAType = 'edit';
        currentPCATemplateData = JSON.parse(JSON.stringify(templateData));

        resetPCAManager();
        fillPCAManager(currentPCATemplateData);
        pcaManagerNameBreadcrumb.text(currentPCATemplateData.general.name);
        checkPCAChanges(); // To correctly set initial save button state
        showPCAManagerView();
        updateUrlForTab(`postanalysis/${templateId}`);
    });

    pcaListContainer.on('click', '.btn-delete', async (e) => {
        e.stopPropagation();
        const card = $(e.currentTarget).closest('.pca-template-card');
        const templateId = card.data('template-id');
        const templateName = card.find('h4').text();

        const confirmDialog = new BootstrapConfirmDialog({
            title: "Delete Template",
            message: `Are you sure you want to delete the template "<strong>${templateName}</strong>"? This action cannot be undone.`,
            confirmText: "Delete",
            cancelText: "Cancel",
            confirmButtonClass: "btn-danger"
        });

        if (await confirmDialog.show()) {
            deletePCATemplate(templateId,
                (response) => {
                    BusinessFullData.businessApp.postAnalysisTemplates = BusinessFullData.businessApp.postAnalysisTemplates.filter(t => t.id !== templateId);
                    card.parent().fadeOut(300, function () { $(this).remove(); });
                    AlertManager.createAlert({ type: "success", message: "Template deleted successfully." });
                },
                (error) => {
                    AlertManager.createAlert({ type: "danger", message: "Failed to delete template. Check console for details." });
                    console.error("Failed to delete PCA template:", error);
                }
            );
        }
    });

    savePCATemplateButton.on('click', (e) => {
        e.preventDefault();
        if (isSavingPCATemplate) return;

        const validation = validatePCATemplate(false);
        if (!validation.validated) {
            AlertManager.createAlert({ type: "danger", message: `Validation failed:<br>${validation.errors.join("<br>")}` });
            return;
        }

        const { hasChanges, changes } = checkPCAChanges(false);
        if (!hasChanges) return;

        isSavingPCATemplate = true;
        savePCATemplateButton.prop("disabled", true);
        savePCATemplateButtonSpinner.removeClass("d-none");

        const formData = new FormData();
        formData.append("postType", managePCAType);
        formData.append("changes", JSON.stringify(changes));
        if (managePCAType === "edit") {
            formData.append("existingTemplateId", currentPCATemplateData.id);
        }

        savePCATemplate(formData,
            (response) => {
                const savedData = response.data;
                currentPCATemplateData = savedData;

                const existingIndex = BusinessFullData.businessApp.postAnalysisTemplates.findIndex(t => t.id === savedData.id);
                if (existingIndex > -1) {
                    BusinessFullData.businessApp.postAnalysisTemplates[existingIndex] = savedData;
                } else {
                    BusinessFullData.businessApp.postAnalysisTemplates.push(savedData);
                }

                fillPCAList();

                isSavingPCATemplate = false;
                savePCATemplateButton.prop("disabled", true);
                savePCATemplateButtonSpinner.addClass("d-none");

                AlertManager.createAlert({ type: "success", message: "Analysis template saved successfully." });
                managePCAType = 'edit';
                pcaManagerNameBreadcrumb.text(savedData.general.name);
            },
            (error, isUnsuccessful) => {
                isSavingPCATemplate = false;
                savePCATemplateButton.prop("disabled", false);
                savePCATemplateButtonSpinner.addClass("d-none");
                AlertManager.createAlert({ type: "danger", message: "Failed to save template. Check console for details." });
                console.error("Failed to save PCA template:", error);
            }
        );
    });

    // Initial Load
    fillPCAList();
}