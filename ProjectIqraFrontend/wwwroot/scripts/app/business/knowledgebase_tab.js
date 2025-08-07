/** Global Variables **/
const ChunkingModeENUM = {
    General: "general",
    ParentChild: "parent-child",
};

const ChunkingSelectMap = {
    "0": "general",
    "1": "parentchild"
};

const RetrievalModeENUM = {
    Vector: 'vectorsearch',
    FullText: 'fulltextsearch',
    Hybrid: 'hybirdsearch'
};

const RetrievalSelectMap = {
    "0": "vectorsearch",
    "1": "fulltextsearch",
    "2": "hybirdsearch"
};


/** Dynamic Variables **/
let ManageKnowledgeBaseType = null; // 'new' or 'edit'
let ManageCurrentKnowledgeBaseData = null;
let ManageCurrentDocumentData = null; // To track document being edited/viewed

// -- NEW: Change tracking arrays for chunks --
let currentAddedChunks = [];
let currentEditedChunks = [];
let currentDeletedChunks = [];

let editingChunkInfo = null; // { mode: 'add'/'edit', type: 'parent'/'child'/'general', parentId: '...', chunkId: '...' }
let IsSavingKnowledgeBase = false;
let IsProcessingDocument = false;
let IsSavingChunks = false;


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
const knowledgeBaseListTable = knowledgeBaseListTab.find("#knowledgeBaseListTable");

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

const generalChunkSettings = knowledgeBaseManagerTab.find('.knowledgebase-document-chunking-type-box[box-type="general"]');
const generalDelimiterInput = generalChunkSettings.find('#generalDelimiter');
const generalMaxLengthInput = generalChunkSettings.find('#generalMaxChunkLength');
const generalOverlapInput = generalChunkSettings.find('#generalChunkOverlap');
const generalReplaceConsecutiveCheck = generalChunkSettings.find('#generalReplaceConsecutive');
const generalDeleteUrlsCheck = generalChunkSettings.find('#generalDeleteUrls');

const parentChildChunkSettings = knowledgeBaseManagerTab.find('.knowledgebase-document-chunking-type-box[box-type="parentchild"]');
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


/** API FUNCTIONS (Stubs) **/
function SaveBusinessKnowledgeBase(formData, successCallback, errorCallback) {
    // $.ajax({ ... }); // Future implementation
    console.log("Simulating Save with FormData:", JSON.parse(formData.get('changes')));

    const changes = JSON.parse(formData.get('changes'));
    const response = {
        success: true,
        data: {
            id: ManageKnowledgeBaseType === 'new' ? `kb_${new Date().getTime()}` : formData.get('existingKnowledgeBaseId'),
            ...changes,
            documents: ManageKnowledgeBaseType === 'edit' ? ManageCurrentKnowledgeBaseData.documents : []
        }
    };

    setTimeout(() => {
        successCallback(response);
    }, 1000);
}

function SaveAndProcessDocument(formData, successCallback, errorCallback) {
    // This would be a real multipart/form-data POST
    const docFile = formData.get('file');
    console.log(`Simulating processing for file: ${docFile.name}`);

    // Create more realistic sample chunk data
    const sampleChunks = [
        {
            id: 'p1', type: 'parent', text: 'Dubai is a city and emirate in the United Arab Emirates known for luxury shopping, ultramodern architecture and a lively nightlife scene.',
            children: [
                { id: 'c1-1', type: 'child', text: 'Known for luxury shopping and ultramodern architecture.' },
                { id: 'c1-2', type: 'child', text: 'It has a lively nightlife scene.' }
            ]
        },
        {
            id: 'p2', type: 'parent', text: 'Burj Khalifa, an 830m-tall tower, dominates the skyscraper-filled skyline. At its foot lies Dubai Fountain, with jets and lights choreographed to music.',
            children: [
                { id: 'c2-1', type: 'child', text: 'The Burj Khalifa is 830m tall.' }
            ]
        }
    ];

    const sampleGeneralChunks = [
        { id: 'g1', text: 'Dubai is a city in the UAE.' },
        { id: 'g2', text: 'It is known for the Burj Khalifa.' },
        { id: 'g3', text: 'The Dubai Mall is a large shopping center.' }
    ];

    const response = {
        success: true,
        data: {
            id: `doc_${new Date().getTime()}`,
            name: docFile.name,
            enabled: true,
            status: 'Ready',
            chunks: ManageCurrentKnowledgeBaseData.configuration.chunking.mode === ChunkingModeENUM.ParentChild ? sampleChunks : sampleGeneralChunks
        }
    };
    setTimeout(() => successCallback(response), 1500);
}

