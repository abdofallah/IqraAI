"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.ToolbarGroup = void 0;
const react_1 = __importDefault(require("react"));
const classnames_1 = __importDefault(require("classnames"));
const context_1 = require("./context");
const ToolbarGroup = ({ children, className, }) => (react_1.default.createElement(context_1.ToolbarContext.Consumer, null, ({ prefixCls }) => (react_1.default.createElement("div", { className: (0, classnames_1.default)(`${prefixCls}-group`, className) }, children))));
exports.ToolbarGroup = ToolbarGroup;
//# sourceMappingURL=group.js.map