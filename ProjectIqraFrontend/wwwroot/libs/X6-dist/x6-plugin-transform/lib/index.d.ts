import { Basecoat, KeyValue, Node, Graph, EventArgs } from '@antv/x6';
import { TransformImpl } from './transform';
import './api';
export declare class Transform extends Basecoat<Transform.EventArgs> implements Graph.Plugin {
    name: string;
    options: Transform.Options;
    private graph;
    protected widgets: Map<Node, TransformImpl>;
    private disabled;
    constructor(options?: Transform.Options);
    init(graph: Graph): void;
    protected startListening(): void;
    protected stopListening(): void;
    enable(): void;
    disable(): void;
    isEnabled(): boolean;
    createWidget(node: Node): void;
    protected onNodeClick({ node }: EventArgs['node:click']): void;
    protected onBlankMouseDown(): void;
    protected createTransform(node: Node): TransformImpl | null;
    protected getTransformOptions(node: Node): TransformImpl.Options;
    clearWidgets(): void;
    dispose(): void;
}
export declare namespace Transform {
    export interface EventArgs extends TransformImpl.EventArgs {
    }
    type OptionItem<T, S> = S | ((this: Graph, arg: T) => S);
    export interface ResizingRaw {
        enabled?: boolean;
        minWidth?: number;
        maxWidth?: number;
        minHeight?: number;
        maxHeight?: number;
        orthogonal?: boolean;
        restrict?: boolean | number;
        autoScroll?: boolean;
        preserveAspectRatio?: boolean;
        allowReverse?: boolean;
    }
    export interface RotatingRaw {
        enabled?: boolean;
        grid?: number;
    }
    export type Resizing = {
        [K in keyof ResizingRaw]?: OptionItem<Node, ResizingRaw[K]>;
    };
    export type Rotating = {
        [K in keyof RotatingRaw]?: OptionItem<Node, RotatingRaw[K]>;
    };
    export type Options = {
        rotating?: boolean | Partial<Rotating>;
        resizing?: boolean | Partial<Resizing>;
    };
    export function parseOptionGroup<K extends KeyValue, S extends KeyValue = KeyValue, T = any>(graph: Graph, arg: T, options: S): K;
    export {};
}
