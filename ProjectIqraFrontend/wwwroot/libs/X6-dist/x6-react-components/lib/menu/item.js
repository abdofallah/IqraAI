"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.MenuItem = exports.MenuItemInner = void 0;
const react_1 = __importDefault(require("react"));
const classnames_1 = __importDefault(require("classnames"));
const context_1 = require("./context");
class MenuItemInner extends react_1.default.PureComponent {
    constructor() {
        super(...arguments);
        this.onHotkey = () => {
            this.triggerHandler();
        };
        this.onClick = (e) => {
            this.triggerHandler(e);
        };
    }
    componentDidMount() {
        const { hotkey } = this.props;
        if (hotkey) {
            this.props.context.registerHotkey(hotkey, this.onHotkey);
        }
    }
    componentWillUnmount() {
        const { hotkey } = this.props;
        if (hotkey) {
            this.props.context.unregisterHotkey(hotkey, this.onHotkey);
        }
    }
    triggerHandler(e) {
        if (!this.props.disabled && !this.props.hidden) {
            if (this.props.name) {
                this.props.context.onClick(this.props.name, e);
            }
            if (this.props.onClick) {
                this.props.onClick();
            }
        }
    }
    render() {
        return (react_1.default.createElement("div", Object.assign({}, MenuItemInner.getProps(this.props)), MenuItemInner.getContent(this.props, this.onClick)));
    }
}
exports.MenuItemInner = MenuItemInner;
(function (MenuItemInner) {
    function getProps(props, extraCls) {
        const { className, disabled, active, hidden } = props;
        const { prefixCls } = props.context;
        const baseCls = `${prefixCls}-item`;
        return {
            className: (0, classnames_1.default)(baseCls, extraCls, {
                [`${baseCls}-active`]: active,
                [`${baseCls}-hidden`]: hidden,
                [`${baseCls}-disabled`]: disabled,
            }, className),
        };
    }
    MenuItemInner.getProps = getProps;
    function getContent(props, onClick, innerExtra, outerExtra) {
        const { icon, text, hotkey, children } = props;
        const { prefixCls } = props.context;
        const baseCls = `${prefixCls}-item`;
        return (react_1.default.createElement(react_1.default.Fragment, null,
            react_1.default.createElement("button", { type: "button", className: `${baseCls}-button`, onClick: onClick },
                icon && react_1.default.isValidElement(icon) && (react_1.default.createElement("span", { className: `${baseCls}-icon` }, icon)),
                react_1.default.createElement("span", { className: `${baseCls}-text` }, text || children),
                hotkey && react_1.default.createElement("span", { className: `${baseCls}-hotkey` }, hotkey),
                innerExtra),
            outerExtra));
    }
    MenuItemInner.getContent = getContent;
})(MenuItemInner = exports.MenuItemInner || (exports.MenuItemInner = {}));
const MenuItem = (props) => (react_1.default.createElement(context_1.MenuContext.Consumer, null, (context) => react_1.default.createElement(MenuItemInner, Object.assign({ context: context }, props))));
exports.MenuItem = MenuItem;
//# sourceMappingURL=item.js.map