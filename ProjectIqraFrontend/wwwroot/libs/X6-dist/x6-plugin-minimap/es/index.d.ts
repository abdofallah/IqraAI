import { Dom, View, Graph, EventArgs } from '@antv/x6';
export declare class MiniMap extends View implements Graph.Plugin {
    name: string;
    private graph;
    readonly options: MiniMap.Options;
    container: HTMLDivElement;
    protected zoomHandle: HTMLDivElement;
    protected viewport: HTMLElement;
    protected sourceGraph: Graph;
    protected targetGraph: Graph;
    protected geometry: Util.ViewGeometry;
    protected ratio: number;
    private targetGraphTransforming;
    protected get scroller(): any;
    protected get graphContainer(): any;
    constructor(options: Partial<MiniMap.Options>);
    init(graph: Graph): void;
    protected startListening(): void;
    protected stopListening(): void;
    protected onRemove(): void;
    protected onTransform(options: {
        ui: boolean;
    }): void;
    protected onModelUpdated(): void;
    protected updatePaper(width: number, height: number): this;
    protected updatePaper({ width, height }: EventArgs['resize']): this;
    protected updateViewport(): void;
    protected startAction(evt: Dom.MouseDownEvent): void;
    protected doAction(evt: Dom.MouseMoveEvent): void;
    protected stopAction(): void;
    protected scrollTo(evt: Dom.MouseDownEvent): void;
    dispose(): void;
}
export declare namespace MiniMap {
    interface Options {
        container: HTMLElement;
        width: number;
        height: number;
        padding: number;
        scalable?: boolean;
        minScale?: number;
        maxScale?: number;
        createGraph?: (options: Graph.Options) => Graph;
        graphOptions?: Graph.Options;
    }
}
declare namespace Util {
    const defaultOptions: Partial<MiniMap.Options>;
    const documentEvents: {
        mousemove: string;
        touchmove: string;
        mouseup: string;
        touchend: string;
    };
    interface ViewGeometry extends Record<string, number> {
        top: number;
        left: number;
        width: number;
        height: number;
    }
    interface EventData {
        frameId?: number;
        action: 'zooming' | 'panning';
        clientX: number;
        clientY: number;
        scrollLeft: number;
        scrollTop: number;
        zoom: number;
        scale: {
            sx: number;
            sy: number;
        };
        geometry: ViewGeometry;
        translateX: number;
        translateY: number;
    }
}
export {};
