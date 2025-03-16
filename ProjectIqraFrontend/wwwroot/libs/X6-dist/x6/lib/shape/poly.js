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
exports.Poly = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const x6_common_1 = require("@antv/x6-common");
const base_1 = require("./base");
class Poly extends base_1.Base {
    get points() {
        return this.getPoints();
    }
    set points(pts) {
        this.setPoints(pts);
    }
    getPoints() {
        return this.getAttrByPath('body/refPoints');
    }
    setPoints(points, options) {
        if (points == null) {
            this.removePoints();
        }
        else {
            this.setAttrByPath('body/refPoints', Poly.pointsToString(points), options);
        }
        return this;
    }
    removePoints() {
        this.removeAttrByPath('body/refPoints');
        return this;
    }
}
exports.Poly = Poly;
(function (Poly) {
    function pointsToString(points) {
        return typeof points === 'string'
            ? points
            : points
                .map((p) => {
                if (Array.isArray(p)) {
                    return p.join(',');
                }
                if (x6_geometry_1.Point.isPointLike(p)) {
                    return `${p.x}, ${p.y}`;
                }
                return '';
            })
                .join(' ');
    }
    Poly.pointsToString = pointsToString;
    Poly.config({
        propHooks(metadata) {
            const { points } = metadata, others = __rest(metadata, ["points"]);
            if (points) {
                const data = pointsToString(points);
                if (data) {
                    x6_common_1.ObjectExt.setByPath(others, 'attrs/body/refPoints', data);
                }
            }
            return others;
        },
    });
})(Poly = exports.Poly || (exports.Poly = {}));
//# sourceMappingURL=poly.js.map