import React from 'react';
import { Graph, Node } from '@antv/x6';
export type ReactShapeConfig = Node.Properties & {
    shape: string;
    component: React.ComponentType<{
        node: Node;
        graph: Graph;
    }>;
    effect?: (keyof Node.Properties)[];
    inherit?: string;
};
export declare const shapeMaps: Record<string, {
    component: React.ComponentType<{
        node: Node;
        graph: Graph;
    }>;
    effect?: (keyof Node.Properties)[];
}>;
export declare function register(config: ReactShapeConfig): void;
