"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CellEditor = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const x6_common_1 = require("@antv/x6-common");
const tool_1 = require("../../view/tool");
const util_1 = require("../../util");
class CellEditor extends tool_1.ToolsView.ToolItem {
    constructor() {
        super(...arguments);
        this.labelIndex = -1;
        this.distance = 0.5;
        this.dblClick = this.onCellDblClick.bind(this);
    }
    onRender() {
        const cellView = this.cellView;
        if (cellView) {
            cellView.on('cell:dblclick', this.dblClick);
        }
    }
    createElement() {
        const classNames = [
            this.prefixClassName(`${this.cell.isEdge() ? 'edge' : 'node'}-tool-editor`),
            this.prefixClassName('cell-tool-editor'),
        ];
        this.editor = tool_1.ToolsView.createElement('div', false);
        this.addClass(classNames, this.editor);
        this.editor.contentEditable = 'true';
        this.container.appendChild(this.editor);
    }
    removeElement() {
        this.undelegateDocumentEvents();
        if (this.editor) {
            this.container.removeChild(this.editor);
            this.editor = null;
        }
    }
    updateEditor() {
        const { cell, editor } = this;
        if (!editor) {
            return;
        }
        const { style } = editor;
        if (cell.isNode()) {
            this.updateNodeEditorTransform();
        }
        else if (cell.isEdge()) {
            this.updateEdgeEditorTransform();
        }
        // set font style
        const { attrs } = this.options;
        style.fontSize = `${attrs.fontSize}px`;
        style.fontFamily = attrs.fontFamily;
        style.color = attrs.color;
        style.backgroundColor = attrs.backgroundColor;
        // set init value
        const text = this.getCellText() || '';
        editor.innerText = text;
        this.setCellText(''); // clear display value when edit status because char ghosting.
        return this;
    }
    updateNodeEditorTransform() {
        const { graph, cell, editor } = this;
        if (!editor) {
            return;
        }
        let pos = x6_geometry_1.Point.create();
        let minWidth = 20;
        let translate = '';
        let { x, y } = this.options;
        const { width, height } = this.options;
        if (typeof x !== 'undefined' && typeof y !== 'undefined') {
            const bbox = cell.getBBox();
            x = x6_common_1.NumberExt.normalizePercentage(x, bbox.width);
            y = x6_common_1.NumberExt.normalizePercentage(y, bbox.height);
            pos = bbox.topLeft.translate(x, y);
            minWidth = bbox.width - x * 2;
        }
        else {
            const bbox = cell.getBBox();
            pos = bbox.center;
            minWidth = bbox.width - 4;
            translate = 'translate(-50%, -50%)';
        }
        const scale = graph.scale();
        const { style } = editor;
        pos = graph.localToGraph(pos);
        style.left = `${pos.x}px`;
        style.top = `${pos.y}px`;
        style.transform = `scale(${scale.sx}, ${scale.sy}) ${translate}`;
        style.minWidth = `${minWidth}px`;
        if (typeof width === 'number') {
            style.width = `${width}px`;
        }
        if (typeof height === 'number') {
            style.height = `${height}px`;
        }
    }
    updateEdgeEditorTransform() {
        if (!this.event) {
            return;
        }
        const { graph, editor } = this;
        if (!editor) {
            return;
        }
        let pos = x6_geometry_1.Point.create();
        let minWidth = 20;
        const { style } = editor;
        const target = this.event.target;
        const parent = target.parentElement;
        const isEdgeLabel = parent && x6_common_1.Dom.hasClass(parent, this.prefixClassName('edge-label'));
        if (isEdgeLabel) {
            const index = parent.getAttribute('data-index') || '0';
            this.labelIndex = parseInt(index, 10);
            const matrix = parent.getAttribute('transform');
            const { translation } = x6_common_1.Dom.parseTransformString(matrix);
            pos = new x6_geometry_1.Point(translation.tx, translation.ty);
            minWidth = util_1.Util.getBBox(target).width;
        }
        else {
            if (!this.options.labelAddable) {
                return this;
            }
            pos = graph.clientToLocal(x6_geometry_1.Point.create(this.event.clientX, this.event.clientY));
            const view = this.cellView;
            const d = view.path.closestPointLength(pos);
            this.distance = d;
            this.labelIndex = -1;
        }
        pos = graph.localToGraph(pos);
        const scale = graph.scale();
        style.left = `${pos.x}px`;
        style.top = `${pos.y}px`;
        style.minWidth = `${minWidth}px`;
        style.transform = `scale(${scale.sx}, ${scale.sy}) translate(-50%, -50%)`;
    }
    onDocumentMouseUp(e) {
        if (this.editor && e.target !== this.editor) {
            const value = this.editor.innerText.replace(/\n$/, '') || '';
            // set value, when value is null, we will remove label in edge
            this.setCellText(value !== '' ? value : null);
            // remove tool
            this.removeElement();
        }
    }
    onCellDblClick({ e }) {
        if (!this.editor) {
            e.stopPropagation();
            this.removeElement();
            this.event = e;
            this.createElement();
            this.updateEditor();
            this.autoFocus();
            this.delegateDocumentEvents(this.options.documentEvents);
        }
    }
    onMouseDown(e) {
        e.stopPropagation();
    }
    autoFocus() {
        setTimeout(() => {
            if (this.editor) {
                this.editor.focus();
                this.selectText();
            }
        });
    }
    selectText() {
        if (window.getSelection && this.editor) {
            const range = document.createRange();
            const selection = window.getSelection();
            range.selectNodeContents(this.editor);
            selection.removeAllRanges();
            selection.addRange(range);
        }
    }
    getCellText() {
        const { getText } = this.options;
        if (typeof getText === 'function') {
            return x6_common_1.FunctionExt.call(getText, this.cellView, {
                cell: this.cell,
                index: this.labelIndex,
            });
        }
        if (typeof getText === 'string') {
            if (this.cell.isNode()) {
                return this.cell.attr(getText);
            }
            if (this.cell.isEdge()) {
                if (this.labelIndex !== -1) {
                    return this.cell.prop(`labels/${this.labelIndex}/attrs/${getText}`);
                }
            }
        }
    }
    setCellText(value) {
        const setText = this.options.setText;
        if (typeof setText === 'function') {
            x6_common_1.FunctionExt.call(setText, this.cellView, {
                cell: this.cell,
                value,
                index: this.labelIndex,
                distance: this.distance,
            });
            return;
        }
        if (typeof setText === 'string') {
            if (this.cell.isNode()) {
                if (value !== null) {
                    this.cell.attr(setText, value);
                }
                return;
            }
            if (this.cell.isEdge()) {
                const edge = this.cell;
                if (this.labelIndex === -1) {
                    if (value) {
                        const newLabel = {
                            position: {
                                distance: this.distance,
                            },
                            attrs: {},
                        };
                        x6_common_1.ObjectExt.setByPath(newLabel, `attrs/${setText}`, value);
                        edge.appendLabel(newLabel);
                    }
                }
                else {
                    if (value !== null) {
                        edge.prop(`labels/${this.labelIndex}/attrs/${setText}`, value);
                    }
                    else if (typeof this.labelIndex === 'number') {
                        edge.removeLabelAt(this.labelIndex);
                    }
                }
            }
        }
    }
    onRemove() {
        const cellView = this.cellView;
        if (cellView) {
            cellView.off('cell:dblclick', this.dblClick);
        }
        this.removeElement();
    }
}
exports.CellEditor = CellEditor;
(function (CellEditor) {
    CellEditor.config({
        tagName: 'div',
        isSVGElement: false,
        events: {
            mousedown: 'onMouseDown',
            touchstart: 'onMouseDown',
        },
        documentEvents: {
            mouseup: 'onDocumentMouseUp',
            touchend: 'onDocumentMouseUp',
            touchcancel: 'onDocumentMouseUp',
        },
    });
})(CellEditor = exports.CellEditor || (exports.CellEditor = {}));
(function (CellEditor) {
    CellEditor.NodeEditor = CellEditor.define({
        attrs: {
            fontSize: 14,
            fontFamily: 'Arial, helvetica, sans-serif',
            color: '#000',
            backgroundColor: '#fff',
        },
        getText: 'text/text',
        setText: 'text/text',
    });
    CellEditor.EdgeEditor = CellEditor.define({
        attrs: {
            fontSize: 14,
            fontFamily: 'Arial, helvetica, sans-serif',
            color: '#000',
            backgroundColor: '#fff',
        },
        labelAddable: true,
        getText: 'label/text',
        setText: 'label/text',
    });
})(CellEditor = exports.CellEditor || (exports.CellEditor = {}));
//# sourceMappingURL=editor.js.map