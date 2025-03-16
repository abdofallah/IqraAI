import { Base } from './base';
export declare class VirtualRenderManager extends Base {
    protected init(): void;
    protected startListening(): void;
    protected stopListening(): void;
    enableVirtualRender(): void;
    disableVirtualRender(): void;
    resetRenderArea(): void;
    dispose(): void;
}
