import { NumberExt } from '@antv/x6-common';
import { Point } from '@antv/x6-geometry';
export function normalizePoint(bbox, args = {}) {
    return new Point(NumberExt.normalizePercentage(args.x, bbox.width), NumberExt.normalizePercentage(args.y, bbox.height));
}
export function toResult(point, angle, rawArgs) {
    return Object.assign({ angle, position: point.toJSON() }, rawArgs);
}
//# sourceMappingURL=util.js.map