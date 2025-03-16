"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Menu = void 0;
const react_1 = __importDefault(require("react"));
const classnames_1 = __importDefault(require("classnames"));
const item_1 = require("./item");
const divider_1 = require("./divider");
const context_1 = require("./context");
const submenu_1 = require("./submenu");
class Menu extends react_1.default.PureComponent {
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
        const ContextProvider = context_1.MenuContext.Provider;
        const contextValue = {
            prefixCls: baseCls,
            onClick: this.onClick,
            registerHotkey: this.registerHotkey,
            unregisterHotkey: this.unregisterHotkey,
        };
        return (react_1.default.createElement("div", { className: (0, classnames_1.default)(baseCls, {
                [`${baseCls}-has-icon`]: hasIcon,
            }, className) },
            react_1.default.createElement(ContextProvider, { value: contextValue }, children)));
    }
}
exports.Menu = Menu;
(function (Menu) {
    Menu.Item = item_1.MenuItem;
    Menu.Divider = divider_1.MenuDivider;
    Menu.SubMenu = submenu_1.MenuSubMenu;
    Menu.defaultProps = {
        prefixCls: 'x6',
        stopPropagation: false,
    };
})(Menu = exports.Menu || (exports.Menu = {}));
//# sourceMappingURL=menu.js.map