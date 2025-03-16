"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Scrollbar = void 0;
const react_1 = __importDefault(require("react"));
const clamp_1 = __importDefault(require("clamp"));
const classnames_1 = __importDefault(require("classnames"));
const KeyCode_1 = __importDefault(require("rc-util/lib/KeyCode"));
const WheelHandler_1 = require("../util/dom/WheelHandler");
const MouseMoveTracker_1 = require("../util/dom/MouseMoveTracker");
class Scrollbar extends react_1.default.PureComponent {
    constructor() {
        super(...arguments);
        this.triggerCallback = (nextPosition) => {
            const max = this.props.contentSize - this.props.containerSize;
            const position = (0, clamp_1.default)(nextPosition, 0, max);
            if (position !== this.props.scrollPosition) {
                this.props.onScroll(position);
            }
        };
        this.onWheel = (delta) => {
            this.triggerCallback(this.props.scrollPosition + delta);
        };
        this.onWheelX = (deltaX, deltaY) => {
            if (Math.abs(deltaX) >= Math.abs(deltaY)) {
                this.onWheel(deltaX);
            }
        };
        this.onWheelY = (deltaX, deltaY) => {
            if (Math.abs(deltaX) <= Math.abs(deltaY)) {
                this.onWheel(deltaY);
            }
        };
        this.onKeyDown = (e) => {
            const keyCode = e.keyCode;
            // let focus move off the scrollbar
            if (keyCode === KeyCode_1.default.TAB) {
                return;
            }
            const { contentSize, containerSize } = this.props;
            let distance = this.props.keyboardScrollAmount;
            let direction = 0;
            if (this.isHorizontal()) {
                switch (keyCode) {
                    case KeyCode_1.default.HOME:
                        direction = -1;
                        distance = contentSize;
                        break;
                    case KeyCode_1.default.LEFT:
                        direction = -1;
                        break;
                    case KeyCode_1.default.RIGHT:
                        direction = 1;
                        break;
                    default:
                        return;
                }
            }
            else {
                switch (keyCode) {
                    case KeyCode_1.default.SPACE:
                        if (e.shiftKey) {
                            direction = -1;
                        }
                        else {
                            direction = 1;
                        }
                        break;
                    case KeyCode_1.default.HOME:
                        direction = -1;
                        distance = contentSize;
                        break;
                    case KeyCode_1.default.UP:
                        direction = -1;
                        break;
                    case KeyCode_1.default.DOWN:
                        direction = 1;
                        break;
                    case KeyCode_1.default.PAGE_UP:
                        direction = -1;
                        distance = containerSize;
                        break;
                    case KeyCode_1.default.PAGE_DOWN:
                        direction = 1;
                        distance = containerSize;
                        break;
                    default:
                        return;
                }
            }
            e.preventDefault();
            this.triggerCallback(this.props.scrollPosition + distance * direction);
        };
        this.onMouseDown = (e) => {
            if (e.target !== this.thumbElem) {
                const nativeEvent = e.nativeEvent;
                const position = this.isHorizontal()
                    ? nativeEvent.offsetX || nativeEvent.layerX
                    : nativeEvent.offsetY || nativeEvent.layerY;
                // mousedown on the scroll-track directly, move the
                // center of the scroll-face to the mouse position.
                this.triggerCallback((position - this.thumbSize * 0.5) / this.scale);
            }
            else {
                this.mouseMoveTracker.capture(e);
            }
            if (this.props.stopPropagation) {
                e.stopPropagation();
            }
            // focus the container so it may receive keyboard events
            this.containerElem.focus();
        };
        this.onMouseMove = (deltaX, deltaY) => {
            let delta = this.isHorizontal() ? deltaX : deltaY;
            if (delta !== 0) {
                delta /= this.scale;
                this.triggerCallback(this.props.scrollPosition + delta);
            }
        };
        this.onMouseMoveEnd = () => {
            this.mouseMoveTracker.release();
        };
        this.refContainer = (container) => {
            this.containerElem = container;
        };
        this.refThumb = (thumb) => {
            this.thumbElem = thumb;
        };
    }
    UNSAFE_componentWillMount() {
        this.wheelHandler = new WheelHandler_1.WheelHandler({
            onWheel: this.isHorizontal() ? this.onWheelX : this.onWheelY,
            shouldHandleScrollX: true,
            shouldHandleScrollY: true,
            stopPropagation: this.props.stopPropagation,
        });
        this.mouseMoveTracker = new MouseMoveTracker_1.MouseMoveTracker({
            elem: document.documentElement,
            onMouseMove: this.onMouseMove,
            onMouseMoveEnd: this.onMouseMoveEnd,
        });
    }
    componentWillUnmount() {
        this.mouseMoveTracker.release();
    }
    isHorizontal() {
        return this.props.orientation === 'horizontal';
    }
    fixPosition(position) {
        const max = this.props.contentSize - this.props.containerSize;
        return (0, clamp_1.default)(position, 0, max);
    }
    render() {
        const { prefixCls, className, scrollPosition, containerSize, contentSize, miniThumbSize, zIndex, scrollbarSize, } = this.props;
        // unscrollable
        if (containerSize < 1 || contentSize <= containerSize) {
            return null;
        }
        let scale = containerSize / contentSize;
        let thumbSize = containerSize * scale;
        if (thumbSize < miniThumbSize) {
            scale = (containerSize - miniThumbSize) / (contentSize - containerSize);
            thumbSize = miniThumbSize;
        }
        // cache
        this.scale = scale;
        this.thumbSize = thumbSize;
        let trackStyle;
        let thumbStyle;
        const horizontal = this.isHorizontal();
        if (horizontal) {
            trackStyle = {
                width: containerSize,
                height: scrollbarSize,
            };
            thumbStyle = {
                width: thumbSize,
                transform: `translate(${scrollPosition * scale}px, 0)`,
            };
        }
        else {
            trackStyle = {
                width: scrollbarSize,
                height: containerSize,
            };
            thumbStyle = {
                height: thumbSize,
                transform: `translate(0, ${scrollPosition * scale}px)`,
            };
        }
        if (zIndex) {
            trackStyle.zIndex = zIndex;
        }
        const baseCls = `${prefixCls}-scrollbar`;
        return (react_1.default.createElement("div", { role: "button", className: (0, classnames_1.default)(baseCls, {
                [`${baseCls}-vertical`]: !horizontal,
                [`${baseCls}-horizontal`]: horizontal,
            }, className), style: trackStyle, tabIndex: 0, ref: this.refContainer, onKeyDown: this.onKeyDown, onMouseDown: this.onMouseDown, onWheel: this.wheelHandler.onWheel },
            react_1.default.createElement("div", { ref: this.refThumb, style: thumbStyle, className: `${baseCls}-thumb` })));
    }
}
exports.Scrollbar = Scrollbar;
(function (Scrollbar) {
    Scrollbar.defaultProps = {
        prefixCls: 'x6',
        orientation: 'vertical',
        contentSize: 0,
        containerSize: 0,
        defaultPosition: 0,
        scrollbarSize: 4,
        miniThumbSize: 16,
        keyboardScrollAmount: 40,
    };
})(Scrollbar = exports.Scrollbar || (exports.Scrollbar = {}));
//# sourceMappingURL=index.js.map