import React from 'react';
import classNames from 'classnames';
import { ToolbarContext } from './context';
export const ToolbarGroup = ({ children, className, }) => (React.createElement(ToolbarContext.Consumer, null, ({ prefixCls }) => (React.createElement("div", { className: classNames(`${prefixCls}-group`, className) }, children))));
//# sourceMappingURL=group.js.map