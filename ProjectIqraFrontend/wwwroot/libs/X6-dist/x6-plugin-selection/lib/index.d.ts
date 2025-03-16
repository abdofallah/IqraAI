import { Basecoat, ModifierKey, Dom, Cell, EventArgs, Graph } from '@antv/x6';
import { SelectionImpl } from './selection';
import './api';
export declare class Selection extends Basecoat<SelectionImpl.EventArgs> implements Graph.Plugin {
    name: string;
    private graph;
    private selectionImpl;
    private readonly options;
    private movedMap;
    private unselectMap;
    get rubberbandDisabled(): boolean;
    get disabled(): boolean;
    get length(): number;
    get cells(): Cell<Cell.Properties>[];
    constructor(options?: Selection.Options);
    init(graph: Graph): void;
    isEnabled(): boolean;
    enable(): void;
    disable(): void;
    toggleEnabled(enabled?: boolean): this;
    isMultipleSelection(): boolean;
    enableMultipleSelection(): this;
    disableMultipleSelection(): this;
    toggleMultipleSelection(multiple?: boolean): this;
    isSelectionMovable(): boolean;
    enableSelectionMovable(): this;
    disableSelectionMovable(): this;
    toggleSelectionMovable(movable?: boolean): this;
    isRubberbandEnabled(): boolean;
    enableRubberband(): this;
    disableRubberband(): this;
    toggleRubberband(enabled?: boolean): this;
    isStrictRubberband(): boolean;
    enableStrictRubberband(): this;
    disableStrictRubberband(): this;
    toggleStrictRubberband(strict?: boolean): this;
    setRubberbandModifiers(modifiers?: string | ModifierKey[] | null): void;
    setSelectionFilter(filter?: Selection.Filter): this;
    setSelectionDisplayContent(content?: Selection.Content): this;
    isEmpty(): boolean;
    clean(options?: Selection.SetOptions): this;
    reset(cells?: Cell | string | (Cell | string)[], options?: Selection.SetOptions): this;
    getSelectedCells(): Cell<Cell.Properties>[];
    getSelectedCellCount(): number;
    isSelected(cell: Cell | string): boolean;
    select(cells: Cell | string | (Cell | string)[], options?: Selection.AddOptions): this;
    unselect(cells: Cell | string | (Cell | string)[], options?: Selection.RemoveOptions): this;
    protected setup(): void;
    protected startListening(): void;
    protected stopListening(): void;
    protected onBlankMouseDown({ e }: EventArgs['blank:mousedown']): void;
    protected allowBlankMouseDown(e: Dom.MouseDownEvent): boolean | undefined;
    protected onBlankClick(): void;
    protected allowRubberband(e: Dom.MouseDownEvent, strict?: boolean): boolean;
    protected allowMultipleSelection(e: Dom.MouseDownEvent | Dom.MouseUpEvent): boolean;
    protected onCellMouseMove({ cell }: EventArgs['cell:mousemove']): void;
    protected onCellMouseUp({ e, cell }: EventArgs['cell:mouseup']): void;
    protected onBoxMouseDown({ e, cell, }: SelectionImpl.EventArgs['box:mousedown']): void;
    protected getCells(cells: Cell | string | (Cell | string)[]): Cell<Cell.Properties>[];
    protected startRubberband(e: Dom.MouseDownEvent): this;
    protected isMultiple(): boolean;
    protected enableMultiple(): this;
    protected disableMultiple(): this;
    protected setModifiers(modifiers?: string | ModifierKey[] | null): this;
    protected setContent(content?: Selection.Content): this;
    protected setFilter(filter?: Selection.Filter): this;
    dispose(): void;
}
export declare namespace Selection {
    interface EventArgs extends SelectionImpl.EventArgs {
    }
    interface Options extends SelectionImpl.CommonOptions {
        enabled?: boolean;
    }
    type Filter = SelectionImpl.Filter;
    type Content = SelectionImpl.Content;
    type SetOptions = SelectionImpl.SetOptions;
    type AddOptions = SelectionImpl.AddOptions;
    type RemoveOptions = SelectionImpl.RemoveOptions;
    const defaultOptions: Partial<SelectionImpl.Options>;
}
