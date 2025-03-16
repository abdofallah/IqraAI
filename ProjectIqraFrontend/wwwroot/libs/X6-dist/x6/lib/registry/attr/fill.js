"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.fill = void 0;
const x6_common_1 = require("@antv/x6-common");
exports.fill = {
    qualify: x6_common_1.ObjectExt.isPlainObject,
    set(fill, { view }) {
        return `url(#${view.graph.defineGradient(fill)})`;
    },
};
//# sourceMappingURL=fill.js.map