import React, { PropsWithChildren } from 'react';
export declare class ScrollBox extends React.PureComponent<PropsWithChildren<ScrollBox.Props>, ScrollBox.State> {
    private containerElem;
    private contentElem;
    private wheelHandler;
    private touchHandler;
    private mouseMoveTracker;
    private scrolling;
    private mounted;
    private triggerScrollStop;
    UNSAFE_componentWillMount(): void;
    componentDidMount(): void;
    UNSAFE_componentWillReceiveProps(nextProps: ScrollBox.Props): void;
    componentWillUnmount(): void;
    calculateState(props?: Readonly<React.PropsWithChildren<ScrollBox.Props>>): ScrollBox.State;
    onScroll: (deltaX: number, deltaY: number) => void;
    onVerticalScroll: (scrollY: number) => void;
    onHorizontalScroll: (scrollX: number) => void;
    scrollVertical(scrollY: number, relative: boolean): void;
    scrollHorizontal(scrollX: number, relative: boolean): void;
    triggerScrollStart(): void;
    triggerScrollStopSync(): void;
    shouldHandleWheelX: (delta: number) => boolean;
    shouldHandleWheelY: (delta: number) => boolean;
    shouldHandleTouchX: (delta: number) => boolean;
    shouldHandleTouchY: (delta: number) => boolean;
    onMouseDown: (e: React.MouseEvent) => void;
    onMouseMove: (deltaX: number, deltaY: number) => void;
    onMouseMoveEnd: () => void;
    getScrollbarProps(): {
        zIndex: number | undefined;
        miniThumbSize: number | undefined;
        scrollbarSize: number | undefined;
        keyboardScrollAmount: number | undefined;
        stopPropagation: boolean;
    };
    renderVerticalBar(): React.JSX.Element | undefined;
    renderHorizontalBar(): React.JSX.Element | undefined;
    refContainer: (container: HTMLDivElement) => void;
    refContent: (content: HTMLDivElement) => void;
    onWheel: (e: React.WheelEvent) => void;
    render(): React.JSX.Element;
}
export declare namespace ScrollBox {
    interface Props {
        prefixCls?: string;
        containerClassName?: string;
        contentClassName?: string;
        containerStyle?: React.CSSProperties;
        contentStyle?: React.CSSProperties;
        containerWidth?: number;
        containerHeight?: number;
        contentWidth?: number;
        contentHeight?: number;
        scrollTop?: number;
        scrollLeft?: number;
        dragable?: boolean;
        touchable?: boolean;
        miniThumbSize?: number;
        scrollbarSize?: number;
        keyboardScrollAmount?: number;
        stopPropagation?: boolean | (() => boolean);
        zIndex?: number;
        scrollbarAutoHide?: boolean;
        onVerticalScroll?: (scrollTop: number) => void;
        onHorizontalScroll?: (scrollLeft: number) => void;
        onScrollStart?: (scrollLeft: number, scrollTop: number) => void;
        onScroll?: (scrollLeft: number, scrollTop: number) => void;
        onScrollEnd?: (scrollLeft: number, scrollTop: number) => void;
    }
    const defaultProps: {
        prefixCls: string;
        scrollTop: number;
        scrollLeft: number;
        dragable: boolean;
        touchable: boolean;
        scrollbarAutoHide: boolean;
        scrollbarSize: number;
        miniThumbSize: number;
        keyboardScrollAmount: number;
    };
    interface State {
        containerWidth?: number;
        containerHeight?: number;
        contentWidth?: number;
        contentHeight?: number;
        verticalBarHeight: number;
        horizontalBarWidth: number;
        scrollTop?: number;
        scrollLeft?: number;
        maxScrollTop?: number;
        maxScrollLeft?: number;
        hasVerticalBar?: boolean;
        hasHorizontalBar?: boolean;
    }
}
