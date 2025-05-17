var CurrentManageUserBusinesses = [];

var CurrentManageAddUserBusinessesSearched = [];

var CurrentManageUserEmail = "";
var CurrentManageUserAddBusiness = [];
var CurrentManageUserDeletedBusiness = [];

var UsersTabListTabPage = 0;
var UsersTabListTabPageSize = 30;

var UsersTabAddBusinessModalListPage = 0;
var UsersTabAddBusinessModalListPageSize = 30;

var IsEditUserAllowed = false;

var CurrentUserBillingData = null;
var CurrentUserFinancialTransactions = [];

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

const usersManageBillingTabButton = usersManageTab.find("#users-manage-billing-tab-button");
const usersManageBillingContent = usersManageTab.find("#users-manage-billing-content"); 

const userManageCurrentPlanSelect = usersManageBillingContent.find("#userManageCurrentPlanSelect");
const userManageCreditBalanceInput = usersManageBillingContent.find("#userManageCreditBalanceInput");
const adjustUserCreditButton = usersManageBillingContent.find("#adjustUserCreditButton");
const userManagePlanBaseConcurrencyDisplay = usersManageBillingContent.find("#userManagePlanBaseConcurrencyDisplay");
const userManagePurchasedConcurrencyInput = usersManageBillingContent.find("#userManagePurchasedConcurrencyInput");
const userManageTotalConcurrencyDisplay = usersManageBillingContent.find("#userManageTotalConcurrencyDisplay");
const userManageLastConcurrencyFeeBilledAtInput = usersManageBillingContent.find("#userManageLastConcurrencyFeeBilledAtInput");
const userManageNextConcurrencyFeeBillingDateInput = usersManageBillingContent.find("#userManageNextConcurrencyFeeBillingDateInput");

const userManageAutoRefillStatusToggle = usersManageBillingContent.find("#userManageAutoRefillStatusToggle");
const userManageAutoRefillDetails = usersManageBillingContent.find("#userManageAutoRefillDetails");
const userManageAutoRefillThresholdInput = userManageAutoRefillDetails.find("#userManageAutoRefillThresholdInput");
const userManageAutoRefillAmountInput = userManageAutoRefillDetails.find("#userManageAutoRefillAmountInput");
const userManageAutoRefillPaymentMethodSelect = userManageAutoRefillDetails.find("#userManageAutoRefillPaymentMethodSelect");
const userManageAutoRefillLastAttemptDisplay = userManageAutoRefillDetails.find("#userManageAutoRefillLastAttemptDisplay");

const userBillingHistoryTable = usersManageBillingContent.find("#userBillingHistoryTable");

const adjustCreditModal = $("#adjustCreditModal");
const adjustCreditUserName = adjustCreditModal.find("#adjustCreditUserName");
const adjustCreditCurrentBalance = adjustCreditModal.find("#adjustCreditCurrentBalance");
const adjustCreditUserEmailInput = adjustCreditModal.find("#adjustCreditUserEmailInput");
const adjustmentTypeSelect = adjustCreditModal.find("#adjustmentTypeSelect");
const adjustmentAmountInput = adjustCreditModal.find("#adjustmentAmountInput");
const adjustmentReasonInput = adjustCreditModal.find("#adjustmentReasonInput");
const confirmCreditAdjustmentButton = adjustCreditModal.find("#confirmCreditAdjustmentButton");

// API Functions
function FetchUserBillingDataFromAPI(email, successCallback, errorCallback) {
	// This might be redundant if your main FetchUserFromAPI returns billing data.
	// If not, implement like other Fetch...FromAPI functions.
	console.log(`API: Fetching billing data for user ${email}`);
	// Mock: Simulate fetching if not part of main user object
	const user = CurrentUsersList.find(u => u.email === email);
	if (user && user.billing) { // Assuming billing is nested in your mock CurrentUsersList
		setTimeout(() => successCallback(user.billing), 100);
	} else {
		// Simulate a default empty billing object if not found or for new users
		setTimeout(() => successCallback({ creditBalance: 0, currentPlanId: null, purchasedAdditionalConcurrencySlots: 0, autoRefill: { status: 'Disabled' } }), 100);
	}
	/*
	$.ajax({
		url: '/app/admin/user/billing', // Example endpoint
		type: 'POST',
		dataType: "json",
		data: { email: email },
		success: (response) => {
			if (!response.success) { errorCallback(response); return; }
			successCallback(response.data);
		},
		error: (error) => { errorCallback(error); }
	});
	*/
}

