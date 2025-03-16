import { Dom, NumberExt } from '@antv/x6-common';
import { Point, Rectangle } from '@antv/x6-geometry';
import { Base } from './base';
import { Cell } from '../model';
export declare class TransformManager extends Base {
    protected viewportMatrix: DOMMatrix | null;
    protected viewportTransformString: string | null;
    protected get container(): HTMLElement;
    protected get viewport(): SVGGElement;
    protected get stage(): SVGGElement;
    protected init(): void;
    /**
     * Returns the current transformation matrix of the graph.
     */
    getMatrix(): DOMMatrix;
    /**
     * Sets new transformation with the given `matrix`
     */
    setMatrix(matrix: DOMMatrix | Dom.MatrixLike | null): void;
    resize(width?: number, height?: number): this;
    getComputedSize(): {
        width: number;
        height: number;
    };
    getScale(): Dom.Scale;
    scale(sx: number, sy?: number, ox?: number, oy?: number): this;
    clampScale(scale: number): number;
    getZoom(): number;
    zoom(factor: number, options?: TransformManager.ZoomOptions): this;
    getRotation(): Dom.Rotation;
    rotate(angle: number, cx?: number, cy?: number): this;
    getTranslation(): Dom.Translation;
    translate(tx: number, ty: number): this;
    setOrigin(ox?: number, oy?: number): this;
    fitToContent(gridWidth?: number | TransformManager.FitToContentFullOptions, gridHeight?: number, padding?: NumberExt.SideOptions, options?: TransformManager.FitToContentOptions): Rectangle;
    scaleContentToFit(options?: TransformManager.ScaleContentToFitOptions): void;
    scaleContentToFitImpl(options?: TransformManager.ScaleContentToFitOptions, translate?: boolean): void;
    getContentArea(options?: TransformManager.GetContentAreaOptions): Rectangle;
    getContentBBox(options?: TransformManager.GetContentAreaOptions): Rectangle;
    getGraphArea(): Rectangle;
    zoomToRect(rect: Rectangle.RectangleLike, options?: TransformManager.ScaleContentToFitOptions): this;
    zoomToFit(options?: TransformManager.GetContentAreaOptions & TransformManager.ScaleContentToFitOptions): this;
    centerPoint(x?: number, y?: number): void;
    centerContent(options?: TransformManager.GetContentAreaOptions): void;
    centerCell(cell: Cell): void | this;
    positionPoint(point: Point.PointLike, x: number | string, y: number | string): void;
    positionRect(rect: Rectangle.RectangleLike, pos: TransformManager.Direction): void | this;
    positionCell(cell: Cell, pos: TransformManager.Direction): void | this;
    positionContent(pos: TransformManager.Direction, options?: TransformManager.GetContentAreaOptions): void | this;
}
export declare namespace TransformManager {
    interface FitToContentOptions extends GetContentAreaOptions {
        minWidth?: number;
        minHeight?: number;
        maxWidth?: number;
        maxHeight?: number;
        contentArea?: Rectangle | Rectangle.RectangleLike;
        border?: number;
        allowNewOrigin?: 'negative' | 'positive' | 'any';
    }
    interface FitToContentFullOptions extends FitToContentOptions {
        gridWidth?: number;
        gridHeight?: number;
        padding?: NumberExt.SideOptions;
    }
    interface ScaleContentToFitOptions extends GetContentAreaOptions {
        padding?: NumberExt.SideOptions;
        minScale?: number;
        maxScale?: number;
        minScaleX?: number;
        minScaleY?: number;
        maxScaleX?: number;
        maxScaleY?: number;
        scaleGrid?: number;
        contentArea?: Rectangle.RectangleLike;
        viewportArea?: Rectangle.RectangleLike;
        preserveAspectRatio?: boolean;
    }
    interface GetContentAreaOptions {
        useCellGeometry?: boolean;
    }
    interface ZoomOptions {
        absolute?: boolean;
        minScale?: number;
        maxScale?: number;
        scaleGrid?: number;
        center?: Point.PointLike;
    }
    type Direction = 'center' | 'top' | 'top-right' | 'top-left' | 'right' | 'bottom-right' | 'bottom' | 'bottom-left' | 'left';
    interface CenterOptions {
        padding?: NumberExt.SideOptions;
    }
    type PositionContentOptions = GetContentAreaOptions & CenterOptions;
}
