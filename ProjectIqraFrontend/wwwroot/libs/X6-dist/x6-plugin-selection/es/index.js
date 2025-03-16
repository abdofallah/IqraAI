var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { Basecoat, ModifierKey, CssLoader, } from '@antv/x6';
import { SelectionImpl } from './selection';
import { content } from './style/raw';
import './api';
export class Selection extends Basecoat {
    get rubberbandDisabled() {
        return this.options.enabled !== true || this.options.rubberband !== true;
    }
    get disabled() {
        return this.options.enabled !== true;
    }
    get length() {
        return this.selectionImpl.length;
    }
    get cells() {
        return this.selectionImpl.cells;
    }
    constructor(options = {}) {
        super();
        this.name = 'selection';
        this.movedMap = new WeakMap();
        this.unselectMap = new WeakMap();
        this.options = Object.assign(Object.assign({ enabled: true }, Selection.defaultOptions), options);
        CssLoader.ensure(this.name, content);
    }
    init(graph) {
        this.graph = graph;
        this.selectionImpl = new SelectionImpl(Object.assign(Object.assign({}, this.options), { graph }));
        this.setup();
        this.startListening();
    }
    // #region api
    isEnabled() {
        return !this.disabled;
    }
    enable() {
        if (this.disabled) {
            this.options.enabled = true;
        }
    }
    disable() {
        if (!this.disabled) {
            this.options.enabled = false;
        }
    }
    toggleEnabled(enabled) {
        if (enabled != null) {
            if (enabled !== this.isEnabled()) {
                if (enabled) {
                    this.enable();
                }
                else {
                    this.disable();
                }
            }
        }
        else if (this.isEnabled()) {
            this.disable();
        }
        else {
            this.enable();
        }
        return this;
    }
    isMultipleSelection() {
        return this.isMultiple();
    }
    enableMultipleSelection() {
        this.enableMultiple();
        return this;
    }
    disableMultipleSelection() {
        this.disableMultiple();
        return this;
    }
    toggleMultipleSelection(multiple) {
        if (multiple != null) {
            if (multiple !== this.isMultipleSelection()) {
                if (multiple) {
                    this.enableMultipleSelection();
                }
                else {
                    this.disableMultipleSelection();
                }
            }
        }
        else if (this.isMultipleSelection()) {
            this.disableMultipleSelection();
        }
        else {
            this.enableMultipleSelection();
        }
        return this;
    }
    isSelectionMovable() {
        return this.options.movable !== false;
    }
    enableSelectionMovable() {
        this.selectionImpl.options.movable = true;
        return this;
    }
    disableSelectionMovable() {
        this.selectionImpl.options.movable = false;
        return this;
    }
    toggleSelectionMovable(movable) {
        if (movable != null) {
            if (movable !== this.isSelectionMovable()) {
                if (movable) {
                    this.enableSelectionMovable();
                }
                else {
                    this.disableSelectionMovable();
                }
            }
        }
        else if (this.isSelectionMovable()) {
            this.disableSelectionMovable();
        }
        else {
            this.enableSelectionMovable();
        }
        return this;
    }
    isRubberbandEnabled() {
        return !this.rubberbandDisabled;
    }
    enableRubberband() {
        if (this.rubberbandDisabled) {
            this.options.rubberband = true;
        }
        return this;
    }
    disableRubberband() {
        if (!this.rubberbandDisabled) {
            this.options.rubberband = false;
        }
        return this;
    }
    toggleRubberband(enabled) {
        if (enabled != null) {
            if (enabled !== this.isRubberbandEnabled()) {
                if (enabled) {
                    this.enableRubberband();
                }
                else {
                    this.disableRubberband();
                }
            }
        }
        else if (this.isRubberbandEnabled()) {
            this.disableRubberband();
        }
        else {
            this.enableRubberband();
        }
        return this;
    }
    isStrictRubberband() {
        return this.selectionImpl.options.strict === true;
    }
    enableStrictRubberband() {
        this.selectionImpl.options.strict = true;
        return this;
    }
    disableStrictRubberband() {
        this.selectionImpl.options.strict = false;
        return this;
    }
    toggleStrictRubberband(strict) {
        if (strict != null) {
            if (strict !== this.isStrictRubberband()) {
                if (strict) {
                    this.enableStrictRubberband();
                }
                else {
                    this.disableStrictRubberband();
                }
            }
        }
        else if (this.isStrictRubberband()) {
            this.disableStrictRubberband();
        }
        else {
            this.enableStrictRubberband();
        }
        return this;
    }
    setRubberbandModifiers(modifiers) {
        this.setModifiers(modifiers);
    }
    setSelectionFilter(filter) {
        this.setFilter(filter);
        return this;
    }
    setSelectionDisplayContent(content) {
        this.setContent(content);
        return this;
    }
    isEmpty() {
        return this.length <= 0;
    }
    clean(options = {}) {
        this.selectionImpl.clean(options);
        return this;
    }
    reset(cells, options = {}) {
        this.selectionImpl.reset(cells ? this.getCells(cells) : [], options);
        return this;
    }
    getSelectedCells() {
        return this.cells;
    }
    getSelectedCellCount() {
        return this.length;
    }
    isSelected(cell) {
        return this.selectionImpl.isSelected(cell);
    }
    select(cells, options = {}) {
        const selected = this.getCells(cells);
        if (selected.length) {
            if (this.isMultiple()) {
                this.selectionImpl.select(selected, options);
            }
            else {
                this.reset(selected.slice(0, 1), options);
            }
        }
        return this;
    }
    unselect(cells, options = {}) {
        this.selectionImpl.unselect(this.getCells(cells), options);
        return this;
    }
    // #endregion
    setup() {
        this.selectionImpl.on('*', (name, args) => {
            this.trigger(name, args);
            this.graph.trigger(name, args);
        });
    }
    startListening() {
        this.graph.on('blank:mousedown', this.onBlankMouseDown, this);
        this.graph.on('blank:click', this.onBlankClick, this);
        this.graph.on('cell:mousemove', this.onCellMouseMove, this);
        this.graph.on('cell:mouseup', this.onCellMouseUp, this);
        this.selectionImpl.on('box:mousedown', this.onBoxMouseDown, this);
    }
    stopListening() {
        this.graph.off('blank:mousedown', this.onBlankMouseDown, this);
        this.graph.off('blank:click', this.onBlankClick, this);
        this.graph.off('cell:mousemove', this.onCellMouseMove, this);
        this.graph.off('cell:mouseup', this.onCellMouseUp, this);
        this.selectionImpl.off('box:mousedown', this.onBoxMouseDown, this);
    }
    onBlankMouseDown({ e }) {
        if (!this.allowBlankMouseDown(e)) {
            return;
        }
        const allowGraphPanning = this.graph.panning.allowPanning(e, true);
        const scroller = this.graph.getPlugin('scroller');
        const allowScrollerPanning = scroller && scroller.allowPanning(e, true);
        if (this.allowRubberband(e, true) ||
            (this.allowRubberband(e) && !allowScrollerPanning && !allowGraphPanning)) {
            this.startRubberband(e);
        }
    }
    allowBlankMouseDown(e) {
        const eventTypes = this.options.eventTypes;
        return (((eventTypes === null || eventTypes === void 0 ? void 0 : eventTypes.includes('leftMouseDown')) && e.button === 0) ||
            ((eventTypes === null || eventTypes === void 0 ? void 0 : eventTypes.includes('mouseWheelDown')) && e.button === 1));
    }
    onBlankClick() {
        this.clean();
    }
    allowRubberband(e, strict) {
        return (!this.rubberbandDisabled &&
            ModifierKey.isMatch(e, this.options.modifiers, strict));
    }
    allowMultipleSelection(e) {
        return (this.isMultiple() &&
            ModifierKey.isMatch(e, this.options.multipleSelectionModifiers));
    }
    onCellMouseMove({ cell }) {
        this.movedMap.set(cell, true);
    }
    onCellMouseUp({ e, cell }) {
        const options = this.options;
        let disabled = this.disabled;
        if (!disabled && this.movedMap.has(cell)) {
            disabled = options.selectCellOnMoved === false;
            if (!disabled) {
                disabled = options.selectNodeOnMoved === false && cell.isNode();
            }
            if (!disabled) {
                disabled = options.selectEdgeOnMoved === false && cell.isEdge();
            }
        }
        if (!disabled) {
            if (!this.allowMultipleSelection(e)) {
                this.reset(cell);
            }
            else if (this.unselectMap.has(cell)) {
                this.unselectMap.delete(cell);
            }
            else if (this.isSelected(cell)) {
                this.unselect(cell);
            }
            else {
                this.select(cell);
            }
        }
        this.movedMap.delete(cell);
    }
    onBoxMouseDown({ e, cell, }) {
        if (!this.disabled) {
            if (this.allowMultipleSelection(e)) {
                this.unselect(cell);
                this.unselectMap.set(cell, true);
            }
        }
    }
    getCells(cells) {
        return (Array.isArray(cells) ? cells : [cells])
            .map((cell) => typeof cell === 'string' ? this.graph.getCellById(cell) : cell)
            .filter((cell) => cell != null);
    }
    startRubberband(e) {
        if (!this.rubberbandDisabled) {
            this.selectionImpl.startSelecting(e);
        }
        return this;
    }
    isMultiple() {
        return this.options.multiple !== false;
    }
    enableMultiple() {
        this.options.multiple = true;
        return this;
    }
    disableMultiple() {
        this.options.multiple = false;
        return this;
    }
    setModifiers(modifiers) {
        this.options.modifiers = modifiers;
        return this;
    }
    setContent(content) {
        this.selectionImpl.setContent(content);
        return this;
    }
    setFilter(filter) {
        this.selectionImpl.setFilter(filter);
        return this;
    }
    dispose() {
        this.stopListening();
        this.off();
        this.selectionImpl.dispose();
        CssLoader.clean(this.name);
    }
}
__decorate([
    Basecoat.dispose()
], Selection.prototype, "dispose", null);
(function (Selection) {
    Selection.defaultOptions = {
        rubberband: false,
        rubberNode: true,
        rubberEdge: false,
        pointerEvents: 'auto',
        multiple: true,
        multipleSelectionModifiers: ['ctrl', 'meta'],
        movable: true,
        strict: false,
        selectCellOnMoved: false,
        selectNodeOnMoved: false,
        selectEdgeOnMoved: false,
        following: true,
        content: null,
        eventTypes: ['leftMouseDown', 'mouseWheelDown'],
    };
})(Selection || (Selection = {}));
//# sourceMappingURL=index.js.map