"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CurveTo = void 0;
const curve_1 = require("../curve");
const point_1 = require("../point");
const segment_1 = require("./segment");
class CurveTo extends segment_1.Segment {
    constructor(arg0, arg1, arg2, arg3, arg4, arg5) {
        super();
        if (curve_1.Curve.isCurve(arg0)) {
            this.controlPoint1 = arg0.controlPoint1.clone().round(2);
            this.controlPoint2 = arg0.controlPoint2.clone().round(2);
            this.endPoint = arg0.end.clone().round(2);
        }
        else if (typeof arg0 === 'number') {
            this.controlPoint1 = new point_1.Point(arg0, arg1).round(2);
            this.controlPoint2 = new point_1.Point(arg2, arg3).round(2);
            this.endPoint = new point_1.Point(arg4, arg5).round(2);
        }
        else {
            this.controlPoint1 = point_1.Point.create(arg0).round(2);
            this.controlPoint2 = point_1.Point.create(arg1).round(2);
            this.endPoint = point_1.Point.create(arg2).round(2);
        }
    }
    get type() {
        return 'C';
    }
    get curve() {
        return new curve_1.Curve(this.start, this.controlPoint1, this.controlPoint2, this.end);
    }
    bbox() {
        return this.curve.bbox();
    }
    closestPoint(p) {
        return this.curve.closestPoint(p);
    }
    closestPointLength(p) {
        return this.curve.closestPointLength(p);
    }
    closestPointNormalizedLength(p) {
        return this.curve.closestPointNormalizedLength(p);
    }
    closestPointTangent(p) {
        return this.curve.closestPointTangent(p);
    }
    length() {
        return this.curve.length();
    }
    divideAt(ratio, options = {}) {
        // TODO: fix options
        const divided = this.curve.divideAt(ratio, options);
        return [new CurveTo(divided[0]), new CurveTo(divided[1])];
    }
    divideAtLength(length, options = {}) {
        // TODO: fix options
        const divided = this.curve.divideAtLength(length, options);
        return [new CurveTo(divided[0]), new CurveTo(divided[1])];
    }
    divideAtT(t) {
        const divided = this.curve.divideAtT(t);
        return [new CurveTo(divided[0]), new CurveTo(divided[1])];
    }
    getSubdivisions() {
        return [];
    }
    pointAt(ratio) {
        return this.curve.pointAt(ratio);
    }
    pointAtLength(length) {
        return this.curve.pointAtLength(length);
    }
    tangentAt(ratio) {
        return this.curve.tangentAt(ratio);
    }
    tangentAtLength(length) {
        return this.curve.tangentAtLength(length);
    }
    isDifferentiable() {
        if (!this.previousSegment) {
            return false;
        }
        const start = this.start;
        const control1 = this.controlPoint1;
        const control2 = this.controlPoint2;
        const end = this.end;
        return !(start.equals(control1) &&
            control1.equals(control2) &&
            control2.equals(end));
    }
    scale(sx, sy, origin) {
        this.controlPoint1.scale(sx, sy, origin);
        this.controlPoint2.scale(sx, sy, origin);
        this.end.scale(sx, sy, origin);
        return this;
    }
    rotate(angle, origin) {
        this.controlPoint1.rotate(angle, origin);
        this.controlPoint2.rotate(angle, origin);
        this.end.rotate(angle, origin);
        return this;
    }
    translate(tx, ty) {
        if (typeof tx === 'number') {
            this.controlPoint1.translate(tx, ty);
            this.controlPoint2.translate(tx, ty);
            this.end.translate(tx, ty);
        }
        else {
            this.controlPoint1.translate(tx);
            this.controlPoint2.translate(tx);
            this.end.translate(tx);
        }
        return this;
    }
    equals(s) {
        return (this.start.equals(s.start) &&
            this.end.equals(s.end) &&
            this.controlPoint1.equals(s.controlPoint1) &&
            this.controlPoint2.equals(s.controlPoint2));
    }
    clone() {
        return new CurveTo(this.controlPoint1, this.controlPoint2, this.end);
    }
    toJSON() {
        return {
            type: this.type,
            start: this.start.toJSON(),
            controlPoint1: this.controlPoint1.toJSON(),
            controlPoint2: this.controlPoint2.toJSON(),
            end: this.end.toJSON(),
        };
    }
    serialize() {
        const c1 = this.controlPoint1;
        const c2 = this.controlPoint2;
        const end = this.end;
        return [this.type, c1.x, c1.y, c2.x, c2.y, end.x, end.y].join(' ');
    }
}
exports.CurveTo = CurveTo;
(function (CurveTo) {
    function create(...args) {
        const len = args.length;
        const arg0 = args[0];
        // curve provided
        if (curve_1.Curve.isCurve(arg0)) {
            return new CurveTo(arg0);
        }
        // points provided
        if (point_1.Point.isPointLike(arg0)) {
            if (len === 3) {
                return new CurveTo(args[0], args[1], args[2]);
            }
            // this is a poly-bezier segment
            const segments = [];
            for (let i = 0; i < len; i += 3) {
                segments.push(new CurveTo(args[i], args[i + 1], args[i + 2]));
            }
            return segments;
        }
        // coordinates provided
        if (len === 6) {
            return new CurveTo(args[0], args[1], args[2], args[3], args[4], args[5]);
        }
        // this is a poly-bezier segment
        const segments = [];
        for (let i = 0; i < len; i += 6) {
            segments.push(new CurveTo(args[i], args[i + 1], args[i + 2], args[i + 3], args[i + 4], args[i + 5]));
        }
        return segments;
    }
    CurveTo.create = create;
})(CurveTo = exports.CurveTo || (exports.CurveTo = {}));
//# sourceMappingURL=curveto.js.map