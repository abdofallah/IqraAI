const NumberProviderEnum = {
	Physical: 1,
	Twilio: 2,
	Vonage: 3,
};

var PhysicalNumbersTabListTabPage = 0;
var PhysicalNumbersTabListTabPageSize = 30;
var CurrentPhysicalNumbersList = null;

var TwilioNumbersTabListTabPage = 0;
var TwilioNumbersTabListTabPageSize = 30;
var CurrentTwilioNumbersList = null;

var VonageNumbersTabListTabPage = 0;
var VonageNumbersTabListTabPageSize = 30;
var CurrentVonageNumbersList = null;

/** Elements **/
const numberTab = $("#phone-numbers-tab");

const phoneNumbersPhysicalManageTab = numberTab.find("#phone-numbers-physical-manage-tab");

const physicalNumbersListTable = numberTab.find("#physicalNumbersListTable");
const twilioNumbersListTable = numberTab.find("#twilioNumbersListTable");
const vonageNumbersListTable = numberTab.find("#vonageNumbersListTable");

const phoneNumbersInnerTab = numberTab.find("#phone-numbers-inner-tab");
const phoneNumbersManageBreadcrumb = numberTab.find("#phone-numbers-manage-breadcrumb");

const switchBackToPhysicalNumbersListTabFromManageTab = numberTab.find("#switchBackToPhysicalNumbersListTabFromManageTab");
const switchBackToTwilioNumbersListTabFromManageTab = numberTab.find("#switchBackToTwilioNumbersListTabFromManageTab");
const switchBackToVonageNumbersListTabFromManageTab = numberTab.find("#switchBackToVonageNumbersListTabFromManageTab");

const phoneNumbersTabContent = numberTab.find("#phone-numbers-tab-content");
const phoneNumbersManageTab = numberTab.find("#phoneNumbersManageTab");

const phoneNumbersManageGeneralTab = phoneNumbersManageTab.find("#phoneNumbersManageGeneralTab");

const phoneNumbersManagePermissionDisableFullInput = phoneNumbersManageTab.find("#phoneNumbersManagePermissionDisableFullInput");
const phoneNumbersManagePermissionDisableFullReasonInput = phoneNumbersManageTab.find("#phoneNumbersManagePermissionDisableFullReasonInput");

const phoneNumbersManagePermissionDisableEditingInput = phoneNumbersManageTab.find("#phoneNumbersManagePermissionDisableEditingInput");
const phoneNumbersManagePermissionDisableEditingReasonInput = phoneNumbersManageTab.find("#phoneNumbersManagePermissionDisableEditingReasonInput");

const phoneNumbersManagePermissionDisableDeletingInput = phoneNumbersManageTab.find("#phoneNumbersManagePermissionDisableDeletingInput");
const phoneNumbersManagePermissionDisableDeletingReasonInput = phoneNumbersManageTab.find("#phoneNumbersManagePermissionDisableDeletingReasonInput");

const phoneNumbersManagePermissionDisableInboundCallingInput = phoneNumbersManageTab.find("#phoneNumbersManagePermissionDisableInboundCallingInput");
const phoneNumbersManagePermissionDisableInboundCallingReasonInput = phoneNumbersManageTab.find("#phoneNumbersManagePermissionDisableInboundCallingReasonInput");

const phoneNumbersManagePermissionDisableOutboundCallingInput = phoneNumbersManageTab.find("#phoneNumbersManagePermissionDisableOutboundCallingInput");
const phoneNumbersManagePermissionDisableOutboundCallingReasonInput = phoneNumbersManageTab.find("#phoneNumbersManagePermissionDisableOutboundCallingReasonInput");

const phoneNumbersManageIdInput = phoneNumbersManageTab.find("#phoneNumbersManageIdInput");
const phoneNumbersManageMasterUserEmailInput = phoneNumbersManageTab.find("#phoneNumbersManageMasterUserEmailInput");
const phoneNumbersManageCountryInput = phoneNumbersManageTab.find("#phoneNumbersManageCountryInput");
const phoneNumbersManageNumberInput = phoneNumbersManageTab.find("#phoneNumbersManageNumberInput");
const phoneNumbersManageAssignedToBusinessInput = phoneNumbersManageTab.find("#phoneNumbersManageAssignedToBusinessInput");
const phoneNumbersManageHostTypeInput = phoneNumbersManageTab.find("#phoneNumbersManageHostTypeInput");

