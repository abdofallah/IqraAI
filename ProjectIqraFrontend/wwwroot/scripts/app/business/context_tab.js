const DAYS = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

/** Dynamic Variables **/
let ContentTabMultiLanguageDropdown = null;

// Context Branding State
let CurrentContextBrandingData = null;
let CurrentContextBrandingNameMultiLangData = {};
let CurrentContextBrandingCountryMultiLangData = {};
let CurrentContextBrandingEmailMultiLangData = {};
let CurrentContextBrandingPhoneMultiLangData = {};
let CurrentContextBrandingWebsiteMultiLangData = {};
let CurrentContextBrandingOtherInformationMultiLangData = {};

// Context Branch State
let CurrentContextBranchData = null;
let ManageContextBranchType = null; // new or edit
let CurrentContextBranchNameMultiLangData = {};
let CurrentContextBranchAddressMultiLangData = {};
let CurrentContextBranchPhoneMultiLangData = {};
let CurrentContextBranchEmailMultiLangData = {};
let CurrentContextBranchWebsiteMultiLangData = {};
let CurrentContextBranchOtherInformationMultiLangData = {};
let CurrentContextBranchTeamMultiLangData = {};

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

// Branding
const brandingTab = contextTab.find("#context-inner-branding");

const brandingBrandNameInput = brandingTab.find("#brandingBrandNameInput");
const brandingBrandCountryInput = brandingTab.find("#brandingBrandCountryInput");
const brandingGlobalContactInput = brandingTab.find("#brandingGlobalContactInput");
const brandingGlobalPhoneInput = brandingTab.find("#brandingGlobalPhoneInput");
const brandingGlobalWebsiteInput = brandingTab.find("#brandingGlobalWebsiteInput");

const addNewBrandInformationButton = contextTab.find("#addNewBrandInformation");
const brandingBranchInformationList = contextTab.find("#brandingBranchInformationList");

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

const branchesManagerTab = contextTab.find("#branchesManagerTab");

const editBranchUniqueIDInput = branchesManagerTab.find("#editBranchUniqueIDInput");
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

const contextProductsManagerTab = contextTab.find("#contextProductsManagerTab");

const inputContextProductName = contextProductsManagerTab.find("#inputContextProductName");
const inputContextProductShortDescription = contextProductsManagerTab.find("#inputContextProductShortDescription");
const inputContextProductFullDescription = contextProductsManagerTab.find("#inputContextProductFullDescription");

const linkContextProductBranchSelect = contextTab.find("#linkContextProductBranchSelect");
const linkedContextProductBranchesList = contextTab.find("#linkedContextProductBranchesList");

const addNewContextProductInformationButton = contextTab.find("#addNewContextProductInformation");
const contextProductInformationList = contextTab.find("#contextProductInformationList");

/** Functions  **/

function resetOrClearBranchManager() {
	$("#editBranchUniqueIDInput").val("");
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
}

function resetOrClearServiceManager() {
	$("#inputContextServiceName").val("");
	$("#inputContextServiceShortDescription").val("");
	$("#inputContextServiceFullDescription").val("");

	linkContextServiceBranchSelect.children().remove();
	linkedContextServiceBranchesList.children().remove();

	linkContextServiceProductSelect.children().remove();
	linkedContextServiceProductsList.children().remove();

	contextServiceInformationList.children().remove();
}

