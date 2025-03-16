"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Menubar = void 0;
const react_1 = __importDefault(require("react"));
const classnames_1 = __importDefault(require("classnames"));
const addEventListener_1 = __importDefault(require("rc-util/lib/Dom/addEventListener"));
const item_1 = require("./item");
const context_1 = require("./context");
class Menubar extends react_1.default.PureComponent {
    constructor(props) {
        super(props);
        this.onDocumentClick = () => {
            this.setState({ active: false });
            this.unbindDocEvent();
        };
        this.activeMenubar = () => {
            this.setState({ active: true });
            if (!this.removeDocClickEvent) {
                this.removeDocClickEvent = (0, addEventListener_1.default)(document.documentElement, 'click', this.onDocumentClick).remove;
            }
        };
        this.state = { active: false };
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
    render() {
        const { prefixCls, className, children, extra } = this.props;
        const baseCls = `${prefixCls}-menubar`;
        const contextValue = {
            prefixCls: baseCls,
            activeMenubar: this.activeMenubar,
            menubarActived: this.state.active === true,
        };
        return (react_1.default.createElement("div", { className: (0, classnames_1.default)(baseCls, className) },
            react_1.default.createElement("div", { className: `${baseCls}-content` },
                react_1.default.createElement("div", { className: `${baseCls}-content-inner` },
                    react_1.default.createElement(context_1.MenubarContext.Provider, { value: contextValue }, children)),
                extra && react_1.default.createElement("div", { className: `${baseCls}-content-extras` }, extra))));
    }
}
exports.Menubar = Menubar;
(function (Menubar) {
    Menubar.Item = item_1.MenubarItem;
    Menubar.defaultProps = {
        prefixCls: 'x6',
    };
})(Menubar = exports.Menubar || (exports.Menubar = {}));
//# sourceMappingURL=menubar.js.map