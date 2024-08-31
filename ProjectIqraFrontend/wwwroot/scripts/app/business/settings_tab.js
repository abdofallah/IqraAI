const DefaultBusinessImgSRC = "/img/logo/logo-light.png";
const DefaultWhiteLabelLogoSRC = "/img/logo/logo-colored-light.png";
const DefaultWhiteLabelFaviconSRC = "/img/logo/logo-colored-light.png";

const IqraBusinessDomain = "iqra.business"; // todo get this dynamically from server

var IsSavingDomainTab = false;
var IsManagerDomainTabOpened = false;
var ManageDomainType = null;
var CurrentManageDomainData = null;

var IsSavingUsersTab = false;
var IsManageUserTabOpened = false;
var ManageUserType = null;
var CurrentManageSubUserData = null;

// Elements
const settingsTab = $("#settings-tab");

const settingsSaveButton = settingsTab.find("#settingsSaveButton");
const settingsSaveButtonSpinner = settingsSaveButton.find(".save-button-spinner");

const settingsGeneralBusinessName = settingsTab.find("#settingsGeneralBusinessName");
const settingsGeneralBusinessLogoPreview = settingsTab.find("#settingsGeneralBusinessLogoPreview");
const settingsGeneralBusinessLogo = settingsTab.find("#settingsGeneralBusinessLogo");

const settingsLanguageAddSelect = settingsTab.find("#settingsLanguageAddSelect");
const settingsLanguageAddButton = settingsTab.find("#settingsLanguageAddButton");
const settingsAddedLanguagesList = settingsTab.find("#settingsAddedLanguagesList");

const businessSubusersTable = settingsTab.find("#businessSubusersTable");
const addNewBusinessSubuserButton = settingsTab.find("#addNewBusinessSubuserButton");

const businessSubusersListTab = settingsTab.find("#businessSubusersListTab");
const subusersManagerTab = settingsTab.find("#subusersManagerTab");

const settingsInnerTabContainer = settingsTab.find("#settings-inner-tab-container");
const settingsInnerGeneralTab = settingsTab.find("#settings-inner-general-tab");

// Subuser Navigation
const settingsManageSubusersBreadcrumb = settingsTab.find("#settings-manage-subusers-breadcrumb");
const switchBackToBusinessSubusersTab = settingsTab.find("#switchBackToBusinessSubusersTab");
const currentBusinessSubuserName = settingsTab.find("#currentBusinessSubuserName");

const saveBusinessSubuserButton = settingsManageSubusersBreadcrumb.find("#saveBusinessSubuserButton");
const saveBusinessSubuserButtonSpinner = saveBusinessSubuserButton.find(".save-button-spinner");

const businessSubuserManagerGeneralTab = settingsManageSubusersBreadcrumb.find("#business-subuser-manager-general-tab");
const businessSubuserPermissionsRoutingTab = subusersManagerTab.find("#business-subuser-permissions-routing-tab");

const subusersWhitelabelGeneralTab = subusersManagerTab.find("#subusers-whitelabel-general-tab");

// Sub Users General
const businessSubuserEmail = subusersManagerTab.find("#businessSubuserEmail");
const businessSubuserPassword = subusersManagerTab.find("#businessSubuserPassword");

const businessSubuserLoginDisabledInput = subusersManagerTab.find("#businessSubuserLoginDisabledInput");
const businessSubuserLoginDisabledReasonInput = subusersManagerTab.find("#businessSubuserLoginDisabledReasonInput");

// White Label
const businessSubuserWhiteLabelPlatformName = subusersManagerTab.find("#business-subuser-white-label-platform-name");
const businessSubuserWhiteLabelPlatformTitle = subusersManagerTab.find("#business-subuser-white-label-platform-title");
const businessSubuserWhiteLabelPlatformDescription = subusersManagerTab.find("#business-subuser-white-label-platform-description");
const businessSubuserWhiteLabelDomainIdentifier = subusersManagerTab.find("#business-subuser-white-label-domain-identifier");

// White Label Styles
const businessSubuserWhiteLabelLogoPreview = subusersManagerTab.find("#business-subuser-white-label-logo-preview");
const businessSubuserWhiteLabelLogo = subusersManagerTab.find("#business-subuser-white-label-logo");

const businessSubuserWhiteLabelFaviconPreview = subusersManagerTab.find("#business-subuser-white-label-favicon-preview");
const businessSubuserWhiteLabelFavicon = subusersManagerTab.find("#business-subuser-white-label-favicon");

const businessSubuserWhiteLabelCustomCss = subusersManagerTab.find("#business-subuser-white-label-custom-css");
const businessSubuserWhiteLabelCustomJs = subusersManagerTab.find("#business-subuser-white-label-custom-js");

// Permissions Routing
const businessSubuserRoutingTabEnabled = subusersManagerTab.find("#business-subuser-routing-tab-enabled");
const businessSubuserRoutingAddNewRoute = subusersManagerTab.find("#business-subuser-routing-add-new-route");
const businessSubuserRoutingEditRoute = subusersManagerTab.find("#business-subuser-routing-edit-route");
const businessSubuserRoutingDeleteRoute = subusersManagerTab.find("#business-subuser-routing-delete-route");

// Permissions Tools
const businessSubuserToolsTabEnabled = subusersManagerTab.find("#business-subuser-tools-tab-enabled");
const businessSubuserToolsAddNewTool = subusersManagerTab.find("#business-subuser-tools-add-new-tool");
const businessSubuserToolsEditTool = subusersManagerTab.find("#business-subuser-tools-edit-tool");
const businessSubuserToolsDeleteTool = subusersManagerTab.find("#business-subuser-tools-delete-tool");

// Permissions Agents
const businessSubuserAgentsTabEnabled = subusersManagerTab.find("#business-subuser-agents-tab-enabled");
const businessSubuserAgentsAddNewAgent = subusersManagerTab.find("#business-subuser-agents-add-new-agent");
const businessSubuserAgentsEditAgent = subusersManagerTab.find("#business-subuser-agents-edit-agent");
const businessSubuserAgentsDeleteAgent = subusersManagerTab.find("#business-subuser-agents-delete-agent");

// Permissions Context
const businessSubuserContextTabEnabled = subusersManagerTab.find("#business-subuser-context-tab-enabled");
// Context Branding
const businessSubuserContextBrandingTabEnabled = subusersManagerTab.find("#business-subuser-context-branding-tab-enabled");
const businessSubuserContextEditBranding = subusersManagerTab.find("#business-subuser-context-edit-branding");
// Context Branches
const businessSubuserContextBranchesTabEnabled = subusersManagerTab.find("#business-subuser-context-branches-tab-enabled");
const businessSubuserContextAddNewBranch = subusersManagerTab.find("#business-subuser-context-add-branches");
const businessSubuserContextEditBranch = subusersManagerTab.find("#business-subuser-context-edit-branches");
const businessSubuserContextDeleteBranch = subusersManagerTab.find("#business-subuser-context-delete-branches");
// Context Services
const businessSubuserContextServicesTabEnabled = subusersManagerTab.find("#business-subuser-context-services-tab-enabled");
const businessSubuserContextAddNewService = subusersManagerTab.find("#business-subuser-context-add-services");
const businessSubuserContextEditService = subusersManagerTab.find("#business-subuser-context-edit-services");
const businessSubuserContextDeleteService = subusersManagerTab.find("#business-subuser-context-delete-services");
// Context Products
const businessSubuserContextProductsTabEnabled = subusersManagerTab.find("#business-subuser-context-products-tab-enabled");
const businessSubuserContextAddNewProduct = subusersManagerTab.find("#business-subuser-context-add-products");
const businessSubuserContextEditProduct = subusersManagerTab.find("#business-subuser-context-edit-products");
const businessSubuserContextDeleteProduct = subusersManagerTab.find("#business-subuser-context-delete-products");

// Permissions MakeCall
const businessSubuserMakeCallsTabEnabled = subusersManagerTab.find("#business-subuser-make-calls-tab-enabled");
const businessSubuserMakeCallsSingleCallEnabled = subusersManagerTab.find("#business-subuser-make-calls-single-call-enabled");
const businessSubuserMakeCallsBulkCallEnabled = subusersManagerTab.find("#business-subuser-make-calls-bulk-call-enabled");

// Permissions Conversations
const businessSubuserConversationsTabEnabled = subusersManagerTab.find("#business-subuser-conversations-tab-enabled");
// Conversation Inbound
const businessSubuserConversationsInboundCallTabEnabled = subusersManagerTab.find("#business-subuser-conversations-inbound-call-tab-enabled");
const businessSubuserConversationsDeleteInboundCall = subusersManagerTab.find("#business-subuser-conversations-delete-inbound-call");
const businessSubuserConversationsExportInboundCall = subusersManagerTab.find("#business-subuser-conversations-export-inbound-call");
// Conversation Outbound
const businessSubuserConversationsOutboundCallTabEnabled = subusersManagerTab.find("#business-subuser-conversations-outbound-call-tab-enabled");
const businessSubuserConversationsDeleteOutboundCall = subusersManagerTab.find("#business-subuser-conversations-delete-outbound-call");
const businessSubuserConversationsExportOutboundCall = subusersManagerTab.find("#business-subuser-conversations-export-outbound-call");
// Conversation Websocket
const businessSubuserConversationsWebsocketTabEnabled = subusersManagerTab.find("#business-subuser-conversations-websocket-tab-enabled");
const businessSubuserConversationsDeleteWebsocket = subusersManagerTab.find("#business-subuser-conversations-delete-websocket");
const businessSubuserConversationsExportWebsocket = subusersManagerTab.find("#business-subuser-conversations-export-websocket");

// Permissions Settings
const businessSubuserSettingsTabEnabled = subusersManagerTab.find("#business-subuser-settings-tab-enabled");
// Settings General
const businessSubuserSettingsGeneralTabEnabled = subusersManagerTab.find("#business-subuser-settings-general-tab-enabled");
const businessSubuserSettingsEditGeneral = subusersManagerTab.find("#business-subuser-settings-edit-general");
// Settings Languages
const businessSubuserSettingsLanguagesTabEnabled = subusersManagerTab.find("#business-subuser-settings-languages-tab-enabled");
const businessSubuserSettingsEditLanguages = subusersManagerTab.find("#business-subuser-settings-edit-languages");
const businessSubuserSettingsAddLanguages = subusersManagerTab.find("#business-subuser-settings-add-languages");
const businessSubuserSettingsDeleteLanguages = subusersManagerTab.find("#business-subuser-settings-delete-languages");
// Settings Users
const businessSubuserSettingsUsersTabEnabled = subusersManagerTab.find("#business-subuser-settings-users-tab-enabled");
const businessSubuserSettingsEditUsers = subusersManagerTab.find("#business-subuser-settings-edit-users");
const businessSubuserSettingsAddUsers = subusersManagerTab.find("#business-subuser-settings-add-users");
const businessSubuserSettingsDeleteUsers = subusersManagerTab.find("#business-subuser-settings-delete-users");

// Domains
const settingsManageDomainsBreadcrumb = settingsTab.find("#settings-manage-domains-breadcrumb");
const currentBusinessDomainName = settingsTab.find("#currentBusinessDomainName");
const switchBackToBusinessDomainsTab = settingsTab.find("#switchBackToBusinessDomainsTab");
const saveBusinessDomainButton = settingsTab.find("#saveBusinessDomainButton");
const saveBusinessDomainButtonSpinner = saveBusinessDomainButton.find(".save-button-spinner");

