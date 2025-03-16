"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.toResult = exports.normalizePoint = void 0;
const x6_common_1 = require("@antv/x6-common");
const x6_geometry_1 = require("@antv/x6-geometry");
function normalizePoint(bbox, args = {}) {
    return new x6_geometry_1.Point(x6_common_1.NumberExt.normalizePercentage(args.x, bbox.width), x6_common_1.NumberExt.normalizePercentage(args.y, bbox.height));
}
exports.normalizePoint = normalizePoint;
function toResult(point, angle, rawArgs) {
    return Object.assign({ angle, position: point.toJSON() }, rawArgs);
}
exports.toResult = toResult;
//# sourceMappingURL=util.js.map