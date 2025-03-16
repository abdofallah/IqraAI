import React from 'react';
import { MenuContext } from './context';
export declare class MenuItemInner extends React.PureComponent<MenuItemInner.Props> {
    componentDidMount(): void;
    componentWillUnmount(): void;
    private onHotkey;
    private onClick;
    private triggerHandler;
    render(): React.JSX.Element;
}
export declare namespace MenuItemInner {
    interface Props extends MenuItem.Props {
        context: MenuContext.Contexts;
    }
    function getProps(props: Props, extraCls?: string): {
        className: string;
    };
    function getContent(props: Props, onClick: any, innerExtra?: any, outerExtra?: any): React.JSX.Element;
}
export declare const MenuItem: React.FC<MenuItem.Props>;
export declare namespace MenuItem {
    interface Props {
        className?: string;
        name?: string;
        icon?: React.ReactNode;
        text?: string | React.ReactNode;
        hotkey?: string;
        active?: boolean;
        hidden?: boolean;
        disabled?: boolean;
        children?: React.ReactNode;
        onClick?: () => void;
    }
}
