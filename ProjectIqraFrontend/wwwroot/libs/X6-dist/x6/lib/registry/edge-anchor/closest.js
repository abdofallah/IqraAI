"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.closest = exports.getClosestPoint = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const util_1 = require("../node-anchor/util");
const getClosestPoint = function (view, magnet, refPoint, options) {
    const closestPoint = view.getClosestPoint(refPoint);
    return closestPoint != null ? closestPoint : new x6_geometry_1.Point();
};
exports.getClosestPoint = getClosestPoint;
exports.closest = (0, util_1.resolve)(exports.getClosestPoint);
//# sourceMappingURL=closest.js.map