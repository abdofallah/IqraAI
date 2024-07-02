$(document).ready(() => {
    const tooltipTriggerList = document.querySelectorAll('#routing-tab [data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));

    const addNewRoutingButton = $("#addNewRouteButton");

    const routingListTab = $("#routingListTab");
    const routingManagerTab = $("#routingManagerTab");

    const currentRouteName = $("#currentRouteName");

    const editRouteDefaultLanguageSelect = $("#editRouteDefaultLanguageSelect");

    const switchBackToRoutingTabButton = $("#switchBackToRoutingTab");

    const editRouteMultiLanguageCheck = $("#editRouteMultiLanguageCheck");

    const editRouteAddMultiLanguageEnabledSelect = $("#editRouteAddMultiLanguageEnabledSelect");
    const routeMultiLanguagesEnabledList = $("#routeMultiLanguagesEnabledList");

    const editRouteAgentConversationTypeSelect = $("#editRouteAgentConversationTypeSelect");

    addNewRoutingButton.on("click", (event) => {
        event.preventDefault();

        currentRouteName.text("New Route");

        routingListTab.removeClass("show");
        setTimeout(() => {
            routingListTab.addClass("d-none");

            routingManagerTab.removeClass("d-none");
            setTimeout(() => {
                routingManagerTab.addClass("show");
            }, 10);
        }, 150);
    });

    switchBackToRoutingTabButton.on("click", (event) => {
        event.preventDefault();

        routingManagerTab.removeClass("show");
        setTimeout(() => {
            routingManagerTab.addClass("d-none");

            routingListTab.removeClass("d-none");
            setTimeout(() => {
                routingListTab.addClass("show");
            }, 10);
        }, 150);
    });

    editRouteMultiLanguageCheck.on('change', (event) => {
        let isChecked = $(event.currentTarget).is(':checked');

        editRouteAddMultiLanguageEnabledSelect.prop('disabled', !isChecked);
        if (isChecked) {
            routeMultiLanguagesEnabledList.removeClass('disabled');
        }
        else {
            routeMultiLanguagesEnabledList.addClass('disabled');
        }

        routeMultiLanguagesEnabledList.find('tr td button, tr td input').each((index, element) => {
            $(element).prop('disabled', !isChecked);
        });
    });

    editRouteAddMultiLanguageEnabledSelect.on('change', (event) => {
        let selectedValue = $(event.currentTarget).val();
        if (selectedValue === "select" || !selectedValue || selectedValue === "") return;

        let optionElement = editRouteAddMultiLanguageEnabledSelect.find('option[value="' + selectedValue + '"]');
        let optionText = optionElement.text();

        let tbody = $(routeMultiLanguagesEnabledList.find('tbody')[0]);

        tbody.append(`
                              <tr code="${selectedValue}" name="${optionText}">
                                   <td class="text-center px-2">
                                        <button class="btn text-center" button-type="move-enabled-language">
                                             <i class="fa-regular fa-arrows-up-down"></i>
                                        </button>
                                   </td>
                                   <td>${tbody.children().length + 1}</td>
                                   <td>${optionText}</td>
                                   <td>${selectedValue}</td>
                                   <td class="py-2">
                                        <input class="form-control" style="width: 90%" placeholder="Message to speak to user for language selection" value="Press {number} for {name}">
                                   </td>
                                   <td>
                                        <button class="btn btn-danger" button-type="remove-enabled-language">
                                             <i class="fa-regular fa-trash"></i>
                                        </button>
                                   </td>
                              </tr>
                         `);

        optionElement.remove();

        editRouteAddMultiLanguageEnabledSelect.val("select");
        editRouteAddMultiLanguageEnabledSelect.change();
    });

    $(document).on('click', '[button-type="remove-enabled-language"]', (event) => {
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();

        let parent = $(event.currentTarget).parent().parent();
        let code = parent.attr('code');
        let name = parent.attr('name');

        editRouteAddMultiLanguageEnabledSelect.append(`<option value="${code}">${name}</option>`);
        parent.remove();

        ResortMultiLanugageEnabledListNumbers();
    });

    routeMultiLanguagesEnabledList.find("tbody").sortable({
        items: 'tr:not([data-type="nothing-added"])',
        cursor: 'pointer',
        axis: 'y',
        dropOnEmpty: false,
        forceHelperSize: true,
        forcePlaceholderSize: true,
        handle: 'button[button-type="move-enabled-language"]',
        cancel: '',
        start: (e, ui) => {
            ui.item.addClass("selected");
        },
        stop: (e, ui) => {
            ui.item.removeClass("selected");

            ResortMultiLanugageEnabledListNumbers();
        }
    });

    function ResortMultiLanugageEnabledListNumbers() {
        let tbodyChild = $(routeMultiLanguagesEnabledList.find('tbody')[0]).children();

        tbodyChild.each((index, element) => {
            console.log(element);
            $(element).find('td:nth-child(2)').text(index + 1);
        });
    }

    editRouteAgentConversationTypeSelect.on('change', (event) => {
        let selectedValue = editRouteAgentConversationTypeSelect.val();

        if (!selectedValue) return;

        let interruptibleBox = $('.route-conversation-type-box[box-type="interruptible"]');

        if (selectedValue === "interruptible") {
            interruptibleBox.removeClass('d-none');
        }
        else if (selectedValue === "turnbyturn") {
            interruptibleBox.addClass('d-none');
        }
    });
});