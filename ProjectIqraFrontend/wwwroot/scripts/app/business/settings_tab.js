var CurrentManageSubUserData = null;
var CurrentManageSubUserWhiteLabelDomainData = null;

var DeletedSubUserIds = [];
var EditedSubUsers = [];
var AddedSubUsers = [];

// Elements
const settingsTab = $("#settings-tab");

const settingsSaveButton = settingsTab.find("#settingsSaveButton");

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
const switchBackToBusinessSubusersTab = settingsTab.find("#switchBackToBusinessSubusersTab");

const currentBusinessSubuserName = settingsTab.find("#currentBusinessSubuserName");

const businessSubuserWhiteLabelDomainType = subusersManagerTab.find("#business-subuser-white-label-domain-type");

const businessSubuserWhiteLabelLogoPreview = subusersManagerTab.find("#business-subuser-white-label-logo-preview");
const businessSubuserWhiteLabelLogo = subusersManagerTab.find("#business-subuser-white-label-logo");

const businessSubuserWhiteLabelFaviconPreview = subusersManagerTab.find("#business-subuser-white-label-favicon-preview");
const businessSubuserWhiteLabelFavicon = subusersManagerTab.find("#business-subuser-white-label-favicon");

const businessSubuserManagerGeneralTab = subusersManagerTab.find("#business-subuser-manager-general-tab");
const businessSubuserPermissionsRoutingTab = subusersManagerTab.find("#business-subuser-permissions-routing-tab");
const subusersWhitelabelGeneralTab = subusersManagerTab.find("#subusers-whitelabel-general-tab");

const saveBusinessSubuserButton = subusersManagerTab.find("#saveBusinessSubuserButton");

const businessSubuserLoginDisabledInput = subusersManagerTab.find("#business-subuser-login-disabled");
const businessSubuserLoginDisabledReasonInput = subusersManagerTab.find("#business-subuser-login-disabled-reason");

const businessSubuserEmail = subusersManagerTab.find("#business-subuser-email");
const businessSubuserPassword = subusersManagerTab.find("#business-subuser-password");

const businessSubuserWhiteLabelPlatformName = subusersManagerTab.find("#business-subuser-white-label-platform-name");
const businessSubuserWhiteLabelPlatformTitle = subusersManagerTab.find("#business-subuser-white-label-platform-title");
const businessSubuserWhiteLabelPlatformDescription = subusersManagerTab.find("#business-subuser-white-label-platform-description");

const businessSubuserWhiteLabelCustomCss = subusersManagerTab.find("#business-subuser-white-label-custom-css");
const businessSubuserWhiteLabelCustomJs = subusersManagerTab.find("#business-subuser-white-label-custom-js");

const businessSubuserWhiteLabelIqraSubdomainContainer = subusersManagerTab.find("#business-subuser-white-label-iqra-subdomain-container");
const businessSubuserWhiteLabelIqraSubdomain = businessSubuserWhiteLabelIqraSubdomainContainer.find("#business-subuser-white-label-iqra-subdomain");

const businessSubuserWhiteLabelCustomDomainContainer = subusersManagerTab.find("#business-subuser-white-label-custom-domain-container");
const businessSubuserWhiteLabelCustomDomain = businessSubuserWhiteLabelCustomDomainContainer.find("#business-subuser-white-label-custom-domain");
const businessSubuserWhiteLabelSslEnabled = subusersManagerTab.find("#business-subuser-white-label-ssl-enabled");
const businessSubuserWhiteLabelSslConfig = subusersManagerTab.find("#business-subuser-white-label-ssl-config");
const businessSubuserWhiteLabelSslPrivateKey = subusersManagerTab.find("#business-subuser-white-label-ssl-private-key");
const businessSubuserWhiteLabelSslCertificate = subusersManagerTab.find("#business-subuser-white-label-ssl-certificate");

// API Functions

function SaveNewSettings(changes, successCallback, errorCallback)
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

