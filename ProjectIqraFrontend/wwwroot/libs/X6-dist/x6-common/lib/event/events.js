"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Events = void 0;
const util_1 = require("./util");
const function_1 = require("../function");
class Events {
    constructor() {
        this.listeners = {};
    }
    on(name, handler, context) {
        if (handler == null) {
            return this;
        }
        if (!this.listeners[name]) {
            this.listeners[name] = [];
        }
        const cache = this.listeners[name];
        cache.push(handler, context);
        return this;
    }
    once(name, handler, context) {
        const cb = (...args) => {
            this.off(name, cb);
            return (0, util_1.call)([handler, context], args);
        };
        return this.on(name, cb, this);
    }
    off(name, handler, context) {
        // remove all events.
        if (!(name || handler || context)) {
            this.listeners = {};
            return this;
        }
        const listeners = this.listeners;
        const names = name ? [name] : Object.keys(listeners);
        names.forEach((n) => {
            const cache = listeners[n];
            if (!cache) {
                return;
            }
            // remove all events with specified name.
            if (!(handler || context)) {
                delete listeners[n];
                return;
            }
            for (let i = cache.length - 2; i >= 0; i -= 2) {
                if (!((handler && cache[i] !== handler) ||
                    (context && cache[i + 1] !== context))) {
                    cache.splice(i, 2);
                }
            }
        });
        return this;
    }
    trigger(name, ...args) {
        let returned = true;
        if (name !== '*') {
            const list = this.listeners[name];
            if (list != null) {
                returned = (0, util_1.call)([...list], args);
            }
        }
        const list = this.listeners['*'];
        if (list != null) {
            return function_1.FunctionExt.toAsyncBoolean([
                returned,
                (0, util_1.call)([...list], [name, ...args]),
            ]);
        }
        return returned;
    }
    emit(name, ...args) {
        return this.trigger(name, ...args);
    }
}
exports.Events = Events;
//# sourceMappingURL=events.js.map