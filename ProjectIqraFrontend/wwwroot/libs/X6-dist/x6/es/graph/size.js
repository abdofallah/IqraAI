var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { SizeSensor } from '@antv/x6-common';
import { Base } from './base';
export class SizeManager extends Base {
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
                SizeSensor.bind(target, () => {
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
        SizeSensor.clear(this.graph.container);
    }
}
__decorate([
    Base.dispose()
], SizeManager.prototype, "dispose", null);
//# sourceMappingURL=size.js.map