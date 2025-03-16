"use strict";
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
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.MenuSubMenu = void 0;
const react_1 = __importDefault(require("react"));
const context_1 = require("./context");
const item_1 = require("./item");
const MenuSubMenu = (props) => {
    const { hotkey, children } = props, others = __rest(props, ["hotkey", "children"]);
    return (react_1.default.createElement(context_1.MenuContext.Consumer, null, (context) => {
        const { prefixCls } = context;
        const wrapProps = item_1.MenuItemInner.getProps(Object.assign({ context }, props), `${prefixCls}-submenu`);
        return (react_1.default.createElement("div", Object.assign({}, wrapProps), item_1.MenuItemInner.getContent(Object.assign({ context }, others), null, react_1.default.createElement("span", { className: `${prefixCls}-submenu-arrow` }), react_1.default.createElement("div", { className: `${prefixCls}-submenu-menu` }, children))));
    }));
};
exports.MenuSubMenu = MenuSubMenu;
//# sourceMappingURL=submenu.js.map