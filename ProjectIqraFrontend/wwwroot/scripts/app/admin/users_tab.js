var CurrentManageUserBusinesses = [];
var CurrentManageUserNumbers = [];

var CurrentManageAddUserBusinessesSearched = [];
var CurrentManageAddUserNumbersSearched = [];

var CurrentManageUserEmail = "";
var CurrentManageUserAddBusiness = [];
var CurrentManageUserDeletedBusiness = [];
var CurrentManageUserAddNumbers = [];
var CurrentManageUserDeletedNumbers = [];

var UsersTabListTabPage = 0;
var UsersTabListTabPageSize = 30;

var UsersTabAddBusinessModalListPage = 0;
var UsersTabAddBusinessModalListPageSize = 30;

var IsEditUserAllowed = false;

const usersTab = $("#users-tab");

const usersListTable = usersTab.find("#usersListTable");

const usersListTableTab = usersTab.find("#usersListTableTab");
const usersManageTab = usersTab.find("#usersManageTab");

const usersInnerTab = usersTab.find("#users-inner-tab");
const usersManageBreadcrumb = usersTab.find("#users-manage-breadcrumb");

const switchBackToUsersListTabFromManageTab = usersTab.find("#switchBackToUsersListTabFromManageTab");
const currentManageUserName = usersTab.find("#currentManageUserName");

const addUserBusinessModal = usersTab.find("#addUserBusinessModal");
const addUserBusinessModalListTable = usersTab.find("#addUserBusinessModalListTable");

const addNewUserButton = usersTab.find("#addNewUserButton");

const usersManageGeneralTab = usersTab.find("#users-manage-general-tab");

const addUserBusinessModalSearchInput = usersTab.find("#addUserBusinessModalSearchInput");
const searchAddUserBusinessModalButton = usersTab.find("#searchAddUserBusinessModalButton");

const addUserBusinessModalSaveButton = usersTab.find("#addUserBusinessModalSaveButton");

const userBusinessesListTable = usersTab.find("#userBusinessesListTable");
const userNumbersListTable = usersTab.find("#userNumbersListTable");

const usersManageDisableCompleteBusinessInput = usersTab.find("#usersManageDisableCompleteBusinessInput");
const usersManageDisableCompleteBusinessReasonInput = usersTab.find("#usersManageDisableCompleteBusinessReasonInput");

const usersManageFirstNameInput = usersManageTab.find("#usersManageFirstNameInput");
const usersManageLastNameInput = usersManageTab.find("#usersManageLastNameInput");
const usersManageEmailInput = usersManageTab.find("#usersManageEmailInput");

const usersManageDisableLoginInput = usersManageTab.find("#usersManageDisableLoginInput");
const usersManageLoginDisabledReasonInput = usersManageTab.find("#usersManageLoginDisabledReasonInput");

const usersManageDisableEditBusinessInput = usersManageTab.find("#usersManageDisableEditBusinessInput");
const usersManageDisableEditBusinessReasonInput = usersManageTab.find("#usersManageDisableEditBusinessReasonInput");

const usersManageDisableDeleteBusinessInput = usersManageTab.find("#usersManageDisableDeleteBusinessInput");
const usersManageDisableDeleteBusinessReasonInput = usersManageTab.find("#usersManageDisableDeleteBusinessReasonInput");

const usersManageDisableAddBusinessInput = usersManageTab.find("#usersManageDisableAddBusinessInput");
const usersManageDisableAddBusinessReasonInput = usersManageTab.find("#usersManageDisableAddBusinessReasonInput");