const businessDomainsListTab = settingsTab.find("#businessDomainsListTab");
const addNewBusinessDomainButton = businessDomainsListTab.find("#addNewBusinessDomainButton");
const businessDomainsTable = businessDomainsListTab.find("#businessDomainsTable");

const businessDomainsManagerTab = settingsTab.find("#businessDomainsManagerTab");

const businessDomainsDomainType = businessDomainsManagerTab.find("#business-domains-domain-type");

// Iqra Domain
const businessDomainsIqraSubdomainContainer = businessDomainsManagerTab.find("#business-domains-iqra-subdomain-container");
const businessDomainsIqraSubdomain = businessDomainsIqraSubdomainContainer.find("#business-domains-iqra-subdomain");

// Custom Domain
const businessDomainsSslConfig = businessDomainsManagerTab.find("#business-domains-ssl-config");

const businessDomainSSLTypeCustom = businessDomainsManagerTab.find("#business-domain-ssl-type-custom");

const businessDomainsCustomDomainContainer = businessDomainsManagerTab.find("#business-domains-custom-domain-container");
const businessDomainsCustomDomain = businessDomainsCustomDomainContainer.find("#business-domains-custom-domain");
const businessDomainsSslPrivateKey = businessDomainsManagerTab.find("#business-domains-ssl-private-key");
const businessDomainsSslCertificate = businessDomainsManagerTab.find("#business-domains-ssl-certificate");

// API Functions

function SaveSettingsChanges(changes, successCallback, errorCallback)
{
    $.ajax({
        type: "POST",
        url: "/app/user/business/" + CurrentBusinessId + "/settings/save",
        data: changes,
        dataType: "json",
        processData: false,
        contentType: false,
        success: (response) => {
            if (!response.success)
            {
                errorCallback(response, false);
                return;
            }

            successCallback(response);
        },
        error: (error) => {
            errorCallback(error, true);
        }
    });
}

function SaveSettingsBusinessSubuser(changes, successCallback, errorCallback)
{
    $.ajax({
        type: "POST",
        url: "/app/user/business/" + CurrentBusinessId + "/subuser/save",
        data: changes,
        dataType: "json",
        processData: false,
        contentType: false,
        success: (response) => {
            if (!response.success)
            {
                errorCallback(response, false);
                return;
            }

            successCallback(response);
        },
        error: (error) => {
            errorCallback(error, true);
        }
    });
}

function SaveSettingsBusinessDomain(changes, successCallback, errorCallback)
{
    $.ajax({
        type: "POST",
        url: "/app/user/business/" + CurrentBusinessId + "/domain/save",
        data: changes,
        dataType: "json",
        processData: false,
        contentType: false,
        success: (response) => {
            if (!response.success)
            {
                errorCallback(response, false);
                return;
            }

            successCallback(response);
        },
        error: (error) => {
            errorCallback(error, true);
        }
    });
}

// Functions
function ValidateSettingsGeneralTabFields(onlyRemove = true)
{
    let errors = [];
    let validated = true;

    let businessName = settingsGeneralBusinessName.val();
    if (!businessName || businessName.trim().length === 0 || businessName === '')
    {
        validated = false;
        errors.push("Business name is required and can not be empty.");
        
        if (!onlyRemove)
        {
            settingsGeneralBusinessName.addClass("is-invalid");
        }
    }
    else
    {
        settingsGeneralBusinessName.removeClass("is-invalid");
    }

    return {
        validated: validated,
        errors: errors
    };
}

function CreateSettingsAddedLanguagesElement(code, name) {
    let element = $(`
        <tr language-code="${code}">
            <td>${code}</td>
            <td>${name}</td>
            <td>
                <button language-code="${code}" class="btn btn-danger" button-type="settingsLanguageRemove" type="button">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `);

    return element;
}

function GetSettingsCurrentAddedLanguages()
{
    let currentAddedLanguages = [];

    let noneNoticeTr = settingsAddedLanguagesList.find("tr[tr-type=none-notice]");
    if (noneNoticeTr.length > 0)
    {
        return currentAddedLanguages;
    }

    settingsAddedLanguagesList.find("tbody tr").each((index, element) => {
        currentAddedLanguages.push($(element).attr("language-code"));
    });

    return currentAddedLanguages;
}

function CheckSettingsGeneralTabHasChanges()
{
    let changes = {};
    let hasChanges = false;

    if (BusinessFullData.businessData.name !== settingsGeneralBusinessName.val())
    {
        hasChanges = true;
        changes.name = settingsGeneralBusinessName.val();
    }

    if (settingsGeneralBusinessLogo[0].files.length > 0)
    {
        hasChanges = true;
        changes.logo = settingsGeneralBusinessLogo[0].files[0];
    }

    return {
        hasChanges: hasChanges,
        changes: changes
    };
}

function CheckSettingsLanguagesTabHasChanges()
{
    let currentAddedLanguages = GetSettingsCurrentAddedLanguages();
    let businessLanguages = BusinessFullData.businessData.languages;

    if (currentAddedLanguages.length !== businessLanguages.length)
    {
        return {
            hasChanges: true,
            changes: {
                languages:  currentAddedLanguages
            }
        };
    }

    let addedCount = 0;
    let removedCount = 0;
    let remainedCount = 0;

    for (let i = 0; i < businessLanguages.length; i++)
    {
        let oldLanguage = businessLanguages[i];

        if (currentAddedLanguages.includes(oldLanguage))
        {
            remainedCount++;
        }
        else
        {
            removedCount++;
        }
    }

    for (let i = 0; i < currentAddedLanguages.length; i++)
    {
        let newLanguage = currentAddedLanguages[i];

        if (!businessLanguages.includes(newLanguage))
        {
            addedCount++;
        }
    }

    if (addedCount > 0 || removedCount > 0)
    {
        return {
            hasChanges: true,
            changes: {
                languages:  currentAddedLanguages
            }
        };
    }

    return {
        hasChanges: false,
        changes: null
    };
}

function CheckIfSettingsHasChanges(enableDisableButton = true) {
    let generalTabHasChanges = CheckSettingsGeneralTabHasChanges();
    let languageTabHasChanges = CheckSettingsLanguagesTabHasChanges();

    let hasChanges = generalTabHasChanges.hasChanges || languageTabHasChanges.hasChanges;

    if (enableDisableButton)
    {
        if (hasChanges)
        {
            settingsSaveButton.prop("disabled", false);
        }
        else
        {
            settingsSaveButton.prop("disabled", true);
        }
    }
    
    let result = {
        hasChanges: hasChanges,
        changes: {
        }
    }

    if (generalTabHasChanges.hasChanges)
    {
        result.changes.general = generalTabHasChanges.changes;
    }

    if (languageTabHasChanges.hasChanges)
    {
        result.changes.languages = languageTabHasChanges.changes;
    }

    return result;
}

function CreateSettingsBusinessSubusersTableElement(userData)
{
    let element = $(`
        <tr user-id="${userData.email}">
            <td>${userData.email}</td>
            <td>
                <button user-email="${userData.email}" class="btn btn-primary" button-type="settingsUserEdit" type="button">
                        <i class="fa-regular fa-pen-to-square"></i>
                    </button>
                <button user-email="${userData.email}" class="btn btn-danger" button-type="settingsUserRemove" type="button">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `);

    return element;
}

function CreateSettingsBusinessDomainTableElement(domainData)
{
    let element = "";
    
    if (domainData.type.value == 1)
    {
        element = $(`
            <tr domain-id="${domainData.id}">
                <td>${domainData.subDomain}.${IqraBusinessDomain}</td>
                <td>
                    <button domain-id="${domainData.id}" class="btn btn-primary" button-type="settingsDomainEdit" type="button">
                        <i class="fa-regular fa-pen-to-square"></i>
                    </button>
                    <button domain-id="${domainData.id}" class="btn btn-danger" button-type="settingsDomainRemove" type="button">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>
        `);
    }

    if (domainData.type.value == 2)
    {
        element = $(`
            <tr domain-id="${domainData.id}">
                <td>${domainData.customDomain}</td>
                <td>
                    <button domain-id="${domainData.id}" class="btn btn-primary" button-type="settingsDomainEdit" type="button">
                        <i class="fa-regular fa-pen-to-square"></i>
                    </button>
                    <button domain-id="${domainData.id}" class="btn btn-danger" button-type="settingsDomainRemove" type="button">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </td>
            </tr>
        `);
    }

    return element;
}

function FillSettingsTab()
{
    function FillSettingsGeneralTab()
    {
        settingsGeneralBusinessName.val(BusinessFullData.businessData.name);

        if (settingsGeneralBusinessLogo[0].files.length > 0)
        {
            settingsGeneralBusinessLogo[0].files = (new DataTransfer()).files;
        }
        if (BusinessFullData.businessData.logoURL && BusinessFullData.businessData.logoURL != null)
        {
            settingsGeneralBusinessLogoPreview.attr("src", BusinessLogoURL + "/" + BusinessFullData.businessData.logoURL + ".webp");
        }
        else
        {
            settingsGeneralBusinessLogoPreview.attr("src", DefaultBusinessImgSRC);
        }
    }

    function FillSettingsLanguagesTab()
    {
        CountryCodeLanguagesList.forEach((value, index) => {
            settingsLanguageAddSelect.append(`<option value="${value.Code}">${value.Name} | ${value.Code}</option>`);
        });

        settingsAddedLanguagesList.find("tbody").empty();
        if (BusinessFullData.businessData.languages.length === 0)
        {
            settingsAddedLanguagesList.find("tbody").append('<tr tr-type="none-notice"><td colspan="3">No language added yet...</td></tr>');
        }
        else
        {
            BusinessFullData.businessData.languages.forEach((value, index) => {
                let countryCodeLanguage = CountryCodeLanguagesList.find((data, index) => {
                    return data.Code === value;
                });
    
                let element = CreateSettingsAddedLanguagesElement(countryCodeLanguage.Code, countryCodeLanguage.Name);
                settingsAddedLanguagesList.append(element);

                settingsLanguageAddSelect.find(`option[value="${value}"]`).remove();
            })
        }
    }

    function FillSettingsUsersTab()
    {
        businessSubusersTable.find("tbody").empty();
        if (BusinessFullData.businessData.subUsers.length === 0)
        {
            businessSubusersTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="2">No subuser added yet...</td></tr>');
        }
        else
        {
            BusinessFullData.businessData.subUsers.forEach((userData, index) => {
                businessSubusersTable.find("tbody").append(CreateSettingsBusinessSubusersTableElement(userData));
            })
        }
    }

    function FillSettingsDomainsTab()
    {
        businessDomainsTable.find("tbody").empty();
        if (BusinessFullData.businessWhiteLabelDomain.length === 0)
        {
            businessDomainsTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="2">No domains added yet...</td></tr>');
        }
        else
        {
            BusinessFullData.businessWhiteLabelDomain.forEach((value, index) => {
                businessDomainsTable.find("tbody").append(CreateSettingsBusinessDomainTableElement(value));
            })
        }
   
        businessDomainsIqraSubdomain.parent().find("span.input-group-text").text("." + IqraBusinessDomain);
    }

    FillSettingsGeneralTab();
    FillSettingsLanguagesTab();
    FillSettingsUsersTab();
    FillSettingsDomainsTab();
}

