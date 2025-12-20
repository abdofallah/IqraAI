/** Dynamic Variables **/
let CurrentIntegrationData = null;
let ManageIntegrationType = null; // new or edit
let SelectedIntegrationType = null;
let IsSavingIntegrationTab = false;
let IsDeletingIntegrationTab = false;

/** Elements Variables **/

const integrationsTab = $("#integrations-tab");
const integrationsListContainer = integrationsTab.find("#integrationsListContainer");
const addNewIntegrationButton = integrationsTab.find("#addNewIntegrationButton");

const addNewIntegrationModal = $("#addNewIntegrationModal");
const availableIntegrationsContainer = addNewIntegrationModal.find("#availableIntegrationsContainer");
const availableIntegrationsList = addNewIntegrationModal.find("#availableIntegrationsList");
const integrationManagerContainer = addNewIntegrationModal.find("#integrationManagerContainer");
const integrationFieldsContainer = addNewIntegrationModal.find("#integrationFieldsContainer");
const integrationHelpContainer = addNewIntegrationModal.find("#integrationHelpContainer");
const backToIntegrationsListButton = addNewIntegrationModal.find("#backToIntegrationsListButton");
const saveIntegrationButton = addNewIntegrationModal.find("#saveIntegrationButton");
const addNewIntegrationModalLabel = addNewIntegrationModal.find("#addNewIntegrationModalLabel");
const editIntegrationModalLabel = addNewIntegrationModal.find("#editIntegrationModalLabel");

/** API Functions **/
function SaveBusinessIntegration(formData, onSuccess, onError) {
	return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/integrations/save`,
		method: "POST",
		data: formData,
		processData: false,
		contentType: false,
		success: (response) => {
			if (response.success) {
				onSuccess(response);
			} else {
				onError(response, true);
			}
		},
		error: (error) => {
			onError(error, false);
		},
	});
}
function DeleteBusinessIntegration(integrationId, onSuccess, onError) {
    return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/integrations/${integrationId}/delete`,
        method: "POST",
        success: (response) => {
            if (response.success) {
                onSuccess(response);
            } else {
                onError(response, true);
            }
        },
        error: (error) => {
            onError(error, false);
        },
    });
}

/** Core Functions **/
function createIntegrationCardElement(integration) {
	const actionDropdownHtml = `
        <div class="dropdown action-dropdown dropdown-menu-end">
            <button class="btn action-button dropdown-toggle" type="button" data-bs-toggle="dropdown" data-bs-auto-close="true" aria-expanded="false">
                <i class="fa-solid fa-ellipsis"></i>
            </button>
            <ul class="dropdown-menu">
                <li>
                    <span class="dropdown-item text-danger" data-item-id="${integration.id}" button-type="delete-integration">
                        <i class="fa-solid fa-trash me-2"></i>Delete
                    </span>
                </li>
            </ul>
        </div>
    `;

	return createIqraCardElement({
		id: integration.id,
		type: 'integration',
		visualHtml: `<img src="${integration.logoUrl}" alt="${integration.friendlyName} logo">`,
		titleHtml: integration.friendlyName,
		actionDropdownHtml: actionDropdownHtml,
	});
}

function createAvailableIntegrationCardElement(integration) {
	const typesBadges = integration.type.map((type) => `<span class="badge border me-1 mb-1">${type}</span>`).join("");

	return `
        <div class="col-lg-6 col-md-6 col-12 mb-3">
            <div class="card h-100 ${(integration.disabledAt == null ? "" : "disabled")} available-integration-card" data-integration-id="${integration.id}">
                <div class="card-body">
					<h5 class="card-title">${integration.name}${(integration.disabledAt == null ? "" : " | <span class='text-danger'>Disabled</span>") }</h5>

					<img class="px-2 my-3" src="${integration.logoUrl}">

                    <div class="mt-2">
                        ${typesBadges}
                    </div>
                </div>
            </div>
        </div>
	   `;
}

