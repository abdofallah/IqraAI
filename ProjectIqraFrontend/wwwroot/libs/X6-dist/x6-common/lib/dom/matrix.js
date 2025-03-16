"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.matrixToTranslation = exports.matrixToRotation = exports.matrixToScale = exports.decomposeMatrix = exports.parseTransformString = exports.matrixToTransformString = exports.transformStringToMatrix = exports.createSVGTransform = exports.createSVGMatrix = exports.createSVGPoint = void 0;
const elem_1 = require("./elem");
const transformRegex = /(\w+)\(([^,)]+),?([^)]+)?\)/gi;
const transformSeparatorRegex = /[ ,]+/;
const transformationListRegex = /^(\w+)\((.*)\)/;
/**
 * Returns a SVG point object initialized with the `x` and `y` coordinates.
 * @see https://developer.mozilla.org/en/docs/Web/API/SVGPoint
 */
function createSVGPoint(x, y) {
    const svgDocument = (0, elem_1.createSvgElement)('svg');
    const p = svgDocument.createSVGPoint();
    p.x = x;
    p.y = y;
    return p;
}
exports.createSVGPoint = createSVGPoint;
/**
 * Returns the SVG transformation matrix initialized with the given matrix.
 *
 * The given matrix is an object of the form:
 * {
 *   a: number
 *   b: number
 *   c: number
 *   d: number
 *   e: number
 *   f: number
 * }
 *
 * @see https://developer.mozilla.org/en/docs/Web/API/SVGMatrix
 */
function createSVGMatrix(matrix) {
    const svgDocument = (0, elem_1.createSvgElement)('svg');
    const mat = svgDocument.createSVGMatrix();
    if (matrix != null) {
        const source = matrix;
        const target = mat;
        // eslint-disable-next-line
        for (const key in source) {
            target[key] = source[key];
        }
    }
    return mat;
}
exports.createSVGMatrix = createSVGMatrix;
/**
 * Returns a SVG transform object.
 * @see https://developer.mozilla.org/en/docs/Web/API/SVGTransform
 */
function createSVGTransform(matrix) {
    const svgDocument = (0, elem_1.createSvgElement)('svg');
    if (matrix != null) {
        if (!(matrix instanceof DOMMatrix)) {
            matrix = createSVGMatrix(matrix); // eslint-disable-line
        }
        return svgDocument.createSVGTransformFromMatrix(matrix);
    }
    return svgDocument.createSVGTransform();
}
exports.createSVGTransform = createSVGTransform;
/**
 * Returns the SVG transformation matrix built from the `transformString`.
 *
 * E.g. 'translate(10,10) scale(2,2)' will result in matrix:
 * `{ a: 2, b: 0, c: 0, d: 2, e: 10, f: 10}`
 */
