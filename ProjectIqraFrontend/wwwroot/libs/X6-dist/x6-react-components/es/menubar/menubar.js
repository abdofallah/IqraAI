import React from 'react';
import classnames from 'classnames';
import addEventListener from 'rc-util/lib/Dom/addEventListener';
import { MenubarItem } from './item';
import { MenubarContext } from './context';
export class Menubar extends React.PureComponent {
    constructor(props) {
        super(props);
        this.onDocumentClick = () => {
            this.setState({ active: false });
            this.unbindDocEvent();
        };
        this.activeMenubar = () => {
            this.setState({ active: true });
            if (!this.removeDocClickEvent) {
                this.removeDocClickEvent = addEventListener(document.documentElement, 'click', this.onDocumentClick).remove;
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
        return (React.createElement("div", { className: classnames(baseCls, className) },
            React.createElement("div", { className: `${baseCls}-content` },
                React.createElement("div", { className: `${baseCls}-content-inner` },
                    React.createElement(MenubarContext.Provider, { value: contextValue }, children)),
                extra && React.createElement("div", { className: `${baseCls}-content-extras` }, extra))));
    }
}
(function (Menubar) {
    Menubar.Item = MenubarItem;
    Menubar.defaultProps = {
        prefixCls: 'x6',
    };
})(Menubar || (Menubar = {}));
//# sourceMappingURL=menubar.js.map