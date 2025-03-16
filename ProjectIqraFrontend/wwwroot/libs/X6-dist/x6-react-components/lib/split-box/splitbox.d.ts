import React, { PropsWithChildren } from 'react';
export declare class SplitBox extends React.PureComponent<PropsWithChildren<SplitBox.Props>, SplitBox.State> {
    private container;
    private box1Elem;
    private box2Elem;
    private maskElem;
    private resizerElem;
    private minSize;
    private maxSize;
    private curSize;
    private rawSize;
    private resizing;
    constructor(props: SplitBox.Props);
    UNSAFE_componentWillReceiveProps(nextProps: SplitBox.Props): void;
    getNextState(props: SplitBox.Props): SplitBox.State;
    isVertical(): boolean;
    isPrimaryFirst(): boolean;
    getDelta(deltaX: number, deltaY: number): number;
    getRange(): {
        minSize: any;
        maxSize: any;
    };
    getPrimarySize(): number;
    setPrimarySize(size: number): void;
    updateCursor(size: number, minSize: number, maxSize: number): void;
    createMask(): void;
    removeMask(): void;
    onMouseDown: () => void;
    onMouseMove: (deltaX: number, deltaY: number) => void;
    onMouseMoveEnd: () => void;
    refContainer: (container: HTMLDivElement) => void;
    refResizer: (elem: HTMLDivElement) => void;
    renderBox(baseCls: string, index: 1 | 2): React.JSX.Element;
    renderResizer(baseCls: string): React.JSX.Element;
    render(): React.JSX.Element;
}
export declare namespace SplitBox {
    interface Props {
        split?: 'vertical' | 'horizontal';
        primary?: 'first' | 'second';
        resizable?: boolean;
        /**
         * Rerender after resize.
         */
        refresh?: boolean;
        size?: number | string;
        minSize?: number;
        maxSize?: number;
        defaultSize?: number | string;
        step?: number;
        prefixCls?: string;
        style?: React.CSSProperties;
        boxStyle?: React.CSSProperties;
        primaryBoxStyle?: React.CSSProperties;
        secondBoxStyle?: React.CSSProperties;
        resizerStyle?: React.CSSProperties;
        onResizeStart?: () => void;
        onResizeEnd?: (newSize: number) => void;
        onResizing?: (newSize: number) => void;
        onResizerClick?: () => void;
        onResizerDoubleClick?: () => void;
    }
    const defaultProps: Props;
    interface State {
        box1Size?: number | string;
        box2Size?: number | string;
    }
}