function SaveDocumentChunks(kbId, docId, changes, successCallback, errorCallback) {
    console.log(`Simulating save for chunks of document ${docId} in KB ${kbId}`);
    console.log("Changes to send:", changes);
    setTimeout(() => successCallback({ success: true }), 1000);
}

function TestRetrievalQuery(kbId, query, successCallback, errorCallback) {
    console.log(`Simulating retrieval test for query "${query}" in KB ${kbId}`);
    const results = [
        { score: 0.92, text: 'Known for luxury shopping and ultramodern architecture.' },
        { score: 0.87, text: 'The Burj Khalifa is 830m tall.' },
        { score: 0.75, text: 'The Dubai Mall is a large shopping center.' }
    ];
    setTimeout(() => successCallback({ success: true, data: results }), 1000);
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
    const documents = kbData.documents || [];
    const retrievalMode = kbData.configuration?.retrieval?.mode || 'N/A';
    return `
        <div class="col-lg-4 col-md-6 col-12">
            <div class="routing-card d-flex flex-column align-items-start justify-content-center" kb-id="${kbData.id}">
                <div class="d-flex flex-row align-items-center justify-content-start mb-4">
                    <span class="route-icon">${kbData.general.emoji}</span>
                    <div class="card-data">
                        <h4>${kbData.general.name}</h4>
                        <h6>${documents.length} Document${documents.length === 1 ? "" : "s"}</h6>
						<h6>Mode: ${retrievalMode.replace('-', ' ')}</h6>
                    </div>
                </div>
                <div>
                    <h5 class="h5-info agent-description">
                        <span>${kbData.general.description}</span>
                    </h5>
                </div>
            </div>
        </div>
    `;
}

function fillKnowledgeBaseList() {
    const knowledgeBases = BusinessFullData.businessApp.knowledgeBases;

    knowledgeBaseListTable.empty();
    if (knowledgeBases.length === 0) {
        knowledgeBaseListTable.append('<div class="col-12"><h6 class="text-center mt-5">No knowledge bases added yet...</h6></div>');
    } else {
        knowledgeBases.forEach(kb => {
            knowledgeBaseListTable.append($(createKnowledgeBaseListElement(kb)));
        });
    }
}

// -- Document & Chunk Management --
function createDocumentTableRow(docData) {
    let statusPill = '';
    switch (docData.status) {
        case 'Ready':
            statusPill = `<span class="badge bg-success">Ready</span>`;
            break;
        case 'Processing':
            statusPill = `<span class="badge bg-primary">Processing</span>`;
            break;
        case 'Failed':
            statusPill = `<span class="badge bg-danger">Failed</span>`;
            break;
        default:
            statusPill = `<span class="badge bg-secondary">Unknown</span>`;
    }

    return `
        <tr doc-id="${docData.id}">
            <td>${docData.name}</td>
            <td>${statusPill}</td>
            <td class="d-flex align-items-center">
                <div class="form-check form-switch me-3">
                    <input class="form-check-input" type="checkbox" role="switch" button-type="toggleDocumentStatus" title="Enable/Disable Document" ${docData.enabled ? 'checked' : ''}>
                </div>
                <button class="btn btn-info btn-sm me-2" button-type="viewDocumentChunks" title="View/Edit Chunks">
                    <i class="fa-regular fa-layer-group"></i>
                </button>
                <button class="btn btn-danger btn-sm" button-type="deleteDocument" title="Delete Document">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `;
}

