var BusinessesTabListTabPage = 0;
var BusinessesTabListTabPageSize = 30;

var CurrentBusinessesList = null;

var CurrentManageBusinessId = null;

const businessesTab = $("#business-tab");

const businessesListTableTab = businessesTab.find("#businessesListTableTab");
const businessesManageTab = businessesTab.find("#businessesManageTab");

const addNewBusinessButton = businessesTab.find("#addNewBusinessButton");

const businessesManageGeneralTab = businessesManageTab.find("#businesses-manage-general-tab");

const businessInnerTab = businessesTab.find("#business-inner-tab");
const businessesManageBreadcrumb = businessesTab.find("#businesses-manage-breadcrumb");
const switchBackToBusinessesListTabFromManageTab = businessesTab.find("#switchBackToBusinessesListTabFromManageTab");
const currentManageBusinessName = businessesTab.find("#currentManageBusinessName");

const businessesListTable = businessesListTableTab.find("table");

// General Information
const manageBusinessNameInput = businessesManageTab.find("#manageBusinessNameInput");
const manageBusinessMasterUserInput = businessesManageTab.find("#manageBusinessMasterUserInput");
const businessManageNumberListTable = businessesManageTab.find("#businessManageNumberListTable");

// General Permissions
const businessesManageDisabledFullInput = businessesManageTab.find("#businessesManageDisabledFullInput");
const businessesManageDisabledFullReasonInput = businessesManageTab.find("#businessesManageDisabledFullReasonInput");
const businessesManageDisabledEditInput = businessesManageTab.find("#businessesManageDisabledEditInput");
const businessesManageDisabledEditReasonInput = businessesManageTab.find("#businessesManageDisabledEditReasonInput");
const businessesManageDisabledDeleteInput = businessesManageTab.find("#businessesManageDisabledDeleteInput");
const businessesManageDisabledDeleteReasonInput = businessesManageTab.find("#businessesManageDisabledDeleteReasonInput");

// Routing Permissions
const businessesManageDisableRoutingInput = businessesManageTab.find("#businessesManageDisableRoutingInput");
const businessesManageDisableRoutingReasonInput = businessesManageTab.find("#businessesManageDisableRoutingReasonInput");
const businessesManageDisableAddingInput = businessesManageTab.find("#businessesManageDisableAddingInput");
const businessesManageDisableAddingReasonInput = businessesManageTab.find("#businessesManageDisableAddingReasonInput");
const businessesManageDisableEditingInput = businessesManageTab.find("#businessesManageDisableEditingInput");
const businessesManageDisableEditingReasonInput = businessesManageTab.find("#businessesManageDisableEditingReasonInput");
const businessesManageDisableDeletingInput = businessesManageTab.find("#businessesManageDisableDeletingInput");
const businessesManageDisableDeletingReasonInput = businessesManageTab.find("#businessesManageDisableDeletingReasonInput");

// Agents Permissions
const businessesManageDisableFullAgentsInput = businessesManageTab.find("#businessesManageDisableFullAgentsInput");
const businessesManageDisableFullAgentsReasonInput = businessesManageTab.find("#businessesManageDisableFullAgentsReasonInput");
const businessesManageDisableAddingAgentsInput = businessesManageTab.find("#businessesManageDisableAddingAgentsInput");
const businessesManageDisableAddingAgentsReasonInput = businessesManageTab.find("#businessesManageDisableAddingAgentsReasonInput");
const businessesManageDisableEditingAgentsInput = businessesManageTab.find("#businessesManageDisableEditingAgentsInput");
const businessesManageDisableEditingAgentsReasonInput = businessesManageTab.find("#businessesManageDisableEditingAgentsReasonInput");
const businessesManageDisableDeletingAgentsInput = businessesManageTab.find("#businessesManageDisableDeletingAgentsInput");
const businessesManageDisableDeletingAgentsReasonInput = businessesManageTab.find("#businessesManageDisableDeletingAgentsReasonInput");

