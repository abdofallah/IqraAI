/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __param = (this && this.__param) || function (paramIndex, decorator) {
    return function (target, key) { decorator(target, key, paramIndex); }
};
import * as dom from '../../../../base/browser/dom.js';
import * as aria from '../../../../base/browser/ui/aria/aria.js';
import { renderIcon } from '../../../../base/browser/ui/iconLabel/iconLabels.js';
import { List } from '../../../../base/browser/ui/list/listWidget.js';
import * as arrays from '../../../../base/common/arrays.js';
import { DeferredPromise, raceCancellation } from '../../../../base/common/async.js';
import { CancellationTokenSource } from '../../../../base/common/cancellation.js';
import { Codicon } from '../../../../base/common/codicons.js';
import { Emitter } from '../../../../base/common/event.js';
import { DisposableStore, toDisposable } from '../../../../base/common/lifecycle.js';
import { StopWatch } from '../../../../base/common/stopwatch.js';
import { assertType, isDefined } from '../../../../base/common/types.js';
import './renameWidget.css';
import { applyFontInfo } from '../../../browser/config/domFontInfo.js';
import { Position } from '../../../common/core/position.js';
import { Range } from '../../../common/core/range.js';
import { NewSymbolNameTag } from '../../../common/languages.js';
import { localize } from '../../../../nls.js';
import { IContextKeyService, RawContextKey } from '../../../../platform/contextkey/common/contextkey.js';
import { IKeybindingService } from '../../../../platform/keybinding/common/keybinding.js';
import { ILogService } from '../../../../platform/log/common/log.js';
import { getListStyles } from '../../../../platform/theme/browser/defaultStyles.js';
import { editorWidgetBackground, inputBackground, inputBorder, inputForeground, quickInputListFocusBackground, quickInputListFocusForeground, widgetBorder, widgetShadow } from '../../../../platform/theme/common/colorRegistry.js';
import { IThemeService } from '../../../../platform/theme/common/themeService.js';
/** for debugging */
const _sticky = false;
export const CONTEXT_RENAME_INPUT_VISIBLE = new RawContextKey('renameInputVisible', false, localize('renameInputVisible', "Whether the rename input widget is visible"));
export const CONTEXT_RENAME_INPUT_FOCUSED = new RawContextKey('renameInputFocused', false, localize('renameInputFocused', "Whether the rename input widget is focused"));
let RenameWidget = class RenameWidget {
    constructor(_editor, _acceptKeybindings, _themeService, _keybindingService, contextKeyService, _logService) {
        this._editor = _editor;
        this._acceptKeybindings = _acceptKeybindings;
        this._themeService = _themeService;
        this._keybindingService = _keybindingService;
        this._logService = _logService;
        // implement IContentWidget
        this.allowEditorOverflow = true;
        this._disposables = new DisposableStore();
        this._visibleContextKey = CONTEXT_RENAME_INPUT_VISIBLE.bindTo(contextKeyService);
        this._isEditingRenameCandidate = false;
        this._beforeFirstInputFieldEditSW = new StopWatch();
        this._input = new RenameInput();
        this._disposables.add(this._input);
        this._editor.addContentWidget(this);
        this._disposables.add(this._editor.onDidChangeConfiguration(e => {
            if (e.hasChanged(50 /* EditorOption.fontInfo */)) {
                this._updateFont();
            }
        }));
        this._disposables.add(_themeService.onDidColorThemeChange(this._updateStyles, this));
    }
    dispose() {
        this._disposables.dispose();
        this._editor.removeContentWidget(this);
    }
    getId() {
        return '__renameInputWidget';
    }
    getDomNode() {
        if (!this._domNode) {
            this._domNode = document.createElement('div');
            this._domNode.className = 'monaco-editor rename-box';
            this._domNode.appendChild(this._input.domNode);
            this._renameCandidateListView = this._disposables.add(new RenameCandidateListView(this._domNode, {
                fontInfo: this._editor.getOption(50 /* EditorOption.fontInfo */),
                onFocusChange: (newSymbolName) => {
                    this._input.domNode.value = newSymbolName;
                    this._isEditingRenameCandidate = false; // @ulugbekna: reset
                },
                onSelectionChange: () => {
                    this._isEditingRenameCandidate = false; // @ulugbekna: because user picked a rename suggestion
                    this.acceptInput(false); // we don't allow preview with mouse click for now
                }
            }));
            this._disposables.add(this._input.onDidChange(() => {
                var _a, _b, _c, _d;
                if (((_a = this._renameCandidateListView) === null || _a === void 0 ? void 0 : _a.focusedCandidate) !== undefined) {
                    this._isEditingRenameCandidate = true;
                }
                (_b = this._timeBeforeFirstInputFieldEdit) !== null && _b !== void 0 ? _b : (this._timeBeforeFirstInputFieldEdit = this._beforeFirstInputFieldEditSW.elapsed());
                if (((_c = this._renameCandidateProvidersCts) === null || _c === void 0 ? void 0 : _c.token.isCancellationRequested) === false) {
                    this._renameCandidateProvidersCts.cancel();
                }
                (_d = this._renameCandidateListView) === null || _d === void 0 ? void 0 : _d.clearFocus();
            }));
            this._label = document.createElement('div');
            this._label.className = 'rename-label';
            this._domNode.appendChild(this._label);
            this._updateFont();
            this._updateStyles(this._themeService.getColorTheme());
        }
        return this._domNode;
    }
    _updateStyles(theme) {
        var _a, _b, _c, _d;
        if (!this._domNode) {
            return;
        }
        const widgetShadowColor = theme.getColor(widgetShadow);
        const widgetBorderColor = theme.getColor(widgetBorder);
        this._domNode.style.backgroundColor = String((_a = theme.getColor(editorWidgetBackground)) !== null && _a !== void 0 ? _a : '');
        this._domNode.style.boxShadow = widgetShadowColor ? ` 0 0 8px 2px ${widgetShadowColor}` : '';
        this._domNode.style.border = widgetBorderColor ? `1px solid ${widgetBorderColor}` : '';
        this._domNode.style.color = String((_b = theme.getColor(inputForeground)) !== null && _b !== void 0 ? _b : '');
        this._input.domNode.style.backgroundColor = String((_c = theme.getColor(inputBackground)) !== null && _c !== void 0 ? _c : '');
        // this._input.style.color = String(theme.getColor(inputForeground) ?? '');
        const border = theme.getColor(inputBorder);
        this._input.domNode.style.borderWidth = border ? '1px' : '0px';
        this._input.domNode.style.borderStyle = border ? 'solid' : 'none';
        this._input.domNode.style.borderColor = (_d = border === null || border === void 0 ? void 0 : border.toString()) !== null && _d !== void 0 ? _d : 'none';
    }
    _updateFont() {
        if (this._domNode === undefined) {
            return;
        }
        assertType(this._label !== undefined, 'RenameWidget#_updateFont: _label must not be undefined given _domNode is defined');
        this._editor.applyFontInfo(this._input.domNode);
        const fontInfo = this._editor.getOption(50 /* EditorOption.fontInfo */);
        this._label.style.fontSize = `${this._computeLabelFontSize(fontInfo.fontSize)}px`;
    }
    _computeLabelFontSize(editorFontSize) {
        return editorFontSize * 0.8;
    }
    getPosition() {
        if (!this._visible) {
            return null;
        }
        if (!this._editor.hasModel() || // @ulugbekna: shouldn't happen
            !this._editor.getDomNode() // @ulugbekna: can happen during tests based on suggestWidget's similar predicate check
        ) {
            return null;
        }
        const bodyBox = dom.getClientArea(this.getDomNode().ownerDocument.body);
        const editorBox = dom.getDomNodePagePosition(this._editor.getDomNode());
        const cursorBoxTop = this._getTopForPosition();
        this._nPxAvailableAbove = cursorBoxTop + editorBox.top;
        this._nPxAvailableBelow = bodyBox.height - this._nPxAvailableAbove;
        const lineHeight = this._editor.getOption(67 /* EditorOption.lineHeight */);
        const { totalHeight: candidateViewHeight } = RenameCandidateView.getLayoutInfo({ lineHeight });
        const positionPreference = this._nPxAvailableBelow > candidateViewHeight * 6 /* approximate # of candidates to fit in (inclusive of rename input box & rename label) */
            ? [2 /* ContentWidgetPositionPreference.BELOW */, 1 /* ContentWidgetPositionPreference.ABOVE */]
            : [1 /* ContentWidgetPositionPreference.ABOVE */, 2 /* ContentWidgetPositionPreference.BELOW */];
        return {
            position: this._position,
            preference: positionPreference,
        };
    }
    beforeRender() {
        var _a, _b;
        const [accept, preview] = this._acceptKeybindings;
        this._label.innerText = localize({ key: 'label', comment: ['placeholders are keybindings, e.g "F2 to Rename, Shift+F2 to Preview"'] }, "{0} to Rename, {1} to Preview", (_a = this._keybindingService.lookupKeybinding(accept)) === null || _a === void 0 ? void 0 : _a.getLabel(), (_b = this._keybindingService.lookupKeybinding(preview)) === null || _b === void 0 ? void 0 : _b.getLabel());
        this._domNode.style.minWidth = `200px`; // to prevent from widening when candidates come in
        return null;
    }
    afterRender(position) {
        this._trace('invoking afterRender, position: ', position ? 'not null' : 'null');
        if (position === null) {
            // cancel rename when input widget isn't rendered anymore
            this.cancelInput(true, 'afterRender (because position is null)');
            return;
        }
        if (!this._editor.hasModel() || // shouldn't happen
            !this._editor.getDomNode() // can happen during tests based on suggestWidget's similar predicate check
        ) {
            return;
        }
        assertType(this._renameCandidateListView);
        assertType(this._nPxAvailableAbove !== undefined);
        assertType(this._nPxAvailableBelow !== undefined);
        const inputBoxHeight = dom.getTotalHeight(this._input.domNode);
        const labelHeight = dom.getTotalHeight(this._label);
        let totalHeightAvailable;
        if (position === 2 /* ContentWidgetPositionPreference.BELOW */) {
            totalHeightAvailable = this._nPxAvailableBelow;
        }
        else {
            totalHeightAvailable = this._nPxAvailableAbove;
        }
        this._renameCandidateListView.layout({
            height: totalHeightAvailable - labelHeight - inputBoxHeight,
            width: dom.getTotalWidth(this._input.domNode),
        });
    }
    acceptInput(wantsPreview) {
        var _a;
        this._trace(`invoking acceptInput`);
        (_a = this._currentAcceptInput) === null || _a === void 0 ? void 0 : _a.call(this, wantsPreview);
    }
    cancelInput(focusEditor, caller) {
        var _a;
        this._trace(`invoking cancelInput, caller: ${caller}, _currentCancelInput: ${this._currentAcceptInput ? 'not undefined' : 'undefined'}`);
        (_a = this._currentCancelInput) === null || _a === void 0 ? void 0 : _a.call(this, focusEditor);
    }
    focusNextRenameSuggestion() {
        var _a;
        if (!((_a = this._renameCandidateListView) === null || _a === void 0 ? void 0 : _a.focusNext())) {
            this._input.domNode.value = this._currentName;
        }
    }
    focusPreviousRenameSuggestion() {
        var _a;
        if (!((_a = this._renameCandidateListView) === null || _a === void 0 ? void 0 : _a.focusPrevious())) {
            this._input.domNode.value = this._currentName;
        }
    }
    getInput(where, currentName, supportPreview, requestRenameCandidates, cts) {
        const { start: selectionStart, end: selectionEnd } = this._getSelection(where, currentName);
        this._renameCandidateProvidersCts = new CancellationTokenSource();
        const candidates = requestRenameCandidates(this._renameCandidateProvidersCts.token);
        this._updateRenameCandidates(candidates, currentName, cts.token);
        this._isEditingRenameCandidate = false;
        this._domNode.classList.toggle('preview', supportPreview);
        this._position = new Position(where.startLineNumber, where.startColumn);
        this._currentName = currentName;
        this._input.domNode.value = currentName;
        this._input.domNode.setAttribute('selectionStart', selectionStart.toString());
        this._input.domNode.setAttribute('selectionEnd', selectionEnd.toString());
        this._input.domNode.size = Math.max((where.endColumn - where.startColumn) * 1.1, 20); // determines width
        this._beforeFirstInputFieldEditSW.reset();
        const disposeOnDone = new DisposableStore();
        disposeOnDone.add(toDisposable(() => cts.dispose(true))); // @ulugbekna: this may result in `this.cancelInput` being called twice, but it should be safe since we set it to undefined after 1st call
        disposeOnDone.add(toDisposable(() => {
            if (this._renameCandidateProvidersCts !== undefined) {
                this._renameCandidateProvidersCts.dispose(true);
                this._renameCandidateProvidersCts = undefined;
            }
        }));
        const inputResult = new DeferredPromise();
        inputResult.p.finally(() => {
            disposeOnDone.dispose();
            this._hide();
        });
        this._currentCancelInput = (focusEditor) => {
            var _a;
            this._trace('invoking _currentCancelInput');
            this._currentAcceptInput = undefined;
            this._currentCancelInput = undefined;
            (_a = this._renameCandidateListView) === null || _a === void 0 ? void 0 : _a.clearCandidates();
            inputResult.complete(focusEditor);
            return true;
        };
        this._currentAcceptInput = (wantsPreview) => {
            this._trace('invoking _currentAcceptInput');
            assertType(this._renameCandidateListView !== undefined);
            const nRenameSuggestions = this._renameCandidateListView.nCandidates;
            let newName;
            let source;
            const focusedCandidate = this._renameCandidateListView.focusedCandidate;
            if (focusedCandidate !== undefined) {
                this._trace('using new name from renameSuggestion');
                newName = focusedCandidate;
                source = { k: 'renameSuggestion' };
            }
            else {
                this._trace('using new name from inputField');
                newName = this._input.domNode.value;
                source = this._isEditingRenameCandidate ? { k: 'userEditedRenameSuggestion' } : { k: 'inputField' };
            }
            if (newName === currentName || newName.trim().length === 0 /* is just whitespace */) {
                this.cancelInput(true, '_currentAcceptInput (because newName === value || newName.trim().length === 0)');
                return;
            }
            this._currentAcceptInput = undefined;
            this._currentCancelInput = undefined;
            this._renameCandidateListView.clearCandidates();
            inputResult.complete({
                newName,
                wantsPreview: supportPreview && wantsPreview,
                stats: {
                    source,
                    nRenameSuggestions,
                    timeBeforeFirstInputFieldEdit: this._timeBeforeFirstInputFieldEdit,
                }
            });
        };
        disposeOnDone.add(cts.token.onCancellationRequested(() => this.cancelInput(true, 'cts.token.onCancellationRequested')));
        if (!_sticky) {
            disposeOnDone.add(this._editor.onDidBlurEditorWidget(() => { var _a; return this.cancelInput(!((_a = this._domNode) === null || _a === void 0 ? void 0 : _a.ownerDocument.hasFocus()), 'editor.onDidBlurEditorWidget'); }));
        }
        this._show();
        return inputResult.p;
    }
    /**
     * This allows selecting only part of the symbol name in the input field based on the selection in the editor
     */
    _getSelection(where, currentName) {
        assertType(this._editor.hasModel());
        const selection = this._editor.getSelection();
        let start = 0;
        let end = currentName.length;
        if (!Range.isEmpty(selection) && !Range.spansMultipleLines(selection) && Range.containsRange(where, selection)) {
            start = Math.max(0, selection.startColumn - where.startColumn);
            end = Math.min(where.endColumn, selection.endColumn) - where.startColumn;
        }
        return { start, end };
    }
    _show() {
        this._trace('invoking _show');
        this._editor.revealLineInCenterIfOutsideViewport(this._position.lineNumber, 0 /* ScrollType.Smooth */);
        this._visible = true;
        this._visibleContextKey.set(true);
        this._editor.layoutContentWidget(this);
        // TODO@ulugbekna: could this be simply run in `afterRender`?
        setTimeout(() => {
            this._input.domNode.focus();
            this._input.domNode.setSelectionRange(parseInt(this._input.domNode.getAttribute('selectionStart')), parseInt(this._input.domNode.getAttribute('selectionEnd')));
        }, 100);
    }
    async _updateRenameCandidates(candidates, currentName, token) {
        const trace = (...args) => this._trace('_updateRenameCandidates', ...args);
        trace('start');
        const namesListResults = await raceCancellation(Promise.allSettled(candidates), token);
        if (namesListResults === undefined) {
            trace('returning early - received updateRenameCandidates results - undefined');
            return;
        }
        const newNames = namesListResults.flatMap(namesListResult => namesListResult.status === 'fulfilled' && isDefined(namesListResult.value)
            ? namesListResult.value
            : []);
        trace(`received updateRenameCandidates results - total (unfiltered) ${newNames.length} candidates.`);
        // deduplicate and filter out the current value
        const distinctNames = arrays.distinct(newNames, v => v.newSymbolName);
        trace(`distinct candidates - ${distinctNames.length} candidates.`);
        const validDistinctNames = distinctNames.filter(({ newSymbolName }) => newSymbolName.trim().length > 0 && newSymbolName !== this._input.domNode.value && newSymbolName !== currentName);
        trace(`valid distinct candidates - ${newNames.length} candidates.`);
        if (validDistinctNames.length < 1) {
            trace('returning early - no valid distinct candidates');
            return;
        }
        // show the candidates
        trace('setting candidates');
        this._renameCandidateListView.setCandidates(validDistinctNames);
        // ask editor to re-layout given that the widget is now of a different size after rendering rename candidates
        trace('asking editor to re-layout');
        this._editor.layoutContentWidget(this);
    }
    _hide() {
        this._trace('invoked _hide');
        this._visible = false;
        this._visibleContextKey.reset();
        this._editor.layoutContentWidget(this);
    }
    _getTopForPosition() {
        const visibleRanges = this._editor.getVisibleRanges();
        let firstLineInViewport;
        if (visibleRanges.length > 0) {
            firstLineInViewport = visibleRanges[0].startLineNumber;
        }
        else {
            this._logService.warn('RenameWidget#_getTopForPosition: this should not happen - visibleRanges is empty');
            firstLineInViewport = Math.max(1, this._position.lineNumber - 5); // @ulugbekna: fallback to current line minus 5
        }
        return this._editor.getTopForLineNumber(this._position.lineNumber) - this._editor.getTopForLineNumber(firstLineInViewport);
    }
    _trace(...args) {
        this._logService.trace('RenameWidget', ...args);
    }
};
RenameWidget = __decorate([
    __param(2, IThemeService),
    __param(3, IKeybindingService),
    __param(4, IContextKeyService),
    __param(5, ILogService)
], RenameWidget);
export { RenameWidget };
class RenameCandidateListView {
    // FIXME@ulugbekna: rewrite using event emitters
    constructor(parent, opts) {
        this._disposables = new DisposableStore();
        this._availableHeight = 0;
        this._minimumWidth = 0;
        this._lineHeight = opts.fontInfo.lineHeight;
        this._typicalHalfwidthCharacterWidth = opts.fontInfo.typicalHalfwidthCharacterWidth;
        this._listContainer = document.createElement('div');
        parent.appendChild(this._listContainer);
        this._listWidget = RenameCandidateListView._createListWidget(this._listContainer, this._candidateViewHeight, opts.fontInfo);
        this._listWidget.onDidChangeFocus(e => {
            if (e.elements.length === 1) {
                opts.onFocusChange(e.elements[0].newSymbolName);
            }
        }, this._disposables);
        this._listWidget.onDidChangeSelection(e => {
            if (e.elements.length === 1) {
                opts.onSelectionChange();
            }
        }, this._disposables);
        this._disposables.add(this._listWidget.onDidBlur(e => {
            this._listWidget.setFocus([]);
        }));
        this._listWidget.style(getListStyles({
            listInactiveFocusForeground: quickInputListFocusForeground,
            listInactiveFocusBackground: quickInputListFocusBackground,
        }));
    }
    dispose() {
        this._listWidget.dispose();
        this._disposables.dispose();
    }
    // height - max height allowed by parent element
    layout({ height, width }) {
        this._availableHeight = height;
        this._minimumWidth = width;
    }
    setCandidates(candidates) {
        // insert candidates into list widget
        this._listWidget.splice(0, 0, candidates);
        // adjust list widget layout
        const height = this._pickListHeight(candidates.length);
        const width = this._pickListWidth(candidates);
        this._listWidget.layout(height, width);
        // adjust list container layout
        this._listContainer.style.height = `${height}px`;
        this._listContainer.style.width = `${width}px`;
        aria.status(localize('renameSuggestionsReceivedAria', "Received {0} rename suggestions", candidates.length));
    }
    clearCandidates() {
        this._listContainer.style.height = '0px';
        this._listContainer.style.width = '0px';
        this._listWidget.splice(0, this._listWidget.length, []);
    }
    get nCandidates() {
        return this._listWidget.length;
    }
    get focusedCandidate() {
        if (this._listWidget.length === 0) {
            return;
        }
        const selectedElement = this._listWidget.getSelectedElements()[0];
        if (selectedElement !== undefined) {
            return selectedElement.newSymbolName;
        }
        const focusedElement = this._listWidget.getFocusedElements()[0];
        if (focusedElement !== undefined) {
            return focusedElement.newSymbolName;
        }
        return;
    }
    focusNext() {
        if (this._listWidget.length === 0) {
            return false;
        }
        const focusedIxs = this._listWidget.getFocus();
        if (focusedIxs.length === 0) {
            this._listWidget.focusFirst();
            return true;
        }
        else {
            if (focusedIxs[0] === this._listWidget.length - 1) {
                this._listWidget.setFocus([]);
                return false;
            }
            else {
                this._listWidget.focusNext();
                return true;
            }
        }
    }
    /**
     * @returns true if focus is moved to previous element
     */
    focusPrevious() {
        if (this._listWidget.length === 0) {
            return false;
        }
        const focusedIxs = this._listWidget.getFocus();
        if (focusedIxs.length === 0) {
            this._listWidget.focusLast();
            return true;
        }
        else {
            if (focusedIxs[0] === 0) {
                this._listWidget.setFocus([]);
                return false;
            }
            else {
                this._listWidget.focusPrevious();
                return true;
            }
        }
    }
    clearFocus() {
        this._listWidget.setFocus([]);
    }
    get _candidateViewHeight() {
        const { totalHeight } = RenameCandidateView.getLayoutInfo({ lineHeight: this._lineHeight });
        return totalHeight;
    }
    _pickListHeight(nCandidates) {
        const heightToFitAllCandidates = this._candidateViewHeight * nCandidates;
        const MAX_N_CANDIDATES = 7; // @ulugbekna: max # of candidates we want to show at once
        const height = Math.min(heightToFitAllCandidates, this._availableHeight, this._candidateViewHeight * MAX_N_CANDIDATES);
        return height;
    }
    _pickListWidth(candidates) {
        const longestCandidateWidth = Math.ceil(Math.max(...candidates.map(c => c.newSymbolName.length)) * this._typicalHalfwidthCharacterWidth);
        const width = Math.max(this._minimumWidth, 4 /* padding */ + 16 /* sparkle icon */ + 5 /* margin-left */ + longestCandidateWidth + 10 /* (possibly visible) scrollbar width */ // TODO@ulugbekna: approximate calc - clean this up
        );
        return width;
    }
    static _createListWidget(container, candidateViewHeight, fontInfo) {
        const virtualDelegate = new class {
            getTemplateId(element) {
                return 'candidate';
            }
            getHeight(element) {
                return candidateViewHeight;
            }
        };
        const renderer = new class {
            constructor() {
                this.templateId = 'candidate';
            }
            renderTemplate(container) {
                return new RenameCandidateView(container, fontInfo);
            }
            renderElement(candidate, index, templateData) {
                templateData.populate(candidate);
            }
            disposeTemplate(templateData) {
                templateData.dispose();
            }
        };
        return new List('NewSymbolNameCandidates', container, virtualDelegate, [renderer], {
            keyboardSupport: false, // @ulugbekna: because we handle keyboard events through proper commands & keybinding service, see `rename.ts`
            mouseSupport: true,
            multipleSelectionSupport: false,
        });
    }
}
/**
 * @remarks lazily creates the DOM node
 */
