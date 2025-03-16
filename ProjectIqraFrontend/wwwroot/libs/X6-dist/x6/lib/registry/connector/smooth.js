"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.smooth = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const smooth = function (sourcePoint, targetPoint, routePoints, options = {}) {
    let path;
    let direction = options.direction;
    if (routePoints && routePoints.length !== 0) {
        const points = [sourcePoint, ...routePoints, targetPoint];
        const curves = x6_geometry_1.Curve.throughPoints(points);
        path = new x6_geometry_1.Path(curves);
    }
    else {
        // If we have no route, use a default cubic bezier curve, cubic bezier
        // requires two control points, the control points have `x` midway
        // between source and target. This produces an S-like curve.
        path = new x6_geometry_1.Path();
        path.appendSegment(x6_geometry_1.Path.createSegment('M', sourcePoint));
        if (!direction) {
            direction =
                Math.abs(sourcePoint.x - targetPoint.x) >=
                    Math.abs(sourcePoint.y - targetPoint.y)
                    ? 'H'
                    : 'V';
        }
        if (direction === 'H') {
            const controlPointX = (sourcePoint.x + targetPoint.x) / 2;
            path.appendSegment(x6_geometry_1.Path.createSegment('C', controlPointX, sourcePoint.y, controlPointX, targetPoint.y, targetPoint.x, targetPoint.y));
        }
        else {
            const controlPointY = (sourcePoint.y + targetPoint.y) / 2;
            path.appendSegment(x6_geometry_1.Path.createSegment('C', sourcePoint.x, controlPointY, targetPoint.x, controlPointY, targetPoint.x, targetPoint.y));
        }
    }
    return options.raw ? path : path.serialize();
};
exports.smooth = smooth;
//# sourceMappingURL=smooth.js.map