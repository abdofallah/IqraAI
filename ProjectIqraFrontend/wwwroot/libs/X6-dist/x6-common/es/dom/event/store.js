export var Store;
(function (Store) {
    const cache = new WeakMap();
    function ensure(target) {
        if (!cache.has(target)) {
            cache.set(target, { events: Object.create(null) });
        }
        return cache.get(target);
    }
    Store.ensure = ensure;
    function get(target) {
        return cache.get(target);
    }
    Store.get = get;
    function remove(target) {
        return cache.delete(target);
    }
    Store.remove = remove;
})(Store || (Store = {}));
//# sourceMappingURL=store.js.map