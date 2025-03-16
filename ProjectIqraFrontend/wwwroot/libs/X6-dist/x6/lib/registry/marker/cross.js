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
exports.cross = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const util_1 = require("./util");
const cross = (_a) => {
    var { size, width, height, offset } = _a, attrs = __rest(_a, ["size", "width", "height", "offset"]);
    const s = size || 10;
    const w = width || s;
    const h = height || s;
    const path = new x6_geometry_1.Path();
    path.moveTo(0, 0).lineTo(w, h).moveTo(0, h).lineTo(w, 0);
    return Object.assign(Object.assign({}, attrs), { tagName: 'path', fill: 'none', d: (0, util_1.normalize)(path.serialize(), offset || -w / 2) });
};
exports.cross = cross;
//# sourceMappingURL=cross.js.map