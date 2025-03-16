"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.rect = void 0;
const x6_common_1 = require("@antv/x6-common");
const bbox_1 = require("./bbox");
const util_1 = require("./util");
/**
 * Places the connection point at the intersection between the
 * link path end segment and the element's unrotated bbox.
 */
const rect = function (line, view, magnet, options, type) {
    const cell = view.cell;
    const angle = cell.isNode() ? cell.getAngle() : 0;
    if (angle === 0) {
        return x6_common_1.FunctionExt.call(bbox_1.bbox, this, line, view, magnet, options, type);
    }
    const bboxRaw = view.getUnrotatedBBoxOfElement(magnet);
    if (options.stroked) {
        bboxRaw.inflate((0, util_1.getStrokeWidth)(magnet) / 2);
    }
    const center = bboxRaw.getCenter();
    const lineRaw = line.clone().rotate(angle, center);
    const intersections = lineRaw.setLength(1e6).intersect(bboxRaw);
    const p = intersections && intersections.length
        ? lineRaw.start.closest(intersections).rotate(-angle, center)
        : line.end;
    return (0, util_1.offset)(p, line.start, options.offset);
};
exports.rect = rect;
//# sourceMappingURL=rect.js.map