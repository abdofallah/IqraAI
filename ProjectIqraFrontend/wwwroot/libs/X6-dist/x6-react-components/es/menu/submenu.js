var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                t[p[i]] = s[p[i]];
        }
    return t;
};
import React from 'react';
import { MenuContext } from './context';
import { MenuItemInner } from './item';
export const MenuSubMenu = (props) => {
    const { hotkey, children } = props, others = __rest(props, ["hotkey", "children"]);
    return (React.createElement(MenuContext.Consumer, null, (context) => {
        const { prefixCls } = context;
        const wrapProps = MenuItemInner.getProps(Object.assign({ context }, props), `${prefixCls}-submenu`);
        return (React.createElement("div", Object.assign({}, wrapProps), MenuItemInner.getContent(Object.assign({ context }, others), null, React.createElement("span", { className: `${prefixCls}-submenu-arrow` }), React.createElement("div", { className: `${prefixCls}-submenu-menu` }, children))));
    }));
};
//# sourceMappingURL=submenu.js.map