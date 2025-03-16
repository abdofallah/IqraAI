"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.absolute = void 0;
const util_1 = require("./util");
const absolute = (portsPositionArgs, elemBBox) => {
    return portsPositionArgs.map(({ x, y, angle }) => (0, util_1.toResult)((0, util_1.normalizePoint)(elemBBox, { x, y }), angle || 0));
};
exports.absolute = absolute;
//# sourceMappingURL=absolute.js.map