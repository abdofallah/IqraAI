"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CoordManager = void 0;
const x6_common_1 = require("@antv/x6-common");
const x6_geometry_1 = require("@antv/x6-geometry");
const base_1 = require("./base");
const util_1 = require("../util");
class CoordManager extends base_1.Base {
    getClientMatrix() {
        return x6_common_1.Dom.createSVGMatrix(this.view.stage.getScreenCTM());
    }
    /**
     * Returns coordinates of the graph viewport, relative to the window.
     */
    getClientOffset() {
        // see: https://developer.mozilla.org/en-US/docs/Web/API/Element/getBoundingClientRect
        const rect = this.view.svg.getBoundingClientRect();
        return new x6_geometry_1.Point(rect.left, rect.top);
    }
    /**
     * Returns coordinates of the graph viewport, relative to the document.
     */
    getPageOffset() {
        // see: https://developer.mozilla.org/en-US/docs/Web/API/Element/getBoundingClientRect
        return this.getClientOffset().translate(window.scrollX, window.scrollY);
    }
    snapToGrid(x, y) {
        const p = typeof x === 'number'
            ? this.clientToLocalPoint(x, y)
            : this.clientToLocalPoint(x.x, x.y);
        return p.snapToGrid(this.graph.getGridSize());
    }
    localToGraphPoint(x, y) {
        const localPoint = x6_geometry_1.Point.create(x, y);
        return util_1.Util.transformPoint(localPoint, this.graph.matrix());
    }
    localToClientPoint(x, y) {
        const localPoint = x6_geometry_1.Point.create(x, y);
        return util_1.Util.transformPoint(localPoint, this.getClientMatrix());
    }
    localToPagePoint(x, y) {
        const p = typeof x === 'number'
            ? this.localToGraphPoint(x, y)
            : this.localToGraphPoint(x);
        return p.translate(this.getPageOffset());
    }
    localToGraphRect(x, y, width, height) {
        const localRect = x6_geometry_1.Rectangle.create(x, y, width, height);
        return util_1.Util.transformRectangle(localRect, this.graph.matrix());
    }
    localToClientRect(x, y, width, height) {
        const localRect = x6_geometry_1.Rectangle.create(x, y, width, height);
        return util_1.Util.transformRectangle(localRect, this.getClientMatrix());
    }
    localToPageRect(x, y, width, height) {
        const rect = typeof x === 'number'
            ? this.localToGraphRect(x, y, width, height)
            : this.localToGraphRect(x);
        return rect.translate(this.getPageOffset());
    }
    graphToLocalPoint(x, y) {
        const graphPoint = x6_geometry_1.Point.create(x, y);
        return util_1.Util.transformPoint(graphPoint, this.graph.matrix().inverse());
    }
    clientToLocalPoint(x, y) {
        const clientPoint = x6_geometry_1.Point.create(x, y);
        return util_1.Util.transformPoint(clientPoint, this.getClientMatrix().inverse());
    }
    clientToGraphPoint(x, y) {
        const clientPoint = x6_geometry_1.Point.create(x, y);
        return util_1.Util.transformPoint(clientPoint, this.graph.matrix().multiply(this.getClientMatrix().inverse()));
    }
    pageToLocalPoint(x, y) {
        const pagePoint = x6_geometry_1.Point.create(x, y);
        const graphPoint = pagePoint.diff(this.getPageOffset());
        return this.graphToLocalPoint(graphPoint);
    }
    graphToLocalRect(x, y, width, height) {
        const graphRect = x6_geometry_1.Rectangle.create(x, y, width, height);
        return util_1.Util.transformRectangle(graphRect, this.graph.matrix().inverse());
    }
    clientToLocalRect(x, y, width, height) {
        const clientRect = x6_geometry_1.Rectangle.create(x, y, width, height);
        return util_1.Util.transformRectangle(clientRect, this.getClientMatrix().inverse());
    }
    clientToGraphRect(x, y, width, height) {
        const clientRect = x6_geometry_1.Rectangle.create(x, y, width, height);
        return util_1.Util.transformRectangle(clientRect, this.graph.matrix().multiply(this.getClientMatrix().inverse()));
    }
    pageToLocalRect(x, y, width, height) {
        const graphRect = x6_geometry_1.Rectangle.create(x, y, width, height);
        const pageOffset = this.getPageOffset();
        graphRect.x -= pageOffset.x;
        graphRect.y -= pageOffset.y;
        return this.graphToLocalRect(graphRect);
    }
}
exports.CoordManager = CoordManager;
//# sourceMappingURL=coord.js.map