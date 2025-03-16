"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.MoveTo = void 0;
const line_1 = require("../line");
const curve_1 = require("../curve");
const point_1 = require("../point");
const lineto_1 = require("./lineto");
const segment_1 = require("./segment");
class MoveTo extends segment_1.Segment {
    constructor(x, y) {
        super();
        this.isVisible = false;
        this.isSubpathStart = true;
        if (line_1.Line.isLine(x) || curve_1.Curve.isCurve(x)) {
            this.endPoint = x.end.clone().round(2);
        }
        else {
            this.endPoint = point_1.Point.create(x, y).round(2);
        }
    }
    get start() {
        throw new Error('Illegal access. Moveto segments should not need a start property.');
    }
    get type() {
        return 'M';
    }
    bbox() {
        return null;
    }
    closestPoint() {
        return this.end.clone();
    }
    closestPointLength() {
        return 0;
    }
    closestPointNormalizedLength() {
        return 0;
    }
    closestPointT() {
        return 1;
    }
    closestPointTangent() {
        return null;
    }
    length() {
        return 0;
    }
    lengthAtT() {
        return 0;
    }
    divideAt() {
        return [this.clone(), this.clone()];
    }
    divideAtLength() {
        return [this.clone(), this.clone()];
    }
    getSubdivisions() {
        return [];
    }
    pointAt() {
        return this.end.clone();
    }
    pointAtLength() {
        return this.end.clone();
    }
    pointAtT() {
        return this.end.clone();
    }
    tangentAt() {
        return null;
    }
    tangentAtLength() {
        return null;
    }
    tangentAtT() {
        return null;
    }
    isDifferentiable() {
        return false;
    }
    scale(sx, sy, origin) {
        this.end.scale(sx, sy, origin);
        return this;
    }
    rotate(angle, origin) {
        this.end.rotate(angle, origin);
        return this;
    }
    translate(tx, ty) {
        if (typeof tx === 'number') {
            this.end.translate(tx, ty);
        }
        else {
            this.end.translate(tx);
        }
        return this;
    }
    clone() {
        return new MoveTo(this.end);
    }
    equals(s) {
        return this.type === s.type && this.end.equals(s.end);
    }
    toJSON() {
        return {
            type: this.type,
            end: this.end.toJSON(),
        };
    }
    serialize() {
        const end = this.end;
        return `${this.type} ${end.x} ${end.y}`;
    }
}
exports.MoveTo = MoveTo;
(function (MoveTo) {
    function create(...args) {
        const len = args.length;
        const arg0 = args[0];
        // line provided
        if (line_1.Line.isLine(arg0)) {
            return new MoveTo(arg0);
        }
        // curve provided
        if (curve_1.Curve.isCurve(arg0)) {
            return new MoveTo(arg0);
        }
        // points provided
        if (point_1.Point.isPointLike(arg0)) {
            if (len === 1) {
                return new MoveTo(arg0);
            }
            // this is a moveto-with-subsequent-poly-line segment
            const segments = [];
            // points come one by one
            for (let i = 0; i < len; i += 1) {
                if (i === 0) {
                    segments.push(new MoveTo(args[i]));
                }
                else {
                    segments.push(new lineto_1.LineTo(args[i]));
                }
            }
            return segments;
        }
        // coordinates provided
        if (len === 2) {
            return new MoveTo(+args[0], +args[1]);
        }
        // this is a moveto-with-subsequent-poly-line segment
        const segments = [];
        for (let i = 0; i < len; i += 2) {
            const x = +args[i];
            const y = +args[i + 1];
            if (i === 0) {
                segments.push(new MoveTo(x, y));
            }
            else {
                segments.push(new lineto_1.LineTo(x, y));
            }
        }
        return segments;
    }
    MoveTo.create = create;
})(MoveTo = exports.MoveTo || (exports.MoveTo = {}));
//# sourceMappingURL=moveto.js.map