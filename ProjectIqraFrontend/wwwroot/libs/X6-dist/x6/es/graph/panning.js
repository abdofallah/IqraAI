var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { ModifierKey, Dom } from '@antv/x6-common';
import { Base } from './base';
export class PanningManager extends Base {
    get widgetOptions() {
        return this.options.panning;
    }
    get pannable() {
        return this.widgetOptions && this.widgetOptions.enabled === true;
    }
    init() {
        this.onRightMouseDown = this.onRightMouseDown.bind(this);
        this.onSpaceKeyDown = this.onSpaceKeyDown.bind(this);
        this.onSpaceKeyUp = this.onSpaceKeyUp.bind(this);
        this.startListening();
        this.updateClassName();
    }
    startListening() {
        this.graph.on('blank:mousedown', this.onMouseDown, this);
        this.graph.on('node:unhandled:mousedown', this.onMouseDown, this);
        this.graph.on('edge:unhandled:mousedown', this.onMouseDown, this);
        Dom.Event.on(this.graph.container, 'mousedown', this.onRightMouseDown);
        Dom.Event.on(document.body, {
            keydown: this.onSpaceKeyDown,
            keyup: this.onSpaceKeyUp,
        });
        this.mousewheelHandle = new Dom.MouseWheelHandle(this.graph.container, this.onMouseWheel.bind(this), this.allowMouseWheel.bind(this));
        this.mousewheelHandle.enable();
    }
    stopListening() {
        this.graph.off('blank:mousedown', this.onMouseDown, this);
        this.graph.off('node:unhandled:mousedown', this.onMouseDown, this);
        this.graph.off('edge:unhandled:mousedown', this.onMouseDown, this);
        Dom.Event.off(this.graph.container, 'mousedown', this.onRightMouseDown);
        Dom.Event.off(document.body, {
            keydown: this.onSpaceKeyDown,
            keyup: this.onSpaceKeyUp,
        });
        if (this.mousewheelHandle) {
            this.mousewheelHandle.disable();
        }
    }
    allowPanning(e, strict) {
        ;
        e.spaceKey = this.isSpaceKeyPressed;
        return (this.pannable &&
            ModifierKey.isMatch(e, this.widgetOptions.modifiers, strict));
    }
    startPanning(evt) {
        const e = this.view.normalizeEvent(evt);
        this.clientX = e.clientX;
        this.clientY = e.clientY;
        this.panning = true;
        this.updateClassName();
        Dom.Event.on(document.body, {
            'mousemove.panning touchmove.panning': this.pan.bind(this),
            'mouseup.panning touchend.panning': this.stopPanning.bind(this),
            'mouseleave.panning': this.stopPanning.bind(this),
        });
        Dom.Event.on(window, 'mouseup.panning', this.stopPanning.bind(this));
    }
    pan(evt) {
        const e = this.view.normalizeEvent(evt);
        const dx = e.clientX - this.clientX;
        const dy = e.clientY - this.clientY;
        this.clientX = e.clientX;
        this.clientY = e.clientY;
        this.graph.translateBy(dx, dy);
    }
    // eslint-disable-next-line
    stopPanning(e) {
        this.panning = false;
        this.updateClassName();
        Dom.Event.off(document.body, '.panning');
        Dom.Event.off(window, '.panning');
    }
    updateClassName() {
        const container = this.view.container;
        const panning = this.view.prefixClassName('graph-panning');
        const pannable = this.view.prefixClassName('graph-pannable');
        if (this.pannable) {
            if (this.panning) {
                Dom.addClass(container, panning);
                Dom.removeClass(container, pannable);
            }
            else {
                Dom.removeClass(container, panning);
                Dom.addClass(container, pannable);
            }
        }
        else {
            Dom.removeClass(container, panning);
            Dom.removeClass(container, pannable);
        }
    }
    onMouseDown({ e }) {
        if (!this.allowBlankMouseDown(e)) {
            return;
        }
        const selection = this.graph.getPlugin('selection');
        const allowRubberband = selection && selection.allowRubberband(e, true);
        if (this.allowPanning(e, true) ||
            (this.allowPanning(e) && !allowRubberband)) {
            this.startPanning(e);
        }
    }
    onRightMouseDown(e) {
        const eventTypes = this.widgetOptions.eventTypes;
        if (!((eventTypes === null || eventTypes === void 0 ? void 0 : eventTypes.includes('rightMouseDown')) && e.button === 2)) {
            return;
        }
        if (this.allowPanning(e, true)) {
            this.startPanning(e);
        }
    }
    onMouseWheel(e, deltaX, deltaY) {
        this.graph.translateBy(-deltaX, -deltaY);
    }
    onSpaceKeyDown(e) {
        if (e.which === 32) {
            this.isSpaceKeyPressed = true;
        }
    }
    onSpaceKeyUp(e) {
        if (e.which === 32) {
            this.isSpaceKeyPressed = false;
        }
    }
    allowBlankMouseDown(e) {
        const eventTypes = this.widgetOptions.eventTypes;
        return (((eventTypes === null || eventTypes === void 0 ? void 0 : eventTypes.includes('leftMouseDown')) && e.button === 0) ||
            ((eventTypes === null || eventTypes === void 0 ? void 0 : eventTypes.includes('mouseWheelDown')) && e.button === 1));
    }
    allowMouseWheel(e) {
        var _a;
        return (this.pannable &&
            !e.ctrlKey &&
            ((_a = this.widgetOptions.eventTypes) === null || _a === void 0 ? void 0 : _a.includes('mouseWheel')));
    }
    autoPanning(x, y) {
        const buffer = 10;
        const graphArea = this.graph.getGraphArea();
        let dx = 0;
        let dy = 0;
        if (x <= graphArea.left + buffer) {
            dx = -buffer;
        }
        if (y <= graphArea.top + buffer) {
            dy = -buffer;
        }
        if (x >= graphArea.right - buffer) {
            dx = buffer;
        }
        if (y >= graphArea.bottom - buffer) {
            dy = buffer;
        }
        if (dx !== 0 || dy !== 0) {
            this.graph.translateBy(-dx, -dy);
        }
    }
    enablePanning() {
        if (!this.pannable) {
            this.widgetOptions.enabled = true;
            this.updateClassName();
        }
    }
    disablePanning() {
        if (this.pannable) {
            this.widgetOptions.enabled = false;
            this.updateClassName();
        }
    }
    dispose() {
        this.stopListening();
    }
}
__decorate([
    Base.dispose()
], PanningManager.prototype, "dispose", null);
//# sourceMappingURL=panning.js.map