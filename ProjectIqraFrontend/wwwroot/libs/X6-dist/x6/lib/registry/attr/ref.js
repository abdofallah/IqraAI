"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.refHeight2 = exports.refWidth2 = exports.refY2 = exports.refX2 = exports.refPoints = exports.refD = exports.refR = exports.refPointsKeepOffset = exports.refPointsResetOffset = exports.refDKeepOffset = exports.refDResetOffset = exports.refCy = exports.refCx = exports.refRCircumscribed = exports.refRInscribed = exports.refRy = exports.refRx = exports.refHeight = exports.refWidth = exports.refDy = exports.refDx = exports.refY = exports.refX = exports.ref = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const x6_common_1 = require("@antv/x6-common");
exports.ref = {
// We do not set `ref` attribute directly on an element.
// The attribute itself does not qualify for relative positioning.
};
// if `refX` is in [0, 1] then `refX` is a fraction of bounding box width
// if `refX` is < 0 then `refX`'s absolute values is the right coordinate of the bounding box
// otherwise, `refX` is the left coordinate of the bounding box
exports.refX = {
    position: positionWrapper('x', 'width', 'origin'),
};
exports.refY = {
    position: positionWrapper('y', 'height', 'origin'),
};
// `ref-dx` and `ref-dy` define the offset of the subelement relative to the right and/or bottom
// coordinate of the reference element.
exports.refDx = {
    position: positionWrapper('x', 'width', 'corner'),
};
exports.refDy = {
    position: positionWrapper('y', 'height', 'corner'),
};
// 'ref-width'/'ref-height' defines the width/height of the subelement relatively to
// the reference element size
// val in 0..1         ref-width = 0.75 sets the width to 75% of the ref. el. width
// val < 0 || val > 1  ref-height = -20 sets the height to the ref. el. height shorter by 20
exports.refWidth = {
    set: setWrapper('width', 'width'),
};
exports.refHeight = {
    set: setWrapper('height', 'height'),
};
exports.refRx = {
    set: setWrapper('rx', 'width'),
};
exports.refRy = {
    set: setWrapper('ry', 'height'),
};
exports.refRInscribed = {
    set: ((attrName) => {
        const widthFn = setWrapper(attrName, 'width');
        const heightFn = setWrapper(attrName, 'height');
        return function (value, options) {
            const refBBox = options.refBBox;
            const fn = refBBox.height > refBBox.width ? widthFn : heightFn;
            return x6_common_1.FunctionExt.call(fn, this, value, options);
        };
    })('r'),
};
exports.refRCircumscribed = {
    set(val, { refBBox }) {
        let value = parseFloat(val);
        const percentage = x6_common_1.NumberExt.isPercentage(val);
        if (percentage) {
            value /= 100;
        }
        const diagonalLength = Math.sqrt(refBBox.height * refBBox.height + refBBox.width * refBBox.width);
        let rValue;
        if (Number.isFinite(value)) {
            if (percentage || (value >= 0 && value <= 1)) {
                rValue = value * diagonalLength;
            }
            else {
                rValue = Math.max(value + diagonalLength, 0);
            }
        }
        return { r: rValue };
    },
};
exports.refCx = {
    set: setWrapper('cx', 'width'),
};
exports.refCy = {
    set: setWrapper('cy', 'height'),
};
exports.refDResetOffset = {
    set: dWrapper({ resetOffset: true }),
};
exports.refDKeepOffset = {
    set: dWrapper({ resetOffset: false }),
};
exports.refPointsResetOffset = {
    set: pointsWrapper({ resetOffset: true }),
};
exports.refPointsKeepOffset = {
    set: pointsWrapper({ resetOffset: false }),
};
// aliases
// -------
exports.refR = exports.refRInscribed;
exports.refD = exports.refDResetOffset;
exports.refPoints = exports.refPointsResetOffset;
// Allows to combine both absolute and relative positioning
// refX: 50%, refX2: 20
exports.refX2 = exports.refX;
exports.refY2 = exports.refY;
exports.refWidth2 = exports.refWidth;
exports.refHeight2 = exports.refHeight;
// utils
// -----
function positionWrapper(axis, dimension, origin) {
    return (val, { refBBox }) => {
        if (val == null) {
            return null;
        }
        let value = parseFloat(val);
        const percentage = x6_common_1.NumberExt.isPercentage(val);
        if (percentage) {
            value /= 100;
        }
        let delta;
        if (Number.isFinite(value)) {
            const refOrigin = refBBox[origin];
            if (percentage || (value > 0 && value < 1)) {
                delta = refOrigin[axis] + refBBox[dimension] * value;
            }
            else {
                delta = refOrigin[axis] + value;
            }
        }
        const point = new x6_geometry_1.Point();
        point[axis] = delta || 0;
        return point;
    };
}
function setWrapper(attrName, dimension) {
    return function (val, { refBBox }) {
        let value = parseFloat(val);
        const percentage = x6_common_1.NumberExt.isPercentage(val);
        if (percentage) {
            value /= 100;
        }
        const attrs = {};
        if (Number.isFinite(value)) {
            const attrValue = percentage || (value >= 0 && value <= 1)
                ? value * refBBox[dimension]
                : Math.max(value + refBBox[dimension], 0);
            attrs[attrName] = attrValue;
        }
        return attrs;
    };
}
function shapeWrapper(shapeConstructor, options) {
    const cacheName = 'x6-shape';
    const resetOffset = options && options.resetOffset;
    return function (value, { elem, refBBox }) {
        let cache = x6_common_1.Dom.data(elem, cacheName);
        if (!cache || cache.value !== value) {
            // only recalculate if value has changed
            const cachedShape = shapeConstructor(value);
            cache = {
                value,
                shape: cachedShape,
                shapeBBox: cachedShape.bbox(),
            };
            x6_common_1.Dom.data(elem, cacheName, cache);
        }
        const shape = cache.shape.clone();
        const shapeBBox = cache.shapeBBox.clone();
        const shapeOrigin = shapeBBox.getOrigin();
        const refOrigin = refBBox.getOrigin();
        shapeBBox.x = refOrigin.x;
        shapeBBox.y = refOrigin.y;
        const fitScale = refBBox.getMaxScaleToFit(shapeBBox, refOrigin);
        // `maxRectScaleToFit` can give Infinity if width or height is 0
        const sx = shapeBBox.width === 0 || refBBox.width === 0 ? 1 : fitScale.sx;
        const sy = shapeBBox.height === 0 || refBBox.height === 0 ? 1 : fitScale.sy;
        shape.scale(sx, sy, shapeOrigin);
        if (resetOffset) {
            shape.translate(-shapeOrigin.x, -shapeOrigin.y);
        }
        return shape;
    };
}
// `d` attribute for SVGPaths
function dWrapper(options) {
    function pathConstructor(value) {
        return x6_geometry_1.Path.parse(value);
    }
    const shape = shapeWrapper(pathConstructor, options);
    return (value, args) => {
        const path = shape(value, args);
        return {
            d: path.serialize(),
        };
    };
}
// `points` attribute for SVGPolylines and SVGPolygons
function pointsWrapper(options) {
    const shape = shapeWrapper((points) => new x6_geometry_1.Polyline(points), options);
    return (value, args) => {
        const polyline = shape(value, args);
        return {
            points: polyline.serialize(),
        };
    };
}
//# sourceMappingURL=ref.js.map