/** Dynamic Variables **/
let AppTabPollingInterval = null;
let CurrentPermissionContext = null; // 'maintenance', 'registration', 'login'

/** Element Variables **/

// Main Tab
const appTab = $("#app-tab");

// Inner Tabs
const appInnerTab = appTab.find("#app-inner-tab");
const appOverviewTabButton = appInnerTab.find("#app-overview-tab-button");
const appPermissionsTabButton = appInnerTab.find("#app-permissions-tab-button");
const appEmailTemplatesTabButton = appInnerTab.find("#app-email-templates-tab-button");

// 1. Overview Content
const appOverviewContent = appTab.find("#app-overview-content");
const appUpdateBanner = appOverviewContent.find("#appUpdateBanner");
const appRemoteVersion = appOverviewContent.find("#appRemoteVersion");
const appUpdateChangelog = appOverviewContent.find("#appUpdateChangelog");
const appCurrentVersion = appOverviewContent.find("#appCurrentVersion");
const appVersionBadge = appOverviewContent.find("#appVersionBadge");
const appInstallDate = appOverviewContent.find("#appInstallDate");
const appSchemaVersion = appOverviewContent.find("#appSchemaVersion");
const appMigrationStatus = appOverviewContent.find("#appMigrationStatus");

// 2. Permissions Content
const appPermissionsContent = appTab.find("#app-permissions-content");
// Maintenance
const appPermissionMaintenanceSwitch = appPermissionsContent.find("#switchMaintenanceMode");
const appPermissionMaintenanceConfigBtn = appPermissionsContent.find("#btnConfigureMaintenance");
// Registration
const appPermissionRegistrationSwitch = appPermissionsContent.find("#switchDisableRegistration");
const appPermissionRegistrationConfigBtn = appPermissionsContent.find("#btnConfigureRegistration");
// Login
const appPermissionLoginSwitch = appPermissionsContent.find("#switchDisableLogin");
const appPermissionLoginConfigBtn = appPermissionsContent.find("#btnConfigureLogin");

// 3. Email Templates Content
const appEmailTemplatesContent = appTab.find("#app-email-templates-content");
const appEmailTemplatesSaveBtn = appEmailTemplatesContent.find("#btnSaveEmailTemplates");
// Verify
const appEmailVerifySubject = appEmailTemplatesContent.find("#inputVerifySubject");
const appEmailVerifyBody = appEmailTemplatesContent.find("#inputVerifyBody");
// Welcome
const appEmailWelcomeSubject = appEmailTemplatesContent.find("#inputWelcomeSubject");
const appEmailWelcomeBody = appEmailTemplatesContent.find("#inputWelcomeBody");
// Reset
const appEmailResetSubject = appEmailTemplatesContent.find("#inputResetSubject");
const appEmailResetBody = appEmailTemplatesContent.find("#inputResetBody");

// Modal
const appPermissionConfigModalEl = document.getElementById('permissionConfigModal'); // Native for Bootstrap
const appPermissionConfigModal = new bootstrap.Modal(appPermissionConfigModalEl);
const appPermissionConfigTitle = $(appPermissionConfigModalEl).find("#permissionConfigTitle");
const appPermissionConfigTarget = $(appPermissionConfigModalEl).find("#permissionConfigTarget");
const appPermissionPublicReason = $(appPermissionConfigModalEl).find("#permissionPublicReason");
const appPermissionPrivateReason = $(appPermissionConfigModalEl).find("#permissionPrivateReason");
const appPermissionConfigWarning = $(appPermissionConfigModalEl).find("#permissionConfigWarning");
const appPermissionConfigSaveBtn = $(appPermissionConfigModalEl).find("#btnSavePermissionConfig");


/** API FUNCTIONS **/

function GetAppConfig(onSuccess, onError) {
    $.get('/app/admin/app/config', (res) => res.success ? onSuccess(res.data) : onError(res));
}

function CheckAppUpdates(onSuccess, onError) {
    $.post('/app/admin/app/update-status', (res) => res.success ? onSuccess(res.data) : onError(res));
}

function GetAppPermissions(onSuccess, onError) {
    $.get('/app/admin/app/permissions', (res) => res.success ? onSuccess(res.data) : onError(res));
}

function UpdateAppPermissions(data, onSuccess, onError) {
    $.ajax({
        url: '/app/admin/app/permissions',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: (res) => res.success ? onSuccess(res.data) : onError(res),
        error: (err) => onError(err)
    });
}

function GetAppEmailTemplates(onSuccess, onError) {
    $.get('/app/admin/app/email-templates', (res) => res.success ? onSuccess(res.data) : onError(res));
}

function UpdateAppEmailTemplates(data, onSuccess, onError) {
    $.ajax({
        url: '/app/admin/app/email-templates',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: (res) => res.success ? onSuccess(res.data) : onError(res),
        error: (err) => onError(err)
    });
}

/** FUNCTIONS **/

