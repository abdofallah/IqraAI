/** Constants **/
const AGENT_INTERRUPTION_TURN_END_TYPE = {
	VAD: 0,
	STT: 1,
	AI: 2,
	ML: 3
};

const AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE = {
	VAD: 0,
	STT: 1
}

const AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE = {
	ALWAYS: 0,
	SPECIFIC_KEYWORD: 1,
	KNOWLEDGEBASE_KEYWORD: 2,
	LLM: 3,
	AGENT_SCRIPT_SYSTEM_TOOL: 3
}

/** Dynamic Variables **/
let CurrentManageAgentData = null;
let ManageAgentType = null; // new or edit

let manageAgentsLanguageDropdown = null;

let AgentBackgroundAudioWaveSurfer = null;

let IsSavingAgentTab = false;
let IsDeletingAgentTab = false;

// Integration Configuration Manager
let agentSTTIntegrationManager = null;
let agentLLMIntegrationManager = null;
let agentTTSIntegrationManager = null;

// Integrations Interruption Configuration Manager
let agentTurnEndLLMIntegrationManager = null;
let agentInterruptionVerifyLLMIntegrationManager = null;

// Integrations Knowledge Base Configuration Manager
let agentKbClassifierLLMIntegrationManager = null;
let agentKbRefinementLLMIntegrationManager = null;

// Cache related states
let CurrentAgentCacheMessages = [];
let CurrentAgentCacheMessagesIndex = 0;
let CurrentAgentCacheAudios = [];
let CurrentAgentCacheAudiosIndex = 0;
let CurrentAgentCacheEmbeddings = [];
let CurrentAgentCacheEmbeddingsIndex = 0;

// Knowledge Base related states
let CurrentAgentLinkedKBs = [];

// Multi Language
let CurrentAgentGeneralNameMultiLangData = {};
let CurrentAgentGeneralDescriptionMultiLangData = {};

let CurrentAgentPersonalityNameMultiLangData = {};
let CurrentAgentPersonalityRoleMultiLangData = {};
let CurrentAgentPersonalityCapabilitiesMultiLangData = {};
let CurrentAgentPersonalityEthicsMultiLangData = {};
let CurrentAgentPersonalityToneMultiLangData = {};

let CurrentAgentUtterancesGreetingMessageMultiLangData = {};

// --- Change Helpers ---
const AgentLanguageTextMap = { kind: "map", keys: () => BusinessFullData.businessData.languages, values: "text" };
const AgentLanguageTextListMap = { kind: "map", keys: () => BusinessFullData.businessData.languages, values: { kind: "list", items: "text" } };
const AgentIntegrationRule = { kind: "object", fields: { id: "text", fieldValues: { kind: "map", values: { kind: "value", split: "," } } } };
const AgentIntegrationListMap = { kind: "map", keys: () => BusinessFullData.businessData.languages, values: { kind: "list", fields: AgentIntegrationRule.fields } };

const AgentIntegrationsChanges = ChangesHelper.define({
	stt: AgentIntegrationListMap,
	llm: AgentIntegrationListMap,
	tts: AgentIntegrationListMap,
});

const AgentGeneralChanges = ChangesHelper.define({
	emoji: "text",
	name: AgentLanguageTextMap,
	description: AgentLanguageTextMap,
});

const AgentContextChanges = ChangesHelper.define({
	useBranding: "boolean",
	useBranches: "boolean",
	useServices: "boolean",
	useProducts: "boolean",
});

const AgentPersonalityChanges = ChangesHelper.define({
	name: AgentLanguageTextMap,
	role: AgentLanguageTextMap,
	capabilities: AgentLanguageTextListMap,
	ethics: AgentLanguageTextListMap,
	tone: AgentLanguageTextListMap,
});

const AgentUtterancesChanges = ChangesHelper.define({
	openingType: "enum",
	greetingMessage: AgentLanguageTextMap,
});

const AgentInterruptionsChanges = ChangesHelper.define({
	turnEnd: {
		kind: "object",
		fields: {
			type: "enum",
			vadSilenceDurationMS: { kind: "number", when: (get) => get("type") === AGENT_INTERRUPTION_TURN_END_TYPE.VAD },
			useAgentLLM: { kind: "boolean", when: (get) => get("type") === AGENT_INTERRUPTION_TURN_END_TYPE.AI },
			llmIntegration: { ...AgentIntegrationRule, when: (get) => get("type") === AGENT_INTERRUPTION_TURN_END_TYPE.AI && get("useAgentLLM") },
		},
	},
	useTurnByTurnMode: "boolean",
	includeInterruptedSpeechInTurnByTurnMode: { kind: "boolean", when: (get) => get("useTurnByTurnMode") },
	pauseTrigger: {
		kind: "object",
		when: (get) => !get("useTurnByTurnMode"),
		fields: {
			type: "enum",
			vadDurationMS: { kind: "number", when: (get) => get("type") === AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE.VAD },
			wordCount: { kind: "number", when: (get) => get("type") === AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE.STT },
		},
	},
	verification: {
		kind: "object",
		when: (get) => !get("useTurnByTurnMode"),
		fields: {
			enabled: "boolean",
			useAgentLLM: { kind: "boolean", when: (get) => get("enabled") },
			llmIntegration: { ...AgentIntegrationRule, when: (get) => get("enabled") && get("useAgentLLM") },
		},
	},
});

const AgentKnowledgeBaseChanges = ChangesHelper.define({
	linkedGroups: { kind: "set", items: "text" },
	searchStrategy: { kind: "object", fields: { type: "enum" } },
	specificKeywords: { kind: "text", when: AgentKnowledgeBaseStrategyIs(AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.SPECIFIC_KEYWORD) },
	llmClassifier: {
		kind: "object",
		when: AgentKnowledgeBaseStrategyIs(AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.LLM),
		fields: {
			useAgentLLM: "boolean",
			llmIntegration: { ...AgentIntegrationRule, when: (get) => get("useAgentLLM") },
		},
	},
	refinement: {
		kind: "object",
		fields: {
			enabled: "boolean",
			queryCount: { kind: "number", when: (get) => get("enabled") },
			useAgentLLM: { kind: "boolean", when: (get) => get("enabled") },
			llmIntegration: { ...AgentIntegrationRule, when: (get) => get("enabled") && get("useAgentLLM") },
		},
	},
});

const AgentCacheChanges = ChangesHelper.define({
	messages: { kind: "list", items: "text" },
	audios: { kind: "list", items: "text" },
	embeddings: { kind: "list", items: "text" },
	audioCacheSettings: {
		kind: "object",
		fields: {
			autoCacheAudioResponses: "boolean",
			autoCacheAudioResponseCacheGroupId: { kind: "text", when: (get) => get("autoCacheAudioResponses") },
			autoCacheAudioResponsesDefaultExpiryHours: { kind: "number", when: (get) => get("autoCacheAudioResponses") },
		},
	},
	embeddingsCacheSettings: {
		kind: "object",
		fields: {
			autoCacheEmbeddingResponses: "boolean",
			autoCacheEmbeddingResponseCacheGroupId: { kind: "text", when: (get) => get("autoCacheEmbeddingResponses") },
			autoCacheEmbeddingResponsesDefaultExpiryHours: { kind: "number", when: (get) => get("autoCacheEmbeddingResponses") },
		},
	},
});

const AgentSettingsChanges = ChangesHelper.define({
	backgroundAudioUrl: { kind: "custom", equals: SameAgentBackgroundAudioCommand },
	backgroundAudioVolume: { kind: "number", when: () => agentBackgroundAudioSelect.val() !== "none" },
});

const AgentTabSectionsChanges = ChangesHelper.defineSections({
	general: { definition: AgentGeneralChanges, saved: () => CurrentManageAgentData.general, current: () => BuildAgentGeneralChanges() },
	context: { definition: AgentContextChanges, saved: () => CurrentManageAgentData.context, current: () => BuildAgentContextChanges() },
	personality: { definition: AgentPersonalityChanges, saved: () => CurrentManageAgentData.personality, current: () => BuildAgentPersonalityChanges() },
	utterances: { definition: AgentUtterancesChanges, saved: () => CurrentManageAgentData.utterances, current: () => BuildAgentUtterancesChanges() },
	interruptions: { definition: AgentInterruptionsChanges, saved: () => CurrentManageAgentData.interruptions, current: () => BuildAgentInterruptionsChanges() },
	integrations: { definition: AgentIntegrationsChanges, saved: () => CurrentManageAgentData.integrations, current: () => BuildAgentIntegrationsChanges() },
	knowledgeBase: { definition: AgentKnowledgeBaseChanges, saved: () => CurrentManageAgentData.knowledgeBase, current: () => BuildAgentKnowledgeBaseChanges() },
	cache: { definition: AgentCacheChanges, saved: () => CurrentManageAgentData.cache, current: () => BuildAgentCacheChanges() },
	settings: { definition: AgentSettingsChanges, saved: () => CurrentManageAgentData.settings, current: () => BuildAgentSettingsChanges() },
});

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
const agentsCardListContainer = agentsListTab.find("#agentsCardListContainer");

// Agent - Manager Tab
const agentsManagerHeader = agentTab.find("#agents-manager-header");

const agentsManagerBreadcrumb = agentsManagerHeader.find("#agents-manager-breadcrumb");

const agentsManagerListTab = agentsManagerHeader.find("#agents-manager-tab");

const switchBackToAgentsTab = agentsManagerHeader.find("#switchBackToAgentsTab");
const currentAgentName = agentsManagerHeader.find("#currentAgentName");

const confirmPublishAgentButton = agentsManagerHeader.find("#confirmPublishAgentButton");
const confirmPublishAgentButtonSpinner = agentsManagerHeader.find(".save-button-spinner");

const agentsManagerTab = agentTab.find("#agentsManagerTab");

// SUB | Integrations Tab
const agentIntegrationsTab = $("#agents-manager-integrations");
const sttIntegrationsList = agentIntegrationsTab.find("#sttIntegrationsList");
const llmIntegrationsList = agentIntegrationsTab.find("#llmIntegrationsList");
const ttsIntegrationsList = agentIntegrationsTab.find("#ttsIntegrationsList");
const addSTTIntegrationButton = agentIntegrationsTab.find("#addSTTIntegration");
const addLLMIntegrationButton = agentIntegrationsTab.find("#addLLMIntegration");
const addTTSIntegrationButton = agentIntegrationsTab.find("#addTTSIntegration");

// SUB | Interruptions Tab
const agentsManagerInterruptionsTab = agentTab.find("#agents-manager-interruptions");
// Turn-end Detection
const editAgentTurnEndTypeSelect = agentsManagerInterruptionsTab.find("#editAgentTurnEndTypeSelect");
const agentTurnEndViaVADBox = agentsManagerInterruptionsTab.find('.agent-turn-end-type-box[box-type="turnendviavad"]');
const editAgentTurnEndViaVADAudioActivityDuration = agentsManagerInterruptionsTab.find("#editAgentTurnEndViaVADAudioActivityDuration");
const agentTurnEndViaAIBox = agentsManagerInterruptionsTab.find('.agent-turn-end-type-box[box-type="turnendviaai"]');
const editAgentTurnEndViaAIUseAgentLLM = agentsManagerInterruptionsTab.find("#editAgentTurnEndViaAIUseAgentLLM");
const agentTurnEndViaLLMIntegrationSelectBox = agentsManagerInterruptionsTab.find("#agentTurnEndViaLLMIntegrationSelectBox");
// Turn by Turn/Disable Interruptions
const editAgentTurnByTurnMode = agentsManagerInterruptionsTab.find("#editAgentTurnByTurnMode");
const editAgentTurnByTurnIncludeInterruptedSpeech = agentsManagerInterruptionsTab.find("#editAgentTurnByTurnIncludeInterruptedSpeech");
// Agent Pause Trigger
const agentInterruptionPauseTypeSelect = agentsManagerInterruptionsTab.find("#agentInterruptionPauseTypeSelect");
const agentInterruptionPauseViaVADBox = agentsManagerInterruptionsTab.find('.agent-interruption-pause-type-box[box-type="pauseviavad"]');
const agentInterruptionPauseVADDuration = agentsManagerInterruptionsTab.find("#agentInterruptionPauseVADDuration");
const agentInterruptionPauseViaWordsBox = agentsManagerInterruptionsTab.find('.agent-interruption-pause-type-box[box-type="pauseviawords"]');
const agentInterruptionPauseWordCount = agentsManagerInterruptionsTab.find("#agentInterruptionPauseWordCount");
// Interruption Verification
const enableAgentInterruptionVerification = agentsManagerInterruptionsTab.find("#enableAgentInterruptionVerification");
const agentInterruptionVerificationContainer = agentsManagerInterruptionsTab.find("#agentInterruptionVerificationContainer");
const agentInterruptionVerifyAIUseAgentLLM = agentsManagerInterruptionsTab.find("#agentInterruptionVerifyAIUseAgentLLM");
const agentInterruptionVerifyLLMIntegrationSelectBox = agentsManagerInterruptionsTab.find("#agentInterruptionVerifyLLMIntegrationSelectBox");

