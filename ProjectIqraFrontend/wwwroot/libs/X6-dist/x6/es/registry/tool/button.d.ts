import { Point } from '@antv/x6-geometry';
import { Dom } from '@antv/x6-common';
import { CellView } from '../../view/cell';
import { NodeView } from '../../view/node';
import { EdgeView } from '../../view/edge';
import { ToolsView } from '../../view/tool';
import { Cell } from '../../model';
export declare class Button extends ToolsView.ToolItem<EdgeView | NodeView, Button.Options> {
    protected onRender(): void;
    update(): this;
    protected updatePosition(): void;
    protected getNodeMatrix(): DOMMatrix;
    protected getEdgeMatrix(): DOMMatrix;
    protected onMouseDown(e: Dom.MouseDownEvent): void;
}
export declare namespace Button {
    interface Options extends ToolsView.ToolItem.Options {
        x?: number | string;
        y?: number | string;
        distance?: number | string;
        offset?: number | Point.PointLike;
        rotate?: boolean;
        useCellGeometry?: boolean;
        onClick?: (this: CellView, args: {
            e: Dom.MouseDownEvent;
            cell: Cell;
            view: CellView;
            btn: Button;
        }) => any;
    }
}
export declare namespace Button {
}
export declare namespace Button {
    const Remove: typeof ToolsView.ToolItem;
}
