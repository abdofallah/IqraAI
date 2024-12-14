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

// Audio Cache
const audioCacheListTab = cacheTab.find("#audioCacheListTab");
const audioCacheManagerTab = cacheTab.find("#audioCacheManagerTab");
const audioCacheBreadcrumb = cacheTab.find("#audioCacheBreadcrumb");
const addNewAudioCacheButton = cacheTab.find("#addNewAudioCacheButton");
const switchBackToAudioCacheListTab = cacheTab.find("#switchBackToAudioCacheListTab");
const currentAudioCacheName = cacheTab.find("#currentAudioCacheName");
const saveAudioCacheButton = cacheTab.find("#saveAudioCacheButton");

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

// Message Group Functions
function resetOrClearMessageGroupManager() {
	$("#messageGroupNameInput").val("");

	messageGroupManagerTab.find(".is-invalid").removeClass("is-invalid");
	saveMessageGroupButton.prop("disabled", true);

	const messageTable = messageCacheTable.find("tbody");
	messageTable.empty();
	messageTable.append('<tr tr-type="none-notice"><td colspan="2">No messages found</td></tr>');
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
		messageTable.append('<tr tr-type="none-notice"><td colspan="2">No messages found</td></tr>');
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
	$("#messageCacheCaseSensitiveCheck").prop("checked", false);

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
		response: "",
		isQueryCaseSensitive: false,
	};
}

function fillMessageCacheManager(cacheData) {
	// Fill form fields
	$("#messageCacheQueryInput").val(cacheData.query);
	$("#messageCacheResponseInput").val(cacheData.response);
	$("#messageCacheCaseSensitiveCheck").prop("checked", cacheData.isQueryCaseSensitive);

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

	// Check case sensitivity changes
	changes.isQueryCaseSensitive = $("#messageCacheCaseSensitiveCheck").is(":checked");
	if (CurrentMessageCacheData.isQueryCaseSensitive !== changes.isQueryCaseSensitive) {
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

// Audio Cache Functions
function resetOrClearAudioCacheManager() {
	$("#audioCacheQueryInput").val("");
	$("#audioCacheExpiryInput").val("");
	$("#audioCacheFileInput").val("");

	audioCacheManagerTab.find(".is-invalid").removeClass("is-invalid");
	saveAudioCacheButton.prop("disabled", true);
}

function ShowAudioCacheManagerTab() {
	audioGroupManagerTab.removeClass("show");
	audioCacheListTab.removeClass("show");
	setTimeout(() => {
		audioGroupManagerTab.addClass("d-none");
		audioCacheListTab.addClass("d-none");

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
		audioCacheListTab.removeClass("d-none");
		setTimeout(() => {
			audioGroupManagerTab.addClass("show");
			audioCacheListTab.addClass("show");
			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function initCacheTab() {
	$(document).ready(() => {
		messageCacheGroupMultilanguage = new MultiLanguageDropdown("messageCacheGroupManagerMultilanguageContainer", BusinessFullLanguagesData, false);

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
				messageTable.append('<tr tr-type="none-notice"><td colspan="2">No messages found</td></tr>');
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

		// Init
		FillCacheMessageGroup();
	});
}
