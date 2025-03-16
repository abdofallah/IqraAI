import { Dom } from '@antv/x6-common';
import { Cell } from '../model';
import { View, Markup, CellView } from '../view';
import { Graph } from '../graph';
export declare class GraphView extends View {
    protected readonly graph: Graph;
    readonly container: HTMLElement;
    readonly background: HTMLDivElement;
    readonly grid: HTMLDivElement;
    readonly svg: SVGSVGElement;
    readonly defs: SVGDefsElement;
    readonly viewport: SVGGElement;
    readonly primer: SVGGElement;
    readonly stage: SVGGElement;
    readonly decorator: SVGGElement;
    readonly overlay: SVGGElement;
    private restore;
    /** Graph's `this.container` is from outer, should not dispose */
    protected get disposeContainer(): boolean;
    protected get options(): import("./options").Options.Definition;
    constructor(graph: Graph);
    delegateEvents(): this;
    /**
     * Guard the specified event. If the event is not interesting, it
     * returns `true`, otherwise returns `false`.
     */
    guard(e: Dom.EventObject, view?: CellView | null): any;
    protected findView(elem: Element): CellView<Cell<Cell.Properties>, CellView.Options> | null;
    protected onDblClick(evt: Dom.DoubleClickEvent): void;
    protected onClick(evt: Dom.ClickEvent): void;
    protected isPreventDefaultContextMenu(view: CellView | null): boolean;
    protected onContextMenu(evt: Dom.ContextMenuEvent): void;
    delegateDragEvents(e: Dom.MouseDownEvent, view: CellView | null): void;
    getMouseMovedCount(e: Dom.EventObject): number;
    protected onMouseDown(evt: Dom.MouseDownEvent): void;
    protected onMouseMove(evt: Dom.MouseMoveEvent): void;
    protected onMouseUp(e: Dom.MouseUpEvent): void;
    protected onMouseOver(evt: Dom.MouseOverEvent): void;
    protected onMouseOut(evt: Dom.MouseOutEvent): void;
    protected onMouseEnter(evt: Dom.MouseEnterEvent): void;
    protected onMouseLeave(evt: Dom.MouseLeaveEvent): void;
    protected onMouseWheel(evt: Dom.EventObject): void;
    protected onCustomEvent(evt: Dom.MouseDownEvent): void;
    protected handleMagnetEvent<T extends Dom.EventObject>(evt: T, handler: (this: Graph, view: CellView, e: T, magnet: Element, x: number, y: number) => void): void;
    protected onMagnetMouseDown(e: Dom.MouseDownEvent): void;
    protected onMagnetDblClick(e: Dom.DoubleClickEvent): void;
    protected onMagnetContextMenu(e: Dom.ContextMenuEvent): void;
    protected onLabelMouseDown(evt: Dom.MouseDownEvent): void;
    protected onImageDragStart(): boolean;
    dispose(): void;
}
export declare namespace GraphView {
    type SortType = 'none' | 'approx' | 'exact';
}
export declare namespace GraphView {
    const markup: Markup.JSONMarkup[];
    function snapshoot(elem: Element): () => void;
}
export declare namespace GraphView {
    const events: {
        [x: string]: string;
        dblclick: string;
        contextmenu: string;
        touchstart: string;
        mousedown: string;
        mouseover: string;
        mouseout: string;
        mouseenter: string;
        mouseleave: string;
        mousewheel: string;
        DOMMouseScroll: string;
    };
    const documentEvents: {
        mousemove: string;
        touchmove: string;
        mouseup: string;
        touchend: string;
        touchcancel: string;
    };
}
