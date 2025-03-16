"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.insideOriented = exports.inside = exports.outsideOriented = exports.outside = void 0;
const util_1 = require("./util");
const outside = (portPosition, elemBBox, args) => outsideLayout(portPosition, elemBBox, false, args);
exports.outside = outside;
const outsideOriented = (portPosition, elemBBox, args) => outsideLayout(portPosition, elemBBox, true, args);
exports.outsideOriented = outsideOriented;
const inside = (portPosition, elemBBox, args) => insideLayout(portPosition, elemBBox, false, args);
exports.inside = inside;
const insideOriented = (portPosition, elemBBox, args) => insideLayout(portPosition, elemBBox, true, args);
exports.insideOriented = insideOriented;
function outsideLayout(portPosition, elemBBox, autoOrient, args) {
    const offset = args.offset != null ? args.offset : 15;
    const angle = elemBBox.getCenter().theta(portPosition);
    const bboxAngles = getBBoxAngles(elemBBox);
    let y;
    let tx;
    let ty;
    let textAnchor;
    let orientAngle = 0;
    if (angle < bboxAngles[1] || angle > bboxAngles[2]) {
        y = '.3em';
        tx = offset;
        ty = 0;
        textAnchor = 'start';
    }
    else if (angle < bboxAngles[0]) {
        y = '0';
        tx = 0;
        ty = -offset;
        if (autoOrient) {
            orientAngle = -90;
            textAnchor = 'start';
        }
        else {
            textAnchor = 'middle';
        }
    }
    else if (angle < bboxAngles[3]) {
        y = '.3em';
        tx = -offset;
        ty = 0;
        textAnchor = 'end';
    }
    else {
        y = '.6em';
        tx = 0;
        ty = offset;
        if (autoOrient) {
            orientAngle = 90;
            textAnchor = 'start';
        }
        else {
            textAnchor = 'middle';
        }
    }
    return (0, util_1.toResult)({
        position: {
            x: Math.round(tx),
            y: Math.round(ty),
        },
        angle: orientAngle,
        attrs: {
            '.': {
                y,
                'text-anchor': textAnchor,
            },
        },
    }, args);
}
function insideLayout(portPosition, elemBBox, autoOrient, args) {
    const offset = args.offset != null ? args.offset : 15;
    const angle = elemBBox.getCenter().theta(portPosition);
    const bboxAngles = getBBoxAngles(elemBBox);
    let y;
    let tx;
    let ty;
    let textAnchor;
    let orientAngle = 0;
    if (angle < bboxAngles[1] || angle > bboxAngles[2]) {
        y = '.3em';
        tx = -offset;
        ty = 0;
        textAnchor = 'end';
    }
    else if (angle < bboxAngles[0]) {
        y = '.6em';
        tx = 0;
        ty = offset;
        if (autoOrient) {
            orientAngle = 90;
            textAnchor = 'start';
        }
        else {
            textAnchor = 'middle';
        }
    }
    else if (angle < bboxAngles[3]) {
        y = '.3em';
        tx = offset;
        ty = 0;
        textAnchor = 'start';
    }
    else {
        y = '0em';
        tx = 0;
        ty = -offset;
        if (autoOrient) {
            orientAngle = -90;
            textAnchor = 'start';
        }
        else {
            textAnchor = 'middle';
        }
    }
    return (0, util_1.toResult)({
        position: {
            x: Math.round(tx),
            y: Math.round(ty),
        },
        angle: orientAngle,
        attrs: {
            '.': {
                y,
                'text-anchor': textAnchor,
            },
        },
    }, args);
}
function getBBoxAngles(elemBBox) {
    const center = elemBBox.getCenter();
    const tl = center.theta(elemBBox.getTopLeft());
    const bl = center.theta(elemBBox.getBottomLeft());
    const br = center.theta(elemBBox.getBottomRight());
    const tr = center.theta(elemBBox.getTopRight());
    return [tl, tr, br, bl];
}
//# sourceMappingURL=inout.js.map