"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.MouseMoveTracker = void 0;
const addEventListener_1 = __importDefault(require("rc-util/lib/Dom/addEventListener"));
const animationFrame_1 = require("./animationFrame");
class MouseMoveTracker {
    constructor(options) {
        this.onMouseMove = (e) => {
            const x = e.clientX;
            const y = e.clientY;
            this.deltaX += x - this.clientX;
            this.deltaY += y - this.clientY;
            if (this.animationFrameID == null) {
                this.animationFrameID = (0, animationFrame_1.requestAnimationFrame)(this.triggerOnMouseMoveCallback);
            }
            this.clientX = x;
            this.clientY = y;
            e.preventDefault();
        };
        this.onMouseUp = () => {
            if (this.animationFrameID) {
                (0, animationFrame_1.cancelAnimationFrame)(this.animationFrameID);
                this.triggerOnMouseMoveCallback();
            }
            this.triggerOnMouseMoveEndCallback(false);
        };
        this.triggerOnMouseMoveCallback = () => {
            this.animationFrameID = null;
            this.onMouseMoveCallback(this.deltaX, this.deltaY, {
                clientX: this.clientX,
                clientY: this.clientY,
            });
            this.deltaX = 0;
            this.deltaY = 0;
        };
        this.triggerOnMouseMoveEndCallback = (cancel) => {
            this.onMouseMoveEndCallback(cancel);
        };
        this.elem = options.elem || document.documentElement;
        this.onMouseMoveCallback = options.onMouseMove;
        this.onMouseMoveEndCallback = options.onMouseMoveEnd;
        this.animationFrameID = null;
    }
    capture(e) {
        if (!this.captured) {
            this.removeMouseMoveEvent = (0, addEventListener_1.default)(this.elem, 'mousemove', this.onMouseMove).remove;
            this.removeMouseUpEvent = (0, addEventListener_1.default)(this.elem, 'mouseup', this.onMouseUp).remove;
        }
        this.captured = true;
        if (!this.dragging) {
            this.clientX = e.clientX;
            this.clientY = e.clientY;
            this.deltaX = 0;
            this.deltaY = 0;
            this.dragging = true;
        }
        e.preventDefault();
    }
    release() {
        if (this.captured) {
            if (this.removeMouseMoveEvent != null) {
                this.removeMouseMoveEvent();
                this.removeMouseMoveEvent = null;
            }
            if (this.removeMouseUpEvent != null) {
                this.removeMouseUpEvent();
                this.removeMouseUpEvent = null;
            }
        }
        this.captured = false;
        if (this.dragging) {
            this.dragging = false;
            this.clientX = 0;
            this.clientY = 0;
            this.deltaX = 0;
            this.deltaY = 0;
        }
    }
    isDragging() {
        return this.dragging;
    }
}
exports.MouseMoveTracker = MouseMoveTracker;
//# sourceMappingURL=MouseMoveTracker.js.map