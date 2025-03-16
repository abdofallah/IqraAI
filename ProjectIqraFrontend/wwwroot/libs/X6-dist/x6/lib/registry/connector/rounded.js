"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.rounded = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const rounded = function (sourcePoint, targetPoint, routePoints, options = {}) {
    const path = new x6_geometry_1.Path();
    path.appendSegment(x6_geometry_1.Path.createSegment('M', sourcePoint));
    const f13 = 1 / 3;
    const f23 = 2 / 3;
    const radius = options.radius || 10;
    let prevDistance;
    let nextDistance;
    for (let i = 0, ii = routePoints.length; i < ii; i += 1) {
        const curr = x6_geometry_1.Point.create(routePoints[i]);
        const prev = routePoints[i - 1] || sourcePoint;
        const next = routePoints[i + 1] || targetPoint;
        prevDistance = nextDistance || curr.distance(prev) / 2;
        nextDistance = curr.distance(next) / 2;
        const startMove = -Math.min(radius, prevDistance);
        const endMove = -Math.min(radius, nextDistance);
        const roundedStart = curr.clone().move(prev, startMove).round();
        const roundedEnd = curr.clone().move(next, endMove).round();
        const control1 = new x6_geometry_1.Point(f13 * roundedStart.x + f23 * curr.x, f23 * curr.y + f13 * roundedStart.y);
        const control2 = new x6_geometry_1.Point(f13 * roundedEnd.x + f23 * curr.x, f23 * curr.y + f13 * roundedEnd.y);
        path.appendSegment(x6_geometry_1.Path.createSegment('L', roundedStart));
        path.appendSegment(x6_geometry_1.Path.createSegment('C', control1, control2, roundedEnd));
    }
    path.appendSegment(x6_geometry_1.Path.createSegment('L', targetPoint));
    return options.raw ? path : path.serialize();
};
exports.rounded = rounded;
//# sourceMappingURL=rounded.js.map