function CreateUsersListTableElement(userData) {
	let element = $(`<tr>
                <td>${userData.email}</td>
                <td>${userData.firstName} ${userData.lastName}</td>
                <td>${userData.businesses.length}</td>
                <td>${userData.numbers.length}</td>
                <td>
                    <button class="btn btn-info btn-sm" user-email="${userData.email}" button-type="edit-user">
                        <i class="fa-regular fa-eye"></i>
                    </button>
                    <button class="btn btn-danger btn-sm">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}

function CreateUserManageBusinessesTableElement(businessData, isNew = false) {
	let element = $(`<tr>
                <td>${businessData.id}</td>
                <td>${businessData.name}</td>
                <td>${businessData.numberIds.length}</td>
                <td>${businessData.subUsers.length}</td>
                <td>
                    <button class="btn btn-info btn-sm" business-id="${businessData.id}" button-type="edit-business" ${isNew ? "disabled" : ""}>
                        <i class="fa-regular fa-eye"></i>
                    </button>
                    <button class="btn btn-danger btn-sm" business-id="${businessData.id}" button-type="${isNew ? "remove-added-business" : "delete-business"}">
                        <i class="fa-regular ${isNew ? "fa-minus" : "fa-trash"}"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}

function CreateUserManageNumbersTableElement(numberData) {
	let countryData = CountriesList[numberData.countryCode.toUpperCase()];

	let element = $(`<tr>
                <td>${numberData.id}</td>
                <td>${countryData["Alpha-2 code"]}</td>
                <td>${numberData.number}</td>
                <td>${numberData.provider.name}</td>
                <td>${numberData.assignedToBusinessId}</td>
                <td>
                    <button class="btn btn-info btn-sm" number-email="${numberData.id}" button-type="edit-number">
                        <i class="fa-regular fa-eye"></i>
                    </button>
                    <button class="btn btn-danger btn-sm">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}

function ResetAndEmptyUsersManageTabData() {
	usersManageTab.find("input[type=text], input[type=email], input[type=number], textarea").val("");
	usersManageTab.find("input[type=checkbox]").prop("checked", false).change();
	usersManageTab.find("table tbody").empty();
}

function FillUserManageTab(userData, userBusinessesData, userNumbersData) {
	ResetAndEmptyUsersManageTabData();

	// General
	usersManageFirstNameInput.val(userData.firstName);
	usersManageLastNameInput.val(userData.lastName);
	usersManageEmailInput.val(userData.email);

	// Permissions
	const permissions = userData.permission;

	SetPermissionInput(usersManageDisableLoginInput, usersManageLoginDisabledReasonInput, permissions.loginDisabledAt, permissions.loginDisabledReason);

	SetPermissionInput(usersManageDisableCompleteBusinessInput, usersManageDisableCompleteBusinessReasonInput, permissions.business.disableBusinessesAt, permissions.business.disableBusinessesReason);
	SetPermissionInput(usersManageDisableEditBusinessInput, usersManageDisableEditBusinessReasonInput, permissions.business.editBusinessDisabledAt, permissions.business.editBusinessDisabledReason);
	SetPermissionInput(
		usersManageDisableDeleteBusinessInput,
		usersManageDisableDeleteBusinessReasonInput,
		permissions.business.deleteBusinessDisableAt,
		permissions.business.deleteBusinessDisabledReason,
	);
	SetPermissionInput(usersManageDisableAddBusinessInput, usersManageDisableAddBusinessReasonInput, permissions.business.addBusinessDisabledAt, permissions.business.addBusinessDisabledReason);

	// Businesses
	userBusinessesListTable.find("tbody").empty();
	if (userBusinessesData.length > 0) {
		userBusinessesData.forEach((businessData) => {
			userBusinessesListTable.find("tbody").append(CreateUserManageBusinessesTableElement(businessData));
		});
	} else {
		userBusinessesListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="5">No businesses</td></tr>');
	}

	// Numbers
	userNumbersListTable.find("tbody").empty();
	if (userNumbersData.length > 0) {
		userNumbersData.forEach((numberData) => {
			console.log(numberData);
			userNumbersListTable.find("tbody").append(CreateUserManageNumbersTableElement(numberData));
		});
	} else {
		userNumbersListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="6">No numbers</td></tr>');
	}
}

function ShowUserManageTab() {
	usersInnerTab.removeClass("show");
	usersListTableTab.removeClass("show");
	setTimeout(() => {
		usersInnerTab.addClass("d-none");
		usersListTableTab.addClass("d-none");

		usersManageBreadcrumb.removeClass("d-none");
		usersManageTab.removeClass("d-none");

		usersManageGeneralTab.click();

		setTimeout(() => {
			usersManageBreadcrumb.addClass("show");
			usersManageTab.addClass("show");
		}, 10);
	}, 300);
}

function ShowUserListTab() {
	usersManageBreadcrumb.removeClass("show");
	usersManageTab.removeClass("show");
	setTimeout(() => {
		usersManageBreadcrumb.addClass("d-none");
		usersManageTab.addClass("d-none");

		usersInnerTab.removeClass("d-none");
		usersListTableTab.removeClass("d-none");

		setTimeout(() => {
			usersInnerTab.addClass("show");
			usersListTableTab.addClass("show");
		}, 10);
	}, 300);
}

function CreateAddUserBusinessListTableElement(businessData, disabled = false) {
	let element = $(`<tr class="selectable ${disabled === true ? "disabled" : ""}" business-id="${businessData.id}" tr-type="add-user-business-modal">
                <td>${businessData.id}</td>
                <td>${businessData.name}</td>
                <td>${businessData.masterUserEmail}</td>
            </tr>`);

	return element;
}

$(document).ready(() => {
	$(document).on("click", "#users-tab #usersListTable tr td button[button-type=edit-user]", (event) => {
		event.preventDefault();
		event.stopPropagation();

		if (!IsEditUserAllowed) {
			AlertManager.createAlert({
				type: "warning",
				message: "Already editing a user! Refresh the page if issue persists.",
				timeout: 5000,
			});
			return;
		}

		IsEditUserAllowed = false;

		let userEmail = $(event.currentTarget).attr("user-email");
		usersManageTab.attr("user-email", userEmail);

		let currentUserData = CurrentUsersList.find((userData) => userData.email == userEmail);
		if (!currentUserData) {
			alert("Could not find user data for: " + user);

			IsEditUserAllowed = true;
			return;
		}

		CurrentManageUserEmail = userEmail;

		FetchUserBusinessesFromAPI(
			userEmail,
			currentUserData.businesses,
			(userBusinessesData) => {
				CurrentManageUserBusinesses = userBusinessesData;

				FetchUserNumbersFromAPI(
					userEmail,
					currentUserData.numbers,
					(userNumbersData) => {
						CurrentManageUserNumbers = userNumbersData;

						FillUserManageTab(currentUserData, CurrentManageUserBusinesses, CurrentManageUserNumbers);
						currentManageUserName.text(userEmail);

						$("#users-manage-general-tab").click();
						ShowUserManageTab();
					},
					(userNumbersError) => {
						AlertManager.createAlert({
							type: "danger",
							message: "Error occured while fetching user numbers. Check browser console for logs.",
							timeout: 5000,
						});

						console.log("Error occured while fetching user numbers: ", userNumbersError);
					},
				);
			},
			(userBusinessesError) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while fetching user businesses. Check browser console for logs.",
					timeout: 5000,
				});

				console.log("Error occured while fetching user businesses: ", userBusinessesError);
			},
		);
	});

	addNewUserButton.on("click", (event) => {
		event.preventDefault();

		ResetAndEmptyUsersManageTabData();
		currentManageUserName.text("New User");
		CurrentManageUserEmail = "new";

		userBusinessesListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="5">No businesses</td></tr>');
		userNumbersListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="6">No numbers</td></tr>');

		ShowUserManageTab();
	});

	switchBackToUsersListTabFromManageTab.on("click", (event) => {
		event.preventDefault();

		ShowUserListTab();

		IsEditUserAllowed = true;
	});

	$("#users-tab #usersManageTab input[check-type=permission-with-reason]").on("change", (event) => {
		event.stopPropagation();

		let current = $(event.currentTarget);

		let reasonInput = current.parent().parent().find("input[type=text]");

		if (current.prop("checked")) {
			reasonInput.removeClass("d-none");
		} else {
			reasonInput.addClass("d-none");
		}
	});

	DisableFullPermissionHelper(usersManageDisableCompleteBusinessInput, [usersManageDisableEditBusinessInput, usersManageDisableDeleteBusinessInput, usersManageDisableAddBusinessInput], {
		title: "Disable Business",
		message: "This will completely disable adding, editing, viewing and deleting the user businesses. Are you sure?",
		confirmText: "Disable",
		cancelText: "Cancel",
		confirmButtonClass: "btn-danger",
		modalClass: "",
	});

	// Add User Business Modal
	addUserBusinessModal.on("show.bs.modal", (event) => {
		addUserBusinessModalListTable.find("tbody").append('<tr><td colspan="3">Search for results...</td></tr>');
	});

	addUserBusinessModal.on("hidden.bs.modal", (event) => {
		addUserBusinessModalListTable.find("tbody").empty();

		addUserBusinessModalSearchInput.val("").removeClass("is-invalid");
		searchAddUserBusinessModalButton.prop("disabled", true);

		addUserBusinessModalSaveButton.prop("disabled", true);
	});

	addUserBusinessModalSearchInput.on("input", (event) => {
		let currentElement = $(event.currentTarget);

		let value = currentElement.val().trim();
		if (!value || value == "") {
			searchAddUserBusinessModalButton.prop("disabled", true);
			currentElement.addClass("is-invalid");
		} else {
			searchAddUserBusinessModalButton.prop("disabled", false);
			currentElement.removeClass("is-invalid");
		}
	});

	addUserBusinessModalSearchInput.on("keypress", (event) => {
		if (event.which == 13) {
			event.preventDefault();

			if (!searchAddUserBusinessModalButton.prop("disabled")) {
				searchAddUserBusinessModalButton.click();
			}
		}
	});

	searchAddUserBusinessModalButton.on("click", (event) => {
		event.preventDefault();

		let value = addUserBusinessModalSearchInput.val().trim();

		FetchSearchedBusinessesFromAPI(
			value,
			UsersTabAddBusinessModalListPage,
			UsersTabAddBusinessModalListPageSize,
			(businesses) => {
				CurrentManageAddUserBusinessesSearched = businesses;

				addUserBusinessModalListTable.find("tbody").empty();

				if (CurrentManageAddUserBusinessesSearched.length == 0) {
					addUserBusinessModalListTable.find("tbody").append('<tr><td colspan="3">No results found...</td></tr>');
				} else {
					CurrentManageAddUserBusinessesSearched.forEach((businessData) => {
						let addedIndex = CurrentManageUserAddBusiness.findIndex((business) => business.id == businessData.id);
						let alreadyIndex = CurrentManageUserBusinesses.findIndex((business) => business.id == businessData.id);

						let alreadyInUser = false;
						if (addedIndex !== -1 || alreadyIndex !== -1) {
							alreadyInUser = true;
						}

						addUserBusinessModalListTable.find("tbody").append(CreateAddUserBusinessListTableElement(businessData, alreadyInUser));
					});
				}
			},
			(businessesError) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while fetching user businesses. Check browser console for logs.",
					timeout: 5000,
				});

				console.log("Error occured while fetching user businesses: ", businessesError);
			},
		);
	});

	$(document).on("click", "#users-tab #addUserBusinessModalListTable tr.selectable[tr-type=add-user-business-modal]", (event) => {
		event.preventDefault();

		let businessId = $(event.currentTarget).attr("business-id");

		let hasDisabledClass = $(event.currentTarget).hasClass("disabled");
		if (hasDisabledClass) {
			return;
		}

		let findActiveSelectable = addUserBusinessModalListTable.find("tr.selectable[tr-type=add-user-business-modal].active");
		findActiveSelectable.removeClass("active");

		$(event.currentTarget).addClass("active");

		addUserBusinessModalSaveButton.prop("disabled", false);
	});

	addUserBusinessModalSaveButton.on("click", (event) => {
		event.preventDefault();

		let findActiveSelectable = addUserBusinessModalListTable.find("tr.selectable[tr-type=add-user-business-modal].active");
		let businessId = findActiveSelectable.attr("business-id");
		let businessData = CurrentManageAddUserBusinessesSearched.find((businessData) => businessData.id == businessId);

		function proceedWithBusinessTransfer() {
			userBusinessesListTable.find("tbody").find("tr[tr-type=none-notice]").remove();
			userBusinessesListTable.find("tbody").append(CreateUserManageBusinessesTableElement(businessData, true));

			addUserBusinessModal.modal("hide");

			CurrentManageUserAddBusiness.push({
				id: businessData.id,
			});
		}

		if (businessData.masterUserEmail && businessData.masterUserEmail !== CurrentManageUserEmail) {
			const transferConfirmDialog = new BootstrapConfirmDialog({
				title: "Confirm Business Transfer",
				message: `This business is managed by: ${businessData.masterUserEmail}\nThey will lose complete access to this business.\nDo you want to continue?`,
				confirmText: "Continue",
				cancelText: "Cancel",
				confirmButtonClass: "btn-warning",
				modalClass: "business-transfer-modal",
				zIndex: 2000,
			});

			transferConfirmDialog.show().then((isTransferConfirmed) => {
				if (!isTransferConfirmed) {
					return;
				}

				if (businessData.numberIds.length > 0) {
					const numberTransferDialog = new BootstrapConfirmDialog({
						title: "Confirm Number Transfer",
						message: `This business has ${businessData.numberIds.length} numbers.\nIf confirmed, all numbers will be disassociated from this business and it's routings numbers will be set as blank.\nDo you want to continue?`,
						confirmText: "Transfer Numbers",
						cancelText: "Disassociate Numbers",
						confirmButtonClass: "btn-primary",
						cancelButtonClass: "btn-secondary",
						modalClass: "number-transfer-modal",
					});

					numberTransferDialog.show().then((shouldTransferNumbers) => {
						businessData.numberIds = [];
						proceedWithBusinessTransfer();
					});
				} else {
					proceedWithBusinessTransfer();
				}
			});
		} else {
			proceedWithBusinessTransfer();
		}
	});

	$(document).on("click", "#users-tab #userBusinessesListTable tr td button[button-type=remove-added-business]", (event) => {
		event.preventDefault();

		let current = $(event.currentTarget);

		let businessId = current.attr("business-id");

		let index = CurrentManageUserAddBusiness.findIndex((businessData) => businessData.id == businessId);
		CurrentManageUserAddBusiness.splice(index, 1);

		current.parent().parent().remove();

		if (userBusinessesListTable.find("tbody").children().length == 0) {
			userBusinessesListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="5">No businesses</td></tr>');
		}
	});

	// Init

	FetchUsersFromAPI(
		UsersTabListTabPage,
		UsersTabListTabPageSize,
		(users) => {
			CurrentUsersList = users;

			users.forEach((userData) => {
				usersListTable.append(CreateUsersListTableElement(userData));
			});

			IsEditUserAllowed = true;
		},
		(usersError) => {
			AlertManager.createAlert({
				type: "danger",
				message: "Error occured while fetching users. Check browser console for logs.",
				timeout: 5000,
			});

			console.log("Error occured while fetching users: ", usersError);
		},
	);
});
