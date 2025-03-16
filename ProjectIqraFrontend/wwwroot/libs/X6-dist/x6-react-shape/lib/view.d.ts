import { Root } from 'react-dom/client';
import { Dom, NodeView } from '@antv/x6';
import { ReactShape } from './node';
export declare class ReactShapeView extends NodeView<ReactShape> {
    root?: Root;
    protected targetId(): string;
    getComponentContainer(): HTMLDivElement;
    confirmUpdate(flag: number): number;
    protected renderReactComponent(): void;
    protected unmountReactComponent(): void;
    onMouseDown(e: Dom.MouseDownEvent, x: number, y: number): void;
    unmount(): this;
}
export declare namespace ReactShapeView {
    const action: any;
}
