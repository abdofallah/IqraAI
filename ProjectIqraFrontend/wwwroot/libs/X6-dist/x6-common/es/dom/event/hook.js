export var EventHook;
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
})(EventHook || (EventHook = {}));
//# sourceMappingURL=hook.js.map