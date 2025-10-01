/**
 * CustomVariableInput.js
 * 
 * A powerful, custom input component for building templates with dynamic variables and functions.
 * Implemented using jQuery and designed to integrate with Bootstrap 5.3.
 * 
 * Features:
 * - Mix plain text with variable "pills".
 * - Variable selection from a searchable, grouped dropdown.
 * - Trigger dropdown via button or by typing a sequence (e.g., '{={').
 * - Keyboard navigation for all interactions.
 * - Smart pill deletion and rich tooltips.
 * - Support for nested object paths (e.g., object.property).
 * - Support for functions with variable and literal arguments.
 * - Recursive getValue() for accurate serialization of complex templates.
 */

class CustomVariableInput {
    /**
     * Initializes the Custom Variable Input component.
     * @param {HTMLElement|jQuery} element - The DOM element to attach the component to.
     * @param {Array<Object>} variables - An array of available variable and function objects.
     * @param {Object} [options={}] - Configuration options for the component.
     */
    constructor(element, variables, options = {}) {
        this.$element = $(element);
        this.variables = variables;
        this.options = $.extend({
            isMultiLine: false,
            placeholder: 'Enter a response...',
            triggerSequence: '{={',
            onValueChange: null
        }, options);

        // State properties
        this.activePillForPath = null;
        this.activeArgSlot = null;
        this.dropdownState = {
            history: [], // To track our path, e.g., ['conversation_history']
            currentList: this.variables
        };
        this.lastSelectionRange = null;

        this._render();
        this._cacheDOMElements();
        this._populateDropdown();
        this._bindEvents();
        this._updatePlaceholder();
    }

    // --- Private Lifecycle & Core Methods ---

    _render() {
        const html = `
            <div class="custom-variable-input-wrapper position-relative">
                <div class="editor-area form-control" contenteditable="true" spellcheck="false" aria-multiline="${this.options.isMultiLine}"></div>
                <div class="editor-placeholder text-muted">${this.options.placeholder}</div>
                <button class="btn btn-sm btn-outline-secondary variable-trigger-btn" type="button" 
                        data-bs-toggle="dropdown" data-bs-auto-close="outside" aria-expanded="false">
                    <i class="fa fa-brackets-curly"></i>
                </button>
                <div class="dropdown-menu variable-dropdown p-2 shadow-lg">
                    <input type="text" class="form-control form-control-sm mb-2 variable-search" placeholder="Search...">
                    <ul class="list-unstyled variable-list m-0"></ul>
                </div>
            </div>`;
        this.$element.html(html);
    }

    _cacheDOMElements() {
        this.$wrapper = this.$element.find('.custom-variable-input-wrapper');
        this.$editor = this.$element.find('.editor-area');
        this.$placeholder = this.$element.find('.editor-placeholder');
        this.$dropdown = this.$element.find('.variable-dropdown');
        this.$dropdownList = this.$element.find('.variable-list');
        this.$searchInput = this.$element.find('.variable-search');
        this.$triggerBtn = this.$element.find('.variable-trigger-btn');
    }

