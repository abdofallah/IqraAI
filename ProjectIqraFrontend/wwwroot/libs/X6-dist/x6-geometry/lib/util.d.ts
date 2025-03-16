import { Point } from './point';
import { Rectangle } from './rectangle';
export declare namespace GeometryUtil {
    function round(num: number, precision?: number): number;
    function random(): number;
    function random(max: number): number;
    function random(min: number, max: number): number;
    function clamp(value: number, min: number, max: number): number;
    function snapToGrid(value: number, gridSize: number): number;
    function containsPoint(rect: Rectangle.RectangleLike, point: Point.PointLike): boolean;
    function squaredLength(p1: Point.PointLike, p2: Point.PointLike): number;
}
