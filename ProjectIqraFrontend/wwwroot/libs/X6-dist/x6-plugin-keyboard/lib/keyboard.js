"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.KeyboardImpl = void 0;
const mousetrap_1 = __importDefault(require("mousetrap"));
const x6_1 = require("@antv/x6");
class KeyboardImpl extends x6_1.Disposable {
    get graph() {
        return this.options.graph;
    }
    constructor(options) {
        super();
        this.options = options;
        const scroller = this.graph.getPlugin('scroller');
        this.container = scroller ? scroller.container : this.graph.container;
        if (options.global) {
            this.target = document;
        }
        else {
            this.target = this.container;
            if (!this.disabled) {
                // ensure the container focusable
                this.target.setAttribute('tabindex', '-1');
            }
            // change to mouseup event，prevent page stalling caused by focus
            this.graph.on('cell:mouseup', this.focus, this);
            this.graph.on('blank:mouseup', this.focus, this);
        }
        this.mousetrap = KeyboardImpl.createMousetrap(this);
    }
    get disabled() {
        return this.options.enabled !== true;
    }
    enable() {
        if (this.disabled) {
            this.options.enabled = true;
            if (this.target instanceof HTMLElement) {
                this.target.setAttribute('tabindex', '-1');
            }
        }
    }
    disable() {
        if (!this.disabled) {
            this.options.enabled = false;
            if (this.target instanceof HTMLElement) {
                this.target.removeAttribute('tabindex');
            }
        }
    }
    on(keys, callback, action) {
        this.mousetrap.bind(this.getKeys(keys), callback, action);
    }
    off(keys, action) {
        this.mousetrap.unbind(this.getKeys(keys), action);
    }
    clear() {
        this.mousetrap.reset();
    }
    trigger(key, action) {
        this.mousetrap.trigger(key, action);
    }
    focus(e) {
        const isInputEvent = this.isInputEvent(e.e);
        if (isInputEvent) {
            return;
        }
        const target = this.target;
        target.focus({
            preventScroll: true,
        });
    }
    getKeys(keys) {
        return (Array.isArray(keys) ? keys : [keys]).map((key) => this.formatkey(key));
    }
    formatkey(key) {
        const formated = key
            .toLocaleLowerCase()
            .replace(/\s/g, '')
            .replace('delete', 'del')
            .replace('cmd', 'command')
            .replace('arrowup', 'up')
            .replace('arrowright', 'right')
            .replace('arrowdown', 'down')
            .replace('arrowleft', 'left');
        const formatFn = this.options.format;
        if (formatFn) {
            return x6_1.FunctionExt.call(formatFn, this.graph, formated);
        }
        return formated;
    }
    isGraphEvent(e) {
        const target = e.target;
        const currentTarget = e.currentTarget;
        if (target) {
            if (target === this.target ||
                currentTarget === this.target ||
                target === document.body) {
                return true;
            }
            return x6_1.Dom.contains(this.container, target);
        }
        return false;
    }
    isInputEvent(e) {
        var _a;
        const target = e.target;
        const tagName = (_a = target === null || target === void 0 ? void 0 : target.tagName) === null || _a === void 0 ? void 0 : _a.toLowerCase();
        let isInput = ['input', 'textarea'].includes(tagName);
        if (x6_1.Dom.attr(target, 'contenteditable') === 'true') {
            isInput = true;
        }
        return isInput;
    }
    isEnabledForEvent(e) {
        const allowed = !this.disabled && this.isGraphEvent(e);
        const isInputEvent = this.isInputEvent(e);
        if (allowed) {
            if (isInputEvent && (e.key === 'Backspace' || e.key === 'Delete')) {
                return false;
            }
            if (this.options.guard) {
                return x6_1.FunctionExt.call(this.options.guard, this.graph, e);
            }
        }
        return allowed;
    }
    dispose() {
        this.mousetrap.reset();
    }
}
__decorate([
    x6_1.Disposable.dispose()
], KeyboardImpl.prototype, "dispose", null);
exports.KeyboardImpl = KeyboardImpl;
(function (KeyboardImpl) {
    function createMousetrap(keyboard) {
        const mousetrap = new mousetrap_1.default(keyboard.target);
        const stopCallback = mousetrap.stopCallback;
        mousetrap.stopCallback = (e, elem, combo) => {
            if (keyboard.isEnabledForEvent(e)) {
                if (stopCallback) {
                    return stopCallback.call(mousetrap, e, elem, combo);
                }
                return false;
            }
            return true;
        };
        return mousetrap;
    }
    KeyboardImpl.createMousetrap = createMousetrap;
})(KeyboardImpl = exports.KeyboardImpl || (exports.KeyboardImpl = {}));
//# sourceMappingURL=keyboard.js.map