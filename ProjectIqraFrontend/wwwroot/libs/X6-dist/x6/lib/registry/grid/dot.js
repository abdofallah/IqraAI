"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.dot = void 0;
const x6_common_1 = require("@antv/x6-common");
exports.dot = {
    color: '#aaaaaa',
    thickness: 1,
    markup: 'rect',
    update(elem, options) {
        const width = options.thickness * options.sx;
        const height = options.thickness * options.sy;
        x6_common_1.Dom.attr(elem, {
            width,
            height,
            rx: width,
            ry: height,
            fill: options.color,
        });
    },
};
//# sourceMappingURL=dot.js.map