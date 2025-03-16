import { Node } from '@antv/x6';
export type VueShapeConfig = Node.Properties & {
    shape: string;
    component: any;
    inherit?: string;
};
export declare const shapeMaps: Record<string, {
    component: any;
}>;
export declare function register(config: VueShapeConfig): void;
