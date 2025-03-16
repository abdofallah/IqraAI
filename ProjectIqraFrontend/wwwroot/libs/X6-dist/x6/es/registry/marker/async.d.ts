import { KeyValue } from '@antv/x6-common';
import { Marker } from './index';
export interface AsyncMarkerOptions extends KeyValue {
    width?: number;
    height?: number;
    offset?: number;
    open?: boolean;
    flip?: boolean;
}
export declare const async: Marker.Factory<AsyncMarkerOptions>;
