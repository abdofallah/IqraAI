import { Point } from '@antv/x6-geometry';
import { KeyValue } from '@antv/x6-common';
import { Registry } from '../registry';
import { Edge } from '../../model';
import { CellView } from '../../view';
import * as strategies from './main';
import { Graph } from '../../graph';
export declare namespace ConnectionStrategy {
    type Definition = (this: Graph, terminal: Edge.TerminalCellData, cellView: CellView, magnet: Element, coords: Point.PointLike, edge: Edge, type: Edge.TerminalType, options: KeyValue) => Edge.TerminalCellData;
}
export declare namespace ConnectionStrategy {
    type Presets = (typeof ConnectionStrategy)['presets'];
    type NativeNames = keyof Presets;
    interface NativeItem<T extends NativeNames = NativeNames> {
        name: T;
        args?: KeyValue;
    }
    interface ManaualItem {
        name: Exclude<string, NativeNames>;
        args?: KeyValue;
    }
}
export declare namespace ConnectionStrategy {
    const presets: typeof strategies;
    const registry: Registry<Definition, typeof strategies, never>;
}
