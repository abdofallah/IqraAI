import { Point } from '@antv/x6-geometry';
import { KeyValue } from '@antv/x6-common';
import { Registry } from '../registry';
import { EdgeView } from '../../view';
import * as routers from './main';
export declare namespace Router {
    type Definition<T> = (this: EdgeView, vertices: Point.PointLike[], options: T, edgeView: EdgeView) => Point.PointLike[];
    type CommonDefinition = Definition<KeyValue>;
}
export declare namespace Router {
    type Presets = (typeof Router)['presets'];
    type OptionsMap = {
        readonly [K in keyof Presets]-?: Parameters<Presets[K]>[1];
    };
    type NativeNames = keyof OptionsMap;
    interface NativeItem<T extends NativeNames = NativeNames> {
        name: T;
        args?: OptionsMap[T];
    }
    interface ManaualItem {
        name: Exclude<string, NativeNames>;
        args?: KeyValue;
    }
}
export declare namespace Router {
    const presets: typeof routers;
    const registry: Registry<CommonDefinition, typeof routers, never>;
}
