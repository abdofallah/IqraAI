/** Global Variables **/
const KnowledgeBaseChunkingType = {
    General: 0,
    ParentChild: 1
};

const KnowledgeBaseChunkingParentChunkType = {
    Paragraph: 0,
    FullDoc: 1
};

const KnowledgeBaseRetrievalType = {
    VectorSearch: 0,
    FullTextSearch: 1,
    HybirdSearch: 2
};

const KnowledgeBaseHybridRetrievalMode = {
    WeightedScore: 0,
    RerankModel: 1
};

const KnowledgeBaseDocumentStatus = {
    Processing: 0,
    Ready: 1,
    Failed: 2
};

const RetrievalTypeDisplayMap = {
    0: "Vector Search",
    1: "Full-Text Search",
    2: "Hybrid Search"
};

const KnowledgeBaseDocumentChunkType = {
    General: 0,
    Parent: 1,
    Child: 2
}

/** Dynamic Variables **/
let ManageKnowledgeBaseType = null;
let ManageCurrentKnowledgeBaseData = null;

let ManageCurrentKnowledgeBaseDocuments = [];
let ManageCurrentDocumentData = null;

let currentAddedChunks = [];
let currentEditedChunks = [];
let currentDeletedChunks = [];

let editingChunkInfo = null;
let IsSavingKnowledgeBase = false;
let IsProcessingDocument = false;
let IsSavingChunks = false;
let IsDeletingKnowledgeBase = false;

// Integration Managers
let knowledgeBaseEmbeddingIntegrationManager = null;
let vectorRerankIntegrationManager = null;
let fulltextRerankIntegrationManager = null;
let hybridRerankIntegrationManager = null;


/** Element Variables **/
const knowledgeBaseTab = $("#knowledge-base-tab");

// -- Main Headers & Views --
const knowledgeBaseHeader = knowledgeBaseTab.find("#knowledge-base-header");
const knowledgeBaseListTab = knowledgeBaseTab.find("#knowledgeBaseListTab");
const knowledgeBaseManagerTab = knowledgeBaseTab.find("#knowledgeBaseManagerTab");

// -- List View Elements --
const addNewKnowledgeBaseButton = knowledgeBaseListTab.find("#addNewKnowledgeBaseButton");
const knowledgeBaseListContainer = knowledgeBaseListTab.find("#knowledgeBaseListTable");

// -- KB Manager Elements --
const knowledgeBaseManagerInnerHeader = knowledgeBaseHeader.find("#knowledge-base-manager-header-inner");
const currentKnowledgeBaseName = knowledgeBaseManagerInnerHeader.find("#currentKnowledgeBaseName");
const switchBackToKnowledgeBaseTabButton = knowledgeBaseManagerInnerHeader.find("#switchBackToKnowledgeBaseTab");
const saveKnowledgeBaseButton = knowledgeBaseManagerInnerHeader.find("#saveKnowledgeBaseButton");
const testRetrievalButton = knowledgeBaseManagerInnerHeader.find('#testRetrievalButton');
const knowledgeBaseManagerNavTab = knowledgeBaseManagerInnerHeader.find('#knowledge-base-manager-tab');
const knowledgeBaseManagerConfigTabButton = knowledgeBaseManagerNavTab.find('#knowledge-base-manager-configuration-tab');
const knowledgeBaseManagerDocumentsTabButton = knowledgeBaseManagerNavTab.find('#knowledge-base-manager-documents-tab');
const knowledgeBaseManagerGeneralPane = knowledgeBaseManagerTab.find('#knowledge-base-manager-general');
const knowledgeBaseManagerConfigurationPane = knowledgeBaseManagerTab.find('#knowledge-base-manager-configuration');


// -- General Pane
const editKnowledgeBaseIconInput = knowledgeBaseManagerTab.find("#editKnowledgeBaseIconInput");
const editKnowledgeBaseNameInput = knowledgeBaseManagerTab.find("#editKnowledgeBaseNameInput");
const editKnowledgeBaseDescriptionInput = knowledgeBaseManagerTab.find("#editKnowledgeBaseDescriptionInput");

const knowledgeBaseIconPicker = new EmojiPicker({
    trigger: [{ selector: "#editKnowledgeBaseIconInput", insertInto: "#editKnowledgeBaseIconInput" }],
    closeButton: true,
    closeOnInsert: true,
});


// -- Configuration Pane
// ---- Chunking
const knowledgebaseDocumentChunkingTypeSelect = knowledgeBaseManagerTab.find('#knowledgebaseDocumentChunkingTypeSelect');
const chunkingTypeBoxes = knowledgeBaseManagerTab.find('.knowledgebase-document-chunking-type-box');
const knowledgebaseDocumentRetrivalTypeSelect = knowledgeBaseManagerTab.find('#knowledgebaseDocumentRetrivalTypeSelect');
const retrievalTypeBoxes = knowledgeBaseManagerTab.find('.knowledgebase-document-retrival-type-box');

const generalChunkSettings = knowledgeBaseManagerTab.find('.knowledgebase-document-chunking-type-box[box-type="0"]');
const generalDelimiterInput = generalChunkSettings.find('#generalDelimiter');
const generalMaxLengthInput = generalChunkSettings.find('#generalMaxChunkLength');
const generalOverlapInput = generalChunkSettings.find('#generalChunkOverlap');
const generalReplaceConsecutiveCheck = generalChunkSettings.find('#generalReplaceConsecutive');
const generalDeleteUrlsCheck = generalChunkSettings.find('#generalDeleteUrls');

const parentChildChunkSettings = knowledgeBaseManagerTab.find('.knowledgebase-document-chunking-type-box[box-type="1"]');
const parentChunkParagraphRadio = knowledgeBaseManagerTab.find('#parentChunkParagraph');
const parentChunkFullDocRadio = knowledgeBaseManagerTab.find('#parentChunkFullDoc');
const parentChunkParagraphSettings = knowledgeBaseManagerTab.find('#parentChunkParagraphSettings');
const parentDelimiterInput = parentChunkParagraphSettings.find('#parentDelimiter');
const parentMaxLengthInput = parentChunkParagraphSettings.find('#parentMaxChunkLength');
const childDelimiterInput = parentChildChunkSettings.find('#childDelimiter');
const childMaxLengthInput = parentChildChunkSettings.find('#childMaxChunkLength');
const parentChildReplaceConsecutiveCheck = parentChildChunkSettings.find('#parentChildReplaceConsecutive');
const parentChildDeleteUrlsCheck = parentChildChunkSettings.find('#parentChildDeleteUrls');

// ---- Embedding
const knowledgeBaseEmbeddingIntegrationContainer = knowledgeBaseManagerTab.find('#knowledgeBaseEmbeddingIntegrationContainer');

// ---- Retrieval
const useVectorScoreThreshold = knowledgeBaseManagerTab.find('#useVectorScoreThreshold');
const vectorTopKInput = knowledgeBaseManagerTab.find('#vectorTopK');
const vectorScoreThresholdInput = knowledgeBaseManagerTab.find('#vectorScoreThreshold');
const vectorRerankModelSwitch = knowledgeBaseManagerTab.find('#vectorRerankModelSwitch');
const vectorRerankContainer = knowledgeBaseManagerTab.find('#vectorRerankContainer');

const fulltextTopKInput = knowledgeBaseManagerTab.find('#fulltextTopK');
const fulltextRerankModelSwitch = knowledgeBaseManagerTab.find('#fulltextRerankModelSwitch');
const fulltextRerankContainer = knowledgeBaseManagerTab.find('#fulltextRerankContainer');

const useHybirdScoreThreshold = knowledgeBaseManagerTab.find('#useHybirdScoreThreshold');
const hybridWeightedScoreRadio = knowledgeBaseManagerTab.find('#hybridWeightedScore');
const hybridRerankModelRadio = knowledgeBaseManagerTab.find('#hybridRerankModel');
const hybridWeightedScoreContainer = knowledgeBaseManagerTab.find('#hybridWeightedScoreContainer');
const hybridWeightSlider = knowledgeBaseManagerTab.find('#hybridWeightSlider');
const semanticWeightSpan = knowledgeBaseManagerTab.find('#semanticWeight');
const keywordWeightSpan = knowledgeBaseManagerTab.find('#keywordWeight');
const hybridRerankContainer = knowledgeBaseManagerTab.find('#hybridRerankContainer');
const hybridTopKInput = knowledgeBaseManagerTab.find('#hybridTopK');
const hybridScoreThresholdInput = knowledgeBaseManagerTab.find('#hybridScoreThreshold');

// -- Documents Pane
const uploadDocumentButton = knowledgeBaseManagerTab.find('#uploadDocumentButton');
const documentsTable = knowledgeBaseManagerTab.find('#documentsTable');

// -- Document Chunk Manager
const documentChunkManagerInnerHeader = knowledgeBaseHeader.find('#knowledge-base-document-chunk-manager-header-inner');
const backToKbManagerFromChunks = documentChunkManagerInnerHeader.find('#backToKbManagerFromChunks');
const currentChunksDocumentName = documentChunkManagerInnerHeader.find('#currentChunksDocumentName');
const saveChunksButton = documentChunkManagerInnerHeader.find('#saveChunksButton');
const documentChunkManagerTab = knowledgeBaseTab.find('#documentChunkManagerTab');
const addNewChunkButton = documentChunkManagerTab.find('#addNewChunkButton');
const chunkManagerSearchInput = documentChunkManagerTab.find('#chunkManagerSearchInput');
const chunksListContainer = documentChunkManagerTab.find('#chunksListContainer');

// -- Modals --
let documentSettingsModal = null;
const documentSettingsModalElement = $('#documentSettingsModal');
const documentUploadInput = documentSettingsModalElement.find('#documentUploadInput');
const modalChunkSettingsContainer = documentSettingsModalElement.find('#modalChunkSettingsContainer');
const saveAndProcessDocumentButton = documentSettingsModalElement.find('#saveAndProcessDocumentButton');

let editChunkModal = null;
const editChunkModalElement = $('#editChunkModal');
const editChunkModalLabel = editChunkModalElement.find('#editChunkModalLabel');
const editChunkTextarea = editChunkModalElement.find('#editChunkTextarea');
const editChunkCharCount = editChunkModalElement.find('#editChunkCharCount');
const saveChunkChangesButton = editChunkModalElement.find('#saveChunkChangesButton');

