"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Button = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const x6_common_1 = require("@antv/x6-common");
const tool_1 = require("../../view/tool");
const Util = __importStar(require("./util"));
class Button extends tool_1.ToolsView.ToolItem {
    onRender() {
        x6_common_1.Dom.addClass(this.container, this.prefixClassName('cell-tool-button'));
        this.update();
    }
    update() {
        this.updatePosition();
        return this;
    }
    updatePosition() {
        const view = this.cellView;
        const matrix = view.cell.isEdge()
            ? this.getEdgeMatrix()
            : this.getNodeMatrix();
        x6_common_1.Dom.transform(this.container, matrix, { absolute: true });
    }
    getNodeMatrix() {
        const view = this.cellView;
        const options = this.options;
        let { x = 0, y = 0 } = options;
        const { offset, useCellGeometry, rotate } = options;
        let bbox = Util.getViewBBox(view, useCellGeometry);
        const angle = view.cell.getAngle();
        if (!rotate) {
            bbox = bbox.bbox(angle);
        }
        let offsetX = 0;
        let offsetY = 0;
        if (typeof offset === 'number') {
            offsetX = offset;
            offsetY = offset;
        }
        else if (typeof offset === 'object') {
            offsetX = offset.x;
            offsetY = offset.y;
        }
        x = x6_common_1.NumberExt.normalizePercentage(x, bbox.width);
        y = x6_common_1.NumberExt.normalizePercentage(y, bbox.height);
        let matrix = x6_common_1.Dom.createSVGMatrix().translate(bbox.x + bbox.width / 2, bbox.y + bbox.height / 2);
        if (rotate) {
            matrix = matrix.rotate(angle);
        }
        matrix = matrix.translate(x + offsetX - bbox.width / 2, y + offsetY - bbox.height / 2);
        return matrix;
    }
    getEdgeMatrix() {
        const view = this.cellView;
        const options = this.options;
        const { offset = 0, distance = 0, rotate } = options;
        let tangent;
        let position;
        let angle;
        const d = x6_common_1.NumberExt.normalizePercentage(distance, 1);
        if (d >= 0 && d <= 1) {
            tangent = view.getTangentAtRatio(d);
        }
        else {
            tangent = view.getTangentAtLength(d);
        }
        if (tangent) {
            position = tangent.start;
            angle = tangent.vector().vectorAngle(new x6_geometry_1.Point(1, 0)) || 0;
        }
        else {
            position = view.getConnection().start;
            angle = 0;
        }
        let matrix = x6_common_1.Dom.createSVGMatrix()
            .translate(position.x, position.y)
            .rotate(angle);
        if (typeof offset === 'object') {
            matrix = matrix.translate(offset.x || 0, offset.y || 0);
        }
        else {
            matrix = matrix.translate(0, offset);
        }
        if (!rotate) {
            matrix = matrix.rotate(-angle);
        }
        return matrix;
    }
    onMouseDown(e) {
        if (this.guard(e)) {
            return;
        }
        e.stopPropagation();
        e.preventDefault();
        const onClick = this.options.onClick;
        if (typeof onClick === 'function') {
            x6_common_1.FunctionExt.call(onClick, this.cellView, {
                e,
                view: this.cellView,
                cell: this.cellView.cell,
                btn: this,
            });
        }
    }
}
exports.Button = Button;
(function (Button) {
    Button.config({
        name: 'button',
        useCellGeometry: true,
        events: {
            mousedown: 'onMouseDown',
            touchstart: 'onMouseDown',
        },
    });
})(Button = exports.Button || (exports.Button = {}));
(function (Button) {
    Button.Remove = Button.define({
        name: 'button-remove',
        markup: [
            {
                tagName: 'circle',
                selector: 'button',
                attrs: {
                    r: 7,
                    fill: '#FF1D00',
                    cursor: 'pointer',
                },
            },
            {
                tagName: 'path',
                selector: 'icon',
                attrs: {
                    d: 'M -3 -3 3 3 M -3 3 3 -3',
                    fill: 'none',
                    stroke: '#FFFFFF',
                    'stroke-width': 2,
                    'pointer-events': 'none',
                },
            },
        ],
        distance: 60,
        offset: 0,
        useCellGeometry: true,
        onClick({ view, btn }) {
            btn.parent.remove();
            view.cell.remove({ ui: true, toolId: btn.cid });
        },
    });
})(Button = exports.Button || (exports.Button = {}));
//# sourceMappingURL=button.js.map