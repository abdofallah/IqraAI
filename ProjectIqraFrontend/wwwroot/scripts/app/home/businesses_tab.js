/** Global Variables */
var isDeletingBusiness = false;

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

/** API Functions **/
function AddNewUserBusinessToAPI(formData, successCallback, errorCallback) {
	$.ajax({
		url: '/app/user/business/add',
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

			successCallback(response.data);
		},
		error: (error) => {
			errorCallback(error);
		}
	});
}

function DeleteBusinessFromAPI(formData, successCallback, errorCallback) {
    $.ajax({
        url: '/app/user/business/delete',
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

            successCallback(response.data);
        },
		error: (error) => {
			errorCallback(error);
		}
    });
}

/** Functions **/
function MoveToBusinessPage(BusinessId) {
	window.location.href = "/business/" + BusinessId;
}

function CreateUserBusinessCard(businessData) {
	let element = `
                <div class="col-lg-4 col-md-6 col-12">
                    <a href="/business/${businessData.id}" class="business-card d-flex flex-column align-items-start justify-content-center" data-business-id="${businessData.id}">
						<div class="dropdown action-dropdown dropstart dropdown-menu-end">
							<button class="btn action-button dropdown-toggle" type="button" data-bs-toggle="dropdown" aria-expanded="false"><i class="fa-solid fa-ellipsis"></i></button>
							<ul class="dropdown-menu">
								<li>
									<span class="dropdown-item text-danger" data-business-id="${businessData.id}" button-type="delete-business"><i class="fa-solid fa-trash me-2"></i>Delete</span>
								</li>
							</ul>
						</div>
						
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

function SetBusinessCardH4Width() {
	const anyBusinessCard = BusinessesList.find(".business-card");
	if (anyBusinessCard.length > 0) {
		const firstBusinessCard = anyBusinessCard.first();

		const businessCardWidth = firstBusinessCard.innerWidth();

		const businessCardLeftRightPadding = parseInt(firstBusinessCard.css("padding-left")) + parseInt(firstBusinessCard.css("padding-right"));
		const businessCardImageWidthAndPadding = firstBusinessCard.find("img").innerWidth();
		const marginLeftForH4 = 20; // .business-card h4 in style.css

		const currentUsedUpSpace = businessCardLeftRightPadding + businessCardImageWidthAndPadding + marginLeftForH4;

		let availableH4Space = businessCardWidth - currentUsedUpSpace;

		if (availableH4Space < 5) {
            availableH4Space = 5;
		}

		$("#dynamicBusinessCardH4CSS").html(`
            .business-card h4 {
				width: ${availableH4Space}px;
			}
		`);
    }
}

/** INIT **/
function InitBusinessesTab() {
	// Init
	FillBusinessList();
	FillAddNewBusinessModalDefaults();
	SetBusinessCardH4Width();

	// Event Handlers
	$(window).resize(() => {
		SetBusinessCardH4Width();
	});

	$(document).on("containerResizeProgress", (event) => {
        SetBusinessCardH4Width();
	})

	BusinessesList.on("click", ".business-card", (event) => {
		event.preventDefault();
		event.stopPropagation();

		// check if target was button or its icon
		if ($(event.target).closest(".dropdown").length != 0) {
			return;
        }

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

	BusinessesList.on("click", ".business-card [button-type=delete-business]", async (event) => {
		event.preventDefault();
		event.stopPropagation();

		if (isDeletingBusiness) {
			AlertManager.createAlert({
				type: "warning",
				message: "Please wait as there is another business being deleted already.",
				timeout: 3000,
			});

			return;
		}

        let currentCardElement = $(event.currentTarget);
		let businessDataId = currentCardElement.attr("data-business-id");

		var businessData = CurrentBusinessesList.find((business) => business.id == businessDataId);

		const confirmDialog = new BootstrapConfirmDialog({
			title: `Delete Business (${businessData.name})`,
			message: "Are you sure you want to delete this business? This action cannot be undone.",
			confirmText: "Delete",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "modal-lg",
		});

		const confirmResult = await confirmDialog.show();
		if (!confirmResult) {
			return;
		}

		isDeletingBusiness = true;

		const formData = new FormData();
        formData.append("businessId", businessDataId);

		DeleteBusinessFromAPI(
			formData,
			(successResponse) => {
				var businessIndex = CurrentBusinessesList.findIndex((business) => business.id == businessDataId);
				CurrentBusinessesList.splice(businessIndex, 1);

				BusinessesList.find(`.business-card[data-business-id=${businessDataId}]`).parent().remove();

				isDeletingBusiness = false;
			},
			(errorResponse) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while adding deleting user business. Check browser console for logs.",
					timeout: 5000,
				});

				console.error("Error occured while adding deleting user business: ", errorResponse);

				isDeletingBusiness = false;
			}
		);
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
				SetBusinessCardH4Width();
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
