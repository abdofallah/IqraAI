/* eslint-disable jsx-a11y/click-events-have-key-events  */
/* eslint-disable jsx-a11y/no-static-element-interactions */
import React from 'react';
import classnames from 'classnames';
import addEventListener from 'rc-util/lib/Dom/addEventListener';
import { MenubarContext } from './context';
const cacheDeactiveMap = new WeakMap();
class MenubarItemInner extends React.PureComponent {
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
                this.removeDocClickEvent = addEventListener(document.documentElement, 'click', this.onDocumentClick).remove;
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
        return (React.createElement("div", { className: classnames(baseCls, {
                [`${baseCls}-hidden`]: hidden,
                [`${baseCls}-hover`]: menubarActived,
                [`${baseCls}-active`]: currentMenuActived,
            }), onMouseEnter: this.onMouseEnter, onMouseLeave: this.onMouseLeave },
            React.createElement("div", { className: classnames(`${baseCls}-text`, {
                    [`${baseCls}-text-active`]: currentMenuActived,
                }), onClick: this.onClick }, text),
            React.createElement("div", { className: this.popupClassName }, children)));
    }
}
export const MenubarItem = (props) => (React.createElement(MenubarContext.Consumer, null, (context) => React.createElement(MenubarItemInner, Object.assign({ context: context }, props))));
//# sourceMappingURL=item.js.map