import { Point, Line, Rectangle, Polyline, Ellipse, Path } from '@antv/x6-geometry';
import { PointData, PointLike } from '@antv/x6-common';
import { normalize } from '../registry/marker/util';
export declare namespace Util {
    const normalizeMarker: typeof normalize;
    /**
     * Transforms point by an SVG transformation represented by `matrix`.
     */
    function transformPoint(point: Point.PointLike, matrix: DOMMatrix): Point;
    /**
     * Transforms line by an SVG transformation represented by `matrix`.
     */
    function transformLine(line: Line, matrix: DOMMatrix): Line;
    /**
     * Transforms polyline by an SVG transformation represented by `matrix`.
     */
    function transformPolyline(polyline: Polyline, matrix: DOMMatrix): Polyline;
    function transformRectangle(rect: Rectangle.RectangleLike, matrix: DOMMatrix): Rectangle;
    /**
     * Returns the bounding box of the element after transformations are
     * applied. If `withoutTransformations` is `true`, transformations of
     * the element will not be considered when computing the bounding box.
     * If `target` is specified, bounding box will be computed relatively
     * to the `target` element.
     */
    function bbox(elem: SVGElement, withoutTransformations?: boolean, target?: SVGElement): Rectangle;
    /**
     * Returns the bounding box of the element after transformations are
     * applied. Unlike `bbox()`, this function fixes a browser implementation
     * bug to return the correct bounding box if this elemenent is a group of
     * svg elements (if `options.recursive` is specified).
     */
    function getBBox(elem: SVGElement, options?: {
        target?: SVGElement | null;
        recursive?: boolean;
    }): Rectangle;
    function getBoundingOffsetRect(elem: HTMLElement): {
        left: number;
        top: number;
        width: number;
        height: number;
    };
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
    function toGeometryShape(elem: SVGElement): Path | Rectangle | Line | Polyline | Ellipse;
    function translateAndAutoOrient(elem: SVGElement, position: PointLike | PointData, reference: PointLike | PointData, target?: SVGElement): void;
    function findShapeNode(magnet: Element): Element | null;
    function getBBoxV2(elem: SVGElement): Rectangle;
}
