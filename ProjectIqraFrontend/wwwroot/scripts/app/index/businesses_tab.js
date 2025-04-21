/** Global Variables */

/** Element Varables **/
const businessTab = $("#business-tab");

const AddNewBusinessButton = businessTab.find("#AddNewBusinessButton");
const searchBusinessInput = businessTab.find("#searchBusinessInput");
const searchBusinessButton = businessTab.find("#searchBusinessButton");
const BusinessesList = businessTab.find("#BusinessesList");

const addNewBusinessModal = $("#addNewBusinessModal");
const addNewBusinessNameInput = addNewBusinessModal.find("#addNewBusinessNameInput");
const addNewBusinessLogoInput = addNewBusinessModal.find("#addNewBusinessLogoInput");
const addNewBusinessLogoPreview = addNewBusinessModal.find("#addNewBusinessLogoPreview");
const addNewBusinessDefaultLanguageSelect = addNewBusinessModal.find("#addNewBusinessDefaultLanguageSelect");

const addNewBusinessButton = addNewBusinessModal.find("#addNewBusinessButton");
const addNewBusinessButtonSpinner = addNewBusinessButton.find(".save-button-spinner");

/** Functions **/
function MoveToBusinessPage(BusinessId) {
	window.location.href = "/business/" + BusinessId;
}

function CreateUserBusinessCard(businessData) {
	let element = `
                <div class="col-lg-4 col-md-6 col-12">
                    <a href="/app/business/${businessData.id}" class="business-card d-flex flex-column align-items-start justify-content-center" data-business-id="${businessData.id}">
                        <div class="d-flex flex-row align-items-center justify-content-start">
                            <img src="${BusinessLogoURL + "/" + businessData.logoURL}.webp">
                            <h4>${businessData.name}</h4>
                        </div>
                    </a>
                </div>
            `;

	return $(element);
}

function ValidateAddNewBusinessModal(enableDisableButton = false) {
	let isValid = true;

	if (addNewBusinessNameInput.val().trim().length == 0) {
		isValid = false;
	}

	const selectedLanguage = addNewBusinessDefaultLanguageSelect.find("option:selected").val();
	if (!selectedLanguage || selectedLanguage == null || selectedLanguage.length == 0 || selectedLanguage == "none") {
		isValid = false;
	}

	if (enableDisableButton) {
		if (isValid) {
			addNewBusinessButton.prop("disabled", false);
		} else {
			addNewBusinessButton.prop("disabled", true);
		}
	}

	return isValid;
}

function FillBusinessList() {
	CurrentBusinessesList.forEach((businessData) => {
		const businessCardElement = CreateUserBusinessCard(businessData);

		BusinessesList.append(businessCardElement);
	});
}

function FillAddNewBusinessModalDefaults() {
	SpecificationLanguagesListData.forEach((languageData) => {
		if (languageData.disabledAt != null) return;

		let optionElement = `<option value="${languageData.id}">${languageData.name} | ${languageData.id}</option>`;

        addNewBusinessDefaultLanguageSelect.append(optionElement);
	});
}

/** INIT **/
function InitBusinessesTab() {
	// Init
	FillBusinessList();
	FillAddNewBusinessModalDefaults();

	// Event Handlers
	$(document).on("click", ".business-card", (event) => {
		event.preventDefault();

		let currentCardElement = $(event.currentTarget);
		let businessDataId = currentCardElement.attr("data-business-id");

		if (!businessDataId || !businessDataId.length) {
			return;
		}

		if (isBusinessAnimationEnabled) {
			$("body").css("overflow", "hidden");

			let transitionTimeMS = 1000;

			currentCardElement.css("transition", `all ${transitionTimeMS}ms ease`);
			currentCardElement.css("transform", "scale(40)");
			currentCardElement.css("background-color", "#1a1a1a");
			currentCardElement.css("color", "#1a1a1a");
			currentCardElement.css("z-index", "99999");

			currentCardElement.find("h4").css("transition", `all ${parseInt(transitionTimeMS / 4)}ms ease`);
			currentCardElement.find("h4").css("opacity", "0");

			setTimeout(() => {
				MoveToBusinessPage(businessDataId);
			}, transitionTimeMS / 1.1);
		} else {
			MoveToBusinessPage(businessDataId);
		}
	});

	addNewBusinessNameInput.on("input", (event) => {
		ValidateAddNewBusinessModal(true);
	});

	addNewBusinessLogoPreview.on("click", (event) => {
		addNewBusinessLogoInput.trigger("click");
	});

	addNewBusinessLogoInput.on("change", (event) => {
		let file = event.currentTarget.files[0];

		if (!file) {
			return;
		}

		let reader = new FileReader();

		reader.onload = (event) => {
			addNewBusinessLogoPreview.attr("src", event.target.result);
		};

		reader.readAsDataURL(file);

		ValidateAddNewBusinessModal(true);
	});

	addNewBusinessDefaultLanguageSelect.on("change", (event) => {
		ValidateAddNewBusinessModal(true);
	})

	addNewBusinessModal.on("hidden.bs.modal", (event) => {
		addNewBusinessNameInput.val("");
		addNewBusinessLogoInput.val("");
		addNewBusinessLogoPreview.attr("src", "/img/picture_placeholder_light.png");

		ValidateAddNewBusinessModal(true);
	});

	addNewBusinessButton.on("click", (event) => {
		event.preventDefault();

		const name = addNewBusinessNameInput.val().trim();
		const logoFile = addNewBusinessLogoInput[0].files[0];
		const selectedLanguage = addNewBusinessDefaultLanguageSelect.find("option:selected").val();

		addNewBusinessButton.prop("disabled", true);
		addNewBusinessButtonSpinner.removeClass("d-none");

		const formData = new FormData();
		formData.append("BusinessName", name);
		if (logoFile) {
			formData.append("BusinessLogo", logoFile, logoFile.name);
		}
		formData.append("BusinessDefaultLanguage", selectedLanguage);

		AddNewUserBusinessToAPI(
			formData,
			(businessData) => {
				AlertManager.createAlert({
					type: "success",
					message: `Business "${name}" added successfully!`,
					timeout: 3000,
				});

				let businessCardElement = CreateUserBusinessCard(businessData);

				BusinessesList.append(businessCardElement);

				addNewBusinessButton.prop("disabled", false);
				addNewBusinessButtonSpinner.addClass("d-none");
				addNewBusinessModal.modal("hide");
			},
			(businessError) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while adding new user business. Check browser console for logs.",
					timeout: 5000,
				});

				console.error("Error occured while adding new user business: ", businessError);

				addNewBusinessButton.prop("disabled", false);
				addNewBusinessButtonSpinner.addClass("d-none");
			},
		);
	});
}
