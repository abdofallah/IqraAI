import { Dom } from "@antv/x6-common";
import { Point, Rectangle } from "@antv/x6-geometry";
import { Graph } from "../graph";
import { Cell } from "../model/cell";
import { Edge } from "../model/edge";
import { Node } from "../model/node";
import { PortManager } from "../model/port";
import { Attr, PortLayout } from "../registry";
import { CellView } from "./cell";
import { EdgeView } from "./edge";
import { Markup } from "./markup";
export declare class NodeView<Entity extends Node = Node, Options extends NodeView.Options = NodeView.Options> extends CellView<Entity, Options> {
    protected portsCache: {
        [id: string]: NodeView.PortCache;
    };
    protected get [Symbol.toStringTag](): string;
    protected getContainerClassName(): string;
    protected updateClassName(e: Dom.MouseEnterEvent): void;
    isNodeView(): this is NodeView;
    confirmUpdate(flag: number, options?: any): number;
    update(partialAttrs?: Attr.CellAttrs): void;
    protected renderMarkup(): void;
    protected renderJSONMarkup(markup: Markup.JSONMarkup | Markup.JSONMarkup[]): void;
    render(): this;
    resize(): void;
    translate(): void;
    rotate(): void;
    protected getTranslationString(): string;
    protected getRotationString(): string | undefined;
    protected updateTransform(): void;
    findPortElem(portId?: string, selector?: string): Element | null;
    protected cleanPortsCache(): void;
    protected removePorts(): void;
    protected renderPorts(): void;
    protected appendPorts(ports: PortManager.Port[], zIndex: number, refs: Element[]): void;
    protected getPortElement(port: PortManager.Port): Element;
    protected createPortElement(port: PortManager.Port): Element;
    protected updatePorts(): void;
    protected updatePortGroup(groupName?: string): void;
    protected applyPortTransform(element: Element, layout: PortLayout.Result, initialAngle?: number): void;
    protected getPortMarkup(port: PortManager.Port): Markup;
    protected getPortLabelMarkup(label: PortManager.Label): Markup;
    protected existPortLabel(port: PortManager.Port): Attr.ComplexAttrs | undefined;
    protected getEventArgs<E>(e: E): NodeView.MouseEventArgs<E>;
    protected getEventArgs<E>(e: E, x: number, y: number): NodeView.PositionEventArgs<E>;
    protected getPortEventArgs<E>(e: E, port: string, pos?: {
        x: number;
        y: number;
    }): NodeView.PositionEventArgs<E> | NodeView.MouseEventArgs<E>;
    notifyMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    notifyMouseMove(e: Dom.MouseMoveEvent, x: number, y: number): void;
    notifyMouseUp(e: Dom.MouseUpEvent, x: number, y: number): void;
    notifyPortEvent(name: string, e: Dom.EventObject, pos?: {
        x: number;
        y: number;
    }): void;
    onClick(e: Dom.ClickEvent, x: number, y: number): void;
    onDblClick(e: Dom.DoubleClickEvent, x: number, y: number): void;
    onContextMenu(e: Dom.ContextMenuEvent, x: number, y: number): void;
    onMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    private isElementOrParentImmovable;
    onMouseMove(e: Dom.MouseMoveEvent, x: number, y: number): void;
    onMouseUp(e: Dom.MouseUpEvent, x: number, y: number): void;
    onMouseOver(e: Dom.MouseOverEvent): void;
    onMouseOut(e: Dom.MouseOutEvent): void;
    onMouseEnter(e: Dom.MouseEnterEvent): void;
    onMouseLeave(e: Dom.MouseLeaveEvent): void;
    onMouseWheel(e: Dom.EventObject, x: number, y: number, delta: number): void;
    onMagnetClick(e: Dom.MouseUpEvent, magnet: Element, x: number, y: number): void;
    onMagnetDblClick(e: Dom.DoubleClickEvent, magnet: Element, x: number, y: number): void;
    onMagnetContextMenu(e: Dom.ContextMenuEvent, magnet: Element, x: number, y: number): void;
    onMagnetMouseDown(e: Dom.MouseDownEvent, magnet: Element, x: number, y: number): void;
    onCustomEvent(e: Dom.MouseDownEvent, name: string, x: number, y: number): void;
    protected prepareEmbedding(e: Dom.MouseMoveEvent): void;
    processEmbedding(e: Dom.MouseMoveEvent, data: EventData.MovingTargetNode): void;
    clearEmbedding(data: EventData.MovingTargetNode): void;
    finalizeEmbedding(e: Dom.MouseUpEvent, data: EventData.MovingTargetNode): void;
    getDelegatedView(): NodeView<Node<Node.Properties>, NodeView.Options> | null;
    protected validateMagnet(cellView: CellView, magnet: Element, e: Dom.MouseDownEvent | Dom.MouseEnterEvent): boolean;
    protected startMagnetDragging(e: Dom.MouseDownEvent, x: number, y: number): void;
    protected startConnectting(e: Dom.MouseDownEvent, magnet: Element, x: number, y: number): void;
    protected getDefaultEdge(sourceView: CellView, sourceMagnet: Element): Edge<Edge.Properties>;
    protected createEdgeFromMagnet(magnet: Element, x: number, y: number): EdgeView<Edge<Edge.Properties>, EdgeView.Options>;
    protected dragMagnet(e: Dom.MouseMoveEvent, x: number, y: number): void;
    protected stopMagnetDragging(e: Dom.MouseUpEvent, x: number, y: number): void;
    protected notifyUnhandledMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    protected notifyNodeMove<Key extends keyof NodeView.EventArgs>(name: Key, e: Dom.MouseMoveEvent | Dom.MouseUpEvent, x: number, y: number, cell: Cell): void;
    protected getRestrictArea(view?: NodeView): Rectangle.RectangleLike | null;
    protected startNodeDragging(e: Dom.MouseDownEvent, x: number, y: number): void;
    protected dragNode(e: Dom.MouseMoveEvent, x: number, y: number): void;
    protected stopNodeDragging(e: Dom.MouseUpEvent, x: number, y: number): void;
    protected autoScrollGraph(x: number, y: number): void;
}
export declare namespace NodeView {
    interface Options extends CellView.Options {
    }
    interface PortCache {
        portElement: Element;
        portSelectors?: Markup.Selectors | null;
        portLabelElement?: Element;
        portLabelSelectors?: Markup.Selectors | null;
        portContentElement?: Element;
        portContentSelectors?: Markup.Selectors | null;
    }
}
export declare namespace NodeView {
    interface MagnetEventArgs {
        magnet: Element;
    }
    export interface MouseEventArgs<E> {
        e: E;
        node: Node;
        cell: Node;
        view: NodeView;
        port?: string;
    }
    export interface PositionEventArgs<E> extends MouseEventArgs<E>, CellView.PositionEventArgs {
    }
    export interface TranslateEventArgs<E> extends PositionEventArgs<E> {
    }
    export interface ResizeEventArgs<E> extends PositionEventArgs<E> {
    }
    export interface RotateEventArgs<E> extends PositionEventArgs<E> {
    }
    export interface EventArgs {
        "node:click": PositionEventArgs<Dom.ClickEvent>;
        "node:dblclick": PositionEventArgs<Dom.DoubleClickEvent>;
        "node:contextmenu": PositionEventArgs<Dom.ContextMenuEvent>;
        "node:mousedown": PositionEventArgs<Dom.MouseDownEvent>;
        "node:mousemove": PositionEventArgs<Dom.MouseMoveEvent>;
        "node:mouseup": PositionEventArgs<Dom.MouseUpEvent>;
        "node:mouseover": MouseEventArgs<Dom.MouseOverEvent>;
        "node:mouseout": MouseEventArgs<Dom.MouseOutEvent>;
        "node:mouseenter": MouseEventArgs<Dom.MouseEnterEvent>;
        "node:mouseleave": MouseEventArgs<Dom.MouseLeaveEvent>;
        "node:mousewheel": PositionEventArgs<Dom.EventObject> & CellView.MouseDeltaEventArgs;
        "node:port:click": PositionEventArgs<Dom.ClickEvent>;
        "node:port:dblclick": PositionEventArgs<Dom.DoubleClickEvent>;
        "node:port:contextmenu": PositionEventArgs<Dom.ContextMenuEvent>;
        "node:port:mousedown": PositionEventArgs<Dom.MouseDownEvent>;
        "node:port:mousemove": PositionEventArgs<Dom.MouseMoveEvent>;
        "node:port:mouseup": PositionEventArgs<Dom.MouseUpEvent>;
        "node:port:mouseover": MouseEventArgs<Dom.MouseOverEvent>;
        "node:port:mouseout": MouseEventArgs<Dom.MouseOutEvent>;
        "node:port:mouseenter": MouseEventArgs<Dom.MouseEnterEvent>;
        "node:port:mouseleave": MouseEventArgs<Dom.MouseLeaveEvent>;
        "node:customevent": PositionEventArgs<Dom.MouseDownEvent> & {
            name: string;
        };
        "node:unhandled:mousedown": PositionEventArgs<Dom.MouseDownEvent>;
        "node:highlight": {
            magnet: Element;
            view: NodeView;
            node: Node;
            cell: Node;
            options: CellView.HighlightOptions;
        };
        "node:unhighlight": EventArgs["node:highlight"];
        "node:magnet:click": PositionEventArgs<Dom.MouseUpEvent> & MagnetEventArgs;
        "node:magnet:dblclick": PositionEventArgs<Dom.DoubleClickEvent> & MagnetEventArgs;
        "node:magnet:contextmenu": PositionEventArgs<Dom.ContextMenuEvent> & MagnetEventArgs;
        "node:move": TranslateEventArgs<Dom.MouseMoveEvent>;
        "node:moving": TranslateEventArgs<Dom.MouseMoveEvent>;
        "node:moved": TranslateEventArgs<Dom.MouseUpEvent>;
        "node:embed": PositionEventArgs<Dom.MouseMoveEvent> & {
            currentParent: Node | null;
        };
        "node:embedding": PositionEventArgs<Dom.MouseMoveEvent> & {
            currentParent: Node | null;
            candidateParent: Node | null;
        };
        "node:embedded": PositionEventArgs<Dom.MouseUpEvent> & {
            currentParent: Node | null;
            previousParent: Node | null;
        };
    }
    export {};
}
export declare namespace NodeView {
    const toStringTag: string;
    function isNodeView(instance: any): instance is NodeView;
}
declare namespace EventData {
    type Mousemove = Moving | Magnet;
    interface Magnet {
        action: "magnet";
        targetMagnet: Element;
        edgeView?: EdgeView;
    }
    interface Moving {
        action: "move";
        targetView: NodeView;
    }
    interface MovingTargetNode {
        moving: boolean;
        offset: Point.PointLike;
        restrict?: Rectangle.RectangleLike | null;
        embedding?: boolean;
        candidateEmbedView?: NodeView | null;
        cell?: Node;
        graph?: Graph;
    }
}
export {};
