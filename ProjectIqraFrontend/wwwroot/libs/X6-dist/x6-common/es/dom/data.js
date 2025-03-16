import { StringExt } from '../string';
const dataset = new WeakMap();
export function getData(elem, name) {
    const key = StringExt.camelCase(name);
    const cache = dataset.get(elem);
    if (cache) {
        return cache[key];
    }
}
export function setData(elem, name, value) {
    const key = StringExt.camelCase(name);
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
export function data(elem, name, value) {
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
//# sourceMappingURL=data.js.map