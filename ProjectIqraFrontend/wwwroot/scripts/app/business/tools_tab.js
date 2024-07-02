const HTTPStatusCodeList = [
    { "code": 100, "name": "Continue" }, { "code": 101, "name": "Switching Protocols" }, { "code": 102, "name": "Processing (WebDAV)" }, { "code": 103, "name": "Early Hints" }, { "code": 200, "name": "OK" }, { "code": 201, "name": "Created" }, { "code": 202, "name": "Accepted" }, { "code": 203, "name": "Non-Authoritative Information" }, { "code": 204, "name": "No Content" }, { "code": 205, "name": "Reset Content" }, { "code": 206, "name": "Partial Content" }, { "code": 207, "name": "Multi-Status (WebDAV)" }, { "code": 208, "name": "Already Reported (WebDAV)" }, { "code": 226, "name": "IM Used (HTTP Delta encoding)" }, { "code": 300, "name": "Multiple Choices" }, { "code": 301, "name": "Moved Permanently" }, { "code": 302, "name": "Found" }, { "code": 303, "name": "See Other" }, { "code": 304, "name": "Not Modified" }, { "code": 305, "name": "Use Proxy Deprecated" }, { "code": 307, "name": "Temporary Redirect" }, { "code": 308, "name": "Permanent Redirect" }, { "code": 400, "name": "Bad Request" }, { "code": 401, "name": "Unauthorized" }, { "code": 402, "name": "Payment Required Experimental" }, { "code": 403, "name": "Forbidden" }, { "code": 404, "name": "Not Found" }, { "code": 405, "name": "Method Not Allowed" }, { "code": 406, "name": "Not Acceptable" }, { "code": 407, "name": "Proxy Authentication Required" }, { "code": 408, "name": "Request Timeout" }, { "code": 409, "name": "Conflict" }, { "code": 410, "name": "Gone" }, { "code": 411, "name": "Length Required" }, { "code": 412, "name": "Precondition Failed" }, { "code": 413, "name": "Payload Too Large" }, { "code": 414, "name": "URI Too Long" }, { "code": 415, "name": "Unsupported Media Type" }, { "code": 416, "name": "Range Not Satisfiable" }, { "code": 417, "name": "Expectation Failed" }, { "code": 418, "name": "I'm a teapot" }, { "code": 421, "name": "Misdirected Request" }, { "code": 422, "name": "Unprocessable Content (WebDAV)" }, { "code": 423, "name": "Locked (WebDAV)" }, { "code": 424, "name": "Failed Dependency (WebDAV)" }, { "code": 425, "name": "Too Early Experimental" }, { "code": 426, "name": "Upgrade Required" }, { "code": 428, "name": "Precondition Required" }, { "code": 429, "name": "Too Many Requests" }, { "code": 431, "name": "Request Header Fields Too Large" }, { "code": 451, "name": "Unavailable For Legal Reasons" }, { "code": 500, "name": "Internal Server Error" }, { "code": 501, "name": "Not Implemented" }, { "code": 502, "name": "Bad Gateway" }, { "code": 503, "name": "Service Unavailable" }, { "code": 504, "name": "Gateway Timeout" }, { "code": 505, "name": "HTTP Version Not Supported" }, { "code": 506, "name": "Variant Also Negotiates" }, { "code": 507, "name": "Insufficient Storage (WebDAV)" }, { "code": 508, "name": "Loop Detected (WebDAV)" }, { "code": 510, "name": "Not Extended" }, { "code": 511, "name": "Network Authentication Required" }
];

var responseStatusMonacoEditors = []; // { statusType: xxx, editor: xyz}

require.config({
    paths: {
        vs: '/libs/monaco-editor-0.48.0/package/min/vs',
        esprima: '/libs/esprima-4.0.1/dist/esprima.js'
    },
});