function ShowSettingsUsersManageTab()
{
    settingsInnerTabContainer.removeClass("show");
    businessSubusersListTab.removeClass("show");

    setTimeout(() => {
        settingsInnerTabContainer.addClass("d-none");
        businessSubusersListTab.addClass("d-none");

        settingsManageSubusersBreadcrumb.removeClass("d-none");
        subusersManagerTab.removeClass("d-none");
        setTimeout(() => {
            settingsManageSubusersBreadcrumb.addClass("show");
            subusersManagerTab.addClass("show");
        }, 10);
    }, 300);
}

function ShowSettingsUsersListTab()
{
    settingsManageSubusersBreadcrumb.removeClass("show");
    subusersManagerTab.removeClass("show");
    setTimeout(() => {
        settingsManageSubusersBreadcrumb.addClass("d-none");
        subusersManagerTab.addClass("d-none");

        settingsInnerTabContainer.removeClass("d-none");
        businessSubusersListTab.removeClass("d-none");
        setTimeout(() => {
            settingsInnerTabContainer.addClass("show");
            businessSubusersListTab.addClass("show");
        }, 10);
    }, 300);
}

function ResetSettingsUsersManageTab(isEdit = false)
{
    subusersManagerTab.find("input, textarea").val("");
    subusersManagerTab.find(".is-invalid").removeClass("is-invalid");
    subusersManagerTab.find("input[type=checkbox]").prop("checked", false).change();
    subusersManagerTab.find("table tbody").empty();

    if (businessSubuserWhiteLabelLogo[0].files.length > 0)
    {
        Array.prototype.slice.call(businessSubuserWhiteLabelLogo[0].files, 1);
    }

    if (businessSubuserWhiteLabelFavicon[0].files.length > 0)
    {
        Array.prototype.slice.call(businessSubuserWhiteLabelFavicon[0].files, 1);
    }

    businessSubuserWhiteLabelLogoPreview.attr("src", DefaultWhiteLabelLogoSRC);
    businessSubuserWhiteLabelFaviconPreview.attr("src", DefaultWhiteLabelFaviconSRC);

    businessSubuserWhiteLabelDomainIdentifier.empty();
    businessSubuserWhiteLabelDomainIdentifier.append("<option value='-1' disabled selected>Select domain</option>");
    BusinessFullData.businessWhiteLabelDomain.forEach((data, index) => {
        if (data.type.value === 1)
        {
            businessSubuserWhiteLabelDomainIdentifier.append(`<option value="${data.id}">${data.subDomain}.${IqraBusinessDomain}</option>`);
        }

        if (data.type.value === 2)
        {
            businessSubuserWhiteLabelDomainIdentifier.append(`<option value="${data.id}">${data.customDomain}</option>`);
        }
    });

    businessSubuserManagerGeneralTab.click();
    businessSubuserPermissionsRoutingTab.click();
    subusersWhitelabelGeneralTab.click();

    businessSubuserEmail.prop("disabled", isEdit);
}

function FillSettingsUsersManageTab(usersData)
{
    // General
    businessSubuserEmail.val(usersData.email);
    businessSubuserPassword.val(usersData.password);

    SetPermissionInput(businessSubuserLoginDisabledInput, businessSubuserLoginDisabledReasonInput, usersData.disabledUserLoginAt, usersData.disabledUserLoginReason);

    // Permissions
    FillSettingsUsersManageTabPermissionsTab(usersData.permission);

    // White Label
    businessSubuserWhiteLabelPlatformName.val(usersData.whiteLabel.platformName);
    businessSubuserWhiteLabelPlatformTitle.val(usersData.whiteLabel.platformTitle);
    businessSubuserWhiteLabelPlatformDescription.val(usersData.whiteLabel.platformDescription);

    businessSubuserWhiteLabelLogoPreview.val(BusinessLogoURL + "/" + usersData.whiteLabel.logoURL);
    businessSubuserWhiteLabelFaviconPreview.val(BusinessLogoURL + "/" + usersData.whiteLabel.faviconURL);

    businessSubuserWhiteLabelCustomCss.val(usersData.whiteLabel.customCSS);
    businessSubuserWhiteLabelCustomJs.val(usersData.whiteLabel.customJavaScript);

    businessSubuserWhiteLabelDomainIdentifier.val(usersData.whiteLabel.domainId).change();
}

function FillSettingsUsersManageTabPermissionsTab(usersDataPermissions)
{
    function FillRoutings() {
        let routingPermissions = usersDataPermissions.routing;

        if (routingPermissions.tabEnabled) {
            businessSubuserRoutingTabEnabled.prop("checked", true).change();

            businessSubuserRoutingAddNewRoute.prop("checked", routingPermissions.add).change();
            businessSubuserRoutingEditRoute.prop("checked", routingPermissions.edit).change();
            businessSubuserRoutingDeleteRoute.prop("checked", routingPermissions.delete).change();
        }
    }
    FillRoutings();

    function FillTools() {
        let toolsPermissions = usersDataPermissions.tools;

        if (toolsPermissions.tabEnabled) {
            businessSubuserToolsTabEnabled.prop("checked", true).change();

            businessSubuserToolsAddNewTool.prop("checked", toolsPermissions.add).change();
            businessSubuserToolsEditTool.prop("checked", toolsPermissions.edit).change();
            businessSubuserToolsDeleteTool.prop("checked", toolsPermissions.delete).change();
        }
    }
    FillTools();

    function FillUsers() {
        let agentsPermissions = usersDataPermissions.agents;

        if (agentsPermissions.tabEnabled) {
            businessSubuserAgentsTabEnabled.prop("checked", true).change();

            businessSubuserAgentsAddNewAgent.prop("checked", agentsPermissions.add).change();
            businessSubuserAgentsEditAgent.prop("checked", agentsPermissions.edit).change();
            businessSubuserAgentsDeleteAgent.prop("checked", agentsPermissions.delete).change();
        }
    }
    FillUsers();

    function FillContext() {
        let contextPermissions = usersDataPermissions.context;

        if (contextPermissions.tabEnabled) {
            businessSubuserContextTabEnabled.prop("checked", true).change();

            // Branding
            if (contextPermissions.branding.tabEnabled) {
                businessSubuserContextBrandingTabEnabled.prop("checked", true).change();
                businessSubuserContextEditBranding.prop("checked", contextPermissions.branding.edit).change();
            }

            // Branches
            if (contextPermissions.branches.tabEnabled) {
                businessSubuserContextBranchesTabEnabled.prop("checked", true).change();
                businessSubuserContextAddNewBranch.prop("checked", contextPermissions.branches.add).change();
                businessSubuserContextEditBranch.prop("checked", contextPermissions.branches.edit).change();
                businessSubuserContextDeleteBranch.prop("checked", contextPermissions.branches.delete).change();
            }

            // Services
            if (contextPermissions.services.tabEnabled) {
                businessSubuserContextServicesTabEnabled.prop("checked", true).change();
                businessSubuserContextAddNewService.prop("checked", contextPermissions.services.add).change();
                businessSubuserContextEditService.prop("checked", contextPermissions.services.edit).change();
                businessSubuserContextDeleteService.prop("checked", contextPermissions.services.delete).change();
            }

            // Products
            if (contextPermissions.products.tabEnabled) {
                businessSubuserContextProductsTabEnabled.prop("checked", true).change();
                businessSubuserContextAddNewProduct.prop("checked", contextPermissions.products.add).change();
                businessSubuserContextEditProduct.prop("checked", contextPermissions.products.edit).change();
                businessSubuserContextDeleteProduct.prop("checked", contextPermissions.products.delete).change();
            }
        }
    }
    FillContext();

    function FillMakeCalls()
    {
        let makeCallsPermissions = usersDataPermissions.makeCalls;

        if (makeCallsPermissions.tabEnabled)
        {
            businessSubuserMakeCallsTabEnabled.prop("checked", true).change();

            businessSubuserMakeCallsSingleCallEnabled.prop("checked", makeCallsPermissions.singleCallEnabled).change();
            businessSubuserMakeCallsBulkCallEnabled.prop("checked", makeCallsPermissions.bulkCallEnabled).change();
        }
    }
    FillMakeCalls();

    function FillConversations() {
        let conversationsPermissions = usersDataPermissions.conversations;

        if (conversationsPermissions.tabEnabled) {
            businessSubuserConversationsTabEnabled.prop("checked", true).change();

            // Inbound
            if (conversationsPermissions.inbound.tabEnabled) {
                businessSubuserConversationsInboundCallTabEnabled.prop("checked", true).change();
                businessSubuserConversationsDeleteInboundCall.prop("checked", conversationsPermissions.inbound.delete).change();
                businessSubuserConversationsExportInboundCall.prop("checked", conversationsPermissions.inbound.export).change();
            }

            // Outbound
            if (conversationsPermissions.outbound.tabEnabled) {
                businessSubuserConversationsOutboundCallTabEnabled.prop("checked", true).change();
                businessSubuserConversationsDeleteOutboundCall.prop("checked", conversationsPermissions.outbound.delete).change();
                businessSubuserConversationsExportOutboundCall.prop("checked", conversationsPermissions.outbound.export).change();
            }

            // Websocket
            if (conversationsPermissions.websocket.tabEnabled) {
                businessSubuserConversationsWebsocketTabEnabled.prop("checked", true).change();
                businessSubuserConversationsDeleteWebsocket.prop("checked", conversationsPermissions.websocket.delete).change();
                businessSubuserConversationsExportWebsocket.prop("checked", conversationsPermissions.websocket.export).change();
            }
        }
    }
    FillConversations();

    function FillSettings() {
        let settingsPermissions = usersDataPermissions.settings;

        if (settingsPermissions.tabEnabled) {
            businessSubuserSettingsTabEnabled.prop("checked", true).change();

            // General
            if (settingsPermissions.general.tabEnabled) {
                businessSubuserSettingsGeneralTabEnabled.prop("checked", true).change();
                businessSubuserSettingsEditGeneral.prop("checked", settingsPermissions.general.edit).change();
            }

            // Languages
            if (settingsPermissions.languages.tabEnabled) {
                businessSubuserSettingsLanguagesTabEnabled.prop("checked", true).change();
                businessSubuserSettingsEditLanguages.prop("checked", settingsPermissions.languages.edit).change();
                businessSubuserSettingsAddLanguages.prop("checked", settingsPermissions.languages.add).change();
                businessSubuserSettingsDeleteLanguages.prop("checked", settingsPermissions.languages.delete).change();
            }

            // Users
            if (settingsPermissions.users.tabEnabled) {
                businessSubuserSettingsUsersTabEnabled.prop("checked", true).change();
                businessSubuserSettingsEditUsers.prop("checked", settingsPermissions.users.edit).change();
                businessSubuserSettingsAddUsers.prop("checked", settingsPermissions.users.add).change();
                businessSubuserSettingsDeleteUsers.prop("checked", settingsPermissions.users.delete).change();
            }
        }
    }
    FillSettings();
}

