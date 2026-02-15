/** Dynamic Variables **/
var CurrentLanguagesList = [];
var CurrentManageLanguageType = null;
var CurrentManageLanguageData = null;

let TempPublicDisabledReason = "";
let TempPrivateDisabledReason = "";

/** Elements **/
const languagesTab = $("#languages-tab");

// Headers
const languagesInnerHeader = languagesTab.find("#languages-inner-header");
const languagesManageInnerHeader = languagesTab.find("#languages-manager-inner-header");
const languagesManageBreadcrumb = languagesManageInnerHeader.find("#languages-manage-breadcrumb");
const switchBackToLanguagesListTabFromManageTab = languagesManageBreadcrumb.find("#switchBackToLanguagesListTabFromManageTab");
const currentManageLanguageName = languagesManageBreadcrumb.find("#currentManageLanguageName");
const saveManageLanguagesButton = languagesManageInnerHeader.find("#saveManageLanguagesButton");

// List Tab
const languagesListTableTab = languagesTab.find("#languagesListTableTab");
const addNewLanguageButton = languagesListTableTab.find("#addNewLanguageButton");
const languagesListTable = languagesListTableTab.find("#languagesListTable");
const searchlanguagesButton = languagesListTableTab.find("#searchlanguagesButton");

// Manager Tab - Main
const languagesManageTab = languagesTab.find("#languagesManageTab");
const languagesManagerPromptsTabButton = languagesManageInnerHeader.find("#languages-manager-prompts-tab");

// Manager Tab - General
const manageLanguagesIdInput = languagesManageTab.find("#manageLanguagesIdInput");
const manageLanguagesNameInput = languagesManageTab.find("#manageLanguagesNameInput");
const manageLanguagesLocaleNameInput = languagesManageTab.find("#manageLanguagesLocaleNameInput");
// Status Elements
const manageLanguagesDisabledInput = languagesManageTab.find("#manageLanguagesDisabledInput");
const manageLanguagesDisabledReasonContainer = languagesManageTab.find("#manageLanguagesDisabledReasonContainer");
const manageLanguagesPublicReasonDisplay = languagesManageTab.find("#manageLanguagesPublicReasonDisplay");
const manageLanguagesPrivateReasonDisplay = languagesManageTab.find("#manageLanguagesPrivateReasonDisplay");

// Manager Tab - Prompts (Conversation)
const promptConversationWarmup = languagesManageTab.find("#manageLanguagesPrompt_ConversationWarmup");
const promptConversationBase = languagesManageTab.find("#manageLanguagesPrompt_ConversationBase");
const promptConversationFailed = languagesManageTab.find("#manageLanguagesPrompt_ConversationFailed");

// Manager Tab - Prompts (Verification)
const promptVerifTurnEnd = languagesManageTab.find("#manageLanguagesPrompt_VerifTurnEnd");
const promptVerifInterruption = languagesManageTab.find("#manageLanguagesPrompt_VerifInterruption");
const promptVerifVoicemail = languagesManageTab.find("#manageLanguagesPrompt_VerifVoicemail");

// Manager Tab - Prompts (RAG)
const promptRagClassifier = languagesManageTab.find("#manageLanguagesPrompt_RagClassifier");
const promptRagRefinement = languagesManageTab.find("#manageLanguagesPrompt_RagRefinement");

// Manager Tab - Prompts (Analysis)
const promptAnalysisSummary = languagesManageTab.find("#manageLanguagesPrompt_AnalysisSummary");
const promptAnalysisSummaryQuery = languagesManageTab.find("#manageLanguagesPrompt_AnalysisSummaryQuery");
const promptAnalysisTags = languagesManageTab.find("#manageLanguagesPrompt_AnalysisTags");
const promptAnalysisTagsQuery = languagesManageTab.find("#manageLanguagesPrompt_AnalysisTagsQuery");
const promptAnalysisExtract = languagesManageTab.find("#manageLanguagesPrompt_AnalysisExtract");
const promptAnalysisExtractQuery = languagesManageTab.find("#manageLanguagesPrompt_AnalysisExtractQuery");