function createIntegrationFieldElement(field) {
	let fieldHtml = `
        <div class="mb-3">
            <label class="form-label d-flex align-items-center">
                <span>${field.name} ${field.required ? "<span class='text-danger'>*</span>" : ""}</span>
    `;

	if (field.tooltip) {
		fieldHtml += `
                <a href="#" class="ms-2" data-bs-toggle="tooltip" data-bs-placement="right" 
                   data-bs-title="${field.tooltip}">
                    <i class="fa-regular fa-circle-question"></i>
                </a>
        `;
	}

	fieldHtml += "</label>";

	if (field.type === "select") {
		fieldHtml += `
            <select class="form-select" id="integration_${field.id}" 
                    ${field.required ? "required" : ""}>
                <option disabled>Select ${field.name}</option>
                ${field.options.map((opt) => `<option value="${opt.key}" ${opt.isDefault ? "selected" : ""}>${opt.value}</option>`).join("")}
            </select>
        `;
	} else {
		fieldHtml += `
            <input type="${field.isEncrypted ? "password" : field.type}" real-type="${field.type}" class="form-control" 
                   id="integration_${field.id}" 
                   placeholder="${field.placeholder || `Enter ${field.name}`}"
                   value="${field.defaultValue || ""}"
                   ${field.required ? "required" : ""}>
        `;
	}

	fieldHtml += "</div>";
	return fieldHtml;
}

function resetOrClearIntegrationManager() {
	$("#integrationFriendlyNameInput").val("");
	integrationFieldsContainer.empty();
	integrationHelpContainer.empty();
	saveIntegrationButton.prop("disabled", true);
	integrationManagerContainer.find(".is-invalid").removeClass("is-invalid");
	backToIntegrationsListButton.addClass("d-none");

	if (ManageIntegrationType === "edit") {
		addNewIntegrationModalLabel.addClass("d-none");
		editIntegrationModalLabel.removeClass("d-none");
	} else {
		editIntegrationModalLabel.addClass("d-none");
		addNewIntegrationModalLabel.removeClass("d-none");
	}
}

function fillIntegrationFields(integration) {
	// Add fields
	integration.fields.forEach((field) => {
		integrationFieldsContainer.append(createIntegrationFieldElement(field));

		// If editing, use the saved value, otherwise use default value
		if (ManageIntegrationType === "edit" && CurrentIntegrationData) {
			const savedValue = CurrentIntegrationData.fields[field.id];
			if (savedValue) {
				$(`#integration_${field.id}`).val(savedValue);
			}
		}
	});

	// Add help link if available
	if (integration.help) {
		integrationHelpContainer.html(`
            <a href="${integration.help.uri}" target="_blank" class="text-decoration-none">
                <i class="fa-regular fa-circle-question me-1"></i>
                ${integration.help.text}
            </a>
        `);
	}

	// Initialize tooltips
	initializeIntegrationTooltips();
}

function initializeIntegrationTooltips() {
	const tooltipTriggerList = [].slice.call(document.querySelectorAll('#integrations-tab [data-bs-toggle="tooltip"]'));
	return [...tooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));
}

function FillIntegrationsList() {
	integrationsListContainer.empty();

	if (!BusinessFullData.businessApp.integrations || BusinessFullData.businessApp.integrations.length === 0) {
		integrationsListContainer.append(`
            <div class="col-12 text-center p-5">
                <p class="text-muted mb-0">No integrations found</p>
            </div>
        `);
		return;
	}

	BusinessFullData.businessApp.integrations.forEach((integration) => {
		const integrationDetails = SpecificationIntegrationsListData.find((i) => i.id === integration.type);
		if (integrationDetails) {
			integration.name = integrationDetails.name;
			integration.logoUrl = integrationDetails.logoUrl;
			integrationsListContainer.append($(createIntegrationCardElement(integration)));
		}
	});
}

function ShowAvailableIntegrations() {
	integrationManagerContainer.removeClass("show");
	setTimeout(() => {
		integrationManagerContainer.addClass("d-none");
		saveIntegrationButton.addClass("d-none");
		availableIntegrationsContainer.removeClass("d-none");
		backToIntegrationsListButton.addClass("d-none");

		setTimeout(() => {
			availableIntegrationsContainer.addClass("show");
		}, 10);
	}, 300);
}