function transformStringToMatrix(transform) {
    let mat = createSVGMatrix();
    const matches = transform != null && transform.match(transformRegex);
    if (!matches) {
        return mat;
    }
    for (let i = 0, n = matches.length; i < n; i += 1) {
        const transformationString = matches[i];
        const transformationMatch = transformationString.match(transformationListRegex);
        if (transformationMatch) {
            let sx;
            let sy;
            let tx;
            let ty;
            let angle;
            let ctm = createSVGMatrix();
            const args = transformationMatch[2].split(transformSeparatorRegex);
            switch (transformationMatch[1].toLowerCase()) {
                case 'scale':
                    sx = parseFloat(args[0]);
                    sy = args[1] === undefined ? sx : parseFloat(args[1]);
                    ctm = ctm.scaleNonUniform(sx, sy);
                    break;
                case 'translate':
                    tx = parseFloat(args[0]);
                    ty = parseFloat(args[1]);
                    ctm = ctm.translate(tx, ty);
                    break;
                case 'rotate':
                    angle = parseFloat(args[0]);
                    tx = parseFloat(args[1]) || 0;
                    ty = parseFloat(args[2]) || 0;
                    if (tx !== 0 || ty !== 0) {
                        ctm = ctm.translate(tx, ty).rotate(angle).translate(-tx, -ty);
                    }
                    else {
                        ctm = ctm.rotate(angle);
                    }
                    break;
                case 'skewx':
                    angle = parseFloat(args[0]);
                    ctm = ctm.skewX(angle);
                    break;
                case 'skewy':
                    angle = parseFloat(args[0]);
                    ctm = ctm.skewY(angle);
                    break;
                case 'matrix':
                    ctm.a = parseFloat(args[0]);
                    ctm.b = parseFloat(args[1]);
                    ctm.c = parseFloat(args[2]);
                    ctm.d = parseFloat(args[3]);
                    ctm.e = parseFloat(args[4]);
                    ctm.f = parseFloat(args[5]);
                    break;
                default:
                    continue;
            }
            mat = mat.multiply(ctm);
        }
    }
    return mat;
}
exports.transformStringToMatrix = transformStringToMatrix;
function matrixToTransformString(matrix) {
    const m = matrix || {};
    const a = m.a != null ? m.a : 1;
    const b = m.b != null ? m.b : 0;
    const c = m.c != null ? m.c : 0;
    const d = m.d != null ? m.d : 1;
    const e = m.e != null ? m.e : 0;
    const f = m.f != null ? m.f : 0;
    return `matrix(${a},${b},${c},${d},${e},${f})`;
}
exports.matrixToTransformString = matrixToTransformString;
function parseTransformString(transform) {
    let translation;
    let rotation;
    let scale;
    if (transform) {
        const separator = transformSeparatorRegex;
        // Allow reading transform string with a single matrix
        if (transform.trim().indexOf('matrix') >= 0) {
            const matrix = transformStringToMatrix(transform);
            const decomposedMatrix = decomposeMatrix(matrix);
            translation = [decomposedMatrix.translateX, decomposedMatrix.translateY];
            rotation = [decomposedMatrix.rotation];
            scale = [decomposedMatrix.scaleX, decomposedMatrix.scaleY];
            const transformations = [];
            if (translation[0] !== 0 || translation[1] !== 0) {
                transformations.push(`translate(${translation.join(',')})`);
            }
            if (scale[0] !== 1 || scale[1] !== 1) {
                transformations.push(`scale(${scale.join(',')})`);
            }
            if (rotation[0] !== 0) {
                transformations.push(`rotate(${rotation[0]})`);
            }
            transform = transformations.join(' '); // eslint-disable-line
        }
        else {
            const translateMatch = transform.match(/translate\((.*?)\)/);
            if (translateMatch) {
                translation = translateMatch[1].split(separator);
            }
            const rotateMatch = transform.match(/rotate\((.*?)\)/);
            if (rotateMatch) {
                rotation = rotateMatch[1].split(separator);
            }
            const scaleMatch = transform.match(/scale\((.*?)\)/);
            if (scaleMatch) {
                scale = scaleMatch[1].split(separator);
            }
        }
    }
    const sx = scale && scale[0] ? parseFloat(scale[0]) : 1;
    return {
        raw: transform || '',
        translation: {
            tx: translation && translation[0]
                ? parseInt(translation[0], 10)
                : 0,
            ty: translation && translation[1]
                ? parseInt(translation[1], 10)
                : 0,
        },
        rotation: {
            angle: rotation && rotation[0] ? parseInt(rotation[0], 10) : 0,
            cx: rotation && rotation[1]
                ? parseInt(rotation[1], 10)
                : undefined,
            cy: rotation && rotation[2]
                ? parseInt(rotation[2], 10)
                : undefined,
        },
        scale: {
            sx,
            sy: scale && scale[1] ? parseFloat(scale[1]) : sx,
        },
    };
}
exports.parseTransformString = parseTransformString;
function deltaTransformPoint(matrix, point) {
    const dx = point.x * matrix.a + point.y * matrix.c + 0;
    const dy = point.x * matrix.b + point.y * matrix.d + 0;
    return { x: dx, y: dy };
}
/**
 * Decomposes the SVG transformation matrix into separate transformations.
 *
 * Returns an object of the form:
 * {
 *   translateX: number
 *   translateY: number
 *   scaleX: number
 *   scaleY: number
 *   skewX: number
 *   skewY: number
 *   rotation: number
 * }
 *
 * @see https://developer.mozilla.org/en/docs/Web/API/SVGMatrix
 */
function decomposeMatrix(matrix) {
    // @see https://gist.github.com/2052247
    const px = deltaTransformPoint(matrix, { x: 0, y: 1 });
    const py = deltaTransformPoint(matrix, { x: 1, y: 0 });
    const skewX = (180 / Math.PI) * Math.atan2(px.y, px.x) - 90;
    const skewY = (180 / Math.PI) * Math.atan2(py.y, py.x);
    return {
        skewX,
        skewY,
        translateX: matrix.e,
        translateY: matrix.f,
        scaleX: Math.sqrt(matrix.a * matrix.a + matrix.b * matrix.b),
        scaleY: Math.sqrt(matrix.c * matrix.c + matrix.d * matrix.d),
        rotation: skewX,
    };
}
exports.decomposeMatrix = decomposeMatrix;
function matrixToScale(matrix) {
    let a;
    let b;
    let c;
    let d;
    if (matrix) {
        a = matrix.a == null ? 1 : matrix.a;
        d = matrix.d == null ? 1 : matrix.d;
        b = matrix.b;
        c = matrix.c;
    }
    else {
        a = d = 1;
    }
    return {
        sx: b ? Math.sqrt(a * a + b * b) : a,
        sy: c ? Math.sqrt(c * c + d * d) : d,
    };
}
exports.matrixToScale = matrixToScale;
function matrixToRotation(matrix) {
    let p = { x: 0, y: 1 };
    if (matrix) {
        p = deltaTransformPoint(matrix, p);
    }
    const deg = (((180 * Math.atan2(p.y, p.x)) / Math.PI) % 360) - 90;
    const angle = (deg % 360) + (deg < 0 ? 360 : 0);
    return {
        angle,
    };
}
exports.matrixToRotation = matrixToRotation;
function matrixToTranslation(matrix) {
    return {
        tx: (matrix && matrix.e) || 0,
        ty: (matrix && matrix.f) || 0,
    };
}
exports.matrixToTranslation = matrixToTranslation;
//# sourceMappingURL=matrix.js.map