function ValidateSettingsSubusersGeneralFields(onlyRemove = true)
{
    let errors = [];
    let validated = true;

    if (businessSubuserEmail.val().trim() === "")
    {
        validated = false;
        errors.push("Email is required and can not be empty.");

        if (!onlyRemove)
        {
            businessSubuserEmail.addClass("is-invalid");
        }
    }
    else
    {
        businessSubuserEmail.removeClass("is-invalid");
    }

    let password = businessSubuserPassword.val().trim();
    if (password === "")
    {
        validated = false;
        errors.push("Password is required and can not be empty.");
        
        if (!onlyRemove)
        {
            businessSubuserPassword.addClass("is-invalid");
        }
    }
    else
    {
        businessSubuserPassword.removeClass("is-invalid");

        if (password.length < 8)
        {
            validated = false;
            errors.push("Password should be at least 8 characters long.");

            if (!onlyRemove)
            {
                businessSubuserPassword.addClass("is-invalid");
            }
        }
        else
        {
            businessSubuserPassword.removeClass("is-invalid");
        }
    }

    return {
        validated: validated,
        errors: errors
    };
}

function ValidateSettingsSubusersWhiteLabelGeneralFields(onlyRemove = true)
{
    let errors = [];
    let validated = true;

    if (businessSubuserWhiteLabelPlatformName.val().trim() === "")
    {
        validated = false;
        errors.push("Platform name is required and can not be empty.");

        if (!onlyRemove)
        {
            businessSubuserWhiteLabelPlatformName.addClass("is-invalid");
        }
    }
    else
    {
        businessSubuserWhiteLabelPlatformName.removeClass("is-invalid");
    }

    if (businessSubuserWhiteLabelPlatformTitle.val().trim() === "")
    {
        validated = false;
        errors.push("Platform title is required and can not be empty.");

        if (!onlyRemove)
        {
            businessSubuserWhiteLabelPlatformTitle.addClass("is-invalid");
        }
    }
    else
    {
        businessSubuserWhiteLabelPlatformTitle.removeClass("is-invalid");
    }

    if (businessSubuserWhiteLabelPlatformDescription.val().trim() === "")
    {
        validated = false;
        errors.push("Platform description is required and can not be empty.");

        if (!onlyRemove)
        {
            businessSubuserWhiteLabelPlatformDescription.addClass("is-invalid");
        }
    }
    else
    {
        businessSubuserWhiteLabelPlatformDescription.removeClass("is-invalid");
    }

    if (businessSubuserWhiteLabelDomainIdentifier.val() === "-1" || businessSubuserWhiteLabelDomainIdentifier.val() === null)
    {
        validated = false;
        errors.push("Domain is required and can not be empty.");

        if (!onlyRemove)
        {
            businessSubuserWhiteLabelDomainIdentifier.addClass("is-invalid");
        }
    }
    else
    {
        businessSubuserWhiteLabelDomainIdentifier.removeClass("is-invalid");
    }

    return {
        validated: validated,
        errors: errors
    };
}

function CheckSettingsSubusersGeneralTabHasChanges()
{
    let changes = {};
    let hasChanges = false;

    changes.email = businessSubuserEmail.val();
    if (businessSubuserEmail.val() !== CurrentManageSubUserData.email)
    {
        hasChanges = true;
    }

    changes.password = businessSubuserPassword.val();
    if (businessSubuserPassword.val() !== CurrentManageSubUserData.password)
    {
        hasChanges = true;
    }

    let isLoginDisableChecked = businessSubuserLoginDisabledInput.prop("checked");
    changes.isLoginDisabled = isLoginDisableChecked;
    if (
        (isLoginDisableChecked == true && CurrentManageSubUserData.disabledUserLoginAt == null)
        ||
        (isLoginDisableChecked == false && CurrentManageSubUserData.disabledUserLoginAt != null)
    )
    {
        hasChanges = true;
    }

    changes.loginDisabledReason = businessSubuserLoginDisabledReasonInput.val();
    if (
        isLoginDisableChecked == true
        &&
        businessSubuserLoginDisabledReasonInput.val() !== CurrentManageSubUserData.disabledUserLoginReason
    )
    {
        hasChanges = true;
    }

    return {
        hasChanges: hasChanges,
        changes: changes
    };
}

function CheckSettingsSubusersPermissionsTabHasChanges()
{
    let changes = {
        routing: {

        },
        tools: {

        },
        agents: {

        },
        context: {
            branding: {

            },
            branches: {

            },
            services: {

            },
            products: {

            }
        },
        makeCalls: {

        },
        conversations: {
            inbound: {

            },
            outbound: {

            },
            websocket: {

            }
        },
        settings: {
            general: {

            },
            languages: {

            },
            users: {

            },
        }
    };
    let hasChanges = false;

    // Routings
    function checkRoutings()
    {
        changes.routing.tabEnabled = businessSubuserRoutingTabEnabled.prop("checked");
        if (businessSubuserRoutingTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.routing.tabEnabled)
        {
            hasChanges = true;
        }

        changes.routing.add = businessSubuserRoutingAddNewRoute.prop("checked");
        if (businessSubuserRoutingAddNewRoute.prop("checked") !== CurrentManageSubUserData.permission.routing.add)
        {
            hasChanges = true;
        }

        changes.routing.edit = businessSubuserRoutingEditRoute.prop("checked");
        if (businessSubuserRoutingEditRoute.prop("checked") !== CurrentManageSubUserData.permission.routing.edit)
        {
            hasChanges = true;
        }

        changes.routing.delete = businessSubuserRoutingDeleteRoute.prop("checked");
        if (businessSubuserRoutingDeleteRoute.prop("checked") !== CurrentManageSubUserData.permission.routing.delete)
        {
            hasChanges = true;
        }
    }
    checkRoutings();

    // Tools
    function checkTools()
    {
        changes.tools.tabEnabled = businessSubuserToolsTabEnabled.prop("checked");
        if (businessSubuserToolsTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.tools.tabEnabled)
        {
            hasChanges = true;
        }

        changes.tools.add = businessSubuserToolsAddNewTool.prop("checked");
        if (businessSubuserToolsAddNewTool.prop("checked") !== CurrentManageSubUserData.permission.tools.add)
        {
            hasChanges = true;
        }

        changes.tools.edit = businessSubuserToolsEditTool.prop("checked");
        if (businessSubuserToolsEditTool.prop("checked") !== CurrentManageSubUserData.permission.tools.edit)
        {
            hasChanges = true;
        }

        changes.tools.delete = businessSubuserToolsDeleteTool.prop("checked");
        if (businessSubuserToolsDeleteTool.prop("checked") !== CurrentManageSubUserData.permission.tools.delete)
        {
            hasChanges = true;
        }
    }
    checkTools();

    // Agents
    function checkAgents()
    {
        changes.agents.tabEnabled = businessSubuserAgentsTabEnabled.prop("checked");
        if (businessSubuserAgentsTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.agents.tabEnabled)
        {
            hasChanges = true;
        }

        changes.agents.add = businessSubuserAgentsAddNewAgent.prop("checked");
        if (businessSubuserAgentsAddNewAgent.prop("checked") !== CurrentManageSubUserData.permission.agents.add)
        {
            hasChanges = true;
        }

        changes.agents.edit = businessSubuserAgentsEditAgent.prop("checked");
        if (businessSubuserAgentsEditAgent.prop("checked") !== CurrentManageSubUserData.permission.agents.edit)
        {
            hasChanges = true;
        }

        changes.agents.delete = businessSubuserAgentsDeleteAgent.prop("checked");
        if (businessSubuserAgentsDeleteAgent.prop("checked") !== CurrentManageSubUserData.permission.agents.delete)
        {
            hasChanges = true;
        }
    }
    checkAgents();

    // Context
    function checkContext()
    {
        changes.context.tabEnabled = businessSubuserContextTabEnabled.prop("checked");
        if (businessSubuserContextTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.context.tabEnabled)
        {
            hasChanges = true;
        }

        // branding
        changes.context.branding.tabEnabled = businessSubuserContextBrandingTabEnabled.prop("checked");
        if (businessSubuserContextBrandingTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.context.branding.tabEnabled)
        {
            hasChanges = true;
        }
        
        changes.context.branding.edit = businessSubuserContextEditBranding.prop("checked");
        if (businessSubuserContextEditBranding.prop("checked") !== CurrentManageSubUserData.permission.context.branding.edit)
        {
            hasChanges = true;
        }

        // branches
        changes.context.branches.tabEnabled = businessSubuserContextBranchesTabEnabled.prop("checked");
        if (businessSubuserContextBranchesTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.context.branches.tabEnabled)
        {
            hasChanges = true;
        }

        changes.context.branches.add = businessSubuserContextAddNewBranch.prop("checked");
        if (businessSubuserContextAddNewBranch.prop("checked") !== CurrentManageSubUserData.permission.context.branches.add)
        {
            hasChanges = true;
        }

        changes.context.branches.edit = businessSubuserContextEditBranch.prop("checked");
        if (businessSubuserContextEditBranch.prop("checked") !== CurrentManageSubUserData.permission.context.branches.edit)
        {
            hasChanges = true;
        }

        changes.context.branches.delete = businessSubuserContextDeleteBranch.prop("checked");
        if (businessSubuserContextDeleteBranch.prop("checked") !== CurrentManageSubUserData.permission.context.branches.delete)
        {
            hasChanges = true;
        }

        // services
        changes.context.services.tabEnabled = businessSubuserContextServicesTabEnabled.prop("checked");
        if (businessSubuserContextServicesTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.context.services.tabEnabled)
        {
            hasChanges = true;
        }

        changes.context.services.add = businessSubuserContextAddNewService.prop("checked");
        if (businessSubuserContextAddNewService.prop("checked") !== CurrentManageSubUserData.permission.context.services.add)
        {
            hasChanges = true;
        }

        changes.context.services.edit = businessSubuserContextEditService.prop("checked");
        if (businessSubuserContextEditService.prop("checked") !== CurrentManageSubUserData.permission.context.services.edit)
        {
            hasChanges = true;
        }

        changes.context.services.delete = businessSubuserContextDeleteService.prop("checked");
        if (businessSubuserContextDeleteService.prop("checked") !== CurrentManageSubUserData.permission.context.services.delete)
        {
            hasChanges = true;
        }

        // products
        changes.context.products.tabEnabled = businessSubuserContextProductsTabEnabled.prop("checked");
        if (businessSubuserContextProductsTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.context.products.tabEnabled)
        {
            hasChanges = true;
        }

        changes.context.products.add = businessSubuserContextAddNewProduct.prop("checked");
        if (businessSubuserContextAddNewProduct.prop("checked") !== CurrentManageSubUserData.permission.context.products.add)
        {
            hasChanges = true;
        }

        changes.context.products.edit = businessSubuserContextEditProduct.prop("checked");
        if (businessSubuserContextEditProduct.prop("checked") !== CurrentManageSubUserData.permission.context.products.edit)
        {
            hasChanges = true;
        }

        changes.context.products.delete = businessSubuserContextDeleteProduct.prop("checked");
        if (businessSubuserContextDeleteProduct.prop("checked") !== CurrentManageSubUserData.permission.context.products.delete)
        {
            hasChanges = true;
        }
    }
    checkContext();

    // Make Calls
    function checkMakeCalls(){
        changes.makeCalls.tabEnabled = businessSubuserMakeCallsTabEnabled.prop("checked");
        if (businessSubuserMakeCallsTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.makeCalls.tabEnabled)
        {
            hasChanges = true;
        }

        changes.makeCalls.singleCallEnabled = businessSubuserMakeCallsSingleCallEnabled.prop("checked");
        if (businessSubuserMakeCallsSingleCallEnabled.prop("checked") !== CurrentManageSubUserData.permission.makeCalls.singleCallEnabled)
        {
            hasChanges = true;
        }

        changes.makeCalls.bulkCallEnabled = businessSubuserMakeCallsBulkCallEnabled.prop("checked");
        if (businessSubuserMakeCallsBulkCallEnabled.prop("checked") !== CurrentManageSubUserData.permission.makeCalls.bulkCallEnabled)
        {
            hasChanges = true;
        }
    }
    checkMakeCalls();

    // Conversations
    function checkConversations()
    {
        changes.conversations.tabEnabled = businessSubuserConversationsTabEnabled.prop("checked");
        if (businessSubuserConversationsTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.conversations.tabEnabled)
        {
            hasChanges = true;
        }

        // inbound
        changes.conversations.inbound.tabEnabled = businessSubuserConversationsInboundCallTabEnabled.prop("checked");
        if (businessSubuserConversationsInboundCallTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.conversations.inbound.tabEnabled)
        {
            hasChanges = true;
        }

        changes.conversations.inbound.delete = businessSubuserConversationsDeleteInboundCall.prop("checked");
        if (businessSubuserConversationsDeleteInboundCall.prop("checked") !== CurrentManageSubUserData.permission.conversations.inbound.delete)
        {
            hasChanges = true;
        }

        changes.conversations.inbound.export = businessSubuserConversationsExportInboundCall.prop("checked");
        if (businessSubuserConversationsExportInboundCall.prop("checked") !== CurrentManageSubUserData.permission.conversations.inbound.export)
        {
            hasChanges = true;
        }

        // outbound
        changes.conversations.outbound.tabEnabled = businessSubuserConversationsOutboundCallTabEnabled.prop("checked");
        if (businessSubuserConversationsOutboundCallTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.conversations.outbound.tabEnabled)
        {
            hasChanges = true;
        }

        changes.conversations.outbound.delete = businessSubuserConversationsDeleteOutboundCall.prop("checked");
        if (businessSubuserConversationsDeleteOutboundCall.prop("checked") !== CurrentManageSubUserData.permission.conversations.outbound.delete)
        {
            hasChanges = true;
        }

        changes.conversations.outbound.export = businessSubuserConversationsExportOutboundCall.prop("checked");
        if (businessSubuserConversationsExportOutboundCall.prop("checked") !== CurrentManageSubUserData.permission.conversations.outbound.export)
        {
            hasChanges = true;
        }

        // websocket
        changes.conversations.websocket.tabEnabled = businessSubuserConversationsWebsocketTabEnabled.prop("checked");
        if (businessSubuserConversationsWebsocketTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.conversations.websocket.tabEnabled)
        {
            hasChanges = true;
        }

        changes.conversations.websocket.delete = businessSubuserConversationsDeleteWebsocket.prop("checked");
        if (businessSubuserConversationsDeleteWebsocket.prop("checked") !== CurrentManageSubUserData.permission.conversations.websocket.delete)
        {
            hasChanges = true;
        }

        changes.conversations.websocket.export = businessSubuserConversationsExportWebsocket.prop("checked");
        if (businessSubuserConversationsExportWebsocket.prop("checked") !== CurrentManageSubUserData.permission.conversations.websocket.export)
        {
            hasChanges = true;
        }
    }
    checkConversations();

    // Settings
    function checkSettings()
    {
        changes.settings.tabEnabled = businessSubuserSettingsTabEnabled.prop("checked");
        if (businessSubuserSettingsTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.settings.tabEnabled)
        {
            hasChanges = true;
        }

        // general
        changes.settings.general.tabEnabled = businessSubuserSettingsGeneralTabEnabled.prop("checked");
        if (businessSubuserSettingsGeneralTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.settings.general.tabEnabled)
        {
            hasChanges = true;
        }

        changes.settings.general.edit = businessSubuserSettingsEditGeneral.prop("checked");
        if (businessSubuserSettingsEditGeneral.prop("checked") !== CurrentManageSubUserData.permission.settings.general.edit)
        {
            hasChanges = true;
        }

        // languages
        changes.settings.languages.tabEnabled = businessSubuserSettingsLanguagesTabEnabled.prop("checked");
        if (businessSubuserSettingsLanguagesTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.settings.languages.tabEnabled)
        {
            hasChanges = true;
        }

        changes.settings.languages.edit = businessSubuserSettingsEditLanguages.prop("checked");
        if (businessSubuserSettingsEditLanguages.prop("checked") !== CurrentManageSubUserData.permission.settings.languages.edit)
        {
            hasChanges = true;
        }

        changes.settings.languages.add = businessSubuserSettingsAddLanguages.prop("checked");
        if (businessSubuserSettingsAddLanguages.prop("checked") !== CurrentManageSubUserData.permission.settings.languages.add)
        {
            hasChanges = true;
        }

        changes.settings.languages.delete = businessSubuserSettingsDeleteLanguages.prop("checked");
        if (businessSubuserSettingsDeleteLanguages.prop("checked") !== CurrentManageSubUserData.permission.settings.languages.delete)
        {
            hasChanges = true;
        }

        // users
        changes.settings.users.tabEnabled = businessSubuserSettingsUsersTabEnabled.prop("checked");
        if (businessSubuserSettingsUsersTabEnabled.prop("checked") !== CurrentManageSubUserData.permission.settings.users.tabEnabled)
        {
            hasChanges = true;
        }

        changes.settings.users.edit = businessSubuserSettingsEditUsers.prop("checked");
        if (businessSubuserSettingsEditUsers.prop("checked") !== CurrentManageSubUserData.permission.settings.users.edit)
        {
            hasChanges = true;
        }

        changes.settings.users.add = businessSubuserSettingsAddUsers.prop("checked");
        if (businessSubuserSettingsAddUsers.prop("checked") !== CurrentManageSubUserData.permission.settings.users.add)
        {
            hasChanges = true;
        }

        changes.settings.users.delete = businessSubuserSettingsDeleteUsers.prop("checked");
        if (businessSubuserSettingsDeleteUsers.prop("checked") !== CurrentManageSubUserData.permission.settings.users.delete)
        {
            hasChanges = true;
        }
    }
    checkSettings();

    return {
        hasChanges: hasChanges,
        changes: changes
    };
}

