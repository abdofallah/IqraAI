$(document).ready(() => {

    const settingsLanguageAddSelect = $("#settingsLanguageAddSelect");

    CountryCodeLanguagesList.forEach((value, index) => {
        settingsLanguageAddSelect.append(`<option value="${value.Code}">${value.Name} | ${value.Code}</option>`);
    });

    $(document).on('click', '#settingsAddedLanguagesList button[button-type="settingsLanguageRemove"]', (event) => {
        event.preventDefault();
        event.stopPropagation();

        $(event.currentTarget).parent().remove();
    })
});