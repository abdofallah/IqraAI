"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.data = exports.setData = exports.getData = void 0;
const string_1 = require("../string");
const dataset = new WeakMap();
function getData(elem, name) {
    const key = string_1.StringExt.camelCase(name);
    const cache = dataset.get(elem);
    if (cache) {
        return cache[key];
    }
}
exports.getData = getData;
function setData(elem, name, value) {
    const key = string_1.StringExt.camelCase(name);
    const cache = dataset.get(elem);
    if (cache) {
        cache[key] = value;
    }
    else {
        dataset.set(elem, {
            [key]: value,
        });
    }
}
exports.setData = setData;
function data(elem, name, value) {
    if (!name) {
        const datas = {};
        Object.keys(dataset).forEach((key) => {
            datas[key] = getData(elem, key);
        });
        return datas;
    }
    if (typeof name === 'string') {
        if (value === undefined) {
            return getData(elem, name);
        }
        setData(elem, name, value);
        return;
    }
    // eslint-disable-next-line
    for (const key in name) {
        data(elem, key, name[key]);
    }
}
exports.data = data;
//# sourceMappingURL=data.js.map