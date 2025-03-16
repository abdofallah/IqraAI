"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Dictionary = void 0;
class Dictionary {
    constructor() {
        this.clear();
    }
    clear() {
        this.map = new WeakMap();
        this.arr = [];
    }
    has(key) {
        return this.map.has(key);
    }
    get(key) {
        return this.map.get(key);
    }
    set(key, value) {
        this.map.set(key, value);
        this.arr.push(key);
    }
    delete(key) {
        const index = this.arr.indexOf(key);
        if (index >= 0) {
            this.arr.splice(index, 1);
        }
        const ret = this.map.get(key);
        this.map.delete(key);
        return ret;
    }
    each(iterator) {
        this.arr.forEach((key) => {
            const value = this.map.get(key);
            iterator(value, key);
        });
    }
    dispose() {
        this.clear();
    }
}
exports.Dictionary = Dictionary;
//# sourceMappingURL=dictionary.js.map