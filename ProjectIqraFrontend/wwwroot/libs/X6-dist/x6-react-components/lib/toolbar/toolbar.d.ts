import React, { PropsWithChildren } from 'react';
import { ToolbarItem } from './item';
import { ToolbarGroup } from './group';
export declare class Toolbar extends React.PureComponent<PropsWithChildren<Toolbar.Props>> {
    onClick: (key: string, value?: any) => void;
    render(): React.JSX.Element;
}
export declare namespace Toolbar {
    const Item: React.FC<ToolbarItem.Props>;
    const Group: React.FC<React.PropsWithChildren<ToolbarGroup.Props>>;
    interface Props {
        prefixCls?: string;
        className?: string;
        extra?: React.ReactNode;
        size?: 'small' | 'big';
        hoverEffect?: boolean;
        align?: 'left' | 'right';
        onClick?: (name: string, value?: any) => void;
    }
    const defaultProps: Props;
}
