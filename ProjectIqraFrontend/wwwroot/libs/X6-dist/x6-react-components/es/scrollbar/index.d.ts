import React from 'react';
export declare class Scrollbar extends React.PureComponent<Scrollbar.Props> {
    private thumbElem;
    private containerElem;
    private wheelHandler;
    private mouseMoveTracker;
    private scale;
    private thumbSize;
    UNSAFE_componentWillMount(): void;
    componentWillUnmount(): void;
    isHorizontal(): boolean;
    fixPosition(position: number): any;
    triggerCallback: (nextPosition: number) => void;
    onWheel: (delta: number) => void;
    onWheelX: (deltaX: number, deltaY: number) => void;
    onWheelY: (deltaX: number, deltaY: number) => void;
    onKeyDown: (e: React.KeyboardEvent) => void;
    onMouseDown: (e: React.MouseEvent) => void;
    onMouseMove: (deltaX: number, deltaY: number) => void;
    onMouseMoveEnd: () => void;
    refContainer: (container: HTMLDivElement) => void;
    refThumb: (thumb: HTMLDivElement) => void;
    render(): React.JSX.Element | null;
}
export declare namespace Scrollbar {
    interface Props {
        prefixCls?: string;
        className?: string;
        orientation: 'vertical' | 'horizontal';
        contentSize: number;
        containerSize: number;
        scrollPosition: number;
        scrollbarSize?: number;
        miniThumbSize?: number;
        keyboardScrollAmount?: number;
        zIndex?: number;
        stopPropagation?: boolean | (() => boolean);
        onScroll: (delta: number) => void;
    }
    const defaultProps: {
        prefixCls: string;
        orientation: string;
        contentSize: number;
        containerSize: number;
        defaultPosition: number;
        scrollbarSize: number;
        miniThumbSize: number;
        keyboardScrollAmount: number;
    };
}