// SUB | Cache Tab
const agentCacheTab = $("#agents-manager-cache");
// Messages
const messageCacheGroupsList = agentCacheTab.find("#messageCacheGroupsList");
const addMessageCacheGroupButton = agentCacheTab.find("#addMessageCacheGroup");
// Audios
const audioCacheGroupsList = agentCacheTab.find("#audioCacheGroupsList");
const addAudioCacheGroupButton = agentCacheTab.find("#addAudioCacheGroup");
// Embeddings
const embeddingsCacheGroupsList = agentCacheTab.find("#embeddingsCacheGroupsList");
const addEmbeddingCacheGroupButton = agentCacheTab.find("#addEmbeddingCacheGroup");
// Audio Settings
const agentCacheSettingsAutoCacheAudioCheckbox = agentCacheTab.find("#agentCacheSettingsAutoCacheAudio");
const agentCacheSettingsAutoCacheAudioBox = agentCacheTab.find("#agentCacheSettingsAutoCacheAudioBox");
const agentAutoCacheAudioGroupSelect = agentCacheTab.find("#agentAutoCacheAudioGroupSelect");
const agentAutoCacheAudioExpiryInput = agentCacheTab.find("#agentAutoCacheAudioExpiryInput");
// Embedding Settings
const agentCacheSettingsAutoCacheEmbeddingCheckbox = agentCacheTab.find("#agentCacheSettingsAutoCacheEmbedding");
const agentCacheSettingsAutoCacheEmbeddingBox = agentCacheTab.find("#agentCacheSettingsAutoCacheEmbeddingBox");
const agentAutoCacheEmbeddingGroupSelect = agentCacheTab.find("#agentAutoCacheEmbeddingGroupSelect");
const agentAutoCacheEmbeddingExpiryInput = agentCacheTab.find("#agentAutoCacheEmbeddingExpiryInput");

// SUB | Knowledge Base Tab
const agentKbTab = $("#agents-manager-knowledgebases");
// List
const agentKbGroupSelect = agentKbTab.find("#agentKbGroupSelect");
const addAgentKbGroupButton = agentKbTab.find("#addAgentKbGroupButton");
const linkedKnowledgeBasesContainer = agentKbTab.find("#linkedKnowledgeBasesContainer");
// Settings
const agentKbSearchStrategySelect = agentKbTab.find("#agentKbSearchStrategySelect");
const agentKbSpecificKeywordsBox = agentKbTab.find("#agentKbSpecificKeywordsBox");
const agentKbSpecificKeywordsTextarea = agentKbTab.find("#agentKbSpecificKeywordsTextarea");
const agentKbKbKeywordsBox = agentKbTab.find("#agentKbKbKeywordsBox");
const agentKbLlmClassifierBox = agentKbTab.find("#agentKbLlmClassifierBox");
const agentKbClassifierUseAgentLLM = agentKbTab.find("#agentKbClassifierUseAgentLLM");
const agentKbClassifierLLMIntegrationSelectBox = agentKbTab.find("#agentKbClassifierLLMIntegrationSelectBox");
const agentKbEnableQueryRefinement = agentKbTab.find("#agentKbEnableQueryRefinement");
const agentKbQueryRefinementOptionsBox = agentKbTab.find("#agentKbQueryRefinementOptionsBox");
const agentKbRefinementQueryCount = agentKbTab.find("#agentKbRefinementQueryCount");
const agentKbRefinementUseAgentLLM = agentKbTab.find("#agentKbRefinementUseAgentLLM");
const agentKbRefinementLLMIntegrationSelectBox = agentKbTab.find("#agentKbRefinementLLMIntegrationSelectBox");

// SUB | Settings Tab
const editAgentBackgroundAudioSelect = agentTab.find("#editAgentBackgroundAudioSelect");

// SUB | Audio Tab
const agentBackgroundAudioBox = agentTab.find("#agentBackgroundAudioBox");
const agentBackgroundAudioSelect = editAgentBackgroundAudioSelect;
const agentBackgroundAudioInputBox = agentBackgroundAudioBox.find("#agentBackgroundAudioInputBox");
const agentBackgroundAudioUploadBtn = agentBackgroundAudioInputBox.find("#agent-background-audio-upload-btn");
const agentBackgroundAudioUploadInput = agentBackgroundAudioInputBox.find("#agentBackgroundAudioUploadInput");
const agentBackgroundAudioVolumeInput = agentBackgroundAudioBox.find("#agentBackgroundAudioVolumeInput");

/** API FUNCTIONS **/

function SaveBusinessAgent(formData, onSuccess, onError) {
	return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/agents/save`,
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
function DeleteBusinessAgent(agentId, onSuccess, onError) {
    return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/agents/${agentId}/delete`,
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

function BuildAgentIntegrationsChanges() {
	return {
		stt: agentSTTIntegrationManager.getData(),
		llm: agentLLMIntegrationManager.getData(),
		tts: agentTTSIntegrationManager.getData(),
	};
}

function CheckAgentIntegrationsChanges() {
	const changes = BuildAgentIntegrationsChanges();
	const hasChanges = AgentIntegrationsChanges.compare(CurrentManageAgentData.integrations, changes).hasChanges;
	return { hasChanges, changes };
}

function CheckAgentTabHasChanges(enableDisableButton = true) {
	if (ManageAgentType === null) {
		return { changes: {}, hasChanges: false };
	}

	if (enableDisableButton) {
		const hasChanges = AgentTabSectionsChanges.compare().hasChanges;
		confirmPublishAgentButton.prop("disabled", !hasChanges);
		return { changes: {}, hasChanges };
	}

	const changes = {};
	let hasChanges = false;

	const sections = [
		["general", CheckAgentGeneralTabChanges],
		["context", CheckAgentContextTabChanges],
		["personality", CheckAgentPersonalityTabChanges],
		["utterances", CheckAgentUtterancesTabChanges],
		["interruptions", CheckAgentInterruptionsTabChanges],
		["integrations", CheckAgentIntegrationsChanges],
		["knowledgeBase", CheckAgentKnowledgeBaseTabChanges],
		["cache", CheckAgentCacheTabChanges],
		["settings", CheckAgentSettingsTabChanges],
	];
	sections.forEach(([name, check]) => {
		const sectionResult = check(false);
		changes[name] = sectionResult.changes;
		if (sectionResult.hasChanges) {
			hasChanges = true;
		}
	});

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
			timeout: 6000,
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
			openingType: {
				value: 0,
			},
			greetingMessage: {}
		},
		interruptions: {
			turnEnd: {
				type: {
					value: AGENT_INTERRUPTION_TURN_END_TYPE.VAD
				},
				vadSilenceDurationMS: 700,
				useAgentLLM: null,
				llmIntegration: null
			},
			useTurnByTurnMode: false,
			includeInterruptedSpeechInTurnByTurnMode: false,
			pauseTrigger: {
				type: {
					value: AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE.VAD
				},
				vadDurationMS: 100,
				wordCount: null
			},
			verification: {
				enabled: false,
				useAgentLLM: true,
				llmIntegration: null
			}
		},
		knowledgeBase: {
			linkedGroups: [],
			searchStrategy: {
				type: {
					value: AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.KNOWLEDGEBASE_KEYWORD
				},
				specificKeywords: null,
				llmClassifier: null,
			},
			refinement: {
				enabled: false,
				queryCount: null,
				useAgentLLM: null,
				llmIntegration: null
			}
		},
		integrations: {
			stt: {},
			llm: {},
			tts: {},
		},
		cache: {
			messages: [],
			audios: [],
			audioCacheSettings: {
				autoCacheAudioResponses: false,
				autoCacheAudioResponseCacheGroupId: null,
				autoCacheAudioResponsesDefaultExpiryHours: null
			},
			embeddings: [],
			embeddingsCacheSettings: {
				autoCacheEmbeddingResponses: false,
				autoCacheEmbeddingResponseCacheGroupId: null,
				autoCacheEmbeddingResponsesDefaultExpiryHours: null
			}
		},
		settings: {
			backgroundAudioUrl: null,
			backgroundAudioVolume: 100,
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

		// Initialize integrations for each language
		agent.integrations.stt[language] = [];
		agent.integrations.llm[language] = [];
		agent.integrations.tts[language] = [];
	});

	return agent;
}

function validateAgentMultiLanguageElements() {
	if (ManageAgentType == null) return;

	const anyLanguagesIncomplete = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		const currentSelectedLanguage = SpecificationLanguagesListData.find((d) => d.id === language);
		anyLanguagesIncomplete[language] = false;

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
		const sttData = agentSTTIntegrationManager.getData();
		const llmData = agentLLMIntegrationManager.getData();
		const ttsData = agentTTSIntegrationManager.getData();

		const sttIntegrationsIncomplete = !(sttData[language] && sttData[language].length > 0);
		const llmIntegrationsIncomplete = !(llmData[language] && llmData[language].length > 0);
		const ttsIntegrationsIncomplete = !(ttsData[language] && ttsData[language].length > 0);

		const isAnyIncompleteInIntegrations = sttIntegrationsIncomplete || llmIntegrationsIncomplete || ttsIntegrationsIncomplete;

		/** Utterances Tab **/
		// Greeting Message
		const greetingMessage = CurrentAgentUtterancesGreetingMessageMultiLangData[currentSelectedLanguage.id];
		const greetingMessageIsIncomplete = !greetingMessage || greetingMessage === "" || greetingMessage.trim() === "";

		// Update language status
		const isAnyIncompleteInUtterances = greetingMessageIsIncomplete;

		/** Update language status **/
		const isAnyIncomplete = isAnyIncompleteInGeneral || isAnyIncompleteInPersonality || isAnyIncompleteInUtterances || isAnyIncompleteInIntegrations;

		anyLanguagesIncomplete[language] = isAnyIncomplete;
		manageAgentsLanguageDropdown.setLanguageStatus(currentSelectedLanguage.id, isAnyIncomplete ? "incomplete" : "complete");
	});

	return anyLanguagesIncomplete;
}

