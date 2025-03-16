import React, { PropsWithChildren } from 'react';
import { MenuItem } from './item';
export declare class Menu extends React.PureComponent<PropsWithChildren<Menu.Props>> {
    private onClick;
    private registerHotkey;
    private unregisterHotkey;
    render(): React.JSX.Element;
}
export declare namespace Menu {
    const Item: React.FC<MenuItem.Props>;
    const Divider: React.FC<{}>;
    const SubMenu: React.FC<MenuItem.Props>;
    interface Props {
        prefixCls?: string;
        className?: string;
        hasIcon?: boolean;
        stopPropagation?: boolean;
        onClick?: (name: string) => void;
        registerHotkey?: (hotkey: string, handler: () => void) => void;
        unregisterHotkey?: (hotkey: string, handler: () => void) => void;
    }
    const defaultProps: Props;
}
