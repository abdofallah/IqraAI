import { Disposable } from '@antv/x6-common';
import { Graph } from './graph';
export declare class Base extends Disposable {
    readonly graph: Graph;
    get options(): import("./options").Options.Definition;
    get model(): import("..").Model;
    get view(): Graph.View;
    constructor(graph: Graph);
    protected init(): void;
}
export declare namespace Base { }
