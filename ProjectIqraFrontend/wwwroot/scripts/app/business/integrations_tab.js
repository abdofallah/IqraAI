/** Dynamic Variables **/
let CurrentIntegrationData = null;
let ManageIntegrationType = null; // new or edit
let SelectedIntegrationType = null;
let IsSavingIntegrationTab = false;
let IntegrationModalCloseConfirmed = false;
let IsLeavingForOAuthConnect = false;
let IsDeletingIntegrationTab = false;
let IntegrationsOAuthStatusData = {};
let IntegrationsOAuthStatusPollInterval = null;
let IsIntegrationsOAuthStatusTabShown = false;
let IntegrationsOAuthRedirectUri = null;

// --- Change Helpers ---
const IntegrationFieldValue = { kind: "text", strict: true, isEmpty: (value) => value === undefined || value === "" };

const OAuthReconnectFieldsChanges = ChangesHelper.define({
	fields: { kind: "map", keys: (context) => context.integration.fields.filter((field) => field.requiresOAuthReconnect).map((field) => field.id), foldKeyCase: true, values: IntegrationFieldValue },
});

const BusinessIntegrationChanges = ChangesHelper.define({
	type: { kind: "text", compare: false },
	friendlyName: "text",
	fields: { kind: "map", keys: () => GetIntegrationSpecFields().map((field) => field.id), foldKeyCase: true, values: IntegrationFieldValue },
});

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

function FetchBusinessIntegrationsOAuthStatus(onSuccess, onError) {
	return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/integrations/oauth/status`,
		method: "GET",
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

function FetchBusinessIntegrationOAuthRedirectUri(onSuccess, onError) {
	return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/integrations/oauth/redirect-uri`,
		method: "GET",
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

function TestBusinessIntegrationOAuthConnection(integrationId, onSuccess, onError) {
	return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/integrations/${encodeURIComponent(integrationId)}/oauth/test`,
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
function ShowIntegrationSpecLogo(image, integrationType, onRenewed) {
	const integrationSpec = SpecificationIntegrationsListData.find((item) => item.id === integrationType);
	if (!integrationSpec || !integrationSpec.logoUrl) return;

	SignedFileHelper.showImage(image, {
		file: integrationSpec.logoUrl,
		fetchFile: () => SignedFileHelper.fetchData(`/app/specification/integrations/${integrationType}/signed-file/Logo`),
		onRenewed: (file) => {
			integrationSpec.logoUrl = file;
			if (onRenewed) onRenewed(file);
		},
		fallbackSrc: "/img/picture_placeholder_light.png",
	});
}

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

	const integrationSpec = SpecificationIntegrationsListData.find((i) => i.id === integration.type);

	const cardElement = $(createIqraCardElement({
		id: integration.id,
		type: 'integration',
		visualHtml: `<img src="${integration.logoUrl?.url}" alt="${integration.friendlyName} logo">`,
		titleHtml: integration.friendlyName,
		descriptionHtml: integrationSpec && IsOAuthIntegrationSpec(integrationSpec) ? CreateOAuthStatusBadgeElement(GetOAuthIntegrationStatus(integration)).prop("outerHTML") : "",
		actionDropdownHtml: actionDropdownHtml,
	}));

	ShowIntegrationSpecLogo(cardElement.find("img").first(), integration.type, (file) => {
		integration.logoUrl = file;
	});
	return cardElement;
}

function createAvailableIntegrationCardElement(integration) {
	const typesBadges = integration.type.map((type) => `<span class="badge border me-1 mb-1">${type}</span>`).join("");

	return `
        <div class="col-lg-6 col-md-6 col-12 mb-3">
            <div class="card h-100 ${(integration.disabledAt == null ? "" : "disabled")} available-integration-card" data-integration-id="${integration.id}">
                <div class="card-body">
					<h5 class="card-title">${integration.name}${(integration.disabledAt == null ? "" : " | <span class='text-danger'>Disabled</span>") }</h5>

					<img class="px-2 my-3" src="${integration.logoUrl?.url}">

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
	} else if (field.isEncrypted) {
		fieldHtml += `
            <div class="input-group">
                <span class="input-group-text d-none" data-type="savedLock" title="A value is saved">
                    <i class="fa-regular fa-lock"></i>
                </span>
                <input type="password" class="form-control" data-secret="true" autocomplete="new-password"
                       id="integration_${field.id}"
                       placeholder="${field.placeholder || `Enter ${field.name}`}"
                       value="${field.defaultValue || ""}"
                       ${field.required ? "required" : ""}>
                <button type="button" class="btn btn-secondary" button-type="toggleSecretVisibility" title="Show or hide the value">
                    <i class="fa-regular fa-eye-slash"></i>
                </button>
            </div>
            ${field.required ? "" : `<a href="#" class="small d-none" button-type="clearSavedIntegrationSecret" data-field-id="${field.id}">Clear saved value</a>`}
        `;
	} else {
		fieldHtml += `
            <input type="${field.type}" class="form-control"
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
			if (savedValue !== undefined && savedValue !== null) {
				$(`#integration_${field.id}`).val(savedValue);
			}
		}

		if (field.isEncrypted && field.type !== "select") {
			UpdateIntegrationSecretState(field);
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

	renderOAuthConnectElement(integration);
	CheckIntegrationTabHasChanges(false);

	// Initialize tooltips
	initializeIntegrationTooltips();
}