// Tools Permissions
const businessesManageDisableFullToolsInput = businessesManageTab.find("#businessesManageDisableFullToolsInput");
const businessesManageDisableFullToolsReasonInput = businessesManageTab.find("#businessesManageDisableFullToolsReasonInput");
const businessesManageDisableAddingToolsInput = businessesManageTab.find("#businessesManageDisableAddingToolsInput");
const businessesManageDisableAddingToolsReasonInput = businessesManageTab.find("#businessesManageDisableAddingToolsReasonInput");
const businessesManageDisableEditingToolsInput = businessesManageTab.find("#businessesManageDisableEditingToolsInput");
const businessesManageDisableEditingToolsReasonInput = businessesManageTab.find("#businessesManageDisableEditingToolsReasonInput");
const businessesManageDisableDeletingToolsInput = businessesManageTab.find("#businessesManageDisableDeletingToolsInput");
const businessesManageDisableDeletingToolsReasonInput = businessesManageTab.find("#businessesManageDisableDeletingToolsReasonInput");

// Context Permissions
const businessPermissionsDisableFullInput = businessesManageTab.find("#businessPermissionsDisableFullInput");
const businessPermissionsDisableFullReasonInput = businessesManageTab.find("#businessPermissionsDisableFullReasonInput");
const businessPermissionsContextBrandingDisableEditingInput = businessesManageTab.find("#businessPermissionsContextBrandingDisableEditingInput");
const businessPermissionsContextBrandingDisableEditingReasonInput = businessesManageTab.find("#businessPermissionsContextBrandingDisableEditingReasonInput");

// Context Branches Permissions
const businessPermissionContextBranchesDisableFullInput = businessesManageTab.find("#businessPermissionContextBranchesDisableFullInput");
const businessPermissionContextBranchesDisableFullReasonInput = businessesManageTab.find("#businessPermissionContextBranchesDisableFullReasonInput");
const businessPermissionContextBranchesDisableAddingInput = businessesManageTab.find("#businessPermissionContextBranchesDisableAddingInput");
const businessPermissionContextBranchesDisableAddingReasonInput = businessesManageTab.find("#businessPermissionContextBranchesDisableAddingReasonInput");
const businessPermissionContextBranchesDisableEditingInput = businessesManageTab.find("#businessPermissionContextBranchesDisableEditingInput");
const businessPermissionContextBranchesDisableEditingReasonInput = businessesManageTab.find("#businessPermissionContextBranchesDisableEditingReasonInput");
const businessPermissionContextBranchesDisableDeletingInput = businessesManageTab.find("#businessPermissionContextBranchesDisableDeletingInput");
const businessPermissionContextBranchesDisableDeletingReasonInput = businessesManageTab.find("#businessPermissionContextBranchesDisableDeletingReasonInput");

// Context Services Permissions
const businessPermissionContextServicesDisableFull = businessesManageTab.find("#businessPermissionContextServicesDisableFull");
const businessPermissionContextServicesDisableFullReasonInput = businessesManageTab.find("#businessPermissionContextServicesDisableFullReasonInput");
const businessPermissionContextServicesDisableAdd = businessesManageTab.find("#businessPermissionContextServicesDisableAdd");
const businessPermissionContextServicesDisableAddReasonInput = businessesManageTab.find("#businessPermissionContextServicesDisableAddReasonInput");
const businessPermissionContextServicesDisableEdit = businessesManageTab.find("#businessPermissionContextServicesDisableEdit");
const businessPermissionContextServicesDisableEditReasonInput = businessesManageTab.find("#businessPermissionContextServicesDisableEditReasonInput");
const businessPermissionContextServicesDisableDelete = businessesManageTab.find("#businessPermissionContextServicesDisableDelete");
const businessPermissionContextServicesDisableDeleteReasonInput = businessesManageTab.find("#businessPermissionContextServicesDisableDeleteReasonInput");

