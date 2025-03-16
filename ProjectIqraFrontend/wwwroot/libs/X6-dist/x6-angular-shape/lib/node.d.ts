import { Node } from '@antv/x6';
export declare class AngularShape<Properties extends AngularShape.Properties = AngularShape.Properties> extends Node<Properties> {
}
export declare namespace AngularShape {
    type Primer = 'rect' | 'circle' | 'path' | 'ellipse' | 'polygon' | 'polyline';
    interface Properties extends Node.Properties {
        primer?: Primer;
    }
}
export declare namespace AngularShape {
}