// Functions
function CreateAddedLanguagesElement(code, name) {
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

function GetCurrentAddedLanguages()
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

function CheckGeneralTabHasChanges()
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

function CheckLanguagesTabHasChanges()
{
    let currentAddedLanguages = GetCurrentAddedLanguages();
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

    for (let i = 0; i < currentAddedLanguages.length; i++)
    {
        if (currentAddedLanguages[i] !== businessLanguages[i])
        {
            return {
                hasChanges: true,
                changes: {
                    languages:  currentAddedLanguages
                }
            };
        }
    }

    return {
        hasChanges: false,
        changes: null
    };
}

function CheckIfSettingsHasChanges(enableDisableButton = true) {
    let generalTabHasChanges = CheckGeneralTabHasChanges();
    let languageTabHasChanges = CheckLanguagesTabHasChanges();

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
    
    return {
        hasChanges: hasChanges,
        changes: {
            general: generalTabHasChanges.changes,
            languages: languageTabHasChanges.changes
        }
    };
}

function CreateBusinessSubusersTableElement(userData)
{
    let element = $(`
        <tr user-id="${userData.email}">
            <td>${userData.email}</td>
            <td>
                <button user-id="${userData.email}" class="btn btn-danger" button-type="settingsUserRemove" type="button">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `);

    return element;
}

function FillSettingsTab()
{
    function FillSettingsGeneralTab()
    {
        settingsGeneralBusinessName.val(BusinessFullData.businessData.name);
        if (BusinessFullData.businessData.logoURL && BusinessFullData.businessData.logoURL != null)
        {
            settingsGeneralBusinessLogoPreview.attr("src", BusinessLogoURL + "/" + BusinessFullData.businessData.logoURL);
        }
    }

    function FillSettingsLanguagesTab()
    {
        CountryCodeLanguagesList.forEach((value, index) => {
            settingsLanguageAddSelect.append(`<option value="${value.Code}">${value.Name} | ${value.Code}</option>`);
        });

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
    
                let element = CreateAddedLanguagesElement(countryCodeLanguage.Code, countryCodeLanguage.Name);
                settingsAddedLanguagesList.append(element);

                settingsLanguageAddSelect.find(`option[value="${value}"]`).remove();
            })
        }
    }

    function FillSettingsUsersTab()
    {
        if (BusinessFullData.businessData.subUsers.length === 0)
        {
            businessSubusersTable.find("tbody").append('<tr tr-type="none-notice"><td colspan="2">No subuser added yet...</td></tr>');
        }
        else
        {
            BusinessFullData.businessData.subUsers.forEach((userData, index) => {
                businessSubusersTable.append(CreateBusinessSubusersTableElement(userData));
            })
        }
    }

    FillSettingsGeneralTab();
    FillSettingsLanguagesTab();
    FillSettingsUsersTab();
}

function ShowUsersManageTab()
{
    settingsInnerTabContainer.removeClass("show");
    businessSubusersListTab.removeClass("show");

    setTimeout(() => {
        settingsInnerTabContainer.addClass("d-none");
        businessSubusersListTab.addClass("d-none");

        subusersManagerTab.removeClass("d-none");
        setTimeout(() => {
            subusersManagerTab.addClass("show");
        }, 10);
    }, 300);
}

function ShowUsersListTab()
{
    subusersManagerTab.removeClass("show");
    setTimeout(() => {
        subusersManagerTab.addClass("d-none");

        settingsInnerTabContainer.removeClass("d-none");
        businessSubusersListTab.removeClass("d-none");
        setTimeout(() => {
            settingsInnerTabContainer.addClass("show");
            businessSubusersListTab.addClass("show");
        }, 10);
    }, 300);
}

function ResetUsersManageTab()
{
    subusersManagerTab.find("input[type=text], input[type=email], input[type=number], textarea").val("");
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

    businessSubuserWhiteLabelLogoPreview.attr("src", "/img/logo/logo-colored-light.png");
    businessSubuserWhiteLabelFaviconPreview.attr("src", "/img/logo/logo-colored-light.png");

    businessSubuserWhiteLabelDomainType.val("Unknown").change();

    businessSubuserManagerGeneralTab.click();
    businessSubuserPermissionsRoutingTab.click();
    subusersWhitelabelGeneralTab.click();
}

