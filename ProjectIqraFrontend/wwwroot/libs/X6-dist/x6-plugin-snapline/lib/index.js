"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Snapline = void 0;
const x6_1 = require("@antv/x6");
const snapline_1 = require("./snapline");
const raw_1 = require("./style/raw");
require("./api");
class Snapline extends x6_1.Disposable {
    constructor(options = {}) {
        super();
        this.name = 'snapline';
        this.options = Object.assign({ enabled: true, tolerance: 10 }, options);
        x6_1.CssLoader.ensure(this.name, raw_1.content);
    }
    init(graph) {
        this.snaplineImpl = new snapline_1.SnaplineImpl(Object.assign(Object.assign({}, this.options), { graph }));
    }
    // #region api
    isEnabled() {
        return !this.snaplineImpl.disabled;
    }
    enable() {
        this.snaplineImpl.enable();
    }
    disable() {
        this.snaplineImpl.disable();
    }
    toggleEnabled(enabled) {
        if (enabled != null) {
            if (enabled !== this.isEnabled()) {
                if (enabled) {
                    this.enable();
                }
                else {
                    this.disable();
                }
            }
        }
        else {
            if (this.isEnabled()) {
                this.disable();
            }
            else {
                this.enable();
            }
            return this;
        }
    }
    hide() {
        this.snaplineImpl.hide();
        return this;
    }
    setFilter(filter) {
        this.snaplineImpl.setFilter(filter);
        return this;
    }
    isOnResizingEnabled() {
        return this.snaplineImpl.options.resizing === true;
    }
    enableOnResizing() {
        this.snaplineImpl.options.resizing = true;
        return this;
    }
    disableOnResizing() {
        this.snaplineImpl.options.resizing = false;
        return this;
    }
    toggleOnResizing(enableOnResizing) {
        if (enableOnResizing != null) {
            if (enableOnResizing !== this.isOnResizingEnabled()) {
                if (enableOnResizing) {
                    this.enableOnResizing();
                }
                else {
                    this.disableOnResizing();
                }
            }
        }
        else if (this.isOnResizingEnabled()) {
            this.disableOnResizing();
        }
        else {
            this.enableOnResizing();
        }
        return this;
    }
    isSharp() {
        return this.snaplineImpl.options.sharp === true;
    }
    enableSharp() {
        this.snaplineImpl.options.sharp = true;
        return this;
    }
    disableSharp() {
        this.snaplineImpl.options.sharp = false;
        return this;
    }
    toggleSharp(sharp) {
        if (sharp != null) {
            if (sharp !== this.isSharp()) {
                if (sharp) {
                    this.enableSharp();
                }
                else {
                    this.disableSharp();
                }
            }
        }
        else if (this.isSharp()) {
            this.disableSharp();
        }
        else {
            this.enableSharp();
        }
        return this;
    }
    getTolerance() {
        return this.snaplineImpl.options.tolerance;
    }
    setTolerance(tolerance) {
        this.snaplineImpl.options.tolerance = tolerance;
        return this;
    }
    captureCursorOffset(e) {
        this.snaplineImpl.captureCursorOffset(e);
    }
    snapOnMoving(args) {
        this.snaplineImpl.snapOnMoving(args);
    }
    // #endregion
    dispose() {
        this.snaplineImpl.dispose();
        x6_1.CssLoader.clean(this.name);
    }
}
__decorate([
    x6_1.Disposable.dispose()
], Snapline.prototype, "dispose", null);
exports.Snapline = Snapline;
//# sourceMappingURL=index.js.map