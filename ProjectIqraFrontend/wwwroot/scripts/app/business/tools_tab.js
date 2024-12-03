const HTTPStatusCodeList = [
	{ code: 100, name: "Continue" },
	{ code: 101, name: "Switching Protocols" },
	{ code: 102, name: "Processing (WebDAV)" },
	{ code: 103, name: "Early Hints" },
	{ code: 200, name: "OK" },
	{ code: 201, name: "Created" },
	{ code: 202, name: "Accepted" },
	{ code: 203, name: "Non-Authoritative Information" },
	{ code: 204, name: "No Content" },
	{ code: 205, name: "Reset Content" },
	{ code: 206, name: "Partial Content" },
	{ code: 207, name: "Multi-Status (WebDAV)" },
	{ code: 208, name: "Already Reported (WebDAV)" },
	{ code: 226, name: "IM Used (HTTP Delta encoding)" },
	{ code: 300, name: "Multiple Choices" },
	{ code: 301, name: "Moved Permanently" },
	{ code: 302, name: "Found" },
	{ code: 303, name: "See Other" },
	{ code: 304, name: "Not Modified" },
	{ code: 305, name: "Use Proxy Deprecated" },
	{ code: 307, name: "Temporary Redirect" },
	{ code: 308, name: "Permanent Redirect" },
	{ code: 400, name: "Bad Request" },
	{ code: 401, name: "Unauthorized" },
	{ code: 402, name: "Payment Required Experimental" },
	{ code: 403, name: "Forbidden" },
	{ code: 404, name: "Not Found" },
	{ code: 405, name: "Method Not Allowed" },
	{ code: 406, name: "Not Acceptable" },
	{ code: 407, name: "Proxy Authentication Required" },
	{ code: 408, name: "Request Timeout" },
	{ code: 409, name: "Conflict" },
	{ code: 410, name: "Gone" },
	{ code: 411, name: "Length Required" },
	{ code: 412, name: "Precondition Failed" },
	{ code: 413, name: "Payload Too Large" },
	{ code: 414, name: "URI Too Long" },
	{ code: 415, name: "Unsupported Media Type" },
	{ code: 416, name: "Range Not Satisfiable" },
	{ code: 417, name: "Expectation Failed" },
	{ code: 418, name: "I'm a teapot" },
	{ code: 421, name: "Misdirected Request" },
	{ code: 422, name: "Unprocessable Content (WebDAV)" },
	{ code: 423, name: "Locked (WebDAV)" },
	{ code: 424, name: "Failed Dependency (WebDAV)" },
	{ code: 425, name: "Too Early Experimental" },
	{ code: 426, name: "Upgrade Required" },
	{ code: 428, name: "Precondition Required" },
	{ code: 429, name: "Too Many Requests" },
	{ code: 431, name: "Request Header Fields Too Large" },
	{ code: 451, name: "Unavailable For Legal Reasons" },
	{ code: 500, name: "Internal Server Error" },
	{ code: 501, name: "Not Implemented" },
	{ code: 502, name: "Bad Gateway" },
	{ code: 503, name: "Service Unavailable" },
	{ code: 504, name: "Gateway Timeout" },
	{ code: 505, name: "HTTP Version Not Supported" },
	{ code: 506, name: "Variant Also Negotiates" },
	{ code: 507, name: "Insufficient Storage (WebDAV)" },
	{ code: 508, name: "Loop Detected (WebDAV)" },
	{ code: 510, name: "Not Extended" },
	{ code: 511, name: "Network Authentication Required" },
]; // Make this dynamic - app/specification

require.config({
	paths: {
		vs: "/libs/monaco-editor-0.48.0/package/min/vs",
		esprima: "/libs/esprima-4.0.1/dist/esprima.js",
	},
});

/** Dynamic Variables **/
let responseStatusMonacoEditors = [];

let ManageToolType = null;
let CurrentManageToolData = null;
let CurrentManageToolSchemeaListIndex = 0;

let ToolAudioBeforeSpeakingWaveSurfer = null;
let ToolAudioDuringSpeakingWaveSurfer = null;
let ToolAudioAfterSpeakingWaveSurfer = null;

let CurrentManageToolNameMultiLangData = {};
let CurrentManageToolShortDescriptionMultiLangData = {};
const CurrentManageToolInputSchemeaMultiLangData = {};
const CurrentManageToolResponseStaticResponse = {};

let IsSavingToolManageTab = false;

/** Elements Variables **/
const toolsTab = $("#tools-tab");

const tooltipTriggerList = document.querySelectorAll('#tools-tab [data-bs-toggle="tooltip"]');
const tooltipList = [...tooltipTriggerList].map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl));

// Tools List Tab
const toolListTab = toolsTab.find("#toolListTab");

const addNewToolbutton = toolListTab.find("#addNewToolButton");
const customToolsTable = toolListTab.find("#customToolsTable");

// Tools Manager Tab
const toolManagerTab = toolsTab.find("#toolManagerTab");

const toolManagerHeader = toolsTab.find("#tool-manager-header");

const switchBackToToolsTab = toolManagerHeader.find("#switchBackToToolsTab");
const currentToolName = toolManagerHeader.find("#currentToolName");

const confirmPublishToolButton = toolManagerHeader.find("#confirmPublishToolButton");
const confirmPublishToolButtonSpinner = confirmPublishToolButton.find(".save-button-spinner");

const toolManagerInnerGeneralTab = toolManagerHeader.find("#toolManager-inner-general-tab");

// General
const inputToolName = toolManagerTab.find("#inputToolName");
const inputToolShortDescription = toolManagerTab.find("#inputToolShortDescription");

// Configuration
const addToolInputArgumentButton = toolManagerTab.find("#addToolInputArgument");
const toolInputArguementsList = toolManagerTab.find("#toolInputArguementsList");

const inputToolType = toolManagerTab.find("#inputToolType");

const inputToolURL = toolManagerTab.find("#inputToolURL");

const addToolHeaderButton = toolManagerTab.find("#addToolHeader");
const toolHeadersList = toolManagerTab.find("#toolHeadersList");

const toolBodyType = toolManagerTab.find('[name="toolBodyTypeCheckbox"]');
const toolBodyNone = toolManagerTab.find("#toolBodyNone");
const toolBodyKeyValueView = toolManagerTab.find("#toolBodyKeyValueView");
const toolBodyKeyValueViewList = toolManagerTab.find("#toolBodyKeyValueViewList");
const toolBodyRawView = toolManagerTab.find("#toolBodyRawView");
const addToolBodyKeyValueButton = toolManagerTab.find("#addToolBodyKeyValue");

const toolBodyRawTextarea = toolManagerTab.find("#toolBodyRawTextarea");

// Response
const toolResponseStatusTypeListButtons = toolManagerTab.find("#toolResponseStatusTypeListButtons");
const toolResponseStatusTypeList = toolManagerTab.find("#toolResponseStatusTypeList");
const toolResponseStatusSelect = toolManagerTab.find("#toolResponseStatusSelect");
const addToolResponseStatusTypeButton = toolManagerTab.find("#addToolResponseStatusType");