    _bindEvents() {
        // --- Editor & Wrapper Events ---
        this.$wrapper.on('click', (e) => {
            const $target = $(e.target);
            // Only focus the main editor if the click is on the wrapper's empty space or the placeholder
            if ($target.is(this.$wrapper) || $target.is(this.$placeholder)) {
                this.$editor.trigger('focus');
            }
        });
        this.$editor.on('focus', () => this.$wrapper.addClass('is-focused'));
        this.$editor.on('blur', (e) => {
            const currentTarget = $(e.currentTarget);
            if (currentTarget == this.$searchInput) return;

            this.$wrapper.removeClass('is-focused');
            const sel = window.getSelection();
            // Save the range only if there is a selection
            if (sel.rangeCount > 0) {
                this.lastSelectionRange = sel.getRangeAt(0);
            }
        });
        this.$editor.on('input focus blur keyup', (e) => {
            const currentTarget = $(e.currentTarget);
            if (currentTarget == this.$searchInput) return;

            this._updatePlaceholder()
        });
        this.$editor.on('input', (e) => {
            this._handleTriggerSequence(e);
            this._triggerValueChange();
        });
        this.$editor.on('keydown', (e) => {
            this._handlePillDeletion(e);
            this._handleObjectPathTriggerKey(e);
            if (!this.options.isMultiLine && e.key === 'Enter') {
                e.preventDefault();
                this.$element.trigger('submit');
            }
        });
        this.$editor.on('click', '.arg-slot', (e) => {
            e.preventDefault();
            e.stopPropagation();
            this._handleArgSlotClick($(e.currentTarget));
        });
        this.$editor.on('cut', (e) => this._handleCopy(e));
        this.$editor.on('copy', (e) => this._handleCopy(e));
        this.$editor.on('paste', (e) => this._handlePaste(e));
        this.$editor.on('click', '.arg-input', (e) => {
            e.stopPropagation(); // Prevent this click from bubbling up to the wrapper
            this._triggerValueChange();
        });

        // --- Dropdown Events ---
        this.$searchInput.on('keyup', (e) => this._filterDropdown($(e.target).val()));
        this.$dropdownList.on('click', 'a', (e) => {
            e.preventDefault();
            e.stopPropagation();

            var currentTarget = $(e.currentTarget);
            if (currentTarget.hasClass('drill-down-btn') || currentTarget.hasClass('dropdown-item-back')) return;

            const variableData = $(e.currentTarget).data('variable-data');

            if (this.dropdownState.history.length > 0) {
                // We are in a drill-down view, so we are inserting a sub-property directly.
                const parentVarData = this._findVariableById(this.dropdownState.parentId); // We need to store parentId

                const compositeData = {
                    id: `${parentVarData.id}.${variableData.id}`,
                    Name: `${parentVarData.Name} | ${variableData.Name}`,
                    Description: `${parentVarData.Name}:\n${parentVarData.Description}\n\n${variableData.Name}:\n${variableData.Description}`,
                    Type: variableData.Type
                };
                this._insertPill(compositeData);

            } else if (this.activeArgSlot) {
                this._fillArgumentSlot(variableData);
            } else if (this.activePillForPath) {
                this._updatePillForObjectPath(variableData);
            } else {
                this._insertPill(variableData);
            }

            bootstrap.Dropdown.getInstance(this.$triggerBtn)?.hide();
            this.$searchInput.val('').trigger('keyup');
        });
        if (this.$triggerBtn.length) {
            const triggerBtnEl = this.$triggerBtn[0];
            triggerBtnEl.addEventListener('shown.bs.dropdown', () => {
                this.$searchInput.trigger('focus');
                this.$wrapper.on('keydown.dropdownNavigation', (e) => this._handleDropdownNavigation(e));
            });
            triggerBtnEl.addEventListener('hidden.bs.dropdown', () => {
                this.$wrapper.off('keydown.dropdownNavigation');
                this.$dropdownList.find('.active').removeClass('active');
                this.activePillForPath = null;
                this.activeArgSlot = null;
                this._populateDropdown();
            });
        }
        this.$dropdownList.on('click', '.drill-down-btn', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const variableData = $(e.target).closest('a').data('variable-data');

            this.dropdownState.parentId = variableData.id;

            this._populateDropdown(variableData.schema, true);
        });
        this.$dropdownList.on('click', '.dropdown-item-back', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const previousList = this.dropdownState.history.pop();
            this._populateDropdown(previousList, false);
        });

        this.selectionChangeHandler = this._handleSelectionChange.bind(this);
        document.addEventListener('selectionchange', this.selectionChangeHandler);
    }

    // --- Private UI & State Management Methods ---

    _populateDropdown(variableList = this.variables, isDrillDown = false) {
        if (isDrillDown) {
            this.dropdownState.history.push(this.dropdownState.currentList);
        } else {
            this.dropdownState.history = [];
        }
        this.dropdownState.currentList = variableList || this.variables;

        this.$dropdownList.empty();

        if (this.dropdownState.history.length > 0) {
            this.$dropdownList.append(`
            <li>
                <a class="dropdown-item dropdown-item-back rounded-1" href="#">
                    <i class="bi bi-arrow-left-short"></i> Back
                </a>
            </li>
            <li><hr class="dropdown-divider"></li>
        `);
        }

        const grouped = this._groupVariables(variableList);
        if (Object.keys(grouped).length === 0) {
            this.$dropdownList.append('<li class="px-2 text-muted"><small>No compatible variables found.</small></li>');
            return;
        }
        for (const groupName in grouped) {
            this.$dropdownList.append(`<li><h6 class="dropdown-header text-primary">${groupName}</h6></li>`);
            grouped[groupName].forEach(variable => {
                if (variable.schema && variable.schema.length > 0) {
                    let itemHtml = `
						<div class="d-flex justify-content-between align-items-center">
							<div>
								<strong>${variable.Name}</strong>
								<small class="d-block text-muted text-wrap">${variable.Description}</small>
							</div>
							<button class="btn btn-sm btn-outline-secondary drill-down-btn"><i class="bi bi-chevron-right"></i></button>
						</div>
					`;
                    const item = $('<li><a class="dropdown-item rounded-1" href="#"></a></li>');
                    item.find('a').html(itemHtml).data('variable-data', variable);
                    this.$dropdownList.append(item);
                }
                else {
                    const item = $(`
						<li>
							<a class="dropdown-item rounded-1" href="#" data-id="${variable.id}">
								<strong>${variable.Name}</strong>
								<small class="d-block text-muted text-wrap">${variable.Description}</small>
							</a>
						</li>
					`);
                    item.find('a').data('variable-data', variable);
                    this.$dropdownList.append(item);
                }
            });
        }
    }

    _filterDropdown(searchTerm) {
        const term = searchTerm.toLowerCase();
        this.$dropdownList.find('a').each((i, el) => {
            const $el = $(el);
            const variable = $el.data('variable-data');
            const isVisible = variable.Name.toLowerCase().includes(term) || variable.Description.toLowerCase().includes(term);
            $el.parent().toggle(isVisible);
        });
    }

    _updatePlaceholder() {
        const hasContent = this.$editor.text().trim().length > 0 || this.$editor.find('.pill').length > 0;
        this.$placeholder.toggle(!hasContent);
    }

    _insertPill(variableData) {
        this.$editor.trigger('focus');
        const sel = window.getSelection();
        if (!sel) return;

        if (this.lastSelectionRange) {
            sel.removeAllRanges();
            sel.addRange(this.lastSelectionRange);
        }
        if (!sel.getRangeAt || !sel.rangeCount) return;

        // --- USE THE NEW HELPER ---
        // Note: We use variableData.id because we already have the full object here.
        const finalNode = this._createPillNode(variableData.id);

        // DOM insertion logic remains the same
        let range = sel.getRangeAt(0);
        range.deleteContents();
        range.insertNode(finalNode);
        new bootstrap.Tooltip(finalNode); // Initialize tooltip on the newly inserted node

        range.setStartAfter(finalNode);
        range.collapse(true);

        sel.removeAllRanges();
        sel.addRange(range);
        this._updatePlaceholder();
        this._triggerValueChange();
    }

    // --- Private Event Handlers ---

    _handleDropdownNavigation(e) {
        if (!['ArrowUp', 'ArrowDown', 'Enter', 'Escape'].includes(e.key)) return;
        e.preventDefault();
        e.stopPropagation();
        const visibleItems = this.$dropdownList.find('li:visible a');
        if (!visibleItems.length) return;
        let activeItem = visibleItems.filter('.active');
        let activeIndex = visibleItems.index(activeItem);
        switch (e.key) {
            case 'ArrowDown':
                activeIndex = activeIndex === -1 ? 0 : (activeIndex + 1) % visibleItems.length;
                break;
            case 'ArrowUp':
                activeIndex = activeIndex === -1 ? visibleItems.length - 1 : (activeIndex - 1 + visibleItems.length) % visibleItems.length;
                break;
            case 'Enter':
                this.$searchInput.trigger('blur');
                if (activeIndex > -1) activeItem.trigger('click');
                return;
            case 'Escape':
                bootstrap.Dropdown.getInstance(this.$triggerBtn)?.hide();
                this.$editor.trigger('focus');
                return;
        }
        activeItem.removeClass('active');
        const newActiveItem = $(visibleItems[activeIndex]).addClass('active');
        newActiveItem[0].scrollIntoView({
            block: 'nearest'
        });
    }

    _handlePillDeletion(e) {
        if (!['Backspace', 'Delete'].includes(e.key)) return;

        const sel = window.getSelection();
        if (!sel.isCollapsed) return;
        if ($(sel.anchorNode).hasClass('arg-input') || $(sel.anchorNode.parentElement).hasClass('arg-input')) return;

        const range = sel.getRangeAt(0);

        if (e.key === 'Backspace') {
            let nodeToDelete = null;

            // --- NEW LOGIC: Handles cursor directly after a pill (the boundary case) ---
            if (range.startContainer === this.$editor[0] && range.startOffset > 0) {
                nodeToDelete = this.$editor[0].childNodes[range.startOffset - 1];
            }
            // --- EXISTING LOGIC: Handles cursor inside a text node or pill argument ---
            else if (range.startOffset === 0) {
                const $parentPill = $(range.startContainer).closest('.pill');
                if ($parentPill.length) {
                    e.preventDefault();
                    $parentPill.remove();
                    this._triggerValueChange();
                    return; // Exit after handling
                }
                nodeToDelete = range.startContainer.previousSibling;
            }

            // Now, check the node we found, handling the space we add after pills
            if (nodeToDelete && nodeToDelete.nodeType === Node.TEXT_NODE && /^\s+$/.test(nodeToDelete.textContent)) {
                nodeToDelete = nodeToDelete.previousSibling;
            }

            if (nodeToDelete && $(nodeToDelete).hasClass('pill')) {
                e.preventDefault();
                $(nodeToDelete).remove();
                this._triggerValueChange();
            }
        }
        // You can add similar robust logic for the 'Delete' key if needed
    }

    _handleTriggerSequence(e) {
        const sel = window.getSelection();
        if (!sel.isCollapsed || sel.rangeCount === 0) return;
        const range = sel.getRangeAt(0);
        if (range.startContainer.nodeType !== Node.TEXT_NODE) return;
        const textBeforeCursor = range.startContainer.textContent.substring(0, range.startOffset);
        if (textBeforeCursor.endsWith(this.options.triggerSequence)) {
            const dropdownInstance = bootstrap.Dropdown.getOrCreateInstance(this.$triggerBtn);
            const triggerRange = document.createRange();
            triggerRange.setStart(range.startContainer, range.startOffset - this.options.triggerSequence.length);
            triggerRange.setEnd(range.startContainer, range.startOffset);
            const rect = triggerRange.getBoundingClientRect();
            const isZeroRect = rect.top === 0 && rect.left === 0 && rect.width === 0 && rect.height === 0;
            const virtualElement = {
                getBoundingClientRect: () => {
                    if (isZeroRect) {
                        const editorRect = this.$editor[0].getBoundingClientRect();
                        return DOMRect.fromRect({ x: editorRect.left, y: editorRect.top, width: 0, height: editorRect.height });
                    }
                    return rect;
                },
                contextElement: this.$editor[0]
            };
            dropdownInstance._config.reference = virtualElement;
            dropdownInstance.show();
            setTimeout(() => dropdownInstance._config.reference = this.$triggerBtn[0], 100);
            triggerRange.deleteContents();
        }
    }

    _handleObjectPathTriggerKey(e) {
        if (e.key !== '.') return;
        const sel = window.getSelection();
        if (sel.rangeCount === 0 || !sel.isCollapsed) return;
        const range = sel.getRangeAt(0);
        let nodeBeforeCursor = range.startOffset === 0 ? range.startContainer.previousSibling : range.startContainer;
        if (nodeBeforeCursor && nodeBeforeCursor.nodeType === Node.TEXT_NODE && /^\s+$/.test(nodeBeforeCursor.textContent)) nodeBeforeCursor = nodeBeforeCursor.previousSibling;
        if (nodeBeforeCursor && $(nodeBeforeCursor).hasClass('pill') && $(nodeBeforeCursor).data('type') === 'object') {
            e.preventDefault();
            this.activePillForPath = $(nodeBeforeCursor);
            const parentVar = this._findVariableById(this.activePillForPath.data('id'));
            if (parentVar?.schema) {
                this._populateDropdown(parentVar.schema);
                const dropdownInstance = bootstrap.Dropdown.getOrCreateInstance(this.$triggerBtn);
                const virtualElement = {
                    getBoundingClientRect: () => this.activePillForPath[0].getBoundingClientRect()
                };
                dropdownInstance._config.reference = virtualElement;
                dropdownInstance.show();
                setTimeout(() => dropdownInstance._config.reference = this.$triggerBtn[0], 100);
            }
        }
    }

    _handleArgSlotClick($slot) {
        const allowedTypes = $slot.data('allowed-types');
        const compatibleVariables = this.variables.filter(v => v.Type !== 'function' && allowedTypes.includes(v.Type));
        this._populateDropdown(compatibleVariables);
        this.activeArgSlot = $slot;
        const dropdownInstance = bootstrap.Dropdown.getOrCreateInstance(this.$triggerBtn);
        const virtualElement = {
            getBoundingClientRect: () => $slot[0].getBoundingClientRect()
        };
        dropdownInstance._config.reference = virtualElement;
        dropdownInstance.show();
        setTimeout(() => dropdownInstance._config.reference = this.$triggerBtn[0], 100);
    }

    _handleSelectionChange() {
        const sel = window.getSelection();

        // First, clear any existing selection styles from our pills
        this.$editor.find('.pill.is-selected').removeClass('is-selected');

        if (
            sel.anchorNode &&
            (
                $(sel.anchorNode).hasClass('arg-input') ||
                (sel.anchorNode.parentElement && $(sel.anchorNode.parentElement).hasClass('arg-input'))
            )
        ) {
            this.$editor.find('.function-pill .arg-input.is-focused').removeClass('is-focused');

            var currentArgInput = $(sel.anchorNode).hasClass('arg-input') ? $(sel.anchorNode) : $(sel.anchorNode.parentElement);
            currentArgInput.addClass('is-focused');
            return;
        }

        // If there's no selection or the selection is not in our editor, do nothing further.
        if (!sel.rangeCount || sel.isCollapsed || !this.$editor[0].contains(sel.anchorNode)) {
            this.$editor.find('.function-pill .arg-input.is-focused').removeClass('is-focused');

            return;
        }

        const range = sel.getRangeAt(0);

        if (range.startContainer === this.$editor[0]) {
            let nodeSelected = this.$editor[0].childNodes[range.startOffset];

            if ($(nodeSelected).hasClass('function-pill')) {
                var nodeWithFocused = $(nodeSelected).find('.arg-input.is-focused');

                if (nodeWithFocused.length > 0) {
                    var textNode = nodeWithFocused[0].firstChild;

                    sel.removeAllRanges();
                    const customRange = document.createRange();
                    customRange.selectNode(textNode ?? nodeWithFocused[0]);
                    if (textNode) {
                        customRange.setStart(textNode, 0);
                        customRange.setEnd(textNode, textNode.textContent.length);
                    }
                    else {
                        customRange.setStart(nodeWithFocused[0], 0);
                    }
                    sel.addRange(customRange);
                    return;
                }
            }
        }

        const allPills = this.$editor.find('.pill');

        // Iterate over every pill and check if it intersects with the selection range
        allPills.each((index, pillNode) => {
            if (range.intersectsNode(pillNode)) {
                $(pillNode).addClass('is-selected');
            }
        });

        this.$editor.find('.function-pill .arg-input.is-focused').removeClass('is-focused');
    }

    _updatePillForObjectPath(variableData) {
        const parentPill = this.activePillForPath;
        const parentId = parentPill.data('id');
        const newId = `${parentId}.${variableData.id}`;
        const parentName = this._findVariableById(parentId).Name;
        parentPill.data('id', newId).data('type', variableData.Type || 'string').text(`${parentName} . ${variableData.Name}`).attr('title', variableData.Description);
        this.activePillForPath = null;
        this._populateDropdown();
        this._triggerValueChange();
    }

    _fillArgumentSlot(variableData) {
        const pillNode = $(`<span class="pill variable-pill" contenteditable="false" data-id="${variableData.id}" data-type="${variableData.Type || 'string'}" data-bs-toggle="tooltip" title="${variableData.Description}">${variableData.Name}</span>`)[0];
        this.activeArgSlot.replaceWith(pillNode);
        new bootstrap.Tooltip(pillNode);
        this.activeArgSlot = null;
        this._populateDropdown();
        this._triggerValueChange();
    }

    _handleCopy(e) {
        const selection = window.getSelection();
        const range = selection.getRangeAt(0);
        const fragment = range.cloneContents(); // Get a copy of the selected DOM nodes

        let copiedText = '';
        fragment.childNodes.forEach(node => {
            copiedText += this._getNodeValue(node); // Reuse our powerful getValue helper!
        });

        // Use the Clipboard API to set the plain text data
        e.originalEvent.clipboardData.setData('text/plain', copiedText);
        e.preventDefault(); // Stop the browser from copying the HTML

        if (e.type === 'cut') {
            range.deleteContents();
        }
    }

    _handlePaste(e) {
        e.preventDefault(); // Stop the browser from pasting directly
        const pastedText = (e.originalEvent || e).clipboardData.getData('text/plain');

        // Now, we create a document fragment from this text
        const fragment = this._createFragmentFromText(pastedText);

        // And insert it at the cursor position
        const sel = window.getSelection();
        if (!sel.rangeCount) return;
        const range = sel.getRangeAt(0);
        range.deleteContents();
        range.insertNode(fragment);
    }

    _triggerValueChange() {
        if (typeof this.options.onValueChange === 'function') {
            this.options.onValueChange();
        }
    }

    // --- Private Utility Methods ---

    _groupVariables(list) {
        return list.reduce((acc, v) => {
            (acc[v.group || 'General'] = acc[v.group || 'General'] || []).push(v);
            return acc;
        }, {});
    }

    _findVariableById(id) {
        const parts = id.split('.');
        let level = this.variables,
            found = null;

        let levelIsArgs = false;
        for (const part of parts) {
            if (levelIsArgs) {
                found = level.find(v => v.name === part);
            }
            else {
                found = level.find(v => v.id === part);
            }

            if (found?.schema) {
                levelIsArgs = false;
                level = found.schema;
            }
            if (found?.args) {
                levelIsArgs = true;
                level = found.args;
            }
            else if (found) continue;
            else return null;
        }
        return found;
    }

    _getNodeValue(node) {
        if (node.nodeType === Node.TEXT_NODE) return node.textContent;
        if (node.nodeType !== Node.ELEMENT_NODE) return '';

        const $node = $(node);

        // First, specifically handle function pills because their serialization is complex
        if ($node.hasClass('function-pill')) {
            const functionName = $node.data('id');
            const args = [];

            // --- THE FIX IS HERE ---
            // Instead of .children(), we use a specific selector to get ONLY the argument values.
            $node.find('.args-container').find('.arg-input, .arg-slot, .variable-pill').each((i, argValueNode) => {
                if ($(argValueNode).hasClass('arg-input')) {
                    if (!$(argValueNode).text()) {
                        args.push('undefined');
                    }
                    else {
                        const argurmentName = $(argValueNode).data('arg-name');
                        var variableArgumentData = this._findVariableById(`${functionName}.${argurmentName}`);
                        if (variableArgumentData) {
                            if (variableArgumentData.type === 'string' || variableArgumentData.type === 'datetime') {
                                var text = $(argValueNode).text()
                                    .replace(/\\/g, '\\\\') // IMPORTANT: escape backslashes FIRST
                                    .replace(/"/g, '\\"');

                                args.push(`"${text}"`);
                            }
                            else if (variableArgumentData.type === 'number') {
                                var parsedInt = parseInt($(argValueNode).text());
                                args.push(parsedInt);
                            }
                            else if (variableArgumentData.type === 'boolean') {
                                var parsedBool = $(argValueNode).text().toLowerCase() === 'true';
                                args.push(parsedBool);
                            }
                            else {
                                args.push($(argValueNode).text());
                            }
                        }
                    }
                } else {
                    // For variable pills or unfilled slots, we recurse to get their {={...}=} syntax.
                    args.push(this._getNodeValue(argValueNode));
                }
            });

            return `{={${functionName}(${args.join(', ')})}=}`;
        }

        // Now, handle ANY other kind of pill (variable-pill, invalid-pill, arg-slot)
        // As long as it has the .pill class and a data-id, we serialize it.
        if ($node.hasClass('pill')) {
            if (!$node.data('id')) return 'undefined';

            return `{={${$node.data('id')}}=}`;
        }

        // Handle line breaks
        if (['DIV', 'BR'].includes(node.tagName)) return '\n';

        // Fallback for any other nodes (like the arg-name-label, which will be ignored by the specific selector above)
        return node.textContent;
    }

    _parseTopLevelVariables(textValue) {
        const tokens = [];
        if (!textValue) return tokens;

        const openDelim = '{={';
        const closeDelim = '}=}';
        let currentIndex = 0;

        while (currentIndex < textValue.length) {
            const openerIndex = textValue.indexOf(openDelim, currentIndex);

            // Case 1: No more openers found. The rest is plain text.
            if (openerIndex === -1) {
                tokens.push({ type: 'text', content: textValue.substring(currentIndex) });
                break;
            }

            // Case 2: There is plain text before the next opener.
            if (openerIndex > currentIndex) {
                tokens.push({ type: 'text', content: textValue.substring(currentIndex, openerIndex) });
            }

            // Case 3: We found an opener. Now we must find its matching closer.
            let nestingLevel = 1;
            let searchIndex = openerIndex + openDelim.length;
            let closerIndex = -1;

            while (searchIndex < textValue.length) {
                const nextOpener = textValue.indexOf(openDelim, searchIndex);
                const nextCloser = textValue.indexOf(closeDelim, searchIndex);

                // If a closer exists and it comes before the next opener (or there is no next opener)
                if (nextCloser !== -1 && (nextOpener === -1 || nextCloser < nextOpener)) {
                    nestingLevel--;
                    if (nestingLevel === 0) {
                        closerIndex = nextCloser;
                        break; // Found our match!
                    }
                    searchIndex = nextCloser + closeDelim.length;
                }
                // If an opener comes first
                else if (nextOpener !== -1) {
                    nestingLevel++;
                    searchIndex = nextOpener + openDelim.length;
                }
                // If no more delimiters are found, break the search
                else {
                    break;
                }
            }

            if (closerIndex !== -1) {
                // We found a complete, top-level block.
                const content = textValue.substring(openerIndex + openDelim.length, closerIndex);
                tokens.push({ type: 'variable', content: content });
                currentIndex = closerIndex + closeDelim.length;
            } else {
                // An opener was found but not its matching closer. Treat it as plain text.
                tokens.push({ type: 'text', content: textValue.substring(openerIndex) });
                break;
            }
        }

        return tokens;
    }

    _createFragmentFromText(textValue) {
        const fragment = document.createDocumentFragment();
        if (!textValue) return fragment;

        const tokens = this._parseTopLevelVariables(textValue);

        tokens.forEach(token => {
            if (token.type === 'text' && token.content) {
                fragment.appendChild(document.createTextNode(token.content));
            } else if (token.type === 'variable') {
                const pillNode = this._createPillNode(token.content);
                fragment.appendChild(pillNode);
                new bootstrap.Tooltip(pillNode); // Initialize tooltip
            }
        });

        return fragment;
    }

    _parseFunctionArgs(argsString) {
        const args = [];
        if (!argsString) return args;

        let currentArg = '';
        let nestingLevel = 0; // For {={ ... }=}
        let inSingleQuotes = false;
        let inDoubleQuotes = false;

        for (let i = 0; i < argsString.length; i++) {
            const char = argsString[i];
            const nextThree = argsString.substring(i, i + 3);

            if (inSingleQuotes) {
                if (char === "'") inSingleQuotes = false;
                currentArg += char;
                continue;
            }
            if (inDoubleQuotes) {
                if (char === '"') inDoubleQuotes = false;
                currentArg += char;
                continue;
            }

            if (nextThree === '{={') {
                nestingLevel++;
                currentArg += nextThree;
                i += 2; // Skip the next two chars
            } else if (nextThree === '}=}') {
                nestingLevel--;
                currentArg += nextThree;
                i += 2; // Skip the next two chars
            } else if (char === ',' && nestingLevel === 0) {
                args.push(currentArg.trim());
                currentArg = '';
            } else {
                if (char === "'") inSingleQuotes = true;
                if (char === '"') inDoubleQuotes = true;
                currentArg += char;
            }
        }

        if (currentArg) {
            args.push(currentArg.trim());
        }

        // Filter out any empty strings that might result from trailing commas
        return args.filter(arg => arg);
    }

    _createPillNode(serializedContent) {
        const functionRegex = /^([a-zA-Z0-9_.-]+)\s*\((.*)\)\s*$/;
        const funcMatch = serializedContent.match(functionRegex);
        let pill;

        const variableData = this._findVariableById(funcMatch ? funcMatch[1] : serializedContent);

        // --- CASE 1: The content is a FULLY SERIALIZED FUNCTION string ---
        if (funcMatch && variableData && variableData.Type === 'function') {
            const functionName = funcMatch[1];
            const argsString = funcMatch[2];

            let functionTitle = `${variableData.Description}<br><br>Arguments:`;
            pill = $(`<span class="pill function-pill" contenteditable="false" data-id="${functionName}" data-type="function" data-bs-toggle="tooltip" data-bs-html="true"><span class="function-name">${variableData.Name}</span>(<span class="args-container"></span>)</span>`);
            const $argsContainer = pill.find('.args-container');

            const parsedArgs = this._parseFunctionArgs(argsString);

            variableData.args.forEach((argDef, index) => {
                var argContent = parsedArgs[index] || 'undefined';

                functionTitle += `<br>- ${argDef.name}: ${argDef.isLiteral ? `Value of type ${argDef.type}` : 'Requires type(s) ' + argDef.allowedTypes.join(', ')}`;

                const nameLabel = `<span class="arg-name-label">${argDef.name}: </span>`;
                $argsContainer.append(nameLabel);

                if (argDef.isLiteral) {
                    if (argContent === 'undefined') {
                        let inputNode = $('<span>', { class: 'arg-input', contenteditable: 'true', 'data-arg-name': argDef.name });
                        $argsContainer.append(inputNode);
                    }
                    else {
                        let literalValue = argContent;
                        if (literalValue.startsWith('"') && literalValue.endsWith('"')) {
                            literalValue = literalValue
                                .slice(1, -1)
                                .replace(/\\"/g, '"')
                                .replace(/\\\\/g, '\\');
                        }
                        const inputNode = $('<span>', { class: 'arg-input', contenteditable: 'true', 'data-arg-name': argDef.name }).text(literalValue);
                        $argsContainer.append(inputNode);
                    }
                }
                else {
                    if (argContent.startsWith('{={') && argContent.endsWith('}=}')) {
                        const innerContent = argContent.slice(3, -3);
                        const argPill = this._createPillNode(innerContent);
                        new bootstrap.Tooltip(argPill);
                        $argsContainer.append(argPill);
                    }
                    else {
                        let inputNode = $('<span>', { class: 'arg-slot pill', contenteditable: 'false', 'data-arg-name': argDef.name, 'data-allowed-types': JSON.stringify(argDef.allowedTypes) }).text('...');
                        $argsContainer.append(inputNode);
                    }
                }

                if (index < parsedArgs.length - 1) {
                    $argsContainer.append(',&nbsp;');
                }
            });

            pill.attr('title', functionTitle);
        }
        // --- CASE 2: The content is a FUNCTION ID ONLY (from _insertPill) ---
        else if (!funcMatch && variableData && variableData.Type === 'function') {
            let functionTitle = `${variableData.Description}<br><br>Arguments:`;
            const argsHtml = variableData.args.map(arg => {
                functionTitle += `<br>- ${arg.name}: ${arg.isLiteral ? `Value of type ${arg.type}` : 'Requires type(s) ' + arg.allowedTypes.join(', ')}`;
                const nameLabel = `<span class="arg-name-label">${arg.name}: </span>`;

                if (arg.isLiteral) {
                    // Use default value from the variable definition
                    const defaultValue = arg.defaultValue || '';
                    return nameLabel + $('<span>', { class: 'arg-input', contenteditable: 'true', 'data-arg-name': arg.name }).text(defaultValue)[0].outerHTML;
                } else {
                    // Create an empty slot
                    return nameLabel + `<span class="arg-slot pill" contenteditable="false" data-arg-name="${arg.name}" data-allowed-types='${JSON.stringify(arg.allowedTypes)}'>...</span>`;
                }
            }).join(',&nbsp;');

            pill = $(`<span class="pill function-pill" contenteditable="false" data-id="${variableData.id}" data-type="function" data-bs-toggle="tooltip" data-bs-html="true" title="${functionTitle}"><span class="function-name">${variableData.Name}</span>(<span class="args-container">${argsHtml}</span>)</span>`);
        }
        // --- CASE 3: The content is a SIMPLE VARIABLE or an INVALID pill ---
        else {
            if (variableData) {
                // Build a standard variable/object pill
                const pathParts = serializedContent.split('.');
                const names = [];
                let currentLevelItems = this.variables;
                for (const part of pathParts) {
                    const item = currentLevelItems.find(v => v.id === part);
                    if (item) {
                        names.push(item.Name);
                        currentLevelItems = item.schema || [];
                    } else { names.push(part); }
                }
                const displayText = names.join(' | ');
                pill = $(`<span class="pill variable-pill" contenteditable="false" data-id="${serializedContent}" data-type="${variableData.Type || 'string'}" data-bs-toggle="tooltip" title="${variableData.Description}">${displayText}</span>`);
            } else {
                // Variable not found, create an invalid pill
                pill = $(`<span class="pill invalid-pill" contenteditable="false" data-id="${serializedContent}" data-bs-toggle="tooltip" title="Variable ID '${serializedContent}' not found.">${serializedContent} (invalid)</span>`);
            }
        }

        return pill[0];
    }

    // --- Public API Methods ---

    /**
     * Gets the serialized value of the input as a plain text string.
     * @returns {string} The value with variable and function syntax.
     */
    getValue() {
        let result = '';
        this.$editor[0].childNodes.forEach(node => {
            result += this._getNodeValue(node);
        });
        result = result.replace(/\u00A0/g, ' ').replace(/\n /g, '\n').trim();
        return result;
    }

    /**
     * Sets the value of the input from a plain text string.
     * NOTE: Currently does not support parsing of functions, only variables and object paths.
     * @param {string} [textValue=''] - The string to parse and render.
     */
    setValue(textValue = '') {
        this.$editor.empty();
        if (!textValue) {
            this._updatePlaceholder();
            return;
        }

        const tokens = this._parseTopLevelVariables(textValue);

        tokens.forEach(token => {
            if (token.type === 'text' && token.content) {
                this.$editor.append(document.createTextNode(token.content));
            } else if (token.type === 'variable') {
                const pillNode = this._createPillNode(token.content);
                this.$editor.append(pillNode);
                new bootstrap.Tooltip(pillNode); // Initialize tooltip
            }
        });

        this._updatePlaceholder();
        this._triggerValueChange();
    }

    /**
     * Validates the current state of the input.
     * Checks for invalid variables/functions and missing function arguments.
     * @returns {{isValidated: boolean, errors: string[]}} An object indicating validity and a list of error strings.
     */
    validate() {
        const errors = [];

        // 1. Check for invalid pills (variables or functions that don't exist in the schema)
        this.$editor.find('.invalid-pill').each((index, element) => {
            const $pill = $(element);
            const invalidId = $pill.data('id');
            errors.push(`The variable or function "${invalidId}" is not valid or does not exist.`);
        });

        // 2. Check for functions with missing arguments
        this.$editor.find('.function-pill').each((index, element) => {
            const $functionPill = $(element);
            const functionData = this._findVariableById($functionPill.data('id'));
            const functionName = functionData ? functionData.Name : $functionPill.data('id');

            // 2a. Check for unfilled variable slots
            $functionPill.find('.arg-slot').each((i, slot) => {
                const $slot = $(slot);
                const argName = $slot.data('arg-name');
                errors.push(`Missing variable argument for "${argName}" in the function "${functionName}".`);
            });

            // 2b. Check for empty literal inputs
            $functionPill.find('.arg-input').each((i, input) => {
                const $input = $(input);
                if ($input.text().trim() === '') {
                    const argName = $input.data('arg-name');
                    errors.push(`Missing literal value for argument "${argName}" in the function "${functionName}".`);
                }
            });
        });

        const isValidated = errors.length === 0;

        return { isValidated, errors };
    }

    /**
     * Removes the component and cleans up all event listeners and tooltips.
     */
    destroy() {
        this.$element.find('[data-bs-toggle="tooltip"]').each(function () {
            bootstrap.Tooltip.getInstance(this)?.dispose();
        });
        document.removeEventListener('selectionchange', this.selectionChangeHandler);
        this.$wrapper.off();
        this.$element.empty();
    }
}