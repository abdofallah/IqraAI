import { Point } from './point';
import { JSONObject, JSONArray } from './types';
export declare abstract class Geometry {
    abstract scale(sx: number, sy: number, origin?: Point.PointLike | Point.PointData): this;
    abstract rotate(angle: number, origin?: Point.PointLike | Point.PointData): this;
    abstract translate(tx: number, ty: number): this;
    abstract translate(p: Point.PointLike | Point.PointData): this;
    abstract clone(): Geometry;
    abstract equals(g: any): boolean;
    abstract toJSON(): JSONObject | JSONArray;
    abstract serialize(): string;
    valueOf(): JSONObject | JSONArray;
    toString(): string;
}