function resetOrClearProductManager() {
	$("#inputContextProductName").val("");
	$("#inputContextProductShortDescription").val("");
	$("#inputContextProductFullDescription").val("");

	linkContextProductBranchSelect.children().remove();
	linkedContextProductBranchesList.children().remove();

	contextProductInformationList.children().remove();
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
		setTimeout(() => {
			branchesManagerTab.addClass("show");
			branchesManagerTabButtonHeader.addClass("show");
			branchesManageTabHeader.addClass("show");
			saveContextBranchesButton.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ShowContextBranchesListTab() {
	branchesManagerTab.removeClass("show");
	branchesManagerTabButtonHeader.removeClass("show");
	branchesManageTabHeader.removeClass("show");
	saveContextBranchesButton.removeClass("d-none");
	setTimeout(() => {
		branchesManagerTab.addClass("d-none");
		branchesManagerTabButtonHeader.addClass("d-none");
		branchesManageTabHeader.addClass("d-none");
		saveContextBranchesButton.addClass("d-none");

		branchesListTab.removeClass("d-none");
		contextTabButtonHeader.removeClass("d-none");
		setTimeout(() => {
			branchesListTab.addClass("show");
			contextTabButtonHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
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
		if (language === ContentTabMultiLanguageDropdown.getSelectedLanguage().id) {
			brandingBranchInformationList.children().each((idx, element) => {
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
		if (language === ContentTabMultiLanguageDropdown.getSelectedLanguage().id) {
			brandingBranchInformationList.children().each((idx, element) => {
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
		ContentTabMultiLanguageDropdown.setLanguageStatus(currentLanguage.id, isAnyFieldIncomplete ? "incomplete" : "complete");
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

	// Other Information validation
	const infoElements = brandingBranchInformationList.children();
	infoElements.each((idx, element) => {
		const currentElement = $(element);
		const keyInput = currentElement.find('[data-type="key"]');
		const valueInput = currentElement.find('[data-type="value"]');

		if (keyInput.val().trim() === "") {
			validated = false;
			errors.push(`Information type #${idx + 1} is required.`);

			if (!onlyRemove) {
				keyInput.addClass("is-invalid");
			}
		} else {
			keyInput.removeClass("is-invalid");
		}

		if (valueInput.val().trim() === "") {
			validated = false;
			errors.push(`Information value #${idx + 1} is required.`);

			if (!onlyRemove) {
				valueInput.addClass("is-invalid");
			}
		} else {
			valueInput.removeClass("is-invalid");
		}
	});

	return {
		validated: validated,
		errors: errors,
	};
}

function updateBrandingOtherInformation(languageId) {
	CurrentContextBrandingOtherInformationMultiLangData[languageId] = {};

	brandingBranchInformationList.children().each((idx, element) => {
		const infoType = $(element).find("input").val().trim();
		const infoValue = $(element).find("textarea").val().trim();

		if (infoType) {
			CurrentContextBrandingOtherInformationMultiLangData[languageId][infoType] = infoValue;
		}
	});
}

function FillContextTab() {
	// Branding
	function fillBrandingTab() {
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
		fillOtherInformationForLanguage(BusinessDefaultLanguage);

		validateContextBrandingAllMultilanguageElements();
		saveContextBrandingButton.prop("disabled", true);
	}
	fillBrandingTab();

	// Branches
	function fillBranchesTab() {
		// Implementation coming next
	}
	fillBranchesTab();

	// Services
	function fillServicesTab() {
		// Implementation coming next
	}
	fillServicesTab();

	// Products
	function fillProductsTab() {
		// Implementation coming next
	}
	fillProductsTab();
}

function fillOtherInformationForLanguage(language) {
	brandingBranchInformationList.empty();
	Object.entries(CurrentContextBrandingOtherInformationMultiLangData[language] || {}).forEach(([key, value]) => {
		const infoBox = $(CreateContextBrandingOtherInformationElement());
		brandingBranchInformationList.append(infoBox);

		infoBox.find('[data-type="key"]').val(key);
		infoBox.find('[data-type="value"]').val(value);
	});
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

function initContextTab() {
	$(document).ready(() => {
		ContentTabMultiLanguageDropdown = new MultiLanguageDropdown("contentTabMultiLanguageSelectContainer", BusinessFullLanguagesData);

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

		addNewBrandInformationButton.on("click", (event) => {
			event.preventDefault();
			const currentSelectedLanguage = ContentTabMultiLanguageDropdown.getSelectedLanguage();

			brandingBranchInformationList.append($(CreateContextBrandingOtherInformationElement()));

			updateBrandingOtherInformation(currentSelectedLanguage.id);
			validateContextBrandingAllMultilanguageElements();
			CheckContextBrandingTabHasChanges(true);
		});

		$(document).on("click", '[button-type="brandingInformationRemove"]', (event) => {
			event.preventDefault();
			const currentSelectedLanguage = ContentTabMultiLanguageDropdown.getSelectedLanguage();

			$(event.currentTarget).parent().parent().remove();

			updateBrandingOtherInformation(currentSelectedLanguage.id);
			validateContextBrandingAllMultilanguageElements();
			CheckContextBrandingTabHasChanges(true);
		});

		addNewBranchButton.on("click", (event) => {
			event.preventDefault();

			currentBranchName.text("New Branch");

			resetOrClearBranchManager();

			ShowContextBranchesManagerTab();
		});

		switchBackToBranchesTab.on("click", (event) => {
			event.preventDefault();

			ShowContextBranchesListTab();
		});

		addContextBranchInformationButton.on("click", (event) => {
			event.preventDefault();

			contextBranchInformationList.append(`
                              <div class="mt-2">
                                   <div class="input-group">
                                        <input type="text" class="form-control" placeholder="Information Type" aria-label="Information Type" value="" style="border-bottom: none; border-bottom-left-radius: 0;">
                                        <button class="btn btn-danger" button-type="contextBranchInformationRemove" style="border-bottom: none; border-bottom-right-radius: 0;">
                                             <i class='fa-regular fa-trash'></i>
                                        </button>
                                   </div>
                                   <textarea class="form-control" placeholder="Information" aria-label="Information" style="border-top-left-radius: 0; border-top-right-radius: 0"></textarea>
                              </div>
                         `);
		});

		$(document).on("click", '[button-type="contextBranchInformationRemove"]', (event) => {
			event.preventDefault();

			$(event.currentTarget).parent().parent().remove();
		});

		$(document).on("click", '[checkbox-type="branchWorkingHourIsClosed"]', (event) => {
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

		$(document).on("click", '[button-type="addBranchWorkingHour"]', (event) => {
			const dayValue = $(event.currentTarget).attr("day-value");

			const dayTimingsList = $(`.workingHoursTimingList[day-value="${dayValue}"]`);

			dayTimingsList.append(`
                              <div class="d-flex flex-row mt-1">
                                   <input type="time" class="form-control" time-type="from" style="border-top-right-radius: 0; border-bottom-right-radius: 0">
                                   <input type="time" class="form-control" time-type="to" style="border-radius: 0; border-left: none;">
                                   <button class="btn btn-danger" button-type="removeBranchWorkingHour" style="border-top-left-radius: 0; border-bottom-left-radius: 0">
                                        <i class="fa-regular fa-trash"></i>
                                   </button>
                              </div>
                         `);
		});

		$(document).on("click", '[button-type="removeBranchWorkingHour"]', (event) => {
			$(event.currentTarget).parent().remove();
		});

		editBranchAddTeamMember.on("click", (event) => {
			event.preventDefault();

			editBranchTeamInputsList.append(`
                              <div class="col-12 col-md-6 mt-3">
                                   <div class="editBranchTeamBox">
                                        <div class="d-flex flex-row gap-2 mb-2">
                                             <div class="w-100">
                                                  <label for="editBranchTeamNameInput" class="form-label">Name</label>
                                                  <input type="text" class="form-control" id="editBranchTeamNameInput" placeholder="Name">
                                             </div>
                                             <div class="w-100">
                                                  <label for="editBranchTeamRoleInput" class="form-label">Role</label>
                                                  <input type="text" class="form-control" id="editBranchTeamRoleInput" placeholder="Role">
                                             </div>
                                        </div>
                                        <div class="d-flex flex-row gap-2 mb-2">
                                             <div class="w-100">
                                                  <label for="editBranchTeamEmailInput" class="form-label">Email</label>
                                                  <input type="text" class="form-control" id="editBranchTeamEmailInput" placeholder="Email">
                                             </div>
                                             <div class="w-100">
                                                  <label for="editBranchTeamPhoneInput" class="form-label">Phone</label>
                                                  <input type="tel" class="form-control" id="editBranchTeamPhoneInput" placeholder="Phone">
                                             </div>
                                        </div>

                                        <div class="mb-2">
                                             <label for="editBranchTeamInformationInput" class="form-label">Information</label>
                                             <textarea class="form-control" id="editBranchTeamInformationInput" placeholder="Information" style="min-height: 70px"></textarea>
                                        </div>

                                        <button class="btn btn-danger w-100" button-type="removeEditBranchTeam">
                                             <i class="fa-regular fa-trash"></i>
                                        </button>
                                   </div>
                              </div>
                         `);
		});

		$(document).on("click", '[button-type="removeEditBranchTeam"]', (event) => {
			$(event.currentTarget).parent().parent().remove();
		});

		addNewServiceButton.on("click", (event) => {
			event.preventDefault();

			currentContextServiceName.text("New Service");

			resetOrClearServiceManager();

			ShowContextServicesManagerTab();
		});

		switchBackToContextServicesTab.on("click", (event) => {
			event.preventDefault();

			ShowContextServicesListTab();
		});

		addNewContextServiceInformationButton.on("click", (event) => {
			event.preventDefault();

			contextServiceInformationList.append(`
                              <div class="mt-2">
                                   <div class="input-group">
                                        <input type="text" class="form-control" placeholder="Information Type" aria-label="Information Type" value="" style="border-bottom: none; border-bottom-left-radius: 0;">
                                        <button class="btn btn-danger" button-type="contextServiceInformationRemove" style="border-bottom: none; border-bottom-right-radius: 0;">
                                             <i class='fa-regular fa-trash'></i>
                                        </button>
                                   </div>
                                   <textarea class="form-control" placeholder="Information" aria-label="Information" style="border-top-left-radius: 0; border-top-right-radius: 0"></textarea>
                              </div>
                         `);
		});

		$(document).on("click", '[button-type="contextServiceInformationRemove"]', (event) => {
			$(event.currentTarget).parent().parent().remove();
		});

		addNewProductButton.on("click", (event) => {
			event.preventDefault();

			currentContextProductName.text("New Product");

			resetOrClearProductManager();

			ShowContextProductsManagerTab();
		});

		switchBackToContextProductsTab.on("click", (event) => {
			event.preventDefault();

			ShowContextProductsListTab();
		});

		addNewContextProductInformationButton.on("click", (event) => {
			event.preventDefault();

			contextProductInformationList.append(`
                              <div class="mt-2">
                                   <div class="input-group">
                                        <input type="text" class="form-control" placeholder="Information Type" aria-label="Information Type" value="" style="border-bottom: none; border-bottom-left-radius: 0;">
                                        <button class="btn btn-danger" button-type="contextProductInformationRemove" style="border-bottom: none; border-bottom-right-radius: 0;">
                                             <i class='fa-regular fa-trash'></i>
                                        </button>
                                   </div>
                                   <textarea class="form-control" placeholder="Information" aria-label="Information" style="border-top-left-radius: 0; border-top-right-radius: 0"></textarea>
                              </div>
                         `);
		});

		$(document).on("click", '[button-type="contextProductInformationRemove"]', (event) => {
			$(event.currentTarget).parent().parent().remove();
		});

		$("#context-inner-tab button.nav-link").on("show.bs.tab", (event) => {
			const newTabId = $(event.target).attr("id");

			if (newTabId !== "context-inner-branding-tab") {
				saveContextBrandingButton.removeClass("show");

				setTimeout(() => {
					saveContextBrandingButton.addClass("d-none");

					setDynamicBodyHeight();
				}, 300);
			} else {
				saveContextBrandingButton.removeClass("d-none");

				setTimeout(() => {
					saveContextBrandingButton.addClass("show");

					setDynamicBodyHeight();
				}, 10);
			}
		});

		ContentTabMultiLanguageDropdown.onLanguageChange((language) => {
			// Update branding fields
			brandingBrandNameInput.val(CurrentContextBrandingNameMultiLangData[language.id] || "");
			brandingBrandCountryInput.val(CurrentContextBrandingCountryMultiLangData[language.id] || "");
			brandingGlobalContactInput.val(CurrentContextBrandingEmailMultiLangData[language.id] || "");
			brandingGlobalPhoneInput.val(CurrentContextBrandingPhoneMultiLangData[language.id] || "");
			brandingGlobalWebsiteInput.val(CurrentContextBrandingWebsiteMultiLangData[language.id] || "");

			// Update other information list
			fillOtherInformationForLanguage(language.id);

			validateContextBrandingAllMultilanguageElements();
		});

		brandingTab.on("input change", "input[type='text'], input[type='email'], input[type='tel'], textarea", (event) => {
			const currentElement = $(event.currentTarget);
			const currentSelectedLanguage = ContentTabMultiLanguageDropdown.getSelectedLanguage();

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
			if ($(event.target).closest("#brandingBranchInformationList").length > 0) {
				updateBrandingOtherInformation(currentSelectedLanguage.id);
			}

			validateContextBrandingAllMultilanguageElements();
			CheckContextBrandingTabHasChanges(true);
		});

		// Init
		FillContextTab();
	});
}
