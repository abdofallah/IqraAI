/** Dynamic Variables **/

let CurrentManageAgentData = null;
let ManageAgentType = null; // new or edit

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
const editAgentIntegrationSTTProviderSelect = agentTab.find("#editAgentIntegrationSTTProviderSelect");
const editAgentIntegrationSTTConfigurationList = agentTab.find("#editAgentIntegrationSTTConfigurationList");

const editAgentIntegrationLLMProviderSelect = agentTab.find("#editAgentIntegrationLLMProviderSelect");
const editAgentIntegrationLLMConfigurationList = agentTab.find("#editAgentIntegrationLLMConfigurationList");

const ttsConfigAzureSilenceTypeSelect = agentTab.find("#ttsConfigAzureSilenceTypeSelect");
const addTTSConfigAzureSilenceTypeButton = agentTab.find("#addTTSConfigAzureSilenceType");
const ttsConfigAzureSilenceTypeList = agentTab.find("#ttsConfigAzureSilenceTypeList");

// SUB | Settings Tab
const editAgentBackgroundAudioSelect = agentTab.find("#editAgentBackgroundAudioSelect");

/** Functions **/

function initAgentTab() {
	$(document).ready(() => {
		addNewAgentButton.on("click", (event) => {
			event.preventDefault();

			currentAgentName.text("New Agent");

			agentsListTab.removeClass("show");
			setTimeout(() => {
				agentsListTab.addClass("d-none");

				agentsManagerTab.removeClass("d-none");
				setTimeout(() => {
					agentsManagerTab.addClass("show");
				}, 10);
			}, 150);
		});

		switchBackToAgentsTab.on("click", (event) => {
			event.preventDefault();

			agentsManagerTab.removeClass("show");
			setTimeout(() => {
				agentsManagerTab.addClass("d-none");

				agentsListTab.removeClass("d-none");
				setTimeout(() => {
					agentsListTab.addClass("show");
				}, 10);
			}, 150);
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

		$(document).on("click", '[button-type="editAgentScriptConditionValueRemove"]', (event) => {
			event.preventDefault();
			event.stopPropagation();
			event.stopImmediatePropagation();

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

		$(document).on("click", '[button-type="editAgentScriptConversationMessageRemove"]', (event) => {
			event.preventDefault();
			event.stopPropagation();
			event.stopImmediatePropagation();

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

		$(document).on("change", '[select-type="set-ai-response-type"]', (event) => {
			event.stopPropagation();
			event.stopImmediatePropagation();

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

		$(document).on("change", '[select-type="set-ai-response-tool"]', (event) => {
			event.stopPropagation();
			event.stopImmediatePropagation();

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

		editAgentIntegrationSTTProviderSelect.on("change", (event) => {
			let selectedValue = editAgentIntegrationSTTProviderSelect.val();

			let activeConfigElement = editAgentIntegrationSTTConfigurationList.find('[div-type="sstconfigbox"].show');
			let activeConfigElementFor = activeConfigElement.attr("config-for");

			if (activeConfigElementFor === selectedValue) return;

			let selectedConfigElement = editAgentIntegrationSTTConfigurationList.find('[div-type="sstconfigbox"][config-for="' + selectedValue + '"]');

			activeConfigElement.removeClass("show");
			setTimeout(() => {
				activeConfigElement.addClass("d-none");

				selectedConfigElement.removeClass("d-none");
				setTimeout(() => {
					selectedConfigElement.addClass("show");
				}, 10);
			}, 150);
		});

		editAgentIntegrationLLMProviderSelect.on("change", (event) => {
			let selectedValue = editAgentIntegrationLLMProviderSelect.val();

			let activeConfigElement = editAgentIntegrationLLMConfigurationList.find('[div-type="llmconfigbox"].show');
			let activeConfigElementFor = activeConfigElement.attr("config-for");

			if (activeConfigElementFor === selectedValue) return;

			let selectedConfigElement = editAgentIntegrationLLMConfigurationList.find('[div-type="llmconfigbox"][config-for="' + selectedValue + '"]');

			activeConfigElement.removeClass("show");
			setTimeout(() => {
				activeConfigElement.addClass("d-none");

				selectedConfigElement.removeClass("d-none");
				setTimeout(() => {
					selectedConfigElement.addClass("show");
				}, 10);
			}, 150);
		});

		ttsConfigAzureSilenceTypeSelect.on("change", (event) => {
			let selectedValue = ttsConfigAzureSilenceTypeSelect.val();
			if (!selectedValue) return;

			addTTSConfigAzureSilenceTypeButton.prop("disabled", false);
		});

		addTTSConfigAzureSilenceTypeButton.on("click", (event) => {
			let selectedValue = ttsConfigAzureSilenceTypeSelect.val();
			if (!selectedValue) return;

			let selectedOption = ttsConfigAzureSilenceTypeSelect.find('option[value="' + selectedValue + '"]');
			let selectedOptionTextSplit = selectedOption.text().split(" - ");

			let newElement = $(`
                              <div class="mt-1">
                                   <label class="form-label btn-ic-span-align d-block" for="ttsConfigAzureSilenceType${selectedValue}">
                                        <span>${selectedOptionTextSplit[0]}</span>

                                        <a href="#" class="d-inline-block" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-title="${selectedOptionTextSplit[1]}">
                                             <i class="fa-regular fa-circle-question"></i>
                                        </a>
                                   </label>
                                   <div class="input-group" silence-type="${selectedValue}">
                                        <input type="number" class="form-control" id="ttsConfigAzureSilenceType${selectedValue}" value="750">
                                        <button class="btn btn-danger" button-type="removeTTSConfigAzureSilenceType">
                                             <i class='fa-regular fa-trash'></i>
                                        </button>
                                   </div>
                              </div>
                         `);

			let toolTip = new bootstrap.Tooltip(newElement.find('[data-bs-toggle="tooltip"]')[0]);

			ttsConfigAzureSilenceTypeList.append(newElement);

			selectedOption.remove();

			if (ttsConfigAzureSilenceTypeSelect.children().length === 1) {
				ttsConfigAzureSilenceTypeSelect.val("none");
				ttsConfigAzureSilenceTypeSelect.change();
				addTTSConfigAzureSilenceTypeButton.prop("disabled", true);
			}
		});

		$(document).on("click", '[button-type="removeTTSConfigAzureSilenceType"]', (event) => {
			event.stopPropagation();
			event.stopImmediatePropagation();

			let target = $(event.currentTarget);
			let parent = target.parent();
			let parentToRemove = parent.parent();

			let silenceType = parent.attr("silence-type");

			let labelValue = $(`label[for="ttsConfigAzureSilenceType${silenceType}"] span`).text();
			let toolTipDescription = $(`label[for="ttsConfigAzureSilenceType${silenceType}"] a`).attr("data-bs-title");

			ttsConfigAzureSilenceTypeSelect.append(`<option value="${silenceType}">${labelValue} - ${toolTipDescription}</option>`);

			parentToRemove.remove();

			if (ttsConfigAzureSilenceTypeSelect.children().length > 1) {
				addTTSConfigAzureSilenceTypeButton.prop("disabled", false);
			}
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
	});
}
