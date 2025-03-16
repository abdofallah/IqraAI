import { Graph } from '@antv/x6';
import { VueShape } from './node';
export declare function connect(id: string, component: any, container: HTMLDivElement, node: VueShape, graph: Graph): void;
export declare function disconnect(id: string): void;
export declare function isActive(): boolean;
export declare function getTeleport(): any;