function IntegrationSecretHasSavedValue(field) {
	return ManageIntegrationType === "edit" && !!CurrentIntegrationData && Object.prototype.hasOwnProperty.call(CurrentIntegrationData.fields, field.id);
}

function IntegrationSecretKeepsSavedValue(field) {
	const box = $(`#integration_${field.id}`);
	return IntegrationSecretHasSavedValue(field) && box.val().trim() === "" && box.attr("data-clear-saved") !== "true";
}

function ReadIntegrationFieldValue(field) {
	if (field.isEncrypted && field.type !== "select" && IntegrationSecretKeepsSavedValue(field)) return null;
	return $(`#integration_${field.id}`).val().trim();
}

function UpdateIntegrationSecretState(field) {
	const box = $(`#integration_${field.id}`);
	const container = box.closest(".mb-3");
	const hasSavedValue = IntegrationSecretHasSavedValue(field);

	if (!hasSavedValue) box.removeAttr("data-clear-saved");

	const clearSaved = box.attr("data-clear-saved") === "true";
	const isVisible = box.attr("data-value-visible") === "true";

	box.attr("type", isVisible ? "text" : "password");
	box.attr("placeholder", hasSavedValue ? (clearSaved ? "Will be cleared when you save" : "Saved. Type to replace it.") : field.placeholder || `Enter ${field.name}`);

	container.find('[data-type="savedLock"]').toggleClass("d-none", !hasSavedValue || clearSaved);
	container.find('[button-type="clearSavedIntegrationSecret"]').toggleClass("d-none", !hasSavedValue).text(clearSaved ? "Keep saved value" : "Clear saved value");
	container.find('[button-type="toggleSecretVisibility"] i').toggleClass("fa-eye", isVisible).toggleClass("fa-eye-slash", !isVisible);
}

function GetIntegrationSpecField(fieldId) {
	return GetIntegrationSpecFields().find((field) => field.id === fieldId);
}

function IsOAuthIntegrationSpec(integration) {
	return integration.isOAuth === true;
}

function GetOAuthIntegrationStatus(integrationData) {
	const status = integrationData ? IntegrationsOAuthStatusData[integrationData.id] : null;
	const checkedAt = (status && status.checkedAt) || null;
	const hasConnection = !!(status && status.hasConnection);

	if (status && status.status === "Failed") {
		return { state: "failed", label: "Connection failed", message: status.message || "", checkedAt: checkedAt, hasConnection: hasConnection };
	}
	if (status && status.status === "Connected") {
		return { state: "connected", label: "Connected", message: "", checkedAt: checkedAt, hasConnection: hasConnection };
	}
	return { state: "not_connected", label: "Not connected", message: "", checkedAt: checkedAt, hasConnection: hasConnection };
}

function RefreshIntegrationsOAuthStatus() {
	return FetchBusinessIntegrationsOAuthStatus(
		(statusResponse) => {
			IntegrationsOAuthStatusData = {};
			(statusResponse.data || []).forEach((status) => {
				IntegrationsOAuthStatusData[status.integrationId] = status;
			});

			RenderIntegrationsOAuthStatus();
		},
		(statusError) => {
			console.log("Error occurred while loading the OAuth connection statuses: ", statusError);
		}
	);
}

