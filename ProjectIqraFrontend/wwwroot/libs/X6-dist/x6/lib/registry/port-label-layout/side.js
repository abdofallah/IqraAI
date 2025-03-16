"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.bottom = exports.top = exports.right = exports.left = exports.manual = void 0;
const util_1 = require("./util");
const manual = (portPosition, elemBBox, args) => (0, util_1.toResult)({ position: elemBBox.getTopLeft() }, args);
exports.manual = manual;
const left = (portPosition, elemBBox, args) => (0, util_1.toResult)({
    position: { x: -15, y: 0 },
    attrs: { '.': { y: '.3em', 'text-anchor': 'end' } },
}, args);
exports.left = left;
const right = (portPosition, elemBBox, args) => (0, util_1.toResult)({
    position: { x: 15, y: 0 },
    attrs: { '.': { y: '.3em', 'text-anchor': 'start' } },
}, args);
exports.right = right;
const top = (portPosition, elemBBox, args) => (0, util_1.toResult)({
    position: { x: 0, y: -15 },
    attrs: { '.': { 'text-anchor': 'middle' } },
}, args);
exports.top = top;
const bottom = (portPosition, elemBBox, args) => (0, util_1.toResult)({
    position: { x: 0, y: 15 },
    attrs: { '.': { y: '.6em', 'text-anchor': 'middle' } },
}, args);
exports.bottom = bottom;
//# sourceMappingURL=side.js.map