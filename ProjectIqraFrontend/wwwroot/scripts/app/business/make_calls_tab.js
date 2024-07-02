$(document).ready(() => {
    const tooltipTriggerList = document.querySelectorAll('#make-calls-tab [data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));

    const makeCallTypeBox = $(".make-call-type-box-choose");

    const makeCallNumberSingleContainer = $("#make-call-number-single-container");
    const makeCallNumberBulkContainer = $("#make-call-number-bulk-container");

    const editMakeCallRetryOnDeclineCheck = $("#editMakeCallRetryOnDeclineCheck");
    const makeCallDeclineRetryBox = editMakeCallRetryOnDeclineCheck.parent().parent().find(".makeCallRetryBox");

    const editMakeCallRetryOnMissCallCheck = $("#editMakeCallRetryOnMisscallCheck");
    const makeCallMissRetryBox = editMakeCallRetryOnMissCallCheck.parent().parent().find(".makeCallRetryBox");



    makeCallTypeBox.on('click', (event) => {
        let boxType = $(event.currentTarget).attr('box-type');

        let activeBox = makeCallTypeBox.filter('.active');
        let activeBoxType = activeBox.attr('box-type');

        if (boxType === activeBoxType) return;

        activeBox.removeClass('active');
        $(event.currentTarget).addClass('active');

        if (boxType === 'single') {
            function makeSingleBoxVisible() {
                makeCallNumberSingleContainer.removeClass("d-none");
                setTimeout(() => {
                    makeCallNumberSingleContainer.addClass("show");
                }, 10);
            }

            if (makeCallNumberBulkContainer.hasClass("show")) {
                makeCallNumberBulkContainer.removeClass("show");
                setTimeout(() => {
                    makeCallNumberBulkContainer.addClass("d-none");
                    setTimeout(() => {
                        makeSingleBoxVisible();
                    }, 100);
                }, 300);
            }
            else {
                makeSingleBoxVisible();
            }


        }
        else if (boxType === 'bulk') {
            function makeBulkBoxVisible() {
                makeCallNumberBulkContainer.removeClass("d-none");
                setTimeout(() => {
                    makeCallNumberBulkContainer.addClass("show");
                }, 10);
            }

            if (makeCallNumberSingleContainer.hasClass("show")) {
                makeCallNumberSingleContainer.removeClass("show");
                setTimeout(() => {
                    makeCallNumberSingleContainer.addClass("d-none");
                    setTimeout(() => {
                        makeBulkBoxVisible();
                    }, 100);
                }, 300);
            }
            else {
                makeBulkBoxVisible();
            }
        }
    });

    editMakeCallRetryOnDeclineCheck.on('change', () => {
        if (editMakeCallRetryOnDeclineCheck.is(':checked')) {
            makeCallDeclineRetryBox.removeClass("d-none");

            setTimeout(() => {
                makeCallDeclineRetryBox.addClass("show");
            }, 10);
        }
        else {
            makeCallDeclineRetryBox.removeClass("show");

            setTimeout(() => {
                makeCallDeclineRetryBox.addClass("d-none");
            }, 300);

        }
    });

    editMakeCallRetryOnMissCallCheck.on('change', () => {
        if (editMakeCallRetryOnMissCallCheck.is(':checked')) {
            makeCallMissRetryBox.removeClass("d-none");

            setTimeout(() => {
                makeCallMissRetryBox.addClass("show");
            }, 10);
        }
        else {
            makeCallMissRetryBox.removeClass("show");

            setTimeout(() => {
                makeCallMissRetryBox.addClass("d-none");
            }, 300);

        }
    });
});