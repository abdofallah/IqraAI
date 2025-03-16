import React from 'react';
import classNames from 'classnames';
import { MenuContext } from './context';
export class MenuItemInner extends React.PureComponent {
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
        return (React.createElement("div", Object.assign({}, MenuItemInner.getProps(this.props)), MenuItemInner.getContent(this.props, this.onClick)));
    }
}
(function (MenuItemInner) {
    function getProps(props, extraCls) {
        const { className, disabled, active, hidden } = props;
        const { prefixCls } = props.context;
        const baseCls = `${prefixCls}-item`;
        return {
            className: classNames(baseCls, extraCls, {
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
        return (React.createElement(React.Fragment, null,
            React.createElement("button", { type: "button", className: `${baseCls}-button`, onClick: onClick },
                icon && React.isValidElement(icon) && (React.createElement("span", { className: `${baseCls}-icon` }, icon)),
                React.createElement("span", { className: `${baseCls}-text` }, text || children),
                hotkey && React.createElement("span", { className: `${baseCls}-hotkey` }, hotkey),
                innerExtra),
            outerExtra));
    }
    MenuItemInner.getContent = getContent;
})(MenuItemInner || (MenuItemInner = {}));
export const MenuItem = (props) => (React.createElement(MenuContext.Consumer, null, (context) => React.createElement(MenuItemInner, Object.assign({ context: context }, props))));
//# sourceMappingURL=item.js.map