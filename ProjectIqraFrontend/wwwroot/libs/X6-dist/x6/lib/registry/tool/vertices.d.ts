import { Point } from '@antv/x6-geometry';
import { Dom, ModifierKey } from '@antv/x6-common';
import { View } from '../../view/view';
import { ToolsView } from '../../view/tool';
import { EdgeView } from '../../view/edge';
import { Edge } from '../../model/edge';
import { Attr } from '../attr';
import { Graph } from '../../graph';
export declare class Vertices extends ToolsView.ToolItem<EdgeView, Vertices.Options> {
    protected handles: Vertices.Handle[];
    protected get vertices(): any[];
    protected onRender(): this;
    update(): this;
    protected resetHandles(): void;
    protected renderHandles(): void;
    protected updateHandles(): void;
    protected updatePath(): void;
    protected startHandleListening(handle: Vertices.Handle): void;
    protected stopHandleListening(handle: Vertices.Handle): void;
    protected getNeighborPoints(index: number): {
        prev: Point;
        next: Point;
    };
    protected getMouseEventArgs<T extends Dom.EventObject>(evt: T): {
        e: T;
        x: number;
        y: number;
    };
    protected onHandleChange({ e }: Vertices.Handle.EventArgs['change']): void;
    protected onHandleChanging({ handle, e, }: Vertices.Handle.EventArgs['changing']): void;
    protected stopBatch(vertexAdded: boolean): void;
    protected onHandleChanged({ e }: Vertices.Handle.EventArgs['changed']): void;
    protected snapVertex(vertex: Point.PointLike, index: number): void;
    protected onHandleRemove({ handle, e }: Vertices.Handle.EventArgs['remove']): void;
    protected allowAddVertex(e: Dom.MouseDownEvent): boolean | undefined;
    protected onPathMouseDown(evt: Dom.MouseDownEvent): void;
    protected onRemove(): void;
}
export declare namespace Vertices {
    interface Options extends ToolsView.ToolItem.Options {
        snapRadius?: number;
        addable?: boolean;
        removable?: boolean;
        removeRedundancies?: boolean;
        stopPropagation?: boolean;
        modifiers?: string | ModifierKey[];
        attrs?: Attr.SimpleAttrs | ((handle: Handle) => Attr.SimpleAttrs);
        createHandle?: (options: Handle.Options) => Handle;
        processHandle?: (handle: Handle) => void;
        onChanged?: (options: {
            edge: Edge;
            edgeView: EdgeView;
        }) => void;
    }
}
export declare namespace Vertices {
    class Handle extends View<Handle.EventArgs> {
        readonly options: Handle.Options;
        protected get graph(): Graph;
        constructor(options: Handle.Options);
        render(): void;
        updatePosition(x: number, y: number): void;
        onMouseDown(evt: Dom.MouseDownEvent): void;
        protected onMouseMove(evt: Dom.MouseMoveEvent): void;
        protected onMouseUp(evt: Dom.MouseUpEvent): void;
        protected onDoubleClick(evt: Dom.DoubleClickEvent): void;
    }
    namespace Handle {
        interface Options {
            graph: Graph;
            index: number;
            guard: (evt: Dom.EventObject) => boolean;
            attrs: Attr.SimpleAttrs | ((handle: Handle) => Attr.SimpleAttrs);
        }
        interface EventArgs {
            change: {
                e: Dom.MouseDownEvent;
                handle: Handle;
            };
            changing: {
                e: Dom.MouseMoveEvent;
                handle: Handle;
            };
            changed: {
                e: Dom.MouseUpEvent;
                handle: Handle;
            };
            remove: {
                e: Dom.DoubleClickEvent;
                handle: Handle;
            };
        }
    }
}
export declare namespace Vertices {
}
