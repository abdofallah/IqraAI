import { KeyValue, Disposable } from '@antv/x6-common';
import { Rectangle } from '@antv/x6-geometry';
import { Model, Cell } from '../model';
import { View, CellView, EdgeView } from '../view';
import { JOB_PRIORITY } from './queueJob';
import { Graph } from '../graph';
export declare class Scheduler extends Disposable {
    views: KeyValue<Scheduler.View>;
    willRemoveViews: KeyValue<Scheduler.View>;
    protected zPivots: KeyValue<Comment>;
    private graph;
    private renderArea?;
    private queue;
    get model(): Model;
    get container(): SVGGElement;
    constructor(graph: Graph);
    protected init(): void;
    protected startListening(): void;
    protected stopListening(): void;
    protected onModelReseted({ options }: Model.EventArgs['reseted']): void;
    protected onCellAdded({ cell, options }: Model.EventArgs['cell:added']): void;
    protected onCellRemoved({ cell }: Model.EventArgs['cell:removed']): void;
    protected onCellZIndexChanged({ cell, options, }: Model.EventArgs['cell:change:zIndex']): void;
    protected onCellVisibleChanged({ cell, current, }: Model.EventArgs['cell:change:visible']): void;
    requestViewUpdate(view: CellView, flag: number, options?: any, priority?: JOB_PRIORITY, flush?: boolean): void;
    setRenderArea(area?: Rectangle): void;
    isViewMounted(view: CellView): boolean;
    protected renderViews(cells: Cell[], options?: any): void;
    protected renderViewInArea(view: CellView, flag: number, options?: any): void;
    protected removeViews(cells: Cell[]): void;
    protected flush(): void;
    protected flushWaitingViews(): void;
    protected updateView(view: View, flag: number, options?: any): number;
    protected insertView(view: CellView): void;
    protected resetViews(): void;
    protected removeView(view: CellView): void;
    protected toggleVisible(cell: Cell, visible: boolean): void;
    protected addZPivot(zIndex?: number): Comment;
    protected removeZPivots(): void;
    protected createCellView(cell: Cell): CellView<Cell<Cell.Properties>, CellView.Options> | EdgeView<import("../model").Edge<import("../model").Edge.Properties>, EdgeView.Options> | null;
    protected getEffectedEdges(view: CellView): {
        id: string;
        view: CellView;
        flag: number;
    }[];
    protected isUpdatable(view: CellView): boolean;
    protected getRenderPriority(view: CellView): JOB_PRIORITY;
    dispose(): void;
}
export declare namespace Scheduler {
    const FLAG_INSERT: number;
    const FLAG_REMOVE: number;
    const FLAG_RENDER: number;
}
export declare namespace Scheduler {
    enum ViewState {
        CREATED = 0,
        MOUNTED = 1,
        WAITING = 2
    }
    interface View {
        view: CellView;
        flag: number;
        options: any;
        state: ViewState;
    }
    interface EventArgs {
        'view:mounted': {
            view: CellView;
        };
        'view:unmounted': {
            view: CellView;
        };
        'render:done': null;
    }
}
