/** Dynamic Variables **/
let CurrentManageAgentData = null;
let ManageAgentType = null; // new or edit

// Integration related states
let CurrentAgentIntegrationsSTT = [];
let CurrentAgentIntegrationsLLM = [];
let CurrentAgentIntegrationsTTS = [];

// Integration Configuration State
let CurrentAgentConfigurationIntegration = null;
let CurrentAgentConfigurationIntegrationType = null;
let CurrentAgentConfigurationFields = null;
let CurrentAgentConfigurationValues = {};
let CurrentAgentConfigurationType = null;

// Cache related states
let CurrentAgentCacheMessages = [];
let CurrentAgentCacheAudios = [];

let manageAgentsLanguageDropdown = null;

// Multi Language
let CurrentAgentGeneralNameMultiLangData = {};
let CurrentAgentGeneralDescriptionMultiLangData = {};

let CurrentAgentPersonalityNameMultiLangData = {};
let CurrentAgentPersonalityRoleMultiLangData = {};
let CurrentAgentPersonalityCapabilitiesMultiLangData = {};
let CurrentAgentPersonalityEthicsMultiLangData = {};
let CurrentAgentPersonalityToneMultiLangData = {};

let CurrentAgentUtterancesGreetingMessageMultiLangData = {};
let CurrentAgentUtterancesPhrasesBeforeReplyMultiLangData = {};

/** Element Variables **/

const agentIconPicker = new EmojiPicker({
	trigger: [
		{
			selector: "#editAgentIconButton",
			insertInto: "#editAgentIconButton",
		},
	],
	closeButton: true,
	closeOnInsert: true,
});