// Context Products Permissions
const businessPermissionContextProductsDisabledFullInput = businessesManageTab.find("#businessPermissionContextProductsDisabledFullInput");
const businessPermissionContextProductsDisabledFullReasonInput = businessesManageTab.find("#businessPermissionContextProductsDisabledFullReasonInput");
const businessPermissionContextProductsDisabledAddInput = businessesManageTab.find("#businessPermissionContextProductsDisabledAddInput");
const businessPermissionContextProductsDisabledAddReasonInput = businessesManageTab.find("#businessPermissionContextProductsDisabledAddReasonInput");
const businessPermissionContextProductsDisabledEditInput = businessesManageTab.find("#businessPermissionContextProductsDisabledEditInput");
const businessPermissionContextProductsDisabledEditReasonInput = businessesManageTab.find("#businessPermissionContextProductsDisabledEditReasonInput");
const businessPermissionContextProductsDisabledDeleteInput = businessesManageTab.find("#businessPermissionContextProductsDisabledDeleteInput");
const businessPermissionContextProductsDisabledDeleteReasonInput = businessesManageTab.find("#businessPermissionContextProductsDisabledDeleteReasonInput");

// Make Call Permissions
const businessPermissionsMakeCallDisableCallingInput = businessesManageTab.find("#businessPermissionsMakeCallDisableCallingInput");
const businessPermissionsMakeCallDisableCallingReasonInput = businessesManageTab.find("#businessPermissionsMakeCallDisableCallingReasonInput");

// Conversations Permissions
const businessPermissionsConversationInboundDisableFullInput = businessesManageTab.find("#businessPermissionsConversationInboundDisableFullInput");
const businessPermissionsConversationInboundDisableFullReasonInput = businessesManageTab.find("#businessPermissionsConversationInboundDisableFullReasonInput");
const businessPermissionsConversationInboundDisableExporting = businessesManageTab.find("#businessPermissionsConversationInboundDisableExporting");
const businessPermissionsConversationInboundDisableExportingReasonInput = businessesManageTab.find("#businessPermissionsConversationInboundDisableExportingReasonInput");
const businessPermissionsConversationInboundDisableDeleting = businessesManageTab.find("#businessPermissionsConversationInboundDisableDeleting");
const businessPermissionsConversationInboundDisableDeletingReasonInput = businessesManageTab.find("#businessPermissionsConversationInboundDisableDeletingReasonInput");

const businessPermissionsConversationOutboundDisableFullInput = businessesManageTab.find("#businessPermissionsConversationOutboundDisableFullInput");
const businessPermissionsConversationOutboundDisableFullReasonInput = businessesManageTab.find("#businessPermissionsConversationOutboundDisableFullReasonInput");
const businessPermissionsConversationOutboundDisableExporting = businessesManageTab.find("#businessPermissionsConversationOutboundDisableExporting");
const businessPermissionsConversationOutboundDisableExportingReasonInput = businessesManageTab.find("#businessPermissionsConversationOutboundDisableExportingReasonInput");
const businessPermissionsConversationOutboundDisableDeleting = businessesManageTab.find("#businessPermissionsConversationOutboundDisableDeleting");
const businessPermissionsConversationOutboundDisableDeletingReasonInput = businessesManageTab.find("#businessPermissionsConversationOutboundDisableDeletingReasonInput");

const businessPermissionsConversationWebsocketDisableFullInput = businessesManageTab.find("#businessPermissionsConversationWebsocketDisableFullInput");
const businessPermissionsConversationWebsocketDisableFullReasonInput = businessesManageTab.find("#businessPermissionsConversationWebsocketDisableFullReasonInput");
const businessPermissionsConversationWebsocketDisableExporting = businessesManageTab.find("#businessPermissionsConversationWebsocketDisableExporting");
const businessPermissionsConversationWebsocketDisableExportingReasonInput = businessesManageTab.find("#businessPermissionsConversationWebsocketDisableExportingReasonInput");
const businessPermissionsConversationWebsocketDisableDeleting = businessesManageTab.find("#businessPermissionsConversationWebsocketDisableDeleting");
const businessPermissionsConversationWebsocketDisableDeletingReasonInput = businessesManageTab.find("#businessPermissionsConversationWebsocketDisableDeletingReasonInput");

