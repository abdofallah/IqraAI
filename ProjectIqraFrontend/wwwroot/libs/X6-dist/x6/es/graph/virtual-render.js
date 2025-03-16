var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { FunctionExt } from '@antv/x6-common';
import { Base } from './base';
export class VirtualRenderManager extends Base {
    init() {
        this.resetRenderArea = FunctionExt.throttle(this.resetRenderArea, 200, {
            leading: true,
        });
        this.resetRenderArea();
        this.startListening();
    }
    startListening() {
        this.graph.on('translate', this.resetRenderArea, this);
        this.graph.on('scale', this.resetRenderArea, this);
        this.graph.on('resize', this.resetRenderArea, this);
    }
    stopListening() {
        this.graph.off('translate', this.resetRenderArea, this);
        this.graph.off('scale', this.resetRenderArea, this);
        this.graph.off('resize', this.resetRenderArea, this);
    }
    enableVirtualRender() {
        this.options.virtual = true;
        this.resetRenderArea();
    }
    disableVirtualRender() {
        this.options.virtual = false;
        this.graph.renderer.setRenderArea(undefined);
    }
    resetRenderArea() {
        if (this.options.virtual) {
            const renderArea = this.graph.getGraphArea();
            this.graph.renderer.setRenderArea(renderArea);
        }
    }
    dispose() {
        this.stopListening();
    }
}
__decorate([
    Base.dispose()
], VirtualRenderManager.prototype, "dispose", null);
//# sourceMappingURL=virtual-render.js.map