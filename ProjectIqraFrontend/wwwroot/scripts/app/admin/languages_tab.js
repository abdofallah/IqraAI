var CurrentManageLanguageType = null;
var CurrentManageLanguageData = null;

/** Elements **/
const languagesTab = $("#languages-tab");

const languagesInnerTab = languagesTab.find("#languages-inner-tab");

const languagesManageBreadcrumb = languagesTab.find("#languages-manage-breadcrumb");
const switchBackToLanguagesListTabFromManageTab = languagesManageBreadcrumb.find("#switchBackToLanguagesListTabFromManageTab");
const currentManageLanguageName = languagesManageBreadcrumb.find("#currentManageLanguageName");

const languagesListTableTab = languagesTab.find("#languagesListTableTab");
const addNewLanguageButton = languagesListTableTab.find("#addNewLanguageButton");
const languagesListTable = languagesListTableTab.find("#languagesListTable");

const languagesManageTab = languagesTab.find("#languagesManageTab");
const saveManageLanguagesButton = languagesManageTab.find("#saveManageLanguagesButton");
const manageLanguagesIdInput = languagesManageTab.find("#manageLanguagesIdInput");
const manageLanguagesNameInput = languagesManageTab.find("#manageLanguagesNameInput");
const manageLanguagesLocaleNameInput = languagesManageTab.find("#manageLanguagesLocaleNameInput");
const manageLanguagesDisabledInput = languagesManageTab.find("#manageLanguagesDisabledInput");

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
	languagesInnerTab.removeClass("show");

	setTimeout(() => {
		languagesListTableTab.addClass("d-none");
		languagesInnerTab.addClass("d-none");

		languagesManageTab.removeClass("d-none");
		languagesManageBreadcrumb.removeClass("d-none");

		setTimeout(() => {
			languagesManageTab.addClass("show");
			languagesManageBreadcrumb.addClass("show");
		}, 10);
	}, 300);
}

function ShowLanguagesListTab() {
	languagesManageTab.removeClass("show");
	languagesManageBreadcrumb.removeClass("show");

	setTimeout(() => {
		languagesManageTab.addClass("d-none");
		languagesManageBreadcrumb.addClass("d-none");

		languagesListTableTab.removeClass("d-none");
		languagesInnerTab.removeClass("d-none");

		setTimeout(() => {
			languagesListTableTab.addClass("show");
			languagesInnerTab.addClass("show");
		}, 10);
	}, 300);
}

function ResetAndEmptyLanguagesManageTab(isEdit = false) {
	languagesManageTab.find("input, textarea").val("").removeClass("is-invalid");
	languagesManageTab.find("input[type=checkbox]").prop("checked", true).change();
	saveManageLanguagesButton.prop("disabled", true);

	manageLanguagesIdInput.prop("disabled", isEdit);
}

function CreateDefaultLanguagesDataObject() {
	let result = {
		id: "",
		localeName: "",
		name: "",
		disabledAt: null,
	};

	return result;
}