function CheckSettingsSubusersWhiteLabelHasChanges()
{
    let changes = {
        general: {

        },
        files: {

        },
        styles: {

        }
    };
    let hasChanges = false;

    // General

    changes.general.platformName = businessSubuserWhiteLabelPlatformName.val();
    if (businessSubuserWhiteLabelPlatformName.val() !== CurrentManageSubUserData.whiteLabel.platformName)
    {
        hasChanges = true;
    }

    changes.general.platformTitle = businessSubuserWhiteLabelPlatformTitle.val();
    if (businessSubuserWhiteLabelPlatformTitle.val() !== CurrentManageSubUserData.whiteLabel.platformTitle)
    {
        hasChanges = true;
    }

    changes.general.platformDescription = businessSubuserWhiteLabelPlatformDescription.val();
    if (businessSubuserWhiteLabelPlatformDescription.val() !== CurrentManageSubUserData.whiteLabel.platformDescription)
    {
        hasChanges = true;
    }

    changes.general.domainId = businessSubuserWhiteLabelDomainIdentifier.val();
    if (businessSubuserWhiteLabelDomainIdentifier.val() !== CurrentManageSubUserData.whiteLabel.domainId)
    {
        hasChanges = true;
    }

    // Styles
    if (businessSubuserWhiteLabelLogo[0].files.length > 0)
    {
        changes.files.logo = businessSubuserWhiteLabelLogo[0].files[0];
        hasChanges = true;
    }

    if (businessSubuserWhiteLabelFavicon[0].files.length > 0)
    {
        changes.files.favicon = businessSubuserWhiteLabelFavicon[0].files[0];
        hasChanges = true;
    }

    changes.styles.customCSS = businessSubuserWhiteLabelCustomCss.val();
    if (businessSubuserWhiteLabelCustomCss.val() !== CurrentManageSubUserData.whiteLabel.customCSS)
    {
        hasChanges = true;
    }

    changes.styles.customJavaScript = businessSubuserWhiteLabelCustomJs.val();
    if (businessSubuserWhiteLabelCustomJs.val() !== CurrentManageSubUserData.whiteLabel.customJavaScript)
    {
        hasChanges = true;
    }

    return {
        hasChanges: hasChanges,
        changes: changes
    };
}

function CheckIfSettingsSubusersManageHasChanges(enableDisableButton = true)
{
    let subusersGeneralTabChanges = CheckSettingsSubusersGeneralTabHasChanges();
    let subusersPermissionTabChanges = CheckSettingsSubusersPermissionsTabHasChanges();
    let subusersWhiteLabelTabChanges = CheckSettingsSubusersWhiteLabelHasChanges();

    let hasChanges = subusersGeneralTabChanges.hasChanges || subusersPermissionTabChanges.hasChanges || subusersWhiteLabelTabChanges.hasChanges;

    if (enableDisableButton)
    {
        if (hasChanges)
        {
            saveBusinessSubuserButton.prop("disabled", false);
        }
        else
        {
            saveBusinessSubuserButton.prop("disabled", true);
        }
    }

    return {
        hasChanges: hasChanges,
        changes: {
            general: subusersGeneralTabChanges.changes,
            permissions: subusersPermissionTabChanges.changes,
            whiteLabel: subusersWhiteLabelTabChanges.changes
        }
    };
}

function CreateSettingsDefaultSubuserObject()
{
    var defaultUserObject = {
        email: "",
        password: "",
        disabledUserLoginAt: null,
        disabledUserLoginReason: null,
        permission: {
            routing: {
                tabEnabled: false,
                add: false,
                edit: false,
                delete: false
            },
            tools: {
                tabEnabled: false,
                add: false,
                edit: false,
                delete: false
            },
            agents: {
                tabEnabled: false,
                add: false,
                edit: false,
                delete: false
            },
            context: {
                tabEnabled: false,
                branding: {
                    tabEnabled: false,
                    edit: false
                },
                branches: {
                    tabEnabled: false,
                    add: false,
                    edit: false,
                    delete: false
                },
                services: {
                    tabEnabled: false,
                    add: false,
                    edit: false,
                    delete: false
                },
                products: {
                    tabEnabled: false,
                    add: false,
                    edit: false,
                    delete: false
                }
            },
            makeCalls: {
                tabEnabled: false,
                singleCallEnabled: false,
                bulkCallEnabled: false
            },
            conversations: {
                tabEnabled: false,
                inbound: {
                    tabEnabled: false,
                    delete: false,
                    export: false
                },
                outbound: {
                    tabEnabled: false,
                    delete: false,
                    export: false
                },
                websocket: {
                    tabEnabled: false,
                    delete: false,
                    export: false
                }
            },
            settings: {
                tabEnabled: false,
                general: {
                    tabEnabled: false,
                    edit: false
                },
                languages: {
                    tabEnabled: false,
                    edit: false,
                    delete: false,
                    add: false
                },
                users: {
                    tabEnabled: false,
                    edit: false,
                    delete: false,
                    add: false
                }
            }
        },
        whiteLabel: {
            platformName: "",
            platformTitle: "",
            platformDescription: "",
            domainId: -1,
            logo: null,
            faviconIcon: null,
            customCSS: "",
            customJavaScript: ""
        }
    }

    return {
        user: defaultUserObject
    };
}

