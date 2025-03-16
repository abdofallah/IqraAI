var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                t[p[i]] = s[p[i]];
        }
    return t;
};
import { Basecoat } from '@antv/x6';
import { ClipboardImpl } from './clipboard';
import './api';
export class Clipboard extends Basecoat {
    get disabled() {
        return this.options.enabled !== true;
    }
    get cells() {
        return this.clipboardImpl.cells;
    }
    constructor(options = {}) {
        super();
        this.name = 'clipboard';
        this.options = Object.assign({ enabled: true }, options);
    }
    init(graph) {
        this.graph = graph;
        this.clipboardImpl = new ClipboardImpl();
        this.clipboardImpl.deserialize(this.options);
    }
    // #region api
    isEnabled() {
        return !this.disabled;
    }
    enable() {
        if (this.disabled) {
            this.options.enabled = true;
        }
    }
    disable() {
        if (!this.disabled) {
            this.options.enabled = false;
        }
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
    isEmpty(options = {}) {
        return this.clipboardImpl.isEmpty(options);
    }
    getCellsInClipboard() {
        return this.cells;
    }
    clean(force) {
        if (!this.disabled || force) {
            this.clipboardImpl.clean();
            this.notify('clipboard:changed', { cells: [] });
        }
        return this;
    }
    copy(cells, options = {}) {
        if (!this.disabled) {
            this.clipboardImpl.copy(cells, this.graph, Object.assign(Object.assign({}, this.commonOptions), options));
            this.notify('clipboard:changed', { cells });
        }
        return this;
    }
    cut(cells, options = {}) {
        if (!this.disabled) {
            this.clipboardImpl.cut(cells, this.graph, Object.assign(Object.assign({}, this.commonOptions), options));
            this.notify('clipboard:changed', { cells });
        }
        return this;
    }
    paste(options = {}, graph = this.graph) {
        if (!this.disabled) {
            return this.clipboardImpl.paste(graph, Object.assign(Object.assign({}, this.commonOptions), options));
        }
        return [];
    }
    // #endregion
    get commonOptions() {
        const _a = this.options, { enabled } = _a, others = __rest(_a, ["enabled"]);
        return others;
    }
    notify(name, args) {
        this.trigger(name, args);
        this.graph.trigger(name, args);
    }
    dispose() {
        this.clean(true);
        this.off();
    }
}
__decorate([
    Basecoat.dispose()
], Clipboard.prototype, "dispose", null);
//# sourceMappingURL=index.js.map