import React from 'react';
import classNames from 'classnames';
import { MenuItem } from './item';
import { MenuDivider } from './divider';
import { MenuContext } from './context';
import { MenuSubMenu } from './submenu';
export class Menu extends React.PureComponent {
    constructor() {
        super(...arguments);
        this.onClick = (name, e) => {
            if (this.props.stopPropagation && e != null) {
                e.stopPropagation();
            }
            if (this.props.onClick) {
                this.props.onClick(name);
            }
        };
        this.registerHotkey = (hotkey, handler) => {
            if (this.props.registerHotkey) {
                this.props.registerHotkey(hotkey, handler);
            }
        };
        this.unregisterHotkey = (hotkey, handler) => {
            if (this.props.unregisterHotkey) {
                this.props.unregisterHotkey(hotkey, handler);
            }
        };
    }
    render() {
        const { prefixCls, className, children, hasIcon } = this.props;
        const baseCls = `${prefixCls}-menu`;
        const ContextProvider = MenuContext.Provider;
        const contextValue = {
            prefixCls: baseCls,
            onClick: this.onClick,
            registerHotkey: this.registerHotkey,
            unregisterHotkey: this.unregisterHotkey,
        };
        return (React.createElement("div", { className: classNames(baseCls, {
                [`${baseCls}-has-icon`]: hasIcon,
            }, className) },
            React.createElement(ContextProvider, { value: contextValue }, children)));
    }
}
(function (Menu) {
    Menu.Item = MenuItem;
    Menu.Divider = MenuDivider;
    Menu.SubMenu = MenuSubMenu;
    Menu.defaultProps = {
        prefixCls: 'x6',
        stopPropagation: false,
    };
})(Menu || (Menu = {}));
//# sourceMappingURL=menu.js.map