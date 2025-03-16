import React from 'react';
import classNames from 'classnames';
import { ToolbarItem } from './item';
import { ToolbarGroup } from './group';
import { ToolbarContext } from './context';
export class Toolbar extends React.PureComponent {
    constructor() {
        super(...arguments);
        this.onClick = (key, value) => {
            if (this.props.onClick) {
                this.props.onClick(key, value);
            }
        };
    }
    render() {
        const { prefixCls, className, children, extra, size, align, hoverEffect } = this.props;
        const baseCls = `${prefixCls}-toolbar`;
        return (React.createElement("div", { className: classNames(baseCls, className, {
                [`${baseCls}-${size}`]: size,
                [`${baseCls}-align-right`]: align === 'right',
                [`${baseCls}-hover-effect`]: hoverEffect,
            }) },
            React.createElement("div", { className: `${baseCls}-content` },
                React.createElement("div", { className: `${baseCls}-content-inner` },
                    React.createElement(ToolbarContext.Provider, { value: {
                            prefixCls: baseCls,
                            onClick: this.onClick,
                        } }, children)),
                extra && React.createElement("div", { className: `${baseCls}-content-extras` }, extra))));
    }
}
(function (Toolbar) {
    Toolbar.Item = ToolbarItem;
    Toolbar.Group = ToolbarGroup;
    Toolbar.defaultProps = {
        prefixCls: 'x6',
        hoverEffect: false,
    };
})(Toolbar || (Toolbar = {}));
//# sourceMappingURL=toolbar.js.map