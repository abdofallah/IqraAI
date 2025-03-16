"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.style = void 0;
const x6_common_1 = require("@antv/x6-common");
exports.style = {
    qualify: x6_common_1.ObjectExt.isPlainObject,
    set(styles, { elem }) {
        x6_common_1.Dom.css(elem, styles);
    },
};
//# sourceMappingURL=style.js.map