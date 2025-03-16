import { NumberExt, Dom, Point, Rectangle, Cell, View, Graph, EventArgs, TransformManager, BackgroundManager } from '@antv/x6';
export declare class ScrollerImpl extends View<ScrollerImpl.EventArgs> {
    private readonly content;
    protected pageBreak: HTMLDivElement | null;
    readonly options: ScrollerImpl.Options;
    readonly container: HTMLDivElement;
    readonly background: HTMLDivElement;
    readonly backgroundManager: ScrollerImpl.Background;
    get graph(): Graph;
    private get model();
    protected sx: number;
    protected sy: number;
    protected clientX: number;
    protected clientY: number;
    protected padding: {
        left: number;
        top: number;
        right: number;
        bottom: number;
    };
    protected cachedScrollLeft: number | null;
    protected cachedScrollTop: number | null;
    protected cachedCenterPoint: Point.PointLike | null;
    protected cachedClientSize: {
        width: number;
        height: number;
    } | null;
    protected delegatedHandlers: {
        [name: string]: (...args: any) => any;
    };
    constructor(options: ScrollerImpl.Options);
    protected startListening(): void;
    protected stopListening(): void;
    enableAutoResize(): void;
    disableAutoResize(): void;
    protected onUpdate(): void;
    protected delegateBackgroundEvents(events?: View.Events): void;
    protected undelegateBackgroundEvents(): void;
    protected onBackgroundEvent(e: Dom.EventObject): void;
    protected onResize(): void;
    protected onScale({ sx, sy, ox, oy }: EventArgs['scale']): void;
    protected storeScrollPosition(): void;
    protected restoreScrollPosition(): void;
    protected storeClientSize(): void;
    protected restoreClientSize(): void;
    protected beforeManipulation(): void;
    protected afterManipulation(): void;
    updatePageSize(width?: number, height?: number): void;
    protected updatePageBreak(): void;
    update(): void;
    protected getFitToContentOptions(options: TransformManager.FitToContentFullOptions): Graph.TransformManager.FitToContentFullOptions;
    protected updateScale(sx: number, sy: number): void;
    scrollbarPosition(): {
        left: number;
        top: number;
    };
    scrollbarPosition(left?: number, top?: number): this;
    /**
     * Try to scroll to ensure that the position (x,y) on the graph (in local
     * coordinates) is at the center of the viewport. If only one of the
     * coordinates is specified, only scroll in the specified dimension and
     * keep the other coordinate unchanged.
     */
    scrollToPoint(x: number | null | undefined, y: number | null | undefined): this;
    /**
     * Try to scroll to ensure that the center of graph content is at the
     * center of the viewport.
     */
    scrollToContent(): this;
    /**
     * Try to scroll to ensure that the center of cell is at the center of
     * the viewport.
     */
    scrollToCell(cell: Cell): this;
    /**
     * The center methods are more aggressive than the scroll methods. These
     * methods position the graph so that a specific point on the graph lies
     * at the center of the viewport, adding paddings around the paper if
     * necessary (e.g. if the requested point lies in a corner of the paper).
     * This means that the requested point will always move into the center
     * of the viewport. (Use the scroll functions to avoid adding paddings
     * and only scroll the viewport as far as the graph boundary.)
     */
    /**
     * Position the center of graph to the center of the viewport.
     */
    center(optons?: ScrollerImpl.CenterOptions): this;
    /**
     * Position the point (x,y) on the graph (in local coordinates) to the
     * center of the viewport. If only one of the coordinates is specified,
     * only center along the specified dimension and keep the other coordinate
     * unchanged.
     */
    centerPoint(x: number, y: null | number, options?: ScrollerImpl.CenterOptions): this;
    centerPoint(x: null | number, y: number, options?: ScrollerImpl.CenterOptions): this;
    centerPoint(optons?: ScrollerImpl.CenterOptions): this;
    centerContent(options?: ScrollerImpl.PositionContentOptions): this;
    centerCell(cell: Cell, options?: ScrollerImpl.CenterOptions): this;
    /**
     * The position methods are a more general version of the center methods.
     * They position the graph so that a specific point on the graph lies at
     * requested coordinates inside the viewport.
     */
    /**
     *
     */
    positionContent(pos: ScrollerImpl.Direction, options?: ScrollerImpl.PositionContentOptions): this;
    positionCell(cell: Cell, pos: ScrollerImpl.Direction, options?: ScrollerImpl.CenterOptions): this;
    positionRect(rect: Rectangle.RectangleLike, pos: ScrollerImpl.Direction, options?: ScrollerImpl.CenterOptions): this;
    positionPoint(point: Point.PointLike, x: number | string, y: number | string, options?: ScrollerImpl.CenterOptions): this;
    zoom(): number;
    zoom(factor: number, options?: TransformManager.ZoomOptions): this;
    zoomToRect(rect: Rectangle.RectangleLike, options?: TransformManager.ScaleContentToFitOptions): this;
    zoomToFit(options?: TransformManager.GetContentAreaOptions & TransformManager.ScaleContentToFitOptions): this;
    transitionToPoint(p: Point.PointLike, options?: ScrollerImpl.TransitionOptions): this;
    transitionToPoint(x: number, y: number, options?: ScrollerImpl.TransitionOptions): this;
    protected syncTransition(scale: number, p: Point.PointLike): this;
    protected removeTransition(): this;
    transitionToRect(rectangle: Rectangle.RectangleLike, options?: ScrollerImpl.TransitionToRectOptions): this;
    startPanning(evt: Dom.MouseDownEvent): void;
    pan(evt: Dom.MouseMoveEvent): void;
    stopPanning(e: Dom.MouseUpEvent): void;
    clientToLocalPoint(p: Point.PointLike): Point;
    clientToLocalPoint(x: number, y: number): Point;
    localToBackgroundPoint(p: Point.PointLike): Point;
    localToBackgroundPoint(x: number, y: number): Point;
    resize(width?: number, height?: number): void;
    getClientSize(): {
        width: number;
        height: number;
    };
    autoScroll(clientX: number, clientY: number): {
        scrollerX: number;
        scrollerY: number;
    };
    protected addPadding(left?: number, right?: number, top?: number, bottom?: number): this;
    protected getPadding(): {
        top: number;
        right: number;
        bottom: number;
        left: number;
    };
    /**
     * Returns the untransformed size and origin of the current viewport.
     */
    getVisibleArea(): Rectangle;
    isCellVisible(cell: Cell, options?: {
        strict?: boolean;
    }): boolean;
    isPointVisible(point: Point.PointLike): boolean;
    /**
     * Lock the current viewport by disabling user scrolling.
     */
    lock(): this;
    /**
     * Enable user scrolling if previously locked.
     */
    unlock(): this;
    protected onRemove(): void;
    dispose(): void;
}
export declare namespace ScrollerImpl {
    interface EventArgs {
        'pan:start': {
            e: Dom.MouseDownEvent;
        };
        panning: {
            e: Dom.MouseMoveEvent;
        };
        'pan:stop': {
            e: Dom.MouseUpEvent;
        };
    }
    interface Options {
        graph: Graph;
        enabled?: boolean;
        className?: string;
        width?: number;
        height?: number;
        pageWidth?: number;
        pageHeight?: number;
        pageVisible?: boolean;
        pageBreak?: boolean;
        minVisibleWidth?: number;
        minVisibleHeight?: number;
        background?: false | BackgroundManager.Options;
        autoResize?: boolean;
        padding?: NumberExt.SideOptions | ((this: ScrollerImpl, scroller: ScrollerImpl) => NumberExt.SideOptions);
        autoResizeOptions?: TransformManager.FitToContentFullOptions | ((this: ScrollerImpl, scroller: ScrollerImpl) => TransformManager.FitToContentFullOptions);
    }
    interface CenterOptions {
        padding?: NumberExt.SideOptions;
    }
    type PositionContentOptions = TransformManager.GetContentAreaOptions & ScrollerImpl.CenterOptions;
    type Direction = 'center' | 'top' | 'top-right' | 'top-left' | 'right' | 'bottom-right' | 'bottom' | 'bottom-left' | 'left';
    interface TransitionOptions {
        /**
         * The zoom level to reach at the end of the transition.
         */
        scale?: number;
        duration?: string;
        delay?: string;
        timing?: string;
        onTransitionEnd?: (this: ScrollerImpl, e: TransitionEvent) => void;
    }
    interface TransitionToRectOptions extends TransitionOptions {
        minScale?: number;
        maxScale?: number;
        scaleGrid?: number;
        visibility?: number;
        center?: Point.PointLike;
    }
    type AutoResizeDirection = 'top' | 'right' | 'bottom' | 'left';
}
export declare namespace ScrollerImpl {
    class Background extends BackgroundManager {
        protected readonly scroller: ScrollerImpl;
        protected get elem(): HTMLDivElement;
        constructor(scroller: ScrollerImpl);
        protected init(): void;
        protected updateBackgroundOptions(options?: BackgroundManager.Options): void;
    }
}
export declare namespace ScrollerImpl {
    const containerClass = "graph-scroller";
    const panningClass: string;
    const pannableClass: string;
    const pagedClass: string;
    const contentClass: string;
    const backgroundClass: string;
    const transitionClassName = "transition-in-progress";
    const transitionEventName = "transitionend.graph-scroller-transition";
    const defaultOptions: Partial<ScrollerImpl.Options>;
    function getOptions(options: ScrollerImpl.Options): Options;
}
