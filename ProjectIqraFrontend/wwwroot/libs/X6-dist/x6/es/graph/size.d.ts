import { Base } from './base';
export declare class SizeManager extends Base {
    private getScroller;
    private getContainer;
    private getSensorTarget;
    protected init(): void;
    resize(width?: number, height?: number): void;
    dispose(): void;
}
