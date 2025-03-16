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
exports.path = void 0;
const util_1 = require("./util");
const path = (_a) => {
    var { d, offsetX, offsetY } = _a, attrs = __rest(_a, ["d", "offsetX", "offsetY"]);
    return Object.assign(Object.assign({}, attrs), { tagName: 'path', d: (0, util_1.normalize)(d, offsetX, offsetY) });
};
exports.path = path;
//# sourceMappingURL=path.js.map