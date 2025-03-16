"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.WheelHandler = void 0;
const fn_1 = require("../fn");
const normalizeWheel_1 = require("./normalizeWheel");
const animationFrame_1 = require("./animationFrame");
class WheelHandler {
    constructor(options) {
        this.onWheel = (e) => {
            const normalizedEvent = (0, normalizeWheel_1.normalizeWheel)(e);
            const { pixelX, pixelY } = normalizedEvent;
            const deltaX = this.deltaX + pixelX;
            const deltaY = this.deltaY + pixelY;
            const handleScrollX = this.shouldHandleScrollX(deltaX, deltaY);
            const handleScrollY = this.shouldHandleScrollY(deltaY, deltaX);
            if (!handleScrollX && !handleScrollY) {
                return;
            }
            this.deltaX += handleScrollX ? pixelX : 0;
            this.deltaY += handleScrollY ? pixelY : 0;
            let changed;
            if (this.deltaX !== 0 || this.deltaY !== 0) {
                if (this.stopPropagation()) {
                    e.stopPropagation();
                }
                changed = true;
            }
            if (changed === true && this.animationFrameID == null) {
                this.animationFrameID = (0, animationFrame_1.requestAnimationFrame)(this.didWheel);
            }
        };
        this.didWheel = () => {
            this.animationFrameID = null;
            if (this.callback) {
                this.callback(this.deltaX, this.deltaY);
            }
            this.deltaX = 0;
            this.deltaY = 0;
        };
        this.callback = options.onWheel;
        this.stopPropagation = (0, fn_1.getJudgeFunction)(options.stopPropagation);
        this.shouldHandleScrollX = (0, fn_1.getJudgeFunction)(options.shouldHandleScrollX);
        this.shouldHandleScrollY = (0, fn_1.getJudgeFunction)(options.shouldHandleScrollY);
        this.deltaX = 0;
        this.deltaY = 0;
    }
}
exports.WheelHandler = WheelHandler;
//# sourceMappingURL=WheelHandler.js.map