let testRetrievalModal = null;
const testRetrievalModalElement = $('#testRetrievalModal');
const runQueryButton = testRetrievalModalElement.find('#runQueryButton');
const retrievalQueryInput = testRetrievalModalElement.find('#retrievalQueryInput');
const retrievalResultsContainer = testRetrievalModalElement.find('#retrievalResultsContainer');


/** API FUNCTIONS **/
function SaveBusinessKnowledgeBase(formData, successCallback, errorCallback) {
    return $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/knowledgebase/save`,
        method: "POST",
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
        error: (error) => {
            errorCallback(error, false);
        },
    });
}
function DeleteBusinessKnowledgeBase(kbId, successCallback, errorCallback) {
    return $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/knowledgebase/${kbId}/delete`,
        method: "POST",
        success: (response) => {
            if (response.success) {
                successCallback(response);
            } else {
                errorCallback(response, true);
            }
        },
        error: (error) => {
            errorCallback(error, false);
        },
    });
}
function SaveAndProcessDocument(kbId, formData, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/knowledgebase/${kbId}/documents/upload`,
        method: "POST",
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
        error: (error) => {
            errorCallback(error, false);
        },
    });
}
function SaveDocumentChunks(kbId, docId, formData, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/knowledgebase/${kbId}/documents/${docId}/chunks/save`,
        method: "POST",
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
        error: (error) => {
            errorCallback(error, false);
        },
    });
}
function TestRetrievalQuery(kbId, formData, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/knowledgebase/${kbId}/retrieve`,
        method: "POST",
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
        error: (error) => {
            errorCallback(error, false);
        },
    });
}
function GetBusinessKnowledgeBaseDocuments(kbId, successCallback, errorCallback) {
    $.ajax({
        url: `/app/user/business/${CurrentBusinessId}/knowledgebase/${kbId}/documents`,
        method: "GET",
        success: (response) => {
            if (response.success) {
                successCallback(response);
            } else {
                errorCallback(response, true);
            }
        },
        error: (error) => {
            errorCallback(error, false);
        },
    });
}

/** Functions **/

// -- View Switching --
function showKnowledgeBaseListTab() {
    knowledgeBaseManagerTab.removeClass("show");
    knowledgeBaseHeader.removeClass("show");
    setTimeout(() => {
        knowledgeBaseManagerTab.addClass("d-none");
        knowledgeBaseHeader.addClass("d-none");

        knowledgeBaseListTab.removeClass("d-none");
        setTimeout(() => {
            knowledgeBaseListTab.addClass("show");
            setDynamicBodyHeight();
        }, 10);
    }, 300);
}
function showKnowledgeBaseManagerTab() {
    knowledgeBaseListTab.removeClass("show");
    documentChunkManagerInnerHeader.removeClass('show');
    documentChunkManagerTab.removeClass('show');
    setTimeout(() => {
        knowledgeBaseListTab.addClass("d-none");
        documentChunkManagerInnerHeader.addClass('d-none');
        documentChunkManagerTab.addClass('d-none');

        knowledgeBaseHeader.removeClass("d-none");
        knowledgeBaseManagerInnerHeader.removeClass("d-none");
        knowledgeBaseManagerTab.removeClass("d-none");
        setTimeout(() => {
            knowledgeBaseHeader.addClass("show");
            knowledgeBaseManagerInnerHeader.addClass("show");
            knowledgeBaseManagerTab.addClass("show");
            setDynamicBodyHeight();
        }, 10);
    }, 300);
}
function showDocumentChunkManagerTab() {
    knowledgeBaseManagerInnerHeader.removeClass("show");
    knowledgeBaseManagerTab.removeClass("show");
    setTimeout(() => {
        knowledgeBaseManagerInnerHeader.addClass("d-none");
        knowledgeBaseManagerTab.addClass("d-none");

        documentChunkManagerInnerHeader.removeClass('d-none');
        documentChunkManagerTab.removeClass('d-none');
        setTimeout(() => {
            documentChunkManagerInnerHeader.addClass('show');
            documentChunkManagerTab.addClass('show');
            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

// -- List Management --
function createKnowledgeBaseListElement(kbData) {
    const documents = kbData.documents;
    const retrievalModeName = RetrievalTypeDisplayMap[kbData.configuration.retrieval.type.value];

    const actionDropdownHtml = `
        <div class="dropdown action-dropdown dropdown-menu-end">
            <button class="btn action-button dropdown-toggle" type="button" data-bs-toggle="dropdown" data-bs-auto-close="true" aria-expanded="false">
                <i class="fa-solid fa-ellipsis"></i>
            </button>
            <ul class="dropdown-menu">
                <li>
                    <span class="dropdown-item text-danger" data-item-id="${kbData.id}" button-type="delete-knowledgebase">
                        <i class="fa-solid fa-trash me-2"></i>Delete
                    </span>
                </li>
            </ul>
        </div>
    `;

    // <h6>${documents.length} Document${documents.length === 1 ? "" : "s"}</h6>
    // <h6>Mode: ${retrievalModeName}</h6>

    return createIqraCardElement({
        id: kbData.id,
        type: 'knowledgebase',
        visualHtml: `<span>${kbData.general.emoji}</span>`,
        titleHtml: kbData.general.name,
        descriptionHtml: kbData.general.description,
        actionDropdownHtml: actionDropdownHtml,
    });
}
function fillKnowledgeBaseList() {
    const knowledgeBases = BusinessFullData.businessApp.knowledgeBases;

    knowledgeBaseListContainer.empty();
    if (knowledgeBases.length === 0) {
        knowledgeBaseListContainer.append('<div class="col-12"><h6 class="text-center mt-5">No knowledge bases added yet...</h6></div>');
    } else {
        knowledgeBases.forEach(kb => {
            knowledgeBaseListContainer.append($(createKnowledgeBaseListElement(kb)));
        });
    }
}

// -- Document & Chunk Management --
function createDocumentTableRow(docData) {
    let statusPill = '';
    switch (docData.status.value) {
        case KnowledgeBaseDocumentStatus.Ready:
            statusPill = `<span class="badge bg-success">Ready</span>`;
            break;
        case KnowledgeBaseDocumentStatus.Processing:
            statusPill = `<span class="badge bg-primary">Processing</span>`;
            break;
        case KnowledgeBaseDocumentStatus.Failed:
            statusPill = `<span class="badge bg-danger">Failed</span>`;
            break;
        default:
            statusPill = `<span class="badge bg-secondary">Unknown</span>`;
    }

    return `
        <tr doc-id="${docData.id}">
            <td>${docData.name}</td>
            <td>${statusPill}</td>
            <td>
                <div class="d-flex align-items-center">
                    <div class="form-check form-switch me-3">
                        <input class="form-check-input" type="checkbox" role="switch" button-type="toggleDocumentStatus" title="Enable/Disable Document" ${docData.enabled ? 'checked' : ''} ${docData.status.value == KnowledgeBaseDocumentStatus.Ready ? '' : 'disabled'}>
                    </div>
                    <button class="btn btn-info btn-sm me-2" button-type="viewDocumentChunks" title="View/Edit Chunks" ${docData.status.value == KnowledgeBaseDocumentStatus.Ready ? '' : 'disabled'}>
                        <i class="fa-regular fa-layer-group"></i>
                    </button>
                    <button class="btn btn-danger btn-sm" button-type="deleteDocument" title="Delete Document" ${docData.status.value != KnowledgeBaseDocumentStatus.Processing ? '' : 'disabled'}>
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </div>
            </td>
        </tr>
    `;
}
function fillDocumentsTable() {
    const docsTableBody = documentsTable.find("tbody");
    docsTableBody.empty();

    if (ManageCurrentKnowledgeBaseDocuments.length === 0) {
        docsTableBody.append('<tr tr-type="none-notice"><td colspan="3">No documents uploaded yet.</td></tr>');
    } else {
        ManageCurrentKnowledgeBaseDocuments.forEach(doc => {
            docsTableBody.append(createDocumentTableRow(doc));
        });
    }
}
function populateDocumentModalSettings() {
    const chunkingConfig = ManageCurrentKnowledgeBaseData.configuration.chunking;
    let settingsHtml = '';

    if (chunkingConfig.type.value === KnowledgeBaseChunkingType.General) {
        settingsHtml = `
            <div class="row">
                <div class="col-md-4">
                    <label class="form-label">Delimiter</label>
                    <input type="text" class="form-control" id="modalGeneralDelimiter" value="${chunkingConfig.delimiter}">
                </div>
                <div class="col-md-4">
                    <label class="form-label">Max length</label>
                    <input type="number" class="form-control" id="modalGeneralMaxLength" value="${chunkingConfig.maxLength}">
                </div>
                <div class="col-md-4">
                    <label class="form-label">Overlap</label>
                    <input type="number" class="form-control" id="modalGeneralOverlap" value="${chunkingConfig.overlap}">
                </div>
            </div>
        `;
    } else { // ParentChild
        settingsHtml = `
            <h6>Parent-chunk</h6>
             <div class="row mb-3">
                <div class="col-md-6">
                    <label class="form-label">Delimiter</label>
                    <input type="text" class="form-control" id="modalParentDelimiter" value="${chunkingConfig.parent.delimiter || ''}">
                </div>
                <div class="col-md-6">
                    <label class="form-label">Max length</label>
                    <input type="number" class="form-control" id="modalParentMaxLength" value="${chunkingConfig.parent.maxLength || ''}">
                </div>
            </div>
            <h6>Child-chunk</h6>
             <div class="row">
                <div class="col-md-6">
                    <label class="form-label">Delimiter</label>
                    <input type="text" class="form-control" id="modalChildDelimiter" value="${chunkingConfig.child.delimiter}">
                </div>
                <div class="col-md-6">
                    <label class="form-label">Max length</label>
                    <input type="number" class="form-control" id="modalChildMaxLength" value="${chunkingConfig.child.maxLength}">
                </div>
            </div>
        `;
    }
    modalChunkSettingsContainer.html(settingsHtml);
}

// NEW CHUNK DISPLAY FUNCTIONS
function createParentChunkCard(parentChunk) {
    const childPills = parentChunk.childrenIds.map(childId => {
        var child = ManageCurrentDocumentData.chunks.find(c => c.id == childId);
        if (!child || child == null) return '';

        var elementString = `
            <div class="d-inline-flex align-items-center me-2 mb-2 chunk-pill" data-parent-id="${parentChunk.id}" data-child-id="${child.id}">
                <button class="btn btn-sm btn-outline-secondary" button-type="edit-chunk" data-parent-id="${parentChunk.id}" data-child-id="${child.id}">${child.text.length > 128 ? (`${child.text.substring(0, 128)}...`) : child.text}</button>
                <button class="btn btn-sm btn-outline-danger ms-1" button-type="delete-chunk" data-parent-id="${parentChunk.id}" data-child-id="${child.id}"><i class="fa-regular fa-xmark"></i></button>
            </div>
        `;

        return elementString;
    }).join('');

    const cardId = `chunk-card-${parentChunk.id}`;

    return `
        <div class="chunk-card mb-3 p-3 border rounded" id="${cardId}" data-parent-id="${parentChunk.id}">
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <p class="mb-1 chunk-text">${parentChunk.text.length > 128 ? (`${parentChunk.text.substring(0, 128)}...`) : parentChunk.text}</p>
                    <small class="text-muted">ID: ${parentChunk.id} | <span class="char-count">${parentChunk.text.length}</span> characters</small>
                </div>
                <div class="btn-group">
                    <button class="btn btn-sm btn-light" button-type="edit-chunk" data-parent-id="${parentChunk.id}" title="Edit Parent Chunk"><i class="fa-regular fa-pen-to-square"></i></button>
                    <button class="btn btn-sm btn-info" button-type="add-child-chunk" data-parent-id="${parentChunk.id}" title="Add Child Chunk"><i class="fa-regular fa-plus"></i></button>
                    <button class="btn btn-sm btn-danger" button-type="delete-chunk" data-parent-id="${parentChunk.id}" title="Delete Parent & Children"><i class="fa-regular fa-trash"></i></button>
                </div>
            </div>
            <hr>
            <div class="child-chunk-header" data-bs-toggle="collapse" href="#collapse-${parentChunk.id}">
                <i class="fa-regular fa-chevron-down me-2"></i>
                <span>${parentChunk.childrenIds.length} CHILD CHUNK${parentChunk.childrenIds.length !== 1 ? 'S' : ''}</span>
            </div>
            <div class="collapse mt-2" id="collapse-${parentChunk.id}">
                <div class="child-chunk-pills">
                    ${childPills}
                </div>
            </div>
        </div>
    `;
}
function createGeneralChunkCard(chunk) {
    const cardId = `chunk-card-${chunk.id}`;
    return `
        <div class="chunk-card mb-3 p-3 border rounded" id="${cardId}" data-chunk-id="${chunk.id}">
            <div class="d-flex justify-content-between align-items-start">
                 <div>
                    <p class="mb-1 chunk-text">${chunk.text.length > 128 ? (`${chunk.text.substring(0, 128)}...`) : chunk.text}</p>
                    <small class="text-muted">ID: ${chunk.id} | <span class="char-count">${chunk.text.length}</span> characters</small>
                </div>
                <div class="btn-group">
                    <button class="btn btn-sm btn-info" button-type="edit-chunk" data-chunk-id="${chunk.id}" title="Edit Chunk"><i class="fa-regular fa-pen-to-square"></i></button>
                    <button class="btn btn-sm btn-danger" button-type="delete-chunk" data-chunk-id="${chunk.id}" title="Delete Chunk"><i class="fa-regular fa-trash"></i></button>
                </div>
            </div>
        </div>
    `;
}
function createRetrievalResultCard(result, index) {
    return `
        <div class="card mb-3">
            <div class="card-header d-flex justify-content-between">
                <strong>Result ${index + 1}</strong>
                <span class="badge bg-info">Score: ${result.score.toFixed(4)}</span>
            </div>
            <div class="card-body">
                <p class="card-text">${result.content}</p>
            </div>
        </div>
    `;
}
function fillChunksList() {
    chunksListContainer.empty();
    const chunks = ManageCurrentDocumentData.chunks || [];

    const chunkingType = ManageCurrentKnowledgeBaseData.configuration.chunking.type.value;

    if (chunks.length === 0) {
        chunksListContainer.html('<h6 class="text-center mt-5">No chunks found for this document.</h6>');
        return;
    }

    if (chunkingType === KnowledgeBaseChunkingType.ParentChild) {
        chunks.filter(c => c.type.value == KnowledgeBaseDocumentChunkType.Parent).forEach(parentChunk => {
            chunksListContainer.append(createParentChunkCard(parentChunk));
        });
    } else { // General Mode
        chunks.forEach(chunk => {
            chunksListContainer.append(createGeneralChunkCard(chunk));
        });
    }
}
function updateSaveChangesButtonState() {
    const hasChanges = currentAddedChunks.length > 0 || currentEditedChunks.length > 0 || currentDeletedChunks.length > 0;
    saveChunksButton.prop('disabled', !hasChanges);
}

// -- Manager & Data Handling --
function createDefaultKnowledgeBaseObject() {
    return {
        general: {
            emoji: "🧠",
            name: "",
            description: ""
        },
        configuration: {
            // Default to General Chunking
            chunking: {
                type: KnowledgeBaseChunkingType.General,
                delimiter: "\\n\\n",
                maxLength: 1024,
                overlap: 50,
                preprocess: {
                    replaceConsecutive: false,
                    deleteUrls: false
                }
            },
            // Default Embedding Integration
            embedding: {
                id: "",
                fieldValues: {}
            },
            // Default to Vector Retrieval
            retrieval: {
                type: KnowledgeBaseRetrievalType.VectorSearch,
                topK: 3,
                useScoreThreshold: false,
                scoreThreshold: null,
                rerank: {
                    enabled: false,
                    integration: null
                }
            }
        },
        documents: []
    };
}
function resetAndEmptyKnowledgeBaseManagerTab() {
    // Data Reset
    ManageKnowledgeBaseType = null;
    ManageCurrentKnowledgeBaseData = null;
    ManageCurrentKnowledgeBaseDocuments = [];
    ManageCurrentDocumentData = null;
    currentAddedChunks = [];
    currentEditedChunks = [];
    currentDeletedChunks = [];
    editingChunkInfo = null;
    IsSavingKnowledgeBase = false;
    IsProcessingDocument = false;
    IsSavingChunks = false;

    // General
    editKnowledgeBaseIconInput.text("🧠");
    editKnowledgeBaseNameInput.val("").removeClass('is-invalid');
    editKnowledgeBaseDescriptionInput.val("").removeClass('is-invalid');

    // Configuration - Re-enable everything for 'new'
    knowledgeBaseManagerConfigurationPane.find('input, select, button').prop('disabled', false);
    knowledgeBaseManagerConfigurationPane.removeClass('disabled-pane');

    // REWRITTEN: Default to enum values
    knowledgebaseDocumentChunkingTypeSelect.val(KnowledgeBaseChunkingType.General).trigger('change');
    knowledgebaseDocumentRetrivalTypeSelect.val(KnowledgeBaseRetrievalType.VectorSearch).trigger('change');

    // -- Integrations
    knowledgeBaseEmbeddingIntegrationManager.reset();
    vectorRerankIntegrationManager.reset();
    fulltextRerankIntegrationManager.reset();
    hybridRerankIntegrationManager.reset();

    // -- Retrieval
    useVectorScoreThreshold.prop('checked', false).trigger('change');
    useHybirdScoreThreshold.prop('checked', false).trigger('change');
    vectorRerankModelSwitch.prop('checked', false).trigger('change');
    fulltextRerankModelSwitch.prop('checked', false).trigger('change');
    hybridWeightedScoreRadio.prop('checked', true).trigger('change');
    hybridWeightSlider.val(0.7).trigger('input');

    // Documents
    fillDocumentsTable();

    // Tabs & Buttons
    $('#knowledge-base-manager-general-tab').click();
    knowledgeBaseManagerConfigTabButton.removeClass('disabled').prop('disabled', false);
    knowledgeBaseManagerDocumentsTabButton.addClass('disabled').prop('disabled', true);
    saveKnowledgeBaseButton.addClass('disabled').prop("disabled", true);
}
function fillKnowledgeBaseManagerTab() {
    const kbData = ManageCurrentKnowledgeBaseData;

    // General
    editKnowledgeBaseIconInput.text(kbData.general.emoji);
    editKnowledgeBaseNameInput.val(kbData.general.name);
    editKnowledgeBaseDescriptionInput.val(kbData.general.description);

    // -- Configuration: Surgical Disabling for 'edit' mode --
    knowledgebaseDocumentChunkingTypeSelect.prop('disabled', true);
    knowledgebaseDocumentRetrivalTypeSelect.prop('disabled', false);
    knowledgeBaseManagerConfigurationPane.removeClass('disabled-pane');

    // -- Set configuration based on loaded data --

    // Chunking
    const chunkingConfig = kbData.configuration.chunking;
    knowledgebaseDocumentChunkingTypeSelect.val(chunkingConfig.type.value).trigger('change');
    if (chunkingConfig.type.value === KnowledgeBaseChunkingType.General)
    {
        generalDelimiterInput.val(chunkingConfig.delimiter);
        generalMaxLengthInput.val(chunkingConfig.maxLength);
        generalOverlapInput.val(chunkingConfig.overlap);
        generalReplaceConsecutiveCheck.prop('checked', chunkingConfig.preprocess.replaceConsecutive);
        generalDeleteUrlsCheck.prop('checked', chunkingConfig.preprocess.deleteUrls);
    }
    else if (chunkingConfig.type.value === KnowledgeBaseChunkingType.ParentChild)
    {
        const parentType = chunkingConfig.parent.type.value === KnowledgeBaseChunkingParentChunkType.Paragraph ? 'paragraph' : 'full_doc';
        $(`input[name="parentChunkType"][value="${parentType}"]`).prop('checked', true).trigger('change');
        parentDelimiterInput.val(chunkingConfig.parent.delimiter);
        parentMaxLengthInput.val(chunkingConfig.parent.maxLength);
        childDelimiterInput.val(chunkingConfig.child.delimiter);
        childMaxLengthInput.val(chunkingConfig.child.maxLength);
        parentChildReplaceConsecutiveCheck.prop('checked', chunkingConfig.preprocess.replaceConsecutive);
        parentChildDeleteUrlsCheck.prop('checked', chunkingConfig.preprocess.deleteUrls);
    }

    // Embedding
    const embeddingConfig = kbData.configuration.embedding;
    knowledgeBaseEmbeddingIntegrationManager.load(embeddingConfig);
    knowledgeBaseEmbeddingIntegrationManager.disable();

    // Retrival
    const retrievalConfig = kbData.configuration.retrieval;
    knowledgebaseDocumentRetrivalTypeSelect.val(retrievalConfig.type.value).trigger('change');
    if (retrievalConfig.type.value === KnowledgeBaseRetrievalType.VectorSearch) {
        vectorTopKInput.val(retrievalConfig.topK);
        useVectorScoreThreshold.prop('checked', retrievalConfig.useScoreThreshold).trigger('change');
        vectorScoreThresholdInput.val(retrievalConfig.scoreThreshold);
        vectorRerankModelSwitch.prop('checked', retrievalConfig.rerank.enabled).trigger('change');

        if (retrievalConfig.rerank.enabled) {
            vectorRerankIntegrationManager.load(retrievalConfig.rerank.integration);
        }
    }
    else if (retrievalConfig.type.value === KnowledgeBaseRetrievalType.FullTextSearch) {
        fulltextTopKInput.val(retrievalConfig.topK);
        fulltextRerankModelSwitch.prop('checked', retrievalConfig.rerank.enabled).trigger('change');
        if (retrievalConfig.rerank.enabled) {
            fulltextRerankIntegrationManager.load(retrievalConfig.rerank.integration);
        }
    }
    else if (retrievalConfig.type.value === KnowledgeBaseRetrievalType.HybirdSearch) {
        hybridTopKInput.val(retrievalConfig.topK);
        useHybirdScoreThreshold.prop('checked', retrievalConfig.useScoreThreshold).trigger('change');
        hybridScoreThresholdInput.val(retrievalConfig.scoreThreshold);

        if (retrievalConfig.mode.value === KnowledgeBaseHybridRetrievalMode.RerankModel) {
            hybridRerankModelRadio.prop('checked', true).trigger('change');
            hybridRerankIntegrationManager.load(retrievalConfig.rerankIntegration);
        }
        else
        {
            hybridWeightedScoreRadio.prop('checked', true).trigger('change');
            hybridWeightSlider.val(retrievalConfig.weight).trigger('input');
        }
    }

    // Documents
    const docsTableBody = documentsTable.find("tbody");
    docsTableBody.empty();
    docsTableBody.append('<tr tr-type="none-notice"><td colspan="3">Loading...</td></tr>');

    uploadDocumentButton.prop('disabled', true);
    GetBusinessKnowledgeBaseDocuments(kbData.id,
        (successResponse) => {
            if (!successResponse.success) {
                AlertManager.createAlert({
                    type: "danger",
                    message: "Unable to retrieve knowledge base documents. Check console for details.",
                    timeout: 6000,
                });

                console.log("Unable to retrieve knowledge base documents.", successResponse);

                return;
            }

            ManageCurrentKnowledgeBaseDocuments = successResponse.data;
            fillDocumentsTable();

            uploadDocumentButton.prop('disabled', false);
        },
        (errorResponse) => {
            AlertManager.createAlert({
                type: "danger",
                message: "Failed to retrieve knowledge base documents. Check console for details.",
                timeout: 6000,
            });

            console.error("Failed to retrieve knowledge base documents.", errorResponse);

            docsTableBody.empty();
            docsTableBody.append('<tr tr-type="none-notice"><td colspan="3">Failed to retrieve knowledge base documents. Try refreshing the page.</td></tr>');

            uploadDocumentButton.prop('disabled', false);
        }
    );
    
    // Tabs & Buttons
    knowledgeBaseManagerDocumentsTabButton.removeClass('disabled').prop('disabled', false);
    saveKnowledgeBaseButton.addClass('disabled').prop("disabled", true);
}

// CHANGES FUNCTION
function checkKnowledgeBaseTabHasChanges(enableDisableButton = true) {
    if (ManageKnowledgeBaseType === null) return { hasChanges: false };

    const originalData = ManageCurrentKnowledgeBaseData;
    const currentData = {
        general: {
            emoji: editKnowledgeBaseIconInput.text(),
            name: editKnowledgeBaseNameInput.val().trim(),
            description: editKnowledgeBaseDescriptionInput.val().trim(),
        },
        configuration: {}
    };

    // --- Build Chunking Configuration ---
    const chunkingType = parseInt(knowledgebaseDocumentChunkingTypeSelect.val());
    if (chunkingType === KnowledgeBaseChunkingType.General) {
        currentData.configuration.chunking = {
            type: chunkingType,
            delimiter: generalDelimiterInput.val(),
            maxLength: parseInt(generalMaxLengthInput.val()),
            overlap: parseInt(generalOverlapInput.val()),
            preprocess: {
                replaceConsecutive: generalReplaceConsecutiveCheck.is(':checked'),
                deleteUrls: generalDeleteUrlsCheck.is(':checked'),
            }
        };
    } else { // ParentChild
        currentData.configuration.chunking = {
            type: chunkingType,
            parent: {
                type: $('input[name="parentChunkType"]:checked').val() === 'paragraph' ? KnowledgeBaseChunkingParentChunkType.Paragraph : KnowledgeBaseChunkingParentChunkType.FullDoc,
                delimiter: parentDelimiterInput.val() || null,
                maxLength: parseInt(parentMaxLengthInput.val()) || null,
            },
            child: {
                delimiter: childDelimiterInput.val(),
                maxLength: parseInt(childMaxLengthInput.val()),
            },
            preprocess: {
                replaceConsecutive: parentChildReplaceConsecutiveCheck.is(':checked'),
                deleteUrls: parentChildDeleteUrlsCheck.is(':checked'),
            }
        };
    }

    // --- Build Embedding Configuration ---
    // Embedding is only set on creation.
    currentData.configuration.embedding = ManageKnowledgeBaseType === 'new'
        ? knowledgeBaseEmbeddingIntegrationManager.getData()
        : originalData.configuration.embedding;

    // --- Build Retrieval Configuration ---
    const retrievalType = parseInt(knowledgebaseDocumentRetrivalTypeSelect.val());
    if (retrievalType === KnowledgeBaseRetrievalType.VectorSearch) {
        currentData.configuration.retrieval = {
            type: retrievalType,
            topK: parseInt(vectorTopKInput.val()),
            useScoreThreshold: useVectorScoreThreshold.is(':checked'),
            scoreThreshold: useVectorScoreThreshold.is(':checked') ? parseFloat(vectorScoreThresholdInput.val()) : null,
            rerank: {
                enabled: vectorRerankModelSwitch.is(':checked'),
                integration: vectorRerankModelSwitch.is(':checked') ? vectorRerankIntegrationManager.getData() : null
            }
        };
    } else if (retrievalType === KnowledgeBaseRetrievalType.FullTextSearch) {
        currentData.configuration.retrieval = {
            type: retrievalType,
            topK: parseInt(fulltextTopKInput.val()),
            rerank: {
                enabled: fulltextRerankModelSwitch.is(':checked'),
                integration: fulltextRerankModelSwitch.is(':checked') ? fulltextRerankIntegrationManager.getData() : null
            }
        };
    } else { // Hybrid Search
        const isRerankMode = hybridRerankModelRadio.is(':checked');
        currentData.configuration.retrieval = {
            type: retrievalType,
            mode: isRerankMode ? KnowledgeBaseHybridRetrievalMode.RerankModel : KnowledgeBaseHybridRetrievalMode.WeightedScore,
            weight: !isRerankMode ? parseFloat(hybridWeightSlider.val()) : null,
            rerankIntegration: isRerankMode ? hybridRerankIntegrationManager.getData() : null,
            topK: parseInt(hybridTopKInput.val()),
            useScoreThreshold: useHybirdScoreThreshold.is(':checked'),
            scoreThreshold: useHybirdScoreThreshold.is(':checked') ? parseFloat(hybridScoreThresholdInput.val()) : null,
        };
    }

    // Using a deep comparison library would be more robust, but JSON.stringify is a good approximation here.
    const hasChanges = JSON.stringify(currentData.general) !== JSON.stringify(originalData.general) ||
        JSON.stringify(currentData.configuration) !== JSON.stringify(originalData.configuration);

    if (enableDisableButton) {
        saveKnowledgeBaseButton.prop("disabled", !hasChanges);
        saveKnowledgeBaseButton.toggleClass('disabled', !hasChanges);
    }

    return {
        hasChanges: hasChanges,
        changes: currentData,
    };
}
function validateKnowledgeBaseTab(onlyRemove = false) {
    if (ManageKnowledgeBaseType === null) return { validated: true, errors: [] };

    const errors = [];
    let validated = true;

    // --- General Tab ---
    const knowledgeBaseName = editKnowledgeBaseNameInput.val();
    if (!knowledgeBaseName || knowledgeBaseName.length === 0 || knowledgeBaseName.trim().length === 0) {
        validated = false;
        errors.push("Knowledge Base name is required.");
        if (!onlyRemove) editKnowledgeBaseNameInput.addClass("is-invalid");
    } else {
        editKnowledgeBaseNameInput.removeClass("is-invalid");
    }

    const knowledgeBaseDescription = editKnowledgeBaseDescriptionInput.val();
    if (!knowledgeBaseDescription || knowledgeBaseDescription.length === 0 || knowledgeBaseDescription.trim().length === 0) {
        validated = false;
        errors.push("Knowledge Base description is required.");
        if (!onlyRemove) editKnowledgeBaseDescriptionInput.addClass("is-invalid");
    } else {
        editKnowledgeBaseDescriptionInput.removeClass("is-invalid");
    }

    // --- Configuration Tab ---

    // Default Chunking Settings
    const selectedChunkingType = parseInt(knowledgebaseDocumentChunkingTypeSelect.val());
    if (selectedChunkingType === KnowledgeBaseChunkingType.General) {
        const delimiter = generalDelimiterInput.val();
        if (!delimiter || delimiter.length === 0 || delimiter.trim().length === 0) {
            validated = false;
            errors.push("General chunk delimiter is required.");
            if (!onlyRemove) {
                generalDelimiterInput.addClass("is-invalid");
            }
        } else {
            generalDelimiterInput.removeClass("is-invalid");
        }

        const generalMaxLength = parseInt(generalMaxLengthInput.val());
        if (isNaN(generalMaxLength) || generalMaxLength < 1 || generalMaxLength > 4000) {
            validated = false;
            errors.push("General chunk max length is invalid. Must be between 1 and 4000.");
            if (!onlyRemove) {
                generalMaxLengthInput.addClass("is-invalid");
            }
        } else {
            generalMaxLengthInput.removeClass("is-invalid");
        }

        const chunkOverlap = parseInt(generalOverlapInput.val());
        if (isNaN(chunkOverlap) || chunkOverlap < 0 || chunkOverlap > generalMaxLength) {
            validated = false;
            errors.push("General chunk overlap is invalid. Must be between 0 and max length.");
            if (!onlyRemove) {
                generalOverlapInput.addClass("is-invalid");
            }
        } else {
            generalOverlapInput.removeClass("is-invalid");
        }
    }
    else if (selectedChunkingType === KnowledgeBaseChunkingType.ParentChild) {
        const parentChunkContextType = $('input[name="parentChunkType"]:checked').val();
        if (parentChunkContextType == "paragraph") {
            const parentDelimiter = parentDelimiterInput.val();
            if (!parentDelimiter) {
                validated = false;
                errors.push("Parent chunk delimiter is required.");
                if (!onlyRemove) {
                    parentDelimiterInput.addClass("is-invalid");
                }
            } else {
                parentDelimiterInput.removeClass("is-invalid");
            }

            const parentMaxLength = parseInt(parentMaxLengthInput.val());
            if (isNaN(parentMaxLength) || parentMaxLength < 1 || parentMaxLength > 4000) {
                validated = false;
                errors.push("Parent chunk max length is invalid. Must be between 1 and 4000.");
                if (!onlyRemove) {
                    parentMaxLengthInput.addClass("is-invalid");
                }
            } else {
                parentMaxLengthInput.removeClass("is-invalid");
            }
        }

        const childChunkDelimiter = childDelimiterInput.val();
        if (!childChunkDelimiter || childChunkDelimiter.length === 0 || childChunkDelimiter.trim().length === 0) {
            validated = false;
            errors.push("Child chunk delimiter is required.");
            if (!onlyRemove) {
                childDelimiterInput.addClass("is-invalid");
            }
        } else {
            childDelimiterInput.removeClass("is-invalid");
        }

        const childMaxLength = parseInt(childMaxLengthInput.val());
        if (isNaN(childMaxLength) || childMaxLength < 1 || childMaxLength > 4000) {
            validated = false;
            errors.push("Child chunk max length is invalid. Must be between 1 and 4000.");
            if (!onlyRemove) {
                childMaxLengthInput.addClass("is-invalid");
            }
        } else {
            childMaxLengthInput.removeClass("is-invalid");
        }
    }

    // Embedding Model
    if (ManageKnowledgeBaseType === 'new') {
        const embeddingSelect = knowledgeBaseEmbeddingIntegrationManager.getSelectElements();
        const embeddingData = knowledgeBaseEmbeddingIntegrationManager.getData();
        if (!embeddingData || !embeddingData.id) {
            validated = false;
            errors.push("Integration for embedding model must be selected.");
            if (!onlyRemove) {
                embeddingSelect.addClass('is-invalid');
            }
        }
        else {
            embeddingSelect.removeClass('is-invalid');

            const embeddingValidation = knowledgeBaseEmbeddingIntegrationManager.validate();
            if (!embeddingValidation.isValid) {
                validated = false;
                errors.push(...embeddingValidation.errors.map(e => `Embedding Model: ${e}`));
                if (!onlyRemove) {
                    embeddingSelect.addClass('is-invalid');
                }
            } else {
                embeddingSelect.removeClass('is-invalid');
            }
        }     
    }

    // Retrieval Settings
    const retrievalType = parseInt(knowledgebaseDocumentRetrivalTypeSelect.val());
    if (retrievalType === KnowledgeBaseRetrievalType.VectorSearch) {
        const vectorTopK = parseInt(vectorTopKInput.val());
        if (isNaN(vectorTopK) || vectorTopK <= 1) {
            validated = false;
            errors.push("Vector top K is invalid.");
            if (!onlyRemove) {
                vectorTopKInput.addClass("is-invalid");
            }
        } else {
            vectorTopKInput.removeClass("is-invalid");
        }

        const vectorRerankEnabled = vectorRerankModelSwitch.is(':checked');
        if (vectorRerankEnabled) {
            const vectorRerankSelect = vectorRerankIntegrationManager.getSelectElements();
            const vectorRerankData = vectorRerankIntegrationManager.getData();
            if (!vectorRerankData || !vectorRerankData.id) {
                validated = false;
                errors.push("Integration for vector rerank must be selected.");
                if (!onlyRemove) {
                    vectorRerankSelect.addClass('is-invalid');
                }
            }
            else {
                vectorRerankSelect.removeClass('is-invalid');

                const vectorRerankValidation = vectorRerankIntegrationManager.validate();
                if (!vectorRerankValidation.isValid) {
                    validated = false;
                    errors.push(...vectorRerankValidation.errors.map(e => `Vector Rerank: ${e}`));
                    if (!onlyRemove) {
                        vectorRerankSelect.addClass('is-invalid');
                    }
                } else {
                    vectorRerankSelect.removeClass('is-invalid');
                }
            }
        }
    }
    else if (retrievalType === KnowledgeBaseRetrievalType.FullTextSearch) {
        const fulltextTopK = parseInt(fulltextTopKInput.val());
        if (isNaN(fulltextTopK) || fulltextTopK <= 1) {
            validated = false;
            errors.push("Full-Text top K is invalid.");
            if (!onlyRemove) {
                fulltextTopKInput.addClass("is-invalid");
            }
        } else {
            fulltextTopKInput.removeClass("is-invalid");
        }

        const fulltextRerankEnabled = fulltextRerankModelSwitch.is(':checked');
        if (fulltextRerankEnabled) {
            const fulltextRerankSelect = fulltextRerankIntegrationManager.getSelectElements();
            const fulltextRerankData = fulltextRerankIntegrationManager.getData();
            if (!fulltextRerankData || !fulltextRerankData.id) {
                validated = false;
                errors.push("Integration for full-text rerank must be selected.");
                if (!onlyRemove) {
                    fulltextRerankSelect.addClass('is-invalid');
                }
            }
            else {
                fulltextRerankSelect.removeClass('is-invalid');

                const fulltextRerankValidation = fulltextRerankIntegrationManager.validate();
                if (!fulltextRerankValidation.isValid) {
                    validated = false;
                    errors.push(...fulltextRerankValidation.errors.map(e => `Full-Text Rerank: ${e}`));
                    if (!onlyRemove) {
                        fulltextRerankSelect.addClass('is-invalid');
                    }
                } else {
                    fulltextRerankSelect.removeClass('is-invalid');
                }
            }  
        }
    }
    else if (retrievalType === KnowledgeBaseRetrievalType.HybirdSearch) {
        const hybirdSearchMode = $('input[name="hybridMode"]:checked').val();
        if (hybirdSearchMode == "rerank_model") {
            const hybridRerankSelect = hybridRerankIntegrationManager.getSelectElements();
            const hybridRerankData = hybridRerankIntegrationManager.getData();
            if (!hybridRerankData || !hybridRerankData.id) {
                validated = false;
                errors.push("Integration for hybrid rerank must be selected.");
                if (!onlyRemove) {
                    hybridRerankSelect.addClass('is-invalid');
                }
            }
            else {
                hybridRerankSelect.removeClass('is-invalid');
                
                const hybridRerankValidation = hybridRerankIntegrationManager.validate();
                if (!hybridRerankValidation.isValid) {
                    validated = false;
                    errors.push(...hybridRerankValidation.errors.map(e => `Hybrid Rerank: ${e}`));
                    if (!onlyRemove) {
                        hybridRerankSelect.addClass('is-invalid');
                    }
                } else {
                    hybridRerankSelect.removeClass('is-invalid');
                }
            }
        }

        const hybridTopK = parseInt(hybridTopKInput.val());
        if (isNaN(hybridTopK) || hybridTopK <= 1) {
            validated = false;
            errors.push("Hybrid top K is invalid.");
            if (!onlyRemove) {
                hybridTopKInput.addClass("is-invalid");
            }
        } else {
            hybridTopKInput.removeClass("is-invalid");
        }
    }

    return {
        validated: validated,
        errors: errors,
    };
}
async function canLeaveKnowledgeBaseTab(leaveMessage = "") {
    if (IsSavingKnowledgeBase) {
        AlertManager.createAlert({
            type: "warning",
            message: "Knowledge Base is currently being saved. Please wait.",
            timeout: 6000
        });
        return false;
    }

    const { hasChanges } = checkKnowledgeBaseTabHasChanges(false);
    if (hasChanges) {
        const confirmDialog = new BootstrapConfirmDialog({
            title: "Unsaved Changes Pending",
            message: `You have unsaved changes.${leaveMessage}`,
            confirmText: "Discard",
            cancelText: "Cancel",
            confirmButtonClass: "btn-danger"
        });
        return await confirmDialog.show();
    }
    return true;
}
async function canLeaveChunkManager() {
    const hasChanges = currentAddedChunks.length > 0 || currentEditedChunks.length > 0 || currentDeletedChunks.length > 0;
    if (hasChanges) {
        const confirmDialog = new BootstrapConfirmDialog({
            title: "Unsaved Chunk Changes",
            message: "You have unsaved changes in the document chunks. Are you sure you want to discard them?",
            confirmText: "Discard",
            confirmButtonClass: 'btn-danger'
        });
        return await confirmDialog.show();
    }
    return true;
}

// Event Handlers Setup
function initKnowledgeBaseTab() {
    $(document).ready(() => {
        // Modal Initialization
        documentSettingsModal = new bootstrap.Modal(documentSettingsModalElement[0]);
        editChunkModal = new bootstrap.Modal(editChunkModalElement[0]);
        testRetrievalModal = new bootstrap.Modal(testRetrievalModalElement[0]);

        // Integration Manager Initializations
        knowledgeBaseEmbeddingIntegrationManager = new IntegrationConfigurationManager('#knowledgeBaseEmbeddingIntegrationContainer', {
            integrationType: 'Embedding',
            allowMultiple: false,
            isLanguageBound: false,
            allIntegrations: BusinessFullData.businessApp.integrations,
            providersData: BusinessEmbeddingProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onIntegrationChange: () => { handleInputChange() },
        });
        vectorRerankIntegrationManager = new IntegrationConfigurationManager('#vectorRerankContainer', {
            integrationType: 'Rerank',
            allowMultiple: false,
            isLanguageBound: false,
            allIntegrations: BusinessFullData.businessApp.integrations,
            providersData: BusinessRerankProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onIntegrationChange: () => { handleInputChange() },
        });
        fulltextRerankIntegrationManager = new IntegrationConfigurationManager('#fulltextRerankContainer', {
            integrationType: 'Rerank',
            allowMultiple: false,
            isLanguageBound: false,
            allIntegrations: BusinessFullData.businessApp.integrations,
            providersData: BusinessRerankProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onIntegrationChange: () => { handleInputChange() },
        });
        hybridRerankIntegrationManager = new IntegrationConfigurationManager('#hybridRerankContainer', {
            integrationType: 'Rerank',
            allowMultiple: false,
            isLanguageBound: false,
            allIntegrations: BusinessFullData.businessApp.integrations,
            providersData: BusinessRerankProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onIntegrationChange: () => { handleInputChange() },
        });

        // -- Event Handlers --

        // View Switching & Navigation
        addNewKnowledgeBaseButton.on("click", (event) => {
            event.preventDefault();

            resetAndEmptyKnowledgeBaseManagerTab();

            ManageCurrentKnowledgeBaseData = createDefaultKnowledgeBaseObject();
            currentKnowledgeBaseName.text("New Knowledge Base");

            showKnowledgeBaseManagerTab();
            ManageKnowledgeBaseType = "new";
        });

        switchBackToKnowledgeBaseTabButton.on('click', async (event) => {
            event.preventDefault();
            if (await canLeaveKnowledgeBaseTab(" Return to the list?")) {
                ManageKnowledgeBaseType = null;
                showKnowledgeBaseListTab();
            }
        });

        knowledgeBaseListContainer.on('click', '.knowledgebase-card', (event) => {
            event.preventDefault();
            event.stopPropagation();

            // check if target was button or its icon
            if ($(event.target).closest(".dropdown").length != 0) {
                return;
            }

            resetAndEmptyKnowledgeBaseManagerTab();

            const kbId = $(event.currentTarget).attr('data-item-id');

            ManageCurrentKnowledgeBaseData = BusinessFullData.businessApp.knowledgeBases.find(kb => kb.id === kbId);

            currentKnowledgeBaseName.text(ManageCurrentKnowledgeBaseData.general.name);
            
            fillKnowledgeBaseManagerTab();

            showKnowledgeBaseManagerTab();

            ManageKnowledgeBaseType = "edit";
        });

        knowledgeBaseListContainer.on("click", ".knowledgebase-card span[button-type='delete-knowledgebase']", async (event) => {
            event.preventDefault();

            const button = $(event.currentTarget);
            const knowledgeBaseId = button.attr("data-item-id");
            const knowledgeBaseIndex = BusinessFullData.businessApp.knowledgeBases.findIndex(n => n.id === knowledgeBaseId);
            if (knowledgeBaseIndex === -1) return;
            const knowledgeBaseData = BusinessFullData.businessApp.knowledgeBases[knowledgeBaseIndex];
            if (!knowledgeBaseData) return;
            const knowledgeBaseCard = knowledgeBaseListContainer.find(`.knowledgebase-card[data-item-id="${knowledgeBaseId}"]`);

            if (IsDeletingKnowledgeBase) {
                AlertManager.createAlert({
                    type: "warning",
                    message: `A delete operation for knowledgebases is already in progress. Please try again once the operation is complete.`,
                    timeout: 6000,
                });
                return;
            }

            const confirmDialog = new BootstrapConfirmDialog({
                title: `Delete "${knowledgeBaseData.general.name}" Knowledgebase`,
                message: `Are you sure you want to delete this knowledgebase?<br><br><b>Note:</b> You must remove any references to this knowledgebase (agent knowledgebase group) and wait or cancel any ongoing call queues or conversations.`,
                confirmText: "Delete",
                confirmButtonClass: "btn-danger",
                modalClass: "modal-lg"
            });

            if (await confirmDialog.show()) {
                showHideButtonSpinnerWithDisableEnable(button, true);
                IsDeletingKnowledgeBase = true;
                knowledgeBaseCard.addClass("disabled");

                DeleteBusinessKnowledgeBase(
                    knowledgeBaseId,
                    () => {

                        BusinessFullData.businessApp.knowledgeBases.splice(knowledgeBaseIndex, 1);

                        knowledgeBaseCard.parent().remove();

                        if (BusinessFullData.businessApp.knowledgeBases.length === 0) {
                            knowledgeBaseListContainer.append('<div class="col-12 none-knowledgebases-list-notice"><h6 class="text-center mt-5">No knowledgebases added yet...</h6></div>');
                        }

                        AlertManager.createAlert({
                            type: "success",
                            message: `Knowledgebase "${knowledgeBaseData.general.name[BusinessDefaultLanguage]}" deleted successfully.`,
                            timeout: 6000,
                        });
                    },
                    (errorResult) => {
                        knowledgeBaseCard.removeClass("disabled");

                        var resultMessage = "Check console logs for more details.";
                        if (errorResult && errorResult.message) resultMessage = errorResult.message;

                        AlertManager.createAlert({
                            type: "danger",
                            message: "Error occured while deleting business knowledgebase.",
                            resultMessage: resultMessage,
                            timeout: 6000,
                        });

                        console.log("Error occured while deleting business knowledgebase: ", errorResult);
                    }
                ).always(() => {
                    showHideButtonSpinnerWithDisableEnable(button, false);
                    IsDeletingKnowledgeBase = false;
                });
            }
        });

        knowledgeBaseManagerNavTab.on('show.bs.tab', 'button', function (e) {
            const targetId = $(e.target).attr('id');
            if (targetId === 'knowledge-base-manager-documents-tab') {
                saveKnowledgeBaseButton.addClass('d-none');
                testRetrievalButton.removeClass('d-none');
            } else {
                saveKnowledgeBaseButton.removeClass('d-none');
                testRetrievalButton.addClass('d-none');
            }
        });

        backToKbManagerFromChunks.on('click', async (e) => {
            e.preventDefault();
            if (!(await canLeaveChunkManager())) {
                return; // User cancelled
            }
            showKnowledgeBaseManagerTab();
        });


        // Manager Interactivity
        const handleInputChange = () => {
            if (ManageKnowledgeBaseType === null) return;
            checkKnowledgeBaseTabHasChanges();
            validateKnowledgeBaseTab(true);
        };
        knowledgeBaseManagerGeneralPane.on('input', 'input', handleInputChange);
        editKnowledgeBaseIconInput.on("emojiSelected", handleInputChange);
        knowledgeBaseManagerConfigurationPane.on('input change', 'input, select', handleInputChange);


        // Chunking Settings
        knowledgebaseDocumentChunkingTypeSelect.on('change', function () {
            const selectedType = $(this).val();
            chunkingTypeBoxes.addClass('d-none');
            chunkingTypeBoxes.filter(`[box-type="${selectedType}"]`).removeClass('d-none');
        });

        $('input[name="parentChunkType"]').on('change', function () {
            if ($(this).val() === 'paragraph') {
                parentChunkParagraphSettings.removeClass('d-none');
            } else {
                parentChunkParagraphSettings.addClass('d-none');
            }
        });

        // Retrieval Settings
        knowledgebaseDocumentRetrivalTypeSelect.on('change', function () {
            const selectedType = $(this).val();
            retrievalTypeBoxes.addClass('d-none');
            retrievalTypeBoxes.filter(`[box-type="${selectedType}"]`).removeClass('d-none');
        });

        vectorRerankModelSwitch.on('change', function () {
            vectorRerankContainer.toggleClass('d-none', !this.checked);
            handleInputChange();
        });

        fulltextRerankModelSwitch.on('change', function () {
            fulltextRerankContainer.toggleClass('d-none', !this.checked);
            handleInputChange();
        });

        $('input[name="hybridMode"]').on('change', function () {
            hybridWeightedScoreContainer.toggleClass('d-none', $(this).val() !== 'weighted_score');
            hybridRerankContainer.toggleClass('d-none', $(this).val() !== 'rerank_model');
            handleInputChange();
        });

        useVectorScoreThreshold.on('change', function () {
            vectorScoreThresholdInput.prop('disabled', !$(this).is(':checked'));
        });

        useHybirdScoreThreshold.on('change', function () {
            hybridScoreThresholdInput.prop('disabled', !$(this).is(':checked'));
        });

        hybridWeightSlider.on('input', function () {
            const semanticWeight = parseFloat($(this).val()).toFixed(1);
            const keywordWeight = (1.0 - semanticWeight).toFixed(1);
            semanticWeightSpan.text(semanticWeight);
            keywordWeightSpan.text(keywordWeight);
        });

        // Document Management Events
        uploadDocumentButton.on('click', (event) => {
            event.preventDefault();
            ManageCurrentDocumentData = null;
            documentUploadInput.val('').removeClass('is-invalid');
            documentSettingsModalElement.find('.modal-title').text('Upload Document');
            populateDocumentModalSettings();
            documentSettingsModal.show();
        });

        saveAndProcessDocumentButton.on('click', (event) => {
            event.preventDefault();
            if (IsProcessingDocument) return;

            const file = documentUploadInput[0].files[0];
            if (!file) {
                documentUploadInput.addClass('is-invalid');
                AlertManager.createAlert({ type: 'warning', message: 'Please select a file to upload.' });
                return;
            }
            documentUploadInput.removeClass('is-invalid');

            IsProcessingDocument = true;
            const spinner = saveAndProcessDocumentButton.find('.spinner-border');
            spinner.removeClass('d-none');
            saveAndProcessDocumentButton.prop('disabled', true);

            const formData = new FormData();
            formData.append('file', file);
            formData.append('knowledgeBaseId', ManageCurrentKnowledgeBaseData.id);

            SaveAndProcessDocument(ManageCurrentKnowledgeBaseData.id, formData,
                (response) => {
                    if (!response.success) {
                        spinner.addClass('d-none');
                        saveAndProcessDocumentButton.prop('disabled', false);
                        IsProcessingDocument = false;

                        AlertManager.createAlert({
                            type: 'danger',
                            message: 'Unable to add document for processing. Check console logs for more details.',
                        });
                        console.log("Unable to add document for processing.", response);

                        return;
                    }

                    const newDoc = response.data;
                    ManageCurrentKnowledgeBaseDocuments.push(newDoc); 
                    ManageCurrentKnowledgeBaseData.documents.push(newDoc.id); 

                    fillDocumentsTable();

                    spinner.addClass('d-none');
                    saveAndProcessDocumentButton.prop('disabled', false);
                    IsProcessingDocument = false;
                    documentSettingsModal.hide();

                    AlertManager.createAlert({
                        type: 'success',
                        message: 'Document added for processing. Refresh and check the status.',
                        timeout: 6000
                    });
                },
                (error) => {
                    spinner.addClass('d-none');
                    saveAndProcessDocumentButton.prop('disabled', false);
                    IsProcessingDocument = false;

                    var resultMessage = "Check console logs for more details.";
                    if (error && error.message) resultMessage = error.message;

                    AlertManager.createAlert({
                        type: 'danger',
                        message: 'Failed to add document for processing. Check console logs for more details.',
                        resultMessage: resultMessage,
                        timeout: 6000
                    });

                    console.log("Failed to add document for processing.", error);
            });
        });

        testRetrievalButton.on('click', (e) => {
            e.preventDefault();
            retrievalQueryInput.val('');
            retrievalResultsContainer.html('<p class="text-muted">Run a query to see results here.</p>');
            testRetrievalModal.show();
        });

        runQueryButton.on('click', (e) => {
            e.preventDefault();
            const query = retrievalQueryInput.val();
            if (!query.trim()) {
                AlertManager.createAlert({ type: 'warning', message: 'Please enter a query.', timeout: 6000 });
                return;
            }

            const formData = new FormData();
            formData.append('query', query);

            const spinner = runQueryButton.find('.spinner-border');
            spinner.removeClass('d-none');
            runQueryButton.prop('disabled', true);

            TestRetrievalQuery(ManageCurrentKnowledgeBaseData.id, formData,
                (response) => {
                    retrievalResultsContainer.empty();

                    if (!response.success) {
                        AlertManager.createAlert({
                            type: 'danger',
                            message: 'Unable to fetch results. Check console logs for more details.',
                            timeout: 6000
                        });

                        console.log("Unable to fetch results.", response);
                    }
                    else {
                        if (response.data.sources.length === 0) {
                            retrievalResultsContainer.html('<p class="text-muted">No results found for your query.</p>');
                        } else {
                            response.data.sources.forEach((result, index) => {
                                retrievalResultsContainer.append(createRetrievalResultCard(result, index));
                            });
                        }
                    }    

                    spinner.addClass('d-none');
                    runQueryButton.prop('disabled', false);
                },
                (error) => {
                    retrievalResultsContainer.html('<p class="text-danger">An error occurred while fetching results.</p>');
                    spinner.addClass('d-none');
                    runQueryButton.prop('disabled', false);
            });
        });

        documentsTable.on('click', '[button-type="deleteDocument"]', async function (e) {
            e.preventDefault();
            const docRow = $(this).closest('tr');
            const docId = docRow.attr('doc-id');
            const doc = ManageCurrentKnowledgeBaseData.documents.find(d => d.id === docId);

            const confirmDialog = new BootstrapConfirmDialog({
                title: 'Confirm Deletion',
                message: `Are you sure you want to delete the document "${doc.name}"? This action cannot be undone.`,
                confirmText: 'Delete',
                cancelText: 'Cancel',
                confirmButtonClass: 'btn-danger'
            });

            if (await confirmDialog.show()) {
                ManageCurrentKnowledgeBaseDocuments = ManageCurrentKnowledgeBaseDocuments.filter(d => d.id.toString() !== docId);
                ManageCurrentKnowledgeBaseData.documents = ManageCurrentKnowledgeBaseData.documents.filter(id => id.toString() !== docId);

                fillDocumentsTable();
                AlertManager.createAlert({ type: 'success', message: 'Document deleted successfully.' });
            }
        });

        documentsTable.on('change', '[button-type="toggleDocumentStatus"]', function (e) {
            const docId = $(this).closest('tr').attr('doc-id');
            const isEnabled = $(this).is(':checked');
            const doc = ManageCurrentKnowledgeBaseData.documents.find(d => d.id === docId);
            if (doc) {
                doc.enabled = isEnabled;
                AlertManager.createAlert({
                    type: 'info',
                    message: `Document "${doc.name}" status updated.`,
                    timeout: 3000
                });
            }
        });

        documentsTable.on('click', '[button-type="viewDocumentChunks"]', function (e) {
            e.preventDefault();
            const docId = $(this).closest('tr').attr('doc-id');
            ManageCurrentDocumentData = ManageCurrentKnowledgeBaseDocuments.find(d => d.id.toString() === docId);

            // Reset change tracking for the new session
            currentAddedChunks = [];
            currentEditedChunks = [];
            currentDeletedChunks = [];

            backToKbManagerFromChunks.text(ManageCurrentKnowledgeBaseData.general.name);
            currentChunksDocumentName.text(ManageCurrentDocumentData.name);

            fillChunksList();
            updateSaveChangesButtonState(); // Ensure it's disabled on load
            showDocumentChunkManagerTab();
        });


        // Chunk Management Events
        addNewChunkButton.on('click', (e) => {
            e.preventDefault();
            const mode = ManageCurrentKnowledgeBaseData.configuration.chunking.type.value;
            const type = mode === KnowledgeBaseChunkingType.General ? 'general' : 'parent';

            editingChunkInfo = { mode: 'add', type: type };
            editChunkModalLabel.text(type === 'general' ? 'Add New Chunk' : 'Add New Parent Chunk');
            editChunkTextarea.val('').trigger('input');
            editChunkModal.show();
        });

        chunkManagerSearchInput.on('keyup', (e) => {
            const searchTerm = $(e.target).val().toLowerCase();
            chunksListContainer.find('.chunk-card').each(function () {
                const card = $(this);
                const cardText = card.find('.chunk-text').text().toLowerCase();
                if (cardText.includes(searchTerm)) {
                    card.show();
                } else {
                    card.hide();
                }
            });
        });

        chunksListContainer.on('click', 'button[button-type="edit-chunk"]', function (e) {
            e.preventDefault();
            e.stopPropagation();
            const parentId = $(this).data('parent-id');
            const childId = $(this).data('child-id');
            const chunkId = $(this).data('chunk-id');

            let chunkToEdit;
            if (childId) {
                editingChunkInfo = { mode: 'edit', type: 'child', chunkId: childId, parentId: parentId };
                chunkToEdit = ManageCurrentDocumentData.chunks.find(c => (c.id === childId && c.parentId === parentId));
            } else if (parentId) {
                editingChunkInfo = { mode: 'edit', type: 'parent', chunkId: parentId };
                chunkToEdit = ManageCurrentDocumentData.chunks.find(p => p.id === parentId);
            } else {
                editingChunkInfo = { mode: 'edit', type: 'general', chunkId: chunkId };
                chunkToEdit = ManageCurrentDocumentData.chunks.find(c => c.id === chunkId);
            }
            editChunkModalLabel.text(`Edit ${editingChunkInfo.type.charAt(0).toUpperCase() + editingChunkInfo.type.slice(1)} Chunk`);
            editChunkTextarea.val(chunkToEdit.text).trigger('input');
            editChunkModal.show();
        });

        chunksListContainer.on('click', 'button[button-type="add-child-chunk"]', function (e) {
            e.preventDefault();
            e.stopPropagation();
            const button = $(this);
            const parentId = button.data('parent-id');

            editingChunkInfo = { mode: 'add', type: 'child', parentId: parentId };
            editChunkModalLabel.text('Add New Child Chunk');
            editChunkTextarea.val('').trigger('input');
            editChunkModal.show();
        });

        chunksListContainer.on('click', 'button[button-type="delete-chunk"]', async function (e) {
            e.preventDefault();
            e.stopPropagation();
            const button = $(this);

            const parentId = button.data('parent-id');
            const childId = button.data('child-id');
            const chunkId = button.data('chunk-id'); // This will be the ID for general chunks or parent chunks

            const confirmDialog = new BootstrapConfirmDialog({
                title: "Confirm Deletion",
                message: `Are you sure you want to delete this chunk? This action cannot be undone.`,
                confirmText: "Delete",
                confirmButtonClass: 'btn-danger'
            });

            if (await confirmDialog.show()) {
                if (childId) { // Deleting a single child
                    // Rule A: Clean up any pending edits for this child
                    currentEditedChunks = currentEditedChunks.filter(c => c.id !== childId);

                    // If the child was newly added, just remove it from the added list.
                    // If it's an existing chunk, add its ID to the deleted list.
                    const wasNewlyAdded = currentAddedChunks.some(c => c.id === childId);
                    if (wasNewlyAdded) {
                        currentAddedChunks = currentAddedChunks.filter(c => c.id !== childId);
                    } else {
                        currentDeletedChunks.push(childId);
                    }

                    // Update the master data and UI
                    const parentChunk = ManageCurrentDocumentData.chunks.find(c => c.id === parentId);
                    parentChunk.childrenIds = parentChunk.childrenIds.filter(id => id !== childId);
                    ManageCurrentDocumentData.chunks = ManageCurrentDocumentData.chunks.filter(c => c.id !== childId);
                }
                else if (parentId) { // Deleting a parent (CASCADING DELETE)
                    const parentChunk = ManageCurrentDocumentData.chunks.find(c => c.id === parentId);
                    if (!parentChunk) return; // Safety check

                    const allChildrenIds = parentChunk.childrenIds;

                    // Rule B: Delete all children associated with this parent.
                    allChildrenIds.forEach(idOfChild => {
                        // Rule A for each child: Clean up pending edits/adds
                        currentEditedChunks = currentEditedChunks.filter(c => c.id !== idOfChild);
                        const wasChildNewlyAdded = currentAddedChunks.some(c => c.id === idOfChild);
                        if (wasChildNewlyAdded) {
                            currentAddedChunks = currentAddedChunks.filter(c => c.id !== idOfChild);
                        } else {
                            // Only add existing children to the delete list
                            if (!currentDeletedChunks.includes(idOfChild)) {
                                currentDeletedChunks.push(idOfChild);
                            }
                        }
                    });

                    // Now, handle the parent itself
                    // Rule A for the parent: Clean up pending edits/adds
                    currentEditedChunks = currentEditedChunks.filter(c => c.id !== parentId);
                    const wasParentNewlyAdded = currentAddedChunks.some(c => c.id === parentId);
                    if (wasParentNewlyAdded) {
                        currentAddedChunks = currentAddedChunks.filter(c => c.id !== parentId);
                    } else {
                        currentDeletedChunks.push(parentId);
                    }

                    // Update the master data by removing the parent and all its children
                    ManageCurrentDocumentData.chunks = ManageCurrentDocumentData.chunks.filter(c => c.id !== parentId && !allChildrenIds.includes(c.id));
                }
                else { // Deleting a general chunk
                    // Rule A: Clean up pending edits/adds
                    currentEditedChunks = currentEditedChunks.filter(c => c.id !== chunkId);
                    const wasNewlyAdded = currentAddedChunks.some(c => c.id === chunkId);
                    if (wasNewlyAdded) {
                        currentAddedChunks = currentAddedChunks.filter(c => c.id !== chunkId);
                    } else {
                        currentDeletedChunks.push(chunkId);
                    }

                    // Update master data
                    ManageCurrentDocumentData.chunks = ManageCurrentDocumentData.chunks.filter(c => c.id !== chunkId);
                }

                fillChunksList(); // Re-render the entire list to reflect all changes
                updateSaveChangesButtonState();
            }
        });

        editChunkTextarea.on('input', function () {
            editChunkCharCount.text(`${$(this).val().length} characters`);
        });

        saveChunkChangesButton.on('click', () => {
            const newText = editChunkTextarea.val().trim();
            if (!newText) {
                AlertManager.createAlert({
                    type: 'warning',
                    message: 'Chunk text cannot be empty.',
                    timeout: 6000
                });
                return;
            }

            if (editingChunkInfo.mode === 'add') {
                const newChunk = {
                    id: `new_${new Date().getTime()}`,
                    text: newText,
                };

                if (editingChunkInfo.type === 'parent') {
                    newChunk.type = KnowledgeBaseDocumentChunkType.Parent;
                }
                else if (editingChunkInfo.type === 'child') {
                    newChunk.parentId = editingChunkInfo.parentId;
                    newChunk.type = KnowledgeBaseDocumentChunkType.Child;
                }
                else if (editingChunkInfo.type === 'general') {
                    newChunk.type = KnowledgeBaseDocumentChunkType.General;
                }

                currentAddedChunks.push(newChunk);
            }
            else
            { // 'edit' mode
                const existingEdit = currentEditedChunks.find(c => c.id === editingChunkInfo.chunkId);
                if (existingEdit) {
                    existingEdit.text = newText;
                } else {
                    currentEditedChunks.push({
                        id: editingChunkInfo.chunkId,
                        text: newText
                    });
                }
            }

            // For immediate visual feedback, we directly manipulate the master data
            // This will be properly saved on clicking the main "Save Chunks" button
            if (editingChunkInfo.type === 'child') {
                const parent = ManageCurrentDocumentData.chunks.find(p => p.id === editingChunkInfo.parentId);
                if (editingChunkInfo.mode === 'add') {
                    var newChunkId = `new_${new Date().getTime()}`;

                    parent.childrenIds.push(newChunkId);
                    ManageCurrentDocumentData.chunks.push({
                        id: newChunkId,
                        text: newText,
                        isEnabled: true,
                        type: {
                            value: KnowledgeBaseDocumentChunkType.Child
                        },
                        parentId: editingChunkInfo.parentId
                    });
                } else {
                    var existingChild = ManageCurrentDocumentData.chunks.find(c => c.id === editingChunkInfo.chunkId);
                    existingChild.text = newText;
                }
            } else if (editingChunkInfo.type === 'parent') {
                if (editingChunkInfo.mode === 'add') {
                    ManageCurrentDocumentData.chunks.push({
                        id: `new_${new Date().getTime()}`,
                        text: newText,
                        isEnabled: true,
                        type: {
                            value: KnowledgeBaseDocumentChunkType.Parent
                        },
                        childrenIds: []
                    });
                } else {
                    ManageCurrentDocumentData.chunks.find(c => c.id === editingChunkInfo.chunkId).text = newText;
                }
            } else { // general
                if (editingChunkInfo.mode === 'add') {
                    ManageCurrentDocumentData.chunks.push({
                        id: `new_${new Date().getTime()}`,
                        text: newText,
                        isEnabled: true,
                        type: {
                            value: KnowledgeBaseDocumentChunkType.General
                        }
                    });
                } else {
                    ManageCurrentDocumentData.chunks.find(c => c.id === editingChunkInfo.chunkId).text = newText;
                }
            }

            fillChunksList(); // Re-render to show all changes
            updateSaveChangesButtonState();
            editChunkModal.hide();
        });

        saveChunksButton.on('click', async (e) => {
            e.preventDefault();
            if (IsSavingChunks) return;

            const changesCount = currentAddedChunks.length + currentEditedChunks.length + currentDeletedChunks.length;
            const confirmDialog = new BootstrapConfirmDialog({
                title: "Confirm Save",
                message: `You have changes affecting ${changesCount} chunk(s). Saving will require re-processing and re-embedding for these chunks. Do you want to proceed?`,
                confirmText: "Save & Process",
            });

            if (!(await confirmDialog.show())) {
                return;
            }

            IsSavingChunks = true;
            const spinner = saveChunksButton.find('.spinner-border');
            spinner.removeClass('d-none');
            saveChunksButton.prop('disabled', true);

            const payload = {
                added: currentAddedChunks,
                edited: currentEditedChunks,
                deleted: currentDeletedChunks,
            };

            const formData = new FormData();
            formData.append('changes', JSON.stringify(payload));

            SaveDocumentChunks(ManageCurrentKnowledgeBaseData.id, ManageCurrentDocumentData.id, formData,
                (response) => {
                    if (!response.success) {
                        AlertManager.createAlert({
                            type: 'success',
                            message: 'Unable to save chunks. Check console logs for more details.',
                            timeout: 6000
                        });

                        console.error("Unable to save chunks:", response);
                    }
                    else {
                        currentAddedChunks = [];
                        currentEditedChunks = [];
                        currentDeletedChunks = [];

                        ManageCurrentDocumentData = response.data;
                        
                        const documentIndex = ManageCurrentKnowledgeBaseDocuments.findIndex(doc => doc.id === ManageCurrentDocumentData.id);
                        ManageCurrentKnowledgeBaseDocuments[documentIndex] = ManageCurrentDocumentData;

                        fillChunksList();

                        // todo maybe update the document list element too

                        AlertManager.createAlert({
                            type: 'success',
                            message: 'Chunks saved successfully and submitted for processing.',
                            timeout: 6000
                        });
                    }   

                    spinner.addClass('d-none');
                    IsSavingChunks = false;
                    updateSaveChangesButtonState();
                },
                (error) => {
                    spinner.addClass('d-none');
                    IsSavingChunks = false;
                    updateSaveChangesButtonState();

                    var resultMessage = "Check console logs for more details.";
                    if (error && error.message) resultMessage = error.message;

                    AlertManager.createAlert({
                        type: 'danger',
                        message: 'Failed to save chunks and submit for processing. Check console logs for more details.',
                        resultMessage: resultMessage,
                        timeout: 6000
                    });

                    console.error("Failed to save chunks and submit for processing:", error);
                }
            );
        });

        // Main Save Button
        saveKnowledgeBaseButton.on('click', (event) => {
            event.preventDefault();
            if (IsSavingKnowledgeBase) return;

            const validationResult = validateKnowledgeBaseTab(false);
            if (!validationResult.validated) {
                AlertManager.createAlert({
                    type: 'danger',
                    message: `Please fix the following errors:<br><br>${validationResult.errors.join('<br>')}`,
                    timeout: 6000
                });
                return;
            }

            const { hasChanges, changes } = checkKnowledgeBaseTabHasChanges(false);
            if (!hasChanges) {
                return;
            }

            IsSavingKnowledgeBase = true;
            const spinner = saveKnowledgeBaseButton.find('.spinner-border');
            spinner.removeClass('d-none');
            saveKnowledgeBaseButton.addClass('disabled').prop('disabled', true);


            const formData = new FormData();
            formData.append('postType', ManageKnowledgeBaseType);
            formData.append('changes', JSON.stringify(changes));
            if (ManageKnowledgeBaseType === 'edit') {
                formData.append('existingKnowledgeBaseId', ManageCurrentKnowledgeBaseData.id);
            }

            SaveBusinessKnowledgeBase(formData,
                (response) => {
                    ManageCurrentKnowledgeBaseData = response.data;
                    currentKnowledgeBaseName.text(ManageCurrentKnowledgeBaseData.general.name);

                    if (ManageKnowledgeBaseType === 'new') {
                        BusinessFullData.businessApp.knowledgeBases.push(ManageCurrentKnowledgeBaseData);
                    } else { // 'edit'
                        const index = BusinessFullData.businessApp.knowledgeBases.findIndex(kb => kb.id === ManageCurrentKnowledgeBaseData.id);
                        BusinessFullData.businessApp.knowledgeBases[index] = ManageCurrentKnowledgeBaseData;
                    }

                    fillKnowledgeBaseList();

                    spinner.addClass('d-none');
                    IsSavingKnowledgeBase = false;
                    ManageKnowledgeBaseType = 'edit';

                    fillKnowledgeBaseManagerTab();
                    saveKnowledgeBaseButton.addClass('disabled').prop('disabled', true);

                    AlertManager.createAlert({
                        type: "success",
                        message: "Knowledge Base saved successfully.",
                        timeout: 6000,
                    });

                    knowledgeBaseManagerDocumentsTabButton.click();
                },
                (error) => {
                    spinner.addClass('d-none');
                    IsSavingKnowledgeBase = false;
                    saveKnowledgeBaseButton.removeClass('disabled').prop('disabled', false);

                    var resultMessage = "Check console logs for more details.";
                    if (error && error.message) resultMessage = error.message;

                    AlertManager.createAlert({
                        type: "danger",
                        message: "Error occured while saving knowledgebase data.",
                        resultMessage: resultMessage,
                        timeout: 6000,
                    });

                    console.error("Error occured while saving knowledgebase data:", error);
                }
            );
        });

        // Initial Load
        if (!BusinessFullData.businessApp.knowledgeBases) {
            BusinessFullData.businessApp.knowledgeBases = [];
        }
        fillKnowledgeBaseList();
    });
}