function ResetAndEmptyBusinessManageTabData() {
	businessesManageTab.find("input[type=text], input[type=email], input[type=number], textarea").val("");
	businessesManageTab.find("input[type=checkbox]").prop("checked", false).change();
	businessesManageTab.find("table tbody").empty();
}

function ShowBusinessManageTab() {
	businessInnerTab.removeClass("show");
	businessesListTableTab.removeClass("show");

	setTimeout(() => {
		businessInnerTab.addClass("d-none");
		businessesListTableTab.addClass("d-none");

		businessesManageBreadcrumb.removeClass("d-none");
		businessesManageTab.removeClass("d-none");

		businessesManageGeneralTab.click();

		setTimeout(() => {
			businessesManageBreadcrumb.addClass("show");
			businessesManageTab.addClass("show");
		}, 10);
	}, 300);
}

function ShowBusinessListTab() {
	businessesManageBreadcrumb.removeClass("show");
	businessesManageTab.removeClass("show");

	setTimeout(() => {
		businessesManageBreadcrumb.addClass("d-none");
		businessesManageTab.addClass("d-none");

		businessInnerTab.removeClass("d-none");
		businessesListTableTab.removeClass("d-none");

		setTimeout(() => {
			businessInnerTab.addClass("show");
			businessesListTableTab.addClass("show");
		}, 10);
	}, 300);
}

function CreateBusinessesListTableElement(businessData) {
	let element = $(
		`<tr tr-type="business">
                    <td>${businessData.id}</td>
                    <td>${businessData.name}</td>
                    <td>${businessData.masterUserEmail}</td>
                    <td>${businessData.subUsers.length}</td>
                    <td>
                        <button class="btn btn-info btn-sm" business-id="${businessData.id}" button-type="edit-list-business">
                            <i class="fa-regular fa-eye"></i>
                        </button>
                        <button class="btn btn-danger btn-sm" business-id="${businessData.id}" button-type="delete-list-business">
                            <i class="fa-regular fa-trash"></i>
                        </button>
                    </td>
                </tr>`,
	);

	return element;
}

