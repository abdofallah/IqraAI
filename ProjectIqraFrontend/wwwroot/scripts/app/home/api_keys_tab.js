/** Global Variables */
var isDeletingApiKey = false;

var searchTimeout = null;
var isSearchActive = false;

/** Element Variables **/
const apiKeysTab = $("#api-keys-tab");

const addNewApiKeyButton = apiKeysTab.find("#addNewApiKeyButton");
const searchApiKeysInput = apiKeysTab.find("#searchApiKeysInput");
const searchApiKeysClearButton = apiKeysTab.find(".search-clear-btn");
const apiKeysTableBody = apiKeysTab.find("#api-keys-table tbody");

const addApiKeyModal = $("#addApiKeyModal");
const addApiKeyNameInput = addApiKeyModal.find("#addApiKeyNameInput");
const apiKeyUnrestrictedAccessCheck = addApiKeyModal.find("#apiKeyUnrestrictedAccessCheck");
const apiKeyBusinessListContainer = addApiKeyModal.find("#apiKeyBusinessListContainer");
const createApiKeyConfirmButton = addApiKeyModal.find("#createApiKeyConfirmButton");
const createApiKeyButtonSpinner = createApiKeyConfirmButton.find(".save-button-spinner");

const apiKeyGeneratedModal = $("#apiKeyGeneratedModal");
const generatedApiKeyValueInput = apiKeyGeneratedModal.find("#generatedApiKeyValue");
const copyApiKeyButton = apiKeyGeneratedModal.find("#copyApiKeyButton");

/** API Functions **/
function CreateNewApiKeyToAPI(formData, successCallback, errorCallback) {
    $.ajax({
        url: '/app/api-keys/create',
        type: 'POST',
        data: formData,
        dataType: "json",
        processData: false,
        contentType: false,
        success: (response) => {
            if (!response.success) {
                errorCallback(response);
                return;
            }

            MasterUserData.apiKeys.push(response.data);
            successCallback(response.data);
        },
        error: (error) => {
            errorCallback(error);
        }
    });
}

function DeleteApiKeyFromAPI(formData, successCallback, errorCallback) {
    const keyIdToDelete = formData.get('apiKeyId');

    $.ajax({
        url: '/app/api-keys/delete',
        type: 'POST',
        data: formData,
        dataType: "json",
        processData: false,
        contentType: false,
        success: (response) => {
            if (!response.success) {
                errorCallback(response);
                return;
            }

            const index = MasterUserData.apiKeys.findIndex(key => key.id === keyIdToDelete);
            if (index > -1) {
                MasterUserData.apiKeys.splice(index, 1);
            }
            successCallback(response.data);
        },
        error: (error) => {
            errorCallback(error);
        }
    });
}


/** Functions **/
function CreateApiKeyTableRow(apiKeyData) {
    const createdDate = moment(apiKeyData.created).format('ll');
    const lastUsedDate = apiKeyData.lastUsed ? moment(apiKeyData.lastUsed).fromNow() : 'Never';

    let element = `
        <tr data-api-key-id="${apiKeyData.id}" data-friendly-name="${apiKeyData.friendlyName.toLowerCase()}">
            <td>${apiKeyData.friendlyName}</td>
            <td><span class="font-monospace">${apiKeyData.displayName}</span></td>
            <td>${createdDate}</td>
            <td>${lastUsedDate}</td>
            <td class="text-end">
                <button class="btn btn-sm btn-outline-danger" button-type="delete-api-key" title="Delete Key">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `;
    return $(element);
}

function FillApiKeysList() {
    apiKeysTableBody.empty();
    if (MasterUserData.apiKeys.length === 0) {
        showNoApiResultsMessage(true, "", true); // Show an initial empty message
        return;
    }
    hideNoApiResultsMessage();
    MasterUserData.apiKeys.forEach((apiKeyData) => {
        const apiKeyRowElement = CreateApiKeyTableRow(apiKeyData);
        apiKeysTableBody.append(apiKeyRowElement);
    });
}

function FillAddApiKeyModalBusinesses() {
    apiKeyBusinessListContainer.empty();
    if (CurrentBusinessesList && CurrentBusinessesList.length > 0) {
        CurrentBusinessesList.forEach(business => {
            const checkbox = `<div class="form-check"><input class="form-check-input" type="checkbox" value="${business.id}" id="business_${business.id}"><label class="form-check-label" for="business_${business.id}">${business.name}</label></div>`;
            apiKeyBusinessListContainer.append(checkbox);
        });
    } else {
        apiKeyBusinessListContainer.html('<p class="text-muted text-center">No businesses available to restrict to.</p>');
    }
}

