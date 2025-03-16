const DAYS = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]; // TODO get directly from server as specification

/** Dynamic Variables **/
let ContextTabMultiLanguageDropdown = null;

// Context Branding State
let CurrentContextBrandingData = null;
const CurrentContextBrandingNameMultiLangData = {};
const CurrentContextBrandingCountryMultiLangData = {};
const CurrentContextBrandingEmailMultiLangData = {};
const CurrentContextBrandingPhoneMultiLangData = {};
const CurrentContextBrandingWebsiteMultiLangData = {};
const CurrentContextBrandingOtherInformationMultiLangData = {};

// Context Branch State
let CurrentContextBranchData = null;
let ManageContextBranchType = null; // new or edit
let CurrentContextBranchNameMultiLangData = {};
let CurrentContextBranchAddressMultiLangData = {};
let CurrentContextBranchPhoneMultiLangData = {};
let CurrentContextBranchEmailMultiLangData = {};
let CurrentContextBranchWebsiteMultiLangData = {};
let CurrentContextBranchOtherInformationMultiLangData = {};
let CurrentContextBranchTeamMultiLangData = [];

// Context Service State
let CurrentContextServiceData = null;
let ManageContextServiceType = null; // new or edit
let CurrentContextServiceNameMultiLangData = {};
let CurrentContextServiceShortDescriptionMultiLangData = {};
let CurrentContextServiceLongDescriptionMultiLangData = {};
let CurrentContextServiceOtherInformationMultiLangData = {};

// Context Product State
let CurrentContextProductData = null;
let ManageContextProductType = null; // new or edit
let CurrentContextProductNameMultiLangData = {};
let CurrentContextProductShortDescriptionMultiLangData = {};
let CurrentContextProductLongDescriptionMultiLangData = {};
let CurrentContextProductOtherInformationMultiLangData = {};

// Saving States
let IsSavingContextBrandingTab = false;
let IsSavingContextBranchTab = false;
let IsSavingContextServiceTab = false;
let IsSavingContextProductTab = false;

