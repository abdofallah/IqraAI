import { Point, Rectangle } from '@antv/x6-geometry';
import { KeyValue } from '@antv/x6-common';
import { Registry } from '../registry';
import { Attr } from '../attr';
import * as layouts from './main';
export declare namespace PortLabelLayout {
    interface Result {
        position: Point.PointLike;
        angle: number;
        attrs: Attr.CellAttrs;
    }
    type Definition<T> = (portPosition: Point, elemBBox: Rectangle, args: T) => Result;
    type CommonDefinition = Definition<KeyValue>;
    interface CommonOptions {
        x?: number;
        y?: number;
        angle?: number;
        attrs?: Attr.CellAttrs;
    }
}
export declare namespace PortLabelLayout {
    type Presets = (typeof PortLabelLayout)['presets'];
    type OptionsMap = {
        readonly [K in keyof Presets]-?: Parameters<Presets[K]>[2];
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
export declare namespace PortLabelLayout {
    const presets: typeof layouts;
    const registry: Registry<CommonDefinition, typeof layouts, never>;
}