function CreateLanguagesListTableElement(languagesData) {
	let disabledAt = "-";
	if (languagesData.disabledAt != null) {
		disabledAt = `<span class="badge bg-danger">${languagesData.disabledAt}</span>`;
	}

	let element = $(`<tr>
                <td>${languagesData.id}</td>
                <td>${languagesData.localeName} | ${languagesData.name}</td>
                <td>${disabledAt}</td>
                <td>
                    <button class="btn btn-info btn-sm" language-id="${languagesData.id}" button-type="edit-language">
                        <i class="fa-regular fa-eye"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}

function CheckLanguageManageTabHasChanges(enableDisableButton = true) {
	let changes = {};
	let hasChanges = false;

	changes.id = manageLanguagesIdInput.val();
	if (CurrentManageLanguageData.id != changes.id) {
		hasChanges = true;
	}

	changes.localeName = manageLanguagesLocaleNameInput.val();
	if (CurrentManageLanguageData.localeName != changes.localeName) {
		hasChanges = true;
	}

	changes.name = manageLanguagesNameInput.val();
	if (CurrentManageLanguageData.name != changes.name) {
		hasChanges = true;
	}

	changes.disabled = manageLanguagesDisabledInput.prop("checked");
	if (changes.disabled == (CurrentManageLanguageData.disabledAt == null)) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveManageLanguagesButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function ValidateLanguageManageTabFields(onlyRemove = true) {
	let errors = [];
	let validated = true;

	let languageId = manageLanguagesIdInput.val();
	if (!languageId || languageId.trim().length === 0 || languageId === "") {
		validated = false;
		errors.push("Language id is required and can not be empty.");

		if (!onlyRemove) {
			manageLanguagesIdInput.addClass("is-invalid");
		}
	} else {
		manageLanguagesIdInput.removeClass("is-invalid");
	}

	let languageLocaleName = manageLanguagesLocaleNameInput.val();
	if (!languageLocaleName || languageLocaleName.trim().length === 0 || languageLocaleName === "") {
		validated = false;
		errors.push("Language locale name is required and can not be empty.");

		if (!onlyRemove) {
			manageLanguagesLocaleNameInput.addClass("is-invalid");
		}
	} else {
		manageLanguagesLocaleNameInput.removeClass("is-invalid");
	}

	let languageName = manageLanguagesNameInput.val();
	if (!languageName || languageName.trim().length === 0 || languageName === "") {
		validated = false;
		errors.push("Language name is required and can not be empty.");

		if (!onlyRemove) {
			manageLanguagesNameInput.addClass("is-invalid");
		}
	} else {
		manageLanguagesNameInput.removeClass("is-invalid");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

function FillLanguagesManageTabData(languagesData) {
	manageLanguagesIdInput.val(languagesData.id);
	manageLanguagesNameInput.val(languagesData.name);
	manageLanguagesLocaleNameInput.val(languagesData.localeName);
	manageLanguagesDisabledInput.prop("checked", languagesData.disabledAt != null);
}

/** Initializer **/
$(document).ready(() => {
	languagesListTableTab.on("click", "button[button-type=edit-language]", (event) => {
		event.preventDefault();

		let languageId = $(event.currentTarget).attr("language-id");
		let languageData = CurrentLanguagesList.find((language) => language.id == languageId);

		CurrentManageLanguageType = "edit";
		CurrentManageLanguageData = languageData;

		currentManageLanguageName.text(languageData.name);

		ResetAndEmptyLanguagesManageTab();
		FillLanguagesManageTabData(languageData);
		ShowLanguagesManageTab();
	});

	saveManageLanguagesButton.on("click", (event) => {
		event.preventDefault();

		let validation = ValidateLanguageManageTabFields(false);
		if (!validation.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: "Validation for required fields failed.<br><br>" + validation.errors.join("<br>"),
				timeout: 6000,
			});

			return;
		}

		let changes = CheckLanguageManageTabHasChanges();
		if (!changes.hasChanges) {
			return;
		}

		saveManageLanguagesButton.prop("disabled", true);

		let formData = new FormData();
		formData.append("postType", CurrentManageLanguageType);
		formData.append("languageCode", changes.changes.id);

		formData.append("changes", JSON.stringify(changes.changes));

		SaveLanguagesData(
			formData,
			(saveResponse) => {
				if (saveResponse.success) {
					setTimeout(() => {
						location.reload();
					}, 1);
				}

				saveManageLanguagesButton.prop("disabled", false);
			},
			(saveError, isUnsuccessful) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while saving languages data. Check browser console for logs.",
					timeout: 6000,
				});

				console.log("Error occured while saving languages data: ", saveError);

				saveManageLanguagesButton.prop("disabled", false);
			},
		);
	});

	languagesManageTab.on("change, input", "input, textarea", (event) => {
		CheckLanguageManageTabHasChanges(true);
	});

	switchBackToLanguagesListTabFromManageTab.on("click", (event) => {
		event.preventDefault();

		CurrentManageLanguageType = null;

		ShowLanguagesListTab();
	});

	addNewLanguageButton.on("click", (event) => {
		event.preventDefault();

		CurrentManageLanguageType = "new";
		CurrentManageLanguageData = CreateDefaultLanguagesDataObject();

		ResetAndEmptyLanguagesManageTab(false);

		ShowLanguagesManageTab();
	});

	// Init

	FetchLanguagesFromAPI(
		0,
		100,
		(languagesData) => {
			CurrentLanguagesList = languagesData;

			if (languagesData.length > 0) {
				languagesListTable.find("tbody").empty();

				languagesData.forEach((langaugeData) => {
					languagesListTable.find("tbody").append(CreateLanguagesListTableElement(langaugeData));
				});
			}
		},
		(languagesError, isUnsuccessful) => {
			AlertManager.createAlert({
				type: "danger",
				message: "Error occured while fetching languages. Check browser console for logs.",
				timeout: 5000,
			});

			console.log("Error occured while fetching languages: ", languagesError);
		},
	);
});