function EventListenersSettingsSubusersPermissionsEnableHelper()
{
    // Routings
    EnableFullPermissionHelper(
        businessSubuserRoutingTabEnabled,
        [businessSubuserRoutingAddNewRoute, businessSubuserRoutingEditRoute, businessSubuserRoutingDeleteRoute]
    );

    // Tools
    EnableFullPermissionHelper(
        businessSubuserToolsTabEnabled,
        [businessSubuserToolsAddNewTool, businessSubuserToolsEditTool, businessSubuserToolsDeleteTool]
    );

    // Agents
    EnableFullPermissionHelper(
        businessSubuserAgentsTabEnabled,
        [businessSubuserAgentsAddNewAgent, businessSubuserAgentsEditAgent, businessSubuserAgentsDeleteAgent]
    );

    // Context
    EnableFullPermissionHelper(
        businessSubuserContextTabEnabled,
        [businessSubuserContextBrandingTabEnabled, businessSubuserContextBranchesTabEnabled, businessSubuserContextServicesTabEnabled, businessSubuserContextProductsTabEnabled]
    );

    EnableFullPermissionHelper(
        businessSubuserContextBrandingTabEnabled,
        [businessSubuserContextEditBranding]
    );

    EnableFullPermissionHelper(
        businessSubuserContextBranchesTabEnabled,
        [businessSubuserContextAddNewBranch, businessSubuserContextEditBranch, businessSubuserContextDeleteBranch]
    );

    EnableFullPermissionHelper(
        businessSubuserContextServicesTabEnabled,
        [businessSubuserContextAddNewService, businessSubuserContextEditService, businessSubuserContextDeleteService]
    );

    EnableFullPermissionHelper(
        businessSubuserContextProductsTabEnabled,
        [businessSubuserContextAddNewProduct, businessSubuserContextEditProduct, businessSubuserContextDeleteProduct]
    );

    // Make Calls
    EnableFullPermissionHelper(
        businessSubuserMakeCallsTabEnabled,
        [businessSubuserMakeCallsSingleCallEnabled, businessSubuserMakeCallsBulkCallEnabled]
    );

    // Conversations
    EnableFullPermissionHelper(
        businessSubuserConversationsTabEnabled,
        [businessSubuserConversationsInboundCallTabEnabled, businessSubuserConversationsOutboundCallTabEnabled, businessSubuserConversationsWebsocketTabEnabled]
    );

    EnableFullPermissionHelper(
        businessSubuserConversationsInboundCallTabEnabled,
        [businessSubuserConversationsDeleteInboundCall, businessSubuserConversationsExportInboundCall]
    );

    EnableFullPermissionHelper(
        businessSubuserConversationsOutboundCallTabEnabled,
        [businessSubuserConversationsDeleteOutboundCall, businessSubuserConversationsExportOutboundCall]
    );

    EnableFullPermissionHelper(
        businessSubuserConversationsWebsocketTabEnabled,
        [businessSubuserConversationsDeleteWebsocket, businessSubuserConversationsExportWebsocket]
    );

    // Settings
    EnableFullPermissionHelper(
        businessSubuserSettingsTabEnabled,
        [businessSubuserSettingsGeneralTabEnabled, businessSubuserSettingsLanguagesTabEnabled, businessSubuserSettingsUsersTabEnabled]
    );

    EnableFullPermissionHelper(
        businessSubuserSettingsGeneralTabEnabled,
        [businessSubuserSettingsEditGeneral]
    );

    EnableFullPermissionHelper(
        businessSubuserSettingsLanguagesTabEnabled,
        [businessSubuserSettingsEditLanguages, businessSubuserSettingsAddLanguages, businessSubuserSettingsDeleteLanguages]
    );

    EnableFullPermissionHelper(
        businessSubuserSettingsUsersTabEnabled,
        [businessSubuserSettingsEditUsers, businessSubuserSettingsAddUsers, businessSubuserSettingsDeleteUsers]
    );
}

function ShowSettingsDomainsManageTab()
{
    settingsInnerTabContainer.removeClass("show");
    businessDomainsListTab.removeClass("show");

    setTimeout(() => {
        settingsInnerTabContainer.addClass("d-none");
        businessDomainsListTab.addClass("d-none");

        settingsManageDomainsBreadcrumb.removeClass("d-none");
        businessDomainsManagerTab.removeClass("d-none");
        setTimeout(() => {
            settingsManageDomainsBreadcrumb.addClass("show");
            businessDomainsManagerTab.addClass("show");
        }, 10);
    }, 300);
}

function ShowSettingsDomainsListTab()
{
    settingsManageDomainsBreadcrumb.removeClass("show");
    businessDomainsManagerTab.removeClass("show");
    setTimeout(() => {
        settingsManageDomainsBreadcrumb.addClass("d-none");
        businessDomainsManagerTab.addClass("d-none");

        settingsInnerTabContainer.removeClass("d-none");
        businessDomainsListTab.removeClass("d-none");
        setTimeout(() => {
            settingsInnerTabContainer.addClass("show");
            businessDomainsListTab.addClass("show");
        }, 10);
    }, 300);
}

function CreateSettingsDefaultDomainObject()
{
    let newObject = {
        id: -1,
        type: {
            value: 0,
            name: "Unknown"
        },
        subDomain: "",
        customDomain: "",
        useCustomSSL: false,
        sslPrivateKey: "",
        sslCertificate: ""
    }

    return newObject;
}

function ResetSettingsDomainsManageTab(isEdit = false)
{
    businessDomainsManagerTab.find("input, textarea").val("");
    businessDomainsManagerTab.find(".is-invalid").removeClass("is-invalid");
    businessDomainsManagerTab.find("input[type=checkbox]").prop("checked", false).change();

    businessDomainsDomainType.val("Unknown").change();
    businessDomainsSslConfig.find('input[name="businessDomainSSLType"][ssl-type="free"]').click().change();

    businessDomainsDomainType.prop("disabled", isEdit);
    businessDomainsIqraSubdomain.prop("disabled", isEdit);
    businessDomainsCustomDomain.prop("disabled", isEdit);

    saveBusinessDomainButton.prop("disabled", false);
    saveBusinessDomainButtonSpinner.addClass("d-none");
}

function FillSettingsDomainsManageTab(domainData)
{
    if (domainData.type.value == 1)
    {
        businessDomainsDomainType.val("IqraSubdomain").change();
        businessDomainsIqraSubdomain.val(domainData.subDomain);
    }

    if (domainData.type.value == 2)
    {
        businessDomainsDomainType.val("CustomDomain").change();
        businessDomainsCustomDomain.val(domainData.customDomain);

        if (domainData.useCustomSSL) {
            businessDomainsSslConfig.find('input[name="businessDomainSSLType"][ssl-type="custom"]').click().change();

            businessDomainsSslCertificate.val(domainData.sslCertificate);
            businessDomainsSslPrivateKey.val(domainData.sslPrivateKey);
        }
    }
}

function CheckIfSettingsDomainManagerHasChanges(enableDisableButton = true)
{
    let hasChanges = false;
    let changes = {};

    let currentDomainType = businessDomainsDomainType.val();

    changes.type = currentDomainType;
    if (CurrentManageDomainData.type.name != currentDomainType)
    {
        hasChanges = true;
    }

    if (currentDomainType == "IqraSubdomain")
    {
        changes.subDomain = businessDomainsIqraSubdomain.val();
        if (businessDomainsIqraSubdomain.val() != CurrentManageDomainData.subDomain)
        {
            hasChanges = true;
        }
    }
    else if (currentDomainType == "CustomDomain")
    {
        changes.customDomain = businessDomainsCustomDomain.val();
        if (businessDomainsCustomDomain.val() != CurrentManageDomainData.customDomain)
        {
            hasChanges = true;
        }

        let checkedSSLType = businessDomainsSslConfig.find('input[name="businessDomainSSLType"]:checked').attr("ssl-type");
        changes.useCustomSSL = (checkedSSLType == "custom");

        if (checkedSSLType == "custom" && CurrentManageDomainData.useCustomSSL == null) {
            hasChanges = true;
        }
        else if (checkedSSLType == "custom" && CurrentManageDomainData.useCustomSSL) {
            changes.sslCertificate = businessDomainsSslCertificate.val();
            if (businessDomainsSslCertificate.val() != CurrentManageDomainData.sslCertificate) {
                hasChanges = true;
            }

            changes.sslPrivateKey = businessDomainsSslPrivateKey.val();
            if (businessDomainsSslPrivateKey.val() != CurrentManageDomainData.sslPrivateKey) {
                hasChanges = true;
            }
        }
    }

    if (enableDisableButton)
    {
        if (hasChanges)
        {
            saveBusinessDomainButton.prop("disabled", false);
        }
        else
        {
            saveBusinessDomainButton.prop("disabled", true);
        }
    }

    return {
        hasChanges: hasChanges,
        changes: changes
    };
}

function ValidateSettingsDomainManageFields(onlyRemove = true)
{
    let errors = [];
    let validated = true;

    let domainType = businessDomainsDomainType.val();
    if (domainType == "Unknown")
    {
        validated = false;
        errors.push("Domain type is required and can not be empty.");

        if (!onlyRemove)
        {
            businessDomainsDomainType.addClass("is-invalid");
        }
    }
    else
    {
        businessDomainsDomainType.removeClass("is-invalid");
    }

    if (domainType == "IqraSubdomain")
    {
        if (businessDomainsIqraSubdomain.val() == "")
        {
            validated = false;
            errors.push("Subdomain name is required and can not be empty.");

            if (!onlyRemove)
            {
                businessDomainsIqraSubdomain.addClass("is-invalid");
            }
        }
        else
        {
            businessDomainsIqraSubdomain.removeClass("is-invalid");
        }
    }

    if (domainType == "CustomDomain")
    {
        if (businessDomainsCustomDomain.val() == "")
        {
            validated = false;
            errors.push("Custom domain name is required and can not be empty.");

            if (!onlyRemove)
            {
                businessDomainsCustomDomain.addClass("is-invalid");
            }
        }
        else
        {
            businessDomainsCustomDomain.removeClass("is-invalid");
        }

        let domainSSLType = businessDomainsSslConfig.find('input[name="businessDomainSSLType"]:checked').attr("ssl-type");
        if (domainSSLType == "custom")
        {
            if (businessDomainsSslCertificate.val() == "")
            {
                validated = false;
                errors.push("Certificate is required and can not be empty.");

                if (!onlyRemove)
                {
                    businessDomainsSslCertificate.addClass("is-invalid");
                }
            }
            else
            {
                businessDomainsSslCertificate.removeClass("is-invalid");
            }

            if (businessDomainsSslPrivateKey.val() == "")
            {
                validated = false;
                errors.push("Private key is required and can not be empty.");

                if (!onlyRemove)
                {
                    businessDomainsSslPrivateKey.addClass("is-invalid");
                }
            }
            else
            {
                businessDomainsSslPrivateKey.removeClass("is-invalid");
            }
        }
    }

    return { validated: validated, errors: errors };
}

