//** Constants **/
const SCRIPT_GRAPH_PLUGINS = {
	Minimap: X6.MiniMap,
	Keyboard: X6.Keyboard,
	Clipboard: X6.Clipboard,
	History: X6.History,
	Snapline: X6.Snapline,
	Selection: X6.Selection,
	Dnd: X6.Dnd,
};

const SCRIPT_NODE_TYPES = {
	START: 1,
	USER_QUERY: 2,
	AI_RESPONSE: 3,
	SYSTEM_TOOL: 4,
	CUSTOM_TOOL: 5,
	FLOW_APP: 6
};

const SCRIPT_SYSTEM_TOOLS = {
	END_CALL: 1,
	CHANGE_LANGUAGE: 2,
	GET_DTMF_INPUT: 3,
	PRESS_DTMF: 4,
	TRANSFER_TO_AGENT: 5,
	TRANSFER_TO_HUMAN: 6,
	ADD_SCRIPT_TO_CONTEXT: 7,
	SEND_SMS: 8,
	GOTONODE: 9,
	RETRIEVE_KNOWLEDGEBASE: 10
};

const SCRIPT_END_CALL_SYSTEM_TOOL_TYPE = {
	IMMEDIATE: 1,
	WITH_MESSAGE: 2,
};

const SCRIPT_NODE_WIDTH = 520; // todo make dynamic
const SCRIPT_NODE_MIN_HEIGHT = 185;

const NODE_PRESETS = {
	// --- Conversation ---
	'user-query': {
		shape: SCRIPT_NODE_TYPES.USER_QUERY,
		defaultPorts: [{ group: 'input' }, { group: 'output' }]
	},
	'ai-response': {
		shape: SCRIPT_NODE_TYPES.AI_RESPONSE,
		defaultPorts: [{ group: 'input' }, { group: 'output' }]
	},

	// --- Logic ---
	'go-to-node': {
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		toolType: SCRIPT_SYSTEM_TOOLS.GOTONODE,
		config: { goToNodeId: null },
		defaultPorts: [{ group: 'input' }] // No output
	},
	'add-script-context': {
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		toolType: SCRIPT_SYSTEM_TOOLS.ADD_SCRIPT_TO_CONTEXT,
		config: { scriptId: null },
		defaultPorts: [{ group: 'input' }, { group: 'output' }]
	},

	// --- Telephony ---
	'end-call': {
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		toolType: SCRIPT_SYSTEM_TOOLS.END_CALL,
		config: { type: SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.IMMEDIATE },
		defaultPorts: [{ group: 'input' }] // No output
	},
	'transfer-agent': {
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		toolType: SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_AGENT,
		config: { agentId: null, transferContext: false, summarizeContext: false },
		defaultPorts: [{ group: 'input' }] // No output
	},
	'transfer-human': {
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		toolType: SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_HUMAN,
		config: { phoneNumber: "" },
		defaultPorts: [{ group: 'input' }] // No output
	},
	'send-sms': {
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		toolType: SCRIPT_SYSTEM_TOOLS.SEND_SMS,
		config: { phoneNumberId: null, messages: {} },
		// Ports added dynamically, but we add defaults for ghosting
		defaultPorts: [{ group: 'input' }, { group: 'output', id: 'success' }, { group: 'output', id: 'error' }]
	},
	'get-dtmf': {
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		toolType: SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT,
		config: { timeout: 5000, requireStartAsterisk: false, requireEndHash: false, maxLength: 1, encryptInput: false, outcomes: [] },
		defaultPorts: [{ group: 'input' }, { group: 'output', id: 'timeout' }]
	},
	'press-dtmf': {
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		toolType: SCRIPT_SYSTEM_TOOLS.PRESS_DTMF,
		config: { digits: "" },
		defaultPorts: [{ group: 'input' }, { group: 'output' }]
	},

	// --- Integrations ---
	'retrieve-kb': {
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		toolType: SCRIPT_SYSTEM_TOOLS.RETRIEVE_KNOWLEDGEBASE,
		config: { responseBeforeExecution: {} },
		defaultPorts: [{ group: 'input' }, { group: 'output' }]
	},
	'custom-tool': {
		shape: SCRIPT_NODE_TYPES.CUSTOM_TOOL,
		data: { toolId: null, config: {} },
		defaultPorts: [{ group: 'input' }, { group: 'output', id: 'outcome-default' }]
	}
};

/** Dynamic Variables **/
let scriptsManagerLanguageDropdown = null;

// Script
let ManageCurrentScriptData = null;

let ManageCurrentScriptType = null; // new or edit

let CurrentScriptGraph = null;
let CurrentScriptGraphHistory = null;
let CurrentScriptGraphSelection = null;
let CurrentScriptGraphDnd = null;

let scriptDMTFNextOutcomeId = null;

let CurrentCanvasConfigCell = null;
let nodeConfigOffcanvas = null;

let CurrentScriptNameMultiLangData = {};
let CurrentScriptDescriptionMultiLangData = {};

let CurrentScriptVariablesData = [];
let NewVariableDescriptionMultiLangData = {};

let CheckScriptMultiLangInterval = null;

let variablesOffcanvas = null;

let IsSavingScriptTab = false;
let IsDeletingScriptTab = false;

/** Element Variables **/
const scriptTab = $("#scripts-tab");
const scriptsManagerHeader = scriptTab.find("#scripts-manager-header");
const addNewScriptButton = scriptTab.find("#addNewScriptButton");

// Script - List Tab
const scriptsListTab = scriptTab.find("#scriptsListTab");
const scriptsCardListContainer = scriptsListTab.find("#scriptsCardListContainer");

// Script - Manager Tab
const switchBackToScriptsListTab = scriptTab.find("#switchBackToScriptsListTab");
const currentScriptName = scriptTab.find("#currentScriptName");

const scriptsManagerTab = scriptTab.find("#scriptsManagerTab");

const saveScriptButton = scriptsManagerHeader.find("#saveScriptButton");
const saveScriptButtonSpinner = scriptsManagerHeader.find(".save-button-spinner");

const inputScriptName = scriptsManagerTab.find("#inputScriptName");
const inputScriptDescription = scriptsManagerTab.find("#inputScriptDescription");

const variableOffcanvasElement = scriptsManagerTab.find("#variablesOffcanvas");
const variableOffcanvasDescriptionInput = variableOffcanvasElement.find("#newVarDescription");
const variableOffcanvasDefaultInput = variableOffcanvasElement.find("#newVarDefault");
const variableOffcanvasEditableInput = variableOffcanvasElement.find("#newVarEditable");

/** API FUNCTIONS **/
function SaveBusinessScript(formData, onSuccess, onError) {
	return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/scripts/save`,
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
function DeleteBusinessScript(scriptId, onSuccess, onError) {
    return $.ajax({
		url: `/app/user/business/${CurrentBusinessId}/scripts/${scriptId}/delete`,
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

// Scripts Tab Functions
function showScriptManagerTab() {
	scriptsListTab.removeClass("show");
	setTimeout(() => {
		scriptsListTab.addClass("d-none");

		scriptsManagerTab.removeClass("d-none");
		scriptsManagerHeader.removeClass("d-none");
		setTimeout(() => {
			scriptsManagerTab.addClass("show");
			scriptsManagerHeader.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function showScriptListTab() {
	scriptsManagerTab.removeClass("show");
	scriptsManagerHeader.removeClass("show");
	setTimeout(() => {
		scriptsManagerTab.addClass("d-none");
		scriptsManagerHeader.addClass("d-none");

		scriptsListTab.removeClass("d-none");
		setTimeout(() => {
			scriptsListTab.addClass("show");

			setDynamicBodyHeight();
		}, 10);
	}, 300);
}

function ResetAndEmptyScriptsManageTab() {
	if (CurrentScriptGraph) {
		CurrentScriptGraph.dispose();
		CurrentScriptGraph = null;
		CurrentScriptGraphHistory.dispose();
		CurrentScriptGraphHistory = null;
		CurrentScriptGraphSelection.dispose();
		CurrentScriptGraphSelection = null;
		CurrentScriptGraphDnd.dispose();
        CurrentScriptGraphDnd = null;
	}

	CurrentScriptNameMultiLangData = {};
	CurrentScriptDescriptionMultiLangData = {};

	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentScriptNameMultiLangData[language] = "";
		CurrentScriptDescriptionMultiLangData[language] = "";
	});

	scriptsManagerTab.find("input, textarea").val("");

	// Variables Reset
	CurrentScriptVariablesData = [];
	NewVariableDescriptionMultiLangData = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		NewVariableDescriptionMultiLangData[language] = "";
	});

	// Clear Variable Inputs
	$("#newVarKey, #newVarDefault, #newVarDescription").val("");
	$("#newVarType").val("1");
	$("#newVarVisible").prop("checked", true);
	$("#newVarEditable").prop("checked", false);

	saveScriptButton.prop("disabled", true);
	$("#scripts-manager-general-tab").click();
}

async function canLeaveScriptsManagerTab(leaveMessage = "") {
	if (IsSavingScriptTab) {
		AlertManager.createAlert({
			type: "warning",
			message: "Script is currently being saved. Please wait for the save to finish.",
			timeout: 6000,
		});
		return false;
	}

	const changes = checkScriptTabHasChanges(false, false);
	if (changes.hasChanges) {
		const confirmDialog = new BootstrapConfirmDialog({
			title: "Unsaved Changes Pending",
			message: `You have unsaved changes in the script.${leaveMessage}`,
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

function createDefaultScriptObject() {
	const data = {
		id: "",
		general: {
			name: {},
			description: {},
		},
		nodes: [],
		edges: [],
	};

	BusinessFullData.businessData.languages.forEach((language) => {
		data.general.name[language.id] = "";
		data.general.description[language.id] = "";
	});

	return data;
}

function validateScriptMultilanguageElements(onlyRemoveInvalid = true) {
	if (ManageCurrentScriptType === null) return;

	// General Tab
	const areLanguagesIncompleteInGeneralTab = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		// General Tab
		const scriptName = CurrentScriptNameMultiLangData[language];
		const scriptNameIsIncomplete = !scriptName || scriptName === "" || scriptName.trim() === "";

		const scriptDescription = CurrentScriptDescriptionMultiLangData[language];
		const scriptDescriptionIsIncomplete = !scriptDescription || scriptDescription === "" || scriptDescription.trim() === "";

		areLanguagesIncompleteInGeneralTab[language] = scriptNameIsIncomplete || scriptDescriptionIsIncomplete;
	});

	// Conversation Tab
	const areLanguagesIncompleteInConversationTab = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		areLanguagesIncompleteInConversationTab[language] = false;
	});

	const currentNodesArray = CurrentScriptGraph.toJSON();
	for (let i = 0; i < currentNodesArray.cells.length; i++) {
		const node = currentNodesArray.cells[i];

		if (node.shape === "edge" || node.shape === SCRIPT_NODE_TYPES.START) continue;

		// Check User Query Node
		if (node.shape === SCRIPT_NODE_TYPES.USER_QUERY) {
			const userQueryData = node.data.query;

			var anyLanguageMissing = false;
			BusinessFullData.businessData.languages.forEach((language) => {
				const currentLanguageQuery = userQueryData[language];

				if (!currentLanguageQuery || currentLanguageQuery === "" || currentLanguageQuery.trim() === "") {
					areLanguagesIncompleteInConversationTab[language] = true;
					anyLanguageMissing = true;
				}
			});
			if (anyLanguageMissing) {
				if (!onlyRemoveInvalid) {
					$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).addClass('invalid-multilang');
				}
			}
			else {
				$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).removeClass('invalid-multilang');
			}

			continue;
		}

		// Check AI Respone Node
		if (node.shape === SCRIPT_NODE_TYPES.AI_RESPONSE) {
			const aiResponseData = node.data.response;

			var anyLanguageMissing = false;
			BusinessFullData.businessData.languages.forEach((language) => {
				const currentLanguageResponse = aiResponseData[language];

				if (!currentLanguageResponse || currentLanguageResponse === "" || currentLanguageResponse.trim() === "") {
					areLanguagesIncompleteInConversationTab[language] = true;
					anyLanguageMissing = true;
				}
			});
			if (anyLanguageMissing) {
				if (!onlyRemoveInvalid) {
					$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).addClass('invalid-multilang');
				}
			}
			else {
				$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).removeClass('invalid-multilang');
			}

			continue;
		}

		// Check System Tool Nodes
		if (node.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL) {
			const systemToolType = node.data.toolType;
			const config = node.data.config;

			// Check End Call Node
			if (systemToolType === SCRIPT_SYSTEM_TOOLS.END_CALL) {
				if (config.type === SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.WITH_MESSAGE) {
					var anyLanguageMissing = false;
					BusinessFullData.businessData.languages.forEach((language) => {
						const currentLanguageMessage = config.messages[language];
						if (!currentLanguageMessage || currentLanguageMessage === "" || currentLanguageMessage.trim() === "") {
							areLanguagesIncompleteInConversationTab[language] = true;
							anyLanguageMissing = true;
						}
					});
					if (anyLanguageMissing) {
						if (!onlyRemoveInvalid) {
							$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).addClass('invalid-multilang');
						}
					}
					else {
						$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).removeClass('invalid-multilang');
					}
				}

				continue;
			}

			// Check Get DTMF Input Node
			if (systemToolType === SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT) {
				var anyLanguageMissing = false;
				BusinessFullData.businessData.languages.forEach((language) => {
					config.outcomes.forEach((outcome) => {
						const currentLanguageOutcomeText = outcome.value[language];
						if (!currentLanguageOutcomeText || currentLanguageOutcomeText === "" || currentLanguageOutcomeText.trim() === "") {
							areLanguagesIncompleteInConversationTab[language] = true;
							anyLanguageMissing = true;
						}
					});
				});
				if (anyLanguageMissing) {
					if (!onlyRemoveInvalid) {
						$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).addClass('invalid-multilang');
					}
				}
				else {
					$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).removeClass('invalid-multilang');
				}

				continue;
			}

			// Check Send SMS Node
			if (systemToolType === SCRIPT_SYSTEM_TOOLS.SEND_SMS) {
				var anyLanguageMissing = false;
				BusinessFullData.businessData.languages.forEach((language) => {
					const currentLanguageMessage = config.messages[language];
					if (!currentLanguageMessage || currentLanguageMessage === "" || currentLanguageMessage.trim() === "") {
						areLanguagesIncompleteInConversationTab[language] = true;
						anyLanguageMissing = true;
					}
				});
				if (anyLanguageMissing) {
					if (!onlyRemoveInvalid) {
						$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).addClass('invalid-multilang');
					}
				}
				else {
					$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).removeClass('invalid-multilang');
				}
			}

			// Check Retrieve KnowledgeBase Node
			if (systemToolType === SCRIPT_SYSTEM_TOOLS.RETRIEVE_KNOWLEDGEBASE) {
				var anyLanguageMissing = false;
				BusinessFullData.businessData.languages.forEach((language) => {
					const currentLanguageMessage = config.responseBeforeExecution[language];
					if (!currentLanguageMessage || currentLanguageMessage === "" || currentLanguageMessage.trim() === "") {
						areLanguagesIncompleteInConversationTab[language] = true;
						anyLanguageMissing = true;
					}
				});
				if (anyLanguageMissing) {
					if (!onlyRemoveInvalid) {
						$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).addClass('invalid-multilang');
					}
				}
				else {
					$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${node.id}"] .script-node`).removeClass('invalid-multilang');
				}

				continue;
			}
		}
	}

	let isAnyIncompleteInScript = false;
	BusinessFullData.businessData.languages.forEach((language) => {
		const anyIncompleteForLanguage = areLanguagesIncompleteInGeneralTab[language] || areLanguagesIncompleteInConversationTab[language];
		if (anyIncompleteForLanguage) isAnyIncompleteInScript = true;

		scriptsManagerLanguageDropdown.setLanguageStatus(language, anyIncompleteForLanguage ? "incomplete" : "complete");
	});

	return {
		isValid: !isAnyIncompleteInScript,
		areLanguagesIncompleteInGeneralTab,
		areLanguagesIncompleteInConversationTab,
	};
}

