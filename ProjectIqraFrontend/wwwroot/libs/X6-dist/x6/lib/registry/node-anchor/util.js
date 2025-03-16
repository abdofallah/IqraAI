"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.getPointAtEdge = exports.resolve = void 0;
const x6_common_1 = require("@antv/x6-common");
const x6_geometry_1 = require("@antv/x6-geometry");
// eslint-disable-next-line
function resolve(fn) {
    return function (view, magnet, ref, options) {
        if (ref instanceof Element) {
            const refView = this.graph.findViewByElem(ref);
            let refPoint;
            if (refView) {
                if (refView.isEdgeElement(ref)) {
                    const distance = options.fixedAt != null ? options.fixedAt : '50%';
                    refPoint = getPointAtEdge(refView, distance);
                }
                else {
                    refPoint = refView.getBBoxOfElement(ref).getCenter();
                }
            }
            else {
                refPoint = new x6_geometry_1.Point();
            }
            return fn.call(this, view, magnet, refPoint, options);
        }
        return fn.apply(this, arguments); // eslint-disable-line
    };
}
exports.resolve = resolve;
function getPointAtEdge(edgeView, value) {
    const isPercentage = x6_common_1.NumberExt.isPercentage(value);
    const num = typeof value === 'string' ? parseFloat(value) : value;
    if (isPercentage) {
        return edgeView.getPointAtRatio(num / 100);
    }
    return edgeView.getPointAtLength(num);
}
exports.getPointAtEdge = getPointAtEdge;
//# sourceMappingURL=util.js.map