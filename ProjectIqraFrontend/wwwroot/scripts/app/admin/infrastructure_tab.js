/** Constants & Enums **/
const INFRA_SERVER_TYPE = {
    UNKNOWN: 0,
    PROXY: 1,
    BACKEND: 2,
    FRONTEND: 3,
    BACKGROUND: 4
};

const INFRA_SERVER_STATUS = {
    OFFLINE: 0,
    ONLINE: 1,
    DRAINING: 2,
    ZOMBIE: 3
};

const CHART_METRIC_TYPE = {
    CPU: 'cpu',
    RAM: 'ram',
    NETWORK: 'network',
    DYNAMIC: 'dynamic'
};

const CHART_POLL_INTERVAL_MS = 1000 * 60;
const GAP_THRESHOLD_MS = 2 * 60 * 1000;

/** Dynamic Variables **/
let CurrentInfraOverviewData = null;
let CurrentManageRegionData = null;
let CurrentManageServerData = null;

let ManageRegionType = null; // 'new' or 'edit'
let ManageServerType = null; // 'new' or 'edit'

let IsSavingRegion = false;
let IsSavingServer = false;

// Status Modal State
let PendingStatusToggle = null;
let PendingStatusAction = null;

// Polling & Timers
let InfraOverviewPollingInterval = null;
let InfraDetailPollingInterval = null;

// Chart State
const InfraChartManager = {
    frontend: { chart: null, nodeId: null, days: 1, metric: 'cpu', lastRealPoint: null, timer: null },
    background: { chart: null, nodeId: null, days: 1, metric: 'cpu', lastRealPoint: null, timer: null },
    server: { chart: null, nodeId: null, days: 1, metric: 'cpu', lastRealPoint: null, timer: null }
};

/** Element Variables **/

// Main Container
const infraTab = $("#infrastructure-tab");
const infraHeaderContainer = infraTab.find("#infra-header-container");

// ROOT - Header
const infraRootHeader = infraHeaderContainer.find("#infra-root-header");
const infraRootDashboardTabBtn = infraRootHeader.find("#infra-root-dashboard-tab");

// ROOT - Content
const infraRootTab = infraTab.find("#infraRootTab");
// Dashboard
const infraDashboardRegionGrid = infraRootTab.find("#infraDashboardRegionGrid");
// Dashboard Stats
const dashboardTotalTelephony = infraRootTab.find("#dashboardTotalTelephony");
const dashboardTotalWeb = infraRootTab.find("#dashboardTotalWeb");
const dashboardQueueProcessing = infraRootTab.find("#dashboardQueueProcessing");
const dashboardQueueMarked = infraRootTab.find("#dashboardQueueMarked");
const dashboardActiveBackendNodes = infraRootTab.find("#dashboardActiveBackendNodes");
const dashboardTotalBackendNodes = infraRootTab.find("#dashboardTotalBackendNodes");
const dashboardActiveProxyNodes = infraRootTab.find("#dashboardActiveProxyNodes");
const dashboardTotalProxyNodes = infraRootTab.find("#dashboardTotalProxyNodes");
const dashboardActiveNodesCount = infraRootTab.find("#dashboardActiveNodesCount");
const dashboardConfiguredNodesCount = infraRootTab.find("#dashboardConfiguredNodesCount");
// Core Services
const infraFrontendContainer = infraRootTab.find("#infraFrontendContainer");
const infraBackgroundContainer = infraRootTab.find("#infraBackgroundContainer");
const infraFrontendChartCanvas = infraRootTab.find("#infraFrontendChart");
const infraBackgroundChartCanvas = infraRootTab.find("#infraBackgroundChart");
// Regions List
const infraRegionsCardListContainer = infraRootTab.find("#infraRegionsCardListContainer");
const addNewRegionButton = infraRootTab.find("#addNewRegionButton");
const searchRegionInput = infraRootTab.find("#searchRegionInput");

// REGION MANAGER - Header
const infraRegionManagerHeader = infraHeaderContainer.find("#infra-region-manager-header");
const switchBackToInfraRoot = infraRegionManagerHeader.find("#switchBackToInfraRoot");
const currentRegionName = infraRegionManagerHeader.find("#currentRegionName");
const confirmSaveRegionButton = infraRegionManagerHeader.find("#confirmSaveRegionButton");
const confirmSaveRegionButtonSpinner = infraRegionManagerHeader.find(".save-button-spinner");
const regionManagerOverviewTabBtn = infraRegionManagerHeader.find("#region-manager-overview-tab");

// REGION MANAGER - Content
const infraRegionManagerTab = infraTab.find("#infraRegionManagerTab");
// Overview
const regionOverviewBackendOnline = infraRegionManagerTab.find("#regionOverviewBackendOnline");
const regionOverviewBackendOffline = infraRegionManagerTab.find("#regionOverviewBackendOffline");
const regionOverviewTelephony = infraRegionManagerTab.find("#regionOverviewTelephony");
const regionOverviewWeb = infraRegionManagerTab.find("#regionOverviewWeb");
const regionOverviewProxyOnline = infraRegionManagerTab.find("#regionOverviewProxyOnline");
const regionOverviewProxyOffline = infraRegionManagerTab.find("#regionOverviewProxyOffline");
const regionOverviewQueueProcessing = infraRegionManagerTab.find("#regionOverviewQueueProcessing");
const regionOverviewQueuePending = infraRegionManagerTab.find("#regionOverviewQueuePending");
// Servers List
const regionProxyListContainer = infraRegionManagerTab.find("#regionProxyListContainer");
const regionBackendListContainer = infraRegionManagerTab.find("#regionBackendListContainer");
const btnAddProxyServer = infraRegionManagerTab.find("#btnAddProxyServer");
const btnAddBackendServer = infraRegionManagerTab.find("#btnAddBackendServer");
// S3 Input Group
const editRegionS3Endpoint = infraRegionManagerTab.find("#editRegionS3Endpoint");
const editRegionS3AccessKey = infraRegionManagerTab.find("#editRegionS3AccessKey");
const editRegionS3SecretKey = infraRegionManagerTab.find("#editRegionS3SecretKey");
const editRegionS3UseSSL = infraRegionManagerTab.find("#editRegionS3UseSSL");
const regionS3Inputs = editRegionS3Endpoint.add(editRegionS3AccessKey).add(editRegionS3SecretKey).add(editRegionS3UseSSL);
// Settings Group
const editRegionMaintenanceMode = infraRegionManagerTab.find("#editRegionMaintenanceMode");
const editRegionDisabled = infraRegionManagerTab.find("#editRegionDisabled");
const regionMaintenanceReasonDisplay = infraRegionManagerTab.find("#regionMaintenanceReasonDisplay");
const regionDisabledReasonDisplay = infraRegionManagerTab.find("#regionDisabledReasonDisplay");

