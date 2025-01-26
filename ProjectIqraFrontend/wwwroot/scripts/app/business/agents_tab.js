// Graph Constants
const AGENT_SCRIPT_GRAPH_PLUGINS = {
	Minimap: X6PluginMinimap.MiniMap,
	Keyboard: X6PluginKeyboard.Keyboard,
	Clipboard: X6PluginClipboard.Clipboard,
	History: X6PluginHistory.History,
	Snapline: X6PluginSnapline.Snapline,
	Selection: X6PluginSelection.Selection,
};

// Constants for node system
const AGENT_SCRIPT_NODE_TYPES = {
	START: "agent-script-start-node",
	USER_QUERY: "agent-script-user-query-node",
	AI_RESPONSE: "agent-script-ai-response-node",
	SYSTEM_TOOL: "agent-script-system-tool-node",
	CUSTOM_TOOL: "agent-script-custom-tool-node",
};

const AGENT_SCRIPT_RESPONSE_TYPES = {
	AI_RESPONSE: "ai_response",
	SYSTEM_TOOL: "system_tool",
	CUSTOM_TOOL: "custom_tool",
};

const AGENT_SCRIPT_SYSTEM_TOOLS = {
	END_CALL: "end_call",
	CHANGE_LANGUAGE: "change_language",
	GET_DTMF_INPUT: "get_dtmf_keypad_input",
	PRESS_DTMF: "press_dtmf_keypad",
	TRANSFER_TO_AGENT: "transfer_to_agent",
	TRANSFER_TO_HUMAN: "transfer_to_human",
	ADD_SCRIPT_TO_CONTEXT: "add_script_to_context",
};

const AGENT_SCRIPT_NODE_WIDTH = 520; // todo make dynamic
const AGENT_SCRIPT_NODE_MIN_HEIGHT = 300;

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
let CurrentAgentScriptGraphStartNode = null;

let agentScriptDMTFNextOutcomeIndex = 0;

let CurrentCanvasConfigCell = null;
let nodeConfigOffcanvas = null;

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

function createDefaultAgentScriptObject() {
	return {
		id: "",
		isDefault: false,
		isInContext: false,
		general: {
			name: {},
			description: {},
			conditions: {},
		},
		nodes: [],
		edges: [],
	};
}

