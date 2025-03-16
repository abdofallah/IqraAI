"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.SizeManager = void 0;
const x6_common_1 = require("@antv/x6-common");
const base_1 = require("./base");
class SizeManager extends base_1.Base {
    getScroller() {
        const scroller = this.graph.getPlugin('scroller');
        if (scroller && scroller.options.enabled) {
            return scroller;
        }
        return null;
    }
    getContainer() {
        const scroller = this.getScroller();
        if (scroller) {
            return scroller.container.parentElement;
        }
        return this.graph.container.parentElement;
    }
    getSensorTarget() {
        const autoResize = this.options.autoResize;
        if (autoResize) {
            if (typeof autoResize === 'boolean') {
                return this.getContainer();
            }
            return autoResize;
        }
    }
    init() {
        const autoResize = this.options.autoResize;
        if (autoResize) {
            const target = this.getSensorTarget();
            if (target) {
                x6_common_1.SizeSensor.bind(target, () => {
                    const width = target.offsetWidth;
                    const height = target.offsetHeight;
                    this.resize(width, height);
                });
            }
        }
    }
    resize(width, height) {
        const scroller = this.getScroller();
        if (scroller) {
            scroller.resize(width, height);
        }
        else {
            this.graph.transform.resize(width, height);
        }
    }
    dispose() {
        x6_common_1.SizeSensor.clear(this.graph.container);
    }
}
__decorate([
    base_1.Base.dispose()
], SizeManager.prototype, "dispose", null);
exports.SizeManager = SizeManager;
//# sourceMappingURL=size.js.map