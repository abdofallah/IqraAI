/** Dynamic Variables **/

/** Element Variables  **/
const tooltipTriggerList = document.querySelectorAll('#routing-tab [data-bs-toggle="tooltip"]');
const tooltipList = [...tooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

const routingTab = $("#routing-tab");

const routingHeader = routingTab.find("#routing-header");

// List Tab
const routingListTab = routingTab.find("#routingListTab");

const addNewRoutingButton = routingListTab.find("#addNewRouteButton");

// Manager Tab
const currentRouteName = routingHeader.find("#currentRouteName");
const switchBackToRoutingTabButton = routingHeader.find("#switchBackToRoutingTab");

const routingManagerTab = routingTab.find("#routingManagerTab");

const editRouteDefaultLanguageSelect = routingTab.find("#editRouteDefaultLanguageSelect");

const editRouteMultiLanguageCheck = routingTab.find("#editRouteMultiLanguageCheck");

const editRouteAddMultiLanguageEnabledSelect = routingTab.find("#editRouteAddMultiLanguageEnabledSelect");
const routeMultiLanguagesEnabledList = routingTab.find("#routeMultiLanguagesEnabledList");

const editRouteAgentConversationTypeSelect = routingTab.find("#editRouteAgentConversationTypeSelect");

/** API FUNCTIONS **/

/** Functions **/
function showRoutingManagerTab() {
	routingListTab.removeClass("show");
	setTimeout(() => {
		routingListTab.addClass("d-none");

		routingManagerTab.removeClass("d-none");
		routingHeader.removeClass("d-none");
		setTimeout(() => {
			routingManagerTab.addClass("show");
			routingHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function showRoutingListTab() {
	routingManagerTab.removeClass("show");
	routingHeader.removeClass("show");
	setTimeout(() => {
		routingManagerTab.addClass("d-none");
		routingHeader.addClass("d-none");

		routingListTab.removeClass("d-none");
		setTimeout(() => {
			routingListTab.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function initRoutingTab() {
	$(document).ready(() => {
		addNewRoutingButton.on("click", (event) => {
			event.preventDefault();

			currentRouteName.text("New Route");

			showRoutingManagerTab();
		});

		switchBackToRoutingTabButton.on("click", (event) => {
			event.preventDefault();

			showRoutingListTab();
		});

		editRouteMultiLanguageCheck.on("change", (event) => {
			const isChecked = $(event.currentTarget).is(":checked");

			editRouteAddMultiLanguageEnabledSelect.prop("disabled", !isChecked);
			if (isChecked) {
				routeMultiLanguagesEnabledList.removeClass("disabled");
			} else {
				routeMultiLanguagesEnabledList.addClass("disabled");
			}

			routeMultiLanguagesEnabledList.find("tr td button, tr td input").each((index, element) => {
				$(element).prop("disabled", !isChecked);
			});
		});

		editRouteAddMultiLanguageEnabledSelect.on("change", (event) => {
			const selectedValue = $(event.currentTarget).val();
			if (selectedValue === "select" || !selectedValue || selectedValue === "") return;

			const optionElement = editRouteAddMultiLanguageEnabledSelect.find('option[value="' + selectedValue + '"]');
			const optionText = optionElement.text();

			const tbody = $(routeMultiLanguagesEnabledList.find("tbody")[0]);

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

		$(document).on("click", '[button-type="remove-enabled-language"]', (event) => {
			event.preventDefault();
			event.stopPropagation();
			event.stopImmediatePropagation();

			const parent = $(event.currentTarget).parent().parent();
			const code = parent.attr("code");
			const name = parent.attr("name");

			editRouteAddMultiLanguageEnabledSelect.append(`<option value="${code}">${name}</option>`);
			parent.remove();

			ResortMultiLanugageEnabledListNumbers();
		});

		routeMultiLanguagesEnabledList.find("tbody").sortable({
			items: 'tr:not([data-type="nothing-added"])',
			cursor: "pointer",
			axis: "y",
			dropOnEmpty: false,
			forceHelperSize: true,
			forcePlaceholderSize: true,
			handle: 'button[button-type="move-enabled-language"]',
			cancel: "",
			start: (e, ui) => {
				ui.item.addClass("selected");
			},
			stop: (e, ui) => {
				ui.item.removeClass("selected");

				ResortMultiLanugageEnabledListNumbers();
			},
		});

		function ResortMultiLanugageEnabledListNumbers() {
			const tbodyChild = $(routeMultiLanguagesEnabledList.find("tbody")[0]).children();

			tbodyChild.each((index, element) => {
				console.log(element);
				$(element)
					.find("td:nth-child(2)")
					.text(index + 1);
			});
		}

		editRouteAgentConversationTypeSelect.on("change", (event) => {
			const selectedValue = editRouteAgentConversationTypeSelect.val();

			if (!selectedValue) return;

			const interruptibleBox = $('.route-conversation-type-box[box-type="interruptible"]');

			if (selectedValue === "interruptible") {
				interruptibleBox.removeClass("d-none");
			} else if (selectedValue === "turnbyturn") {
				interruptibleBox.addClass("d-none");
			}
		});
	});
}
