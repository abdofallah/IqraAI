import { IDisablable, Vector, Point, Rectangle, Graph, EventArgs, Model, Node, CellView, NodeView, View } from '@antv/x6';
export declare class SnaplineImpl extends View implements IDisablable {
    readonly options: SnaplineImpl.Options;
    protected readonly graph: Graph;
    protected offset: Point.PointLike;
    protected timer: number | null;
    container: SVGElement;
    protected containerWrapper: Vector;
    protected horizontal: Vector;
    protected vertical: Vector;
    protected get model(): Model;
    protected get containerClassName(): string;
    protected get verticalClassName(): string;
    protected get horizontalClassName(): string;
    constructor(options: SnaplineImpl.Options & {
        graph: Graph;
    });
    get disabled(): boolean;
    enable(): void;
    disable(): void;
    setFilter(filter?: SnaplineImpl.Filter): void;
    protected render(): void;
    protected startListening(): void;
    protected stopListening(): void;
    protected onBatchStop({ name, data }: Model.EventArgs['batch:stop']): void;
    captureCursorOffset({ view, x, y }: EventArgs['node:mousedown']): void;
    protected isNodeMovable(view: CellView): boolean;
    protected getRestrictArea(view?: NodeView): Rectangle.RectangleLike | null;
    protected snapOnResizing(node: Node, options: Node.ResizeOptions): void;
    snapOnMoving({ view, e, x, y }: EventArgs['node:mousemove']): void;
    protected isIgnored(snapNode: Node, targetNode: Node): boolean;
    protected filter(node: Node): boolean;
    protected update(metadata: {
        verticalLeft?: number;
        verticalTop?: number;
        verticalHeight?: number;
        horizontalTop?: number;
        horizontalLeft?: number;
        horizontalWidth?: number;
    }): void;
    protected resetTimer(): void;
    show(): this;
    hide(): this;
    protected onRemove(): void;
    dispose(): void;
}
export declare namespace SnaplineImpl {
    interface Options {
        enabled?: boolean;
        className?: string;
        tolerance?: number;
        sharp?: boolean;
        /**
         * Specify if snap on node resizing or not.
         */
        resizing?: boolean;
        clean?: boolean | number;
        filter?: Filter;
    }
    type Filter = null | (string | {
        id: string;
    })[] | FilterFunction;
    type FilterFunction = (this: Graph, node: Node) => boolean;
}
