"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.toResult = void 0;
const x6_common_1 = require("@antv/x6-common");
const defaults = {
    position: { x: 0, y: 0 },
    angle: 0,
    attrs: {
        '.': {
            y: '0',
            'text-anchor': 'start',
        },
    },
};
function toResult(preset, args) {
    const { x, y, angle, attrs } = args || {};
    return x6_common_1.ObjectExt.defaultsDeep({}, { angle, attrs, position: { x, y } }, preset, defaults);
}
exports.toResult = toResult;
//# sourceMappingURL=util.js.map