function FillBusinessManageTab(businessData) {
	// General Information
	manageBusinessNameInput.val(businessData.name);
	manageBusinessMasterUserInput.val(businessData.masterUserEmail);

	// Numbers List
	businessManageNumberListTable.find("tbody").empty();
	// TODO NUMBERS
	businessManageNumberListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="5">No numbers</td></tr>');

	// Permissions
	const permissions = businessData.permission;

	// General Permissions
	SetPermissionInput(businessesManageDisabledFullInput, businessesManageDisabledFullReasonInput, permissions.disabledFullAt, permissions.disabledFullReason);
	SetPermissionInput(businessesManageDisabledEditInput, businessesManageDisabledEditReasonInput, permissions.disabledEditingAt, permissions.disabledEditingReason);
	SetPermissionInput(businessesManageDisabledDeleteInput, businessesManageDisabledDeleteReasonInput, permissions.disabledDeletingAt, permissions.disabledDeletingReason);

	// Routing Permissions
	SetPermissionInput(businessesManageDisableRoutingInput, businessesManageDisableRoutingReasonInput, permissions.routing.disabledFullAt, permissions.routing.disabledFullReason);
	SetPermissionInput(businessesManageDisableAddingInput, businessesManageDisableAddingReasonInput, permissions.routing.disabledAddingAt, permissions.routing.disabledAddingReason);
	SetPermissionInput(businessesManageDisableEditingInput, businessesManageDisableEditingReasonInput, permissions.routing.disabledEditingAt, permissions.routing.disabledEditingReason);
	SetPermissionInput(businessesManageDisableDeletingInput, businessesManageDisableDeletingReasonInput, permissions.routing.disabledDeletingAt, permissions.routing.disabledDeletingReason);

	// Agents Permissions
	SetPermissionInput(businessesManageDisableFullAgentsInput, businessesManageDisableFullAgentsReasonInput, permissions.agents.disabledFullAt, permissions.agents.disabledFullReason);
	SetPermissionInput(businessesManageDisableAddingAgentsInput, businessesManageDisableAddingAgentsReasonInput, permissions.agents.disabledAddingAt, permissions.agents.disabledAddingReason);
	SetPermissionInput(businessesManageDisableEditingAgentsInput, businessesManageDisableEditingAgentsReasonInput, permissions.agents.disabledEditingAt, permissions.agents.disabledEditingReason);
	SetPermissionInput(businessesManageDisableDeletingAgentsInput, businessesManageDisableDeletingAgentsReasonInput, permissions.agents.disabledDeletingAt, permissions.agents.disabledDeletingReason);

	// Tools Permissions
	SetPermissionInput(businessesManageDisableFullToolsInput, businessesManageDisableFullToolsReasonInput, permissions.tools.disabledFullAt, permissions.tools.disabledFullReason);
	SetPermissionInput(businessesManageDisableAddingToolsInput, businessesManageDisableAddingToolsReasonInput, permissions.tools.disabledAddingAt, permissions.tools.disabledAddingReason);
	SetPermissionInput(businessesManageDisableEditingToolsInput, businessesManageDisableEditingToolsReasonInput, permissions.tools.disabledEditingAt, permissions.tools.disabledEditingReason);
	SetPermissionInput(businessesManageDisableDeletingToolsInput, businessesManageDisableDeletingToolsReasonInput, permissions.tools.disabledDeletingAt, permissions.tools.disabledDeletingReason);

	// Context Permissions
	SetPermissionInput(businessPermissionsDisableFullInput, businessPermissionsDisableFullReasonInput, permissions.context.disabledFullAt, permissions.context.disabledFullReason);
	SetPermissionInput(
		businessPermissionsContextBrandingDisableEditingInput,
		businessPermissionsContextBrandingDisableEditingReasonInput,
		permissions.context.branding.disabledEditingAt,
		permissions.context.branding.disabledEditingReason,
	);

	// Context Branches Permissions
	SetPermissionInput(
		businessPermissionContextBranchesDisableFullInput,
		businessPermissionContextBranchesDisableFullReasonInput,
		permissions.context.branches.disabledFullAt,
		permissions.context.branches.disabledFullReason,
	);
	SetPermissionInput(
		businessPermissionContextBranchesDisableAddingInput,
		businessPermissionContextBranchesDisableAddingReasonInput,
		permissions.context.branches.disabledAddingAt,
		permissions.context.branches.disabledAddingReason,
	);
	SetPermissionInput(
		businessPermissionContextBranchesDisableEditingInput,
		businessPermissionContextBranchesDisableEditingReasonInput,
		permissions.context.branches.disabledEditingAt,
		permissions.context.branches.disabledEditingReason,
	);
	SetPermissionInput(
		businessPermissionContextBranchesDisableDeletingInput,
		businessPermissionContextBranchesDisableDeletingReasonInput,
		permissions.context.branches.disabledDeletingAt,
		permissions.context.branches.disabledDeletingReason,
	);

	// Context Services Permissions
	SetPermissionInput(
		businessPermissionContextServicesDisableFull,
		businessPermissionContextServicesDisableFullReasonInput,
		permissions.context.services.disabledFullAt,
		permissions.context.services.disabledFullReason,
	);
	SetPermissionInput(
		businessPermissionContextServicesDisableAdd,
		businessPermissionContextServicesDisableAddReasonInput,
		permissions.context.services.disabledAddingAt,
		permissions.context.services.disabledAddingReason,
	);
	SetPermissionInput(
		businessPermissionContextServicesDisableEdit,
		businessPermissionContextServicesDisableEditReasonInput,
		permissions.context.services.disabledEditingAt,
		permissions.context.services.disabledEditingReason,
	);
	SetPermissionInput(
		businessPermissionContextServicesDisableDelete,
		businessPermissionContextServicesDisableDeleteReasonInput,
		permissions.context.services.disabledDeletingAt,
		permissions.context.services.disabledDeletingReason,
	);

	// Context Products Permissions
	SetPermissionInput(
		businessPermissionContextProductsDisabledFullInput,
		businessPermissionContextProductsDisabledFullReasonInput,
		permissions.context.products.disabledFullAt,
		permissions.context.products.disabledFullReason,
	);
	SetPermissionInput(
		businessPermissionContextProductsDisabledAddInput,
		businessPermissionContextProductsDisabledAddReasonInput,
		permissions.context.products.disabledAddingAt,
		permissions.context.products.disabledAddingReason,
	);
	SetPermissionInput(
		businessPermissionContextProductsDisabledEditInput,
		businessPermissionContextProductsDisabledEditReasonInput,
		permissions.context.products.disabledEditingAt,
		permissions.context.products.disabledEditingReason,
	);
	SetPermissionInput(
		businessPermissionContextProductsDisabledDeleteInput,
		businessPermissionContextProductsDisabledDeleteReasonInput,
		permissions.context.products.disabledDeletingAt,
		permissions.context.products.disabledDeletingReason,
	);

	// Make Call Permissions
	SetPermissionInput(
		businessPermissionsMakeCallDisableCallingInput,
		businessPermissionsMakeCallDisableCallingReasonInput,
		permissions.makeCall.disabledCallingAt,
		permissions.makeCall.disabledCallingReason,
	);

	// Conversations Permissions
	SetPermissionInput(
		businessPermissionsConversationInboundDisableFullInput,
		businessPermissionsConversationInboundDisableFullReasonInput,
		permissions.conversations.inbound.disabledFullAt,
		permissions.conversations.inbound.disabledFullReason,
	);
	SetPermissionInput(
		businessPermissionsConversationInboundDisableExporting,
		businessPermissionsConversationInboundDisableExportingReasonInput,
		permissions.conversations.inbound.disabledExportingAt,
		permissions.conversations.inbound.disabledExportingReason,
	);
	SetPermissionInput(
		businessPermissionsConversationInboundDisableDeleting,
		businessPermissionsConversationInboundDisableDeletingReasonInput,
		permissions.conversations.inbound.disabledDeletingAt,
		permissions.conversations.inbound.disabledDeletingReason,
	);

	SetPermissionInput(
		businessPermissionsConversationOutboundDisableFullInput,
		businessPermissionsConversationOutboundDisableFullReasonInput,
		permissions.conversations.outbound.disabledFullAt,
		permissions.conversations.outbound.disabledFullReason,
	);
	SetPermissionInput(
		businessPermissionsConversationOutboundDisableExporting,
		businessPermissionsConversationOutboundDisableExportingReasonInput,
		permissions.conversations.outbound.disabledExportingAt,
		permissions.conversations.outbound.disabledExportingReason,
	);
	SetPermissionInput(
		businessPermissionsConversationOutboundDisableDeleting,
		businessPermissionsConversationOutboundDisableDeletingReasonInput,
		permissions.conversations.outbound.disabledDeletingAt,
		permissions.conversations.outbound.disabledDeletingReason,
	);

	SetPermissionInput(
		businessPermissionsConversationWebsocketDisableFullInput,
		businessPermissionsConversationWebsocketDisableFullReasonInput,
		permissions.conversations.websocket.disabledFullAt,
		permissions.conversations.websocket.disabledFullReason,
	);
	SetPermissionInput(
		businessPermissionsConversationWebsocketDisableExporting,
		businessPermissionsConversationWebsocketDisableExportingReasonInput,
		permissions.conversations.websocket.disabledExportingAt,
		permissions.conversations.websocket.disabledExportingReason,
	);
	SetPermissionInput(
		businessPermissionsConversationWebsocketDisableDeleting,
		businessPermissionsConversationWebsocketDisableDeletingReasonInput,
		permissions.conversations.websocket.disabledDeletingAt,
		permissions.conversations.websocket.disabledDeletingReason,
	);
}

