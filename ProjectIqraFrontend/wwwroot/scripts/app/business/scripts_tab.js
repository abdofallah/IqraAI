//** Constants **/
const SCRIPT_GRAPH_PLUGINS = {
	Minimap: X6.MiniMap,
	Keyboard: X6.Keyboard,
	Clipboard: X6.Clipboard,
	History: X6.History,
	Snapline: X6.Snapline,
	Selection: X6.Selection,
};

const SCRIPT_NODE_TYPES = {
	START: 1,
	USER_QUERY: 2,
	AI_RESPONSE: 3,
	SYSTEM_TOOL: 4,
	CUSTOM_TOOL: 5,
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
const SCRIPT_NODE_MIN_HEIGHT = 300;

/** Dynamic Variables **/
let scriptsManagerLanguageDropdown = null;

// Script
let ManageCurrentScriptData = null;

let ManageCurrentScriptType = null; // new or edit

let CurrentScriptGraph = null;
let CurrentScriptGraphHistory = null;
let CurrentScriptGraphSelection = null;

let scriptDMTFNextOutcomeId = null;

let CurrentCanvasConfigCell = null;
let nodeConfigOffcanvas = null;

let CurrentScriptNameMultiLangData = {};
let CurrentScriptDescriptionMultiLangData = {};

let CheckScriptMultiLangInterval = null;

let IsSavingScriptTab = false;

/** Element Variables **/
const scriptTab = $("#scripts-tab");
const scriptsManagerHeader = scriptTab.find("#scripts-manager-header");
const addNewScriptButton = scriptTab.find("#addNewScriptButton");

// Script - List Tab
const scriptsListTab = scriptTab.find("#scriptsListTab");
const scriptsTable = scriptsListTab.find("#scriptsTable");

// Script - Manager Tab
const switchBackToScriptsListTab = scriptTab.find("#switchBackToScriptsListTab");
const currentScriptName = scriptTab.find("#currentScriptName");

const scriptsManagerTab = scriptTab.find("#scriptsManagerTab");

const saveScriptButton = scriptsManagerHeader.find("#saveScriptButton");
const saveScriptButtonSpinner = scriptsManagerHeader.find(".save-button-spinner");

const inputScriptName = scriptsManagerTab.find("#inputScriptName");
const inputScriptDescription = scriptsManagerTab.find("#inputScriptDescription");

/** API FUNCTIONS **/
function SaveBusinessScript(formData, onSuccess, onError) {
	$.ajax({
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
	}

	CurrentScriptNameMultiLangData = {};
	CurrentScriptDescriptionMultiLangData = {};

	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentScriptNameMultiLangData[language] = "";
		CurrentScriptDescriptionMultiLangData[language] = "";
	});

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

	if (enableDisableButton) {
		saveScriptButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges,
		changes,
	};
}

function createScriptTableElement(scriptData) {
	return `
		<tr script-id="${scriptData.id}">
			<td class="script-name">${scriptData.general.name[BusinessDefaultLanguage]}</td>
			<td>
				<button class="btn btn-info btn-sm" button-type="edit-script" script-id="${scriptData.id}">
                    <i class="fa-regular fa-pen-to-square"></i>
                </button>
                <button class="btn btn-danger btn-sm" button-type="remove-script" script-id="${scriptData.id}">
                    <i class="fa-regular fa-trash"></i>
                </button>
			</td>
		</tr>
	`;
}

function fillScriptsListTab() {
	if (BusinessFullData.businessApp.scripts.length !== 0) {
		scriptsListTab.find("tbody").empty();
		BusinessFullData.businessApp.scripts.forEach((script) => {
			const element = createScriptTableElement(script);
			scriptsListTab.find("tbody").append($(element));
		});
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

		// Remove the circle from line
		currentEdge.setAttrs({ line: { targetMarker: "" } });

		// Add Remove Button Tool
		currentEdge.addTools({
			name: "button-remove",
			args: { distance: "50%" },
		});
	});
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

			updateScriptGraphNodeSize(cell, div);

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

			updateScriptGraphNodeSize(cell, div);

			return div;
		},
	});

	// System Tool Node
	X6.Shape.HTML.register({
		shape: SCRIPT_NODE_TYPES.SYSTEM_TOOL,
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

			updateScriptGraphNodeSize(cell, div);

			return div;
		},
	});

	// Custom Tool Node
	X6.Shape.HTML.register({
		shape: SCRIPT_NODE_TYPES.CUSTOM_TOOL,
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

			updateScriptGraphNodeSize(cell, div);

			return div;
		},
	});
}