// SERVER MANAGER - Header
const infraServerManagerHeader = infraHeaderContainer.find("#infra-server-manager-header");
const switchBackToRegionManager = infraServerManagerHeader.find("#switchBackToRegionManager");
const currentServerName = infraServerManagerHeader.find("#currentServerName");
const confirmSaveServerButton = infraServerManagerHeader.find("#confirmSaveServerButton");
const confirmSaveServerButtonSpinner = infraServerManagerHeader.find(".save-button-spinner");
const serverManagerMetricsTabBtn = infraServerManagerHeader.find("#server-manager-metrics-tab");
const serverManagerSettingsTabBtn = infraServerManagerHeader.find("#server-manager-settings-tab");

// SERVER MANAGER - Content
const infraServerManagerTab = infraTab.find("#infraServerManagerTab");
// Inputs
const editServerEndpoint = infraServerManagerTab.find("#editServerEndpoint");
const editServerType = infraServerManagerTab.find("#editServerType");
const editServerApiKey = infraServerManagerTab.find("#editServerApiKey");
const editServerSipPort = infraServerManagerTab.find("#editServerSipPort");
const editServerIsDev = infraServerManagerTab.find("#editServerIsDev");
const serverConfigInputs = editServerEndpoint.add(editServerType).add(editServerApiKey).add(editServerSipPort).add(editServerIsDev);
// Settings Controls
const serverControlsCard = infraServerManagerTab.find("#serverControlsCard");
const editServerMaintenanceMode = infraServerManagerTab.find("#editServerMaintenanceMode");
const editServerDisabled = infraServerManagerTab.find("#editServerDisabled");
const serverMaintenanceReasonDisplay = infraServerManagerTab.find("#serverMaintenanceReasonDisplay");
const serverDisabledReasonDisplay = infraServerManagerTab.find("#serverDisabledReasonDisplay");
// Metrics
const serverMetricStatusBadge = infraServerManagerTab.find("#serverMetricStatusBadge");
const serverMetricCpu = infraServerManagerTab.find("#serverMetricCpu");
const serverMetricRam = infraServerManagerTab.find("#serverMetricRam");
const serverMetricDynamicCard = infraServerManagerTab.find("#serverMetricDynamicCard");
const serverDynamicMetricTab = infraServerManagerTab.find("#serverDynamicMetricTab");
const serverMetricsChartCanvas = infraServerManagerTab.find("#serverMetricsChart");

// Global Chart Controls (Event Delegation Targets)
const historyRangeSelectors = infraTab.find(".history-range-selector");
const historyMetricTabsButtons = infraTab.find(".history-metric-tabs button");

// MODAL: Status Change
const infraStatusReasonModalEl = document.getElementById('infraStatusReasonModal');
const infraStatusReasonModal = new bootstrap.Modal(infraStatusReasonModalEl);
const infraStatusModalTitle = $(infraStatusReasonModalEl).find("#infraStatusModalTitle");
const infraStatusModalMessage = $(infraStatusReasonModalEl).find("#infraStatusModalMessage");
const infraStatusPublicReason = $(infraStatusReasonModalEl).find("#infraStatusPublicReason");
const infraStatusPrivateReason = $(infraStatusReasonModalEl).find("#infraStatusPrivateReason");
const infraStatusConfirmButton = $(infraStatusReasonModalEl).find("#infraStatusConfirmButton");

// MODAL: Add Region
const infraAddRegionModalEl = document.getElementById('infraAddRegionModal');
const infraAddRegionModal = new bootstrap.Modal(infraAddRegionModalEl);
const infraNewRegionCountry = $(infraAddRegionModalEl).find("#infraNewRegionCountry");
const infraNewRegionId = $(infraAddRegionModalEl).find("#infraNewRegionId");
const infraNewRegionPreview = $(infraAddRegionModalEl).find("#infraNewRegionPreview");
const infraBtnConfirmAddRegion = $(infraAddRegionModalEl).find("#infraBtnConfirmAddRegion");



/** API FUNCTIONS **/

function GetInfraOverview(onSuccess, onError) {
    return $.get('/app/admin/infrastructure/overview', (res) => res.success ? onSuccess(res.data) : onError(res));
}

function GetRegionDetail(regionId, onSuccess, onError) {
    return $.get(`/app/admin/infrastructure/regions/${regionId}`, (res) => res.success ? onSuccess(res.data) : onError(res));
}

function FetchServerHistory(nodeId, startIso, endIso, onSuccess) {
    $.get(`/app/admin/infrastructure/servers/${nodeId}/history?start=${startIso}&end=${endIso}`,
        (res) => { if (res.success) onSuccess(res.data); }
    );
}

function AddRegion(data, onSuccess, onError) {
    return $.ajax({
        url: '/app/admin/infrastructure/regions',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: (res) => res.success ? onSuccess(res.data) : onError(res),
        error: (err) => onError(err)
    });
}

function SaveRegionConfig(regionId, changes, onSuccess, onError) {
    if (changes.s3) {
        return $.ajax({
            url: `/app/admin/infrastructure/regions/${regionId}/s3`,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(changes.s3),
            success: (res) => res.success ? onSuccess(res.data) : onError(res),
            error: (err) => onError(err)
        });
    }
}

function DeleteRegion(regionId, onSuccess, onError) {
    return $.ajax({
        url: `/app/admin/infrastructure/regions/${regionId}`,
        type: 'DELETE',
        success: (res) => res.success ? onSuccess(res.data) : onError(res),
        error: (err) => onError(err)
    });
}

function SaveServerConfig(regionId, serverId, data, onSuccess, onError) {
    const url = serverId
        ? `/app/admin/infrastructure/regions/${regionId}/servers/${serverId}`
        : `/app/admin/infrastructure/regions/${regionId}/servers`;

    return $.ajax({
        url: url,
        type: serverId ? 'PUT' : 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: (res) => res.success ? onSuccess(res.data) : onError(res),
        error: (err) => onError(err)
    });
}

function ToggleRegionStatus(regionId, type, enabled, reasons, onSuccess, onError) {
    const endpoint = type === 'maintenance' ? 'maintenance' : 'disabled';
    return $.post(`/app/admin/infrastructure/regions/${regionId}/${endpoint}`, {
        enabled: enabled,
        publicReason: reasons.public,
        privateReason: reasons.private
    }, (res) => res.success ? onSuccess(res.data) : onError(res));
}

