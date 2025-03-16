var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                t[p[i]] = s[p[i]];
        }
    return t;
};
import React from 'react';
import ResizeDetector from 'react-resize-detector';
import { ScrollBox } from '../scroll-box';
export class AutoScrollBox extends React.PureComponent {
    constructor(props) {
        super(props);
        this.onContentResize = (width, height) => {
            if (this.props.scrollX) {
                this.setState({ contentWidth: width });
            }
            if (this.props.scrollY) {
                this.setState({ contentHeight: height });
            }
        };
        this.state = {
            contentWidth: null,
            contentHeight: null,
        };
    }
    render() {
        const _a = this.props, { prefixCls, children, scrollX, scrollY, scrollBoxProps } = _a, props = __rest(_a, ["prefixCls", "children", "scrollX", "scrollY", "scrollBoxProps"]);
        return (React.createElement(ResizeDetector, Object.assign({ handleWidth: scrollX, handleHeight: scrollY }, props), (size) => {
            const { width, height } = size;
            const others = {};
            if (!scrollX) {
                others.contentWidth = width;
            }
            if (!scrollY) {
                others.contentHeight = height;
            }
            if (this.state.contentWidth != null) {
                others.contentWidth = this.state.contentWidth;
            }
            if (this.state.contentHeight != null) {
                others.contentHeight = this.state.contentHeight;
            }
            return (React.createElement(ScrollBox, Object.assign({ dragable: false, scrollbarSize: 3 }, scrollBoxProps, { containerWidth: width, containerHeight: height }),
                React.createElement("div", { className: `${prefixCls}-auto-scroll-box-content` },
                    React.createElement(ResizeDetector, { handleWidth: scrollX, handleHeight: scrollY, skipOnMount: true, onResize: this.onContentResize }, children))));
        }));
    }
}
(function (AutoScrollBox) {
    AutoScrollBox.defaultProps = {
        prefixCls: 'x6',
        scrollX: true,
        scrollY: true,
    };
})(AutoScrollBox || (AutoScrollBox = {}));
//# sourceMappingURL=index.js.map