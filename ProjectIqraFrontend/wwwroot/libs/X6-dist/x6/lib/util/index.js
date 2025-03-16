"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Util = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const x6_common_1 = require("@antv/x6-common");
const util_1 = require("../registry/marker/util");
var Util;
(function (Util) {
    Util.normalizeMarker = util_1.normalize;
    /**
     * Transforms point by an SVG transformation represented by `matrix`.
     */
    function transformPoint(point, matrix) {
        const ret = x6_common_1.Dom.createSVGPoint(point.x, point.y).matrixTransform(matrix);
        return new x6_geometry_1.Point(ret.x, ret.y);
    }
    Util.transformPoint = transformPoint;
    /**
     * Transforms line by an SVG transformation represented by `matrix`.
     */
    function transformLine(line, matrix) {
        return new x6_geometry_1.Line(transformPoint(line.start, matrix), transformPoint(line.end, matrix));
    }
    Util.transformLine = transformLine;
    /**
     * Transforms polyline by an SVG transformation represented by `matrix`.
     */
    function transformPolyline(polyline, matrix) {
        let points = polyline instanceof x6_geometry_1.Polyline ? polyline.points : polyline;
        if (!Array.isArray(points)) {
            points = [];
        }
        return new x6_geometry_1.Polyline(points.map((p) => transformPoint(p, matrix)));
    }
    Util.transformPolyline = transformPolyline;
    function transformRectangle(rect, matrix) {
        const svgDocument = x6_common_1.Dom.createSvgElement('svg');
        const p = svgDocument.createSVGPoint();
        p.x = rect.x;
        p.y = rect.y;
        const corner1 = p.matrixTransform(matrix);
        p.x = rect.x + rect.width;
        p.y = rect.y;
        const corner2 = p.matrixTransform(matrix);
        p.x = rect.x + rect.width;
        p.y = rect.y + rect.height;
        const corner3 = p.matrixTransform(matrix);
        p.x = rect.x;
        p.y = rect.y + rect.height;
        const corner4 = p.matrixTransform(matrix);
        const minX = Math.min(corner1.x, corner2.x, corner3.x, corner4.x);
        const maxX = Math.max(corner1.x, corner2.x, corner3.x, corner4.x);
        const minY = Math.min(corner1.y, corner2.y, corner3.y, corner4.y);
        const maxY = Math.max(corner1.y, corner2.y, corner3.y, corner4.y);
        return new x6_geometry_1.Rectangle(minX, minY, maxX - minX, maxY - minY);
    }
    Util.transformRectangle = transformRectangle;
    /**
     * Returns the bounding box of the element after transformations are
     * applied. If `withoutTransformations` is `true`, transformations of
     * the element will not be considered when computing the bounding box.
     * If `target` is specified, bounding box will be computed relatively
     * to the `target` element.
     */
    function bbox(elem, withoutTransformations, target) {
        let box;
        const ownerSVGElement = elem.ownerSVGElement;
        // If the element is not in the live DOM, it does not have a bounding
        // box defined and so fall back to 'zero' dimension element.
        if (!ownerSVGElement) {
            return new x6_geometry_1.Rectangle(0, 0, 0, 0);
        }
        try {
            box = elem.getBBox();
        }
        catch (e) {
            // Fallback for IE.
            box = {
                x: elem.clientLeft,
                y: elem.clientTop,
                width: elem.clientWidth,
                height: elem.clientHeight,
            };
        }
        if (withoutTransformations) {
            return x6_geometry_1.Rectangle.create(box);
        }
        const matrix = x6_common_1.Dom.getTransformToElement(elem, target || ownerSVGElement);
        return transformRectangle(box, matrix);
    }
    Util.bbox = bbox;
    /**
     * Returns the bounding box of the element after transformations are
     * applied. Unlike `bbox()`, this function fixes a browser implementation
     * bug to return the correct bounding box if this elemenent is a group of
     * svg elements (if `options.recursive` is specified).
     */
    function getBBox(elem, options = {}) {
        let outputBBox;
        const ownerSVGElement = elem.ownerSVGElement;
        // If the element is not in the live DOM, it does not have a bounding box
        // defined and so fall back to 'zero' dimension element.
        // If the element is not an SVGGraphicsElement, we could not measure the
        // bounding box either
        if (!ownerSVGElement || !x6_common_1.Dom.isSVGGraphicsElement(elem)) {
            if (x6_common_1.Dom.isHTMLElement(elem)) {
                // If the element is a HTMLElement, return the position relative to the body
                const { left, top, width, height } = getBoundingOffsetRect(elem);
                return new x6_geometry_1.Rectangle(left, top, width, height);
            }
            return new x6_geometry_1.Rectangle(0, 0, 0, 0);
        }
        let target = options.target;
        const recursive = options.recursive;
        if (!recursive) {
            try {
                outputBBox = elem.getBBox();
            }
            catch (e) {
                outputBBox = {
                    x: elem.clientLeft,
                    y: elem.clientTop,
                    width: elem.clientWidth,
                    height: elem.clientHeight,
                };
            }
            if (!target) {
                return x6_geometry_1.Rectangle.create(outputBBox);
            }
            // transform like target
            const matrix = x6_common_1.Dom.getTransformToElement(elem, target);
            return transformRectangle(outputBBox, matrix);
        }
        // recursive
        {
            const children = elem.childNodes;
            const n = children.length;
            if (n === 0) {
                return getBBox(elem, {
                    target,
                });
            }
            if (!target) {
                target = elem; // eslint-disable-line
            }
            for (let i = 0; i < n; i += 1) {
                const child = children[i];
                let childBBox;
                if (child.childNodes.length === 0) {
                    childBBox = getBBox(child, {
                        target,
                    });
                }
                else {
                    // if child is a group element, enter it with a recursive call
                    childBBox = getBBox(child, {
                        target,
                        recursive: true,
                    });
                }
                if (!outputBBox) {
                    outputBBox = childBBox;
                }
                else {
                    outputBBox = outputBBox.union(childBBox);
                }
            }
            return outputBBox;
        }
    }
    Util.getBBox = getBBox;
    function getBoundingOffsetRect(elem) {
        let left = 0;
        let top = 0;
        let width = 0;
        let height = 0;
        if (elem) {
            let current = elem;
            while (current) {
                left += current.offsetLeft;
                top += current.offsetTop;
                current = current.offsetParent;
                if (current) {
                    left += parseInt(x6_common_1.Dom.getComputedStyle(current, 'borderLeft'), 10);
                    top += parseInt(x6_common_1.Dom.getComputedStyle(current, 'borderTop'), 10);
                }
            }
            width = elem.offsetWidth;
            height = elem.offsetHeight;
        }
        return {
            left,
            top,
            width,
            height,
        };
    }
    Util.getBoundingOffsetRect = getBoundingOffsetRect;
    /**
     * Convert the SVGElement to an equivalent geometric shape. The element's
     * transformations are not taken into account.
     *
     * SVGRectElement      => Rectangle
     *
     * SVGLineElement      => Line
     *
     * SVGCircleElement    => Ellipse
     *
     * SVGEllipseElement   => Ellipse
     *
     * SVGPolygonElement   => Polyline
     *
     * SVGPolylineElement  => Polyline
     *
     * SVGPathElement      => Path
     *
     * others              => Rectangle
     */
    function toGeometryShape(elem) {
        const attr = (name) => {
            const s = elem.getAttribute(name);
            const v = s ? parseFloat(s) : 0;
            return Number.isNaN(v) ? 0 : v;
        };
        switch (elem instanceof SVGElement && elem.nodeName.toLowerCase()) {
            case 'rect':
                return new x6_geometry_1.Rectangle(attr('x'), attr('y'), attr('width'), attr('height'));
            case 'circle':
                return new x6_geometry_1.Ellipse(attr('cx'), attr('cy'), attr('r'), attr('r'));
            case 'ellipse':
                return new x6_geometry_1.Ellipse(attr('cx'), attr('cy'), attr('rx'), attr('ry'));
            case 'polyline': {
                const points = x6_common_1.Dom.getPointsFromSvgElement(elem);
                return new x6_geometry_1.Polyline(points);
            }
            case 'polygon': {
                const points = x6_common_1.Dom.getPointsFromSvgElement(elem);
                if (points.length > 1) {
                    points.push(points[0]);
                }
                return new x6_geometry_1.Polyline(points);
            }
            case 'path': {
                let d = elem.getAttribute('d');
                if (!x6_geometry_1.Path.isValid(d)) {
                    d = x6_geometry_1.Path.normalize(d);
                }
                return x6_geometry_1.Path.parse(d);
            }
            case 'line': {
                return new x6_geometry_1.Line(attr('x1'), attr('y1'), attr('x2'), attr('y2'));
            }
            default:
                break;
        }
        // Anything else is a rectangle
        return getBBox(elem);
    }
    Util.toGeometryShape = toGeometryShape;
    function translateAndAutoOrient(elem, position, reference, target) {
        const pos = x6_geometry_1.Point.create(position);
        const ref = x6_geometry_1.Point.create(reference);
        if (!target) {
            const svg = elem instanceof SVGSVGElement ? elem : elem.ownerSVGElement;
            target = svg; // eslint-disable-line
        }
        // Clean-up previously set transformations except the scale.
        // If we didn't clean up the previous transformations then they'd
        // add up with the old ones. Scale is an exception as it doesn't
        // add up, consider: `this.scale(2).scale(2).scale(2)`. The result
        // is that the element is scaled by the factor 2, not 8.
        const s = x6_common_1.Dom.scale(elem);
        elem.setAttribute('transform', '');
        const bbox = getBBox(elem, {
            target,
        }).scale(s.sx, s.sy);
        // 1. Translate to origin.
        const translateToOrigin = x6_common_1.Dom.createSVGTransform();
        translateToOrigin.setTranslate(-bbox.x - bbox.width / 2, -bbox.y - bbox.height / 2);
        // 2. Rotate around origin.
        const rotateAroundOrigin = x6_common_1.Dom.createSVGTransform();
        const angle = pos.angleBetween(ref, pos.clone().translate(1, 0));
        if (angle)
            rotateAroundOrigin.setRotate(angle, 0, 0);
        // 3. Translate to the `position` + the offset (half my width)
        //    towards the `reference` point.
        const translateFromOrigin = x6_common_1.Dom.createSVGTransform();
        const finalPosition = pos.clone().move(ref, bbox.width / 2);
        translateFromOrigin.setTranslate(2 * pos.x - finalPosition.x, 2 * pos.y - finalPosition.y);
        // 4. Get the current transformation matrix of this node
        const ctm = x6_common_1.Dom.getTransformToElement(elem, target);
        // 5. Apply transformations and the scale
        const transform = x6_common_1.Dom.createSVGTransform();
        transform.setMatrix(translateFromOrigin.matrix.multiply(rotateAroundOrigin.matrix.multiply(translateToOrigin.matrix.multiply(ctm.scale(s.sx, s.sy)))));
        elem.setAttribute('transform', x6_common_1.Dom.matrixToTransformString(transform.matrix));
    }
    Util.translateAndAutoOrient = translateAndAutoOrient;
    function findShapeNode(magnet) {
        if (magnet == null) {
            return null;
        }
        let node = magnet;
        do {
            let tagName = node.tagName;
            if (typeof tagName !== 'string')
                return null;
            tagName = tagName.toUpperCase();
            if (x6_common_1.Dom.hasClass(node, 'x6-port')) {
                node = node.nextElementSibling;
            }
            else if (tagName === 'G') {
                node = node.firstElementChild;
            }
            else if (tagName === 'TITLE') {
                node = node.nextElementSibling;
            }
            else
                break;
        } while (node);
        return node;
    }
    Util.findShapeNode = findShapeNode;
    // BBox is calculated by the attribute and shape of the node.
    // Because of the reduction in DOM API calls, there is a significant performance improvement.
    function getBBoxV2(elem) {
        const node = findShapeNode(elem);
        if (!x6_common_1.Dom.isSVGGraphicsElement(node)) {
            if (x6_common_1.Dom.isHTMLElement(elem)) {
                const { left, top, width, height } = getBoundingOffsetRect(elem);
                return new x6_geometry_1.Rectangle(left, top, width, height);
            }
            return new x6_geometry_1.Rectangle(0, 0, 0, 0);
        }
        const shape = toGeometryShape(node);
        const bbox = shape.bbox() || x6_geometry_1.Rectangle.create();
        // const transform = node.getAttribute('transform')
        // if (transform) {
        //   const nodeMatrix = Dom.transformStringToMatrix(transform)
        //   return transformRectangle(bbox, nodeMatrix)
        // }
        return bbox;
    }
    Util.getBBoxV2 = getBBoxV2;
})(Util = exports.Util || (exports.Util = {}));
//# sourceMappingURL=index.js.map