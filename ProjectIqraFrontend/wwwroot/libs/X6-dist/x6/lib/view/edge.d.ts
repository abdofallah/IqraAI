import { Rectangle, Point, Path, Line } from '@antv/x6-geometry';
import { Dom, KeyValue } from '@antv/x6-common';
import { NodeAnchor, ConnectionPoint } from '../registry';
import { Cell } from '../model/cell';
import { Edge } from '../model/edge';
import { Markup } from './markup';
import { CellView } from './cell';
import { Options as GraphOptions } from '../graph/options';
export declare class EdgeView<Entity extends Edge = Edge, Options extends EdgeView.Options = EdgeView.Options> extends CellView<Entity, Options> {
    protected readonly POINT_ROUNDING = 2;
    path: Path;
    routePoints: Point[];
    sourceAnchor: Point;
    targetAnchor: Point;
    sourcePoint: Point;
    targetPoint: Point;
    sourceMarkerPoint: Point;
    targetMarkerPoint: Point;
    sourceView: CellView | null;
    targetView: CellView | null;
    sourceMagnet: Element | null;
    targetMagnet: Element | null;
    protected labelContainer: Element | null;
    protected labelCache: {
        [index: number]: Element;
    };
    protected labelSelectors: {
        [index: number]: Markup.Selectors;
    };
    protected labelDestroyFn: {
        [index: number]: (args: GraphOptions.OnEdgeLabelRenderedArgs) => void;
    };
    protected get [Symbol.toStringTag](): string;
    protected getContainerClassName(): string;
    get sourceBBox(): Rectangle;
    get targetBBox(): Rectangle;
    isEdgeView(): this is EdgeView;
    confirmUpdate(flag: number, options?: any): number;
    render(): this;
    protected renderMarkup(): void;
    protected renderJSONMarkup(markup: Markup.JSONMarkup | Markup.JSONMarkup[]): void;
    protected customizeLabels(): void;
    protected destroyCustomizeLabels(): void;
    protected renderLabels(): this;
    onLabelsChange(options?: any): void;
    protected shouldRerenderLabels(options?: any): boolean;
    protected parseLabelMarkup(markup?: Markup): {
        fragment: DocumentFragment;
        selectors: {};
    } | null;
    protected parseLabelStringMarkup(labelMarkup: string): {
        fragment: DocumentFragment;
        selectors: {};
    };
    protected normalizeLabelMarkup(markup?: {
        fragment: DocumentFragment;
        selectors: Markup.Selectors;
    } | null): {
        node: SVGElement;
        selectors: Markup.Selectors;
    } | undefined;
    protected updateLabels(): void;
    protected renderTools(): this;
    update(options?: any): this;
    removeRedundantLinearVertices(options?: Edge.SetOptions): number;
    getTerminalView(type: Edge.TerminalType): CellView<Cell<Cell.Properties>, CellView.Options> | null;
    getTerminalAnchor(type: Edge.TerminalType): Point;
    getTerminalConnectionPoint(type: Edge.TerminalType): Point;
    getTerminalMagnet(type: Edge.TerminalType, options?: {
        raw?: boolean;
    }): Element | null;
    updateConnection(options?: any): void;
    protected findAnchors(vertices: Point.PointLike[]): {
        [x: string]: Point;
    };
    protected findAnchorsOrdered(firstType: Edge.TerminalType, firstPoint: Point.PointLike, secondType: Edge.TerminalType, secondPoint: Point.PointLike): {
        [x: string]: Point;
    };
    protected getAnchor(def: NodeAnchor.ManaualItem | string | undefined, cellView: CellView, magnet: Element | null, ref: Point | Element | null, terminalType: Edge.TerminalType): Point;
    protected findRoutePoints(vertices?: Point.PointLike[]): Point[];
    protected findConnectionPoints(routePoints: Point[], sourceAnchor: Point, targetAnchor: Point): {
        source: Point;
        target: Point;
    };
    protected getConnectionPoint(def: string | ConnectionPoint.ManaualItem | undefined, view: CellView, magnet: Element, line: Line, endType: Edge.TerminalType): Point;
    protected findMarkerPoints(routePoints: Point[], sourcePoint: Point, targetPoint: Point): {
        source: Point | undefined;
        target: Point | undefined;
    };
    protected findPath(routePoints: Point[], sourcePoint: Point, targetPoint: Point): Path;
    protected translateConnectionPoints(tx: number, ty: number): void;
    updateLabelPositions(): this;
    updateTerminalProperties(type: Edge.TerminalType): boolean;
    updateTerminalMagnet(type: Edge.TerminalType): void;
    protected getLabelPositionAngle(idx: number): number;
    protected getLabelPositionArgs(idx: number): Edge.LabelPositionOptions | undefined;
    protected getDefaultLabelPositionArgs(): Edge.LabelPositionOptions | undefined;
    protected mergeLabelPositionArgs(labelPositionArgs?: Edge.LabelPositionOptions, defaultLabelPositionArgs?: Edge.LabelPositionOptions): Edge.LabelPositionOptions | null | undefined;
    getConnection(): Path | null;
    getConnectionPathData(): string;
    getConnectionSubdivisions(): import("@antv/x6-geometry").Segment[][] | null | undefined;
    getConnectionLength(): number | undefined;
    getPointAtLength(length: number): Point | null;
    getPointAtRatio(ratio: number): Point | null;
    getTangentAtLength(length: number): Line | null;
    getTangentAtRatio(ratio: number): Line | null;
    getClosestPoint(point: Point.PointLike): Point | null;
    getClosestPointLength(point: Point.PointLike): number | null;
    getClosestPointRatio(point: Point.PointLike): number | null;
    getLabelPosition(x: number, y: number, options?: Edge.LabelPositionOptions | null): Edge.LabelPositionObject;
    getLabelPosition(x: number, y: number, angle: number, options?: Edge.LabelPositionOptions | null): Edge.LabelPositionObject;
    protected normalizeLabelPosition(): undefined;
    protected normalizeLabelPosition(pos: Edge.LabelPosition): Edge.LabelPositionObject;
    protected getLabelTransformationMatrix(labelPosition: Edge.LabelPosition): DOMMatrix;
    getVertexIndex(x: number, y: number): number;
    protected getEventArgs<E>(e: E): EdgeView.MouseEventArgs<E>;
    protected getEventArgs<E>(e: E, x: number, y: number): EdgeView.PositionEventArgs<E>;
    protected notifyUnhandledMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    notifyMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    notifyMouseMove(e: Dom.MouseMoveEvent, x: number, y: number): void;
    notifyMouseUp(e: Dom.MouseUpEvent, x: number, y: number): void;
    onClick(e: Dom.ClickEvent, x: number, y: number): void;
    onDblClick(e: Dom.DoubleClickEvent, x: number, y: number): void;
    onContextMenu(e: Dom.ContextMenuEvent, x: number, y: number): void;
    onMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    onMouseMove(e: Dom.MouseMoveEvent, x: number, y: number): KeyValue<any>;
    onMouseUp(e: Dom.MouseUpEvent, x: number, y: number): KeyValue<any>;
    onMouseOver(e: Dom.MouseOverEvent): void;
    onMouseOut(e: Dom.MouseOutEvent): void;
    onMouseEnter(e: Dom.MouseEnterEvent): void;
    onMouseLeave(e: Dom.MouseLeaveEvent): void;
    onMouseWheel(e: Dom.EventObject, x: number, y: number, delta: number): void;
    onCustomEvent(e: Dom.MouseDownEvent, name: string, x: number, y: number): void;
    onLabelMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    protected startEdgeDragging(e: Dom.MouseDownEvent, x: number, y: number): void;
    protected dragEdge(e: Dom.MouseMoveEvent, x: number, y: number): void;
    protected stopEdgeDragging(e: Dom.MouseUpEvent, x: number, y: number): void;
    prepareArrowheadDragging(type: Edge.TerminalType, options: {
        x: number;
        y: number;
        options?: KeyValue;
        isNewEdge?: boolean;
        fallbackAction?: EventData.ArrowheadDragging['fallbackAction'];
    }): EventData.ArrowheadDragging;
    protected createValidateConnectionArgs(type: Edge.TerminalType): (cellView: CellView, magnet: Element) => EventData.ValidateConnectionArgs;
    protected beforeArrowheadDragging(data: EventData.ArrowheadDragging): void;
    protected afterArrowheadDragging(data: EventData.ArrowheadDragging): void;
    protected validateConnection(sourceView: CellView | null | undefined, sourceMagnet: Element | null | undefined, targetView: CellView | null | undefined, targetMagnet: Element | null | undefined, terminalType: Edge.TerminalType, edgeView?: EdgeView | null | undefined, candidateTerminal?: Edge.TerminalCellData | null | undefined): boolean;
    protected allowConnectToBlank(edge: Edge): boolean;
    protected validateEdge(edge: Edge, type: Edge.TerminalType, initialTerminal: Edge.TerminalData): boolean;
    protected arrowheadDragging(target: Element, x: number, y: number, data: EventData.ArrowheadDragging): void;
    protected arrowheadDragged(data: EventData.ArrowheadDragging, x: number, y: number): void;
    protected snapArrowhead(x: number, y: number, data: EventData.ArrowheadDragging): void;
    protected snapArrowheadEnd(data: EventData.ArrowheadDragging): void;
    protected finishEmbedding(data: EventData.ArrowheadDragging): void;
    protected fallbackConnection(data: EventData.ArrowheadDragging): void;
    protected notifyConnectionEvent(data: EventData.ArrowheadDragging, e: Dom.MouseUpEvent): void;
    protected highlightAvailableMagnets(data: EventData.ArrowheadDragging): void;
    protected unhighlightAvailableMagnets(data: EventData.ArrowheadDragging): void;
    protected startArrowheadDragging(e: Dom.MouseDownEvent, x: number, y: number): void;
    protected dragArrowhead(e: Dom.MouseMoveEvent, x: number, y: number): void;
    protected stopArrowheadDragging(e: Dom.MouseUpEvent, x: number, y: number): void;
    startLabelDragging(e: Dom.MouseDownEvent, x: number, y: number): void;
    dragLabel(e: Dom.MouseMoveEvent, x: number, y: number): void;
    stopLabelDragging(e: Dom.MouseUpEvent, x: number, y: number): void;
}
export declare namespace EdgeView {
    interface Options extends CellView.Options {
    }
}
export declare namespace EdgeView {
    interface MouseEventArgs<E> {
        e: E;
        edge: Edge;
        cell: Edge;
        view: EdgeView;
    }
    interface PositionEventArgs<E> extends MouseEventArgs<E>, CellView.PositionEventArgs {
    }
    interface EventArgs {
        'edge:click': PositionEventArgs<Dom.ClickEvent>;
        'edge:dblclick': PositionEventArgs<Dom.DoubleClickEvent>;
        'edge:contextmenu': PositionEventArgs<Dom.ContextMenuEvent>;
        'edge:mousedown': PositionEventArgs<Dom.MouseDownEvent>;
        'edge:mousemove': PositionEventArgs<Dom.MouseMoveEvent>;
        'edge:mouseup': PositionEventArgs<Dom.MouseUpEvent>;
        'edge:mouseover': MouseEventArgs<Dom.MouseOverEvent>;
        'edge:mouseout': MouseEventArgs<Dom.MouseOutEvent>;
        'edge:mouseenter': MouseEventArgs<Dom.MouseEnterEvent>;
        'edge:mouseleave': MouseEventArgs<Dom.MouseLeaveEvent>;
        'edge:mousewheel': PositionEventArgs<Dom.EventObject> & CellView.MouseDeltaEventArgs;
        'edge:customevent': EdgeView.PositionEventArgs<Dom.MouseDownEvent> & {
            name: string;
        };
        'edge:unhandled:mousedown': PositionEventArgs<Dom.MouseDownEvent>;
        'edge:connected': {
            e: Dom.MouseUpEvent;
            edge: Edge;
            view: EdgeView;
            isNew: boolean;
            type: Edge.TerminalType;
            previousCell?: Cell | null;
            previousView?: CellView | null;
            previousPort?: string | null;
            previousPoint?: Point.PointLike | null;
            previousMagnet?: Element | null;
            currentCell?: Cell | null;
            currentView?: CellView | null;
            currentPort?: string | null;
            currentPoint?: Point.PointLike | null;
            currentMagnet?: Element | null;
        };
        'edge:highlight': {
            magnet: Element;
            view: EdgeView;
            edge: Edge;
            cell: Edge;
            options: CellView.HighlightOptions;
        };
        'edge:unhighlight': EventArgs['edge:highlight'];
        'edge:move': PositionEventArgs<Dom.MouseMoveEvent>;
        'edge:moving': PositionEventArgs<Dom.MouseMoveEvent>;
        'edge:moved': PositionEventArgs<Dom.MouseUpEvent>;
    }
}
export declare namespace EdgeView {
    const toStringTag: string;
    function isEdgeView(instance: any): instance is EdgeView;
}
declare namespace EventData {
    interface MousemoveEventData {
    }
    interface EdgeDragging {
        action: 'drag-edge';
        moving: boolean;
        x: number;
        y: number;
    }
    type ValidateConnectionArgs = [
        CellView | null | undefined,
        // source view
        Element | null | undefined,
        // source magnet
        CellView | null | undefined,
        // target view
        Element | null | undefined,
        Edge.TerminalType,
        EdgeView
    ];
    interface ArrowheadDragging {
        action: 'drag-arrowhead';
        x: number;
        y: number;
        isNewEdge: boolean;
        terminalType: Edge.TerminalType;
        fallbackAction: 'remove' | 'revert';
        initialMagnet: Element | null;
        initialTerminal: Edge.TerminalData;
        getValidateConnectionArgs: (cellView: CellView, magnet: Element | null) => ValidateConnectionArgs;
        zIndex?: number | null;
        pointerEvents?: string | null;
        /**
         * Current event target.
         */
        currentTarget?: Element;
        /**
         * Current view under pointer.
         */
        currentView?: CellView | null;
        /**
         * Current magnet under pointer.
         */
        currentMagnet?: Element | null;
        closestView?: CellView | null;
        closestMagnet?: Element | null;
        marked?: KeyValue<Element[]> | null;
        options?: KeyValue;
    }
    interface LabelDragging {
        action: 'drag-label';
        index: number;
        positionAngle: number;
        positionArgs?: Edge.LabelPositionOptions | null;
        stopPropagation: true;
    }
}
export {};
