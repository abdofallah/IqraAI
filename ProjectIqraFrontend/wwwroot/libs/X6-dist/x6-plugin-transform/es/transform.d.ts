import { Dom, KeyValue, Node, Graph, View, NodeView } from '@antv/x6';
export declare class TransformImpl extends View<TransformImpl.EventArgs> {
    private node;
    private graph;
    private options;
    protected handle: Element | null;
    protected prevShift: number;
    container: HTMLElement;
    protected get model(): import("@antv/x6").Model;
    protected get view(): import("@antv/x6").CellView<import("@antv/x6").Cell<import("@antv/x6").Cell.Properties>, import("@antv/x6").CellView.Options>;
    protected get containerClassName(): string;
    protected get resizeClassName(): string;
    protected get rotateClassName(): string;
    constructor(options: TransformImpl.Options, node: Node, graph: Graph);
    protected startListening(): void;
    protected stopListening(): void;
    protected renderHandles(): void;
    render(): this;
    update(): this;
    remove(): this;
    protected onKnobMouseDown(): void;
    protected onKnobMouseUp(): void;
    protected updateResizerDirections(): void;
    protected getTrueDirection(dir: Node.ResizeDirection): Node.ResizeDirection;
    protected toValidResizeDirection(dir: string): Node.ResizeDirection;
    protected startResizing(evt: Dom.MouseDownEvent): void;
    protected prepareResizing(evt: Dom.EventObject, relativeDirection: Node.ResizeDirection): void;
    protected startRotating(evt: Dom.MouseDownEvent): void;
    protected onMouseMove(evt: Dom.MouseMoveEvent): void;
    protected onMouseUp(evt: Dom.MouseUpEvent): void;
    protected startHandle(handle?: Element | null): void;
    protected stopHandle(): void;
    protected startAction(evt: Dom.MouseDownEvent): void;
    protected stopAction(evt: Dom.MouseUpEvent): void;
    protected notify<K extends keyof TransformImpl.EventArgs, T extends Dom.EventObject>(name: K, evt: T, view: NodeView, args?: KeyValue): void;
    dispose(): void;
}
export declare namespace TransformImpl {
    interface ResizeEventArgs<E> extends NodeView.PositionEventArgs<E> {
    }
    interface RotateEventArgs<E> extends NodeView.PositionEventArgs<E> {
    }
    export interface EventArgs {
        'node:resize': ResizeEventArgs<Dom.MouseDownEvent>;
        'node:resizing': ResizeEventArgs<Dom.MouseMoveEvent>;
        'node:resized': ResizeEventArgs<Dom.MouseUpEvent>;
        'node:rotate': RotateEventArgs<Dom.MouseDownEvent>;
        'node:rotating': RotateEventArgs<Dom.MouseMoveEvent>;
        'node:rotated': RotateEventArgs<Dom.MouseUpEvent>;
    }
    export type Direction = 'nw' | 'n' | 'ne' | 'e' | 'se' | 's' | 'sw' | 'w';
    export interface Options {
        className?: string;
        minWidth?: number;
        maxWidth?: number;
        minHeight?: number;
        maxHeight?: number;
        resizable?: boolean;
        rotatable?: boolean;
        rotateGrid?: number;
        orthogonalResizing?: boolean;
        restrictedResizing?: boolean | number;
        autoScrollOnResizing?: boolean;
        /**
         * Set to `true` if you want the resizing to preserve the
         * aspect ratio of the node. Default is `false`.
         */
        preserveAspectRatio?: boolean;
        /**
         * Reaching the minimum width or height is whether to allow control points to reverse
         */
        allowReverse?: boolean;
    }
    export {};
}
