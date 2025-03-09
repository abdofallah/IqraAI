/** Elements **/
const regionTab = $("#region-tab");

const regionListTable = regionTab.find("#regionListTable");

const regionListTableTab = regionTab.find("#regionListTableTab");
const regionManageTab = regionTab.find("#regionManageTab");

const regionInnerTab = regionTab.find("#region-inner-tab");
const regionManageBreadcrumb = regionTab.find("#region-manage-breadcrumb");

const currentManageRegionName = regionTab.find("#currentManageRegionName");
const switchBackToRegionListTabFromManageTab = regionTab.find("#switchBackToRegionListTabFromManageTab");

const manageRegionCountryInputSelect = regionTab.find("#manageRegionCountryInput");
const manageRegionCountryRegionInput = regionTab.find("#manageRegionCountryRegionInput");
const manageRegionDisabledInput = regionTab.find("#manageRegionDisabledInput");

const regionServerListTable = regionTab.find("#regionServerListTable");

const regionManagerServersListTab = regionTab.find("#regionManagerServersListTab");
const regionManagerServerManageTab = regionTab.find("#regionManagerServerManageTab");

const currentManageServerRegionName = regionTab.find("#currentManageServerRegionName");
const currentManageRegionServerName = regionTab.find("#currentManageRegionServerName");
const regionServerManagerBreadcrumb = regionTab.find("#region-server-manager-breadcrumb");
const regionManagerInnerTabContainer = regionTab.find("#region-manager-inner-tab-container");
const regionManagerGeneralTab = regionTab.find("#region-manager-general-tab");

const switchBackToRegionManagerServersListTabFromServersTab = regionTab.find("#switchBackToRegionManagerServersListTabFromServersTab");

const manageRegionServerIpInput = regionTab.find("#manageRegionServerIpInput");
const manageRegionServerTypeSelect = regionTab.find("#manageRegionServerTypeSelect");
const manageRegionServerDisabledInput = regionTab.find("#manageRegionServerDisabledInput");

const addNewRegionButton = regionTab.find("#addNewRegionButton");

const addNewRegionServerButton = regionTab.find("#addNewRegionServerButton");

/** Functions **/

