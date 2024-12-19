/** Dynamic Variables **/
let CurrentIntegrationData = null;
const AvailableIntegrationsData = [
	{
		id: "whatsapp",
		name: "WhatsApp",
		description: "Connect your WhatsApp Business account",
		logo: "/img/temp/whatsapp.png",
		type: ["messaging", "customer-support"],
		fields: [
			{
				id: "api_key",
				name: "API Key",
				type: "text",
				tooltip: "Find this in your WhatsApp Business dashboard",
				required: true,
				isEncrypted: true,
			},
			{
				id: "phone_number",
				name: "Phone Number",
				type: "text",
				required: true,
				isEncrypted: false,
			},
		],
		help: {
			text: "How to get WhatsApp Business API key?",
			uri: "https://business.whatsapp.com/products/business-platform",
		},
	},
	{
		id: "telegram",
		name: "Telegram",
		description: "Connect your Telegram bot",
		logo: "/img/temp/telegram.png",
		type: ["messaging"],
		fields: [
			{
				id: "bot_token",
				name: "Bot Token",
				type: "text",
				tooltip: "Get this from BotFather",
				required: true,
				isEncrypted: true,
			},
		],
		help: {
			text: "How to create a Telegram bot?",
			uri: "https://core.telegram.org/bots#how-do-i-create-a-bot",
		},
	},
	{
		id: "stripe",
		name: "Stripe",
		description: "Accept payments through Stripe",
		logo: "/img/temp/stripe.png",
		type: ["payment"],
		fields: [
			{
				id: "mode",
				name: "Environment",
				type: "select",
				options: [
					{ key: "test", value: "Test Mode" },
					{ key: "live", value: "Live Mode" },
				],
				required: true,
				isEncrypted: false,
			},
			{
				id: "secret_key",
				name: "Secret Key",
				type: "text",
				tooltip: "Find this in your Stripe dashboard",
				required: true,
				isEncrypted: true,
			},
		],
		help: {
			text: "Where to find Stripe API keys?",
			uri: "https://stripe.com/docs/keys",
		},
	},
];
let ManageIntegrationType = null; // new or edit
let SelectedIntegrationType = null;
let IsSavingIntegrationTab = false;

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

/** API Functions **/
function SaveBusinessIntegration(formData, onSuccess, onError) {
	$.ajax({
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

/** Core Functions **/
function createIntegrationCardElement(integration) {
	return `
        <div class="col-lg-4 col-md-6 col-12">
            <div class="business-card d-flex flex-column align-items-start justify-content-center" data-integration-id="${integration.id}">
                <div class="d-flex flex-row align-items-center justify-content-start">
                    <img src="${integration.logo}" class="me-3">
                    <div>
                        <h4 class="mb-1">${integration.friendlyName}</h4>
                        <p class="mb-0 text-muted">${integration.name}</p>
                    </div>
                </div>
            </div>
        </div>
    `;
}

function createAvailableIntegrationCardElement(integration) {
	const typesBadges = integration.type.map((type) => `<span class="badge border me-1">${type}</span>`).join("");

	return `
        <div class="col-lg-4 col-md-6 col-12 mb-3">
            <div class="card h-100 cursor-pointer available-integration-card" data-integration-id="${integration.id}">
                <div class="card-body">
                    <img class="px-2 mb-3" src="${integration.logo}">
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
                <span>${field.name}</span>
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
                <option value="">Select ${field.name}</option>
                ${field.options.map((opt) => `<option value="${opt.key}">${opt.value}</option>`).join("")}
            </select>
        `;
	} else {
		fieldHtml += `
            <input type="${field.type}" class="form-control" 
                   id="integration_${field.id}" 
                   placeholder="Enter ${field.name}"
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
}

function fillIntegrationFields(integrationType) {
	resetOrClearIntegrationManager();

	const integration = AvailableIntegrationsData.find((i) => i.id === integrationType);
	if (!integration) return;

	// Add fields
	integration.fields.forEach((field) => {
		integrationFieldsContainer.append(createIntegrationFieldElement(field));
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
	initializeTooltips();
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
		const integrationDetails = AvailableIntegrationsData.find((i) => i.id === integration.type);
		if (integrationDetails) {
			integration.name = integrationDetails.name;
			integration.logo = integrationDetails.logo;
			integrationsListContainer.append($(createIntegrationCardElement(integration)));
		}
	});
}

function ShowAvailableIntegrations() {
	integrationManagerContainer.removeClass("show");
	setTimeout(() => {
		integrationManagerContainer.addClass("d-none");
		backToIntegrationsListButton.addClass("d-none");
		saveIntegrationButton.addClass("d-none");
		availableIntegrationsContainer.removeClass("d-none");

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
		backToIntegrationsListButton.removeClass("d-none");
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
	const integration = AvailableIntegrationsData.find((i) => i.id === SelectedIntegrationType);
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
	const integration = AvailableIntegrationsData.find((i) => i.id === SelectedIntegrationType);
	if (integration) {
		integration.fields.forEach((field) => {
			const value = $(`#integration_${field.id}`).val().trim();
			changes.fields[field.id] = value;

			if (ManageIntegrationType === "new" && value) {
				hasChanges = true;
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

function initializeTooltips() {
	const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
	tooltipTriggerList.map(function (tooltipTriggerEl) {
		return new bootstrap.Tooltip(tooltipTriggerEl);
	});
}

/** Initialize Function **/
function initIntegrationsTab() {
	// Fill available integrations in modal
	availableIntegrationsList.empty();
	AvailableIntegrationsData.forEach((integration) => {
		availableIntegrationsList.append($(createAvailableIntegrationCardElement(integration)));
	});

	// Initialize tooltip
	initializeTooltips();

	// Fill existing integrations list
	FillIntegrationsList();

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

		ShowAvailableIntegrations();
		addNewIntegrationModal.modal("show");
	});

	// Handle clicking on an available integration card
	availableIntegrationsList.on("click", ".available-integration-card", (event) => {
		event.preventDefault();
		const card = $(event.currentTarget);
		SelectedIntegrationType = card.data("integration-id");

		fillIntegrationFields(SelectedIntegrationType);
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
	integrationsListContainer.on("click", ".business-card", (event) => {
		event.preventDefault();
		const card = $(event.currentTarget);
		const integrationId = card.data("integration-id");

		ManageIntegrationType = "edit";
		CurrentIntegrationData = BusinessFullData.businessApp.integrations.find((integration) => integration.id === integrationId);

		if (!CurrentIntegrationData) return;

		SelectedIntegrationType = CurrentIntegrationData.type;

		// Fill the form
		$("#integrationFriendlyNameInput").val(CurrentIntegrationData.friendlyName);
		fillIntegrationFields(SelectedIntegrationType);

		// Fill existing values
		Object.entries(CurrentIntegrationData.fields).forEach(([fieldId, value]) => {
			const field = $(`#integration_${fieldId}`);
			if (field.length) {
				field.val(value);
			}
		});

		ShowIntegrationManager();
		addNewIntegrationModal.modal("show");
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

		if (ManageIntegrationType === "edit") {
			formData.append("existingIntegrationId", CurrentIntegrationData.id);
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
		resetOrClearIntegrationManager();
		ShowAvailableIntegrations();
		ManageIntegrationType = null;
		CurrentIntegrationData = null;
		SelectedIntegrationType = null;
	});
}
