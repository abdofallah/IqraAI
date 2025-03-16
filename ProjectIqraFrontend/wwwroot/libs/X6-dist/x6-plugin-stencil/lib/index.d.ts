import { Dom, Cell, Node, Model, View, Graph, EventArgs } from '@antv/x6';
import { Dnd } from '@antv/x6-plugin-dnd';
export declare class Stencil extends View implements Graph.Plugin {
    name: string;
    options: Stencil.Options;
    dnd: Dnd;
    protected graphs: {
        [groupName: string]: Graph;
    };
    protected groups: {
        [groupName: string]: HTMLElement;
    };
    protected content: HTMLDivElement;
    protected get targetScroller(): any;
    protected get targetGraph(): Graph;
    protected get targetModel(): Model;
    constructor(options?: Partial<Stencil.Options>);
    init(): void;
    load(groups: {
        [groupName: string]: (Node | Node.Metadata)[];
    }): this;
    load(nodes: (Node | Node.Metadata)[], groupName?: string): this;
    unload(groups: {
        [groupName: string]: (Node | Node.Metadata)[];
    }): this;
    unload(nodes: (Node | Node.Metadata)[], groupName?: string): this;
    toggleGroup(groupName: string): this;
    collapseGroup(groupName: string): this;
    expandGroup(groupName: string): this;
    isGroupCollapsable(groupName: string): boolean;
    isGroupCollapsed(groupName: string): boolean;
    collapseGroups(): this;
    expandGroups(): this;
    resizeGroup(groupName: string, size: {
        width: number;
        height: number;
    }): this;
    addGroup(group: Stencil.Group | Stencil.Group[]): void;
    removeGroup(groupName: string | string[]): void;
    protected initContainer(): void;
    protected initContent(): void;
    protected initSearch(): void;
    protected initGroup(group: Stencil.Group): void;
    protected initGroups(): void;
    protected setCollapsableState(): void;
    protected setTitle(): void;
    protected renderSearch(): HTMLDivElement;
    protected startListening(): void;
    protected stopListening(): void;
    protected registerGraphEvents(graph: Graph): void;
    protected unregisterGraphEvents(graph: Graph): void;
    protected loadGroup(cells: (Node | Node.Metadata)[], groupName?: string, reverse?: boolean): this;
    protected onDragStart(args: EventArgs['node:mousedown']): void;
    protected filter(keyword: string, filter?: Stencil.Filter): void;
    protected isCellMatched(cell: Cell, keyword: string, filters: Stencil.Filters | undefined, ignoreCase: boolean): boolean;
    protected onSearch(evt: Dom.EventObject): void;
    protected onSearchFocusIn(): void;
    protected onSearchFocusOut(): void;
    protected onTitleClick(): void;
    protected onGroupTitleClick(evt: Dom.EventObject): void;
    protected getModel(groupName?: string): Model | null;
    protected getGraph(groupName?: string): Graph;
    protected getGroup(groupName?: string): Stencil.Group | null | undefined;
    protected getGroupByNode(node: Node): Stencil.Group | null | undefined;
    protected clearGroups(): void;
    protected onRemove(): void;
    dispose(): void;
}
export declare namespace Stencil {
    interface Options extends Dnd.Options {
        title: string;
        groups?: Group[];
        search?: Filter;
        placeholder?: string;
        notFoundText?: string;
        collapsable?: boolean;
        stencilGraphWidth: number;
        stencilGraphHeight: number;
        stencilGraphOptions?: Graph.Options;
        stencilGraphPadding?: number;
        layout?: (this: Stencil, model: Model, group?: Group | null) => any;
        layoutOptions?: any;
    }
    type Filter = Filters | FilterFn | boolean;
    type Filters = {
        [shape: string]: string | string[] | boolean;
    };
    type FilterFn = (this: Stencil, cell: Node, keyword: string, groupName: string | null, stencil: Stencil) => boolean;
    interface Group {
        name: string;
        title?: string;
        collapsed?: boolean;
        collapsable?: boolean;
        nodeMovable?: boolean;
        graphWidth?: number;
        graphHeight?: number;
        graphPadding?: number;
        graphOptions?: Graph.Options;
        layout?: (this: Stencil, model: Model, group?: Group | null) => any;
        layoutOptions?: any;
    }
    const defaultOptions: Partial<Options>;
}