function ToggleServerStatus(regionId, serverId, type, enabled, reasons, onSuccess, onError) {
    const endpoint = type === 'maintenance' ? 'maintenance' : 'disabled';
    return $.post(`/app/admin/infrastructure/regions/${regionId}/servers/${serverId}/${endpoint}`, {
        enabled: enabled,
        publicReason: reasons.public,
        privateReason: reasons.private
    }, (res) => res.success ? onSuccess(res.data) : onError(res));
}


/** FUNCTIONS **/

// --- Navigation ---
function showInfraRootTab() {
    infraRegionManagerTab.removeClass("show");
    infraRegionManagerHeader.removeClass("show");
    infraServerManagerTab.removeClass("show");
    infraServerManagerHeader.removeClass("show");

    setTimeout(() => {
        infraRegionManagerTab.addClass("d-none");
        infraRegionManagerHeader.addClass("d-none");
        infraServerManagerTab.addClass("d-none");
        infraServerManagerHeader.addClass("d-none");

        infraRootTab.removeClass("d-none");
        infraRootHeader.removeClass("d-none");
        setTimeout(() => {
            infraRootTab.addClass("show");
            infraRootHeader.addClass("show");

            setDynamicBodyHeight();
            StartInfraOverviewPolling();
            StopInfraDetailPolling();

            // Default select
            if (!infraRootDashboardTabBtn.hasClass("active")) {
                infraRootDashboardTabBtn.click();
            }
        }, 10)
    }, 300);
}

function showRegionManagerTab() {
    infraRootTab.removeClass("show");
    infraRootHeader.removeClass("show");
    infraServerManagerTab.removeClass("show");
    infraServerManagerHeader.removeClass("show");

    setTimeout(() => {
        infraRootTab.addClass("d-none");
        infraRootHeader.addClass("d-none");
        infraServerManagerTab.addClass("d-none");
        infraServerManagerHeader.addClass("d-none");

        infraRegionManagerTab.removeClass("d-none");
        infraRegionManagerHeader.removeClass("d-none");

        setTimeout(() => {
            infraRegionManagerTab.addClass("show");
            infraRegionManagerHeader.addClass("show");

            setDynamicBodyHeight();
            StopInfraOverviewPolling();
            if (CurrentManageRegionData) {
                StartInfraDetailPolling(CurrentManageRegionData.regionId);
            }

            // Default select
            regionManagerOverviewTabBtn.click();
        }, 10);
    }, 300);
}

function showServerManagerTab() {
    infraRegionManagerTab.removeClass("show");
    infraRegionManagerHeader.removeClass("show");

    setTimeout(() => {
        infraRegionManagerTab.addClass("d-none");
        infraRegionManagerHeader.addClass("d-none");

        infraServerManagerTab.removeClass("d-none");
        infraServerManagerHeader.removeClass("d-none");
        setTimeout(() => {
            infraServerManagerTab.addClass("show");
            infraServerManagerHeader.addClass("show");

            setDynamicBodyHeight();

            // Handle New vs Edit UI
            if (ManageServerType === 'new') {
                serverManagerMetricsTabBtn.prop("disabled", true).addClass("disabled");
                serverControlsCard.addClass("d-none"); // Hide controls for new server
                serverManagerSettingsTabBtn.click();
            } else {
                serverManagerMetricsTabBtn.prop("disabled", false).removeClass("disabled");
                serverControlsCard.removeClass("d-none");
                serverManagerSettingsTabBtn.click(); // Default to settings
            }
        }, 10);
    }, 300);
}


// --- Polling ---
function StartInfraOverviewPolling() {
    if (InfraOverviewPollingInterval) return;
    FetchAndFillOverview();
    InfraOverviewPollingInterval = setInterval(FetchAndFillOverview, 3000);
}
function StopInfraOverviewPolling() {
    if (InfraOverviewPollingInterval) { clearInterval(InfraOverviewPollingInterval); InfraOverviewPollingInterval = null; }
}
function StartInfraDetailPolling(regionId) {
    if (InfraDetailPollingInterval) clearInterval(InfraDetailPollingInterval);
    FetchAndFillRegionDetail(regionId);
    InfraDetailPollingInterval = setInterval(() => FetchAndFillRegionDetail(regionId, true), 2000);
}
function StopInfraDetailPolling() {
    if (InfraDetailPollingInterval) { clearInterval(InfraDetailPollingInterval); InfraDetailPollingInterval = null; }
}


// --- Root Tab Logic ---
function FetchAndFillOverview() {
    if (infraTab.hasClass('d-none')) return;
    GetInfraOverview((data) => {
        CurrentInfraOverviewData = data;
        FillInfraDashboard();
        FillSingletonNodes();
        FillInfraRegionsList();
    }, (err) => console.log("Poll Error", err));
}

function FillInfraDashboard() {
    const data = CurrentInfraOverviewData;
    if (!data) return;

    // Stats
    dashboardTotalTelephony.text(data.totalActiveTelephonySessions);
    dashboardTotalWeb.text(data.totalActiveWebSessions);
    dashboardQueueProcessing.text(data.totalOutboundProcessingQueues);
    dashboardQueueMarked.text(data.totalOutboundMarkedQueues);

    dashboardActiveBackendNodes.text(data.activeBackendNodes);
    dashboardTotalBackendNodes.text(data.totalBackendNodes || 0);

    dashboardActiveProxyNodes.text(data.activeProxyNodes);
    dashboardTotalProxyNodes.text(data.totalProxyNodes || 0);

    dashboardActiveNodesCount.text(data.activeNodesCount);
    dashboardConfiguredNodesCount.text(data.configuredNodesCount);

    // Region Grid
    infraDashboardRegionGrid.empty();
    if (data.regions.length === 0) {
        infraDashboardRegionGrid.html('<div class="col-12 text-muted text-center py-5">No regions configured.</div>');
    } else {
        data.regions.forEach(r => {
            const card = `
                <div class="col-md-3">
                    <div class="card bg-dark border-secondary h-100">
                        <div class="card-body">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <h6 class="card-title mb-0 text-white">${r.countryCode}</h6>
                                <span class="badge h-100 bg-secondary">${r.regionId}</span>
                            </div>
                            <div class="d-flex justify-content-between mb-1">
                                <span class="text-success small fw-bold">${r.onlineBackendNodes}/${r.totalBackendNodes} Backend</span>
                                <span class="text-info small fw-bold">${r.onlineProxyNodes}/${r.totalProxyNodes} Proxy</span>
                            </div>
                            <div class="border-top border-secondary pt-2 mt-2">
                                <div class="d-flex justify-content-between small text-muted">
                                    <span><i class="fa-regular fa-phone me-1"></i>${r.totalActiveTelephonySessions + r.totalActiveWebSessions}</span>
                                    <span><i class="fa-regular fa-hourglass me-1"></i>${r.totalOutboundProcessingQueues}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>`;
            infraDashboardRegionGrid.append(card);
        });
    }
}

