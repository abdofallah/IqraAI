import { ValuesType } from 'utility-types';
import { KeyValue } from '@antv/x6-common';
import { Registry } from '../registry';
export declare namespace Background {
    interface Options {
        color?: string;
        image?: string;
        position?: Background.BackgroundPosition<{
            x: number;
            y: number;
        }>;
        size?: Background.BackgroundSize<{
            width: number;
            height: number;
        }>;
        repeat?: Background.BackgroundRepeat;
        opacity?: number;
    }
    interface CommonOptions extends Omit<Options, 'repeat'> {
        quality?: number;
    }
    type Definition<T extends CommonOptions = CommonOptions> = (img: HTMLImageElement, options: T) => HTMLCanvasElement;
}
export declare namespace Background {
    type Presets = (typeof Background)['presets'];
    type OptionsMap = {
        readonly [K in keyof Presets]-?: Parameters<Presets[K]>[1] & {
            repeat: K;
        };
    };
    type NativeNames = keyof Presets;
    type NativeItem = ValuesType<OptionsMap>;
    type ManaualItem = CommonOptions & KeyValue & {
        repeat: string;
    };
}
export declare namespace Background {
    const presets: {
        [name: string]: Definition;
    };
    const registry: Registry<Definition<CommonOptions>, {
        [name: string]: Definition<CommonOptions>;
    }, never>;
}
export declare namespace Background {
    type Globals = '-moz-initial' | 'inherit' | 'initial' | 'revert' | 'unset';
    type BgPosition<TLength> = TLength | 'bottom' | 'center' | 'left' | 'right' | 'top' | (string & {});
    type BgSize<TLength> = TLength | 'auto' | 'contain' | 'cover' | (string & {});
    type RepeatStyle = 'no-repeat' | 'repeat' | 'repeat-x' | 'repeat-y' | 'round' | 'space' | (string & {});
    export type BackgroundPosition<TLength = (string & {}) | 0> = Globals | BgPosition<TLength> | (string & {});
    export type BackgroundSize<TLength = (string & {}) | 0> = Globals | BgSize<TLength> | (string & {});
    export type BackgroundRepeat = Globals | RepeatStyle | (string & {});
    export interface Padding {
        left: number;
        top: number;
        right: number;
        bottom: number;
    }
    export {};
}