require(['vs/editor/editor.main', 'esprima'], function (_, parser) {
    $(document).ready(() => {
        const tooltipTriggerList = document.querySelectorAll('#tools-tab [data-bs-toggle="tooltip"]');
        const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));

        const addNewToolbutton = $("#addNewToolButton");
        const switchBackToToolsTab = $("#switchBackToToolsTab");

        const toolListTab = $("#toolListTab");
        const toolManagerTab = $("#toolManagerTab");

        const currentToolName = $("#currentToolName");

        const addToolInputArgumentButton = $("#addToolInputArgument");
        const toolInputArguementsList = $("#toolInputArguementsList");

        const toolHeadersList = $("#toolHeadersList");
        const addToolHeaderButton = $("#addToolHeader");

        const toolBodyType = $('[name="toolBodyTypeCheckbox"]');
        const toolBodyNone = $("#toolBodyNone");
        const toolBodyKeyValueView = $("#toolBodyKeyValueView");
        const toolBodyKeyValueViewList = $("#toolBodyKeyValueViewList");
        const toolBodyRawView = $("#toolBodyRawView");
        const addToolBodyKeyValueButton = $("#addToolBodyKeyValue");

        const toolResponseStatusTypeListButtons = $("#toolResponseStatusTypeListButtons");
        const toolResponseStatusTypeList = $("#toolResponseStatusTypeList");
        const toolResponseStatusSelect = $("#toolResponseStatusSelect");
        const addToolResponseStatusTypeButton = $("#addToolResponseStatusType");

        const toolAudioBeforeSpeakingSelect = $("#toolAudioBeforeSpeakingSelect");
        const toolAudioBeforeSpeakingBox = $("#toolAudioBeforeSpeakingBox");
        const toolAudioBeforeSpeakingUpload = $("#toolAudioBeforeSpeakingUpload");

        const toolAudioDuringSpeakingSelect = $("#toolAudioDuringSpeakingSelect");
        const toolAudioDuringSpeakingBox = $("#toolAudioDuringSpeakingBox");
        const toolAudioDuringSpeakingUpload = $("#toolAudioDuringSpeakingUpload");

        const toolAudioAfterSpeakingSelect = $("#toolAudioAfterSpeakingSelect");
        const toolAudioAfterSpeakingBox = $("#toolAudioAfterSpeakingBox");
        const toolAudioAfterSpeakingUpload = $("#toolAudioAfterSpeakingUpload");

        function addResponseStatusOptions() {
            toolResponseStatusSelect.children().remove();

            HTTPStatusCodeList.forEach((element) => {
                toolResponseStatusSelect.append(`<option value="${element.code}">${element.code} | ${element.name}</option>`);
            });
        }

        function initResponseCodeEditor(statusType, containerId) {
            monaco.languages.typescript.javascriptDefaults.setDiagnosticsOptions({
                noSemanticValidation: false,
                noSyntaxValidation: false,
                diagnosticCodesToIgnore: [1108]
            });

            monaco.languages.typescript.javascriptDefaults.setCompilerOptions({
                target: monaco.languages.typescript.ScriptTarget.ES2015,
                allowNonTsExtensions: true,
            });

            var libUri = `ts:filename/response${statusType}.d.ts`;
            var libSource = "const responseData = any";
            monaco.languages.typescript.javascriptDefaults.addExtraLib(libSource, libUri);
            monaco.editor.createModel(libSource, "typescript", monaco.Uri.parse(libUri));

            var editor = monaco.editor.create($(`#${containerId} .mon-editor`)[0], {
                theme: "vs-dark",
                value: 'return responseData;',
                language: 'javascript',
                automaticLayout: true,

                scrollBeyondLastLine: false
            });

            function validateCode() {
                var code = editor.getValue();
                var ast = esprima.parseScript(code, { tolerant: true });

                var returnStatements = [];

                function hasReturnStatement(node) {
                    let result = null;

                    if (node.type === 'ReturnStatement') {
                        result = node.argument !== null;
                        returnStatements.push(result);
                    }

                    if (node.type === 'BlockStatement' || node.type === 'TryStatement') {
                        node.body.forEach((subNode) => {
                            result = hasReturnStatement(subNode);
                        });
                    }

                    if (node.type === 'IfStatement') {
                        result = hasReturnStatement(node.consequent);

                        if (node.alternate) {
                            result = hasReturnStatement(node.alternate);
                            returnStatements.push(result);
                        }
                        else {
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
            editor.onDidChangeModelContent(function (changes) {
                var isValid = validateCode();

                var alert = $(`#${containerId} .error-result-container .returnAlert`);

                if (!isValid) {
                    alert.removeClass("d-none");
                }
                else {
                    alert.addClass("d-none");
                }


            });

            monaco.editor.onDidChangeMarkers(([uri]) => {
                if (uri.scheme !== "inmemory") return;
                let thisModel = monaco.editor.getModel(uri);
                let editorsList = monaco.editor.getEditors(uri);

                var thisEditorParent;
                editorsList.forEach((editor) => {
                    if (editor.getModel() !== thisModel) return;
                    thisEditorParent = editor.getContainerDomNode().parentNode;
                });
                if (!thisEditorParent) return;

                if ($(thisEditorParent).attr('element-type') !== "toolResponseStatusElement") return;
                const markers = monaco.editor.getModelMarkers({ resource: uri });

                var list = $(thisEditorParent).find(`.error-result-container ul`);
                var listData = markers.map(
                    ({ message, startLineNumber, startColumn, endLineNumber, endColumn }) => {
                        if (!message || message.trim() == "") return;
                        return `<li>${message} [${startLineNumber}:${startColumn}-${endLineNumber}:${endColumn}]</li>`;
                    }
                );

                if (listData.length > 0) {
                    list.html(`
                                             <span class="btn-ic-span-align"><i class="fa-regular fa-circle-exclamation"></i> <span>${listData.length} Errors</span></span>
                                             ${listData.join("")}
                                        `);
                }
                else {
                    list.html('');
                }
            });

            responseStatusMonacoEditors.push({ statusType: statusType, editor: editor });
        }

        function resetOrClearEverything() {
            toolInputArguementsList.children().remove();
            toolHeadersList.children().remove();

            toolResponseStatusTypeListButtons.children().remove();
            toolResponseStatusTypeList.children().remove();
            responseStatusMonacoEditors.forEach((data) => {
                data.editor.dispose();
            });
            responseStatusMonacoEditors = [];

            let allMonacoModels = monaco.editor.getModels();
            allMonacoModels.forEach((model) => {
                model.dispose();
            });

            $("#toolBodyTypeNone").click();
            toolBodyKeyValueViewList.children().remove();
            $("#toolBodyRawTextarea").val("");

            $("#inputToolType").val(0).change();

            $("#inputToolName").val("");
            $("#inputToolShortDescription").val("");
            $("#inputToolURL").val("");
            $("#inputToolServiceName").val("");
        }

        addNewToolbutton.on('click', (event) => {
            event.preventDefault();

            currentToolName.text("New Tool");
            addResponseStatusOptions();

            toolListTab.removeClass("show");
            setTimeout(() => {
                toolListTab.addClass("d-none");

                toolManagerTab.removeClass("d-none");
                setTimeout(() => {
                    toolManagerTab.addClass("show");
                }, 10);
            }, 150);
        });

        switchBackToToolsTab.on('click', (event) => {
            event.preventDefault();

            toolManagerTab.removeClass("show");

            setTimeout(() => {
                toolManagerTab.addClass("d-none");

                resetOrClearEverything();

                toolListTab.removeClass("d-none");
                setTimeout(() => {
                    toolListTab.addClass("show");
                }, 10);
            }, 150);
        });

        addToolInputArgumentButton.on('click', (event) => {
            event.preventDefault();

            let dateTimeNow = Date.now();
            console.log(dateTimeNow);
            toolInputArguementsList.append(`
                              <div class="toolInputArguementBox input-group mt-1">
                                   <div style="width: calc(100% - 50px)">
                                        <div class="input-group">
                                             <input type="text" class="form-control" data-type="name" placeholder="Argument Name" style="max-width: 250px; border-bottom-left-radius: 0; border-bottom: none;">
                                             <input type="text" class="form-control" data-type="description" placeholder="Description (explanation for AI)" style="border-bottom-right-radius: 0; border-top-right-radius: 0; border-bottom: none;">
                                        </div>
                                        <div class="input-group">
                                             <select class="form-select" data-type="typeSelect" select-type="toolInputArgumentTypeSelect" style="border-top-left-radius: 0;">
                                                  <option value="string">String</option>
                                                  <option value="number">Number</option>
                                                  <option value="boolean">Boolean</option>
                                                  <option value="datetime">Datetime</option>
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
                         `);
        });

        $(document).on('change', '[select-type="toolInputArgumentTypeSelect" ]', (event) => {
            let target = $(event.currentTarget);

            let selectedValue = target.val();

            if (selectedValue === "datetime") {
                target.parent().find('[data-type="datetime-format"]').removeClass("d-none");
            }
            else {
                target.parent().find('[data-type="datetime-format"]').addClass("d-none");
            }
        });

        $(document).on('click', '[button-type="removeToolInputArgument"]', (event) => {
            event.preventDefault();

            $(event.currentTarget).parent().remove();
        });

        addToolHeaderButton.on('click', (event) => {
            event.preventDefault();

            toolHeadersList.append(`
                              <div class="input-group mt-1">
                                   <input type="text" class="form-control" data-type="key" placeholder="Key">
                                   <input type="text" class="form-control" data-type="value" placeholder="Value">
                                   <button class="btn btn-danger" button-type="removeToolHeader">
                                        <i class="fa-regular fa-trash"></i>
                                   </button>
                              </div>
                         `);
        });

        $(document).on('click', '[button-type="removeToolHeader"]', (event) => {
            event.preventDefault();

            $(event.currentTarget).parent().remove();
        });

        toolBodyType.on('change', (event) => {
            let target = $(event.currentTarget);
            let value = target.val();

            if (
                value === "form-data"
                ||
                value === "x-www-form-urlencoded"
            ) {
                toolBodyNone.addClass("d-none");
                toolBodyRawView.addClass("d-none");

                toolBodyKeyValueView.removeClass("d-none");
            }
            else if (
                value === "raw"
            ) {
                toolBodyNone.addClass("d-none");
                toolBodyKeyValueView.addClass("d-none");

                toolBodyRawView.removeClass("d-none");
            }
            else {
                toolBodyKeyValueView.addClass("d-none");
                toolBodyRawView.addClass("d-none");

                toolBodyNone.removeClass("d-none");
            }
        });

        addToolBodyKeyValueButton.on('click', (event) => {
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

        $(document).on('click', '[button-type="removeToolBodyKeyValue"]', (event) => {
            event.preventDefault();

            $(event.currentTarget).parent().remove();
        });

        addToolResponseStatusTypeButton.on('click', (event) => {
            event.preventDefault();

            addToolResponseStatusTypeButton.prop('disabled', true);
            setTimeout(() => {
                addToolResponseStatusTypeButton.prop('disabled', false);
            }, (toolResponseStatusTypeListButtons.children().length === 0 ? 500 : 100));

            let selectedResponseType = toolResponseStatusSelect.val();
            let selectedOptionElementChild = toolResponseStatusSelect.children();

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

            $('.responseStatusBox').addClass('d-none');

            let editorId = `responseStatus${selectedResponseType}CodeInput`;

            let elementData = $(`
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

                         `)
            toolResponseStatusTypeList.append(elementData);

            let staticResponseTooltip = new bootstrap.Tooltip(elementData.find('a[data-bs-toggle="tooltip"]'))

            initResponseCodeEditor(selectedResponseType, editorId);

            $('[button-type="selectToolResponseStatusType"]').removeClass('active');
            toolResponseStatusTypeListButtons.append(`
                              <button class="btn btn-light me-2 active" button-type="selectToolResponseStatusType" status-type="${selectedResponseType}">
                                   ${selectedResponseType}
                              </button>
                         `);

            selectedOptionElement.remove();
        });

        $(document).on('click', '[input-type="toolResponseStatusSpeakStaticResponseCheck"]', (event) => {
            let statusType = $(event.currentTarget).attr("status-type");

            $(`[input-type="toolResponseStatusSpeakStaticResponseText"][status-type="${statusType}"]`).parent().toggleClass('d-none');
        });

        $(document).on('click', '[button-type="removeToolResponseStatusType"]', (event) => {
            event.preventDefault();

            let statusType = $(event.currentTarget).attr("status-type");

            let thisStatusMonacoEditorIndex = responseStatusMonacoEditors.findIndex(x => x.statusType === statusType);
            if (thisStatusMonacoEditorIndex === -1) {
                alert("Please select a valid status code. Error 0");
                return;
            }

            let thisStatusMonacoEditor = responseStatusMonacoEditors[thisStatusMonacoEditorIndex];

            thisStatusMonacoEditor.editor.dispose();
            let allMonacoModels = monaco.editor.getModels();
            const thisEditorModelIndex = allMonacoModels.findIndex(x => x._associatedResource._formatted === `ts:filename/response${statusType}.d.ts`);
            if (thisEditorModelIndex === -1) {
                alert("Please select a valid status code. Error 1");
                return;
            }
            allMonacoModels[thisEditorModelIndex].dispose();

            responseStatusMonacoEditors.splice(thisStatusMonacoEditorIndex, 1);
            toolResponseStatusTypeList.find(`[status-type="${statusType}"]`).remove();

            let statusTypeData = HTTPStatusCodeList.find(x => x.code === parseInt(statusType));
            toolResponseStatusSelect.children().each((index, element) => {
                if (parseInt($(element).val()) > parseInt(statusType)) {
                    $(`<option value="${statusTypeData.code}">${statusTypeData.code} | ${statusTypeData.name}</option>`).insertBefore(element);
                    return false;
                }

                if ((index + 1) === toolResponseStatusSelect.children().length) {
                    $(`<option value="${statusTypeData.code}">${statusTypeData.code} | ${statusTypeData.name}</option>`).insertAfter(element);
                    return false;
                }
            });

            $(`[button-type="selectToolResponseStatusType"][status-type="${statusType}"]`).remove();

            if (toolResponseStatusTypeListButtons.children().length > 0) {
                toolResponseStatusTypeListButtons.children()[0].click();
            }
        });

        $(document).on('click', '[button-type="selectToolResponseStatusType"]', (event) => {
            event.preventDefault();

            let statusType = $(event.currentTarget).attr("status-type");

            $('.responseStatusBox').addClass('d-none');

            $(`.responseStatusBox[status-type="${statusType}"]`).removeClass('d-none');

            $('[button-type="selectToolResponseStatusType"]').removeClass('active');
            $(event.currentTarget).addClass('active');
        });

        toolAudioBeforeSpeakingSelect.on('change', (event) => {
            let selectedValue = $(event.currentTarget).val();

            if (selectedValue === "none") {
                toolAudioBeforeSpeakingBox.addClass('d-none');
            }
            else {
                toolAudioBeforeSpeakingBox.removeClass('d-none');
            }

            if (selectedValue === "custom") {
                toolAudioBeforeSpeakingUpload.removeClass('d-none');
            }
            else {
                toolAudioBeforeSpeakingUpload.addClass('d-none');
            }
        });

        toolAudioDuringSpeakingSelect.on('change', (event) => {
            let selectedValue = $(event.currentTarget).val();

            if (selectedValue === "none") {
                toolAudioDuringSpeakingBox.addClass('d-none');
            }
            else {
                toolAudioDuringSpeakingBox.removeClass('d-none');
            }

            if (selectedValue === "custom") {
                toolAudioDuringSpeakingUpload.removeClass('d-none');
            }
            else {
                toolAudioDuringSpeakingUpload.addClass('d-none');
            }
        });

        toolAudioAfterSpeakingSelect.on('change', (event) => {
            let selectedValue = $(event.currentTarget).val();

            if (selectedValue === "none") {
                toolAudioAfterSpeakingBox.addClass('d-none');
            }
            else {
                toolAudioAfterSpeakingBox.removeClass('d-none');
            }

            if (selectedValue === "custom") {
                toolAudioAfterSpeakingUpload.removeClass('d-none');
            }
            else {
                toolAudioAfterSpeakingUpload.addClass('d-none');
            }
        });

        // init
        addResponseStatusOptions();
    });
});