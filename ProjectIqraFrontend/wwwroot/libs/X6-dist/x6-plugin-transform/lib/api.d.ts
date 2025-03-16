import { Node } from '@antv/x6';
import { TransformImpl } from './transform';
declare module '@antv/x6/lib/graph/graph' {
    interface Graph {
        createTransformWidget: (node: Node) => Graph;
        clearTransformWidgets: () => Graph;
    }
}
declare module '@antv/x6/lib/graph/events' {
    interface EventArgs extends TransformImpl.EventArgs {
    }
}