function checkScriptTabHasChanges(enableDisableButton = true, compileConversationChanges = false) {
	const changes = {};
	let hasChanges = false;

	// General Tab
	changes.general = {
		name: CurrentScriptNameMultiLangData,
		description: CurrentScriptDescriptionMultiLangData,
	};

	BusinessFullData.businessData.languages.forEach((language) => {
		if (ManageCurrentScriptData.general.name[language] !== CurrentScriptNameMultiLangData[language]) hasChanges = true;
		if (ManageCurrentScriptData.general.description[language] !== CurrentScriptDescriptionMultiLangData[language]) hasChanges = true;
	});

	// Conversation/Graph Tab
	const currentGraphJsonData = CurrentScriptGraph.toJSON();
	const currentNodes = currentGraphJsonData.cells;
	const edgeNodes = currentNodes.filter((node) => node.shape === "edge");
	const scriptNodes = currentNodes.filter((node) => node.shape !== "edge");

	if (ManageCurrentScriptData.nodes.length !== scriptNodes.length || ManageCurrentScriptData.edges.length !== edgeNodes.length) {
		hasChanges = true;
	}

	// Nodes
	changes.nodes = [];
	if (!hasChanges || (hasChanges && compileConversationChanges)) {
		for (let i = 0; i < scriptNodes.length; i++) {
			const newNode = scriptNodes[i];
			const pushNewNode = {
				id: newNode.id,
				type: parseInt(newNode.shape),
				position: {
					x: newNode.position.x,
					y: newNode.position.y,
				},
			};

			let oldNode = {};
			const oldNodeIndex = ManageCurrentScriptData.nodes.findIndex((node) => node.id === newNode.id);
			if (oldNodeIndex === -1) {
				hasChanges = true;
				if (!compileConversationChanges) break;
			} else {
				oldNode = ManageCurrentScriptData.nodes[oldNodeIndex];
			}

			const newScriptNodeData = newNode.data;

			// Check node position compared to old
			if (oldNodeIndex !== -1) {
				if (oldNode.position?.x !== newNode.position.x || oldNode.position?.y !== newNode.position.y) {
					hasChanges = true;
					if (!compileConversationChanges) break;
				}
			}

			// User Query Node
			if (newNode.shape === SCRIPT_NODE_TYPES.USER_QUERY) {
				pushNewNode.query = newScriptNodeData.query;
				pushNewNode.examples = newScriptNodeData.examples;

				if (oldNodeIndex !== -1) {
					if (JSON.stringify(oldNode.query) !== JSON.stringify(newScriptNodeData.query) || JSON.stringify(oldNode.examples) !== JSON.stringify(newScriptNodeData.examples)) {
						hasChanges = true;
						if (!compileConversationChanges) break;
					}
				}
			}

			// AI Response Node
			if (newNode.shape === SCRIPT_NODE_TYPES.AI_RESPONSE) {
				pushNewNode.response = newScriptNodeData.response;
				pushNewNode.examples = newScriptNodeData.examples;

				if (oldNodeIndex !== -1) {
					if (JSON.stringify(oldNode.response) !== JSON.stringify(newScriptNodeData.response) || JSON.stringify(oldNode.examples) !== JSON.stringify(newScriptNodeData.examples)) {
						hasChanges = true;
						if (!compileConversationChanges) break;
					}
				}
			}

			// System Tool Node
			if (newNode.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL) {
				pushNewNode.toolType = newScriptNodeData.toolType;

				if (oldNodeIndex !== -1) {
					if (oldNode.toolType.value !== pushNewNode.toolType) {
						hasChanges = true;
						if (!compileConversationChanges) break;
					}
				}

				pushNewNode.config = {};

				// End Call Tool
				if (newScriptNodeData.toolType === SCRIPT_SYSTEM_TOOLS.END_CALL) {
					pushNewNode.config.type = newScriptNodeData.config.type;
					pushNewNode.config.messages = newScriptNodeData.config.messages;

					if (oldNodeIndex !== -1 && oldNode.toolType.value === pushNewNode.toolType) {
						if (oldNode.type.value !== pushNewNode.config.type || JSON.stringify(oldNode.messages) !== JSON.stringify(pushNewNode.config.messages)) {
							hasChanges = true;
							if (!compileConversationChanges) break;
						}
					}
				}

				// DTMF Input Tool
				if (newScriptNodeData.toolType === SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT) {
					pushNewNode.config.timeout = newScriptNodeData.config.timeout;
					pushNewNode.config.requireStartAsterisk = newScriptNodeData.config.requireStartAsterisk;
					pushNewNode.config.requireEndHash = newScriptNodeData.config.requireEndHash;
					pushNewNode.config.maxLength = newScriptNodeData.config.maxLength;
					pushNewNode.config.encryptInput = newScriptNodeData.config.encryptInput;
					pushNewNode.config.variableName = newScriptNodeData.config.variableName;
					pushNewNode.config.outcomes = newScriptNodeData.config.outcomes;

					if (oldNodeIndex !== -1 && oldNode.toolType.value === pushNewNode.toolType) {
						if (
							oldNode.timeout !== pushNewNode.config.timeout ||
							oldNode.requireStartAsterisk !== pushNewNode.config.requireStartAsterisk ||
							oldNode.requireEndHash !== pushNewNode.config.requireEndHash ||
							oldNode.maxLength !== pushNewNode.config.maxLength ||
							oldNode.encryptInput !== pushNewNode.config.encryptInput ||
							oldNode.variableName !== pushNewNode.variableName ||
							JSON.stringify(oldNode.outcomes) !== JSON.stringify(pushNewNode.config.outcomes)
						) {
							hasChanges = true;
							if (!compileConversationChanges) break;
						}
					}
				}

				// Transfer To Agent Tool
				if (newScriptNodeData.toolType === SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_AGENT) {
					pushNewNode.config.agentId = newScriptNodeData.config.agentId;
					pushNewNode.config.transferContext = newScriptNodeData.config.transferContext;
					pushNewNode.config.summarizeContext = newScriptNodeData.config.summarizeContext;

					if (oldNodeIndex !== -1 && oldNode.toolType.value === pushNewNode.toolType) {
						if (oldNode.agentId !== pushNewNode.config.agentId || oldNode.transferContext !== pushNewNode.config.transferContext || oldNode.summarizeContext !== pushNewNode.config.summarizeContext) {
							hasChanges = true;
							if (!compileConversationChanges) break;
						}
					}
				}

				// Add Script To Context Tool
				if (newScriptNodeData.toolType === SCRIPT_SYSTEM_TOOLS.ADD_SCRIPT_TO_CONTEXT) {
					pushNewNode.config.scriptId = newScriptNodeData.config.scriptId;

					if (oldNodeIndex !== -1 && oldNode.toolType.value === pushNewNode.toolType) {
						if (oldNode.scriptId !== pushNewNode.config.scriptId) {
							hasChanges = true;
							if (!compileConversationChanges) break;
						}
					}
				}

				// Send SMS Tool
				if (newScriptNodeData.toolType === SCRIPT_SYSTEM_TOOLS.SEND_SMS) {
					pushNewNode.config.phoneNumberId = newScriptNodeData.config.phoneNumberId;
					pushNewNode.config.messages = newScriptNodeData.config.messages;

					if (oldNodeIndex !== -1 && oldNode.toolType.value === pushNewNode.toolType) {
						if (oldNode.phoneNumberId !== pushNewNode.config.phoneNumberId || JSON.stringify(oldNode.messages) !== JSON.stringify(pushNewNode.config.messages)) {
							hasChanges = true;
							if (!compileConversationChanges) break;
						}
					}
				}

				// Go To Node
				if (newScriptNodeData.toolType === SCRIPT_SYSTEM_TOOLS.GOTONODE) {
					pushNewNode.config.goToNodeId = newScriptNodeData.config.goToNodeId;

					if (oldNodeIndex !== -1 && oldNode.toolType.value === pushNewNode.toolType) {
						if (oldNode.goToNodeId !== pushNewNode.config.goToNodeId) {
							hasChanges = true;
							if (!compileConversationChanges) break;
						}
					}
				}

				// Retrieve KnowledgeBase Tool
				if (newScriptNodeData.toolType === SCRIPT_SYSTEM_TOOLS.RETRIEVE_KNOWLEDGEBASE) {
					pushNewNode.config.type = newScriptNodeData.config.type;
					pushNewNode.config.responseBeforeExecution = newScriptNodeData.config.responseBeforeExecution;

					if (oldNodeIndex !== -1 && oldNode.toolType.value === pushNewNode.toolType) {
						if (JSON.stringify(oldNode.responseBeforeExecution) !== JSON.stringify(pushNewNode.config.responseBeforeExecution)) {
							hasChanges = true;
							if (!compileConversationChanges) break;
						}
					}
				}
			}

			// Custom Tool Node
			if (newNode.shape === SCRIPT_NODE_TYPES.CUSTOM_TOOL) {
				pushNewNode.toolId = newScriptNodeData.toolId;
				pushNewNode.config = newScriptNodeData.config;

				if (oldNodeIndex !== -1) {
					if (oldNode.toolId !== newScriptNodeData.toolId || JSON.stringify(oldNode.config ?? {}) !== JSON.stringify(newScriptNodeData.config)) {
						hasChanges = true;
						if (!compileConversationChanges) break;
					}
				}
			}

			if (compileConversationChanges) {
				changes.nodes.push(pushNewNode);
			}
		}
	}

	// Edges
	changes.edges = [];
	if (!hasChanges || (hasChanges && compileConversationChanges)) {
		for (let i = 0; i < edgeNodes.length; i++) {
			var sourceCellNode = scriptNodes.find((node) => node.id === edgeNodes[i].source.cell);
			var sourceCellNodePort = sourceCellNode.ports.items.find((port) => port.id === edgeNodes[i].source.port);

			var targetCellNode = scriptNodes.find((node) => node.id === edgeNodes[i].target.cell);
			var targetCellNodePort = targetCellNode.ports.items.find((port) => port.id === edgeNodes[i].target.port);

			var isSourceNodeOutput = sourceCellNodePort.group === "output";

			const pushNewEdgeNode = {
				id: edgeNodes[i].id,
				sourceNodeId: isSourceNodeOutput ? edgeNodes[i].source.cell : edgeNodes[i].target.cell,
				sourceNodePortId: isSourceNodeOutput ? edgeNodes[i].source.port : edgeNodes[i].target.port,
				targetNodeId: isSourceNodeOutput ? edgeNodes[i].target.cell : edgeNodes[i].source.cell,
				targetNodePortId: isSourceNodeOutput ? edgeNodes[i].target.port : edgeNodes[i].source.port,
			};

			if (compileConversationChanges) {
				changes.edges.push(pushNewEdgeNode);
			}

			if (hasChanges) continue;

			const oldNodeIndex = ManageCurrentScriptData.edges.findIndex((node) => node.id === pushNewEdgeNode.id);
			if (oldNodeIndex === -1) {
				hasChanges = true;
				if (!compileConversationChanges) break;
				continue;
			}

			const oldNode = ManageCurrentScriptData.edges[oldNodeIndex];

			// Check Source Data
			if (pushNewEdgeNode.sourceNodeId !== oldNode.sourceNodeId || pushNewEdgeNode.sourceNodePortId !== oldNode.sourceNodePortId) {
				hasChanges = true;
				if (!compileConversationChanges) break;
				continue;
			}

			// Check Target Data
			if (pushNewEdgeNode.targetNodeId !== oldNode.targetNodeId || pushNewEdgeNode.targetNodePortId !== oldNode.targetNodePortId) {
				hasChanges = true;
				if (!compileConversationChanges) break;
				continue;
			}
		}
	}

	// Variables
	changes.variables = [];
	if (compileConversationChanges) {
		changes.variables = structuredClone(CurrentScriptVariablesData);
	}

	const savedVariables = ManageCurrentScriptData.variables || [];
	if (CurrentScriptVariablesData.length !== savedVariables.length) {
		hasChanges = true;
	}

	if (!hasChanges) {
		// We iterate current to see if anything changed or was added
		for (let i = 0; i < CurrentScriptVariablesData.length; i++) {
			const currVar = CurrentScriptVariablesData[i];
			const oldVar = savedVariables.find(v => v.key === currVar.key);

			if (!oldVar) {
				hasChanges = true; // New variable found
				break;
			}

			// Primitive Fields
			if (
				(currVar.type.value ?? currVar.type) !== oldVar.type.value ||
				currVar.defaultValue !== oldVar.defaultValue ||
				currVar.isVisibleToAgent !== oldVar.isVisibleToAgent ||
				currVar.isEditableByAI !== oldVar.isEditableByAI
			) {
				hasChanges = true;
				break;
			}

			// Description (Multi-lang Dictionary)
			let descChanged = false;
			BusinessFullData.businessData.languages.forEach(lang => {
				if (currVar.description[lang.id] !== oldVar.description[lang.id]) {
					descChanged = true;
				}
			});
			if (descChanged) {
				hasChanges = true;
				break;
			}
		}
	}

	if (enableDisableButton) {
		saveScriptButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

function createScriptListCardElement(scriptData) {
	const actionDropdownHtml = `
        <div class="dropdown action-dropdown dropdown-menu-end">
            <button class="btn action-button dropdown-toggle" type="button" data-bs-toggle="dropdown" data-bs-auto-close="true" aria-expanded="false">
                <i class="fa-solid fa-ellipsis"></i>
            </button>
            <ul class="dropdown-menu">
                <li>
                    <span class="dropdown-item text-danger" data-item-id="${scriptData.id}" button-type="delete-script">
                        <i class="fa-solid fa-trash me-2"></i>Delete
                    </span>
                </li>
            </ul>
        </div>
    `;

	return createIqraCardElement({
		id: scriptData.id,
		type: 'script',
		visualHtml: `<span>${scriptData.general.emoji}</span>`,
		titleHtml: scriptData.general.name[BusinessDefaultLanguage],
		descriptionHtml: scriptData.general.description[BusinessDefaultLanguage],
		actionDropdownHtml: actionDropdownHtml,
	});
}

function fillScriptsListTab() {
	scriptsCardListContainer.empty();

	if (BusinessFullData.businessApp.scripts.length !== 0) {
		BusinessFullData.businessApp.scripts.forEach((script) => {
			const element = createScriptListCardElement(script);
			scriptsCardListContainer.append($(element));
		});
	}
	else {
		scriptsCardListContainer.append("<div class='none-script-notice col-12'>No scripts found.</tr>");
	}
}

function validateScriptConnections() {
	let isValid = true;
	const errors = [];

	const currentNodesEdgesArray = CurrentScriptGraph.toJSON();

	const edgesArray = currentNodesEdgesArray.cells.filter((node) => node.shape === "edge");
	if (edgesArray.length === 0) {
		isValid = false;
		errors.push("No script connections found. Please connect atleast one script node.");
	}

	edgesArray.forEach((edge) => {
		const sourceNodeId = edge.source.cell;
		const sourceNodePortId = edge.source.port;
		const targetNodeId = edge.target.cell;
		const targetNodePortId = edge.target.port;

		const sourceNode = currentNodesEdgesArray.cells.find((node) => node.id === sourceNodeId);
		const sourceNodePortGroup = sourceNode.ports.items.find((port) => port.id === sourceNodePortId).group;
		const targetNode = currentNodesEdgesArray.cells.find((node) => node.id === targetNodeId);
		const targetNodePortGroup = targetNode.ports.items.find((port) => port.id === targetNodePortId).group;

		if ((sourceNodePortGroup === "output" && targetNodePortGroup === "output") || (sourceNodePortGroup === "input" && targetNodePortGroup === "input")) {
			isValid = false;
			errors.push("You can't connect two input nodes or two output nodes.");
			return;
		}

		let inputCell;
		let outputCell;

		let inputPort;
		let outputPort;

		if (sourceNodePortGroup === "input") {
			inputCell = sourceNode;
			outputCell = targetNode;

			inputPort = sourceNodePortId;
			outputPort = targetNodePortId;
		} else {
			inputCell = targetNode;
			outputCell = sourceNode;

			inputPort = targetNodePortId;
			outputPort = sourceNodePortId;
		}

		// start node can not connect to ai response node
		if (outputCell.shape === SCRIPT_NODE_TYPES.START && inputCell.shape === SCRIPT_NODE_TYPES.AI_RESPONSE) {
			isValid = false;
			errors.push("Start node can not connect to ai response node.");
		}

		// ai response node can only connect to user query node
		if (outputCell.shape === SCRIPT_NODE_TYPES.AI_RESPONSE && inputCell.shape !== SCRIPT_NODE_TYPES.USER_QUERY) {
			isValid = false;
			errors.push("AI response node can only connect to user query node.");
		}

		// validate if source already has connected nodes
		CurrentScriptGraph.getEdges().forEach((edge) => {
			const letEdgeSource = edge.getSource();

			if (letEdgeSource.cell === outputCell.id && letEdgeSource.port === outputPort) {
				const letEdgeTarget = edge.getTarget();

				if (letEdgeTarget.cell) {
					letEdgeTargetCell = CurrentScriptGraph.getCellById(letEdgeTarget.cell);

					// if atleast one user query is connected, then no other node type can be connected
					if (letEdgeTargetCell.shape === SCRIPT_NODE_TYPES.USER_QUERY && inputCell.shape !== SCRIPT_NODE_TYPES.USER_QUERY) {
						isValid = false;
						errors.push("A node connected to one user query node can only connect to user query nodes.");
					}

					// if one custom tool/system tool/ai response is connected, then no other type and no more nodes can be connected
					if (
						(letEdgeTargetCell.shape === SCRIPT_NODE_TYPES.CUSTOM_TOOL ||
							letEdgeTargetCell.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL ||
							letEdgeTargetCell.shape === SCRIPT_NODE_TYPES.AI_RESPONSE) &&
						letEdgeTargetCell.shape !== inputCell.shape
					) {
						isValid = false;
						errors.push("A node connected to one custom tool/system tool/ai response node can only connect to custom tool/system tool/ai response nodes.");
					}
				}
			}
		});
	});

	return { isValid, errors };
}

function validateScriptNodes(onlyRemove = true) {
	let isValid = true;
	const errors = [];

	const currentNodesEdgesArray = CurrentScriptGraph.toJSON().cells;
	if (currentNodesEdgesArray.length > 0) {
		currentNodesEdgesArray.forEach((nodeData) => {

			if (nodeData.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL) {
				if (!nodeData.data.toolType || nodeData.data.toolType === null) {
					isValid = false;
					errors.push("System tool does not have a valid tool type.");

					if (!onlyRemove) {
						$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${nodeData.id}"] .script-node`).addClass('invalid-multilang');
					}
				}
				else {
					$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${nodeData.id}"] .script-node`).removeClass('invalid-multilang');
				}

				// GOTO NODE
				if (nodeData.data.toolType === SCRIPT_SYSTEM_TOOLS.GOTONODE) {
					var foundNode = currentNodesEdgesArray.find((node) => node.id === nodeData.data.config.goToNodeId);
					if (!foundNode || foundNode.shape === "edge" || foundNode.id === nodeData.id || (foundNode.SYSTEM_TOOL === SCRIPT_NODE_TYPES.GOTONODE && foundNode.data.toolType === SCRIPT_SYSTEM_TOOLS.GOTONODE)) {
						isValid = false;
						errors.push("Go to node does not have a valid node selection.");

						if (!onlyRemove) {
							$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${nodeData.id}"] .script-node`).addClass('invalid-multilang');
						}
					}
					else {
						$(CurrentScriptGraph.view.container).find(`g[data-cell-id="${nodeData.id}"] .script-node`).removeClass('invalid-multilang');
					}

					return;
				}
			}

		});
	}

	return { isValid, errors };
}

function fillScriptManagerTab() {
	const currentSelectedLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

	// Fill General Tab
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentScriptNameMultiLangData[language] = ManageCurrentScriptData.general.name[language];
	});
	inputScriptName.val(CurrentScriptNameMultiLangData[currentSelectedLanguage]);

	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentScriptDescriptionMultiLangData[language] = ManageCurrentScriptData.general.description[language];
	});
	inputScriptDescription.val(CurrentScriptDescriptionMultiLangData[currentSelectedLanguage]);

	// Fill Conversations Tab
	const backendNodes = ManageCurrentScriptData.nodes;
	const backendEdges = ManageCurrentScriptData.edges;

	backendNodes.forEach((node) => {
		const nodeBase = {
			id: node.id,
			shape: node.nodeType.value,
			view: "html-view",
			position: {
				x: node.position.x,
				y: node.position.y,
			},
			size: {
				width: SCRIPT_NODE_WIDTH,
				height: SCRIPT_NODE_MIN_HEIGHT,
			},
			attrs: {},
			data: {
				type: node.nodeType.value,
			},
		};

		// Node Size for Start Node
		if (nodeBase.shape === SCRIPT_NODE_TYPES.START) {
			nodeBase.size = {
				width: 250,
				height: 70,
			};
		}

		// Data based on node types
		// User Query Data
		if (nodeBase.shape === SCRIPT_NODE_TYPES.USER_QUERY) {
			nodeBase.data.query = node.query;
			nodeBase.data.examples = node.examples;
		}
		// AI Response Data
		else if (nodeBase.shape === SCRIPT_NODE_TYPES.AI_RESPONSE) {
			nodeBase.data.response = node.response;
			nodeBase.data.examples = node.examples;
		}
		// System Tool Data
		else if (nodeBase.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL) {
			nodeBase.size.height = 135;

			nodeBase.data.toolType = node.toolType.value;

			nodeBase.data.config = {};

			// END CALL TOOL DATA
			if (node.toolType.value === SCRIPT_SYSTEM_TOOLS.END_CALL) {
				nodeBase.data.config.type = node.type.value;
				if (node.type.value === SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.WITH_MESSAGE) {
					nodeBase.data.config.messages = node.messages;
				}
			}
			// GET DTMF TOOL DATA
			else if (node.toolType.value === SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT) {
				nodeBase.data.config.timeout = node.timeout;
				nodeBase.data.config.requireStartAsterisk = node.requireStartAsterisk;
				nodeBase.data.config.requireEndHash = node.requireEndHash;
				nodeBase.data.config.maxLength = node.maxLength;
				nodeBase.data.config.encryptInput = node.encryptInput;
				nodeBase.data.config.outcomes = node.outcomes;
			}
			// TRANSFER TO AGENT DATA
			else if (node.toolType.value === SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_AGENT) {
				nodeBase.data.config.agentId = node.agentId;
			}
			// ADD SCRIPT TO CONTEXT DATA
			else if (node.toolType.value === SCRIPT_SYSTEM_TOOLS.ADD_SCRIPT_TO_CONTEXT) {
				nodeBase.data.config.scriptId = node.scriptId;
			}
			// SEND SMS DATA
			else if (node.toolType.value === SCRIPT_SYSTEM_TOOLS.SEND_SMS) {
				nodeBase.data.config.phoneNumberId = node.phoneNumberId;
				nodeBase.data.config.messages = node.messages;
			}
			// Go To Node Data
			else if (node.toolType.value === SCRIPT_SYSTEM_TOOLS.GOTONODE) {
				nodeBase.data.config.goToNodeId = node.goToNodeId;
			}
			// END CALL TOOL DATA
			else if (node.toolType.value === SCRIPT_SYSTEM_TOOLS.RETRIEVE_KNOWLEDGEBASE) {
				nodeBase.data.config.responseBeforeExecution = node.responseBeforeExecution;
			}
		}
		// Custom Tool Data
		else if (nodeBase.shape === SCRIPT_NODE_TYPES.CUSTOM_TOOL) {
			nodeBase.size.height = 135;

			nodeBase.data.config = {};
			nodeBase.data.toolId = node.toolId;
		}

		const currentNodeCell = CurrentScriptGraph.addNode(nodeBase);

		// Ports
		const edgesWhereTargetNode = backendEdges.filter((edge) => edge.targetNodeId === node.id);
		edgesWhereTargetNode.forEach((edgeData) => {
			const targetNodePortId = edgeData.targetNodePortId;

			if (currentNodeCell.getPortIndex(targetNodePortId) !== -1) {
				return;
			}

			currentNodeCell.addPort({
				group: "input",
				id: targetNodePortId,
			});
		});

		const edgesWhereSourceNode = backendEdges.filter((edge) => edge.sourceNodeId === node.id);
		edgesWhereSourceNode.forEach((edgeData) => {
			const sourceNodePortId = edgeData.sourceNodePortId;

			if (currentNodeCell.getPortIndex(sourceNodePortId) !== -1) {
				return;
			}

			const baseNodePort = {
				group: "output",
				id: sourceNodePortId,
			};

			const sourceNodeCell = CurrentScriptGraph.getCellById(edgeData.sourceNodeId);
			if (sourceNodeCell.shape === SCRIPT_NODE_TYPES.CUSTOM_TOOL) {
				if (sourceNodePortId === "outcome-default") {
					baseNodePort.attrs = {
						circle: {
							fill: "#ffc107",
						},
						text: {
							text: "Default",
						},
					};
				} else {
					const responseCode = sourceNodePortId.replace("outcome-", "");

					baseNodePort.attrs = {
						circle: {
							fill: "#fff",
						},
						text: {
							text: `Response ${responseCode}`,
						},
					};
				}
			}

			if (sourceNodeCell.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL) {
				const systemToolType = sourceNodeCell.data.toolType;

				// GET DTMF INPUT NODE
				if (systemToolType === SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT) {
					if (sourceNodePortId === "timeout") {
						baseNodePort.attrs = {
							circle: {
								fill: "#ffc107",
							},
							text: {
								text: "Timeout",
							},
						};
					} else {
						const cellData = currentNodeCell.getData();
						const currentOutcomeValue = cellData.config.outcomes.find((outcome) => outcome.portId === sourceNodePortId).value[currentSelectedLanguage];

						baseNodePort.attrs = {
							circle: {
								fill: "#fff",
							},
							text: {
								text: currentOutcomeValue,
							},
						};
					}
				}
				// SEND SMS NODE
				else if (systemToolType === SCRIPT_SYSTEM_TOOLS.SEND_SMS) {
					if (sourceNodePortId === "error") {
						baseNodePort.attrs = {
							circle: {
								fill: "#ffc107",
							},
							text: {
								text: "Error",
							},
						};
					}
					else if (sourceNodePortId === "success") {
						baseNodePort.attrs = {
							circle: {
								fill: "#fff",
							},
							text: {
								text: "Success",
							},
						};
					}
				}
			}

			currentNodeCell.addPort(baseNodePort);
		});

		// Add Missing Ports if needed
		const currentPorts = currentNodeCell.getPorts();
		if (nodeBase.shape === SCRIPT_NODE_TYPES.START) {
			const hasOutputPort = currentPorts.some((port) => port.group === "output");
			if (!hasOutputPort) {
				currentNodeCell.addPort({ group: "output" });
			}
		}
		if (nodeBase.shape === SCRIPT_NODE_TYPES.USER_QUERY || nodeBase.shape === SCRIPT_NODE_TYPES.AI_RESPONSE) {
			const hasInputPort = currentPorts.some((port) => port.group === "input");
			if (!hasInputPort) {
				currentNodeCell.addPort({ group: "input" });
			}

			const hasOutputPort = currentPorts.some((port) => port.group === "output");
			if (!hasOutputPort) {
				currentNodeCell.addPort({ group: "output" });
			}
		}
		if (nodeBase.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL || nodeBase.shape === SCRIPT_NODE_TYPES.CUSTOM_TOOL) {
			const hasInputPort = currentPorts.some((port) => port.group === "input");
			if (!hasInputPort) {
				currentNodeCell.addPort({ group: "input" });
			}
		}
		if (nodeBase.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL) {
			const nodeData = currentNodeCell.getData();

			const toolType = nodeData.toolType;

			if (toolType === SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT) {
				const hasTimeoutPort = currentPorts.some((port) => port.id === "timeout");
				if (!hasTimeoutPort) {
					currentNodeCell.addPort({
						id: "timeout",
						group: "output",
						attrs: {
							circle: {
								fill: "#ffc107",
							},
							text: {
								text: "Timeout",
							},
						},
					});
				}

				const toolOutcomes = nodeData.config.outcomes;
				toolOutcomes.forEach((outcome) => {
					const hasOutcomePort = currentPorts.some((port) => port.id === outcome.portId);
					if (!hasOutcomePort) {
						currentNodeCell.addPort({
							id: outcome.portId,
							group: "output",
							attrs: {
								circle: {
									fill: "#fff",
								},
								text: {
									text: outcome.value[currentSelectedLanguage],
								},
							},
						});
					}
				});
			}
			else if (toolType === SCRIPT_SYSTEM_TOOLS.SEND_SMS) {
				const hasErrorPort = currentPorts.some((port) => port.id === "error");
				if (!hasErrorPort) {
					currentNodeCell.addPort({
						id: "error",
						group: "output",
						attrs: {
							circle: {
								fill: "#ffc107",
							},
							text: {
								text: "Error",
							},
						},
					});
				}

				const hasSuccessPort = currentPorts.some((port) => port.id === "success");
				if (!hasSuccessPort) {
					currentNodeCell.addPort({
						id: "success",
						group: "output",
						attrs: {
							text: {
								text: "Success",
							},
						},
					});
				}
			}
			else if (
				toolType !== SCRIPT_SYSTEM_TOOLS.END_CALL
				&& toolType !== SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_AGENT
				&& toolType !== SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_HUMAN
				&& toolType !== SCRIPT_SYSTEM_TOOLS.GOTONODE
			) {
				const hasOutputPort = currentPorts.some((port) => port.group === "output");
				if (!hasOutputPort) {
					currentNodeCell.addPort({ group: "output" });
				}
			}
		}
		if (nodeBase.shape === SCRIPT_NODE_TYPES.CUSTOM_TOOL) {
			const hasDefaultPort = currentPorts.some((port) => port.id === "outcome-default");
			if (!hasDefaultPort) {
				currentNodeCell.addPort({
					id: "outcome-default",
					group: "output",
					attrs: {
						circle: {
							fill: "#ffc107",
						},
						text: {
							text: "Default",
						},
					},
				});
			}

			const toolResponses = BusinessFullData.businessApp.tools.find((t) => t.id === node.toolId).response;
			Object.keys(toolResponses).forEach((responseCode) => {
				const hasResponsePort = currentPorts.some((port) => port.id === `outcome-${responseCode}`);
				if (!hasResponsePort) {
					currentNodeCell.addPort({
						id: `outcome-${responseCode}`,
						group: "output",
						attrs: {
							circle: {
								fill: "#fff",
							},
							text: {
								text: `Response ${responseCode}`,
							},
						},
					});
				}
			});
		}
	});

	backendEdges.forEach((edge) => {
		const edgeBase = {
			id: edge.id,
			source: {
				cell: edge.sourceNodeId,
				port: edge.sourceNodePortId,
			},
			target: {
				cell: edge.targetNodeId,
				port: edge.targetNodePortId,
			},
		};

		const currentEdge = CurrentScriptGraph.addEdge(edgeBase);
	});

	// Load Variables (Deep Copy)
	CurrentScriptVariablesData = structuredClone(ManageCurrentScriptData.variables || []);
}

