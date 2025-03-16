import React from 'react';
import classNames from 'classnames';
import { Tooltip } from 'antd';
import { Menu } from '../menu';
import { Dropdown } from '../dropdown';
import { ToolbarContext } from './context';
class ToolbarItemInner extends React.PureComponent {
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
            className: classNames(baseCls, {
                [`${baseCls}-hidden`]: hidden,
                [`${baseCls}-active`]: active,
                [`${baseCls}-disabled`]: disabled,
                [`${baseCls}-dropdown`]: dropdown,
            }, className),
        };
        if (tooltip && tooltipAsTitle) {
            props.title = tooltip;
        }
        const button = (React.createElement("button", Object.assign({ type: "button" }, props),
            icon && React.isValidElement(icon) && (React.createElement("span", { className: `${baseCls}-icon` }, icon)),
            (text || children) && (React.createElement("span", { className: `${baseCls}-text` }, text || children)),
            dropdown && dropdownArrow && (React.createElement("span", { className: `${baseCls}-dropdown-arrow` }))));
        if (tooltip && !tooltipAsTitle && !disabled) {
            return (React.createElement(Tooltip, Object.assign({ title: tooltip, placement: "bottom", mouseEnterDelay: 0, mouseLeaveDelay: 0 }, tooltipProps), button));
        }
        return button;
    }
    render() {
        const { dropdown, dropdownProps, disabled } = this.props;
        const content = this.renderButton();
        if (dropdown != null && !disabled) {
            const overlay = (React.createElement("div", null, dropdown.type === Menu
                ? React.cloneElement(dropdown, {
                    onClick: this.handleDropdownItemClick,
                })
                : dropdown));
            const props = Object.assign(Object.assign({ trigger: ['click'] }, dropdownProps), { disabled,
                overlay });
            return React.createElement(Dropdown, Object.assign({}, props), content);
        }
        return content;
    }
}
export const ToolbarItem = (props) => (React.createElement(ToolbarContext.Consumer, null, (context) => React.createElement(ToolbarItemInner, Object.assign({ context: context }, props))));
ToolbarItem.defaultProps = {
    dropdownArrow: true,
};
//# sourceMappingURL=item.js.map