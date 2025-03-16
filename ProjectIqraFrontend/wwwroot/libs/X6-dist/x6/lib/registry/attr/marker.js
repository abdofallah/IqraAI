"use strict";
var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                t[p[i]] = s[p[i]];
        }
    return t;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.vertexMarker = exports.targetMarker = exports.sourceMarker = void 0;
const x6_common_1 = require("@antv/x6-common");
const marker_1 = require("../marker");
function qualify(value) {
    return typeof value === 'string' || x6_common_1.ObjectExt.isPlainObject(value);
}
exports.sourceMarker = {
    qualify,
    set(marker, { view, attrs }) {
        return createMarker('marker-start', marker, view, attrs);
    },
};
exports.targetMarker = {
    qualify,
    set(marker, { view, attrs }) {
        return createMarker('marker-end', marker, view, attrs, {
            transform: 'rotate(180)',
        });
    },
};
exports.vertexMarker = {
    qualify,
    set(marker, { view, attrs }) {
        return createMarker('marker-mid', marker, view, attrs);
    },
};
function createMarker(type, marker, view, attrs, manual = {}) {
    const def = typeof marker === 'string' ? { name: marker } : marker;
    const { name, args } = def, others = __rest(def, ["name", "args"]);
    let preset = others;
    if (name && typeof name === 'string') {
        const fn = marker_1.Marker.registry.get(name);
        if (fn) {
            preset = fn(Object.assign(Object.assign({}, others), args));
        }
        else {
            return marker_1.Marker.registry.onNotFound(name);
        }
    }
    const options = Object.assign(Object.assign(Object.assign({}, normalizeAttr(attrs, type)), manual), preset);
    return {
        [type]: `url(#${view.graph.defineMarker(options)})`,
    };
}
function normalizeAttr(attr, type) {
    const result = {};
    // The context 'fill' is disregared here. The usual case is to use the
    // marker with a connection(for which 'fill' attribute is set to 'none').
    const stroke = attr.stroke;
    if (typeof stroke === 'string') {
        result.stroke = stroke;
        result.fill = stroke;
    }
    // Again the context 'fill-opacity' is ignored.
    let strokeOpacity = attr.strokeOpacity;
    if (strokeOpacity == null) {
        strokeOpacity = attr['stroke-opacity'];
    }
    if (strokeOpacity == null) {
        strokeOpacity = attr.opacity;
    }
    if (strokeOpacity != null) {
        result['stroke-opacity'] = strokeOpacity;
        result['fill-opacity'] = strokeOpacity;
    }
    if (type !== 'marker-mid') {
        const strokeWidth = parseFloat((attr.strokeWidth || attr['stroke-width']));
        if (Number.isFinite(strokeWidth) && strokeWidth > 1) {
            const offset = Math.ceil(strokeWidth / 2);
            result.refX = type === 'marker-start' ? offset : -offset;
        }
    }
    return result;
}
//# sourceMappingURL=marker.js.map