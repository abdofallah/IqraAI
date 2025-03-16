"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Toolbar = void 0;
const react_1 = __importDefault(require("react"));
const classnames_1 = __importDefault(require("classnames"));
const item_1 = require("./item");
const group_1 = require("./group");
const context_1 = require("./context");
class Toolbar extends react_1.default.PureComponent {
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
        return (react_1.default.createElement("div", { className: (0, classnames_1.default)(baseCls, className, {
                [`${baseCls}-${size}`]: size,
                [`${baseCls}-align-right`]: align === 'right',
                [`${baseCls}-hover-effect`]: hoverEffect,
            }) },
            react_1.default.createElement("div", { className: `${baseCls}-content` },
                react_1.default.createElement("div", { className: `${baseCls}-content-inner` },
                    react_1.default.createElement(context_1.ToolbarContext.Provider, { value: {
                            prefixCls: baseCls,
                            onClick: this.onClick,
                        } }, children)),
                extra && react_1.default.createElement("div", { className: `${baseCls}-content-extras` }, extra))));
    }
}
exports.Toolbar = Toolbar;
(function (Toolbar) {
    Toolbar.Item = item_1.ToolbarItem;
    Toolbar.Group = group_1.ToolbarGroup;
    Toolbar.defaultProps = {
        prefixCls: 'x6',
        hoverEffect: false,
    };
})(Toolbar = exports.Toolbar || (exports.Toolbar = {}));
//# sourceMappingURL=toolbar.js.map