function FillUsersManageTab(usersData, whitelabelDomainData)
{
    // General
    businessSubuserEmail.val(usersData.email);
    businessSubuserPassword.val(usersData.password);

    SetPermissionInput(businessSubuserLoginDisabledInput, businessSubuserLoginDisabledReasonInput, usersData.disabledUserLoginAt, usersData.disabledUserLoginReason);

    // Permissions
    // TODO

    // White Label
    businessSubuserWhiteLabelPlatformName.val(usersData.whitelabel.platformName);
    businessSubuserWhiteLabelPlatformTitle.val(usersData.whitelabel.platformTitle);
    businessSubuserWhiteLabelPlatformDescription.val(usersData.whitelabel.platformDescription);

    businessSubuserWhiteLabelLogoPreview.val(BusinessLogoURL + "/" + usersData.whitelabel.logoURL);
    businessSubuserWhiteLabelFaviconPreview.val(BusinessLogoURL + "/" + usersData.whitelabel.faviconURL);

    businessSubuserWhiteLabelCustomCss.val(usersData.whitelabel.customCSS);
    businessSubuserWhiteLabelCustomJs.val(usersData.whitelabel.customJavaScript);

    if (whitelabelDomainData)
    {
        businessSubuserWhiteLabelDomainType.val(whitelabelDomainData.type).change();
        if (whitelabelDomainData.type === "IqraSubdomain")
        {
            businessSubuserWhiteLabelIqraSubdomainContainer.removeClass("d-none");
            businessSubuserWhiteLabelIqraSubdomain.val(whitelabelDomainData.subDomain);
        }
        else if (whitelabelDomainData.type === "CustomDomain")
        {
            businessSubuserWhiteLabelCustomDomainContainer.removeClass("d-none");
            businessSubuserWhiteLabelCustomDomain.val(whitelabelDomainData.customDomain);

            if (whitelabelDomainData.sslEnabled != null)
            {
                businessSubuserWhiteLabelSslEnabled.prop("checked", true).change();
                businessSubuserWhiteLabelSslPrivateKey.val(whitelabelDomainData.sslPrivateKey);
                businessSubuserWhiteLabelSslCertificate.val(whitelabelDomainData.sslCertificate);
            }
        }
        else
        {
            businessSubuserWhiteLabelDomainType.val("Unknown").change();
        }
    }
    else
    {
        businessSubuserWhiteLabelDomainType.val("Unknown").change();
    }
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
                settingsGeneralBusinessLogoPreview.attr("src", BusinessLogoURL + "/" + BusinessFullData.businessData.logoURL);
                CheckIfSettingsHasChanges();
                return;
            }
    
            let reader = new FileReader();
    
            reader.onload = (event) => {
                settingsGeneralBusinessLogoPreview.attr("src", event.target.result);
            };
    
            reader.readAsDataURL(file);
    
            CheckIfSettingsHasChanges();
        });
    
        settingsGeneralBusinessName.on("input", (event) => {
            let currentValue = settingsGeneralBusinessName.val();
            
            if (!currentValue || currentValue.trim().length === 0 || currentValue === '')
            {
                settingsGeneralBusinessName.addClass("is-invalid");
            }
            else
            {
                settingsGeneralBusinessName.removeClass("is-invalid");
            }
    
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
    
            CheckIfSettingsHasChanges();
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
    
            settingsAddedLanguagesList.find("tbody").append(CreateAddedLanguagesElement(countryCodeLanguage.Code, countryCodeLanguage.Name));
    
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

            SaveNewSettings(formData,
                (saveResponse) => {
                    setTimeout(() => {
                        location.reload();
                    }, 100);
                },
                (saveError, isUnsuccessful) => {
                    AlertManager.createAlert({
                        type: 'danger',
                        message: 'Error occured while saving business settings data. Check browser console for logs.',
                        timeout: 6000
                    });

                    console.log('Error occured while saving business settings data: ', saveError);
                }
            )
        });

        addNewBusinessSubuserButton.on("click", (event) => {
            event.preventDefault();

            ResetUsersManageTab();
            currentBusinessSubuserName.text("New Subuser");
            saveBusinessSubuserButton.prop("disabled", false);

            ShowUsersManageTab();
        });

        switchBackToBusinessSubusersTab.on("click", (event) => {
            event.preventDefault();

            ShowUsersListTab();
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

        businessSubuserWhiteLabelDomainType.on("change", (event) => {
            let current = $(event.currentTarget);
            let currentValue = current.val();

            businessSubuserWhiteLabelIqraSubdomainContainer.addClass("d-none");
            businessSubuserWhiteLabelCustomDomainContainer.addClass("d-none");

            if (currentValue === "Unknown")
            { 
                return;
            }

            if (currentValue === "IqraSubdomain")
            {
                businessSubuserWhiteLabelIqraSubdomainContainer.removeClass("d-none");
            }
            else if (currentValue === "CustomDomain")
            {
                businessSubuserWhiteLabelCustomDomainContainer.removeClass("d-none");
            }
        });

        businessSubuserWhiteLabelSslEnabled.on("change", (event) => {
            let current = $(event.currentTarget);
            let currentChecked = current.prop("checked");

            if (currentChecked)
            {
                businessSubuserWhiteLabelSslConfig.removeClass("d-none");
            }
            else
            {
                businessSubuserWhiteLabelSslConfig.addClass("d-none");
            }
        });
    
        FillSettingsTab();
    });
}
