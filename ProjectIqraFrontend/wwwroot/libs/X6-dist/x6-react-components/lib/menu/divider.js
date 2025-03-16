"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.MenuDivider = void 0;
const react_1 = __importDefault(require("react"));
const context_1 = require("./context");
const MenuDivider = () => (react_1.default.createElement(context_1.MenuContext.Consumer, null, ({ prefixCls }) => (react_1.default.createElement("div", { className: `${prefixCls}-item ${prefixCls}-item-divider` }))));
exports.MenuDivider = MenuDivider;
//# sourceMappingURL=divider.js.map