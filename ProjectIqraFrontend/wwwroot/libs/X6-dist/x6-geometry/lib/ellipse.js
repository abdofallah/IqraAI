"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Ellipse = void 0;
const point_1 = require("./point");
const rectangle_1 = require("./rectangle");
const geometry_1 = require("./geometry");
class Ellipse extends geometry_1.Geometry {
    get center() {
        return new point_1.Point(this.x, this.y);
    }
    constructor(x, y, a, b) {
        super();
        this.x = x == null ? 0 : x;
        this.y = y == null ? 0 : y;
        this.a = a == null ? 0 : a;
        this.b = b == null ? 0 : b;
    }
    /**
     * Returns a rectangle that is the bounding box of the ellipse.
     */
    bbox() {
        return rectangle_1.Rectangle.fromEllipse(this);
    }
    /**
     * Returns a point that is the center of the ellipse.
     */
    getCenter() {
        return this.center;
    }
    inflate(dx, dy) {
        const w = dx;
        const h = dy != null ? dy : dx;
        this.a += 2 * w;
        this.b += 2 * h;
        return this;
    }
    normalizedDistance(x, y) {
        const ref = point_1.Point.create(x, y);
        const dx = ref.x - this.x;
        const dy = ref.y - this.y;
        const a = this.a;
        const b = this.b;
        return (dx * dx) / (a * a) + (dy * dy) / (b * b);
    }
    containsPoint(x, y) {
        return this.normalizedDistance(x, y) <= 1;
    }
    /**
     * Returns an array of the intersection points of the ellipse and the line.
     * Returns `null` if no intersection exists.
     */
    intersectsWithLine(line) {
        const intersections = [];
        const rx = this.a;
        const ry = this.b;
        const a1 = line.start;
        const a2 = line.end;
        const dir = line.vector();
        const diff = a1.diff(new point_1.Point(this.x, this.y));
        const mDir = new point_1.Point(dir.x / (rx * rx), dir.y / (ry * ry));
        const mDiff = new point_1.Point(diff.x / (rx * rx), diff.y / (ry * ry));
        const a = dir.dot(mDir);
        const b = dir.dot(mDiff);
        const c = diff.dot(mDiff) - 1.0;
        const d = b * b - a * c;
        if (d < 0) {
            return null;
        }
        if (d > 0) {
            const root = Math.sqrt(d);
            const ta = (-b - root) / a;
            const tb = (-b + root) / a;
            if ((ta < 0 || ta > 1) && (tb < 0 || tb > 1)) {
                // outside
                return null;
            }
            if (ta >= 0 && ta <= 1) {
                intersections.push(a1.lerp(a2, ta));
            }
            if (tb >= 0 && tb <= 1) {
                intersections.push(a1.lerp(a2, tb));
            }
        }
        else {
            const t = -b / a;
            if (t >= 0 && t <= 1) {
                intersections.push(a1.lerp(a2, t));
            }
            else {
                // outside
                return null;
            }
        }
        return intersections;
    }
    /**
     * Returns the point on the boundary of the ellipse that is the
     * intersection of the ellipse with a line starting in the center
     * of the ellipse ending in the point `p`.
     *
     * If angle is specified, the intersection will take into account
     * the rotation of the ellipse by angle degrees around its center.
     */
    intersectsWithLineFromCenterToPoint(p, angle = 0) {
        const ref = point_1.Point.clone(p);
        if (angle) {
            ref.rotate(angle, this.getCenter());
        }
        const dx = ref.x - this.x;
        const dy = ref.y - this.y;
        let result;
        if (dx === 0) {
            result = this.bbox().getNearestPointToPoint(ref);
            if (angle) {
                return result.rotate(-angle, this.getCenter());
            }
            return result;
        }
        const m = dy / dx;
        const mSquared = m * m;
        const aSquared = this.a * this.a;
        const bSquared = this.b * this.b;
        let x = Math.sqrt(1 / (1 / aSquared + mSquared / bSquared));
        x = dx < 0 ? -x : x;
        const y = m * x;
        result = new point_1.Point(this.x + x, this.y + y);
        if (angle) {
            return result.rotate(-angle, this.getCenter());
        }
        return result;
    }
    /**
     * Returns the angle between the x-axis and the tangent from a point. It is
     * valid for points lying on the ellipse boundary only.
     */
    tangentTheta(p) {
        const ref = point_1.Point.clone(p);
        const x0 = ref.x;
        const y0 = ref.y;
        const a = this.a;
        const b = this.b;
        const center = this.bbox().center;
        const cx = center.x;
        const cy = center.y;
        const refPointDelta = 30;
        const q1 = x0 > center.x + a / 2;
        const q3 = x0 < center.x - a / 2;
        let x;
        let y;
        if (q1 || q3) {
            y = x0 > center.x ? y0 - refPointDelta : y0 + refPointDelta;
            x =
                (a * a) / (x0 - cx) -
                    (a * a * (y0 - cy) * (y - cy)) / (b * b * (x0 - cx)) +
                    cx;
        }
        else {
            x = y0 > center.y ? x0 + refPointDelta : x0 - refPointDelta;
            y =
                (b * b) / (y0 - cy) -
                    (b * b * (x0 - cx) * (x - cx)) / (a * a * (y0 - cy)) +
                    cy;
        }
        return new point_1.Point(x, y).theta(ref);
    }
    scale(sx, sy) {
        this.a *= sx;
        this.b *= sy;
        return this;
    }
    rotate(angle, origin) {
        const rect = rectangle_1.Rectangle.fromEllipse(this);
        rect.rotate(angle, origin);
        const ellipse = Ellipse.fromRect(rect);
        this.a = ellipse.a;
        this.b = ellipse.b;
        this.x = ellipse.x;
        this.y = ellipse.y;
        return this;
    }
    translate(dx, dy) {
        const p = point_1.Point.create(dx, dy);
        this.x += p.x;
        this.y += p.y;
        return this;
    }
    equals(ellipse) {
        return (ellipse != null &&
            ellipse.x === this.x &&
            ellipse.y === this.y &&
            ellipse.a === this.a &&
            ellipse.b === this.b);
    }
    clone() {
        return new Ellipse(this.x, this.y, this.a, this.b);
    }
    toJSON() {
        return { x: this.x, y: this.y, a: this.a, b: this.b };
    }
    serialize() {
        return `${this.x} ${this.y} ${this.a} ${this.b}`;
    }
}
exports.Ellipse = Ellipse;
(function (Ellipse) {
    function isEllipse(instance) {
        return instance != null && instance instanceof Ellipse;
    }
    Ellipse.isEllipse = isEllipse;
})(Ellipse = exports.Ellipse || (exports.Ellipse = {}));
(function (Ellipse) {
    function create(x, y, a, b) {
        if (x == null || typeof x === 'number') {
            return new Ellipse(x, y, a, b);
        }
        return parse(x);
    }
    Ellipse.create = create;
    function parse(e) {
        if (Ellipse.isEllipse(e)) {
            return e.clone();
        }
        if (Array.isArray(e)) {
            return new Ellipse(e[0], e[1], e[2], e[3]);
        }
        return new Ellipse(e.x, e.y, e.a, e.b);
    }
    Ellipse.parse = parse;
    function fromRect(rect) {
        const center = rect.center;
        return new Ellipse(center.x, center.y, rect.width / 2, rect.height / 2);
    }
    Ellipse.fromRect = fromRect;
})(Ellipse = exports.Ellipse || (exports.Ellipse = {}));
//# sourceMappingURL=ellipse.js.map