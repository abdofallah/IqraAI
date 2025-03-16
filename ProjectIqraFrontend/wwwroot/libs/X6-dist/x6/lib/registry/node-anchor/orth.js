"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.orth = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const util_1 = require("./util");
const orthogonal = function (view, magnet, refPoint, options) {
    const angle = x6_geometry_1.Angle.normalize(view.cell.getAngle());
    const bbox = view.cell.visible
        ? view.getBBoxOfElement(magnet)
        : view.cell.getBBox();
    const result = bbox.getCenter();
    const topLeft = bbox.getTopLeft();
    const bottomRight = bbox.getBottomRight();
    let padding = options.padding;
    if (!Number.isFinite(padding)) {
        padding = 0;
    }
    if (topLeft.y + padding <= refPoint.y &&
        refPoint.y <= bottomRight.y - padding) {
        const dy = refPoint.y - result.y;
        result.x +=
            angle === 0 || angle === 180
                ? 0
                : (dy * 1) / Math.tan(x6_geometry_1.Angle.toRad(angle));
        result.y += dy;
    }
    else if (topLeft.x + padding <= refPoint.x &&
        refPoint.x <= bottomRight.x - padding) {
        const dx = refPoint.x - result.x;
        result.y +=
            angle === 90 || angle === 270 ? 0 : dx * Math.tan(x6_geometry_1.Angle.toRad(angle));
        result.x += dx;
    }
    return result;
};
/**
 * Tries to place the anchor of the edge inside the view bbox so that the
 * edge is made orthogonal. The anchor is placed along two line segments
 * inside the view bbox (between the centers of the top and bottom side and
 * between the centers of the left and right sides). If it is not possible
 * to place the anchor so that the edge would be orthogonal, the anchor is
 * placed at the center of the view bbox instead.
 */
exports.orth = (0, util_1.resolve)(orthogonal);
//# sourceMappingURL=orth.js.map