function LoadIntegrationsOAuthRedirectUri() {
	return FetchBusinessIntegrationOAuthRedirectUri(
		(redirectUriResponse) => {
			IntegrationsOAuthRedirectUri = redirectUriResponse.data;

			const integrationSpec = SpecificationIntegrationsListData.find((i) => i.id === SelectedIntegrationType);
			if (integrationSpec && IsOAuthIntegrationSpec(integrationSpec) && !integrationManagerContainer.hasClass("d-none")) {
				renderOAuthConnectElement(integrationSpec);
			}
		},
		(redirectUriError) => {
			console.log("Error occurred while loading the OAuth redirect URI: ", redirectUriError);
		}
	);
}

function RenderIntegrationsOAuthStatus() {
	FillIntegrationsList();

	if (ManageIntegrationType === "edit" && CurrentIntegrationData) {
		const integrationSpec = SpecificationIntegrationsListData.find((i) => i.id === CurrentIntegrationData.type);
		if (integrationSpec && IsOAuthIntegrationSpec(integrationSpec)) {
			renderOAuthConnectElement(integrationSpec);
			CheckIntegrationTabHasChanges(false);
		}
	}
}

function UpdateIntegrationsOAuthStatusPolling() {
	const shouldPoll = IsIntegrationsOAuthStatusTabShown && document.visibilityState === "visible";

	if (shouldPoll && !IntegrationsOAuthStatusPollInterval) {
		RefreshIntegrationsOAuthStatus();
		IntegrationsOAuthStatusPollInterval = setInterval(RefreshIntegrationsOAuthStatus, 30000);
	} else if (!shouldPoll && IntegrationsOAuthStatusPollInterval) {
		clearInterval(IntegrationsOAuthStatusPollInterval);
		IntegrationsOAuthStatusPollInterval = null;
	}
}

function CreateOAuthStatusBadgeElement(status) {
	const badgeClass = status.state === "connected" ? "bg-success" : (status.state === "failed" ? "bg-danger" : "bg-warning text-dark");
	return $("<span>", { class: `badge ${badgeClass}` }).text(status.label);
}

function IsIntegrationOAuthConnected(integrationData) {
	return GetOAuthIntegrationStatus(integrationData).state === "connected";
}

function GetOAuthConnectUrl(integrationId) {
	return `/app/user/business/${CurrentBusinessId}/integrations/${encodeURIComponent(integrationId)}/oauth/connect`;
}

function OpenOAuthConnectWindow() {
	return window.open("", "iqraIntegrationOAuthConnect", "width=600,height=720");
}

function StartOAuthConnect(connectWindow, integrationId) {
	const connectUrl = GetOAuthConnectUrl(integrationId);

	if (connectWindow && !connectWindow.closed) {
		connectWindow.location.href = connectUrl;
		connectWindow.focus();
	} else {
		IsLeavingForOAuthConnect = true;
		window.location.href = connectUrl;
	}
}

function HasOAuthBreakingChanges(integration, changes) {
	if (ManageIntegrationType !== "edit" || !CurrentIntegrationData) return false;

	return OAuthReconnectFieldsChanges.compare(CurrentIntegrationData, { fields: changes.fields }, { context: { integration: integration } }).hasChanges;
}

function UpdateSaveIntegrationButtonLabel(changes) {
	const integration = SpecificationIntegrationsListData.find((i) => i.id === SelectedIntegrationType);

	let label = "Save Integration";
	if (integration && IsOAuthIntegrationSpec(integration)) {
		if (ManageIntegrationType !== "edit" || !IsIntegrationOAuthConnected(CurrentIntegrationData)) {
			label = "Save & Connect";
		} else if (HasOAuthBreakingChanges(integration, changes)) {
			label = "Save & Reconnect";
		}
	}

	saveIntegrationButton.text(label);
}

