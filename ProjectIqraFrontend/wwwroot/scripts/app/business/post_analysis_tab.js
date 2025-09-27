// --- 1. CONSTANTS & ENUMS ---
const PCA_SUMMARY_FORMAT = {
    Paragraph: 0,
    BulletPoints: 1
};

const PCA_EXTRACTION_FIELD_DATA_TYPE = {
    String: 0,
    Boolean: 1,
    Number: 2,
    DateTime: 3,
    Enum: 4
};

// --- 2. DYNAMIC VARIABLES ---
let managePCAType = null; // 'new' or 'edit'
let currentPCATemplateData = null; // Stores the original data of the template being edited
let isSavingPCATemplate = false;

// --- 3. ELEMENT VARIABLES ---
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


// --- 4. API FUNCTIONS ---
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


// --- 5. CORE UI & DATA FUNCTIONS ---

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
        general: { emoji: "📊", name: "", description: "" },
        summary: { isActive: true, prompt: "", parameters: { maxLength: 150, format: PCA_SUMMARY_FORMAT.Paragraph } },
        tagging: { tagSets: [] },
        extraction: { fields: [] }
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
    pcaSummaryMaxLengthInput.val(150);
    pcaSummaryFormatSelect.val(PCA_SUMMARY_FORMAT.Paragraph);

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
    pcaSummaryMaxLengthInput.val(templateData.summary.parameters.maxLength);
    pcaSummaryFormatSelect.val(templateData.summary.parameters.format);

    // Tags
    templateData.tagging.tagSets.forEach(tagSet => {
        const tagSetElement = $(createPCATagSetElement(tagSet));
        tagSetElement.find('[data-type="setName"]').val(tagSet.name);
        tagSetElement.find('[data-type="setDescription"]').val(tagSet.description);
        tagSetElement.find('[data-type="setRequired"]').prop('checked', tagSet.rules.isRequired);
        tagSetElement.find('[data-type="setAllowMultiple"]').prop('checked', tagSet.rules.allowMultiple);

        const tagsListContainer = tagSetElement.find('.pca-tags-list-container');
        tagSet.tags.forEach(tag => {
            const tagElement = $(createPCATagDefinitionElement(tag));
            tagElement.find('[data-type="tagName"]').val(tag.name);
            tagElement.find('[data-type="tagDescription"]').val(tag.description);
            // Parent ID will be set after all tags in the set are rendered
            tagsListContainer.append(tagElement);
        });

        pcaTagSetsList.append(tagSetElement);
        updateParentTagDropdowns(tagSetElement, tagSet);
    });

    // Extraction
    templateData.extraction.fields.forEach(field => {
        const fieldElement = $(createPCAExtractionFieldElement(field));
        fieldElement.find('[data-type="keyName"]').val(field.keyName);
        fieldElement.find('[data-type="description"]').val(field.description);
        fieldElement.find('[data-type="isRequired"]').prop('checked', field.isRequired);
        const dataTypeSelect = fieldElement.find('[data-type="DataType"]');
        dataTypeSelect.val(field.dataType);

        // IMPORTANT: Trigger change to render conditional inputs
        dataTypeSelect.trigger('change');

        if (field.dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum) {
            fieldElement.find('.field-options-container input').val(field.options.join(','));
        } else if (field.dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.String) {
            fieldElement.find('.field-options-container input').val(field.validation.pattern || '');
        }
        pcaExtractionFieldsList.append(fieldElement);
    });
}

