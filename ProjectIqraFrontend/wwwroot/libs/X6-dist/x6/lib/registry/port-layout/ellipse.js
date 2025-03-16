"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ellipseSpread = exports.ellipse = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const util_1 = require("./util");
const ellipse = (portsPositionArgs, elemBBox, groupPositionArgs) => {
    const startAngle = groupPositionArgs.start || 0;
    const stepAngle = groupPositionArgs.step || 20;
    return ellipseLayout(portsPositionArgs, elemBBox, startAngle, (index, count) => (index + 0.5 - count / 2) * stepAngle);
};
exports.ellipse = ellipse;
const ellipseSpread = (portsPositionArgs, elemBBox, groupPositionArgs) => {
    const startAngle = groupPositionArgs.start || 0;
    const stepAngle = groupPositionArgs.step || 360 / portsPositionArgs.length;
    return ellipseLayout(portsPositionArgs, elemBBox, startAngle, (index) => {
        return index * stepAngle;
    });
};
exports.ellipseSpread = ellipseSpread;
function ellipseLayout(portsPositionArgs, elemBBox, startAngle, stepFn) {
    const center = elemBBox.getCenter();
    const start = elemBBox.getTopCenter();
    const ratio = elemBBox.width / elemBBox.height;
    const ellipse = x6_geometry_1.Ellipse.fromRect(elemBBox);
    const count = portsPositionArgs.length;
    return portsPositionArgs.map((item, index) => {
        const angle = startAngle + stepFn(index, count);
        const p = start.clone().rotate(-angle, center).scale(ratio, 1, center);
        const theta = item.compensateRotate ? -ellipse.tangentTheta(p) : 0;
        if (item.dx || item.dy) {
            p.translate(item.dx || 0, item.dy || 0);
        }
        if (item.dr) {
            p.move(center, item.dr);
        }
        return (0, util_1.toResult)(p.round(), theta, item);
    });
}
//# sourceMappingURL=ellipse.js.map