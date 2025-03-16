"use strict";
/* eslint-disable jsx-a11y/click-events-have-key-events  */
/* eslint-disable jsx-a11y/no-static-element-interactions */
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.MenubarItem = void 0;
const react_1 = __importDefault(require("react"));
const classnames_1 = __importDefault(require("classnames"));
const addEventListener_1 = __importDefault(require("rc-util/lib/Dom/addEventListener"));
const context_1 = require("./context");
const cacheDeactiveMap = new WeakMap();
class MenubarItemInner extends react_1.default.PureComponent {
    constructor(props) {
        super(props);
        this.onDocumentClick = () => {
            this.deactive();
        };
        this.onClick = (e) => {
            e.stopPropagation();
            this.props.context.activeMenubar();
            this.removeDeactive(e.currentTarget.parentElement);
            this.active();
        };
        this.onMouseEnter = (e) => {
            if (this.props.context.menubarActived &&
                !this.state.active &&
                !this.isPrevMenuHiddening(e)) {
                const currentTarget = e.currentTarget;
                const childNodes = currentTarget.parentElement.childNodes;
                childNodes.forEach((child) => {
                    if (child === currentTarget) {
                        this.removeDeactive(child);
                    }
                    else {
                        this.callDeactive(child);
                    }
                });
                this.active();
            }
        };
        this.onMouseLeave = (e) => {
            const relatedTarget = e.relatedTarget;
            const currentTarget = e.currentTarget;
            if (this.props.context.menubarActived && this.state.active) {
                const childNodes = currentTarget.parentElement.childNodes;
                let shoudDeactive = false;
                if (relatedTarget !== window) {
                    for (let i = 0, l = childNodes.length; i < l; i += 1) {
                        const child = childNodes[i];
                        if (child === relatedTarget ||
                            child.contains(relatedTarget)) {
                            shoudDeactive = true;
                            break;
                        }
                    }
                }
                if (shoudDeactive) {
                    this.deactive();
                }
                else {
                    // 缓存一下，当再次 hover 到其他菜单时被调用
                    this.cacheDeactive(currentTarget);
                }
            }
        };
        this.active = () => {
            this.setState({ active: true });
            if (!this.removeDocClickEvent) {
                this.removeDocClickEvent = (0, addEventListener_1.default)(document.documentElement, 'click', this.onDocumentClick).remove;
            }
        };
        this.deactive = () => {
            this.setState({ active: false });
            if (this.removeDocClickEvent) {
                this.removeDocClickEvent();
                this.removeDocClickEvent = null;
            }
        };
        this.popupClassName = `${props.context.prefixCls}-item-dropdown`;
        this.state = { active: false };
    }
    isPrevMenuHiddening(e) {
        const toElement = e.nativeEvent.toElement;
        if (toElement && toElement.className === this.popupClassName) {
            return true;
        }
        const currentTarget = e.currentTarget;
        const childNodes = currentTarget.parentElement.childNodes;
        for (let i = 0, l = childNodes.length; i < l; i += 1) {
            const child = childNodes[i];
            const popupElem = child.querySelector(`.${this.popupClassName}`);
            if (popupElem.contains(toElement)) {
                return true;
            }
        }
        return false;
    }
    cacheDeactive(elem) {
        cacheDeactiveMap.set(elem, this.deactive);
    }
    callDeactive(elem) {
        if (cacheDeactiveMap.has(elem)) {
            cacheDeactiveMap.get(elem)();
            cacheDeactiveMap.delete(elem);
        }
    }
    removeDeactive(elem) {
        cacheDeactiveMap.delete(elem);
    }
    render() {
        const { text, children, hidden } = this.props;
        const { prefixCls, menubarActived } = this.props.context;
        const currentMenuActived = menubarActived && this.state.active;
        const baseCls = `${prefixCls}-item`;
        return (react_1.default.createElement("div", { className: (0, classnames_1.default)(baseCls, {
                [`${baseCls}-hidden`]: hidden,
                [`${baseCls}-hover`]: menubarActived,
                [`${baseCls}-active`]: currentMenuActived,
            }), onMouseEnter: this.onMouseEnter, onMouseLeave: this.onMouseLeave },
            react_1.default.createElement("div", { className: (0, classnames_1.default)(`${baseCls}-text`, {
                    [`${baseCls}-text-active`]: currentMenuActived,
                }), onClick: this.onClick }, text),
            react_1.default.createElement("div", { className: this.popupClassName }, children)));
    }
}
const MenubarItem = (props) => (react_1.default.createElement(context_1.MenubarContext.Consumer, null, (context) => react_1.default.createElement(MenubarItemInner, Object.assign({ context: context }, props))));
exports.MenubarItem = MenubarItem;
//# sourceMappingURL=item.js.map