function ResetAndEmptyAgentsManageTab() {
	// Audio
	if (AgentBackgroundAudioWaveSurfer !== null) {
		AgentBackgroundAudioWaveSurfer.destroy();
        AgentBackgroundAudioWaveSurfer = null;
	}
	agentBackgroundAudioVolumeInput.val("100");
	agentBackgroundAudioInputBox.find(".no-audio-notice").removeClass("d-none");
	agentBackgroundAudioInputBox.find(".recording-container-waveform").addClass("d-none");
	agentBackgroundAudioInputBox.find(".audio-controller").addClass("d-none");
	agentBackgroundAudioUploadInput.val("");
	agentBackgroundAudioSelect.val("none").change();

	// General Tab
	CurrentAgentGeneralNameMultiLangData = {};
	CurrentAgentGeneralDescriptionMultiLangData = {};

	// Personality Tab
	CurrentAgentPersonalityNameMultiLangData = {};
	CurrentAgentPersonalityRoleMultiLangData = {};
	CurrentAgentPersonalityCapabilitiesMultiLangData = {};
	CurrentAgentPersonalityEthicsMultiLangData = {};
	CurrentAgentPersonalityToneMultiLangData = {};

	// Utterances Tab
	CurrentAgentUtterancesGreetingMessageMultiLangData = {};

	// Reset Interruptions Tab
	editAgentTurnEndTypeSelect.val("0").change(); // Default to VAD
	editAgentTurnEndViaVADAudioActivityDuration.val(700);
	editAgentTurnEndViaAIUseAgentLLM.prop("checked", true).change();
	if (agentTurnEndLLMIntegrationManager) agentTurnEndLLMIntegrationManager.reset();

	agentInterruptionPauseTypeSelect.val("vad").change(); // Default to VAD
	agentInterruptionPauseVADDuration.val(100);
	agentInterruptionPauseWordCount.val(2);

	enableAgentInterruptionVerification.prop("checked", false).change();
	agentInterruptionVerifyAIUseAgentLLM.prop("checked", true).change();
	if (agentInterruptionVerifyLLMIntegrationManager) agentInterruptionVerifyLLMIntegrationManager.reset();

	// Integration Tab
	if (agentSTTIntegrationManager) agentSTTIntegrationManager.reset();
	if (agentLLMIntegrationManager) agentLLMIntegrationManager.reset();
	if (agentTTSIntegrationManager) agentTTSIntegrationManager.reset();

	// Cache
	CurrentAgentCacheMessages = [];
	CurrentAgentCacheAudios = [];

	// Knowledge Base
	ResetAndEmptyAgentKnowledgeBaseTab();

	// Reset languages
	BusinessFullData.businessData.languages.forEach((language) => {
		// General Tab
		CurrentAgentGeneralNameMultiLangData[language] = "";
		CurrentAgentGeneralDescriptionMultiLangData[language] = "";

		// Personality Tab
		CurrentAgentPersonalityNameMultiLangData[language] = "";
		CurrentAgentPersonalityRoleMultiLangData[language] = "";
		CurrentAgentPersonalityCapabilitiesMultiLangData[language] = [];
		CurrentAgentPersonalityEthicsMultiLangData[language] = [];
		CurrentAgentPersonalityToneMultiLangData[language] = [];

		// Utterances Tab
		CurrentAgentUtterancesGreetingMessageMultiLangData[language] = "";

		manageAgentsLanguageDropdown.setLanguageStatus(language, "incomplete");
	});

	// Audio Settings
	agentCacheSettingsAutoCacheAudioCheckbox.prop("checked", false).change();
	agentAutoCacheAudioGroupSelect.empty();
	agentAutoCacheAudioGroupSelect.append($(`<option value="" disabled selected>Select Audio Group</option>`));
	BusinessFullData.businessApp.cache.audioGroups.forEach((group) => {
		agentAutoCacheAudioGroupSelect.append($(`<option value="${group.id}">${group.name}</option>`));
	});
	agentAutoCacheAudioExpiryInput.val(24);

	// Embedding Settings
    agentCacheSettingsAutoCacheEmbeddingCheckbox.prop("checked", false).change();
    agentAutoCacheEmbeddingGroupSelect.empty();
    agentAutoCacheEmbeddingGroupSelect.append($(`<option value="" disabled selected>Select Embedding Group</option>`));
    BusinessFullData.businessApp.cache.embeddingGroups.forEach((group) => {
        agentAutoCacheEmbeddingGroupSelect.append($(`<option value="${group.id}">${group.name}</option>`));
	});
    agentAutoCacheEmbeddingExpiryInput.val(24);

	confirmPublishAgentButton.prop("disabled", true);
	$("#agents-manager-general-tab").click();
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

function ValidateAgentTab(onlyRemove = true) {
	const errors = [];
	let isValid = true;

	const isGeneralTabValid = validateAgentGeneralTab(onlyRemove);
	if (!isGeneralTabValid.isValid) {
		isValid = false;
		errors.push(...isGeneralTabValid.errors);
	}

	const isPersontalityTabValid = validateAgentPersonalityTab(onlyRemove);
	if (!isPersontalityTabValid.isValid) {
		isValid = false;
		errors.push(...isPersontalityTabValid.errors);
	}

	const isUtterancesTabValid = validateAgentUtterancesTab(onlyRemove);
	if (!isUtterancesTabValid.isValid) {
		isValid = false;
		errors.push(...isUtterancesTabValid.errors);
	}

	const isInterruptionsTabValid = validateAgentInterruptionsTab(onlyRemove);
	if (!isInterruptionsTabValid.isValid) {
		isValid = false;
		errors.push(...isInterruptionsTabValid.errors);
	}

	const isIntegrationsTabValid = validateAgentIntegrationsTab(onlyRemove);
	if (!isIntegrationsTabValid.isValid) {
		isValid = false;
		errors.push(...isIntegrationsTabValid.errors);
	}

	const isKnowledgeBaseTabValid = validateAgentKnowledgeBaseTab(onlyRemove);
	if (!isKnowledgeBaseTabValid.isValid) {
		isValid = false;
		errors.push(...isKnowledgeBaseTabValid.errors);
	}

	const isCacheTabValid = validateAgentCacheTab(onlyRemove);
	if (!isCacheTabValid.isValid) {
		isValid = false;
		errors.push(...isCacheTabValid.errors);
	}

	const isSettingsTabValid = validateAgentSettingsTab(onlyRemove);
	if (!isSettingsTabValid.isValid) {
		isValid = false;
		errors.push(...isSettingsTabValid.errors);
	}

	return { isValid, errors };
}

function CreateAgentsCardElement(agentData) {
	const actionDropdownHtml = `
        <div class="dropdown action-dropdown dropdown-menu-end">
            <button class="btn action-button dropdown-toggle" type="button" data-bs-toggle="dropdown" data-bs-auto-close="true" aria-expanded="false">
                <i class="fa-solid fa-ellipsis"></i>
            </button>
            <ul class="dropdown-menu">
                <li>
                    <span class="dropdown-item text-danger" data-item-id="${agentData.id}" button-type="delete-agent">
                        <i class="fa-solid fa-trash me-2"></i>Delete
                    </span>
                </li>
            </ul>
        </div>
    `;

	return createIqraCardElement({
		id: agentData.id,
		type: 'agent',
		visualHtml: `<span>${agentData.general.emoji}</span>`,
		titleHtml: agentData.general.name[BusinessDefaultLanguage],
		descriptionHtml: agentData.general.description[BusinessDefaultLanguage],
		actionDropdownHtml: actionDropdownHtml,
	});
}

function FillAgentsListTab() {
	const agents = BusinessFullData.businessApp.agents;

	if (agents.length === 0) {
		agentsCardListContainer.append('<div class="col-12 none-agents-list-notice"><h6 class="text-center mt-5">No agents added yet...</h6></div>');
		return;
	}

	agents.forEach((agent) => {
		const element = CreateAgentsCardElement(agent);
		agentsCardListContainer.append(element);
	});
}

function FillAgentsManagerTab() {
	fillAgentGeneralTab();
	FillAgentContextTab();
	fillAgentPersonalityTab();
	fillAgentUtterancesTab();
	fillAgentInterruptionsTab();
	fillAgentKnowledgeBaseTab();
	fillIntegrationsFromAgentData();
	fillAgentCacheTab();
	fillAgentSettingsTab();
}

// General Tab Functions
function BuildAgentGeneralChanges() {
	const changes = {
		emoji: $("#editAgentIconButton").text(),
		name: {},
		description: {},
	};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.name[language] = CurrentAgentGeneralNameMultiLangData[language];
	});
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.description[language] = CurrentAgentGeneralDescriptionMultiLangData[language];
	});
	return changes;
}

function CheckAgentGeneralTabChanges(enableDisableButton = true) {
	const changes = BuildAgentGeneralChanges();
	const hasChanges = AgentGeneralChanges.compare(CurrentManageAgentData.general, changes).hasChanges;

	if (enableDisableButton) {
		confirmPublishAgentButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

function fillAgentGeneralTab() {
	const currentLanguage = manageAgentsLanguageDropdown.getSelectedLanguage().id;

	// Emoji
	$("#editAgentIconButton").text(CurrentManageAgentData.general.emoji);

	// Name
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentGeneralNameMultiLangData[language] = CurrentManageAgentData.general.name[language];
	});
	$("#editAgentIdentifierInput").val(CurrentAgentGeneralNameMultiLangData[currentLanguage]);

	// Description
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentGeneralDescriptionMultiLangData[language] = CurrentManageAgentData.general.description[language];
	});
	$("#editAgentDescriptionInput").val(CurrentAgentGeneralDescriptionMultiLangData[currentLanguage]);
}

function validateAgentGeneralTab(onlyRemove = true) {
	const errors = [];
	let isValid = true;

	// Validate identifier for all languages
	BusinessFullData.businessData.languages.forEach((language) => {
		if (!CurrentAgentGeneralNameMultiLangData[language] || CurrentAgentGeneralNameMultiLangData[language].trim().length === 0) {
			isValid = false;
			errors.push(`Agent identifier for language ${language} is required.`);

			if (!onlyRemove) {
				$("#editAgentIdentifierInput").addClass("is-invalid");
			}
		} else {
			$("#editAgentIdentifierInput").removeClass("is-invalid");
		}
	});

	// Validate description for all languages
	BusinessFullData.businessData.languages.forEach((language) => {
		if (!CurrentAgentGeneralDescriptionMultiLangData[language] || CurrentAgentGeneralDescriptionMultiLangData[language].trim().length === 0) {
			isValid = false;
			errors.push(`Agent description for language ${language} is required.`);

			if (!onlyRemove) {
				$("#editAgentDescriptionInput").addClass("is-invalid");
			}
		} else {
			$("#editAgentDescriptionInput").removeClass("is-invalid");
		}
	});

	return {
		isValid,
		errors,
	};
}

// Context Tab Functions
function BuildAgentContextChanges() {
	return {
		useBranding: $("#agentEditContextEnableBranding").prop("checked"),
		useBranches: $("#agentEditContextEnableBranches").prop("checked"),
		useServices: $("#agentEditContextEnableServices").prop("checked"),
		useProducts: $("#agentEditContextEnableProducts").prop("checked"),
	};
}

function CheckAgentContextTabChanges(enableDisableButton = true) {
	const changes = BuildAgentContextChanges();
	const hasChanges = AgentContextChanges.compare(CurrentManageAgentData.context, changes).hasChanges;

	if (enableDisableButton) {
		confirmPublishAgentButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

function FillAgentContextTab() {
	$("#agentEditContextEnableBranding").prop("checked", CurrentManageAgentData.context.useBranding);
	$("#agentEditContextEnableBranches").prop("checked", CurrentManageAgentData.context.useBranches);
	$("#agentEditContextEnableServices").prop("checked", CurrentManageAgentData.context.useServices);
	$("#agentEditContextEnableProducts").prop("checked", CurrentManageAgentData.context.useProducts);
}

// Personality Tab Functions
function BuildAgentPersonalityChanges() {
	const changes = { name: {}, role: {} };
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.name[language] = CurrentAgentPersonalityNameMultiLangData[language];
	});
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.role[language] = CurrentAgentPersonalityRoleMultiLangData[language];
	});

	["capabilities", "ethics", "tone"].forEach((listType) => {
		changes[listType] = {};
		const currentData =
			listType === "capabilities" ? CurrentAgentPersonalityCapabilitiesMultiLangData : listType === "ethics" ? CurrentAgentPersonalityEthicsMultiLangData : CurrentAgentPersonalityToneMultiLangData;
		BusinessFullData.businessData.languages.forEach((language) => {
			changes[listType][language] = currentData[language] || [];
		});
	});

	return changes;
}