function ShowIntegrationManager() {
	availableIntegrationsContainer.removeClass("show");
	setTimeout(() => {
		availableIntegrationsContainer.addClass("d-none");
		integrationManagerContainer.removeClass("d-none");
		saveIntegrationButton.removeClass("d-none");

		setTimeout(() => {
			integrationManagerContainer.addClass("show");
		}, 10);
	}, 300);
}

function ValidateIntegrationTab(onlyRemove = true) {
	const errors = [];
	let validated = true;

	// Validate friendly name
	const friendlyName = $("#integrationFriendlyNameInput").val().trim();
	if (!friendlyName) {
		validated = false;
		errors.push("Friendly name is required");
		if (!onlyRemove) {
			$("#integrationFriendlyNameInput").addClass("is-invalid");
		}
	} else {
		$("#integrationFriendlyNameInput").removeClass("is-invalid");
	}

	// Get current integration type
	const integration = SpecificationIntegrationsListData.find((i) => i.id === SelectedIntegrationType);
	if (!integration) return { validated: false, errors: ["Invalid integration type"] };

	// Validate required fields
	integration.fields.forEach((field) => {
		if (field.required) {
			const value = $(`#integration_${field.id}`).val().trim();
			if (!value) {
				validated = false;
				errors.push(`${field.name} is required`);
				if (!onlyRemove) {
					$(`#integration_${field.id}`).addClass("is-invalid");
				}
			} else {
				$(`#integration_${field.id}`).removeClass("is-invalid");
			}
		}
	});

	return {
		validated: validated,
		errors: errors,
	};
}

