import { Dom, ModifierKey, Basecoat, Rectangle, Point, Graph, TransformManager, Cell, BackgroundManager } from '@antv/x6';
import { ScrollerImpl } from './scroller';
import './api';
export declare class Scroller extends Basecoat<Scroller.EventArgs> implements Graph.Plugin {
    name: string;
    options: Scroller.Options;
    private graph;
    private scrollerImpl;
    get pannable(): boolean;
    get container(): HTMLDivElement;
    constructor(options?: Scroller.Options);
    init(graph: Graph): void;
    resize(width?: number, height?: number): void;
    resizePage(width?: number, height?: number): void;
    zoom(): number;
    zoom(factor: number, options?: TransformManager.ZoomOptions): this;
    zoomTo(factor: number, options?: Omit<TransformManager.ZoomOptions, 'absolute'>): this;
    zoomToRect(rect: Rectangle.RectangleLike, options?: TransformManager.ScaleContentToFitOptions & TransformManager.ScaleContentToFitOptions): this;
    zoomToFit(options?: TransformManager.GetContentAreaOptions & TransformManager.ScaleContentToFitOptions): this;
    center(optons?: ScrollerImpl.CenterOptions): this;
    centerPoint(x: number, y: null | number, options?: ScrollerImpl.CenterOptions): this;
    centerPoint(x: null | number, y: number, options?: ScrollerImpl.CenterOptions): this;
    centerPoint(optons?: ScrollerImpl.CenterOptions): this;
    centerContent(options?: ScrollerImpl.PositionContentOptions): this;
    centerCell(cell: Cell, options?: ScrollerImpl.CenterOptions): this;
    positionPoint(point: Point.PointLike, x: number | string, y: number | string, options?: ScrollerImpl.CenterOptions): this;
    positionRect(rect: Rectangle.RectangleLike, direction: ScrollerImpl.Direction, options?: ScrollerImpl.CenterOptions): this;
    positionCell(cell: Cell, direction: ScrollerImpl.Direction, options?: ScrollerImpl.CenterOptions): this;
    positionContent(pos: ScrollerImpl.Direction, options?: ScrollerImpl.PositionContentOptions): this;
    drawBackground(options?: BackgroundManager.Options, onGraph?: boolean): this;
    clearBackground(onGraph?: boolean): this;
    isPannable(): boolean;
    enablePanning(): void;
    disablePanning(): void;
    togglePanning(pannable?: boolean): this;
    lockScroller(): this;
    unlockScroller(): this;
    updateScroller(): this;
    getScrollbarPosition(): {
        left: number;
        top: number;
    };
    setScrollbarPosition(left?: number, top?: number): this;
    scrollToPoint(x: number | null | undefined, y: number | null | undefined): this;
    scrollToContent(): this;
    scrollToCell(cell: Cell): this;
    transitionToPoint(p: Point.PointLike, options?: ScrollerImpl.TransitionOptions): this;
    transitionToPoint(x: number, y: number, options?: ScrollerImpl.TransitionOptions): this;
    transitionToRect(rect: Rectangle.RectangleLike, options?: ScrollerImpl.TransitionToRectOptions): this;
    enableAutoResize(): void;
    disableAutoResize(): void;
    autoScroll(clientX: number, clientY: number): {
        scrollerX: number;
        scrollerY: number;
    };
    clientToLocalPoint(x: number, y: number): Point;
    protected setup(): void;
    protected startListening(): void;
    protected stopListening(): void;
    protected onRightMouseDown(e: Dom.MouseDownEvent): void;
    protected preparePanning({ e }: {
        e: Dom.MouseDownEvent;
    }): void;
    protected allowPanning(e: Dom.MouseDownEvent, strict?: boolean): boolean;
    protected updateClassName(isPanning?: boolean): void;
    dispose(): void;
}
export declare namespace Scroller {
    export interface EventArgs extends ScrollerImpl.EventArgs {
    }
    type EventType = 'leftMouseDown' | 'rightMouseDown';
    interface ScrollerOptions extends ScrollerImpl.Options {
        pannable?: boolean | {
            enabled: boolean;
            eventTypes: EventType[];
        };
        modifiers?: string | ModifierKey[] | null;
    }
    export type Options = Omit<ScrollerOptions, 'graph'>;
    export {};
}
