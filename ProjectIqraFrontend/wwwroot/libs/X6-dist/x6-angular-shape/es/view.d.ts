import { Dom, NodeView } from '@antv/x6';
import { AngularShape } from './node';
export declare class AngularShapeView extends NodeView<AngularShape> {
    getNodeContainer(): HTMLDivElement;
    confirmUpdate(flag: number): number;
    private getNgArguments;
    /** 当执行 node.setData() 时需要对实例设置新的输入值 */
    private setInstanceInput;
    protected renderAngularContent(): void;
    protected unmountAngularContent(): HTMLDivElement;
    onMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    unmount(): this;
}
export declare namespace AngularShapeView {
    const action: any;
}