function CheckIntegrationTabHasChanges(enableDisableButton = true) {
	const changes = {
		type: SelectedIntegrationType,
		friendlyName: $("#integrationFriendlyNameInput").val().trim(),
		fields: {},
	};

	let hasChanges = false;

	// Compare friendly name
	if (ManageIntegrationType === "new" && changes.friendlyName) {
		hasChanges = true;
	} else if (CurrentIntegrationData && CurrentIntegrationData.friendlyName !== changes.friendlyName) {
		hasChanges = true;
	}

	// Get field values
	const integration = SpecificationIntegrationsListData.find((i) => i.id === SelectedIntegrationType);
	if (integration) {
		integration.fields.forEach((field) => {
			const value = $(`#integration_${field.id}`).val().trim();
			changes.fields[field.id] = value;

			if (ManageIntegrationType === "new") {
				// For new integration, compare with default value
				if (field.type === "select") {
					const defaultOption = field.options.find((opt) => opt.isDefault);
					if (value !== (defaultOption?.key || "")) {
						hasChanges = true;
					}
				} else if (value !== (field.defaultValue || "")) {
					hasChanges = true;
				}
			} else if (CurrentIntegrationData && CurrentIntegrationData.fields[field.id] !== value) {
				hasChanges = true;
			}
		});
	}

	if (enableDisableButton) {
		saveIntegrationButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

/** Initialize Function **/
function initIntegrationsTab() {
	// Fill available integrations in modal
	availableIntegrationsList.empty();
	SpecificationIntegrationsListData.forEach((integration) => {
		availableIntegrationsList.append($(createAvailableIntegrationCardElement(integration)));
	});

	// Fill existing integrations list
	FillIntegrationsList();
	initializeIntegrationTooltips();

	/**
	 *
	 * Event Handlers
	 *
	 **/

	// Open add integration modal
	addNewIntegrationButton.on("click", (event) => {
		event.preventDefault();
		ManageIntegrationType = "new";
		CurrentIntegrationData = null;
		SelectedIntegrationType = null;

		resetOrClearIntegrationManager();
		ShowAvailableIntegrations();
		addNewIntegrationModal.modal("show");
	});

	// Handle clicking on an available integration card
	availableIntegrationsList.on("click", ".available-integration-card", (event) => {
		event.preventDefault();
		const card = $(event.currentTarget);
		SelectedIntegrationType = card.data("integration-id");

		const integration = SpecificationIntegrationsListData.find((i) => i.id === SelectedIntegrationType);
		if (!integration) return;
		if (integration.disabledAt != null) return;

		resetOrClearIntegrationManager();
		fillIntegrationFields(integration);

		setTimeout(() => {
			backToIntegrationsListButton.removeClass("d-none");
		}, 300);
		ShowIntegrationManager();
	});

	// Handle back button in integration manager
	backToIntegrationsListButton.on("click", (event) => {
		event.preventDefault();

		const changes = CheckIntegrationTabHasChanges(false);
		if (changes.hasChanges) {
			new BootstrapConfirmDialog({
				title: "Unsaved Changes",
				message: "You have unsaved changes. Are you sure you want to go back?",
				confirmText: "Yes, go back",
				cancelText: "No, stay here",
				confirmButtonClass: "btn-danger",
			})
				.show()
				.then((confirmed) => {
					if (confirmed) {
						ShowAvailableIntegrations();
					}
				});
		} else {
			ShowAvailableIntegrations();
		}
	});

	// Handle edit integration
	integrationsListContainer.on("click", ".integration-card", (event) => {
		event.preventDefault();
		event.stopPropagation();

		// check if target was button or its icon
		if ($(event.target).closest(".dropdown").length != 0) {
			return;
		}

		const card = $(event.currentTarget);
		const integrationId = card.attr("data-item-id");

		ManageIntegrationType = "edit";
		CurrentIntegrationData = BusinessFullData.businessApp.integrations.find((integration) => integration.id === integrationId);

		if (!CurrentIntegrationData) return;

		SelectedIntegrationType = CurrentIntegrationData.type;

        const integration = SpecificationIntegrationsListData.find((i) => i.id === SelectedIntegrationType);
        if (!integration) return;

		resetOrClearIntegrationManager();

		// Fill the form
		$("#integrationFriendlyNameInput").val(CurrentIntegrationData.friendlyName);
		fillIntegrationFields(integration);

		// Fill existing values
		Object.entries(CurrentIntegrationData.fields).forEach(([fieldId, value]) => {
			const field = $(`#integration_${fieldId}`);
			if (field.length) {
				field.val(value);
			}
		});

		ShowIntegrationManager();

		setTimeout(() => {
			addNewIntegrationModal.modal("show");
		}, 150);
	});

	integrationsListContainer.on("click", ".integration-card span[button-type='delete-integration']", async (event) => {
		event.preventDefault();

		const button = $(event.currentTarget);
		const integrationId = button.attr("data-item-id");
		const integrationIndex = BusinessFullData.businessApp.integrations.findIndex(n => n.id === integrationId);
		if (integrationIndex === -1) return;
		const integrationData = BusinessFullData.businessApp.integrations[integrationIndex];
		if (!integrationData) return;
		const integrationCard = integrationsListContainer.find(`.integration-card[data-item-id="${integrationId}"]`);

		if (IsDeletingIntegrationTab) {
			AlertManager.createAlert({
				type: "warning",
				message: `A delete operation for integrations is already in progress. Please try again once the operation is complete.`,
				timeout: 6000,
			});
			return;
		}

		const confirmDialog = new BootstrapConfirmDialog({
			title: `Delete "${integrationData.friendlyName}" Integration`,
			message: `Are you sure you want to delete this integration?<br><br><b>Note:</b> You must remove any references to this integration (agent, knowledgebase, numbers, etc) and wait or cancel any ongoing call queues or conversations.`,
			confirmText: "Delete",
			confirmButtonClass: "btn-danger",
			modalClass: "modal-lg"
		});

		if (await confirmDialog.show()) {
			showHideButtonSpinnerWithDisableEnable(button, true);
			IsDeletingIntegrationTab = true;
			integrationCard.addClass("disabled");

			DeleteBusinessIntegration(
				integrationId,
				() => {

					BusinessFullData.businessApp.integrations.splice(integrationIndex, 1);

					integrationCard.parent().remove();

					if (BusinessFullData.businessApp.integrations.length === 0) {
						integrationsListContainer.append('<div class="col-12 text-center p-5"><p class="text-muted mb-0">No integrations found</p></div>');
					}

					AlertManager.createAlert({
						type: "success",
						message: `Integration "${integrationData.friendlyName}" deleted successfully.`,
						timeout: 6000,
					});
				},
				(errorResult) => {
					integrationCard.removeClass("disabled");

					var resultMessage = "Check console logs for more details.";
					if (errorResult && errorResult.message) resultMessage = errorResult.message;

					AlertManager.createAlert({
						type: "danger",
						message: "Error occured while deleting business integration.",
						resultMessage: resultMessage,
						timeout: 6000,
					});

					console.log("Error occured while deleting business integration: ", errorResult);
				}
			).always(() => {
				showHideButtonSpinnerWithDisableEnable(button, false);
				IsDeletingIntegrationTab = false;
			});
		}
	});

	// Handle form input changes
	integrationManagerContainer.on("input change", "input, select", (event) => {
		if (!SelectedIntegrationType) return;
		CheckIntegrationTabHasChanges(true);
	});

	// Handle save integration
	saveIntegrationButton.on("click", async (event) => {
		event.preventDefault();

		if (IsSavingIntegrationTab) return;

		const validationResult = ValidateIntegrationTab(false);
		if (!validationResult.validated) {
			AlertManager.createAlert({
				type: "danger",
				message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
				timeout: 6000,
			});
			return;
		}

		const changes = CheckIntegrationTabHasChanges(false);
		if (!changes.hasChanges) return;

		saveIntegrationButton.prop("disabled", true);
		const saveButtonSpinner = saveIntegrationButton.find(".spinner-border");
		saveButtonSpinner.removeClass("d-none");

		IsSavingIntegrationTab = true;

		const formData = new FormData();
		formData.append("changes", JSON.stringify(changes.changes));
		formData.append("postType", ManageIntegrationType);
		formData.append("currentIntegrationType", changes.changes.type);

		if (ManageIntegrationType === "edit") {
			formData.append("currentIntegrationId", CurrentIntegrationData.id);
		}

		SaveBusinessIntegration(
			formData,
			(saveResponse) => {
				if (ManageIntegrationType === "new") {
					BusinessFullData.businessApp.integrations.push(saveResponse.data);
				} else {
					const integrationIndex = BusinessFullData.businessApp.integrations.findIndex((i) => i.id === saveResponse.data.id);
					if (integrationIndex !== -1) {
						BusinessFullData.businessApp.integrations[integrationIndex] = saveResponse.data;
					}
				}

				FillIntegrationsList();
				addNewIntegrationModal.modal("hide");

				AlertManager.createAlert({
					type: "success",
					message: `Integration ${ManageIntegrationType === "new" ? "added" : "updated"} successfully.`,
					timeout: 6000,
				});

				saveIntegrationButton.prop("disabled", true);
				saveButtonSpinner.addClass("d-none");
				IsSavingIntegrationTab = false;
			},
			(saveError, isUnsuccessful) => {
				AlertManager.createAlert({
					type: "danger",
					message: "Error occurred while saving integration. Check browser console for logs.",
					timeout: 6000,
				});

				console.log("Error occurred while saving integration: ", saveError);

				saveIntegrationButton.prop("disabled", false);
				saveButtonSpinner.addClass("d-none");
				IsSavingIntegrationTab = false;
			},
		);
	});

	// Add modal hidden event handler to reset state
	addNewIntegrationModal.on("hidden.bs.modal", () => {
		ManageIntegrationType = null;
		CurrentIntegrationData = null;
		SelectedIntegrationType = null;

		ShowAvailableIntegrations();
	});

	// Handle Encrypted Fields
	integrationManagerContainer.on("keydown", 'input[type="password"]', (event) => {
		event.stopPropagation();

		const currentElement = $(event.currentTarget);
		const realTypeAttr = currentElement.attr("real-type");

		currentElement.attr("type", realTypeAttr);

		currentElement.val("");
	});
}