function CreateBusinessManageNumberListTableElement(numberData) {
	let countryData = CountriesList[numberData.countryCode.toUpperCase()];

	let element = $(
		`<tr>
                    <td>${numberData.id}</td>
                    <td>${countryData["Alpha-2 code"]}</td>
                    <td>${numberData.number}</td>
                    <td>${numberData.provider.name}</td>
                    <td>
                        <button class="btn btn-danger btn-sm" number-id="${numberData.id}" button-type="delete-list-business-number">
                            <i class="fa-regular fa-trash"></i>
                        </button>
                    </td>
                </tr>`,
	);

	return element;
}

function AddBusinessPermissionsHelper() {
	// General Business Permissions
	DisableFullPermissionHelper(businessesManageDisabledFullInput, [businessesManageDisabledEditInput, businessesManageDisabledDeleteInput], {
		title: "Disable Business",
		message: "This will completely disable editing, viewing, and deleting the business. Are you sure?",
		confirmText: "Disable",
		cancelText: "Cancel",
		confirmButtonClass: "btn-danger",
		modalClass: "",
	});

	// Routing Permissions
	DisableFullPermissionHelper(businessesManageDisableRoutingInput, [businessesManageDisableAddingInput, businessesManageDisableEditingInput, businessesManageDisableDeletingInput], {
		title: "Disable Routing",
		message: "This will completely disable adding, editing, viewing, and deleting all routings of the business. Are you sure?",
		confirmText: "Disable",
		cancelText: "Cancel",
		confirmButtonClass: "btn-danger",
		modalClass: "",
	});

	// Agents Permissions
	DisableFullPermissionHelper(
		businessesManageDisableFullAgentsInput,
		[businessesManageDisableAddingAgentsInput, businessesManageDisableEditingAgentsInput, businessesManageDisableDeletingAgentsInput],
		{
			title: "Disable Agents",
			message: "This will completely disable adding, editing, viewing, and deleting all agents of the business. Are you sure?",
			confirmText: "Disable",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "",
		},
	);

	// Tools Permissions
	DisableFullPermissionHelper(businessesManageDisableFullToolsInput, [businessesManageDisableAddingToolsInput, businessesManageDisableEditingToolsInput, businessesManageDisableDeletingToolsInput], {
		title: "Disable Tools",
		message: "This will completely disable adding, editing, viewing, and deleting tools of the business. Are you sure?",
		confirmText: "Disable",
		cancelText: "Cancel",
		confirmButtonClass: "btn-danger",
		modalClass: "",
	});

	// Context Permissions
	DisableFullPermissionHelper(
		businessPermissionsDisableFullInput,
		[
			// Branding
			businessPermissionsContextBrandingDisableEditingInput,
			// Branches
			businessPermissionContextBranchesDisableFullInput,
			businessPermissionContextBranchesDisableAddingInput,
			businessPermissionContextBranchesDisableEditingInput,
			businessPermissionContextBranchesDisableDeletingInput,
			// Services
			businessPermissionContextServicesDisableFull,
			businessPermissionContextServicesDisableAdd,
			businessPermissionContextServicesDisableEdit,
			businessPermissionContextServicesDisableDelete,
			// Products
			businessPermissionContextProductsDisabledFullInput,
			businessPermissionContextProductsDisabledAddInput,
			businessPermissionContextProductsDisabledEditInput,
			businessPermissionContextProductsDisabledDeleteInput,
		],
		{
			title: "Disable All Context Permissions",
			message: "This will completely disable all context-related settings including branding, branches, services, and products of the business. Are you sure?",
			confirmText: "Disable All",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "",
		},
	);

	// Context Branches Permissions
	DisableFullPermissionHelper(
		businessPermissionContextBranchesDisableFullInput,
		[businessPermissionContextBranchesDisableAddingInput, businessPermissionContextBranchesDisableEditingInput, businessPermissionContextBranchesDisableDeletingInput],
		{
			title: "Disable Branches",
			message: "This will completely disable adding, editing, viewing, and deleting branches of the business. Are you sure?",
			confirmText: "Disable",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "",
		},
	);

	// Context Services Permissions
	DisableFullPermissionHelper(
		businessPermissionContextServicesDisableFull,
		[businessPermissionContextServicesDisableAdd, businessPermissionContextServicesDisableEdit, businessPermissionContextServicesDisableDelete],
		{
			title: "Disable Services",
			message: "This will completely disable adding, editing, viewing, and deleting services of the business. Are you sure?",
			confirmText: "Disable",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "",
		},
	);

	// Context Products Permissions
	DisableFullPermissionHelper(
		businessPermissionContextProductsDisabledFullInput,
		[businessPermissionContextProductsDisabledAddInput, businessPermissionContextProductsDisabledEditInput, businessPermissionContextProductsDisabledDeleteInput],
		{
			title: "Disable Products",
			message: "This will completely disable adding, editing, viewing, and deleting products of the business. Are you sure?",
			confirmText: "Disable",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "",
		},
	);

	// Conversations Permissions
	DisableFullPermissionHelper(businessPermissionsConversationInboundDisableFullInput, [businessPermissionsConversationInboundDisableExporting, businessPermissionsConversationInboundDisableDeleting], {
		title: "Disable Inbound Conversations",
		message: "This will completely disable exporting and deleting inbound conversations of the business. Are you sure?",
		confirmText: "Disable",
		cancelText: "Cancel",
		confirmButtonClass: "btn-danger",
		modalClass: "",
	});
	DisableFullPermissionHelper(
		businessPermissionsConversationOutboundDisableFullInput,
		[businessPermissionsConversationOutboundDisableExporting, businessPermissionsConversationOutboundDisableDeleting],
		{
			title: "Disable Outbound Conversations",
			message: "This will completely disable exporting and deleting outbound conversations of the business. Are you sure?",
			confirmText: "Disable",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "",
		},
	);
	DisableFullPermissionHelper(
		businessPermissionsConversationWebsocketDisableFullInput,
		[businessPermissionsConversationWebsocketDisableExporting, businessPermissionsConversationWebsocketDisableDeleting],
		{
			title: "Disable Websocket Conversations",
			message: "This will completely disable exporting and deleting websocket conversations of the business. Are you sure?",
			confirmText: "Disable",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "",
		},
	);
}

