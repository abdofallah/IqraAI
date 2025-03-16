import React from 'react';
import { MenuContext } from './context';
export const MenuDivider = () => (React.createElement(MenuContext.Consumer, null, ({ prefixCls }) => (React.createElement("div", { className: `${prefixCls}-item ${prefixCls}-item-divider` }))));
//# sourceMappingURL=divider.js.map