function FillSingletonNodes() {
    const data = CurrentInfraOverviewData;

    // Helper to render live card
    const renderCard = (name, node, container) => {
        container.empty();
        const isOnline = node && node.isOnline;
        const html = `
        <div class="col-md-12">
            <div class="card bg-dark h-100">
                <div class="card-body">
                    <div class="d-flex justify-content-between mb-3">
                        <h6 class="fw-bold">${name} Live Status</h6>
                        <span class="badge h-100 ${isOnline ? 'bg-success' : 'bg-danger'}">${isOnline ? 'Online' : 'Offline'}</span>
                    </div>
                    <div class="row text-center">
                        <div class="col-4"><div class="small text-white-50">CPU</div><div class="fw-bold fs-5">${isOnline ? node.cpuUsage.toFixed(1) : 0}%</div></div>
                        <div class="col-4"><div class="small text-white-50">RAM</div><div class="fw-bold fs-5">${isOnline ? node.ramUsage.toFixed(1) : 0}%</div></div>
                        <div class="col-4"><div class="small text-white-50">Version</div><div class="fw-bold fs-6">${isOnline ? node.version : '-'}</div></div>
                    </div>
                </div>
            </div>
        </div>`;
        container.append(html);
    };

    renderCard("Frontend", data.frontendNode, infraFrontendContainer);
    renderCard("Background", data.backgroundNode, infraBackgroundContainer);

    // Initialize Charts if Node ID exists
    InitializeNodeChart('frontend', 'Frontend');
    InitializeNodeChart('background', 'Background');
}

function FillInfraRegionsList() {
    const container = infraRegionsCardListContainer;
    if (searchRegionInput.is(":focus")) return;

    container.empty();
    if (!CurrentInfraOverviewData || CurrentInfraOverviewData.regions.length === 0) {
        container.html('<div class="col-12 text-center text-muted mt-5">No regions found.</div>');
        return;
    }

    CurrentInfraOverviewData.regions.forEach(r => {
        const term = searchRegionInput.val().toLowerCase();
        if (term && !r.regionId.toLowerCase().includes(term)) return;

        const countryDisplay = (typeof CountriesList !== 'undefined' && CountriesList[r.countryCode]) ? CountriesList[r.countryCode].Country : r.countryCode;

        const html = `
        <div class="col-md-4">
            <div class="card bg-dark border-secondary h-100 region-card" data-item-id="${r.regionId}" style="cursor: pointer;">
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <h5 class="card-title mb-0">${countryDisplay}</h5>
                        <div class="dropdown action-dropdown dropdown-menu-end" onclick="event.stopPropagation()">
                             <button class="btn action-button dropdown-toggle" type="button" data-bs-toggle="dropdown" aria-expanded="false"><i class="fa-solid fa-ellipsis"></i></button>
                             <ul class="dropdown-menu">
                                <li><span class="dropdown-item text-danger" data-item-id="${r.regionId}" button-type="delete-region"><i class="fa-solid fa-trash me-2"></i>Delete</span></li>
                             </ul>
                        </div>
                    </div>
                    <div class="small text-muted font-monospace mb-3">${r.regionId}</div>
                    <div class="d-flex justify-content-between text-center border-top border-secondary pt-2">
                        <div><div class="h5 mb-0 text-white">${r.onlineBackendNodes}/${r.totalBackendNodes}</div><div class="small text-muted">Backend</div></div>
                        <div><div class="h5 mb-0 text-white">${r.onlineProxyNodes}/${r.totalProxyNodes}</div><div class="small text-muted">Proxy</div></div>
                        <div><div class="h5 mb-0 ${r.isMaintenanceMode ? 'text-danger' : 'text-success'}">${r.isMaintenanceMode ? 'Maint.' : 'Live'}</div><div class="small text-muted">Status</div></div>
                    </div>
                </div>
            </div>
        </div>`;
        container.append(html);
    });
}


// --- Region Manager Logic ---
function FetchAndFillRegionDetail(regionId, isRefresh = false) {
    GetRegionDetail(regionId, (data) => {
        CurrentManageRegionData = data;
        FillRegionManagerTab(isRefresh);

        // Update Server if open
        if (!infraServerManagerTab.hasClass('d-none') && CurrentManageServerData) {
            const updatedServer = data.servers.find(s => s.endpoint === CurrentManageServerData.endpoint);
            if (updatedServer) {
                CurrentManageServerData = updatedServer;
                FillServerManagerTab(isRefresh); // Refreshes metrics
            }
        }
    }, (err) => console.log("Region Detail Error", err));
}

function FillRegionManagerTab(isRefresh = false) {
    const data = CurrentManageRegionData;
    if (!data) return;

    currentRegionName.text(data.regionId);

    // Overview Stats
    regionOverviewBackendOnline.text(data.onlineBackendCount);
    regionOverviewBackendOffline.text(data.totalBackendNodes - data.onlineBackendCount);
    regionOverviewTelephony.text(data.totalActiveTelephonySessions);
    regionOverviewWeb.text(data.totalActiveWebSessions);

    regionOverviewProxyOnline.text(data.onlineProxyCount);
    regionOverviewProxyOffline.text(data.totalProxyNodes - data.onlineProxyCount);
    regionOverviewQueueProcessing.text(data.totalOutboundProcessingQueues);
    regionOverviewQueuePending.text(data.totalOutboundMarkedQueues);

    // Servers List
    regionProxyListContainer.empty();
    regionBackendListContainer.empty();

    data.servers.forEach(server => {
        const isOnline = server.metrics && server.metrics.runtimeStatus.value === INFRA_SERVER_STATUS.ONLINE;

        let cardContent = '';
        if (isOnline) {
            cardContent = `
                <div class="d-flex justify-content-between align-items-center mt-3">
                    <div class="small text-muted">CPU: <span class="text-white">${server.metrics.cpuUsagePercent.toFixed(1)}%</span></div>
                    <div class="small text-muted">RAM: <span class="text-white">${server.metrics.memoryUsagePercent.toFixed(1)}%</span></div>
                </div>`;
        } else {
            cardContent = `<div class="mt-3 text-center small text-danger">Server Offline</div>`;
        }

        const html = `
        <div class="col-md-4">
            <div class="card bg-black border-secondary h-100 server-card" data-endpoint="${server.endpoint}" style="cursor: pointer;">
                <div class="card-body p-3">
                    <div class="d-flex justify-content-between align-items-start mb-2">
                        <span class="font-monospace small text-truncate" title="${server.endpoint}">${server.endpoint}</span>
                        <span class="badge h-100 ${isOnline ? 'bg-success' : 'bg-danger'}">${isOnline ? 'Online' : 'Offline'}</span>
                    </div>
                    ${cardContent}
                </div>
            </div>
        </div>`;

        if (server.type.value === INFRA_SERVER_TYPE.PROXY) regionProxyListContainer.append(html);
        else regionBackendListContainer.append(html);
    });

    // S3
    if (!isRefresh) {
        editRegionS3Endpoint.val(data.s3Config.endpoint);
        editRegionS3AccessKey.val(data.s3Config.accessKey);
        editRegionS3SecretKey.val(data.s3Config.secretKey);
        editRegionS3UseSSL.prop("checked", data.s3Config.useSSL);
    }

    // Settings (Status Toggles)
    if (PendingStatusAction?.entityId !== data.regionId) {
        editRegionMaintenanceMode.prop("checked", !!data.maintenanceEnabledAt);
        if (data.maintenanceEnabledAt) {
            regionMaintenanceReasonDisplay.removeClass("d-none").find("span").text(data.publicMaintenanceEnabledReason || "No reason provided");
        } else {
            regionMaintenanceReasonDisplay.addClass("d-none");
        }

        editRegionDisabled.prop("checked", !!data.disabledAt);
        if (data.disabledAt) {
            regionDisabledReasonDisplay.removeClass("d-none").find("span").text(data.publicDisabledReason || "No reason provided");
        } else {
            regionDisabledReasonDisplay.addClass("d-none");
        }
    }
}