// Collection for bulk operations
const allPromptInputs = promptConversationWarmup.add(promptConversationBase).add(promptConversationFailed)
    .add(promptVerifTurnEnd).add(promptVerifInterruption).add(promptVerifVoicemail)
    .add(promptRagClassifier).add(promptRagRefinement)
    .add(promptAnalysisSummary).add(promptAnalysisSummaryQuery)
    .add(promptAnalysisTags).add(promptAnalysisTagsQuery)
    .add(promptAnalysisExtract).add(promptAnalysisExtractQuery);

// Modal Elements
const languageStatusReasonModalEl = document.getElementById('languageStatusReasonModal');
const languageStatusReasonModal = new bootstrap.Modal(languageStatusReasonModalEl);
const languageStatusPublicReason = $(languageStatusReasonModalEl).find("#languageStatusPublicReason");
const languageStatusPrivateReason = $(languageStatusReasonModalEl).find("#languageStatusPrivateReason");
const languageStatusConfirmButton = $(languageStatusReasonModalEl).find("#languageStatusConfirmButton");


/** API Functions **/
function FetchLanguagesFromAPI(page, pageSize, successCallback, errorCallback) {
    $.ajax({
        url: '/app/admin/languages',
        type: 'POST',
        dataType: "json",
        data: {
            page: page,
            pageSize: pageSize
        },
        success: (response) => {
            if (!response.success) {
                errorCallback(response, true);
                return;
            }
            successCallback(response.data);
        },
        error: (error) => {
            errorCallback(error, false);
        }
    });
}

function SaveLanguagesData(formData, successCallback, errorCallback) {
    $.ajax({
        type: "POST",
        url: "/app/admin/languages/save",
        data: formData,
        dataType: "json",
        processData: false,
        contentType: false,
        success: (response) => {
            if (!response.success) {
                errorCallback(response, true);
                return;
            }
            successCallback(response);
        },
        error: (error) => {
            errorCallback(error, false);
        }
    });
}

