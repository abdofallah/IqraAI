"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.getTargetAnchor = exports.getSourceAnchor = exports.getTargetBBox = exports.getSourceBBox = exports.getPaddingBox = exports.getPointBBox = void 0;
const x6_common_1 = require("@antv/x6-common");
const x6_geometry_1 = require("@antv/x6-geometry");
function getPointBBox(p) {
    return new x6_geometry_1.Rectangle(p.x, p.y, 0, 0);
}
exports.getPointBBox = getPointBBox;
function getPaddingBox(options = {}) {
    const sides = x6_common_1.NumberExt.normalizeSides(options.padding || 20);
    return {
        x: -sides.left,
        y: -sides.top,
        width: sides.left + sides.right,
        height: sides.top + sides.bottom,
    };
}
exports.getPaddingBox = getPaddingBox;
function getSourceBBox(view, options = {}) {
    return view.sourceBBox.clone().moveAndExpand(getPaddingBox(options));
}
exports.getSourceBBox = getSourceBBox;
function getTargetBBox(view, options = {}) {
    return view.targetBBox.clone().moveAndExpand(getPaddingBox(options));
}
exports.getTargetBBox = getTargetBBox;
function getSourceAnchor(view, options = {}) {
    if (view.sourceAnchor) {
        return view.sourceAnchor;
    }
    const bbox = getSourceBBox(view, options);
    return bbox.getCenter();
}
exports.getSourceAnchor = getSourceAnchor;
function getTargetAnchor(view, options = {}) {
    if (view.targetAnchor) {
        return view.targetAnchor;
    }
    const bbox = getTargetBBox(view, options);
    return bbox.getCenter();
}
exports.getTargetAnchor = getTargetAnchor;
//# sourceMappingURL=util.js.map