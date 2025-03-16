import { Rectangle } from '@antv/x6-geometry';
import { Dom, Nilable, KeyValue } from '@antv/x6-common';
import { Registry } from '../registry/registry';
import { View } from './view';
import { Cache } from './cache';
import { Markup } from './markup';
import { ToolsView } from './tool';
import { AttrManager } from './attr';
import { FlagManager } from './flag';
import { Attr } from '../registry/attr';
import { Cell } from '../model/cell';
import { Edge } from '../model/edge';
import { Model } from '../model/model';
import { EdgeView } from './edge';
import { NodeView } from './node';
import { Graph } from '../graph';
export declare class CellView<Entity extends Cell = Cell, Options extends CellView.Options = CellView.Options> extends View<CellView.EventArgs> {
    protected static defaults: Partial<CellView.Options>;
    static getDefaults(): Partial<CellView.Options>;
    static config<T extends CellView.Options = CellView.Options>(options: Partial<T>): void;
    static getOptions<T extends CellView.Options = CellView.Options>(options: Partial<T>): T;
    graph: Graph;
    cell: Entity;
    protected selectors: Markup.Selectors;
    protected readonly options: Options;
    protected readonly flag: FlagManager;
    protected readonly attr: AttrManager;
    protected readonly cache: Cache;
    protected get [Symbol.toStringTag](): string;
    constructor(cell: Entity, options?: Partial<Options>);
    protected init(): void;
    protected onRemove(): void;
    get priority(): number;
    protected get rootSelector(): string;
    protected getConstructor<T extends CellView.Definition>(): T;
    protected ensureOptions(options: Partial<Options>): Options;
    protected getContainerTagName(): string;
    protected getContainerStyle(): Nilable<Record<string, string | number>> | void;
    protected getContainerAttrs(): Nilable<Attr.SimpleAttrs>;
    protected getContainerClassName(): Nilable<string | string[]>;
    protected ensureContainer(): SVGElement | HTMLElement;
    protected setContainer(container: Element): this;
    isNodeView(): this is NodeView;
    isEdgeView(): this is EdgeView;
    render(): this;
    confirmUpdate(flag: number, options?: any): number;
    getBootstrapFlag(): number;
    getFlag(actions: FlagManager.Actions): number;
    hasAction(flag: number, actions: FlagManager.Actions): number;
    removeAction(flag: number, actions: FlagManager.Actions): number;
    handleAction(flag: number, action: FlagManager.Action, handle: () => void, additionalRemovedActions?: FlagManager.Actions | null): number;
    protected setup(): void;
    protected onCellChanged({ options }: Cell.EventArgs['changed']): void;
    protected onAttrsChange(options: Cell.MutateOptions): void;
    parseJSONMarkup(markup: Markup.JSONMarkup | Markup.JSONMarkup[], rootElem?: Element): Markup.ParseResult;
    can(feature: CellView.InteractionNames): boolean;
    cleanCache(): this;
    getCache(elem: Element): Cache.Item;
    getDataOfElement(elem: Element): import("@antv/x6-common").JSONObject;
    getMatrixOfElement(elem: Element): DOMMatrix;
    getShapeOfElement(elem: SVGElement): import("@antv/x6-geometry").Path | Rectangle | import("@antv/x6-geometry").Line | import("@antv/x6-geometry").Polyline | import("@antv/x6-geometry").Ellipse;
    getBoundingRectOfElement(elem: Element): Rectangle;
    getBBoxOfElement(elem: Element): Rectangle;
    getUnrotatedBBoxOfElement(elem: SVGElement): Rectangle;
    getBBox(options?: {
        useCellGeometry?: boolean;
    }): Rectangle;
    getRootTranslatedMatrix(): DOMMatrix;
    getRootRotatedMatrix(): DOMMatrix;
    findMagnet(elem?: Element): Element | null;
    updateAttrs(rootNode: Element, attrs: Attr.CellAttrs, options?: Partial<AttrManager.UpdateOptions>): void;
    isEdgeElement(magnet?: Element | null): boolean;
    protected prepareHighlight(elem?: Element | null, options?: CellView.HighlightOptions): Element;
    highlight(elem?: Element | null, options?: CellView.HighlightOptions): this;
    unhighlight(elem?: Element | null, options?: CellView.HighlightOptions): this;
    notifyUnhighlight(magnet: Element, options: CellView.HighlightOptions): void;
    getEdgeTerminal(magnet: Element, x: number, y: number, edge: Edge, type: Edge.TerminalType): Edge.TerminalCellData;
    getMagnetFromEdgeTerminal(terminal: Edge.TerminalData): any;
    protected tools: ToolsView | null;
    hasTools(name?: string): boolean;
    addTools(options: ToolsView.Options | null): this;
    addTools(tools: ToolsView | null): this;
    updateTools(options?: ToolsView.UpdateOptions): this;
    removeTools(): this;
    hideTools(): this;
    showTools(): this;
    protected renderTools(): this;
    notify<Key extends keyof CellView.EventArgs>(name: Key, args: CellView.EventArgs[Key]): this;
    notify(name: Exclude<string, keyof CellView.EventArgs>, args: any): this;
    protected getEventArgs<E>(e: E): CellView.MouseEventArgs<E>;
    protected getEventArgs<E>(e: E, x: number, y: number): CellView.MousePositionEventArgs<E>;
    onClick(e: Dom.ClickEvent, x: number, y: number): void;
    onDblClick(e: Dom.DoubleClickEvent, x: number, y: number): void;
    onContextMenu(e: Dom.ContextMenuEvent, x: number, y: number): void;
    protected cachedModelForMouseEvent: Model | null;
    onMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    onMouseUp(e: Dom.MouseUpEvent, x: number, y: number): void;
    onMouseMove(e: Dom.MouseMoveEvent, x: number, y: number): void;
    onMouseOver(e: Dom.MouseOverEvent): void;
    onMouseOut(e: Dom.MouseOutEvent): void;
    onMouseEnter(e: Dom.MouseEnterEvent): void;
    onMouseLeave(e: Dom.MouseLeaveEvent): void;
    onMouseWheel(e: Dom.EventObject, x: number, y: number, delta: number): void;
    onCustomEvent(e: Dom.MouseDownEvent, name: string, x: number, y: number): void;
    onMagnetMouseDown(e: Dom.MouseDownEvent, magnet: Element, x: number, y: number): void;
    onMagnetDblClick(e: Dom.DoubleClickEvent, magnet: Element, x: number, y: number): void;
    onMagnetContextMenu(e: Dom.ContextMenuEvent, magnet: Element, x: number, y: number): void;
    onLabelMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    checkMouseleave(e: Dom.EventObject): void;
    dispose(): void;
}
export declare namespace CellView {
    export interface Options {
        graph: Graph;
        priority: number;
        isSvgElement: boolean;
        rootSelector: string;
        bootstrap: FlagManager.Actions;
        actions: KeyValue<FlagManager.Actions>;
        events?: View.Events | null;
        documentEvents?: View.Events | null;
    }
    type Interactable = boolean | ((this: Graph, cellView: CellView) => boolean);
    interface InteractionMap {
        edgeMovable?: Interactable;
        edgeLabelMovable?: Interactable;
        arrowheadMovable?: Interactable;
        vertexMovable?: Interactable;
        vertexAddable?: Interactable;
        vertexDeletable?: Interactable;
        useEdgeTools?: Interactable;
        nodeMovable?: Interactable;
        magnetConnectable?: Interactable;
        stopDelegateOnDragging?: Interactable;
        toolsAddable?: Interactable;
    }
    export type InteractionNames = keyof InteractionMap;
    export type Interacting = boolean | InteractionMap | ((this: Graph, cellView: CellView) => InteractionMap | boolean);
    export interface HighlightOptions {
        highlighter?: string | {
            name: string;
            args: KeyValue;
        };
        type?: 'embedding' | 'nodeAvailable' | 'magnetAvailable' | 'magnetAdsorbed';
        partial?: boolean;
    }
    export {};
}
export declare namespace CellView {
    interface PositionEventArgs {
        x: number;
        y: number;
    }
    interface MouseDeltaEventArgs {
        delta: number;
    }
    interface MouseEventArgs<E> {
        e: E;
        view: CellView;
        cell: Cell;
    }
    interface MousePositionEventArgs<E> extends MouseEventArgs<E>, PositionEventArgs {
    }
    interface EventArgs extends NodeView.EventArgs, EdgeView.EventArgs {
        'cell:click': MousePositionEventArgs<Dom.ClickEvent>;
        'cell:dblclick': MousePositionEventArgs<Dom.DoubleClickEvent>;
        'cell:contextmenu': MousePositionEventArgs<Dom.ContextMenuEvent>;
        'cell:mousedown': MousePositionEventArgs<Dom.MouseDownEvent>;
        'cell:mousemove': MousePositionEventArgs<Dom.MouseMoveEvent>;
        'cell:mouseup': MousePositionEventArgs<Dom.MouseUpEvent>;
        'cell:mouseover': MouseEventArgs<Dom.MouseOverEvent>;
        'cell:mouseout': MouseEventArgs<Dom.MouseOutEvent>;
        'cell:mouseenter': MouseEventArgs<Dom.MouseEnterEvent>;
        'cell:mouseleave': MouseEventArgs<Dom.MouseLeaveEvent>;
        'cell:mousewheel': MousePositionEventArgs<Dom.EventObject> & MouseDeltaEventArgs;
        'cell:customevent': MousePositionEventArgs<Dom.MouseDownEvent> & {
            name: string;
        };
        'cell:highlight': {
            magnet: Element;
            view: CellView;
            cell: Cell;
            options: CellView.HighlightOptions;
        };
        'cell:unhighlight': EventArgs['cell:highlight'];
    }
}
export declare namespace CellView {
    const Flag: typeof FlagManager;
    const Attr: typeof AttrManager;
}
export declare namespace CellView {
    const toStringTag: string;
    function isCellView(instance: any): instance is CellView;
}
export declare namespace CellView {
    function priority(value: number): (ctor: Definition) => void;
    function bootstrap(actions: FlagManager.Actions): (ctor: Definition) => void;
}
export declare namespace CellView {
    type CellViewClass = typeof CellView;
    export interface Definition extends CellViewClass {
        new <Entity extends Cell = Cell, Options extends CellView.Options = CellView.Options>(cell: Entity, options: Partial<Options>): CellView;
    }
    export const registry: Registry<Definition, KeyValue<Definition>, never>;
    export {};
}
