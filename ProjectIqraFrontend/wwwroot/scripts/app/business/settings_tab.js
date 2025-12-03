const DefaultBusinessImgSRC = "/img/logo/logo-light.png";
const DefaultWhiteLabelLogoSRC = "/img/logo/logo-colored-light.png";
const DefaultWhiteLabelFaviconSRC = "/img/logo/logo-colored-light.png";

const IqraBusinessDomain = "iqra.business"; // todo get this dynamically from server

let IsSavingDomainTab = false;
let IsManagerDomainTabOpened = false;
let ManageDomainType = null;
let CurrentManageDomainData = null;

// Elements
const settingsTab = $("#settings-tab");

const settingsSaveButton = settingsTab.find("#settingsSaveButton");
const settingsSaveButtonSpinner = settingsSaveButton.find(".save-button-spinner");

const settingsGeneralBusinessName = settingsTab.find("#settingsGeneralBusinessName");
const settingsGeneralBusinessLogoPreview = settingsTab.find("#settingsGeneralBusinessLogoPreview");
const settingsGeneralBusinessLogo = settingsTab.find("#settingsGeneralBusinessLogo");

const settingsLanguageAddSelect = settingsTab.find("#settingsLanguageAddSelect");
const settingsLanguageAddButton = settingsTab.find("#settingsLanguageAddButton");
const settingsAddedLanguagesList = settingsTab.find("#settingsAddedLanguagesList");

const settingsInnerTabContainer = settingsTab.find("#settings-inner-tab-container");
const settingsInnerGeneralTab = settingsTab.find("#settings-inner-general-tab");

// API Functions
function SaveSettingsChanges(changes, successCallback, errorCallback) {
	$.ajax({
		type: "POST",
		url: `/app/user/business/${CurrentBusinessId}/settings/save`,
		data: changes,
		dataType: "json",
		processData: false,
		contentType: false,
		success: (response) => {
			if (!response.success) {
				errorCallback(response, false);
				return;
			}

			successCallback(response);
		},
		error: (error) => {
			errorCallback(error, true);
		},
	});
}