// Audio
const toolAudioBeforeSpeakingBox = toolManagerTab.find("#toolAudioBeforeSpeakingBox");
const toolAudioBeforeSpeakingSelect = toolManagerTab.find("#toolAudioBeforeSpeakingSelect");
const toolAudioBeforeSpeakingInputBox = toolAudioBeforeSpeakingBox.find("#toolAudioBeforeSpeakingInputBox");
const toolAudioBeforeSpeakingUploadBtn = toolAudioBeforeSpeakingInputBox.find("#tool-audio-before-upload-btn");
const toolAudioBeforeSpeakingUploadInput = toolAudioBeforeSpeakingInputBox.find("#toolAudioBeforeSpeakingUploadInput");
const toolAudioBeforeSpeakingVolumeInput = toolAudioBeforeSpeakingBox.find("#toolAudioBeforeSpeakingVolumeInput");

const toolAudioDuringSpeakingBox = toolManagerTab.find("#toolAudioDuringSpeakingBox");
const toolAudioDuringSpeakingSelect = toolManagerTab.find("#toolAudioDuringSpeakingSelect");
const toolAudioDuringSpeakingInputBox = toolAudioDuringSpeakingBox.find("#toolAudioDuringSpeakingInputBox");
const toolAudioDuringSpeakingUploadBtn = toolAudioDuringSpeakingInputBox.find("#tool-audio-durning-upload-btn");
const toolAudioDuringSpeakingUploadInput = toolAudioDuringSpeakingInputBox.find("#toolAudioDuringSpeakingUploadInput");
const toolAudioDuringSpeakingVolumeInput = toolAudioDuringSpeakingBox.find("#toolAudioDuringSpeakingVolumeInput");

const toolAudioAfterSpeakingBox = toolManagerTab.find("#toolAudioAfterSpeakingBox");
const toolAudioAfterSpeakingSelect = toolManagerTab.find("#toolAudioAfterSpeakingSelect");
const toolAudioAfterSpeakingInputBox = toolAudioAfterSpeakingBox.find("#toolAudioAfterSpeakingInputBox");
const toolAudioAfterSpeakingUploadBtn = toolAudioAfterSpeakingInputBox.find("#tool-audio-after-upload-btn");
const toolAudioAfterSpeakingUploadInput = toolAudioAfterSpeakingInputBox.find("#toolAudioAfterSpeakingUploadInput");
const toolAudioAfterSpeakingVolumeInput = toolAudioAfterSpeakingBox.find("#toolAudioAfterSpeakingVolumeInput");

let manageToolsLanguageDropdown = null;
RunActionAfterBusinessDataLoad(() => {
	RunActionAfterLanguagesSpecificationLoad(() => {
		const businessLanguages = [];

		BusinessFullData.businessData.languages.forEach((value, index) => {
			const countryCodeLanguage = SpecificationLanguagesListData.find((data, index) => {
				return data.id === value;
			});

			if (countryCodeLanguage) {
				businessLanguages.push(countryCodeLanguage);
			}
		});

		manageToolsLanguageDropdown = new MultiLanguageDropdown("manageToolsLanguageDropdown", businessLanguages);
	});
});

// Api Functions

function SaveBusinessTool(changes, successCallback, errorCallback) {
	$.ajax({
		type: "POST",
		url: `/app/user/business/${CurrentBusinessId}/tools/save`,
		data: changes,
		dataType: "json",
		processData: false,
		contentType: false,
		success: (response) => {
			if (!response.success) {
				errorCallback(response, false);
				return;
			}

			successCallback(response);
		},
		error: (error) => {
			errorCallback(error, true);
		},
	});
}

// Functions

function ShowToolsManageTab() {
	toolListTab.removeClass("show");
	setTimeout(() => {
		toolListTab.addClass("d-none");

		toolManagerTab.removeClass("d-none");
		toolManagerHeader.removeClass("d-none");
		setTimeout(() => {
			toolManagerTab.addClass("show");
			toolManagerHeader.addClass("show");

			setDynamicBodyHeight(true, toolManagerHeader);
		}, 10);
	}, 300);
}

function initResponseCodeEditor(statusType, containerId) {
	monaco.languages.typescript.javascriptDefaults.setDiagnosticsOptions({
		noSemanticValidation: false,
		noSyntaxValidation: false,
		diagnosticCodesToIgnore: [1108],
	});

	monaco.languages.typescript.javascriptDefaults.setCompilerOptions({
		target: monaco.languages.typescript.ScriptTarget.ES2015,
		allowNonTsExtensions: true,
	});

	const libUri = `ts:filename/response${statusType}.d.ts`;
	const libSource = "const responseData = any";
	monaco.languages.typescript.javascriptDefaults.addExtraLib(libSource, libUri);
	monaco.editor.createModel(libSource, "typescript", monaco.Uri.parse(libUri));

	const editor = monaco.editor.create($(`#${containerId} .mon-editor`)[0], {
		theme: "vs-dark",
		value: "return responseData;",
		language: "javascript",
		automaticLayout: true,

		scrollBeyondLastLine: false,
	});

	function validateCode() {
		const code = editor.getValue();
		const ast = esprima.parseScript(code, { tolerant: true });

		const returnStatements = [];

		function hasReturnStatement(node) {
			let result = null;

			if (node.type === "ReturnStatement") {
				result = node.argument !== null;
				returnStatements.push(result);
			}

			if (node.type === "BlockStatement" || node.type === "TryStatement") {
				node.body.forEach((subNode) => {
					result = hasReturnStatement(subNode);
				});
			}

			if (node.type === "IfStatement") {
				result = hasReturnStatement(node.consequent);

				if (node.alternate) {
					result = hasReturnStatement(node.alternate);
					returnStatements.push(result);
				} else {
					if (result !== null) {
						returnStatements.push(false);
						result = false;
					}
				}
			}

			return result;
		}

		ast.body.forEach(hasReturnStatement);

		return returnStatements.length > 0 && returnStatements.every(Boolean);
	}

	// Validate the code whenever the content changes
	editor.onDidChangeModelContent((changes) => {
		const isValid = validateCode();

		const alert = $(`#${containerId} .error-result-container .returnAlert`);

		if (!isValid) {
			alert.removeClass("d-none");
		} else {
			alert.addClass("d-none");
		}
	});

	monaco.editor.onDidChangeMarkers(([uri]) => {
		if (uri.scheme !== "inmemory") return;
		const thisModel = monaco.editor.getModel(uri);
		const editorsList = monaco.editor.getEditors(uri);

		let thisEditorParent;
		editorsList.forEach((editor) => {
			if (editor.getModel() !== thisModel) return;
			thisEditorParent = editor.getContainerDomNode().parentNode;
		});
		if (!thisEditorParent) return;

		if ($(thisEditorParent).attr("element-type") !== "toolResponseStatusElement") return;
		const markers = monaco.editor.getModelMarkers({ resource: uri });

		const list = $(thisEditorParent).find(".error-result-container ul");
		const listData = markers.map(({ message, startLineNumber, startColumn, endLineNumber, endColumn }) => {
			if (!message || message.trim() === "") return;
			return `<li>${message} [${startLineNumber}:${startColumn}-${endLineNumber}:${endColumn}]</li>`;
		});

		if (listData.length > 0) {
			list.html(`
                        <span class="btn-ic-span-align"><i class="fa-regular fa-circle-exclamation"></i> <span>${listData.length} Errors</span></span>
                        ${listData.join("")}
                    `);
		} else {
			list.html("");
		}
	});

	responseStatusMonacoEditors.push({ statusType: statusType, editor: editor });
}