class RenameInput {
    constructor() {
        this._onDidChange = new Emitter();
        this.onDidChange = this._onDidChange.event;
        this._disposables = new DisposableStore();
    }
    get domNode() {
        if (!this._domNode) {
            this._domNode = document.createElement('input');
            this._domNode.className = 'rename-input';
            this._domNode.type = 'text';
            this._domNode.setAttribute('aria-label', localize('renameAriaLabel', "Rename input. Type new name and press Enter to commit."));
            this._disposables.add(dom.addDisposableListener(this._domNode, 'input', () => this._onDidChange.fire()));
        }
        return this._domNode;
    }
    dispose() {
        this._onDidChange.dispose();
        this._disposables.dispose();
    }
}
class RenameCandidateView {
    constructor(parent, fontInfo) {
        this._domNode = document.createElement('div');
        this._domNode.style.display = `flex`;
        this._domNode.style.columnGap = `5px`;
        this._domNode.style.alignItems = `center`;
        this._domNode.style.height = `${fontInfo.lineHeight}px`;
        this._domNode.style.padding = `${RenameCandidateView._PADDING}px`;
        // @ulugbekna: needed to keep space when the `icon.style.display` is set to `none`
        const iconContainer = document.createElement('div');
        iconContainer.style.display = `flex`;
        iconContainer.style.alignItems = `center`;
        iconContainer.style.width = iconContainer.style.height = `${fontInfo.lineHeight * 0.8}px`;
        this._domNode.appendChild(iconContainer);
        this._icon = renderIcon(Codicon.sparkle);
        this._icon.style.display = `none`;
        iconContainer.appendChild(this._icon);
        this._label = document.createElement('div');
        applyFontInfo(this._label, fontInfo);
        this._domNode.appendChild(this._label);
        parent.appendChild(this._domNode);
    }
    populate(value) {
        this._updateIcon(value);
        this._updateLabel(value);
    }
    _updateIcon(value) {
        var _a;
        const isAIGenerated = !!((_a = value.tags) === null || _a === void 0 ? void 0 : _a.includes(NewSymbolNameTag.AIGenerated));
        this._icon.style.display = isAIGenerated ? 'inherit' : 'none';
    }
    _updateLabel(value) {
        this._label.innerText = value.newSymbolName;
    }
    static getLayoutInfo({ lineHeight }) {
        const totalHeight = lineHeight + RenameCandidateView._PADDING * 2 /* top & bottom padding */;
        return { totalHeight };
    }
    dispose() {
    }
}
RenameCandidateView._PADDING = 2;
