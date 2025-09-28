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
    GreaterThanOrEqual: 4,
    LessThan: 5,
    LessThanOrEqual: 6
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

// Integration Managers
let pcaConfigurationLLMIntegrationManager = null;

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

// Manager View - Configuration Tab
const pcaLlmIntegrationSelector = pcaManagerView.find("#pca-llm-integration-selector");

// Manager View - Summary Tab
const pcaSummaryActiveToggle = pcaManagerView.find("#pca-summary-active-toggle");
const pcaSummaryConfigContainer = pcaManagerView.find("#pca-summary-config-container");
const pcaSummaryPromptInput = pcaManagerView.find("#pca-summary-prompt-input");
const pcaSummaryMaxLengthInput = pcaManagerView.find("#pca-summary-maxlength-input");
const pcaSummaryFormatSelect = pcaManagerView.find("#pca-summary-format-select");

// Manager View - Conversation Tags Tab
const pcaTagsActiveToggle = pcaManagerView.find("#pca-tags-active-toggle");
const pcaTagsConfigContainer = pcaManagerView.find("#pca-tags-config-container");
const pcaAddTagSetButton = pcaManagerView.find("#pca-add-tag-set-button");
const pcaTagSetsList = pcaManagerView.find("#pca-tag-sets-list");

// Manager View - Data Extraction Tab
const pcaExtractionActiveToggle = pcaManagerView.find("#pca-extraction-active-toggle");
const pcaExtractionConfigContainer = pcaManagerView.find("#pca-extraction-config-container");
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
    const templates = BusinessFullData.businessApp.postAnalysis;
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
        configuration: {
            llmIntegration: null
        },
        summary: {
            isActive: true,
            prompt: ""
        },
        tagging: {
            isActive: true,
            tags: []
        },
        extraction: {
            isActive: true,
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

    // Configuration
    if (pcaConfigurationLLMIntegrationManager) pcaConfigurationLLMIntegrationManager.reset();

    // Summary
    pcaSummaryActiveToggle.prop("checked", true).change();
    pcaSummaryPromptInput.val("");

    // Tags
    pcaTagsActiveToggle.prop("checked", true).change();
    pcaTagSetsList.empty();

    // Extraction
    pcaExtractionActiveToggle.prop("checked", true).change();
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

    // Configuration
    if (pcaConfigurationLLMIntegrationManager) pcaConfigurationLLMIntegrationManager.load(templateData.configuration.llmIntegration);

    // Summary
    pcaSummaryActiveToggle.prop("checked", templateData.summary.isActive).change();
    pcaSummaryPromptInput.val(templateData.summary.prompt);

    // Tags
    pcaTagsActiveToggle.prop("checked", templateData.tagging.isActive).change();
    renderPCATags(pcaTagSetsList, templateData.tagging.tags, 0);

    // Extraction
    pcaExtractionActiveToggle.prop("checked", templateData.extraction.isActive).change();
    renderPCAExtractionFields(pcaExtractionFieldsList, templateData.extraction.fields, 0);
}
function checkPCAChanges(enableDisableButton = true) {
    if (managePCAType === null) return;

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
        configuration: {
            llmIntegration: pcaConfigurationLLMIntegrationManager.getData()
        },
        summary: {
            isActive: pcaSummaryActiveToggle.is(':checked'),
            prompt: pcaSummaryPromptInput.val().trim()
        },
        tagging: {
            isActive: pcaTagsActiveToggle.is(':checked'),
            tags: getPCATagsFromDOM(pcaTagSetsList)
        },
        extraction: {
            isActive: pcaExtractionActiveToggle.is(':checked'),
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

    // Compare Configuration
    // TODO better check
    if (JSON.stringify(currentState.configuration.llmIntegration) !== JSON.stringify(original.configuration.llmIntegration)) {
        hasChanges = true;
    }

    // Compare Summary
    if (currentState.summary.isActive !== original.summary.isActive ||
        currentState.summary.prompt !== original.summary.prompt
    ) {
        hasChanges = true;
    }

    // Compare Tagging recursively
    if (currentState.tagging.isActive !== original.tagging.isActive) {
        hasChanges = true;
    }
    if (!hasChanges) {
        hasChanges = comparePCATags(currentState.tagging.tags, original.tagging.tags);
    }

    // Compare Extraction recursively
    if (currentState.extraction.isActive !== original.extraction.isActive) {
        hasChanges = true;
    }
    if (!hasChanges) {
        hasChanges = comparePCAFields(currentState.extraction.fields, original.extraction.fields);
    }

    if (enableDisableButton) {
        savePCATemplateButton.prop("disabled", !hasChanges);
    }
    return { hasChanges, changes: currentState };
}
function validatePCATemplate(onlyRemove = true) {
    if (managePCAType === null) return;

    const errors = [];
    let validated = true;

    // General
    var nameValue = pcaNameInput.val()?.trim() ?? null;
    if (!nameValue || nameValue == null || nameValue == "") {
        validated = false; errors.push("Template Name is required.");

        if (!onlyRemove) {
            pcaNameInput.addClass('is-invalid');
        }
    }
    else {
        pcaNameInput.removeClass('is-invalid');
    }

    // Compare Configuration
    const llmValidation = pcaConfigurationLLMIntegrationManager.validate();
    if (!llmValidation.isValid) {
        validated = false;
        errors.push(...llmValidation.errors.map(e => `Configuration LLM: ${e}`));
        if (!onlyRemove) pcaConfigurationLLMIntegrationManager.getSelectElements().addClass('is-invalid');
    }

    // Tagging
    var tagsInvalid = validatePCATagsRecursive(pcaTagSetsList, errors, onlyRemove, 0);
    if (tagsInvalid) validated = false;

    // Extraction
    const keyNames = new Set();
    var fieldsInvalid = validatePCAFieldsRecursive(pcaExtractionFieldsList, errors, keyNames, onlyRemove, 0);
    if (fieldsInvalid) validated = false;

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
        let correctPath;
        if (managePCAType === 'new') {
            correctPath = 'postanalysis/new';
        } else {
            correctPath = `postanalysis/${currentPCATemplateData.id}`;
        }

        replaceUrlForTab(correctPath);
        return;
    }

    if (!subPath || subPath.length === 0) {
        if (pcaManagerView.hasClass('show') && !pcaListView.hasClass('show')) {
            showPCAListView();
        }
        replaceUrlForTab('postanalysis');
        return;
    }

    const action = subPath[0];
    const postAnalysisCard = pcaListContainer.find(`.post-analysis-card[data-template-id="${action}"]`);

    if (action === 'new') {
        if (!pcaManagerView.hasClass('show')) {
            addNewPCATemplateButton.click();
        }
    } else if (postAnalysisCard.length > 0) {
        if (!pcaManagerView.hasClass('show')) {
            postAnalysisCard.click();
        }
    } else {
        showPCAListView();
        replaceUrlForTab('postanalysis');
    }
}
function SetPostAnalysisCardDynamicWidth() {
    if (!postAnalysisTab.hasClass("show")) return;

    const anyPostAnalysisCard = pcaListContainer.find(".post-analysis-card");
    if (anyPostAnalysisCard.length > 0) {
        const firstPostAnalysisCard = anyPostAnalysisCard.first();

        const postAnalysisCardWidth = firstPostAnalysisCard.innerWidth();

        const postAnalysisCardLeftRightPadding = parseInt(firstPostAnalysisCard.css("padding-left")) + parseInt(firstPostAnalysisCard.css("padding-right"));
        const postAnalysisIconWidthAndPadding = firstPostAnalysisCard.find(".route-icon").innerWidth();

        // .campaign-card h4
        const marginLeftForH4 = 20; // .campaign-card h4 in style.css

        const currentUsedUpSpace = postAnalysisCardLeftRightPadding + postAnalysisIconWidthAndPadding + marginLeftForH4;

        let availableH4Space = postAnalysisCardWidth - currentUsedUpSpace;

        if (availableH4Space < 5) {
            availableH4Space = 5;
        }

        // .campaign-card h5-info
        let availableH5Space = postAnalysisCardWidth - postAnalysisCardLeftRightPadding;

        // FINAL
        $("#dynamicPostAnalysisCardCSS").html(`
            .post-analysis-card .card-data {
				width: ${availableH4Space}px;
			}

            .post-analysis-card .h5-info {
                width: ${availableH5Space}px;
            }
		`);
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
        const rulesContainer = $el.children('.rules-container').first();
        const dataType = parseInt($el.find('[data-type="DataType"]').first().find('option:selected').val());
        const field = {
            id: $el.data('id'),
            keyName: $el.find('[data-type="keyName"]').first().val().trim(),
            description: $el.find('[data-type="description"]').first().val().trim(),
            isRequired: $el.find('[data-type="isRequired"]').first().is(':checked'),
            isEmptyOrNullAllowed: $el.find('[data-type="isEmptyOrNullAllowed"]').first().is(':checked'),
            dataType: dataType,
            validation: {},
            conditionalRules: getPCARulesFromDOM(rulesContainer)
        };
        if (dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum) {
            field.options = $el.find('.field-options-container input').first().val()?.split(',').map(s => s.trim()).filter(Boolean) ?? [];
        }
        else if (dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Number) {
            const minlengthValue = $el.find('[data-type="minLength"]').first().val()?.trim() ?? null;
            const maxlengthValue = $el.find('[data-type="maxLength"]').first().val()?.trim() ?? null;

            if (minlengthValue) field.validation.minLength = parseInt(minlengthValue);
            if (maxlengthValue) field.validation.maxLength = parseInt(maxlengthValue);

            fields.validation.minLength = (minlengthValue == null || isNaN(minlengthValue)) ? null : minlengthValue;
            fields.validation.maxLength = (maxlengthValue == null || isNaN(maxlengthValue)) ? null : maxlengthValue;
        }
        else if (dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.String) {
            field.validation.pattern = $el.find('.field-options-container input').first().val()?.trim() ?? '';
        }
        fields.push(field);
    });
    return fields;
}
function getPCARulesFromDOM(fieldBox) {
    const rules = [];
    fieldBox.children('.rule-box').each((_, el) => {
        const $el = $(el);
        const dependentFieldsContainer = $el.find('.dependent-fields-container').first();
        rules.push({
            id: $el.data('id'),
            condition: {
                operator: parseInt($el.find('[data-type="conditionOperator"]').first().val()),
                value: $el.find('[data-type="conditionValue"]').first().val()
            },
            fieldsToExtract: getPCAFieldsFromDOM(dependentFieldsContainer)
        });
    });
    return rules;
}
function validatePCATagsRecursive(container, errors, onlyRemove, level, path = "Tags") {
    let isInvalid = false;

    container.children('.tag-box').each((i, el) => {
        const $el = $(el);
        const currentPath = `${path} -> #${i + 1}`;

        if (level >= MAX_PCA_TAG_LEVELS) {
            errors.push(`${path}: Exceeded maximum tag nesting depth of ${MAX_PCA_TAG_LEVELS}.`);
            return true;

            if (!onlyRemove) {
                $el.addClass('is-invalid');
            }
        }
        else {
            $el.removeClass('is-invalid');
        }

        if (container.children('.tag-box').length > MAX_PCA_TAGS_PER_LEVEL) {
            errors.push(`${path}: Exceeded maximum of ${MAX_PCA_TAGS_PER_LEVEL} tags per level.`);
            isInvalid = true;

            if (!onlyRemove) {
                $el.addClass('is-invalid');
            }
        }
        else {
            $el.removeClass('is-invalid');
        }

        const nameInput = $el.find('[data-type="name"]');
        const nameValue = nameInput.val()?.trim() ?? null;
        if (!nameValue || nameValue == null || nameValue == "") {
            errors.push(`${currentPath}: Tag Name is missing or invalid.`);
            isInvalid = true;

            if (!onlyRemove) {
                nameInput.addClass('is-invalid');
            }
        }
        else {
            nameInput.removeClass('is-invalid');
        }

        const descriptionInput = $el.find('[data-type="description"]');
        const descriptionValue = descriptionInput.val()?.trim() ?? null;
        if (!descriptionInput || descriptionValue == null || descriptionValue == "") {
            errors.push(`${currentPath}: Tag Description is missing or invalid.`);
            isInvalid = true;

            if (!onlyRemove) {
                descriptionInput.addClass('is-invalid');
            }
        }
        else {
            descriptionInput.removeClass('is-invalid');
        }

        const subTagContainer = $el.find('.sub-tags-container').first();

        var subTagsIsInvalid = validatePCATagsRecursive(subTagContainer, errors, onlyRemove, level + 1, currentPath);
        if (!isInvalid) isInvalid = subTagsIsInvalid;
    });

    return isInvalid;
}
function validatePCAFieldsRecursive(container, errors, uniqueKeys, onlyRemove, level = 0, path = "Fields") {
    var isInvalid = false;

    if (level >= MAX_PCA_EXTRACTION_LEVELS) {
        errors.push(`${path}: Exceeded maximum field nesting depth of ${MAX_PCA_EXTRACTION_LEVELS}.`);      
        isInvalid = true;

        if (!onlyRemove) {
            container.closest('.extraction-field-box, .rule-box').addClass('is-invalid'); // todo
        }
    }
    else {
        container.closest('.extraction-field-box, .rule-box').removeClass('is-invalid'); // todo
    }

    if (level > 0 && container.children('.extraction-field-box').length > MAX_PCA_FIELDS_PER_LEVEL) {
        errors.push(`${path}: Exceeded maximum of ${MAX_PCA_FIELDS_PER_LEVEL} fields per level.`);
        isInvalid = true;

        if (!onlyRemove) {
            container.addClass('is-invalid');
        }
    }
    else {
        container.removeClass('is-invalid');
    }

    container.children('.extraction-field-box').each((i, el) => {
        const $el = $(el);
        const currentPath = `${path} -> #${i + 1}`;

        const keyNameInput = $el.find('[data-type="keyName"]').first();
        const keyName = keyNameInput.val()?.trim() ?? null;
        if (!keyName || keyName == null || keyName == "") {
            errors.push(`${currentPath}: Key Name is required.`);
            isInvalid = true;

            if (!onlyRemove) {
                keyNameInput.addClass('is-invalid');
            }
        }
        else if (uniqueKeys.has(keyName)) {
            errors.push(`${currentPath}: Key Name "${keyName}" must be unique across the entire template.`);
            isInvalid = true;

            if (!onlyRemove) {
                keyNameInput.addClass('is-invalid');
            }
        }
        else {
            keyNameInput.removeClass('is-invalid');
            uniqueKeys.add(keyName)
        }

        const descriptionInput = $el.find('[data-type="description"]').first();
        const description = descriptionInput.val()?.trim() ?? null;
        if (!description || description == null || description == "") {
            errors.push(`${currentPath}: Description is required.`);
            isInvalid = true;

            if (!onlyRemove) {
                descriptionInput.addClass('is-invalid');
            }
        }
        else {
            descriptionInput.removeClass('is-invalid');
        }

        const dataTypeInput = $el.find('[data-type="DataType"]').first();
        const dataTypeString = dataTypeInput.find('option:selected').val()?.trim() ?? null;
        if (!dataTypeString || dataTypeString == null || dataTypeString == "") {
            errors.push(`${currentPath}: Data Type is required.`);
            isInvalid = true;

            if (!onlyRemove) {
                dataTypeInput.addClass('is-invalid');
            }
        }
        else {
            const dataType = parseInt(dataTypeString);

            if (dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum) {
                const optionsInput = $el.find('.field-options-container input[data-type="enum-options"]').first();
                const options = optionsInput.val()?.split(',').map(s => s.trim()).filter(Boolean) ?? [];

                if (options.length === 0) {
                    isInvalid = true; errors.push(`${currentPath}: Enum options cannot be empty.`);

                    if (!onlyRemove) {
                        optionsInput.addClass('is-invalid');
                    }
                }
                else if (new Set(options).size !== options.length) {
                    isInvalid = true; errors.push(`${currentPath}: Enum options must be unique.`);

                    if (!onlyRemove) {
                        optionsInput.addClass('is-invalid');
                    }
                }
                else {
                    optionsInput.removeClass('is-invalid');
                }
            }
            else if (dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Number) {
                const minInput = $el.find('.field-options-container input[data-type="min"]').first();
                const maxInput = $el.find('.field-options-container input[data-type="max"]').first();

                const minValue = parseInt(minInput.val()?.trim() ?? null);
                const maxValue = parseInt(maxInput.val()?.trim() ?? null);

                if (
                    (minValue && minValue != null && !isNaN(minValue))
                    &&
                    (maxValue && maxValue != null && !isNaN(maxValue))
                    &&
                    (minValue > maxValue)
                ) {
                    isInvalid = true;
                    errors.push(`${currentPath}: Min value must be less than max value.`);

                    if (!onlyRemove) {
                        minInput.addClass('is-invalid');
                        maxInput.addClass('is-invalid');
                    }
                }
                else {
                    minInput.removeClass('is-invalid');
                    maxInput.removeClass('is-invalid');
                }
            }

            dataTypeInput.removeClass('is-invalid');
        }

        const rulesContainer = $el.children('.rules-container').first();
        if (rulesContainer.children('.rule-box').length > MAX_PCA_RULES_PER_FIELD) {
            isInvalid = true;
            errors.push(`${currentPath}: Exceeded maximum of ${MAX_PCA_RULES_PER_FIELD} rules per field.`);

            if (!onlyRemove) {
                rulesContainer.addClass('is-invalid');
            }
        }
        else {
            rulesContainer.removeClass('is-invalid');
        }

        const ruleBoxChilds = rulesContainer.children('.rule-box');
        if (ruleBoxChilds.length > 0) {
            ruleBoxChilds.each((j, ruleEl) => {
                const conditionalSelect = $(ruleEl).find('select[data-type="conditionOperator"]').first();
                const operator = conditionalSelect.val()?.trim() ?? null;
                if (!operator || operator == null || operator == "") {
                    errors.push(`${currentPath} -> Rule #${j + 1}: Operator is required.`);
                    isInvalid = true;

                    if (!onlyRemove) {
                        conditionalSelect.addClass('is-invalid');
                    }
                }
                else {
                    conditionalSelect.removeClass('is-invalid');
                }

                const valueInput = $(ruleEl).find('[data-type="conditionValue"]').first();
                const isValueInputSelect = valueInput.is('select');
                const value = isValueInputSelect ? (valueInput.find('option:selected').val()?.trim() ?? null) : (valueInput.val()?.trim() ?? null);
                if (!value || value == null || value == "") {
                    errors.push(`${currentPath} -> Rule #${j + 1}: Value is required.`);
                    isInvalid = true;

                    if (!onlyRemove) {
                        valueInput.addClass('is-invalid');
                    }
                }
                else {
                    valueInput.removeClass('is-invalid');
                }

                const dependentFieldsContainer = $(ruleEl).find('.dependent-fields-container').first();
                if (dependentFieldsContainer.children('.extraction-field-box').length == 0) {
                    errors.push(`${currentPath} -> Rule #${j + 1}: At least one field is required.`);
                    isInvalid = true;

                    if (!onlyRemove) {
                        dependentFieldsContainer.addClass('is-invalid');
                    }
                }
                else {
                    dependentFieldsContainer.removeClass('is-invalid');

                    const isRuleInvalid = validatePCAFieldsRecursive($(ruleEl).find('.dependent-fields-container').first(), errors, uniqueKeys, onlyRemove, level + 1, `${currentPath} -> Rule #${j + 1}`);
                    if (!isInvalid) isInvalid = isRuleInvalid;
                }
            });
        }
    });

    return isInvalid;
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
function resetPCARuleConditions($fieldBox) {
    const newDataType = parseInt($fieldBox.find('select[data-type="DataType"]').first().val());
    const newOptions = (newDataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum)
        ? ($fieldBox.find('.field-options-container input').first().val() || '').split(',').map(s => s.trim()).filter(Boolean)
        : [];

    $fieldBox.find('.rules-container').first().children('.rule-box').each((_, ruleEl) => {
        const $ruleBox = $(ruleEl);
        const $operatorSelect = $ruleBox.find('select[data-type="conditionOperator"]').first();
        const $valueContainer = $operatorSelect.next('div');

        // 1. Rebuild the operators dropdown
        let operatorsHtml = `<option value="" disabled selected>Select Operator</option>
                             <option value="${PCA_CONDITION_OPERATOR.Equals}">Equals</option>
                             <option value="${PCA_CONDITION_OPERATOR.NotEquals}">Not Equals</option>`;
        if (newDataType === PCA_EXTRACTION_FIELD_DATA_TYPE.String) {
            operatorsHtml += `<option value="${PCA_CONDITION_OPERATOR.Contains}">Contains</option>`;
        }
        if (newDataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Number || newDataType === PCA_EXTRACTION_FIELD_DATA_TYPE.DateTime) {
            operatorsHtml += `<option value="${PCA_CONDITION_OPERATOR.GreaterThan}">Greater Than</option>
                              <option value="${PCA_CONDITION_OPERATOR.GreaterThanOrEqual}">Greater Than Or Equal</option>
                              <option value="${PCA_CONDITION_OPERATOR.LessThan}">Less Than</option>
                              <option value="${PCA_CONDITION_OPERATOR.LessThanOrEqual}">Less Than Or Equal</option>`;
        }
        $operatorSelect.html(operatorsHtml);

        // 2. Rebuild the value input
        let valueInputHtml = '<input type="text" class="form-control form-control-sm" data-type="conditionValue">';
        if (newDataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Boolean) {
            valueInputHtml = `<select class="form-select form-select-sm" data-type="conditionValue"><option value="true">True</option><option value="false">False</option></select>`;
        } else if (newDataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum) {
            const optionsHtml = newOptions.map(opt => `<option value="${opt}">${opt}</option>`).join('');
            valueInputHtml = `
                <select class="form-select form-select-sm" data-type="conditionValue">
                        <option value="" disabled selected>Select Option</option>
                        ${optionsHtml}
                </select>
            `;
        }
        $valueContainer.html(valueInputHtml);
    });
}
function renderPCAFieldOptionsContainer($selectElement) {
    function findFieldByIdRecursive(fields, id) {
        for (const field of fields) {
            if (field.id === id) return field;
            for (const rule of field.conditionalRules) {
                const found = findFieldByIdRecursive(rule.fieldsToExtract, id);
                if (found) return found;
            }
        }
        return null;
    }

    const $fieldBox = $selectElement.closest('.extraction-field-box');
    const $optionsContainer = $fieldBox.find('.field-options-container').first();
    const dataType = parseInt($selectElement.val());

    // Find existing data to pre-fill if available
    const fieldId = $fieldBox.data('id');
    const fieldData = managePCAType === 'edit' ? findFieldByIdRecursive(currentPCATemplateData.extraction.fields, fieldId) : {};

    $optionsContainer.empty(); // Clear previous options

    switch (dataType) {
        case PCA_EXTRACTION_FIELD_DATA_TYPE.String:
            const pattern = fieldData?.validation?.pattern || '';
            $optionsContainer.html(`<input type="text" class="form-control form-control-sm" data-type="pattern" placeholder="Optional: Regex Pattern" value="${pattern}">`);
            break;
        case PCA_EXTRACTION_FIELD_DATA_TYPE.Enum:
            const options = fieldData?.options?.join(', ') || '';
            $optionsContainer.html(`<input type="text" class="form-control form-control-sm" data-type="enum-options" placeholder="Comma-separated options (e.g., Low,Medium,High)" value="${options}">`);
            break;
        case PCA_EXTRACTION_FIELD_DATA_TYPE.Number:
            const min = fieldData?.validation?.min || '';
            const max = fieldData?.validation?.max || '';
            $optionsContainer.html(`
                <div class="d-flex align-items-center">
                    <input type="number" class="form-control form-control-sm me-2" data-type="min" placeholder="Min" value="${min}">
                    <input type="number" class="form-control form-control-sm" data-type="max" placeholder="Max" value="${max}">
                </div>`);
            break;
    }
}

// DYNAMIC ELEMENT CREATORS
function createPCAListElement(templateData) {
    return `
        <div class="col-lg-4 col-md-6 col-12">
            <div class="post-analysis-card d-flex flex-column align-items-start justify-content-center" data-template-id="${templateData.id}">
                 <div class="d-flex flex-row align-items-center justify-content-start mb-4">
                    <span class="route-icon">${templateData.general.emoji}</span>
                    <div class="card-data">
                        <h4>${templateData.general.name}</h4>
                    </div>
                </div>
                <div><h5 class="h5-info agent-description"><span>${templateData.general.description}</span></h5></div>
            </div>
        </div>`;
}
function createPCATagElement(tagData, level) {
    const data = tagData || {};
    const id = data.id || crypto.randomUUID();
    const isRequired = data.rules ? data.rules.isRequired : false;
    const allowMultiple = data.rules ? data.rules.allowMultiple : false;
    const subTags = data.subTags || [];
    return `
        <div class="p-3 border rounded mb-2 tag-box" data-id="${id}" data-level="${level}" style="background-color: ${(level == 0 ? '#1a1a1a' : `rgba(255,255,255,${level * 0.03});`)}">
            <div class="d-flex align-items-center mb-2">
                <input type="text" class="form-control me-2" placeholder="Tag Name" data-type="name" value="${data.name || ''}">
                <button class="btn btn-danger btn-sm" button-type="remove-item"><i class="fa-regular fa-trash"></i></button>
            </div>
            <textarea class="form-control form-control-sm mb-2" rows="2" placeholder="Description for AI..." data-type="description">${data.description || ''}</textarea>
            <div class="d-flex justify-content-between align-items-center">
                <div data-type="sub-tags-rules">
                    <small class="text-muted me-3">Rules for Sub-Tags:</small>
                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="checkbox" id="tag-required-${id}" data-type="isRequired" ${isRequired ? 'checked' : ''} ${subTags.length == 0 ? 'disabled' : ''}>
                        <label class="form-check-label" for="tag-required-${id}">Required</label>
                    </div>
                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="checkbox" id="tag-multiple-${id}" data-type="allowMultiple" ${allowMultiple ? 'checked' : ''} ${subTags.length == 0 ? 'disabled' : ''}>
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
    const isEmptyOrNullAllowed = data.isEmptyOrNullAllowed || false; // NEW
    const dataType = (data.dataType !== undefined) ? data.dataType : null;

    return `
        <div class="p-3 border rounded mb-3 extraction-field-box" data-id="${id}" data-level="${level}" style="background-color: ${(level == 0 ? '#1a1a1a' : `rgba(255,255,255,${level * 0.03});`)}">
            <div class="d-flex justify-content-between align-items-center mb-2">
                <h6 class="mb-0 text-muted">Field Definition</h6>
                <button class="btn btn-danger btn-sm" button-type="remove-item"><i class="fa-regular fa-trash"></i></button>
            </div>
            <div class="mb-3">
                <label for="field-key-${id}" class="form-label form-label-sm">Key Name</label>
                <input type="text" class="form-control form-control-sm" id="field-key-${id}" placeholder="e.g., customer_email" data-type="keyName" value="${data.keyName || ''}">
            </div>
            <div class="mb-3">
                <label for="field-desc-${id}" class="form-label form-label-sm">Description for AI</label>
                <input type="text" class="form-control form-control-sm" id="field-desc-${id}" placeholder="e.g., Extract the customer's primary contact email." data-type="description" value="${data.description || ''}">
            </div>
            <div>
                <label for="field-type-${id}" class="form-label form-label-sm">Data Type</label>
                <select class="form-select form-select-sm" id="field-type-${id}" data-type="DataType">
                    <option value="" disabled ${dataType === null ? 'selected' : ''}>Select Data Type</option>
                    <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.String}" ${dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.String ? 'selected' : ''}>String</option>
                    <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.Boolean}" ${dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Boolean ? 'selected' : ''}>Boolean</option>
                    <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.Number}" ${dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Number ? 'selected' : ''}>Number</option>
                    <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.DateTime}" ${dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.DateTime ? 'selected' : ''}>DateTime</option>
                    <option value="${PCA_EXTRACTION_FIELD_DATA_TYPE.Enum}" ${dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum ? 'selected' : ''}>Enum (Options)</option>
                </select>
            </div>
            <div class="mt-2 field-options-container"></div>
            <div class="d-flex mt-3">
                <div class="form-check form-check-inline">
                    <input class="form-check-input" type="checkbox" id="field-required-${id}" data-type="isRequired" ${isRequired ? 'checked' : ''}>
                    <label class="form-check-label" for="field-required-${id}">Required</label>
                </div>
                <div class="form-check form-check-inline">
                    <input class="form-check-input" type="checkbox" id="field-empty-allowed-${id}" data-type="isEmptyOrNullAllowed" ${isEmptyOrNullAllowed ? 'checked' : ''}>
                    <label class="form-check-label" for="field-empty-allowed-${id}">Empty/Null Allowed</label>
                </div>
            </div>
            <div>
                <label class="form-label mt-3 mb-0 d-block">Conditional Rules</label>
                <button class="btn btn-light btn-sm mt-1" button-type="add-conditional-rule">
                    <i class="fa-regular fa-plus"></i> Add Conditional Rule
                </button>
                <div class="mt-1 rules-container"></div>            
            </div>
        </div>`;
}
function createPCAConditionalRuleElement(ruleData, parentFieldData) {
    const data = ruleData || {};
    const id = data.id || crypto.randomUUID();
    const parentDataType = parentFieldData.dataType;

    let valueInputHtml = '<input type="text" class="form-control form-control-sm" data-type="conditionValue">';
    if (parentFieldData.dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Boolean)
    {
        valueInputHtml = `<select class="form-select form-select-sm" data-type="conditionValue"><option value="true">True</option><option value="false">False</option></select>`;
    }
    else if (parentFieldData.dataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Enum)
    {
        const optionsHtml = parentFieldData.options.map(opt => `<option value="${opt}">${opt}</option>`).join('');
        valueInputHtml =
            `<select class="form-select form-select-sm" data-type="conditionValue">
                <option value="" disabled selected>Select Option</option>
                ${optionsHtml}
            </select>
        `;
    }

    let operatorsHtml = `
        <option value="" disabled selected>Select Operator</option>
        <option value="${PCA_CONDITION_OPERATOR.Equals}">Equals</option>
        <option value="${PCA_CONDITION_OPERATOR.NotEquals}">Not Equals</option>`;
    if (parentDataType === PCA_EXTRACTION_FIELD_DATA_TYPE.String)
    {
        operatorsHtml += `<option value="${PCA_CONDITION_OPERATOR.Contains}">Contains</option>`;
    }
    if (parentDataType === PCA_EXTRACTION_FIELD_DATA_TYPE.Number || parentDataType === PCA_EXTRACTION_FIELD_DATA_TYPE.DateTime)
    {
        operatorsHtml += `
            <option value="${PCA_CONDITION_OPERATOR.GreaterThan}">Greater Than</option>
            <option value="${PCA_CONDITION_OPERATOR.GreaterThanOrEqual}">Greater Than Or Equal</option>
            <option value="${PCA_CONDITION_OPERATOR.LessThan}">Less Than</option>
            <option value="${PCA_CONDITION_OPERATOR.LessThanOrEqual}">Less Than Or Equal</option>`;
    }

    return `
        <div class="p-2 mt-2 border rounded rule-box" data-id="${id}">
            <div class="d-flex align-items-center justify-content-between">
                <div class="d-flex align-items-center">
                    <strong class="me-2">IF Value</strong>
                    <select class="form-select form-select-sm me-2" data-type="conditionOperator" style="width: auto;">
                        ${operatorsHtml}
                    </select>
                    <div style="width: 150px;">${valueInputHtml}</div>
                    <strong class="mx-2">THEN Extract:</strong>
                </div>
                <button class="btn btn-danger btn-sm" button-type="remove-item"><i class="fa-regular fa-trash"></i></button>
            </div>
            <div>
                <label class="form-label mt-3 mb-0 d-block">Dependent Fields</label>
                <button class="btn btn-light btn-sm mt-1" button-type="add-dependent-field"><i class="fa-regular fa-plus"></i> Add Field</button>
                <div class="mt-1 dependent-fields-container"></div>
            </div>
        </div>`;
}

// EVENT HANDLER INITIALIZERS
function initPCAManagerEventHandlers() {
    new EmojiPicker({ trigger: [{ selector: "#pca-icon-input", insertInto: "#pca-icon-input" }], closeOnInsert: true });

    // Universal change listener
    pcaManagerView.on('input change', 'input, select, textarea', () => {
        checkPCAChanges();
        validatePCATemplate(true);
    });

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

        checkPCAChanges();
        validatePCATemplate(true);
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

        checkPCAChanges();
        validatePCATemplate(true);
    });

    // Universal remove button
    pcaManagerView.on('click', '.extraction-field-box [button-type="remove-item"], .rule-box [button-type="remove-item"]', (e) => {
        $(e.currentTarget).closest('.extraction-field-box, .rule-box').remove();

        checkPCAChanges();
        validatePCATemplate(true);
    });

    // --- Tagging Specific Delegation ---
    pcaTagSetsList.on('click', '.tag-box [button-type="remove-item"]', (e) => {
        const $targetBox = $(e.target).closest('.tag-box');
        const parentTagBox = $targetBox.parent().parent();

        $targetBox.remove();

        const parentSubTagsRules = parentTagBox.find('[data-type="sub-tags-rules"]').first();
        const parentSubTagsContainer = parentTagBox.find('.sub-tags-container').first();

        if (parentSubTagsContainer.children().length === 0) {
            parentSubTagsRules.find('input[type="checkbox"]').prop('checked', false).prop('disabled', true);
        }

        checkPCAChanges();
        validatePCATemplate(true);
    });
    pcaTagSetsList.on('click', '[button-type="add-sub-tag"]', (e) => {
        const $parentBox = $(e.currentTarget).closest('.tag-box');
        const subTagRules = $parentBox.find('[data-type="sub-tags-rules"]').first();
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

        subTagRules.find('input[type="checkbox"]').prop('disabled', false);
        subTagContainer.append(createPCATagElement(null, newLevel));

        checkPCAChanges();
        validatePCATemplate(true);
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

        let dataType = $fieldBox.find('[data-type="DataType"]').first().find('option:selected').val();
        if (!dataType || dataType == null || dataType == '') {
            AlertManager.createAlert({
                type: 'warning',
                message: 'Select a data type before adding a conditional rule.',
                timeout: 3000
            });
            return;
        }

        let dataTypeEnum = parseInt(dataType);

        const parentFieldData = {
            dataType: dataTypeEnum,
            options: (dataTypeEnum == PCA_EXTRACTION_FIELD_DATA_TYPE.Enum)
                ? $fieldBox.find('.field-options-container input').first().val().split(',').map(s => s.trim()).filter(Boolean)
                : []
        };
        $fieldBox.find('.rules-container').first().append(createPCAConditionalRuleElement(null, parentFieldData));

        checkPCAChanges();
        validatePCATemplate(true);
    });

    pcaExtractionFieldsList.on('click', '[button-type="add-dependent-field"]', (e) => {
        const $ruleBox = $(e.currentTarget).closest('.rule-box');
        const dependentFieldsContainer = $ruleBox.find('.dependent-fields-container').first();
        const $parentFieldBox = $ruleBox.closest('.extraction-field-box').first();
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

        const totalSiblingFields = $parentFieldBox.find(`.extraction-field-box[data-level="${newLevel}"]`).first().length;

        if (totalSiblingFields >= MAX_PCA_FIELDS_PER_LEVEL) {
            AlertManager.createAlert({
                type: 'warning',
                message: `You can only add a maximum of ${MAX_PCA_FIELDS_PER_LEVEL} fields per level.`,
                timeout: 3000
            });
            return;
        }

        dependentFieldsContainer.append(createPCAExtractionFieldElement(null, newLevel));

        checkPCAChanges();
        validatePCATemplate(true);
    });

    pcaExtractionFieldsList.on('change', 'select[data-type="DataType"]', (e) => {
        const $select = $(e.currentTarget);
        const $fieldBox = $select.closest('.extraction-field-box');

        // 1. Render the options container for the parent field itself
        renderPCAFieldOptionsContainer($select);

        // 2. Alert the user and reset child rule conditions if any exist
        if ($fieldBox.find('.rules-container .rule-box').first().length > 0) {
            AlertManager.createAlert({
                type: 'info',
                message: 'Data type changed. Please re-configure your conditional rules.',
                timeout: 3000
            });
            resetPCARuleConditions($fieldBox);
        }
    });

    pcaExtractionFieldsList.on('change', '[data-type="enum-options"]', (e) => {
        const $input = $(e.currentTarget);
        const $fieldBox = $input.closest('.extraction-field-box');

        if ($fieldBox.find('.rules-container .rule-box').first().length > 0) {
            AlertManager.createAlert({
                type: 'info',
                message: 'Enum options changed. Please re-configure your conditional rules.',
                timeout: 3000
            });
            resetPCARuleConditions($fieldBox);
        }
    });
}

// MAIN INITIALIZER
function initPostAnalysisTab() {
    pcaConfigurationLLMIntegrationManager = new IntegrationConfigurationManager('#pca-llm-integration-selector', {
        integrationType: 'LLM',
        allIntegrations: BusinessFullData.businessApp.integrations,
        providersData: BusinessLLMProvidersForIntegrations,
        modalSelector: '#integrationConfigurationModal',
        onSaveSuccessful: () => {
            checkPCAChanges();
            validatePCATemplate(true);
        },
        onIntegrationChange: () => {
            checkPCAChanges();
            validatePCATemplate(true);
        },
    });

    // Event Handlers
    initPCAManagerEventHandlers();

    $(window).resize(() => {
        SetPostAnalysisCardDynamicWidth();
    });

    $(document).on("containerResizeProgress", (event) => {
        SetPostAnalysisCardDynamicWidth();
    })

    $(document).on("tabShowing", function (event, data) {
        if (data.tabId === 'post-analysis-tab') {
            handlePCARouting(data.urlSubPath);
        }
    });

    $(document).on("tabShown", function (event, data) {
        if (data.tabId === 'post-analysis-tab') {
            SetPostAnalysisCardDynamicWidth();
        }
    });

    addNewPCATemplateButton.on('click', (e) => {
        e.preventDefault();

        currentPCATemplateData = createDefaultPCATemplateObject();
        pcaManagerNameBreadcrumb.text("New Template");
        resetPCAManager();
        managePCAType = 'new';    
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

    pcaListContainer.on('click', '.post-analysis-card', (e) => {
        e.preventDefault();
        const templateId = $(e.currentTarget).attr('data-template-id');
        const templateData = BusinessFullData.businessApp.postAnalysis.find(t => t.id === templateId);
        if (!templateData) return;

        currentPCATemplateData = JSON.parse(JSON.stringify(templateData));
        pcaManagerNameBreadcrumb.text(currentPCATemplateData.general.name);
        resetPCAManager();
        fillPCAManager(currentPCATemplateData);
        managePCAType = 'edit';
        showPCAManagerView();
        updateUrlForTab(`postanalysis/${templateId}`);
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

                const existingIndex = BusinessFullData.businessApp.postAnalysis.findIndex(t => t.id === savedData.id);
                if (existingIndex > -1) {
                    BusinessFullData.businessApp.postAnalysis[existingIndex] = savedData;
                } else {
                    BusinessFullData.businessApp.postAnalysis.push(savedData);
                }

                fillPCAList(); // todo instead of this we should update/add the card directly

                isSavingPCATemplate = false;
                savePCATemplateButton.prop("disabled", true);
                savePCATemplateButtonSpinner.addClass("d-none");

                AlertManager.createAlert({
                    type: "success",
                    message: "Analysis template saved successfully.",
                    timeout: 3000
                });
                managePCAType = 'edit';
                pcaManagerNameBreadcrumb.text(savedData.general.name);
                updateUrlForTab(`postanalysis/${currentPCATemplateData.id}`);
            },
            (error, isUnsuccessful) => {
                isSavingPCATemplate = false;
                savePCATemplateButton.prop("disabled", false);
                savePCATemplateButtonSpinner.addClass("d-none");
                AlertManager.createAlert({
                    type: "danger",
                    message: "Failed to save template. Check console for details.",
                    timeout: 3000
                });
                console.error("Failed to save PCA template:", error);
            }
        );
    });

    // Initial Load
    fillPCAList();
}