function checkPCAChanges(enableDisableButton = true) {
    if (managePCAType === null) return { hasChanges: false, changes: {} };

    let hasChanges = false;
    const original = currentPCATemplateData;
    const findById = (arr, id) => arr.find(item => item.id === id);

    // Build current state from DOM
    const currentState = {
        general: { emoji: pcaIconInput.text(), name: pcaNameInput.val().trim(), description: pcaDescriptionInput.val().trim() },
        summary: { isActive: pcaSummaryActiveToggle.is(':checked'), prompt: pcaSummaryPromptInput.val().trim(), parameters: { maxLength: parseInt(pcaSummaryMaxLengthInput.val()) || 150, format: parseInt(pcaSummaryFormatSelect.val()) } },
        tagging: { tagSets: [] },
        extraction: { fields: [] }
    };
    pcaTagSetsList.find('.pca-tag-set-box').each((_, setEl) => {
        const $setEl = $(setEl);
        const tagSet = {
            id: $setEl.data('set-id'),
            name: $setEl.find('[data-type="setName"]').val().trim(),
            description: $setEl.find('[data-type="setDescription"]').val().trim(),
            rules: {
                isRequired: $setEl.find('[data-type="setRequired"]').is(':checked'),
                allowMultiple: $setEl.find('[data-type="setAllowMultiple"]').is(':checked'),
            },
            tags: []
        };
        $setEl.find('.pca-tag-def-box').each((_, tagEl) => {
            const $tagEl = $(tagEl);
            tagSet.tags.push({
                id: $tagEl.data('tag-id'),
                name: $tagEl.find('[data-type="tagName"]').val().trim(),
                description: $tagEl.find('[data-type="tagDescription"]').val().trim(),
                parentTagId: $tagEl.find('[data-type="parentTag"]').val() || null,
            });
        });
        currentState.tagging.tagSets.push(tagSet);
    });
    pcaExtractionFieldsList.find('.pca-extraction-field-box').each((_, fieldEl) => {
        const $fieldEl = $(fieldEl);
        const dataType = parseInt($fieldEl.find('[data-type="DataType"]').val());
        const field = {
            id: $fieldEl.data('field-id'),
            keyName: $fieldEl.find('[data-type="keyName"]').val().trim(),
            description: $fieldEl.find('[data-type="description"]').val().trim(),
            isRequired: $fieldEl.find('[data-type="isRequired"]').is(':checked'),
            dataType: dataType,
            options: [],
            validation: { pattern: null }
        };
        if (dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum) {
            field.options = $fieldEl.find('.field-options-container input').val().split(',').map(s => s.trim()).filter(Boolean);
        } else if (dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.String) {
            field.validation.pattern = $fieldEl.find('.field-options-container input').val().trim() || null;
        }
        currentState.extraction.fields.push(field);
    });

    // Compare General
    if (currentState.general.name !== original.general.name ||
        currentState.general.description !== original.general.description ||
        currentState.general.emoji !== original.general.emoji)
    {
        hasChanges = true;
    }

    // Compare Summary
    if (currentState.summary.isActive !== original.summary.isActive ||
        currentState.summary.prompt !== original.summary.prompt ||
        currentState.summary.parameters.maxLength !== original.summary.parameters.maxLength ||
        currentState.summary.parameters.format !== original.summary.parameters.format)
    {
        hasChanges = true;
    }

    // Compare Tagging
    if (currentState.tagging.tagSets.length !== original.tagging.tagSets.length) {
        hasChanges = true;
    } else {
        for (const currentSet of currentState.tagging.tagSets) {
            const originalSet = findById(original.tagging.tagSets, currentSet.id);
            if (!originalSet || currentSet.name !== originalSet.name ||
                currentSet.description !== originalSet.description ||
                currentSet.rules.isRequired !== originalSet.rules.isRequired ||
                currentSet.rules.allowMultiple !== originalSet.rules.allowMultiple ||
                currentSet.tags.length !== originalSet.tags.length)
            {
                hasChanges = true; break;
            }
            for (const currentTag of currentSet.tags) {
                const originalTag = findById(originalSet.tags, currentTag.id);
                if (!originalTag || currentTag.name !== originalTag.name ||
                    currentTag.description !== originalTag.description ||
                    currentTag.parentTagId !== originalTag.parentTagId)
                {
                    hasChanges = true; break;
                }
            }
            if (hasChanges) break;
        }
    }
    
    // Compare Extraction
    if (currentState.extraction.fields.length !== original.extraction.fields.length) {
        hasChanges = true;
    } else {
        for (const currentField of currentState.extraction.fields) {
            const originalField = findById(original.extraction.fields, currentField.id);
            if (!originalField || currentField.keyName !== originalField.keyName ||
                currentField.description !== originalField.description ||
                currentField.isRequired !== originalField.isRequired ||
                currentField.dataType !== originalField.dataType ||
                JSON.stringify(currentField.options) !== JSON.stringify(originalField.options) ||
                currentField.validation.pattern !== originalField.validation.pattern)
            {
                hasChanges = true; break;
            }
        }
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
    pcaTagSetsList.find('.pca-tag-set-box').each((i, setEl) => {
        const $setEl = $(setEl);
        const setNameInput = $setEl.find('[data-type="setName"]');
        if (!setNameInput.val().trim()) {
            validated = false; errors.push(`Tag Set #${i + 1}: Name is required.`);
            if (!onlyRemove) setNameInput.addClass('is-invalid');
        }
        $setEl.find('.pca-tag-def-box').each((j, tagEl) => {
            const tagNameInput = $(tagEl).find('[data-type="tagName"]');
            if (!tagNameInput.val().trim()) {
                validated = false; errors.push(`Tag Set "${setNameInput.val() || 'Untitled'}", Tag #${j + 1}: Name is required.`);
                if (!onlyRemove) tagNameInput.addClass('is-invalid');
            }
        });
    });

    // Extraction
    const keyNames = new Set();
    pcaExtractionFieldsList.find('.pca-extraction-field-box').each((i, fieldEl) => {
        const $fieldEl = $(fieldEl);
        const keyNameInput = $fieldEl.find('[data-type="keyName"]');
        const keyName = keyNameInput.val().trim();
        if (!keyName) {
            validated = false; errors.push(`Extraction Field #${i + 1}: Key Name is required.`);
            if (!onlyRemove) keyNameInput.addClass('is-invalid');
        } else if (keyNames.has(keyName)) {
            validated = false; errors.push(`Extraction Field #${i + 1}: Key Name "${keyName}" must be unique.`);
            if (!onlyRemove) keyNameInput.addClass('is-invalid');
        } else {
            keyNames.add(keyName);
        }
    });

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

// --- 6. DYNAMIC ELEMENT CREATORS ---

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

function createPCATagSetElement(tagSetData) {
    const setId = (tagSetData && tagSetData.id) ? tagSetData.id : crypto.randomUUID();
    return `
        <div class="p-3 border rounded mb-3 pca-tag-set-box" data-set-id="${setId}">
            <div class="d-flex justify-content-between align-items-center mb-2">
                <h5 class="mb-0">Tag Set</h5>
                <button class="btn btn-danger btn-sm" button-type="remove-tag-set"><i class="fa-regular fa-trash"></i></button>
            </div>
            <div class="row">
                <div class="col-md-6 mb-3"><input type="text" class="form-control" placeholder="Tag Set Name (e.g., Call Outcome)" data-type="setName"></div>
                <div class="col-md-6 mb-3"><input type="text" class="form-control" placeholder="Description for AI" data-type="setDescription"></div>
            </div>
            <div class="d-flex mb-3">
                <div class="form-check form-check-inline me-3">
                    <input class="form-check-input" type="checkbox" id="tag-set-required-${setId}" data-type="setRequired">
                    <label class="form-check-label" for="tag-set-required-${setId}">Required</label>
                </div>
                <div class="form-check form-check-inline">
                    <input class="form-check-input" type="checkbox" id="tag-set-multiple-${setId}" data-type="setAllowMultiple">
                    <label class="form-check-label" for="tag-set-multiple-${setId}">Allow Multiple Selections</label>
                </div>
            </div>
            <h6>Tags</h6>
            <div class="pca-tags-list-container mb-2"></div>
            <button class="btn btn-light btn-sm" button-type="add-tag-definition">
                <i class="fa-regular fa-plus"></i> Add Tag
            </button>
        </div>`;
}

function createPCATagDefinitionElement(tagDefData) {
    const tagId = (tagDefData && tagDefData.id) ? tagDefData.id : crypto.randomUUID();
    return `
        <div class="p-2 border-top pca-tag-def-box" data-tag-id="${tagId}">
            <div class="input-group mb-2">
                <input type="text" class="form-control" placeholder="Tag Name (e.g., Resolved)" data-type="tagName">
                <select class="form-select" data-type="parentTag" style="max-width: 200px;"><option value="">No Parent</option></select>
                <button class="btn btn-danger btn-sm" button-type="remove-tag-definition"><i class="fa-regular fa-trash"></i></button>
            </div>
            <textarea class="form-control form-control-sm" rows="2" placeholder="Description for AI to understand when to use this tag." data-type="tagDescription"></textarea>
        </div>`;
}

function createPCAExtractionFieldElement(fieldData) {
    const fieldId = (fieldData && fieldData.id) ? fieldData.id : crypto.randomUUID();
    return `
        <div class="p-3 border rounded mb-3 pca-extraction-field-box" data-field-id="${fieldId}">
             <div class="d-flex justify-content-between align-items-center mb-2">
                <h6 class="mb-0">Field Definition</h6>
                <button class="btn btn-danger btn-sm" button-type="remove-extraction-field"><i class="fa-regular fa-trash"></i></button>
            </div>
            <div class="row">
                <div class="col-md-4 mb-3"><input type="text" class="form-control" placeholder="Key Name (e.g., customer_email)" data-type="keyName"></div>
                <div class="col-md-8 mb-3"><input type="text" class="form-control" placeholder="Description for AI (e.g., Extract the customer's primary contact email.)" data-type="description"></div>
            </div>
            <div class="row align-items-center">
                <div class="col-md-3">
                    <select class="form-select form-select-sm" data-type="DataType">
                        <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.String}">String</option>
                        <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.Boolean}">Boolean</option>
                        <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.Number}">Number</option>
                        <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.DateTime}">DateTime</option>
                        <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.Enum}">Enum (Options)</option>
                    </select>
                </div>
                <div class="col-md-7 field-options-container d-none"></div>
                <div class="col-md-2 text-end">
                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="checkbox" id="field-required-${fieldId}" data-type="isRequired">
                        <label class="form-check-label" for="field-required-${fieldId}">Required</label>
                    </div>
                </div>
            </div>
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


// --- 7. EVENT HANDLER INITIALIZERS ---

function initPCAManagerEventHandlers() {
    // General Tab
    new EmojiPicker({ trigger: [{ selector: "#pca-icon-input", insertInto: "#pca-icon-input" }], closeOnInsert: true });

    // Tags Tab
    pcaAddTagSetButton.on('click', () => {
        pcaTagSetsList.append(createPCATagSetElement(null));
    });

    pcaTagSetsList.on('click', 'button[button-type="remove-tag-set"]', (e) => {
        $(e.currentTarget).closest('.pca-tag-set-box').remove();
        checkPCAChanges();
    });

    pcaTagSetsList.on('click', 'button[button-type="add-tag-definition"]', (e) => {
        const $tagSetBox = $(e.currentTarget).closest('.pca-tag-set-box');
        $tagSetBox.find('.pca-tags-list-container').append(createPCATagDefinitionElement(null));
        updateParentTagDropdowns($tagSetBox);
    });

    pcaTagSetsList.on('click', 'button[button-type="remove-tag-definition"]', (e) => {
        const $tagSetBox = $(e.currentTarget).closest('.pca-tag-set-box');
        $(e.currentTarget).closest('.pca-tag-def-box').remove();
        updateParentTagDropdowns($tagSetBox);
        checkPCAChanges();
    });

    pcaTagSetsList.on('input', 'input[data-type="tagName"]', (e) => {
        const $tagSetBox = $(e.currentTarget).closest('.pca-tag-set-box');
        updateParentTagDropdowns($tagSetBox);
    });

    // Extraction Tab
    pcaAddExtractionFieldButton.on('click', () => {
        pcaExtractionFieldsList.append(createPCAExtractionFieldElement(null));
    });

    pcaExtractionFieldsList.on('click', 'button[button-type="remove-extraction-field"]', (e) => {
        $(e.currentTarget).closest('.pca-extraction-field-box').remove();
        checkPCAChanges();
    });

    pcaExtractionFieldsList.on('change', 'select[data-type="DataType"]', (e) => {
        const selectedType = parseInt($(e.currentTarget).val());
        const optionsContainer = $(e.currentTarget).closest('.pca-extraction-field-box').find('.field-options-container');
        optionsContainer.addClass('d-none').empty();
        if (selectedType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum) {
            optionsContainer.removeClass('d-none').html('<input type="text" class="form-control form-control-sm" placeholder="Comma-separated options (e.g., Low,Medium,High)">');
        } else if (selectedType === PCA_EXTRACTION_FIELD_DATA_TYPE.String) {
            optionsContainer.removeClass('d-none').html('<input type="text" class="form-control form-control-sm" placeholder="Optional: Regex Pattern">');
        }
    });

    // Universal change listener
    pcaManagerView.on('input change', 'input, select, textarea', () => {
        if (managePCAType) {
            checkPCAChanges();
            validatePCATemplate(true);
        }
    });
}


// --- 8. MAIN INITIALIZER ---
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