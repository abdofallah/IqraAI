import { Node } from '@antv/x6';
export declare class ReactShape<Properties extends ReactShape.Properties = ReactShape.Properties> extends Node<Properties> {
}
export declare namespace ReactShape {
    type Primer = 'rect' | 'circle' | 'path' | 'ellipse' | 'polygon' | 'polyline';
    interface Properties extends Node.Properties {
        primer?: Primer;
    }
}
export declare namespace ReactShape {
}
