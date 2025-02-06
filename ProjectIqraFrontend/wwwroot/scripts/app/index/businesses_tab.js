const BusinessTypeEnum = {
	Unknown: 0,
	NoCode: 1,
	Advanced: 2,
};

const businessTab = $("#business-tab");

const AddNewBusinessButton = businessTab.find("#AddNewBusinessButton");
const searchBusinessInput = businessTab.find("#searchBusinessInput");
const searchBusinessButton = businessTab.find("#searchBusinessButton");
const BusinessesList = businessTab.find("#BusinessesList");

const addNewBusinessModal = businessTab.find("#addNewBusinessModal");
const addNewBusinessNameInput = addNewBusinessModal.find("#addNewBusinessNameInput");
const addNewBusinessLogoInput = addNewBusinessModal.find("#addNewBusinessLogoInput");
const addNewBusinessLogoPreview = addNewBusinessModal.find("#addNewBusinessLogoPreview");
const addNewBusinessButton = addNewBusinessModal.find("#addNewBusinessButton");

function MoveToBusinessPage(BusinessId) {
	window.location.href = "/app/business/" + BusinessId;
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

	let activeBusinessType = addNewBusinessModal.find(".new-business-type-card.active");
	if (activeBusinessType.length == 0) {
		isValid = false;
	}
	let activeBusinessTypeAttr = activeBusinessType.attr("business-type");
	if (!activeBusinessTypeAttr || !activeBusinessTypeAttr.length) {
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

/** INIT **/
function InitBusinessesTab() {
	$(document).ready(() => {
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

		$(document).on("click", "#addNewBusinessModal .new-business-type-card", (event) => {
			event.preventDefault();
			event.stopPropagation();

			let current = $(event.currentTarget);

			if (current.hasClass("disabled")) {
				return;
			}

			let currentActive = $("#addNewBusinessModal .new-business-type-card.active");

			let businessType = current.attr("business-type");
			let activeBusinessType = currentActive.attr("business-type");

			if (businessType == activeBusinessType) {
				return;
			}

			currentActive.removeClass("active");
			current.addClass("active");

			ValidateAddNewBusinessModal(true);
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
			const type = BusinessTypeEnum[addNewBusinessModal.find(".new-business-type-card.active").attr("business-type")];

			addNewBusinessButton.prop("disabled", true);

			const formData = new FormData();
			formData.append("BusinessName", name);
			if (logoFile) {
				formData.append("BusinessLogo", logoFile, logoFile.name);
			}
			formData.append("BusinessType", type);

			AddNewUserBusinessToAPI(
				formData,
				(businessData) => {
					let businessCardElement = CreateUserBusinessCard(businessData);

					BusinessesList.append(businessCardElement);

					addNewBusinessButton.prop("disabled", false);
					addNewBusinessModal.modal("hide");
				},
				(businessError) => {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occured while adding new user business. Check browser console for logs.",
						timeout: 5000,
					});

					console.log("Error occured while adding new user business: ", businessError);

					addNewBusinessButton.prop("disabled", false);
				},
			);
		});

		// Init
		FetchUserBusinessPermissionsFromAPI(
			(userBusinessPermission) => {
				CurrentUserBusinessPermission = userBusinessPermission;

				if (userBusinessPermission.disableBusinessesAt == null) {
					searchBusinessInput.prop("disabled", false);
					searchBusinessButton.prop("disabled", false);

					FetchUserBusinessesFromAPI(
						(userBusinesses) => {
							CurrentBusinessesList = userBusinesses;

							userBusinesses.forEach((businessData) => {
								const businessCardElement = CreateUserBusinessCard(businessData);

								BusinessesList.append(businessCardElement);
							});
						},
						(userBusinessesError) => {
							AlertManager.createAlert({
								type: "danger",
								message: "Error occured while fetching user businesses. Check browser console for logs.",
								enableDismiss: false,
							});

							console.log("Error occured while fetching user businesses: ", userBusinessesError);
						},
					);
				} else {
					CurrentBusinessesList = [];

					let alertMessage = "Bussiness Viewing, Adding, Editing and Deleting are disabled for your account.";
					if (userBusinessPermission.disableBusinessesReason != null) {
						alertMessage = `${alertMessage}<br>Reason: ${userBusinessPermission.disableBusinessesReason}`;
					} else {
						alertMessage = `${alertMessage}<br>Please contact support for more information.`;
					}

					AlertManager.createAlert({
						type: "danger",
						message: alertMessage,
						enableDismiss: false,
					});
				}

				if (userBusinessPermission.addBusinessDisabledAt != null || userBusinessPermission.disableBusinessesAt != null) {
					if (userBusinessPermission.disableBusinessesAt == null) {
						let alertMessage = "Bussiness Adding is disabled for your account.";

						if (userBusinessPermission.addBusinessDisableReason != null) {
							alertMessage = `${alertMessage}<br>Reason: ${userBusinessPermission.addBusinessDisableReason}`;
						} else {
							alertMessage = `${alertMessage}<br>Please contact support for more information.`;
						}

						AlertManager.createAlert({
							type: "danger",
							message: alertMessage,
							enableDismiss: false,
						});
					}
				} else {
					AddNewBusinessButton.prop("disabled", false);
				}

				if (userBusinessPermission.editBusinessDisabledAt != null || userBusinessPermission.disableBusinessesAt != null) {
					if (userBusinessPermission.disableBusinessesAt == null) {
						let alertMessage = "Bussiness Editing is disabled for your account.";

						if (userBusinessPermission.editBusinessDisableReason != null) {
							alertMessage = `${alertMessage}<br>Reason: ${userBusinessPermission.editBusinessDisableReason}`;
						} else {
							alertMessage = `${alertMessage}<br>Please contact support for more information.`;
						}

						AlertManager.createAlert({
							type: "danger",
							message: alertMessage,
							enableDismiss: false,
						});
					}
				}

				if (userBusinessPermission.deleteBusinessDisableAt != null || userBusinessPermission.disableBusinessesAt != null) {
					if (userBusinessPermission.disableBusinessesAt == null) {
						let alertMessage = "Bussiness Deleting is disabled for your account.";

						if (userBusinessPermission.deleteBusinessDisableReason != null) {
							alertMessage = `${alertMessage}<br>Reason: ${userBusinessPermission.deleteBusinessDisableReason}`;
						} else {
							alertMessage = `${alertMessage}<br>Please contact support for more information.`;
						}

						AlertManager.createAlert({
							type: "danger",
							message: alertMessage,
							enableDismiss: false,
						});
					}
				}
			},
			(userBusinessPermissionError) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while fetching user businesses. Check browser console for logs.",
					enableDismiss: false,
				});

				console.log("Error occured while fetching user businesses: ", userBusinessPermissionError);
			},
		);
	});
}