function CheckAgentPersonalityTabChanges(enableDisableButton = true) {
	const changes = BuildAgentPersonalityChanges();
	const hasChanges = AgentPersonalityChanges.compare(CurrentManageAgentData.personality, changes).hasChanges;

	if (enableDisableButton) {
		confirmPublishAgentButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

function fillAgentPersonalityTab() {
	const currentLanguage = manageAgentsLanguageDropdown.getSelectedLanguage().id;

	// Name
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentPersonalityNameMultiLangData[language] = CurrentManageAgentData.personality.name[language];
	});
	$("#editAgentPersonalityNameInput").val(CurrentAgentPersonalityNameMultiLangData[currentLanguage]);

	// Role
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentPersonalityRoleMultiLangData[language] = CurrentManageAgentData.personality.role[language];
	});
	$("#editAgentPersonalityRoleInput").val(CurrentAgentPersonalityRoleMultiLangData[currentLanguage]);

	// Lists
	["capabilities", "ethics", "tone"].forEach((listType) => {
		const currentData =
			listType === "capabilities" ? CurrentAgentPersonalityCapabilitiesMultiLangData : listType === "ethics" ? CurrentAgentPersonalityEthicsMultiLangData : CurrentAgentPersonalityToneMultiLangData;

		BusinessFullData.businessData.languages.forEach((language) => {
			currentData[language] = CurrentManageAgentData.personality[listType][language] || [];
		});

		// Fill the list for default language
		const container = $(`#editAgentPersonality${listType.charAt(0).toUpperCase() + listType.slice(1)}ValueInputs`);
		container.empty();
		currentData[currentLanguage].forEach((value) => {
			container.append(`
                <div class="input-group mb-1">
                    <textarea class="form-control">${value}</textarea>
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

			if (!onlyRemove) {
				$("#editAgentPersonalityNameInput").addClass("is-invalid");
			}
		} else {
			$("#editAgentPersonalityNameInput").removeClass("is-invalid");
		}
	});

	// Validate role for all languages
	BusinessFullData.businessData.languages.forEach((language) => {
		if (!CurrentAgentPersonalityRoleMultiLangData[language] || CurrentAgentPersonalityRoleMultiLangData[language].trim().length === 0) {
			isValid = false;
			errors.push(`Agent personality role for language ${language} is required.`);

			if (!onlyRemove) {
				$("#editAgentPersonalityRoleInput").addClass("is-invalid");
			}
		} else {
			$("#editAgentPersonalityRoleInput").removeClass("is-invalid");
		}
	});

	return {
		isValid,
		errors,
	};
}

// Utterances Tab Functions
function BuildAgentUtterancesChanges() {
	const changes = {
		openingType: parseInt($("#editAgentGreetingStartTypeInput").val()),
		greetingMessage: {},
	};
	BusinessFullData.businessData.languages.forEach((language) => {
		changes.greetingMessage[language] = CurrentAgentUtterancesGreetingMessageMultiLangData[language];
	});
	return changes;
}

function CheckAgentUtterancesTabChanges(enableDisableButton = true) {
	const changes = BuildAgentUtterancesChanges();
	const hasChanges = AgentUtterancesChanges.compare(CurrentManageAgentData.utterances, changes).hasChanges;

	if (enableDisableButton) {
		confirmPublishAgentButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

function fillAgentUtterancesTab() {
	const currentLanguage = manageAgentsLanguageDropdown.getSelectedLanguage().id;

	// Opening Type
	$("#editAgentGreetingStartTypeInput").val(CurrentManageAgentData.utterances.openingType.value).change();

	// Greeting Message
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentAgentUtterancesGreetingMessageMultiLangData[language] = CurrentManageAgentData.utterances.greetingMessage[language];
	});
	$("#editAgentPersonalityGreetingInput").val(CurrentAgentUtterancesGreetingMessageMultiLangData[currentLanguage]);
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

			if (!onlyRemove) {
				$("#editAgentPersonalityGreetingInput").addClass("is-invalid");
			}
		} else {
			$("#editAgentPersonalityGreetingInput").removeClass("is-invalid");
		}
	});

	return {
		isValid,
		errors,
	};
}

// Interruptions Tab Functions
function fillAgentInterruptionsTab() {
	const interruptions = CurrentManageAgentData.interruptions;

	if (!interruptions) return; // Safeguard for older data models

	// Fill Turn-end
	editAgentTurnEndTypeSelect.val(interruptions.turnEnd.type.value).change();

	if (interruptions.turnEnd.type.value == AGENT_INTERRUPTION_TURN_END_TYPE.VAD) {
		editAgentTurnEndViaVADAudioActivityDuration.val(interruptions.turnEnd.vadSilenceDurationMS);
	}
	else if (interruptions.turnEnd.type.value == AGENT_INTERRUPTION_TURN_END_TYPE.AI) {
		editAgentTurnEndViaAIUseAgentLLM.prop("checked", interruptions.turnEnd.useAgentLLM).change();
		if (!interruptions.turnEnd.useAgentLLM && interruptions.turnEnd.llmIntegration) {
			agentTurnEndLLMIntegrationManager.load(interruptions.turnEnd.llmIntegration, BusinessFullData.businessApp.integrations);
		} else {
			agentTurnEndLLMIntegrationManager.reset();
		}
	}

	// Turn by Turn
	editAgentTurnByTurnMode.prop("checked", interruptions.useTurnByTurnMode).change();

	if (interruptions.useTurnByTurnMode) {
		editAgentTurnByTurnIncludeInterruptedSpeech.prop("checked", interruptions.includeInterruptedSpeechInTurnByTurnMode).change();
	}
	else {
		// Fill Pause Trigger
		agentInterruptionPauseTypeSelect.val(interruptions.pauseTrigger.type.value).change();

		if (interruptions.pauseTrigger.type.value == AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE.VAD) {
			agentInterruptionPauseVADDuration.val(interruptions.pauseTrigger.vadDurationMS);
		}
		else if (interruptions.pauseTrigger.type.value == AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE.STT) {
			agentInterruptionPauseWordCount.val(interruptions.pauseTrigger.wordCount);
		}

		// Fill Verification
		enableAgentInterruptionVerification.prop("checked", interruptions.verification.enabled).change();
		if (interruptions.verification.enabled) {
			agentInterruptionVerifyAIUseAgentLLM.prop("checked", interruptions.verification.useAgentLLM).change();
			
			if (interruptions.verification.enabled && !interruptions.verification.useAgentLLM && interruptions.verification.llmIntegration) {
				agentInterruptionVerifyLLMIntegrationManager.load(interruptions.verification.llmIntegration, BusinessFullData.businessApp.integrations);
			} else {
				agentInterruptionVerifyLLMIntegrationManager.reset();
			}
		}		
	}
}

function BuildAgentInterruptionsChanges() {
	const changes = {};

	changes.turnEnd = {
		type: parseInt(editAgentTurnEndTypeSelect.val())
	};
	if (changes.turnEnd.type == AGENT_INTERRUPTION_TURN_END_TYPE.VAD) {
		changes.turnEnd.vadSilenceDurationMS = parseInt(editAgentTurnEndViaVADAudioActivityDuration.val());
	}
	else if (changes.turnEnd.type == AGENT_INTERRUPTION_TURN_END_TYPE.AI) {
		changes.turnEnd.useAgentLLM = editAgentTurnEndViaAIUseAgentLLM.is(":checked");
		if (!changes.turnEnd.useAgentLLM) {
			changes.turnEnd.llmIntegration = agentTurnEndLLMIntegrationManager.getData();
		}
	}

	changes.useTurnByTurnMode = editAgentTurnByTurnMode.is(":checked");
	if (changes.useTurnByTurnMode) {
		changes.includeInterruptedSpeechInTurnByTurnMode = editAgentTurnByTurnIncludeInterruptedSpeech.is(":checked");
	}
	else {
		changes.pauseTrigger = {
			type: parseInt(agentInterruptionPauseTypeSelect.val())
		};
		if (changes.pauseTrigger.type == AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE.VAD) {
			changes.pauseTrigger.vadDurationMS = parseInt(agentInterruptionPauseVADDuration.val());
		}
		else if (changes.pauseTrigger.type == AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE.STT) {
			changes.pauseTrigger.wordCount = parseInt(agentInterruptionPauseWordCount.val());
		}

		changes.verification = {
			enabled: enableAgentInterruptionVerification.is(":checked")
		};
		if (changes.verification.enabled) {
			changes.verification.useAgentLLM = agentInterruptionVerifyAIUseAgentLLM.is(":checked");
			if (!changes.verification.useAgentLLM) {
				changes.verification.llmIntegration = agentInterruptionVerifyLLMIntegrationManager.getData();
			}
		}
	}

	return changes;
}

function CheckAgentInterruptionsTabChanges(enableDisableButton = true) {
	const changes = BuildAgentInterruptionsChanges();
	const hasChanges = AgentInterruptionsChanges.compare(CurrentManageAgentData.interruptions, changes).hasChanges;
	return { hasChanges, changes };
}

function validateAgentInterruptionsTab(onlyRemove = true) {
	const errors = [];
	let isValid = true;

	// Validate Turn-end
	const turnEndType = parseInt(editAgentTurnEndTypeSelect.val());
	if (turnEndType == AGENT_INTERRUPTION_TURN_END_TYPE.VAD) { // VAD
		const duration = parseInt(editAgentTurnEndViaVADAudioActivityDuration.val());
		if (isNaN(duration) || duration <= 0) {
			isValid = false;
			errors.push("Turn-end silence duration must be a positive number.");
			if (!onlyRemove) editAgentTurnEndViaVADAudioActivityDuration.addClass("is-invalid");
		} else {
			editAgentTurnEndViaVADAudioActivityDuration.removeClass("is-invalid");
		}
	} else if (turnEndType == AGENT_INTERRUPTION_TURN_END_TYPE.AI) { // AI
		if (!editAgentTurnEndViaAIUseAgentLLM.is(":checked")) {
			const validation = agentTurnEndLLMIntegrationManager.validate();
			if (!validation.isValid) {
				isValid = false;
				errors.push(...validation.errors.map(e => `Turn-end LLM: ${e}`));
			}
		}
	}

	// Validate Pause Trigger
	const pauseTriggerType = parseInt(agentInterruptionPauseTypeSelect.val());
	if (pauseTriggerType == AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE.VAD) {
		const duration = parseInt(agentInterruptionPauseVADDuration.val());
		if (isNaN(duration) || duration <= 0) {
			isValid = false;
			errors.push("Pause trigger voice duration must be a positive number.");
			if (!onlyRemove) agentInterruptionPauseVADDuration.addClass("is-invalid");
		} else {
			agentInterruptionPauseVADDuration.removeClass("is-invalid");
		}
	} else if (pauseTriggerType == AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE.STT) {
		const count = parseInt(agentInterruptionPauseWordCount.val());
		if (isNaN(count) || count <= 0) {
			isValid = false;
			errors.push("Pause trigger word count must be a positive number.");
			if (!onlyRemove) agentInterruptionPauseWordCount.addClass("is-invalid");
		} else {
			agentInterruptionPauseWordCount.removeClass("is-invalid");
		}
	}

	// Validate Verification
	if (enableAgentInterruptionVerification.is(":checked")) {
		if (!agentInterruptionVerifyAIUseAgentLLM.is(":checked")) {
			const validation = agentInterruptionVerifyLLMIntegrationManager.validate();
			if (!validation.isValid) {
				isValid = false;
				errors.push(...validation.errors.map(e => `Verification LLM: ${e}`));
			}
		}
	}

	return { isValid, errors };
}

// Integration Tab Functions
function fillIntegrationsFromAgentData() {
	agentSTTIntegrationManager.load(CurrentManageAgentData.integrations.stt, BusinessFullData.businessApp.integrations);
	agentLLMIntegrationManager.load(CurrentManageAgentData.integrations.llm, BusinessFullData.businessApp.integrations);
	agentTTSIntegrationManager.load(CurrentManageAgentData.integrations.tts, BusinessFullData.businessApp.integrations);
}

function validateAgentIntegrationsTab() {
	const errors = [];
	let isValid = true;

	// Validate each language has required integrations
	BusinessFullData.businessData.languages.forEach((languageId) => {
		const langData = SpecificationLanguagesListData.find(l => l.id === languageId);
		const langName = langData ? langData.name : languageId;

		// Get data directly from managers
		const sttData = agentSTTIntegrationManager.getData()[languageId] || [];
		const llmData = agentLLMIntegrationManager.getData()[languageId] || [];
		const ttsData = agentTTSIntegrationManager.getData()[languageId] || [];

		if (sttData.length === 0) {
			isValid = false;
			errors.push(`${langName}: At least one Speech-to-Text integration is required.`);
		}
		if (llmData.length === 0) {
			isValid = false;
			errors.push(`${langName}: At least one Language Model integration is required.`);
		}
		if (ttsData.length === 0) {
			isValid = false;
			errors.push(`${langName}: At least one Text-to-Speech integration is required.`);
		}
	});

	if (!isValid) {
		// Return early if basic requirements aren't met
		return { isValid, errors };
	}

	// Now, use the managers to validate the configurations of all selected integrations
	const sttValidation = agentSTTIntegrationManager.validate();
	if (!sttValidation.isValid) {
		isValid = false;
		errors.push(...sttValidation.errors);
	}

	const llmValidation = agentLLMIntegrationManager.validate();
	if (!llmValidation.isValid) {
		isValid = false;
		errors.push(...llmValidation.errors);
	}

	const ttsValidation = agentTTSIntegrationManager.validate();
	if (!ttsValidation.isValid) {
		isValid = false;
		errors.push(...ttsValidation.errors);
	}

	return { isValid, errors };
}

// Knowledge Base Tab Functions
function renderLinkedKbGroups() {
	linkedKnowledgeBasesContainer.empty();
	CurrentAgentLinkedKBs.forEach(kbId => {
		const kbGroup = BusinessFullData.businessApp.knowledgeBases.find(g => g.id === kbId);
		if (kbGroup) {
			const badge = `
                <span class="badge text-bg-secondary p-2 me-2 mb-2">
                    <span>${kbGroup.general.name}</span>
                    <button type="button" class="btn-close ms-2" aria-label="Remove" data-kb-id="${kbId}"></button>
                </span>`;
			linkedKnowledgeBasesContainer.append(badge);
		}
	});
}

function updateKbGroupSelectOptions() {
	agentKbGroupSelect.empty().append('<option selected disabled>Select Group</option>');
	const availableGroups = BusinessFullData.businessApp.knowledgeBases.filter(g => !CurrentAgentLinkedKBs.includes(g.id));
	availableGroups.forEach(group => {
		agentKbGroupSelect.append(`<option value="${group.id}">${group.general.name}</option>`);
	});
}

function fillAgentKnowledgeBaseTab() {
	// Ensure data model exists
	if (!CurrentManageAgentData.knowledgeBase) {
		CurrentManageAgentData.knowledgeBase = createDefaultAgentObject().knowledgeBase;
	}
	const kbData = CurrentManageAgentData.knowledgeBase;

	// List
	CurrentAgentLinkedKBs = [...kbData.linkedGroups];
	renderLinkedKbGroups();
	updateKbGroupSelectOptions();

	// Settings
	agentKbSearchStrategySelect.val(kbData.searchStrategy.type.value).trigger('change');

	if (kbData.searchStrategy.type.value == AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.SPECIFIC_KEYWORD) {
		agentKbSpecificKeywordsTextarea.val(kbData.specificKeywords);
	}
	else if (kbData.searchStrategy.type.value == AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.LLM) {
		agentKbClassifierUseAgentLLM.prop('checked', kbData.classifier.useAgentLLM).trigger('change');
		if (!kbData.classifier.useAgentLLM && kbData.classifier.llmIntegration) {
			agentKbClassifierLLMIntegrationManager.load(kbData.classifier.llmIntegration, BusinessFullData.businessApp.integrations);
		}
    }

	if (kbData.refinement.enabled) {
		agentKbEnableQueryRefinement.prop('checked',).trigger('change');
		agentKbRefinementQueryCount.val(kbData.refinement.queryCount);
		agentKbRefinementUseAgentLLM.prop('checked', kbData.refinement.useAgentLLM).trigger('change');
		if (!kbData.refinement.useAgentLLM && kbData.refinement.llmIntegration) {
			agentKbRefinementLLMIntegrationManager.load(kbData.refinement.llmIntegration, BusinessFullData.businessApp.integrations);
		}
	}	
}

function ResetAndEmptyAgentKnowledgeBaseTab() {
	CurrentAgentLinkedKBs = [];
	renderLinkedKbGroups();
	updateKbGroupSelectOptions();

	agentKbSearchStrategySelect.val('2').trigger('change');
	agentKbSpecificKeywordsTextarea.val('');

	agentKbClassifierUseAgentLLM.prop('checked', true).trigger('change');
	if (agentKbClassifierLLMIntegrationManager) agentKbClassifierLLMIntegrationManager.reset();

	agentKbEnableQueryRefinement.prop('checked', false).trigger('change');
	agentKbRefinementQueryCount.val(3);
	agentKbRefinementUseAgentLLM.prop('checked', true).trigger('change');
	if (agentKbRefinementLLMIntegrationManager) agentKbRefinementLLMIntegrationManager.reset();
}

function AgentKnowledgeBaseStrategyIs(type) {
	return (get) => {
		const strategy = get("searchStrategy");
		if (!strategy) return false;
		const value = strategy.type !== null && typeof strategy.type === "object" ? strategy.type.value : strategy.type;
		return value == type;
	};
}

function BuildAgentKnowledgeBaseChanges() {
	const changes = {};

	changes.linkedGroups = [...CurrentAgentLinkedKBs].sort();

	changes.searchStrategy = {
		type: parseInt(agentKbSearchStrategySelect.val())
	};
	if (changes.searchStrategy.type == AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.SPECIFIC_KEYWORD) {
		changes.specificKeywords = agentKbSpecificKeywordsTextarea.val();
	}
	else if (changes.searchStrategy.type == AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.LLM) {
		changes.llmClassifier = {
			useAgentLLM: agentKbClassifierUseAgentLLM.is(':checked')
		};
	}

	changes.refinement = {
		enabled: agentKbEnableQueryRefinement.is(':checked')
	};
	if (changes.refinement.enabled) {
		changes.refinement.queryCount = parseInt(agentKbRefinementQueryCount.val());
		changes.refinement.useAgentLLM = agentKbRefinementUseAgentLLM.is(':checked');
		if (!changes.refinement.useAgentLLM) {
			changes.refinement.llmIntegration = agentKbRefinementLLMIntegrationManager.getData();
		}
	}

	return changes;
}

function CheckAgentKnowledgeBaseTabChanges(enableDisableButton = true) {
	const changes = BuildAgentKnowledgeBaseChanges();
	const hasChanges = AgentKnowledgeBaseChanges.compare(CurrentManageAgentData.knowledgeBase, changes).hasChanges;

	if (enableDisableButton) {
		confirmPublishAgentButton.prop("disabled", !hasChanges);
	}

	return { hasChanges, changes };
}

function validateAgentKnowledgeBaseTab(onlyRemove = true) {
	const errors = [];
	let isValid = true;

	// Strategy Specific Validations
	const strategy = parseInt(agentKbSearchStrategySelect.val());
	if (strategy === AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.SPECIFIC_KEYWORD) {
		if (!agentKbSpecificKeywordsTextarea.val().trim()) {
			isValid = false;
			errors.push("Knowledge Base: Trigger Keywords cannot be empty when using the 'Specific Keyword Match' strategy.");
			if (!onlyRemove) agentKbSpecificKeywordsTextarea.addClass('is-invalid');
		} else {
			agentKbSpecificKeywordsTextarea.removeClass('is-invalid');
		}
	} else if (strategy == AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.LLM) {
		if (!agentKbClassifierUseAgentLLM.is(':checked')) {
			const validation = agentKbClassifierLLMIntegrationManager.validate();
			if (!validation.isValid) {
				isValid = false;
				errors.push(...validation.errors.map(e => `KB Classifier LLM: ${e}`));
			}
		}
	}

	// Refinement Validations
	if (agentKbEnableQueryRefinement.is(':checked')) {
		const count = parseInt(agentKbRefinementQueryCount.val());
		if (isNaN(count) || count < 1) {
			isValid = false;
			errors.push("Knowledge Base: 'Number of Queries to Generate' must be between 1 and 5.");
			if (!onlyRemove) agentKbRefinementQueryCount.addClass('is-invalid');
		} else {
			agentKbRefinementQueryCount.removeClass('is-invalid');
		}

		if (!agentKbRefinementUseAgentLLM.is(':checked')) {
			const validation = agentKbRefinementLLMIntegrationManager.validate();
			if (!validation.isValid) {
				isValid = false;
				errors.push(...validation.errors.map(e => `KB Refinement LLM: ${e}`));
			}
		}
	}

	return { isValid, errors };
}

function initAgentKnowledgeBaseTabHandlers() {
	// --- List Sub-tab ---
	addAgentKbGroupButton.on("click", () => {
		const selectedId = agentKbGroupSelect.val();
		if (!selectedId) return;

		if (CurrentAgentLinkedKBs.includes(selectedId)) {
			AlertManager.createAlert({ type: 'warning', message: 'This Knowledge Base group is already linked.', timeout: 3000 });
			return;
		}

		CurrentAgentLinkedKBs.push(selectedId);
		renderLinkedKbGroups();
		updateKbGroupSelectOptions();
		CheckAgentTabHasChanges();
		validateAgentKnowledgeBaseTab(true);
	});

	linkedKnowledgeBasesContainer.on("click", ".btn-close", function () {
		const kbIdToRemove = $(this).data("kb-id");
		CurrentAgentLinkedKBs = CurrentAgentLinkedKBs.filter(id => id !== kbIdToRemove);
		renderLinkedKbGroups();
		updateKbGroupSelectOptions();
		CheckAgentTabHasChanges();
		validateAgentKnowledgeBaseTab(true);
	});

	// --- Settings Sub-tab ---
	agentKbSearchStrategySelect.on("change", function () {
		const strategy = parseInt($(this).val());
		// Hide all boxes first
		$("#agentKbStrategySettingsContainer > div").hide();

		if (strategy == AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.SPECIFIC_KEYWORD) {
			agentKbSpecificKeywordsBox.show();
		} else if (strategy == AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.KNOWLEDGEBASE_KEYWORD) {
			agentKbKbKeywordsBox.show();
		} else if (strategy == AGENT_KNOWLEDGE_BASE_STRATEGY_TYPE.LLM) {
			agentKbLlmClassifierBox.show();
		}
		CheckAgentTabHasChanges();
		validateAgentKnowledgeBaseTab(true);
	});

	agentKbSpecificKeywordsTextarea.on("input", () => {
		CheckAgentTabHasChanges();
		validateAgentKnowledgeBaseTab(true);
	});

	agentKbClassifierUseAgentLLM.on("change", function () {
		agentKbClassifierLLMIntegrationSelectBox.toggle(!$(this).is(":checked"));
		CheckAgentTabHasChanges();
		validateAgentKnowledgeBaseTab(true);
	});

	agentKbEnableQueryRefinement.on("change", function () {
		agentKbQueryRefinementOptionsBox.toggle($(this).is(":checked"));
		CheckAgentTabHasChanges();
		validateAgentKnowledgeBaseTab(true);
	});

	agentKbRefinementQueryCount.on("input", () => {
		CheckAgentTabHasChanges();
		validateAgentKnowledgeBaseTab(true);
	});

	agentKbRefinementUseAgentLLM.on("change", function () {
		agentKbRefinementLLMIntegrationSelectBox.toggle(!$(this).is(":checked"));
		CheckAgentTabHasChanges();
		validateAgentKnowledgeBaseTab(true);
	});
}

// Cache Tab Functions
function fillAgentCacheTab() {
	fillCacheGroupsList("message");
	fillCacheGroupsList("audio");
	fillCacheGroupsList("embedding");

	if (CurrentManageAgentData.cache.audioCacheSettings.autoCacheAudioResponses) {
		agentCacheSettingsAutoCacheAudioCheckbox.prop("checked", true).change();

		agentAutoCacheAudioGroupSelect.val(CurrentManageAgentData.cache.audioCacheSettings.autoCacheAudioResponseCacheGroupId).change();
		agentAutoCacheAudioExpiryInput.val(CurrentManageAgentData.cache.audioCacheSettings.autoCacheAudioResponsesDefaultExpiryHours);
	}

	if (CurrentManageAgentData.cache.embeddingsCacheSettings.autoCacheEmbeddingResponses) {
		agentCacheSettingsAutoCacheEmbeddingCheckbox.prop("checked", true).change();

        agentAutoCacheEmbeddingGroupSelect.val(CurrentManageAgentData.cache.embeddingsCacheSettings.autoCacheEmbeddingResponseCacheGroupId).change();
		agentAutoCacheEmbeddingExpiryInput.val(CurrentManageAgentData.cache.embeddingsCacheSettings.autoCacheEmbeddingResponsesDefaultExpiryHours);
	}
}

function createCacheGroupSelectElement(type, index) {
	var groups = [];
	var index = 0;
	if (type === "message") {
		groups = BusinessFullData.businessApp.cache.messageGroups;
		index = CurrentAgentCacheMessagesIndex++;
	}
	else if (type === "audio") {
		groups = BusinessFullData.businessApp.cache.audioGroups;
		index = CurrentAgentCacheAudiosIndex++;
    }
    else if (type === "embedding") {
		groups = BusinessFullData.businessApp.cache.embeddingGroups;
		index = CurrentAgentCacheEmbeddingsIndex++;
	}

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
	var container = [];
	var currentGroups = [];

	if (type === "message") {
		container = messageCacheGroupsList;
		CurrentAgentCacheMessages = structuredClone(CurrentManageAgentData.cache.messages);
		currentGroups = CurrentAgentCacheMessages;
	} else if (type === "audio") {
		container = audioCacheGroupsList;
		CurrentAgentCacheAudios = structuredClone(CurrentManageAgentData.cache.audios);
		currentGroups = CurrentAgentCacheAudios;
	} else if (type === "embedding") {
		container = embeddingsCacheGroupsList;
		CurrentAgentCacheEmbeddings = structuredClone(CurrentManageAgentData.cache.embeddings);
		currentGroups = CurrentAgentCacheEmbeddings;
	}

	// Clear existing items except alert
	container.find(".cache-group-item").remove();

	// Add current groups
	currentGroups.forEach((groupId, index) => {
		const element = $(createCacheGroupSelectElement(type, index));
		element.find("select").val(groupId);
		container.append(element);
	});
}

function BuildAgentCacheChanges() {
	const changes = {
		messages: CurrentAgentCacheMessages,
		audios: CurrentAgentCacheAudios,
		embeddings: CurrentAgentCacheEmbeddings,
		audioCacheSettings: {
			autoCacheAudioResponses: agentCacheSettingsAutoCacheAudioCheckbox.is(":checked"),
		},
		embeddingsCacheSettings: {
			autoCacheEmbeddingResponses: agentCacheSettingsAutoCacheEmbeddingCheckbox.is(":checked"),
		},
	};

	if (changes.audioCacheSettings.autoCacheAudioResponses) {
		changes.audioCacheSettings.autoCacheAudioResponseCacheGroupId = agentAutoCacheAudioGroupSelect.val();
		changes.audioCacheSettings.autoCacheAudioResponsesDefaultExpiryHours = parseInt(agentAutoCacheAudioExpiryInput.val(), 10) || 0;
	}
	if (changes.embeddingsCacheSettings.autoCacheEmbeddingResponses) {
		changes.embeddingsCacheSettings.autoCacheEmbeddingResponseCacheGroupId = agentAutoCacheEmbeddingGroupSelect.val();
		changes.embeddingsCacheSettings.autoCacheEmbeddingResponsesDefaultExpiryHours = parseInt(agentAutoCacheEmbeddingExpiryInput.val(), 10) || 0;
	}

	return changes;
}

function CheckAgentCacheTabChanges(enableDisableButton = true) {
	const changes = BuildAgentCacheChanges();
	const hasChanges = AgentCacheChanges.compare(CurrentManageAgentData.cache, changes).hasChanges;

	if (enableDisableButton) {
		confirmPublishAgentButton.prop("disabled", !hasChanges);
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

	// Validate Embeddings Cache Groups
	embeddingsCacheGroupsList.find('select[select-type^="cache-embeddings-group"]').each((index, select) => {
        const value = $(select).val();
        if (!value) {
            isValid = false;
            errors.push(`Embeddings cache group at position ${index + 1} must be selected`);
            if (!onlyRemove) {
                $(select).addClass("is-invalid");
            }
        } else {
            $(select).removeClass("is-invalid");
        }
	});

    // Validate unique selections for embeddings cache groups
    const selectedEmbeddingsGroups = new Set();
	embeddingsCacheGroupsList.find('select[select-type^="cache-embeddings-group"]').each((index, select) => {
        const value = $(select).val();
        if (value) {
            if (selectedEmbeddingsGroups.has(value)) {
                isValid = false;
                errors.push(`Duplicate embeddings cache group selection at position ${index + 1}`);
                if (!onlyRemove) {
                    $(select).addClass("is-invalid");
                }
            }
            selectedEmbeddingsGroups.add(value);
        }
    });

	// Validate Auto Cache Audio
	if (agentCacheSettingsAutoCacheAudioCheckbox.is(":checked")) {
		// Validate that a cache group is selected
		const autoCacheGroupId = agentAutoCacheAudioGroupSelect.val();
		if (!autoCacheGroupId || autoCacheGroupId.trim() === "") {
			isValid = false;
			errors.push("Auto Cache Audio Group must be selected when auto-caching is enabled.");
			if (!onlyRemove) {
				agentAutoCacheAudioGroupSelect.addClass("is-invalid");
			}
		} else {
			agentAutoCacheAudioGroupSelect.removeClass("is-invalid");
		}

		// Validate that the expiry hours is a non-negative number
		const autoCacheExpiryHours = parseInt(agentAutoCacheAudioExpiryInput.val(), 10);
		if (isNaN(autoCacheExpiryHours) || autoCacheExpiryHours < 0) {
			isValid = false;
			errors.push("Auto Cache Expiry (Hours) must be a valid, non-negative number.");
			if (!onlyRemove) {
				agentAutoCacheAudioExpiryInput.addClass("is-invalid");
			}
		} else {
			agentAutoCacheAudioExpiryInput.removeClass("is-invalid");
		}
	}

	// Validate Auto Cache Embedding
	if (agentCacheSettingsAutoCacheEmbeddingCheckbox.is(":checked")) {
        // Validate that a cache group is selected
        const autoCacheGroupId = agentAutoCacheEmbeddingGroupSelect.val();
        if (!autoCacheGroupId || autoCacheGroupId.trim() === "") {
            isValid = false;
            errors.push("Auto Cache Embedding Group must be selected when auto-caching is enabled.");
            if (!onlyRemove) {
                agentAutoCacheEmbeddingGroupSelect.addClass("is-invalid");
            }
        } else {
            agentAutoCacheEmbeddingGroupSelect.removeClass("is-invalid");
		}

        // Validate that the expiry hours is a non-negative number
        const autoCacheExpiryHours = parseInt(agentAutoCacheEmbeddingExpiryInput.val(), 10);
        if (isNaN(autoCacheExpiryHours) || autoCacheExpiryHours < 0) {
            isValid = false;
            errors.push("Auto Cache Expiry (Hours) must be a valid, non-negative number.");
            if (!onlyRemove) {
                agentAutoCacheEmbeddingExpiryInput.addClass("is-invalid");
            }
        } else {
            agentAutoCacheEmbeddingExpiryInput.removeClass("is-invalid");
        }
    }

	return {
		isValid,
		errors,
	};
}

// Settings Tab Functions
function SameAgentBackgroundAudioCommand(saved, current) {
	if (current === "custom") return false;
	if (current === null) return saved === null;
	return true;
}

function BuildAgentSettingsChanges() {
	const changes = {};

	const backgroundAudioType = agentBackgroundAudioSelect.val();
	if (backgroundAudioType === "none") {
		changes.backgroundAudioUrl = null;
	}
	if (backgroundAudioType === "custom") {
		if (
			agentBackgroundAudioUploadInput[0].files.length === 1
			||
			(agentBackgroundAudioUploadInput[0].files.length === 0 && AgentBackgroundAudioWaveSurfer == null)
		) {
			changes.backgroundAudioUrl = "custom";
		}
		if (CurrentManageAgentData.settings.backgroundAudioUrl !== null && agentBackgroundAudioUploadInput[0].files.length === 0 && AgentBackgroundAudioWaveSurfer != null) {
			changes.backgroundAudioUrl = "previous";
		}
	}
	if (backgroundAudioType !== "none") {
		changes.backgroundAudioVolume = parseInt(agentBackgroundAudioVolumeInput.val());
	}

	return changes;
}

function CheckAgentSettingsTabChanges(enableDisableButton = true) {
	const changes = BuildAgentSettingsChanges();
	const hasChanges = AgentSettingsChanges.compare(CurrentManageAgentData.settings, changes).hasChanges;

	if (enableDisableButton) {
		confirmPublishAgentButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

function validateAgentSettingsTab(onlyRemove = true) {
	const errors = [];
	let isValid = true;

	if (agentBackgroundAudioSelect.val() === "custom" && agentBackgroundAudioUploadInput[0].files.length === 0 && AgentBackgroundAudioWaveSurfer == null) {
		isValid = false;
		errors.push("Audio file for background audio is required.");

		if (!onlyRemove) {
			agentBackgroundAudioSelect.addClass("is-invalid");
		}
	} else {
		agentBackgroundAudioSelect.removeClass("is-invalid");
	}

	if (agentBackgroundAudioSelect.val() === "custom" || agentBackgroundAudioSelect.val() === "previous") {
		const backgroundAudioVolume = parseInt(agentBackgroundAudioVolumeInput.val());
		if (isNaN(backgroundAudioVolume) || backgroundAudioVolume < 0 || backgroundAudioVolume > 100) {
			validated = false;
			errors.push("Background audio volume must be a number between 0 and 100.");

			if (!onlyRemove) {
				agentBackgroundAudioVolumeInput.addClass("is-invalid");
			}
		} else {
			agentBackgroundAudioVolumeInput.removeClass("is-invalid");
		}
	}

	return {
		isValid,
		errors,
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
			timeout: 6000,
		});

		$(event.currentTarget).val("");
		return false;
	}

	return true;
}

function fillAgentSettingsTab() {
	if (CurrentManageAgentData.settings.backgroundAudioUrl) {
		agentBackgroundAudioSelect.val("custom").change();

		AgentBackgroundAudioWaveSurfer = CreateAgentBackgroundAudioWavesurfer("#agent-background-audio-waveform");
		AgentBackgroundAudioWaveSurfer.load(CurrentManageAgentData.settings.backgroundAudioUrl);
		agentBackgroundAudioVolumeInput.val(CurrentManageAgentData.settings.backgroundAudioVolume);

		agentBackgroundAudioInputBox.find(".no-audio-notice").addClass("d-none");
		agentBackgroundAudioInputBox.find(".recording-container-waveform").removeClass("d-none");
		agentBackgroundAudioInputBox.find(".audio-controller").removeClass("d-none");
	}
}

/** INIT **/
function initAgentTab() {
	$(document).ready(() => {
		// Init
		FillAgentsListTab();

		manageAgentsLanguageDropdown = new MultiLanguageDropdown("agentsManagerMultiLanguageContainer", BusinessFullLanguagesData);

		const sharedIntegrationConfigurationManagerOptions = {
			allowMultiple: true,
			isLanguageBound: true,
			languageDropdown: manageAgentsLanguageDropdown,
			allIntegrations: BusinessFullData.businessApp.integrations,
			modalSelector: '#integrationConfigurationModal',
			onSaveSuccessful: () => {
				CheckAgentTabHasChanges();
				validateAgentMultiLanguageElements();
			},
			onIntegrationChange: () => {
				CheckAgentTabHasChanges();
				validateAgentMultiLanguageElements();
			},
		};

		agentSTTIntegrationManager = new IntegrationConfigurationManager('#sttIntegrationsList', {
			...sharedIntegrationConfigurationManagerOptions,
			integrationType: 'STT',
			providersData: BusinessSTTProvidersForIntegrations,
		});

		agentLLMIntegrationManager = new IntegrationConfigurationManager('#llmIntegrationsList', {
			...sharedIntegrationConfigurationManagerOptions,
			integrationType: 'LLM',
			providersData: BusinessLLMProvidersForIntegrations,
		});

		agentTTSIntegrationManager = new IntegrationConfigurationManager('#ttsIntegrationsList', {
			...sharedIntegrationConfigurationManagerOptions,
			integrationType: 'TTS',
			providersData: BusinessTTSProvidersForIntegrations,
		});

		agentTurnEndLLMIntegrationManager = new IntegrationConfigurationManager('#agentTurnEndViaLLMIntegrationContainer', {
			integrationType: 'LLM',
			allowMultiple: false,
			isLanguageBound: false, // Turn-end logic is universal, not per-language
			allIntegrations: BusinessFullData.businessApp.integrations,
			providersData: BusinessLLMProvidersForIntegrations,
			modalSelector: '#integrationConfigurationModal',
			onSaveSuccessful: () => { CheckAgentTabHasChanges(); validateAgentInterruptionsTab(true); },
			onIntegrationChange: () => { CheckAgentTabHasChanges(); validateAgentInterruptionsTab(true); },
		});

		agentInterruptionVerifyLLMIntegrationManager = new IntegrationConfigurationManager('#agentInterruptionVerifyLLMIntegrationContainer', {
			integrationType: 'LLM',
			allowMultiple: false,
			isLanguageBound: false, // Verification logic is also universal
			allIntegrations: BusinessFullData.businessApp.integrations,
			providersData: BusinessLLMProvidersForIntegrations,
			modalSelector: '#integrationConfigurationModal',
			onSaveSuccessful: () => { CheckAgentTabHasChanges(); validateAgentInterruptionsTab(true); },
			onIntegrationChange: () => { CheckAgentTabHasChanges(); validateAgentInterruptionsTab(true); },
		});

		agentKbClassifierLLMIntegrationManager = new IntegrationConfigurationManager('#agentKbClassifierLLMIntegrationContainer', {
			integrationType: 'LLM',
			allowMultiple: false,
			isLanguageBound: false,
			allIntegrations: BusinessFullData.businessApp.integrations,
			providersData: BusinessLLMProvidersForIntegrations,
			modalSelector: '#integrationConfigurationModal',
			onSaveSuccessful: () => { CheckAgentTabHasChanges(); validateAgentKnowledgeBaseTab(true); },
			onIntegrationChange: () => { CheckAgentTabHasChanges(); validateAgentKnowledgeBaseTab(true); },
		});

		agentKbRefinementLLMIntegrationManager = new IntegrationConfigurationManager('#agentKbRefinementLLMIntegrationContainer', {
			integrationType: 'LLM',
			allowMultiple: false,
			isLanguageBound: false,
			allIntegrations: BusinessFullData.businessApp.integrations,
			providersData: BusinessLLMProvidersForIntegrations,
			modalSelector: '#integrationConfigurationModal',
			onSaveSuccessful: () => { CheckAgentTabHasChanges(); validateAgentKnowledgeBaseTab(true); },
			onIntegrationChange: () => { CheckAgentTabHasChanges(); validateAgentKnowledgeBaseTab(true); },
		});

		/** Event Handlers **/
		addNewAgentButton.on("click", (event) => {
			event.preventDefault();

			currentAgentName.text("New Agent");
			CurrentManageAgentData = createDefaultAgentObject();

			ResetAndEmptyAgentsManageTab();
			showAgentManagerTab();
			FillAgentsManagerTab();

			ManageAgentType = "new";
		});

		switchBackToAgentsTab.on("click", async (event) => {
			event.preventDefault();

			if (ManageAgentType !== null) {
				const canLeaveResult = await canLeaveAgentTab(" Are you sure you want to discard these changes and leave the agents manage tab?");
				if (!canLeaveResult) {
					return false;
				}
			}

			ManageAgentType = null;

			showAgentListTab();
		});

		agentsCardListContainer.on("click", ".agent-card", (event) => {
			event.stopPropagation();
			event.preventDefault();

			// check if target was button or its icon
			if ($(event.target).closest(".dropdown").length != 0) {
				return;
			}

			const agentId = $(event.currentTarget).attr("data-item-id");
			CurrentManageAgentData = BusinessFullData.businessApp.agents.find((a) => a.id === agentId);

			currentAgentName.text(CurrentManageAgentData.general.name[BusinessDefaultLanguage]);

			ResetAndEmptyAgentsManageTab();
			FillAgentsManagerTab();

			showAgentManagerTab();

			ManageAgentType = "edit";

			validateAgentMultiLanguageElements();
		});

		agentsCardListContainer.on("click", ".agent-card span[button-type='delete-agent']", async (event) => {
			event.preventDefault();

			const button = $(event.currentTarget);
			const agentId = button.attr("data-item-id");
			const agentIndex = BusinessFullData.businessApp.agents.findIndex(n => n.id === agentId);
			if (agentIndex === -1) return;
			const agentData = BusinessFullData.businessApp.agents[agentIndex];
			if (!agentData) return;
			const agentCard = agentsCardListContainer.find(`.agent-card[data-item-id="${agentId}"]`);

			if (IsDeletingAgentTab) {
				AlertManager.createAlert({
					type: "warning",
					message: `A delete operation for agent is already in progress. Please try again once the operation is complete.`,
					timeout: 6000,
				});
				return;
			}

			const confirmDialog = new BootstrapConfirmDialog({
				title: `Delete "${agentData.general.name[BusinessDefaultLanguage]}" Agent`,
				message: `Are you sure you want to delete this agent?<br><br><b>Note:</b> You must remove any references to this agent (script transfer to agent, inbound route, telephony/web campaigns) and wait or cancel any ongoing call queues or conversations.`,
				confirmText: "Delete",
				confirmButtonClass: "btn-danger",
				modalClass: "modal-lg"
			});

			if (await confirmDialog.show()) {
				showHideButtonSpinnerWithDisableEnable(button, true);
				IsDeletingAgentTab = true;
				agentCard.addClass("disabled");

				DeleteBusinessAgent(
					agentId,
					() => {
						
						BusinessFullData.businessApp.agents.splice(agentIndex, 1);

						agentCard.parent().remove();

						if (BusinessFullData.businessApp.agents.length === 0) {
                            agentsCardListContainer.append('<div class="col-12 none-agents-list-notice"><h6 class="text-center mt-5">No agents added yet...</h6></div>');
                        }

						AlertManager.createAlert({
							type: "success",
							message: `Agent "${agentData.general.name[BusinessDefaultLanguage]}" deleted successfully.`,
							timeout: 6000,
						});
					},
					(errorResult) => {
						agentCard.removeClass("disabled");

						var resultMessage = "Check console logs for more details.";
						if (errorResult && errorResult.message) resultMessage = errorResult.message;

						AlertManager.createAlert({
							type: "danger",
							message: "Error occured while deleting business agent.",
							resultMessage: resultMessage,
							timeout: 6000,
						});

						console.log("Error occured while deleting business agent: ", errorResult);
					}
				).always(() => {
					showHideButtonSpinnerWithDisableEnable(button, false);
					IsDeletingAgentTab = false;
				});
			}
		});

		$("#nav-bar").on("tabChange", async (event) => {
			const activeTab = event.detail.from;
			if (activeTab !== "agents-tab") return;

			if (ManageAgentType == null) return;

			const canLeaveResult = await canLeaveAgentTab(" Are you sure you want to discard these changes and leave the agents tab?");

			if (canLeaveResult) {
				if (ManageAgentType != null) {
					ManageAgentType = null;
					switchBackToAgentsTab.click();
				}
			} else {
				event.preventDefault();
			}
		});

		// General Tab Handlers
		function initAgentGeneralTabHandlers() {
			// Name input changes
			$("#editAgentIdentifierInput").on("input", (event) => {
				const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
				CurrentAgentGeneralNameMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
				validateAgentMultiLanguageElements();
				validateAgentGeneralTab(true);
				CheckAgentTabHasChanges();
			});

			// Description input changes
			$("#editAgentDescriptionInput").on("input", (event) => {
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

		// Context Tab Handlers
		$("#agentEditContextEnableBranding, #agentEditContextEnableBranches, #agentEditContextEnableServices, #agentEditContextEnableProducts").on("change", () => {
			CheckAgentTabHasChanges();
		});

		// Personality Tab Handlers
		function initAgentPersonalityTabHandlers() {
			// Name input changes
			$("#editAgentPersonalityNameInput").on("input", (event) => {
				const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
				CurrentAgentPersonalityNameMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
				validateAgentMultiLanguageElements();
				validateAgentPersonalityTab(true);
				CheckAgentTabHasChanges();
			});

			// Role input changes
			$("#editAgentPersonalityRoleInput").on("input", (event) => {
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
							<textarea class="form-control"></textarea>
							<button class="btn btn-danger" button-type="editAgentPersonalityValueRemove">
								<i class='fa-regular fa-trash'></i>
							</button>
						</div>
					`);

					// Update data
					currentData[currentSelectedLanguage.id] = Array.from(container.find("textarea")).map((input) => $(input).val().trim());
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
					currentData[currentSelectedLanguage.id] = Array.from(container.find("textarea")).map((input) => $(input).val().trim());
					validateAgentMultiLanguageElements();
					validateAgentPersonalityTab(true);
					CheckAgentTabHasChanges();
				});

				// Value changes
				container.on("input", "textarea", () => {
					const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
					const currentData =
						listType === "capabilities"
							? CurrentAgentPersonalityCapabilitiesMultiLangData
							: listType === "ethics"
								? CurrentAgentPersonalityEthicsMultiLangData
								: CurrentAgentPersonalityToneMultiLangData;

					currentData[currentSelectedLanguage.id] = Array.from(container.find("textarea")).map((input) => $(input).val().trim());
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
								<textarea class="form-control">${value}</textarea>
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

		// Utterances Tab Handlers
		function initAgentUtterancesTabHandlers() {
			// Opening Type changes
			$("#editAgentGreetingStartTypeInput").on("change", () => {
				CheckAgentTabHasChanges();
				validateAgentUtterancesTab(true);
			});

			// Greeting Message changes
			$("#editAgentPersonalityGreetingInput").on("input", (event) => {
				const currentSelectedLanguage = manageAgentsLanguageDropdown.getSelectedLanguage();
				CurrentAgentUtterancesGreetingMessageMultiLangData[currentSelectedLanguage.id] = $(event.currentTarget).val();
				validateAgentMultiLanguageElements();
				validateAgentUtterancesTab(true);
				CheckAgentTabHasChanges();
			});

			// Language change handler
			manageAgentsLanguageDropdown.onLanguageChange((language) => {
				// Update greeting message
				$("#editAgentPersonalityGreetingInput").val(CurrentAgentUtterancesGreetingMessageMultiLangData[language.id] || "");

				validateAgentMultiLanguageElements();
				validateAgentUtterancesTab(true);
			});
		}
		initAgentUtterancesTabHandlers();

		// Interruptions Tab Handlers
		function initAgentInterruptionsTabHandlers() {
			// --- Turn-end Detection ---
			editAgentTurnEndTypeSelect.on("change", (event) => {
				const selectedValue = parseInt($(event.currentTarget).val());
				agentTurnEndViaVADBox.hide();
				agentTurnEndViaAIBox.hide();

				if (selectedValue == AGENT_INTERRUPTION_TURN_END_TYPE.VAD) { // Turn End via VAD
					agentTurnEndViaVADBox.show();
				} else if (selectedValue == AGENT_INTERRUPTION_TURN_END_TYPE.AI) { // Turn End via AI
					agentTurnEndViaAIBox.show();
				}

				CheckAgentTabHasChanges();
				validateAgentInterruptionsTab(true);
			});

			editAgentTurnEndViaAIUseAgentLLM.on("change", (event) => {
				const isChecked = $(event.currentTarget).is(":checked");
				agentTurnEndViaLLMIntegrationSelectBox.toggle(!isChecked);
				CheckAgentTabHasChanges();
				validateAgentInterruptionsTab(true);
			});

			editAgentTurnEndViaVADAudioActivityDuration.on("input", () => {
				CheckAgentTabHasChanges();
				validateAgentInterruptionsTab(true);
			});

			// Turn by Turn
			editAgentTurnByTurnMode.on("change", () => {
				var isChecked = editAgentTurnByTurnMode.is(":checked");

				if (isChecked) {
					editAgentTurnByTurnIncludeInterruptedSpeech.prop("disabled", false);
					agentInterruptionPauseTypeSelect.children().first().prop("selected", true).change();
					agentInterruptionPauseTypeSelect.change().prop("disabled", true);
					enableAgentInterruptionVerification.prop("checked", false).change().prop("disabled", true);
				}
				else {
					editAgentTurnByTurnIncludeInterruptedSpeech.prop("checked", false).change().prop("disabled", true);
					agentInterruptionPauseTypeSelect.prop("disabled", false);
                    enableAgentInterruptionVerification.prop("disabled", false);
				}

                CheckAgentTabHasChanges();
                validateAgentInterruptionsTab(true);
			});

			editAgentTurnByTurnIncludeInterruptedSpeech.on("change", () => {
                CheckAgentTabHasChanges();
				validateAgentInterruptionsTab(true);
            });

			// --- Agent Pause Trigger ---
			agentInterruptionPauseTypeSelect.on("change", (event) => {
				const selectedValue = parseInt($(event.currentTarget).val());
				agentInterruptionPauseViaVADBox.hide();
				agentInterruptionPauseViaWordsBox.hide();

				if (selectedValue == AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE.VAD) {
					agentInterruptionPauseViaVADBox.show();
				} else if (selectedValue == AGENT_INTERRUPTION_PAUSE_TRIGGER_TYPE.STT) {
					agentInterruptionPauseViaWordsBox.show();
				}

				CheckAgentTabHasChanges();
				validateAgentInterruptionsTab(true);
			});

			agentInterruptionPauseVADDuration.on("input", () => {
				CheckAgentTabHasChanges();
				validateAgentInterruptionsTab(true);
			});

			agentInterruptionPauseWordCount.on("input", () => {
				CheckAgentTabHasChanges();
				validateAgentInterruptionsTab(true);
			});

			// --- Interruption Verification ---
			enableAgentInterruptionVerification.on("change", (event) => {
				const isChecked = $(event.currentTarget).is(":checked");
				agentInterruptionVerificationContainer.toggle(isChecked);
				CheckAgentTabHasChanges();
				validateAgentInterruptionsTab(true);
			});

			agentInterruptionVerifyAIUseAgentLLM.on("change", (event) => {
				const isChecked = $(event.currentTarget).is(":checked");
				agentInterruptionVerifyLLMIntegrationSelectBox.toggle(!isChecked);
				CheckAgentTabHasChanges();
				validateAgentInterruptionsTab(true);
			});
		}
		initAgentInterruptionsTabHandlers();

		// Knowledge Base Tab Handlers
		initAgentKnowledgeBaseTabHandlers();

		// Cache Tab Handlers
		function initAgentCacheTabHandlers() {
			// Message Cache
			addMessageCacheGroupButton.on("click", (event) => {
				event.preventDefault();
				messageCacheGroupsList.append(createCacheGroupSelectElement("message"));

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
						timeout: 6000,
					});
					currentElement.val("");
					return;
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
				const parent = $(event.currentTarget).closest(".cache-group-item");
                const index = parent.data("index");
                CurrentAgentCacheMessages.splice(index, 1);

				parent.remove();

				validateAgentCacheTab(true);
				CheckAgentTabHasChanges();
			});

			// Audio Cache
			addAudioCacheGroupButton.on("click", (event) => {
				event.preventDefault();
				audioCacheGroupsList.append(createCacheGroupSelectElement("audio"));

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
						timeout: 6000,
					});
					currentElement.val("");
                    return;
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
				const parent = $(event.currentTarget).closest(".cache-group-item");
				const index = parent.data("index");
				CurrentAgentCacheAudios.splice(index, 1);

				parent.remove();

				validateAgentCacheTab(true);
				CheckAgentTabHasChanges();
			});

			// Embeddings
			addEmbeddingCacheGroupButton.on("click", (event) => {
				event.preventDefault();
				embeddingsCacheGroupsList.append(createCacheGroupSelectElement("embedding"));

                CheckAgentTabHasChanges();
			});

			embeddingsCacheGroupsList.on("change", 'select[select-type^="cache-embedding-group"]', (event) => {
				const currentElement = $(event.currentTarget);
				const currentValue = currentElement.val();

                // check if select has this value
				const allSelectElements = embeddingsCacheGroupsList.find(`select[select-type="cache-embedding-group"]`);
				const anyHasSelectedValue = allSelectElements.filter((index, select) => $(select).val() === currentValue).length > 1;

				if (anyHasSelectedValue) {
                    AlertManager.createAlert({
                        type: "warning",
                        message: "Embedding cache group has already been selected.",
                        timeout: 6000,
                    });
					currentElement.val("");
					return;
				}

				const index = currentElement.closest(".cache-group-item").data("index");
				if (currentValue) {
					CurrentAgentCacheEmbeddings[index] = currentValue;
				} else {
                    CurrentAgentCacheEmbeddings.splice(index, 1);
				}

				validateAgentCacheTab(true);
				CheckAgentTabHasChanges();
			});

			embeddingsCacheGroupsList.on("click", '[button-type="remove-cache-group"]', (event) => {
				const parent = $(event.currentTarget).closest(".cache-group-item");
				const index = parent.data("index");
				CurrentAgentCacheEmbeddings.splice(index, 1);

				parent.remove();

                validateAgentCacheTab(true);
                CheckAgentTabHasChanges();
			});

			// Auto Cache Audio Settings
			agentCacheSettingsAutoCacheAudioCheckbox.on("change", (e) => {
				var isChecked = $(e.currentTarget).is(":checked");

				agentCacheSettingsAutoCacheAudioBox.toggleClass("d-none", !isChecked);

				validateAgentCacheTab(true);
				CheckAgentTabHasChanges();
			});

			agentAutoCacheAudioGroupSelect.on("change", (e) => {
				validateAgentCacheTab(true);
				CheckAgentTabHasChanges();
			});

			agentAutoCacheAudioExpiryInput.on("change", (e) => {
                validateAgentCacheTab(true);
                CheckAgentTabHasChanges();
			});

			// Auto Cache Embedding Settings
			agentCacheSettingsAutoCacheEmbeddingCheckbox.on("change", (e) => {
				var isChecked = $(e.currentTarget).is(":checked");

				agentCacheSettingsAutoCacheEmbeddingBox.toggleClass("d-none", !isChecked);

                validateAgentCacheTab(true);
                CheckAgentTabHasChanges();
			});

			agentAutoCacheEmbeddingGroupSelect.on("change", (e) => {
                validateAgentCacheTab(true);
                CheckAgentTabHasChanges();
			});

			agentAutoCacheEmbeddingExpiryInput.on("change", (e) => {
                validateAgentCacheTab(true);
				CheckAgentTabHasChanges();
            });
		}
		initAgentCacheTabHandlers();

		// Settings Tab Handlers
		function initAgentSettingsTabHandlers() {
			agentBackgroundAudioVolumeInput.on("input", () => {
				CheckAgentTabHasChanges();
			});

			editAgentBackgroundAudioSelect.on("change", (event) => {
				const selectedValue = $(event.currentTarget).val();

				if (selectedValue === "none") {
					if (AgentBackgroundAudioWaveSurfer !== null) {
						AgentBackgroundAudioWaveSurfer.destroy();
						AgentBackgroundAudioWaveSurfer = null;
					}

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

				validateAgentSettingsTab(true);
				CheckAgentTabHasChanges();
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

						if (AgentBackgroundAudioWaveSurfer !== null) {
                            AgentBackgroundAudioWaveSurfer.destroy();
						}
						AgentBackgroundAudioWaveSurfer = CreateAgentBackgroundAudioWavesurfer("#agent-background-audio-waveform");
						AgentBackgroundAudioWaveSurfer.loadBlob(blob);

						agentBackgroundAudioInputBox.find(".no-audio-notice").addClass("d-none");
						agentBackgroundAudioInputBox.find(".recording-container-waveform").removeClass("d-none");
						agentBackgroundAudioInputBox.find(".audio-controller").removeClass("d-none");

						CheckAgentTabHasChanges();
					};

					reader.onerror = (evt) => {
						AlertManager.createAlert({
							type: "error",
							message: "Error reading audio file for agenst background audio upload.",
							timeout: 6000,
						});
					};

					// Read File as an ArrayBuffer
					reader.readAsArrayBuffer(file);
				}
			});
		}
		initAgentSettingsTabHandlers();

		// Handle language changes
		manageAgentsLanguageDropdown.onLanguageChange(() => {
			CheckAgentTabHasChanges();
		});

		// Saving
		confirmPublishAgentButton.on("click", (event) => {
			event.preventDefault();

			const validationResult = ValidateAgentTab(false);
			if (!validationResult.isValid) {
				AlertManager.createAlert({
					type: "danger",
					message: `Validation failed:<br><br>${validationResult.errors.join("<br>")}`,
					timeout: 6000,
				});
				return;
			}

			const changes = CheckAgentTabHasChanges(false);
			if (!changes.hasChanges) {
				return;
			}

			IsSavingAgentTab = true;
			confirmPublishAgentButton.prop("disabled", true);
			confirmPublishAgentButtonSpinner.removeClass("d-none");

			const formData = new FormData();
			formData.append("postType", ManageAgentType);
			if (ManageAgentType === "edit") {
				formData.append("agentId", CurrentManageAgentData.id);
			}
			formData.append("changes", JSON.stringify(changes.changes));

			if (changes.changes.settings.backgroundAudioUrl === "custom") {
				formData.append("backgroundAudio", agentBackgroundAudioUploadInput[0].files[0]);
			}

			SaveBusinessAgent(
				formData,
				(saveResponse) => {
					CurrentManageAgentData = saveResponse.data;

					currentAgentName.text(CurrentManageAgentData.general.name[BusinessDefaultLanguage]);

					if (ManageAgentType === "edit") {
						const exisitingDataIndex = BusinessFullData.businessApp.agents.findIndex((agent) => agent.id === CurrentManageAgentData.id);
						BusinessFullData.businessApp.agents[exisitingDataIndex] = CurrentManageAgentData;

						const agentCard = agentsCardListContainer.find(`.agent-card[data-item-id="${CurrentManageAgentData.id}"]`);

						agentCard.find(".iqra-card-visual span").text(CurrentManageAgentData.general.emoji);
						agentCard.find(".iqra-card-title").text(CurrentManageAgentData.general.name[BusinessDefaultLanguage]);
						agentCard.find(".iqra-card-description span").text(CurrentManageAgentData.general.description[BusinessDefaultLanguage]);
					} else if (ManageAgentType === "new") {
						BusinessFullData.businessApp.agents.push(CurrentManageAgentData);

						const noneAgentNotice = agentsCardListContainer.find(".none-agents-list-notice");
						if (noneAgentNotice.length > 0) {
							noneAgentNotice.remove();
						}

						agentsCardListContainer.prepend($(CreateAgentsCardElement(CurrentManageAgentData)));
					}

					if (agentBackgroundAudioUploadInput[0].files.length > 0) {
						agentBackgroundAudioUploadInput.val("");
					}

					confirmPublishAgentButton.prop("disabled", true);
					confirmPublishAgentButtonSpinner.addClass("d-none");

					IsSavingAgentTab = false;

					AlertManager.createAlert({
						type: "success",
						message: `Business agent ${ManageAgentType === "new" ? "added" : "updated"} successfully.`,
						timeout: 6000,
					});

					ManageAgentType = "edit";
				},
				(errorResult, isUnsuccessful) => {
					var resultMessage = "Check console logs for more details.";
					if (errorResult && errorResult.message) resultMessage = errorResult.message;

					AlertManager.createAlert({
						type: "danger",
						message: "Error occured while saving business agent data.",
						resultMessage: resultMessage,
						timeout: 6000,
					});

					console.log("Error occured while saving business agent data: ", errorResult);

					confirmPublishAgentButton.prop("disabled", false);
					confirmPublishAgentButtonSpinner.addClass("d-none");

					IsSavingAgentTab = false;
				},
			);
		});
	});
}
