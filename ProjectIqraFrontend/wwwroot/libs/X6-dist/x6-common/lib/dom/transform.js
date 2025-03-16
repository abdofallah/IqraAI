"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.toLocalPoint = exports.getTransformToParentElement = exports.getTransformToElement = exports.scale = exports.rotate = exports.translate = exports.transform = void 0;
const attr_1 = require("./attr");
const elem_1 = require("./elem");
const matrix_1 = require("./matrix");
function transform(elem, matrix, options = {}) {
    if (matrix == null) {
        return (0, matrix_1.transformStringToMatrix)((0, attr_1.attr)(elem, 'transform'));
    }
    if (options.absolute) {
        elem.setAttribute('transform', (0, matrix_1.matrixToTransformString)(matrix));
        return;
    }
    const transformList = elem.transform;
    const svgTransform = (0, matrix_1.createSVGTransform)(matrix);
    transformList.baseVal.appendItem(svgTransform);
}
exports.transform = transform;
function translate(elem, tx, ty = 0, options = {}) {
    let transformAttr = (0, attr_1.attr)(elem, 'transform');
    const transform = (0, matrix_1.parseTransformString)(transformAttr);
    if (tx == null) {
        return transform.translation;
    }
    transformAttr = transform.raw;
    transformAttr = transformAttr.replace(/translate\([^)]*\)/g, '').trim();
    const newTx = options.absolute ? tx : transform.translation.tx + tx;
    const newTy = options.absolute ? ty : transform.translation.ty + ty;
    const newTranslate = `translate(${newTx},${newTy})`;
    // Note that `translate()` is always the first transformation. This is
    // usually the desired case.
    elem.setAttribute('transform', `${newTranslate} ${transformAttr}`.trim());
}
exports.translate = translate;
function rotate(elem, angle, cx, cy, options = {}) {
    let transformAttr = (0, attr_1.attr)(elem, 'transform');
    const transform = (0, matrix_1.parseTransformString)(transformAttr);
    if (angle == null) {
        return transform.rotation;
    }
    transformAttr = transform.raw;
    transformAttr = transformAttr.replace(/rotate\([^)]*\)/g, '').trim();
    angle %= 360; // eslint-disable-line
    const newAngle = options.absolute ? angle : transform.rotation.angle + angle;
    const newOrigin = cx != null && cy != null ? `,${cx},${cy}` : '';
    const newRotate = `rotate(${newAngle}${newOrigin})`;
    elem.setAttribute('transform', `${transformAttr} ${newRotate}`.trim());
}
exports.rotate = rotate;
function scale(elem, sx, sy) {
    let transformAttr = (0, attr_1.attr)(elem, 'transform');
    const transform = (0, matrix_1.parseTransformString)(transformAttr);
    if (sx == null) {
        return transform.scale;
    }
    sy = sy == null ? sx : sy; // eslint-disable-line
    transformAttr = transform.raw;
    transformAttr = transformAttr.replace(/scale\([^)]*\)/g, '').trim();
    const newScale = `scale(${sx},${sy})`;
    elem.setAttribute('transform', `${transformAttr} ${newScale}`.trim());
}
exports.scale = scale;
/**
 * Returns an DOMMatrix that specifies the transformation necessary
 * to convert `elem` coordinate system into `target` coordinate system.
 */
function getTransformToElement(elem, target) {
    if ((0, elem_1.isSVGGraphicsElement)(target) && (0, elem_1.isSVGGraphicsElement)(elem)) {
        const targetCTM = target.getScreenCTM();
        const nodeCTM = elem.getScreenCTM();
        if (targetCTM && nodeCTM) {
            return targetCTM.inverse().multiply(nodeCTM);
        }
    }
    // Could not get actual transformation matrix
    return (0, matrix_1.createSVGMatrix)();
}
exports.getTransformToElement = getTransformToElement;
/**
 * Returns an DOMMatrix that specifies the transformation necessary
 * to convert `elem` coordinate system into `target` coordinate system.
 * Unlike getTransformToElement, elem is child of target,Because of the reduction in DOM API calls,
 * there is a significant performance improvement.
 */
function getTransformToParentElement(elem, target) {
    let matrix = (0, matrix_1.createSVGMatrix)();
    if ((0, elem_1.isSVGGraphicsElement)(target) && (0, elem_1.isSVGGraphicsElement)(elem)) {
        let node = elem;
        const matrixList = [];
        while (node && node !== target) {
            const transform = node.getAttribute('transform') || null;
            const nodeMatrix = (0, matrix_1.transformStringToMatrix)(transform);
            matrixList.push(nodeMatrix);
            node = node.parentNode;
        }
        matrixList.reverse().forEach((m) => {
            matrix = matrix.multiply(m);
        });
    }
    return matrix;
}
exports.getTransformToParentElement = getTransformToParentElement;
/**
 * Converts a global point with coordinates `x` and `y` into the
 * coordinate space of the element.
 */
function toLocalPoint(elem, x, y) {
    const svg = elem instanceof SVGSVGElement
        ? elem
        : elem.ownerSVGElement;
    const p = svg.createSVGPoint();
    p.x = x;
    p.y = y;
    try {
        const ctm = svg.getScreenCTM();
        const globalPoint = p.matrixTransform(ctm.inverse());
        const globalToLocalMatrix = getTransformToElement(elem, svg).inverse();
        return globalPoint.matrixTransform(globalToLocalMatrix);
    }
    catch (e) {
        return p;
    }
}
exports.toLocalPoint = toLocalPoint;
//# sourceMappingURL=transform.js.map