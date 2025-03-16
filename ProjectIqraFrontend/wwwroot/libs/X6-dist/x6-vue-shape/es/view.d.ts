import { NodeView, Dom } from '@antv/x6';
import { VueShape } from './node';
export declare class VueShapeView extends NodeView<VueShape> {
    private vm;
    getComponentContainer(): HTMLDivElement;
    confirmUpdate(flag: number): number;
    protected targetId(): string;
    protected renderVueComponent(): void;
    protected unmountVueComponent(): HTMLDivElement;
    onMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    unmount(): this;
}
export declare namespace VueShapeView {
    const action: any;
}