function initializeScriptGraph(isNew = true) {
	const container = $("#script-graph")[0];

	return resizeScriptGraphCSS((graphSize) => {
		// Set Default Shape Attributes
		X6.Shape.Edge.defaults.attrs.line.stroke = "#fff";
		X6.Shape.Edge.defaults.attrs.line.targetMarker = "circle";

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
			interacting: true,
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
			},
			preventDefaultBlankAction: true,
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

		graph.on("edge:connected", (event) => {
			// Remove the circle from line
			event.edge.setAttrs({ line: { targetMarker: "" } });

			// Add Remove Button Tool
			event.edge.addTools({
				name: "button-remove",
				args: { distance: "50%" },
			});
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
		});

		graph.on("blank:click", () => {
			nodeConfigOffcanvas.hide();
		});

		CurrentScriptGraph = graph;
	});
}

function updateScriptGraphNodeSize(cell, div) {
	if (!div || (div === null && cell)) {
		setTimeout(() => updateScriptGraphNodeSize(cell, div), 100);
		return;
	}

	const contentHeight = div.offsetHeight;

	if (contentHeight === 0) {
		setTimeout(() => updateScriptGraphNodeSize(cell, div), 100);
		return;
	}

	if (contentHeight !== cell.getSize().height) {
		cell.resize(SCRIPT_NODE_WIDTH, contentHeight);
	}
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

// Event Handlers
function initScriptsTabHandlers() {
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

	scriptsTable.on("click", 'button[button-type="edit-script"]', (event) => {
		event.preventDefault();

		const scriptId = $(event.currentTarget).attr("script-id");

		ResetAndEmptyScriptsManageTab();
		initializeScriptGraph(false);

		ManageCurrentScriptData = BusinessFullData.businessApp.scripts.find((script) => script.id === scriptId);

		fillScriptManagerTab();

		ManageCurrentScriptType = "edit";

		currentScriptName.text(ManageCurrentScriptData.general.name[BusinessDefaultLanguage]);
		showScriptManagerTab();
	});

	scriptsTable.on("click", 'button[button-type="delete-script"]', (event) => {
		event.preventDefault();

		alert("not implemented");
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

	// Graph Add Node Buttons
	// Add new event handlers
	$(".sidebar-node-button").on("click", function () {
		const nodeType = $(this).data("node-type");
		const currentGraphArea = CurrentScriptGraph.getGraphArea();
		const x = currentGraphArea.x + currentGraphArea.width / 2 - SCRIPT_NODE_WIDTH / 2;
		const y = currentGraphArea.y + currentGraphArea.height / 2 - SCRIPT_NODE_MIN_HEIGHT / 2;

		switch (nodeType) {
			case "user-query":
				addUserQueryNode(CurrentScriptGraph, x, y);
				break;
			case "ai-response":
				addAIResponseNode(CurrentScriptGraph, x, y);
				break;
			case "system-tool":
				addSystemToolNode(CurrentScriptGraph, x, y);
				break;
			case "custom-tool":
				addCustomToolNode(CurrentScriptGraph, x, y);
				break;
		}
	});

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
					scriptsTable.find(`[script-id="${ManageCurrentScriptData.id}"]`).find(".script-name").text(ManageCurrentScriptData.general.name[BusinessDefaultLanguage]);
				} else if (ManageCurrentScriptType === "new") {
					BusinessFullData.businessApp.scripts.push(structuredClone(ManageCurrentScriptData));
					const noneScriptNotice = scriptsTable.find(".none-script-notice");
					if (noneScriptNotice.length > 0) {
						noneScriptNotice.remove();
					}
					scriptsTable.prepend($(createScriptTableElement(ManageCurrentScriptData)));
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
				AlertManager.createAlert({
					type: "danger",
					message: "Error occured while saving business script data. Check browser console for logs.",
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
		scriptsManagerLanguageDropdown = new MultiLanguageDropdown("scriptsManagerMultiLanguageContainer", BusinessFullLanguagesData);

		registerScriptNodes();
		initScriptsTabHandlers();
		fillScriptsListTab();
	});
}
