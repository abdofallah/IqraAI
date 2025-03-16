import { Disposable, Graph, EventArgs } from '@antv/x6';
import { SnaplineImpl } from './snapline';
import './api';
export declare class Snapline extends Disposable implements Graph.Plugin {
    name: string;
    private snaplineImpl;
    options: Snapline.Options;
    constructor(options?: Snapline.Options);
    init(graph: Graph): void;
    isEnabled(): boolean;
    enable(): void;
    disable(): void;
    toggleEnabled(enabled?: boolean): this | undefined;
    hide(): this;
    setFilter(filter?: SnaplineImpl.Filter): this;
    isOnResizingEnabled(): boolean;
    enableOnResizing(): this;
    disableOnResizing(): this;
    toggleOnResizing(enableOnResizing?: boolean): this;
    isSharp(): boolean;
    enableSharp(): this;
    disableSharp(): this;
    toggleSharp(sharp?: boolean): this;
    getTolerance(): number | undefined;
    setTolerance(tolerance: number): this;
    captureCursorOffset(e: EventArgs['node:mousedown']): void;
    snapOnMoving(args: EventArgs['node:mousemove']): void;
    dispose(): void;
}
export declare namespace Snapline {
    interface Options extends SnaplineImpl.Options {
    }
    type Filter = SnaplineImpl.Filter;
}