// Functions
function ValidateSettingsGeneralTabFields(onlyRemove = true) {
	const errors = [];
	let validated = true;

	const businessName = settingsGeneralBusinessName.val();
	if (!businessName || businessName.trim().length === 0 || businessName === "") {
		validated = false;
		errors.push("Business name is required and can not be empty.");

		if (!onlyRemove) {
			settingsGeneralBusinessName.addClass("is-invalid");
		}
	} else {
		settingsGeneralBusinessName.removeClass("is-invalid");
	}

	return {
		validated: validated,
		errors: errors,
	};
}
function CreateSettingsAddedLanguagesElement(data, isDefault = false) {
	const element = $(`
        <tr language-code="${data.id}">
            <td>${data.id}</td>	
            <td>${data.name}</td>
			<td>
				<input type="radio" name="businessDefaultLanguage" value="${data.id}" ${isDefault ? "checked" : ""}>
			</td>
            <td>
                <button language-code="${data.id}" class="btn btn-danger" button-type="settingsLanguageRemove" type="button" ${isDefault ? "disabled" : ""}>
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `);

	return element;
}
function GetSettingsCurrentAddedLanguages() {
	const currentAddedLanguages = [];

	const noneNoticeTr = settingsAddedLanguagesList.find("tr[tr-type=none-notice]");
	if (noneNoticeTr.length > 0) {
		return currentAddedLanguages;
	}

	settingsAddedLanguagesList.find("tbody tr").each((index, element) => {
		currentAddedLanguages.push($(element).attr("language-code"));
	});

	return currentAddedLanguages;
}
function CheckSettingsGeneralTabHasChanges() {
	const changes = {};
	let hasChanges = false;

	if (BusinessFullData.businessData.name !== settingsGeneralBusinessName.val()) {
		hasChanges = true;
		changes.name = settingsGeneralBusinessName.val();
	}

	if (settingsGeneralBusinessLogo[0].files.length > 0) {
		hasChanges = true;
		changes.logo = settingsGeneralBusinessLogo[0].files[0];
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}
function CheckSettingsLanguagesTabHasChanges() {
	let hasChanges = false;
	let changes = {
		languages: GetSettingsCurrentAddedLanguages(),
        defaultLanguage: settingsAddedLanguagesList.find("input[name=businessDefaultLanguage]:checked").val(),
	};

	if (changes.languages.length !== BusinessFullData.businessData.languages.length) {
		hasChanges = true;
	}

	if (changes.defaultLanguage !== BusinessDefaultLanguage) {
		hasChanges = true;
    }

	if (hasChanges === true)
	{
		let addedCount = 0;
		let removedCount = 0;
		let remainedCount = 0;

		for (let i = 0; i < BusinessFullData.businessData.languages.length; i++) {
			const oldLanguage = BusinessFullData.businessData.languages[i];

			if (changes.languages.includes(oldLanguage)) {
				remainedCount++;
			} else {
				removedCount++;
			}
		}

		for (let i = 0; i < changes.languages.length; i++) {
			const newLanguage = changes.languages[i];

			if (!BusinessFullData.businessData.languages.includes(newLanguage)) {
				addedCount++;
			}
		}

		if (addedCount > 0 || removedCount > 0) {
			hasChanges = true;
		}
	}	

	return {
		hasChanges,
		changes
	};
}
function CheckIfSettingsHasChanges(enableDisableButton = true) {
	const generalTabHasChanges = CheckSettingsGeneralTabHasChanges();
	const languageTabHasChanges = CheckSettingsLanguagesTabHasChanges();

	const hasChanges = generalTabHasChanges.hasChanges || languageTabHasChanges.hasChanges;

	if (enableDisableButton) {
		if (hasChanges) {
			settingsSaveButton.prop("disabled", false);
		} else {
			settingsSaveButton.prop("disabled", true);
		}
	}

	const result = {
		hasChanges: hasChanges,
		changes: {},
	};

	if (generalTabHasChanges.hasChanges) {
		result.changes.general = generalTabHasChanges.changes;
	}

	if (languageTabHasChanges.hasChanges) {
		result.changes.languages = languageTabHasChanges.changes;
	}

	return result;
}
function FillSettingsTab() {
	function FillSettingsGeneralTab() {
		settingsGeneralBusinessName.val(BusinessFullData.businessData.name);

		if (settingsGeneralBusinessLogo[0].files.length > 0) {
			settingsGeneralBusinessLogo[0].files = new DataTransfer().files;
		}
		if (BusinessFullData.businessData.logoUrl && BusinessFullData.businessData.logoUrl != null) {
			settingsGeneralBusinessLogoPreview.attr("src", BusinessFullData.businessData.logoUrl);
		} else {
			settingsGeneralBusinessLogoPreview.attr("src", DefaultBusinessImgSRC);
		}
	}
	function FillSettingsLanguagesTab() {
		SpecificationLanguagesListData.forEach((value, index) => {
			if (value.disabledAt != null) {
				return false;
			}

			settingsLanguageAddSelect.append(`<option value="${value.id}">${value.name} | ${value.id}</option>`);
		});

		settingsAddedLanguagesList.find("tbody").empty();
		if (BusinessFullData.businessData.languages.length === 0) {
			settingsAddedLanguagesList.find("tbody").append('<tr tr-type="none-notice"><td colspan="3">No language added yet...</td></tr>');
		} else {
			BusinessFullData.businessData.languages.forEach((value, index) => {
				const countryCodeLanguage = SpecificationLanguagesListData.find((data, index) => {
					return data.id === value;
				});

				const element = CreateSettingsAddedLanguagesElement(countryCodeLanguage, (countryCodeLanguage.id === BusinessDefaultLanguage));
				settingsAddedLanguagesList.append(element);

				settingsLanguageAddSelect.find(`option[value="${value}"]`).remove();
			});
		}
	}

	FillSettingsGeneralTab();
	FillSettingsLanguagesTab();
}

// Init
function initSettingsTab() {
	$(document).ready(() => {
		settingsGeneralBusinessLogoPreview.on("click", (event) => {
			event.preventDefault();
			settingsGeneralBusinessLogo.click();
		});

		settingsGeneralBusinessLogo.on("change", (event) => {
			event.preventDefault();

			const file = settingsGeneralBusinessLogo[0].files[0];
			if (!file) {
				settingsGeneralBusinessLogoPreview.attr("src", BusinessFullData.businessData.logoUrl);
				CheckIfSettingsHasChanges();
				return;
			}

			// check if file is greater than 5mb
			if (file.size > 5 * 1024 * 1024) {
				AlertManager.createAlert({
					type: "danger",
					message: "File size is too large. Maximum size is 5MB.",
					timeout: 6000,
				});

				ettingsGeneralBusinessLogo[0].files = new DataTransfer().files;

				return;
			}

			// validate file type
			if (!file.type.match("image.*")) {
				AlertManager.createAlert({
					type: "danger",
					message: "File type is not supported. Only JPEG, PNG, WEBP and GIF are supported.",
					timeout: 6000,
				});

				ettingsGeneralBusinessLogo[0].files = new DataTransfer().files;

				return;
			}

			const reader = new FileReader();

			reader.onload = (event) => {
				settingsGeneralBusinessLogoPreview.attr("src", event.target.result);
			};

			reader.readAsDataURL(file);

			CheckIfSettingsHasChanges();
		});

		settingsGeneralBusinessName.on("input", (event) => {
			ValidateSettingsGeneralTabFields(true);
			CheckIfSettingsHasChanges();
		});

		settingsLanguageAddSelect.on("change", (event) => {
			const currentValue = settingsLanguageAddSelect.val();

			if (!currentValue || currentValue === null || currentValue === "none") {
				settingsLanguageAddButton.prop("disabled", true);
			} else {
				settingsLanguageAddButton.prop("disabled", false);
			}
		});

		settingsLanguageAddButton.on("click", (event) => {
			event.preventDefault();

			const selectedValue = settingsLanguageAddSelect.val();
			const countryCodeLanguage = SpecificationLanguagesListData.find((value, index) => {
				return value.id === selectedValue;
			});

			const noNoticeTr = settingsAddedLanguagesList.find("tbody").find("tr[tr-type=none-notice]");
			if (noNoticeTr.length !== 0) {
				noNoticeTr.remove();
			}

			settingsAddedLanguagesList.find("tbody").append(CreateSettingsAddedLanguagesElement(countryCodeLanguage));

			settingsLanguageAddSelect.val("none");
			settingsLanguageAddButton.prop("disabled", true);

			settingsLanguageAddSelect.find(`option[value="${selectedValue}"]`).remove();

			CheckIfSettingsHasChanges();
		});

		settingsAddedLanguagesList.on("change", "input[name=businessDefaultLanguage]", (event) => {
			const selectedValue = $(event.currentTarget).val();

			var alreadyDisabledButton = settingsAddedLanguagesList.find(`[button-type="settingsLanguageRemove"]:disabled`);
            if (alreadyDisabledButton.length > 0) {
                alreadyDisabledButton.prop("disabled", false);
            }

			var selectedRemoveButton = settingsAddedLanguagesList.find(`[button-type="settingsLanguageRemove"][language-code="${selectedValue}"]`);
            if (selectedRemoveButton.length > 0) {
                selectedRemoveButton.prop("disabled", true);
			}

			CheckIfSettingsHasChanges();
		});

		settingsAddedLanguagesList.on("click", 'button[button-type="settingsLanguageRemove"]', async (event) => {
			event.preventDefault();
			event.stopPropagation();

			const languageCode = $(event.currentTarget).attr("language-code");

			const languageData = SpecificationLanguagesListData.find((value, index) => {
				return value.id === languageCode;
			});

			if (BusinessFullData.businessData.languages.includes(languageCode)) {
				const confirmDeleteDialog = new BootstrapConfirmDialog({
					title: "Confirm Delete Language",
					message: `Are you sure you want to delete the language <b>${languageData.name}</b>?<br><br>Deleting the language will remove all references to this language from context, tools, agents, and other components while automatically re-publishing inbound call routings without this language.<br><br><b>This action cannot be undone.</b>`,
					confirmText: "Delete",
					cancelText: "Cancel",
					confirmButtonClass: "btn-danger",
					modalClass: "modal-lg",
				});

				const confirmDeleteDialogResult = await confirmDeleteDialog.show();

				if (!confirmDeleteDialogResult) {
					return;
				}
			}

			settingsAddedLanguagesList.find(`tr[language-code="${languageCode}"]`).remove();

			settingsLanguageAddSelect.append(`<option value="${languageData.id}">${languageData.name} | ${languageData.id}</option>`);

			if (settingsAddedLanguagesList.find("tbody").find("tr").length === 0) {
				settingsAddedLanguagesList.find("tbody").append('<tr tr-type="none-notice"><td colspan="3">No language added yet...</td></tr>');
			}

			CheckIfSettingsHasChanges();
		});

		settingsSaveButton.on("click", (event) => {
			event.preventDefault();

			const generalTabValidation = ValidateSettingsGeneralTabFields(false);
			if (!generalTabValidation.validated) {
				AlertManager.createAlert({
					type: "danger",
					message: `Validation for required fields failed.<br><br>${generalTabValidation.errors.join("<br>")}`,
					timeout: 6000,
				});

				return;
			}

			const changes = CheckIfSettingsHasChanges(false).changes;
			if (changes.languages?.languages) {
				if (changes.languages.languages.length === 0) {
					AlertManager.createAlert({
						type: "danger",
						message: "You must have atleast one language in order to save settings.",
						timeout: 6000,
					});

					return;
				}
			}

			const formData = new FormData();
			if (changes.general) {
				if (changes.general.name) {
					formData.append("general.name", changes.general.name);
				}

				if (changes.general.logo) {
					formData.append("general.logo", changes.general.logo);
				}
			}

			if (changes.languages) {
				formData.append("languagesTab", JSON.stringify(changes.languages));
			}

			if (settingsAddedLanguagesList.find("tbody").find("tr").length === 0) {
				AlertManager.createAlert({
					type: "danger",
					message: "You must have atleast one language added in order to save settings.",
					timeout: 6000,
				});
				return;
			}

			settingsSaveButton.prop("disabled", true);
			settingsSaveButtonSpinner.removeClass("d-none");

			SaveSettingsChanges(
				formData,
				(saveResponse) => {
					if (saveResponse.success) {
						setTimeout(() => {
							location.reload();
						}, 1);
					}

					settingsSaveButton.prop("disabled", false);
					settingsSaveButtonSpinner.addClass("d-none");
				},
				(saveError, isUnsuccessful) => {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occured while saving business settings data. Check browser console for logs.",
						timeout: 6000,
					});

					console.error("Error occured while saving business settings data: ", saveError);

					settingsSaveButton.prop("disabled", false);
					settingsSaveButtonSpinner.addClass("d-none");
				},
			);
		});

		// Initialize
		FillSettingsTab();
	});
}
