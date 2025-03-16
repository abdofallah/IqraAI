"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.orth = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const x6_common_1 = require("@antv/x6-common");
const util_1 = require("../node-anchor/util");
const closest_1 = require("./closest");
const orthogonal = function (view, magnet, refPoint, options) {
    const OFFSET = 1e6;
    const path = view.getConnection();
    const segmentSubdivisions = view.getConnectionSubdivisions();
    const vLine = new x6_geometry_1.Line(refPoint.clone().translate(0, OFFSET), refPoint.clone().translate(0, -OFFSET));
    const hLine = new x6_geometry_1.Line(refPoint.clone().translate(OFFSET, 0), refPoint.clone().translate(-OFFSET, 0));
    const vIntersections = vLine.intersect(path, {
        segmentSubdivisions,
    });
    const hIntersections = hLine.intersect(path, {
        segmentSubdivisions,
    });
    const intersections = [];
    if (vIntersections) {
        intersections.push(...vIntersections);
    }
    if (hIntersections) {
        intersections.push(...hIntersections);
    }
    if (intersections.length > 0) {
        return refPoint.closest(intersections);
    }
    if (options.fallbackAt != null) {
        return (0, util_1.getPointAtEdge)(view, options.fallbackAt);
    }
    return x6_common_1.FunctionExt.call(closest_1.getClosestPoint, this, view, magnet, refPoint, options);
};
exports.orth = (0, util_1.resolve)(orthogonal);
//# sourceMappingURL=orth.js.map