function fillDocumentsTable() {
    const docsTableBody = documentsTable.find("tbody");
    docsTableBody.empty();
    if (!ManageCurrentKnowledgeBaseData || ManageCurrentKnowledgeBaseData.documents.length === 0) {
        docsTableBody.append('<tr tr-type="none-notice"><td colspan="3">No documents uploaded yet.</td></tr>');
    } else {
        ManageCurrentKnowledgeBaseData.documents.forEach(doc => {
            docsTableBody.append(createDocumentTableRow(doc));
        });
    }
}

function populateDocumentModalSettings() {
    const chunkingConfig = ManageCurrentKnowledgeBaseData.configuration.chunking;
    let settingsHtml = '';

    if (chunkingConfig.mode === ChunkingModeENUM.General) {
        settingsHtml = `
            <div class="row">
                <div class="col-md-4">
                    <label class="form-label">Delimiter</label>
                    <input type="text" class="form-control" id="modalGeneralDelimiter" value="${chunkingConfig.general.delimiter}">
                </div>
                <div class="col-md-4">
                    <label class="form-label">Max length</label>
                    <input type="number" class="form-control" id="modalGeneralMaxLength" value="${chunkingConfig.general.maxLength}">
                </div>
                <div class="col-md-4">
                    <label class="form-label">Overlap</label>
                    <input type="number" class="form-control" id="modalGeneralOverlap" value="${chunkingConfig.general.overlap}">
                </div>
            </div>
        `;
    } else { // ParentChild
        settingsHtml = `
            <h6>Parent-chunk</h6>
             <div class="row mb-3">
                <div class="col-md-6">
                    <label class="form-label">Delimiter</label>
                    <input type="text" class="form-control" id="modalParentDelimiter" value="${chunkingConfig.parentChild.parent.delimiter}">
                </div>
                <div class="col-md-6">
                    <label class="form-label">Max length</label>
                    <input type="number" class="form-control" id="modalParentMaxLength" value="${chunkingConfig.parentChild.parent.maxLength}">
                </div>
            </div>
            <h6>Child-chunk</h6>
             <div class="row">
                <div class="col-md-6">
                    <label class="form-label">Delimiter</label>
                    <input type="text" class="form-control" id="modalChildDelimiter" value="${chunkingConfig.parentChild.child.delimiter}">
                </div>
                <div class="col-md-6">
                    <label class="form-label">Max length</label>
                    <input type="number" class="form-control" id="modalChildMaxLength" value="${chunkingConfig.parentChild.child.maxLength}">
                </div>
            </div>
        `;
    }
    modalChunkSettingsContainer.html(settingsHtml);
}

// NEW CHUNK DISPLAY FUNCTIONS
function createParentChunkCard(parentChunk) {
    const childPills = parentChunk.children.map(child => `
        <div class="d-inline-flex align-items-center me-2 mb-2 chunk-pill" data-parent-id="${parentChunk.id}" data-child-id="${child.id}">
            <button class="btn btn-sm btn-outline-secondary" button-type="edit-chunk" data-parent-id="${parentChunk.id}" data-child-id="${child.id}">${child.text.substring(0, 50)}...</button>
            <button class="btn btn-sm btn-outline-danger ms-1" button-type="delete-chunk" data-parent-id="${parentChunk.id}" data-child-id="${child.id}"><i class="fa-regular fa-xmark"></i></button>
        </div>
    `).join('');

    const cardId = `chunk-card-${parentChunk.id}`;

    return `
        <div class="chunk-card mb-3 p-3 border rounded" id="${cardId}" data-parent-id="${parentChunk.id}">
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <p class="mb-1 chunk-text">${parentChunk.text}</p>
                    <small class="text-muted">ID: ${parentChunk.id} | <span class="char-count">${parentChunk.text.length}</span> characters</small>
                </div>
                <div class="btn-group">
                    <button class="btn btn-sm btn-light" button-type="edit-chunk" data-parent-id="${parentChunk.id}" title="Edit Parent Chunk"><i class="fa-regular fa-pen-to-square"></i></button>
                    <button class="btn btn-sm btn-light" button-type="add-child-chunk" data-parent-id="${parentChunk.id}" title="Add Child Chunk"><i class="fa-regular fa-plus"></i></button>
                    <button class="btn btn-sm btn-light" button-type="delete-chunk" data-parent-id="${parentChunk.id}" title="Delete Parent & Children"><i class="fa-regular fa-trash"></i></button>
                </div>
            </div>
            <hr>
            <div class="child-chunk-header" data-bs-toggle="collapse" href="#collapse-${parentChunk.id}">
                <i class="fa-regular fa-chevron-down me-2"></i>
                <span>${parentChunk.children.length} CHILD CHUNK${parentChunk.children.length !== 1 ? 'S' : ''}</span>
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
                    <p class="mb-1 chunk-text">${chunk.text}</p>
                    <small class="text-muted">ID: ${chunk.id} | <span class="char-count">${chunk.text.length}</span> characters</small>
                </div>
                <div class="btn-group">
                    <button class="btn btn-sm btn-light" button-type="edit-chunk" data-chunk-id="${chunk.id}" title="Edit Chunk"><i class="fa-regular fa-pen-to-square"></i></button>
                    <button class="btn btn-sm btn-light" button-type="delete-chunk" data-chunk-id="${chunk.id}" title="Delete Chunk"><i class="fa-regular fa-trash"></i></button>
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
                <p class="card-text">${result.text}</p>
            </div>
        </div>
    `;
}

