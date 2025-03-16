"use strict";
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
exports.bottom = exports.top = exports.right = exports.left = exports.line = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const util_1 = require("./util");
const line = (portsPositionArgs, elemBBox, groupPositionArgs) => {
    const start = (0, util_1.normalizePoint)(elemBBox, groupPositionArgs.start || elemBBox.getOrigin());
    const end = (0, util_1.normalizePoint)(elemBBox, groupPositionArgs.end || elemBBox.getCorner());
    return lineLayout(portsPositionArgs, start, end, groupPositionArgs);
};
exports.line = line;
const left = (portsPositionArgs, elemBBox, groupPositionArgs) => {
    return lineLayout(portsPositionArgs, elemBBox.getTopLeft(), elemBBox.getBottomLeft(), groupPositionArgs);
};
exports.left = left;
const right = (portsPositionArgs, elemBBox, groupPositionArgs) => {
    return lineLayout(portsPositionArgs, elemBBox.getTopRight(), elemBBox.getBottomRight(), groupPositionArgs);
};
exports.right = right;
const top = (portsPositionArgs, elemBBox, groupPositionArgs) => {
    return lineLayout(portsPositionArgs, elemBBox.getTopLeft(), elemBBox.getTopRight(), groupPositionArgs);
};
exports.top = top;
const bottom = (portsPositionArgs, elemBBox, groupPositionArgs) => {
    return lineLayout(portsPositionArgs, elemBBox.getBottomLeft(), elemBBox.getBottomRight(), groupPositionArgs);
};
exports.bottom = bottom;
function lineLayout(portsPositionArgs, p1, p2, groupPositionArgs) {
    const line = new x6_geometry_1.Line(p1, p2);
    const length = portsPositionArgs.length;
    return portsPositionArgs.map((_a, index) => {
        var { strict } = _a, offset = __rest(_a, ["strict"]);
        const ratio = strict || groupPositionArgs.strict
            ? (index + 1) / (length + 1)
            : (index + 0.5) / length;
        const p = line.pointAt(ratio);
        if (offset.dx || offset.dy) {
            p.translate(offset.dx || 0, offset.dy || 0);
        }
        return (0, util_1.toResult)(p.round(), 0, offset);
    });
}
//# sourceMappingURL=line.js.map