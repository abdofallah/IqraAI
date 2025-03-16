"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __exportStar = (this && this.__exportStar) || function(m, exports) {
    for (var p in m) if (p !== "default" && !Object.prototype.hasOwnProperty.call(exports, p)) __createBinding(exports, m, p);
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.flatten = exports.unsetByPath = exports.setByPath = exports.getByPath = exports.isMaliciousProp = exports.getBoolean = exports.getNumber = exports.getValue = exports.ensure = exports.defaultsDeep = exports.defaults = exports.cloneDeep = exports.clone = exports.isPlainObject = exports.isObject = exports.isEmpty = exports.isEqual = exports.merge = exports.pick = exports.has = void 0;
var lodash_es_1 = require("lodash-es");
Object.defineProperty(exports, "has", { enumerable: true, get: function () { return lodash_es_1.has; } });
Object.defineProperty(exports, "pick", { enumerable: true, get: function () { return lodash_es_1.pick; } });
Object.defineProperty(exports, "merge", { enumerable: true, get: function () { return lodash_es_1.merge; } });
Object.defineProperty(exports, "isEqual", { enumerable: true, get: function () { return lodash_es_1.isEqual; } });
Object.defineProperty(exports, "isEmpty", { enumerable: true, get: function () { return lodash_es_1.isEmpty; } });
Object.defineProperty(exports, "isObject", { enumerable: true, get: function () { return lodash_es_1.isObject; } });
Object.defineProperty(exports, "isPlainObject", { enumerable: true, get: function () { return lodash_es_1.isPlainObject; } });
Object.defineProperty(exports, "clone", { enumerable: true, get: function () { return lodash_es_1.clone; } });
Object.defineProperty(exports, "cloneDeep", { enumerable: true, get: function () { return lodash_es_1.cloneDeep; } });
Object.defineProperty(exports, "defaults", { enumerable: true, get: function () { return lodash_es_1.defaults; } });
Object.defineProperty(exports, "defaultsDeep", { enumerable: true, get: function () { return lodash_es_1.defaultsDeep; } });
__exportStar(require("./mixins"), exports);
__exportStar(require("./inherit"), exports);
function ensure(value, defaultValue) {
    return value != null ? value : defaultValue;
}
exports.ensure = ensure;
function getValue(obj, key, defaultValue) {
    const value = obj != null ? obj[key] : null;
    return defaultValue !== undefined ? ensure(value, defaultValue) : value;
}
exports.getValue = getValue;
function getNumber(obj, key, defaultValue) {
    let value = obj != null ? obj[key] : null;
    if (value == null) {
        return defaultValue;
    }
    value = +value;
    if (Number.isNaN(value) || !Number.isFinite(value)) {
        return defaultValue;
    }
    return value;
}
exports.getNumber = getNumber;
function getBoolean(obj, key, defaultValue) {
    const value = obj != null ? obj[key] : null;
    if (value == null) {
        return defaultValue;
    }
    return !!value;
}
exports.getBoolean = getBoolean;
function isMaliciousProp(prop) {
    return prop === '__proto__';
}
exports.isMaliciousProp = isMaliciousProp;
function getByPath(obj, path, delimiter = '/') {
    let ret;
    const keys = Array.isArray(path) ? path : path.split(delimiter);
    if (keys.length) {
        ret = obj;
        while (keys.length) {
            const key = keys.shift();
            if (Object(ret) === ret && key && key in ret) {
                ret = ret[key];
            }
            else {
                return undefined;
            }
        }
    }
    return ret;
}
exports.getByPath = getByPath;
function setByPath(obj, path, value, delimiter = '/') {
    const keys = Array.isArray(path) ? path : path.split(delimiter);
    const lastKey = keys.pop();
    if (lastKey && !isMaliciousProp(lastKey)) {
        let diver = obj;
        keys.forEach((key) => {
            if (!isMaliciousProp(key)) {
                if (diver[key] == null) {
                    diver[key] = {};
                }
                diver = diver[key];
            }
        });
        diver[lastKey] = value;
    }
    return obj;
}
exports.setByPath = setByPath;
function unsetByPath(obj, path, delimiter = '/') {
    const keys = Array.isArray(path) ? path.slice() : path.split(delimiter);
    const propertyToRemove = keys.pop();
    if (propertyToRemove) {
        if (keys.length > 0) {
            const parent = getByPath(obj, keys);
            if (parent) {
                delete parent[propertyToRemove];
            }
        }
        else {
            delete obj[propertyToRemove];
        }
    }
    return obj;
}
exports.unsetByPath = unsetByPath;
// eslint-disable-next-line default-param-last
function flatten(obj, delim = '/', stop) {
    const ret = {};
    Object.keys(obj).forEach((key) => {
        const val = obj[key];
        let deep = typeof val === 'object' || Array.isArray(val);
        if (deep && stop && stop(val)) {
            deep = false;
        }
        if (deep) {
            const flatObject = flatten(val, delim, stop);
            Object.keys(flatObject).forEach((flatKey) => {
                ret[key + delim + flatKey] = flatObject[flatKey];
            });
        }
        else {
            ret[key] = val;
        }
    });
    // eslint-disable-next-line no-restricted-syntax
    for (const key in obj) {
        if (!Object.prototype.hasOwnProperty.call(obj, key)) {
            continue;
        }
    }
    return ret;
}
exports.flatten = flatten;
//# sourceMappingURL=object.js.map