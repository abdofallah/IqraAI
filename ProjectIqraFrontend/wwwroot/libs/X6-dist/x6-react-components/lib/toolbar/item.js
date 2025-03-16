"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.ToolbarItem = void 0;
const react_1 = __importDefault(require("react"));
const classnames_1 = __importDefault(require("classnames"));
const antd_1 = require("antd");
const menu_1 = require("../menu");
const dropdown_1 = require("../dropdown");
const context_1 = require("./context");
class ToolbarItemInner extends react_1.default.PureComponent {
    constructor() {
        super(...arguments);
        this.handleClick = () => {
            this.processClick();
        };
        this.handleDropdownItemClick = (name) => {
            this.processClick(name, false);
        };
    }
    processClick(name = this.props.name, dropdown = this.props.dropdown) {
        if (!this.props.disabled && !dropdown) {
            if (name) {
                this.props.context.onClick(name);
            }
            if (this.props.onClick) {
                this.props.onClick(name);
            }
        }
    }
    renderButton() {
        const { className, hidden, disabled, active, icon, text, dropdown, dropdownArrow, tooltip, tooltipProps, tooltipAsTitle, children, } = this.props;
        const { prefixCls } = this.props.context;
        const baseCls = `${prefixCls}-item`;
        const props = {
            onClick: this.handleClick,
            className: (0, classnames_1.default)(baseCls, {
                [`${baseCls}-hidden`]: hidden,
                [`${baseCls}-active`]: active,
                [`${baseCls}-disabled`]: disabled,
                [`${baseCls}-dropdown`]: dropdown,
            }, className),
        };
        if (tooltip && tooltipAsTitle) {
            props.title = tooltip;
        }
        const button = (react_1.default.createElement("button", Object.assign({ type: "button" }, props),
            icon && react_1.default.isValidElement(icon) && (react_1.default.createElement("span", { className: `${baseCls}-icon` }, icon)),
            (text || children) && (react_1.default.createElement("span", { className: `${baseCls}-text` }, text || children)),
            dropdown && dropdownArrow && (react_1.default.createElement("span", { className: `${baseCls}-dropdown-arrow` }))));
        if (tooltip && !tooltipAsTitle && !disabled) {
            return (react_1.default.createElement(antd_1.Tooltip, Object.assign({ title: tooltip, placement: "bottom", mouseEnterDelay: 0, mouseLeaveDelay: 0 }, tooltipProps), button));
        }
        return button;
    }
    render() {
        const { dropdown, dropdownProps, disabled } = this.props;
        const content = this.renderButton();
        if (dropdown != null && !disabled) {
            const overlay = (react_1.default.createElement("div", null, dropdown.type === menu_1.Menu
                ? react_1.default.cloneElement(dropdown, {
                    onClick: this.handleDropdownItemClick,
                })
                : dropdown));
            const props = Object.assign(Object.assign({ trigger: ['click'] }, dropdownProps), { disabled,
                overlay });
            return react_1.default.createElement(dropdown_1.Dropdown, Object.assign({}, props), content);
        }
        return content;
    }
}
const ToolbarItem = (props) => (react_1.default.createElement(context_1.ToolbarContext.Consumer, null, (context) => react_1.default.createElement(ToolbarItemInner, Object.assign({ context: context }, props))));
exports.ToolbarItem = ToolbarItem;
exports.ToolbarItem.defaultProps = {
    dropdownArrow: true,
};
//# sourceMappingURL=item.js.map