const agentstooltipTriggerList = document.querySelectorAll('#agents-tab [data-bs-toggle="tooltip"]');
[...agentstooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

// Agent Tab
const agentTab = $("#agents-tab");

const addNewAgentButton = agentTab.find("#addNewAgentButton");

// Agent - List Tab
const agentsListTab = agentTab.find("#agentsListTab");

// Agent - Manager Tab
const agentsManagerHeader = agentTab.find("#agents-manager-header");

const switchBackToAgentsTab = agentsManagerHeader.find("#switchBackToAgentsTab");
const currentAgentName = agentsManagerHeader.find("#currentAgentName");

const agentsManagerTab = agentTab.find("#agentsManagerTab");

// SUB | Script Tab
const addNewAgentScriptButton = agentTab.find("#addNewAgentScriptButton");

// SUB | Script - List Tab
const agentScriptsListTab = agentTab.find("#agentScriptsListTab");

// SUB | Script - Manager Tab
const switchBackToAgentsScriptManagerTab = agentTab.find("#switchBackToAgentsScriptManagerTab");
const currentAgentScriptName = agentTab.find("#currentAgentScriptName");

const agentScriptsManagerTab = agentTab.find("#agentScriptsManagerTab");

const addAgentScriptConditionValueButton = agentScriptsManagerTab.find("#addAgentScriptConditionValue");
const editAgentScriptConditionValueInputs = agentScriptsManagerTab.find("#editAgentScriptConditionValueInputs");

const addAgentScriptConversationMessageButton = agentScriptsManagerTab.find("#addAgentScriptConversationMessage");

const agentScriptConversationMessages = agentScriptsManagerTab.find(".agentScriptConversationMessages");

const noMessagesInConversationAlert = agentScriptsManagerTab.find(".no-messages-conversation");

// SUB | Integrations Tab
const agentIntegrationsTab = $("#agents-manager-integrations");
const sttIntegrationsList = agentIntegrationsTab.find("#sttIntegrationsList");
const llmIntegrationsList = agentIntegrationsTab.find("#llmIntegrationsList");
const ttsIntegrationsList = agentIntegrationsTab.find("#ttsIntegrationsList");
const addSTTIntegrationButton = agentIntegrationsTab.find("#addSTTIntegration");
const addLLMIntegrationButton = agentIntegrationsTab.find("#addLLMIntegration");
const addTTSIntegrationButton = agentIntegrationsTab.find("#addTTSIntegration");

const integrationConfigurationModal = $("#integrationConfigurationModal");
const integrationConfigurationFieldsContainer = integrationConfigurationModal.find("#integrationConfigurationFieldsContainer");
const saveIntegrationConfigButton = integrationConfigurationModal.find("#saveIntegrationConfigButton");

// SUB | Cache Tab
const agentCacheTab = $("#agents-manager-cache");
const messageCacheGroupsList = agentCacheTab.find("#messageCacheGroupsList");
const audioCacheGroupsList = agentCacheTab.find("#audioCacheGroupsList");
const addMessageCacheGroupButton = agentCacheTab.find("#addMessageCacheGroup");
const addAudioCacheGroupButton = agentCacheTab.find("#addAudioCacheGroup");

// SUB | Settings Tab
const editAgentBackgroundAudioSelect = agentTab.find("#editAgentBackgroundAudioSelect");

/** Functions **/

function showAgentManagerTab() {
	agentsListTab.removeClass("show");
	setTimeout(() => {
		agentsListTab.addClass("d-none");

		agentsManagerTab.removeClass("d-none");
		agentsManagerHeader.removeClass("d-none");
		setTimeout(() => {
			agentsManagerTab.addClass("show");
			agentsManagerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function showAgentListTab() {
	agentsManagerTab.removeClass("show");
	agentsManagerHeader.removeClass("show");
	setTimeout(() => {
		agentsManagerTab.addClass("d-none");
		agentsManagerHeader.addClass("d-none");

		agentsListTab.removeClass("d-none");
		setTimeout(() => {
			agentsListTab.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function CheckAgentTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Check General tab changes
	const generalChanges = CheckAgentGeneralTabChanges(false);
	if (generalChanges.hasChanges) {
		changes.general = generalChanges.changes;
		hasChanges = true;
	}

	// Check Context tab changes
	const contextChanges = CheckAgentContextTabChanges(false);
	if (contextChanges.hasChanges) {
		changes.context = contextChanges.changes;
		hasChanges = true;
	}

	// Check Personality tab changes
	const personalityChanges = CheckAgentPersonalityTabChanges(false);
	if (personalityChanges.hasChanges) {
		changes.personality = personalityChanges.changes;
		hasChanges = true;
	}

	// Check Utterances tab changes
	const utterancesChanges = CheckAgentUtterancesTabChanges(false);
	if (utterancesChanges.hasChanges) {
		changes.utterances = utterancesChanges.changes;
		hasChanges = true;
	}

	// Check Integrations tab changes
	const integrationsChanges = {
		STT: CurrentAgentIntegrationsSTT,
		LLM: CurrentAgentIntegrationsLLM,
		TTS: CurrentAgentIntegrationsTTS,
	};
	if (JSON.stringify(CurrentManageAgentData.integrations) !== JSON.stringify(integrationsChanges)) {
		changes.integrations = integrationsChanges;
		hasChanges = true;
	}

	// Check Cache tab changes
	const cacheChanges = CheckAgentCacheTabChanges(false);
	if (cacheChanges.hasChanges) {
		changes.cache = cacheChanges.changes;
		hasChanges = true;
	}

	// Check Settings tab changes
	const settingsChanges = CheckAgentSettingsTabChanges(false);
	if (settingsChanges.hasChanges) {
		changes.settings = settingsChanges.changes;
		hasChanges = true;
	}

	if (enableDisableButton) {
		$("#confirmPublishAgentButton").prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

async function canLeaveAgentTab(leaveMessage = "") {
	if (IsSavingAgentTab) {
		AlertManager.createAlert({
			type: "warning",
			message: "Agent is currently being saved. Please wait for the save to finish.",
			enableDismiss: false,
		});
		return false;
	}

	const changes = CheckAgentTabHasChanges(false);
	if (changes.hasChanges) {
		const confirmDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in the agent.${leaveMessage}`,
			confirmText: "Discard",
			cancelText: "Cancel",
			confirmButtonClass: "btn-danger",
			modalClass: "modal-lg",
		});

		const confirmResult = await confirmDialog.show();
		if (!confirmResult) {
			return false;
		}
	}

	return true;
}

function createDefaultAgentObject() {
	const agent = {
		general: {
			emoji: "🤖",
			name: {},
			description: {},
		},
		context: {
			useBranding: true,
			useBranches: true,
			useServices: true,
			useProducts: true,
		},
		personality: {
			name: {},
			role: {},
			capabilities: {},
			ethics: {},
			tone: {},
		},
		utterances: {
			openingType: "AgentFirst",
			greetingMessage: {},
			phrasesBeforeReply: {},
		},
		scripts: [],
		integrations: {
			STT: [],
			LLM: [],
			TTS: [],
		},
		cache: {
			messages: [],
			audios: [],
			autoCacheAudioSettings: {
				autoCacheAudioResponses: false,
				autoCacheAudioResponsesDefaultExpiryHours: 24,
				autoCacheAudioResponseCacheGroupId: null,
			},
		},
		settings: {
			backgroundAudioUrl: null,
			backgroundAudioVolume: 10,
		},
	};

	// Initialize multi-language properties for all languages
	BusinessFullData.businessData.languages.forEach((language) => {
		// General
		agent.general.name[language] = "";
		agent.general.description[language] = "";

		// Personality
		agent.personality.name[language] = "";
		agent.personality.role[language] = "";
		agent.personality.capabilities[language] = [];
		agent.personality.ethics[language] = [];
		agent.personality.tone[language] = [];

		// Utterances
		agent.utterances.greetingMessage[language] = "";
		agent.utterances.phrasesBeforeReply[language] = [];
	});

	return agent;
}

function validateAgentMultiLanguageElements() {
	if (ManageAgentType == null) return;

	BusinessFullData.businessData.languages.forEach((language) => {
		const currentSelectedLanguage = SpecificationLanguagesListData.find((d) => d.id === language);

		/** General Tab **/
		// Identifier
		const agentIdentifier = CurrentAgentGeneralNameMultiLangData[currentSelectedLanguage.id];
		const agentIdentifierIsIncomplete = !agentIdentifier || agentIdentifier === "" || agentIdentifier.trim() === "";

		// Description
		const agentDescription = CurrentAgentGeneralDescriptionMultiLangData[currentSelectedLanguage.id];
		const agentDescriptionIsIncomplete = !agentDescription || agentDescription === "" || agentDescription.trim() === "";

		const isAnyIncompleteInGeneral = agentIdentifierIsIncomplete || agentDescriptionIsIncomplete;

		/** Personality Tab **/
		// Name
		const agentName = CurrentAgentPersonalityNameMultiLangData[currentSelectedLanguage.id];
		const nameIsIncomplete = !agentName || agentName === "" || agentName.trim() === "";

		// Role
		const agentRole = CurrentAgentPersonalityRoleMultiLangData[currentSelectedLanguage.id];
		const roleIsIncomplete = !agentRole || agentRole === "" || agentRole.trim() === "";

		// Lists
		let listsIncomplete = false;
		["capabilities", "ethics", "tone"].forEach((listType) => {
			const currentData =
				listType === "capabilities" ? CurrentAgentPersonalityCapabilitiesMultiLangData : listType === "ethics" ? CurrentAgentPersonalityEthicsMultiLangData : CurrentAgentPersonalityToneMultiLangData;

			const list = currentData[currentSelectedLanguage.id] || [];
			if (list || list.length !== 0) {
				list.forEach((item) => {
					if (!item || item === "" || item.trim() === "") {
						listsIncomplete = true;
					}
				});
			}
		});

		const isAnyIncompleteInPersonality = nameIsIncomplete || roleIsIncomplete || listsIncomplete;

		/** Utterances Tab **/
		// Greeting Message
		const greetingMessage = CurrentAgentUtterancesGreetingMessageMultiLangData[currentSelectedLanguage.id];
		const greetingMessageIsIncomplete = !greetingMessage || greetingMessage === "" || greetingMessage.trim() === "";

		// Phrases Before Reply
		const phrases = CurrentAgentUtterancesPhrasesBeforeReplyMultiLangData[currentSelectedLanguage.id];
		const phrasesIsIncomplete = !phrases || phrases.length === 0;

		// Update language status
		const isAnyIncompleteInUtterances = greetingMessageIsIncomplete || phrasesIsIncomplete;

		/** Update language status **/
		const isAnyIncomplete = isAnyIncompleteInGeneral || isAnyIncompleteInPersonality || isAnyIncompleteInUtterances;
		manageAgentsLanguageDropdown.setLanguageStatus(currentSelectedLanguage.id, isAnyIncomplete ? "incomplete" : "complete");
	});
}

// General Tab Functions
function CheckAgentGeneralTabChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Emoji
	changes.emoji = $("#editAgentIconButton").text();
	if (CurrentManageAgentData.general.emoji !== changes.emoji) {
		hasChanges = true;
	}

	// Name (multi-language)
	changes.name = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.name[language] = CurrentAgentGeneralNameMultiLangData[language];

		if (CurrentManageAgentData.general.name[language] !== changes.name[language]) {
			hasChanges = true;
		}
	});

	// Description (multi-language)
	changes.description = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.description[language] = CurrentAgentGeneralDescriptionMultiLangData[language];

		if (CurrentManageAgentData.general.description[language] !== changes.description[language]) {
			hasChanges = true;
		}
	});

	if (enableDisableButton) {
		$("#confirmPublishAgentButton").prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

function fillAgentGeneralTab() {
	// Emoji
	$("#editAgentIconButton").text(CurrentManageAgentData.general.emoji);

	// Name
	CurrentAgentGeneralNameMultiLangData = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentGeneralNameMultiLangData[language] = CurrentManageAgentData.general.name[language];
	});
	$("#editAgentIdentifierInput").val(CurrentAgentGeneralNameMultiLangData[BusinessDefaultLanguage]);

	// Description
	CurrentAgentGeneralDescriptionMultiLangData = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentGeneralDescriptionMultiLangData[language] = CurrentManageAgentData.general.description[language];
	});
	$("#editAgentDescriptionInput").val(CurrentAgentGeneralDescriptionMultiLangData[BusinessDefaultLanguage]);
}

function validateAgentGeneralTab(onlyRemove = true) {
	const errors = [];
	let isValid = true;

	// Validate name for all languages
	BusinessFullData.businessData.languages.forEach((language) => {
		if (!CurrentAgentGeneralNameMultiLangData[language] || CurrentAgentGeneralNameMultiLangData[language].trim().length === 0) {
			isValid = false;
			errors.push(`Agent name for language ${language} is required.`);

			if (!onlyRemove && language === manageAgentsLanguageDropdown.getSelectedLanguage().id) {
				$("#editAgentIdentifierInput").addClass("is-invalid");
			}
		}
	});

	// Validate description for all languages
	BusinessFullData.businessData.languages.forEach((language) => {
		if (!CurrentAgentGeneralDescriptionMultiLangData[language] || CurrentAgentGeneralDescriptionMultiLangData[language].trim().length === 0) {
			isValid = false;
			errors.push(`Agent description for language ${language} is required.`);

			if (!onlyRemove && language === manageAgentsLanguageDropdown.getSelectedLanguage().id) {
				$("#editAgentDescriptionInput").addClass("is-invalid");
			}
		}
	});

	return {
		isValid,
		errors,
	};
}

// Context Tab Functions
function CheckAgentContextTabChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Context checkboxes
	changes.useBranding = $("#agentEditContextEnableBranding").prop("checked");
	if (CurrentManageAgentData.context.useBranding !== changes.useBranding) {
		hasChanges = true;
	}

	changes.useBranches = $("#agentEditContextEnableBranches").prop("checked");
	if (CurrentManageAgentData.context.useBranches !== changes.useBranches) {
		hasChanges = true;
	}

	changes.useServices = $("#agentEditContextEnableServices").prop("checked");
	if (CurrentManageAgentData.context.useServices !== changes.useServices) {
		hasChanges = true;
	}

	changes.useProducts = $("#agentEditContextEnableProducts").prop("checked");
	if (CurrentManageAgentData.context.useProducts !== changes.useProducts) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		$("#confirmPublishAgentButton").prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

// Personality Tab Functions
function CheckAgentPersonalityTabChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Name
	changes.name = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.name[language] = CurrentAgentPersonalityNameMultiLangData[language];
		if (CurrentManageAgentData.personality.name[language] !== changes.name[language]) {
			hasChanges = true;
		}
	});

	// Role
	changes.role = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.role[language] = CurrentAgentPersonalityRoleMultiLangData[language];
		if (CurrentManageAgentData.personality.role[language] !== changes.role[language]) {
			hasChanges = true;
		}
	});

	// Lists (Capabilities, Ethics, Tone)
	["capabilities", "ethics", "tone"].forEach((listType) => {
		changes[listType] = {};
		const currentData =
			listType === "capabilities" ? CurrentAgentPersonalityCapabilitiesMultiLangData : listType === "ethics" ? CurrentAgentPersonalityEthicsMultiLangData : CurrentAgentPersonalityToneMultiLangData;

		BusinessFullData.businessData.languages.forEach((language) => {
			changes[listType][language] = currentData[language] || [];

			// Compare arrays
			const originalArray = CurrentManageAgentData.personality[listType][language] || [];
			if (JSON.stringify(originalArray) !== JSON.stringify(changes[listType][language])) {
				hasChanges = true;
			}
		});
	});

	if (enableDisableButton) {
		$("#confirmPublishAgentButton").prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

function fillAgentPersonalityTab() {
	// Name
	CurrentAgentPersonalityNameMultiLangData = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentPersonalityNameMultiLangData[language] = CurrentManageAgentData.personality.name[language];
	});
	$("#editAgentPersonalityNameInput").val(CurrentAgentPersonalityNameMultiLangData[BusinessDefaultLanguage]);

	// Role
	CurrentAgentPersonalityRoleMultiLangData = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentPersonalityRoleMultiLangData[language] = CurrentManageAgentData.personality.role[language];
	});
	$("#editAgentPersonalityRoleInput").val(CurrentAgentPersonalityRoleMultiLangData[BusinessDefaultLanguage]);

	// Lists
	["capabilities", "ethics", "tone"].forEach((listType) => {
		const currentData =
			listType === "capabilities" ? CurrentAgentPersonalityCapabilitiesMultiLangData : listType === "ethics" ? CurrentAgentPersonalityEthicsMultiLangData : CurrentAgentPersonalityToneMultiLangData;

		currentData = {};
		BusinessFullData.businessData.languages.forEach((language) => {
			currentData[language] = CurrentManageAgentData.personality[listType][language] || [];
		});

		// Fill the list for default language
		const container = $(`#editAgentPersonality${listType.charAt(0).toUpperCase() + listType.slice(1)}ValueInputs`);
		container.empty();
		currentData[BusinessDefaultLanguage].forEach((value) => {
			container.append(`
                <div class="input-group mb-1">
                    <input type="text" class="form-control" value="${value}">
                    <button class="btn btn-danger" button-type="editAgentPersonalityValueRemove">
                        <i class='fa-regular fa-trash'></i>
                    </button>
                </div>
            `);
		});
	});
}

function validateAgentPersonalityTab(onlyRemove = true) {
	const errors = [];
	let isValid = true;

	// Validate name for all languages
	BusinessFullData.businessData.languages.forEach((language) => {
		if (!CurrentAgentPersonalityNameMultiLangData[language] || CurrentAgentPersonalityNameMultiLangData[language].trim().length === 0) {
			isValid = false;
			errors.push(`Agent personality name for language ${language} is required.`);

			if (!onlyRemove && language === manageAgentsLanguageDropdown.getSelectedLanguage().id) {
				$("#editAgentPersonalityNameInput").addClass("is-invalid");
			}
		}
	});

	// Validate role for all languages
	BusinessFullData.businessData.languages.forEach((language) => {
		if (!CurrentAgentPersonalityRoleMultiLangData[language] || CurrentAgentPersonalityRoleMultiLangData[language].trim().length === 0) {
			isValid = false;
			errors.push(`Agent personality role for language ${language} is required.`);

			if (!onlyRemove && language === manageAgentsLanguageDropdown.getSelectedLanguage().id) {
				$("#editAgentPersonalityRoleInput").addClass("is-invalid");
			}
		}
	});

	return {
		isValid,
		errors,
	};
}

// Utterances Tab Functions
function CheckAgentUtterancesTabChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Opening Type
	changes.openingType = $("#editAgentGreetingStartTypeInput").val();
	if (CurrentManageAgentData.utterances.openingType !== changes.openingType) {
		hasChanges = true;
	}

	// Greeting Message (multi-language)
	changes.greetingMessage = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.greetingMessage[language] = CurrentAgentUtterancesGreetingMessageMultiLangData[language];
		if (CurrentManageAgentData.utterances.greetingMessage[language] !== changes.greetingMessage[language]) {
			hasChanges = true;
		}
	});

	// Phrases Before Reply (multi-language)
	changes.phrasesBeforeReply = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.phrasesBeforeReply[language] = CurrentAgentUtterancesPhrasesBeforeReplyMultiLangData[language];

		// Compare arrays
		const originalArray = CurrentManageAgentData.utterances.phrasesBeforeReply[language] || [];
		if (JSON.stringify(originalArray) !== JSON.stringify(changes.phrasesBeforeReply[language])) {
			hasChanges = true;
		}
	});

	if (enableDisableButton) {
		$("#confirmPublishAgentButton").prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

function fillAgentUtterancesTab() {
	// Opening Type
	$("#editAgentGreetingStartTypeInput").val(CurrentManageAgentData.utterances.openingType);

	// Greeting Message
	CurrentAgentUtterancesGreetingMessageMultiLangData = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentUtterancesGreetingMessageMultiLangData[language] = CurrentManageAgentData.utterances.greetingMessage[language];
	});
	$("#editAgentPersonalityGreetingInput").val(CurrentAgentUtterancesGreetingMessageMultiLangData[BusinessDefaultLanguage]);

	// Phrases Before Reply
	CurrentAgentUtterancesPhrasesBeforeReplyMultiLangData = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentUtterancesPhrasesBeforeReplyMultiLangData[language] = CurrentManageAgentData.utterances.phrasesBeforeReply[language] || [];
	});
	$("#editAgentPhrasesBeforeReply").val(CurrentAgentUtterancesPhrasesBeforeReplyMultiLangData[BusinessDefaultLanguage].join(", "));
}

function validateAgentUtterancesTab(onlyRemove = true) {
	const errors = [];
	let isValid = true;

	// Validate opening type
	const openingType = $("#editAgentGreetingStartTypeInput").val();
	if (!openingType) {
		isValid = false;
		errors.push("Opening type is required");
		if (!onlyRemove) {
			$("#editAgentGreetingStartTypeInput").addClass("is-invalid");
		}
	}

	// Validate greeting message for all languages
	BusinessFullData.businessData.languages.forEach((language) => {
		if (!CurrentAgentUtterancesGreetingMessageMultiLangData[language] || CurrentAgentUtterancesGreetingMessageMultiLangData[language].trim().length === 0) {
			isValid = false;
			errors.push(`Greeting message for language ${language} is required.`);

			if (!onlyRemove && language === manageAgentsLanguageDropdown.getSelectedLanguage().id) {
				$("#editAgentPersonalityGreetingInput").addClass("is-invalid");
			}
		}
	});

	// Validate phrases before reply for all languages
	BusinessFullData.businessData.languages.forEach((language) => {
		const phrases = CurrentAgentUtterancesPhrasesBeforeReplyMultiLangData[language];
		if (!phrases || phrases.length === 0) {
			isValid = false;
			errors.push(`At least one phrase before reply for language ${language} is required.`);

			if (!onlyRemove && language === manageAgentsLanguageDropdown.getSelectedLanguage().id) {
				$("#editAgentPhrasesBeforeReply").addClass("is-invalid");
			}
		}
	});

	return {
		isValid,
		errors,
	};
}

// Integration Tab Functions
function createIntegrationSelectElement(type, index) {
	const integrations = BusinessFullData.businessApp.integrations.filter((integration) => {
		const integrationTypeData = SpecificationIntegrationsListData.find((integrationType) => integrationType.id === integration.type);
		return integrationTypeData.type.includes(type);
	});

	let options = '<option value="">Select Integration</option>';
	integrations.forEach((integration) => {
		options += `<option value="${integration.id}">${integration.friendlyName}</option>`;
	});

	return `
        <div class="mb-2 integration-item" data-index="${index}">
            <div class="input-group">
                <span class="input-group-text">
                    <i class="fa-regular fa-${index + 1}"></i>
                </span>
                <select class="form-select" select-type="integration-${type.toLowerCase()}">
                    ${options}
                </select>
                <button class="btn btn-secondary" button-type="configure-integration" data-bs-toggle="tooltip" data-bs-title="Configure Integration">
                    <i class="fa-regular fa-gear"></i>
                </button>
                <button class="btn btn-danger" button-type="remove-integration" data-index="${index}" data-integration-id="">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </div>
        </div>
    `;
}

function fillIntegrationsList(type) {
	const container = type === "STT" ? sttIntegrationsList : type === "LLM" ? llmIntegrationsList : ttsIntegrationsList;

	const currentIntegrations = type === "STT" ? CurrentAgentIntegrationsSTT : type === "LLM" ? CurrentAgentIntegrationsLLM : CurrentAgentIntegrationsTTS;

	// Clear existing items except alert
	container.find(".integration-item").remove();

	// Add current integrations
	currentIntegrations.forEach((integrationId, index) => {
		const element = $(createIntegrationSelectElement(type, index));
		element.find("select").val(integrationId);
		container.append(element);
	});
}

function createAgentIntegrationConfigurationField(field) {
	let fieldHtml = "";

	switch (field.type) {
		case "text":
			fieldHtml = `
                <div class="mb-3 config-field" data-field-id="${field.id}">
                    <label class="form-label btn-ic-span-align">
                        <span>${field.name} ${field.required ? '<span class="text-danger">*</span>' : ""}</span>
                        ${
													field.tooltip
														? `
                            <a href="#" class="d-inline-block" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${field.tooltip}">
                                <i class="fa-regular fa-circle-question"></i>
                            </a>
                        `
														: ""
												}
                    </label>
                    <input type="${field.isEncrypted ? "password" : "text"}" 
                           class="form-control config-field-input"
                           placeholder="${field.placeholder || ""}"
                           value="${CurrentAgentConfigurationValues[field.id] || field.defaultValue || ""}">
                </div>
            `;
			break;

		case "number":
			fieldHtml = `
                <div class="mb-3 config-field" data-field-id="${field.id}">
                    <label class="form-label btn-ic-span-align">
                        <span>${field.name} ${field.required ? '<span class="text-danger">*</span>' : ""}</span>
                        ${
													field.tooltip
														? `
                            <a href="#" class="d-inline-block" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${field.tooltip}">
                                <i class="fa-regular fa-circle-question"></i>
                            </a>
                        `
														: ""
												}
                    </label>
                    <input type="number" 
                           class="form-control config-field-input"
                           placeholder="${field.placeholder || ""}"
                           value="${CurrentAgentConfigurationValues[field.id] || field.defaultValue || ""}">
                </div>
            `;
			break;

		case "select": {
			const options = field.options?.map((opt) => `<option value="${opt.key}" ${opt.isDefault ? "selected" : ""}>${opt.value}</option>`).join("") || "";

			fieldHtml = `
                <div class="mb-3 config-field" data-field-id="${field.id}">
                    <label class="form-label btn-ic-span-align">
                        <span>${field.name} ${field.required ? '<span class="text-danger">*</span>' : ""}</span>
                        ${
													field.tooltip
														? `
                            <a href="#" class="d-inline-block" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${field.tooltip}">
                                <i class="fa-regular fa-circle-question"></i>
                            </a>
                        `
														: ""
												}
                    </label>
                    <select class="form-select config-field-input">
                        <option value="" disabled>Select ${field.name}</option>
                        ${options}
                    </select>
                </div>
            `;
			break;
		}

		case "models":
			fieldHtml = `
                <div class="mb-3 config-field" data-field-id="${field.id}">
                    <label class="form-label btn-ic-span-align">
                        <span>${field.name} ${field.required ? '<span class="text-danger">*</span>' : ""}</span>
                        ${
													field.tooltip
														? `
                            <a href="#" class="d-inline-block" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${field.tooltip}">
                                <i class="fa-regular fa-circle-question"></i>
                            </a>
                        `
														: ""
												}
                    </label>
                    <select class="form-select config-field-input">
                        <option value="" disabled>Select ${field.name}</option>
                        <!-- Models will be populated dynamically -->
                    </select>
                </div>
            `;
			break;
	}

	return $(fieldHtml);
}

function fillAgentIntegrationConfigurationFields() {
	integrationConfigurationFieldsContainer.empty();

	CurrentAgentConfigurationFields.forEach((field) => {
		const fieldElement = createAgentIntegrationConfigurationField(field);
		integrationConfigurationFieldsContainer.append(fieldElement);

		// For models field type, populate with available models
		if (field.type === "models") {
			populateAgentIntegrationModelsField(field);
		}

		// Initialize tooltips for new elements
		const tooltips = fieldElement.find('[data-bs-toggle="tooltip"]');
		tooltips.each((index, element) => {
			new bootstrap.Tooltip(element);
		});
	});
}

function populateAgentIntegrationModelsField(field) {
	const provider = BusinessLLMProvidersForIntegrations.find((p) => p.integrationId === CurrentAgentConfigurationIntegrationType);

	if (!provider || !provider.models) return;

	const selectElement = integrationConfigurationFieldsContainer.find(`.config-field[data-field-id="${field.id}"] select`);

	// Add enabled models only
	const enabledModels = provider.models.filter((model) => model.disabledAt === null);

	enabledModels.forEach((model) => {
		selectElement.append(`
            <option value="${model.id}" 
                ${CurrentAgentConfigurationValues[field.id] === model.id ? "selected" : ""}>
                ${model.name}
            </option>
        `);
	});
}

function loadAgentIntegrationConfiguration(integrationId, integrationType) {
	// Get provider configuration based on integration type
	const businessIntegrationData = BusinessFullData.businessApp.integrations.find((integration) => integration.id === integrationId);
	const provider = BusinessLLMProvidersForIntegrations.find((p) => p.integrationId === businessIntegrationData.type);

	if (!provider) {
		AlertManager.createAlert({
			type: "error",
			message: "Provider configuration not found",
			timeout: 3000,
		});
		return;
	}

	CurrentAgentConfigurationIntegration = integrationId;
	CurrentAgentConfigurationIntegrationType = businessIntegrationData.type;
	CurrentAgentConfigurationType = integrationType;
	CurrentAgentConfigurationFields = provider.userIntegrationFields;

	// Get current values if they exist
	CurrentAgentConfigurationValues = {}; // Reset values

	// Find existing configuration values
	const currentArray = integrationType === "STT" ? CurrentAgentIntegrationsSTT : integrationType === "LLM" ? CurrentAgentIntegrationsLLM : CurrentAgentIntegrationsTTS;

	const existingConfig = currentArray.find((i) => i && i.id === integrationId);
	if (existingConfig?.fieldValues) {
		CurrentAgentConfigurationValues = { ...existingConfig.fieldValues };
	}

	// Fill the modal with fields
	fillAgentIntegrationConfigurationFields();
}

function validateAgentIntegrationConfiguration() {
	const errors = [];
	let isValid = true;

	CurrentAgentConfigurationFields.forEach((field) => {
		const fieldElement = integrationConfigurationFieldsContainer.find(`.config-field[data-field-id="${field.id}"]`);
		const input = fieldElement.find(".config-field-input");
		const value = input.val().trim();

		// Remove existing invalid state
		input.removeClass("is-invalid");

		// Required field validation
		if (field.required && !value) {
			isValid = false;
			errors.push(`${field.name} is required`);
			input.addClass("is-invalid");
		}

		// Type-specific validation
		if (value) {
			switch (field.type) {
				case "number":
					if (isNaN(value)) {
						isValid = false;
						errors.push(`${field.name} must be a valid number`);
						input.addClass("is-invalid");
					}
					break;

				case "models":
					const provider = BusinessLLMProvidersForIntegrations.find((p) => p.integrationId === CurrentAgentConfigurationIntegration);

					if (provider) {
						const model = provider.models.find((m) => m.id === value);
						if (!model) {
							isValid = false;
							errors.push(`${field.name}: Selected model is invalid`);
							input.addClass("is-invalid");
						} else if (model.disabledAt !== null) {
							isValid = false;
							errors.push(`${field.name}: Selected model is disabled`);
							input.addClass("is-invalid");
						}
					}
					break;

				case "select":
					if (field.options && !field.options.some((opt) => opt.key === value)) {
						isValid = false;
						errors.push(`${field.name}: Invalid option selected`);
						input.addClass("is-invalid");
					}
					break;
			}
		}
	});

	return {
		isValid,
		errors,
	};
}

function getAgentIntegrationConfigurationChanges() {
	const changes = {};
	let hasChanges = false;

	CurrentAgentConfigurationFields.forEach((field) => {
		const fieldElement = integrationConfigurationFieldsContainer.find(`.config-field[data-field-id="${field.id}"]`);
		const value = fieldElement.find(".config-field-input").val().trim();
		const currentValue = CurrentAgentConfigurationValues[field.id] || "";

		if (value !== currentValue) {
			changes[field.id] = value;
			hasChanges = true;
		}
	});

	return {
		hasChanges,
		changes,
	};
}

function saveAgentIntegrationConfigurationChanges(changes) {
	// Update current agent integration configuration
	const currentArray = CurrentAgentConfigurationType === "STT" ? CurrentAgentIntegrationsSTT : CurrentAgentConfigurationType === "LLM" ? CurrentAgentIntegrationsLLM : CurrentAgentIntegrationsTTS;

	const integrationIndex = currentArray.findIndex((i) => {
		if (!i) return false;

		return i.id === CurrentAgentConfigurationIntegration;
	});
	if (integrationIndex !== -1) {
		// Update existing configuration
		Object.keys(changes).forEach((fieldId) => {
			currentArray[integrationIndex].fieldValues[fieldId] = changes[fieldId];
		});
	}

	// Update local state
	CurrentAgentConfigurationValues = {
		...CurrentAgentConfigurationValues,
		...changes,
	};

	// Close modal
	integrationConfigurationModal.modal("hide");
}

// Cache Tab Functions
function createCacheGroupSelectElement(type, index) {
	const groups = type === "message" ? BusinessFullData.businessApp.cache.messageGroups : BusinessFullData.businessApp.cache.audioGroups;

	let options = '<option value="">Select Group</option>';
	groups.forEach((group) => {
		options += `<option value="${group.id}">${group.name}</option>`;
	});

	return `
        <div class="mb-2 cache-group-item" data-index="${index}">
            <div class="input-group">
                <select class="form-select" select-type="cache-${type}-group">
                    ${options}
                </select>
                <button class="btn btn-danger" button-type="remove-cache-group">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </div>
        </div>
    `;
}

function fillCacheGroupsList(type) {
	const container = type === "message" ? messageCacheGroupsList : audioCacheGroupsList;
	const currentGroups = type === "message" ? CurrentAgentCacheMessages : CurrentAgentCacheAudios;

	// Clear existing items except alert
	container.find(".cache-group-item").remove();

	// Add current groups
	currentGroups.forEach((groupId, index) => {
		const element = $(createCacheGroupSelectElement(type, index));
		element.find("select").val(groupId);
		container.append(element);
	});
}

function CheckAgentCacheTabChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Messages
	changes.messages = CurrentAgentCacheMessages;
	if (JSON.stringify(CurrentManageAgentData.cache.messages) !== JSON.stringify(changes.messages)) {
		hasChanges = true;
	}

	// Audios
	changes.audios = CurrentAgentCacheAudios;
	if (JSON.stringify(CurrentManageAgentData.cache.audios) !== JSON.stringify(changes.audios)) {
		hasChanges = true;
	}

	// Auto Cache Audio Settings
	changes.autoCacheAudioSettings = {
		autoCacheAudioResponses: false, // Add appropriate element ID
		autoCacheAudioResponsesDefaultExpiryHours: 24, // Add appropriate element ID
		autoCacheAudioResponseCacheGroupId: null, // Add appropriate element ID
	};

	if (JSON.stringify(CurrentManageAgentData.cache.autoCacheAudioSettings) !== JSON.stringify(changes.autoCacheAudioSettings)) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		$("#confirmPublishAgentButton").prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

// Settings Tab Functions
function CheckAgentSettingsTabChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// Background Audio URL
	const backgroundAudioType = $("#editAgentBackgroundAudioSelect").val();
	changes.backgroundAudioUrl = backgroundAudioType === "none" ? null : backgroundAudioType;

	if (CurrentManageAgentData.settings.backgroundAudioUrl !== changes.backgroundAudioUrl) {
		hasChanges = true;
	}

	// Background Audio Volume
	changes.backgroundAudioVolume = parseInt($("#editAgentBackgroundAudioVolume").val());
	if (CurrentManageAgentData.settings.backgroundAudioVolume !== changes.backgroundAudioVolume) {
		hasChanges = true;
	}

	if (enableDisableButton) {
		$("#confirmPublishAgentButton").prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

function initAgentTab() {
	$(document).ready(() => {
		manageAgentsLanguageDropdown = new MultiLanguageDropdown("agentsManagerMultiLanguageContainer", BusinessFullLanguagesData);

		/** Event Handlers **/
		addNewAgentButton.on("click", (event) => {
			event.preventDefault();

			currentAgentName.text("New Agent");
			CurrentManageAgentData = createDefaultAgentObject();

			showAgentManagerTab();

			ManageAgentType = "new";
		});

		switchBackToAgentsTab.on("click", (event) => {
			event.preventDefault();

			showAgentListTab();
		});

		addNewAgentScriptButton.on("click", (event) => {
			event.preventDefault();

			currentAgentScriptName.text("New Script");

			agentScriptsListTab.removeClass("show");
			setTimeout(() => {
				agentScriptsListTab.addClass("d-none");

				agentScriptsManagerTab.removeClass("d-none");
				setTimeout(() => {
					agentScriptsManagerTab.addClass("show");
				}, 10);
			}, 150);
		});

		switchBackToAgentsScriptManagerTab.on("click", (event) => {
			event.preventDefault();

			agentScriptsManagerTab.removeClass("show");
			setTimeout(() => {
				agentScriptsManagerTab.addClass("d-none");

				agentScriptsListTab.removeClass("d-none");
				setTimeout(() => {
					agentScriptsListTab.addClass("show");
				}, 10);
			}, 150);
		});

		addAgentScriptConditionValueButton.on("click", (event) => {
			event.preventDefault();

			editAgentScriptConditionValueInputs.append(`
                              <div class="input-group mb-1">
                                   <input type="text" class="form-control" placeholder="Script Condition" aria-label="Condition Value" value="">
                                   <button class="btn btn-danger" button-type="editAgentScriptConditionValueRemove">
                                        <i class='fa-regular fa-trash'></i>
                                   </button>
                              </div>
                         `);
		});

		editAgentScriptConditionValueInputs.on("click", '[button-type="editAgentScriptConditionValueRemove"]', (event) => {
			event.preventDefault();
			event.stopPropagation();

			$(event.currentTarget).parent().remove();
		});

		addAgentScriptConversationMessageButton.on("click", (event) => {
			// todo try to make the options of functions static

			agentScriptConversationMessages.append(`
                              <div class="conversationMessage">
                                   <div class="user-message singleMessage">
                                        <div class="input-group">
                                             <button class="btn btn-danger btn-sm" button-type="editAgentScriptConversationMessageRemove">
                                                  <i class='fa-regular fa-trash'></i>
                                             </button>
                                             <textarea class="form-control" placeholder="Type the user message..." rows="1"></textarea>
                                        </div>
                                        <div class="message-icon">
                                             <i class='fa-regular fa-user'></i>
                                        </div>
                                   </div>
                                   <div class="ai-message">
                                        <div class="singleMessage">
                                             <div class="message-icon">
                                                  <i class='fa-regular fa-user'></i>
                                             </div>
                                             <div class="input-group">
                                                  <select class="form-select" style="max-width: 180px" select-type="set-ai-response-type">
                                                       <option value="response_to_user" selected>User Reply</option>
                                                       <option value="response_to_system">Execute Tool</option>
                                                  </select>
                                                  <textarea textarea-type="set-ai-response-text-area" class="form-control" placeholder="Type the abstract AI message..." rows="1"></textarea>
                                                  <select class="form-select d-none" select-type="set-ai-response-tool" previous-value="none">
                                                       <option value="none" can-continue="true" is-custom-tool="false" selected>Select Tool Function</option>
                                                       <option value="make_appointment" can-continue="true" is-custom-tool="true">Make Appointment</option>
                                                       <option value="get_dtmf_keypad_input" can-continue="true" is-custom-tool="false">Get DTMF Keypad Input</option>
                                                       <option value="change_language" can-continue="true" is-custom-tool="false">Change Language</option>
                                                       <option value="transfer_to_agent" can-continue="false" is-custom-tool="false">Transfer to Agent</option>
                                                       <option value="transfer_to_human" can-continue="false" is-custom-tool="false">Transfer to Human</option>
                                                       <option value="end_call" can-continue="false" is-custom-tool="false">End Call</option>
                                                       ${
																													// todo
																													""
																												}
                                                  </select>
                                             </div>
                                        </div>
                                        <div class="d-none mt-2" data-type="transfer-to-agent-type">
                                             <label class="form-label mb-1">Transfer to Agent</label>
                                             <select class="form-select" select-type="transfer-to-agent-type-select">
                                                  <option value="-1">Select Agent</option>
                                                  <option value="21412">Sales Agent</option>
                                                  <option value="412512">Technical Agent</option>
                                             </select>
                                        </div>
                                        <div class="d-none mt-2" data-type="tool-status-conditional-conversation">
                                             <label class="form-label mb-1">Tool Status Conditional Conversation</label>
                                              <select class="form-select" select-type="set-ai-tool-status-type">
                                                  <option value="-1">All Status</option>
                                                  <option value="200">200</option>
                                                  <option value="500">500</option>
                                             </select>
                                        </div>
                                   </div>
                              </div>
                         `);

			agentScriptConversationMessages.stop().animate({ scrollTop: agentScriptConversationMessages[0].scrollHeight }, 500, "linear", () => {});

			noMessagesInConversationAlert.addClass("d-none");
		});

		agentScriptConversationMessages.on("click", '[button-type="editAgentScriptConversationMessageRemove"]', (event) => {
			event.preventDefault();
			event.stopPropagation();

			let actualParent = $(event.currentTarget).parent().parent().parent();

			let toolType = actualParent.find('[select-type="set-ai-response-type"]').val();

			if (toolType === "response_to_system") {
				let selectedToolValue = actualParent.find('[select-type="set-ai-response-tool"]').val();
				let selectedToolOption = actualParent.find('[select-type="set-ai-response-tool"] option[value="' + selectedToolValue + '"]');

				let isToolThatCanContinue = selectedToolOption.attr("can-continue");

				if (typeof isToolThatCanContinue !== "undefined" && (isToolThatCanContinue == "false" || isToolThatCanContinue == "false")) {
					addAgentScriptConversationMessageButton.prop("disabled", false);
				}
			}

			actualParent.remove();

			if (agentScriptConversationMessages.children().length === 0) {
				noMessagesInConversationAlert.removeClass("d-none");
			}
		});

		agentScriptConversationMessages.on("change", '[select-type="set-ai-response-type"]', (event) => {
			event.stopPropagation();

			let target = $(event.currentTarget);
			let selectedValue = target.val();

			let parent = target.parent();
			let textAreaForUserResponse = parent.find('[textarea-type="set-ai-response-text-area"]');
			let toolForUserResponse = parent.find('[select-type="set-ai-response-tool"]');

			let toolStatusElement = parent.parent().parent().find('[data-type="tool-status-conditional-conversation"]');

			let transferToAgentElement = parent.parent().parent().find('[data-type="transfer-to-agent-type"]');
			let transferToAgentElementSelect = transferToAgentElement.find('[select-type="transfer-to-agent-type-select"]');

			if (selectedValue === "response_to_user") {
				textAreaForUserResponse.removeClass("d-none");
				toolForUserResponse.addClass("d-none");

				toolStatusElement.addClass("d-none");
				transferToAgentElement.addClass("d-none");

				addAgentScriptConversationMessageButton.prop("disabled", false);

				toolForUserResponse.val("none");
				toolForUserResponse.change();

				transferToAgentElementSelect.val("-1");
				transferToAgentElementSelect.change();
			} else if (selectedValue === "response_to_system") {
				textAreaForUserResponse.addClass("d-none");
				toolForUserResponse.removeClass("d-none");

				toolForUserResponse.change();
			}
		});

		agentScriptConversationMessages.on("change", '[select-type="set-ai-response-tool"]', (event) => {
			event.stopPropagation();

			let target = $(event.currentTarget);
			let selectedValue = target.val();

			let selectedOption = target.find('option[value="' + selectedValue + '"]');

			let canContinueAfterTool = selectedOption.attr("can-continue");
			let isCustomTool = selectedOption.attr("is-custom-tool");

			let messagesListLength = agentScriptConversationMessages.children().length;
			let messagePositionInConversationList = target.parent().parent().parent().parent().index();

			let customToolStatusConditionalConversationElement = target.parent().parent().parent().find('[data-type="tool-status-conditional-conversation"]');
			let transferToAgentElement = target.parent().parent().parent().find('[data-type="transfer-to-agent-type"]');

			if (typeof canContinueAfterTool !== "undefined" && (canContinueAfterTool == "false" || canContinueAfterTool == false)) {
				if (messagesListLength > messagePositionInConversationList + 1) {
					if (!confirm("Using this tool will ignore the rest of the conversation. Are you sure you want to continue?\n\n Confirming will delete every other message after this message.")) {
						event.preventDefault();
						target.val(target.attr("previous-value"));
						return false;
					} else {
						agentScriptConversationMessages.children().each((index, data) => {
							if (index > messagePositionInConversationList) data.remove();
						});
					}
				}

				messagesListLength = agentScriptConversationMessages.children().length;

				if (messagesListLength === messagePositionInConversationList + 1) {
					addAgentScriptConversationMessageButton.prop("disabled", true);
				}
			} else {
				if (messagesListLength === messagePositionInConversationList + 1) {
					addAgentScriptConversationMessageButton.prop("disabled", false);
				}
			}

			if (typeof isCustomTool !== "undefined" && (isCustomTool == "true" || isCustomTool == true)) {
				customToolStatusConditionalConversationElement.removeClass("d-none");
			} else {
				customToolStatusConditionalConversationElement.addClass("d-none");
			}

			if (selectedValue === "transfer_to_agent") {
				transferToAgentElement.removeClass("d-none");
			} else {
				transferToAgentElement.addClass("d-none");
			}

			target.attr("previous-value", selectedValue);
		});

		editAgentBackgroundAudioSelect.on("change", (event) => {
			let selectedValue = editAgentBackgroundAudioSelect.val();

			if (!selectedValue) return;

			let audioConfigBox = $(".agent-background-audio-box");

			if (selectedValue === "none") {
				audioConfigBox.addClass("d-none");
				return;
			}

			let customAudioBox = $(".agent-background-custom-audio-box");

			if (selectedValue === "custom") {
				customAudioBox.removeClass("d-none");
			} else {
				customAudioBox.addClass("d-none");
			}

			audioConfigBox.removeClass("d-none");
		});

		// Integration Event Handlers
		addSTTIntegrationButton.on("click", (event) => {
			event.preventDefault();
			const newIndex = sttIntegrationsList.find(".integration-item").length;
			sttIntegrationsList.append(createIntegrationSelectElement("STT", newIndex));
		});

		addLLMIntegrationButton.on("click", (event) => {
			event.preventDefault();
			const newIndex = llmIntegrationsList.find(".integration-item").length;
			llmIntegrationsList.append(createIntegrationSelectElement("LLM", newIndex));
		});

		addTTSIntegrationButton.on("click", (event) => {
			event.preventDefault();
			const newIndex = ttsIntegrationsList.find(".integration-item").length;
			ttsIntegrationsList.append(createIntegrationSelectElement("TTS", newIndex));
		});

		// Cache Event Handlers
		addMessageCacheGroupButton.on("click", (event) => {
			event.preventDefault();
			const newIndex = messageCacheGroupsList.find(".cache-group-item").length;
			messageCacheGroupsList.append(createCacheGroupSelectElement("message", newIndex));
		});

		addAudioCacheGroupButton.on("click", (event) => {
			event.preventDefault();
			const newIndex = audioCacheGroupsList.find(".cache-group-item").length;
			audioCacheGroupsList.append(createCacheGroupSelectElement("audio", newIndex));
		});

		// Handle cache group removal
		agentCacheTab.on("click", '[button-type="remove-cache-group"]', function (event) {
			event.preventDefault();
			$(this).closest(".cache-group-item").remove();
		});

		// Handle integration removal
		agentIntegrationsTab.on("click", '[button-type="remove-integration"]', (event) => {
			event.preventDefault();

			const currentElement = $(event.currentTarget);
			const dataIndex = currentElement.attr("data-index");

			currentElement.closest(".integration-item").remove();

			// todo make sure to check whether its llm stt or tts
			CurrentAgentIntegrationsLLM.splice(dataIndex, 1);

			// Refresh indices
			$(".integration-item").each((idx, element) => {
				$(element).attr("data-index", idx);
				$(element)
					.find(".input-group-text i")
					.attr("class", `fa-regular fa-${idx + 1}`);
			});
		});

		// Handle cache group selection changes
		agentCacheTab.on("change", 'select[select-type^="cache-"]', function () {
			const type = $(this).attr("select-type").split("-")[1];
			const index = $(this).closest(".cache-group-item").data("index");
			const value = $(this).val();

			const currentArray = type === "message" ? CurrentAgentCacheMessages : CurrentAgentCacheAudios;

			if (value) {
				currentArray[index] = value;
			} else {
				currentArray.splice(index, 1);
			}
		});

		// Handle integration selection changes
		agentIntegrationsTab.on("change", 'select[select-type^="integration-"]', (event) => {
			const currentElement = $(event.currentTarget);
			const type = currentElement.attr("select-type").split("-")[1].toUpperCase();
			const index = currentElement.closest(".integration-item").data("index");
			const value = currentElement.val();

			const currentArray = type === "STT" ? CurrentAgentIntegrationsSTT : type === "LLM" ? CurrentAgentIntegrationsLLM : CurrentAgentIntegrationsTTS;

			if (value) {
				const alreadyExists = currentArray.some((i) => i.id === value);
				if (alreadyExists) {
					AlertManager.createAlert({
						type: "warning",
						message: "This integration is already added.",
						timeout: 3000,
					});

					currentElement.val("");
					return;
				}

				currentArray[index] = {
					id: value,
					fieldValues: {},
				};
			} else {
				currentArray.splice(index, 1);
			}
		});

		// Handle integration configuration
		agentIntegrationsTab.on("click", '[button-type="configure-integration"]', function (event) {
			event.preventDefault();

			const integrationSelect = $(this).closest(".integration-item").find("select");
			const integrationId = integrationSelect.val();
			const integrationType = integrationSelect.attr("select-type").split("-")[1].toUpperCase();

			if (!integrationId) {
				AlertManager.createAlert({
					type: "warning",
					message: "Please select an integration first.",
					timeout: 3000,
				});
				return;
			}

			// Load configuration before showing modal
			loadAgentIntegrationConfiguration(integrationId, integrationType);

			// Show the modal
			integrationConfigurationModal.modal("show");
		});

		// Save configuration
		saveIntegrationConfigButton.on("click", (event) => {
			event.preventDefault();

			const validation = validateAgentIntegrationConfiguration();
			if (!validation.isValid) {
				AlertManager.createAlert({
					type: "danger",
					message: `Validation failed:<br>${validation.errors.join("<br>")}`,
					timeout: 6000,
				});
				return;
			}

			const changes = getAgentIntegrationConfigurationChanges();
			if (!changes.hasChanges) {
				integrationConfigurationModal.modal("hide");
				return;
			}

			saveAgentIntegrationConfigurationChanges(changes.changes);
		});

		integrationConfigurationModal.on("hide.bs.modal", (event) => {
			CurrentAgentConfigurationIntegration = null;
			CurrentAgentConfigurationIntegrationType = null;
			CurrentAgentConfigurationFields = null;
			CurrentAgentConfigurationValues = {};
			CurrentAgentConfigurationType = null;
		});

		// Track changes in fields
		integrationConfigurationFieldsContainer.on("input change", ".config-field-input", () => {
			const changes = getAgentIntegrationConfigurationChanges();
			saveIntegrationConfigButton.prop("disabled", !changes.hasChanges);
		});

		function initAgentTabChangeHandlers() {
			// General Tab Changes
			function initAgentGeneralTabHandlers() {
				// Name input changes
				$("#editAgentIdentifierInput").on("input change", (event) => {
					const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
					CurrentAgentGeneralNameMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
					validateAgentMultiLanguageElements();
					CheckAgentTabHasChanges();
				});

				// Description input changes
				$("#editAgentDescriptionInput").on("input change", (event) => {
					const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
					CurrentAgentGeneralDescriptionMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
					validateAgentMultiLanguageElements();
					CheckAgentTabHasChanges();
				});

				// Language change handler
				manageAgentsLanguageDropdown.onLanguageChange((language) => {
					// Update name field
					$("#editAgentIdentifierInput").val(CurrentAgentGeneralNameMultiLangData[language.id] || "");

					// Update description field
					$("#editAgentDescriptionInput").val(CurrentAgentGeneralDescriptionMultiLangData[language.id] || "");

					validateAgentMultiLanguageElements();
				});
			}
			initAgentGeneralTabHandlers();

			// Context Tab Changes
			$("#agentEditContextEnableBranding, #agentEditContextEnableBranches, #agentEditContextEnableServices, #agentEditContextEnableProducts").on("change", () => {
				CheckAgentTabHasChanges();
			});

			// Personality Tab Changes
			function initAgentPersonalityTabHandlers() {
				// Name input changes
				$("#editAgentPersonalityNameInput").on("input change", (event) => {
					const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
					CurrentAgentPersonalityNameMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
					validateAgentMultiLanguageElements();
					CheckAgentTabHasChanges();
				});

				// Role input changes
				$("#editAgentPersonalityRoleInput").on("input change", (event) => {
					const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
					CurrentAgentPersonalityRoleMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
					validateAgentMultiLanguageElements();
					CheckAgentTabHasChanges();
				});

				// List input changes
				["capabilities", "ethics", "tone"].forEach((listType) => {
					const container = $(`#editAgentPersonality${listType.charAt(0).toUpperCase() + listType.slice(1)}ValueInputs`);

					// Add new value
					$(`#addAgentPersonality${listType.charAt(0).toUpperCase() + listType.slice(1)}Value`).on("click", () => {
						const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
						const currentData =
							listType === "capabilities"
								? CurrentAgentPersonalityCapabilitiesMultiLangData
								: listType === "ethics"
									? CurrentAgentPersonalityEthicsMultiLangData
									: CurrentAgentPersonalityToneMultiLangData;

						container.append(`
							<div class="input-group mb-1">
								<input type="text" class="form-control" value="">
								<button class="btn btn-danger" button-type="editAgentPersonalityValueRemove">
									<i class='fa-regular fa-trash'></i>
								</button>
							</div>
						`);

						// Update data
						currentData[currentSelectedLanguage.id] = Array.from(container.find("input")).map((input) => $(input).val().trim());
						validateAgentMultiLanguageElements();
						CheckAgentTabHasChanges();
					});

					// Remove value
					container.on("click", '[button-type="editAgentPersonalityValueRemove"]', function () {
						const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
						const currentData =
							listType === "capabilities"
								? CurrentAgentPersonalityCapabilitiesMultiLangData
								: listType === "ethics"
									? CurrentAgentPersonalityEthicsMultiLangData
									: CurrentAgentPersonalityToneMultiLangData;

						$(this).closest(".input-group").remove();

						// Update data
						currentData[currentSelectedLanguage.id] = Array.from(container.find("input")).map((input) => $(input).val().trim());
						validateAgentMultiLanguageElements();
						CheckAgentTabHasChanges();
					});

					// Value changes
					container.on("input change", "input", () => {
						const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
						const currentData =
							listType === "capabilities"
								? CurrentAgentPersonalityCapabilitiesMultiLangData
								: listType === "ethics"
									? CurrentAgentPersonalityEthicsMultiLangData
									: CurrentAgentPersonalityToneMultiLangData;

						currentData[currentSelectedLanguage.id] = Array.from(container.find("input")).map((input) => $(input).val().trim());
						validateAgentMultiLanguageElements();
						CheckAgentTabHasChanges();
					});
				});

				// Language change handler
				manageAgentsLanguageDropdown.onLanguageChange((language) => {
					// Update name and role
					$("#editAgentPersonalityNameInput").val(CurrentAgentPersonalityNameMultiLangData[language.id] || "");
					$("#editAgentPersonalityRoleInput").val(CurrentAgentPersonalityRoleMultiLangData[language.id] || "");

					// Update lists
					["capabilities", "ethics", "tone"].forEach((listType) => {
						const container = $(`#editAgentPersonality${listType.charAt(0).toUpperCase() + listType.slice(1)}ValueInputs`);
						const currentData =
							listType === "capabilities"
								? CurrentAgentPersonalityCapabilitiesMultiLangData
								: listType === "ethics"
									? CurrentAgentPersonalityEthicsMultiLangData
									: CurrentAgentPersonalityToneMultiLangData;

						container.empty();
						(currentData[language.id] || []).forEach((value) => {
							container.append(`
								<div class="input-group mb-1">
									<input type="text" class="form-control" value="${value}">
									<button class="btn btn-danger" button-type="editAgentPersonalityValueRemove">
										<i class='fa-regular fa-trash'></i>
									</button>
								</div>
							`);
						});
					});

					validateAgentMultiLanguageElements();
				});
			}
			initAgentPersonalityTabHandlers();

			// Utterances Tab Changes
			function initAgentUtterancesTabHandlers() {
				// Opening Type changes
				$("#editAgentGreetingStartTypeInput").on("change", () => {
					CheckAgentTabHasChanges();
				});

				// Greeting Message changes
				$("#editAgentPersonalityGreetingInput").on("input change", (event) => {
					const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
					CurrentAgentUtterancesGreetingMessageMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
					validateAgentMultiLanguageElements();
					CheckAgentTabHasChanges();
				});

				// Phrases Before Reply changes
				$("#editAgentPhrasesBeforeReply").on("input change", (event) => {
					const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
					const phrasesText = $(event.currentTarget).val();

					// Split by comma and clean up each phrase
					CurrentAgentUtterancesPhrasesBeforeReplyMultiLangData[currentSelectedLanguage.id] = phrasesText
						.split(",")
						.map((phrase) => phrase.trim())
						.filter((phrase) => phrase.length > 0);

					validateAgentMultiLanguageElements();
					CheckAgentTabHasChanges();
				});

				// Language change handler
				manageAgentsLanguageDropdown.onLanguageChange((language) => {
					// Update greeting message
					$("#editAgentPersonalityGreetingInput").val(CurrentAgentUtterancesGreetingMessageMultiLangData[language.id] || "");

					// Update phrases before reply
					$("#editAgentPhrasesBeforeReply").val((CurrentAgentUtterancesPhrasesBeforeReplyMultiLangData[language.id] || []).join(", "));

					validateAgentMultiLanguageElements();
				});
			}
			initAgentUtterancesTabHandlers();

			// Cache Tab Changes
			// Message Cache
			messageCacheGroupsList.on("change", 'select[select-type^="cache-message-group"]', () => {
				CheckAgentTabHasChanges();
			});

			messageCacheGroupsList.on("click", '[button-type="remove-cache-group"]', () => {
				CheckAgentTabHasChanges();
			});

			// Audio Cache
			audioCacheGroupsList.on("change", 'select[select-type^="cache-audio-group"]', () => {
				CheckAgentTabHasChanges();
			});

			audioCacheGroupsList.on("click", '[button-type="remove-cache-group"]', () => {
				CheckAgentTabHasChanges();
			});

			// Integration Tab Changes
			agentIntegrationsTab.on("change", 'select[select-type^="integration-"]', () => {
				CheckAgentTabHasChanges();
			});

			agentIntegrationsTab.on("click", '[button-type="remove-integration"]', () => {
				CheckAgentTabHasChanges();
			});

			// Integration Configuration Changes
			integrationConfigurationFieldsContainer.on("input change", ".config-field-input", () => {
				CheckAgentTabHasChanges();
			});

			// Settings Tab Changes
			$("#editAgentBackgroundAudioSelect, #editAgentBackgroundAudioVolume").on("input change", () => {
				CheckAgentTabHasChanges();
			});

			// Handle language changes
			manageAgentsLanguageDropdown.onLanguageChange(() => {
				CheckAgentTabHasChanges();
			});

			// Handle items being added
			addSTTIntegrationButton.on("click", () => {
				setTimeout(() => CheckAgentTabHasChanges(), 100);
			});

			addLLMIntegrationButton.on("click", () => {
				setTimeout(() => CheckAgentTabHasChanges(), 100);
			});

			addTTSIntegrationButton.on("click", () => {
				setTimeout(() => CheckAgentTabHasChanges(), 100);
			});

			addMessageCacheGroupButton.on("click", () => {
				setTimeout(() => CheckAgentTabHasChanges(), 100);
			});

			addAudioCacheGroupButton.on("click", () => {
				setTimeout(() => CheckAgentTabHasChanges(), 100);
			});
		}
		initAgentTabChangeHandlers();
	});
}