// Overview Logic
function startAppOverviewPolling() {
    refreshAppConfig(true);
    refreshUpdateStatus(true);

    if (AppTabPollingInterval) clearInterval(AppTabPollingInterval);
    AppTabPollingInterval = setInterval(() => {
        // Only poll if tab is visible
        if (appTab.hasClass("show")) {
            refreshAppConfig();
            refreshUpdateStatus();
        }
    }, 5000);
}

function refreshAppConfig(force = false) {
    GetAppConfig(
        (data) => {
            CurrentAppConfigData = data;
            fillAppOverview();
        },
        (err) => console.error("Config Poll Error", err)
    );
}

function refreshUpdateStatus(force = false) {
    CheckAppUpdates(
        (data) => {
            fillUpdateStatus(data);
        },
        (err) => console.error("Update Poll Error", err)
    );
}

function fillAppOverview() {
    if (!CurrentAppConfigData) return;

    appCurrentVersion.text("v" + CurrentAppConfigData.installedVersion);

    const date = new Date(CurrentAppConfigData.installationDate);
    appInstallDate.text(date.toLocaleDateString() + ' ' + date.toLocaleTimeString());

    appSchemaVersion.text(CurrentAppConfigData.installedVersion);

    if (CurrentAppConfigData.isMigrationInProgress) {
        appMigrationStatus.removeClass('bg-success text-success').addClass('bg-warning text-dark')
            .html('<i class="fa-solid fa-arrows-rotate fa-spin me-1"></i> In Progress');
    } else {
        appMigrationStatus.removeClass('bg-warning text-dark').addClass('bg-success bg-opacity-10 text-success')
            .text('Idle / Ready');
    }
}

function fillUpdateStatus(updateData) {
    if (!updateData) return;

    if (updateData.isUpdateAvailable) {
        appUpdateBanner.removeClass('d-none').addClass('d-flex');
        appRemoteVersion.text(updateData.latestVersion);
        appVersionBadge.addClass('d-none');

        const link = updateData.changelogUrl || 'https://github.com/abdofallah/IqraAI';
        appUpdateChangelog.attr('href', link);
    } else {
        appUpdateBanner.addClass('d-none').removeClass('d-flex');
        appVersionBadge.removeClass('d-none');
    }
}

// Permissions Logic
function fetchAndFillPermissions() {
    GetAppPermissions((data) => {
        CurrentAppPermissionsData = data;
        fillPermissionsTab();
    }, (err) => AlertManager.createAlert({ type: 'danger', message: 'Failed to load permissions', timeout: 5000 }));
}

function fillPermissionsTab() {
    if (!CurrentAppPermissionsData) return;

    // Maintenance
    const isMaintenance = !!CurrentAppPermissionsData.maintenanceEnabledAt;
    appPermissionMaintenanceSwitch.prop('checked', isMaintenance);
    appPermissionMaintenanceConfigBtn.toggleClass('active', isMaintenance);

    // Registration
    const isRegDisabled = !!CurrentAppPermissionsData.registerationDisabledAt;
    appPermissionRegistrationSwitch.prop('checked', isRegDisabled);
    appPermissionRegistrationConfigBtn.toggleClass('active', isRegDisabled);

    // Login
    const isLoginDisabled = !!CurrentAppPermissionsData.loginDisabledAt;
    appPermissionLoginSwitch.prop('checked', isLoginDisabled);
    appPermissionLoginConfigBtn.toggleClass('active', isLoginDisabled);
}

function handlePermissionSwitchClick(event, context, currentState) {
    event.preventDefault(); // Stop immediate toggle, let logic decide

    if (currentState) {
        // Turning OFF -> Immediate
        const newData = structuredClone(CurrentAppPermissionsData);
        setLocalPermissionState(newData, context, false);
        savePermissions(newData);
    } else {
        // Turning ON -> Reason Modal
        openPermissionModal(context);
    }
}

function openPermissionModal(context) {
    CurrentPermissionContext = context;
    let title = "Configure ";
    let publicVal = "";
    let privateVal = "";

    if (context === 'maintenance') {
        title += "Maintenance Mode";
        publicVal = CurrentAppPermissionsData.publicMaintenanceEnabledReason;
        privateVal = CurrentAppPermissionsData.privateMaintenanceEnabledReason;
    } else if (context === 'registration') {
        title += "Registration Lock";
        publicVal = CurrentAppPermissionsData.publicRegisterationDisabledReason;
        privateVal = CurrentAppPermissionsData.privateRegisterationDisabledReason;
    } else if (context === 'login') {
        title += "Login Lock";
        publicVal = CurrentAppPermissionsData.publicLoginDisabledReason;
        privateVal = CurrentAppPermissionsData.privateLoginDisabledReason;
    }

    appPermissionConfigTitle.text(title);
    appPermissionConfigTarget.val(context);
    appPermissionPublicReason.val(publicVal || "");
    appPermissionPrivateReason.val(privateVal || "");

    appPermissionConfigModal.show();
}

