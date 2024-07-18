// Elements
const settingsTab = $("#settings-tab");

const settingsSaveButton = settingsTab.find("#settingsSaveButton");

const settingsGeneralBusinessName = settingsTab.find("#settingsGeneralBusinessName");
const settingsGeneralBusinessLogoPreview = settingsTab.find("#settingsGeneralBusinessLogoPreview");
const settingsGeneralBusinessLogo = settingsTab.find("#settingsGeneralBusinessLogo");

const settingsLanguageAddSelect = settingsTab.find("#settingsLanguageAddSelect");
const settingsLanguageAddButton = settingsTab.find("#settingsLanguageAddButton");
const settingsAddedLanguagesList = settingsTab.find("#settingsAddedLanguagesList");

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
    let hasChanges = false;
    if (BusinessFullData.businessData.name !== settingsGeneralBusinessName.val())
    {
        hasChanges = true;
    }

    if (settingsGeneralBusinessLogo[0].files.length > 0)
    {
        hasChanges = true;
    }

    return hasChanges;
}

function CheckLanguagesTabHasChanges()
{
    let currentAddedLanguages = GetCurrentAddedLanguages();

    let businessLanguages = BusinessFullData.businessData.languages;

    if (currentAddedLanguages.length !== businessLanguages.length)
    {
        return true;
    }

    for (let i = 0; i < currentAddedLanguages.length; i++)
    {
        if (currentAddedLanguages[i] !== businessLanguages[i])
        {
            return true;
        }
    }

    return false;
}

function CheckIfSettingsHasChanges(enableDisableButton = true) {
    let generalTabHasChanges = CheckGeneralTabHasChanges();
    let languageTabHasChanges = CheckLanguagesTabHasChanges();

    let hasChanges = generalTabHasChanges || languageTabHasChanges;

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
    
    return hasChanges;
}

function FillSettingsTab()
{
    function FillSettingsGeneralTab()
    {
        settingsGeneralBusinessName.val(BusinessFullData.businessData.name);
        if (BusinessFullData.businessData.logoURL)
        {
            // todo
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

    FillSettingsGeneralTab();
    FillSettingsLanguagesTab();
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
                settingsGeneralBusinessLogoPreview.attr("src", BusinessFullData.businessData.logoURL);
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
    
        $(document).on('click', '#settingsAddedLanguagesList button[button-type="settingsLanguageRemove"]', (event) => {
            event.preventDefault();
            event.stopPropagation();
    
            let languageCode = $(event.currentTarget).attr('language-code');
            settingsAddedLanguagesList.find(`tr[language-code="${languageCode}"]`).remove();
    
            let languageData = CountryCodeLanguagesList.find((value, index) => {
                return value.Code === languageCode;
            });
    
            settingsLanguageAddSelect.append(`<option value="${languageData.Code}">${languageData.Name} | ${languageData.Code}</option>`);
    
            if (settingsAddedLanguagesList.find("tbody").find("tr").length === 0) {
                settingsAddedLanguagesList.find("tbody").append('<tr tr-type="none-notice"><td colspan="3">No language added yet...</td></tr>');
            }
    
            CheckIfSettingsHasChanges();
        });
    
        settingsSaveButton.on("click", (event) => {
            event.preventDefault();
    
            
        });
    
        FillSettingsTab();
    });
}
