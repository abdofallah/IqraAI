"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Dropdown = void 0;
const react_1 = __importDefault(require("react"));
const classnames_1 = __importDefault(require("classnames"));
const rc_dropdown_1 = __importDefault(require("rc-dropdown"));
class Dropdown extends react_1.default.Component {
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
        const child = react_1.default.Children.only(children);
        const dropdownTrigger = react_1.default.cloneElement(child, {
            className: (0, classnames_1.default)(children.props.className, `${prefixCls}-trigger`),
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
        const overlay = react_1.default.Children.only(this.props.overlay);
        const fixedOverlay = react_1.default.createElement("div", { className: `${prefixCls}-overlay` }, overlay);
        return (react_1.default.createElement(rc_dropdown_1.default, Object.assign({}, this.props, { prefixCls: prefixCls, overlay: fixedOverlay, alignPoint: alignPoint, trigger: triggers }), dropdownTrigger));
    }
}
exports.Dropdown = Dropdown;
(function (Dropdown) {
    Dropdown.defaultProps = {
        trigger: 'hover',
        prefixCls: 'x6',
        mouseEnterDelay: 0.15,
        mouseLeaveDelay: 0.1,
        placement: 'bottomLeft',
    };
})(Dropdown = exports.Dropdown || (exports.Dropdown = {}));
//# sourceMappingURL=index.js.map