/** Element Variables  **/
const contextTabTooltipTriggerList = document.querySelectorAll('#context-tab [data-bs-toggle="tooltip"]');
const contextTabTooltipList = [...contextTabTooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

// Context
const contextTab = $("#context-tab");

const contextTabHeader = contextTab.find(".inner-header-container");
const contextTabButtonHeader = contextTabHeader.find("#contextTabButtonHeader");

const saveContextBrandingButton = contextTabHeader.find("#saveContextBrandingButton");
const saveContextBrandingButtonSpinner = saveContextBrandingButton.find(".spinner-border");

// Branding
const brandingTab = contextTab.find("#context-inner-branding");

const brandingBrandNameInput = brandingTab.find("#brandingBrandNameInput");
const brandingBrandCountryInput = brandingTab.find("#brandingBrandCountryInput");
const brandingGlobalContactInput = brandingTab.find("#brandingGlobalContactInput");
const brandingGlobalPhoneInput = brandingTab.find("#brandingGlobalPhoneInput");
const brandingGlobalWebsiteInput = brandingTab.find("#brandingGlobalWebsiteInput");

const addNewBrandInformationButton = contextTab.find("#addNewBrandInformation");
const brandingOtherInformationList = contextTab.find("#brandingOtherInformationList");

// Branches
// List Tab
const branchesListTab = contextTab.find("#branchesListTab");

const addNewBranchButton = branchesListTab.find("#addNewBranchButton");
const branchesTable = branchesListTab.find("#branchesTable");

// Manager Tab
const currentBranchName = contextTabHeader.find("#currentBranchName");
const switchBackToBranchesTab = contextTabHeader.find("#switchBackToBranchesTab");
const branchesManageTabHeader = contextTabHeader.find("#contextBranchesManageTabHeader");
const branchesManagerTabButtonHeader = contextTabHeader.find("#contextBranchesManagerTabButtonHeader");
const saveContextBranchesButton = contextTabHeader.find("#saveContextBranchesButton");
const saveContextBranchesButtonSpinner = saveContextBranchesButton.find(".spinner-border");

const branchesManagerTab = contextTab.find("#branchesManagerTab");

const editBranchNameInput = branchesManagerTab.find("#editBranchNameInput");
const editBranchAddressInput = branchesManagerTab.find("#editBranchAddressInput");
const editBranchPhoneInput = branchesManagerTab.find("#editBranchPhoneInput");
const editBranchEmailInput = branchesManagerTab.find("#editBranchEmailInput");
const editBranchWebsiteInput = branchesManagerTab.find("#editBranchWebsiteInput");

const addContextBranchInformationButton = branchesManagerTab.find("#addContextBranchInformation");
const contextBranchInformationList = branchesManagerTab.find("#contextBranchInformationList");

const editBranchOpeningHoursInputsList = branchesManagerTab.find("#editBranchOpeningHoursInputs");

const editBranchAddTeamMember = branchesManagerTab.find("#editBranchAddTeamMember");
const editBranchTeamInputsList = branchesManagerTab.find("#editBranchTeamInputs");

// Services
// List Tab
const contextServicesListTab = contextTab.find("#contextServicesListTab");

const addNewServiceButton = contextServicesListTab.find("#addNewServiceButton");
const contextServicesTable = contextServicesListTab.find("#contextServicesTable");

// Manager Tab
const switchBackToContextServicesTab = contextTabHeader.find("#switchBackToContextServicesTab");
const currentContextServiceName = contextTabHeader.find("#currentContextServiceName");
const servicesManageTabHeader = contextTabHeader.find("#contextServicesManageTabHeader");
const saveContextServicesButton = contextTabHeader.find("#saveContextServicesButton");
const saveContextServicesButtonSpinner = saveContextServicesButton.find(".spinner-border");

const contextServicesManagerTab = contextTab.find("#contextServicesManagerTab");

const inputContextServiceName = contextServicesManagerTab.find("#inputContextServiceName");
const inputContextServiceShortDescription = contextServicesManagerTab.find("#inputContextServiceShortDescription");
const inputContextServiceFullDescription = contextServicesManagerTab.find("#inputContextServiceFullDescription");

const linkContextServiceBranchSelect = contextServicesManagerTab.find("#linkContextServiceBranchSelect");
const linkContextServiceBranchButton = contextServicesManagerTab.find("#linkContextServiceBranch");
const linkedContextServiceBranchesList = contextServicesManagerTab.find("#linkedContextServiceBranchesList");

const linkContextServiceProductSelect = contextServicesManagerTab.find("#linkContextServiceProductSelect");
const linkContextServiceProductButton = contextServicesManagerTab.find("#linkContextServiceProduct");
const linkedContextServiceProductsList = contextServicesManagerTab.find("#linkedContextServiceProductsList");

const addNewContextServiceInformationButton = contextServicesManagerTab.find("#addNewContextServiceInformation");
const contextServiceInformationList = contextServicesManagerTab.find("#contextServiceInformationList");

// Products
// List Tab
const contextProductsListTab = contextTab.find("#contextProductsListTab");

const addNewProductButton = contextProductsListTab.find("#addNewProductButton");
const contextProductsTable = contextProductsListTab.find("#contextProductsTable");

// ManagerTab
const switchBackToContextProductsTab = contextTabHeader.find("#switchBackToContextProductsTab");
const currentContextProductName = contextTabHeader.find("#currentContextProductName");
const productsManageTabHeader = contextTabHeader.find("#contextProductsManageTabHeader");
const saveContextProductsButton = contextTabHeader.find("#saveContextProductsButton");
const saveContextProductsButtonSpinner = saveContextProductsButton.find(".spinner-border");

const contextProductsManagerTab = contextTab.find("#contextProductsManagerTab");

const inputContextProductName = contextProductsManagerTab.find("#inputContextProductName");
const inputContextProductShortDescription = contextProductsManagerTab.find("#inputContextProductShortDescription");
const inputContextProductFullDescription = contextProductsManagerTab.find("#inputContextProductFullDescription");

const linkContextProductBranchSelect = contextTab.find("#linkContextProductBranchSelect");
const linkContextProductBranchButton = contextTab.find("#linkContextProductBranch");
const linkedContextProductBranchesList = contextTab.find("#linkedContextProductBranchesList");

const addNewContextProductInformationButton = contextTab.find("#addNewContextProductInformation");
const contextProductInformationList = contextTab.find("#contextProductInformationList");

/** API Functions */
function SaveBusinessContextBranding(formData, onSuccess, onError) {
	$.ajax({
		url: `/app/user/business/${CurrentBusinessId}/context/branding/save`,
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

function SaveBusinessContextBranch(formData, onSuccess, onError) {
	$.ajax({
		url: `/app/user/business/${CurrentBusinessId}/context/branches/save`,
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

function SaveBusinessContextService(formData, onSuccess, onError) {
	$.ajax({
		url: `/app/user/business/${CurrentBusinessId}/context/services/save`,
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

function SaveBusinessContextProduct(formData, onSuccess, onError) {
	$.ajax({
		url: `/app/user/business/${CurrentBusinessId}/context/products/save`,
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

/** Functions **/

function FillContextTab() {
	FillContextBrandingTab();
	FillContextBranchesTab();
	fillContextServicesTab();
	fillContextProductsTab();
}

// TAB | Branding

function CheckContextBrandingTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Name
	changes.name = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.name[language] = CurrentContextBrandingNameMultiLangData[language];
		if (CurrentContextBrandingData.name[language] !== changes.name[language]) {
			hasChanges = true;
		}
	});

	// Country
	changes.country = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.country[language] = CurrentContextBrandingCountryMultiLangData[language];
		if (CurrentContextBrandingData.country[language] !== changes.country[language]) {
			hasChanges = true;
		}
	});

	// Email
	changes.email = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.email[language] = CurrentContextBrandingEmailMultiLangData[language];
		if (CurrentContextBrandingData.email[language] !== changes.email[language]) {
			hasChanges = true;
		}
	});

	// Phone
	changes.phone = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.phone[language] = CurrentContextBrandingPhoneMultiLangData[language];
		if (CurrentContextBrandingData.phone[language] !== changes.phone[language]) {
			hasChanges = true;
		}
	});

	// Website
	changes.website = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.website[language] = CurrentContextBrandingWebsiteMultiLangData[language];
		if (CurrentContextBrandingData.website[language] !== changes.website[language]) {
			hasChanges = true;
		}
	});

	// Other Information - now checked per language
	changes.otherInformation = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		const currentOtherInfo = {};
		const originalOtherInfo = CurrentContextBrandingData.otherInformation[language] || {};

		// Only check current elements if it's the selected language
		if (language === ContextTabMultiLanguageDropdown.getSelectedLanguage().id) {
			brandingOtherInformationList.children().each((idx, element) => {
				const currentElement = $(element);
				const key = currentElement.find('[data-type="key"]').val().trim();
				const value = currentElement.find('[data-type="value"]').val().trim();
				if (key) {
					currentOtherInfo[key] = value;
				}
			});
		} else {
			Object.assign(currentOtherInfo, CurrentContextBrandingOtherInformationMultiLangData[language]);
		}

		if (JSON.stringify(originalOtherInfo) !== JSON.stringify(currentOtherInfo)) {
			hasChanges = true;
		}

		changes.otherInformation[language] = currentOtherInfo;
	});

	if (enableDisableButton) {
		saveContextBrandingButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function validateContextBrandingAllMultilanguageElements() {
	BusinessFullData.businessData.languages.forEach((language) => {
		const currentLanguage = SpecificationLanguagesListData.find((d) => d.id === language);
		let isAnyFieldIncomplete = false;

		// Brand Name
		const brandName = CurrentContextBrandingNameMultiLangData[currentLanguage.id];
		if (!brandName || brandName.trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Country
		const brandCountry = CurrentContextBrandingCountryMultiLangData[currentLanguage.id];
		if (!brandCountry || brandCountry.trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Email
		const brandEmail = CurrentContextBrandingEmailMultiLangData[currentLanguage.id];
		if (!brandEmail || brandEmail.trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Phone
		const brandPhone = CurrentContextBrandingPhoneMultiLangData[currentLanguage.id];
		if (!brandPhone || brandPhone.trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Website
		const brandWebsite = CurrentContextBrandingWebsiteMultiLangData[currentLanguage.id];
		if (!brandWebsite || brandWebsite.trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Other Information validation - now language specific
		if (language === ContextTabMultiLanguageDropdown.getSelectedLanguage().id) {
			brandingOtherInformationList.children().each((idx, element) => {
				const currentElement = $(element);
				const key = currentElement.find('[data-type="key"]').val().trim();
				const value = currentElement.find('[data-type="value"]').val().trim();

				if (!key || !value) {
					isAnyFieldIncomplete = true;
				}
			});
		} else {
			const otherInfo = CurrentContextBrandingOtherInformationMultiLangData[language];
			Object.entries(otherInfo || {}).forEach(([key, value]) => {
				if (!key || !value || value.trim() === "") {
					isAnyFieldIncomplete = true;
				}
			});
		}

		// Update language status in dropdown
		ContextTabMultiLanguageDropdown.setLanguageStatus(currentLanguage.id, isAnyFieldIncomplete ? "incomplete" : "complete");
	});
}

function ValidateContextBrandingTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Validate all languages
	BusinessFullData.businessData.languages.forEach((language) => {
		// Brand Name validation
		if (!CurrentContextBrandingNameMultiLangData[language] || CurrentContextBrandingNameMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Brand name for language ${language} is required and cannot be empty.`);

			if (!onlyRemove) {
				brandingBrandNameInput.addClass("is-invalid");
			}
		} else {
			brandingBrandNameInput.removeClass("is-invalid");
		}

		// Country validation
		if (!CurrentContextBrandingCountryMultiLangData[language] || CurrentContextBrandingCountryMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Country for language ${language} is required and cannot be empty.`);

			if (!onlyRemove) {
				brandingBrandCountryInput.addClass("is-invalid");
			}
		} else {
			brandingBrandCountryInput.removeClass("is-invalid");
		}

		// Email validation
		if (!CurrentContextBrandingEmailMultiLangData[language] || CurrentContextBrandingEmailMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Email for language ${language} is required and cannot be empty.`);

			if (!onlyRemove) {
				brandingGlobalContactInput.addClass("is-invalid");
			}
		} else {
			brandingGlobalContactInput.removeClass("is-invalid");
		}

		// Phone validation
		if (!CurrentContextBrandingPhoneMultiLangData[language] || CurrentContextBrandingPhoneMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Phone for language ${language} is required and cannot be empty.`);

			if (!onlyRemove) {
				brandingGlobalPhoneInput.addClass("is-invalid");
			}
		} else {
			brandingGlobalPhoneInput.removeClass("is-invalid");
		}

		// Website validation
		if (!CurrentContextBrandingWebsiteMultiLangData[language] || CurrentContextBrandingWebsiteMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Website for language ${language} is required and cannot be empty.`);

			if (!onlyRemove) {
				brandingGlobalWebsiteInput.addClass("is-invalid");
			}
		} else {
			brandingGlobalWebsiteInput.removeClass("is-invalid");
		}
	});

	if (!validateBrandingOtherInformationKeys()) {
		validated = false;
		errors.push("Duplicate information types found. Please ensure all information types are unique.");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

function updateBrandingOtherInformation(languageId) {
	CurrentContextBrandingOtherInformationMultiLangData[languageId] = {};

	brandingOtherInformationList.children().each((idx, element) => {
		const infoType = $(element).find("input").val().trim();
		const infoValue = $(element).find("textarea").val().trim();

		if (infoType) {
			CurrentContextBrandingOtherInformationMultiLangData[languageId][infoType] = infoValue;
		}
	});
}

function FillContextBrandingTab() {
	// Set current data
	CurrentContextBrandingData = BusinessFullData.businessApp.context.branding;

	// Initialize multilanguage data
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentContextBrandingNameMultiLangData[language] = CurrentContextBrandingData.name[language];
		CurrentContextBrandingCountryMultiLangData[language] = CurrentContextBrandingData.country[language];
		CurrentContextBrandingEmailMultiLangData[language] = CurrentContextBrandingData.email[language];
		CurrentContextBrandingPhoneMultiLangData[language] = CurrentContextBrandingData.phone[language];
		CurrentContextBrandingWebsiteMultiLangData[language] = CurrentContextBrandingData.website[language];
		CurrentContextBrandingOtherInformationMultiLangData[language] = {};

		// Initialize other information for each language
		Object.keys(CurrentContextBrandingData.otherInformation[language] || {}).forEach((key) => {
			CurrentContextBrandingOtherInformationMultiLangData[language][key] = CurrentContextBrandingData.otherInformation[language][key];
		});
	});

	// Fill default language values
	brandingBrandNameInput.val(CurrentContextBrandingData.name[BusinessDefaultLanguage]);
	brandingBrandCountryInput.val(CurrentContextBrandingData.country[BusinessDefaultLanguage]);
	brandingGlobalContactInput.val(CurrentContextBrandingData.email[BusinessDefaultLanguage]);
	brandingGlobalPhoneInput.val(CurrentContextBrandingData.phone[BusinessDefaultLanguage]);
	brandingGlobalWebsiteInput.val(CurrentContextBrandingData.website[BusinessDefaultLanguage]);

	// Fill other information for default language
	fillContextBranchOtherInformationForLanguage(BusinessDefaultLanguage);

	validateContextBrandingAllMultilanguageElements();
	saveContextBrandingButton.prop("disabled", true);
}

function CreateContextBrandingOtherInformationElement() {
	const element = `
		<div class="mt-2">
			<div class="input-group">
				<input type="text" class="form-control" placeholder="Information Type" data-type="key" aria-label="Information Type" value="" style="border-bottom: none; border-bottom-left-radius: 0;">
				<button class="btn btn-danger" button-type="brandingInformationRemove" style="border-bottom: none; border-bottom-right-radius: 0;">
						<i class='fa-regular fa-trash'></i>
				</button>
			</div>
			<textarea class="form-control" placeholder="Information" aria-label="Information" data-type="value" style="border-top-left-radius: 0; border-top-right-radius: 0"></textarea>
		</div>
	`;

	return element;
}

function fillContextBrandOtherInformationForLanguage(language) {
	brandingOtherInformationList.empty();
	Object.entries(CurrentContextBrandingOtherInformationMultiLangData[language] || {}).forEach(([key, value]) => {
		const infoBox = $(CreateContextBrandingOtherInformationElement());
		brandingOtherInformationList.append(infoBox);

		infoBox.find('[data-type="key"]').val(key);
		infoBox.find('[data-type="value"]').val(value);
	});
}

function validateBrandingOtherInformationKeys() {
	const seenKeys = new Set();
	let hasDuplicates = false;

	brandingOtherInformationList.children().each((idx, element) => {
		const currentElement = $(element);
		const keyInput = currentElement.find('[data-type="key"]');
		const key = keyInput.val().trim();

		if (key && seenKeys.has(key)) {
			hasDuplicates = true;
			keyInput.addClass("is-invalid");
		} else {
			keyInput.removeClass("is-invalid");
			if (key) {
				seenKeys.add(key);
			}
		}
	});

	return !hasDuplicates;
}

async function canLeaveContextBrandingTab(leaveMessage = "") {
	if (IsSavingContextBrandingTab) {
		AlertManager.createAlert({
			type: "warning",
			message: "Branding tab is currently being saved. Please wait for the save to finish.",
			enableDismiss: false,
		});
		return false;
	}

	const brandingChanges = CheckContextBrandingTabHasChanges(false);
	if (brandingChanges.hasChanges) {
		const confirmDiscardChangesDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in branding tab.${leaveMessage}`,
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

// TAB | Branches

function resetOrClearContextBranchManager() {
	$("#editBranchNameInput").val("");
	$("#editBranchAddressInput").val("");
	$("#editBranchPhoneInput").val("");
	$("#editBranchEmailInput").val("");
	$("#editBranchWebsiteInput").val("");

	contextBranchInformationList.children().remove();

	DAYS.forEach((day) => {
		editBranchOpeningHoursInputsList.find(`#editBranchOpeningHours${day}Input`).prop("checked", false);
		editBranchOpeningHoursInputsList.find(`[button-type="addBranchWorkingHour"][day-value="${day}"]`).prop("disabled", false);
		editBranchOpeningHoursInputsList.find(`.workingHoursTimingList[day-value="${day}"]`).children().remove();
	});

	editBranchTeamInputsList.children().remove();
	$("#branding-branch-manager-general-tab").click();

	CurrentContextBranchNameMultiLangData = {};
	CurrentContextBranchAddressMultiLangData = {};
	CurrentContextBranchPhoneMultiLangData = {};
	CurrentContextBranchEmailMultiLangData = {};
	CurrentContextBranchWebsiteMultiLangData = {};
	CurrentContextBranchOtherInformationMultiLangData = {};
	CurrentContextBranchTeamMultiLangData = [];

	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentContextBranchNameMultiLangData[language] = "";
		CurrentContextBranchAddressMultiLangData[language] = "";
		CurrentContextBranchPhoneMultiLangData[language] = "";
		CurrentContextBranchEmailMultiLangData[language] = "";
		CurrentContextBranchWebsiteMultiLangData[language] = "";
		CurrentContextBranchOtherInformationMultiLangData[language] = {};
	});

	branchesManagerTab.find(".is-invalid").removeClass("is-invalid");

	saveContextBranchesButton.prop("disabled", true);
}

function ShowContextBranchesManagerTab() {
	branchesListTab.removeClass("show");
	contextTabButtonHeader.removeClass("show");
	setTimeout(() => {
		branchesListTab.addClass("d-none");
		contextTabButtonHeader.addClass("d-none");

		branchesManagerTab.removeClass("d-none");
		branchesManagerTabButtonHeader.removeClass("d-none");
		branchesManageTabHeader.removeClass("d-none");
		saveContextBranchesButton.removeClass("d-none");
		ContextTabMultiLanguageDropdown.$container.removeClass("d-none");
		setTimeout(() => {
			branchesManagerTab.addClass("show");
			branchesManagerTabButtonHeader.addClass("show");
			branchesManageTabHeader.addClass("show");
			saveContextBranchesButton.addClass("show");
            ContextTabMultiLanguageDropdown.$container.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowContextBranchesListTab() {
	branchesManagerTab.removeClass("show");
	branchesManagerTabButtonHeader.removeClass("show");
	branchesManageTabHeader.removeClass("show");
	ContextTabMultiLanguageDropdown.$container.removeClass("show");
	saveContextBranchesButton.removeClass("show");
	setTimeout(() => {
		branchesManagerTab.addClass("d-none");
		branchesManagerTabButtonHeader.addClass("d-none");
		branchesManageTabHeader.addClass("d-none");
		saveContextBranchesButton.addClass("d-none");
		ContextTabMultiLanguageDropdown.$container.addClass("d-none");

		branchesListTab.removeClass("d-none");
		contextTabButtonHeader.removeClass("d-none");
		setTimeout(() => {
			branchesListTab.addClass("show");
			contextTabButtonHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function createContextBranchesTableElement(branch) {
	const branchRow = `
		<tr branch-id="${branch.id}">
			<td>
				<b>${branch.general.name[BusinessDefaultLanguage]}</b>
			</td>
			<td>
				<button class="btn btn-info btn-sm" branch-id="${branch.id}" button-type="editBranch">
					<i class="fa-regular fa-pen-to-square"></i>
				</button>
				<button class="btn btn-danger btn-sm" branch-id="${branch.id}" button-type="deleteBranch">
					<i class="fa-regular fa-trash"></i>
				</button>
			</td>
		</tr>
	`;

	return branchRow;
}

function FillContextBranchesTab() {
	const branchesData = BusinessFullData.businessApp.context.branches || [];
	branchesTable.find("tbody").empty();

	if (branchesData.length === 0) {
		branchesTable.find("tbody").append("<tr tr-type='none-notice'><td colspan='2'>No branches found</td></tr>");
	} else {
		branchesData.forEach((branch) => {
			branchesTable.find("tbody").append($(createContextBranchesTableElement(branch)));
		});
	}
}

function CheckContextBranchTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Check general section
	function checkGeneralTab() {
		changes.general = {
			name: {},
			address: {},
			phone: {},
			email: {},
			website: {},
			otherInformation: {},
		};

		BusinessFullData.businessData.languages.forEach((language) => {
			// Name changes
			changes.general.name[language] = CurrentContextBranchNameMultiLangData[language];
			if (CurrentContextBranchData.general.name[language] !== changes.general.name[language]) {
				hasChanges = true;
			}

			// Address changes
			changes.general.address[language] = CurrentContextBranchAddressMultiLangData[language];
			if (CurrentContextBranchData.general.address[language] !== changes.general.address[language]) {
				hasChanges = true;
			}

			// Phone changes
			changes.general.phone[language] = CurrentContextBranchPhoneMultiLangData[language];
			if (CurrentContextBranchData.general.phone[language] !== changes.general.phone[language]) {
				hasChanges = true;
			}

			// Email changes
			changes.general.email[language] = CurrentContextBranchEmailMultiLangData[language];
			if (CurrentContextBranchData.general.email[language] !== changes.general.email[language]) {
				hasChanges = true;
			}

			// Website changes
			changes.general.website[language] = CurrentContextBranchWebsiteMultiLangData[language];
			if (CurrentContextBranchData.general.website[language] !== changes.general.website[language]) {
				hasChanges = true;
			}

			// Other Information changes
			changes.general.otherInformation[language] = CurrentContextBranchOtherInformationMultiLangData[language] || {};
			if (JSON.stringify(CurrentContextBranchData.general.otherInformation[language]) !== JSON.stringify(changes.general.otherInformation[language])) {
				hasChanges = true;
			}
		});
	}
	checkGeneralTab();

	// Working Hours changes
	function checkWorkingHoursTab() {
		changes.workingHours = {};
		DAYS.forEach((day, index) => {
			changes.workingHours[index] = {
				isClosed: $(`#editBranchOpeningHours${day}Input`).is(":checked"),
				timings: [],
			};

			const timingsList = $(`.workingHoursTimingList[day-value="${day}"]`).children();
			timingsList.each((idx, element) => {
				const fromTime = $(element).find('[time-type="from"]').val();
				const toTime = $(element).find('[time-type="to"]').val();
				if (fromTime && toTime) {
					changes.workingHours[index].timings.push([fromTime, toTime]);
				}
			});

			if (JSON.stringify(CurrentContextBranchData.workingHours[index]) !== JSON.stringify(changes.workingHours[index])) {
				hasChanges = true;
			}
		});
	}
	checkWorkingHoursTab();

	// Team changes
	changes.team = CurrentContextBranchTeamMultiLangData;
	if (JSON.stringify(CurrentContextBranchData.team) !== JSON.stringify(changes.team)) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveContextBranchesButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function validateContextBranchAllMultilanguageElements() {
	BusinessFullData.businessData.languages.forEach((language) => {
		const currentLanguage = SpecificationLanguagesListData.find((d) => d.id === language);
		let isAnyFieldIncomplete = false;

		// Validate general section fields
		if (!CurrentContextBranchNameMultiLangData[language] || CurrentContextBranchNameMultiLangData[language].trim() === "") {
			isAnyFieldIncomplete = true;
		}

		if (!CurrentContextBranchAddressMultiLangData[language] || CurrentContextBranchAddressMultiLangData[language].trim() === "") {
			isAnyFieldIncomplete = true;
		}

		if (!CurrentContextBranchPhoneMultiLangData[language] || CurrentContextBranchPhoneMultiLangData[language].trim() === "") {
			isAnyFieldIncomplete = true;
		}

		if (!CurrentContextBranchEmailMultiLangData[language] || CurrentContextBranchEmailMultiLangData[language].trim() === "") {
			isAnyFieldIncomplete = true;
		}

		if (!CurrentContextBranchWebsiteMultiLangData[language] || CurrentContextBranchWebsiteMultiLangData[language].trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Validate Other Information
		if (language === ContextTabMultiLanguageDropdown.getSelectedLanguage().id) {
			contextBranchInformationList.children().each((idx, element) => {
				const key = $(element).find("input").val().trim();
				const value = $(element).find("textarea").val().trim();
				if (!key || !value) {
					isAnyFieldIncomplete = true;
				}
			});
		}

		// Update language status in dropdown
		ContextTabMultiLanguageDropdown.setLanguageStatus(currentLanguage.id, isAnyFieldIncomplete ? "incomplete" : "complete");
	});
}

function ValidateContextBranchTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	BusinessFullData.businessData.languages.forEach((language) => {
		// Branch Name validation
		if (!CurrentContextBranchNameMultiLangData[language] || CurrentContextBranchNameMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Branch name for language ${language} is required.`);
			if (!onlyRemove) {
				editBranchNameInput.addClass("is-invalid");
			}
		} else {
			editBranchNameInput.removeClass("is-invalid");
		}

		// Branch Address validation
		if (!CurrentContextBranchAddressMultiLangData[language] || CurrentContextBranchAddressMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Branch address for language ${language} is required.`);
			if (!onlyRemove) {
				editBranchAddressInput.addClass("is-invalid");
			}
		} else {
			editBranchAddressInput.removeClass("is-invalid");
		}

		// Branch Phone validation
		if (!CurrentContextBranchPhoneMultiLangData[language] || CurrentContextBranchPhoneMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Branch phone for language ${language} is required.`);
			if (!onlyRemove) {
				editBranchPhoneInput.addClass("is-invalid");
			}
		} else {
			editBranchPhoneInput.removeClass("is-invalid");
		}

		// Branch Email validation
		if (!CurrentContextBranchEmailMultiLangData[language] || CurrentContextBranchEmailMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Branch email for language ${language} is required.`);
			if (!onlyRemove) {
				editBranchEmailInput.addClass("is-invalid");
			}
		} else {
			// Additional email format validation
			const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
			if (!emailRegex.test(CurrentContextBranchEmailMultiLangData[language].trim())) {
				validated = false;
				errors.push(`Invalid email format for language ${language}.`);
				if (!onlyRemove) {
					editBranchEmailInput.addClass("is-invalid");
				}
			} else {
				editBranchEmailInput.removeClass("is-invalid");
			}
		}

		// Branch Website validation
		if (!CurrentContextBranchWebsiteMultiLangData[language] || CurrentContextBranchWebsiteMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Branch website for language ${language} is required.`);
			if (!onlyRemove) {
				editBranchWebsiteInput.addClass("is-invalid");
			}
		} else {
			editBranchWebsiteInput.removeClass("is-invalid");
		}
	});

	// Validate Working Hours
	let hasWorkingHours = false;
	DAYS.forEach((day) => {
		const isClosed = $(`#editBranchOpeningHours${day}Input`).is(":checked");
		if (!isClosed) {
			const timings = $(`.workingHoursTimingList[day-value="${day}"]`).children();
			if (timings.length > 0) {
				hasWorkingHours = true;

				// Validate each timing entry
				timings.each((idx, element) => {
					const fromTime = $(element).find('[time-type="from"]').val();
					const toTime = $(element).find('[time-type="to"]').val();

					if (!fromTime || !toTime) {
						validated = false;
						errors.push(`Incomplete timing entry for ${day}. Both start and end times are required.`);
						if (!onlyRemove) {
							$(element).find('input[type="time"]').addClass("is-invalid");
						}
					} else if (fromTime >= toTime) {
						validated = false;
						errors.push(`Invalid timing for ${day}: End time must be after start time.`);
						if (!onlyRemove) {
							$(element).find('input[type="time"]').addClass("is-invalid");
						}
					} else {
						$(element).find('input[type="time"]').removeClass("is-invalid");
					}
				});
			}
		}
	});

	if (!hasWorkingHours) {
		validated = false;
		errors.push("At least one working hour timing must be set for open days.");
	}

	// Validate Team Members
	editBranchTeamInputsList.children().each((idx, teamElement) => {
		const teamMember = $(teamElement);
		BusinessFullData.businessData.languages.forEach((language) => {
			const nameInput = teamMember.find('[data-field="name"]');
			const roleInput = teamMember.find('[data-field="role"]');
			const emailInput = teamMember.find('[data-field="email"]');
			const phoneInput = teamMember.find('[data-field="phone"]');

			// Required fields validation
			if (!nameInput.val().trim()) {
				validated = false;
				errors.push(`Team member name is required for language ${language}.`);
				if (!onlyRemove) {
					nameInput.addClass("is-invalid");
				}
			} else {
				nameInput.removeClass("is-invalid");
			}

			if (!roleInput.val().trim()) {
				validated = false;
				errors.push(`Team member role is required for language ${language}.`);
				if (!onlyRemove) {
					roleInput.addClass("is-invalid");
				}
			} else {
				roleInput.removeClass("is-invalid");
			}

			// Email format validation if provided
			if (emailInput.val().trim()) {
				const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
				if (!emailRegex.test(emailInput.val().trim())) {
					validated = false;
					errors.push(`Invalid email format for team member ${nameInput.val() || "unnamed"}.`);
					if (!onlyRemove) {
						emailInput.addClass("is-invalid");
					}
				} else {
					emailInput.removeClass("is-invalid");
				}
			}
		});
	});

	// Validate other information keys for duplicates
	if (!validateBranchOtherInformationKeys()) {
		validated = false;
		errors.push("Duplicate information types found. Please ensure all information types are unique.");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

function updateContextBranchOtherInformation(languageId) {
	CurrentContextBranchOtherInformationMultiLangData[languageId] = {};

	contextBranchInformationList.children().each((idx, element) => {
		const infoType = $(element).find("input").val().trim();
		const infoValue = $(element).find("textarea").val().trim();

		if (infoType) {
			CurrentContextBranchOtherInformationMultiLangData[languageId][infoType] = infoValue;
		}
	});
}

function createDefaultContextBranchesObject() {
	const object = {
		id: null,
		general: {
			name: {},
			address: {},
			phone: {},
			email: {},
			website: {},
			otherInformation: {},
		},
		workingHours: {},
		team: [],
	};

	DAYS.forEach((day, index) => {
		object.workingHours[day] = {
			isClosed: false,
			timings: [],
		};
	});

	BusinessFullData.businessData.languages.forEach((language) => {
		object.general.name[language] = "";
		object.general.address[language] = "";
		object.general.phone[language] = "";
		object.general.email[language] = "";
		object.general.website[language] = "";
		object.general.otherInformation[language] = {};
	});

	return object;
}

function fillContextBranchManager(branchData) {
	// Fill general section
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentContextBranchNameMultiLangData[language] = branchData.general.name[language];
		CurrentContextBranchAddressMultiLangData[language] = branchData.general.address[language];
		CurrentContextBranchPhoneMultiLangData[language] = branchData.general.phone[language];
		CurrentContextBranchEmailMultiLangData[language] = branchData.general.email[language];
		CurrentContextBranchWebsiteMultiLangData[language] = branchData.general.website[language];
		CurrentContextBranchOtherInformationMultiLangData[language] = branchData.general.otherInformation[language] || {};
	});

	// Fill form with default language values
	editBranchNameInput.val(branchData.general.name[BusinessDefaultLanguage]);
	editBranchAddressInput.val(branchData.general.address[BusinessDefaultLanguage]);
	editBranchPhoneInput.val(branchData.general.phone[BusinessDefaultLanguage]);
	editBranchEmailInput.val(branchData.general.email[BusinessDefaultLanguage]);
	editBranchWebsiteInput.val(branchData.general.website[BusinessDefaultLanguage]);

	// Fill working hours
	DAYS.forEach((day, index) => {
		const dayData = branchData.workingHours[index];
		$(`#editBranchOpeningHours${day}Input`).prop("checked", dayData.isClosed);

		const timingsList = $(`.workingHoursTimingList[day-value="${day}"]`);
		timingsList.empty();

		dayData.timings.forEach((timing) => {
			const timingElement = $(createContextBranchWorkingHourElement());
			timingElement.find("[time-type='from']").val(timing[0]);
			timingElement.find("[time-type='to']").val(timing[1]);
			timingsList.append(timingElement);
		});
	});

	// Fill team members
	editBranchTeamInputsList.empty();
	branchData.team.forEach((teamMember) => {
		const teamMemberElement = createContextBranchTeamElement(teamMember);
		editBranchTeamInputsList.append(teamMemberElement);
		CurrentContextBranchTeamMultiLangData.push(JSON.parse(JSON.stringify(teamMember)));
	});

	validateContextBranchAllMultilanguageElements();
	saveContextBranchesButton.prop("disabled", true);
}

function createContextBranchTeamElement(teamMember = null) {
	const element = $(`
        <div class="col-12 col-md-6 mt-3">
            <div class="editBranchTeamBox">
                <div class="d-flex flex-row gap-2 mb-2">
                    <div class="w-100">
                        <label class="form-label">Name</label>
                        <input type="text" class="form-control" data-field="name" value="${teamMember?.name[BusinessDefaultLanguage] || ""}" placeholder="Member Name">
                    </div>
                    <div class="w-100">
                        <label class="form-label">Role</label>
                        <input type="text" class="form-control" data-field="role" value="${teamMember?.role[BusinessDefaultLanguage] || ""}" placeholder="Member Role">
                    </div>
                </div>
                <div class="d-flex flex-row gap-2 mb-2">
                    <div class="w-100">
                        <label class="form-label">Email</label>
                        <input type="email" class="form-control" data-field="email" value="${teamMember?.email[BusinessDefaultLanguage] || ""}" placeholder="Member Email">
                    </div>
                    <div class="w-100">
                        <label class="form-label">Phone</label>
                        <input type="tel" class="form-control" data-field="phone" value="${teamMember?.phone[BusinessDefaultLanguage] || ""}" placeholder="Member Phone">
                    </div>
                </div>
                <div class="mb-2">
                    <label class="form-label">Information</label>
                    <textarea class="form-control" data-field="information" style="min-height: 70px" placeholder="Member Information">${teamMember?.information[BusinessDefaultLanguage] || ""}</textarea>
                </div>
                <button class="btn btn-danger w-100" button-type="removeEditBranchTeam">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </div>
        </div>
    `);

	return element;
}

function fillContextBranchOtherInformationForLanguage(language) {
	contextBranchInformationList.empty();
	Object.entries(CurrentContextBranchOtherInformationMultiLangData[language] || {}).forEach(([key, value]) => {
		const infoBox = $(CreateContextBrandingOtherInformationElement());
		contextBranchInformationList.append(infoBox);

		infoBox.find('[data-type="key"]').val(key);
		infoBox.find('[data-type="value"]').val(value);
	});
}

function validateBranchOtherInformationKeys() {
	const seenKeys = new Set();
	let hasDuplicates = false;

	contextBranchInformationList.children().each((idx, element) => {
		const currentElement = $(element);
		const keyInput = currentElement.find('[data-type="key"]');
		const key = keyInput.val().trim();

		if (key && seenKeys.has(key)) {
			hasDuplicates = true;
			keyInput.addClass("is-invalid");
		} else {
			keyInput.removeClass("is-invalid");
			if (key) {
				seenKeys.add(key);
			}
		}
	});

	return !hasDuplicates;
}

function createContextBranchOtherInformationElement() {
	const element = `
		<div class="mt-2">
			<div class="input-group">
				<input data-type="key" type="text" class="form-control" placeholder="Information Type" aria-label="Information Type" value="" style="border-bottom: none; border-bottom-left-radius: 0;">
				<button class="btn btn-danger" button-type="contextBranchInformationRemove" style="border-bottom: none; border-bottom-right-radius: 0;">
						<i class='fa-regular fa-trash'></i>
				</button>
			</div>
			<textarea data-type="value" class="form-control" placeholder="Information" aria-label="Information" style="border-top-left-radius: 0; border-top-right-radius: 0"></textarea>
		</div>
	`;

	return element;
}

function createDefaultContextBranchTeamMemberObject() {
	return {
		name: {},
		role: {},
		email: {},
		phone: {},
		information: {},
	};
}

function updateContextBranchTeamMembersData(languageId, index) {
	var teamMemberData = CurrentContextBranchTeamMultiLangData[index];
	var editBoxElement = $(editBranchTeamInputsList.children().get(index));

	teamMemberData.name[languageId] = editBoxElement.find('[data-field="name"]').val();
	teamMemberData.role[languageId] = editBoxElement.find('[data-field="role"]').val();
	teamMemberData.email[languageId] = editBoxElement.find('[data-field="email"]').val();
	teamMemberData.phone[languageId] = editBoxElement.find('[data-field="phone"]').val();
	teamMemberData.information[languageId] = editBoxElement.find('[data-field="information"]').val();
}

function createContextBranchWorkingHourElement() {
	const element = `
		<div class="d-flex flex-row mt-1">
			<input type="time" class="form-control" time-type="from" style="border-top-right-radius: 0; border-bottom-right-radius: 0">
			<input type="time" class="form-control" time-type="to" style="border-radius: 0; border-left: none;">
			<button class="btn btn-danger" button-type="removeBranchWorkingHour" style="border-top-left-radius: 0; border-bottom-left-radius: 0">
				<i class="fa-regular fa-trash"></i>
			</button>
		</div>
	`;

	return element;
}

async function canLeaveContextBranchesTab(leaveMessage = "") {
	if (ManageContextBranchType == null) return true;

	if (IsSavingContextBranchTab) {
		AlertManager.createAlert({
			type: "warning",
			message: "Branch manager tab is currently being saved. Please wait for the save to finish.",
			enableDismiss: false,
		});
		return false;
	}

	const branchManagerChanges = CheckContextBranchTabHasChanges(false);
	if (branchManagerChanges.hasChanges) {
		const confirmDiscardChangesDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in branch manager tab.${leaveMessage}`,
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

// TAB | Services

function resetOrClearContextServiceManager() {
	$("#inputContextServiceName").val("");
	$("#inputContextServiceShortDescription").val("");
	$("#inputContextServiceFullDescription").val("");

	linkContextServiceBranchSelect.children().remove();
	linkedContextServiceBranchesList.children().remove();

	linkContextServiceProductSelect.children().remove();
	linkedContextServiceProductsList.children().remove();

	contextServiceInformationList.children().remove();

	CurrentContextServiceNameMultiLangData = {};
	CurrentContextServiceShortDescriptionMultiLangData = {};
	CurrentContextServiceLongDescriptionMultiLangData = {};
	CurrentContextServiceOtherInformationMultiLangData = {};

	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentContextServiceNameMultiLangData[language] = "";
		CurrentContextServiceShortDescriptionMultiLangData[language] = "";
		CurrentContextServiceLongDescriptionMultiLangData[language] = "";
		CurrentContextServiceOtherInformationMultiLangData[language] = {};
	});

	contextServicesManagerTab.find(".is-invalid").removeClass("is-invalid");

	saveContextServicesButton.prop("disabled", true);
}

function ShowContextServicesManagerTab() {
	contextServicesListTab.removeClass("show");
	contextTabButtonHeader.removeClass("show");
	setTimeout(() => {
		contextServicesListTab.addClass("d-none");
		contextTabButtonHeader.addClass("d-none");

		contextServicesManagerTab.removeClass("d-none");
		servicesManageTabHeader.removeClass("d-none");
		saveContextServicesButton.removeClass("d-none");
		setTimeout(() => {
			contextServicesManagerTab.addClass("show");
			servicesManageTabHeader.addClass("show");
			saveContextServicesButton.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowContextServicesListTab() {
	contextServicesManagerTab.removeClass("show");
	servicesManageTabHeader.removeClass("show");
	saveContextServicesButton.removeClass("d-none");
	setTimeout(() => {
		contextServicesManagerTab.addClass("d-none");
		servicesManageTabHeader.addClass("d-none");
		saveContextServicesButton.addClass("d-none");

		contextServicesListTab.removeClass("d-none");
		contextTabButtonHeader.removeClass("d-none");
		setTimeout(() => {
			contextServicesListTab.addClass("show");
			contextTabButtonHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function createDefaultContextServicesObject() {
	const object = {
		name: {},
		shortDescription: {},
		longDescription: {},
		availableAtBranches: [],
		relatedProducts: [],
		otherInformation: {},
	};

	BusinessFullData.businessData.languages.forEach((language) => {
		object.name[language] = "";
		object.shortDescription[language] = "";
		object.longDescription[language] = "";
		object.otherInformation[language] = {};
	});

	return object;
}

function createContextServiceTableElement(service) {
	const serviceRow = `
        <tr service-id="${service.id}">
            <td>
                <b>${service.name[BusinessDefaultLanguage]}</b>
            </td>
            <td>
                <button class="btn btn-info btn-sm" service-id="${service.id}" button-type="editService">
                    <i class="fa-regular fa-pen-to-square"></i>
                </button>
                <button class="btn btn-danger btn-sm" service-id="${service.id}" button-type="deleteService">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `;

	return serviceRow;
}

function createContextServiceOtherInformationElement() {
	const element = `
        <div class="mt-2">
            <div class="input-group">
                <input data-type="key" type="text" class="form-control" placeholder="Information Type" aria-label="Information Type" value="" style="border-bottom: none; border-bottom-left-radius: 0;">
                <button class="btn btn-danger" button-type="contextServiceInformationRemove" style="border-bottom: none; border-bottom-right-radius: 0;">
                    <i class='fa-regular fa-trash'></i>
                </button>
            </div>
            <textarea data-type="value" class="form-control" placeholder="Information" aria-label="Information" style="border-top-left-radius: 0; border-top-right-radius: 0"></textarea>
        </div>
    `;

	return element;
}

function fillContextServiceManager(serviceData) {
	// Fill multilanguage data
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentContextServiceNameMultiLangData[language] = serviceData.name[language];
		CurrentContextServiceShortDescriptionMultiLangData[language] = serviceData.shortDescription[language];
		CurrentContextServiceLongDescriptionMultiLangData[language] = serviceData.longDescription[language];
		CurrentContextServiceOtherInformationMultiLangData[language] = serviceData.otherInformation[language] || {};
	});

	// Fill form with default language values
	inputContextServiceName.val(serviceData.name[BusinessDefaultLanguage]);
	inputContextServiceShortDescription.val(serviceData.shortDescription[BusinessDefaultLanguage]);
	inputContextServiceFullDescription.val(serviceData.longDescription[BusinessDefaultLanguage]);

	// Fill branches list
	linkedContextServiceBranchesList.empty();
	serviceData.availableAtBranches.forEach((branchId) => {
		const branch = BusinessFullData.businessApp.context.branches.find((b) => b.id === branchId);
		if (branch) {
			linkedContextServiceBranchesList.append(`
                <li class="list-group-item" branch-id="${branch.id}">
                    <div class="d-flex flex-row align-items-center justify-content-between">
                        <span>${branch.general.name[BusinessDefaultLanguage]}</span>
                        <button class="btn btn-danger btn-sm" button-type="removeLinkedBranch">
                            <i class="fa-regular fa-trash"></i>
                        </button>
                    </div>
                </li>
            `);
		}
	});

	// Fill products list
	linkedContextServiceProductsList.empty();
	serviceData.relatedProducts.forEach((productId) => {
		const product = BusinessFullData.businessApp.context.products.find((p) => p.id === productId);
		if (product) {
			linkedContextServiceProductsList.append(`
                <li class="list-group-item" product-id="${product.id}">
                    <div class="d-flex flex-row align-items-center justify-content-between">
                        <span>${product.name[BusinessDefaultLanguage]}</span>
                        <button class="btn btn-danger btn-sm" button-type="removeLinkedProduct">
                            <i class="fa-regular fa-trash"></i>
                        </button>
                    </div>
                </li>
            `);
		}
	});

	// Fill other information
	contextServiceInformationList.empty();
	Object.entries(serviceData.otherInformation[BusinessDefaultLanguage] || {}).forEach(([key, value]) => {
		const infoElement = $(createContextServiceOtherInformationElement());
		infoElement.find('[data-type="key"]').val(key);
		infoElement.find('[data-type="value"]').val(value);
		contextServiceInformationList.append(infoElement);
	});

	validateContextServiceAllMultilanguageElements();
	saveContextServicesButton.prop("disabled", true);
}

function validateContextServiceAllMultilanguageElements() {
	BusinessFullData.businessData.languages.forEach((language) => {
		const currentLanguage = SpecificationLanguagesListData.find((d) => d.id === language);
		let isAnyFieldIncomplete = false;

		// Validate Name
		if (!CurrentContextServiceNameMultiLangData[language] || CurrentContextServiceNameMultiLangData[language].trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Validate Short Description
		if (!CurrentContextServiceShortDescriptionMultiLangData[language] || CurrentContextServiceShortDescriptionMultiLangData[language].trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Validate Long Description
		if (!CurrentContextServiceLongDescriptionMultiLangData[language] || CurrentContextServiceLongDescriptionMultiLangData[language].trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Validate Other Information
		if (language === ContextTabMultiLanguageDropdown.getSelectedLanguage().id) {
			contextServiceInformationList.children().each((idx, element) => {
				const key = $(element).find('[data-type="key"]').val().trim();
				const value = $(element).find('[data-type="value"]').val().trim();
				if (!key || !value) {
					isAnyFieldIncomplete = true;
				}
			});
		}

		// Update language status in dropdown
		ContextTabMultiLanguageDropdown.setLanguageStatus(currentLanguage.id, isAnyFieldIncomplete ? "incomplete" : "complete");
	});
}

function ValidateContextServiceTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	BusinessFullData.businessData.languages.forEach((language) => {
		// Name validation
		if (!CurrentContextServiceNameMultiLangData[language] || CurrentContextServiceNameMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Service name for language ${language} is required.`);
			if (!onlyRemove) {
				inputContextServiceName.addClass("is-invalid");
			}
		} else {
			inputContextServiceName.removeClass("is-invalid");
		}

		// Short Description validation
		if (!CurrentContextServiceShortDescriptionMultiLangData[language] || CurrentContextServiceShortDescriptionMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Service short description for language ${language} is required.`);
			if (!onlyRemove) {
				inputContextServiceShortDescription.addClass("is-invalid");
			}
		} else {
			inputContextServiceShortDescription.removeClass("is-invalid");
		}

		// Long Description validation
		if (!CurrentContextServiceLongDescriptionMultiLangData[language] || CurrentContextServiceLongDescriptionMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Service full description for language ${language} is required.`);
			if (!onlyRemove) {
				inputContextServiceFullDescription.addClass("is-invalid");
			}
		} else {
			inputContextServiceFullDescription.removeClass("is-invalid");
		}
	});

	// Validate other information keys for duplicates
	if (!validateServiceOtherInformationKeys()) {
		validated = false;
		errors.push("Duplicate information types found. Please ensure all information types are unique.");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

function validateServiceOtherInformationKeys() {
	const seenKeys = new Set();
	let hasDuplicates = false;

	contextServiceInformationList.children().each((idx, element) => {
		const currentElement = $(element);
		const keyInput = currentElement.find('[data-type="key"]');
		const key = keyInput.val().trim();

		if (key && seenKeys.has(key)) {
			hasDuplicates = true;
			keyInput.addClass("is-invalid");
		} else {
			keyInput.removeClass("is-invalid");
			if (key) {
				seenKeys.add(key);
			}
		}
	});

	return !hasDuplicates;
}

function CheckContextServiceTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Check multilanguage fields
	BusinessFullData.businessData.languages.forEach((language) => {
		// Name changes
		if (!changes.name) changes.name = {};
		changes.name[language] = CurrentContextServiceNameMultiLangData[language];
		if (CurrentContextServiceData.name[language] !== changes.name[language]) {
			hasChanges = true;
		}

		// Short Description changes
		if (!changes.shortDescription) changes.shortDescription = {};
		changes.shortDescription[language] = CurrentContextServiceShortDescriptionMultiLangData[language];
		if (CurrentContextServiceData.shortDescription[language] !== changes.shortDescription[language]) {
			hasChanges = true;
		}

		// Long Description changes
		if (!changes.longDescription) changes.longDescription = {};
		changes.longDescription[language] = CurrentContextServiceLongDescriptionMultiLangData[language];
		if (CurrentContextServiceData.longDescription[language] !== changes.longDescription[language]) {
			hasChanges = true;
		}

		// Other Information changes
		if (!changes.otherInformation) changes.otherInformation = {};
		changes.otherInformation[language] = CurrentContextServiceOtherInformationMultiLangData[language] || {};
		if (JSON.stringify(CurrentContextServiceData.otherInformation[language]) !== JSON.stringify(changes.otherInformation[language])) {
			hasChanges = true;
		}
	});

	// Check branches
	changes.availableAtBranches = [];
	linkedContextServiceBranchesList.children().each((idx, element) => {
		const branchId = $(element).attr("branch-id");
		changes.availableAtBranches.push(branchId);
	});
	if (JSON.stringify(CurrentContextServiceData.availableAtBranches) !== JSON.stringify(changes.availableAtBranches)) {
		hasChanges = true;
	}

	// Check products
	changes.relatedProducts = [];
	linkedContextServiceProductsList.children().each((idx, element) => {
		const productId = $(element).attr("product-id");
		changes.relatedProducts.push(productId);
	});
	if (JSON.stringify(CurrentContextServiceData.relatedProducts) !== JSON.stringify(changes.relatedProducts)) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveContextServicesButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function updateContextServiceOtherInformation(languageId) {
	CurrentContextServiceOtherInformationMultiLangData[languageId] = {};

	contextServiceInformationList.children().each((idx, element) => {
		const infoType = $(element).find('[data-type="key"]').val().trim();
		const infoValue = $(element).find('[data-type="value"]').val().trim();

		if (infoType) {
			CurrentContextServiceOtherInformationMultiLangData[languageId][infoType] = infoValue;
		}
	});
}

async function canLeaveContextServicesTab(leaveMessage = "") {
	if (ManageContextServiceType == null) return true;

	if (IsSavingContextServiceTab) {
		AlertManager.createAlert({
			type: "warning",
			message: "Service manager tab is currently being saved. Please wait for the save to finish.",
			enableDismiss: false,
		});
		return false;
	}

	const serviceManagerChanges = CheckContextServiceTabHasChanges(false);
	if (serviceManagerChanges.hasChanges) {
		const confirmDiscardChangesDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in service manager tab.${leaveMessage}`,
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

function fillContextServicesTab() {
	const servicesData = BusinessFullData.businessApp.context.services || [];
	contextServicesTable.find("tbody").empty();

	if (servicesData.length === 0) {
		contextServicesTable.find("tbody").append("<tr tr-type='none-notice'><td colspan='2'>No services found</td></tr>");
	} else {
		servicesData.forEach((service) => {
			contextServicesTable.find("tbody").append($(createContextServiceTableElement(service)));
		});
	}
}

// TAB | Products

function resetOrClearContextProductManager() {
	$("#inputContextProductName").val("");
	$("#inputContextProductShortDescription").val("");
	$("#inputContextProductFullDescription").val("");

	linkContextProductBranchSelect.children().remove();
	linkedContextProductBranchesList.children().remove();

	contextProductInformationList.children().remove();

	CurrentContextProductNameMultiLangData = {};
	CurrentContextProductShortDescriptionMultiLangData = {};
	CurrentContextProductLongDescriptionMultiLangData = {};
	CurrentContextProductOtherInformationMultiLangData = {};

	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentContextProductNameMultiLangData[language] = "";
		CurrentContextProductShortDescriptionMultiLangData[language] = "";
		CurrentContextProductLongDescriptionMultiLangData[language] = "";
		CurrentContextProductOtherInformationMultiLangData[language] = {};
	});

	contextProductsManagerTab.find(".is-invalid").removeClass("is-invalid");

	saveContextProductsButton.prop("disabled", true);
}

function ShowContextProductsManagerTab() {
	contextProductsListTab.removeClass("show");
	contextTabButtonHeader.removeClass("show");
	setTimeout(() => {
		contextProductsListTab.addClass("d-none");
		contextTabButtonHeader.addClass("d-none");

		contextProductsManagerTab.removeClass("d-none");
		productsManageTabHeader.removeClass("d-none");
		saveContextProductsButton.removeClass("d-none");
		setTimeout(() => {
			contextProductsManagerTab.addClass("show");
			productsManageTabHeader.addClass("show");
			saveContextProductsButton.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowContextProductsListTab() {
	contextProductsManagerTab.removeClass("show");
	productsManageTabHeader.removeClass("show");
	saveContextProductsButton.removeClass("d-none");
	setTimeout(() => {
		contextProductsManagerTab.addClass("d-none");
		productsManageTabHeader.addClass("d-none");
		saveContextProductsButton.addClass("d-none");

		contextProductsListTab.removeClass("d-none");
		contextTabButtonHeader.removeClass("d-none");
		setTimeout(() => {
			contextProductsListTab.addClass("show");
			contextTabButtonHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 150);
}

function createDefaultContextProductsObject() {
	const object = {
		name: {},
		shortDescription: {},
		longDescription: {},
		availableAtBranches: [],
		otherInformation: {},
	};

	BusinessFullData.businessData.languages.forEach((language) => {
		object.name[language] = "";
		object.shortDescription[language] = "";
		object.longDescription[language] = "";
		object.otherInformation[language] = {};
	});

	return object;
}

function createContextProductTableElement(product) {
	const productRow = `
        <tr product-id="${product.id}">
            <td>
                <b>${product.name[BusinessDefaultLanguage]}</b>
            </td>
            <td>
                <button class="btn btn-info btn-sm" product-id="${product.id}" button-type="editProduct">
                    <i class="fa-regular fa-pen-to-square"></i>
                </button>
                <button class="btn btn-danger btn-sm" product-id="${product.id}" button-type="deleteProduct">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `;

	return productRow;
}

function createContextProductOtherInformationElement() {
	const element = `
        <div class="mt-2">
            <div class="input-group">
                <input data-type="key" type="text" class="form-control" placeholder="Information Type" aria-label="Information Type" value="" style="border-bottom: none; border-bottom-left-radius: 0;">
                <button class="btn btn-danger" button-type="contextProductInformationRemove" style="border-bottom: none; border-bottom-right-radius: 0;">
                    <i class='fa-regular fa-trash'></i>
                </button>
            </div>
            <textarea data-type="value" class="form-control" placeholder="Information" aria-label="Information" style="border-top-left-radius: 0; border-top-right-radius: 0"></textarea>
        </div>
    `;

	return element;
}

function fillContextProductManager(productData) {
	// Fill multilanguage data
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentContextProductNameMultiLangData[language] = productData.name[language];
		CurrentContextProductShortDescriptionMultiLangData[language] = productData.shortDescription[language];
		CurrentContextProductLongDescriptionMultiLangData[language] = productData.longDescription[language];
		CurrentContextProductOtherInformationMultiLangData[language] = productData.otherInformation[language] || {};
	});

	// Fill form with default language values
	inputContextProductName.val(productData.name[BusinessDefaultLanguage]);
	inputContextProductShortDescription.val(productData.shortDescription[BusinessDefaultLanguage]);
	inputContextProductFullDescription.val(productData.longDescription[BusinessDefaultLanguage]);

	// Fill branches list
	linkedContextProductBranchesList.empty();
	productData.availableAtBranches.forEach((branchId) => {
		const branch = BusinessFullData.businessApp.context.branches.find((b) => b.id === branchId);
		if (branch) {
			linkedContextProductBranchesList.append(`
                <li class="list-group-item" branch-id="${branch.id}">
                    <div class="d-flex flex-row align-items-center justify-content-between">
                        <span>${branch.general.name[BusinessDefaultLanguage]}</span>
                        <button class="btn btn-danger btn-sm" button-type="removeLinkedBranch">
                            <i class="fa-regular fa-trash"></i>
                        </button>
                    </div>
                </li>
            `);
		}
	});

	// Fill other information
	contextProductInformationList.empty();
	Object.entries(productData.otherInformation[BusinessDefaultLanguage] || {}).forEach(([key, value]) => {
		const infoElement = $(createContextProductOtherInformationElement());
		infoElement.find('[data-type="key"]').val(key);
		infoElement.find('[data-type="value"]').val(value);
		contextProductInformationList.append(infoElement);
	});

	validateContextProductAllMultilanguageElements();
	saveContextProductsButton.prop("disabled", true);
}

function fillContextProductsTab() {
	const productsData = BusinessFullData.businessApp.context.products || [];
	contextProductsTable.find("tbody").empty();

	if (productsData.length === 0) {
		contextProductsTable.find("tbody").append("<tr tr-type='none-notice'><td colspan='2'>No products found</td></tr>");
	} else {
		productsData.forEach((product) => {
			contextProductsTable.find("tbody").append($(createContextProductTableElement(product)));
		});
	}
}

function validateContextProductAllMultilanguageElements() {
	BusinessFullData.businessData.languages.forEach((language) => {
		const currentLanguage = SpecificationLanguagesListData.find((d) => d.id === language);
		let isAnyFieldIncomplete = false;

		// Validate Name
		if (!CurrentContextProductNameMultiLangData[language] || CurrentContextProductNameMultiLangData[language].trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Validate Short Description
		if (!CurrentContextProductShortDescriptionMultiLangData[language] || CurrentContextProductShortDescriptionMultiLangData[language].trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Validate Long Description
		if (!CurrentContextProductLongDescriptionMultiLangData[language] || CurrentContextProductLongDescriptionMultiLangData[language].trim() === "") {
			isAnyFieldIncomplete = true;
		}

		// Validate Other Information
		if (language === ContextTabMultiLanguageDropdown.getSelectedLanguage().id) {
			contextProductInformationList.children().each((idx, element) => {
				const key = $(element).find('[data-type="key"]').val().trim();
				const value = $(element).find('[data-type="value"]').val().trim();
				if (!key || !value) {
					isAnyFieldIncomplete = true;
				}
			});
		}

		// Update language status in dropdown
		ContextTabMultiLanguageDropdown.setLanguageStatus(currentLanguage.id, isAnyFieldIncomplete ? "incomplete" : "complete");
	});
}

function ValidateContextProductTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	BusinessFullData.businessData.languages.forEach((language) => {
		// Name validation
		if (!CurrentContextProductNameMultiLangData[language] || CurrentContextProductNameMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Product name for language ${language} is required.`);
			if (!onlyRemove) {
				inputContextProductName.addClass("is-invalid");
			}
		} else {
			inputContextProductName.removeClass("is-invalid");
		}

		// Short Description validation
		if (!CurrentContextProductShortDescriptionMultiLangData[language] || CurrentContextProductShortDescriptionMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Product short description for language ${language} is required.`);
			if (!onlyRemove) {
				inputContextProductShortDescription.addClass("is-invalid");
			}
		} else {
			inputContextProductShortDescription.removeClass("is-invalid");
		}

		// Long Description validation
		if (!CurrentContextProductLongDescriptionMultiLangData[language] || CurrentContextProductLongDescriptionMultiLangData[language].trim().length === 0) {
			validated = false;
			errors.push(`Product full description for language ${language} is required.`);
			if (!onlyRemove) {
				inputContextProductFullDescription.addClass("is-invalid");
			}
		} else {
			inputContextProductFullDescription.removeClass("is-invalid");
		}
	});

	// Validate other information keys for duplicates
	if (!validateProductOtherInformationKeys()) {
		validated = false;
		errors.push("Duplicate information types found. Please ensure all information types are unique.");
	}

	return {
		validated: validated,
		errors: errors,
	};
}

function validateProductOtherInformationKeys() {
	const seenKeys = new Set();
	let hasDuplicates = false;

	contextProductInformationList.children().each((idx, element) => {
		const currentElement = $(element);
		const keyInput = currentElement.find('[data-type="key"]');
		const key = keyInput.val().trim();

		if (key && seenKeys.has(key)) {
			hasDuplicates = true;
			keyInput.addClass("is-invalid");
		} else {
			keyInput.removeClass("is-invalid");
			if (key) {
				seenKeys.add(key);
			}
		}
	});

	return !hasDuplicates;
}

function CheckContextProductTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Check multilanguage fields
	BusinessFullData.businessData.languages.forEach((language) => {
		// Name changes
		if (!changes.name) changes.name = {};
		changes.name[language] = CurrentContextProductNameMultiLangData[language];
		if (CurrentContextProductData.name[language] !== changes.name[language]) {
			hasChanges = true;
		}

		// Short Description changes
		if (!changes.shortDescription) changes.shortDescription = {};
		changes.shortDescription[language] = CurrentContextProductShortDescriptionMultiLangData[language];
		if (CurrentContextProductData.shortDescription[language] !== changes.shortDescription[language]) {
			hasChanges = true;
		}

		// Long Description changes
		if (!changes.longDescription) changes.longDescription = {};
		changes.longDescription[language] = CurrentContextProductLongDescriptionMultiLangData[language];
		if (CurrentContextProductData.longDescription[language] !== changes.longDescription[language]) {
			hasChanges = true;
		}

		// Other Information changes
		if (!changes.otherInformation) changes.otherInformation = {};
		changes.otherInformation[language] = CurrentContextProductOtherInformationMultiLangData[language] || {};
		if (JSON.stringify(CurrentContextProductData.otherInformation[language]) !== JSON.stringify(changes.otherInformation[language])) {
			hasChanges = true;
		}
	});

	// Check branches
	changes.availableAtBranches = [];
	linkedContextProductBranchesList.children().each((idx, element) => {
		const branchId = $(element).attr("branch-id");
		changes.availableAtBranches.push(branchId);
	});
	if (JSON.stringify(CurrentContextProductData.availableAtBranches) !== JSON.stringify(changes.availableAtBranches)) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		saveContextProductsButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function updateContextProductOtherInformation(languageId) {
	CurrentContextProductOtherInformationMultiLangData[languageId] = {};

	contextProductInformationList.children().each((idx, element) => {
		const infoType = $(element).find('[data-type="key"]').val().trim();
		const infoValue = $(element).find('[data-type="value"]').val().trim();

		if (infoType) {
			CurrentContextProductOtherInformationMultiLangData[languageId][infoType] = infoValue;
		}
	});
}

async function canLeaveContextProductsTab(leaveMessage = "") {
	if (ManageContextProductType == null) return true;

	if (IsSavingContextProductTab) {
		AlertManager.createAlert({
			type: "warning",
			message: "Product manager tab is currently being saved. Please wait for the save to finish.",
			enableDismiss: false,
		});
		return false;
	}

	const productManagerChanges = CheckContextProductTabHasChanges(false);
	if (productManagerChanges.hasChanges) {
		const confirmDiscardChangesDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in product manager tab.${leaveMessage}`,
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

/** INIT **/

function initContextTab() {
	$(document).ready(() => {
		ContextTabMultiLanguageDropdown = new MultiLanguageDropdown("contextTabMultiLanguageSelectContainer", BusinessFullLanguagesData);

		DAYS.forEach((day) => {
			editBranchOpeningHoursInputsList.append(`
                         <div class="col-12">
                              <div class="mb-2 branchOpeningHoursDayBox">
                                   <div class="d-flex flex-row gap-2 align-items-center mb-2">
                                        <label class="form-label fw-bold my-auto">${day}</label>
                                        <div class="">
                                             <input type="checkbox" class="form-check-input" id="editBranchOpeningHours${day}Input" checkbox-type="branchWorkingHourIsClosed" day-value="${day}">
                                             <label for="editBranchOpeningHours${day}Input" class="form-check-label">is closed?</label>
                                        </div>
                                   </div>

                                   <label class="form-label d-block mb-1">Timings</label>
                                   <button class="btn btn-light btn-sm mb-2" button-type="addBranchWorkingHour" day-value="${day}">Add Timing</button>
                                   <div class="workingHoursTimingList" day-value="${day}">
                                   </div>
                              </div>
                         </div>
                         `);
		});

		/** Event Handlers **/

		$("#nav-bar").on("tabChange", async (event) => {
			const activeTab = event.detail.from;
			if (activeTab !== "context-tab") return;

			const canLeaveBrandingTabResult = await canLeaveContextBrandingTab(" Are you sure you want to discard these changes and leave the context tab?");
			if (!canLeaveBrandingTabResult) {
				event.preventDefault();
			} else if (ManageContextBranchType == null && ManageContextServiceType == null && ManageContextProductType == null) {
				FillContextBrandingTab();
			}

			const canLeaveBranchesTabResult = await canLeaveContextBranchesTab(" Are you sure you want to discard these changes and leave the context tab?");
			if (!canLeaveBranchesTabResult) {
				event.preventDefault();
			} else if (ManageContextBranchType != null) {
				ManageContextBranchType = null;
				CurrentContextBranchData = null;
				ShowContextBranchesListTab();
				contextTabHeader.find("#context-inner-branding-tab").click();
			}

			const canLeaveServicesTabResult = await canLeaveContextServicesTab(" Are you sure you want to discard these changes and leave the context tab?");
			if (!canLeaveServicesTabResult) {
				event.preventDefault();
			} else if (ManageContextServiceType != null) {
				ManageContextServiceType = null;
				CurrentContextServiceData = null;
				ShowContextServicesListTab();
				contextTabHeader.find("#context-inner-branding-tab").click();
			}

			const canLeaveProductsTabResult = await canLeaveContextProductsTab(" Are you sure you want to discard these changes and leave the context tab?");
			if (!canLeaveProductsTabResult) {
				event.preventDefault();
			} else if (ManageContextProductType != null) {
				ManageContextProductType = null;
				CurrentContextProductData = null;
				ShowContextProductsListTab();
				contextTabHeader.find("#context-inner-branding-tab").click();
			}
		});

		ContextTabMultiLanguageDropdown.onLanguageChange((language) => {
			// Branding Tab
			brandingBrandNameInput.val(CurrentContextBrandingNameMultiLangData[language.id] || "");
			brandingBrandCountryInput.val(CurrentContextBrandingCountryMultiLangData[language.id] || "");
			brandingGlobalContactInput.val(CurrentContextBrandingEmailMultiLangData[language.id] || "");
			brandingGlobalPhoneInput.val(CurrentContextBrandingPhoneMultiLangData[language.id] || "");
			brandingGlobalWebsiteInput.val(CurrentContextBrandingWebsiteMultiLangData[language.id] || "");
			fillContextBrandOtherInformationForLanguage(language.id);

			// Branches Tab
			if (ManageContextBranchType != null) {
				editBranchNameInput.val(CurrentContextBranchNameMultiLangData[language.id] || "");
				editBranchAddressInput.val(CurrentContextBranchAddressMultiLangData[language.id] || "");
				editBranchPhoneInput.val(CurrentContextBranchPhoneMultiLangData[language.id] || "");
				editBranchEmailInput.val(CurrentContextBranchEmailMultiLangData[language.id] || "");
                editBranchWebsiteInput.val(CurrentContextBranchWebsiteMultiLangData[language.id] || "");
				fillContextBranchOtherInformationForLanguage(language.id);

				editBranchTeamInputsList.children().each((index, teamElement) => {
					var teamElementBox = $(teamElement);

					var teamMemberData = CurrentContextBranchTeamMultiLangData[index];

					teamElementBox.find('[data-field="name"]').val(teamMemberData.name[language.id]);
					teamElementBox.find('[data-field="role"]').val(teamMemberData.role[language.id]);
					teamElementBox.find('[data-field="email"]').val(teamMemberData.email[language.id]);
					teamElementBox.find('[data-field="phone"]').val(teamMemberData.phone[language.id]);
					teamElementBox.find('[data-field="information"]').val(teamMemberData.information[language.id]);
				});
			}

            // Services Tab
			if (ManageContextServiceType != null) {

			}

            // Products Tab
			if (ManageContextProductType != null) {

			}

			// Validation
			let activeTab = $("#context-inner-tab button.nav-link.active").attr("id");
			if (activeTab === "context-inner-branding-tab") {
				validateContextBrandingAllMultilanguageElements();
			}
			else if (activeTab === "context-inner-branches-tab") {
				validateContextBranchAllMultilanguageElements();
			}
			else if (activeTab === "context-inner-services-tab") {
				validateContextServiceAllMultilanguageElements();
			}
			else if (activeTab === "context-inner-products-tab") {
				validateContextProductAllMultilanguageElements();
			}
		});

		$("#context-inner-tab button.nav-link").on("show.bs.tab", (event) => {
			const newTabId = $(event.target).attr("id");

			if (newTabId !== "context-inner-branding-tab") {
				saveContextBrandingButton.removeClass("show");
				ContextTabMultiLanguageDropdown.$container.removeClass("show");

				setTimeout(() => {
					saveContextBrandingButton.addClass("d-none");
                    ContextTabMultiLanguageDropdown.$container.addClass("d-none");

					setDynamicBodyHeight();
				}, 300);
			} else {
				validateContextBrandingAllMultilanguageElements();

				saveContextBrandingButton.removeClass("d-none");
                ContextTabMultiLanguageDropdown.$container.removeClass("d-none");

				setTimeout(() => {
					saveContextBrandingButton.addClass("show");
                    ContextTabMultiLanguageDropdown.$container.addClass("show");

					setDynamicBodyHeight();
				}, 10);
			}
		});

		// TAB | Branding

		addNewBrandInformationButton.on("click", (event) => {
			event.preventDefault();
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			brandingOtherInformationList.append($(CreateContextBrandingOtherInformationElement()));

			updateBrandingOtherInformation(currentSelectedLanguage.id);
			validateContextBrandingAllMultilanguageElements();
			CheckContextBrandingTabHasChanges(true);
		});

		brandingOtherInformationList.on("click", '[button-type="brandingInformationRemove"]', (event) => {
			event.preventDefault();
			event.stopPropagation();
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			$(event.currentTarget).parent().parent().remove();

			updateBrandingOtherInformation(currentSelectedLanguage.id);
			validateContextBrandingAllMultilanguageElements();
			CheckContextBrandingTabHasChanges(true);
		});

		brandingTab.on("input change", "input[type='text'], input[type='email'], input[type='tel'], textarea", (event) => {
			const currentElement = $(event.currentTarget);
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			// Update corresponding multilanguage data based on input id
			switch (currentElement.attr("id")) {
				case "brandingBrandNameInput":
					CurrentContextBrandingNameMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "brandingBrandCountryInput":
					CurrentContextBrandingCountryMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "brandingGlobalContactInput":
					CurrentContextBrandingEmailMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "brandingGlobalPhoneInput":
					CurrentContextBrandingPhoneMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "brandingGlobalWebsiteInput":
					CurrentContextBrandingWebsiteMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
			}

			// Update other information when those fields change
			if ($(event.target).closest("#brandingOtherInformationList").length > 0) {
				if (validateBrandingOtherInformationKeys()) {
					updateBrandingOtherInformation(currentSelectedLanguage.id);
				}
			}

			validateContextBrandingAllMultilanguageElements();
			CheckContextBrandingTabHasChanges(true);
		});

		saveContextBrandingButton.on("click", async (event) => {
			event.preventDefault();

			if (IsSavingContextBrandingTab) return;

			const validationResult = ValidateContextBrandingTab(false);
			if (!validationResult.validated) {
				AlertManager.createAlert({
					type: "danger",
					message: `Validation for required branding fields failed.<br><br>${validationResult.errors.join("<br>")}`,
					timeout: 6000,
				});
				return;
			}

			const brandingTabChanges = CheckContextBrandingTabHasChanges(false);
			if (!brandingTabChanges.hasChanges) {
				return;
			}

			saveContextBrandingButton.prop("disabled", true);
			saveContextBrandingButtonSpinner.removeClass("d-none");

			IsSavingContextBrandingTab = true;

			const formData = new FormData();
			formData.append("changes", JSON.stringify(brandingTabChanges.changes));

			SaveBusinessContextBranding(
				formData,
				(saveResponse) => {
					CurrentContextBrandingData = saveResponse.data;

					BusinessFullData.businessApp.context.branding = CurrentContextBrandingData;

					saveContextBrandingButton.prop("disabled", true);
					saveContextBrandingButtonSpinner.addClass("d-none");

					IsSavingContextBrandingTab = false;

					AlertManager.createAlert({
						type: "success",
						message: "Business branding updated successfully.",
						timeout: 6000,
					});
				},
				(saveError, isUnsuccessful) => {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving business branding data. Check browser console for logs.",
						timeout: 6000,
					});

					console.log("Error occurred while saving business branding data: ", saveError);

					saveContextBrandingButton.prop("disabled", false);
					saveContextBrandingButtonSpinner.addClass("d-none");

					IsSavingContextBrandingTab = false;
				},
			);
		});

		// TAB | Branches

		addNewBranchButton.on("click", (event) => {
			event.preventDefault();

			currentBranchName.text("New Branch");

			resetOrClearContextBranchManager();

			ShowContextBranchesManagerTab();

			ManageContextBranchType = "new";
			CurrentContextBranchData = createDefaultContextBranchesObject();

			validateContextBranchAllMultilanguageElements();
		});

		switchBackToBranchesTab.on("click", async (event) => {
			event.preventDefault();

			const canLeaveResult = await canLeaveContextBranchesTab();
			if (!canLeaveResult) return;

			ShowContextBranchesListTab();
			ManageContextBranchType = null;
		});

		branchesTable.on("click", "button[button-type='editBranch']", (event) => {
			event.preventDefault();

			const branchId = $(event.currentTarget).attr("branch-id");

			resetOrClearContextBranchManager();

			CurrentContextBranchData = BusinessFullData.businessApp.context.branches.find((branch) => branch.id === branchId);

			currentBranchName.text(CurrentContextBranchData.general.name[BusinessDefaultLanguage]);

			fillContextBranchManager(CurrentContextBranchData);
			ShowContextBranchesManagerTab();

			ManageContextBranchType = "edit";

			validateContextBranchAllMultilanguageElements();
		});

		saveContextBranchesButton.on("click", async (event) => {
			event.preventDefault();

			if (IsSavingContextBranchTab) return;

			const validationResult = ValidateContextBranchTab(false);
			if (!validationResult.validated) {
				AlertManager.createAlert({
					type: "danger",
					message: `Validation for required branch fields failed.<br><br>${validationResult.errors.join("<br>")}`,
					timeout: 6000,
				});
				return;
			}

			const branchTabChanges = CheckContextBranchTabHasChanges(false);
			if (!branchTabChanges.hasChanges) {
				return;
			}

			saveContextBranchesButton.prop("disabled", true);
			saveContextBranchesButtonSpinner.removeClass("d-none");

			IsSavingContextBranchTab = true;

			const formData = new FormData();
			formData.append("changes", JSON.stringify(branchTabChanges.changes));
			formData.append("postType", ManageContextBranchType);

			if (ManageContextBranchType === "edit") {
				formData.append("exisitingBranchId", CurrentContextBranchData.id);
			}

			SaveBusinessContextBranch(
				formData,
				(saveResponse) => {
					CurrentContextBranchData = saveResponse.data;

					currentBranchName.text(CurrentContextBranchData.general.name[BusinessDefaultLanguage]);

					if (ManageContextBranchType === "new") {
						BusinessFullData.businessApp.context.branches.push(CurrentContextBranchData);
						branchesTable.find("tbody").append($(createContextBranchesTableElement(CurrentContextBranchData)));

						branchesTable.find("tbody tr[tr-type='none-notice']").remove();
					} else {
						const branchIndex = BusinessFullData.businessApp.context.branches.findIndex((b) => b.id === CurrentContextBranchData.id);
						if (branchIndex !== -1) {
							BusinessFullData.businessApp.context.branches[branchIndex] = CurrentContextBranchData;
						}

						branchesTable.find(`tbody tr[branch-id="${CurrentContextBranchData.id}"]`).replaceWith($(createContextBranchesTableElement(CurrentContextBranchData)));
					}

					saveContextBranchesButton.prop("disabled", true);
					saveContextBranchesButtonSpinner.addClass("d-none");

					IsSavingContextBranchTab = false;

					AlertManager.createAlert({
						type: "success",
						message: `Branch ${ManageContextBranchType === "new" ? "added" : "updated"} successfully.`,
						timeout: 6000,
					});

					ManageContextBranchType = "edit";
				},
				(saveError, isUnsuccessful) => {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving business branch data. Check browser console for logs.",
						timeout: 6000,
					});

					console.log("Error occurred while saving business branch data: ", saveError);

					saveContextBranchesButton.prop("disabled", false);
					saveContextBranchesButtonSpinner.addClass("d-none");

					IsSavingContextBranchTab = false;
				},
			);
		});

		// Branches - General

		addContextBranchInformationButton.on("click", (event) => {
			event.preventDefault();
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			contextBranchInformationList.append($(createContextBranchOtherInformationElement()));

			updateContextBranchOtherInformation(currentSelectedLanguage.id);
			CheckContextBranchTabHasChanges(true);
			validateContextBranchAllMultilanguageElements();
		});

		contextBranchInformationList.on("click", '[button-type="contextBranchInformationRemove"]', (event) => {
			event.preventDefault();
			event.stopPropagation();
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			$(event.currentTarget).parent().parent().remove();

			updateContextBranchOtherInformation(currentSelectedLanguage.id);
			CheckContextBranchTabHasChanges(true);
			validateContextBranchAllMultilanguageElements();
		});

		branchesManagerTab.on("input change", "input[type='text'], input[type='email'], input[type='tel'], textarea", (event) => {
			if (ManageContextBranchType == null) return;

			const currentElement = $(event.currentTarget);
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			switch (currentElement.attr("id")) {
				case "editBranchNameInput":
					CurrentContextBranchNameMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "editBranchAddressInput":
					CurrentContextBranchAddressMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "editBranchPhoneInput":
					CurrentContextBranchPhoneMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "editBranchEmailInput":
					CurrentContextBranchEmailMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "editBranchWebsiteInput":
					CurrentContextBranchWebsiteMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
			}

			validateContextBranchAllMultilanguageElements();
			CheckContextBranchTabHasChanges(true);
		});

		contextBranchInformationList.on("input change", "input, textarea", (event) => {
			if (ManageContextBranchType == null) return;

			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			if (validateBranchOtherInformationKeys()) {
				updateContextBranchOtherInformation(currentSelectedLanguage.id);
			}

			validateContextBranchAllMultilanguageElements();
			CheckContextBranchTabHasChanges(true);
		});

		// Branches - Working Hours

		editBranchOpeningHoursInputsList.on("click", '[checkbox-type="branchWorkingHourIsClosed"]', (event) => {
			event.stopPropagation();

			const dayValue = $(event.currentTarget).attr("day-value");
			const isChecked = $(event.currentTarget).is(":checked");

			$(`[button-type="addBranchWorkingHour"][day-value="${dayValue}"]`).prop("disabled", isChecked);

			const dayTimingsList = $(`.workingHoursTimingList[day-value="${dayValue}"]`);
			if (isChecked) {
				dayTimingsList.addClass("d-none");
			} else {
				dayTimingsList.removeClass("d-none");
			}
		});

		editBranchOpeningHoursInputsList.on("click", '[button-type="addBranchWorkingHour"]', (event) => {
			event.stopPropagation();

			const dayValue = $(event.currentTarget).attr("day-value");

			const dayTimingsList = $(`.workingHoursTimingList[day-value="${dayValue}"]`);

			dayTimingsList.append($(createContextBranchWorkingHourElement()));
		});

		editBranchOpeningHoursInputsList.on("click", '[button-type="removeBranchWorkingHour"]', (event) => {
			event.stopPropagation();

			$(event.currentTarget).parent().remove();
		});

		editBranchOpeningHoursInputsList.on("change", 'input[type="time"]', (event) => {
			if (ManageContextBranchType == null) return;

			CheckContextBranchTabHasChanges(true);
		});

		// Branches - Teams

		editBranchAddTeamMember.on("click", (event) => {
			event.preventDefault();

			editBranchTeamInputsList.append($(createContextBranchTeamElement(null)));

			CurrentContextBranchTeamMultiLangData.push(createDefaultContextBranchTeamMemberObject());
		});

		editBranchTeamInputsList.on("click", '[button-type="removeEditBranchTeam"]', (event) => {
			event.stopPropagation();

			const elementToRemove = $(event.currentTarget).closest(".editBranchTeamBox").parent();
			const index = $(elementToRemove).index();

			CurrentContextBranchTeamMultiLangData.splice(index, 1);
			
			elementToRemove.remove();
		});

		editBranchTeamInputsList.on("input change", "input, textarea", (event) => {
			if (ManageContextBranchType == null) return;

			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			const elementParentBox = $(event.currentTarget).closest(".editBranchTeamBox").parent();
			const index = $(elementParentBox).index();

			updateContextBranchTeamMembersData(currentSelectedLanguage.id, index);

			validateContextBranchAllMultilanguageElements();
			CheckContextBranchTabHasChanges(true);
		});

		// TAB | Services

		addNewServiceButton.on("click", (event) => {
			event.preventDefault();

			currentContextServiceName.text("New Service");

			resetOrClearContextServiceManager();
			ShowContextServicesManagerTab();

			ManageContextServiceType = "new";
			CurrentContextServiceData = createDefaultContextServicesObject();

			// Fill branch select options
			linkContextServiceBranchSelect.empty();
			BusinessFullData.businessApp.context.branches.forEach((branch) => {
				linkContextServiceBranchSelect.append(`
            <option value="${branch.id}">${branch.general.name[BusinessDefaultLanguage]}</option>
        `);
			});

			// Fill product select options
			linkContextServiceProductSelect.empty();
			BusinessFullData.businessApp.context.products.forEach((product) => {
				linkContextServiceProductSelect.append(`
            <option value="${product.id}">${product.name[BusinessDefaultLanguage]}</option>
        `);
			});
		});

		switchBackToContextServicesTab.on("click", async (event) => {
			event.preventDefault();

			const canLeaveResult = await canLeaveContextServicesTab();
			if (!canLeaveResult) return;

			ShowContextServicesListTab();
			ManageContextServiceType = null;
		});

		contextServicesTable.on("click", "button[button-type='editService']", (event) => {
			event.preventDefault();

			const serviceId = $(event.currentTarget).attr("service-id");

			resetOrClearContextServiceManager();

			CurrentContextServiceData = BusinessFullData.businessApp.context.services.find((service) => service.id === serviceId);

			currentContextServiceName.text(CurrentContextServiceData.name[BusinessDefaultLanguage]);

			// Fill branch select options
			linkContextServiceBranchSelect.empty();
			BusinessFullData.businessApp.context.branches.forEach((branch) => {
				if (!CurrentContextServiceData.availableAtBranches.includes(branch.id)) {
					linkContextServiceBranchSelect.append(`
                <option value="${branch.id}">${branch.general.name[BusinessDefaultLanguage]}</option>
            `);
				}
			});

			// Fill product select options
			linkContextServiceProductSelect.empty();
			BusinessFullData.businessApp.context.products.forEach((product) => {
				if (!CurrentContextServiceData.relatedProducts.includes(product.id)) {
					linkContextServiceProductSelect.append(`
                <option value="${product.id}">${product.name[BusinessDefaultLanguage]}</option>
            `);
				}
			});

			fillContextServiceManager(CurrentContextServiceData);
			ShowContextServicesManagerTab();

			ManageContextServiceType = "edit";
		});

		addNewContextServiceInformationButton.on("click", (event) => {
			event.preventDefault();
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			contextServiceInformationList.append($(createContextServiceOtherInformationElement()));

			updateContextServiceOtherInformation(currentSelectedLanguage.id);
			validateContextServiceAllMultilanguageElements();
			CheckContextServiceTabHasChanges(true);
		});

		contextServiceInformationList.on("click", '[button-type="contextServiceInformationRemove"]', (event) => {
			event.preventDefault();
			event.stopPropagation();
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			$(event.currentTarget).parent().parent().remove();

			updateContextServiceOtherInformation(currentSelectedLanguage.id);
			validateContextServiceAllMultilanguageElements();
			CheckContextServiceTabHasChanges(true);
		});

		contextServicesManagerTab.on("input change", "input[type='text'], textarea", (event) => {
			if (ManageContextServiceType == null) return;

			const currentElement = $(event.currentTarget);
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			switch (currentElement.attr("id")) {
				case "inputContextServiceName":
					CurrentContextServiceNameMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "inputContextServiceShortDescription":
					CurrentContextServiceShortDescriptionMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "inputContextServiceFullDescription":
					CurrentContextServiceLongDescriptionMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
			}

			validateContextServiceAllMultilanguageElements();
			CheckContextServiceTabHasChanges(true);
		});

		contextServiceInformationList.on("input change", "input, textarea", (event) => {
			if (ManageContextServiceType == null) return;

			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			if (validateServiceOtherInformationKeys()) {
				updateContextServiceOtherInformation(currentSelectedLanguage.id);
			}

			validateContextServiceAllMultilanguageElements();
			CheckContextServiceTabHasChanges(true);
		});

		linkContextServiceBranchButton.on("click", (event) => {
			event.preventDefault();

			const selectedBranchId = linkContextServiceBranchSelect.val();
			if (!selectedBranchId) return;

			const selectedBranch = BusinessFullData.businessApp.context.branches.find((branch) => branch.id === selectedBranchId);
			if (!selectedBranch) return;

			linkedContextServiceBranchesList.append(`
        <li class="list-group-item" branch-id="${selectedBranch.id}">
            <div class="d-flex flex-row align-items-center justify-content-between">
                <span>${selectedBranch.general.name[BusinessDefaultLanguage]}</span>
                <button class="btn btn-danger btn-sm" button-type="removeLinkedBranch">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </div>
        </li>
    `);

			// Remove from select
			linkContextServiceBranchSelect.find(`option[value="${selectedBranch.id}"]`).remove();

			CheckContextServiceTabHasChanges(true);
		});

		linkedContextServiceBranchesList.on("click", '[button-type="removeLinkedBranch"]', (event) => {
			event.preventDefault();
			const listItem = $(event.currentTarget).closest(".list-group-item");
			const branchId = listItem.attr("branch-id");

			const branch = BusinessFullData.businessApp.context.branches.find((b) => b.id === branchId);
			if (branch) {
				linkContextServiceBranchSelect.append(`
            <option value="${branch.id}">${branch.general.name[BusinessDefaultLanguage]}</option>
        `);
			}

			listItem.remove();
			CheckContextServiceTabHasChanges(true);
		});

		linkContextServiceProductButton.on("click", (event) => {
			event.preventDefault();

			const selectedProductId = linkContextServiceProductSelect.val();
			if (!selectedProductId) return;

			const selectedProduct = BusinessFullData.businessApp.context.products.find((product) => product.id === selectedProductId);
			if (!selectedProduct) return;

			linkedContextServiceProductsList.append(`
        <li class="list-group-item" product-id="${selectedProduct.id}">
            <div class="d-flex flex-row align-items-center justify-content-between">
                <span>${selectedProduct.name[BusinessDefaultLanguage]}</span>
                <button class="btn btn-danger btn-sm" button-type="removeLinkedProduct">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </div>
        </li>
    `);

			// Remove from select
			linkContextServiceProductSelect.find(`option[value="${selectedProduct.id}"]`).remove();

			CheckContextServiceTabHasChanges(true);
		});

		linkedContextServiceProductsList.on("click", '[button-type="removeLinkedProduct"]', (event) => {
			event.preventDefault();
			const listItem = $(event.currentTarget).closest(".list-group-item");
			const productId = listItem.attr("product-id");

			const product = BusinessFullData.businessApp.context.products.find((p) => p.id === productId);
			if (product) {
				linkContextServiceProductSelect.append(`
            <option value="${product.id}">${product.name[BusinessDefaultLanguage]}</option>
        `);
			}

			listItem.remove();
			CheckContextServiceTabHasChanges(true);
		});

		saveContextServicesButton.on("click", async (event) => {
			event.preventDefault();

			if (IsSavingContextServiceTab) return;

			const validationResult = ValidateContextServiceTab(false);
			if (!validationResult.validated) {
				AlertManager.createAlert({
					type: "danger",
					message: `Validation for required service fields failed.<br><br>${validationResult.errors.join("<br>")}`,
					timeout: 6000,
				});
				return;
			}

			const serviceTabChanges = CheckContextServiceTabHasChanges(false);
			if (!serviceTabChanges.hasChanges) {
				return;
			}

			saveContextServicesButton.prop("disabled", true);
			saveContextServicesButtonSpinner.removeClass("d-none");

			IsSavingContextServiceTab = true;

			const formData = new FormData();
			formData.append("changes", JSON.stringify(serviceTabChanges.changes));
			formData.append("postType", ManageContextServiceType);

			if (ManageContextServiceType === "edit") {
				formData.append("exisitingServiceId", CurrentContextServiceData.id);
			}

			SaveBusinessContextService(
				formData,
				(saveResponse) => {
					CurrentContextServiceData = saveResponse.data;

					currentContextServiceName.text(CurrentContextServiceData.name[BusinessDefaultLanguage]);

					if (ManageContextServiceType === "new") {
						BusinessFullData.businessApp.context.services.push(CurrentContextServiceData);
						contextServicesTable.find("tbody").append($(createContextServiceTableElement(CurrentContextServiceData)));

						contextServicesTable.find("tbody tr[tr-type='none-notice']").remove();
					} else {
						const serviceIndex = BusinessFullData.businessApp.context.services.findIndex((s) => s.id === CurrentContextServiceData.id);
						if (serviceIndex !== -1) {
							BusinessFullData.businessApp.context.services[serviceIndex] = CurrentContextServiceData;
						}

						contextServicesTable.find(`tbody tr[service-id="${CurrentContextServiceData.id}"]`).replaceWith($(createContextServiceTableElement(CurrentContextServiceData)));
					}

					saveContextServicesButton.prop("disabled", true);
					saveContextServicesButtonSpinner.addClass("d-none");

					IsSavingContextServiceTab = false;

					AlertManager.createAlert({
						type: "success",
						message: `Service ${ManageContextServiceType === "new" ? "added" : "updated"} successfully.`,
						timeout: 6000,
					});

					ManageContextServiceType = "edit";
				},
				(saveError, isUnsuccessful) => {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving service data. Check browser console for logs.",
						timeout: 6000,
					});

					console.log("Error occurred while saving service data: ", saveError);

					saveContextServicesButton.prop("disabled", false);
					saveContextServicesButtonSpinner.addClass("d-none");

					IsSavingContextServiceTab = false;
				},
			);
		});

		// TAB | Products

		addNewProductButton.on("click", (event) => {
			event.preventDefault();

			currentContextProductName.text("New Product");

			resetOrClearContextProductManager();
			ShowContextProductsManagerTab();

			ManageContextProductType = "new";
			CurrentContextProductData = createDefaultContextProductsObject();

			// Fill branch select options
			linkContextProductBranchSelect.empty();
			BusinessFullData.businessApp.context.branches.forEach((branch) => {
				linkContextProductBranchSelect.append(`
            <option value="${branch.id}">${branch.general.name[BusinessDefaultLanguage]}</option>
        `);
			});
		});

		switchBackToContextProductsTab.on("click", async (event) => {
			event.preventDefault();

			const canLeaveResult = await canLeaveContextProductsTab();
			if (!canLeaveResult) return;

			ShowContextProductsListTab();
			ManageContextProductType = null;
		});

		contextProductsTable.on("click", "button[button-type='editProduct']", (event) => {
			event.preventDefault();

			const productId = $(event.currentTarget).attr("product-id");

			resetOrClearContextProductManager();

			CurrentContextProductData = BusinessFullData.businessApp.context.products.find((product) => product.id === productId);

			currentContextProductName.text(CurrentContextProductData.name[BusinessDefaultLanguage]);

			// Fill branch select options
			linkContextProductBranchSelect.empty();
			BusinessFullData.businessApp.context.branches.forEach((branch) => {
				if (!CurrentContextProductData.availableAtBranches.includes(branch.id)) {
					linkContextProductBranchSelect.append(`
                <option value="${branch.id}">${branch.general.name[BusinessDefaultLanguage]}</option>
            `);
				}
			});

			fillContextProductManager(CurrentContextProductData);
			ShowContextProductsManagerTab();

			ManageContextProductType = "edit";
		});

		contextProductsManagerTab.on("input change", "input[type='text'], textarea", (event) => {
			if (ManageContextProductType == null) return;

			const currentElement = $(event.currentTarget);
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			switch (currentElement.attr("id")) {
				case "inputContextProductName":
					CurrentContextProductNameMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "inputContextProductShortDescription":
					CurrentContextProductShortDescriptionMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
				case "inputContextProductFullDescription":
					CurrentContextProductLongDescriptionMultiLangData[currentSelectedLanguage.id] = currentElement.val();
					break;
			}

			validateContextProductAllMultilanguageElements();
			CheckContextProductTabHasChanges(true);
		});

		addNewContextProductInformationButton.on("click", (event) => {
			event.preventDefault();
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			contextProductInformationList.append($(createContextProductOtherInformationElement()));

			updateContextProductOtherInformation(currentSelectedLanguage.id);
			validateContextProductAllMultilanguageElements();
			CheckContextProductTabHasChanges(true);
		});

		contextProductInformationList.on("click", '[button-type="contextProductInformationRemove"]', (event) => {
			event.preventDefault();
			event.stopPropagation();
			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			$(event.currentTarget).parent().parent().remove();

			updateContextProductOtherInformation(currentSelectedLanguage.id);
			validateContextProductAllMultilanguageElements();
			CheckContextProductTabHasChanges(true);
		});

		contextProductInformationList.on("input change", "input, textarea", (event) => {
			if (ManageContextProductType == null) return;

			const currentSelectedLanguage = ContextTabMultiLanguageDropdown.getSelectedLanguage();

			if (validateProductOtherInformationKeys()) {
				updateContextProductOtherInformation(currentSelectedLanguage.id);
			}

			validateContextProductAllMultilanguageElements();
			CheckContextProductTabHasChanges(true);
		});

		linkContextProductBranchButton.on("click", (event) => {
			event.preventDefault();

			const selectedBranchId = linkContextProductBranchSelect.val();
			if (!selectedBranchId) return;

			const selectedBranch = BusinessFullData.businessApp.context.branches.find((branch) => branch.id === selectedBranchId);
			if (!selectedBranch) return;

			linkedContextProductBranchesList.append(`
        <li class="list-group-item" branch-id="${selectedBranch.id}">
            <div class="d-flex flex-row align-items-center justify-content-between">
                <span>${selectedBranch.general.name[BusinessDefaultLanguage]}</span>
                <button class="btn btn-danger btn-sm" button-type="removeLinkedBranch">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </div>
        </li>
    `);

			// Remove from select
			linkContextProductBranchSelect.find(`option[value="${selectedBranch.id}"]`).remove();

			CheckContextProductTabHasChanges(true);
		});

		linkedContextProductBranchesList.on("click", '[button-type="removeLinkedBranch"]', (event) => {
			event.preventDefault();
			const listItem = $(event.currentTarget).closest(".list-group-item");
			const branchId = listItem.attr("branch-id");

			const branch = BusinessFullData.businessApp.context.branches.find((b) => b.id === branchId);
			if (branch) {
				linkContextProductBranchSelect.append(`
            <option value="${branch.id}">${branch.general.name[BusinessDefaultLanguage]}</option>
        `);
			}

			listItem.remove();
			CheckContextProductTabHasChanges(true);
		});

		saveContextProductsButton.on("click", async (event) => {
			event.preventDefault();

			if (IsSavingContextProductTab) return;

			const validationResult = ValidateContextProductTab(false);
			if (!validationResult.validated) {
				AlertManager.createAlert({
					type: "danger",
					message: `Validation for required product fields failed.<br><br>${validationResult.errors.join("<br>")}`,
					timeout: 6000,
				});
				return;
			}

			const productTabChanges = CheckContextProductTabHasChanges(false);
			if (!productTabChanges.hasChanges) {
				return;
			}

			saveContextProductsButton.prop("disabled", true);
			saveContextProductsButtonSpinner.removeClass("d-none");

			IsSavingContextProductTab = true;

			const formData = new FormData();
			formData.append("changes", JSON.stringify(productTabChanges.changes));
			formData.append("postType", ManageContextProductType);

			if (ManageContextProductType === "edit") {
				formData.append("exisitingProductId", CurrentContextProductData.id);
			}

			SaveBusinessContextProduct(
				formData,
				(saveResponse) => {
					CurrentContextProductData = saveResponse.data;

					currentContextProductName.text(CurrentContextProductData.name[BusinessDefaultLanguage]);

					if (ManageContextProductType === "new") {
						BusinessFullData.businessApp.context.products.push(CurrentContextProductData);
						contextProductsTable.find("tbody").append($(createContextProductTableElement(CurrentContextProductData)));

						contextProductsTable.find("tbody tr[tr-type='none-notice']").remove();
					} else {
						const productIndex = BusinessFullData.businessApp.context.products.findIndex((p) => p.id === CurrentContextProductData.id);
						if (productIndex !== -1) {
							BusinessFullData.businessApp.context.products[productIndex] = CurrentContextProductData;
						}

						contextProductsTable.find(`tbody tr[product-id="${CurrentContextProductData.id}"]`).replaceWith($(createContextProductTableElement(CurrentContextProductData)));
					}

					saveContextProductsButton.prop("disabled", true);
					saveContextProductsButtonSpinner.addClass("d-none");

					IsSavingContextProductTab = false;

					AlertManager.createAlert({
						type: "success",
						message: `Product ${ManageContextProductType === "new" ? "added" : "updated"} successfully.`,
						timeout: 6000,
					});

					ManageContextProductType = "edit";
				},
				(saveError, isUnsuccessful) => {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occurred while saving product data. Check browser console for logs.",
						timeout: 6000,
					});

					console.log("Error occurred while saving product data: ", saveError);

					saveContextProductsButton.prop("disabled", false);
					saveContextProductsButtonSpinner.addClass("d-none");

					IsSavingContextProductTab = false;
				},
			);
		});

		/** Init **/
		FillContextTab();
	});
}
