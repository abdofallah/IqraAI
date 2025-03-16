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
exports.DefsManager = void 0;
const x6_common_1 = require("@antv/x6-common");
const registry_1 = require("../registry");
const view_1 = require("../view");
const base_1 = require("./base");
class DefsManager extends base_1.Base {
    get cid() {
        return this.graph.view.cid;
    }
    get svg() {
        return this.view.svg;
    }
    get defs() {
        return this.view.defs;
    }
    isDefined(id) {
        return this.svg.getElementById(id) != null;
    }
    filter(options) {
        let filterId = options.id;
        const name = options.name;
        if (!filterId) {
            filterId = `filter-${name}-${this.cid}-${x6_common_1.StringExt.hashcode(JSON.stringify(options))}`;
        }
        if (!this.isDefined(filterId)) {
            const fn = registry_1.Filter.registry.get(name);
            if (fn == null) {
                return registry_1.Filter.registry.onNotFound(name);
            }
            const markup = fn(options.args || {});
            // Set the filter area to be 3x the bounding box of the cell
            // and center the filter around the cell.
            const attrs = Object.assign(Object.assign({ x: -1, y: -1, width: 3, height: 3, filterUnits: 'objectBoundingBox' }, options.attrs), { id: filterId });
            x6_common_1.Vector.create(view_1.Markup.sanitize(markup), attrs).appendTo(this.defs);
        }
        return filterId;
    }
    gradient(options) {
        let id = options.id;
        const type = options.type;
        if (!id) {
            id = `gradient-${type}-${this.cid}-${x6_common_1.StringExt.hashcode(JSON.stringify(options))}`;
        }
        if (!this.isDefined(id)) {
            const stops = options.stops;
            const arr = stops.map((stop) => {
                const opacity = stop.opacity != null && Number.isFinite(stop.opacity)
                    ? stop.opacity
                    : 1;
                return `<stop offset="${stop.offset}" stop-color="${stop.color}" stop-opacity="${opacity}"/>`;
            });
            const markup = `<${type}>${arr.join('')}</${type}>`;
            const attrs = Object.assign({ id }, options.attrs);
            x6_common_1.Vector.create(markup, attrs).appendTo(this.defs);
        }
        return id;
    }
    marker(options) {
        const { id, refX, refY, markerUnits, markerOrient, tagName, children } = options, attrs = __rest(options, ["id", "refX", "refY", "markerUnits", "markerOrient", "tagName", "children"]);
        let markerId = id;
        if (!markerId) {
            markerId = `marker-${this.cid}-${x6_common_1.StringExt.hashcode(JSON.stringify(options))}`;
        }
        if (!this.isDefined(markerId)) {
            if (tagName !== 'path') {
                // remove unnecessary d attribute inherit from standard edge.
                delete attrs.d;
            }
            const pathMarker = x6_common_1.Vector.create('marker', {
                refX,
                refY,
                id: markerId,
                overflow: 'visible',
                orient: markerOrient != null ? markerOrient : 'auto',
                markerUnits: markerUnits || 'userSpaceOnUse',
            }, children
                ? children.map((_a) => {
                    var { tagName } = _a, other = __rest(_a, ["tagName"]);
                    return x6_common_1.Vector.create(`${tagName}` || 'path', x6_common_1.Dom.kebablizeAttrs(Object.assign(Object.assign({}, attrs), other)));
                })
                : [x6_common_1.Vector.create(tagName || 'path', x6_common_1.Dom.kebablizeAttrs(attrs))]);
            this.defs.appendChild(pathMarker.node);
        }
        return markerId;
    }
    remove(id) {
        const elem = this.svg.getElementById(id);
        if (elem && elem.parentNode) {
            elem.parentNode.removeChild(elem);
        }
    }
}
exports.DefsManager = DefsManager;
//# sourceMappingURL=defs.js.map