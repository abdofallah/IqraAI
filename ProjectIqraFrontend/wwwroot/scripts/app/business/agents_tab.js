/** Dynamic Variables **/

let CurrentManageAgentData = null;
let ManageAgentType = null; // new or edit

// Integration related states
let CurrentAgentIntegrationsSTT = [];
let CurrentAgentIntegrationsLLM = [];
let CurrentAgentIntegrationsTTS = [];

// Cache related states
let CurrentAgentCacheMessages = [];
let CurrentAgentCacheAudios = [];

let manageAgentsLanguageDropdown = null;

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
const agentstooltipList = [...agentstooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

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

// Integration Functions
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
                <button class="btn btn-danger" button-type="remove-integration">
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

// Cache Functions
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

function initAgentTab() {
	$(document).ready(() => {
		manageAgentsLanguageDropdown = new MultiLanguageDropdown("agentsManagerMultiLanguageContainer", BusinessFullLanguagesData);

		/** Event Handlers **/
		addNewAgentButton.on("click", (event) => {
			event.preventDefault();

			currentAgentName.text("New Agent");
			showAgentManagerTab();
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

		// Handle integration removal
		agentIntegrationsTab.on("click", '[button-type="remove-integration"]', function (event) {
			event.preventDefault();
			$(this).closest(".integration-item").remove();

			// Refresh indices
			$(".integration-item").each((idx, element) => {
				$(element).attr("data-index", idx);
				$(element)
					.find(".input-group-text i")
					.attr("class", `fa-regular fa-${idx + 1}`);
			});
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

		// Handle integration selection changes
		agentIntegrationsTab.on("change", 'select[select-type^="integration-"]', function () {
			const type = $(this).attr("select-type").split("-")[1].toUpperCase();
			const index = $(this).closest(".integration-item").data("index");
			const value = $(this).val();

			const currentArray = type === "STT" ? CurrentAgentIntegrationsSTT : type === "LLM" ? CurrentAgentIntegrationsLLM : CurrentAgentIntegrationsTTS;

			if (value) {
				currentArray[index] = value;
			} else {
				currentArray.splice(index, 1);
			}
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

			// Show the modal
			integrationConfigurationModal.modal("show");

			// TODO: Load and display configuration fields based on integration type
			// This will be implemented next when we work on the configuration fields
		});
	});
}
