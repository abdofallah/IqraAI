"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.MouseWheel = void 0;
const x6_common_1 = require("@antv/x6-common");
const base_1 = require("./base");
class MouseWheel extends base_1.Base {
    constructor() {
        super(...arguments);
        this.cumulatedFactor = 1;
    }
    get widgetOptions() {
        return this.options.mousewheel;
    }
    init() {
        this.container = this.graph.container;
        this.target = this.widgetOptions.global ? document : this.container;
        this.mousewheelHandle = new x6_common_1.Dom.MouseWheelHandle(this.target, this.onMouseWheel.bind(this), this.allowMouseWheel.bind(this));
        if (this.widgetOptions.enabled) {
            this.enable(true);
        }
    }
    get disabled() {
        return this.widgetOptions.enabled !== true;
    }
    enable(force) {
        if (this.disabled || force) {
            this.widgetOptions.enabled = true;
            this.mousewheelHandle.enable();
        }
    }
    disable() {
        if (!this.disabled) {
            this.widgetOptions.enabled = false;
            this.mousewheelHandle.disable();
        }
    }
    allowMouseWheel(e) {
        const guard = this.widgetOptions.guard;
        return ((guard == null || guard(e)) &&
            x6_common_1.ModifierKey.isMatch(e, this.widgetOptions.modifiers));
    }
    onMouseWheel(e) {
        const guard = this.widgetOptions.guard;
        if ((guard == null || guard(e)) &&
            x6_common_1.ModifierKey.isMatch(e, this.widgetOptions.modifiers)) {
            const factor = this.widgetOptions.factor || 1.2;
            if (this.currentScale == null) {
                this.startPos = { x: e.clientX, y: e.clientY };
                this.currentScale = this.graph.transform.getScale().sx;
            }
            const delta = e.deltaY;
            if (delta < 0) {
                // zoomin
                // ------
                // Switches to 1% zoom steps below 15%
                if (this.currentScale < 0.15) {
                    this.cumulatedFactor = (this.currentScale + 0.01) / this.currentScale;
                }
                else {
                    // Uses to 5% zoom steps for better grid rendering in
                    // webkit and to avoid rounding errors for zoom steps
                    this.cumulatedFactor =
                        Math.round(this.currentScale * factor * 20) / 20 / this.currentScale;
                }
                if (this.cumulatedFactor <= 1) {
                    this.cumulatedFactor = 1.05;
                }
            }
            else {
                // zoomout
                // -------
                // Switches to 1% zoom steps below 15%
                if (this.currentScale <= 0.15) {
                    this.cumulatedFactor = (this.currentScale - 0.01) / this.currentScale;
                }
                else {
                    // Uses to 5% zoom steps for better grid rendering in
                    // webkit and to avoid rounding errors for zoom steps
                    this.cumulatedFactor =
                        Math.round(this.currentScale * (1 / factor) * 20) /
                            20 /
                            this.currentScale;
                }
                if (this.cumulatedFactor >= 1) {
                    this.cumulatedFactor = 0.95;
                }
            }
            this.cumulatedFactor = Math.max(0.01, Math.min(this.currentScale * this.cumulatedFactor, 160) /
                this.currentScale);
            const currentScale = this.currentScale;
            let targetScale = this.graph.transform.clampScale(currentScale * this.cumulatedFactor);
            const minScale = this.widgetOptions.minScale || Number.MIN_SAFE_INTEGER;
            const maxScale = this.widgetOptions.maxScale || Number.MAX_SAFE_INTEGER;
            targetScale = x6_common_1.NumberExt.clamp(targetScale, minScale, maxScale);
            if (targetScale !== currentScale) {
                if (this.widgetOptions.zoomAtMousePosition) {
                    const hasScroller = !!this.graph.getPlugin('scroller');
                    const origin = hasScroller
                        ? this.graph.clientToLocal(this.startPos)
                        : this.graph.clientToGraph(this.startPos);
                    this.graph.zoom(targetScale, {
                        absolute: true,
                        center: origin.clone(),
                    });
                }
                else {
                    this.graph.zoom(targetScale, { absolute: true });
                }
            }
            this.currentScale = null;
            this.cumulatedFactor = 1;
        }
    }
    dispose() {
        this.disable();
    }
}
__decorate([
    x6_common_1.Disposable.dispose()
], MouseWheel.prototype, "dispose", null);
exports.MouseWheel = MouseWheel;
//# sourceMappingURL=mousewheel.js.map