// Script Graph
function registerAgentScriptNodes() {
	// Register Start Node
	X6.Shape.HTML.register({
		shape: AGENT_SCRIPT_NODE_TYPES.START,
		width: 250, // Smaller width for pill shape
		height: 70, // Fixed height for pill shape
		effect: [],
		ports: {
			groups: {
				output: {
					position: "bottom",
					attrs: {
						circle: {
							r: 8,
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
                    <div class="btn-ic-span-align">
						<i class="fa-regular fa-flag"></i>
						<span>Start</span>
					</div>
                </div>
            `;

			return div;
		},
	});

	// User Query/Message Node
	X6.Shape.HTML.register({
		shape: AGENT_SCRIPT_NODE_TYPES.USER_QUERY,
		width: AGENT_SCRIPT_NODE_WIDTH,
		height: AGENT_SCRIPT_NODE_MIN_HEIGHT,
		effect: [],
		ports: {
			groups: {
				input: {
					position: "top",
					attrs: {
						circle: {
							r: 10,
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
							r: 8,
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
			div.className = `agent-script-node ${AGENT_SCRIPT_NODE_TYPES.USER_QUERY}`;

			const data = cell.getData() || {};
			const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

			div.innerHTML = `
                <div class="agent-script-node-header">
                    <div class="d-flex align-items-center btn-ic-span-align">
                        <i class="fa-regular fa-message me-2"></i>
                        <span>User Query</span>
                    </div>
                    <div class="node-actions html-shape-immovable">
                        <button class="btn btn-light btn-sm me-2" data-action="configure-user-query">
                            <i class="fa-regular fa-gear"></i>
                        </button>
                        <button class="btn btn-danger btn-sm" data-action="delete-node">
                            <i class="fa-regular fa-trash"></i>
                        </button>
                    </div>
                </div>
                <div class="agent-script-node-content">
                    <div class="agent-script-node-input-group html-shape-immovable">
                        <textarea 
                            class="form-control" 
                            placeholder="Type the user query..."
                            data-input="user-query"
                            rows="3"
                        >${data.query?.[currentLanguage] || ""}</textarea>
                    </div>
                </div>
            `;

			setTimeout(() => {
				updateAgentScriptGraphNodeSize(cell, div);
			}, 10);

			return div;
		},
	});

	// AI Response Node
	X6.Shape.HTML.register({
		shape: AGENT_SCRIPT_NODE_TYPES.AI_RESPONSE,
		width: AGENT_SCRIPT_NODE_WIDTH,
		height: AGENT_SCRIPT_NODE_MIN_HEIGHT,
		effect: [],
		ports: {
			groups: {
				input: {
					position: "top",
					attrs: {
						circle: {
							r: 10,
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
							r: 8,
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
			div.className = `agent-script-node ${AGENT_SCRIPT_NODE_TYPES.AI_RESPONSE}`;

			const data = cell.getData() || {};
			const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

			div.innerHTML = `
                <div class="agent-script-node-header">
                    <div class="d-flex align-items-center btn-ic-span-align">
                        <i class="fa-regular fa-robot me-2"></i>
                        <span>AI Response</span>
                    </div>
                    <div class="node-actions html-shape-immovable">
                        <button class="btn btn-light btn-sm me-2" data-action="configure-ai-response">
                            <i class="fa-regular fa-gear"></i>
                        </button>
                        <button class="btn btn-danger btn-sm" data-action="delete-node">
                            <i class="fa-regular fa-trash"></i>
                        </button>
                    </div>
                </div>
                <div class="agent-script-node-content">
                    <div class="agent-script-node-input-group html-shape-immovable">
                        <textarea 
                            class="form-control" 
                            placeholder="Type the AI response..."
                            data-input="ai-response"
                            rows="3"
                        >${data.response?.[currentLanguage] || ""}</textarea>
                    </div>
                </div>
            `;

			setTimeout(() => {
				updateAgentScriptGraphNodeSize(cell, div);
			}, 10);

			return div;
		},
	});

	// System Tool Node
	X6.Shape.HTML.register({
		shape: AGENT_SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		width: AGENT_SCRIPT_NODE_WIDTH,
		height: AGENT_SCRIPT_NODE_MIN_HEIGHT,
		effect: [],
		ports: {
			groups: {
				input: {
					position: "top",
					attrs: {
						circle: {
							r: 10,
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
							r: 8,
							magnet: true,
							stroke: "#8f8f8f",
							strokeWidth: 2,
							fill: "#fff",
						},
						text: {
							fill: "#fff",
						},
					},
					label: {
						position: "bottom",
					},
				},
			},
		},
		html(cell) {
			const div = document.createElement("div");
			div.className = `agent-script-node ${AGENT_SCRIPT_NODE_TYPES.SYSTEM_TOOL}`;

			const data = cell.getData() || {};
			const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

			div.innerHTML = `
                <div class="agent-script-node-header">
                    <div class="d-flex align-items-center btn-ic-span-align">
                        <i class="fa-regular fa-toolbox me-2"></i>
                        <span>System Tool</span>
                    </div>
                    <div class="node-actions html-shape-immovable">
						<button class="btn btn-light btn-sm me-2" data-action="configure-system-tool" disabled>
							<i class="fa-regular fa-gear"></i>
						</button>
                        <button class="btn btn-danger btn-sm" data-action="delete-node">
                            <i class="fa-regular fa-trash"></i>
                        </button>
                    </div>
                </div>
                <div class="agent-script-node-content">
                    <div class="agent-script-node-input-group html-shape-immovable">
                        <div class="d-flex gap-2">
                            <select class="form-select" data-input="system-tool-type">
                                <option value="" disabled ${!data.toolType ? "selected" : ""}>Select Tool</option>
                                <option value="end_call" ${data.toolType === "end_call" ? "selected" : ""}>End Call</option>
                                <option value="change_language" ${data.toolType === "change_language" ? "selected" : ""}>Change Language</option>
                                <option value="get_dtmf_keypad_input" ${data.toolType === "get_dtmf_keypad_input" ? "selected" : ""}>Get DTMF Keypad Input</option>
                                <option value="press_dtmf_keypad" ${data.toolType === "press_dtmf_keypad" ? "selected" : ""}>Press DTMF Keypad</option>
                                <option value="transfer_to_agent" ${data.toolType === "transfer_to_agent" ? "selected" : ""}>Transfer to Agent</option>
                                <option value="transfer_to_human" ${data.toolType === "transfer_to_human" ? "selected" : ""}>Transfer to Human</option>
                                <option value="add_script_to_context" ${data.toolType === "add_script_to_context" ? "selected" : ""}>Add Script to Context</option>
                            </select>
                        </div>
                    </div>
                </div>
            `;

			setTimeout(() => {
				updateAgentScriptGraphNodeSize(cell, div);
			}, 10);

			return div;
		},
	});

	// Custom Tool Node
	X6.Shape.HTML.register({
		shape: AGENT_SCRIPT_NODE_TYPES.CUSTOM_TOOL,
		width: AGENT_SCRIPT_NODE_WIDTH,
		height: AGENT_SCRIPT_NODE_MIN_HEIGHT,
		effect: [],
		ports: {
			groups: {
				input: {
					position: "top",
					attrs: {
						circle: {
							r: 10,
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
							r: 8,
							magnet: true,
							stroke: "#8f8f8f",
							strokeWidth: 2,
							fill: "#fff",
						},
						text: {
							fill: "#fff",
						},
					},
					label: {
						position: "bottom",
					},
				},
			},
		},
		html(cell) {
			const div = document.createElement("div");
			div.className = `agent-script-node ${AGENT_SCRIPT_NODE_TYPES.CUSTOM_TOOL}`;

			const data = cell.getData() || {};
			const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

			// Get available custom tools
			const tools = BusinessFullData.businessApp.tools || [];
			const toolOptions = tools
				.map(
					(tool) => `
                    <option value="${tool.id}" ${data.toolId === tool.id ? "selected" : ""}>
                        ${tool.general.name[currentLanguage] || tool.general.name["en-us"] || "Unnamed Tool"}
                    </option>
                `,
				)
				.join("");

			div.innerHTML = `
                <div class="agent-script-node-header">
                    <div class="d-flex align-items-center btn-ic-span-align">
                        <i class="fa-regular fa-wrench me-2"></i>
                        <span>Custom Tool</span>
                    </div>
                    <div class="node-actions html-shape-immovable">
                        <button class="btn btn-light btn-sm" data-action="configure-custom-tool" disabled>
							<i class="fa-regular fa-gear"></i>
						</button>
                        <button class="btn btn-danger btn-sm" data-action="delete-node">
                            <i class="fa-regular fa-trash"></i>
                        </button>
                    </div>
                </div>
                <div class="agent-script-node-content">
                    <div class="agent-script-node-input-group html-shape-immovable">
                        <select class="form-select" data-input="custom-tool-select">
                            <option value="" disabled ${!data.toolId ? "selected" : ""}>Select Tool</option>
                            ${toolOptions}
                        </select>
                    </div>
                </div>
            `;

			setTimeout(() => {
				updateAgentScriptGraphNodeSize(cell, div);
			}, 10);

			return div;
		},
	});
}

function initializeAgentScriptGraph(container) {
	return resizeAgentScriptGraphCSS((graphSize) => {
		// Set Default Shape Attributes
		X6.Shape.Edge.defaults.attrs.line.stroke = "#fff";
		X6.Shape.Edge.defaults.attrs.line.targetMarker = "circle";

		// Create the graph instance
		const graph = new X6.Graph({
			container: container,
			width: "100%",
			height: "100%",
			// Grid settings
			grid: {
				visible: true,
				type: "fixedDot",
				size: 30,
				args: {
					color: "#2a2a2a",
					thickness: 3,
				},
			},
			// Background settings
			background: {
				color: "#0f0f0f",
			},
			// Interaction settings
			mousewheel: {
				enabled: true,
				modifiers: [],
				factor: 1.1,
				maxScale: 2,
				minScale: 0.5,
			},
			panning: {
				enabled: true,
				modifiers: [],
			},
			connecting: {
				anchor: "center",
				connector: {
					name: "rounded",
					args: {},
				},
				connectionPoint: "boundary",
				allowBlank: false,
				allowLoop: false,
				allowNode: false,
				allowEdge: false,
				allowMulti: false,
				highlight: true,
				router: {
					name: "manhattan",
					args: {
						padding: 10,
					},
				},
				validateMagnet({ magnet, cell, view }) {
					return true;
				},
				validateConnection({ sourceView, targetView, sourceMagnet, targetMagnet }) {
					if (!sourceView || !targetView) {
						return false;
					}

					if (!sourceMagnet || !targetMagnet) {
						return false;
					}

					const sourcePort = sourceMagnet.attributes["port-group"].value;
					const targetPort = targetMagnet.attributes["port-group"].value;

					if ((sourcePort === "output" && targetPort === "output") || (sourcePort === "input" && targetPort === "input")) {
						return false;
					}

					let inputCell;
					let outputCell;

					let inputPort;
					let outputPort;

					if (sourcePort === "input") {
						inputCell = sourceView.cell;
						outputCell = targetView.cell;

						inputPort = sourceMagnet.attributes.port.value;
						outputPort = targetMagnet.attributes.port.value;
					} else {
						inputCell = targetView.cell;
						outputCell = sourceView.cell;

						inputPort = targetMagnet.attributes.port.value;
						outputPort = sourceMagnet.attributes.port.value;
					}

					// start node can not connect to ai response node
					if (outputCell.shape === AGENT_SCRIPT_NODE_TYPES.START && inputCell.shape === AGENT_SCRIPT_NODE_TYPES.AI_RESPONSE) {
						return false;
					}

					// ai response node can only connect to user query node
					if (outputCell.shape === AGENT_SCRIPT_NODE_TYPES.AI_RESPONSE && inputCell.shape !== AGENT_SCRIPT_NODE_TYPES.USER_QUERY) {
						return false;
					}

					// validate if source already has connected nodes
					let validateNoDiffOuputTypes = false;
					CurrentAgentScriptGraph.getEdges().forEach((edge) => {
						const letEdgeSource = edge.getSource();

						if (letEdgeSource.cell === outputCell.id && letEdgeSource.port === outputPort) {
							const letEdgeTarget = edge.getTarget();

							if (letEdgeTarget.cell) {
								letEdgeTargetCell = CurrentAgentScriptGraph.getCellById(letEdgeTarget.cell);

								// if atleast one user query is connected, then no other node type can be connected
								if (letEdgeTargetCell.shape === AGENT_SCRIPT_NODE_TYPES.USER_QUERY && inputCell.shape !== AGENT_SCRIPT_NODE_TYPES.USER_QUERY) {
									validateNoDiffOuputTypes = true;
								}

								// if one custom tool/system tool/ai response is connected, then no other type and no more nodes can be connected
								if (
									letEdgeTargetCell.shape === AGENT_SCRIPT_NODE_TYPES.CUSTOM_TOOL ||
									letEdgeTargetCell.shape === AGENT_SCRIPT_NODE_TYPES.SYSTEM_TOOL ||
									letEdgeTargetCell.shape === AGENT_SCRIPT_NODE_TYPES.AI_RESPONSE
								) {
									validateNoDiffOuputTypes = true;
								}
							}
						}
					});
					if (validateNoDiffOuputTypes) return false;

					return true;
				},
			},
			interacting: true,
			// Prevent node text selection
			preventDefaultContextMenu: true,
			preventDefaultBlankAction: true,
		});

		// Add minimap plugin
		const minimapContainer = document.getElementById("agent-script-graph-minimap");
		const enableMinimap = false;
		if (minimapContainer && enableMinimap) {
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

		// Add selection plugin
		if (AGENT_SCRIPT_GRAPH_PLUGINS.Selection) {
			graph.use(
				new AGENT_SCRIPT_GRAPH_PLUGINS.Selection({
					enabled: true,
					modifiers: ["ctrl"],
					rubberband: true,
					multiple: true,
					movable: true,
					showNodeSelectionBox: true,
					eventTypes: "leftMouseDown",
				}),
			);
		}

		// Add start node
		CurrentAgentScriptGraphStartNode = graph.addNode({
			shape: AGENT_SCRIPT_NODE_TYPES.START,
			data: { type: AGENT_SCRIPT_NODE_TYPES.START },
			x: graphSize.width / 2,
			y: graphSize.height / 5,
			ports: {
				items: [{ group: "output" }],
			},
		});

		// Event Listeners
		graph.on("scale", ({ sx, sy }) => {
			const scale = sx;
			$("#agent-script-graph-zoom-in").prop("disabled", scale >= 2);
			$("#agent-script-graph-zoom-out").prop("disabled", scale <= 0.5);
		});

		graph.on("history:change", () => {
			$("#agent-script-graph-undo").prop("disabled", !graph.canUndo());
			$("#agent-script-graph-redo").prop("disabled", !graph.canRedo());
		});

		graph.on("cell:click", ({ cell, e }) => {
			const target = e.target;
			if (target.closest('[data-action="delete-node"]')) {
				const nodeType = cell.getData()?.type;
				if (nodeType === AGENT_SCRIPT_NODE_TYPES.START) {
					return;
				}

				if (CurrentCanvasConfigCell !== null && cell.id === CurrentCanvasConfigCell.id) {
					nodeConfigOffcanvas.hide();
				}

				cell.remove();
			}
		});

		graph.on("edge:connected", (event) => {
			// Remove the circle from line
			event.edge.setAttrs({ line: { targetMarker: "" } });

			console.log(event);
		});

		graph.on("edge:mouseenter", (event) => {
			event.edge.setAttrs({ line: { strokeDasharray: 5, style: "animation: ant-line 30s infinite linear" } });
		});

		graph.on("edge:mouseleave", (event) => {
			event.edge.setAttrs({ line: { strokeDasharray: 0, style: {} } });
		});

		graph.on("edge:removed", (event) => {
			return;
			if (event.options.ui) {
				const parentInputCell = event.cell.store.data.source.cell;

				const allConnectionForInputCell = CurrentAgentScriptGraph.getCellById(parentInputCell)._model.outgoings[parentInputCell];

				if (!allConnectionForInputCell) return;

				while (allConnectionForInputCell.length > 0) {
					CurrentAgentScriptGraph.removeCell(allConnectionForInputCell[0]);
				}
			}
		});

		graph.on("cell:click", (event) => {
			graph.cleanSelection();
		});

		graph.on("blank:click", () => {
			nodeConfigOffcanvas.hide();
		});

		CurrentAgentScriptGraph = graph;
	});
}

function updateAgentScriptGraphNodeSize(cell, div) {
	const contentHeight = div.offsetHeight;
	if (contentHeight !== cell.getSize().height) {
		cell.resize(AGENT_SCRIPT_NODE_WIDTH, contentHeight);
	}
}

function resizeAgentScriptGraphCSS(callback, isFullscreen = false) {
	let currentInnerContentHeight;
	let currentInnerContentWidth;

	if (isFullscreen) {
		currentInnerContentHeight = window.innerHeight;
		currentInnerContentWidth = window.innerWidth;
	} else {
		currentInnerContentHeight = agentTab.find(".inner-container")[0].clientHeight;
		currentInnerContentWidth = agentTab.find(".inner-container")[0].clientWidth;
	}

	$("#agent-script-graph").css("height", `${currentInnerContentHeight}px`);

	callback({ width: currentInnerContentWidth, height: currentInnerContentHeight });
}

function adjustAgentScriptGraphMultilanguageDropdownForFullscreen(isFullscreen) {
	const container = $(".agent-script-graph-controls");
	let languageDropdown;

	if (isFullscreen) {
		languageDropdown = $("#agentsScriptManagerMultiLanguageContainer .multilanguage-dropdown");
		languageDropdown.prependTo(container);
	} else {
		languageDropdown = $(".agent-script-graph-controls .multilanguage-dropdown");
		languageDropdown.appendTo("#agentsScriptManagerMultiLanguageContainer");
	}
}

// Script User Query Node
function addUserQueryNode(graph, x = 100, y = 200) {
	return graph.addNode({
		shape: AGENT_SCRIPT_NODE_TYPES.USER_QUERY,
		data: {
			type: AGENT_SCRIPT_NODE_TYPES.USER_QUERY,
			query: {},
			examples: {},
		},
		x,
		y,
		ports: {
			items: [{ group: "input" }, { group: "output" }],
		},
	});
}

function generateUserQueryConfig(cell) {
	const data = cell.getData() || {};
	const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

	return `
        <div class="node-config-section">
            <label class="form-label">Query Examples <i class="fa-regular fa-language"></i></label>
            <div id="userQueryExamplesContainer">
                ${(data.examples?.[currentLanguage] || [])
									.map(
										(example) => `
                        <div class="input-group mb-2">
                            <input type="text" class="form-control" 
                                   value="${example}"
                                   data-input="query-example">
                            <button class="btn btn-danger" data-action="remove-example">
                                <i class="fa-regular fa-trash"></i>
                            </button>
                        </div>
                    `,
									)
									.join("")}
            </div>
            <button class="btn btn-light btn-sm mt-2" data-action="addUserQueryExampleButton">
                <i class="fa-regular fa-plus me-2"></i>
                Add Example
            </button>
        </div>
    `;
}

// Script AI Response Node
function addAIResponseNode(graph, x = 100, y = 200) {
	return graph.addNode({
		shape: AGENT_SCRIPT_NODE_TYPES.AI_RESPONSE,
		data: {
			type: AGENT_SCRIPT_NODE_TYPES.AI_RESPONSE,
			response: {},
			examples: {},
		},
		x,
		y,
		ports: {
			items: [{ group: "input" }, { group: "output" }],
		},
	});
}

function generateAIResponseConfig(cell) {
	const data = cell.getData() || {};
	const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

	return `
        <div class="node-config-section">
            <label class="form-label">Response Examples <i class="fa-regular fa-language"></i></label>
            <div id="aiResponseExamplesContainer">
                ${(data.examples?.[currentLanguage] || [])
									.map(
										(example) => `
                        <div class="input-group mb-2">
                            <input type="text" class="form-control" 
                                   value="${example}"
                                   data-input="response-example">
                            <button class="btn btn-danger" data-action="remove-example">
                                <i class="fa-regular fa-trash"></i>
                            </button>
                        </div>
                    `,
									)
									.join("")}
            </div>
            <button class="btn btn-light btn-sm mt-2" data-action="addAIResponseExampleButton">
                <i class="fa-regular fa-plus me-2"></i>
                Add Example
            </button>
        </div>
    `;
}

// Script System Tool Node
function addSystemToolNode(graph, x = 100, y = 200) {
	return graph.addNode({
		shape: AGENT_SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		data: {
			type: AGENT_SCRIPT_NODE_TYPES.SYSTEM_TOOL,
			toolType: null,
			config: {},
		},
		x,
		y,
		ports: {
			items: [
				{ group: "input" },
				// Output ports will be added based on tool type
			],
		},
	});
}

function generateSystemToolConfig(cell) {
	const data = cell.getData() || {};
	const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

	return getAgentScriptSystemToolConfig(data.toolType, currentLanguage, {
		systemTool: {
			type: data.toolType,
			config: data.config || {},
		},
	});
}

function getAgentScriptSystemToolConfig(toolType, currentLanguage, data = {}) {
	const config = data.systemTool?.config || {};

	if (toolType === AGENT_SCRIPT_SYSTEM_TOOLS.END_CALL) {
		return `
                <div class="tool-config-group">
                    <label class="form-label">End Call Configuration</label>
                    <div class="mb-2">
                        <select class="form-select" data-input="end-call-type">
                            <option value="immediate" ${config.type === "immediate" ? "selected" : ""}>End Immediately</option>
                            <option value="with_message" ${config.type === "with_message" ? "selected" : ""}>End with Message</option>
                        </select>
                    </div>
                    ${
											config.type === "with_message"
												? `
                        <div class="mb-2">
                            <textarea 
                                class="form-control" 
                                data-input="end-call-message"
                                placeholder="Enter end call message..."
                                rows="2"
                            >${config.messages?.[currentLanguage] || ""}</textarea>
                        </div>
                    `
												: ""
										}
                </div>
            `;
	}

	if (toolType === AGENT_SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT) {
		return `
                <div class="tool-config-group">
                    <label class="form-label">DTMF Input Configuration</label>
                    <div class="mb-2">
                        <label class="form-label small">Timeout (milliseconds)</label>
                        <input 
                            type="number" 
                            class="form-control" 
                            data-input="dtmf-timeout"
                            value="${config.timeout || 5000}"
                            min="1000"
                            max="30000"
                            step="1000"
                        >
                    </div>
                    <div class="mb-2">
                        <div class="form-check mb-2">
                            <input 
                                class="form-check-input" 
                                type="checkbox" 
                                data-input="dtmf-require-start"
                                ${config.requireStartAsterisk ? "checked" : ""}
                            >
                            <label class="form-check-label">
                                Require * at start
                            </label>
                        </div>
                        <div class="form-check mb-2">
                            <input 
                                class="form-check-input" 
                                type="checkbox" 
                                data-input="dtmf-require-end"
                                ${config.requireEndHash ? "checked" : ""}
                            >
                            <label class="form-check-label">
                                Require # at end
                            </label>
                        </div>
                    </div>
                    <div class="mb-2">
                        <label class="form-label small">Max Length</label>
                        <input 
                            type="number" 
                            class="form-control" 
                            data-input="dtmf-max-length"
                            value="${config.maxLength || 1}"
                            min="1"
                            max="20"
                        >
                    </div>
                    <div class="mb-2">
                        <div class="form-check mb-2">
                            <input 
                                class="form-check-input" 
                                type="checkbox" 
                                data-input="dtmf-encrypt"
                                ${config.encryptInput ? "checked" : ""}
                            >
                            <label class="form-check-label">
                                Encrypt Input
                            </label>
                        </div>
                    </div>
                    ${
											config.encryptInput
												? `
                        <div class="mb-2">
                            <label class="form-label small">Variable Name</label>
                            <input 
                                type="text" 
                                class="form-control" 
                                data-input="dtmf-variable"
                                value="${config.variableName || ""}"
                                placeholder="Enter variable name"
                            >
                        </div>
                    `
												: ""
										}
                    <div class="mb-2">
                        <label class="form-label small">DTMF Outcomes</label>
                        <div data-container="dtmf-outcomes">
                            ${(config.outcomes || [])
															.map(
																(outcome, index) => `
                                <div class="input-group mb-2" data-outcome-index="${outcome.outcomeIndex}">
                                    <input 
                                        type="text" 
                                        class="form-control" 
                                        placeholder="DTMF Value"
                                        value="${outcome.value}"
                                        data-input="outcome-value"
                                    >
                                    <button class="btn btn-danger" data-action="remove-outcome">
                                        <i class="fa-regular fa-trash"></i>
                                    </button>
                                </div>
                            `,
															)
															.join("")}
                        </div>
                        <button class="btn btn-light btn-sm" data-action="add-outcome">
                            Add Outcome
                        </button>
                    </div>
                </div>
            `;
	}

	if (toolType === AGENT_SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_AGENT) {
		const agents = BusinessFullData.businessApp.agents || [];
		const agentOptions = agents
			.map((agent) => {
				const agentName = agent.general.name[currentLanguage] || agent.general.name["en-us"] || "Unnamed Agent";
				return `<option value="${agent.id}" ${config.agentId === agent.id ? "selected" : ""}>${agentName}</option>`;
			})
			.join("");

		return `
                <div class="tool-config-group">
                    <label class="form-label">Transfer Agent Configuration</label>
                    <div class="mb-2">
                        <select class="form-select" data-input="transfer-agent">
                            <option value="">Select Agent</option>
                            ${agentOptions}
                        </select>
                    </div>
                    <div class="form-check mb-2">
                        <input 
                            class="form-check-input" 
                            type="checkbox" 
                            data-input="transfer-context"
                            ${config.transferContext ? "checked" : ""}
                        >
                        <label class="form-check-label">
                            Transfer Context
                        </label>
                    </div>
                    ${
											config.transferContext
												? `
                        <div class="form-check mb-2">
                            <input 
                                class="form-check-input" 
                                type="checkbox" 
                                data-input="summarize-context"
                                ${config.summarizeContext ? "checked" : ""}
                            >
                            <label class="form-check-label">
                                Summarize Context
                            </label>
                        </div>
                    `
												: ""
										}
                </div>
            `;
	}

	if (toolType === AGENT_SCRIPT_SYSTEM_TOOLS.ADD_SCRIPT_TO_CONTEXT) {
		const scripts = CurrentManageAgentData.scripts || [];

		const scriptOptions = scripts
			.map((script) => {
				const scriptName = script.general.name[currentLanguage] || script.general.name["en-us"] || "Unnamed Script";
				return `<option value="${script.id}" ${config.scriptId === script.id ? "selected" : ""}>${scriptName}</option>`;
			})
			.join("");

		return `
				<div class="tool-config-group">
					<label class="form-label">Add Script Configuration</label>
					<div class="mb-2">
						<select class="form-select" data-input="add-script">
							<option value="">Select Script</option>
							${scriptOptions}
						</select>
					</div>
				</div>
			`;
	}

	return "";
}

function UpdateSystemToolNodePorts(cell, toolType) {
	// Remove all existing output ports
	const currentPorts = cell.getPorts();
	const outputPorts = currentPorts.filter((port) => port.group === "output");
	outputPorts.forEach((port) => cell.removePort(port.id));

	// Add appropriate ports based on tool type
	switch (toolType) {
		case "end_call":
		case "transfer_to_agent":
		case "transfer_to_human":
			// These are end nodes, no output ports needed
			break;

		case "get_dtmf_keypad_input": {
			// Add port for timeout
			cell.addPort({
				group: "output",
				id: "timeout",
				attrs: {
					circle: {
						fill: "#ffc107",
					},
					text: {
						text: "Timeout",
					},
					label: {
						position: "bottom",
					},
				},
			});
			break;
		}

		default:
			// All other tools have a single output
			cell.addPort({
				group: "output",
			});
			break;
	}

	cell.updatePortData();
}

function updateSystemToolConfig(newConfig) {
	if (CurrentCanvasConfigCell) {
		const data = CurrentCanvasConfigCell.getData();
		CurrentCanvasConfigCell.setData({
			...data,
			config: newConfig,
		});
	}
}

// Script Custom Tool Node
function addCustomToolNode(graph, x = 100, y = 200) {
	return graph.addNode({
		shape: AGENT_SCRIPT_NODE_TYPES.CUSTOM_TOOL,
		data: {
			type: AGENT_SCRIPT_NODE_TYPES.CUSTOM_TOOL,
			toolId: null,
			config: {},
		},
		x,
		y,
		ports: {
			items: [
				{ group: "input" },
				// Output ports will be added based on tool configuration
			],
		},
	});
}

function generateCustomToolConfig(cell) {
	const data = cell.getData() || {};
	const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

	const selectedTool = BusinessFullData.businessApp.tools.find((t) => t.id === data.toolId);
	if (!selectedTool) return '<div class="alert alert-warning">Please select a tool first</div>';

	return `
        <div class="node-config-section">
            <h6>Tool Parameters</h6>
            ${(selectedTool.parameters || [])
							.map(
								(param) => `
                    <div class="mb-2">
                        <label class="form-label">${param.name}</label>
                        <input type="text" 
                               class="form-control" 
                               data-param="${param.id}"
                               value="${data.config?.[param.id] || ""}"
                               placeholder="${param.description || ""}">
                    </div>
                `,
							)
							.join("")}
        </div>
    `;
}

function getCustomToolConfigValues() {
	// Collect all parameter values
	const config = {};
	$("#customToolConfigContainer [data-param]").each((i, el) => {
		config[$(el).data("param")] = $(el).val();
	});
	return config;
}

function updateCustomToolConfig(config) {
	if (CurrentCanvasConfigCell) {
		const data = CurrentCanvasConfigCell.getData();
		CurrentCanvasConfigCell.setData({
			...data,
			config: config,
		});
	}
}

function UpdateCustomToolNodePorts(cell, toolId) {
	// Remove all existing output ports
	const currentPorts = cell.getPorts();
	const outputPorts = currentPorts.filter((port) => port.group === "output");
	outputPorts.forEach((port) => cell.removePort(port.id));

	// Get the tool definition
	const tool = BusinessFullData.businessApp.tools.find((t) => t.id === toolId);
	if (!tool) return;

	cell.addPort({
		group: "output",
		id: "outcome-default",
		attrs: {
			circle: {
				fill: "#ffc107",
			},
			text: {
				text: "Default",
			},
		},
	});

	Object.keys(tool.response).forEach((responseCode) => {
		cell.addPort({
			group: "output",
			id: `outcome-${responseCode}`,
			attrs: {
				circle: {
					fill: "#fff",
				},
				text: {
					text: `Response ${responseCode}`,
				},
			},
		});
	});
}

//Helpers
function toPascalCase(str) {
	return str
		.match(/[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+/g)
		.map((x) => x.charAt(0).toUpperCase() + x.slice(1).toLowerCase())
		.join(" ");
}

/** INIT **/
function initAgentTab() {
	$(document).ready(() => {
		// Init
		registerAgentScriptNodes();
		nodeConfigOffcanvas = new bootstrap.Offcanvas("#nodeConfigOffcanvas");

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

		// Script Tab Handler
		function initAgentScriptsTabHandlers() {
			addNewAgentScriptButton.on("click", (event) => {
				event.preventDefault();

				ResetAndEmptyAgentsScriptManageTab();
				initializeAgentScriptGraph(document.getElementById("agent-script-graph"));

				currentAgentScriptName.text("New Script");
				showAgentScriptManagerTab();
			});

			switchBackToAgentsScriptManagerTab.on("click", async (event) => {
				event.preventDefault();

				const canLeave = await canLeaveAgentScriptManagerTab();
				if (!canLeave) return;

				showAgentScriptListTab();
			});

			// Nodes
			// click handlers for configuration buttons
			$("#agent-script-graph").on("click", "[data-action^='configure-']", (e) => {
				const closestNode = $(e.target).closest(".x6-node");
				const cellId = closestNode.attr("data-cell-id");
				const cell = CurrentAgentScriptGraph.getCellById(cellId);

				CurrentCanvasConfigCell = cell;

				let configType = toPascalCase($(e.target).closest("[data-action]").attr("data-action").replace("configure-", "").replace("-", " "));

				if (configType === "Ai Response") {
					configType = "AI Response";
				}

				const configContent = $("#nodeConfigContent");
				$("#nodeConfigTitle").text(`${configType} Configuration`);

				switch (configType) {
					case "User Query":
						configContent.html(generateUserQueryConfig(cell));
						break;
					case "AI Response":
						configContent.html(generateAIResponseConfig(cell));
						break;
					case "System Tool":
						configContent.html(generateSystemToolConfig(cell));
						break;
					case "Custom Tool":
						configContent.html(generateCustomToolConfig(cell));
						break;
				}

				nodeConfigOffcanvas.show();
			});

			$("#nodeConfigOffcanvas").on("hidden.bs.offcanvas", () => {
				CurrentCanvasConfigCell = null;
			});

			// User Query Node
			function initializeUserQueryHandlers() {
				// Query text change handler
				$("#agent-script-graph").on("input", '[data-input="user-query"]', (e) => {
					const currentElement = $(e.currentTarget);
					const closestNode = currentElement.closest(".x6-node");
					const cellId = closestNode.attr("data-cell-id");

					const cell = CurrentAgentScriptGraph.getCellById(cellId);
					const data = cell.getData() || {};
					const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

					const queries = data.query || {};
					queries[currentLanguage] = currentElement.val();

					cell.setData({
						...data,
						query: queries,
					});
				});

				agentsScriptManagerLanguageDropdown.onLanguageChange((language) => {
					const currentLanguage = language.id;

					CurrentAgentScriptGraph.getCells().forEach((cell) => {
						const nodeData = cell.getData() || {};

						if (nodeData.type !== AGENT_SCRIPT_NODE_TYPES.USER_QUERY) {
							return;
						}

						const currentLangQueryData = nodeData.query[currentLanguage] || "";
						$(`[data-cell-id="${cell.id}"] .${AGENT_SCRIPT_NODE_TYPES.USER_QUERY} [data-input="user-query"]`).val(currentLangQueryData);
					});

					if (CurrentCanvasConfigCell && nodeConfigOffcanvas._element.classList.contains("show")) {
						const nodeData = CurrentCanvasConfigCell.getData() || {};

						if (nodeData.type !== AGENT_SCRIPT_NODE_TYPES.USER_QUERY) {
							return;
						}

						$(`[data-cell-id="${CurrentCanvasConfigCell.id}"] .${AGENT_SCRIPT_NODE_TYPES.USER_QUERY} [data-action="configure-user-query"]`).click();
					}
				});
			}
			initializeUserQueryHandlers();
			function initUserQueryConfigHandlers() {
				// Add Example Button Handler
				$("#nodeConfigOffcanvas").on("click", '[data-action="addUserQueryExampleButton"]', (e) => {
					e.stopPropagation();

					$("#userQueryExamplesContainer").append(`
						<div class="input-group mb-2">
							<input type="text" class="form-control" 
								placeholder="Enter example query"
								data-input="query-example">
							<button class="btn btn-danger" data-action="remove-example">
								<i class="fa-regular fa-trash"></i>
							</button>
						</div>
					`);
				});

				// Remove Example Button Handler
				$("#nodeConfigOffcanvas").on("click", '#userQueryExamplesContainer [data-action="remove-example"]', (e) => {
					e.stopPropagation();

					$(e.currentTarget).closest(".input-group").remove();

					const queryExamples = Array.from($("#userQueryExamplesContainer input"))
						.map((input) => $(input).val().trim())
						.filter((value) => value !== ""); // Remove empty values

					const examples = data.examples || {};
					examples[currentLanguage] = queryExamples;

					CurrentCanvasConfigCell.setData({
						...data,
						examples,
					});
				});

				// On Type Query Example
				$("#nodeConfigOffcanvas").on("input", '#userQueryExamplesContainer [data-input="query-example"]', (e) => {
					e.stopPropagation();

					const data = CurrentCanvasConfigCell.getData();
					const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

					const queryExamples = Array.from($("#userQueryExamplesContainer input"))
						.map((input) => $(input).val().trim())
						.filter((value) => value !== ""); // Remove empty values

					const examples = data.examples || {};
					examples[currentLanguage] = queryExamples;

					CurrentCanvasConfigCell.setData({
						...data,
						examples,
					});
				});
			}
			initUserQueryConfigHandlers();

			// AI Response Node
			function initializeAIResponseHandlers() {
				// Response text change handler
				$("#agent-script-graph").on("input", '[data-input="ai-response"]', (e) => {
					const currentElement = $(e.currentTarget);
					const closestNode = currentElement.closest(".x6-node");
					const cellId = closestNode.attr("data-cell-id");

					const cell = CurrentAgentScriptGraph.getCellById(cellId);
					const data = cell.getData() || {};
					const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

					const responses = data.response || {};
					responses[currentLanguage] = currentElement.val();

					cell.setData({
						...data,
						response: responses,
					});
				});

				agentsScriptManagerLanguageDropdown.onLanguageChange((language) => {
					const currentLanguage = language.id;

					CurrentAgentScriptGraph.getCells().forEach((cell) => {
						const nodeData = cell.getData() || {};

						if (nodeData.type !== AGENT_SCRIPT_NODE_TYPES.AI_RESPONSE) {
							return;
						}

						const currentLangResponseData = nodeData.response[currentLanguage] || "";
						$(`[data-cell-id="${cell.id}"] .${AGENT_SCRIPT_NODE_TYPES.AI_RESPONSE} [data-input="ai-response"]`).val(currentLangResponseData);
					});

					if (CurrentCanvasConfigCell && nodeConfigOffcanvas._element.classList.contains("show")) {
						const nodeData = CurrentCanvasConfigCell.getData() || {};

						if (nodeData.type !== AGENT_SCRIPT_NODE_TYPES.AI_RESPONSE) {
							return;
						}

						$(`[data-cell-id="${CurrentCanvasConfigCell.id}"] .${AGENT_SCRIPT_NODE_TYPES.AI_RESPONSE} [data-action="configure-ai-response"]`).click();
					}
				});
			}
			initializeAIResponseHandlers();
			function initAIResponseConfigHandlers() {
				// Add Example Button Handler
				$("#nodeConfigOffcanvas").on("click", '[data-action="addAIResponseExampleButton"]', (e) => {
					e.stopPropagation();

					$("#aiResponseExamplesContainer").append(`
						<div class="input-group mb-2">
							<input type="text" class="form-control" 
								placeholder="Enter example response"
								data-input="response-example">
							<button class="btn btn-danger" data-action="remove-example">
								<i class="fa-regular fa-trash"></i>
							</button>
						</div>
					`);
				});

				// Remove Example Button Handler
				$("#nodeConfigOffcanvas").on("click", '#aiResponseExamplesContainer [data-action="remove-example"]', (e) => {
					e.stopPropagation();

					$(e.currentTarget).closest(".input-group").remove();

					const responseExamples = Array.from($("#aiResponseExamplesContainer input"))
						.map((input) => $(input).val().trim())
						.filter((value) => value !== ""); // Remove empty values

					const examples = data.examples || {};
					examples[currentLanguage] = responseExamples;

					CurrentCanvasConfigCell.setData({
						...data,
						examples,
					});
				});

				// On Type Response Example
				$("#nodeConfigOffcanvas").on("input", '#aiResponseExamplesContainer [data-input="response-example"]', (e) => {
					e.stopPropagation();

					const data = CurrentCanvasConfigCell.getData();
					const currentLanguage = agentsScriptManagerLanguageDropdown.getSelectedLanguage().id;

					const responseExamples = Array.from($("#aiResponseExamplesContainer input"))
						.map((input) => $(input).val().trim())
						.filter((value) => value !== ""); // Remove empty values

					const examples = data.examples || {};
					examples[currentLanguage] = responseExamples;

					CurrentCanvasConfigCell.setData({
						...data,
						examples,
					});
				});
			}
			initAIResponseConfigHandlers();

			// System Tool Node
			function initSystemToolNodeHandlers() {
				// Tool type change handler
				$("#agent-script-graph").on("change", '[data-input="system-tool-type"]', (e) => {
					const currentElement = $(e.currentTarget);
					const closestNode = currentElement.closest(".x6-node");
					const cellId = closestNode.attr("data-cell-id");

					const cell = CurrentAgentScriptGraph.getCellById(cellId);
					const data = cell.getData() || {};

					const toolType = currentElement.val();

					// Update cell data
					const newData = {
						...data,
						toolType: toolType,
						config: {},
					};

					// Update cell data
					cell.setData(newData);

					const requiresConfig =
						newData.toolType &&
						(newData.toolType === "end_call" || newData.toolType === "get_dtmf_keypad_input" || newData.toolType === "transfer_to_agent" || newData.toolType === "add_script_to_context");

					closestNode.find('[data-action="configure-system-tool"]').prop("disabled", !requiresConfig);

					// Update ports based on tool type
					UpdateSystemToolNodePorts(cell, toolType);

					if (CurrentCanvasConfigCell !== null && cell.id === CurrentCanvasConfigCell.id) {
						nodeConfigOffcanvas.hide();
					}
				});
			}
			initSystemToolNodeHandlers();
			function initSystemToolConfigHandlers() {
				$("#nodeConfigOffcanvas").on("change", '[data-input="end-call-type"]', (e) => {
					const data = CurrentCanvasConfigCell.getData();
					const config = data.systemTool?.config || {};
					const currentLanguage = manageAgentsLanguageDropdown.getSelectedLanguage().id;

					const newConfig = {
						...config,
						type: e.target.value,
					};

					// Show/hide message textarea based on type
					const messageContainer = `
					<div class="mb-2">
						<textarea 
							class="form-control" 
							data-input="end-call-message"
							placeholder="Enter end call message..."
							rows="2"
						>${config.messages?.[currentLanguage] || ""}</textarea>
					</div>
				`;

					if (e.target.value === "with_message") {
						$(e.target).closest(".tool-config-group").append(messageContainer);
					} else {
						$(e.target).closest(".tool-config-group").find('[data-input="end-call-message"]').parent().remove();
					}

					updateSystemToolConfig(newConfig);
				});

				$("#nodeConfigOffcanvas").on("input", '[data-input="end-call-message"]', (e) => {
					const data = CurrentCanvasConfigCell.getData();
					const config = data.systemTool?.config || {};
					const currentLanguage = manageAgentsLanguageDropdown.getSelectedLanguage().id;

					const messages = config.messages || {};
					messages[currentLanguage] = e.target.value;

					updateSystemToolConfig({ ...config, messages });
				});

				$("#nodeConfigOffcanvas").on("input", '[data-input="dtmf-timeout"]', (e) => {
					const value = parseInt(e.target.value);
					if (value >= 1000 && value <= 30000) {
						const data = CurrentCanvasConfigCell.getData();
						const config = data.systemTool?.config || {};
						updateSystemToolConfig({ ...config, timeout: value });
					}
				});

				$("#nodeConfigOffcanvas").on("change", '[data-input="dtmf-require-start"]', (e) => {
					const data = CurrentCanvasConfigCell.getData();
					const config = data.systemTool?.config || {};
					updateSystemToolConfig({ ...config, requireStartAsterisk: e.target.checked });
				});

				$("#nodeConfigOffcanvas").on("change", '[data-input="dtmf-require-end"]', (e) => {
					const data = CurrentCanvasConfigCell.getData();
					const config = data.systemTool?.config || {};
					updateSystemToolConfig({ ...config, requireEndHash: e.target.checked });
				});

				$("#nodeConfigOffcanvas").on("input", '[data-input="dtmf-max-length"]', (e) => {
					const value = parseInt(e.target.value);
					if (value >= 1 && value <= 20) {
						const data = CurrentCanvasConfigCell.getData();
						const config = data.systemTool?.config || {};
						updateSystemToolConfig({ ...config, maxLength: value });
					}
				});

				$("#nodeConfigOffcanvas").on("change", '[data-input="dtmf-encrypt"]', (e) => {
					const data = CurrentCanvasConfigCell.getData();
					const config = data.systemTool?.config || {};

					const newConfig = {
						...config,
						encryptInput: e.target.checked,
					};

					// Show/hide variable name input based on encrypt checkbox
					const variableNameInput = `
					<div class="mb-2">
						<label class="form-label small">Variable Name</label>
						<input 
							type="text" 
							class="form-control" 
							data-input="dtmf-variable"
							value="${config.variableName || ""}"
							placeholder="Enter variable name"
						>
					</div>
				`;

					if (e.target.checked) {
						$(e.target).closest(".tool-config-group").append(variableNameInput);
					} else {
						$(e.target).closest(".tool-config-group").find('[data-input="dtmf-variable"]').parent().remove();
					}

					updateSystemToolConfig(newConfig);
				});

				$("#nodeConfigOffcanvas").on("input", '[data-input="dtmf-variable"]', (e) => {
					const data = CurrentCanvasConfigCell.getData();
					const config = data.systemTool?.config || {};
					updateSystemToolConfig({ ...config, variableName: e.target.value });
				});

				$("#nodeConfigOffcanvas").on("click", '[data-action="add-outcome"]', () => {
					let anyInputValidationFail = false;
					$('[data-container="dtmf-outcomes"]')
						.find('input[data-input="outcome-value"]')
						.each((index, input) => {
							const val = input.value.trim();

							if (val === "") {
								input.focus();

								anyInputValidationFail = true;
								return;
							}
						});

					if (anyInputValidationFail) {
						return;
					}

					agentScriptDMTFNextOutcomeIndex = agentScriptDMTFNextOutcomeIndex + 1;

					const outcomeTemplate = `
						<div class="input-group mb-2" data-outcome-index="${agentScriptDMTFNextOutcomeIndex}">
							<input 
								type="text" 
								class="form-control" 
								placeholder="DTMF Value"
								value=""
								data-input="outcome-value"
							>
							<button class="btn btn-danger" data-action="remove-outcome">
								<i class="fa-regular fa-trash"></i>
							</button>
						</div>
					`;

					$('[data-container="dtmf-outcomes"]').append(outcomeTemplate);

					const data = CurrentCanvasConfigCell.getData();
					const config = data.config || {};
					const outcomes = [...(config.outcomes || []), { value: "", outcomeIndex: agentScriptDMTFNextOutcomeIndex }];

					updateSystemToolConfig({ ...config, outcomes });

					CurrentCanvasConfigCell.addPort({
						group: "output",
						id: `outcome-${agentScriptDMTFNextOutcomeIndex}`,
						attrs: {
							text: {
								text: "",
							},
						},
						label: {
							position: "bottom",
						},
					});
				});

				$("#nodeConfigOffcanvas").on("input", '[data-input="outcome-value"]', (e) => {
					const val = e.target.value.trim();

					const outcomeIndex = parseInt($(e.target).closest("[data-outcome-index]").data("outcome-index"));
					const data = CurrentCanvasConfigCell.getData();
					const config = data.config || {};
					const outcomes = [...(config.outcomes || [])];

					outcomes.find((d) => d.outcomeIndex === outcomeIndex).value = val;

					CurrentCanvasConfigCell.portProp(`outcome-${outcomeIndex}`, "attrs/text/text", val);

					updateSystemToolConfig({ ...config, outcomes });
				});

				$("#nodeConfigOffcanvas").on("click", '[data-action="remove-outcome"]', (e) => {
					const outcomeIndex = $(e.target).closest("[data-outcome-index]").data("outcome-index");
					const data = CurrentCanvasConfigCell.getData();
					const config = data.config || {};
					const outcomes = [...(config.outcomes || [])];

					const outcomeDataIndex = outcomes.findIndex((d) => d.outcomeIndex === outcomeIndex);

					outcomes.splice(outcomeDataIndex, 1);
					$(e.target).closest("[data-outcome-index]").remove();

					CurrentCanvasConfigCell.removePort(`outcome-${outcomeIndex}`);

					updateSystemToolConfig({ ...config, outcomes });
				});

				$("#nodeConfigOffcanvas").on("change", '[data-input="transfer-agent"]', (e) => {
					const data = CurrentCanvasConfigCell.getData();
					const config = data.systemTool?.config || {};
					updateSystemToolConfig({ ...config, agentId: e.target.value });
				});

				$("#nodeConfigOffcanvas").on("change", '[data-input="transfer-context"]', (e) => {
					const data = CurrentCanvasConfigCell.getData();
					const config = data.systemTool?.config || {};

					const newConfig = {
						...config,
						transferContext: e.target.checked,
					};

					// Show/hide summarize context option
					const summarizeContextOption = `
					<div class="form-check mb-2">
						<input 
							class="form-check-input" 
							type="checkbox" 
							data-input="summarize-context"
							${config.summarizeContext ? "checked" : ""}
						>
						<label class="form-check-label">
							Summarize Context
						</label>
					</div>
				`;

					if (e.target.checked) {
						$(e.target).closest(".form-check").after(summarizeContextOption);
					} else {
						$(e.target).closest(".tool-config-group").find('[data-input="summarize-context"]').parent().remove();
					}

					updateSystemToolConfig(newConfig);
				});

				$("#nodeConfigOffcanvas").on("change", '[data-input="summarize-context"]', (e) => {
					const data = CurrentCanvasConfigCell.getData();
					const config = data.systemTool?.config || {};
					updateSystemToolConfig({ ...config, summarizeContext: e.target.checked });
				});
			}
			initSystemToolConfigHandlers();

			// Custom Tool Node
			function initializeCustomToolHandlers() {
				$("#agent-script-graph").on("change", '[data-input="custom-tool-select"]', (e) => {
					const currentElement = $(e.currentTarget);
					const closestNode = currentElement.closest(".x6-node");
					const cellId = closestNode.attr("data-cell-id");

					const cell = CurrentAgentScriptGraph.getCellById(cellId);
					const toolId = currentElement.val();

					// Update cell data
					cell.setData({
						...cell.getData(),
						toolId: toolId,
						config: {},
					});

					$('[data-action="configure-custom-tool"]').attr("disabled", toolId === "");

					// Update ports based on tool
					UpdateCustomToolNodePorts(cell, toolId);

					if (CurrentCanvasConfigCell !== null && cell.id === CurrentCanvasConfigCell.id) {
						nodeConfigOffcanvas.hide();
					}
				});
			}
			initializeCustomToolHandlers();

			// Graph Add Node Buttons
			// Add new event handlers
			$(".sidebar-node-button").on("click", function () {
				const nodeType = $(this).data("node-type");
				const currentGraphArea = CurrentAgentScriptGraph.getGraphArea();
				const x = currentGraphArea.x + currentGraphArea.width / 2 - AGENT_SCRIPT_NODE_WIDTH / 2;
				const y = currentGraphArea.y + currentGraphArea.height / 2 - AGENT_SCRIPT_NODE_MIN_HEIGHT / 2;

				switch (nodeType) {
					case "user-query":
						addUserQueryNode(CurrentAgentScriptGraph, x, y);
						break;
					case "ai-response":
						addAIResponseNode(CurrentAgentScriptGraph, x, y);
						break;
					case "system-tool":
						addSystemToolNode(CurrentAgentScriptGraph, x, y);
						break;
					case "custom-tool":
						addCustomToolNode(CurrentAgentScriptGraph, x, y);
						break;
				}
			});

			// Graph Toolbar Bottom
			$("#agent-script-graph-zoom-in").on("click", () => {
				const zoom = CurrentAgentScriptGraph.zoom();
				if (zoom < 2) {
					CurrentAgentScriptGraph.zoom(0.1);
				}
			});

			$("#agent-script-graph-zoom-out").on("click", () => {
				const zoom = CurrentAgentScriptGraph.zoom();
				if (zoom > 0.5) {
					CurrentAgentScriptGraph.zoom(-0.1);
				}
			});

			$("#agent-script-graph-undo").on("click", () => {
				if (CurrentAgentScriptGraph.canUndo()) {
					CurrentAgentScriptGraph.undo();
				}
			});

			$("#agent-script-graph-redo").on("click", () => {
				if (CurrentAgentScriptGraph.canRedo()) {
					CurrentAgentScriptGraph.redo();
				}
			});

			$("#agent-script-graph-fullscreen").on("click", () => {
				const container = $(".agent-script-graph-container");
				container.toggleClass("fullscreen");

				if (container.hasClass("fullscreen")) {
					$("body").css("overflow", "hidden");
					adjustAgentScriptGraphMultilanguageDropdownForFullscreen(true);
					resizeAgentScriptGraphCSS(() => {}, container.hasClass("fullscreen"));
				} else {
					$("body").css("overflow", "");
					adjustAgentScriptGraphMultilanguageDropdownForFullscreen(false);
					setDynamicBodyHeight("agents-tab");
				}
			});

			$(document).on("keydown", (e) => {
				if (e.key === "Escape" && $(".agent-script-graph-container").hasClass("fullscreen")) {
					$("#agent-script-graph-fullscreen").click();

					setDynamicBodyHeight("agents-tab");
				}
			});
		}
		initAgentScriptsTabHandlers();

		// Handle language changes
		manageAgentsLanguageDropdown.onLanguageChange(() => {
			CheckAgentTabHasChanges();
		});

		$(document).on("containerResizeProgress", (e) => {
			if (ManageAgentType == null || CurrentAgentScriptGraph == null) return;

			const $graph = $("#agent-script-graph");

			const isFullscreen = $(".agent-script-graph-container").hasClass("fullscreen");
			if (isFullscreen) {
				$graph.css("height", "100vh");
				return;
			}

			const { containerId, currentHeight, progress, targetHeight } = e.detail;

			if (currentHeight === 0) return;
			if (currentHeight === targetHeight) return;

			const currentGraphHeight = $graph.height();

			if (progress === 1) {
				$graph.css("height", `${targetHeight}px`);
				return;
			}

			if (currentGraphHeight > targetHeight) {
				const heightDifference = currentGraphHeight - targetHeight;
				const newHeight = currentGraphHeight - heightDifference * progress;

				$graph.css("height", `${newHeight}px`);
			} else {
				$graph.css("height", `${currentHeight}px`);
			}
		});
	});
}