function CreateRegionListTableElement(regionData) {
	let countryData = CountriesList[regionData.countryCode.toUpperCase()];

	let element = $(`<tr>
                <td><span class="badge bg-info">UNK</span></td>
                <td>${regionData.countryRegion}</td>
                <td>${countryData["Country"]}</td>
                <td>${regionData.servers.length}</td>
                <td>
                    <button class="btn btn-info btn-sm" region-id="${regionData.countryRegion}" button-type="edit-region">
                        <i class="fa-regular fa-eye"></i>
                    </button>
                    <button class="btn btn-danger btn-sm">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}

function CreateServerListTableElement(regionId, serverData) {
	let element = $(`<tr>
                <td><span class="badge bg-info">UNK</span></td>
                <td>${serverData.endpoint}</td>
                <td>${serverData.type.name}</td>
                <td>
                    <button class="btn btn-info btn-sm" region-id="${regionId}" server-id="${serverData.endpoint}" button-type="edit-region-server">
                        <i class="fa-regular fa-eye"></i>
                    </button>
                    <button class="btn btn-danger btn-sm">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>`);

	return element;
}

function ShowRegionManageTab() {
	regionManagerGeneralTab.click();

	regionInnerTab.removeClass("show");
	regionListTableTab.removeClass("show");

	setTimeout(() => {
		regionInnerTab.addClass("d-none");
		regionListTableTab.addClass("d-none");

		regionManageBreadcrumb.removeClass("d-none");
		regionManageTab.removeClass("d-none");

		setTimeout(() => {
			regionManageBreadcrumb.addClass("show");
			regionManageTab.addClass("show");
		}, 10);
	}, 300);
}

function HideRegionManageTab() {
	regionManageBreadcrumb.removeClass("show");
	regionManageTab.removeClass("show");

	setTimeout(() => {
		regionManageBreadcrumb.addClass("d-none");
		regionManageTab.addClass("d-none");

		regionInnerTab.removeClass("d-none");
		regionListTableTab.removeClass("d-none");

		setTimeout(() => {
			regionInnerTab.addClass("show");
			regionListTableTab.addClass("show");
		}, 10);
	}, 300);
}

function FillRegionManageTab(regionData) {
	manageRegionCountryInputSelect.val(regionData.countryCode.toUpperCase());
	manageRegionCountryInputSelect.prop("disabled", true);

	manageRegionCountryRegionInput.val(regionData.countryRegion);
	manageRegionCountryRegionInput.prop("disabled", true);

	if (regionData.disabled != null) {
		manageRegionDisabledInput.prop("checked", true);
	}

	regionData.servers.forEach((serverData) => {
		regionServerListTable.append(CreateServerListTableElement(regionData.countryRegion, serverData));
	});
}

function ResetAndEmptyRegionManageTab() {
	regionManageTab.find("select").prop("disabled", false).val("").change();
	regionManageTab.find("input[type=text]").prop("disabled", false).val("");
	regionManageTab.find("input[type=checkbox]").prop("disabled", false).prop("checked", false).change();
	regionManageTab.find("table tbody").empty();
}

function ShowRegionServerManageTab() {
	regionManageBreadcrumb.removeClass("show");
	regionManagerInnerTabContainer.removeClass("show");
	regionManagerServersListTab.removeClass("show");

	setTimeout(() => {
		regionManageBreadcrumb.addClass("d-none");
		regionManagerInnerTabContainer.addClass("d-none");
		regionManagerServersListTab.addClass("d-none");

		regionServerManagerBreadcrumb.removeClass("d-none");
		regionManagerServerManageTab.removeClass("d-none");

		setTimeout(() => {
			regionServerManagerBreadcrumb.addClass("show");
			regionManagerServerManageTab.addClass("show");
		}, 10);
	}, 300);
}

function HideRegionServerManageTab() {
	regionServerManagerBreadcrumb.removeClass("show");
	regionManagerServerManageTab.removeClass("show");

	setTimeout(() => {
		regionServerManagerBreadcrumb.addClass("d-none");
		regionManagerServerManageTab.addClass("d-none");

		regionManageBreadcrumb.removeClass("d-none");
		regionManagerInnerTabContainer.removeClass("d-none");
		regionManagerServersListTab.removeClass("d-none");

		setTimeout(() => {
			regionManageBreadcrumb.addClass("show");
			regionManagerInnerTabContainer.addClass("show");
			regionManagerServersListTab.addClass("show");
		}, 10);
	}, 300);
}

function FillRegionServerManageTab(serverData) {
	manageRegionServerIpInput.val(serverData.endpoint);
	manageRegionServerIpInput.prop("disabled", true);

	manageRegionServerTypeSelect.val(serverData.type.value);
    manageRegionServerTypeSelect.prop("disabled", true);

	if (serverData.disabledAt != null) {
		manageRegionServerDisabledInput.prop("checked", true);
	}
}

function ResetAndEmptyRegionServerManageTab() {
	regionManagerServerManageTab.find("input[type=text]").val("");
	regionManagerServerManageTab.find("input[type=checkbox]").prop("checked", false).change();
	regionManagerServerManageTab.find("table tbody").empty();
}

/** Initalizer **/

$(document).ready(() => {
	$(document).on("click", "#region-tab #regionListTable tr td button[button-type=edit-region]", (event) => {
		event.preventDefault();

		let elementRegionId = $(event.currentTarget).attr("region-id");
		currentManageRegionName.text(elementRegionId);
		currentManageServerRegionName.text(elementRegionId);

		let currentRegionData = CurrentRegionsList.find((regionData) => {
			return regionData.countryRegion == elementRegionId;
		});

		// View

		ResetAndEmptyRegionManageTab();
		ResetAndEmptyRegionServerManageTab();

		FillRegionManageTab(currentRegionData);

		ShowRegionManageTab();
	});

	switchBackToRegionListTabFromManageTab.on("click", (event) => {
		event.preventDefault();

		HideRegionManageTab();
	});

	$(document).on("click", "#region-tab #regionServerListTable tr td button[button-type=edit-region-server]", (event) => {
		event.preventDefault();

		let elementRegionId = $(event.currentTarget).attr("region-id");
		let elementServerId = $(event.currentTarget).attr("server-id");

		let currentRegionData = CurrentRegionsList.find((regionData) => {
			return regionData.countryRegion == elementRegionId;
		});
		let currentServerData = currentRegionData.servers.find((serverData) => {
			return serverData.endpoint == elementServerId;
		});

		currentManageRegionServerName.text(elementServerId);

		// View

		FillRegionServerManageTab(currentServerData);

		ShowRegionServerManageTab();
	});

	switchBackToRegionManagerServersListTabFromServersTab.on("click", (event) => {
		event.preventDefault();

		HideRegionServerManageTab();
	});

	addNewRegionButton.on("click", (event) => {
		event.preventDefault();

		currentManageRegionName.text("New Region");
		currentManageServerRegionName.text("New Region");

		// View

		ResetAndEmptyRegionManageTab();

		ShowRegionManageTab();
	});

	addNewRegionServerButton.on("click", (event) => {
		event.preventDefault();

		currentManageRegionServerName.text("New Server");

		// View

		ResetAndEmptyRegionServerManageTab();

		ShowRegionServerManageTab();
	});

	// Init
	Object.keys(CountriesList).forEach((countryCode) => {
		let countryData = CountriesList[countryCode];
		manageRegionCountryInputSelect.append(`<option value="${countryData["Alpha-2 code"]}">${countryData["Country"]}</option>`);
	});

	FetchRegionsFromAPI(
		(regionsData) => {
			CurrentRegionsList = regionsData;

			CurrentRegionsList.forEach((regionData) => {
				regionListTable.append(CreateRegionListTableElement(regionData));
			});
		},
		(regionsError) => {
			AlertManager.createAlert({
				type: "danger",
				message: "Error occured while fetching regions. Check browser console for logs.",
				timeout: 5000,
			});

			console.log("Error occured while fetching regions: ", regionsError);
		},
	);
});