function renderOAuthConnectElement(integration) {
	integrationManagerContainer.find("#oauthConnectContainer").remove();

	if (!IsOAuthIntegrationSpec(integration)) return;

	const isEditing = ManageIntegrationType === "edit" && !!CurrentIntegrationData;
	const status = isEditing ? GetOAuthIntegrationStatus(CurrentIntegrationData) : null;
	const hasStoredConnection = isEditing && status.hasConnection;

	const connectElement = $(`
        <div id="oauthConnectContainer" class="mt-3 mb-0">
            ${IntegrationsOAuthRedirectUri ? `
            <div class="mb-3">
                <label class="form-label" for="oauthRedirectUriInput">Redirect URI to register at the provider</label>
                <div class="input-group">
                    <input type="text" class="form-control" id="oauthRedirectUriInput" readonly>
                    <button type="button" class="btn btn-outline-secondary" id="oauthCopyRedirectUriButton">
                        <i class="fa-regular fa-copy"></i>
                    </button>
                </div>
            </div>` : ""}
            ${isEditing ? `
            <div class="d-flex align-items-center flex-wrap gap-2">
                <span class="oauth-status-badge"></span>
                ${hasStoredConnection ? `
                <button type="button" class="btn btn-outline-secondary btn-sm" id="oauthTestConnectionButton">
                    <i class="fa-solid fa-plug-circle-check me-2"></i>Test connection
                </button>` : ""}
                <a class="btn btn-outline-primary btn-sm" id="oauthConnectButton">
                    <i class="fa-solid fa-link me-2"></i>${hasStoredConnection ? "Reconnect" : "Connect"}
                </a>
            </div>
            <small class="d-block text-muted mt-2 oauth-status-details"></small>` : ""}
        </div>
    `);
	connectElement.find("#oauthRedirectUriInput").val(IntegrationsOAuthRedirectUri);

	if (isEditing) {
		connectElement.find(".oauth-status-badge").replaceWith(CreateOAuthStatusBadgeElement(status));
		connectElement.find("#oauthConnectButton").attr("href", GetOAuthConnectUrl(CurrentIntegrationData.id));

		const statusDetails = [];
		if (status.message) statusDetails.push(status.message);
		if (status.checkedAt) statusDetails.push(`Last checked ${new Date(status.checkedAt).toLocaleString()}`);
		connectElement.find(".oauth-status-details").text(statusDetails.join(". "));
	}

	if (connectElement.children().length === 0) return;

	integrationFieldsContainer.after(connectElement);
}

function ShowOAuthConnectResult(success, reason) {
	if (success) {
		AlertManager.createAlert({
			type: "success",
			message: "Integration connected successfully via OAuth.",
			timeout: 6000,
		});
	} else {
		const alertElement = AlertManager.createAlert({
			type: "danger",
			message: "OAuth connection failed. Please verify your Client ID/Secret and the registered callback URL, then try again.",
			timeout: 8000,
		});

		if (reason) {
			$("<code>", { class: "bg-dark p-2 rounded border mt-2", style: "display: block;" })
				.text(reason)
				.insertBefore(alertElement.find(".btn-close"));
		}
	}
}

