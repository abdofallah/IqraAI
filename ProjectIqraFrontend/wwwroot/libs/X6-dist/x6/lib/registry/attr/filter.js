"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.filter = void 0;
const x6_common_1 = require("@antv/x6-common");
exports.filter = {
    qualify: x6_common_1.ObjectExt.isPlainObject,
    set(filter, { view }) {
        return `url(#${view.graph.defineFilter(filter)})`;
    },
};
//# sourceMappingURL=filter.js.map