// --- Server Manager Logic ---
function FillServerManagerTab(isRefresh = false) {
    const data = CurrentManageServerData;
    currentServerName.text(ManageServerType === 'new' ? "New Server" : data.endpoint);

    // Metrics (If edit mode)
    if (ManageServerType === 'edit') {
        if (data.metrics) {
            const metrics = data.metrics;
            const isOnline = metrics.runtimeStatus.value === INFRA_SERVER_STATUS.ONLINE;
            serverMetricStatusBadge.text(isOnline ? "Online" : "Offline").removeClass("bg-secondary bg-success bg-danger").addClass(isOnline ? "bg-success" : "bg-danger");
            serverMetricCpu.text(metrics.cpuUsagePercent.toFixed(1));
            serverMetricRam.text(metrics.memoryUsagePercent.toFixed(1));

            if (data.type.value === INFRA_SERVER_TYPE.BACKEND) {
                serverMetricDynamicCard.find(".small").text("Active Sessions");
                serverMetricDynamicCard.find(".display-6").text((metrics.currentActiveTelephonySessionCount || 0) + (metrics.currentActiveWebSessionCount || 0));
            } else {
                serverMetricDynamicCard.find(".small").text("Queue Pending");
                serverMetricDynamicCard.find(".display-6").text(metrics.currentOutboundMarkedQueues || 0);
            }

            // Dynamic Tab Visibility
            if (data.type.value === INFRA_SERVER_TYPE.BACKEND) {
                serverDynamicMetricTab.text("Sessions").removeClass("d-none");
            } else if (data.type.value === INFRA_SERVER_TYPE.PROXY) {
                serverDynamicMetricTab.text("Queues").removeClass("d-none");
            } else {
                serverDynamicMetricTab.addClass("d-none");
            }
        }
        else {
            serverMetricStatusBadge.text("Offline").removeClass("bg-secondary bg-success bg-danger").addClass("bg-danger");
            serverMetricCpu.text("N/A");
            serverMetricRam.text("N/A");
            serverMetricDynamicCard.find(".small").text("N/A");
            serverMetricDynamicCard.find(".display-6").text("N/A");
        }

        InitializeNodeChart('server', data.id);
    }

    // Settings
    if (ManageServerType === 'edit' && !isRefresh) {
        editServerEndpoint.val(data.endpoint);
        editServerApiKey.val(data.apiKey);
        editServerSipPort.val(data.sipPort);
        editServerType.val(data.type.value).prop('disabled', true);
        editServerIsDev.prop('checked', data.isDevelopmentServer);

        // Status Toggles
        if (PendingStatusAction?.entityId !== data.id) {
            editServerMaintenanceMode.prop('checked', !!data.maintenanceEnabledAt);
            if (data.maintenanceEnabledAt) serverMaintenanceReasonDisplay.removeClass("d-none").find("span").text(data.publicMaintenanceEnabledReason);
            else serverMaintenanceReasonDisplay.addClass("d-none");

            editServerDisabled.prop('checked', !!data.disabledAt);
            if (data.disabledAt) serverDisabledReasonDisplay.removeClass("d-none").find("span").text(data.publicDisabledReason);
            else serverDisabledReasonDisplay.addClass("d-none");
        }
    }
}


// --- Status Toggle Logic (Modal) ---
function handleStatusToggleClick(e, entityType, entityId) {
    e.preventDefault();

    const checkbox = $(e.currentTarget);
    const actionType = checkbox.data("type");
    const desiredState = checkbox.prop("checked");

    PendingStatusToggle = checkbox;
    PendingStatusAction = {
        entityId: entityId,
        entityType: entityType,
        actionType: actionType,
        enable: desiredState
    };

    if (desiredState) {
        infraStatusModalTitle.text(`Enable ${actionType === 'maintenance' ? 'Maintenance Mode' : 'Disable Entity'}`);
        infraStatusModalMessage.text("Please provide a public reason (visible to users) and a private reason (for logs).");
        infraStatusPublicReason.val("");
        infraStatusPrivateReason.val("");
        infraStatusReasonModal.show();
    } else {
        submitStatusChange({}, true);
    }
}

function submitStatusChange(reasons = { public: "", private: "" }, skipModal = false) {
    const action = PendingStatusAction;
    if (!action) return;

    const successCb = () => {
        AlertManager.createAlert({
            type: 'success',
            message: 'Status Updated',
            timeout: 3000
        });
        if (!skipModal) infraStatusReasonModal.hide();

        // Update UI state
        PendingStatusToggle.prop("checked", action.enable);

        // Clear pending
        PendingStatusToggle = null;
        PendingStatusAction = null;

        // Force refresh data
        if (action.entityType === 'region') FetchAndFillRegionDetail(CurrentManageRegionData.regionId);
        else FetchAndFillRegionDetail(CurrentManageRegionData.regionId);
    };

    const errorCb = (err) => {
        AlertManager.createAlert({
            type: 'danger',
            message: 'Failed to update status',
            timeout: 5000
        });
        PendingStatusToggle = null;
        PendingStatusAction = null;
    };

    if (action.entityType === 'region') {
        ToggleRegionStatus(action.entityId, action.actionType, action.enable, reasons, successCb, errorCb);
    } else {
        ToggleServerStatus(CurrentManageRegionData.regionId, action.entityId, action.actionType, action.enable, reasons, successCb, errorCb);
    }
}