function fillChunksList() {
    chunksListContainer.empty();
    const chunks = ManageCurrentDocumentData.chunks || [];
    const mode = ManageCurrentKnowledgeBaseData.configuration.chunking.mode;

    if (chunks.length === 0) {
        chunksListContainer.html('<h6 class="text-center mt-5">No chunks found for this document.</h6>');
        return;
    }

    if (mode === ChunkingModeENUM.ParentChild) {
        chunks.forEach(parentChunk => {
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
            chunking: {
                mode: ChunkingModeENUM.General,
                general: {
                    delimiter: "\\n\\n",
                    maxLength: 1024,
                    overlap: 50,
                    preprocess: {
                        replaceConsecutive: false,
                        deleteUrls: false
                    }
                },
                parentChild: {
                    parent: {
                        type: 'paragraph',
                        delimiter: "\\n\\n",
                        maxLength: 1024
                    },
                    child: {
                        delimiter: "\\n",
                        maxLength: 512,
                    },
                    preprocess: {
                        replaceConsecutive: false,
                        deleteUrls: false
                    }
                }
            },
            embedding: null, // To be filled by IntegrationManager
            retrieval: {
                mode: RetrievalModeENUM.Vector,
                vector: {
                    topK: 3,
                    useScoreThreshold: false,
                    scoreThreshold: 0.5,
                    rerank: {
                        enabled: false,
                        integration: null
                    }
                },
                fullText: {
                    topK: 3,
                    rerank: {
                        enabled: false,
                        integration: null
                    }
                },
                hybrid: {
                    mode: 'weighted_score', // or 'rerank_model'
                    weight: 0.7,
                    topK: 3,
                    useScoreThreshold: false,
                    scoreThreshold: 0.5,
                    rerank: {
                        integration: null
                    }
                }
            }
        },
        documents: []
    };
}

function resetAndEmptyKnowledgeBaseManagerTab() {
    // General
    editKnowledgeBaseIconInput.text("🧠");
    editKnowledgeBaseNameInput.val("").removeClass('is-invalid');
    editKnowledgeBaseDescriptionInput.val("").removeClass('is-invalid');

    // Configuration - Re-enable everything for 'new'
    knowledgeBaseManagerConfigurationPane.find('input, select, button').prop('disabled', false);
    knowledgeBaseManagerConfigurationPane.removeClass('disabled-pane');
    knowledgebaseDocumentChunkingTypeSelect.val('0').trigger('change');
    knowledgebaseDocumentRetrivalTypeSelect.val('0').trigger('change');

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
    knowledgebaseDocumentRetrivalTypeSelect.prop('disabled', false); // Can always change retrieval
    knowledgeBaseEmbeddingIntegrationManager.disable();
    knowledgeBaseManagerConfigurationPane.addClass('disabled-pane');

    // -- NEW: Set select boxes based on loaded data --
    const chunkingModeVal = Object.keys(ChunkingSelectMap).find(key => ChunkingSelectMap[key] === kbData.configuration.chunking.mode);
    knowledgebaseDocumentChunkingTypeSelect.val(chunkingModeVal).trigger('change');

    const retrievalModeVal = Object.keys(RetrievalSelectMap).find(key => RetrievalSelectMap[key] === kbData.configuration.retrieval.mode);
    knowledgebaseDocumentRetrivalTypeSelect.val(retrievalModeVal).trigger('change');

    // -- Set Score Thresholds --
    useVectorScoreThreshold.prop('checked', kbData.configuration.retrieval.vector.useScoreThreshold).trigger('change');
    useHybirdScoreThreshold.prop('checked', kbData.configuration.retrieval.hybrid.useScoreThreshold).trigger('change');
    vectorScoreThresholdInput.val(kbData.configuration.retrieval.vector.scoreThreshold);
    hybridScoreThresholdInput.val(kbData.configuration.retrieval.hybrid.scoreThreshold);

    // Documents
    fillDocumentsTable();

    // Tabs & Buttons
    knowledgeBaseManagerDocumentsTabButton.removeClass('disabled').prop('disabled', false);
    saveKnowledgeBaseButton.addClass('disabled').prop("disabled", true);
}

function checkKnowledgeBaseTabHasChanges(enableDisableButton = true) {
    if (ManageKnowledgeBaseType === null) return { hasChanges: false };

    const changes = {};
    let hasChanges = false;

    // --- General Tab ---
    changes.general = {
        emoji: editKnowledgeBaseIconInput.text(),
        name: editKnowledgeBaseNameInput.val().trim(),
        description: editKnowledgeBaseDescriptionInput.val().trim(),
    };

    if (
        changes.general.emoji !== ManageCurrentKnowledgeBaseData.general.emoji ||
        changes.general.name !== ManageCurrentKnowledgeBaseData.general.name ||
        changes.general.description !== ManageCurrentKnowledgeBaseData.general.description
    ) {
        hasChanges = true;
    }

    const chunkingMode = ChunkingSelectMap[knowledgebaseDocumentChunkingTypeSelect.val()];
    const retrievalMode = RetrievalSelectMap[knowledgebaseDocumentRetrivalTypeSelect.val()];

    // --- Configuration Tab ---
    changes.configuration = {
        chunking: {
            mode: chunkingMode,
            general: {
                delimiter: generalDelimiterInput.val(),
                maxLength: parseInt(generalMaxLengthInput.val()),
                overlap: parseInt(generalOverlapInput.val()),
                preprocess: {
                    replaceConsecutive: generalReplaceConsecutiveCheck.is(':checked'),
                    deleteUrls: generalDeleteUrlsCheck.is(':checked'),
                }
            },
            parentChild: {
                parent: {
                    type: $('input[name="parentChunkType"]:checked').val(),
                    delimiter: parentDelimiterInput.val(),
                    maxLength: parseInt(parentMaxLengthInput.val()),
                },
                child: {
                    delimiter: childDelimiterInput.val(),
                    maxLength: parseInt(childMaxLengthInput.val()),
                },
                preprocess: {
                    replaceConsecutive: parentChildReplaceConsecutiveCheck.is(':checked'),
                    deleteUrls: parentChildDeleteUrlsCheck.is(':checked'),
                }
            }
        },
        embedding: ManageCurrentKnowledgeBaseData.configuration.embedding,
        retrieval: {
            mode: retrievalMode,
            vector: {
                topK: parseInt(vectorTopKInput.val()),
                useScoreThreshold: useVectorScoreThreshold.is(':checked'),
                scoreThreshold: parseFloat(vectorScoreThresholdInput.val()),
                rerank: {
                    enabled: vectorRerankModelSwitch.is(':checked'),
                    integration: vectorRerankIntegrationManager.getData()
                }
            },
            fullText: {
                topK: parseInt(fulltextTopKInput.val()),
                rerank: {
                    enabled: fulltextRerankModelSwitch.is(':checked'),
                    integration: fulltextRerankIntegrationManager.getData()
                }
            },
            hybrid: {
                mode: $('input[name="hybridMode"]:checked').val(),
                weight: parseFloat(hybridWeightSlider.val()),
                topK: parseInt(hybridTopKInput.val()),
                useScoreThreshold: useHybirdScoreThreshold.is(':checked'),
                scoreThreshold: parseFloat(hybridScoreThresholdInput.val()),
                rerank: {
                    integration: hybridRerankIntegrationManager.getData()
                }
            }
        }
    };
    if (ManageKnowledgeBaseType === 'new') {
        changes.configuration.chunking.mode = $('input[name="chunkMode"]:checked').val();
        changes.configuration.embedding = knowledgeBaseEmbeddingIntegrationManager.getData();
    }


    if (JSON.stringify(changes.configuration) !== JSON.stringify(ManageCurrentKnowledgeBaseData.configuration)) {
        hasChanges = true;
    }


    if (enableDisableButton) {
        saveKnowledgeBaseButton.prop("disabled", !hasChanges);
        if (hasChanges) saveKnowledgeBaseButton.removeClass('disabled');
        else saveKnowledgeBaseButton.addClass('disabled');
    }

    return {
        hasChanges: hasChanges,
        changes: changes,
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
    const selectedChunkingType = ChunkingSelectMap[knowledgebaseDocumentChunkingTypeSelect.val()];
    if (selectedChunkingType === 'general') {
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
        if (isNaN(generalMaxLength) || generalMaxLength <= 0) {
            validated = false;
            errors.push("General chunk max length is invalid.");
            if (!onlyRemove) {
                generalMaxLengthInput.addClass("is-invalid");
            }
        } else {
            generalMaxLengthInput.removeClass("is-invalid");
        }

        const chunkOverlap = parseInt(generalOverlapInput.val());
        if (isNaN(chunkOverlap) || chunkOverlap < 0) {
            validated = false;
            errors.push("General chunk overlap is invalid.");
            if (!onlyRemove) {
                generalOverlapInput.addClass("is-invalid");
            }
        } else {
            generalOverlapInput.removeClass("is-invalid");
        }
    }
    else if (selectedChunkingType === 'parent-child') {
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
            if (isNaN(parentMaxLength) || parentMaxLength <= 0) {
                validated = false;
                errors.push("Parent chunk max length is invalid.");
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
        if (isNaN(childMaxLength) || childMaxLength <= 0) {
            validated = false;
            errors.push("Child chunk max length is invalid.");
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
    const retrivalType = RetrievalSelectMap[knowledgebaseDocumentRetrivalTypeSelect.val()];
    if (retrivalType === 'vectorsearch') {
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
    else if (retrivalType == "fulltextsearch") {
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
    else if (retrivalType == "hybirdsearch") {
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
            integrationType: 'LLM',
            allowMultiple: false,
            isLanguageBound: false,
            allIntegrations: BusinessFullData.businessApp.integrations,
            providersData: BusinessLLMProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onIntegrationChange: () => { handleInputChange() },
        });
        vectorRerankIntegrationManager = new IntegrationConfigurationManager('#vectorRerankContainer', {
            integrationType: 'LLM',
            allowMultiple: false,
            isLanguageBound: false,
            allIntegrations: BusinessFullData.businessApp.integrations,
            providersData: BusinessLLMProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onIntegrationChange: () => { handleInputChange() },
        });
        fulltextRerankIntegrationManager = new IntegrationConfigurationManager('#fulltextRerankContainer', {
            integrationType: 'LLM',
            allowMultiple: false,
            isLanguageBound: false,
            allIntegrations: BusinessFullData.businessApp.integrations,
            providersData: BusinessLLMProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onIntegrationChange: () => { handleInputChange() },
        });
        hybridRerankIntegrationManager = new IntegrationConfigurationManager('#hybridRerankContainer', {
            integrationType: 'LLM',
            allowMultiple: false,
            isLanguageBound: false,
            allIntegrations: BusinessFullData.businessApp.integrations,
            providersData: BusinessLLMProvidersForIntegrations,
            modalSelector: '#integrationConfigurationModal',
            onIntegrationChange: () => { handleInputChange() },
        });

        // -- Event Handlers --

        // View Switching & Navigation
        addNewKnowledgeBaseButton.on("click", (event) => {
            event.preventDefault();
            ManageCurrentKnowledgeBaseData = createDefaultKnowledgeBaseObject();
            currentKnowledgeBaseName.text("New Knowledge Base");
            resetAndEmptyKnowledgeBaseManagerTab();
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

        knowledgeBaseListTable.on('click', '.routing-card', (event) => {
            event.preventDefault();
            const kbId = $(event.currentTarget).attr('kb-id');
            ManageCurrentKnowledgeBaseData = BusinessFullData.businessApp.knowledgeBases.find(kb => kb.id === kbId);
            currentKnowledgeBaseName.text(ManageCurrentKnowledgeBaseData.general.name);
            resetAndEmptyKnowledgeBaseManagerTab();
            fillKnowledgeBaseManagerTab();
            showKnowledgeBaseManagerTab();
            ManageKnowledgeBaseType = "edit";
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
            const selectedType = ChunkingSelectMap[$(this).val()];
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
            const selectedType = RetrievalSelectMap[$(this).val()];
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

            SaveAndProcessDocument(formData, (response) => {
                const newDoc = response.data;
                ManageCurrentKnowledgeBaseData.documents.push(newDoc);
                fillDocumentsTable();

                spinner.addClass('d-none');
                saveAndProcessDocumentButton.prop('disabled', false);
                IsProcessingDocument = false;
                documentSettingsModal.hide();

                AlertManager.createAlert({ type: 'success', message: 'Document processed successfully.' });
            }, (error) => {
                spinner.addClass('d-none');
                saveAndProcessDocumentButton.prop('disabled', false);
                IsProcessingDocument = false;
                AlertManager.createAlert({ type: 'danger', message: 'Failed to process document.' });
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
                AlertManager.createAlert({ type: 'warning', message: 'Please enter a query.' });
                return;
            }

            const spinner = runQueryButton.find('.spinner-border');
            spinner.removeClass('d-none');
            runQueryButton.prop('disabled', true);

            TestRetrievalQuery(ManageCurrentKnowledgeBaseData.id, query, (response) => {
                retrievalResultsContainer.empty();
                if (response.data.length === 0) {
                    retrievalResultsContainer.html('<p class="text-muted">No results found for your query.</p>');
                } else {
                    response.data.forEach((result, index) => {
                        retrievalResultsContainer.append(createRetrievalResultCard(result, index));
                    });
                }
                spinner.addClass('d-none');
                runQueryButton.prop('disabled', false);
            }, (error) => {
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
                ManageCurrentKnowledgeBaseData.documents = ManageCurrentKnowledgeBaseData.documents.filter(d => d.id !== docId);
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
            ManageCurrentDocumentData = ManageCurrentKnowledgeBaseData.documents.find(d => d.id === docId);

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
            const mode = ManageCurrentKnowledgeBaseData.configuration.chunking.mode;
            const type = mode === ChunkingModeENUM.General ? 'general' : 'parent';

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

        chunksListContainer.on('click', 'button', async function (e) {
            e.preventDefault();
            e.stopPropagation();
            const button = $(this);
            const buttonType = button.attr('button-type');

            const parentId = button.data('parent-id');
            const childId = button.data('child-id');
            const chunkId = button.data('chunk-id');

            if (buttonType === 'edit-chunk') {
                let chunkToEdit;
                if (childId) {
                    editingChunkInfo = { mode: 'edit', type: 'child', chunkId: childId, parentId: parentId };
                    chunkToEdit = ManageCurrentDocumentData.chunks.find(p => p.id === parentId).children.find(c => c.id === childId);
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
            }
            else if (buttonType === 'add-child-chunk') {
                editingChunkInfo = { mode: 'add', type: 'child', parentId: parentId };
                editChunkModalLabel.text('Add New Child Chunk');
                editChunkTextarea.val('').trigger('input');
                editChunkModal.show();
            }
            else if (buttonType === 'delete-chunk') {
                const confirmDialog = new BootstrapConfirmDialog({
                    title: "Confirm Deletion",
                    message: `Are you sure you want to delete this chunk?`,
                    confirmText: "Delete",
                    confirmButtonClass: 'btn-danger'
                });
                if (await confirmDialog.show()) {
                    if (childId) { // Deleting a child
                        currentDeletedChunks.push(childId);
                        button.closest('.chunk-pill').remove();
                    } else if (parentId) { // Deleting a parent
                        currentDeletedChunks.push(parentId);
                        button.closest('.chunk-card').remove();
                    } else { // Deleting a general chunk
                        currentDeletedChunks.push(chunkId);
                        button.closest('.chunk-card').remove();
                    }
                    updateSaveChangesButtonState();
                }
            }
        });

        editChunkTextarea.on('input', function () {
            editChunkCharCount.text(`${$(this).val().length} characters`);
        });

        saveChunkChangesButton.on('click', () => {
            const newText = editChunkTextarea.val().trim();
            if (!newText) {
                AlertManager.createAlert({ type: 'warning', message: 'Chunk text cannot be empty.' });
                return;
            }

            if (editingChunkInfo.mode === 'add') {
                const newChunk = {
                    id: `new_${new Date().getTime()}`,
                    text: newText,
                };
                if (editingChunkInfo.type === 'child') {
                    newChunk.parentId = editingChunkInfo.parentId;
                }
                currentAddedChunks.push(newChunk);

            } else { // 'edit' mode
                const existingEdit = currentEditedChunks.find(c => c.id === editingChunkInfo.chunkId);
                if (existingEdit) {
                    existingEdit.text = newText;
                } else {
                    currentEditedChunks.push({ id: editingChunkInfo.chunkId, text: newText });
                }
            }

            // For immediate visual feedback, we directly manipulate the master data
            // This will be properly saved on clicking the main "Save Chunks" button
            if (editingChunkInfo.type === 'child') {
                const parent = ManageCurrentDocumentData.chunks.find(p => p.id === editingChunkInfo.parentId);
                if (editingChunkInfo.mode === 'add') {
                    parent.children.push({ id: `new_${new Date().getTime()}`, text: newText, type: 'child' });
                } else {
                    parent.children.find(c => c.id === editingChunkInfo.chunkId).text = newText;
                }
            } else if (editingChunkInfo.type === 'parent') {
                if (editingChunkInfo.mode === 'add') {
                    ManageCurrentDocumentData.chunks.push({ id: `new_${new Date().getTime()}`, text: newText, type: 'parent', children: [] });
                } else {
                    ManageCurrentDocumentData.chunks.find(c => c.id === editingChunkInfo.chunkId).text = newText;
                }
            } else { // general
                if (editingChunkInfo.mode === 'add') {
                    ManageCurrentDocumentData.chunks.push({ id: `new_${new Date().getTime()}`, text: newText, type: 'general' });
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

            SaveDocumentChunks(ManageCurrentKnowledgeBaseData.id, ManageCurrentDocumentData.id, payload,
                (response) => {
                    // This is where the permanent merge happens after successful save
                    // For now, our temporary merge in the modal save handler suffices for UI
                    // A real implementation would now update the master `ManageCurrentDocumentData` from the server response
                    currentAddedChunks = [];
                    currentEditedChunks = [];
                    currentDeletedChunks = [];

                    spinner.addClass('d-none');
                    IsSavingChunks = false;
                    updateSaveChangesButtonState(); // Will disable the button
                    AlertManager.createAlert({ type: 'success', message: 'Chunks saved successfully.' });
                },
                (error) => {
                    spinner.addClass('d-none');
                    IsSavingChunks = false;
                    updateSaveChangesButtonState(); // Re-enable on failure
                    AlertManager.createAlert({ type: 'danger', message: 'Failed to save chunks.' });
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

            SaveBusinessKnowledgeBase(formData, (response) => {
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

            }, (error) => {
                spinner.addClass('d-none');
                IsSavingKnowledgeBase = false;
                saveKnowledgeBaseButton.removeClass('disabled').prop('disabled', false);
                AlertManager.createAlert({
                    type: "danger",
                    message: "An error occurred while saving.",
                    timeout: 6000,
                });
            });
        });


        // Initial Load
        if (!BusinessFullData.businessApp.knowledgeBases) {
            BusinessFullData.businessApp.knowledgeBases = [];
        }
        fillKnowledgeBaseList();
    });
}