function FetchUserTransactionsFromAPI(email, page, pageSize, successCallback, errorCallback) {
	console.log(`API: Fetching transactions for user ${email}`);
	// Mock
	const mockTransactions = [
		// { timestamp: new Date(Date.now() - 86400000).toISOString(), type: "CreditTopUp", description: "Initial Top-up", amount: 50.00, balanceAfter: 50.00 },
		// { timestamp: new Date().toISOString(), type: "DebitUsage", description: "Call Usage (100 mins)", amount: -4.00, balanceAfter: 46.00 }
	];
	setTimeout(() => successCallback(mockTransactions), 200);
	/*
	$.ajax({
		url: '/app/admin/user/transactions', // Example endpoint
		type: 'POST',
		dataType: "json",
		data: { email: email, page: page, pageSize: pageSize },
		success: (response) => {
			if (!response.success) { errorCallback(response); return; }
			successCallback(response.data); // Assuming response.data is an array of transactions
		},
		error: (error) => { errorCallback(error); }
	});
	*/
}

function AdjustUserCreditAPI(userEmail, adjustmentType, amount, reason, successCallback, errorCallback) {
	console.log(`API: Adjusting credit for ${userEmail}. Type: ${adjustmentType}, Amount: ${amount}, Reason: ${reason}`);
	// Mock
	setTimeout(() => {
		// Find user, update balance (mock only, real update on server)
		const user = CurrentUsersList.find(u => u.email === userEmail);
		if (user) {
			if (!user.billing) user.billing = { creditBalance: 0 }; // Ensure billing object exists
			let numericAmount = parseFloat(amount);
			if (adjustmentType === "ManualDebitAdjustment" || adjustmentType === "Refund" && numericAmount > 0) { // Refund can be positive for user
				numericAmount = -Math.abs(numericAmount); // Ensure debit is negative, refund to user is positive for their balance
			} else if (adjustmentType === "Refund") { // If refund implies money taken back from user (rare)
				// numericAmount = -Math.abs(numericAmount);
			}


			const newBalance = parseFloat(user.billing.creditBalance) + numericAmount;
			// user.billing.creditBalance = newBalance; // Client-side mock update

			// Add to mock transactions
			// CurrentUserFinancialTransactions.push({ timestamp: new Date().toISOString(), type: adjustmentType, description: reason, amount: numericAmount, balanceAfter: newBalance });

			successCallback({ success: true, newBalance: newBalance, message: "Credit adjusted successfully." });
		} else {
			errorCallback({ success: false, message: "User not found for credit adjustment." });
		}
	}, 300);
	/*
	$.ajax({
		url: '/app/admin/user/credit/adjust', // Example endpoint
		type: 'POST',
		dataType: "json",
		data: {
			email: userEmail,
			transactionType: adjustmentType, // Ensure this matches backend enum/string
			amount: amount, // Backend should handle if it's credit or debit based on type
			description: reason
		},
		success: (response) => {
			if (!response.success) { errorCallback(response); return; }
			successCallback(response); // Expect { success: true, newBalance: ..., message: ... }
		},
		error: (error) => { errorCallback(error); }
	});
	*/
}