function initSettingsTab()
{
    $(document).ready(() => {
        settingsGeneralBusinessLogoPreview.on("click", (event) => {
            event.preventDefault();
            settingsGeneralBusinessLogo.click();
        });
    
        settingsGeneralBusinessLogo.on("change", (event) => {
            event.preventDefault();
    
            let file = settingsGeneralBusinessLogo[0].files[0];
            if (!file) {
                settingsGeneralBusinessLogoPreview.attr("src", BusinessLogoURL + "/" + BusinessFullData.businessData.logoURL + ".webp");
                CheckIfSettingsHasChanges();
                return;
            }
            else
            {
                // check if file is greater than 5mb
                if (file.size > 5 * 1024 * 1024)
                {
                    AlertManager.createAlert({
                        type: "danger",
                        message: "File size is too large. Maximum size is 5MB.",
                        timeout: 6000
                    })

                    ettingsGeneralBusinessLogo[0].files = (new DataTransfer()).files;

                    return;
                }

                // validate file type
                if (!file.type.match("image.*"))
                {
                    AlertManager.createAlert({
                        type: "danger",
                        message: "File type is not supported. Only JPEG, PNG, WEBP and GIF are supported.",
                        timeout: 6000
                    })

                    ettingsGeneralBusinessLogo[0].files = (new DataTransfer()).files;

                    return;
                }
            }
    
            let reader = new FileReader();
    
            reader.onload = (event) => {
                settingsGeneralBusinessLogoPreview.attr("src", event.target.result);
            };
    
            reader.readAsDataURL(file);
    
            CheckIfSettingsHasChanges();
        });
    
        settingsGeneralBusinessName.on("input", (event) => {
            ValidateSettingsGeneralTabFields(true);
            CheckIfSettingsHasChanges();
        });
    
        settingsLanguageAddSelect.on("change", (event) => {
            let currentValue = settingsLanguageAddSelect.val();
    
            if (!currentValue || currentValue === null || currentValue === "none")
            {
                settingsLanguageAddButton.prop("disabled", true);
            }
            else
            {
                settingsLanguageAddButton.prop("disabled", false);
            }
        });
    
        settingsLanguageAddButton.on("click", (event) => {
            event.preventDefault();
    
            let selectedValue = settingsLanguageAddSelect.val();
            let countryCodeLanguage = CountryCodeLanguagesList.find((value, index) => {
                return value.Code === selectedValue;
            });
    
            let noNoticeTr = settingsAddedLanguagesList.find("tbody").find("tr[tr-type=none-notice]");
            if (noNoticeTr.length != 0) {
                noNoticeTr.remove();
            }
    
            settingsAddedLanguagesList.find("tbody").append(CreateSettingsAddedLanguagesElement(countryCodeLanguage.Code, countryCodeLanguage.Name));
    
            settingsLanguageAddSelect.val("none");
            settingsLanguageAddButton.prop("disabled", true);
    
            settingsLanguageAddSelect.find(`option[value="${selectedValue}"]`).remove();
    
            CheckIfSettingsHasChanges();
        });
    
        $(document).on('click', '#settingsAddedLanguagesList button[button-type="settingsLanguageRemove"]', async (event) => {
            event.preventDefault();
            event.stopPropagation();
            
            let languageCode = $(event.currentTarget).attr('language-code');
    
            let languageData = CountryCodeLanguagesList.find((value, index) => {
                return value.Code === languageCode;
            });

            if (BusinessFullData.businessData.languages.includes(languageCode))
            {
                var confirmDeleteDialog = new BootstrapConfirmDialog(
                    {
                        title: 'Confirm Delete Language',
                        message: 'Are you sure you want to delete the language <b>' + languageData.Name + '</b>?<br><br>Deleting the language will remove all references to this language from context, tools, agents, and other components while automatically re-publishing inbound call routings without this language.<br><br><b>This action cannot be undone.</b>',
                        confirmText: 'Delete',
                        cancelText: 'Cancel', 
                        confirmButtonClass: 'btn-danger',
                        modalClass: 'modal-lg',
                    }
                );
                
                var confirmDeleteDialogResult = await confirmDeleteDialog.show();
    
                if (!confirmDeleteDialogResult)
                {
                    return;
                }
            }            
    
            settingsAddedLanguagesList.find(`tr[language-code="${languageCode}"]`).remove();
    
            settingsLanguageAddSelect.append(`<option value="${languageData.Code}">${languageData.Name} | ${languageData.Code}</option>`);
    
            if (settingsAddedLanguagesList.find("tbody").find("tr").length === 0) {
                settingsAddedLanguagesList.find("tbody").append('<tr tr-type="none-notice"><td colspan="3">No language added yet...</td></tr>');
            }
    
            CheckIfSettingsHasChanges();
        });
    
        settingsSaveButton.on("click", (event) => {
            event.preventDefault();

            let generalTabValidation = ValidateSettingsGeneralTabFields(false);
            if (!generalTabValidation.validated)
            {
                AlertManager.createAlert({
                    type: 'danger',
                    message: 'Validation for required fields failed.<br><br>' + generalTabValidation.errors.join('<br>'),
                    timeout: 6000
                });

                return;
            }

            let changes = CheckIfSettingsHasChanges(false).changes;

            if (changes.languages && changes.languages.languages)
            {
                if (changes.languages.languages.length === 0)
                {
                    AlertManager.createAlert({
                        type: 'danger',
                        message: 'You must have atleast one language in order to save settings.',
                        timeout: 6000
                    });

                    return;
                }
            }

            let formData = new FormData();
            if (changes.general)
            {
                if (changes.general.name)
                {
                    formData.append("general.name", changes.general.name);
                }
                
                if (changes.general.logo)
                {
                    formData.append("general.logo", changes.general.logo);
                }
            }

            if (changes.languages)
            {
                if (changes.languages.languages)
                {
                    formData.append("languages", changes.languages.languages);
                }
            }        

            if (settingsAddedLanguagesList.find("tbody").find("tr").length === 0) {
                AlertManager.createAlert({
                    type: 'danger',
                    message: 'You must have atleast one language added in order to save settings.',
                    timeout: 6000
                });
                return;
            }

            settingsSaveButton.prop("disabled", true);
            settingsSaveButtonSpinner.removeClass("d-none");

            SaveSettingsChanges(formData,
                (saveResponse) => {
                    if (saveResponse.success)
                    {
                        setTimeout(() => {
                            location.reload();
                        }, 1);
                    }
                    
                    settingsSaveButton.prop("disabled", false);
                    settingsSaveButtonSpinner.addClass("d-none");
                },
                (saveError, isUnsuccessful) => {
                    AlertManager.createAlert({
                        type: 'danger',
                        message: 'Error occured while saving business settings data. Check browser console for logs.',
                        timeout: 6000
                    });

                    console.log('Error occured while saving business settings data: ', saveError);

                    settingsSaveButton.prop("disabled", false);
                    settingsSaveButtonSpinner.addClass("d-none");
                }
            )
        });

        addNewBusinessSubuserButton.on("click", (event) => {
            event.preventDefault();

            ResetSettingsUsersManageTab();
            currentBusinessSubuserName.text("New Subuser");
            saveBusinessSubuserButton.prop("disabled", true);

            IsManageUserTabOpened = true;
            ManageUserType = "new";

            let newObject = CreateSettingsDefaultSubuserObject();

            CurrentManageSubUserData = newObject.user;

            ShowSettingsUsersManageTab();
        });

        switchBackToBusinessSubusersTab.on("click", (event) => {
            event.preventDefault();

            if (IsSavingUsersTab)
            {
                AlertManager.createAlert({
                    type: 'warning',
                    message: 'Unable to leave this tab while saving. Please wait until the save is complete.',
                    timeout: 6000
                });
                
                return;
            }

            IsManageUserTabOpened = false;
            ManageUserType = null;

            ShowSettingsUsersListTab();
        });

        businessSubuserWhiteLabelLogoPreview.on("click", (event) => {
            event.preventDefault();

            businessSubuserWhiteLabelLogo.click();
        });

        businessSubuserWhiteLabelLogo.on("change", (event) => {
            event.preventDefault();

            let file = businessSubuserWhiteLabelLogo[0].files[0];
            if (!file) {
                return;
            }

            let reader = new FileReader();
            reader.onload = (event) => {
                businessSubuserWhiteLabelLogoPreview.attr("src", event.target.result);
            }

            reader.readAsDataURL(file);
        });

        businessSubuserWhiteLabelFaviconPreview.on("click", (event) => {
            event.preventDefault();

            businessSubuserWhiteLabelFavicon.click();
        });

        businessSubuserWhiteLabelFavicon.on("change", (event) => {
            event.preventDefault();

            let file = businessSubuserWhiteLabelFavicon[0].files[0];
            if (!file) {
                return;
            }

            let reader = new FileReader();
            reader.onload = (event) => {
                businessSubuserWhiteLabelFaviconPreview.attr("src", event.target.result);
            }

            reader.readAsDataURL(file);
        });

        businessDomainsDomainType.on("change", (event) => {
            let current = $(event.currentTarget);
            let currentValue = current.val();

            businessDomainsIqraSubdomainContainer.addClass("d-none");
            businessDomainsCustomDomainContainer.addClass("d-none");

            if (currentValue === "Unknown")
            { 
                return;
            }

            if (currentValue === "IqraSubdomain")
            {
                businessDomainsIqraSubdomainContainer.removeClass("d-none");
            }
            else if (currentValue === "CustomDomain")
            {
                businessDomainsCustomDomainContainer.removeClass("d-none");
            }
        });
    
        $("#settings-inner-tab button.nav-link").on("show.bs.tab", (event) => {
            let newTabId = $(event.target).attr("id");

            if (newTabId === "settings-inner-users-tab" || newTabId === "settings-inner-domains-tab")
            {
                settingsSaveButton.addClass("d-none");
            }
            else
            {
                settingsSaveButton.removeClass("d-none");
            }
        });

        $("#nav-bar").on('tabChange', async(event) => {
            if (IsManagerDomainTabOpened)
            {
                if (IsSavingDomainTab)
                {
                    AlertManager.createAlert({
                        type: 'warning',
                        message: 'Unable to leave this tab while saving. Please wait until the save is complete.',
                        timeout: 6000
                    });
                    
                    event.preventDefault();
                    return;
                }

                let manageDomainChanges = CheckIfSettingsDomainManagerHasChanges(false).hasChanges;
                if (!manageDomainChanges)
                {
                    switchBackToBusinessDomainsTab.click();
                }
                else
                {
                    var confirmCloseMangeDomainDialog = new BootstrapConfirmDialog(
                        {
                            title: 'Discard Domain Edit',
                            message: 'You currently have manage domain tab opened. Are you sure you want to discard these changes and leave the settings tab?',
                            confirmText: 'Discard',
                            cancelText: 'Cancel', 
                            confirmButtonClass: 'btn-danger',
                            modalClass: 'modal-lg',
                        }
                    );

                    var confirmCloseMangeDomainResult = await confirmCloseMangeDomainDialog.show(); 
                    if (confirmCloseMangeDomainResult)
                    {
                        switchBackToBusinessDomainsTab.click();
                    }
                    else
                    {
                        event.preventDefault();
                        return;
                    }
                }
            }

            if (IsManageUserTabOpened)
            {
                if (IsSavingUsersTab)
                {
                    AlertManager.createAlert({
                        type: 'warning',
                        message: 'Unable to leave this tab while saving. Please wait until the save is complete.',
                        timeout: 6000
                    });
                    
                    event.preventDefault();
                    return;
                }

                let manageSubuserChanges = CheckIfSettingsSubusersManageHasChanges(false);
                if (!manageSubuserChanges)
                {
                    switchBackToBusinessSubusersTab.click();
                }
                else
                {
                    var confirmCloseMangeUserDialog = new BootstrapConfirmDialog(
                        {
                            title: 'Discard Subuser Edit',
                            message: 'You currently have manage subuser tab opened. Are you sure you want to discard these changes and leave the settings tab?',
                            confirmText: 'Discard',
                            cancelText: 'Cancel', 
                            confirmButtonClass: 'btn-danger',
                            modalClass: 'modal-lg',
                        }
                    );

                    var confirmCloseMangeUserResult = await confirmCloseMangeUserDialog.show(); 
                    if (confirmCloseMangeUserResult)
                    {
                        switchBackToBusinessSubusersTab.click();
                    }
                    else
                    {
                        event.preventDefault();
                        return;
                    }
                } 
            }

            let settingsChanges = CheckIfSettingsHasChanges(false);
            if (settingsChanges.hasChanges)
            {
                let changesInTabs = [];
                if (settingsChanges.changes.general)
                {
                    changesInTabs.push("general");
                }

                if (settingsChanges.changes.languages)
                {
                    changesInTabs.push("languages");
                }

                var confirmDiscardChangesDialog = new BootstrapConfirmDialog(
                    {
                        title: 'Unsaved Changes Pending',
                        message: 'You have unsaved changes in your ' + changesInTabs.join(", ") + (changesInTabs.length > 1 ? " tabs" : " tab") + '. Are you sure you want to discard these changes and leave the settings tab?',
                        confirmText: 'Discard',
                        cancelText: 'Cancel', 
                        confirmButtonClass: 'btn-danger',
                        modalClass: 'modal-lg',
                    }
                );
                
                var confirmDiscardChangesResult = await confirmDiscardChangesDialog.show();
    
                if (!confirmDiscardChangesResult)
                {
                    event.preventDefault();
                    return;
                }
                else
                {
                    FillSettingsTab();
                }
            }

            settingsInnerGeneralTab.click();
        });

        subusersManagerTab.on('change input', 'input, select, textarea', (event) => {
            event.stopPropagation();

            if (IsManageUserTabOpened == false) return;

            ValidateSettingsSubusersGeneralFields(true);
            ValidateSettingsSubusersWhiteLabelGeneralFields(true);
            CheckIfSettingsSubusersManageHasChanges();
        });

        saveBusinessSubuserButton.on("click", (event) => {
            event.preventDefault();

            let generalTabValidation = ValidateSettingsSubusersGeneralFields(false);
            if (!generalTabValidation.validated)
            {
                AlertManager.createAlert({
                    type: 'danger',
                    message: 'Validation for required general tab fields failed.<br><br>' + generalTabValidation.errors.join('<br>'),
                    timeout: 6000
                });

                return;
            }

            let whiteLabelGeneralTabValidation = ValidateSettingsSubusersWhiteLabelGeneralFields(false);
            if (!whiteLabelGeneralTabValidation.validated)
            {
                AlertManager.createAlert({
                    type: 'danger',
                    message: 'Validation for required white label tab general tab fields failed.<br><br>' + whiteLabelGeneralTabValidation.errors.join('<br>'),
                    timeout: 6000
                });

                return;
            }

            let subUsersTabChanges = CheckIfSettingsSubusersManageHasChanges(false);
            if (!subUsersTabChanges.hasChanges)
            {
                return;
            }

            saveBusinessSubuserButton.prop("disabled", true);
            saveBusinessSubuserButtonSpinner.removeClass("d-none");

            IsSavingUsersTab = true;

            let formData = new FormData();

            formData.append("postType", ManageUserType);
            formData.append("subUserEmail", subUsersTabChanges.changes.general.email);

            if (subUsersTabChanges.changes.general)
            {
                formData.append("general", JSON.stringify(subUsersTabChanges.changes.general));
            }

            if (subUsersTabChanges.changes.whiteLabel)
            {
                if (subUsersTabChanges.changes.whiteLabel.files)
                {
                    if (subUsersTabChanges.changes.whiteLabel.files.logo)
                    {
                        formData.append("whiteLabel.logo", subUsersTabChanges.changes.whiteLabel.files.logo);
                    }

                    if (subUsersTabChanges.changes.whiteLabel.files.favicon)
                    {
                        formData.append("whiteLabel.favicon", subUsersTabChanges.changes.whiteLabel.files.favicon);
                    }

                    subUsersTabChanges.changes.whiteLabel.files = undefined;
                }

                formData.append("whiteLabel", JSON.stringify(subUsersTabChanges.changes.whiteLabel));
            }

            if (subUsersTabChanges.changes.permissions)
            {
                formData.append("permissions", JSON.stringify(subUsersTabChanges.changes.permissions));
            }

            SaveSettingsBusinessSubuser(formData,
                (saveResponse) => {
                    console.log(saveResponse);

                    // todo

                    saveBusinessSubuserButton.prop("disabled", false);
                    saveBusinessSubuserButtonSpinner.addClass("d-none");

                    IsSavingUsersTab = false;
                },
                (saveError, isUnsuccessful) => {
                    AlertManager.createAlert({
                        type: 'danger',
                        message: 'Error occured while saving business subuser data. Check browser console for logs.',
                        timeout: 6000
                    });

                    console.log('Error occured while saving business subuser data: ', saveError);

                    saveBusinessSubuserButton.prop("disabled", false);
                    saveBusinessSubuserButtonSpinner.addClass("d-none");

                    IsSavingUsersTab = false;
                }
            )
        });

        $("#subusersManagerTab input[check-type=permission-with-reason]").on("change", (event) => {
            event.stopPropagation();

            let current = $(event.currentTarget);

            let reasonInput = current.parent().parent().find("input[type=text]");

            if (current.prop("checked")) {
                reasonInput.removeClass("d-none");
            }
            else {
                reasonInput.addClass("d-none");
            }
        });

        addNewBusinessDomainButton.on("click", (event) => {
            event.preventDefault();

            ResetSettingsDomainsManageTab(false);
            currentBusinessDomainName.text("New Domain");
            saveBusinessDomainButton.prop("disabled", true);

            IsManagerDomainTabOpened = true;
            ManageDomainType = "new";

            let newObject = CreateSettingsDefaultDomainObject();
            CurrentManageDomainData = newObject;

            ShowSettingsDomainsManageTab();
        });

        switchBackToBusinessDomainsTab.on("click", (event) => {
            event.preventDefault();

            if (IsSavingDomainTab)
            {
                AlertManager.createAlert({
                    type: 'warning',
                    message: 'Unable to leave this tab while saving. Please wait until the save is complete.',
                    timeout: 6000
                });
                
                return;
            }

            IsManagerDomainTabOpened = false;
            ManageDomainType = null;

            ShowSettingsDomainsListTab();
        });

        businessDomainsSslConfig.on("change", 'input[name="businessDomainSSLType"]', (event) => {
            event.stopPropagation();

            let current = $(event.currentTarget);
            let currentCheckedSSLType = current.attr("ssl-type");
            
            if (currentCheckedSSLType == "custom")
            {
                businessDomainSSLTypeCustom.removeClass("d-none");
            }
            else
            {
                businessDomainSSLTypeCustom.addClass("d-none");
                businessDomainSSLTypeCustom.find("textarea, input").val("");
            }

        });

        businessDomainsTable.on("click", 'button[button-type="settingsDomainEdit"]', (event) => {
           event.preventDefault();

           let current = $(event.currentTarget);

           let currentDomainId = current.attr("domain-id");

           let currentDomainData = BusinessFullData.businessWhiteLabelDomain.find(x => x.id == currentDomainId);

           ResetSettingsDomainsManageTab(true);

           if (currentDomainData.type.value == 1)
           {
                currentBusinessDomainName.text(currentDomainData.subDomain + "." + IqraBusinessDomain);
           }

           if (currentDomainData.type.value == 2)
           {
               currentBusinessDomainName.text(currentDomainData.customDomain);
           }

           
           CurrentManageDomainData = currentDomainData;

           FillSettingsDomainsManageTab(currentDomainData);
           ShowSettingsDomainsManageTab();

           IsManagerDomainTabOpened = true;
           ManageDomainType = "edit";
        });

        businessDomainsManagerTab.on('change input', 'input, select, textarea', (event) => {
            event.stopPropagation();

            if (IsManagerDomainTabOpened == false) return;

            ValidateSettingsDomainManageFields(true);
            CheckIfSettingsDomainManagerHasChanges();
        });

        saveBusinessDomainButton.on("click", (event) => {
            event.preventDefault();

            let manageTabValidation = ValidateSettingsDomainManageFields(false);
            if (!manageTabValidation.validated)
            {
                AlertManager.createAlert({
                    type: 'danger',
                    message: 'Validation for required fields failed.<br><br>' + manageTabValidation.errors.join('<br>'),
                    timeout: 6000
                });

                return;
            }

            saveBusinessDomainButton.prop("disabled", true);
            saveBusinessDomainButtonSpinner.removeClass("d-none");
            IsSavingDomainTab = true;

            let formData = new FormData();

            // Is edit or new
            formData.append("postType", ManageDomainType);

            if (ManageDomainType == "edit")
            {
                formData.append("domainId", CurrentManageDomainData.id);
            }

            let domainManagerChanges = CheckIfSettingsDomainManagerHasChanges(false);

            formData.append("changes", JSON.stringify(domainManagerChanges.changes));

            SaveSettingsBusinessDomain(formData,
                (saveResponse) => {
                    console.log(saveResponse);

                    let newDomainData = saveResponse.data;
                    
                    if (ManageDomainType == "edit")
                    {
                        let updatedDomainCurrentIndex = BusinessFullData.businessWhiteLabelDomain.findIndex(x => x.id == CurrentManageDomainData.id);
                        BusinessFullData.businessWhiteLabelDomain[updatedDomainCurrentIndex] = newDomainData;
                    }
                    else if (ManageDomainType == "new")
                    {
                        BusinessFullData.businessWhiteLabelDomain.push(newDomainData);
                        BusinessFullData.businessData.whiteLabelDomainIds.push(newDomainData.id);

                        businessDomainsTable.find("tbody").append(CreateSettingsBusinessDomainTableElement(newDomainData));
                    }

                    saveBusinessDomainButton.prop("disabled", false);
                    saveBusinessDomainButtonSpinner.addClass("d-none");
                    IsSavingDomainTab = false;

                    switchBackToBusinessDomainsTab.click();
                },
                (saveError, isUnsuccessful) => {
                    AlertManager.createAlert({
                        type: 'danger',
                        message: 'Error occured while saving business domain data. Check browser console for logs.',
                        timeout: 6000
                    });

                    console.log('Error occured while saving business domain data: ', saveError);

                    saveBusinessDomainButton.prop("disabled", false);
                    saveBusinessDomainButtonSpinner.addClass("d-none");
                    IsSavingDomainTab = false;
                }
            )
        });

        businessSubusersTable.on("click", 'button[button-type="settingsUserEdit"]', (event) => {
            event.preventDefault();
            let current = $(event.currentTarget);

            let currentSubuserEmail = current.attr("user-email");

            let currentSubuserData = BusinessFullData.businessData.subUsers.find(x => x.email == currentSubuserEmail);

            ResetSettingsUsersManageTab(true);

            currentBusinessSubuserName.text(currentSubuserData.email);

            CurrentManageSubUserData = currentSubuserData;

            FillSettingsUsersManageTab(currentSubuserData);
            ShowSettingsUsersManageTab();

            IsManageUserTabOpened = true;
            ManageUserType = "edit";
        });

        EventListenersSettingsSubusersPermissionsEnableHelper();

        // Initialize
        FillSettingsTab();
    });
}