function CreateToolsTableElement(data) {
	const element = `
        <tr tool-id="${data.id}">
            <td>
                <b>${data.general.name}</b>
            </td>
            <td>
                <button class="btn btn-info btn-sm" button-type="edit-tool" tool-id="${data.id}">
                    <i class="fa-regular fa-pen-to-square"></i>
                </button>
                <button class="btn btn-danger btn-sm" button-type="remove-tool" tool-id="${data.id}">
                    <i class="fa-regular fa-trash"></i>
                </button>
            </td>
        </tr>
    `;

	return element;
}

function FillToolsTab() {
	customToolsTable.find("tbody").empty();

	const toolsList = BusinessFullData.businessApp.tools;

	if (toolsList.length === 0) {
		customToolsTable.find("tbody").append(`<tr tr-type="none-notice"><td colspan="2">No tools added yet...</td></tr>`);
	} else {
		toolsList.forEach((toolData) => {
			customToolsTable.find("tbody").append(CreateToolsTableElement(toolData));
		});
	}
}

function ResetAndEmptyToolsManageTab() {
	toolInputArguementsList.children().remove();
	toolHeadersList.children().remove();

	toolResponseStatusTypeListButtons.children().remove();
	toolResponseStatusTypeList.children().remove();
	responseStatusMonacoEditors.forEach((data) => {
		data.editor.dispose();
	});
	responseStatusMonacoEditors = [];

	const allMonacoModels = monaco.editor.getModels();
	allMonacoModels.forEach((model) => {
		model.dispose();
	});

	$("#toolBodyTypeNone").click();
	toolBodyKeyValueViewList.children().remove();

	$("#inputToolType").val(0).change();

	toolManagerTab.find("input[type=text], input[type=number], textarea").val("").change();

	toolResponseStatusSelect.children().remove();

	HTTPStatusCodeList.forEach((element) => {
		toolResponseStatusSelect.append(`<option value="${element.code}">${element.code} | ${element.name}</option>`);
	});

	if (ToolAudioBeforeSpeakingWaveSurfer?.destroy) {
		ToolAudioBeforeSpeakingWaveSurfer.destroy();
	}

	if (ToolAudioDuringSpeakingWaveSurfer?.destroy) {
		ToolAudioDuringSpeakingWaveSurfer.destroy();
	}

	if (ToolAudioAfterSpeakingWaveSurfer?.destroy) {
		ToolAudioAfterSpeakingWaveSurfer.destroy();
	}

	ToolAudioBeforeSpeakingWaveSurfer = CreateToolAudioWavesurfer("#tool-audio-before-waveform");
	ToolAudioDuringSpeakingWaveSurfer = CreateToolAudioWavesurfer("#tool-audio-during-waveform");
	ToolAudioAfterSpeakingWaveSurfer = CreateToolAudioWavesurfer("#tool-audio-after-waveform");

	toolAudioBeforeSpeakingVolumeInput.val("100");
	toolAudioDuringSpeakingVolumeInput.val("100");
	toolAudioAfterSpeakingVolumeInput.val("100");

	toolAudioBeforeSpeakingInputBox.find(".no-audio-notice").removeClass("d-none");
	toolAudioBeforeSpeakingInputBox.find(".recording-container-waveform").addClass("d-none");
	toolAudioBeforeSpeakingInputBox.find(".audio-controller").addClass("d-none");

	toolAudioDuringSpeakingInputBox.find(".no-audio-notice").removeClass("d-none");
	toolAudioDuringSpeakingInputBox.find(".recording-container-waveform").addClass("d-none");
	toolAudioDuringSpeakingInputBox.find(".audio-controller").addClass("d-none");

	toolAudioAfterSpeakingInputBox.find(".no-audio-notice").removeClass("d-none");
	toolAudioAfterSpeakingInputBox.find(".recording-container-waveform").addClass("d-none");
	toolAudioAfterSpeakingInputBox.find(".audio-controller").addClass("d-none");

	toolAudioBeforeSpeakingUploadInput.val("");
	toolAudioDuringSpeakingUploadInput.val("");
	toolAudioAfterSpeakingUploadInput.val("");

	toolAudioBeforeSpeakingSelect.val("none").change();
	toolAudioDuringSpeakingSelect.val("none").change();
	toolAudioAfterSpeakingSelect.val("none").change();

	toolManagerInnerGeneralTab.click();

	CurrentManageToolNameMultiLangData = {};
	CurrentManageToolShortDescriptionMultiLangData = {};
	BusinessFullData.businessData.languages.forEach((language) => {
		CurrentManageToolNameMultiLangData[language] = "";
		CurrentManageToolShortDescriptionMultiLangData[language] = "";
	});

	confirmPublishToolButton.prop("disabled", true);
}

function ShowToolsListTab() {
	toolManagerHeader.removeClass("show");
	toolManagerTab.removeClass("show");

	setTimeout(() => {
		toolManagerTab.addClass("d-none");
		toolManagerHeader.addClass("d-none");

		toolListTab.removeClass("d-none");

		setDynamicBodyHeight(false);
		setTimeout(() => {
			toolListTab.addClass("show");
		}, 10);
	}, 300);
}

function CreateToolsDefaultToolObject() {
	const object = {
		id: null,
		general: {
			name: {},
			shortDescription: {},
		},
		configuration: {
			inputSchemea: [],
			requestType: 0,
			endpoint: "",
			headers: {},
			bodyType: 0,
			bodyData: null,
		},
		response: {},
		audio: {
			beforeSpeaking: null,
			duringSpeaking: null,
			afterSpeaking: null,
		},
	};

	BusinessFullData.businessData.languages.forEach((language) => {
		object.general.name[language] = "";
		object.general.shortDescription[language] = "";
	});

	return object;
}