function ValidateAddApiKeyModal(enableDisableButton = false) {
    let isValid = addApiKeyNameInput.val().trim().length > 0;
    if (enableDisableButton) {
        createApiKeyConfirmButton.prop('disabled', !isValid);
    }
    return isValid;
}


/** Search Functions **/
function performApiKeySearch(searchTerm) {
    const trimmedSearchTerm = searchTerm.trim().toLowerCase();
    hideNoApiResultsMessage();

    if (trimmedSearchTerm === '') {
        clearApiKeySearch();
        return;
    }

    isSearchActive = true;
    searchApiKeysInput.addClass('search-active');
    searchApiKeysClearButton.removeClass('d-none');

    let visibleRowCount = 0;
    apiKeysTableBody.find('tr').each(function () {
        const row = $(this);
        const friendlyName = row.data('friendly-name');

        if (friendlyName && friendlyName.includes(trimmedSearchTerm)) {
            row.fadeIn(200);
            visibleRowCount++;
        } else {
            row.fadeOut(200);
        }
    });

    setTimeout(() => {
        showNoApiResultsMessage(visibleRowCount === 0, searchTerm);
    }, 250);
}

function clearApiKeySearch() {
    searchApiKeysInput.val('').removeClass('search-active');
    searchApiKeysClearButton.addClass('d-none');
    isSearchActive = false;
    hideNoApiResultsMessage();
    apiKeysTableBody.find('tr').fadeIn(200);
    if (MasterUserData.apiKeys.length === 0) {
        showNoApiResultsMessage(true, "", true);
    }
}

function showNoApiResultsMessage(show, searchTerm, isInitial = false) {
    const noResultsId = 'no-api-key-results-message';
    let noResultsRow = $(`#${noResultsId}`);

    if (show) {
        if (noResultsRow.length === 0) {
            const columnsCount = apiKeysTab.find('thead th').length;
            const messageHtml = isInitial
                ? `<div class="text-muted text-center py-5">
                       <i class="fa-regular fa-key fa-3x mb-3"></i>
                       <h5>No API Keys Found</h5>
                       <p>Click "Create API Key" to get started.</p>
                   </div>`
                : `<div class="text-muted text-center py-5">
                       <i class="fa-regular fa-magnifying-glass fa-3x mb-3"></i>
                       <h5>No API Keys found</h5>
                       <p>No keys match your search for "<span class="fw-bold">${searchTerm}</span>"</p>
                       <button class="btn btn-outline-primary btn-sm mt-2" onclick="clearApiKeySearch()">
                           <i class="fa-solid fa-times me-2"></i>Clear search
                       </button>
                   </div>`;
            noResultsRow = $(`<tr id="${noResultsId}"><td colspan="${columnsCount}">${messageHtml}</td></tr>`);
            apiKeysTableBody.append(noResultsRow);
        } else {
            if (!isInitial) {
                noResultsRow.find('.fw-bold').text(searchTerm);
            }
        }
        noResultsRow.show();
    } else {
        noResultsRow.hide();
    }
}

function hideNoApiResultsMessage() {
    $(`#no-api-key-results-message`).remove();
}

function debounceSearch(searchTerm) {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
        performApiKeySearch(searchTerm);
    }, 400);
}