function ShowOAuthConnectResultAlert() {
	const urlParams = new URLSearchParams(window.location.search);
	const oauthStatus = urlParams.get("oauth");
	if (!oauthStatus) return;

	ShowOAuthConnectResult(oauthStatus === "success", urlParams.get("reason"));

	urlParams.delete("oauth");
	urlParams.delete("reason");
	const newQuery = urlParams.toString();
	history.replaceState(history.state, document.title, window.location.pathname + (newQuery ? `?${newQuery}` : "") + window.location.hash);
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
			const keepsSavedValue = field.isEncrypted && field.type !== "select" && IntegrationSecretKeepsSavedValue(field);
			if (!value && !keepsSavedValue) {
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

function GetIntegrationSpecFields() {
	const integration = SpecificationIntegrationsListData.find((i) => i.id === SelectedIntegrationType);
	return integration ? integration.fields : [];
}

function CreateIntegrationSpecDefaultsObject() {
	const fields = {};
	GetIntegrationSpecFields().forEach((field) => {
		fields[field.id] = field.type === "select" ? field.options.find((opt) => opt.isDefault)?.key || "" : field.defaultValue || "";
	});
	return { friendlyName: "", fields: fields };
}

function CheckIntegrationTabHasChanges(enableDisableButton = true) {
	const fields = {};
	GetIntegrationSpecFields().forEach((field) => {
		fields[field.id] = ReadIntegrationFieldValue(field);
	});
	const current = {
		type: SelectedIntegrationType,
		friendlyName: $("#integrationFriendlyNameInput").val().trim(),
		fields: fields,
	};

	const saved = ManageIntegrationType === "new" ? CreateIntegrationSpecDefaultsObject() : CurrentIntegrationData;
	const result = BusinessIntegrationChanges.compare(saved || current, current, { build: true });

	if (enableDisableButton) {
		saveIntegrationButton.prop("disabled", !result.hasChanges);
	}

	UpdateSaveIntegrationButtonLabel(result.changes);

	return {
		hasChanges: result.hasChanges,
		changes: result.changes,
	};
}

/** Initialize Function **/
function initIntegrationsTab() {
	// Fill available integrations in modal
	availableIntegrationsList.empty();
	SpecificationIntegrationsListData.forEach((integration) => {
		const availableIntegrationCard = $(createAvailableIntegrationCardElement(integration));
		availableIntegrationsList.append(availableIntegrationCard);
		ShowIntegrationSpecLogo(availableIntegrationCard.find("img").first(), integration.id);
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

		const integrationSpec = SpecificationIntegrationsListData.find((i) => i.id === SelectedIntegrationType);
		const isOAuthIntegration = !!integrationSpec && IsOAuthIntegrationSpec(integrationSpec);

		const breaksOAuthConnection = isOAuthIntegration && ManageIntegrationType === "edit" && IsIntegrationOAuthConnected(CurrentIntegrationData) && HasOAuthBreakingChanges(integrationSpec, changes.changes);
		const connectAfterSave = isOAuthIntegration && (ManageIntegrationType !== "edit" || !IsIntegrationOAuthConnected(CurrentIntegrationData) || breaksOAuthConnection);

		if (breaksOAuthConnection) {
			const confirmDialog = new BootstrapConfirmDialog({
				title: "Reconnect Required",
				message: "These changes break the current OAuth connection. After saving you will be sent to the provider to connect again.",
				confirmText: "Save & Reconnect",
				confirmButtonClass: "btn-warning",
			});

			if (!(await confirmDialog.show())) return;
		}

		const connectWindow = connectAfterSave ? OpenOAuthConnectWindow() : null;

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
				IntegrationModalCloseConfirmed = true;
				addNewIntegrationModal.modal("hide");

				AlertManager.createAlert({
					type: "success",
					message: `Integration ${ManageIntegrationType === "new" ? "added" : "updated"} successfully.`,
					timeout: 6000,
				});

				saveIntegrationButton.prop("disabled", true);
				saveButtonSpinner.addClass("d-none");
				IsSavingIntegrationTab = false;

				if (connectAfterSave) {
					StartOAuthConnect(connectWindow, saveResponse.data.id);
				}
			},
			(saveError, isUnsuccessful) => {
				if (connectWindow && !connectWindow.closed) {
					connectWindow.close();
				}

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

	addNewIntegrationModal.on("hide.bs.modal", (event) => {
		if (IntegrationModalCloseConfirmed || ManageIntegrationType == null || !integrationManagerContainer.hasClass("show")) {
			IntegrationModalCloseConfirmed = false;
			return;
		}
		if (!CheckIntegrationTabHasChanges(false).hasChanges) return;

		event.preventDefault();

		const confirmDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: "You have unsaved changes in the integration. Are you sure you want to discard these changes and close it?",
			confirmText: "Discard",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "modal-lg",
		});
		confirmDialog.show().then((confirmResult) => {
			if (!confirmResult) return;

			IntegrationModalCloseConfirmed = true;
			addNewIntegrationModal.modal("hide");
		});

		return false;
	});

	$(window).on("beforeunload", (event) => {
		if (IsLeavingForOAuthConnect || ManageIntegrationType == null || !integrationManagerContainer.hasClass("show")) return;
		if (!IsSavingIntegrationTab && !CheckIntegrationTabHasChanges(false).hasChanges) return;

		askBeforePageLeave(event);
	});

	// Add modal hidden event handler to reset state
	addNewIntegrationModal.on("hidden.bs.modal", () => {
		ManageIntegrationType = null;
		CurrentIntegrationData = null;
		SelectedIntegrationType = null;

		ShowAvailableIntegrations();
	});

	integrationManagerContainer.on("click", "#oauthConnectButton", (event) => {
		event.preventDefault();

		if (!CurrentIntegrationData) return;

		if (CheckIntegrationTabHasChanges(false).hasChanges) {
			AlertManager.createAlert({
				type: "warning",
				message: "Save or discard your changes before connecting.",
				timeout: 6000,
			});
			return;
		}

		StartOAuthConnect(OpenOAuthConnectWindow(), CurrentIntegrationData.id);
	});

	window.addEventListener("message", (event) => {
		if (event.origin !== window.location.origin || !event.data || event.data.type !== "iqra-integration-oauth-result") return;

		ShowOAuthConnectResult(event.data.success === true, event.data.reason);
		RefreshIntegrationsOAuthStatus();
	});

	integrationManagerContainer.on("click", "#oauthTestConnectionButton", (event) => {
		event.preventDefault();

		if (!CurrentIntegrationData) return;

		const button = $(event.currentTarget);

		const applyTestedStatus = (testedStatus) => {
			if (!testedStatus || !testedStatus.integrationId) return;

			IntegrationsOAuthStatusData[testedStatus.integrationId] = testedStatus;
			RenderIntegrationsOAuthStatus();
		};

		showHideButtonSpinnerWithDisableEnable(button, true);

		TestBusinessIntegrationOAuthConnection(
			CurrentIntegrationData.id,
			(testResponse) => {
				applyTestedStatus(testResponse.data);

				AlertManager.createAlert({
					type: "success",
					message: "The OAuth connection works.",
					timeout: 6000,
				});
			},
			(testError, isUnsuccessful) => {
				if (isUnsuccessful) applyTestedStatus(testError.data);

				AlertManager.createAlert({
					type: "danger",
					message: "The OAuth connection test failed. See the connection status for details.",
					timeout: 6000,
				});

				console.log("Error occurred while testing the OAuth connection: ", testError);
			}
		).always(() => {
			showHideButtonSpinnerWithDisableEnable(button, false);
		});
	});

	// Handle Encrypted Fields
	integrationManagerContainer.on("input", '[data-secret="true"]', (event) => {
		const box = $(event.currentTarget);
		const field = GetIntegrationSpecField(box.attr("id").replace("integration_", ""));
		if (!field) return;

		if (box.val().trim() !== "") box.removeAttr("data-clear-saved");

		UpdateIntegrationSecretState(field);
	});

	integrationManagerContainer.on("click", '[button-type="toggleSecretVisibility"]', (event) => {
		event.preventDefault();

		const box = $(event.currentTarget).closest(".input-group").find('[data-secret="true"]');
		const field = GetIntegrationSpecField(box.attr("id").replace("integration_", ""));
		if (!field) return;

		box.attr("data-value-visible", box.attr("data-value-visible") === "true" ? "false" : "true");

		UpdateIntegrationSecretState(field);
	});

	integrationManagerContainer.on("click", '[button-type="clearSavedIntegrationSecret"]', (event) => {
		event.preventDefault();

		const field = GetIntegrationSpecField($(event.currentTarget).attr("data-field-id"));
		if (!field) return;

		const box = $(`#integration_${field.id}`);
		box.attr("data-clear-saved", box.attr("data-clear-saved") === "true" ? "false" : "true");
		box.val("");

		UpdateIntegrationSecretState(field);
		CheckIntegrationTabHasChanges(true);
	});

	$(document).on("tabShown", (event, tabData) => {
		IsIntegrationsOAuthStatusTabShown = !!tabData && (tabData.tabId === "integrations-tab" || tabData.tabId === "scripts-tab");
		UpdateIntegrationsOAuthStatusPolling();
	});
	document.addEventListener("visibilitychange", UpdateIntegrationsOAuthStatusPolling);

	integrationManagerContainer.on("click", "#oauthCopyRedirectUriButton", (event) => {
		event.preventDefault();

		navigator.clipboard.writeText(integrationManagerContainer.find("#oauthRedirectUriInput").val()).then(() => {
			AlertManager.createAlert({
				type: "success",
				message: "Copied the redirect URI to clipboard.",
				timeout: 2000,
			});
		});
	});

	LoadIntegrationsOAuthRedirectUri();
	RefreshIntegrationsOAuthStatus();
	ShowOAuthConnectResultAlert();
}
