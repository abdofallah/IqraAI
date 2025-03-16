import { Rectangle, Point, Dom, Cell, Node, View, NodeView, Graph } from '@antv/x6';
export declare class Dnd extends View implements Graph.Plugin {
    name: string;
    protected sourceNode: Node | null;
    protected draggingNode: Node | null;
    protected draggingView: NodeView | null;
    protected draggingBBox: Rectangle;
    protected geometryBBox: Rectangle;
    protected candidateEmbedView: NodeView | null;
    protected delta: Point | null;
    protected padding: number | null;
    protected snapOffset: Point.PointLike | null;
    options: Dnd.Options;
    draggingGraph: Graph;
    protected get targetScroller(): any;
    protected get targetGraph(): Graph;
    protected get targetModel(): import("@antv/x6").Model;
    protected get snapline(): any;
    constructor(options: Partial<Dnd.Options> & {
        target: Graph;
    });
    init(): void;
    start(node: Node, evt: Dom.MouseDownEvent | MouseEvent): void;
    protected isSnaplineEnabled(): any;
    protected prepareDragging(sourceNode: Node, clientX: number, clientY: number): void;
    protected updateGraphPosition(clientX: number, clientY: number): void;
    protected updateNodePosition(x: number, y: number): Point;
    protected snap({ cell, current, options, }: Cell.EventArgs['change:position']): void;
    protected onDragging(evt: Dom.MouseMoveEvent): void;
    protected onDragEnd(evt: Dom.MouseUpEvent): void;
    protected clearDragging(): void;
    protected onDropped(draggingNode: Node): void;
    protected onDropInvalid(): void;
    protected isInsideValidArea(p: Point.PointLike): boolean;
    protected getDropArea(elem: Element): Rectangle;
    protected drop(draggingNode: Node, pos: Point.PointLike): Node<Node.Properties> | Promise<Node<Node.Properties> | null> | null;
    protected onRemove(): void;
    dispose(): void;
}
export declare namespace Dnd {
    interface Options {
        target: Graph;
        /**
         * Should scale the dragging node or not.
         */
        scaled?: boolean;
        delegateGraphOptions?: Graph.Options;
        draggingContainer?: HTMLElement;
        /**
         * dnd tool box container.
         */
        dndContainer?: HTMLElement;
        getDragNode: (sourceNode: Node, options: GetDragNodeOptions) => Node;
        getDropNode: (draggingNode: Node, options: GetDropNodeOptions) => Node;
        validateNode?: (droppingNode: Node, options: ValidateNodeOptions) => boolean | Promise<boolean>;
    }
    interface GetDragNodeOptions {
        sourceNode: Node;
        targetGraph: Graph;
        draggingGraph: Graph;
    }
    interface GetDropNodeOptions extends GetDragNodeOptions {
        draggingNode: Node;
    }
    interface ValidateNodeOptions extends GetDropNodeOptions {
        droppingNode: Node;
    }
    const defaults: Partial<Options>;
    const documentEvents: {
        mousemove: string;
        touchmove: string;
        mouseup: string;
        touchend: string;
        touchcancel: string;
    };
}