const phoneNumbersManageHostIqraContainer = phoneNumbersManageTab.find("#phone-numbers-manage-host-iqra-container");
const phoneNumbersManageRegionInput = phoneNumbersManageHostIqraContainer.find("#phoneNumbersManageRegionInput");
const phoneNumbersManageRegionServerInput = phoneNumbersManageHostIqraContainer.find("#phoneNumbersManageRegionServerInput");

/** Functions **/

function CreatePhysicalNumberListTableElement(numberData) {
	let countryData = CountriesList[numberData.countryCode.toUpperCase()];

	let element = $(`<tr>
                <td>${numberData.id}</td>
                <td>${countryData["Alpha-2 code"]}</td>
                <td>${numberData.number}</td>
                <td>${numberData.masterUserEmail}</td>
                <td>${numberData.assignedToBusinessId}</td>
                <td>${numberData.hostType.name}</td>
                <td>
                    <button class="btn btn-info btn-sm" number-id="${numberData.id}" button-type="edit-physical-number">
                        <i class="fa-regular fa-eye"></i>
                    </button>
                    <button class="btn btn-danger btn-sm" number-id="${numberData.id}" button-type="delete-physical-number">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}

function CreateTwilioNumberListTableElement(numberData) {
	let countryData = CountriesList[numberData.countryCode.toUpperCase()];

	let element = $(`<tr>
                <td>${numberData.id}</td>
                <td>${countryData["Alpha-2 code"]}</td>
                <td>${numberData.number}</td>
                <td>${numberData.masterUserEmail}</td>
                <td>${numberData.assignedToBusinessId}</td>
                <td>
                    <button class="btn btn-info btn-sm" number-email="${numberData.id}" button-type="edit-twilio-number">
                        <i class="fa-regular fa-eye"></i>
                    </button>
                    <button class="btn btn-danger btn-sm">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}

function CreateVonageNumberListTableElement(numberData) {
	let countryData = CountriesList[numberData.countryCode.toUpperCase()];

	let element = $(`<tr>
                <td>${numberData.id}</td>
                <td>${countryData["Alpha-2 code"]}</td>
                <td>${numberData.number}</td>
                <td>${numberData.assignedToBusinessId}</td>
                <td>
                    <button class="btn btn-info btn-sm" number-email="${numberData.id}" button-type="edit-vonage-number">
                        <i class="fa-regular fa-eye"></i>
                    </button>
                    <button class="btn btn-danger btn-sm">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}

function ResetAndEmptyNumberManageTab() {
	phoneNumbersManageTab.find("input[type=text], input[type=email], input[type=number], textarea").val("");
	phoneNumbersManageTab.find("input[type=checkbox]").prop("checked", false).change();
	phoneNumbersManageTab.find("table tbody").empty();

	switchBackToPhysicalNumbersListTabFromManageTab.addClass("d-none");
	switchBackToTwilioNumbersListTabFromManageTab.addClass("d-none");
	switchBackToVonageNumbersListTabFromManageTab.addClass("d-none");

	phoneNumbersManageHostIqraContainer.addClass("d-none");
}

function ShowNumberManageTab(provider) {
	phoneNumbersInnerTab.removeClass("show");
	phoneNumbersTabContent.removeClass("show");

	setTimeout(() => {
		phoneNumbersInnerTab.addClass("d-none");
		phoneNumbersTabContent.addClass("d-none");

		phoneNumbersManageGeneralTab.click();

		switch (provider) {
			case 1:
				switchBackToPhysicalNumbersListTabFromManageTab.removeClass("d-none");
				break;

			case 2:
				switchBackToTwilioNumbersListTabFromManageTab.removeClass("d-none");
				break;

			case 3:
				switchBackToVonageNumbersListTabFromManageTab.removeClass("d-none");
				break;

			default:
				break;
		}

		phoneNumbersManageBreadcrumb.removeClass("d-none");
		phoneNumbersManageTab.removeClass("d-none");

		setTimeout(() => {
			phoneNumbersManageBreadcrumb.addClass("show");
			phoneNumbersManageTab.addClass("show");
		}, 10);
	}, 300);
}

function ShowNumberListTab(provider) {
	phoneNumbersManageBreadcrumb.removeClass("show");
	phoneNumbersManageTab.removeClass("show");

	setTimeout(() => {
		phoneNumbersManageBreadcrumb.addClass("d-none");
		phoneNumbersManageTab.addClass("d-none");

		phoneNumbersInnerTab.removeClass("d-none");
		phoneNumbersTabContent.removeClass("d-none");

		setTimeout(() => {
			phoneNumbersInnerTab.addClass("show");
			phoneNumbersTabContent.addClass("show");
		}, 10);
	}, 300);
}

function FillNumberManageTab(provider, numberData, regionData = null) {
	let countryData = CountriesList[numberData.countryCode.toUpperCase()];

	phoneNumbersManageIdInput.val(numberData.id);
	phoneNumbersManageMasterUserEmailInput.val(numberData.masterUserEmail);
	phoneNumbersManageCountryInput.val(countryData["Alpha-2 code"]);
	phoneNumbersManageNumberInput.val(numberData.number);
	phoneNumbersManageAssignedToBusinessInput.val(numberData.assignedToBusinessId);
	phoneNumbersManageHostTypeInput.val(numberData.hostType.name);

	switch (provider) {
		case 1:
			phoneNumbersManageHostIqraContainer.removeClass("d-none");

			if (regionData) {
				phoneNumbersManageRegionInput.val(regionData.name);

				let regionServerData = regionData.servers.find((serverData) => {
					return serverData.id == numberData.regionServerId;
				});

				if (regionServerData) {
					phoneNumbersManageRegionServerInput.val(regionServerData.id);
				} else {
					AlertManager.createAlert({
						type: "warning",
						message: "Region [" + numberData.regionId + "] Server [" + numberData.regionServerId + "] not found for number with id: " + numberData.id,
						timeout: 5000,
					});
				}
			} else {
				AlertManager.createAlert({
					type: "warning",
					message: "Region " + numberData.regionId + " not found for number with id: " + numberData.id,
					timeout: 5000,
				});
			}

			break;

		case 2:
			break;

		case 3:
			break;

		default:
			break;
	}

	SetPermissionInput(
		phoneNumbersManagePermissionDisableFullInput,
		phoneNumbersManagePermissionDisableFullReasonInput,
		numberData.permissions.disabledFullAt,
		numberData.permissions.disabledFullReason,
	);
	SetPermissionInput(
		phoneNumbersManagePermissionDisableEditingInput,
		phoneNumbersManagePermissionDisableEditingReasonInput,
		numberData.permissions.disabledEditingAt,
		numberData.permissions.disabledEditingReason,
	);
	SetPermissionInput(
		phoneNumbersManagePermissionDisableDeletingInput,
		phoneNumbersManagePermissionDisableDeletingReasonInput,
		numberData.permissions.disabledDeletingAt,
		numberData.permissions.disabledDeletingReason,
	);
	SetPermissionInput(
		phoneNumbersManagePermissionDisableInboundCallingInput,
		phoneNumbersManagePermissionDisableInboundCallingReasonInput,
		numberData.permissions.disabledInboundCallingAt,
		numberData.permissions.disabledInboundCallingReason,
	);
	SetPermissionInput(
		phoneNumbersManagePermissionDisableOutboundCallingInput,
		phoneNumbersManagePermissionDisableOutboundCallingReasonInput,
		numberData.permissions.disabledOutboundCallingAt,
		numberData.permissions.disabledOutboundCallingReason,
	);
}

$(document).ready(() => {
	$("#phone-numbers-tab #phoneNumbersManageTab input[check-type=permission-with-reason]").on("change", (event) => {
		event.stopPropagation();

		let current = $(event.currentTarget);

		let reasonInput = current.parent().parent().find("input[type=text]");

		if (current.prop("checked")) {
			reasonInput.removeClass("d-none");
		} else {
			reasonInput.addClass("d-none");
		}
	});

	DisableFullPermissionHelper(
		phoneNumbersManagePermissionDisableFullInput,
		[
			phoneNumbersManagePermissionDisableEditingInput,
			phoneNumbersManagePermissionDisableDeletingInput,
			phoneNumbersManagePermissionDisableInboundCallingInput,
			phoneNumbersManagePermissionDisableOutboundCallingInput,
		],
		{
			title: "Disable Number",
			message: "This will completely disable editing, deleting, inbound calling, and outbound calling of the number. Are you sure?",
			confirmText: "Disable",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "",
		},
	);

	$(document).on("click", "#phone-numbers-tab #physicalNumbersListTable tr td button[button-type=edit-physical-number]", (event) => {
		event.preventDefault();

		let current = $(event.currentTarget);

		let numberId = current.attr("number-id");

		let numberData = CurrentPhysicalNumbersList.find((number) => number.id == numberId);
		let regionData = CurrentRegionsList.find((region) => region.id == numberData.regionId);

		ResetAndEmptyNumberManageTab();
		FillNumberManageTab(NumberProviderEnum["Physical"], numberData, regionData);

		ShowNumberManageTab(NumberProviderEnum["Physical"]);
	});

	switchBackToPhysicalNumbersListTabFromManageTab.on("click", (event) => {
		event.preventDefault();

		ShowNumberListTab(NumberProviderEnum["Physical"]);
	});

	switchBackToTwilioNumbersListTabFromManageTab.on("click", (event) => {
		event.preventDefault();

		ShowNumberListTab(NumberProviderEnum["Twilio"]);
	});

	switchBackToVonageNumbersListTabFromManageTab.on("click", (event) => {
		event.preventDefault();

		ShowNumberListTab(NumberProviderEnum["Vonage"]);
	});

	// Init

	// Physical
	FetchNumbersByProviderFromAPI(
		NumberProviderEnum["Physical"],
		PhysicalNumbersTabListTabPage,
		PhysicalNumbersTabListTabPageSize,
		(physicalNumbersList) => {
			CurrentPhysicalNumbersList = physicalNumbersList;

			if (CurrentPhysicalNumbersList.length == 0) {
				physicalNumbersListTable.append('<tr><td colspan="7">No numbers found</td></tr>');
			} else {
				CurrentPhysicalNumbersList.forEach((numberData) => {
					physicalNumbersListTable.append(CreatePhysicalNumberListTableElement(numberData));
				});
			}
		},
		(physicalNumbersError) => {
			AlertManager.createAlert({
				type: "danger",
				message: "Error occured while fetching user numbers. Check browser console for logs.",
				timeout: 5000,
			});

			console.log("Error occured while fetching user numbers: ", physicalNumbersError);
		},
	);

	// Twilio
	FetchNumbersByProviderFromAPI(
		NumberProviderEnum["Twilio"],
		TwilioNumbersTabListTabPage,
		TwilioNumbersTabListTabPageSize,
		(twilioNumbersList) => {
			CurrentTwilioNumbersList = twilioNumbersList;

			if (CurrentTwilioNumbersList.length === 0) {
				twilioNumbersListTable.append('<tr><td colspan="6">No numbers found</td></tr>');
			} else {
				CurrentTwilioNumbersList.forEach((numberData) => {
					twilioNumbersListTable.append(CreateTwilioNumberListTableElement(numberData));
				});
			}
		},
		(twilioNumbersError) => {
			AlertManager.createAlert({
				type: "danger",
				message: "Error occured while fetching user numbers. Check browser console for logs.",
				timeout: 5000,
			});

			console.log("Error occured while fetching user numbers: ", twilioNumbersError);
		},
	);

	// Vonage
	FetchNumbersByProviderFromAPI(
		NumberProviderEnum["Vonage"],
		VonageNumbersTabListTabPage,
		VonageNumbersTabListTabPageSize,
		(vonageNumbersList) => {
			CurrentVonageNumbersList = vonageNumbersList;

			if (CurrentVonageNumbersList.length === 0) {
				vonageNumbersListTable.append('<tr><td colspan="6">No numbers found</td></tr>');
			} else {
				CurrentVonageNumbersList.forEach((numberData) => {
					vonageNumbersListTable.append(CreateVonageNumberListTableElement(numberData));
				});
			}
		},
		(vonageNumbersError) => {
			AlertManager.createAlert({
				type: "danger",
				message: "Error occured while fetching user numbers. Check browser console for logs.",
				timeout: 5000,
			});

			console.log("Error occured while fetching user numbers: ", vonageNumbersError);
		},
	);
});
