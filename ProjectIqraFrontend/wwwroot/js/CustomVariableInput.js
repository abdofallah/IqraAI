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
                <div class="editor-area form-control" contenteditable="true" spellcheck="false" 
                     aria-multiline="${this.options.isMultiLine}"></div>
                <div class="editor-placeholder text-muted">${this.options.placeholder}</div>
                <button class="btn btn-sm btn-outline-secondary variable-trigger-btn" type="button" 
                        data-bs-toggle="dropdown" data-bs-auto-close="outside" aria-expanded="false">
                    <i class="bi bi-braces"></i>
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
		this.$editor.on('blur', () => {
			this.$wrapper.removeClass('is-focused');
			const sel = window.getSelection();
			// Save the range only if there is a selection
			if (sel.rangeCount > 0) {
				this.lastSelectionRange = sel.getRangeAt(0);
			}
		});
		this.$editor.on('input focus blur keyup', () => this._updatePlaceholder());
		this.$editor.on('input', (e) => this._handleTriggerSequence(e));
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
		this.$editor.on('copy', (e) => this._handleCopy(e));
		this.$editor.on('paste', (e) => this._handlePaste(e));
		this.$editor.on('click', '.arg-input', (e) => {
			e.stopPropagation(); // Prevent this click from bubbling up to the wrapper
		});

		// --- Dropdown Events ---
		this.$searchInput.on('keyup', (e) => this._filterDropdown($(e.target).val()));
		this.$dropdownList.on('click', 'a', (e) => {
			e.preventDefault();
			const variableData = $(e.currentTarget).data('variable-data');

			if (this.activeArgSlot) this._fillArgumentSlot(variableData);
			else if (this.activePillForPath) this._updatePillForObjectPath(variableData);
			else this._insertPill(variableData);

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
			this._populateDropdown(variableData.schema, true);
		});
		this.$dropdownList.on('click', '.dropdown-item-back', (e) => {
			e.preventDefault();
			const previousList = this.dropdownState.history.pop();
			this._populateDropdown(previousList, false);
		});
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

		let finalNode;
		if (variableData.Type === 'function') {
			const argsHtml = variableData.args.map(arg => {
				if (arg.isLiteral) {
					return $('<span>', {
						class: 'arg-input',
						contenteditable: 'true',
						'data-arg-name': arg.name
					}).text(arg.defaultValue || '...')[0].outerHTML;
				} else {
					return `<span class="arg-slot pill" contenteditable="false" data-arg-name="${arg.name}" data-allowed-types='${JSON.stringify(arg.allowedTypes)}'>${arg.name}</span>`;
				}
			}).join(',&nbsp;');
			finalNode = $(`<span class="pill function-pill" contenteditable="false" data-id="${variableData.id}" data-type="function"><span class="function-name">${variableData.Name}</span>(<span class="args-container">${argsHtml}</span>)</span>`)[0];
		} else {
			finalNode = $(`<span class="pill variable-pill" contenteditable="false" data-id="${variableData.id}" data-type="${variableData.Type || 'string'}" data-bs-toggle="tooltip" title="${variableData.Description}">${variableData.Name}</span>`)[0];
		}

		let range = sel.getRangeAt(0);
		range.deleteContents();
		range.insertNode(finalNode);
		if (variableData.Type !== 'function') new bootstrap.Tooltip(finalNode);

		const spaceNode = document.createTextNode('\u00A0');
		range.setStartAfter(finalNode);
		range.collapse(true);
		range.insertNode(spaceNode);
		range.setStartAfter(spaceNode);
		sel.removeAllRanges();
		sel.addRange(range);
		this._updatePlaceholder();
	}

	// --- Private Event Handlers ---

	_handleDropdownNavigation(e) {
		if (!['ArrowUp', 'ArrowDown', 'Enter', 'Escape'].includes(e.key)) return;
		e.preventDefault();
		e.stopPropagation();
		if ($(e.target).is(this.$searchInput) && ['ArrowUp', 'ArrowDown'].includes(e.key)) return;
		const visibleItems = this.$dropdownList.find('li:visible a');
		if (!visibleItems.length) return;
		let activeItem = visibleItems.filter('.active');
		let activeIndex = visibleItems.index(activeItem);
		switch (e.key) {
			case 'ArrowDown':
				activeIndex = (activeIndex + 1) % visibleItems.length;
				break;
			case 'ArrowUp':
				activeIndex = activeIndex === -1 ? visibleItems.length - 1 : (activeIndex - 1 + visibleItems.length) % visibleItems.length;
				break;
			case 'Enter':
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
		const range = sel.getRangeAt(0);
		if (e.key === 'Backspace' && range.startOffset === 0) {
			let nodeToDelete = range.startContainer.previousSibling;
			if (nodeToDelete && nodeToDelete.nodeType === Node.TEXT_NODE && /^\s+$/.test(nodeToDelete.textContent)) nodeToDelete = nodeToDelete.previousSibling;
			if (nodeToDelete && $(nodeToDelete).hasClass('pill')) {
				e.preventDefault();
				if (range.startContainer.previousSibling.nodeType === Node.TEXT_NODE) $(range.startContainer.previousSibling).remove();
				$(nodeToDelete).remove();
			}
		} else if (e.key === 'Delete' && range.startOffset === range.startContainer.textContent.length) {
			let nodeToDelete = range.startContainer.nextSibling;
			if (nodeToDelete && nodeToDelete.nodeType === Node.TEXT_NODE && /^\s+$/.test(nodeToDelete.textContent)) nodeToDelete = nodeToDelete.nextSibling;
			if (nodeToDelete && $(nodeToDelete).hasClass('pill')) {
				e.preventDefault();
				if (range.startContainer.nextSibling.nodeType === Node.TEXT_NODE) $(range.startContainer.nextSibling).remove();
				$(nodeToDelete).remove();
			}
		}
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

	_updatePillForObjectPath(variableData) {
		const parentPill = this.activePillForPath;
		const parentId = parentPill.data('id');
		const newId = `${parentId}.${variableData.id}`;
		const parentName = this._findVariableById(parentId).Name;
		parentPill.data('id', newId).data('type', variableData.Type || 'string').text(`${parentName} . ${variableData.Name}`).attr('title', variableData.Description);
		this.activePillForPath = null;
		this._populateDropdown();
	}

	_fillArgumentSlot(variableData) {
		const pillNode = $(`<span class="pill variable-pill" contenteditable="false" data-id="${variableData.id}" data-type="${variableData.Type || 'string'}" data-bs-toggle="tooltip" title="${variableData.Description}">${variableData.Name}</span>`)[0];
		this.activeArgSlot.replaceWith(pillNode);
		new bootstrap.Tooltip(pillNode);
		this.activeArgSlot = null;
		this._populateDropdown();
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
		for (const part of parts) {
			found = level.find(v => v.id === part);
			if (found?.schema) level = found.schema;
			else if (found) continue;
			else return null;
		}
		return found;
	}

	_getNodeValue(node) {
		if (node.nodeType === Node.TEXT_NODE) return node.textContent;
		if (node.nodeType !== Node.ELEMENT_NODE) return '';
		const $node = $(node);
		if ($node.hasClass('variable-pill')) return `{={${$node.data('id')}}=}`;
		if ($node.hasClass('function-pill')) {
			const args = [];
			$node.find('.args-container').children().each((i, child) => {
				args.push($(child).hasClass('arg-input') ? $(child).text() : this._getNodeValue(child));
			});
			return `{={${$node.data('id')}(${args.join(', ')})=}}`;
		}
		if (['DIV', 'BR'].includes(node.tagName)) return '\n';
		return node.textContent;
	}

	_createFragmentFromText(textValue) {
		const fragment = document.createDocumentFragment();
		if (!textValue) return fragment;

		const regex = /({=([a-zA-Z0-9_.-]+)=})/g;
		let lastIndex = 0, match;
		while ((match = regex.exec(textValue)) !== null) {
			// ... (This logic will be almost identical to the loop in setValue)
			// Instead of this.$editor.append(), we do fragment.appendChild()
			// ...
			// For example:
			fragment.appendChild(document.createTextNode(textValue.substring(lastIndex, match.index)));
			const pillNode = this._createPillNodeFromId(match[2]); // We'll need another helper
			fragment.appendChild(pillNode);
			fragment.appendChild(document.createTextNode('\u00A0')); // &nbsp;
			lastIndex = regex.lastIndex;
		}
		if (lastIndex < textValue.length) {
			fragment.appendChild(document.createTextNode(textValue.substring(lastIndex)));
		}
		return fragment;
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
		if (!textValue) return this._updatePlaceholder();
		const regex = /({=([a-zA-Z0-9_.-]+)=})/g;
		let lastIndex = 0,
			match;
		while ((match = regex.exec(textValue)) !== null) {
			this.$editor.append(document.createTextNode(textValue.substring(lastIndex, match.index)));
			const variableId = match[2];
			const finalVarData = this._findVariableById(variableId);
			let pill;
			if (finalVarData) {
				const pathParts = variableId.split('.');
				const names = [];
				let level = this.variables;
				for (const part of pathParts) {
					const item = level.find(v => v.id === part);
					if (item) {
						names.push(item.Name);
						level = item.schema || [];
					}
				}
				pill = $(`<span class="pill variable-pill" contenteditable="false" data-id="${variableId}" data-type="${finalVarData.Type || 'string'}" data-bs-toggle="tooltip" title="${finalVarData.Description}">${names.join(' . ')}</span>`);
			} else {
				pill = $(`<span class="pill invalid-pill" contenteditable="false" data-id="${variableId}" data-bs-toggle="tooltip" title="Variable ID '${variableId}' not found.">${variableId} (invalid)</span>`);
			}
			this.$editor.append(pill).append(document.createTextNode('\u00A0'));
			new bootstrap.Tooltip(pill[0]);
			lastIndex = regex.lastIndex;
		}
		if (lastIndex < textValue.length) this.$editor.append(document.createTextNode(textValue.substring(lastIndex)));
		this._updatePlaceholder();
	}

	/**
	 * Removes the component and cleans up all event listeners and tooltips.
	 */
	destroy() {
		this.$element.find('[data-bs-toggle="tooltip"]').each(function () {
			bootstrap.Tooltip.getInstance(this)?.dispose();
		});
		this.$wrapper.off();
		this.$element.empty();
	}
}