import { Cell, ModifierKey } from '@antv/x6';
import { Selection } from './index';
import { SelectionImpl } from './selection';
declare module '@antv/x6/lib/graph/graph' {
    interface Graph {
        isSelectionEnabled: () => boolean;
        enableSelection: () => Graph;
        disableSelection: () => Graph;
        toggleSelection: (enabled?: boolean) => Graph;
        isMultipleSelection: () => boolean;
        enableMultipleSelection: () => Graph;
        disableMultipleSelection: () => Graph;
        toggleMultipleSelection: (multiple?: boolean) => Graph;
        isSelectionMovable: () => boolean;
        enableSelectionMovable: () => Graph;
        disableSelectionMovable: () => Graph;
        toggleSelectionMovable: (movable?: boolean) => Graph;
        isRubberbandEnabled: () => boolean;
        enableRubberband: () => Graph;
        disableRubberband: () => Graph;
        toggleRubberband: (enabled?: boolean) => Graph;
        isStrictRubberband: () => boolean;
        enableStrictRubberband: () => Graph;
        disableStrictRubberband: () => Graph;
        toggleStrictRubberband: (strict?: boolean) => Graph;
        setRubberbandModifiers: (modifiers?: string | ModifierKey[] | null) => Graph;
        setSelectionFilter: (filter?: Selection.Filter) => Graph;
        setSelectionDisplayContent: (content?: Selection.Content) => Graph;
        isSelectionEmpty: () => boolean;
        cleanSelection: (options?: Selection.SetOptions) => Graph;
        resetSelection: (cells?: Cell | string | (Cell | string)[], options?: Selection.SetOptions) => Graph;
        getSelectedCells: () => Cell[];
        getSelectedCellCount: () => number;
        isSelected: (cell: Cell | string) => boolean;
        select: (cells: Cell | string | (Cell | string)[], options?: Selection.AddOptions) => Graph;
        unselect: (cells: Cell | string | (Cell | string)[], options?: Selection.RemoveOptions) => Graph;
    }
}
declare module '@antv/x6/lib/graph/events' {
    interface EventArgs extends SelectionImpl.SelectionEventArgs {
    }
}