$(document).ready(() => {
	$("#business-tab #businesses-manage-permissions input[check-type=permission-with-reason]").on("change", (event) => {
		event.stopPropagation();

		let current = $(event.currentTarget);
		let reasonInput = current.parent().parent().find("input[type=text]");

		if (current.prop("checked")) {
			reasonInput.removeClass("d-none");
		} else {
			reasonInput.addClass("d-none");
		}
	});

	addNewBusinessButton.on("click", (event) => {
		event.preventDefault();

		ResetAndEmptyBusinessManageTabData();
		currentManageBusinessName.text("New Business");
		CurrentManageBusinessId = "new";

		businessManageNumberListTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="5">No numbers</td></tr>');

		ShowBusinessManageTab();
	});

	switchBackToBusinessesListTabFromManageTab.on("click", (event) => {
		event.preventDefault();

		ShowBusinessListTab();
	});

	$(document).on("click", "#business-tab #businessesListTable tr td button[button-type=edit-list-business]", (event) => {
		event.preventDefault();

		let elementBusinessId = $(event.currentTarget).attr("business-id");

		let currentBusinessData = CurrentBusinessesList.find((businessData) => {
			return businessData.id == elementBusinessId;
		});

		currentManageBusinessName.text(currentBusinessData.name);
		CurrentManageBusinessId = currentBusinessData.id;

		ResetAndEmptyBusinessManageTabData();

		FillBusinessManageTab(currentBusinessData);
		ShowBusinessManageTab();
	});

	AddBusinessPermissionsHelper();

	// Init
	FetchBusinessesFromAPI(
		BusinessesTabListTabPage,
		BusinessesTabListTabPageSize,
		(businesses) => {
			CurrentBusinessesList = businesses;

			businesses.forEach((businessData) => {
				businessesListTable.append(CreateBusinessesListTableElement(businessData));
			});
		},
		(businessesError) => {
			AlertManager.createAlert({
				type: "danger",
				message: "Error occured while fetching businesses. Check browser console for logs.",
				timeout: 5000,
			});

			console.log("Error occured while fetching businesses: ", businessesError);
		},
	);
});
