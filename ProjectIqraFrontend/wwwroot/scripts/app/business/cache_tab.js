/** Dynamic Variables **/

// Messages
let CurrentMessageGroupData = null;
let ManageMessageGroupType = null; // new or edit
let CurrentMessageCacheData = null;
let ManageMessageCacheType = null; // new or edit

let messageCacheGroupMultilanguage = null;

let IsSavingMessageGroupTab = false;
let IsSavingMessageCacheTab = false;

// Audio
let CurrentAudioGroupData = null;
let ManageAudioGroupType = null; // new or edit
let CurrentAudioCacheData = null;
let ManageAudioCacheType = null; // new or edi

let audioCacheGroupMultilanguage = null;

let IsSavingAudioGroupTab = false;
let IsSavingAudioCacheTab = false;

// Embeddings
let CurrentEmbeddingGroupData = null;
let ManageEmbeddingGroupType = null; // new or edit
let CurrentEmbeddingCacheData = null;
let ManageEmbeddingCacheType = null; // new or edit

let embeddingCacheGroupMultilanguage = null;

let IsSavingEmbeddingGroupTab = false;
let IsSavingEmbeddingCacheTab = false;

/** Elements Variables **/

// Cache Tab
const cacheTab = $("#cache-tab");

const cacheInnerTab = cacheTab.find("#cache-inner-tab");
const cacheHeader = cacheTab.find("#cache-header");

// Message Group Tab
const switchBackToMessageGroupsListTab = cacheTab.find("#switchBackToMessageGroupsListTab");
const messageGroupBreadcrumb = cacheTab.find("#messageGroupBreadcrumb");
const messageGroupsListTab = cacheTab.find("#messageGroupsListTab");
const messageGroupManagerTab = cacheTab.find("#messageGroupManagerTab");
const addNewMessageGroupButton = cacheTab.find("#addNewMessageGroupButton");
const currentMessageGroupName = cacheTab.find("#currentMessageGroupName");
const saveMessageGroupButton = cacheTab.find("#saveMessageGroupButton");
const messageGroupsTable = cacheTab.find("#messageGroupsTable");
const messageCacheTable = cacheTab.find("#messageCacheTable");

// Message Cache Manager
const messageCacheListTab = cacheTab.find("#messageCacheListTab");
const messageCacheManagerTab = cacheTab.find("#messageCacheManagerTab");
const messageCacheBreadcrumb = cacheTab.find("#messageCacheBreadcrumb");
const addNewMessageCacheButton = cacheTab.find("#addNewMessageCacheButton");
const switchBackToMessageCacheListTab = cacheTab.find("#switchBackToMessageCacheListTab");
const currentMessageCacheName = cacheTab.find("#currentMessageCacheName");
const currentMessageGroupNameInCache = cacheTab.find("#currentMessageGroupNameInCache");
const saveMessageCacheButton = cacheTab.find("#saveMessageCacheButton");

// Audio Groups
const audioGroupsListTab = cacheTab.find("#audioGroupsListTab");
const audioGroupManagerTab = cacheTab.find("#audioGroupManagerTab");
const audioGroupBreadcrumb = cacheTab.find("#audioGroupBreadcrumb");
const addNewAudioGroupButton = cacheTab.find("#addNewAudioGroupButton");
const switchBackToAudioGroupsListTab = cacheTab.find("#switchBackToAudioGroupsListTab");
const currentAudioGroupName = cacheTab.find("#currentAudioGroupName");
const saveAudioGroupButton = cacheTab.find("#saveAudioGroupButton");
const audioGroupsTable = cacheTab.find("#audioGroupsTable");
const audioCacheTable = cacheTab.find("#audioCacheTable");

// Audio Cache
const audioCacheListTab = cacheTab.find("#audioCacheListTab");
const audioCacheManagerTab = cacheTab.find("#audioCacheManagerTab");
const audioCacheBreadcrumb = cacheTab.find("#audioCacheBreadcrumb");
const addNewAudioCacheButton = cacheTab.find("#addNewAudioCacheButton");
const switchBackToAudioCacheListTab = cacheTab.find("#switchBackToAudioCacheListTab");
const currentAudioCacheName = cacheTab.find("#currentAudioCacheName");
const saveAudioCacheButton = cacheTab.find("#saveAudioCacheButton");
const currentAudioGroupNameInCache = cacheTab.find("#currentAudioGroupNameInCache");

// Embedding Groups
const embeddingGroupsListTab = cacheTab.find("#embeddingGroupsListTab");
const embeddingGroupManagerTab = cacheTab.find("#embeddingGroupManagerTab");
const embeddingGroupBreadcrumb = cacheTab.find("#embeddingGroupBreadcrumb");
const addNewEmbeddingGroupButton = cacheTab.find("#addNewEmbeddingGroupButton");
const switchBackToEmbeddingGroupsListTab = cacheTab.find("#switchBackToEmbeddingGroupsListTab");
const currentEmbeddingGroupName = cacheTab.find("#currentEmbeddingGroupName");
const saveEmbeddingGroupButton = cacheTab.find("#saveEmbeddingGroupButton");
const embeddingGroupsTable = cacheTab.find("#embeddingGroupsTable");
const embeddingCacheTable = cacheTab.find("#embeddingCacheTable");

// Embedding Cache
const embeddingCacheListTab = cacheTab.find("#embeddingCacheListTab");
const embeddingCacheManagerTab = cacheTab.find("#embeddingCacheManagerTab");
const embeddingCacheBreadcrumb = cacheTab.find("#embeddingCacheBreadcrumb");
const addNewEmbeddingCacheButton = cacheTab.find("#addNewEmbeddingCacheButton");
const switchBackToEmbeddingCacheListTab = cacheTab.find("#switchBackToEmbeddingCacheListTab");
const currentEmbeddingCacheName = cacheTab.find("#currentEmbeddingCacheName");
const saveEmbeddingCacheButton = cacheTab.find("#saveEmbeddingCacheButton");
const currentEmbeddingGroupNameInCache = cacheTab.find("#currentEmbeddingGroupNameInCache");

/** Functions **/

// API Functions

function SaveBusinessContextMessageGroup(formData, onSuccess, onError) {
	$.ajax({
		url: `/app/user/business/${CurrentBusinessId}/cache/messagegroups/save`,
		method: "POST",
		data: formData,
		processData: false,
		contentType: false,
		success: (response) => {
			if (response.success) {
				onSuccess(response);
			} else {
				onError(response, true);
			}
		},
		error: (error) => {
			onError(error, false);
		},
	});
}

function SaveBusinessContextMessageCache(formData, onSuccess, onError) {
	$.ajax({
		url: `/app/user/business/${CurrentBusinessId}/cache/messagegroups/messages/save`,
		method: "POST",
		data: formData,
		processData: false,
		contentType: false,
		success: (response) => {
			if (response.success) {
				onSuccess(response);
			} else {
				onError(response, true);
			}
		},
		error: (error) => {
			onError(error, false);
		},
	});
}

function SaveBusinessContextAudioGroup(formData, onSuccess, onError) {
	$.ajax({
		url: `/app/user/business/${CurrentBusinessId}/cache/audiogroups/save`,
		method: "POST",
		data: formData,
		processData: false,
		contentType: false,
		success: (response) => {
			if (response.success) {
				onSuccess(response);
			} else {
				onError(response, true);
			}
		},
		error: (error) => {
			onError(error, false);
		},
	});
}

function SaveBusinessContextAudioCache(formData, onSuccess, onError) {
	$.ajax({
		url: `/app/user/business/${CurrentBusinessId}/cache/audiogroups/audios/save`,
		method: "POST",
		data: formData,
		processData: false,
		contentType: false,
		success: (response) => {
			if (response.success) {
				onSuccess(response);
			} else {
				onError(response, true);
			}
		},
		error: (error) => {
			onError(error, false);
		},
	});
}

function SaveBusinessContextEmbeddingGroup(formData, onSuccess, onError) {
	$.ajax({
		url: `/app/user/business/${CurrentBusinessId}/cache/embeddinggroups/save`,
		method: "POST",
		data: formData,
		processData: false,
		contentType: false,
		success: (response) => {
			if (response.success) {
				onSuccess(response);
			} else {
				onError(response, true);
			}
		},
		error: (error) => {
			onError(error, false);
		},
	});
}