function CheckToolsManageTabHasChanges(enableDisableButton = true) {
	const changes = {};
	let hasChanges = false;

	// General
	function CheckGeneralTabHasChanges() {
		changes.general = {};

		changes.general.name = {};
		BusinessFullData.businessData.languages.forEach((language) => {
			changes.general.name[language] = CurrentManageToolNameMultiLangData[language];

			const previousData = CurrentManageToolData.general.name[language];
			if (previousData !== changes.general.name[language]) {
				hasChanges = true;
			}
		});

		changes.general.shortDescription = {};
		BusinessFullData.businessData.languages.forEach((language) => {
			changes.general.shortDescription[language] = CurrentManageToolShortDescriptionMultiLangData[language];

			const previousData = CurrentManageToolData.general.shortDescription[language];
			if (previousData !== changes.general.shortDescription[language]) {
				hasChanges = true;
			}
		});
	}
	CheckGeneralTabHasChanges();

	// Configuration
	function CheckConfigurationTabHasChanges() {
		changes.configuration = {};

		function checkInputArguementsList() {
			const inputArgumentsList = [];
			const argumentElements = toolInputArguementsList.find(".toolInputArguementBox");

			if (argumentElements.length !== CurrentManageToolData.configuration.inputSchemea.length) {
				hasChanges = true;
			}

			argumentElements.each((idx, element) => {
				const currentElement = $(element);

				const index = parseInt(currentElement.attr("data-index"));
				const argumentData = {
					name: {},
					description: {},
					type: parseInt(currentElement.find('[data-type="typeSelect"]').val()),
					isArray: element.querySelector(
						`[data-type="required"]#toolInputArguementArray${element.querySelector('[data-type="required"][id^="toolInputArguementArray"]').id.replace("toolInputArguementArray", "")}`,
					).checked,
					isRequired: element.querySelector(
						`[data-type="required"]#toolInputArguementRequired${element.querySelector('[data-type="required"][id^="toolInputArguementRequired"]').id.replace("toolInputArguementRequired", "")}`,
					).checked,
				};

				BusinessFullData.businessData.languages.forEach((language) => {
					argumentData.name[language] = CurrentManageToolInputSchemeaMultiLangData[index].name[language];
					argumentData.description[language] = CurrentManageToolInputSchemeaMultiLangData[index].description[language];

					const previousData = CurrentManageToolData.configuration.inputSchemea[index];
					if (!previousData) {
						hasChanges = true;
					} else {
						if (previousData.name[language] !== argumentData.name[language] || previousData.description[language] !== argumentData.description[language]) {
							hasChanges = true;
						}
					}
				});

				if (index < CurrentManageToolData.configuration.inputSchemea.length) {
					const originalArgument = CurrentManageToolData.configuration.inputSchemea[index];

					if (originalArgument.type !== argumentData.type || originalArgument.isArray !== argumentData.isArray || originalArgument.isRequired !== argumentData.isRequired) {
						hasChanges = true;
					}
				} else {
					hasChanges = true;
				}

				inputArgumentsList.push(argumentData);
			});

			return inputArgumentsList;
		}
		changes.configuration.inputSchemea = checkInputArguementsList();

		changes.configuration.requestType = inputToolType.val();
		if (parseInt(inputToolType.val()) !== CurrentManageToolData.configuration.requestType) {
			hasChanges = true;
		}

		changes.configuration.endpoint = inputToolURL.val();
		if (inputToolURL.val() !== CurrentManageToolData.configuration.endpoint) {
			hasChanges = true;
		}

		function checkHeadersList() {
			const headers = {};
			const headerElements = toolHeadersList.find(".tool-header-box");

			const currentHeaderCount = headerElements.length;
			const originalHeaderCount = Object.keys(CurrentManageToolData.configuration.headers).length;
			if (currentHeaderCount !== originalHeaderCount) {
				hasChanges = true;
			}

			headerElements.each((idx, element) => {
				const currentElement = $(element);

				const keyInput = currentElement.find('[data-type="key"]');
				const valueInput = currentElement.find('[data-type="value"]');

				const key = keyInput.val().trim();
				const value = valueInput.val().trim();

				headers[key] = value;

				if (!CurrentManageToolData.configuration.headers.hasOwnProperty(key) || CurrentManageToolData.headers[key] !== value) {
					hasChanges = true;
				}
			});

			Object.keys(CurrentManageToolData.configuration.headers).forEach((originalKey) => {
				if (!headers.hasOwnProperty(originalKey)) {
					hasChanges = true;
				}
			});

			return headers;
		}
		changes.configuration.headers = checkHeadersList();

		changes.configuration.bodyType = parseInt(toolManagerTab.find('[name="toolBodyTypeCheckbox"]:checked').val());
		if (changes.configuration.bodyType !== CurrentManageToolData.configuration.bodyType) {
			hasChanges = true;
		}
		if (changes.configuration.bodyType !== "none") {
			if (changes.configuration.bodyType === "form-data" || changes.configuration.bodyType === "x-www-form-urlencoded") {
				const formKeyValData = {};
				const formKeyValElements = toolBodyKeyValueViewList.children();

				if (CurrentManageToolData.configuration.bodyType === "form-data" || CurrentManageToolData.configuration.bodyType === "x-www-form-urlencoded") {
					const currentFormKeyValCount = formKeyValElements.length;
					const originalFormKeyValCount = Object.keys(CurrentManageToolData.configuration.bodyData).length;
					if (currentFormKeyValCount !== originalFormKeyValCount) {
						hasChanges = true;
					}

					Object.keys(CurrentManageToolData.configuration.bodyData).forEach((originalKey) => {
						if (!formKeyValData.hasOwnProperty(originalKey)) {
							hasChanges = true;
						}
					});
				}

				formKeyValElements.each((idx, element) => {
					const currentElement = $(element);

					const keyInput = currentElement.find('[data-type="key"]');
					const valueInput = currentElement.find('[data-type="value"]');

					const key = keyInput.val().trim();
					const value = valueInput.val().trim();

					formKeyValData[key] = value;

					if (CurrentManageToolData.configuration.bodyType === "form-data" || CurrentManageToolData.configuration.bodyType === "x-www-form-urlencoded") {
						if (!CurrentManageToolData.configuration.bodyData.hasOwnProperty(key) || CurrentManageToolData.bodyData[key] !== value) {
							hasChanges = true;
						}
					}
				});

				changes.configuration.bodyData = formKeyValData;
			}

			if (changes.configuration.bodyType === "raw") {
				changes.configuration.bodyData = toolBodyRawTextarea.val();

				if (CurrentManageToolData.configuration.bodyType === "raw" && CurrentManageToolData.configuration.bodyData !== toolBodyRawTextarea.val()) {
					hasChanges = true;
				}
			}
		}
	}
	CheckConfigurationTabHasChanges();

	// Response
	function CheckResponseTabHasChanges() {
		function checkResponseList() {
			const response = {};

			if (responseStatusMonacoEditors.length !== Object.keys(CurrentManageToolData.response).length) {
				hasChanges = true;
			}

			responseStatusMonacoEditors.forEach((data) => {
				const statusType = data.statusType;
				const editor = data.editor;
				const editorValue = editor.getValue();

				response[statusType] = {
					javascript: editorValue,
					hasStaticResponse: toolManagerTab.find(`input[input-type="toolResponseStatusSpeakStaticResponseCheck"][status-type="${statusType}"]`).prop("checked"),
					staticResponse: {},
				};

				const previousData = CurrentManageToolData.response[statusType];
				if (!previousData) {
					hasChanges = true;
				} else {
					if (previousData.javascript !== response[statusType].editorValue) {
						hasChanges = true;
					}

					if (previousData.hasStaticResponse !== response[statusType].hasStaticResponse) {
						hasChanges = true;
					}
				}

				if (response[statusType].hasStaticResponse) {
					BusinessFullData.businessData.languages.forEach((language) => {
						const fullLanguageData = SpecificationLanguagesListData.find((d) => d.id === language);

						const value = CurrentManageToolResponseStaticResponse[statusType][language];
						response[statusType].staticResponse[language] = value;

						if (previousData && previousData.staticResponse[language] !== value) {
							hasChanges = true;
						}
					});
				}
			});

			return response;
		}
		changes.response = checkResponseList();
	}
	CheckResponseTabHasChanges();

	// Audio
	function CheckAudioTabHasChanges() {
		changes.audio = {};

		if (toolAudioBeforeSpeakingUploadInput[0].files.length > 0) {
			hasChanges = true;
		}

		if (toolAudioBeforeSpeakingUploadInput[0].files.length > 0) {
			hasChanges = true;
		}

		if (toolAudioBeforeSpeakingUploadInput[0].files.length > 0) {
			hasChanges = true;
		}
	}
	CheckAudioTabHasChanges();

	if (enableDisableButton) {
		confirmPublishToolButton.prop("disabled", !hasChanges);
	}

	return {
		hasChanges: hasChanges,
		changes: changes,
	};
}