// -- Chart Logic --

function InitInfraCharts() {
    // 1. Range Change
    historyRangeSelectors.on("change", function () {
        const group = $(this).attr("name").replace("HistoryRange", ""); // frontend, background, server
        const days = parseInt($(this).val());

        InfraChartManager[group].days = days;
        LoadFullHistory(group);
    });

    // 2. Metric Tab Change
    historyMetricTabsButtons.on("shown.bs.tab", function (e) {
        const group = $(e.target).closest("ul").data("target-chart");
        const metric = $(e.target).data("metric");

        InfraChartManager[group].metric = metric;
        LoadFullHistory(group);
    });
}

function InitializeNodeChart(group, nodeId) {
    if (!nodeId) return;

    // Stop existing poller if any
    if (InfraChartManager[group].timer) clearInterval(InfraChartManager[group].timer);

    InfraChartManager[group].nodeId = nodeId;
    InfraChartManager[group].lastRealPoint = null;

    // Initial Load
    LoadFullHistory(group);

    // Start Polling
    InfraChartManager[group].timer = setInterval(() => UpdateHistory(group), CHART_POLL_INTERVAL_MS);
}

function LoadFullHistory(group) {
    const config = InfraChartManager[group];
    if (!config.nodeId) return;

    const end = new Date();
    const start = new Date();
    start.setDate(end.getDate() - config.days);

    FetchServerHistory(config.nodeId, start.toISOString(), end.toISOString(), (rawData) => {
        if (!rawData || rawData.length === 0) {
            ToggleChartNoData(group, false);
            config.lastRealPoint = null;
            return;
        }

        ToggleChartNoData(group, true);

        // 1. Process Internal Gaps
        const processedData = ProcessDataWithGaps(rawData, null);

        // 2. Identify Last Real Point
        config.lastRealPoint = rawData[rawData.length - 1];

        // 3. Check Trailing Gap
        AddTrailingGapToNow(processedData, config.lastRealPoint);

        // 4. Render
        RenderFreshChart(group, processedData);
    });
}

function UpdateHistory(group) {
    const config = InfraChartManager[group];
    if (!config.nodeId || !config.lastRealPoint || !config.chart) return;

    const lastTime = new Date(config.lastRealPoint.lastUpdated);
    const start = new Date(lastTime.getTime() + 1000);
    const end = new Date();

    FetchServerHistory(config.nodeId, start.toISOString(), end.toISOString(), (rawData) => {
        let dataToAppend = [];

        if (rawData && rawData.length > 0) {
            dataToAppend = ProcessDataWithGaps(rawData, config.lastRealPoint);
            config.lastRealPoint = rawData[rawData.length - 1];
            AddTrailingGapToNow(dataToAppend, config.lastRealPoint);
        } else {
            // Offline - extend flat line
            const tempArray = [];
            AddTrailingGapToNow(tempArray, config.lastRealPoint);
            dataToAppend = tempArray;
        }

        if (dataToAppend.length > 0) {
            AppendDataToChart(group, dataToAppend);
        }
    });
}

function RenderFreshChart(group, data) {
    const config = InfraChartManager[group];

    let ctx = null;
    if (group === 'frontend') ctx = infraFrontendChartCanvas[0].getContext('2d');
    else if (group === 'background') ctx = infraBackgroundChartCanvas[0].getContext('2d');
    else ctx = serverMetricsChartCanvas[0].getContext('2d');

    if (config.chart) config.chart.destroy();

    const datasets = BuildDatasets(data, config.metric);
    const labels = data.map(d => d.lastUpdated);

    config.chart = new Chart(ctx, {
        type: 'line',
        data: { labels: labels, datasets: datasets },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: false,
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: { labels: { color: '#aaa' } },
                tooltip: {
                    callbacks: { title: (context) => new Date(context[0].label).toLocaleString() }
                }
            },
            scales: {
                x: {
                    ticks: {
                        color: '#777', maxTicksLimit: 8,
                        callback: function (val) {
                            const date = new Date(this.getLabelForValue(val));
                            if (config.days > 1) return date.toLocaleDateString();
                            return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
                        }
                    },
                    grid: { color: '#333' }
                },
                y: {
                    beginAtZero: true, grid: { color: '#333' },
                    suggestedMax: (config.metric === 'cpu' || config.metric === 'ram') ? 100 : undefined
                }
            },
            elements: {
                point: { radius: 0, hitRadius: 10 },
                line: { borderWidth: 1.5 }
            }
        }
    });
}

function AppendDataToChart(group, newData) {
    const chart = InfraChartManager[group].chart;
    const config = InfraChartManager[group];

    const newLabels = newData.map(d => d.lastUpdated);
    const newValuesMap = ExtractValuesForDatasets(newData, config.metric);

    newLabels.forEach(l => chart.data.labels.push(l));
    chart.data.datasets.forEach((dataset, index) => {
        newValuesMap[index].forEach(v => dataset.data.push(v));
    });

    // Trim Old Data
    const cutOffDate = new Date();
    cutOffDate.setDate(cutOffDate.getDate() - config.days);
    const cutOffTime = cutOffDate.getTime();

    while (chart.data.labels.length > 0) {
        const labelTime = new Date(chart.data.labels[0]).getTime();
        if (labelTime < cutOffTime) {
            chart.data.labels.shift();
            chart.data.datasets.forEach(ds => ds.data.shift());
        } else {
            break;
        }
    }

    chart.update('none');
}

