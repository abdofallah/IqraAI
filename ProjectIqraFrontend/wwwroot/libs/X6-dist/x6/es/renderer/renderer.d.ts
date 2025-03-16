import { Point, Rectangle } from '@antv/x6-geometry';
import { Base } from '../graph/base';
import { Cell } from '../model';
import { CellView, EdgeView } from '../view';
export declare class Renderer extends Base {
    private readonly schedule;
    requestViewUpdate(view: CellView, flag: number, options?: any): void;
    isViewMounted(view: CellView): boolean;
    setRenderArea(area?: Rectangle): void;
    findViewByElem(elem: string | Element | undefined | null): CellView<Cell<Cell.Properties>, CellView.Options> | null;
    findViewByCell(cellId: string | number): CellView | null;
    findViewByCell(cell: Cell | null): CellView | null;
    findViewsFromPoint(p: Point.PointLike): CellView<Cell<Cell.Properties>, CellView.Options>[];
    findEdgeViewsFromPoint(p: Point.PointLike, threshold?: number): EdgeView<import("../model").Edge<import("../model").Edge.Properties>, EdgeView.Options>[];
    findViewsInArea(rect: Rectangle.RectangleLike, options?: {
        strict?: boolean;
        nodeOnly?: boolean;
    }): CellView<Cell<Cell.Properties>, CellView.Options>[];
    dispose(): void;
}
export declare namespace Renderer {
    interface FindViewsInAreaOptions {
        strict?: boolean;
    }
}
