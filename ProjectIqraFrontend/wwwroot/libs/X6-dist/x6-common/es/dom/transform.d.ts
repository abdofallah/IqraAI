import { Scale, Rotation, Translation } from './matrix';
export interface TransformOptions {
    absolute?: boolean;
}
export declare function transform(elem: Element): DOMMatrix;
export declare function transform(elem: SVGElement, matrix: DOMMatrix, options?: TransformOptions): void;
export declare function translate(elem: Element): Translation;
export declare function translate(elem: Element, tx: number, ty?: number, options?: TransformOptions): void;
export declare function rotate(elem: Element): Rotation;
export declare function rotate(elem: Element, angle: number, cx?: number, cy?: number, options?: TransformOptions): void;
export declare function scale(elem: Element): Scale;
export declare function scale(elem: Element, sx: number, sy?: number): void;
/**
 * Returns an DOMMatrix that specifies the transformation necessary
 * to convert `elem` coordinate system into `target` coordinate system.
 */
export declare function getTransformToElement(elem: SVGElement, target: SVGElement): DOMMatrix;
/**
 * Returns an DOMMatrix that specifies the transformation necessary
 * to convert `elem` coordinate system into `target` coordinate system.
 * Unlike getTransformToElement, elem is child of target,Because of the reduction in DOM API calls,
 * there is a significant performance improvement.
 */
export declare function getTransformToParentElement(elem: SVGElement, target: SVGElement): DOMMatrix;
/**
 * Converts a global point with coordinates `x` and `y` into the
 * coordinate space of the element.
 */
export declare function toLocalPoint(elem: SVGElement | SVGSVGElement, x: number, y: number): DOMPoint;
