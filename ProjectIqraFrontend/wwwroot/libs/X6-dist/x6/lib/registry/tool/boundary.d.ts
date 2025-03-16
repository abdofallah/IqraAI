import { NumberExt } from '@antv/x6-common';
import { Attr } from '../attr';
import { NodeView } from '../../view/node';
import { EdgeView } from '../../view/edge';
import { ToolsView } from '../../view/tool';
export declare class Boundary extends ToolsView.ToolItem<EdgeView | NodeView, Boundary.Options> {
    protected onRender(): void;
    update(): this;
}
export declare namespace Boundary {
    interface Options extends ToolsView.ToolItem.Options {
        padding?: NumberExt.SideOptions;
        rotate?: boolean;
        useCellGeometry?: boolean;
        attrs?: Attr.SimpleAttrs;
    }
}
export declare namespace Boundary {
}
