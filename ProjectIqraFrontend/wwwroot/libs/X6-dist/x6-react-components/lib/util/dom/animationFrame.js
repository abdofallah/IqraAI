"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.requestAnimationFrame = exports.cancelAnimationFrame = void 0;
let animationFrameTime = 0;
const nativeRequestAnimationFrame = window.requestAnimationFrame || window.webkitRequestAnimationFrame;
exports.cancelAnimationFrame = (window.cancelAnimationFrame ||
    window.webkitCancelAnimationFrame ||
    window.clearTimeout).bind(window);
exports.requestAnimationFrame = nativeRequestAnimationFrame
    ? nativeRequestAnimationFrame.bind(window)
    : (callback) => {
        const currTime = Date.now();
        const timeDelay = Math.max(0, 16 - (currTime - animationFrameTime));
        animationFrameTime = currTime + timeDelay;
        return window.setTimeout(() => {
            callback(Date.now());
        }, timeDelay);
    };
//# sourceMappingURL=animationFrame.js.map