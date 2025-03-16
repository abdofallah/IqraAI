"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.loop = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const loop = function (sourcePoint, targetPoint, routePoints, options = {}) {
    const fix = routePoints.length === 3 ? 0 : 1;
    const p1 = x6_geometry_1.Point.create(routePoints[0 + fix]);
    const p2 = x6_geometry_1.Point.create(routePoints[2 + fix]);
    const center = x6_geometry_1.Point.create(routePoints[1 + fix]);
    if (!x6_geometry_1.Point.equals(sourcePoint, targetPoint)) {
        const middle = new x6_geometry_1.Point((sourcePoint.x + targetPoint.x) / 2, (sourcePoint.y + targetPoint.y) / 2);
        const angle = middle.angleBetween(x6_geometry_1.Point.create(sourcePoint).rotate(90, middle), center);
        if (angle > 1) {
            p1.rotate(180 - angle, middle);
            p2.rotate(180 - angle, middle);
            center.rotate(180 - angle, middle);
        }
    }
    const pathData = `
     M ${sourcePoint.x} ${sourcePoint.y}
     Q ${p1.x} ${p1.y} ${center.x} ${center.y}
     Q ${p2.x} ${p2.y} ${targetPoint.x} ${targetPoint.y}
  `;
    return options.raw ? x6_geometry_1.Path.parse(pathData) : pathData;
};
exports.loop = loop;
//# sourceMappingURL=loop.js.map