function BuildDatasets(data, metric) {
    const segmentOptions = {
        borderColor: ctx => {
            if (ctx.p0.parsed.y === 0 && ctx.p1.parsed.y === 0) return 'rgba(220, 53, 69, 1)';
            if (ctx.p0.parsed.y === 0 || ctx.p1.parsed.y === 0) return 'rgba(220, 53, 69, 0.5)';
            return undefined;
        },
        borderDash: ctx => {
            if (ctx.p0.parsed.y === 0 && ctx.p1.parsed.y === 0) return [4, 4];
            return undefined;
        }
    };

    const commonOpts = { tension: 0.2, fill: true, segment: segmentOptions };

    switch (metric) {
        case 'cpu':
            return [{ ...commonOpts, label: 'CPU %', data: data.map(d => d.cpuUsagePercent), borderColor: '#0dcaf0', backgroundColor: 'rgba(13, 202, 240, 0.1)' }];
        case 'ram':
            return [{ ...commonOpts, label: 'RAM %', data: data.map(d => d.memoryUsagePercent), borderColor: '#d63384', backgroundColor: 'rgba(214, 51, 132, 0.1)' }];
        case 'network':
            return [
                { ...commonOpts, label: 'Down (Mbps)', data: data.map(d => d.networkDownloadMbps), borderColor: '#20c997', backgroundColor: 'rgba(32, 201, 151, 0.05)', fill: false },
                { ...commonOpts, label: 'Up (Mbps)', data: data.map(d => d.networkUploadMbps), borderColor: '#ffc107', backgroundColor: 'rgba(255, 193, 7, 0.05)', fill: false }
            ];
        case 'dynamic':
            const isBackend = data.length > 0 && data[0].maxConcurrentCallsCount !== undefined;
            if (isBackend) {
                return [
                    { ...commonOpts, label: 'Phone', data: data.map(d => d.currentActiveTelephonySessionCount), borderColor: '#0d6efd', backgroundColor: 'rgba(13, 110, 253, 0.1)' },
                    { ...commonOpts, label: 'Web', data: data.map(d => d.currentActiveWebSessionCount), borderColor: '#6610f2', backgroundColor: 'rgba(102, 16, 242, 0.1)' }
                ];
            } else {
                return [
                    { ...commonOpts, label: 'Processing', data: data.map(d => d.currentOutboundProcessingMarkedQueues), borderColor: '#fd7e14', backgroundColor: 'rgba(253, 126, 20, 0.1)' },
                    { ...commonOpts, label: 'Pending', data: data.map(d => d.currentOutboundMarkedQueues), borderColor: '#dc3545', backgroundColor: 'rgba(220, 53, 69, 0.1)' }
                ];
            }
    }
}

function ExtractValuesForDatasets(data, metric) {
    switch (metric) {
        case 'cpu': return [data.map(d => d.cpuUsagePercent)];
        case 'ram': return [data.map(d => d.memoryUsagePercent)];
        case 'network': return [data.map(d => d.networkDownloadMbps), data.map(d => d.networkUploadMbps)];
        case 'dynamic':
            const isBackend = data.length > 0 && data[0].maxConcurrentCallsCount !== undefined;
            if (isBackend) return [data.map(d => d.currentActiveTelephonySessionCount), data.map(d => d.currentActiveWebSessionCount)];
            else return [data.map(d => d.currentOutboundProcessingMarkedQueues), data.map(d => d.currentOutboundMarkedQueues)];
    }
}

function ToggleChartNoData(group, hasData) {
    const canvas = group === 'frontend' ? infraFrontendChartCanvas :
        group === 'background' ? infraBackgroundChartCanvas : serverMetricsChartCanvas;

    if (hasData) {
        canvas.removeClass('d-none');
        const id = canvas.attr('id');
        infraTab.find("#" + id + "NoData").addClass('d-none');
    } else {
        canvas.addClass('d-none');
        const id = canvas.attr('id');
        infraTab.find("#" + id + "NoData").removeClass('d-none');
    }
}

function createZeroPoint(timestamp) {
    return {
        isSynthetic: true,
        lastUpdated: new Date(timestamp).toISOString(),
        cpuUsagePercent: 0, memoryUsagePercent: 0,
        networkDownloadMbps: 0, networkUploadMbps: 0,
        currentActiveTelephonySessionCount: 0, currentActiveWebSessionCount: 0,
        currentOutboundProcessingMarkedQueues: 0, currentOutboundMarkedQueues: 0
    };
}

function ProcessDataWithGaps(newData, previousPoint) {
    if (!newData || newData.length === 0) return [];
    const processed = [];
    let prev = previousPoint;

    for (let i = 0; i < newData.length; i++) {
        const curr = newData[i];
        const currTime = new Date(curr.lastUpdated).getTime();

        if (prev) {
            const prevTime = new Date(prev.lastUpdated).getTime();
            if (currTime - prevTime > GAP_THRESHOLD_MS) {
                processed.push(createZeroPoint(prevTime + 1000));
                processed.push(createZeroPoint(currTime - 1000));
            }
        }
        processed.push(curr);
        prev = curr;
    }
    return processed;
}

function AddTrailingGapToNow(dataArray, lastRealPoint) {
    if (!lastRealPoint) return;

    const lastTime = new Date(lastRealPoint.lastUpdated).getTime();
    const now = new Date().getTime();

    if (now - lastTime > GAP_THRESHOLD_MS) {
        const lastRendered = dataArray.length > 0 ? dataArray[dataArray.length - 1] : lastRealPoint;
        const lastRenderedTime = new Date(lastRendered.lastUpdated).getTime();

        if (lastRenderedTime < lastTime + 2000) {
            dataArray.push(createZeroPoint(lastTime + 1000));
        }
        dataArray.push(createZeroPoint(now));
    }
}

// Helper for select population
function populateCountriesList() {
    infraNewRegionCountry.empty().append('<option value="" disabled selected>Select Country</option>');
    if (typeof CountriesList !== 'undefined') {
        Object.keys(CountriesList).forEach(code => {
            infraNewRegionCountry.append(`<option value="${code}">${CountriesList[code].Country}</option>`);
        });
    }
}


