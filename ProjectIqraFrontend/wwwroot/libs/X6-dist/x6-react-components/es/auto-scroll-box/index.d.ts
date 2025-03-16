import React, { PropsWithChildren } from 'react';
import { ScrollBox } from '../scroll-box';
export declare class AutoScrollBox extends React.PureComponent<PropsWithChildren<AutoScrollBox.Props>, AutoScrollBox.State> {
    constructor(props: AutoScrollBox.Props);
    onContentResize: (width: number, height: number) => void;
    render(): React.JSX.Element;
}
export declare namespace AutoScrollBox {
    interface Props {
        prefixCls?: string;
        /**
         * Function that will be invoked with width and height arguments
         */
        onResize?: (width: number, height: number) => void;
        /**
         * Do not trigger onResize when a component mounts.
         *
         * Default: `false`
         */
        skipOnMount?: boolean;
        /**
         * Possible values: throttle and debounce.
         */
        refreshMode?: 'throttle' | 'debounce';
        /**
         * Makes sense only when refreshMode is set.
         *
         * Default: `1000`
         */
        refreshRate?: number;
        scrollX?: boolean;
        scrollY?: boolean;
        scrollBoxProps?: ScrollBox.Props;
    }
    const defaultProps: Props;
    interface State {
        contentWidth?: number | null;
        contentHeight?: number | null;
    }
}
