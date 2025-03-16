import { Point, Path } from '@antv/x6-geometry';
import { KeyValue } from '@antv/x6-common';
import { Registry } from '../registry';
import { EdgeView } from '../../view';
import * as connectors from './main';
export declare namespace Connector {
    interface BaseOptions {
        raw?: boolean;
    }
    type Definition<T extends BaseOptions = BaseOptions> = (this: EdgeView, sourcePoint: Point.PointLike, targetPoint: Point.PointLike, routePoints: Point.PointLike[], options: T, edgeView: EdgeView) => Path | string;
}
export declare namespace Connector {
    type Presets = (typeof Connector)['presets'];
    type OptionsMap = {
        readonly [K in keyof Presets]-?: Parameters<Presets[K]>[3];
    };
    type NativeNames = keyof Presets;
    interface NativeItem<T extends NativeNames = NativeNames> {
        name: T;
        args?: OptionsMap[T];
    }
    interface ManaualItem {
        name: Exclude<string, NativeNames>;
        args?: KeyValue;
    }
}
export declare namespace Connector {
    const presets: typeof connectors;
    const registry: Registry<Definition<BaseOptions>, typeof connectors, never>;
}