/** INIT **/
function initInfrastructureTab() {
    $(document).ready(() => {
        showInfraRootTab();

        // --- Event Handlers ---

        InitInfraCharts();

        // ROOT: Add Region Modal
        addNewRegionButton.on("click", () => {
            infraNewRegionCountry.val("");
            infraNewRegionId.val("");
            infraNewRegionPreview.text("...");
            populateCountriesList();
            infraAddRegionModal.show();
        });

        // Add Region Modal: Input Change
        infraNewRegionCountry.add(infraNewRegionId).on("input change", () => {
            const country = (infraNewRegionCountry.val() || "??").toUpperCase();
            const region = (infraNewRegionId.val() || "??").toUpperCase();
            infraNewRegionPreview.text(`${country}-${region}`);
        });

        // Add Region Modal: Confirm
        infraBtnConfirmAddRegion.on("click", () => {
            const country = infraNewRegionCountry.val();
            const region = infraNewRegionId.val();

            if (!country || !region) {
                AlertManager.createAlert({
                    type: 'warning',
                    message: 'Please select a country and enter a region identifier.',
                    timeout: 4000
                });
                return;
            }

            const data = {
                countryCode: country.toUpperCase(),
                regionName: region.toUpperCase(),
            };

            const btn = infraBtnConfirmAddRegion;
            const originalText = btn.text();
            btn.prop('disabled', true).text('Creating...');

            AddRegion(data,
                () => {
                    AlertManager.createAlert({
                        type: 'success',
                        message: 'Region created successfully.',
                        timeout: 6000
                    });
                    infraAddRegionModal.hide();
                    FetchAndFillOverview();
                    btn.prop('disabled', false).text(originalText);
                },
                (err) => {
                    AlertManager.createAlert({
                        type: 'danger',
                        message: 'Failed to create region.',
                        timeout: 6000
                    })
                    btn.prop('disabled', false).text(originalText);
                }
            );
        });

        // ROOT: Click Region
        infraRegionsCardListContainer.on("click", ".region-card", (e) => {
            if ($(e.target).closest(".dropdown").length != 0) return;
            ManageRegionType = 'edit';
            const rId = $(e.currentTarget).data("item-id");
            GetRegionDetail(rId, (data) => {
                CurrentManageRegionData = data;
                showRegionManagerTab();
                FillRegionManagerTab();
            }, (e) => AlertManager.createAlert({
                type: 'danger',
                message: 'Error loading region',
                timeout: 6000
            }));
        });

        // ROOT: Delete Region
        infraRegionsCardListContainer.on("click", "span[button-type='delete-region']", (e) => {
            const rId = $(e.currentTarget).data("item-id");
            const confirm = new BootstrapConfirmDialog({
                title: "Delete Region",
                message: `Delete ${rId}?`,
                confirmButtonClass: "btn-danger"
            });
            confirm.show().then(ok => {
                if (ok) {
                    DeleteRegion(rId, () => {
                        AlertManager.createAlert({
                            type: 'success',
                            message: 'Deleted',
                            timeout: 6000
                        });
                        FetchAndFillOverview();
                    },
                    (e) => AlertManager.createAlert({
                        type: 'danger',
                        message: 'Failed',
                        timeout: 6000
                    }));
                }
            });
        });

        // REGION MANAGER: Back
        switchBackToInfraRoot.on("click", (e) => {
            e.preventDefault();
            showInfraRootTab();
        });

        // REGION MANAGER: Save S3
        confirmSaveRegionButton.on("click", () => {
            confirmSaveRegionButtonSpinner.removeClass("d-none");
            confirmSaveRegionButton.prop("disabled", true);

            const s3Data = {
                endpoint: editRegionS3Endpoint.val(),
                accessKey: editRegionS3AccessKey.val(),
                secretKey: editRegionS3SecretKey.val(),
                useSSL: editRegionS3UseSSL.is(":checked")
            };

            SaveRegionConfig(CurrentManageRegionData.regionId, { s3: s3Data },
                () => {
                    AlertManager.createAlert({
                        type: 'success',
                        message: 'Saved',
                        timeout: 3000
                    });
                    confirmSaveRegionButtonSpinner.addClass("d-none");
                },
                (e) => {
                    AlertManager.createAlert({
                        type: 'danger',
                        message: 'Failed',
                        timeout: 6000
                    });
                    confirmSaveRegionButtonSpinner.addClass("d-none");
                    confirmSaveRegionButton.prop("disabled", false);
                }
            );
        });

        // REGION MANAGER: S3 Input Changes
        regionS3Inputs.on("input change", () => {
            confirmSaveRegionButton.prop("disabled", false);
        });

        // REGION MANAGER & SERVER MANAGER: Status Toggles
        const statusToggles = editRegionMaintenanceMode.add(editRegionDisabled).add(editServerMaintenanceMode).add(editServerDisabled);
        statusToggles.on("click", function (e) {
            const entityType = $(this).attr("id").includes("Region") ? "region" : "server";
            const entityId = entityType === "region" ? CurrentManageRegionData.regionId : CurrentManageServerData.id;
            handleStatusToggleClick(e, entityType, entityId);
        });

        // SERVER MANAGER: Open Add
        btnAddProxyServer.on("click", () => openServerHandler(null));
        btnAddBackendServer.on("click", () => openServerHandler(null));

        // SERVER MANAGER: Open Edit (Delegation)
        regionProxyListContainer
            .add(regionBackendListContainer)
            .on("click", ".server-card", (e) => openServerHandler($(e.currentTarget).data("endpoint")));

        const openServerHandler = (endpoint) => {
            ManageServerType = endpoint ? 'edit' : 'new';
            if (ManageServerType === 'edit') {
                CurrentManageServerData = CurrentManageRegionData.servers.find(s => s.endpoint === endpoint);
            } else {
                CurrentManageServerData = {};
            }
            showServerManagerTab();
            FillServerManagerTab();
        };

        // SERVER MANAGER: Back
        switchBackToRegionManager.on("click", (e) => {
            e.preventDefault();
            clearInterval(InfraChartManager.server.timer);
            InfraChartManager.server.timer = null;
            InfraChartManager.server.nodeId = null;
            showRegionManagerTab();
        });

        // SERVER MANAGER: Save
        confirmSaveServerButton.on("click", () => {
            confirmSaveServerButtonSpinner.removeClass("d-none");
            confirmSaveServerButton.prop("disabled", true);

            const data = {
                endpoint: editServerEndpoint.val(),
                apiKey: editServerApiKey.val(),
                sipPort: parseInt(editServerSipPort.val()),
                type: parseInt(editServerType.val()),
                isDevelopmentServer: editServerIsDev.is(":checked")
            };

            const sId = ManageServerType === 'edit' ? CurrentManageServerData.id : null;
            SaveServerConfig(CurrentManageRegionData.regionId, sId, data,
                () => {
                    AlertManager.createAlert({ type: 'success', message: 'Server Saved', timeout: 3000 });
                    confirmSaveServerButtonSpinner.addClass("d-none");
                    showRegionManagerTab();
                },
                (e) => {
                    AlertManager.createAlert({ type: 'danger', message: 'Failed', timeout: 6000 });
                    confirmSaveServerButtonSpinner.addClass("d-none");
                    confirmSaveServerButton.prop("disabled", false);
                }
            );
        });

        // SERVER MANAGER: Config Changes
        serverConfigInputs.on("input change", () => {
            confirmSaveServerButton.prop("disabled", false);
        });

        // SERVER MANAGER: Delete
        infraServerManagerTab.find("#btnDeleteServer").on("click", () => {
            if (confirm("Delete Server?")) {
                DeleteServer(CurrentManageRegionData.regionId, CurrentManageServerData.id,
                    () => {
                        AlertManager.createAlert({
                            type: 'success',
                            message: 'Deleted',
                            timeout: 6000
                        }); showRegionManagerTab();
                    },
                    (e) => AlertManager.createAlert({
                        type: 'danger',
                        message: 'Failed',
                        timeout: 6000
                    })
                );
            }
        });

        // MODAL CONFIRM
        infraStatusConfirmButton.on("click", () => {
            const pub = infraStatusPublicReason.val();
            const priv = infraStatusPrivateReason.val();
            if (!pub || !priv) {
                AlertManager.createAlert({
                    type: 'warning',
                    message: 'Both reasons required',
                    timeout: 4000
                }); return;
            }
            submitStatusChange({ public: pub, private: priv });
        });
    });
}