function SaveBusinessContextEmbeddingCache(formData, onSuccess, onError) {
	$.ajax({
		url: `/app/user/business/${CurrentBusinessId}/cache/embeddinggroups/embeddings/save`,
		method: "POST",
		data: formData,
		processData: false,
		contentType: false,
		success: (response) => {
			if (response.success) {
				onSuccess(response);
			} else {
				onError(response, true);
			}
		},
		error: (error) => {
			onError(error, false);
		},
	});
}

// Message Group Functions
function resetOrClearMessageGroupManager() {
	$("#messageGroupNameInput").val("");

	messageGroupManagerTab.find(".is-invalid").removeClass("is-invalid");
	saveMessageGroupButton.prop("disabled", true);

	const messageTable = messageCacheTable.find("tbody");
	messageTable.empty();
	messageTable.append('<tr tr-type="none-notice"><td colspan="2">No message queries found</td></tr>');
}

function ShowMessageGroupManagerTab() {
	cacheInnerTab.removeClass("show");
	messageGroupsListTab.removeClass("show");
	setTimeout(() => {
		cacheInnerTab.addClass("d-none");
		messageGroupsListTab.addClass("d-none");

		messageGroupBreadcrumb.removeClass("d-none");
		messageGroupManagerTab.removeClass("d-none");
		setTimeout(() => {
			messageGroupBreadcrumb.addClass("show");
			messageGroupManagerTab.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowMessageGroupsListTab() {
	messageGroupBreadcrumb.removeClass("show");
	messageGroupManagerTab.removeClass("show");
	setTimeout(() => {
		messageGroupBreadcrumb.addClass("d-none");
		messageGroupManagerTab.addClass("d-none");

		cacheInnerTab.removeClass("d-none");
		messageGroupsListTab.removeClass("d-none");
		setTimeout(() => {
			cacheInnerTab.addClass("show");
			messageGroupsListTab.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function createMessageGroupTableElement(messageGroup) {
	const element = `
		<tr tr-type="message-group" data-id="${messageGroup.id}">
			<td>${messageGroup.name}</td>
			<td>
				 <button class="btn btn-info btn-sm" button-type="editMessageGroupCache" group-id="${messageGroup.id}">
                    <i class="fa-regular fa-pen-to-square"></i>
                </button>
                <button class="btn btn-danger btn-sm" button-type="deleteMessageGroupCache" group-id="${messageGroup.id}">
                    <i class="fa-regular fa-trash"></i>
                </button>
			</td>
		</tr>`;

	return element;
}

function FillCacheMessageGroup() {
	const messageGroup = BusinessFullData.businessApp.cache.messageGroups;

	messageGroupsTable.find("tbody").empty();
	if (messageGroup.length === 0) {
		messageGroupsTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="2">No message groups found</td></tr>');
	} else {
		messageGroup.forEach((group) => {
			messageGroupsTable.find("tbody").append($(createMessageGroupTableElement(group)));
		});
	}
}

function fillMessageGroupManager(groupData) {
	// Fill general fields
	$("#messageGroupNameInput").val(groupData.name);

	// Get current language messages
	const currentLanguage = messageCacheGroupMultilanguage.getSelectedLanguage().id;

	// Populate messages table in List tab
	const messageTable = messageCacheTable.find("tbody");
	messageTable.empty();

	const languageMessages = groupData.messages[currentLanguage] || [];
	if (languageMessages.length === 0) {
		messageTable.append('<tr tr-type="none-notice"><td colspan="2">No message queries found</td></tr>');
	} else {
		languageMessages.forEach((message) => {
			messageTable.append($(createMessageCacheTableElement(message)));
		});
	}

	saveMessageGroupButton.prop("disabled", true);
}

function CheckMessageGroupTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Check name changes
	changes.name = $("#messageGroupNameInput").val().trim();
	if (CurrentMessageGroupData.name !== changes.name) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveMessageGroupButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

async function canLeaveMessageGroupTab(leaveMessage = "") {
	if (ManageMessageGroupType == null) return true;

	const groupChanges = CheckMessageGroupTabHasChanges(false);
	if (groupChanges.hasChanges) {
		const confirmDiscardChangesDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in message group.${leaveMessage}`,
			confirmText: "Discard",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "modal-lg",
		});

		const confirmDiscardChangesResult = await confirmDiscardChangesDialog.show();
		if (!confirmDiscardChangesResult) {
			return false;
		}
	}

	return true;
}

function createDefaultMessageGroupObject() {
	const object = {
		name: "",
		messages: {},
	};

	// Initialize messages for each language
	BusinessFullData.businessData.languages.forEach((language) => {
		object.messages[language] = [];
	});

	return object;
}

function ValidateMessageGroupTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Validate name
	const groupName = $("#messageGroupNameInput").val().trim();
	if (!groupName || groupName.length === 0) {
		validated = false;
		errors.push("Group name is required.");
		if (!onlyRemove) {
			$("#messageGroupNameInput").addClass("is-invalid");
		}
	} else {
		$("#messageGroupNameInput").removeClass("is-invalid");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

// Message Cache Functions
function resetOrClearMessageCacheManager() {
	$("#messageCacheQueryInput").val("");
	$("#messageCacheResponseInput").val("");

	messageCacheManagerTab.find(".is-invalid").removeClass("is-invalid");
	saveMessageCacheButton.prop("disabled", true);
}

function ShowMessageCacheManagerTab() {
	messageGroupManagerTab.removeClass("show");
	messageGroupBreadcrumb.removeClass("show");
	setTimeout(() => {
		messageGroupManagerTab.addClass("d-none");
		messageGroupBreadcrumb.addClass("d-none");

		messageCacheBreadcrumb.removeClass("d-none");
		messageCacheManagerTab.removeClass("d-none");
		setTimeout(() => {
			messageCacheBreadcrumb.addClass("show");
			messageCacheManagerTab.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowMessageCacheListTab() {
	messageCacheBreadcrumb.removeClass("show");
	messageCacheManagerTab.removeClass("show");
	setTimeout(() => {
		messageCacheBreadcrumb.addClass("d-none");
		messageCacheManagerTab.addClass("d-none");

		messageGroupManagerTab.removeClass("d-none");
		messageGroupBreadcrumb.removeClass("d-none");
		setTimeout(() => {
			messageGroupManagerTab.addClass("show");
			messageGroupBreadcrumb.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function createMessageCacheTableElement(message) {
	return `
        <tr message-id="${message.id}">
            <td>
                <b>${message.query}</b>
            </td>
            <td>
                <button class="btn btn-info btn-sm" button-type="editMessageCache" message-id="${message.id}">
                    <i class="fa-regular fa-pen-to-square"></i>
                </button>
                <button class="btn btn-danger btn-sm" button-type="deleteMessageCache" message-id="${message.id}">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `;
}

function createDefaultMessageCacheObject() {
	return {
		query: "",
		response: ""
	};
}

function fillMessageCacheManager(cacheData) {
	// Fill form fields
	$("#messageCacheQueryInput").val(cacheData.query);
	$("#messageCacheResponseInput").val(cacheData.response);

	saveMessageCacheButton.prop("disabled", true);
}

function CheckMessageCacheTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Check query changes
	changes.query = $("#messageCacheQueryInput").val().trim();
	if (CurrentMessageCacheData.query !== changes.query) {
		hasChanges = true;
	}

	// Check response changes
	changes.response = $("#messageCacheResponseInput").val().trim();
	if (CurrentMessageCacheData.response !== changes.response) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveMessageCacheButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

async function canLeaveMessageCacheTab(leaveMessage = "") {
	if (ManageMessageCacheType == null) return true;

	const cacheChanges = CheckMessageCacheTabHasChanges(false);
	if (cacheChanges.hasChanges) {
		const confirmDiscardChangesDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in message cache.${leaveMessage}`,
			confirmText: "Discard",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "modal-lg",
		});

		const confirmDiscardChangesResult = await confirmDiscardChangesDialog.show();
		if (!confirmDiscardChangesResult) {
			return false;
		}
	}

	return true;
}

function ValidateMessageCacheTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Validate query
	const query = $("#messageCacheQueryInput").val().trim();
	if (!query || query.length === 0) {
		validated = false;
		errors.push("Message query is required.");
		if (!onlyRemove) {
			$("#messageCacheQueryInput").addClass("is-invalid");
		}
	} else {
		$("#messageCacheQueryInput").removeClass("is-invalid");
	}

	// Validate response
	const response = $("#messageCacheResponseInput").val().trim();
	if (!response || response.length === 0) {
		validated = false;
		errors.push("Message response is required.");
		if (!onlyRemove) {
			$("#messageCacheResponseInput").addClass("is-invalid");
		}
	} else {
		$("#messageCacheResponseInput").removeClass("is-invalid");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

// AUDIO RELATED FUNCTIONS

// Audio Group Functions
function resetOrClearAudioGroupManager() {
	$("#audioGroupNameInput").val("");
	$("#audioGroupDefaultExpiryInput").val("24");

	audioGroupManagerTab.find(".is-invalid").removeClass("is-invalid");
	saveAudioGroupButton.prop("disabled", true);

	const audioTable = audioCacheTable.find("tbody");
	audioTable.empty();
	audioTable.append('<tr tr-type="none-notice"><td colspan="2">No audio queries found</td></tr>');
}

function ShowAudioGroupManagerTab() {
	cacheInnerTab.removeClass("show");
	audioGroupsListTab.removeClass("show");
	setTimeout(() => {
		cacheInnerTab.addClass("d-none");
		audioGroupsListTab.addClass("d-none");

		audioGroupBreadcrumb.removeClass("d-none");
		audioGroupManagerTab.removeClass("d-none");
		setTimeout(() => {
			audioGroupBreadcrumb.addClass("show");
			audioGroupManagerTab.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowAudioGroupsListTab() {
	audioGroupBreadcrumb.removeClass("show");
	audioGroupManagerTab.removeClass("show");
	setTimeout(() => {
		audioGroupBreadcrumb.addClass("d-none");
		audioGroupManagerTab.addClass("d-none");

		cacheInnerTab.removeClass("d-none");
		audioGroupsListTab.removeClass("d-none");
		setTimeout(() => {
			cacheInnerTab.addClass("show");
			audioGroupsListTab.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function createDefaultAudioGroupObject() {
	const object = {
		name: "",
		audios: {},
	};

	// Initialize audios for each language
	BusinessFullData.businessData.languages.forEach((language) => {
		object.audios[language] = [];
	});

	return object;
}

function createAudioGroupTableElement(audioGroup) {
	return `
        <tr tr-type="audio-group" data-id="${audioGroup.id}">
            <td>${audioGroup.name}</td>
            <td>
                <button class="btn btn-info btn-sm" button-type="editAudioGroupCache" group-id="${audioGroup.id}">
                    <i class="fa-regular fa-pen-to-square"></i>
                </button>
                <button class="btn btn-danger btn-sm" button-type="deleteAudioGroupCache" group-id="${audioGroup.id}">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>`;
}

function FillCacheAudioGroup() {
	const audioGroups = BusinessFullData.businessApp.cache.audioGroups;

	const audioGroupsTableBody = audioGroupsTable.find("tbody");
	audioGroupsTableBody.empty();

	if (audioGroups.length === 0) {
		audioGroupsTableBody.append('<tr tr-type="none-notice"><td colspan="2">No audio groups found</td></tr>');
	} else {
		audioGroups.forEach((group) => {
			audioGroupsTableBody.append($(createAudioGroupTableElement(group)));
		});
	}
}

function fillAudioGroupManager(groupData) {
	// Fill general fields
	$("#audioGroupNameInput").val(groupData.name);

	// Get current language audios
	const currentLanguage = audioCacheGroupMultilanguage.getSelectedLanguage().id;

	// Populate audios table in List tab
	const audioTable = audioCacheTable.find("tbody");
	audioTable.empty();

	const languageAudios = groupData.audios[currentLanguage] || [];
	if (languageAudios.length === 0) {
		audioTable.append('<tr tr-type="none-notice"><td colspan="2">No audio queries found</td></tr>');
	} else {
		languageAudios.forEach((audio) => {
			audioTable.append($(createAudioCacheTableElement(audio)));
		});
	}

	saveAudioGroupButton.prop("disabled", true);
}

function ValidateAudioGroupTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Validate name
	const groupName = $("#audioGroupNameInput").val().trim();
	if (!groupName || groupName.length === 0) {
		validated = false;
		errors.push("Group name is required.");
		if (!onlyRemove) {
			$("#audioGroupNameInput").addClass("is-invalid");
		}
	} else {
		$("#audioGroupNameInput").removeClass("is-invalid");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

function CheckAudioGroupTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Check name changes
	changes.name = $("#audioGroupNameInput").val().trim();
	if (CurrentAudioGroupData.name !== changes.name) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveAudioGroupButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

async function canLeaveAudioGroupTab(leaveMessage = "") {
	if (ManageAudioGroupType == null) return true;

	const groupChanges = CheckAudioGroupTabHasChanges(false);
	if (groupChanges.hasChanges) {
		const confirmDiscardChangesDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in audio group.${leaveMessage}`,
			confirmText: "Discard",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "modal-lg",
		});

		const confirmDiscardChangesResult = await confirmDiscardChangesDialog.show();
		if (!confirmDiscardChangesResult) {
			return false;
		}
	}

	return true;
}

// Audio Cache Functions
function createDefaultAudioCacheObject() {
	return {
		query: "",
		unusedExpiryHours: 24,
	};
}

function createAudioCacheTableElement(audio) {
	return `
        <tr audio-id="${audio.id}">
            <td>
                <b>${audio.query}</b>
            </td>
            <td>
                <button class="btn btn-info btn-sm" button-type="editAudioCache" audio-id="${audio.id}">
                    <i class="fa-regular fa-pen-to-square"></i>
                </button>
                <button class="btn btn-danger btn-sm" button-type="deleteAudioCache" audio-id="${audio.id}">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `;
}

function resetOrClearAudioCacheManager() {
	$("#audioCacheQueryInput").val("");
	$("#audioCacheExpiryInput").val("24");

	audioCacheManagerTab.find(".is-invalid").removeClass("is-invalid");
	saveAudioCacheButton.prop("disabled", true);
}

function ShowAudioCacheManagerTab() {
	audioGroupManagerTab.removeClass("show");
	audioGroupBreadcrumb.removeClass("show");
	setTimeout(() => {
		audioGroupManagerTab.addClass("d-none");
		audioGroupBreadcrumb.addClass("d-none");

		audioCacheBreadcrumb.removeClass("d-none");
		audioCacheManagerTab.removeClass("d-none");
		setTimeout(() => {
			audioCacheBreadcrumb.addClass("show");
			audioCacheManagerTab.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowAudioCacheListTab() {
	audioCacheBreadcrumb.removeClass("show");
	audioCacheManagerTab.removeClass("show");
	setTimeout(() => {
		audioCacheBreadcrumb.addClass("d-none");
		audioCacheManagerTab.addClass("d-none");

		audioGroupManagerTab.removeClass("d-none");
		audioGroupBreadcrumb.removeClass("d-none");
		setTimeout(() => {
			audioGroupManagerTab.addClass("show");
			audioGroupBreadcrumb.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function fillAudioCacheManager(cacheData) {
	// Fill form fields
	$("#audioCacheQueryInput").val(cacheData.query);
	$("#audioCacheExpiryInput").val(cacheData.unusedExpiryHours);

	saveAudioCacheButton.prop("disabled", true);
}

function ValidateAudioCacheTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Validate query
	const query = $("#audioCacheQueryInput").val().trim();
	if (!query || query.length === 0) {
		validated = false;
		errors.push("Audio query is required.");
		if (!onlyRemove) {
			$("#audioCacheQueryInput").addClass("is-invalid");
		}
	} else {
		$("#audioCacheQueryInput").removeClass("is-invalid");
	}

	// Validate expiry hours
	const expiryHours = $("#audioCacheExpiryInput").val();
	if (expiryHours && (!Number.isInteger(Number(expiryHours)) || Number(expiryHours) < 1)) {
		validated = false;
		errors.push("Expiry hours must be a positive whole number.");
		if (!onlyRemove) {
			$("#audioCacheExpiryInput").addClass("is-invalid");
		}
	} else {
		$("#audioCacheExpiryInput").removeClass("is-invalid");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

function CheckAudioCacheTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Check query changes
	changes.query = $("#audioCacheQueryInput").val().trim();
	if (CurrentAudioCacheData.query !== changes.query) {
		hasChanges = true;
	}

	// Check expiry hours changes
	changes.unusedExpiryHours = parseInt($("#audioCacheExpiryInput").val()) || 24;
	if (CurrentAudioCacheData.unusedExpiryHours !== changes.unusedExpiryHours) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveAudioCacheButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

async function canLeaveAudioCacheTab(leaveMessage = "") {
	if (ManageAudioCacheType == null) return true;

	const cacheChanges = CheckAudioCacheTabHasChanges(false);
	if (cacheChanges.hasChanges) {
		const confirmDiscardChangesDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in audio cache.${leaveMessage}`,
			confirmText: "Discard",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "modal-lg",
		});

		const confirmDiscardChangesResult = await confirmDiscardChangesDialog.show();
		if (!confirmDiscardChangesResult) {
			return false;
		}
	}

	return true;
}

// EMBEDDING RELATED FUNCTIONS

// Embedding Group Functions
function resetOrClearEmbeddingGroupManager() {
	$("#embeddingGroupNameInput").val("");

	embeddingGroupManagerTab.find(".is-invalid").removeClass("is-invalid");
	saveEmbeddingGroupButton.prop("disabled", true);

	const embeddingTable = embeddingCacheTable.find("tbody");
	embeddingTable.empty();
	embeddingTable.append('<tr tr-type="none-notice"><td colspan="2">No embedding queries found</td></tr>');
}

function ShowEmbeddingGroupManagerTab() {
	cacheInnerTab.removeClass("show");
	embeddingGroupsListTab.removeClass("show");
	setTimeout(() => {
		cacheInnerTab.addClass("d-none");
		embeddingGroupsListTab.addClass("d-none");

		embeddingGroupBreadcrumb.removeClass("d-none");
		embeddingGroupManagerTab.removeClass("d-none");
		setTimeout(() => {
			embeddingGroupBreadcrumb.addClass("show");
			embeddingGroupManagerTab.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowEmbeddingGroupsListTab() {
	embeddingGroupBreadcrumb.removeClass("show");
	embeddingGroupManagerTab.removeClass("show");
	setTimeout(() => {
		embeddingGroupBreadcrumb.addClass("d-none");
		embeddingGroupManagerTab.addClass("d-none");

		cacheInnerTab.removeClass("d-none");
		embeddingGroupsListTab.removeClass("d-none");
		setTimeout(() => {
			cacheInnerTab.addClass("show");
			embeddingGroupsListTab.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function createDefaultEmbeddingGroupObject() {
	const object = {
		name: "",
		embeddings: {},
	};

	BusinessFullData.businessData.languages.forEach((language) => {
		object.embeddings[language] = [];
	});

	return object;
}

function createEmbeddingGroupTableElement(embeddingGroup) {
	return `
        <tr tr-type="embedding-group" data-id="${embeddingGroup.id}">
            <td>${embeddingGroup.name}</td>
            <td>
                <button class="btn btn-info btn-sm" button-type="editEmbeddingGroupCache" group-id="${embeddingGroup.id}">
                    <i class="fa-regular fa-pen-to-square"></i>
                </button>
                <button class="btn btn-danger btn-sm" button-type="deleteEmbeddingGroupCache" group-id="${embeddingGroup.id}">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>`;
}

function FillCacheEmbeddingGroup() {
	const embeddingGroups = BusinessFullData.businessApp.cache.embeddingGroups || []; // Ensure it exists

	const embeddingGroupsTableBody = embeddingGroupsTable.find("tbody");
	embeddingGroupsTableBody.empty();

	if (embeddingGroups.length === 0) {
		embeddingGroupsTableBody.append('<tr tr-type="none-notice"><td colspan="2">No embedding groups found</td></tr>');
	} else {
		embeddingGroups.forEach((group) => {
			embeddingGroupsTableBody.append($(createEmbeddingGroupTableElement(group)));
		});
	}
}

function fillEmbeddingGroupManager(groupData) {
	$("#embeddingGroupNameInput").val(groupData.name);

	const currentLanguage = embeddingCacheGroupMultilanguage.getSelectedLanguage().id;
	const embeddingTable = embeddingCacheTable.find("tbody");
	embeddingTable.empty();

	const languageEmbeddings = groupData.embeddings[currentLanguage] || [];
	if (languageEmbeddings.length === 0) {
		embeddingTable.append('<tr tr-type="none-notice"><td colspan="2">No embedding queries found</td></tr>');
	} else {
		languageEmbeddings.forEach((embedding) => {
			embeddingTable.append($(createEmbeddingCacheTableElement(embedding)));
		});
	}

	saveEmbeddingGroupButton.prop("disabled", true);
}

function ValidateEmbeddingGroupTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	const groupName = $("#embeddingGroupNameInput").val().trim();
	if (!groupName || groupName.length === 0) {
		validated = false;
		errors.push("Group name is required.");
		if (!onlyRemove) {
			$("#embeddingGroupNameInput").addClass("is-invalid");
		}
	} else {
		$("#embeddingGroupNameInput").removeClass("is-invalid");
	}

	return { validated, errors };
}

function CheckEmbeddingGroupTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	changes.name = $("#embeddingGroupNameInput").val().trim();
	if (CurrentEmbeddingGroupData.name !== changes.name) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveEmbeddingGroupButton.prop("disabled", !hasChanges);
	}

	return { hasChanges, changes };
}

async function canLeaveEmbeddingGroupTab(leaveMessage = "") {
	if (ManageEmbeddingGroupType == null) return true;

	const groupChanges = CheckEmbeddingGroupTabHasChanges(false);
	if (groupChanges.hasChanges) {
		const confirmDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in the embedding group.${leaveMessage}`,
			confirmText: "Discard",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
		});
		return await confirmDialog.show();
	}
	return true;
}

// Embedding Cache Functions
function createDefaultEmbeddingCacheObject() {
	return { query: "" };
}

function createEmbeddingCacheTableElement(embedding) {
	return `
        <tr embedding-id="${embedding.id}">
            <td><b>${embedding.query}</b></td>
            <td>
                <button class="btn btn-info btn-sm" button-type="editEmbeddingCache" embedding-id="${embedding.id}">
                    <i class="fa-regular fa-pen-to-square"></i>
                </button>
                <button class="btn btn-danger btn-sm" button-type="deleteEmbeddingCache" embedding-id="${embedding.id}">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>`;
}

function resetOrClearEmbeddingCacheManager() {
	$("#embeddingCacheQueryInput").val("");
	embeddingCacheManagerTab.find(".is-invalid").removeClass("is-invalid");
	saveEmbeddingCacheButton.prop("disabled", true);
}

function ShowEmbeddingCacheManagerTab() {
	embeddingGroupManagerTab.removeClass("show");
	embeddingGroupBreadcrumb.removeClass("show");
	setTimeout(() => {
		embeddingGroupManagerTab.addClass("d-none");
		embeddingGroupBreadcrumb.addClass("d-none");

		embeddingCacheBreadcrumb.removeClass("d-none");
		embeddingCacheManagerTab.removeClass("d-none");
		setTimeout(() => {
			embeddingCacheBreadcrumb.addClass("show");
			embeddingCacheManagerTab.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowEmbeddingCacheListTab() {
	embeddingCacheBreadcrumb.removeClass("show");
	embeddingCacheManagerTab.removeClass("show");
	setTimeout(() => {
		embeddingCacheBreadcrumb.addClass("d-none");
		embeddingCacheManagerTab.addClass("d-none");

		embeddingGroupManagerTab.removeClass("d-none");
		embeddingGroupBreadcrumb.removeClass("d-none");
		setTimeout(() => {
			embeddingGroupManagerTab.addClass("show");
			embeddingGroupBreadcrumb.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function fillEmbeddingCacheManager(cacheData) {
	$("#embeddingCacheQueryInput").val(cacheData.query);
	saveEmbeddingCacheButton.prop("disabled", true);
}

function ValidateEmbeddingCacheTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	const query = $("#embeddingCacheQueryInput").val().trim();
	if (!query || query.length === 0) {
		validated = false;
		errors.push("Embedding query is required.");
		if (!onlyRemove) {
			$("#embeddingCacheQueryInput").addClass("is-invalid");
		}
	} else {
		$("#embeddingCacheQueryInput").removeClass("is-invalid");
	}
	return { validated, errors };
}

function CheckEmbeddingCacheTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	changes.query = $("#embeddingCacheQueryInput").val().trim();
	if (CurrentEmbeddingCacheData.query !== changes.query) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveEmbeddingCacheButton.prop("disabled", !hasChanges);
	}
	return { hasChanges, changes };
}

async function canLeaveEmbeddingCacheTab(leaveMessage = "") {
	if (ManageEmbeddingCacheType == null) return true;

	const cacheChanges = CheckEmbeddingCacheTabHasChanges(false);
	if (cacheChanges.hasChanges) {
		const confirmDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in the embedding cache.${leaveMessage}`,
			confirmText: "Discard",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
		});
		return await confirmDialog.show();
	}
	return true;
}

function initCacheTab() {
	$(document).ready(() => {
		messageCacheGroupMultilanguage = new MultiLanguageDropdown("messageCacheGroupManagerMultilanguageContainer", BusinessFullLanguagesData, false);
		audioCacheGroupMultilanguage = new MultiLanguageDropdown("audioCacheGroupManagerMultilanguageContainer", BusinessFullLanguagesData, false);
		embeddingCacheGroupMultilanguage = new MultiLanguageDropdown("embeddingCacheGroupManagerMultilanguageContainer", BusinessFullLanguagesData, false);

		function InitMessagesCacheHandlers() {
			// Message Group handlers
			addNewMessageGroupButton.on("click", (event) => {
				event.preventDefault();

				currentMessageGroupName.text("New Group");
				resetOrClearMessageGroupManager();
				ShowMessageGroupManagerTab();

				ManageMessageGroupType = "new";
				CurrentMessageGroupData = createDefaultMessageGroupObject();
			});

			messageGroupsListTab.on("click", "button[button-type='editMessageGroupCache']", (event) => {
				event.preventDefault();

				const groupId = $(event.currentTarget).attr("group-id");

				resetOrClearMessageGroupManager();
				CurrentMessageGroupData = BusinessFullData.businessApp.cache.messageGroups.find((group) => group.id === groupId);

				currentMessageGroupName.text(CurrentMessageGroupData.name);
				fillMessageGroupManager(CurrentMessageGroupData);
				ShowMessageGroupManagerTab();

				ManageMessageGroupType = "edit";
			});

			switchBackToMessageGroupsListTab.on("click", async (event) => {
				event.preventDefault();

				const canLeaveResult = await canLeaveMessageGroupTab();
				if (!canLeaveResult) return;

				ShowMessageGroupsListTab();
				ManageMessageGroupType = null;
			});

			messageGroupManagerTab.on("input change", "input[type='text'], textarea", (event) => {
				if (ManageMessageGroupType == null) return;

				CheckMessageGroupTabHasChanges(true);
			});

			addNewMessageCacheButton.on("click", (event) => {
				event.preventDefault();

				if (ManageMessageGroupType === "new") {
					AlertManager.createAlert({
						type: "warning",
						message: "Please save the message group first before adding messages to it.",
						timeout: 6000,
					});
					return;
				}

				currentMessageCacheName.text("New Message");
				currentMessageGroupNameInCache.text(CurrentMessageGroupData.name);

				resetOrClearMessageCacheManager();
				ShowMessageCacheManagerTab();

				ManageMessageCacheType = "new";
				CurrentMessageCacheData = createDefaultMessageCacheObject();
			});

			messageCacheGroupMultilanguage.onLanguageChange((language) => {
				if (!CurrentMessageGroupData) return;

				const messageTable = messageCacheTable.find("tbody");
				messageTable.empty();

				const languageMessages = CurrentMessageGroupData.messages[language.id] || [];
				if (languageMessages.length === 0) {
					messageTable.append('<tr tr-type="none-notice"><td colspan="2">No message queries found</td></tr>');
				} else {
					languageMessages.forEach((message) => {
						messageTable.append($(createMessageCacheTableElement(message)));
					});
				}
			});

			saveMessageGroupButton.on("click", async (event) => {
				event.preventDefault();

				if (IsSavingMessageGroupTab) return;

				const validationResult = ValidateMessageGroupTab(false);
				if (!validationResult.validated) {
					AlertManager.createAlert({
						type: "danger",
						message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
						timeout: 6000,
					});
					return;
				}

				const groupChanges = CheckMessageGroupTabHasChanges(false);
				if (!groupChanges.hasChanges) {
					return;
				}

				saveMessageGroupButton.prop("disabled", true);
				const saveButtonSpinner = saveMessageGroupButton.find(".spinner-border");
				saveButtonSpinner.removeClass("d-none");

				IsSavingMessageGroupTab = true;

				const formData = new FormData();
				formData.append("changes", JSON.stringify(groupChanges.changes));
				formData.append("postType", ManageMessageGroupType);

				if (ManageMessageGroupType === "edit") {
					formData.append("existingGroupId", CurrentMessageGroupData.id);
				}

				SaveBusinessContextMessageGroup(
					formData,
					(saveResponse) => {
						CurrentMessageGroupData = saveResponse.data;

						currentMessageGroupName.text(CurrentMessageGroupData.name);

						if (ManageMessageGroupType === "new") {
							BusinessFullData.businessApp.cache.messageGroups.push(CurrentMessageGroupData);
							messageGroupsTable.find("tbody tr[tr-type='none-notice']").remove();
							messageGroupsTable.find("tbody").append($(createMessageGroupTableElement(CurrentMessageGroupData)));
						} else {
							const groupIndex = BusinessFullData.businessApp.cache.messageGroups.findIndex((g) => g.id === CurrentMessageGroupData.id);
							if (groupIndex !== -1) {
								BusinessFullData.businessApp.cache.messageGroups[groupIndex] = CurrentMessageGroupData;
							}

							messageGroupsTable.find(`tbody tr[data-id="${CurrentMessageGroupData.id}"]`).replaceWith($(createMessageGroupTableElement(CurrentMessageGroupData)));
						}

						ManageMessageGroupType = "edit";

						saveMessageGroupButton.prop("disabled", true);
						saveButtonSpinner.addClass("d-none");

						IsSavingMessageGroupTab = false;

						AlertManager.createAlert({
							type: "success",
							message: `Message group ${ManageMessageGroupType === "new" ? "added" : "updated"} successfully.`,
							timeout: 6000,
						});
					},
					(saveError, isUnsuccessful) => {
						AlertManager.createAlert({
							type: "danger",
							message: "Error occurred while saving message group. Check browser console for logs.",
							timeout: 6000,
						});

						console.log("Error occurred while saving message group: ", saveError);

						saveMessageGroupButton.prop("disabled", false);
						saveButtonSpinner.addClass("d-none");

						IsSavingMessageGroupTab = false;
					},
				);
			});

			// Message Cache handlers
			messageCacheListTab.on("click", "button[button-type='editMessageCache']", (event) => {
				event.preventDefault();

				const cacheId = $(event.currentTarget).attr("cache-id");

				resetOrClearMessageCacheManager();
				CurrentMessageCacheData = CurrentMessageGroupData.messages.find((cache) => cache.id === cacheId);

				currentMessageCacheName.text(CurrentMessageCacheData.query);
				fillMessageCacheManager(CurrentMessageCacheData); // We'll implement this next
				ShowMessageCacheManagerTab();

				ManageMessageCacheType = "edit";
			});

			messageCacheTable.on("click", "button[button-type='editMessageCache']", (event) => {
				event.preventDefault();

				const messageId = $(event.currentTarget).attr("message-id");
				const currentLanguage = messageCacheGroupMultilanguage.getSelectedLanguage().id;

				resetOrClearMessageCacheManager();
				CurrentMessageCacheData = CurrentMessageGroupData.messages[currentLanguage].find((message) => message.id === messageId);

				currentMessageCacheName.text(CurrentMessageCacheData.query);
				currentMessageGroupNameInCache.text(CurrentMessageGroupData.name);

				fillMessageCacheManager(CurrentMessageCacheData);
				ShowMessageCacheManagerTab();

				ManageMessageCacheType = "edit";
			});

			switchBackToMessageCacheListTab.on("click", async (event) => {
				event.preventDefault();

				const canLeaveResult = await canLeaveMessageCacheTab();
				if (!canLeaveResult) return;

				ShowMessageCacheListTab();
				ManageMessageCacheType = null;
			});

			messageCacheManagerTab.on("input change", "input[type='text'], textarea, input[type='checkbox']", (event) => {
				if (ManageMessageCacheType == null) return;

				CheckMessageCacheTabHasChanges(true);
			});

			saveMessageCacheButton.on("click", async (event) => {
				event.preventDefault();

				if (IsSavingMessageCacheTab) return;

				const validationResult = ValidateMessageCacheTab(false);
				if (!validationResult.validated) {
					AlertManager.createAlert({
						type: "danger",
						message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
						timeout: 6000,
					});
					return;
				}

				const cacheChanges = CheckMessageCacheTabHasChanges(false);
				if (!cacheChanges.hasChanges) {
					return;
				}

				saveMessageCacheButton.prop("disabled", true);
				const saveButtonSpinner = saveMessageCacheButton.find(".spinner-border");
				saveButtonSpinner.removeClass("d-none");

				IsSavingMessageCacheTab = true;

				const formData = new FormData();
				formData.append("changes", JSON.stringify(cacheChanges.changes));
				formData.append("postType", ManageMessageCacheType);
				formData.append("groupId", CurrentMessageGroupData.id);
				formData.append("language", messageCacheGroupMultilanguage.getSelectedLanguage().id);

				if (ManageMessageCacheType === "edit") {
					formData.append("existingCacheId", CurrentMessageCacheData.id);
				}

				SaveBusinessContextMessageCache(
					formData,
					(saveResponse) => {
						CurrentMessageCacheData = saveResponse.data;

						// Update the messages array for current language
						const currentLanguage = messageCacheGroupMultilanguage.getSelectedLanguage().id;

						if (ManageMessageCacheType === "new") {
							if (!CurrentMessageGroupData.messages[currentLanguage]) {
								CurrentMessageGroupData.messages[currentLanguage] = [];
							}
							CurrentMessageGroupData.messages[currentLanguage].push(CurrentMessageCacheData);

							// Update table
							messageCacheTable.find("tbody tr[tr-type='none-notice']").remove();
							messageCacheTable.find("tbody").append($(createMessageCacheTableElement(CurrentMessageCacheData)));
						} else {
							const messageIndex = CurrentMessageGroupData.messages[currentLanguage].findIndex((m) => m.id === CurrentMessageCacheData.id);
							if (messageIndex !== -1) {
								CurrentMessageGroupData.messages[currentLanguage][messageIndex] = CurrentMessageCacheData;
							}

							messageCacheTable.find(`tbody tr[message-id="${CurrentMessageCacheData.id}"]`).replaceWith($(createMessageCacheTableElement(CurrentMessageCacheData)));
						}

						currentMessageCacheName.text(CurrentMessageCacheData.query);
						ManageMessageCacheType = "edit";

						saveMessageCacheButton.prop("disabled", true);
						saveButtonSpinner.addClass("d-none");

						IsSavingMessageCacheTab = false;

						AlertManager.createAlert({
							type: "success",
							message: `Message cache ${ManageMessageCacheType === "new" ? "added" : "updated"} successfully.`,
							timeout: 6000,
						});
					},
					(saveError, isUnsuccessful) => {
						AlertManager.createAlert({
							type: "danger",
							message: "Error occurred while saving message cache. Check browser console for logs.",
							timeout: 6000,
						});

						console.log("Error occurred while saving message cache: ", saveError);

						saveMessageCacheButton.prop("disabled", false);
						saveButtonSpinner.addClass("d-none");

						IsSavingMessageCacheTab = false;
					},
				);
			});
		}
		InitMessagesCacheHandlers();

		function InitAudioCacheHandlers() {
			// Audio Group handlers
			addNewAudioGroupButton.on("click", (event) => {
				event.preventDefault();

				currentAudioGroupName.text("New Group");
				resetOrClearAudioGroupManager();
				ShowAudioGroupManagerTab();

				ManageAudioGroupType = "new";
				CurrentAudioGroupData = createDefaultAudioGroupObject();
			});

			audioGroupsListTab.on("click", "button[button-type='editAudioGroupCache']", (event) => {
				event.preventDefault();

				const groupId = $(event.currentTarget).attr("group-id");

				resetOrClearAudioGroupManager();
				CurrentAudioGroupData = BusinessFullData.businessApp.cache.audioGroups.find((group) => group.id === groupId);

				currentAudioGroupName.text(CurrentAudioGroupData.name);
				fillAudioGroupManager(CurrentAudioGroupData);
				ShowAudioGroupManagerTab();

				ManageAudioGroupType = "edit";
			});

			switchBackToAudioGroupsListTab.on("click", async (event) => {
				event.preventDefault();

				const canLeaveResult = await canLeaveAudioGroupTab();
				if (!canLeaveResult) return;

				ShowAudioGroupsListTab();
				ManageAudioGroupType = null;
			});

			audioGroupManagerTab.on("input change", "input[type='text']", (event) => {
				if (ManageAudioGroupType == null) return;

				CheckAudioGroupTabHasChanges(true);
			});

			saveAudioGroupButton.on("click", async (event) => {
				event.preventDefault();

				if (IsSavingAudioGroupTab) return;

				const validationResult = ValidateAudioGroupTab(false);
				if (!validationResult.validated) {
					AlertManager.createAlert({
						type: "danger",
						message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
						timeout: 6000,
					});
					return;
				}

				const groupChanges = CheckAudioGroupTabHasChanges(false);
				if (!groupChanges.hasChanges) {
					return;
				}

				saveAudioGroupButton.prop("disabled", true);
				const saveButtonSpinner = saveAudioGroupButton.find(".spinner-border");
				saveButtonSpinner.removeClass("d-none");

				IsSavingAudioGroupTab = true;

				const formData = new FormData();
				formData.append("changes", JSON.stringify(groupChanges.changes));
				formData.append("postType", ManageAudioGroupType);

				if (ManageAudioGroupType === "edit") {
					formData.append("existingGroupId", CurrentAudioGroupData.id);
				}

				SaveBusinessContextAudioGroup(
					formData,
					(saveResponse) => {
						CurrentAudioGroupData = saveResponse.data;

						currentAudioGroupName.text(CurrentAudioGroupData.name);

						if (ManageAudioGroupType === "new") {
							BusinessFullData.businessApp.cache.audioGroups.push(CurrentAudioGroupData);
							audioGroupsTable.find("tbody tr[tr-type='none-notice']").remove();
							audioGroupsTable.find("tbody").append($(createAudioGroupTableElement(CurrentAudioGroupData)));
						} else {
							const groupIndex = BusinessFullData.businessApp.cache.audioGroups.findIndex((g) => g.id === CurrentAudioGroupData.id);
							if (groupIndex !== -1) {
								BusinessFullData.businessApp.cache.audioGroups[groupIndex] = CurrentAudioGroupData;
							}

							audioGroupsTable.find(`tbody tr[data-id="${CurrentAudioGroupData.id}"]`).replaceWith($(createAudioGroupTableElement(CurrentAudioGroupData)));
						}

						ManageAudioGroupType = "edit";

						saveAudioGroupButton.prop("disabled", true);
						saveButtonSpinner.addClass("d-none");

						IsSavingAudioGroupTab = false;

						AlertManager.createAlert({
							type: "success",
							message: `Audio group ${ManageAudioGroupType === "new" ? "added" : "updated"} successfully.`,
							timeout: 6000,
						});
					},
					(saveError, isUnsuccessful) => {
						AlertManager.createAlert({
							type: "danger",
							message: "Error occurred while saving audio group. Check browser console for logs.",
							timeout: 6000,
						});

						console.log("Error occurred while saving audio group: ", saveError);

						saveAudioGroupButton.prop("disabled", false);
						saveButtonSpinner.addClass("d-none");

						IsSavingAudioGroupTab = false;
					},
				);
			});

			audioCacheGroupMultilanguage.onLanguageChange((language) => {
				if (!CurrentAudioGroupData) return;

				const audioTable = audioCacheTable.find("tbody");
				audioTable.empty();

				const languageAudios = CurrentAudioGroupData.audios[language.id] || [];
				if (languageAudios.length === 0) {
					audioTable.append('<tr tr-type="none-notice"><td colspan="2">No audio queries found</td></tr>');
				} else {
					languageAudios.forEach((audio) => {
						audioTable.append($(createAudioCacheTableElement(audio)));
					});
				}
			});

			// Audio Cache handlers
			addNewAudioCacheButton.on("click", (event) => {
				event.preventDefault();

				if (ManageAudioGroupType === "new") {
					AlertManager.createAlert({
						type: "warning",
						message: "Please save the audio group first before adding audios to it.",
						timeout: 6000,
					});
					return;
				}

				currentAudioCacheName.text("New Audio");
				currentAudioGroupNameInCache.text(CurrentAudioGroupData.name);

				resetOrClearAudioCacheManager();
				ShowAudioCacheManagerTab();

				ManageAudioCacheType = "new";
				CurrentAudioCacheData = createDefaultAudioCacheObject();
			});

			audioCacheTable.on("click", "button[button-type='editAudioCache']", (event) => {
				event.preventDefault();

				const audioId = $(event.currentTarget).attr("audio-id");
				const currentLanguage = audioCacheGroupMultilanguage.getSelectedLanguage().id;

				resetOrClearAudioCacheManager();
				CurrentAudioCacheData = CurrentAudioGroupData.audios[currentLanguage].find((audio) => audio.id === audioId);

				currentAudioCacheName.text(CurrentAudioCacheData.query);
				currentAudioGroupNameInCache.text(CurrentAudioGroupData.name);

				fillAudioCacheManager(CurrentAudioCacheData);
				ShowAudioCacheManagerTab();

				ManageAudioCacheType = "edit";
			});

			switchBackToAudioCacheListTab.on("click", async (event) => {
				event.preventDefault();

				const canLeaveResult = await canLeaveAudioCacheTab();
				if (!canLeaveResult) return;

				ShowAudioCacheListTab();
				ManageAudioCacheType = null;
			});

			audioCacheManagerTab.on("input change", "input[type='text'], input[type='number']", (event) => {
				if (ManageAudioCacheType == null) return;

				CheckAudioCacheTabHasChanges(true);
			});

			saveAudioCacheButton.on("click", async (event) => {
				event.preventDefault();

				if (IsSavingAudioCacheTab) return;

				const validationResult = ValidateAudioCacheTab(false);
				if (!validationResult.validated) {
					AlertManager.createAlert({
						type: "danger",
						message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
						timeout: 6000,
					});
					return;
				}

				const cacheChanges = CheckAudioCacheTabHasChanges(false);
				if (!cacheChanges.hasChanges) {
					return;
				}

				saveAudioCacheButton.prop("disabled", true);
				const saveButtonSpinner = saveAudioCacheButton.find(".spinner-border");
				saveButtonSpinner.removeClass("d-none");

				IsSavingAudioCacheTab = true;

				const formData = new FormData();
				formData.append("changes", JSON.stringify(cacheChanges.changes));
				formData.append("postType", ManageAudioCacheType);
				formData.append("groupId", CurrentAudioGroupData.id);
				formData.append("language", audioCacheGroupMultilanguage.getSelectedLanguage().id);

				if (ManageAudioCacheType === "edit") {
					formData.append("existingCacheId", CurrentAudioCacheData.id);
				}

				SaveBusinessContextAudioCache(
					formData,
					(saveResponse) => {
						CurrentAudioCacheData = saveResponse.data;

						// Update the audios array for current language
						const currentLanguage = audioCacheGroupMultilanguage.getSelectedLanguage().id;

						if (ManageAudioCacheType === "new") {
							if (!CurrentAudioGroupData.audios[currentLanguage]) {
								CurrentAudioGroupData.audios[currentLanguage] = [];
							}
							CurrentAudioGroupData.audios[currentLanguage].push(CurrentAudioCacheData);

							// Update table
							audioCacheTable.find("tbody tr[tr-type='none-notice']").remove();
							audioCacheTable.find("tbody").append($(createAudioCacheTableElement(CurrentAudioCacheData)));
						} else {
							const audioIndex = CurrentAudioGroupData.audios[currentLanguage].findIndex((a) => a.id === CurrentAudioCacheData.id);
							if (audioIndex !== -1) {
								CurrentAudioGroupData.audios[currentLanguage][audioIndex] = CurrentAudioCacheData;
							}

							audioCacheTable.find(`tbody tr[audio-id="${CurrentAudioCacheData.id}"]`).replaceWith($(createAudioCacheTableElement(CurrentAudioCacheData)));
						}

						currentAudioCacheName.text(CurrentAudioCacheData.query);
						ManageAudioCacheType = "edit";

						saveAudioCacheButton.prop("disabled", true);
						saveButtonSpinner.addClass("d-none");

						IsSavingAudioCacheTab = false;

						AlertManager.createAlert({
							type: "success",
							message: `Audio cache ${ManageAudioCacheType === "new" ? "added" : "updated"} successfully.`,
							timeout: 6000,
						});
					},
					(saveError, isUnsuccessful) => {
						AlertManager.createAlert({
							type: "danger",
							message: "Error occurred while saving audio cache. Check browser console for logs.",
							timeout: 6000,
						});

						console.log("Error occurred while saving audio cache: ", saveError);

						saveAudioCacheButton.prop("disabled", false);
						saveButtonSpinner.addClass("d-none");

						IsSavingAudioCacheTab = false;
					},
				);
			});
		}
		InitAudioCacheHandlers();

		function InitEmbeddingsCacheHandlers() {
			// Embedding Group handlers
			addNewEmbeddingGroupButton.on("click", (event) => {
				event.preventDefault();

				currentEmbeddingGroupName.text("New Group");
				resetOrClearEmbeddingGroupManager();
				ShowEmbeddingGroupManagerTab();

				ManageEmbeddingGroupType = "new";
				CurrentEmbeddingGroupData = createDefaultEmbeddingGroupObject();
			});

			embeddingGroupsListTab.on("click", "button[button-type='editEmbeddingGroupCache']", (event) => {
				event.preventDefault();
				const groupId = $(event.currentTarget).attr("group-id");

				resetOrClearEmbeddingGroupManager();
				CurrentEmbeddingGroupData = BusinessFullData.businessApp.cache.embeddingGroups.find((g) => g.id === groupId);

				currentEmbeddingGroupName.text(CurrentEmbeddingGroupData.name);
				fillEmbeddingGroupManager(CurrentEmbeddingGroupData);
				ShowEmbeddingGroupManagerTab();

				ManageEmbeddingGroupType = "edit";
			});

			// Use the correct selector for the back link
			embeddingGroupBreadcrumb.on("click", "#switchBackToEmbeddingGroupsListTab", async (event) => {
				event.preventDefault();

				const canLeaveResult = await canLeaveEmbeddingGroupTab();
				if (!canLeaveResult) return;

				ShowEmbeddingGroupsListTab();
				ManageEmbeddingGroupType = null;
			});

			embeddingGroupManagerTab.on("input change", "input[type='text']", (event) => {
				if (ManageEmbeddingGroupType == null) return;
				CheckEmbeddingGroupTabHasChanges(true);
			});

			saveEmbeddingGroupButton.on("click", async (event) => {
				event.preventDefault();
				if (IsSavingEmbeddingGroupTab) return;

				const validation = ValidateEmbeddingGroupTab(false);
				if (!validation.validated) {
					AlertManager.createAlert({ type: "danger", message: `Validation failed:<br>${validation.errors.join("<br>")}` });
					return;
				}
				if (!CheckEmbeddingGroupTabHasChanges(false).hasChanges) return;

				saveEmbeddingGroupButton.prop("disabled", true);
				IsSavingEmbeddingGroupTab = true;

				const formData = new FormData();
				formData.append("changes", JSON.stringify(CheckEmbeddingGroupTabHasChanges(false).changes));
				formData.append("postType", ManageEmbeddingGroupType);
				if (ManageEmbeddingGroupType === "edit") {
					formData.append("existingGroupId", CurrentEmbeddingGroupData.id);
				}

				SaveBusinessContextEmbeddingGroup(formData,
					(response) => {
						CurrentEmbeddingGroupData = response.data;
						currentEmbeddingGroupName.text(CurrentEmbeddingGroupData.name);

						if (ManageEmbeddingGroupType === "new") {
							if (!BusinessFullData.businessApp.cache.embeddingGroups) BusinessFullData.businessApp.cache.embeddingGroups = [];
							BusinessFullData.businessApp.cache.embeddingGroups.push(CurrentEmbeddingGroupData);
							embeddingGroupsTable.find("tbody tr[tr-type='none-notice']").remove();
							embeddingGroupsTable.find("tbody").append($(createEmbeddingGroupTableElement(CurrentEmbeddingGroupData)));
						} else {
							const index = BusinessFullData.businessApp.cache.embeddingGroups.findIndex((g) => g.id === CurrentEmbeddingGroupData.id);
							if (index !== -1) BusinessFullData.businessApp.cache.embeddingGroups[index] = CurrentEmbeddingGroupData;
							embeddingGroupsTable.find(`tbody tr[data-id="${CurrentEmbeddingGroupData.id}"]`).replaceWith($(createEmbeddingGroupTableElement(CurrentEmbeddingGroupData)));
						}

						ManageEmbeddingGroupType = "edit";
						saveEmbeddingGroupButton.prop("disabled", true);
						IsSavingEmbeddingGroupTab = false;
						AlertManager.createAlert({ type: "success", message: `Embedding group saved successfully.` });
					},
					(error) => {
						AlertManager.createAlert({ type: "danger", message: "Error saving embedding group." });
						console.error("Error saving embedding group:", error);
						saveEmbeddingGroupButton.prop("disabled", false);
						IsSavingEmbeddingGroupTab = false;
					}
				);
			});

			embeddingCacheGroupMultilanguage.onLanguageChange((language) => {
				if (!CurrentEmbeddingGroupData) return;
				const tableBody = embeddingCacheTable.find("tbody");
				tableBody.empty();
				const embeddings = CurrentEmbeddingGroupData.embeddings[language.id] || [];
				if (embeddings.length === 0) {
					tableBody.append('<tr tr-type="none-notice"><td colspan="2">No embedding queries found</td></tr>');
				} else {
					embeddings.forEach((emb) => tableBody.append($(createEmbeddingCacheTableElement(emb))));
				}
			});

			// Embedding Cache handlers
			addNewEmbeddingCacheButton.on("click", (event) => {
				event.preventDefault();
				if (ManageEmbeddingGroupType === "new") {
					AlertManager.createAlert({ type: "warning", message: "Please save the group first." });
					return;
				}
				currentEmbeddingCacheName.text("New Embedding");
				currentEmbeddingGroupNameInCache.text(CurrentEmbeddingGroupData.name);
				resetOrClearEmbeddingCacheManager();
				ShowEmbeddingCacheManagerTab();
				ManageEmbeddingCacheType = "new";
				CurrentEmbeddingCacheData = createDefaultEmbeddingCacheObject();
			});

			embeddingCacheTable.on("click", "button[button-type='editEmbeddingCache']", (event) => {
				event.preventDefault();
				const embeddingId = $(event.currentTarget).attr("embedding-id");
				const lang = embeddingCacheGroupMultilanguage.getSelectedLanguage().id;

				resetOrClearEmbeddingCacheManager();
				CurrentEmbeddingCacheData = CurrentEmbeddingGroupData.embeddings[lang].find((e) => e.id === embeddingId);
				currentEmbeddingCacheName.text(CurrentEmbeddingCacheData.query);
				currentEmbeddingGroupNameInCache.text(CurrentEmbeddingGroupData.name);
				fillEmbeddingCacheManager(CurrentEmbeddingCacheData);
				ShowEmbeddingCacheManagerTab();
				ManageEmbeddingCacheType = "edit";
			});

			switchBackToEmbeddingCacheListTab.on("click", async (event) => {
				event.preventDefault();
				if (!(await canLeaveEmbeddingCacheTab())) return;
				ShowEmbeddingCacheListTab();
				ManageEmbeddingCacheType = null;
			});

			embeddingCacheManagerTab.on("input change", "input[type='text']", (event) => {
				if (ManageEmbeddingCacheType == null) return;
				CheckEmbeddingCacheTabHasChanges(true);
			});

			saveEmbeddingCacheButton.on("click", async (event) => {
				event.preventDefault();
				if (IsSavingEmbeddingCacheTab) return;

				const validation = ValidateEmbeddingCacheTab(false);
				if (!validation.validated) {
					AlertManager.createAlert({ type: "danger", message: `Validation failed:<br>${validation.errors.join("<br>")}` });
					return;
				}
				if (!CheckEmbeddingCacheTabHasChanges(false).hasChanges) return;

				saveEmbeddingCacheButton.prop("disabled", true);
				IsSavingEmbeddingCacheTab = true;

				const formData = new FormData();
				formData.append("changes", JSON.stringify(CheckEmbeddingCacheTabHasChanges(false).changes));
				formData.append("postType", ManageEmbeddingCacheType);
				formData.append("groupId", CurrentEmbeddingGroupData.id);
				formData.append("language", embeddingCacheGroupMultilanguage.getSelectedLanguage().id);
				if (ManageEmbeddingCacheType === "edit") {
					formData.append("existingCacheId", CurrentEmbeddingCacheData.id);
				}

				SaveBusinessContextEmbeddingCache(formData,
					(response) => {
						CurrentEmbeddingCacheData = response.data;
						const lang = embeddingCacheGroupMultilanguage.getSelectedLanguage().id;

						if (ManageEmbeddingCacheType === "new") {
							if (!CurrentEmbeddingGroupData.embeddings[lang]) CurrentEmbeddingGroupData.embeddings[lang] = [];
							CurrentEmbeddingGroupData.embeddings[lang].push(CurrentEmbeddingCacheData);
							embeddingCacheTable.find("tbody tr[tr-type='none-notice']").remove();
							embeddingCacheTable.find("tbody").append($(createEmbeddingCacheTableElement(CurrentEmbeddingCacheData)));
						} else {
							const index = CurrentEmbeddingGroupData.embeddings[lang].findIndex((e) => e.id === CurrentEmbeddingCacheData.id);
							if (index !== -1) CurrentEmbeddingGroupData.embeddings[lang][index] = CurrentEmbeddingCacheData;
							embeddingCacheTable.find(`tbody tr[embedding-id="${CurrentEmbeddingCacheData.id}"]`).replaceWith($(createEmbeddingCacheTableElement(CurrentEmbeddingCacheData)));
						}

						currentEmbeddingCacheName.text(CurrentEmbeddingCacheData.query);
						ManageEmbeddingCacheType = "edit";
						saveEmbeddingCacheButton.prop("disabled", true);
						IsSavingEmbeddingCacheTab = false;
						AlertManager.createAlert({ type: "success", message: `Embedding cache saved successfully.` });
					},
					(error) => {
						AlertManager.createAlert({ type: "danger", message: "Error saving embedding cache." });
						console.error("Error saving embedding cache:", error);
						saveEmbeddingCacheButton.prop("disabled", false);
						IsSavingEmbeddingCacheTab = false;
					}
				);
			});
		}
		InitEmbeddingsCacheHandlers();

		// Init
		FillCacheMessageGroup();
		FillCacheAudioGroup();
		FillCacheEmbeddingGroup();
	});
}
