// Graph Constants
const AGENT_SCRIPT_GRAPH_GRID_SIZE = 10;
const AGENT_SCRIPT_GRAPH_BACKGROUND_COLOR = "#f8f9fa";
const AGENT_SCRIPT_GRAPH_DOT_COLOR = "#e9ecef";
const AGENT_SCRIPT_GRAPH_DOT_SIZE = 1;
const AGENT_SCRIPT_GRAPH_PLUGINS = {
	Minimap: X6PluginMinimap.MiniMap,
	Keyboard: X6PluginKeyboard.Keyboard,
	Clipboard: X6PluginClipboard.Clipboard,
	History: X6PluginHistory.History,
};

// Constants for node system
const AGENT_SCRIPT_NODE_TYPES = {
	START: "agent-script-start-node",
	USER_MESSAGE: "agent-script-user-message-node",
	AI_RESPONSE: "agent-script-ai-response-node",
	SYSTEM_TOOL: "agent-script-system-tool-node",
	CUSTOM_TOOL: "agent-script-custom-tool-node",
};

const AGENT_SCRIPT_NODE_WIDTH = 320;
const AGENT_SCRIPT_NODE_MIN_HEIGHT = 100;

/** Dynamic Variables **/
let CurrentManageAgentData = null;
let ManageAgentType = null; // new or edit

let manageAgentsLanguageDropdown = null;
let agentsScriptManagerLanguageDropdown = null;

let AgentBackgroundAudioWaveSurfer = null;

// Integration related states
let CurrentAgentIntegrationsSTT = {};
let CurrentAgentIntegrationsLLM = {};
let CurrentAgentIntegrationsTTS = {};

// Integration Configuration State
let CurrentAgentConfigurationIntegration = null;
let CurrentAgentConfigurationIntegrationType = null;
let CurrentAgentConfigurationFields = null;
let CurrentAgentConfigurationValues = {};
let CurrentAgentConfigurationType = null;

// Cache related states
let CurrentAgentCacheMessages = [];
let CurrentAgentCacheAudios = [];

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

// Script
let CurrentAgentScriptGraph = null;

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

const agentsManagerBreadcrumb = agentsManagerHeader.find("#agents-manager-breadcrumb");
const agentsScriptManagerBreadcrumb = agentsManagerHeader.find("#agents-script-manager-breadcrumb");

const agentsManagerListTab = agentsManagerHeader.find("#agents-manager-tab");
const agentsManagerScriptTab = agentsManagerHeader.find("#agents-manager-script-tab");

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

// SUB | Audio Tab
const agentBackgroundAudioBox = agentTab.find("#agentBackgroundAudioBox");
const agentBackgroundAudioSelect = editAgentBackgroundAudioSelect;
const agentBackgroundAudioInputBox = agentBackgroundAudioBox.find("#agentBackgroundAudioInputBox");
const agentBackgroundAudioUploadBtn = agentBackgroundAudioInputBox.find("#agent-background-audio-upload-btn");
const agentBackgroundAudioUploadInput = agentBackgroundAudioInputBox.find("#agentBackgroundAudioUploadInput");
const agentBackgroundAudioVolumeInput = agentBackgroundAudioBox.find("#agentBackgroundAudioVolumeInput");

/** Functions **/

function showAgentManagerTab() {
	agentsListTab.removeClass("show");
	setTimeout(() => {
		agentsListTab.addClass("d-none");

		agentsManagerTab.removeClass("d-none");
		agentsManagerHeader.removeClass("d-none");
		agentsManagerBreadcrumb.removeClass("d-none");
		agentsManagerListTab.removeClass("d-none");
		setTimeout(() => {
			agentsManagerTab.addClass("show");
			agentsManagerHeader.addClass("show");
			agentsManagerBreadcrumb.addClass("show");
			agentsManagerListTab.addClass("show");

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
			STT: {},
			LLM: {},
			TTS: {},
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

		// Initialize integrations for each language
		agent.integrations.STT[language] = [];
		agent.integrations.LLM[language] = [];
		agent.integrations.TTS[language] = [];
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

		/** Integrations Tab **/
		const sttIntegrationsIncomplete = !CurrentAgentIntegrationsSTT[language]?.length;
		const llmIntegrationsIncomplete = !CurrentAgentIntegrationsLLM[language]?.length;
		const ttsIntegrationsIncomplete = !CurrentAgentIntegrationsTTS[language]?.length;

		const isAnyIncompleteInIntegrations = sttIntegrationsIncomplete || llmIntegrationsIncomplete || ttsIntegrationsIncomplete;

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
		const isAnyIncomplete = isAnyIncompleteInGeneral || isAnyIncompleteInPersonality || isAnyIncompleteInUtterances || isAnyIncompleteInIntegrations;
		manageAgentsLanguageDropdown.setLanguageStatus(currentSelectedLanguage.id, isAnyIncomplete ? "incomplete" : "complete");
	});
}

function ResetAndEmptyAgentsManageTab() {
	// Audio
	if (AgentBackgroundAudioWaveSurfer?.destroy) {
		AgentBackgroundAudioWaveSurfer.destroy();
	}
	AgentBackgroundAudioWaveSurfer = CreateAgentBackgroundAudioWavesurfer("#agent-background-audio-waveform");
	agentBackgroundAudioVolumeInput.val("100");
	agentBackgroundAudioInputBox.find(".no-audio-notice").removeClass("d-none");
	agentBackgroundAudioInputBox.find(".recording-container-waveform").addClass("d-none");
	agentBackgroundAudioInputBox.find(".audio-controller").addClass("d-none");
	agentBackgroundAudioUploadInput.val("");
	agentBackgroundAudioSelect.val("none").change();

	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentIntegrationsSTT[language] = [];
		CurrentAgentIntegrationsLLM[language] = [];
		CurrentAgentIntegrationsTTS[language] = [];
	});
}