/** INIT **/
function InitApiKeysTab() {
    // Event Handlers
    addNewApiKeyButton.on("click", () => FillAddApiKeyModalBusinesses());
    addApiKeyNameInput.on("input", () => ValidateAddApiKeyModal(true));

    apiKeyUnrestrictedAccessCheck.on("change", (e) => {
        const isChecked = $(e.currentTarget).is(':checked');
        apiKeyBusinessListContainer.find('.form-check-input').prop('checked', false).prop('disabled', isChecked);
        apiKeyBusinessListContainer.toggleClass('disabled-container', isChecked);
    });

    createApiKeyConfirmButton.on("click", (e) => {
        e.preventDefault();
        if (!ValidateAddApiKeyModal()) return;

        const friendlyName = addApiKeyNameInput.val().trim();
        const isUnrestricted = apiKeyUnrestrictedAccessCheck.is(':checked');
        createApiKeyConfirmButton.prop("disabled", true);
        createApiKeyButtonSpinner.removeClass("d-none");

        const formData = new FormData();
        formData.append("FriendlyName", friendlyName);
        if (!isUnrestricted) {
            apiKeyBusinessListContainer.find('input:checked').each(function () {
                formData.append('RestrictedBusinessIds[]', $(this).val());
            });
        }

        CreateNewApiKeyToAPI(
            formData,
            (response) => {
                hideNoApiResultsMessage(); // Remove "No keys" message if it exists
                var newKeyData = response.createdKey;
                var rawApiKey = response.rawApiKey;

                const newRow = CreateApiKeyTableRow(newKeyData);
                apiKeysTableBody.append(newRow).children().last().hide().fadeIn(300);

                if (isSearchActive) {
                    performApiKeySearch(searchApiKeysInput.val());
                }

                addApiKeyModal.modal("hide");
                generatedApiKeyValueInput.val(rawApiKey);
                apiKeyGeneratedModal.modal("show");
                createApiKeyConfirmButton.prop("disabled", false);
                createApiKeyButtonSpinner.addClass("d-none");
            },
            (error) => {
                AlertManager.createAlert({ type: "danger", message: "Error creating API key.", timeout: 5000 });
                console.error("Error creating API key: ", error);
                createApiKeyConfirmButton.prop("disabled", false);
                createApiKeyButtonSpinner.addClass("d-none");
            }
        );
    });

    apiKeysTableBody.on("click", "[button-type=delete-api-key]", async (e) => {
        e.preventDefault();
        e.stopPropagation();
        if (isDeletingApiKey) return;

        let rowElement = $(e.currentTarget).closest('tr');
        let apiKeyId = rowElement.data("api-key-id");
        let keyFriendlyName = rowElement.find('td:first').text();

        const confirmDialog = new BootstrapConfirmDialog({
            title: `Delete API Key (${keyFriendlyName})`,
            message: "Are you sure you want to permanently delete this key? This action is irreversible.",
            confirmText: "Yes, Delete It",
            confirmButtonClass: "btn-danger",
        });

        if (!await confirmDialog.show()) return;
        isDeletingApiKey = true;
        rowElement.addClass('table-danger');

        const formData = new FormData();
        formData.append("apiKeyId", apiKeyId);

        DeleteApiKeyFromAPI(
            formData,
            () => {
                rowElement.fadeOut(400, function () { $(this).remove(); });
                if (isSearchActive) {
                    performApiKeySearch(searchApiKeysInput.val());
                } else if (MasterUserData.apiKeys.length === 0) {
                    showNoApiResultsMessage(true, "", true);
                }
                isDeletingApiKey = false;
            },
            (error) => {
                AlertManager.createAlert({ type: "danger", message: "Error deleting API key.", timeout: 5000 });
                console.error("Error deleting API key: ", error);
                rowElement.removeClass('table-danger');
                isDeletingApiKey = false;
            }
        );
    });

    copyApiKeyButton.on("click", () => {
        navigator.clipboard.writeText(generatedApiKeyValueInput.val()).then(() => {
            const originalIcon = copyApiKeyButton.html();
            copyApiKeyButton.html('<i class="fa-regular fa-check"></i>').prop('disabled', true);
            setTimeout(() => {
                copyApiKeyButton.html(originalIcon).prop('disabled', false);
            }, 2000);
        });
    });

    addApiKeyModal.on("hidden.bs.modal", () => {
        addApiKeyNameInput.val("");
        apiKeyUnrestrictedAccessCheck.prop('checked', false).trigger('change');
        ValidateAddApiKeyModal(true);
    });

    // Search Event Handlers
    searchApiKeysInput.on("input", (e) => debounceSearch(e.target.value));
    searchApiKeysInput.on("keydown", (e) => {
        if (e.key === "Enter") {
            e.preventDefault();
            clearTimeout(searchTimeout); // Prevent debounce from firing
            performApiKeySearch(searchApiKeysInput.val());
        } else if (e.key === "Escape") {
            e.preventDefault();
            clearApiKeySearch();
        }
    });

    searchApiKeysClearButton.on("click", (e) => {
        e.preventDefault();
        clearApiKeySearch();
        searchApiKeysInput.focus();
    });

    searchApiKeysInput.on("focus", () => searchApiKeysInput.parent().addClass('input-group-focus'));
    searchApiKeysInput.on("blur", () => searchApiKeysInput.parent().removeClass('input-group-focus'));

    // Init
    FillApiKeysList();
}