function setLocalPermissionState(dataObj, context, isEnabled, pubReason = null, privReason = null) {
    const timestamp = isEnabled ? new Date().toISOString() : null;

    if (context === 'maintenance') {
        dataObj.maintenanceEnabledAt = timestamp;
        if (isEnabled) {
            dataObj.publicMaintenanceEnabledReason = pubReason;
            dataObj.privateMaintenanceEnabledReason = privReason;
        }
    } else if (context === 'registration') {
        dataObj.registerationDisabledAt = timestamp;
        if (isEnabled) {
            dataObj.publicRegisterationDisabledReason = pubReason;
            dataObj.privateRegisterationDisabledReason = privReason;
        }
    } else if (context === 'login') {
        dataObj.loginDisabledAt = timestamp;
        if (isEnabled) {
            dataObj.publicLoginDisabledReason = pubReason;
            dataObj.privateLoginDisabledReason = privReason;
        }
    }
}

function savePermissions(newData, onSuccessCallback) {
    UpdateAppPermissions(newData, (updatedData) => {
        CurrentAppPermissionsData = updatedData;
        fillPermissionsTab();
        AlertManager.createAlert({ type: 'success', message: 'Permissions updated successfully.', timeout: 3000 });
        if (onSuccessCallback) onSuccessCallback();
    }, (err) => {
        AlertManager.createAlert({ type: 'danger', message: err.message || 'Failed to save permissions.', timeout: 5000 });
    });
}

// Email Templates Logic
function fetchAndFillEmailTemplates() {
    GetAppEmailTemplates((data) => {
        CurrentAppEmailTemplatesData = data;
        fillEmailTemplatesTab();
    }, (err) => AlertManager.createAlert({ type: 'danger', message: 'Failed to load templates', timeout: 5000 }));
}

function fillEmailTemplatesTab() {
    if (!CurrentAppEmailTemplatesData) return;

    appEmailVerifySubject.val(CurrentAppEmailTemplatesData.verifyEmailTemplate.subject);
    appEmailVerifyBody.val(CurrentAppEmailTemplatesData.verifyEmailTemplate.body);

    appEmailWelcomeSubject.val(CurrentAppEmailTemplatesData.welcomeUserTemplate.subject);
    appEmailWelcomeBody.val(CurrentAppEmailTemplatesData.welcomeUserTemplate.body);

    appEmailResetSubject.val(CurrentAppEmailTemplatesData.resetPasswordTemplate.subject);
    appEmailResetBody.val(CurrentAppEmailTemplatesData.resetPasswordTemplate.body);
}

/** INIT **/
function initAppTab() {
    $(document).ready(() => {
        // 1. Overview Init
        startAppOverviewPolling();
        fetchAndFillPermissions();
        fetchAndFillEmailTemplates();

        // Maintenance Switch
        appPermissionMaintenanceSwitch.on('click', (e) => {
            handlePermissionSwitchClick(e, 'maintenance', !!CurrentAppPermissionsData.maintenanceEnabledAt);
        });
        appPermissionMaintenanceConfigBtn.on('click', () => openPermissionModal('maintenance'));

        // Registration Switch
        appPermissionRegistrationSwitch.on('click', (e) => {
            handlePermissionSwitchClick(e, 'registration', !!CurrentAppPermissionsData.registerationDisabledAt);
        });
        appPermissionRegistrationConfigBtn.on('click', () => openPermissionModal('registration'));

        // Login Switch
        appPermissionLoginSwitch.on('click', (e) => {
            handlePermissionSwitchClick(e, 'login', !!CurrentAppPermissionsData.loginDisabledAt);
        });
        appPermissionLoginConfigBtn.on('click', () => openPermissionModal('login'));

        // Modal Save
        appPermissionConfigSaveBtn.on('click', () => {
            const pub = appPermissionPublicReason.val();
            const priv = appPermissionPrivateReason.val();

            if (!pub || !priv) {
                AlertManager.createAlert({ type: 'warning', message: 'Public and Private reasons are required.', timeout: 4000 });
                return;
            }

            const newData = structuredClone(CurrentAppPermissionsData);
            setLocalPermissionState(newData, CurrentPermissionContext, true, pub, priv);

            savePermissions(newData, () => {
                appPermissionConfigModal.hide();
            });
        });

        appEmailTemplatesSaveBtn.on('click', function () {
            const btn = $(this);
            const originalText = btn.html();
            btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status"></span> Saving...');

            const payload = {
                verifyEmailTemplate: {
                    subject: appEmailVerifySubject.val(),
                    body: appEmailVerifyBody.val()
                },
                welcomeUserTemplate: {
                    subject: appEmailWelcomeSubject.val(),
                    body: appEmailWelcomeBody.val()
                },
                resetPasswordTemplate: {
                    subject: appEmailResetSubject.val(),
                    body: appEmailResetBody.val()
                }
            };

            UpdateAppEmailTemplates(payload, (data) => {
                CurrentAppEmailTemplatesData = data;
                AlertManager.createAlert({ type: 'success', message: 'Email templates saved.', timeout: 3000 });
                btn.prop('disabled', false).html(originalText);
            }, (err) => {
                AlertManager.createAlert({ type: 'danger', message: err.message || 'Failed to save templates.', timeout: 5000 });
                btn.prop('disabled', false).html(originalText);
            });
        });
    });
}