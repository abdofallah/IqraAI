"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Cache = void 0;
const x6_common_1 = require("@antv/x6-common");
const util_1 = require("../util");
class Cache {
    constructor(view) {
        this.view = view;
        this.clean();
    }
    clean() {
        if (this.elemCache) {
            this.elemCache.dispose();
        }
        this.elemCache = new x6_common_1.Dictionary();
        this.pathCache = {};
    }
    get(elem) {
        const cache = this.elemCache;
        if (!cache.has(elem)) {
            this.elemCache.set(elem, {});
        }
        return this.elemCache.get(elem);
    }
    getData(elem) {
        const meta = this.get(elem);
        if (!meta.data) {
            meta.data = {};
        }
        return meta.data;
    }
    getMatrix(elem) {
        const meta = this.get(elem);
        if (meta.matrix == null) {
            const target = this.view.container;
            meta.matrix = x6_common_1.Dom.getTransformToParentElement(elem, target);
        }
        return x6_common_1.Dom.createSVGMatrix(meta.matrix);
    }
    getShape(elem) {
        const meta = this.get(elem);
        if (meta.shape == null) {
            meta.shape = util_1.Util.toGeometryShape(elem);
        }
        return meta.shape.clone();
    }
    getBoundingRect(elem) {
        const meta = this.get(elem);
        if (meta.boundingRect == null) {
            meta.boundingRect = util_1.Util.getBBoxV2(elem);
        }
        return meta.boundingRect.clone();
    }
}
exports.Cache = Cache;
//# sourceMappingURL=cache.js.map