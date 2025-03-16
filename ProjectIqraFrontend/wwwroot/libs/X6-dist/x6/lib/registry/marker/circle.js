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
exports.circlePlus = exports.circle = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const util_1 = require("./util");
const circle = (_a) => {
    var { r } = _a, attrs = __rest(_a, ["r"]);
    const radius = r || 5;
    return Object.assign(Object.assign({ cx: radius }, attrs), { tagName: 'circle', r: radius });
};
exports.circle = circle;
const circlePlus = (_a) => {
    var { r } = _a, attrs = __rest(_a, ["r"]);
    const radius = r || 5;
    const path = new x6_geometry_1.Path();
    path.moveTo(radius, 0).lineTo(radius, radius * 2);
    path.moveTo(0, radius).lineTo(radius * 2, radius);
    return {
        children: [
            Object.assign(Object.assign({}, (0, exports.circle)({ r: radius })), { fill: 'none' }),
            Object.assign(Object.assign({}, attrs), { tagName: 'path', d: (0, util_1.normalize)(path.serialize(), -radius) }),
        ],
    };
};
exports.circlePlus = circlePlus;
//# sourceMappingURL=circle.js.map