function CreateAgentBackgroundAudioWavesurfer(containerId) {
	const waveSurferConversation = WaveSurfer.create({
		container: containerId,
		waveColor: "#5f6833",
		progressColor: "#CBE54E",
		height: 35,
		barWidth: 2,
		barHeight: 0.7,
		fillParent: true,
		audioRate: 1,
		plugins: [
			WaveSurfer.Hover.create({
				lineColor: "#fff",
				lineWidth: 2,
				labelBackground: "#555",
				labelColor: "#fff",
				labelSize: "11px",
			}),
		],
	});

	const audioPlayPauseButton = $(containerId).parent().parent().find('.audio-controller button[button-type="start-stop-audio"]');
	audioPlayPauseButton.on("click", (event) => {
		waveSurferConversation.playPause();

		const currentMode = $(event.currentTarget).attr("mode");

		if (currentMode === "play") {
			$(event.currentTarget).attr("mode", "pause");
			$(event.currentTarget).find("i").removeClass("fa-play").addClass("fa-pause");
		} else {
			$(event.currentTarget).attr("mode", "play");
			$(event.currentTarget).find("i").removeClass("fa-pause").addClass("fa-play");
		}
	});

	waveSurferConversation.on("ready", (duration) => {
		audioPlayPauseButton.prop("disabled", false);
	});

	return waveSurferConversation;
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
function fillIntegrationsFromAgentData(agentData) {
	// Fill integrations for each language from agent data
	BusinessFullData.businessData.languages.forEach((language) => {
		if (agentData.integrations.STT[language]) {
			CurrentAgentIntegrationsSTT[language] = agentData.integrations.STT[language];
		}
		if (agentData.integrations.LLM[language]) {
			CurrentAgentIntegrationsLLM[language] = agentData.integrations.LLM[language];
		}
		if (agentData.integrations.TTS[language]) {
			CurrentAgentIntegrationsTTS[language] = agentData.integrations.TTS[language];
		}
	});
}

function createIntegrationSelectElement(type, index) {
	const integrations = BusinessFullData.businessApp.integrations.filter((integration) => {
		const integrationTypeData = SpecificationIntegrationsListData.find((integrationType) => integrationType.id === integration.type);
		return integrationTypeData.type.includes(type) || (type === "STT" && integrationTypeData.type.includes("SPEECH2TEXT")) || (type === "TTS" && integrationTypeData.type.includes("TEXT2SPEECH"));
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

function fillAgentIntegrationsList(type) {
	const container = type === "STT" ? sttIntegrationsList : type === "LLM" ? llmIntegrationsList : ttsIntegrationsList;

	const currentLanguage = manageAgentsLanguageDropdown.getSelectedLanguage().id;
	const currentIntegrations =
		type === "STT" ? CurrentAgentIntegrationsSTT[currentLanguage] : type === "LLM" ? CurrentAgentIntegrationsLLM[currentLanguage] : CurrentAgentIntegrationsTTS[currentLanguage];

	// Clear existing items
	container.find(".integration-item").remove();

	// Add current integrations for selected language
	if (currentIntegrations && currentIntegrations.length > 0) {
		currentIntegrations.forEach((integration, index) => {
			const element = $(createIntegrationSelectElement(type, index));
			element.find("select").val(integration.id);
			container.append(element);
		});
	}
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
                            <a href="#" class="d-inline-block" data-bs-html="true" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${field.tooltip}">
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
                            <a href="#" class="d-inline-block" data-bs-html="true" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${field.tooltip}">
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
                            <a href="#" class="d-inline-block" data-bs-html="true" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${field.tooltip}">
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
                            <a href="#" class="d-inline-block" data-bs-html="true" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${field.tooltip}">
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
	const provider =
		CurrentAgentConfigurationType === "LLM"
			? BusinessLLMProvidersForIntegrations.find((p) => p.integrationId === CurrentAgentConfigurationIntegrationType)
			: CurrentAgentConfigurationType === "STT"
				? BusinessSTTProvidersForIntegrations.find((p) => p.integrationId === CurrentAgentConfigurationIntegrationType)
				: CurrentAgentConfigurationType === "TTS"
					? BusinessTTSProvidersForIntegrations.find((p) => p.integrationId === CurrentAgentConfigurationIntegrationType)
					: null;

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
	const provider =
		integrationType === "LLM"
			? BusinessLLMProvidersForIntegrations.find((p) => p.integrationId === businessIntegrationData.type)
			: integrationType === "STT"
				? BusinessSTTProvidersForIntegrations.find((p) => p.integrationId === businessIntegrationData.type)
				: integrationType === "TTS"
					? BusinessTTSProvidersForIntegrations.find((p) => p.integrationId === businessIntegrationData.type)
					: null;

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

	if (!currentArray) return;

	const currentLanguageArray = currentArray[manageAgentsLanguageDropdown.getSelectedLanguage().id];

	const existingConfig = currentLanguageArray.find((i) => i && i.id === integrationId);
	if (existingConfig?.fieldValues) {
		CurrentAgentConfigurationValues = { ...existingConfig.fieldValues };
	}

	// Fill the modal with fields
	fillAgentIntegrationConfigurationFields();
}

// TODO CHECK THESE VALIDATION FUNCTIONS
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

				case "models": {
					const provider =
						CurrentAgentConfigurationType === "LLM"
							? BusinessLLMProvidersForIntegrations.find((p) => p.integrationId === CurrentAgentConfigurationIntegrationType)
							: CurrentAgentConfigurationType === "STT"
								? BusinessSTTProvidersForIntegrations.find((p) => p.integrationId === CurrentAgentConfigurationIntegrationType)
								: CurrentAgentConfigurationType === "TTS"
									? BusinessTTSProvidersForIntegrations.find((p) => p.integrationId === CurrentAgentConfigurationIntegrationType)
									: null;

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
				}

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

function validateAgentIntegrationsTab() {
	const errors = [];
	let isValid = true;

	// Validate each language has required integrations
	BusinessFullData.businessData.languages.forEach((languageId) => {
		const language = SpecificationLanguagesListData.find((l) => l.id === languageId);
		const languageName = language ? language.name : languageId;

		// Validate STT integrations
		if (!CurrentAgentIntegrationsSTT[languageId] || CurrentAgentIntegrationsSTT[languageId].length === 0) {
			isValid = false;
			errors.push(`${languageName}: At least one Speech-to-Text integration is required`);
		}

		// Validate LLM integrations
		if (!CurrentAgentIntegrationsLLM[languageId] || CurrentAgentIntegrationsLLM[languageId].length === 0) {
			isValid = false;
			errors.push(`${languageName}: At least one Language Model integration is required`);
		}

		// Validate TTS integrations
		if (!CurrentAgentIntegrationsTTS[languageId] || CurrentAgentIntegrationsTTS[languageId].length === 0) {
			isValid = false;
			errors.push(`${languageName}: At least one Text-to-Speech integration is required`);
		}

		// Validate integration configurations
		if (CurrentAgentIntegrationsSTT[languageId]) {
			CurrentAgentIntegrationsSTT[languageId].forEach((integration, index) => {
				if (!validateAgentIntegrationConfigurationFields(integration, "STT", index, languageName)) {
					isValid = false;
				}
			});
		}

		if (CurrentAgentIntegrationsLLM[languageId]) {
			CurrentAgentIntegrationsLLM[languageId].forEach((integration, index) => {
				if (!validateAgentIntegrationConfigurationFields(integration, "LLM", index, languageName)) {
					isValid = false;
				}
			});
		}

		if (CurrentAgentIntegrationsTTS[languageId]) {
			CurrentAgentIntegrationsTTS[languageId].forEach((integration, index) => {
				if (!validateAgentIntegrationConfigurationFields(integration, "TTS", index, languageName)) {
					isValid = false;
				}
			});
		}
	});

	return {
		isValid,
		errors,
	};
}

function validateAgentIntegrationConfigurationFields(integration, type, index, languageName) {
	const errors = [];
	let isValid = true;

	// Get provider configuration based on integration type
	const businessIntegrationData = BusinessFullData.businessApp.integrations.find((i) => i.id === integration.id);
	if (!businessIntegrationData) {
		errors.push(`${languageName}: ${type} Integration #${index + 1} - Invalid integration selected`);
		return false;
	}

	const provider =
		type === "LLM"
			? BusinessLLMProvidersForIntegrations.find((p) => p.integrationId === businessIntegrationData.type)
			: type === "STT"
				? BusinessSTTProvidersForIntegrations.find((p) => p.integrationId === businessIntegrationData.type)
				: BusinessTTSProvidersForIntegrations.find((p) => p.integrationId === businessIntegrationData.type);

	if (!provider) {
		errors.push(`${languageName}: ${type} Integration #${index + 1} - Provider configuration not found`);
		return false;
	}

	// Validate required fields
	provider.userIntegrationFields.forEach((field) => {
		if (field.required) {
			const value = integration.fieldValues[field.id];
			if (!value || value.trim() === "") {
				isValid = false;
				errors.push(`${languageName}: ${type} Integration #${index + 1} - ${field.name} is required`);
			}
		}
	});

	return isValid;
}
// TODO END

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

	if (!currentArray) return;

	const currentLanguageArray = currentArray[manageAgentsLanguageDropdown.getSelectedLanguage().id];

	const integrationIndex = currentLanguageArray.findIndex((i) => {
		if (!i) return false;

		return i.id === CurrentAgentConfigurationIntegration;
	});
	if (integrationIndex !== -1) {
		// Update existing configuration
		Object.keys(changes).forEach((fieldId) => {
			currentLanguageArray[integrationIndex].fieldValues[fieldId] = changes[fieldId];
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

function refreshAgentIntegrationIndices(type) {
	const container = type === "STT" ? sttIntegrationsList : type === "LLM" ? llmIntegrationsList : ttsIntegrationsList;

	container.find(".integration-item").each((idx, element) => {
		$(element).attr("data-index", idx);
		$(element)
			.find(".input-group-text i")
			.attr("class", `fa-regular fa-${idx + 1}`);
	});
}

// Cache Tab Functions
function createCacheGroupSelectElement(type, index) {
	const groups = type === "message" ? BusinessFullData.businessApp.cache.messageGroups : BusinessFullData.businessApp.cache.audioGroups;

	let options = '<option value="" disabled selected>Select Group</option>';
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

function validateAgentCacheTab(onlyRemove = true) {
	const errors = [];
	let isValid = true;

	// Validate Message Cache Groups
	messageCacheGroupsList.find('select[select-type^="cache-message-group"]').each((index, select) => {
		const value = $(select).val();
		if (!value) {
			isValid = false;
			errors.push(`Message cache group at position ${index + 1} must be selected`);
			if (!onlyRemove) {
				$(select).addClass("is-invalid");
			}
		} else {
			$(select).removeClass("is-invalid");
		}
	});

	// Validate Audio Cache Groups
	audioCacheGroupsList.find('select[select-type^="cache-audio-group"]').each((index, select) => {
		const value = $(select).val();
		if (!value) {
			isValid = false;
			errors.push(`Audio cache group at position ${index + 1} must be selected`);
			if (!onlyRemove) {
				$(select).addClass("is-invalid");
			}
		} else {
			$(select).removeClass("is-invalid");
		}
	});

	// Validate unique selections for message cache groups
	const selectedMessageGroups = new Set();
	messageCacheGroupsList.find('select[select-type^="cache-message-group"]').each((index, select) => {
		const value = $(select).val();
		if (value) {
			if (selectedMessageGroups.has(value)) {
				isValid = false;
				errors.push(`Duplicate message cache group selection at position ${index + 1}`);
				if (!onlyRemove) {
					$(select).addClass("is-invalid");
				}
			}
			selectedMessageGroups.add(value);
		}
	});

	// Validate unique selections for audio cache groups
	const selectedAudioGroups = new Set();
	audioCacheGroupsList.find('select[select-type^="cache-audio-group"]').each((index, select) => {
		const value = $(select).val();
		if (value) {
			if (selectedAudioGroups.has(value)) {
				isValid = false;
				errors.push(`Duplicate audio cache group selection at position ${index + 1}`);
				if (!onlyRemove) {
					$(select).addClass("is-invalid");
				}
			}
			selectedAudioGroups.add(value);
		}
	});

	/**
	// Auto Cache Audio Settings validation (if needed)
	const autoCacheAudioResponses = false; // Add appropriate element check
	const autoCacheExpiryHours = 24; // Add appropriate element check
	const autoCacheGroupId = null; // Add appropriate element check

	if (autoCacheAudioResponses && !autoCacheGroupId) {
		isValid = false;
		errors.push("Auto cache audio group must be selected when auto cache is enabled");
		if (!onlyRemove) {
			// Add invalid class to appropriate element
		}
	}
	**/

	return {
		isValid,
		errors,
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

function onAgentsBackgroundAudioUploadValidation(event) {
	const selectedFile = event.currentTarget.files[0];

	if (selectedFile == null) {
		return false;
	}

	if (selectedFile.size > 25 * 1024 * 1024) {
		AlertManager.createAlert({
			type: "danger",
			message: "Audio file size should not exceed 25MB.",
			enableDismiss: false,
		});

		$(event.currentTarget).val("");
		return false;
	}

	return true;
}

function fillAgentSettingsTab() {
	if (CurrentManageAgentData.settings.backgroundAudioUrl) {
		agentBackgroundAudioSelect.val("custom").change();

		AgentBackgroundAudioWaveSurfer.load(`${BusinessAgentBackgroundAudioURL}/${CurrentManageAgentData.settings.backgroundAudioUrl}`);
		agentBackgroundAudioVolumeInput.val(CurrentManageAgentData.settings.backgroundAudioVolume);

		agentBackgroundAudioInputBox.find(".no-audio-notice").addClass("d-none");
		agentBackgroundAudioInputBox.find(".recording-container-waveform").removeClass("d-none");
		agentBackgroundAudioInputBox.find(".audio-controller").removeClass("d-none");
	}
}

// Scripts Tab Functions
function showAgentScriptManagerTab() {
	agentScriptsListTab.removeClass("show");
	agentsManagerBreadcrumb.removeClass("show");
	agentsManagerListTab.removeClass("show");
	setTimeout(() => {
		agentScriptsListTab.addClass("d-none");
		agentsManagerBreadcrumb.addClass("d-none");
		agentsManagerListTab.addClass("d-none");

		agentScriptsManagerTab.removeClass("d-none");
		agentsManagerScriptTab.removeClass("d-none");
		agentsScriptManagerBreadcrumb.removeClass("d-none");
		setTimeout(() => {
			agentScriptsManagerTab.addClass("show");
			agentsManagerScriptTab.addClass("show");
			agentsScriptManagerBreadcrumb.addClass("show");
		}, 10);
	}, 300);
}

function showAgentScriptListTab() {
	agentScriptsManagerTab.removeClass("show");
	agentsManagerScriptTab.removeClass("show");
	agentsScriptManagerBreadcrumb.removeClass("show");
	setTimeout(() => {
		agentScriptsManagerTab.addClass("d-none");
		agentsManagerScriptTab.addClass("d-none");
		agentsScriptManagerBreadcrumb.addClass("d-none");

		agentScriptsListTab.removeClass("d-none");
		agentsManagerBreadcrumb.removeClass("d-none");
		agentsManagerListTab.removeClass("d-none");
		setTimeout(() => {
			agentScriptsListTab.addClass("show");
			agentsManagerBreadcrumb.addClass("show");
			agentsManagerListTab.addClass("show");
		}, 10);
	}, 300);
}

function ResetAndEmptyAgentsScriptManageTab() {
	if (CurrentAgentScriptGraph) {
		CurrentAgentScriptGraph.dispose();
		CurrentAgentScriptGraph = null;
	}
}

async function canLeaveAgentScriptManagerTab() {
	// todo

	return true;
}

function initializeAgentScriptGraph(container) {
	resizeAgentScriptGraphCSS();

	// Create the graph instance
	const graph = new X6.Graph({
		container: container,
		width: "100%",
		height: "100%",
		// Grid settings
		grid: {
			visible: true,
			type: "doubleMesh",
			args: [
				{
					color: AGENT_SCRIPT_GRAPH_DOT_COLOR,
					thickness: AGENT_SCRIPT_GRAPH_DOT_SIZE,
				},
				{
					color: AGENT_SCRIPT_GRAPH_DOT_COLOR,
					thickness: AGENT_SCRIPT_GRAPH_DOT_SIZE,
					factor: 4,
				},
			],
		},
		// Background settings
		background: {
			color: AGENT_SCRIPT_GRAPH_BACKGROUND_COLOR,
		},
		// Interaction settings
		mousewheel: {
			enabled: true,
			modifiers: ["ctrl", "meta"],
			factor: 1.1,
			maxScale: 2,
			minScale: 0.5,
		},
		panning: {
			enabled: true,
			modifiers: ["shift", "ctrl"],
		},
		connecting: {
			anchor: "center",
			connectionPoint: "anchor",
			allowBlank: false,
			allowLoop: false,
			allowNode: false,
			allowEdge: false,
			allowMulti: false,
			highlight: true,
		},
		// Prevent node text selection
		preventDefaultContextMenu: true,
		preventDefaultBlankAction: true,
	});

	// Add minimap plugin
	const minimapContainer = document.getElementById("agent-script-graph-minimap");
	if (minimapContainer) {
		graph.use(
			new AGENT_SCRIPT_GRAPH_PLUGINS.Minimap({
				container: minimapContainer,
				width: 200,
				height: 150,
				padding: 10,
			}),
		);
	}

	// Add keyboard shortcuts plugin
	if (AGENT_SCRIPT_GRAPH_PLUGINS.Keyboard) {
		graph.use(
			new AGENT_SCRIPT_GRAPH_PLUGINS.Keyboard({
				enabled: true,
				global: true,
			}),
		);
	}

	// Add clipboard plugin
	if (AGENT_SCRIPT_GRAPH_PLUGINS.Clipboard) {
		graph.use(
			new AGENT_SCRIPT_GRAPH_PLUGINS.Clipboard({
				enabled: true,
			}),
		);
	}

	// Add history plugin (undo/redo)
	if (AGENT_SCRIPT_GRAPH_PLUGINS.History) {
		graph.use(
			new AGENT_SCRIPT_GRAPH_PLUGINS.History({
				enabled: true,
				beforeAddCommand: (event, args) => {
					// Validate before adding to history
					return true;
				},
			}),
		);
	}

	// Initialize node system
	initializeAgentScriptNodeSystem(graph);

	// Add start node
	const startNode = graph.addNode({
		shape: AGENT_SCRIPT_NODE_TYPES.START,
		data: { type: AGENT_SCRIPT_NODE_TYPES.START },
		x: 100,
		y: 50,
		ports: {
			items: [{ group: "output" }],
		},
	});

	return graph;
}

function initializeAgentScriptGraphControls(graph) {
	// Zoom controls
	$("#agent-script-graph-zoom-in").on("click", () => {
		const zoom = graph.zoom();
		if (zoom < 2) {
			graph.zoom(0.1);
		}
	});

	$("#agent-script-graph-zoom-out").on("click", () => {
		const zoom = graph.zoom();
		if (zoom > 0.5) {
			graph.zoom(-0.1);
		}
	});

	$("#agent-script-graph-zoom-fit").on("click", () => {
		graph.zoomToFit({ padding: 20, maxScale: 1 });
	});

	$("#agent-script-graph-zoom-reset").on("click", () => {
		graph.scale(1);
		graph.centerContent();
	});

	// Undo/Redo controls
	$("#agent-script-graph-undo").on("click", () => {
		if (graph.canUndo()) {
			graph.undo();
		}
	});

	$("#agent-script-graph-redo").on("click", () => {
		if (graph.canRedo()) {
			graph.redo();
		}
	});

	// Update button states based on graph state
	graph.on("scale", ({ sx, sy }) => {
		const scale = sx; // or sy, they're the same
		$("#agent-script-graph-zoom-in").prop("disabled", scale >= 2);
		$("#agent-script-graph-zoom-out").prop("disabled", scale <= 0.5);
	});

	graph.on("history:change", () => {
		$("#agent-script-graph-undo").prop("disabled", !graph.canUndo());
		$("#agent-script-graph-redo").prop("disabled", !graph.canRedo());
	});
}

function registerAgentScriptNodes() {
	// Register Start Node
	X6.Shape.HTML.register({
		shape: AGENT_SCRIPT_NODE_TYPES.START,
		width: 100, // Smaller width for pill shape
		height: 40, // Fixed height for pill shape
		effect: ["data"],
		ports: {
			groups: {
				output: {
					position: "bottom",
					attrs: {
						circle: {
							r: 6,
							magnet: true,
							stroke: "#198754",
							strokeWidth: 2,
							fill: "#fff",
						},
					},
				},
			},
		},
		html(cell) {
			const div = document.createElement("div");
			div.className = `agent-script-node ${AGENT_SCRIPT_NODE_TYPES.START}`;

			div.innerHTML = `
                <div class="agent-script-node-content">
                    Start
                </div>
            `;

			return div;
		},
	});

	// Register User Message Node
	X6.Shape.HTML.register({
		shape: AGENT_SCRIPT_NODE_TYPES.USER_MESSAGE,
		width: AGENT_SCRIPT_NODE_WIDTH,
		height: AGENT_SCRIPT_NODE_MIN_HEIGHT,
		effect: ["data"],
		ports: {
			groups: {
				input: {
					position: "top",
					attrs: {
						circle: {
							r: 6,
							magnet: true,
							stroke: "#8f8f8f",
							strokeWidth: 2,
							fill: "#fff",
						},
					},
				},
				output: {
					position: "bottom",
					attrs: {
						circle: {
							r: 6,
							magnet: true,
							stroke: "#8f8f8f",
							strokeWidth: 2,
							fill: "#fff",
						},
					},
				},
			},
		},
		html(cell) {
			const div = document.createElement("div");
			div.className = `agent-script-node ${AGENT_SCRIPT_NODE_TYPES.USER_MESSAGE}`;

			const data = cell.getData() || {};
			const currentLanguage = manageAgentsLanguageDropdown.getSelectedLanguage().id;

			div.innerHTML = `
                <div class="agent-script-node-header">
                    <button class="agent-script-node-delete-btn" data-action="delete-node">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </div>
                <div class="agent-script-node-content">
                    <div class="agent-script-node-input-group">
                        <label>User Message</label>
                        <textarea 
                            class="form-control" 
                            placeholder="Type the user message..."
                            data-input="user-message"
                        >${data.userMessage?.[currentLanguage] || ""}</textarea>
                    </div>
                    <div class="agent-script-node-response-type">
                        <select class="form-select" data-input="response-type">
                            <option value="ai_response" ${data.responseType === "ai_response" ? "selected" : ""}>AI Response</option>
                            <option value="system_tool" ${data.responseType === "system_tool" ? "selected" : ""}>System Tool</option>
                            <option value="custom_tool" ${data.responseType === "custom_tool" ? "selected" : ""}>Custom Tool</option>
                        </select>
                    </div>
                </div>
            `;

			// Add event listeners
			const textarea = div.querySelector('[data-input="user-message"]');
			textarea.addEventListener("input", (e) => {
				const messages = data.userMessage || {};
				messages[currentLanguage] = e.target.value;
				cell.setData({
					...data,
					userMessage: messages,
				});
			});

			const select = div.querySelector('[data-input="response-type"]');
			select.addEventListener("change", (e) => {
				cell.setData({
					...data,
					responseType: e.target.value,
				});
			});

			return div;
		},
	});
}

function initializeAgentScriptNodeSystem(graph) {
	registerAgentScriptNodes();

	// Handle node deletion
	graph.on("cell:click", ({ cell, e }) => {
		const target = e.target;
		if (target.closest('[data-action="delete-node"]')) {
			const nodeType = cell.getData()?.type;
			if (nodeType !== AGENT_SCRIPT_NODE_TYPES.START) {
				cell.remove();
			}
		}
	});
}

function resizeAgentScriptGraphCSS() {
	const currentInnerContentHeight = agentTab.find(".inner-container")[0].clientHeight - 64; // 64 = 32+32 padding
	const currentInnerContentWidth = agentTab.find(".inner-container")[0].clientWidth - 64; // 64 = 32+32 padding

	$("#agentGraphDyanmicStyle").html(`
		#agent-script-graph {
			width: ${currentInnerContentWidth}px;
			height: ${currentInnerContentHeight}px;
		}
	`);
}

function addAgentScriptUserMessageNode(graph, x = 100, y = 200) {
	return graph.addNode({
		shape: AGENT_SCRIPT_NODE_TYPES.USER_MESSAGE,
		data: {
			type: AGENT_SCRIPT_NODE_TYPES.USER_MESSAGE,
			userMessage: {},
			responseType: "ai_response",
		},
		x,
		y,
		ports: {
			items: [{ group: "input" }, { group: "output" }],
		},
	});
}

/** INIT **/
function initAgentTab() {
	$(document).ready(() => {
		manageAgentsLanguageDropdown = new MultiLanguageDropdown("agentsManagerMultiLanguageContainer", BusinessFullLanguagesData);
		agentsScriptManagerLanguageDropdown = new MultiLanguageDropdown("agentsScriptManagerMultiLanguageContainer", BusinessFullLanguagesData);

		/** Event Handlers **/
		addNewAgentButton.on("click", (event) => {
			event.preventDefault();

			currentAgentName.text("New Agent");
			CurrentManageAgentData = createDefaultAgentObject();

			ResetAndEmptyAgentsManageTab();
			showAgentManagerTab();

			ManageAgentType = "new";
		});

		switchBackToAgentsTab.on("click", (event) => {
			event.preventDefault();

			showAgentListTab();
		});

		function initAgentTabChangeHandlers() {
			// General Tab Changes
			function initAgentGeneralTabHandlers() {
				// Name input changes
				$("#editAgentIdentifierInput").on("input change", (event) => {
					const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
					CurrentAgentGeneralNameMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
					validateAgentMultiLanguageElements();
					validateAgentGeneralTab(true);
					CheckAgentTabHasChanges();
				});

				// Description input changes
				$("#editAgentDescriptionInput").on("input change", (event) => {
					const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
					CurrentAgentGeneralDescriptionMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
					validateAgentMultiLanguageElements();
					validateAgentGeneralTab(true);
					CheckAgentTabHasChanges();
				});

				// Language change handler
				manageAgentsLanguageDropdown.onLanguageChange((language) => {
					// Update name field
					$("#editAgentIdentifierInput").val(CurrentAgentGeneralNameMultiLangData[language.id] || "");

					// Update description field
					$("#editAgentDescriptionInput").val(CurrentAgentGeneralDescriptionMultiLangData[language.id] || "");

					validateAgentMultiLanguageElements();
					validateAgentGeneralTab(true);
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
					validateAgentPersonalityTab(true);
					CheckAgentTabHasChanges();
				});

				// Role input changes
				$("#editAgentPersonalityRoleInput").on("input change", (event) => {
					const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
					CurrentAgentPersonalityRoleMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
					validateAgentMultiLanguageElements();
					validateAgentPersonalityTab(true);
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
						validateAgentPersonalityTab(true);
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
						validateAgentPersonalityTab(true);
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
						validateAgentPersonalityTab(true);
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
					validateAgentPersonalityTab(true);
				});
			}
			initAgentPersonalityTabHandlers();

			// Utterances Tab Changes
			function initAgentUtterancesTabHandlers() {
				// Opening Type changes
				$("#editAgentGreetingStartTypeInput").on("change", () => {
					CheckAgentTabHasChanges();
					validateAgentUtterancesTab(true);
				});

				// Greeting Message changes
				$("#editAgentPersonalityGreetingInput").on("input change", (event) => {
					const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
					CurrentAgentUtterancesGreetingMessageMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
					validateAgentMultiLanguageElements();
					validateAgentUtterancesTab(true);
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
					validateAgentUtterancesTab(true);
					CheckAgentTabHasChanges();
				});

				// Language change handler
				manageAgentsLanguageDropdown.onLanguageChange((language) => {
					// Update greeting message
					$("#editAgentPersonalityGreetingInput").val(CurrentAgentUtterancesGreetingMessageMultiLangData[language.id] || "");

					// Update phrases before reply
					$("#editAgentPhrasesBeforeReply").val((CurrentAgentUtterancesPhrasesBeforeReplyMultiLangData[language.id] || []).join(", "));

					validateAgentMultiLanguageElements();
					validateAgentUtterancesTab(true);
				});
			}
			initAgentUtterancesTabHandlers();

			// Cache Tab Changes
			function initAgentCacheTabHandlers() {
				// Message Cache
				addMessageCacheGroupButton.on("click", (event) => {
					event.preventDefault();
					const newIndex = messageCacheGroupsList.find(".cache-group-item").length;
					messageCacheGroupsList.append(createCacheGroupSelectElement("message", newIndex));

					CheckAgentTabHasChanges();
				});

				messageCacheGroupsList.on("change", 'select[select-type^="cache-message-group"]', (event) => {
					const currentElement = $(event.currentTarget);
					const currentValue = currentElement.val();

					// check if select has this value
					const allSelectElements = messageCacheGroupsList.find(`select[select-type="cache-message-group"]`);
					const anyHasSelectedValue = allSelectElements.filter((index, select) => $(select).val() === currentValue).length > 1;

					if (anyHasSelectedValue) {
						AlertManager.createAlert({
							type: "warning",
							message: "Message cache group has already been selected.",
						});
						currentElement.val("");
					}

					const index = currentElement.closest(".cache-group-item").data("index");
					if (currentValue) {
						CurrentAgentCacheMessages[index] = currentValue;
					} else {
						CurrentAgentCacheMessages.splice(index, 1);
					}

					validateAgentCacheTab(true);
					CheckAgentTabHasChanges();
				});

				messageCacheGroupsList.on("click", '[button-type="remove-cache-group"]', (event) => {
					$(event.currentTarget).closest(".cache-group-item").remove();

					validateAgentCacheTab(true);
					CheckAgentTabHasChanges();
				});

				// Audio Cache
				addAudioCacheGroupButton.on("click", (event) => {
					event.preventDefault();
					const newIndex = audioCacheGroupsList.find(".cache-group-item").length;
					audioCacheGroupsList.append(createCacheGroupSelectElement("audio", newIndex));

					CheckAgentTabHasChanges();
				});

				audioCacheGroupsList.on("change", 'select[select-type^="cache-audio-group"]', (event) => {
					const currentElement = $(event.currentTarget);
					const currentValue = currentElement.val();

					// check if select has this value
					const allSelectElements = audioCacheGroupsList.find(`select[select-type="cache-audio-group"]`);
					const anyHasSelectedValue = allSelectElements.filter((index, select) => $(select).val() === currentValue).length > 1;

					if (anyHasSelectedValue) {
						AlertManager.createAlert({
							type: "warning",
							message: "Audio cache group has already been selected.",
						});
						currentElement.val("");
					}

					const index = currentElement.closest(".cache-group-item").data("index");
					if (currentValue) {
						CurrentAgentCacheAudios[index] = currentValue;
					} else {
						CurrentAgentCacheAudios.splice(index, 1);
					}

					validateAgentCacheTab(true);
					CheckAgentTabHasChanges();
				});

				audioCacheGroupsList.on("click", '[button-type="remove-cache-group"]', (event) => {
					$(event.currentTarget).closest(".cache-group-item").remove();

					validateAgentCacheTab(true);
					CheckAgentTabHasChanges();
				});
			}
			initAgentCacheTabHandlers();

			// Integration Tab Changes
			function initAgentIntegrationsTabHandlers() {
				manageAgentsLanguageDropdown.onLanguageChange((language) => {
					fillAgentIntegrationsList("STT");
					fillAgentIntegrationsList("LLM");
					fillAgentIntegrationsList("TTS");
				});

				// Integration Event Handlers
				addSTTIntegrationButton.on("click", (event) => {
					event.preventDefault();
					const newIndex = sttIntegrationsList.find(".integration-item").length;
					sttIntegrationsList.append(createIntegrationSelectElement("STT", newIndex));

					CheckAgentTabHasChanges();
				});

				addLLMIntegrationButton.on("click", (event) => {
					event.preventDefault();
					const newIndex = llmIntegrationsList.find(".integration-item").length;
					llmIntegrationsList.append(createIntegrationSelectElement("LLM", newIndex));

					CheckAgentTabHasChanges();
				});

				addTTSIntegrationButton.on("click", (event) => {
					event.preventDefault();
					const newIndex = ttsIntegrationsList.find(".integration-item").length;
					ttsIntegrationsList.append(createIntegrationSelectElement("TTS", newIndex));

					CheckAgentTabHasChanges();
				});

				// Handle integration removal
				agentIntegrationsTab.on("click", '[button-type="remove-integration"]', (event) => {
					event.preventDefault();

					const currentElement = $(event.currentTarget);
					const dataIndex = parseInt(currentElement.attr("data-index"));
					const type = currentElement.parent().find('[select-type^="integration-"]').attr("select-type").split("-")[1].toUpperCase();
					const currentLanguage = manageAgentsLanguageDropdown.getSelectedLanguage().id;

					// Remove the integration
					if (type === "STT") CurrentAgentIntegrationsSTT[currentLanguage].splice(dataIndex, 1);
					else if (type === "LLM") CurrentAgentIntegrationsLLM[currentLanguage].splice(dataIndex, 1);
					else if (type === "TTS") CurrentAgentIntegrationsTTS[currentLanguage].splice(dataIndex, 1);

					currentElement.closest(".integration-item").remove();

					// Refresh indices
					refreshAgentIntegrationIndices(type);
					CheckAgentTabHasChanges();
					validateAgentMultiLanguageElements();
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

				// Handle integration selection changes
				agentIntegrationsTab.on("change", 'select[select-type^="integration-"]', (event) => {
					const currentElement = $(event.currentTarget);
					const type = currentElement.attr("select-type").split("-")[1].toUpperCase();
					const index = currentElement.closest(".integration-item").data("index");
					const value = currentElement.val();
					const currentLanguage = manageAgentsLanguageDropdown.getSelectedLanguage().id;

					const currentArray =
						type === "STT" ? CurrentAgentIntegrationsSTT[currentLanguage] : type === "LLM" ? CurrentAgentIntegrationsLLM[currentLanguage] : CurrentAgentIntegrationsTTS[currentLanguage];

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

					CheckAgentTabHasChanges();
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

					CheckAgentTabHasChanges();
				});
			}
			initAgentIntegrationsTabHandlers();

			// Settings Tab Changes
			function initAgentSettingsTabHandlers() {
				$("#editAgentBackgroundAudioSelect, #editAgentBackgroundAudioVolume").on("input change", () => {
					CheckAgentTabHasChanges();
				});

				editAgentBackgroundAudioSelect.on("change", (event) => {
					const selectedValue = $(event.currentTarget).val();

					if (selectedValue === "none") {
						agentBackgroundAudioBox.addClass("d-none");
						agentBackgroundAudioUploadInput.val("");

						agentBackgroundAudioInputBox.find(".no-audio-notice").removeClass("d-none");
						agentBackgroundAudioInputBox.find(".recording-container-waveform").addClass("d-none");
						agentBackgroundAudioInputBox.find(".audio-controller").addClass("d-none");
					} else {
						agentBackgroundAudioBox.removeClass("d-none");
					}

					if (selectedValue === "custom") {
						agentBackgroundAudioInputBox.removeClass("d-none");
					} else {
						agentBackgroundAudioInputBox.addClass("d-none");
					}
				});

				agentBackgroundAudioUploadBtn.on("click", (event) => {
					event.preventDefault();

					agentBackgroundAudioUploadInput.click();
				});

				agentBackgroundAudioUploadInput.on("change", (event) => {
					const resultValidate = onAgentsBackgroundAudioUploadValidation(event);

					if (resultValidate) {
						const file = agentBackgroundAudioUploadInput[0].files[0];

						const reader = new FileReader();

						reader.onload = (evt) => {
							const blob = new window.Blob([new Uint8Array(evt.target.result)]);
							AgentBackgroundAudioWaveSurfer.loadBlob(blob);

							agentBackgroundAudioInputBox.find(".no-audio-notice").addClass("d-none");
							agentBackgroundAudioInputBox.find(".recording-container-waveform").removeClass("d-none");
							agentBackgroundAudioInputBox.find(".audio-controller").removeClass("d-none");
						};

						reader.onerror = (evt) => {
							AlertManager.createAlert({
								type: "error",
								message: "Error reading audio file for agenst background audio upload.",
								enableDismiss: false,
							});
						};

						// Read File as an ArrayBuffer
						reader.readAsArrayBuffer(file);
					}
				});
			}
			initAgentSettingsTabHandlers();

			function initAgentScriptsTabHandlers() {
				addNewAgentScriptButton.on("click", (event) => {
					event.preventDefault();

					ResetAndEmptyAgentsScriptManageTab();
					CurrentAgentScriptGraph = initializeAgentScriptGraph(document.getElementById("agent-script-graph"));
					initializeAgentScriptGraphControls(CurrentAgentScriptGraph);

					currentAgentScriptName.text("New Script");
					showAgentScriptManagerTab();
				});

				switchBackToAgentsScriptManagerTab.on("click", async (event) => {
					event.preventDefault();

					const canLeave = await canLeaveAgentScriptManagerTab();
					if (!canLeave) return;

					showAgentScriptListTab();
				});

				$("#agent-script-add-user-message").on("click", () => {
					addAgentScriptUserMessageNode(CurrentAgentScriptGraph);
				});
			}
			initAgentScriptsTabHandlers();

			// Handle language changes
			manageAgentsLanguageDropdown.onLanguageChange(() => {
				CheckAgentTabHasChanges();
			});

			$(window).on("resize", () => {
				if (ManageAgentType == null) return;

				setTimeout(() => {
					resizeAgentScriptGraphCSS();
				}, 500);
			});
		}
		initAgentTabChangeHandlers();
	});
}
