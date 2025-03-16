"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Close = void 0;
const line_1 = require("../line");
const lineto_1 = require("./lineto");
const segment_1 = require("./segment");
class Close extends segment_1.Segment {
    get end() {
        if (!this.subpathStartSegment) {
            throw new Error('Missing subpath start segment. (This segment needs a subpath ' +
                'start segment (e.g. MoveTo), or segment has not yet been added' +
                ' to a path.)');
        }
        return this.subpathStartSegment.end;
    }
    get type() {
        return 'Z';
    }
    get line() {
        return new line_1.Line(this.start, this.end);
    }
    bbox() {
        return this.line.bbox();
    }
    closestPoint(p) {
        return this.line.closestPoint(p);
    }
    closestPointLength(p) {
        return this.line.closestPointLength(p);
    }
    closestPointNormalizedLength(p) {
        return this.line.closestPointNormalizedLength(p);
    }
    closestPointTangent(p) {
        return this.line.closestPointTangent(p);
    }
    length() {
        return this.line.length();
    }
    divideAt(ratio) {
        const divided = this.line.divideAt(ratio);
        return [
            // do not actually cut into the segment, first divided part can stay as Z
            divided[1].isDifferentiable() ? new lineto_1.LineTo(divided[0]) : this.clone(),
            new lineto_1.LineTo(divided[1]),
        ];
    }
    divideAtLength(length) {
        const divided = this.line.divideAtLength(length);
        return [
            divided[1].isDifferentiable() ? new lineto_1.LineTo(divided[0]) : this.clone(),
            new lineto_1.LineTo(divided[1]),
        ];
    }
    getSubdivisions() {
        return [];
    }
    pointAt(ratio) {
        return this.line.pointAt(ratio);
    }
    pointAtLength(length) {
        return this.line.pointAtLength(length);
    }
    tangentAt(ratio) {
        return this.line.tangentAt(ratio);
    }
    tangentAtLength(length) {
        return this.line.tangentAtLength(length);
    }
    isDifferentiable() {
        if (!this.previousSegment || !this.subpathStartSegment) {
            return false;
        }
        return !this.start.equals(this.end);
    }
    scale() {
        return this;
    }
    rotate() {
        return this;
    }
    translate() {
        return this;
    }
    equals(s) {
        return (this.type === s.type &&
            this.start.equals(s.start) &&
            this.end.equals(s.end));
    }
    clone() {
        return new Close();
    }
    toJSON() {
        return {
            type: this.type,
            start: this.start.toJSON(),
            end: this.end.toJSON(),
        };
    }
    serialize() {
        return this.type;
    }
}
exports.Close = Close;
(function (Close) {
    function create() {
        return new Close();
    }
    Close.create = create;
})(Close = exports.Close || (exports.Close = {}));
//# sourceMappingURL=close.js.map