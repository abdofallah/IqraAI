/* eslint-disable jsx-a11y/click-events-have-key-events  */
/* eslint-disable jsx-a11y/no-static-element-interactions */
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
import classNames from 'classnames';
import { Popover } from 'antd';
import addEventListener from 'rc-util/lib/Dom/addEventListener';
import { SketchPicker, } from 'react-color';
export class ColorPicker extends React.Component {
    constructor(props) {
        super(props);
        this.onDocumentClick = (e) => {
            const target = e.target;
            const picker = this.container.querySelector('.sketch-picker');
            if (target === picker || picker.contains(target)) {
                return;
            }
            this.setState({ active: false });
            this.unbindDocEvent();
        };
        this.handleChange = (value, event) => {
            if (this.props.onChange) {
                this.props.onChange(value, event);
            }
            this.setState({
                color: value.rgb,
            });
        };
        this.handleClick = (e) => {
            e.stopPropagation();
            if (this.state.active) {
                this.setState({ active: false });
                this.unbindDocEvent();
            }
            else {
                this.setState({ active: true });
                if (!this.removeDocClickEvent) {
                    this.removeDocClickEvent = addEventListener(document.documentElement, 'click', this.onDocumentClick).remove;
                }
            }
        };
        this.state = {
            active: false,
            color: props.color,
        };
    }
    componentWillUnmount() {
        this.unbindDocEvent();
    }
    unbindDocEvent() {
        if (this.removeDocClickEvent) {
            this.removeDocClickEvent();
            this.removeDocClickEvent = null;
        }
    }
    renderPicker() {
        const _a = this.props, { prefixCls, disabled, style } = _a, props = __rest(_a, ["prefixCls", "disabled", "style"]);
        return (React.createElement(SketchPicker, Object.assign({ width: "240px" }, props, { onChange: this.handleChange })));
    }
    render() {
        const { color } = this.state;
        const { disabled, overlayProps, style } = this.props;
        const baseCls = `${this.props.prefixCls}-color-picker`;
        const popoverProps = {};
        if (disabled) {
            popoverProps.visible = false;
            // Support for antd 5.0
            popoverProps.open = false;
        }
        else {
            popoverProps.visible = this.state.active;
            // Support for antd 5.0
            popoverProps.open = this.state.active;
        }
        const colorStr = typeof color === 'string'
            ? color
            : `rgba(${color.r},${color.g},${color.b},${color.a})`;
        return (React.createElement(Popover, Object.assign({ placement: "topLeft" }, overlayProps, popoverProps, { content: this.renderPicker(), overlayClassName: `${baseCls}-overlay`, destroyTooltipOnHide: true, ref: (ref) => {
                if (ref) {
                    this.container = ref.getContainer();
                }
            }, trigger: [] }),
            React.createElement("div", { style: style, onClick: this.handleClick, className: classNames(baseCls, {
                    [`${baseCls}-disabled`]: disabled,
                }) },
                React.createElement("div", { className: `${baseCls}-block`, style: { backgroundColor: disabled ? '#c4c4c4' : colorStr } }))));
    }
}
(function (ColorPicker) {
    ColorPicker.defaultProps = {
        prefixCls: 'x6',
        color: '#1890FF',
    };
})(ColorPicker || (ColorPicker = {}));
//# sourceMappingURL=index.js.map