function CreateToolsConfigurationInputSchemeaElement(index) {
	const dateTimeNow = Date.now();

	const element = `
                <div class="toolInputArguementBox input-group mt-1" data-index="${index}">
                    <div style="width: calc(100% - 50px)">
                        <div class="input-group">
                                <input type="text" class="form-control" data-type="name" placeholder="Argument Name &#xf1ab;" style="max-width: 250px; border-bottom-left-radius: 0; border-bottom: none;font-family: Roboto, 'Font Awesome 6 Pro'">
                                <input type="text" class="form-control" data-type="description" placeholder="Description &#xf1ab;" style="border-bottom-right-radius: 0; border-top-right-radius: 0; border-bottom: none;font-family: Roboto, 'Font Awesome 6 Pro'">
                        </div>
                        <div class="input-group">
                                <select class="form-select" data-type="typeSelect" select-type="toolInputArgumentTypeSelect" style="border-top-left-radius: 0;">
                                    <option value="1">String</option>
                                    <option value="2">Number</option>
                                    <option value="3">Boolean</option>
                                    <option value="4">Datetime</option>
                                </select>
                                <input type="text" class="form-control d-none" data-type="datetime-format" placeholder="Datetime Format (Example YYYY-MM-DD)">
                                <div class="form-check d-flex flex-row align-items-center justify-content-center m-0" style="background-color: #1a1a1a; border: 1px solid var(--bs-border-color); border-left: none">
                                    <div class="px-3">
                                        <input class="form-check-input" type="checkbox" data-type="required" id="toolInputArguementArray${dateTimeNow}">
                                        <label class="form-check-label" for="toolInputArguementArray${dateTimeNow}">
                                            Array?
                                        </label>
                                    </div>
                                </div>
                                <div class="form-check d-flex flex-row align-items-center justify-content-center m-0" style="background-color: #1a1a1a; border: 1px solid var(--bs-border-color); border-left: none; border-top-right-radius: 0;">
                                    <div class="px-3">
                                        <input class="form-check-input" type="checkbox" data-type="required" id="toolInputArguementRequired${dateTimeNow}">
                                        <label class="form-check-label" for="toolInputArguementRequired${dateTimeNow}">
                                            Required?
                                        </label>
                                    </div>
                                </div>
                        </div>
                    </div>
                    <button class="btn btn-danger" button-type="removeToolInputArgument" style="width: 50px;">
                        <i class="fa-regular fa-trash"></i>
                    </button>
                </div>
            `;

	return element;
}

function validateToolsAllMultilanguageElements() {
	if (ManageToolType == null) return;

	BusinessFullData.businessData.languages.forEach((language) => {
		const currentSelectedLanguage = SpecificationLanguagesListData.find((d) => d.id === language);

		// Tool Name
		const toolName = CurrentManageToolNameMultiLangData[currentSelectedLanguage.id];
		const toolNameIsInComplete = !toolName || toolName === "" || toolName.trim() === "";

		// Tool Description
		const toolShortDescription = CurrentManageToolShortDescriptionMultiLangData[currentSelectedLanguage.id];
		const toolShortDescriptionIsInComplete = !toolShortDescription || toolShortDescription === "" || toolShortDescription.trim() === "";

		// Tool Input Schemea List
		let isAnyInputSchemeaElementsInComplete = false;
		Object.keys(CurrentManageToolInputSchemeaMultiLangData).forEach((key) => {
			if (isAnyInputSchemeaElementsInComplete) return;

			const currentInputElement = CurrentManageToolInputSchemeaMultiLangData[key];
			const currentInputElementName = currentInputElement.name[currentSelectedLanguage.id];
			const currentInputElementDescription = currentInputElement.description[currentSelectedLanguage.id];

			const isCurrentElementNameInComplete = !currentInputElementName || currentInputElementName === "" || currentInputElementName.trim() === "";
			const isCurrentElementDescriptionInComplete = !currentInputElementDescription || currentInputElementDescription === "" || currentInputElementDescription.trim() === "";

			if (isCurrentElementNameInComplete || isCurrentElementDescriptionInComplete) {
				isAnyInputSchemeaElementsInComplete = true;
			}
		});

		// Tool Static Response List
		let isAnyResponseStaticResponseInComplete = false;
		Object.keys(CurrentManageToolResponseStaticResponse).forEach((key) => {
			if (isAnyResponseStaticResponseInComplete) return;

			const currentInputElementValue = CurrentManageToolResponseStaticResponse[key][currentSelectedLanguage.id];
			const isCurrentElementValueInComplete = !currentInputElementValue || currentInputElementValue === "" || currentInputElementValue.trim() === "";

			if (isCurrentElementValueInComplete) {
				isAnyResponseStaticResponseInComplete = true;
			}
		});

		// Final
		const isAnyInComplete = toolNameIsInComplete || toolShortDescriptionIsInComplete || isAnyInputSchemeaElementsInComplete || isAnyResponseStaticResponseInComplete;
		manageToolsLanguageDropdown.setLanguageStatus(currentSelectedLanguage.id, isAnyInComplete ? "incomplete" : "complete");
	});
}

function onToolsAudioUploadValidation(event) {
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

		toolAudioBeforeSpeakingUploadInput.val("");
		return false;
	}

	return true;
}

