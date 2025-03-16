import { Rectangle, Point, ModifierKey, Dom, KeyValue, Cell, Node, Edge, Model, Collection, View, CellView, Graph } from '@antv/x6';
export declare class SelectionImpl extends View<SelectionImpl.EventArgs> {
    readonly options: SelectionImpl.Options;
    protected readonly collection: Collection;
    protected selectionContainer: HTMLElement;
    protected selectionContent: HTMLElement;
    protected boxCount: number;
    protected boxesUpdated: boolean;
    get graph(): Graph;
    protected get boxClassName(): string;
    protected get $boxes(): Element[];
    protected get handleOptions(): SelectionImpl.Options;
    constructor(options: SelectionImpl.Options);
    protected startListening(): void;
    protected stopListening(): void;
    protected onRemove(): void;
    protected onGraphTransformed(): void;
    protected onCellChanged(): void;
    protected translating: boolean;
    protected onNodePositionChanged({ node, options, }: Collection.EventArgs['node:change:position']): void;
    protected onModelUpdated({ removed }: Collection.EventArgs['updated']): void;
    isEmpty(): boolean;
    isSelected(cell: Cell | string): boolean;
    get length(): number;
    get cells(): Cell<Cell.Properties>[];
    select(cells: Cell | Cell[], options?: SelectionImpl.AddOptions): this;
    unselect(cells: Cell | Cell[], options?: SelectionImpl.RemoveOptions): this;
    reset(cells?: Cell | Cell[], options?: SelectionImpl.SetOptions): this;
    clean(options?: SelectionImpl.SetOptions): this;
    setFilter(filter?: SelectionImpl.Filter): void;
    setContent(content?: SelectionImpl.Content): void;
    startSelecting(evt: Dom.MouseDownEvent): void;
    filter(cells: Cell[]): Cell<Cell.Properties>[];
    protected stopSelecting(evt: Dom.MouseUpEvent): void;
    protected onMouseUp(evt: Dom.MouseUpEvent): void;
    protected onSelectionBoxMouseDown(evt: Dom.MouseDownEvent): void;
    protected startTranslating(evt: Dom.MouseDownEvent): void;
    private getRestrictArea;
    protected getSelectionOffset(client: Point, data: EventData.Translating): {
        dx: number;
        dy: number;
    };
    private updateElementPosition;
    protected updateSelectedNodesPosition(offset: {
        dx: number;
        dy: number;
    }): void;
    protected autoScrollGraph(x: number, y: number): any;
    protected adjustSelection(evt: Dom.MouseMoveEvent): void;
    protected translateSelectedNodes(dx: number, dy: number, exclude?: Cell, otherOptions?: KeyValue): void;
    protected getCellViewsInArea(rect: Rectangle): CellView<Cell<Cell.Properties>, CellView.Options>[];
    protected notifyBoxEvent<K extends keyof SelectionImpl.BoxEventArgs, T extends Dom.EventObject>(name: K, e: T, x: number, y: number): void;
    protected getSelectedClassName(cell: Cell): string;
    protected addCellSelectedClassName(cell: Cell): void;
    protected removeCellUnSelectedClassName(cell: Cell): void;
    protected destroySelectionBox(cell: Cell): void;
    protected destroyAllSelectionBoxes(cells: Cell[]): void;
    hide(): void;
    protected showRubberband(): void;
    protected hideRubberband(): void;
    protected showSelected(): void;
    protected createContainer(): void;
    protected updateContainerPosition(offset: {
        dx: number;
        dy: number;
    }): void;
    protected updateContainer(): void;
    protected canShowSelectionBox(cell: Cell): boolean;
    protected getPointerEventsValue(pointerEvents: 'none' | 'auto' | ((cells: Cell[]) => 'none' | 'auto')): "none" | "auto";
    protected createSelectionBox(cell: Cell): void;
    protected updateSelectionBoxes(): void;
    confirmUpdate(): number;
    protected getCellViewFromElem(elem: Element): CellView<Cell<Cell.Properties>, CellView.Options> | null;
    protected onCellRemoved({ cell }: Collection.EventArgs['removed']): void;
    protected onReseted({ previous, current }: Collection.EventArgs['reseted']): void;
    protected onCellAdded({ cell }: Collection.EventArgs['added']): void;
    protected listenCellRemoveEvent(cell: Cell): void;
    protected onCollectionUpdated({ added, removed, options, }: Collection.EventArgs['updated']): void;
    dispose(): void;
}
export declare namespace SelectionImpl {
    type SelectionEventType = 'leftMouseDown' | 'mouseWheelDown';
    export interface CommonOptions {
        model?: Model;
        collection?: Collection;
        className?: string;
        strict?: boolean;
        filter?: Filter;
        modifiers?: string | ModifierKey[] | null;
        multiple?: boolean;
        multipleSelectionModifiers?: string | ModifierKey[] | null;
        selectCellOnMoved?: boolean;
        selectNodeOnMoved?: boolean;
        selectEdgeOnMoved?: boolean;
        showEdgeSelectionBox?: boolean;
        showNodeSelectionBox?: boolean;
        movable?: boolean;
        following?: boolean;
        content?: Content;
        rubberband?: boolean;
        rubberNode?: boolean;
        rubberEdge?: boolean;
        pointerEvents?: 'none' | 'auto' | ((cells: Cell[]) => 'none' | 'auto');
        eventTypes?: SelectionEventType[];
    }
    export interface Options extends CommonOptions {
        graph: Graph;
    }
    export type Content = null | false | string | ((this: Graph, selection: SelectionImpl, contentElement: HTMLElement) => string);
    export type Filter = null | (string | {
        id: string;
    })[] | ((this: Graph, cell: Cell) => boolean);
    export interface SetOptions extends Collection.SetOptions {
        batch?: boolean;
    }
    export interface AddOptions extends Collection.AddOptions {
    }
    export interface RemoveOptions extends Collection.RemoveOptions {
    }
    export {};
}
export declare namespace SelectionImpl {
    interface SelectionBoxEventArgs<T> {
        e: T;
        view: CellView;
        cell: Cell;
        x: number;
        y: number;
    }
    export interface BoxEventArgs {
        'box:mousedown': SelectionBoxEventArgs<Dom.MouseDownEvent>;
        'box:mousemove': SelectionBoxEventArgs<Dom.MouseMoveEvent>;
        'box:mouseup': SelectionBoxEventArgs<Dom.MouseUpEvent>;
    }
    export interface SelectionEventArgs {
        'cell:selected': {
            cell: Cell;
            options: Model.SetOptions;
        };
        'node:selected': {
            cell: Cell;
            node: Node;
            options: Model.SetOptions;
        };
        'edge:selected': {
            cell: Cell;
            edge: Edge;
            options: Model.SetOptions;
        };
        'cell:unselected': {
            cell: Cell;
            options: Model.SetOptions;
        };
        'node:unselected': {
            cell: Cell;
            node: Node;
            options: Model.SetOptions;
        };
        'edge:unselected': {
            cell: Cell;
            edge: Edge;
            options: Model.SetOptions;
        };
        'selection:changed': {
            added: Cell[];
            removed: Cell[];
            selected: Cell[];
            options: Model.SetOptions;
        };
    }
    export interface EventArgs extends BoxEventArgs, SelectionEventArgs {
    }
    export {};
}
declare namespace EventData {
    interface Common {
        action: 'selecting' | 'translating';
    }
    interface Selecting extends Common {
        action: 'selecting';
        moving?: boolean;
        clientX: number;
        clientY: number;
        offsetX: number;
        offsetY: number;
        scrollerX: number;
        scrollerY: number;
    }
    interface Translating extends Common {
        action: 'translating';
        clientX: number;
        clientY: number;
        originX: number;
        originY: number;
    }
    interface SelectionBox {
        activeView: CellView;
    }
    interface Rotation {
        rotated?: boolean;
        center: Point.PointLike;
        start: number;
        angles: {
            [id: string]: number;
        };
    }
    interface Resizing {
        resized?: boolean;
        bbox: Rectangle;
        cells: Cell[];
        minWidth: number;
        minHeight: number;
    }
}
export {};
