import React, { PropsWithChildren } from 'react';
export declare class Dropdown extends React.Component<PropsWithChildren<Dropdown.Props>, any> {
    render(): React.JSX.Element;
}
export declare namespace Dropdown {
    type Trigger = 'click' | 'hover' | 'contextMenu';
    type Placement = 'topLeft' | 'topCenter' | 'topRight' | 'bottomLeft' | 'bottomCenter' | 'bottomRight';
    interface Props {
        prefixCls?: string;
        className?: string;
        overlay?: React.ReactNode;
        overlayStyle?: React.CSSProperties;
        overlayClassName?: string;
        visible?: boolean;
        disabled?: boolean;
        trigger?: Trigger | Trigger[];
        transitionName?: string;
        placement?: Placement;
        forceRender?: boolean;
        mouseEnterDelay?: number;
        mouseLeaveDelay?: number;
        onVisibleChange?: (visible?: boolean) => void;
        getPopupContainer?: (triggerNode: Element) => HTMLElement;
    }
    const defaultProps: Props;
}