function CreateToolAudioWavesurfer(containerId) {
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

require(["vs/editor/editor.main", "esprima"], (_, parser) => {
	$(document).ready(() => {
		addNewToolbutton.on("click", (event) => {
			event.preventDefault();

			ResetAndEmptyToolsManageTab();
			currentToolName.text("New Tool");

			ManageToolType = "new";
			CurrentManageToolData = CreateToolsDefaultToolObject();

			ShowToolsManageTab();
		});

		switchBackToToolsTab.on("click", (event) => {
			event.preventDefault();

			toolManagerTab.removeClass("show");

			ShowToolsListTab();
		});

		addToolInputArgumentButton.on("click", (event) => {
			event.preventDefault();

			const index = CurrentManageToolSchemeaListIndex++;

			CurrentManageToolInputSchemeaMultiLangData[index] = {
				name: {},
				description: {},
			};

			BusinessFullData.businessData.languages.forEach((language) => {
				CurrentManageToolInputSchemeaMultiLangData[index].name[language] = "";
				CurrentManageToolInputSchemeaMultiLangData[index].description[language] = "";
			});

			toolInputArguementsList.append($(CreateToolsConfigurationInputSchemeaElement(index)));

			validateToolsAllMultilanguageElements();
		});

		$(document).on("change", '[select-type="toolInputArgumentTypeSelect" ]', (event) => {
			const target = $(event.currentTarget);

			const selectedValue = target.val();

			if (selectedValue === "datetime") {
				target.parent().find('[data-type="datetime-format"]').removeClass("d-none");
			} else {
				target.parent().find('[data-type="datetime-format"]').addClass("d-none");
			}
		});

		$(document).on("click", '[button-type="removeToolInputArgument"]', (event) => {
			event.preventDefault();

			const currentElement = $(event.currentTarget);
			const parentElement = currentElement.parent();

			const dataIndex = parentElement.attr("data-index");

			delete CurrentManageToolInputSchemeaMultiLangData[dataIndex];

			parentElement.remove();

			validateToolsAllMultilanguageElements();
		});

		addToolHeaderButton.on("click", (event) => {
			event.preventDefault();

			toolHeadersList.append(`
                              <div class="input-group mt-1 tool-header-box">
                                   <input type="text" class="form-control" data-type="key" placeholder="Key">
                                   <input type="text" class="form-control" data-type="value" placeholder="Value">
                                   <button class="btn btn-danger" button-type="removeToolHeader">
                                        <i class="fa-regular fa-trash"></i>
                                   </button>
                              </div>
                         `);
		});

		$(document).on("click", '[button-type="removeToolHeader"]', (event) => {
			event.preventDefault();

			$(event.currentTarget).parent().remove();
		});

		toolBodyType.on("change", (event) => {
			const target = $(event.currentTarget);
			const value = parseInt(target.val());

			if (value === 1 || value === 2) {
				toolBodyNone.addClass("d-none");
				toolBodyRawView.addClass("d-none");

				toolBodyKeyValueView.removeClass("d-none");
			} else if (value === 3) {
				toolBodyNone.addClass("d-none");
				toolBodyKeyValueView.addClass("d-none");

				toolBodyRawView.removeClass("d-none");
			} else {
				toolBodyKeyValueView.addClass("d-none");
				toolBodyRawView.addClass("d-none");

				toolBodyNone.removeClass("d-none");
			}
		});

		addToolBodyKeyValueButton.on("click", (event) => {
			event.preventDefault();

			toolBodyKeyValueViewList.append(`
                              <div class="input-group mt-1">
                                   <input type="text" class="form-control" data-type="key" placeholder="Key">
                                   <input type="text" class="form-control" data-type="value" placeholder="Value">
                                   <button class="btn btn-danger" button-type="removeToolBodyKeyValue">
                                        <i class="fa-regular fa-trash"></i>
                                   </button>
                              </div>
                         `);
		});

		$(document).on("click", '[button-type="removeToolBodyKeyValue"]', (event) => {
			event.preventDefault();

			$(event.currentTarget).parent().remove();
		});

		addToolResponseStatusTypeButton.on("click", (event) => {
			event.preventDefault();

			addToolResponseStatusTypeButton.prop("disabled", true);
			setTimeout(
				() => {
					addToolResponseStatusTypeButton.prop("disabled", false);
				},
				toolResponseStatusTypeListButtons.children().length === 0 ? 500 : 100,
			);

			const selectedResponseType = toolResponseStatusSelect.val();
			const selectedOptionElementChild = toolResponseStatusSelect.children();

			let selectedOptionElement = null;
			selectedOptionElementChild.each((index, element) => {
				if ($(element).val() === selectedResponseType) {
					selectedOptionElement = $(element);
				}
			});

			if (!selectedOptionElement) {
				alert("Please select a valid status code");
				return;
			}

			$(".responseStatusBox").addClass("d-none");

			const editorId = `responseStatus${selectedResponseType}CodeInput`;

			const elementData = $(`
                <div class="responseStatusBox" status-type="${selectedResponseType}">
                    <h5 class="mb-3">${selectedResponseType} Response</h5>
                    <div class="mb-3">
                        <div class="d-flex flex-row align-items-center justify-content-between mb-2">
                                <label class="form-label mb-0">Javascript Code</label>
                                <button class="btn btn-danger" button-type="removeToolResponseStatusType" status-type="${selectedResponseType}">
                                    <i class="fa-regular fa-trash"></i>
                                </button>
                        </div>
                        <div id="${editorId}" element-type="toolResponseStatusElement" status-code="${selectedResponseType}">
                                <div class="mon-editor"></div>
                                <div class="error-result-container">
                                    <p class="returnAlert d-none m-0 mt-2">
                                        <span class="d-block"><i class="fa-regular fa-circle-exclamation"></i> Does not contain a return statement.<br>Make sure a value is returned in every possible scenario.</span>
                                        <span style="color: orange" class="d-block">- Every If condition that is returning a value must have an else statement returning a value too. Even if the condition will always be true.<br>- Wrapping or using IIFE in the code is not supported: <code>(() => { ... })()</code></span>
                                    </p>

                                    <ul></ul>
                                </div>
                        </div>
                    </div>
                    <div>
                        <div class="form-check form-switch">
                                <input class="form-check-input" type="checkbox" role="switch" status-type="${selectedResponseType}" input-type="toolResponseStatusSpeakStaticResponseCheck" id="toolResponseStatusSpeakStaticResponseCheck${selectedResponseType}">
                                <label class="form-check-label" for="toolResponseStatusSpeakStaticResponseCheck${selectedResponseType}">
                                    <span>Speak Static Response</span>
									<i class="fa-regular fa-language"></i>
                                </label>
                                <a href="#" class="d-inline-block" data-bs-toggle="tooltip" data-bs-placement="right" data-bs-html="true" data-bs-title="Predefined response for AI to speak rather than a generated one.<br><br>Use {{response_data}} variable to include the response data in the spoken response.">
                                    <i class="fa-regular fa-circle-question"></i>
                                </a>
                        </div>
                        <div class="mt-2 d-none">
                                <input type="text" class="form-control" status-type="${selectedResponseType}" input-type="toolResponseStatusSpeakStaticResponseText" id="toolResponseStatusSpeakStaticResponseText${selectedResponseType}" placeholder="Static response for AI to speak">
                        </div>
                    </div>
                </div>
            `);
			toolResponseStatusTypeList.append(elementData);

			const staticResponseTooltip = new bootstrap.Tooltip(elementData.find('a[data-bs-toggle="tooltip"]'));

			initResponseCodeEditor(selectedResponseType, editorId);

			$('[button-type="selectToolResponseStatusType"]').removeClass("active");
			toolResponseStatusTypeListButtons.append(`
                              <button class="btn btn-light me-2 active" button-type="selectToolResponseStatusType" status-type="${selectedResponseType}">
                                   ${selectedResponseType}
                              </button>
                         `);

			selectedOptionElement.remove();
		});

		$(document).on("click", '[input-type="toolResponseStatusSpeakStaticResponseCheck"]', (event) => {
			const statusType = $(event.currentTarget).attr("status-type");

			$(`[input-type="toolResponseStatusSpeakStaticResponseText"][status-type="${statusType}"]`).parent().toggleClass("d-none");

			const isChecked = $(event.currentTarget).is(":checked");

			if (isChecked) {
				CurrentManageToolResponseStaticResponse[statusType] = {};

				BusinessFullData.businessData.languages.forEach((language) => {
					CurrentManageToolResponseStaticResponse[statusType][language] = "";
				});
			} else {
				delete CurrentManageToolResponseStaticResponse[statusType];
			}

			validateToolsAllMultilanguageElements();
		});

		$(document).on("click", '[button-type="removeToolResponseStatusType"]', (event) => {
			event.preventDefault();

			const statusType = $(event.currentTarget).attr("status-type");

			const thisStatusMonacoEditorIndex = responseStatusMonacoEditors.findIndex((x) => x.statusType === statusType);
			if (thisStatusMonacoEditorIndex === -1) {
				alert("Please select a valid status code. Error 0");
				return;
			}

			const thisStatusMonacoEditor = responseStatusMonacoEditors[thisStatusMonacoEditorIndex];

			thisStatusMonacoEditor.editor.dispose();
			const allMonacoModels = monaco.editor.getModels();
			const thisEditorModelIndex = allMonacoModels.findIndex((x) => x._associatedResource._formatted === `ts:filename/response${statusType}.d.ts`);
			if (thisEditorModelIndex === -1) {
				alert("Please select a valid status code. Error 1");
				return;
			}
			allMonacoModels[thisEditorModelIndex].dispose();

			responseStatusMonacoEditors.splice(thisStatusMonacoEditorIndex, 1);
			toolResponseStatusTypeList.find(`[status-type="${statusType}"]`).remove();

			const statusTypeData = HTTPStatusCodeList.find((x) => x.code === parseInt(statusType));
			toolResponseStatusSelect.children().each((index, element) => {
				if (parseInt($(element).val()) > parseInt(statusType)) {
					$(`<option value="${statusTypeData.code}">${statusTypeData.code} | ${statusTypeData.name}</option>`).insertBefore(element);
					return false;
				}

				if (index + 1 === toolResponseStatusSelect.children().length) {
					$(`<option value="${statusTypeData.code}">${statusTypeData.code} | ${statusTypeData.name}</option>`).insertAfter(element);
					return false;
				}
			});

			$(`[button-type="selectToolResponseStatusType"][status-type="${statusType}"]`).remove();

			if (toolResponseStatusTypeListButtons.children().length > 0) {
				toolResponseStatusTypeListButtons.children()[0].click();
			}

			delete CurrentManageToolResponseStaticResponse[statusType];

			validateToolsAllMultilanguageElements();
		});

		$(document).on("click", '[button-type="selectToolResponseStatusType"]', (event) => {
			event.preventDefault();

			const statusType = $(event.currentTarget).attr("status-type");

			$(".responseStatusBox").addClass("d-none");

			$(`.responseStatusBox[status-type="${statusType}"]`).removeClass("d-none");

			$('[button-type="selectToolResponseStatusType"]').removeClass("active");
			$(event.currentTarget).addClass("active");
		});

		toolAudioBeforeSpeakingSelect.on("change", (event) => {
			const selectedValue = $(event.currentTarget).val();

			if (selectedValue === "none") {
				toolAudioBeforeSpeakingBox.addClass("d-none");
			} else {
				toolAudioBeforeSpeakingBox.removeClass("d-none");
			}

			if (selectedValue === "custom") {
				toolAudioBeforeSpeakingInputBox.removeClass("d-none");
			} else {
				toolAudioBeforeSpeakingInputBox.addClass("d-none");
			}
		});

		toolAudioDuringSpeakingSelect.on("change", (event) => {
			const selectedValue = $(event.currentTarget).val();

			if (selectedValue === "none") {
				toolAudioDuringSpeakingBox.addClass("d-none");
			} else {
				toolAudioDuringSpeakingBox.removeClass("d-none");
			}

			if (selectedValue === "custom") {
				toolAudioDuringSpeakingInputBox.removeClass("d-none");
			} else {
				toolAudioDuringSpeakingInputBox.addClass("d-none");
			}
		});

		toolAudioAfterSpeakingSelect.on("change", (event) => {
			const selectedValue = $(event.currentTarget).val();

			if (selectedValue === "none") {
				toolAudioAfterSpeakingBox.addClass("d-none");
			} else {
				toolAudioAfterSpeakingBox.removeClass("d-none");
			}

			if (selectedValue === "custom") {
				toolAudioAfterSpeakingInputBox.removeClass("d-none");
			} else {
				toolAudioAfterSpeakingInputBox.addClass("d-none");
			}
		});

		toolAudioBeforeSpeakingUploadBtn.on("click", (event) => {
			event.preventDefault();

			toolAudioBeforeSpeakingUploadInput.click();
		});

		toolAudioDuringSpeakingUploadBtn.on("click", (event) => {
			event.preventDefault();

			toolAudioDuringSpeakingUploadInput.click();
		});

		toolAudioAfterSpeakingUploadBtn.on("click", (event) => {
			event.preventDefault();

			toolAudioAfterSpeakingUploadInput.click();
		});

		toolAudioBeforeSpeakingUploadInput.on("change", (event) => {
			const resultValidate = onToolsAudioUploadValidation(event);

			if (resultValidate) {
				const file = toolAudioBeforeSpeakingUploadInput[0].files[0];

				const reader = new FileReader();

				reader.onload = (evt) => {
					const blob = new window.Blob([new Uint8Array(evt.target.result)]);
					ToolAudioBeforeSpeakingWaveSurfer.loadBlob(blob);

					toolAudioBeforeSpeakingInputBox.find(".no-audio-notice").addClass("d-none");
					toolAudioBeforeSpeakingInputBox.find(".recording-container-waveform").removeClass("d-none");
					toolAudioBeforeSpeakingInputBox.find(".audio-controller").removeClass("d-none");
				};

				reader.onerror = (evt) => {
					AlertManager.createAlert({
						type: "error",
						message: "Error reading audio file for tool audio before speaking upload.",
						enableDismiss: false,
					});
				};

				// Read File as an ArrayBuffer
				reader.readAsArrayBuffer(file);
			}
		});

		toolAudioDuringSpeakingUploadInput.on("change", (event) => {
			const resultValidate = onToolsAudioUploadValidation(event);

			if (resultValidate) {
				const file = toolAudioDuringSpeakingUploadInput[0].files[0];

				const reader = new FileReader();

				reader.onload = (evt) => {
					const blob = new window.Blob([new Uint8Array(evt.target.result)]);
					ToolAudioDuringSpeakingWaveSurfer.loadBlob(blob);

					toolAudioDuringSpeakingInputBox.find(".no-audio-notice").addClass("d-none");
					toolAudioDuringSpeakingInputBox.find(".recording-container-waveform").removeClass("d-none");
					toolAudioDuringSpeakingInputBox.find(".audio-controller").removeClass("d-none");
				};

				reader.onerror = (evt) => {
					AlertManager.createAlert({
						type: "error",
						message: "Error reading audio file for tool audio during speaking upload.",
						enableDismiss: false,
					});
				};

				// Read File as an ArrayBuffer
				reader.readAsArrayBuffer(file);
			}
		});

		toolAudioAfterSpeakingUploadInput.on("change", (event) => {
			const resultValidate = onToolsAudioUploadValidation(event);

			if (resultValidate) {
				const file = toolAudioAfterSpeakingUploadInput[0].files[0];

				const reader = new FileReader();

				reader.onload = (evt) => {
					const blob = new window.Blob([new Uint8Array(evt.target.result)]);
					ToolAudioAfterSpeakingWaveSurfer.loadBlob(blob);

					toolAudioAfterSpeakingInputBox.find(".no-audio-notice").addClass("d-none");
					toolAudioAfterSpeakingInputBox.find(".recording-container-waveform").removeClass("d-none");
					toolAudioAfterSpeakingInputBox.find(".audio-controller").removeClass("d-none");
				};

				reader.onerror = (evt) => {
					AlertManager.createAlert({
						type: "error",
						message: "Error reading audio file for tool audio after speaking upload.",
						enableDismiss: false,
					});
				};

				// Read File as an ArrayBuffer
				reader.readAsArrayBuffer(file);
			}
		});

		const manageToolsLanguageDropdownInterval = setInterval(() => {
			if (manageToolsLanguageDropdown != null) {
				manageToolsLanguageDropdown.onLanguageChange((language) => {
					// Tool Name
					const toolNameValue = CurrentManageToolNameMultiLangData[language.id];
					inputToolName.val(toolNameValue);

					// Tool Description
					const toolShortDescriptionValue = CurrentManageToolShortDescriptionMultiLangData[language.id];
					inputToolShortDescription.val(toolShortDescriptionValue);

					// Tool Input Schema
					Object.keys(CurrentManageToolInputSchemeaMultiLangData).forEach((key) => {
						const inputSchemeaValue = CurrentManageToolInputSchemeaMultiLangData[key];

						toolInputArguementsList.find(`.toolInputArguementBox[data-index="${key}"] input[data-type="name"]`).val(inputSchemeaValue.name[language.id]);
						toolInputArguementsList.find(`.toolInputArguementBox[data-index="${key}"] input[data-type="description"]`).val(inputSchemeaValue.description[language.id]);
					});

					// Tool Status Static Response
					Object.keys(CurrentManageToolResponseStaticResponse).forEach((key) => {
						const staticResponseValue = CurrentManageToolResponseStaticResponse[key][language.id];

						toolResponseStatusTypeList.find(`input[status-type="${key}"][input-type="toolResponseStatusSpeakStaticResponseText"]`).val(staticResponseValue);
					});

					validateToolsAllMultilanguageElements();
				});

				inputToolName.on("input change", (event) => {
					const currentSelectedLanguage = manageToolsLanguageDropdown.getSelectedLanguage();

					const currentValue = $(event.currentTarget).val();

					CurrentManageToolNameMultiLangData[currentSelectedLanguage.id] = currentValue;

					validateToolsAllMultilanguageElements();
				});

				inputToolShortDescription.on("input change", (event) => {
					const currentSelectedLanguage = manageToolsLanguageDropdown.getSelectedLanguage();

					const currentValue = $(event.currentTarget).val();

					CurrentManageToolShortDescriptionMultiLangData[currentSelectedLanguage.id] = currentValue;

					validateToolsAllMultilanguageElements();
				});

				toolInputArguementsList.on("input change", '.toolInputArguementBox input[data-type="name"], .toolInputArguementBox input[data-type="description"]', (event) => {
					const currentSelectedLanguage = manageToolsLanguageDropdown.getSelectedLanguage();

					const currentElement = $(event.currentTarget);
					const elementBoxParent = currentElement.parent().parent().parent();

					const dataType = currentElement.attr("data-type");
					const dataIndex = elementBoxParent.attr("data-index");

					if (dataType === "name") {
						if (!CurrentManageToolInputSchemeaMultiLangData[dataIndex].name) {
							CurrentManageToolInputSchemeaMultiLangData[dataIndex].name = {};
						}

						CurrentManageToolInputSchemeaMultiLangData[dataIndex].name[currentSelectedLanguage.id] = currentElement.val();
					} else if (dataType === "description") {
						if (!CurrentManageToolInputSchemeaMultiLangData[dataIndex].description) {
							CurrentManageToolInputSchemeaMultiLangData[dataIndex].description = {};
						}

						CurrentManageToolInputSchemeaMultiLangData[dataIndex].description[currentSelectedLanguage.id] = currentElement.val();
					}

					validateToolsAllMultilanguageElements();
				});

				toolResponseStatusTypeList.on("input change", 'input[input-type="toolResponseStatusSpeakStaticResponseText"]', (event) => {
					const currentSelectedLanguage = manageToolsLanguageDropdown.getSelectedLanguage();

					const currentElement = $(event.currentTarget);
					const statusType = currentElement.attr("status-type");
					const currentValue = currentElement.val();

					CurrentManageToolResponseStaticResponse[statusType][currentSelectedLanguage.id] = currentValue;

					validateToolsAllMultilanguageElements();
				});

				clearInterval(manageToolsLanguageDropdownInterval);
			}
		}, 100);

		toolManagerTab.on("input change", "input, textarea", (event) => {
			event.stopPropagation();

			if (ManageToolType == null) return;

			CheckToolsManageTabHasChanges(true);
		});

		$("#nav-bar").on("tabChange", async (event) => {
			const activeTab = event.detail.from;
			if (activeTab !== "tools-tab") return;

			if (ManageToolType == null) return;

			const toolsChanges = CheckToolsManageTabHasChanges(false);
			if (toolsChanges.hasChanges) {
				const confirmDiscardChangesDialog = new BootstrapConfirmDialog({
					title: "Unsaved Changes Pending",
					message: "You have unsaved changes in tools manage tab. Are you sure you want to discard these changes and leave the tools tab?",
					confirmText: "Discard",
					cancelText: "Cancel",
					confirmButtonClass: "btn-danger",
					modalClass: "modal-lg",
				});

				const confirmDiscardChangesResult = await confirmDiscardChangesDialog.show();

				if (!confirmDiscardChangesResult) {
					event.preventDefault();
					return;
				}

				switchBackToToolsTab.click();
				ManageToolType = null;
			}
		});

		confirmPublishToolButton.on("click", async (event) => {
			event.preventDefault();

			if (IsSavingToolManageTab) return;

			// TODO PERFORM VALIDATIONS

			const toolsManageTabChanges = CheckToolsManageTabHasChanges(false);
			if (!toolsManageTabChanges.hasChanges) {
				return;
			}

			confirmPublishToolButton.prop("disabled", true);
			confirmPublishToolButtonSpinner.removeClass("d-none");

			IsSavingToolManageTab = true;

			const formData = new FormData();

			formData.append("postType", ManageToolType);
			formData.append("changes", JSON.stringify(toolsManageTabChanges.changes));

			if (ManageToolType === "edit") {
				formData.append("exisitingToolId", ManageToolId);
			}

			if (toolAudioBeforeSpeakingUploadInput[0].files.length > 0) {
				formData.append("audioBeforeSpeaking", toolAudioBeforeSpeakingUploadInput[0].files[0]);
			}

			if (toolAudioAfterSpeakingUploadInput[0].files.length > 0) {
				formData.append("audioAfterSpeaking", toolAudioAfterSpeakingUploadInput[0].files[0]);
			}

			if (toolAudioAfterSpeakingUploadInput[0].files.length > 0) {
				formData.append("audioAfterSpeaking", toolAudioAfterSpeakingUploadInput[0].files[0]);
			}

			SaveBusinessTool(
				formData,
				(saveResponse) => {
					// todo

					alert("success saving tool manage data");
				},
				(saveError, isUnsuccessful) => {
					AlertManager.createAlert({
						type: "danger",
						message: "Error occured while saving business tool data. Check browser console for logs.",
						timeout: 6000,
					});

					console.log("Error occured while saving business tool data: ", saveError);

					confirmPublishToolButton.prop("disabled", false);
					confirmPublishToolButtonSpinner.addClass("d-none");

					IsSavingToolManageTab = false;
				},
			);
		});
	});
});
