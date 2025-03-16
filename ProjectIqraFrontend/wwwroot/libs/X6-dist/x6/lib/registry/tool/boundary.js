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
var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                t[p[i]] = s[p[i]];
        }
    return t;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Boundary = void 0;
const x6_common_1 = require("@antv/x6-common");
const tool_1 = require("../../view/tool");
const Util = __importStar(require("./util"));
class Boundary extends tool_1.ToolsView.ToolItem {
    onRender() {
        x6_common_1.Dom.addClass(this.container, this.prefixClassName('cell-tool-boundary'));
        if (this.options.attrs) {
            const _a = this.options.attrs, { class: className } = _a, attrs = __rest(_a, ["class"]);
            x6_common_1.Dom.attr(this.container, x6_common_1.Dom.kebablizeAttrs(attrs));
            if (className) {
                x6_common_1.Dom.addClass(this.container, className);
            }
        }
        this.update();
    }
    update() {
        const view = this.cellView;
        const options = this.options;
        const { useCellGeometry, rotate } = options;
        const padding = x6_common_1.NumberExt.normalizeSides(options.padding);
        let bbox = Util.getViewBBox(view, useCellGeometry).moveAndExpand({
            x: -padding.left,
            y: -padding.top,
            width: padding.left + padding.right,
            height: padding.top + padding.bottom,
        });
        const cell = view.cell;
        if (cell.isNode()) {
            const angle = cell.getAngle();
            if (angle) {
                if (rotate) {
                    const origin = cell.getBBox().getCenter();
                    x6_common_1.Dom.rotate(this.container, angle, origin.x, origin.y, {
                        absolute: true,
                    });
                }
                else {
                    bbox = bbox.bbox(angle);
                }
            }
        }
        x6_common_1.Dom.attr(this.container, bbox.toJSON());
        return this;
    }
}
exports.Boundary = Boundary;
(function (Boundary) {
    Boundary.config({
        name: 'boundary',
        tagName: 'rect',
        padding: 10,
        useCellGeometry: true,
        attrs: {
            fill: 'none',
            stroke: '#333',
            'stroke-width': 0.5,
            'stroke-dasharray': '5, 5',
            'pointer-events': 'none',
        },
    });
})(Boundary = exports.Boundary || (exports.Boundary = {}));
//# sourceMappingURL=boundary.js.map