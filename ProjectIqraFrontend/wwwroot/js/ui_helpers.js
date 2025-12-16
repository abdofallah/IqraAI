function showHideButtonSpinnerWithDisableEnable(button, show = true, disableEnable = true) {
    const spinner = `<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>`;
    if (show) {
        if (disableEnable) button.prop("disabled", true);
        button.prepend(spinner);
    }
    else {
        if (disableEnable) button.prop("disabled", false);
        button.find(".spinner-border").remove();
    }
}