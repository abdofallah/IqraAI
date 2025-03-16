"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.EventHook = void 0;
var EventHook;
(function (EventHook) {
    const cache = {};
    function get(type) {
        return cache[type] || {};
    }
    EventHook.get = get;
    function register(type, hook) {
        cache[type] = hook;
    }
    EventHook.register = register;
    function unregister(type) {
        delete cache[type];
    }
    EventHook.unregister = unregister;
})(EventHook = exports.EventHook || (exports.EventHook = {}));
//# sourceMappingURL=hook.js.map