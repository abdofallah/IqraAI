"use strict";
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
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.AutoScrollBox = void 0;
const react_1 = __importDefault(require("react"));
const react_resize_detector_1 = __importDefault(require("react-resize-detector"));
const scroll_box_1 = require("../scroll-box");
class AutoScrollBox extends react_1.default.PureComponent {
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
        return (react_1.default.createElement(react_resize_detector_1.default, Object.assign({ handleWidth: scrollX, handleHeight: scrollY }, props), (size) => {
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
            return (react_1.default.createElement(scroll_box_1.ScrollBox, Object.assign({ dragable: false, scrollbarSize: 3 }, scrollBoxProps, { containerWidth: width, containerHeight: height }),
                react_1.default.createElement("div", { className: `${prefixCls}-auto-scroll-box-content` },
                    react_1.default.createElement(react_resize_detector_1.default, { handleWidth: scrollX, handleHeight: scrollY, skipOnMount: true, onResize: this.onContentResize }, children))));
        }));
    }
}
exports.AutoScrollBox = AutoScrollBox;
(function (AutoScrollBox) {
    AutoScrollBox.defaultProps = {
        prefixCls: 'x6',
        scrollX: true,
        scrollY: true,
    };
})(AutoScrollBox = exports.AutoScrollBox || (exports.AutoScrollBox = {}));
//# sourceMappingURL=index.js.map