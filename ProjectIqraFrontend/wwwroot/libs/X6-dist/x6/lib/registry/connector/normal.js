"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.normal = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const normal = function (sourcePoint, targetPoint, routePoints, options = {}) {
    const points = [sourcePoint, ...routePoints, targetPoint];
    const polyline = new x6_geometry_1.Polyline(points);
    const path = new x6_geometry_1.Path(polyline);
    return options.raw ? path : path.serialize();
};
exports.normal = normal;
//# sourceMappingURL=normal.js.map