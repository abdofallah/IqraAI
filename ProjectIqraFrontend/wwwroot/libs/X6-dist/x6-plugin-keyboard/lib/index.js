"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Keyboard = void 0;
const x6_1 = require("@antv/x6");
const keyboard_1 = require("./keyboard");
require("./api");
class Keyboard extends x6_1.Disposable {
    constructor(options = {}) {
        super();
        this.name = 'keyboard';
        this.options = Object.assign({ enabled: true }, options);
    }
    init(graph) {
        this.keyboardImpl = new keyboard_1.KeyboardImpl(Object.assign(Object.assign({}, this.options), { graph }));
    }
    // #region api
    isEnabled() {
        return !this.keyboardImpl.disabled;
    }
    enable() {
        this.keyboardImpl.enable();
    }
    disable() {
        this.keyboardImpl.disable();
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
        else if (this.isEnabled()) {
            this.disable();
        }
        else {
            this.enable();
        }
        return this;
    }
    bindKey(keys, callback, action) {
        this.keyboardImpl.on(keys, callback, action);
        return this;
    }
    trigger(key, action) {
        this.keyboardImpl.trigger(key, action);
        return this;
    }
    clear() {
        this.keyboardImpl.clear();
        return this;
    }
    unbindKey(keys, action) {
        this.keyboardImpl.off(keys, action);
        return this;
    }
    // #endregion
    dispose() {
        this.keyboardImpl.dispose();
    }
}
__decorate([
    x6_1.Disposable.dispose()
], Keyboard.prototype, "dispose", null);
exports.Keyboard = Keyboard;
//# sourceMappingURL=index.js.map