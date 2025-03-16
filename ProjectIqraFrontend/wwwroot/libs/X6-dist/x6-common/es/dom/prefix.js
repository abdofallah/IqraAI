const hyphenPattern = /-(.)/g;
function camelize(str) {
    return str.replace(hyphenPattern, (_, char) => char.toUpperCase());
}
const memoized = {};
const prefixes = ['webkit', 'ms', 'moz', 'o'];
const testStyle = typeof document !== 'undefined' ? document.createElement('div').style : {};
function getWithPrefix(name) {
    for (let i = 0; i < prefixes.length; i += 1) {
        const prefixedName = prefixes[i] + name;
        if (prefixedName in testStyle) {
            return prefixedName;
        }
    }
    return null;
}
export function getVendorPrefixedName(property) {
    const name = camelize(property);
    if (memoized[name] == null) {
        const capitalizedName = name.charAt(0).toUpperCase() + name.slice(1);
        memoized[name] = name in testStyle ? name : getWithPrefix(capitalizedName);
    }
    return memoized[name];
}
//# sourceMappingURL=prefix.js.map