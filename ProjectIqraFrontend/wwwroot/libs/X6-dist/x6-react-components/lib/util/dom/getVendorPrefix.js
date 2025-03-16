"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.getVendorPrefix = void 0;
const executionEnvironment_1 = require("./executionEnvironment");
const memoized = {};
const prefixes = ['Webkit', 'ms', 'Moz', 'O'];
const testStyle = executionEnvironment_1.canUseDOM ? document.createElement('div').style : {};
const hyphenPattern = /-(.)/g;
function camelize(str) {
    return str.replace(hyphenPattern, (_, char) => char.toUpperCase());
}
function getWithPrefix(name) {
    for (let i = 0; i < prefixes.length; i += 1) {
        const prefixedName = prefixes[i] + name;
        if (prefixedName in testStyle) {
            return prefixedName;
        }
    }
    return null;
}
function getVendorPrefix(property) {
    const name = camelize(property);
    if (memoized[name] === undefined) {
        const capitalizedName = name.charAt(0).toUpperCase() + name.slice(1);
        memoized[name] = name in testStyle ? name : getWithPrefix(capitalizedName);
    }
    return memoized[name];
}
exports.getVendorPrefix = getVendorPrefix;
//# sourceMappingURL=getVendorPrefix.js.map