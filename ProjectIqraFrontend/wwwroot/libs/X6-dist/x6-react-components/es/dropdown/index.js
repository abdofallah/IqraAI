import React from 'react';
import classNames from 'classnames';
import RcDropdown from 'rc-dropdown';
export class Dropdown extends React.Component {
    // getTransitionName() {
    //   const { placement = '', transitionName } = this.props
    //   if (transitionName !== undefined) {
    //     return transitionName
    //   }
    //   if (placement.indexOf('top') >= 0) {
    //     return 'slide-down'
    //   }
    //   return 'slide-up'
    // }
    render() {
        const { children, trigger, disabled } = this.props;
        const prefixCls = `${this.props.prefixCls}-dropdown`;
        const child = React.Children.only(children);
        const dropdownTrigger = React.cloneElement(child, {
            className: classNames(children.props.className, `${prefixCls}-trigger`),
            disabled,
        });
        const triggers = disabled
            ? []
            : Array.isArray(trigger)
                ? trigger
                : [trigger];
        let alignPoint = false;
        if (triggers && triggers.indexOf('contextMenu') !== -1) {
            alignPoint = true;
        }
        const overlay = React.Children.only(this.props.overlay);
        const fixedOverlay = React.createElement("div", { className: `${prefixCls}-overlay` }, overlay);
        return (React.createElement(RcDropdown, Object.assign({}, this.props, { prefixCls: prefixCls, overlay: fixedOverlay, alignPoint: alignPoint, trigger: triggers }), dropdownTrigger));
    }
}
(function (Dropdown) {
    Dropdown.defaultProps = {
        trigger: 'hover',
        prefixCls: 'x6',
        mouseEnterDelay: 0.15,
        mouseLeaveDelay: 0.1,
        placement: 'bottomLeft',
    };
})(Dropdown || (Dropdown = {}));
//# sourceMappingURL=index.js.map