// Script Graph
function registerScriptNodes() {
	// Register Start Node
	X6.Shape.HTML.register({
		shape: SCRIPT_NODE_TYPES.START,
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
			div.className = "script-node script-start-node";

			div.innerHTML = `
                <div class="script-node-content">
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
		shape: SCRIPT_NODE_TYPES.USER_QUERY,
		width: SCRIPT_NODE_WIDTH,
		height: SCRIPT_NODE_MIN_HEIGHT,
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
			div.className = "script-node script-user-query-node";

			const data = cell.getData() || {};
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			div.innerHTML = `
                <div class="script-node-header">
                    <div>
						<div class="d-flex align-items-center btn-ic-span-align node-title">
							<i class="fa-regular fa-message me-2"></i>
							<span>User Query</span>
						</div>
						<span class="node-id">${cell.id}</span>
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
                <div class="script-node-content">
                    <div class="script-node-input-group html-shape-immovable">
                        <textarea 
                            class="form-control" 
                            placeholder="Type the user query..."
                            data-input="user-query"
                            rows="3"
                        >${data.query?.[currentLanguage] || ""}</textarea>
                    </div>
                </div>
            `;

			return div;
		},
	});

	// AI Response Node
	X6.Shape.HTML.register({
		shape: SCRIPT_NODE_TYPES.AI_RESPONSE,
		width: SCRIPT_NODE_WIDTH,
		height: SCRIPT_NODE_MIN_HEIGHT,
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
			div.className = "script-node script-ai-response-node";

			const data = cell.getData() || {};
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			div.innerHTML = `
                <div class="script-node-header">
                    <div>
						<div class="d-flex align-items-center btn-ic-span-align node-title">
							<i class="fa-regular fa-robot me-2"></i>
							<span>AI Response</span>
						</div>
						<span class="node-id">${cell.id}</span>
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
                <div class="script-node-content">
                    <div class="script-node-input-group html-shape-immovable">
                        <textarea 
                            class="form-control" 
                            placeholder="Type the AI response..."
                            data-input="ai-response"
                            rows="3"
                        >${data.response?.[currentLanguage] || ""}</textarea>
                    </div>
                </div>
            `;

			return div;
		},
	});

	// System Tool Node
	X6.Shape.HTML.register({
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		width: SCRIPT_NODE_WIDTH,
		height: 135,
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
			div.className = "script-node script-system-tool-node";

			const data = cell.getData() || {};
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			div.innerHTML = `
                <div class="script-node-header">
                    <div>
						<div class="d-flex align-items-center btn-ic-span-align node-title">
							<i class="fa-regular fa-toolbox me-2"></i>
							<span>System Tool</span>
						</div>
						<span class="node-id">${cell.id}</span>
					</div>
                    <div class="node-actions html-shape-immovable">
						<button class="btn btn-light btn-sm me-2" data-action="configure-system-tool" ${doesScriptSystemToolRequireConfig(data.toolType) ? "" : "disabled"}>
							<i class="fa-regular fa-gear"></i>
						</button>
                        <button class="btn btn-danger btn-sm" data-action="delete-node">
                            <i class="fa-regular fa-trash"></i>
                        </button>
                    </div>
                </div>
                <div class="script-node-content">
                    <div class="script-node-input-group html-shape-immovable">
                        <div class="d-flex gap-2">
                            <select class="form-select" data-input="system-tool-type">
                                <option value="" disabled ${!data.toolType ? "selected" : ""}>Select Tool</option>
                                <option value="${SCRIPT_SYSTEM_TOOLS.END_CALL}" ${data.toolType === SCRIPT_SYSTEM_TOOLS.END_CALL ? "selected" : ""}>End Call</option>
                                <option value="${SCRIPT_SYSTEM_TOOLS.CHANGE_LANGUAGE}" ${data.toolType === SCRIPT_SYSTEM_TOOLS.CHANGE_LANGUAGE ? "selected" : ""}>Change Language</option>
                                <option value="${SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT}" ${data.toolType === SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT ? "selected" : ""}>Get DTMF Keypad Input</option>
                                <option value="${SCRIPT_SYSTEM_TOOLS.PRESS_DTMF}" ${data.toolType === SCRIPT_SYSTEM_TOOLS.PRESS_DTMF ? "selected" : ""}>Press DTMF Keypad</option>
                                <option value="${SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_AGENT}" ${data.toolType === SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_AGENT ? "selected" : ""}>Transfer to Agent</option>
                                <option value="${SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_HUMAN}" ${data.toolType === SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_HUMAN ? "selected" : ""}>Transfer to Human</option>
                                <option value="${SCRIPT_SYSTEM_TOOLS.ADD_SCRIPT_TO_CONTEXT}" ${data.toolType === SCRIPT_SYSTEM_TOOLS.ADD_SCRIPT_TO_CONTEXT ? "selected" : ""}>Add Script to Context</option>
                                <option value="${SCRIPT_SYSTEM_TOOLS.SEND_SMS}" ${data.toolType === SCRIPT_SYSTEM_TOOLS.SEND_SMS ? "selected" : ""}>Send SMS</option>
                                <option value="${SCRIPT_SYSTEM_TOOLS.GOTONODE}" ${data.toolType === SCRIPT_SYSTEM_TOOLS.GOTONODE ? "selected" : ""}>Go To Node</option>
								<option value="${SCRIPT_SYSTEM_TOOLS.RETRIEVE_KNOWLEDGEBASE}" ${data.toolType === SCRIPT_SYSTEM_TOOLS.RETRIEVE_KNOWLEDGEBASE ? "selected" : ""}>Retrieve KnowledgeBase</option>
                            </select>
                        </div>
                    </div>
                </div>
            `;

			return div;
		},
	});

	// Custom Tool Node
	X6.Shape.HTML.register({
		shape: SCRIPT_NODE_TYPES.CUSTOM_TOOL,
		width: SCRIPT_NODE_WIDTH,
		height: 135,
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
			div.className = "script-node script-custom-tool-node";

			const data = cell.getData();

			// Get available custom tools
			const tools = BusinessFullData.businessApp.tools || [];
			const toolOptions = tools
				.map(
					(tool) => `
                    <option value="${tool.id}" ${data.toolId === tool.id ? "selected" : ""}>
                        ${tool.general.name[BusinessDefaultLanguage]}
                    </option>
                `,
				)
				.join("");

			div.innerHTML = `
                <div class="script-node-header">
                    <div>
						<div class="d-flex align-items-center btn-ic-span-align node-title">
							<i class="fa-regular fa-wrench me-2"></i>
							<span>Custom Tool</span>
						</div>
						<span class="node-id">${cell.id}</span>
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
                <div class="script-node-content">
                    <div class="script-node-input-group html-shape-immovable">
                        <select class="form-select" data-input="custom-tool-select">
                            <option value="" disabled ${!data.toolId ? "selected" : ""}>Select Tool</option>
                            ${toolOptions}
                        </select>
                    </div>
                </div>
            `;

			return div;
		},
	});

	// FlowApp Node
	X6.Shape.HTML.register({
		shape: SCRIPT_NODE_TYPES.FLOW_APP,
		width: SCRIPT_NODE_WIDTH,
		height: 135,
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
			div.className = "script-node script-flowapp-node"; // Add specific class for styling

			const data = cell.getData() || {};
			const appDef = SpecificationFlowAppsListData.find(a => a.appKey === data.appKey);

			// Fallback visuals if app def is missing (e.g. app deleted/disabled)
			const appName = appDef ? appDef.name : (data.appKey || "Unknown App");
			const appIcon = appDef && appDef.iconUrl
				? `<img src="${appDef.iconUrl}" style="width: 20px; height: 20px; margin-right: 8px;">`
				: `<i class="fa-regular fa-plug me-2"></i>`;

			// Determine Action Label
			let actionLabel = '<span class="text-muted fst-italic" data-input="flow-app-action-label">Select Action...</span>';
			if (data.actionKey && appDef) {
				const actionDef = appDef.actions.find(a => a.actionKey === data.actionKey);
				if (actionDef) {
					actionLabel = `<span class="fw-bold text-white" data-input="flow-app-action-label">${actionDef.name}</span>`;
				} else {
					actionLabel = `<span class="text-danger" data-input="flow-app-action-label">Invalid Action</span>`;
				}
			}

			// Styling for the specific node type (inline or class)
			// We reuse the header structure but inject the App Icon
			div.innerHTML = `
                <div class="script-node-header">
                    <div>
						<div class="d-flex align-items-center btn-ic-span-align node-title">
							${appIcon}
							<span>${appName}</span>
						</div>
						<span class="node-id">${cell.id}</span>
					</div>
                    <div class="node-actions html-shape-immovable">
                        <button class="btn btn-light btn-sm me-2" data-action="configure-flow-app">
                            <i class="fa-regular fa-gear"></i>
                        </button>
                        <button class="btn btn-danger btn-sm" data-action="delete-node">
                            <i class="fa-regular fa-trash"></i>
                        </button>
                    </div>
                </div>
                <div class="script-node-content">
                    <div class="card bg-dark border-secondary">
                        <div class="card-body p-2 d-flex align-items-center justify-content-between">
                            <span class="small text-muted">Action:</span>
                            ${actionLabel}
                        </div>
                    </div>
                    ${data.integrationId ? `<div class="mt-1 text-end"><span class="badge bg-success bg-opacity-10 text-success" style="font-size: 0.65rem;"><i class="fa-solid fa-link me-1"></i>Connected</span></div>` : ''}
                </div>
            `;

			return div;
		},
	});
}

function initializeScriptGraph(isNew = true) {
	const container = $("#script-graph")[0];

	return resizeScriptGraphCSS((graphSize) => {
		// Set Default Shape Attributes
		X6.Shape.Edge.defaults.attrs.line.stroke = "#fff";
		X6.Shape.Edge.defaults.attrs.line.sourceMarker = "classic";
		X6.Shape.Edge.defaults.attrs.line.targetMarker = "classic";

		// Create the graph instance
		const graph = new X6.Graph({
			container: container,
			width: graphSize.width,
			height: graphSize.height,

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
				maxScale: 16,
				minScale: 0.01,
			},
			scaling: {
				min: 0.01,
				max: 16
			},
			panning: {
				enabled: true,
				modifiers: [],
			},
			connecting: {
				connector: {
					name: "rounded",
					args: {
						radius: 20,
					},
				},
				allowBlank: false,
				allowLoop: false,
				allowNode: false,
				allowEdge: false,
				allowMulti: false,
				highlight: true,
				router: {
					name: "orth",
					args: {
						padding: 4,
					},
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
					if (outputCell.shape === SCRIPT_NODE_TYPES.START && inputCell.shape === SCRIPT_NODE_TYPES.AI_RESPONSE) {
						return false;
					}

					// ai response node can only connect to user query node
					if (outputCell.shape === SCRIPT_NODE_TYPES.AI_RESPONSE && inputCell.shape !== SCRIPT_NODE_TYPES.USER_QUERY) {
						return false;
					}

					// validate if source already has connected nodes
					let validateNoDiffOuputTypes = false;
					CurrentScriptGraph.getEdges().forEach((edge) => {
						const letEdgeSource = edge.getSource();

						if (letEdgeSource.cell === outputCell.id && letEdgeSource.port === outputPort) {
							const letEdgeTarget = edge.getTarget();

							if (letEdgeTarget.cell) {
								letEdgeTargetCell = CurrentScriptGraph.getCellById(letEdgeTarget.cell);

								// if atleast one user query is connected, then no other node type can be connected
								if (letEdgeTargetCell.shape === SCRIPT_NODE_TYPES.USER_QUERY && inputCell.shape !== SCRIPT_NODE_TYPES.USER_QUERY) {
									validateNoDiffOuputTypes = true;
								}

								// if one custom tool/system tool/ai response is connected, then no other type and no more nodes can be connected
								if (
									(letEdgeTargetCell.shape === SCRIPT_NODE_TYPES.CUSTOM_TOOL ||
										letEdgeTargetCell.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL ||
										letEdgeTargetCell.shape === SCRIPT_NODE_TYPES.AI_RESPONSE) &&
									letEdgeTargetCell.shape !== inputCell.shape
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
			// Prevent node text selection
			preventDefaultContextMenu: ({ view, event }) => {
				if (!view || view == null) {
					return true;
				}

				// if event.target is textarea, then reutrn flase
				if ($(event.target).is("textarea") || $(event.target).is("input")) {
					return false;
				}

				return true;
			}
		});

		// Add minimap plugin
		const minimapContainer = document.getElementById("script-graph-minimap");
		const enableMinimap = true;
		if (minimapContainer && enableMinimap) {
			setTimeout(() => {
				graph.use(
					new SCRIPT_GRAPH_PLUGINS.Minimap({
						container: minimapContainer,
						width: 180,
						height: 150,
						scalable: false,
						padding: 0,
						graphOptions: {
							width: 180,
							height: 150,
							autoResize: true,
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
							}
						}
					}),
				);
			}, 2000);
		}

		// Add keyboard shortcuts plugin
		if (SCRIPT_GRAPH_PLUGINS.Keyboard) {
			graph.use(
				new SCRIPT_GRAPH_PLUGINS.Keyboard({
					enabled: true,
					global: true,
				}),
			);
		}

		// Add clipboard plugin
		if (SCRIPT_GRAPH_PLUGINS.Clipboard) {
			graph.use(
				new SCRIPT_GRAPH_PLUGINS.Clipboard({
					enabled: true,
				}),
			);
		}

		// Add history plugin (undo/redo)
		if (SCRIPT_GRAPH_PLUGINS.History) {
			CurrentScriptGraphHistory = new SCRIPT_GRAPH_PLUGINS.History({
				enabled: true,
				beforeAddCommand: (event, args) => {
					// Validate before adding to history
					return true;
				},
			});
			graph.use(CurrentScriptGraphHistory);
		}

		// Add selection plugin
		if (SCRIPT_GRAPH_PLUGINS.Selection) {
			CurrentScriptGraphSelection = new SCRIPT_GRAPH_PLUGINS.Selection({
				enabled: true,
				modifiers: ["ctrl"],
				rubberband: true,
				multiple: true,
				movable: true,
				showNodeSelectionBox: true,
				eventTypes: ["leftMouseDown"],
			});
			graph.use(CurrentScriptGraphSelection);
		}

		// DND Plugin
		if (SCRIPT_GRAPH_PLUGINS.Dnd) {
			CurrentScriptGraphDnd = new SCRIPT_GRAPH_PLUGINS.Dnd({
				target: graph,
				scaled: false, // Don't scale the node based on zoom while dragging (keeps it readable)
				dndContainer: document.querySelector('.script-graph-sidebar-left'),
				getDragNode: (node) => {
					return node.clone({ keepId: true }) // Ghost node
				}, 
				getDropNode: (node) => {
					return node.clone({ keepId: true }) // Final node
				}, 
			});
		}

		// Add start node if new graph
		if (isNew) {
			const CurrentScriptGraphStartNode = graph.addNode({
				id: "start_node",
				shape: SCRIPT_NODE_TYPES.START,
				data: { type: SCRIPT_NODE_TYPES.START },
				x: graphSize.width / 2,
				y: graphSize.height / 5,
				ports: {
					items: [{ group: "output" }],
				},
			});
			ManageCurrentScriptData.nodes.push({
				id: CurrentScriptGraphStartNode.id,
				type: SCRIPT_NODE_TYPES.START,
				position: CurrentScriptGraphStartNode.getPosition(),
			});
		}

		// Event Listeners
		graph.on("scale", ({ sx, sy }) => {
			const scale = sx;
			$("#script-graph-zoom-in").prop("disabled", scale >= 16);
			$("#script-graph-zoom-out").prop("disabled", scale <= 0.01);
		});

		graph.on("history:change", (event) => {
			$("#script-graph-undo").prop("disabled", !CurrentScriptGraphHistory.canUndo());
			$("#script-graph-redo").prop("disabled", !CurrentScriptGraphHistory.canRedo());
		});

		graph.on("cell:click", ({ cell, e }) => {
			const target = e.target;
			if (target.closest('[data-action="delete-node"]')) {
				const nodeType = cell.getData()?.type;
				if (nodeType === SCRIPT_NODE_TYPES.START) {
					return;
				}

				if (CurrentCanvasConfigCell !== null && cell.id === CurrentCanvasConfigCell.id) {
					nodeConfigOffcanvas.hide();
				}

				cell.remove();
			}
		});

		graph.on("edge:mouseenter", (event) => {
			event.edge.setAttrs({ line: { strokeDasharray: 5, style: "animation: ant-line 30s infinite linear" } });
		});

		graph.on("edge:mouseleave", (event) => {
			event.edge.setAttrs({ line: { strokeDasharray: 0, style: {} } });
		});

		graph.on("cell:click", ({ cell, e }) => {
			if ($(e.target).is("textarea") || $(e.target).is("input") || $(e.target).is("select") || $(e.target).is("button")) {
				CurrentScriptGraphSelection.clean();
				return;
			}

			if (CurrentScriptGraphSelection.getSelectedCellCount() === 1) {
				if (CurrentScriptGraphSelection.getSelectedCells()[0].id === cell.id) {
					return;
				}
			}

			CurrentScriptGraphSelection.clean();

			console.log(cell);
		});

		graph.on("edge:added", (event) => {
			console.log("edge:added", event);

			// Add Remove Button Tool
			event.edge.addTools({
				name: "button-remove",
				args: { distance: "50%" },
			});
		});

		graph.on("edge:connected", (event) => {
			console.log("edge:connected", event);
		});

		graph.on("blank:click", () => {
			nodeConfigOffcanvas.hide();
		});

		CurrentScriptGraph = graph;
	});
}

function resizeScriptGraphCSS(callback, isFullscreen = false) {
	let currentInnerContentHeight;
	let currentInnerContentWidth;

	if (isFullscreen) {
		currentInnerContentHeight = window.innerHeight;
		currentInnerContentWidth = window.innerWidth;
	} else {
		currentInnerContentHeight = scriptTab.find(".inner-container")[0].clientHeight;
		currentInnerContentWidth = scriptTab.find(".inner-container")[0].clientWidth;
	}

	$("#script-graph").css("height", `${currentInnerContentHeight}px`);

	callback({ width: currentInnerContentWidth, height: currentInnerContentHeight });
}

function adjustScriptGraphMultilanguageDropdownForFullscreen(isFullscreen) {
	const languageDropdown = $("#scriptsManagerMultiLanguageContainer");

	if (isFullscreen) {
		languageDropdown.prependTo("#scriptsManagerMultiLanguageParentSideControlContainer");
	} else {
		languageDropdown.appendTo("#scriptsManagerMultiLanguageParentContainer");
	}
}

// Nodes
function createNodeFromPreset(presetId, graph) {
	// Handle Dynamic FlowApps
	if (presetId.startsWith("flowapp-")) {
		const appKey = presetId.replace("flowapp-", "");
		const appDef = SpecificationFlowAppsListData.find(a => a.appKey === appKey);

		if (!appDef) {
			console.error("FlowApp definition not found:", appKey);
			return null;
		}

		// Initialize Multi-Language Speaking Field
		const speakingBeforeExecution = {};
		BusinessFullData.businessData.languages.forEach(l => speakingBeforeExecution[l.id] = "");

		const nodeData = {
			type: SCRIPT_NODE_TYPES.FLOW_APP,
			appKey: appKey,
			actionKey: null, // User must select this later
			integrationId: null,
			speakingBeforeExecution: speakingBeforeExecution,
			inputs: [] // List of { key, value, isAiGenerated, isRedacted }
		};

		return graph.createNode({
			shape: SCRIPT_NODE_TYPES.FLOW_APP,
			width: SCRIPT_NODE_WIDTH,
			height: 135, // Slightly taller to show Action Name
			data: nodeData,
			ports: {} // Ports will be added later when action is selected
		});
	}

	const preset = NODE_PRESETS[presetId];
	if (!preset) {
		console.error("Unknown node preset:", presetId);
		return null;
	}

	// Base Data Structure
	const nodeData = {
		type: preset.shape
	};

	// 1. Initialize Multi-Language Fields
	if (preset.shape === SCRIPT_NODE_TYPES.USER_QUERY) {
		nodeData.query = {};
		nodeData.examples = {};
		BusinessFullData.businessData.languages.forEach(l => {
			nodeData.query[l.id] = "";
			nodeData.examples[l.id] = [];
		});
	}
	else if (preset.shape === SCRIPT_NODE_TYPES.AI_RESPONSE) {
		nodeData.response = {};
		nodeData.examples = {};
		BusinessFullData.businessData.languages.forEach(l => {
			nodeData.response[l.id] = "";
			nodeData.examples[l.id] = [];
		});
	}
	else if (preset.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL) {
		nodeData.toolType = preset.toolType;
		nodeData.config = JSON.parse(JSON.stringify(preset.config)); // Deep copy

		// Specific multi-lang inits for System Tools
		if (preset.toolType === SCRIPT_SYSTEM_TOOLS.SEND_SMS) {
			nodeData.config.messages = {};
			BusinessFullData.businessData.languages.forEach(l => nodeData.config.messages[l.id] = "");
		}
		else if (preset.toolType === SCRIPT_SYSTEM_TOOLS.RETRIEVE_KNOWLEDGEBASE) {
			nodeData.config.responseBeforeExecution = {};
			BusinessFullData.businessData.languages.forEach(l => nodeData.config.responseBeforeExecution[l.id] = "");
		}
		else if (preset.toolType === SCRIPT_SYSTEM_TOOLS.END_CALL) {
			// If type is with message (not default but possible in future presets)
			if (nodeData.config.type === SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.WITH_MESSAGE) {
				nodeData.config.messages = {};
				BusinessFullData.businessData.languages.forEach(l => nodeData.config.messages[l.id] = "");
			}
		}
	}
	else if (preset.shape === SCRIPT_NODE_TYPES.CUSTOM_TOOL) {
		nodeData.toolId = null;
		nodeData.config = {};
	}

	// 2. Create the Node Definition
	// We use the same size constants as the HTML shapes
	let width = SCRIPT_NODE_WIDTH;
	let height = SCRIPT_NODE_MIN_HEIGHT;

	if (preset.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL || preset.shape === SCRIPT_NODE_TYPES.CUSTOM_TOOL) {
		height = 135;
	}

	const node = graph.createNode({
		shape: preset.shape, // e.g. 2, 3, 4
		width: width,
		height: height,
		data: nodeData,
		ports: {
			items: preset.defaultPorts || []
		}
	});

	return node;
}

// Script User Query Node
function addUserQueryNode(graph, x = 100, y = 200) {
	const queryData = {};
	const examplesData = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		queryData[language] = "";
		examplesData[language] = [];
	});

	return graph.addNode({
		shape: SCRIPT_NODE_TYPES.USER_QUERY,
		data: {
			type: SCRIPT_NODE_TYPES.USER_QUERY,
			query: queryData,
			examples: examplesData,
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
	const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

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
	const responseData = {};
	const examplesData = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		responseData[language] = "";
		examplesData[language] = [];
	});

	return graph.addNode({
		shape: SCRIPT_NODE_TYPES.AI_RESPONSE,
		data: {
			type: SCRIPT_NODE_TYPES.AI_RESPONSE,
			response: responseData,
			examples: examplesData,
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
	const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

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
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
		data: {
			type: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
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
	const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

	return getScriptSystemToolConfig(cell.id, data.toolType, currentLanguage, data);
}

function getScriptSystemToolConfig(cellId, toolType, currentLanguage, data = {}) {
	const config = data.config || {};

	if (toolType === SCRIPT_SYSTEM_TOOLS.END_CALL) {
		return `
                <div class="tool-config-group">
                    <label class="form-label">End Call Configuration</label>
                    <div class="mb-3">
                        <select class="form-select" data-input="end-call-type">
                            <option value="${SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.IMMEDIATE}" ${config.type === SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.IMMEDIATE ? "selected" : ""}>End Immediately</option>
                            <option value="${SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.WITH_MESSAGE}" ${config.type === SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.WITH_MESSAGE ? "selected" : ""}>End with Message</option>
                        </select>
                    </div>
                    <div class="${config.type === SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.IMMEDIATE ? "d-none" : ""}" id="end-call-message-container">
						<label class="form-label btn-ic-span-align"><span>End Call Message</span> <i class="fa-regular fa-language"></i></label>
						<textarea 
							class="form-control" 
							data-input="end-call-message"
							placeholder="Enter end call message..."
							rows="2"
						>${config.messages?.[currentLanguage] || ""}</textarea>
					</div>
                </div>
            `;
	}

	// send sms
	if (toolType === SCRIPT_SYSTEM_TOOLS.SEND_SMS) {
		const phoneNumbers = BusinessFullData.businessApp.numbers || [];
		const numberOptions = phoneNumbers
			.map((number) => {
				const numberName = `${number.countryCode}-${number.number}`;
				return `<option value="${number.id}" ${config.phoneNumberId === number.id ? "selected" : ""}>${numberName}</option>`;
			})
			.join("");

		return `
                <div class="tool-config-group">
                    <label class="form-label">Send SMS Configuration</label>
					<div class="mb-2">
                        <select class="form-select" data-input="send-sms-number">
                            <option value="">Select Number</option>
                            ${numberOptions}
                        </select>
                    </div>
                    <div class="mb-2">
                        <label class="form-label small">Message</label>
                        <textarea 
                            class="form-control" 
                            data-input="send-sms-message"
                            placeholder="Enter send sms message..."
                            rows="2"
                        >${config.messages?.[currentLanguage] || ""}</textarea>
                    </div>
                </div>
            `;
	}

	// go to node
	if (toolType === SCRIPT_SYSTEM_TOOLS.GOTONODE) {
		// select with all the nodes in the graph todo gotonode
		const nodes = CurrentScriptGraph.getNodes();
		const nodesOptions = nodes
			.map((node) => {
				if (node.id == cellId) return "";
				if (node.shape === SCRIPT_NODE_TYPES.SYSTEM_TOOL && node.getData().toolType === SCRIPT_SYSTEM_TOOLS.GOTONODE) return "";
				return `<option value="${node.id}" ${config.goToNodeId === node.id ? "selected" : ""}>${node.id}</option>`;
			})
			.join("");

		return `
			<div class="tool-config-group">
				<label>Go To Node Configuration</label>
				<div>
					<select class="form-select" data-input="go-to-node">
						<option value="">Select Node</option>
						${nodesOptions}
                    </select>
				</div>
			</div>
		`;
	}

	// get dtmf
	if (toolType === SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT) {
		return `
                <div class="tool-config-group">
                    <label class="form-label">DTMF Input Configuration</label>
                    <div class="mb-2">
                        <label class="form-label small">Timeout (milliseconds)</label>
                        <input 
                            type="number" 
                            class="form-control" 
                            data-input="dtmf-timeout"
                            value="${config.silencetimeout || 5000}"
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
                    ${config.encryptInput
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
                        <label class="form-label small btn-ic-span-align"><span>DTMF Outcomes</span> <i class="fa-regular fa-language"></i></label>
                        <div data-container="dtmf-outcomes">
                            ${(config.outcomes || [])
				.map(
					(outcome, index) => `
                                <div class="input-group mb-2" data-outcome-port-id="${outcome.portId}">
                                    <input 
                                        type="text" 
                                        class="form-control" 
                                        placeholder="DTMF Value"
                                        value="${outcome.value[currentLanguage] || ""}"
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

	// transfer to agent
	if (toolType === SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_AGENT) {
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
                    ${config.transferContext
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

	// add script to conext
	if (toolType === SCRIPT_SYSTEM_TOOLS.ADD_SCRIPT_TO_CONTEXT) {
		const scripts = BusinessFullData.businessApp.scripts;

		const scriptOptions = scripts
			.map((script) => {
				if (ManageCurrentScriptData?.id && ManageCurrentScriptData.id === script.id) return;
				const scriptName = script.general.name[currentLanguage] || script.general.name["en-us"] || "Unnamed Script";
				return `<option value="${script.id}" ${config.scriptId === script.id ? "selected" : ""}>${scriptName}</option>`;
			})
			.join("");

		return `
				<div class="tool-config-group">
					<label class="form-label">Add Script Configuration</label>
					<div class="mb-2">
						<select class="form-select" data-input="add-script">
							<option value="" disabled ${config.scriptId ? "" : "selected"}>Select Script</option>
							${scriptOptions}
						</select>
					</div>
				</div>
			`;
	}

	// Retrieve Knowledgebase
	if (toolType === SCRIPT_SYSTEM_TOOLS.RETRIEVE_KNOWLEDGEBASE) {
		return `
                <div class="tool-config-group">
                    <label class="form-label">Retrieve KnowledgeBase Configuration</label>
                    <div id="retrieve-knowledgebase-response-before-execution-container">
						<label class="form-label btn-ic-span-align"><span>Response Before Execution</span> <i class="fa-regular fa-language"></i></label>
						<textarea 
							class="form-control" 
							data-input="retrieve-knowledgebase-response-before-execution"
							placeholder="Enter response before execution..."
							rows="2"
						>${config.responseBeforeExecution?.[currentLanguage] || ""}</textarea>
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
		case SCRIPT_SYSTEM_TOOLS.END_CALL:
		case SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_AGENT:
		case SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_HUMAN:
		case SCRIPT_SYSTEM_TOOLS.GOTONODE:
			// These are end nodes, no output ports needed
			break;

		case SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT: {
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

		case SCRIPT_SYSTEM_TOOLS.SEND_SMS: {
			cell.addPort({
				group: "output",
				id: "error",
				attrs: {
					circle: {
						fill: "#ffc107",
					},
					text: {
						text: "Error",
					},
					label: {
						position: "bottom",
					},
				},
			});
			cell.addPort({
				group: "output",
				id: "success",
				attrs: {
					text: {
						text: "Success",
					},
					label: {
						position: "bottom",
					}
				}
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
		data.config = newConfig;
		CurrentCanvasConfigCell.replaceData(data);
	}
}

function doesScriptSystemToolRequireConfig(toolType) {
	return (
		toolType &&
		(
			toolType === SCRIPT_SYSTEM_TOOLS.END_CALL ||
			toolType === SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT ||
			toolType === SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_AGENT ||
			toolType === SCRIPT_SYSTEM_TOOLS.ADD_SCRIPT_TO_CONTEXT ||
			toolType === SCRIPT_SYSTEM_TOOLS.SEND_SMS ||
			toolType === SCRIPT_SYSTEM_TOOLS.GOTONODE ||
			toolType === SCRIPT_SYSTEM_TOOLS.RETRIEVE_KNOWLEDGEBASE
		)
	);
}

// Script Custom Tool Node
function addCustomToolNode(graph, x = 100, y = 200) {
	return graph.addNode({
		shape: SCRIPT_NODE_TYPES.CUSTOM_TOOL,
		data: {
			type: SCRIPT_NODE_TYPES.CUSTOM_TOOL,
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
	const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

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
		data.config = config;
		CurrentCanvasConfigCell.replaceData(data);
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

// Script FlowApps
function renderFlowAppsSidebar() {
	const container = $("#sidebarIntegrationsGrid");

	// Remove existing dynamic items (if any re-init happens) to prevent duplicates
	container.find('.sidebar-node-item[data-preset-id^="flowapp-"]').remove();

	if (!SpecificationFlowAppsListData || SpecificationFlowAppsListData.length === 0) return;

	SpecificationFlowAppsListData.forEach(app => {
		if (app.isDisabled) return; // Skip disabled apps if we want to hide them, or show with lock icon

		// Use a generic icon if URL is missing/broken, otherwise use the provided URL
		const iconHtml = app.iconUrl
			? `<img src="${app.iconUrl}">`
			: `<i class="fa-regular fa-plug"></i>`;

		const itemHtml = `
            <div class="sidebar-node-item" data-preset-id="flowapp-${app.appKey}">
                ${iconHtml}
                <span>${app.name}</span>
            </div>
        `;

		container.append(itemHtml);
	});
}

function generateFlowAppConfig(cell) {
	const data = cell.getData() || {};
	const appKey = data.appKey;
	const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

	// 1. Find Definition
	const appDef = SpecificationFlowAppsListData.find(a => a.appKey === appKey);
	if (!appDef) return `<div class="alert alert-danger">App Definition not found for ${appKey}</div>`;

	// 2. Action Selector
	const actionsOptions = appDef.actions
		.filter(a => !a.isDisabled) // Filter out disabled actions
		.map(action => `
            <option value="${action.actionKey}" ${data.actionKey === action.actionKey ? "selected" : ""}>
                ${action.name}
            </option>
        `).join("");

	let actionSection = `
        <div class="tool-config-group mb-4">
            <label class="form-label fw-bold">Select Action</label>
            <select class="form-select" data-input="flow-app-action">
                <option value="" disabled ${!data.actionKey ? "selected" : ""}>Choose an action...</option>
                ${actionsOptions}
            </select>
            ${data.actionKey ? `<div class="form-text text-muted small mt-1">${appDef.actions.find(a => a.actionKey === data.actionKey)?.description || ""}</div>` : ""}
        </div>
    `;

	// If no action selected, stop here
	if (!data.actionKey) {
		return actionSection + `<div class="text-center text-muted fst-italic mt-5">Please select an action to configure.</div>`;
	}

	const selectedActionDef = appDef.actions.find(a => a.actionKey === data.actionKey);

	// 3. Integration Selector (If required)
	let integrationSection = "";
	if (selectedActionDef.requiresIntegration && appDef.integrationType) {
		// Filter business integrations that match the App's type (e.g. "CalCom")
		const validIntegrations = BusinessFullData.businessApp.integrations.filter(
			i => i.type === appDef.integrationType
		);

		const integrationOptions = validIntegrations.map(integ => `
            <option value="${integ.id}" ${data.integrationId === integ.id ? "selected" : ""}>
                ${integ.friendlyName}
            </option>
        `).join("");

		integrationSection = `
            <div class="tool-config-group mb-4">
                <label class="form-label fw-bold">Integration Connection <span class="text-danger">*</span></label>
                <select class="form-select" data-input="flow-app-integration">
                    <option value="" disabled ${!data.integrationId ? "selected" : ""}>Select Account...</option>
                    ${integrationOptions}
                </select>
                ${validIntegrations.length === 0 ? `<div class="alert alert-warning mt-2 small">No ${appDef.name} integrations found. Please add one in the Integrations tab.</div>` : ""}
            </div>
        `;
	}

	// 4. Speaking Before Execution
	const speakingSection = `
        <div class="tool-config-group mb-4">
            <label class="form-label fw-bold btn-ic-span-align">
                <span>Speak Before Execution</span> <i class="fa-regular fa-language"></i>
            </label>
            <textarea 
                class="form-control" 
                data-input="flow-app-speaking"
                placeholder="e.g., Let me check that for you..."
                rows="2"
            >${data.speakingBeforeExecution?.[currentLanguage] || ""}</textarea>
            <div class="form-text text-muted small">Optional audio filler while the tool runs.</div>
        </div>
    `;

	// 5. Inputs Container (Schema Renderer)
	const inputsSection = `
        <div class="tool-config-group">
            <div class="d-flex justify-content-between align-items-center mb-2">
                <label class="form-label fw-bold mb-0">Inputs</label>
                <!-- Optional: Test Button could go here later -->
            </div>
            <div id="flowAppSchemaContainer" class="border rounded p-3 bg-darker">
                <!-- Schema Renderer will inject form here -->
                <div class="text-center"><i class="fa-solid fa-spinner fa-spin text-muted"></i></div>
            </div>
        </div>
    `;

	return actionSection + integrationSection + speakingSection + inputsSection;
}

function updateFlowAppConfig(partialData) {
	if (CurrentCanvasConfigCell) {
		const currentData = CurrentCanvasConfigCell.getData();
		CurrentCanvasConfigCell.replaceData({ ...currentData, ...partialData });
	}
}

function UpdateFlowAppNodePorts(cell, actionDef) {
	if (!actionDef) return;

	const portsToAdd = actionDef.outputPorts || [];

	// 1. Remove existing OUTPUT ports
	const currentPorts = cell.getPorts();
	const outputPorts = currentPorts.filter(p => p.group === 'output');
	outputPorts.forEach(p => cell.removePort(p.id));

	// 2. Add New Ports
	portsToAdd.forEach(portDef => {
		// Style specific ports (Success/Error)
		let attrs = { circle: { fill: "#fff" }, text: { text: portDef.label } };

		if (portDef.key === 'error') attrs.circle.fill = '#dc3545'; // Red
		if (portDef.key === 'success') attrs.circle.fill = '#198754'; // Green
		if (portDef.key === 'timeout' || portDef.key === 'not_found') attrs.circle.fill = '#ffc107'; // Yellow

		cell.addPort({
			group: 'output',
			id: portDef.key, // Use Key as ID (e.g. "success")
			attrs: attrs,
			label: { position: "bottom" }
		});
	});
}

function renderFlowAppSchemaInputs() {
	const container = $("#flowAppSchemaContainer");

	if (!CurrentCanvasConfigCell) {
		container.html('<div class="text-muted small">Select a node first.</div>');
		return;
	}

	const data = CurrentCanvasConfigCell.getData();
	if (!data.actionKey) {
		container.html('<div class="text-muted small text-center">Select an action above to configure inputs.</div>');
		return;
	}

	const appDef = SpecificationFlowAppsListData.find(a => a.appKey === data.appKey);
	const actionDef = appDef.actions.find(a => a.actionKey === data.actionKey);

	let schema;
	try {
		schema = JSON.parse(actionDef.inputSchemaJson);
	} catch (e) {
		container.html('<div class="alert alert-danger small">Invalid Schema JSON</div>');
		return;
	}

	let html = "";

	// --- 1. Handle Polymorphism (oneOf) ---
	let activeProperties = schema.properties || {};
	let requiredFields = schema.required || [];

	if (schema.oneOf && schema.oneOf.length > 0) {
		// Get saved selection or default to 0
		const selectedIndex = data.uiState?.oneOfSelection || 0;

		// Render Selector
		const options = schema.oneOf.map((opt, idx) =>
			`<option value="${idx}" ${idx === selectedIndex ? "selected" : ""}>${opt.title || `Option ${idx + 1}`}</option>`
		).join("");

		html += `
            <div class="mb-3 border-bottom border-secondary pb-3">
                <label class="form-label small text-info">Input Mode</label>
                <select class="form-select form-select-sm" data-action="flowapp-oneof-change">
                    ${options}
                </select>
            </div>
        `;

		// Merge Sub-Schema Properties
		const subSchema = schema.oneOf[selectedIndex];
		activeProperties = { ...activeProperties, ...subSchema.properties };
		if (subSchema.required) {
			requiredFields = [...requiredFields, ...subSchema.required];
		}
	}

	// --- 2. Render Fields ---
	const inputsMap = convertInputsArrayToMap(data.inputs); // Helper to find current values

	if (Object.keys(activeProperties).length === 0) {
		html += '<div class="text-muted small fst-italic">No inputs required for this action.</div>';
	} else {
		Object.keys(activeProperties).forEach(key => {
			const propSchema = activeProperties[key];
			const isRequired = requiredFields.includes(key);
			const currentValue = inputsMap[key]; // { value, isAiGenerated, isRedacted } or undefined

			html += renderSchemaField(key, propSchema, currentValue, isRequired, data.appKey);
		});
	}

	container.html(html);

	// Trigger Fetchers for dropdowns
	triggerFlowAppFetchers(activeProperties, data);
}

function renderSchemaField(key, schema, currentInput, isRequired, appKey) {
	const label = schema.title || key;
	const description = schema.description || "";

	// Defaults
	const val = currentInput?.value ?? (schema.default || "");
	const isAi = currentInput?.isAiGenerated || false;
	const isRedacted = currentInput?.isRedacted || false;

	// --- Control Type Logic ---
	let inputControl = "";

	// A. Fetcher (Dynamic Dropdown)
	if (schema["x-fetcher"]) {
		inputControl = `
            <select class="form-select form-select-sm" 
                data-input-key="${key}" 
                data-fetcher="${schema["x-fetcher"]}"
                data-dependent-on='${JSON.stringify(schema["x-fetcher-dependent-on"] || [])}'
                ${isAi ? "disabled style='display:none'" : ""}
            >
                <option value="${val}" selected>${val || "Loading..."}</option>
            </select>
        `;
	}
	// B. Static Enum
	else if (schema.enum) {
		const opts = schema.enum.map(opt => `<option value="${opt}" ${val === opt ? "selected" : ""}>${opt}</option>`).join("");
		inputControl = `<select class="form-select form-select-sm" data-input-key="${key}" ${isAi ? "disabled style='display:none'" : ""}>${opts}</select>`;
	}
	// C. Boolean
	else if (schema.type === "boolean") {
		inputControl = `
            <div class="form-check form-switch" ${isAi ? "style='display:none'" : ""}>
                <input class="form-check-input" type="checkbox" data-input-key="${key}" ${val === true ? "checked" : ""}>
            </div>
        `;
	}
	// D. Standard Text/Number (Default)
	else {
		inputControl = `
            <input type="${schema.type === 'integer' || schema.type === 'number' ? 'text' : 'text'}" 
                class="form-control form-control-sm" 
                data-input-key="${key}" 
                value="${val}" 
                placeholder="${schema.placeholder || ''}"
                ${isAi ? "style='display:none'" : ""} 
            />
        `;
	}

	// --- AI Generated Overlay ---
	const aiOverlay = `
        <div class="ai-generated-placeholder ${isAi ? "" : "d-none"} border border-info rounded p-2 bg-dark d-flex justify-content-between align-items-center">
            <span class="small text-info"><i class="fa-regular fa-sparkles me-1"></i>AI Generated</span>
            <div class="form-check form-check-inline m-0">
                <input class="form-check-input" type="checkbox" data-redact-key="${key}" ${isRedacted ? "checked" : ""}>
                <label class="form-check-label small text-muted">Redact</label>
            </div>
        </div>
    `;

	return `
        <div class="mb-3 input-field-group" data-field-key="${key}">
            <div class="d-flex justify-content-between mb-1">
                <label class="form-label small mb-0">${label} ${isRequired ? '<span class="text-danger">*</span>' : ''}</label>
                <div class="form-check form-switch min-h-0 mb-0">
                    <input class="form-check-input" type="checkbox" data-ai-toggle="${key}" ${isAi ? "checked" : ""} title="Generated by AI">
                </div>
            </div>
            ${inputControl}
            ${aiOverlay}
            ${description ? `<div class="form-text small mt-0">${description}</div>` : ""}
        </div>
    `;
}

function initFlowAppSchemaHandlers() {

	// 1. OneOf Mode Change
	$("#nodeConfigOffcanvas").on("change", '[data-action="flowapp-oneof-change"]', (e) => {
		const idx = parseInt(e.target.value);

		// Save UI State
		const data = CurrentCanvasConfigCell.getData();
		const uiState = data.uiState || {};
		uiState.oneOfSelection = idx;

		// Reset Inputs (Schema changed, old inputs might be invalid)
		// Optionally logic could try to keep overlapping keys
		// For now, let's keep inputs but they might just be hidden

		updateFlowAppConfig({ uiState });
		renderFlowAppSchemaInputs(); // Re-render form
	});

	// 2. Input Change (Text, Select, Boolean)
	$("#nodeConfigOffcanvas").on("input change", '[data-input-key]', (e) => {
		const key = $(e.target).data("input-key");
		let value;

		if ($(e.target).attr("type") === "checkbox") {
			value = $(e.target).is(":checked");
		} else {
			value = $(e.target).val();
		}

		saveFlowAppInput(key, { value });

		// Trigger dependencies update if this field drives others
		checkFetcherDependencies(key);
	});

	// 3. AI Toggle Change
	$("#nodeConfigOffcanvas").on("change", '[data-ai-toggle]', (e) => {
		const key = $(e.target).data("ai-toggle");
		const isAi = $(e.target).is(":checked");

		saveFlowAppInput(key, { isAiGenerated: isAi });
		renderFlowAppSchemaInputs(); // Re-render to show/hide overlay
	});

	// 4. Redact Change
	$("#nodeConfigOffcanvas").on("change", '[data-redact-key]', (e) => {
		const key = $(e.target).data("redact-key");
		const isRedacted = $(e.target).is(":checked");

		saveFlowAppInput(key, { isRedacted: isRedacted });
	});
}

// Helper to save partial input updates to the Node Data List
function saveFlowAppInput(key, updates) {
	if (!CurrentCanvasConfigCell) return;
	const data = CurrentCanvasConfigCell.getData();
	let inputs = data.inputs || [];

	let inputObj = inputs.find(i => i.key === key);
	if (!inputObj) {
		inputObj = { key: key, value: "", isAiGenerated: false, isRedacted: false };
		inputs.push(inputObj);
	}

	// Merge updates
	if (updates.value !== undefined) inputObj.value = updates.value;
	if (updates.isAiGenerated !== undefined) inputObj.isAiGenerated = updates.isAiGenerated;
	if (updates.isRedacted !== undefined) inputObj.isRedacted = updates.isRedacted;

	CurrentCanvasConfigCell.replaceData({ ...data, inputs });
}

function convertInputsArrayToMap(inputsArr) {
	const map = {};
	if (inputsArr) {
		inputsArr.forEach(i => map[i.key] = i);
	}
	return map;
}

async function triggerFlowAppFetchers(activeProperties, nodeData) {
	// Loop through all rendered fetcher selects
	$('[data-fetcher]').each(async function () {
		const selectEl = $(this);
		const key = selectEl.data("input-key");
		const fetcherKey = selectEl.data("fetcher");
		const dependencies = selectEl.data("dependent-on"); // Array

		// 1. Check Dependencies
		// We construct a context object from current inputs
		const context = {};
		const inputsMap = convertInputsArrayToMap(nodeData.inputs);

		// If dependencies are missing, show default option and disable
		let dependenciesMet = true;
		if (dependencies && dependencies.length > 0) {
			dependencies.forEach(depKey => {
				const depVal = inputsMap[depKey]?.value;
				if (!depVal || depVal === "") dependenciesMet = false;
				context[depKey] = depVal;
			});
		}

		if (!dependenciesMet && dependencies.length > 0) {
			selectEl.html('<option disabled selected>Select parent first...</option>');
			selectEl.prop("disabled", true);
			return;
		} else {
			selectEl.prop("disabled", false);
		}

		// 2. Loading State
		// Only fetch if we haven't already populated it OR if it's dependent (might need refresh)
		// For simplicity, we fetch when dependencies are met.
		selectEl.prop("disabled", true);
		const originalVal = selectEl.val(); // Keep selected value if possible
		selectEl.html('<option disabled selected>Loading...</option>');

		try {
			// 3. Call API
			// POST /app/user/business/{id}/flowapps/{appKey}/fetchers/{fetcherKey}
			const response = await $.ajax({
				url: `/app/user/business/${CurrentBusinessId}/flowapps/${nodeData.appKey}/fetchers/${fetcherKey}`,
				method: "POST",
				contentType: "application/json",
				data: JSON.stringify({
					integrationId: nodeData.integrationId,
					context: context // Pass current form state
				})
			});

			if (response.success) {
				let optionsHtml = `<option value="" disabled selected>Select...</option>`;
				response.data.forEach(opt => {
					const isSelected = String(opt.value) === String(originalVal) ? "selected" : "";
					optionsHtml += `<option value="${opt.value}" ${isSelected}>${opt.label}</option>`;
				});
				selectEl.html(optionsHtml);
			} else {
				selectEl.html(`<option disabled>Error: ${response.message}</option>`);
			}
		} catch (err) {
			console.error(err);
			selectEl.html('<option disabled>Fetch Error</option>');
		} finally {
			selectEl.prop("disabled", false);
		}
	});
}

function checkFetcherDependencies(changedKey) {
	// Re-trigger fetchers if the changed key is a dependency
	const nodeData = CurrentCanvasConfigCell.getData();
	// Re-render essentially triggers the fetchers logic
	// Optimization: Only trigger specific fetchers that have changedKey in their 'data-dependent-on'
	// But re-rendering logic is safer to keep state consistent
	renderFlowAppSchemaInputs();
}

// Scripts Variables List
function renderVariablesList() {
	const container = $("#variablesListContainer");
	container.empty();

	if (!CurrentScriptVariablesData || CurrentScriptVariablesData.length === 0) {
		container.html(`
            <div class="text-center text-muted mt-5 empty-vars-notice">
                <i class="fa-regular fa-brackets-curly fa-2x mb-2"></i>
                <p>No variables defined.</p>
            </div>
        `);
		return;
	}

	const currentLang = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

	CurrentScriptVariablesData.forEach((v, index) => {
		const typeLabel = v.type === 1 ? "String" : v.type === 2 ? "Number" : "Boolean";

		let badges = "";
		if (v.isVisibleToAgent) badges += '<i class="fa-regular fa-eye text-success me-2" title="Visible to Agent"></i>';
		else badges += '<i class="fa-regular fa-eye-slash text-warning me-2" title="Hidden from Agent"></i>';

		if (v.isEditableByAI) badges += '<i class="fa-regular fa-pen-to-square text-info" title="Editable by AI"></i>';
		else badges += '<i class="fa-regular fa-lock text-secondary" title="Static/Read-only"></i>';

		const itemHtml = `
            <div class="card bg-dark border-secondary mb-2 variable-item" data-index="${index}">
                <div class="card-body p-2">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <span class="fw-bold text-info font-monospace" style="word-break: break-all;max-width: 75%;">{{scriptVariable.${v.key}}}</span>
                        <div>
                            ${badges}
                            <button class="btn btn-link btn-sm text-danger p-0 ms-2 delete-var-btn" style="text-decoration:none;">
                                <i class="fa-solid fa-times"></i>
                            </button>
                        </div>
                    </div>
                    <div class="small text-white mb-1 text-truncate">${v.description[currentLang] || v.description[BusinessDefaultLanguage]}</div>
                    <div class="d-flex justify-content-between small text-muted">
                        <span>${typeLabel}</span>
                        <span>Def: ${v.defaultValue || "<em>null</em>"}</span>
                    </div>
                </div>
            </div>
        `;
		container.append(itemHtml);
	});
}

function initVariablesHandlers() {
	variableOffcanvasEditableInput.on("change", function () {
		const isEditable = $(this).is(":checked");
		if (!isEditable) {
			variableOffcanvasDefaultInput.attr("placeholder", "Value (Required for Static Variables)");
			variableOffcanvasDefaultInput.addClass("border-warning"); // Visual cue
		} else {
			variableOffcanvasDefaultInput.attr("placeholder", "Default Value (Optional)");
			variableOffcanvasDefaultInput.removeClass("border-warning");
		}
	});

	variableOffcanvasDescriptionInput.on("input", (e) => {
		const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;
		NewVariableDescriptionMultiLangData[currentLanguage] = $(e.target).val();
	});

	scriptsManagerLanguageDropdown.onLanguageChange((language) => {
		// Only update if the offcanvas is open
		if (variableOffcanvasElement.hasClass("show")) {
			variableOffcanvasDescriptionInput.val(NewVariableDescriptionMultiLangData[language.id] || "");
		}
	});

	// Open Offcanvas
	$("#script-graph-variables").on("click", () => {
		// Render current list
		renderVariablesList();

		// Reset inputs for new entry
		const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;
		variableOffcanvasDescriptionInput.val(NewVariableDescriptionMultiLangData[currentLanguage] || "");

		variablesOffcanvas.show();
	});

	$("#addVariableBtn").on("click", () => {
		const keyInput = variableOffcanvasElement.find("#newVarKey");
		const typeInput = variableOffcanvasElement.find("#newVarType");
		const visibleInput = variableOffcanvasElement.find("#newVarVisible");

		const key = keyInput.val().trim();
		const type = parseInt(typeInput.val());
		const defaultValue = variableOffcanvasDefaultInput.val().trim();
		const isVisible = visibleInput.is(":checked");
		const isEditable = variableOffcanvasEditableInput.is(":checked");

		// A. Validation: Key
		if (!key) return AlertManager.createAlert({ type: "danger", message: "Variable Key is required.", timeout: 6000 });
		if (!/^[a-zA-Z0-9_]+$/.test(key)) return AlertManager.createAlert({ type: "danger", message: "Key must contain only letters, numbers, and underscores.", timeout: 6000 });

		// B. Validation: Uniqueness
		if (CurrentScriptVariablesData.some(v => v.key === key)) {
			return AlertManager.createAlert({ type: "danger", message: "A variable with this Key already exists.", timeout: 6000 });
		}

		// C. Validation: Description (All Languages)
		let missingLang = null;
		BusinessFullData.businessData.languages.forEach(lang => {
			if (!NewVariableDescriptionMultiLangData[lang.id] || NewVariableDescriptionMultiLangData[lang.id].trim() === "") {
				missingLang = lang.name;
			}
		});

		if (missingLang) {
			return AlertManager.createAlert({ type: "danger", message: `Description is missing for language: ${missingLang}`, timeout: 6000 });
		}

		// D. Validation: Static Variables
		if (!isEditable && (defaultValue === "" || !defaultValue)) {
			return AlertManager.createAlert({
				type: "danger",
				message: "This variable is Read-Only (not editable by AI), so you must provide a Default Value."
			});
		}

		// E. Construct Object
		const newVariable = {
			key: key,
			type: type,
			defaultValue: defaultValue,
			isVisibleToAgent: isVisible,
			isEditableByAI: isEditable,
			description: structuredClone(NewVariableDescriptionMultiLangData)
		};

		// F. Add & Reset
		CurrentScriptVariablesData.push(newVariable);
		renderVariablesList();

		// Reset Form
		keyInput.val("");
		variableOffcanvasDefaultInput.val("");
		visibleInput.prop("checked", true);
		variableOffcanvasEditableInput.prop("checked", false);

		// Reset Description Data & Input
		BusinessFullData.businessData.languages.forEach(l => NewVariableDescriptionMultiLangData[l.id] = "");
		variableOffcanvasDescriptionInput.val("");

		// Trigger UI refresh if needed
		checkScriptTabHasChanges();
	});

	// 5. Delete Variable
	$("#variablesListContainer").on("click", ".delete-var-btn", function () {
		const index = $(this).closest(".variable-item").data("index");
		CurrentScriptVariablesData.splice(index, 1);
		renderVariablesList();
		checkScriptTabHasChanges();
	});
}

// Event Handlers
function initScriptsTabHandlers() {
	initVariablesHandlers();
	initFlowAppSchemaHandlers();

	$(document).on("containerResizeProgress", (e) => {
		if (CurrentScriptGraph == null) return;

		const $graph = $("#script-graph");

		const isFullscreen = $(".script-graph-container").hasClass("fullscreen");
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

	$("#nav-bar").on("tabChange", async (event) => {
		const activeTab = event.detail.from;
		if (activeTab !== "scripts-tab") return;

		if (ManageCurrentScriptType == null) return;

		const canLeaveResult = await canLeaveScriptsManagerTab(" Are you sure you want to discard these changes and leave the scripts tab?");

		if (canLeaveResult) {
			if (ManageCurrentScriptType != null) {
				ManageCurrentScriptType = null;
				switchBackToScriptsListTab.click();
			}
		} else {
			event.preventDefault();
		}
	});

	addNewScriptButton.on("click", (event) => {
		event.preventDefault();

		ManageCurrentScriptData = createDefaultScriptObject();

		ResetAndEmptyScriptsManageTab();
		initializeScriptGraph();

		ManageCurrentScriptType = "new";

		currentScriptName.text("New Script");
		showScriptManagerTab();
	});

	switchBackToScriptsListTab.on("click", async (event) => {
		event.preventDefault();

		if (ManageCurrentScriptType !== null) {
			const canLeave = await canLeaveScriptsManagerTab(" Are you sure you want to discard these changes and leave the scripts manage tab?");
			if (!canLeave) return false;
		}

		ManageCurrentScriptType = null;

		ResetAndEmptyScriptsManageTab();
		showScriptListTab();
	});

	CheckScriptMultiLangInterval = setInterval(() => {
		if (ManageCurrentScriptType === null) return;
		if (IsSavingScriptTab) return;

		validateScriptMultilanguageElements(true);
		validateScriptNodes(true);
		checkScriptTabHasChanges(true, false); // todo remove out of here later
	}, 500);

	scriptsCardListContainer.on("click", '.script-card', (event) => {
		event.preventDefault();
		event.stopPropagation();

		// check if target was button or its icon
		if ($(event.target).closest(".dropdown").length != 0) {
			return;
		}

		const scriptId = $(event.currentTarget).attr("data-item-id");

		ResetAndEmptyScriptsManageTab();
		initializeScriptGraph(false);

		ManageCurrentScriptData = BusinessFullData.businessApp.scripts.find((script) => script.id === scriptId);

		fillScriptManagerTab();

		ManageCurrentScriptType = "edit";

		currentScriptName.text(ManageCurrentScriptData.general.name[BusinessDefaultLanguage]);
		showScriptManagerTab();
	});

	scriptsCardListContainer.on("click", '.script-card span[button-type="delete-script"]', async (event) => {
		event.preventDefault();

		const button = $(event.currentTarget);
		const scriptId = button.attr("data-item-id");
		const scriptIndex = BusinessFullData.businessApp.scripts.findIndex(n => n.id === scriptId);
		if (scriptIndex === -1) return;
		const scriptData = BusinessFullData.businessApp.scripts[scriptIndex];
		if (scriptData == null) return;

		const scriptCard = scriptsCardListContainer.find(`.script-card[data-item-id="${scriptId}"]`);

		if (IsDeletingScriptTab) {
			AlertManager.createAlert({
				type: "warning",
				message: "A delete operation for script is already in progress. Please try again once the operation is complete.",
				timeout: 6000,
			});
			return;
		}

		const confirmDialog = new BootstrapConfirmDialog({
			title: `Delete "${scriptData.general.name[BusinessDefaultLanguage]}" Script`,
			message: "Are you sure you want to delete this script?<br><br><b>Note:</b> You must remove any references (inbound route, telephony/web campaigns, transfer to script node, add to context script node) to this script before deleting and wait or cancel any ongoing call queues or conversations.",
			confirmText: "Delete",
			confirmButtonClass: "btn-danger",
			modalClass: "modal-lg",
		});

		if (await confirmDialog.show()) {
			showHideButtonSpinnerWithDisableEnable(button, true);
			IsDeletingScriptTab = true;
			scriptCard.addClass("disabled");

			DeleteBusinessScript(
				scriptId,
				() => {
					scriptCard.parent().remove()

					BusinessFullData.businessApp.scripts.splice(scriptIndex, 1);

					if (BusinessFullData.businessApp.scripts.length === 0) {
						scriptsCardListContainer.append("<div class='none-script-notice col-12'>No scripts found.</div>");
                    }

					AlertManager.createAlert({
						type: "success",
						message: `Script "${scriptData.general.name[BusinessDefaultLanguage]}" deleted successfully.`,
						timeout: 6000,
					});
				},
				(errorResult) => {
					scriptCard.removeClass("disabled");

					var resultMessage = "Check console logs for more details.";
					if (errorResult && errorResult.message) resultMessage = errorResult.message;

					AlertManager.createAlert({
						type: "danger",
						message: "Error occured while deleting script.",
						resultMessage: resultMessage,
						timeout: 6000,
					});

					console.log("Error occured while deleting script: ", errorResult);
				}
			).always(() => {
				showHideButtonSpinnerWithDisableEnable(button, false);
				IsDeletingScriptTab = false;
			});
		}
	});

	// General Tab
	function initScriptGeneralTabHandlers() {
		// Name
		inputScriptName.on("input", (event) => {
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			CurrentScriptNameMultiLangData[currentLanguage] = $(event.currentTarget).val();
		});

		// Description
		inputScriptDescription.on("input", (event) => {
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			CurrentScriptDescriptionMultiLangData[currentLanguage] = $(event.currentTarget).val();
		});

		scriptsManagerLanguageDropdown.onLanguageChange((language) => {
			const currentLanguage = language.id;

			inputScriptName.val(CurrentScriptNameMultiLangData[currentLanguage]);
			inputScriptDescription.val(CurrentScriptDescriptionMultiLangData[currentLanguage]);
		});
	}
	initScriptGeneralTabHandlers();

	// Nodes
	// click handlers for configuration buttons
	$(document).on("click mousedown mousemove", ".html-shape-immovable, .html-shape-immovable > *", (e) => {
		e.stopImmediatePropagation();
		e.stopPropagation();
	})

	$("#script-graph").on("click", "[data-action^='configure-']", (e) => {
		const closestNode = $(e.target).closest(".x6-node");
		const cellId = closestNode.attr("data-cell-id");
		const cell = CurrentScriptGraph.getCellById(cellId);

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
			case "Flow App":
				configContent.html(generateFlowAppConfig(cell));
				// Trigger Schema Render immediately after opening
				setTimeout(() => renderFlowAppSchemaInputs(), 50);
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
		$("#script-graph").on("input", '[data-input="user-query"]', (e) => {
			const currentElement = $(e.currentTarget);
			const closestNode = currentElement.closest(".x6-node");
			const cellId = closestNode.attr("data-cell-id");

			const cell = CurrentScriptGraph.getCellById(cellId);
			const data = cell.getData() || {};
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			const queries = data.query || {};
			queries[currentLanguage] = currentElement.val();

			cell.replaceData({
				...data,
				query: queries,
			});
		});

		scriptsManagerLanguageDropdown.onLanguageChange((language) => {
			const currentLanguage = language.id;

			CurrentScriptGraph.getCells().forEach((cell) => {
				if (cell.shape === "edge") return;

				const nodeData = cell.getData();

				if (nodeData.type !== SCRIPT_NODE_TYPES.USER_QUERY) {
					return;
				}

				const currentLangQueryData = nodeData.query[currentLanguage];
				$(`g[data-cell-id="${cell.id}"] textarea[data-input="user-query"]`).val(currentLangQueryData);
			});

			if (CurrentCanvasConfigCell && nodeConfigOffcanvas._element.classList.contains("show")) {
				const nodeData = CurrentCanvasConfigCell.getData();

				if (nodeData.type !== SCRIPT_NODE_TYPES.USER_QUERY) {
					return;
				}

				$(`g[data-cell-id="${CurrentCanvasConfigCell.id}"] button[data-action="configure-user-query"]`).click();
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

			const data = CurrentCanvasConfigCell.getData();
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			$(e.currentTarget).closest(".input-group").remove();

			const queryExamples = Array.from($("#userQueryExamplesContainer input"))
				.map((input) => $(input).val().trim())
				.filter((value) => value !== ""); // Remove empty values

			const examples = data.examples || {};
			examples[currentLanguage] = queryExamples;

			CurrentCanvasConfigCell.replaceData({
				...data,
				examples,
			});
		});

		// On Type Query Example
		$("#nodeConfigOffcanvas").on("input", '#userQueryExamplesContainer [data-input="query-example"]', (e) => {
			e.stopPropagation();

			const data = CurrentCanvasConfigCell.getData();
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			const queryExamples = Array.from($("#userQueryExamplesContainer input"))
				.map((input) => $(input).val().trim())
				.filter((value) => value !== ""); // Remove empty values

			const examples = data.examples || {};
			examples[currentLanguage] = queryExamples;

			CurrentCanvasConfigCell.replaceData({
				...data,
				examples,
			});
		});
	}
	initUserQueryConfigHandlers();

	// AI Response Node
	function initializeAIResponseHandlers() {
		// Response text change handler
		$("#script-graph").on("input", '[data-input="ai-response"]', (e) => {
			const currentElement = $(e.currentTarget);
			const closestNode = currentElement.closest(".x6-node");
			const cellId = closestNode.attr("data-cell-id");

			const cell = CurrentScriptGraph.getCellById(cellId);
			const data = cell.getData() || {};
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			const responses = data.response || {};
			responses[currentLanguage] = currentElement.val();

			cell.replaceData({
				...data,
				response: responses,
			});
		});

		scriptsManagerLanguageDropdown.onLanguageChange((language) => {
			const currentLanguage = language.id;

			CurrentScriptGraph.getCells().forEach((cell) => {
				if (cell.shape === "edge") return;

				const nodeData = cell.getData();

				if (nodeData.type !== SCRIPT_NODE_TYPES.AI_RESPONSE) {
					return;
				}

				const currentLangResponseData = nodeData.response[currentLanguage];
				$(`g[data-cell-id="${cell.id}"] textarea[data-input="ai-response"]`).val(currentLangResponseData);
			});

			if (CurrentCanvasConfigCell && nodeConfigOffcanvas._element.classList.contains("show")) {
				const nodeData = CurrentCanvasConfigCell.getData();

				if (nodeData.type !== SCRIPT_NODE_TYPES.AI_RESPONSE) {
					return;
				}

				$(`g[data-cell-id="${CurrentCanvasConfigCell.id}"] button[data-action="configure-ai-response"]`).click();
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

			const data = CurrentCanvasConfigCell.getData();
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			$(e.currentTarget).closest(".input-group").remove();

			const responseExamples = Array.from($("#aiResponseExamplesContainer input"))
				.map((input) => $(input).val().trim())
				.filter((value) => value !== ""); // Remove empty values

			const examples = data.examples || {};
			examples[currentLanguage] = responseExamples;

			CurrentCanvasConfigCell.replaceData({
				...data,
				examples,
			});
		});

		// On Type Response Example
		$("#nodeConfigOffcanvas").on("input", '#aiResponseExamplesContainer [data-input="response-example"]', (e) => {
			e.stopPropagation();

			const data = CurrentCanvasConfigCell.getData();
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			const responseExamples = Array.from($("#aiResponseExamplesContainer input"))
				.map((input) => $(input).val().trim())
				.filter((value) => value !== ""); // Remove empty values

			const examples = data.examples || {};
			examples[currentLanguage] = responseExamples;

			CurrentCanvasConfigCell.replaceData({
				...data,
				examples,
			});
		});
	}
	initAIResponseConfigHandlers();

	// System Tool Node
	function initSystemToolNodeHandlers() {
		// Tool type change handler
		$("#script-graph").on("change", '[data-input="system-tool-type"]', (e) => {
			const currentElement = $(e.currentTarget);
			const closestNode = currentElement.closest(".x6-node");
			const cellId = closestNode.attr("data-cell-id");

			const cell = CurrentScriptGraph.getCellById(cellId);

			const toolType = parseInt(currentElement.val());

			// Update cell data
			const newData = {
				type: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
				toolType: toolType,
				config: {},
			};

			// TODO SET DEFAULT TOOL CONFIG
			if (toolType === SCRIPT_SYSTEM_TOOLS.END_CALL) {
				newData.config = {
					type: SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.IMMEDIATE,
				};
			} else if (toolType === SCRIPT_SYSTEM_TOOLS.SEND_SMS) {
				newData.config = {
					phoneNumberId: null,
					messages: {}
				}
			}
			else if (toolType === SCRIPT_SYSTEM_TOOLS.GOTONODE) {
				newData.config = {
					goToNodeId: null
				}
			}
			else if (toolType === SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT) {
				newData.config = {
					timeout: 5000,
					requireStartAsterisk: false,
					requireEndHash: false,
					maxLength: 1,
					encryptInput: false,
					outcomes: [],
				};
			} else if (toolType === SCRIPT_SYSTEM_TOOLS.TRANSFER_TO_AGENT) {
				newData.config = {
					agentId: null,
					transferContext: false,
					summarizeContext: false,
				};
			} else if (toolType === SCRIPT_SYSTEM_TOOLS.ADD_SCRIPT_TO_CONTEXT) {
				newData.config = {
					scriptId: null,
				};
			} else if (toolType === SCRIPT_SYSTEM_TOOLS.RETRIEVE_KNOWLEDGEBASE) {
				newData.config = {
					responseBeforeExecution: {}
				}

				BusinessFullData.businessData.languages.forEach((language) => {
					newData.config.responseBeforeExecution[language] = "";
				});
			}

			// Update cell data
			cell.replaceData(newData);

			const requiresConfig = doesScriptSystemToolRequireConfig(newData.toolType);

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
		// End Call Node
		$("#nodeConfigOffcanvas").on("change", '[data-input="end-call-type"]', (e) => {
			const value = parseInt(e.target.value);

			const newConfig = {
				type: value,
			};

			const messageContainer = $(e.target).closest(".tool-config-group").find("#end-call-message-container");

			if (value === SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.WITH_MESSAGE) {
				newConfig.messages = {};

				BusinessFullData.businessData.languages.forEach((language) => {
					newConfig.messages[language] = "";
				});

				messageContainer.find("textarea").val("");
				messageContainer.removeClass("d-none");
			} else {
				messageContainer.addClass("d-none");
			}

			updateSystemToolConfig(newConfig);
		});
		$("#nodeConfigOffcanvas").on("input", '[data-input="end-call-message"]', (e) => {
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config;
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			const messages = config.messages;
			messages[currentLanguage] = e.target.value;

			updateSystemToolConfig({ ...config, messages });
		});
		scriptsManagerLanguageDropdown.onLanguageChange((language) => {
			const currentLanguage = language.id;

			if (!CurrentCanvasConfigCell || !nodeConfigOffcanvas._element.classList.contains("show")) {
				return;
			}

			const nodeData = CurrentCanvasConfigCell.getData() || {};

			if (nodeData.type !== SCRIPT_NODE_TYPES.SYSTEM_TOOL && nodeData.toolType !== SCRIPT_SYSTEM_TOOLS.END_CALL) {
				return;
			}

			const config = nodeData.config;

			if (config.type !== SCRIPT_END_CALL_SYSTEM_TOOL_TYPE.WITH_MESSAGE) {
				return;
			}

			const messages = config.messages;

			$(`#nodeConfigOffcanvas [data-input="end-call-message"]`).val(messages[currentLanguage]);
		});
		$("#nodeConfigOffcanvas").on("change", '[data-input="send-sms-number"]', (e) => {
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config;

			updateSystemToolConfig({ ...config, phoneNumberId: e.target.value });
		});
		// SEND Sms Node
		$("#nodeConfigOffcanvas").on("input", '[data-input="send-sms-message"]', (e) => {
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config;
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			const messages = config.messages;
			messages[currentLanguage] = e.target.value;

			updateSystemToolConfig({ ...config, messages });
		});
		scriptsManagerLanguageDropdown.onLanguageChange((language) => {
			const currentLanguage = language.id;

			if (!CurrentCanvasConfigCell || !nodeConfigOffcanvas._element.classList.contains("show")) {
				return;
			}

			const nodeData = CurrentCanvasConfigCell.getData() || {};

			if (nodeData.type !== SCRIPT_NODE_TYPES.SYSTEM_TOOL && nodeData.toolType !== SCRIPT_SYSTEM_TOOLS.SEND_SMS) {
				return;
			}

			const config = nodeData.config;
			const messages = config.messages;

			$(`#nodeConfigOffcanvas [data-input="send-sms-message"]`).val(messages[currentLanguage]);
		});
		// Go To Node
		$("#nodeConfigOffcanvas").on("change", '[data-input="go-to-node"]', (e) => {
			const value = e.target.value;

			const newConfig = {
				goToNodeId: value,
			};

			updateSystemToolConfig(newConfig);
		});

		// Get DTMF Keypad Input Node
		$("#nodeConfigOffcanvas").on("input", '[data-input="dtmf-timeout"]', (e) => {
			const value = parseInt(e.target.value);
			if (value >= 1000 && value <= 30000) {
				const data = CurrentCanvasConfigCell.getData();
				const config = data.config || {};
				updateSystemToolConfig({ ...config, timeout: value });
			}
		});
		$("#nodeConfigOffcanvas").on("change", '[data-input="dtmf-require-start"]', (e) => {
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config || {};
			updateSystemToolConfig({ ...config, requireStartAsterisk: e.target.checked });
		});
		$("#nodeConfigOffcanvas").on("change", '[data-input="dtmf-require-end"]', (e) => {
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config || {};
			updateSystemToolConfig({ ...config, requireEndHash: e.target.checked });
		});
		$("#nodeConfigOffcanvas").on("input", '[data-input="dtmf-max-length"]', (e) => {
			const value = parseInt(e.target.value);
			if (value >= 1 && value <= 20) {
				const data = CurrentCanvasConfigCell.getData();
				const config = data.config || {};
				updateSystemToolConfig({ ...config, maxLength: value });
			}
		});
		$("#nodeConfigOffcanvas").on("change", '[data-input="dtmf-encrypt"]', (e) => {
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config || {};

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
			const config = data.config || {};
			updateSystemToolConfig({ ...config, variableName: e.target.value });
		});
		$("#nodeConfigOffcanvas").on("click", '[data-action="add-outcome"]', () => {
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

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

			const currentPortId = UniqueIdGenerator(scriptDMTFNextOutcomeId);
			scriptDMTFNextOutcomeId = currentPortId;

			const outcomeTemplate = `
						<div class="input-group mb-2" data-outcome-port-id="outcome-${currentPortId}">
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
			const outcomes = [...(config.outcomes || []), { value: {}, portId: `outcome-${currentPortId}` }];

			BusinessFullData.businessData.languages.forEach((language) => {
				outcomes.find((d) => d.portId === `outcome-${currentPortId}`).value[language] = "";
			});

			updateSystemToolConfig({ ...config, outcomes });

			const newPort = CurrentCanvasConfigCell.addPort({
				group: "output",
				id: `outcome-${currentPortId}`,
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
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			const val = e.target.value.trim();

			const outcomePortId = $(e.target).closest("[data-outcome-port-id]").data("outcome-port-id");
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config || {};
			const outcomes = config.outcomes || [];

			outcomes.find((d) => d.portId === outcomePortId).value[currentLanguage] = val;

			CurrentCanvasConfigCell.portProp(outcomePortId, "attrs/text/text", val);

			updateSystemToolConfig({ ...config, outcomes });
		});
		$("#nodeConfigOffcanvas").on("click", '[data-action="remove-outcome"]', (e) => {
			const outcomePortId = $(e.target).closest("[data-outcome-port-id]").data("outcome-port-id");
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config || {};
			const outcomes = config.outcomes || [];

			const outcomeDataIndex = outcomes.findIndex((d) => d.portId === outcomePortId);

			outcomes.splice(outcomeDataIndex, 1);
			$(e.target).closest("[data-outcome-port-id]").remove();

			CurrentCanvasConfigCell.removePort(outcomePortId);

			updateSystemToolConfig({ ...config, outcomes });
		});
		scriptsManagerLanguageDropdown.onLanguageChange((language) => {
			const currentLanguage = language.id;

			CurrentScriptGraph.getCells().forEach((cell) => {
				if (cell.shape === "edge") return;

				const nodeData = cell.getData();

				if (nodeData.type !== SCRIPT_NODE_TYPES.SYSTEM_TOOL) {
					return;
				}

				if (nodeData.toolType !== SCRIPT_SYSTEM_TOOLS.GET_DTMF_INPUT) {
					return;
				}

				const config = nodeData.config;
				const outcomes = config.outcomes;

				outcomes.forEach((outcome) => {
					cell.portProp(outcome.portId, "attrs/text/text", outcome.value[currentLanguage] || "");
				});

				if (CurrentCanvasConfigCell && nodeConfigOffcanvas._element.classList.contains("show")) {
					$('#nodeConfigOffcanvas [data-container="dtmf-outcomes"]')
						.find(".input-group")
						.each((index, container) => {
							const dataOutcomePortId = $(container).attr("data-outcome-port-id");

							$(container)
								.find('input[data-input="outcome-value"]')
								.val(outcomes.find((d) => d.portId === dataOutcomePortId).value[currentLanguage] || "");
						});
				}
			});
		});

		// Transfer Agent Node
		$("#nodeConfigOffcanvas").on("change", '[data-input="transfer-agent"]', (e) => {
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config || {};
			updateSystemToolConfig({ ...config, agentId: e.target.value });
		});
		$("#nodeConfigOffcanvas").on("change", '[data-input="transfer-context"]', (e) => {
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config || {};

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
			const config = data.config || {};
			updateSystemToolConfig({ ...config, summarizeContext: e.target.checked });
		});

		// Add Script Node
		$("#nodeConfigOffcanvas").on("change", '[data-input="add-script"]', (e) => {
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config || {};
			updateSystemToolConfig({ ...config, scriptId: e.target.value });
			// todo
			alert("todo 841284812");
		});

		// Retrieve KnowledgeBase Node
		$("#nodeConfigOffcanvas").on("input", '[data-input="retrieve-knowledgebase-response-before-execution"]', (e) => {
			const data = CurrentCanvasConfigCell.getData();
			const config = data.config;
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			const responseBeforeExecution = config.responseBeforeExecution;
			responseBeforeExecution[currentLanguage] = e.target.value;

			updateSystemToolConfig({ ...config, responseBeforeExecution });
		});
		scriptsManagerLanguageDropdown.onLanguageChange((language) => {
			const currentLanguage = language.id;

			if (!CurrentCanvasConfigCell || !nodeConfigOffcanvas._element.classList.contains("show")) {
				return;
			}

			const nodeData = CurrentCanvasConfigCell.getData() || {};

			if (nodeData.type !== SCRIPT_NODE_TYPES.SYSTEM_TOOL && nodeData.toolType !== SCRIPT_SYSTEM_TOOLS.RETRIEVE_KNOWLEDGEBASE) {
				return;
			}

			const config = nodeData.config;
			const responseBeforeExecution = config.responseBeforeExecution;

			$(`#nodeConfigOffcanvas [data-input="retrieve-knowledgebase-response-before-execution"]`).val(responseBeforeExecution[currentLanguage]);
		});
	}
	initSystemToolConfigHandlers();

	// Custom Tool Node
	function initializeCustomToolHandlers() {
		$("#script-graph").on("change", '[data-input="custom-tool-select"]', (e) => {
			const currentElement = $(e.currentTarget);
			const closestNode = currentElement.closest(".x6-node");
			const cellId = closestNode.attr("data-cell-id");

			const cell = CurrentScriptGraph.getCellById(cellId);
			const toolId = currentElement.val();

			// Update cell data
			cell.replaceData({
				type: SCRIPT_NODE_TYPES.CUSTOM_TOOL,
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

	// FlowApp Node
	function initFlowAppConfigHandlers() {

		// 1. Action Change
		$("#nodeConfigOffcanvas").on("change", '[data-input="flow-app-action"]', (e) => {
			if (!CurrentCanvasConfigCell) return;

			const newActionKey = e.target.value;
			const data = CurrentCanvasConfigCell.getData();
			const appDef = SpecificationFlowAppsListData.find(a => a.appKey === data.appKey);
			const actionDef = appDef.actions.find(a => a.actionKey === newActionKey);

			// Reset Inputs when action changes (schema changes)
			const newData = {
				...data,
				actionKey: newActionKey,
				inputs: [] // Clear previous inputs
			};

			// Update Node Data
			CurrentCanvasConfigCell.replaceData(newData);

			// Update Node Label
			let actionLabel = '<span class="text-muted fst-italic">Select Action...</span>';
			if (actionDef) {
				actionLabel = `<span class="fw-bold text-white">${actionDef.name}</span>`;
			} else {
				actionLabel = `<span class="text-danger">Invalid Action</span>`;
			}

			$(`.x6-node[data-cell-id="${CurrentCanvasConfigCell.id}"] .script-node.script-flowapp-node [data-input="flow-app-action-label"]`).html(actionLabel);

			// Update Output Ports based on Action Definition
			UpdateFlowAppNodePorts(CurrentCanvasConfigCell, actionDef);

			// Re-render the offcanvas to show new fields
			$("#nodeConfigContent").html(generateFlowAppConfig(CurrentCanvasConfigCell));

			// Trigger Schema Render (Phase 3 function placeholder)
			renderFlowAppSchemaInputs();
		});

		// 2. Integration Change
		$("#nodeConfigOffcanvas").on("change", '[data-input="flow-app-integration"]', (e) => {
			updateFlowAppConfig({ integrationId: e.target.value });
		});

		// 3. Speaking Text Change
		$("#nodeConfigOffcanvas").on("input", '[data-input="flow-app-speaking"]', (e) => {
			const data = CurrentCanvasConfigCell.getData();
			const currentLanguage = scriptsManagerLanguageDropdown.getSelectedLanguage().id;

			const speakingMap = data.speakingBeforeExecution || {};
			speakingMap[currentLanguage] = e.target.value;

			updateFlowAppConfig({ speakingBeforeExecution: speakingMap });
		});

		// 4. Language Change Listener (Sync Speaking Field)
		scriptsManagerLanguageDropdown.onLanguageChange((language) => {
			if (!CurrentCanvasConfigCell || !nodeConfigOffcanvas._element.classList.contains("show")) return;

			const data = CurrentCanvasConfigCell.getData();
			if (data.type !== SCRIPT_NODE_TYPES.FLOW_APP) return;

			// Update Speaking Textarea
			const speakingVal = data.speakingBeforeExecution?.[language.id] || "";
			$('[data-input="flow-app-speaking"]').val(speakingVal);

			// Re-render Schema (if schema has multi-lang labels/descriptions?)
			// Usually schema structure doesn't change by lang, but existing values might if we support multi-lang inputs later.
			// For now, inputs are typically logic-based (English keys).
			renderFlowAppSchemaInputs();
		});
	}
    initFlowAppConfigHandlers();

	// Sidebar
	function setupSidebarHandlers() {
		// 1. Drag Start Listener
		// Using delegated event to handle dynamic elements (like FlowApps later)
		$('.script-graph-sidebar-left').on('mousedown', '.sidebar-node-item', function (e) {
			const presetId = $(this).data('preset-id');

			// Factory create
			const node = createNodeFromPreset(presetId, CurrentScriptGraph);

			if (node) {
				// Start Dragging
				CurrentScriptGraphDnd.start(node, e);
			}
		});

		// 2. Search Filter
		$('#sidebarNodeSearch').on('input', function () {
			const term = $(this).val().toLowerCase().trim();

			// Filter Items
			$('.sidebar-node-item').each(function () {
				const text = $(this).find('span').text().toLowerCase();
				const isMatch = text.includes(term);
				$(this).toggleClass('hidden', !isMatch);
			});

			// Hide Empty Categories
			$('.sidebar-category').each(function () {
				// Check if any visible items exist in this category
				const visibleItems = $(this).find('.sidebar-node-item:not(.hidden)').length;
				$(this).toggleClass('hidden', visibleItems === 0);
			});
		});
	}
	setupSidebarHandlers();

	// Graph Toolbar Bottom
	$("#script-graph-zoom-in").on("click", () => {
		const zoom = CurrentScriptGraph.zoom();
		if (zoom < 16) {
			CurrentScriptGraph.zoom(0.1);
		}
	});

	$("#script-graph-zoom-out").on("click", () => {
		const zoom = CurrentScriptGraph.zoom();
		if (zoom > 0.01) {
			CurrentScriptGraph.zoom(-0.1);
		}
	});

	$("#script-graph-undo").on("click", () => {
		if (CurrentScriptGraphHistory.canUndo()) {
			CurrentScriptGraphHistory.undo();
		}
	});

	$("#script-graph-redo").on("click", () => {
		if (CurrentScriptGraphHistory.canRedo()) {
			CurrentScriptGraphHistory.redo();
		}
	});

	$("#script-graph-fullscreen").on("click", () => {
		const container = $(".script-graph-container");
		container.toggleClass("fullscreen");

		if (container.hasClass("fullscreen")) {
			$("body").css("overflow", "hidden");
			adjustScriptGraphMultilanguageDropdownForFullscreen(true);
			resizeScriptGraphCSS(() => { }, container.hasClass("fullscreen"));
		} else {
			$("body").css("overflow", "");
			adjustScriptGraphMultilanguageDropdownForFullscreen(false);
			setDynamicBodyHeight("scripts-tab");
		}
	});

	$(document).on("keydown", (e) => {
		if (ManageCurrentScriptType === null) return;

		if (e.key === "Escape" && $(".script-graph-container").hasClass("fullscreen")) {
			$("#script-graph-fullscreen").click();

			setDynamicBodyHeight("scripts-tab");
		}
	});

	// Other Functionality
	saveScriptButton.on("click", (event) => {
		event.preventDefault();

		const isMultiLanguageValidated = validateScriptMultilanguageElements(false);
		if (!isMultiLanguageValidated.isValid) {
			const errors = [];
			Object.keys(isMultiLanguageValidated.areLanguagesIncompleteInGeneralTab).forEach((lang) => {
				if (isMultiLanguageValidated.areLanguagesIncompleteInGeneralTab[lang]) {
					errors.push(`Please fill all multi-language fields for language ${lang} in general tab.`);
				}
			});
			Object.keys(isMultiLanguageValidated.areLanguagesIncompleteInConversationTab).forEach((lang) => {
				if (isMultiLanguageValidated.areLanguagesIncompleteInConversationTab[lang]) {
					errors.push(`Please fill all multi-language fields for language ${lang} in conversation tab.`);
				}
			});

			AlertManager.createAlert({
				type: "danger",
				message: `Please fill in all required multilangauge fields:<br><br>${errors.join("<br>")}`,
				timeout: 6000,
			});

			return;
		}

		const scriptConnectionValidation = validateScriptConnections();
		if (!scriptConnectionValidation.isValid) {
			AlertManager.createAlert({
				type: "danger",
				message: `Script nodes connection error:<br><br>${scriptConnectionValidation.errors.join("<br>")}`,
				timeout: 6000,
			});

			return;
		}

		const scriptNodesValidation = validateScriptNodes(false);
		if (!scriptNodesValidation.isValid) {
			AlertManager.createAlert({
				type: "danger",
				message: `Script nodes error:<br><br>${scriptNodesValidation.errors.join("<br>")}`,
				timeout: 6000,
			});

			return;
		}

		const changes = checkScriptTabHasChanges(false, true);
		if (!changes.hasChanges) {
			return;
		}

		IsSavingScriptTab = true;
		saveScriptButton.prop("disabled", true);
		saveScriptButtonSpinner.removeClass("d-none");

		const formData = new FormData();
		formData.append("postType", ManageCurrentScriptType);
		formData.append("changes", JSON.stringify(changes.changes));
		if (ManageCurrentScriptType === "edit") {
			formData.append("scriptId", ManageCurrentScriptData.id);
		}

		SaveBusinessScript(
			formData,
			(saveResponse) => {
				ManageCurrentScriptData = saveResponse.data;

				currentScriptName.text(ManageCurrentScriptData.general.name[BusinessDefaultLanguage]);

				if (ManageCurrentScriptType === "edit") {
					const exisitingScriptDataIndex = BusinessFullData.businessApp.scripts.findIndex((script) => script.id === ManageCurrentScriptData.id);
					BusinessFullData.businessApp.scripts[exisitingScriptDataIndex] = structuredClone(ManageCurrentScriptData);

					const scriptCard = scriptsCardListContainer.find(`.script-card[data-item-id="${ManageCurrentScriptData.id}"]`);

					scriptCard.find(".iqra-card-visual span").text(ManageCurrentScriptData.general.emoji);
					scriptCard.find(".iqra-card-title").text(ManageCurrentScriptData.general.name[BusinessDefaultLanguage]);
					scriptCard.find(".iqra-card-description span").text(ManageCurrentScriptData.general.description[BusinessDefaultLanguage]);
				} else if (ManageCurrentScriptType === "new") {
					BusinessFullData.businessApp.scripts.push(structuredClone(ManageCurrentScriptData));
					const noneScriptNotice = scriptsCardListContainer.find(".none-script-notice");
					if (noneScriptNotice.length > 0) {
						noneScriptNotice.remove();
					}
					scriptsCardListContainer.prepend($(createScriptListCardElement(ManageCurrentScriptData)));
				}

				saveScriptButton.prop("disabled", true);
				saveScriptButtonSpinner.addClass("d-none");

				IsSavingScriptTab = false;

				AlertManager.createAlert({
					type: "success",
					message: `Business script ${ManageCurrentScriptType === "new" ? "added" : "updated"} successfully.`,
					timeout: 6000,
				});

				IsSavingScriptTab = false;
				ManageCurrentScriptType = "edit";
			},
			(saveError, isUnsuccessful) => {
				var resultMessage = "Check console logs for more details.";
				if (saveError && saveError.message) resultMessage = saveError.message;

				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while saving business script data.",
					resultMessage: resultMessage,
					timeout: 6000,
				});

				console.log("Error occured while saving business script data: ", saveError);

				saveScriptButton.prop("disabled", false);
				saveScriptButtonSpinner.addClass("d-none");

				IsSavingScriptTab = false;
			},
		);
	});
}

//Helpers
function toPascalCase(str) {
	return str
		.match(/[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+/g)
		.map((x) => x.charAt(0).toUpperCase() + x.slice(1).toLowerCase())
		.join(" ");
}

function UniqueIdGenerator(compareId) {
	const generatedCode = Date.now() + Math.random().toString(36);

	if (compareId === generatedCode) {
		return uniqueNumber(compareId);
	}

	return generatedCode;
}

/** INIT **/
function initScriptsTab() {
	$(document).ready(() => {
		nodeConfigOffcanvas = new bootstrap.Offcanvas("#nodeConfigOffcanvas");
		variablesOffcanvas = new bootstrap.Offcanvas(variableOffcanvasElement);
		scriptsManagerLanguageDropdown = new MultiLanguageDropdown("scriptsManagerMultiLanguageContainer", BusinessFullLanguagesData);

		registerScriptNodes();
		initScriptsTabHandlers();
		fillScriptsListTab();
		renderFlowAppsSidebar();
	});
}
