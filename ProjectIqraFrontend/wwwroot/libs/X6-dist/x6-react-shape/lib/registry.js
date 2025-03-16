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
exports.register = exports.shapeMaps = void 0;
const x6_1 = require("@antv/x6");
exports.shapeMaps = {};
function register(config) {
    const { shape, component, effect, inherit } = config, others = __rest(config, ["shape", "component", "effect", "inherit"]);
    if (!shape) {
        throw new Error('should specify shape in config');
    }
    exports.shapeMaps[shape] = {
        component,
        effect,
    };
    x6_1.Graph.registerNode(shape, Object.assign({ inherit: inherit || 'react-shape' }, others), true);
}
exports.register = register;
//# sourceMappingURL=registry.js.map