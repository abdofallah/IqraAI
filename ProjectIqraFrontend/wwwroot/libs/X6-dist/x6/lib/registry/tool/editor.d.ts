import { Dom } from '@antv/x6-common';
import { ToolsView } from '../../view/tool';
import { Cell } from '../../model';
import { CellView, NodeView, EdgeView } from '../../view';
export declare class CellEditor extends ToolsView.ToolItem<NodeView | EdgeView, CellEditor.CellEditorOptions & {
    event: Dom.EventObject;
}> {
    private editor;
    private labelIndex;
    private distance;
    private event;
    private dblClick;
    onRender(): void;
    createElement(): void;
    removeElement(): void;
    updateEditor(): this | undefined;
    updateNodeEditorTransform(): void;
    updateEdgeEditorTransform(): this | undefined;
    onDocumentMouseUp(e: Dom.MouseDownEvent): void;
    onCellDblClick({ e }: {
        e: Dom.DoubleClickEvent;
    }): void;
    onMouseDown(e: Dom.MouseDownEvent): void;
    autoFocus(): void;
    selectText(): void;
    getCellText(): any;
    setCellText(value: string | null): void;
    protected onRemove(): void;
}
export declare namespace CellEditor {
    interface CellEditorOptions extends ToolsView.ToolItem.Options {
        x?: number | string;
        y?: number | string;
        width?: number;
        height?: number;
        attrs: {
            fontSize: number;
            fontFamily: string;
            color: string;
            backgroundColor: string;
        };
        labelAddable?: boolean;
        getText: ((this: CellView, args: {
            cell: Cell;
            index?: number;
        }) => string) | string;
        setText: ((this: CellView, args: {
            cell: Cell;
            value: string | null;
            index?: number;
            distance?: number;
        }) => void) | string;
    }
}
export declare namespace CellEditor {
}
export declare namespace CellEditor {
    const NodeEditor: typeof ToolsView.ToolItem;
    const EdgeEditor: typeof ToolsView.ToolItem;
}