/** Functions **/
function ShowLanguagesManageTab() {
    languagesListTableTab.removeClass("show");
    languagesInnerHeader.removeClass("show");

    setTimeout(() => {
        languagesListTableTab.addClass("d-none");
        languagesInnerHeader.addClass("d-none");

        languagesManageTab.removeClass("d-none");
        languagesManageInnerHeader.removeClass("d-none");

        setTimeout(() => {
            languagesManageTab.addClass("show");
            languagesManageInnerHeader.addClass("show");

            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function ShowLanguagesListTab() {
    languagesManageTab.removeClass("show");
    languagesManageInnerHeader.removeClass("show");

    setTimeout(() => {
        languagesManageTab.addClass("d-none");
        languagesManageInnerHeader.addClass("d-none");

        languagesListTableTab.removeClass("d-none");
        languagesInnerHeader.removeClass("d-none");

        setTimeout(() => {
            languagesListTableTab.addClass("show");
            languagesInnerHeader.addClass("show");

            setDynamicBodyHeight();
        }, 10);
    }, 300);
}

function ResetAndEmptyLanguagesManageTab(isEdit = false) {
    // Clear inputs
    languagesManageTab.find("input, textarea").val("").removeClass("is-invalid");

    // Reset Tabs
    $("#languages-manager-general-tab").click(); // Reset to General
    $("#prompt-cat-conversation-tab").click(); // Reset inner prompt tab

    // Logic for New vs Edit
    if (!isEdit) {
        // NEW: Lock ID editable, Lock Disabled Checked
        manageLanguagesIdInput.prop("disabled", false);

        // Force Disabled = True and Lock it
        manageLanguagesDisabledInput.prop("checked", true).prop("disabled", true);
        TempPublicDisabledReason = "Language Initialization";
        TempPrivateDisabledReason = "New language created. Can enable language after adding & filling all prompts.";

        // Show defaults in UI
        manageLanguagesPublicReasonDisplay.text(TempPublicDisabledReason);
        manageLanguagesPrivateReasonDisplay.text(TempPrivateDisabledReason);
        manageLanguagesDisabledReasonContainer.removeClass("d-none");
    } else {
        // EDIT: Lock ID, Unlock Disabled switch (validation happens on change)
        manageLanguagesIdInput.prop("disabled", true);

        TempPublicDisabledReason = "";
        TempPrivateDisabledReason = "";
        manageLanguagesDisabledInput.prop("disabled", false);
    }

    saveManageLanguagesButton.prop("disabled", true);
}

function CreateDefaultLanguagesDataObject() {
    return {
        id: "",
        localeName: "",
        name: "",
        disabledAt: null,
        publicDisabledReason: "Language Initialization",
        privateDisabledReason: "New language created",
        prompts: {
            // Initialize empty strings to avoid null issues
            conversationWarmupLLMPrompt: "",
            conversationBasePrompt: "",
            failedConversationBasePromptGenerationPrompt: "",
            turnEndVerificationPrompt: "",
            interruptionVerificationPrompt: "",
            voicemailVerificationPrompt: "",
            ragQueryClassifierPrompt: "",
            ragQueryRefinementPrompt: "",
            postAnalaysisSummaryGenerationPrompt: "",
            postAnalaysisSummaryGenerationPromptQuery: "",
            postAnalaysisTagsClassificationPrompt: "",
            postAnalaysisTagsClassificationPromptQuery: "",
            postAnalaysisDataExtractionPrompt: "",
            postAnalaysisDataExtractionPromptQuery: ""
        }
    };
}

function CreateLanguagesListTableElement(languagesData) {
    let disabledAt = "-";
    if (languagesData.disabledAt != null) {
        disabledAt = `<span class="badge bg-danger">Disabled</span>`;
    } else {
        disabledAt = `<span class="badge bg-success">Active</span>`;
    }

    let element = $(`<tr language-id="${languagesData.id}">
                <td class="font-monospace">${languagesData.id}</td>
                <td>${languagesData.name} <span class="text-muted small">(${languagesData.localeName})</span></td>
                <td>${disabledAt}</td>
                <td>
                    <button class="btn btn-info btn-sm" language-id="${languagesData.id}" button-type="edit-language">
                        <i class="fa-regular fa-pen-to-square"></i> Edit
                    </button>
                </td>
            </tr>`);

    return element;
}

function CheckLanguageManageTabHasChanges(enableDisableButton = true) {
    let changes = {};
    let hasChanges = false;
    const currentData = CurrentManageLanguageData;
    const currentPrompts = currentData.prompts || {};

    // General
    changes.id = manageLanguagesIdInput.val(); // Not really changeable in Edit, but tracked

    changes.name = manageLanguagesNameInput.val();
    if (currentData.name !== changes.name) hasChanges = true;

    changes.localeName = manageLanguagesLocaleNameInput.val();
    if (currentData.localeName !== changes.localeName) hasChanges = true;

    // Disabled Logic
    changes.disabled = manageLanguagesDisabledInput.prop("checked");
    const isCurrentlyDisabled = currentData.disabledAt != null;

    if (changes.disabled !== isCurrentlyDisabled) {
        hasChanges = true;
    }

    // If disabled, check/send reasons
    if (changes.disabled) {
        changes.publicDisabledReason = TempPublicDisabledReason;
        changes.privateDisabledReason = TempPrivateDisabledReason;

        if (changes.publicDisabledReason !== currentData.publicDisabledReason) hasChanges = true;
        if (changes.privateDisabledReason !== currentData.privateDisabledReason) hasChanges = true;
    }

    // Prompts Object Construction
    changes.prompts = {
        conversationWarmupLLMPrompt: promptConversationWarmup.val(),
        conversationBasePrompt: promptConversationBase.val(),
        failedConversationBasePromptGenerationPrompt: promptConversationFailed.val(),

        turnEndVerificationPrompt: promptVerifTurnEnd.val(),
        interruptionVerificationPrompt: promptVerifInterruption.val(),
        voicemailVerificationPrompt: promptVerifVoicemail.val(),

        ragQueryClassifierPrompt: promptRagClassifier.val(),
        ragQueryRefinementPrompt: promptRagRefinement.val(),

        postAnalaysisSummaryGenerationPrompt: promptAnalysisSummary.val(),
        postAnalaysisSummaryGenerationPromptQuery: promptAnalysisSummaryQuery.val(),
        postAnalaysisTagsClassificationPrompt: promptAnalysisTags.val(),
        postAnalaysisTagsClassificationPromptQuery: promptAnalysisTagsQuery.val(),
        postAnalaysisDataExtractionPrompt: promptAnalysisExtract.val(),
        postAnalaysisDataExtractionPromptQuery: promptAnalysisExtractQuery.val()
    };

    // Deep compare prompts
    for (const key in changes.prompts) {
        if ((currentPrompts[key] || "") !== changes.prompts[key]) {
            hasChanges = true;
            break;
        }
    }

    if (enableDisableButton) {
        saveManageLanguagesButton.prop("disabled", !hasChanges);
    }

    return { hasChanges, changes };
}

function ValidateLanguageManageTabFields(onlyRemove = true) {
    let errors = [];
    let validated = true;

    // General Validation
    let languageId = manageLanguagesIdInput.val();
    if (!languageId || !languageId.trim()) {
        validated = false;
        errors.push("Identifier is required.");
        if (!onlyRemove) manageLanguagesIdInput.addClass("is-invalid");
    } else {
        manageLanguagesIdInput.removeClass("is-invalid");
    }

    let languageName = manageLanguagesNameInput.val();
    if (!languageName || !languageName.trim()) {
        validated = false;
        errors.push("Name is required.");
        if (!onlyRemove) manageLanguagesNameInput.addClass("is-invalid");
    } else {
        manageLanguagesNameInput.removeClass("is-invalid");
    }

    let localeName = manageLanguagesLocaleNameInput.val();
    if (!localeName || !localeName.trim()) {
        validated = false;
        errors.push("Locale Name is required.");
        if (!onlyRemove) manageLanguagesLocaleNameInput.addClass("is-invalid");
    } else {
        manageLanguagesLocaleNameInput.removeClass("is-invalid");
    }

    // Safety Layer: Prompts Validation
    // Only enforced if user is trying to ENABLE the language (Unchecked Disabled)
    const isEnabling = !manageLanguagesDisabledInput.prop("checked");

    if (isEnabling) {
        let missingPrompts = false;

        allPromptInputs.each(function () {
            const val = $(this).val();
            if (!val || !val.trim()) {
                missingPrompts = true;
                if (!onlyRemove) $(this).addClass("is-invalid");
            } else {
                $(this).removeClass("is-invalid");
            }
        });

        if (missingPrompts) {
            validated = false;
            errors.push("<b>Cannot Enable Language:</b> All prompt fields must be filled before enabling this language.");

            // Auto-switch to prompts tab to show errors
            if (!onlyRemove) {
                languagesManagerPromptsTabButton.click();
                // We could also try to detect which inner tab has errors, 
                // but highlighting the fields (red border) is usually enough cue.
            }
        }
    } else {
        // If disabled, we don't strictly validate prompts visual state (remove invalid classes)
        allPromptInputs.removeClass("is-invalid");
    }

    return { validated, errors };
}

function FillLanguagesManageTabData(data) {
    // General
    manageLanguagesIdInput.val(data.id);
    manageLanguagesNameInput.val(data.name);
    manageLanguagesLocaleNameInput.val(data.localeName);

    // Status & Reasons
    const isDisabled = data.disabledAt != null;
    manageLanguagesDisabledInput.prop("checked", isDisabled);

    if (isDisabled) {
        TempPublicDisabledReason = data.publicDisabledReason || "";
        TempPrivateDisabledReason = data.privateDisabledReason || "";

        manageLanguagesPublicReasonDisplay.text(TempPublicDisabledReason || "-");
        manageLanguagesPrivateReasonDisplay.text(TempPrivateDisabledReason || "-");
        manageLanguagesDisabledReasonContainer.removeClass("d-none");
    } else {
        TempPublicDisabledReason = "";
        TempPrivateDisabledReason = "";
        manageLanguagesDisabledReasonContainer.addClass("d-none");
    }

    // Prompts
    const p = data.prompts || {};

    promptConversationWarmup.val(p.conversationWarmupLLMPrompt || "");
    promptConversationBase.val(p.conversationBasePrompt || "");
    promptConversationFailed.val(p.failedConversationBasePromptGenerationPrompt || "");

    promptVerifTurnEnd.val(p.turnEndVerificationPrompt || "");
    promptVerifInterruption.val(p.interruptionVerificationPrompt || "");
    promptVerifVoicemail.val(p.voicemailVerificationPrompt || "");

    promptRagClassifier.val(p.ragQueryClassifierPrompt || "");
    promptRagRefinement.val(p.ragQueryRefinementPrompt || "");

    promptAnalysisSummary.val(p.postAnalaysisSummaryGenerationPrompt || "");
    promptAnalysisSummaryQuery.val(p.postAnalaysisSummaryGenerationPromptQuery || "");
    promptAnalysisTags.val(p.postAnalaysisTagsClassificationPrompt || "");
    promptAnalysisTagsQuery.val(p.postAnalaysisTagsClassificationPromptQuery || "");
    promptAnalysisExtract.val(p.postAnalaysisDataExtractionPrompt || "");
    promptAnalysisExtractQuery.val(p.postAnalaysisDataExtractionPromptQuery || "");
}

/** Initializer **/
$(document).ready(() => {

    // --- List Actions ---
    languagesListTableTab.on("click", "button[button-type=edit-language]", (event) => {
        event.preventDefault();
        let languageId = $(event.currentTarget).attr("language-id");
        let languageData = CurrentLanguagesList.find((l) => l.id == languageId);

        CurrentManageLanguageType = "edit";
        CurrentManageLanguageData = languageData;

        currentManageLanguageName.text(languageData.name);

        ResetAndEmptyLanguagesManageTab(true); // Is Edit
        FillLanguagesManageTabData(languageData);
        ShowLanguagesManageTab();

        // Initial validation check to clear artifacts
        CheckLanguageManageTabHasChanges();
    });

    addNewLanguageButton.on("click", (event) => {
        event.preventDefault();
        
        CurrentManageLanguageData = CreateDefaultLanguagesDataObject();

        currentManageLanguageName.text("New Language");

        ResetAndEmptyLanguagesManageTab(false); // Is New (Locks disabled)

        CurrentManageLanguageType = "new";

        ShowLanguagesManageTab();
    });

    switchBackToLanguagesListTabFromManageTab.on("click", (event) => {
        event.preventDefault();
        CurrentManageLanguageType = null;
        ShowLanguagesListTab();
    });

    // --- Save Action ---
    saveManageLanguagesButton.on("click", (event) => {
        event.preventDefault();

        // Validation
        let validation = ValidateLanguageManageTabFields(false);
        if (!validation.validated) {
            AlertManager.createAlert({
                type: "danger",
                message: "Validation Failed:<br>" + validation.errors.join("<br>"),
                timeout: 6000,
            });
            return;
        }

        // Changes Check
        let changesResult = CheckLanguageManageTabHasChanges(false);
        if (!changesResult.hasChanges) {
            return;
        }

        saveManageLanguagesButton.prop("disabled", true);

        // Payload
        let formData = new FormData();
        formData.append("postType", CurrentManageLanguageType);
        formData.append("languageCode", changesResult.changes.id); // ID needed for New/Edit lookup
        formData.append("changes", JSON.stringify(changesResult.changes));

        // Submit
        SaveLanguagesData(
            formData,
            (saveResponse) => {
                AlertManager.createAlert({
                    type: "success",
                    message: `Language ${CurrentManageLanguageType === 'new' ? 'Created' : 'Updated'} Successfully.`,
                    timeout: 4000
                });

                var newLanguageData = saveResponse.data;

                var newTableElement = CreateLanguagesListTableElement(newLanguageData);

                if (CurrentManageLanguageType === 'new') {
                    CurrentManageLanguageType = 'edit';
                    manageLanguagesIdInput.prop('disabled', true);
                    manageLanguagesDisabledInput.prop('disabled', false);

                    CurrentLanguagesList.push(newLanguageData);

                    languagesListTable.find("tbody").append(newTableElement);
                }
                else if (CurrentManageLanguageType === 'edit') {
                    var existingIndex = CurrentLanguagesList.findIndex((l) => l.id == newLanguageData.id);
                    if (existingIndex != -1) {
                        CurrentLanguagesList[existingIndex] = newLanguageData;
                    }

                    var existingTableElement = languagesListTable.find("tr[language-id='" + newLanguageData.id + "']");
                    if (existingTableElement.length > 0) {
                        existingTableElement.replaceWith(newTableElement);
                    }
                }

                currentManageLanguageName.text(newLanguageData.name);

                saveManageLanguagesButton.prop("disabled", true); // Disable until next change
            },
            (errorResult) => {
                let msg = "Error saving language.";
                if (errorResult && errorResult.message) msg = errorResult.message;

                AlertManager.createAlert({
                    type: "danger",
                    message: msg,
                    timeout: 6000,
                });
                saveManageLanguagesButton.prop("disabled", false);
            },
        );
    });

    // --- Input Change Monitoring ---
    languagesManageTab.on("change input", "input, textarea", (event) => {
        CheckLanguageManageTabHasChanges(true);
    });

    // --- Status Switch Logic ---
    manageLanguagesDisabledInput.on("click", (e) => {     
        if (CurrentManageLanguageType == null) return;

        const isCurrentlyChecked = manageLanguagesDisabledInput.prop("checked");

        if (isCurrentlyChecked) {
            e.preventDefault(); // Stop immediate toggle

            // Turning ON Disabled Mode -> Open Modal
            languageStatusPublicReason.val(TempPublicDisabledReason || "");
            languageStatusPrivateReason.val(TempPrivateDisabledReason || "");
            languageStatusReasonModal.show();
        } else {
            manageLanguagesDisabledInput.prop("checked", false);
            manageLanguagesDisabledReasonContainer.addClass("d-none");

            TempPrivateDisabledReason = "";
            TempPublicDisabledReason = "";

            // Trigger change check
            CheckLanguageManageTabHasChanges(true);
        }
    });

    // Modal Confirm Button
    languageStatusConfirmButton.on("click", () => {
        const pub = languageStatusPublicReason.val();
        const priv = languageStatusPrivateReason.val();

        if (!pub || !priv) {
            AlertManager.createAlert({
                type: 'warning',
                message: 'Both reasons are required.',
                timeout: 4000
            });
            return;
        }

        // Store Reasons
        TempPublicDisabledReason = pub;
        TempPrivateDisabledReason = priv;

        // Update UI
        manageLanguagesPublicReasonDisplay.text(pub);
        manageLanguagesPrivateReasonDisplay.text(priv);
        manageLanguagesDisabledReasonContainer.removeClass("d-none");

        // Toggle Switch
        manageLanguagesDisabledInput.prop("checked", true);

        languageStatusReasonModal.hide();
        CheckLanguageManageTabHasChanges(true);
    });

    // --- Initial Load ---
    FetchLanguages();
});

function FetchLanguages() {
    FetchLanguagesFromAPI(0, 100,
        (languagesData) => {
            CurrentLanguagesList = languagesData;
            languagesListTable.find("tbody").empty();

            if (languagesData.length > 0) {
                languagesData.forEach((langaugeData) => {
                    languagesListTable.find("tbody").append(CreateLanguagesListTableElement(langaugeData));
                });
            } else {
                languagesListTable.find("tbody").append('<tr class="none-notice"><td colspan="4" class="text-center text-muted">No Languages Found</td></tr>');
            }
        },
        (error) => {
            console.error(error);
            AlertManager.createAlert({ type: 'danger', message: 'Failed to fetch languages.' });
        }
    );
}