// Functions
function CreateUsersListTableElement(userData) {
	let element = $(`<tr>
                <td>${userData.email}</td>
                <td>${userData.firstName} ${userData.lastName}</td>
                <td>${userData.businesses.length}</td>
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

function CreateUserBillingHistoryRowElement(transaction) {
	const amountClass = transaction.amount >= 0 ? "text-success" : "text-danger";
	const amountFormatted = `${transaction.amount >= 0 ? '+' : ''}${parseFloat(transaction.amount).toFixed(2)}`;
	return $(`
        <tr>
            <td>${new Date(transaction.timestamp).toLocaleString()}</td>
            <td>${transaction.type.replace(/([A-Z])/g, ' $1').trim()}</td> {/* Add spaces to enum */}
            <td>${transaction.description || '-'}</td>
            <td class="${amountClass}">${amountFormatted}</td>
            <td>${parseFloat(transaction.balanceAfter).toFixed(2)}</td>
        </tr>
    `);
}

function ResetAndEmptyUsersManageTabData() {
	usersManageTab.find("input[type=text], input[type=email], input[type=number], textarea").val("");
	usersManageTab.find("input[type=checkbox]").prop("checked", false).change();
	usersManageTab.find("table tbody").empty();
	usersManageTab.attr("user-email", "");

	userManageCurrentPlanSelect.val("");
	userManageCreditBalanceInput.val("0.00");
	userManagePlanBaseConcurrencyDisplay.val("N/A");
	userManagePurchasedConcurrencyInput.val("0");
	userManageTotalConcurrencyDisplay.val("N/A");
	userManageLastConcurrencyFeeBilledAtInput.val("N/A");
	userManageNextConcurrencyFeeBillingDateInput.val("N/A");

	userManageAutoRefillStatusToggle.prop("checked", false).change();
	userManageAutoRefillThresholdInput.val("");
	userManageAutoRefillAmountInput.val("");
	userManageAutoRefillPaymentMethodSelect.val("");
	userManageAutoRefillLastAttemptDisplay.val("N/A");
	userManageAutoRefillDetails.addClass("d-none");

	userBillingHistoryTable.find("tbody").empty().append('<tr><td colspan="5" class="text-center">No transactions found.</td></tr>');

	// Data
	CurrentManageUserBusinesses = [];

	CurrentManageAddUserBusinessesSearched = [];

	CurrentManageUserEmail = "";
	CurrentManageUserAddBusiness = [];
	CurrentManageUserDeletedBusiness = [];

	UsersTabAddBusinessModalListPage = 0;
	UsersTabAddBusinessModalListPageSize = 30;

	IsEditUserAllowed = false;

	CurrentUserBillingData = null;
	CurrentUserFinancialTransactions = [];
}

function UpdateUserConcurrencyDisplays() {
	const selectedPlanId = userManageCurrentPlanSelect.val();
	const purchasedConcurrency = parseInt(userManagePurchasedConcurrencyInput.val()) || 0;
	let planBaseConcurrency = 0;

	if (selectedPlanId && CurrentPlansList.length > 0) {
		const plan = CurrentPlansList.find(p => p.id === selectedPlanId);
		if (plan) {
			// Determine base concurrency from the correct part of the plan object
			if (plan.pricingModel === "StandardPayAsYouGo" && plan.standardPlan) {
				planBaseConcurrency = plan.standardPlan.baseConcurrency || 0;
			} else if (plan.pricingModel === "VolumeBasedTiered" && plan.volumeTieredPlan) {
				planBaseConcurrency = plan.volumeTieredPlan.baseConcurrency || 0;
			} else if (plan.pricingModel === "FixedPricePackage" && plan.fixedPackagePlan) {
				planBaseConcurrency = plan.fixedPackagePlan.includedConcurrency || 0;
			}
		}
	}
	userManagePlanBaseConcurrencyDisplay.val(planBaseConcurrency > 0 ? planBaseConcurrency : (selectedPlanId ? "0" : "N/A"));
	userManageTotalConcurrencyDisplay.val(planBaseConcurrency + purchasedConcurrency);
}

function FillUserManageTab(userData, userBusinessesData) {
	ResetAndEmptyUsersManageTabData();

	CurrentManageUserEmail = userData.email;
	usersManageTab.attr("user-email", userData.email);

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

	// Billing
	CurrentUserBillingData = userData.billing;
	PopulateUserBillingFields(CurrentUserBillingData);

	// Billing Transacitons
	FetchUserTransactionsFromAPI(userData.email, 0, 100, // page, pageSize (adjust as needed)
		(transactions) => {
			CurrentUserFinancialTransactions = transactions;
			userBillingHistoryTable.find("tbody").empty();
			if (transactions && transactions.length > 0) {
				transactions.forEach(tx => {
					userBillingHistoryTable.find("tbody").append(CreateUserBillingHistoryRowElement(tx));
				});
			} else {
				userBillingHistoryTable.find("tbody").append('<tr><td colspan="5" class="text-center">No transactions found.</td></tr>');
			}
		},
		(error) => {
			userBillingHistoryTable.find("tbody").empty().append('<tr><td colspan="5" class="text-center text-danger">Error loading transactions.</td></tr>');
			console.error("Error fetching user transactions:", error);
		}
	);
}

function PopulateUserBillingFields(billingData) {
	if (!billingData) return;

	userManageCurrentPlanSelect.val(billingData.currentPlanId || "");
	userManageCreditBalanceInput.val(parseFloat(billingData.creditBalance || 0).toFixed(2));
	userManagePurchasedConcurrencyInput.val(billingData.purchasedAdditionalConcurrencySlots || 0);

	userManageLastConcurrencyFeeBilledAtInput.val(billingData.lastConcurrencyFeeBilledAt ? new Date(billingData.lastConcurrencyFeeBilledAt).toLocaleString() : "N/A");
	userManageNextConcurrencyFeeBillingDateInput.val(billingData.nextConcurrencyFeeBillingDate ? new Date(billingData.nextConcurrencyFeeBillingDate).toLocaleDateString() : "N/A");

	UpdateUserConcurrencyDisplays(); // Call this after plan and purchased concurrency are set

	if (billingData.autoRefill) {
		const autoRefillEnabled = billingData.autoRefill.status === "Enabled"; // Assuming "Enabled" string from enum
		userManageAutoRefillStatusToggle.prop("checked", autoRefillEnabled).change(); // .change() triggers handler
		if (autoRefillEnabled) {
			userManageAutoRefillThresholdInput.val(parseFloat(billingData.autoRefill.refillWhenBalanceBelow || 0).toFixed(2));
			userManageAutoRefillAmountInput.val(parseFloat(billingData.autoRefill.refillAmount || 0).toFixed(2));
			// TODO: Populate userManageAutoRefillPaymentMethodSelect with user's payment methods and select billingData.autoRefill.defaultPaymentMethodId
			userManageAutoRefillLastAttemptDisplay.val(
				billingData.autoRefill.lastAttemptTimestamp
					? `${new Date(billingData.autoRefill.lastAttemptTimestamp).toLocaleString()} - ${billingData.autoRefill.lastAttemptStatusMessage || 'Unknown'}`
					: "N/A"
			);
		}
	}
}

function CollectUserBillingFormData() {
	if (!CurrentUserBillingData) CurrentUserBillingData = {}; // Ensure object exists

	const billingFormData = {
		// currentPlanId: userManageCurrentPlanSelect.val() || null, // Plan is set by admin directly
		// creditBalance: parseFloat(userManageCreditBalanceInput.val()), // Balance adjusted via modal
		purchasedAdditionalConcurrencySlots: parseInt(userManagePurchasedConcurrencyInput.val()) || 0,
		autoRefill: {
			status: userManageAutoRefillStatusToggle.is(":checked") ? "Enabled" : "Disabled",
			refillWhenBalanceBelow: userManageAutoRefillStatusToggle.is(":checked") ? parseFloat(userManageAutoRefillThresholdInput.val()) : null,
			refillAmount: userManageAutoRefillStatusToggle.is(":checked") ? parseFloat(userManageAutoRefillAmountInput.val()) : null,
			defaultPaymentMethodId: userManageAutoRefillStatusToggle.is(":checked") ? userManageAutoRefillPaymentMethodSelect.val() : null,
			// lastAttemptTimestamp and lastAttemptStatusMessage are read-only from server
		}
	};
	// Only send planId if it changed, or if it's a new user assignment.
	// For simplicity, we can send it always, backend can decide if it needs update.
	billingFormData.currentPlanId = userManageCurrentPlanSelect.val() || null;

	return billingFormData;
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

// Event Handlers
function initUsersBillingTabEventHandlers() {
	userManageCurrentPlanSelect.on("change", function () {
		UpdateUserConcurrencyDisplays();
	});

	userManagePurchasedConcurrencyInput.on("input", function () {
		UpdateUserConcurrencyDisplays();
	});

	userManageAutoRefillStatusToggle.on("change", function () {
		if ($(this).is(":checked")) {
			userManageAutoRefillDetails.removeClass("d-none");
			// TODO: Potentially fetch user's payment methods for userManageAutoRefillPaymentMethodSelect
		} else {
			userManageAutoRefillDetails.addClass("d-none");
		}
	});

	adjustUserCreditButton.on("click", function () {
		if (!CurrentManageUserEmail || CurrentManageUserEmail === "new") {
			AlertManager.createAlert({ type: "info", message: "Please save the user before adjusting credit.", timeout: 3000 });
			return;
		}
		adjustCreditUserName.text(usersManageFirstNameInput.val() + " " + usersManageLastNameInput.val() || CurrentManageUserEmail);
		adjustCreditCurrentBalance.text(userManageCreditBalanceInput.val());
		adjustCreditUserEmailInput.val(CurrentManageUserEmail);
		adjustmentTypeSelect.val("ManualCreditAdjustment"); // Default
		adjustmentAmountInput.val("");
		adjustmentReasonInput.val("");
		adjustCreditModal.modal("show");
	});

	confirmCreditAdjustmentButton.on("click", function () {
		const userEmail = adjustCreditUserEmailInput.val();
		const type = adjustmentTypeSelect.val();
		const amount = parseFloat(adjustmentAmountInput.val());
		const reason = adjustmentReasonInput.val().trim();

		if (isNaN(amount) || amount <= 0) {
			AlertManager.createAlert({ type: "warning", message: "Please enter a valid positive amount.", source: adjustCreditModal });
			adjustmentAmountInput.addClass("is-invalid");
			return;
		}
		adjustmentAmountInput.removeClass("is-invalid");
		if (!reason) {
			AlertManager.createAlert({ type: "warning", message: "Reason for adjustment is required.", source: adjustCreditModal });
			adjustmentReasonInput.addClass("is-invalid");
			return;
		}
		adjustmentReasonInput.removeClass("is-invalid");

		$(this).prop("disabled", true).find(".fa-regular").toggleClass("fa-check fa-spinner fa-spin");

		AdjustUserCreditAPI(userEmail, type, amount, reason,
			(response) => {
				AlertManager.createAlert({ type: "success", message: response.message || "Credit adjusted successfully!", timeout: 3000 });
				userManageCreditBalanceInput.val(parseFloat(response.newBalance).toFixed(2));
				// Refresh transaction history
				FetchUserTransactionsFromAPI(userEmail, 0, 100, (transactions) => {
					CurrentUserFinancialTransactions = transactions;
					userBillingHistoryTable.find("tbody").empty();
					if (transactions && transactions.length > 0) {
						transactions.forEach(tx => { userBillingHistoryTable.find("tbody").append(CreateUserBillingHistoryRowElement(tx)); });
					} else {
						userBillingHistoryTable.find("tbody").append('<tr><td colspan="5" class="text-center">No transactions found.</td></tr>');
					}
				}, (err) => console.error("Error refreshing transactions:", err));
				adjustCreditModal.modal("hide");
			},
			(error) => {
				AlertManager.createAlert({ type: "danger", message: error.message || "Failed to adjust credit.", source: adjustCreditModal });
			}
		).always(() => {
			$(this).prop("disabled", false).find(".fa-regular").toggleClass("fa-check fa-spinner fa-spin");
		});
	});
}


function initUsersTab() {
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

				FillUserManageTab(currentUserData, CurrentManageUserBusinesses);
				currentManageUserName.text(userEmail);

				$("#users-manage-general-tab").click();
				ShowUserManageTab();
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
	CurrentUsersList.forEach((userData) => {
		usersListTable.append(CreateUsersListTableElement(userData));
	});

	IsEditUserAllowed = true;

	initUsersBillingTabEventHandlers();
}
