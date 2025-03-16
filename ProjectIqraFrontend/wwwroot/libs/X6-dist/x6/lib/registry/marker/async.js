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
exports.async = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const util_1 = require("./util");
const async = (_a) => {
    var { width, height, offset, open, flip } = _a, attrs = __rest(_a, ["width", "height", "offset", "open", "flip"]);
    let h = height || 6;
    const w = width || 10;
    const opened = open === true;
    const fliped = flip === true;
    const result = Object.assign(Object.assign({}, attrs), { tagName: 'path' });
    if (fliped) {
        h = -h;
    }
    const path = new x6_geometry_1.Path();
    path.moveTo(0, h).lineTo(w, 0);
    if (!opened) {
        path.lineTo(w, h);
        path.close();
    }
    else {
        result.fill = 'none';
    }
    result.d = (0, util_1.normalize)(path.serialize(), {
        x: offset || -w / 2,
        y: h / 2,
    });
    return result;
};
exports.async = async;
//# sourceMappingURL=async.js.map