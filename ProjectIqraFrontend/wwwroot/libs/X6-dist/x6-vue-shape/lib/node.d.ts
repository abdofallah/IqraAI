import { Node } from '@antv/x6';
export declare class VueShape<Properties extends VueShape.Properties = VueShape.Properties> extends Node<Properties> {
}
export declare namespace VueShape {
    type Primer = 'rect' | 'circle' | 'path' | 'ellipse' | 'polygon' | 'polyline';
    interface Properties extends Node.Properties {
        primer?: Primer;
    }
}
export declare namespace VueShape {
}
