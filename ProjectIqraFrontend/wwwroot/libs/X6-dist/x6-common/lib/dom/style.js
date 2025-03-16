"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.hasScrollbars = exports.getComputedStyle = exports.setPrefixedStyle = void 0;
const prefix_1 = require("./prefix");
function setPrefixedStyle(style, name, value) {
    const vendor = (0, prefix_1.getVendorPrefixedName)(name);
    if (vendor != null) {
        style[vendor] = value;
    }
    style[name] = value;
}
exports.setPrefixedStyle = setPrefixedStyle;
function getComputedStyle(elem, name) {
    // IE9+
    const computed = elem.ownerDocument &&
        elem.ownerDocument.defaultView &&
        elem.ownerDocument.defaultView.opener
        ? elem.ownerDocument.defaultView.getComputedStyle(elem, null)
        : window.getComputedStyle(elem, null);
    if (computed && name) {
        return computed.getPropertyValue(name) || computed[name];
    }
    return computed;
}
exports.getComputedStyle = getComputedStyle;
function hasScrollbars(container) {
    const style = getComputedStyle(container);
    return (style != null && (style.overflow === 'scroll' || style.overflow === 'auto'));
}
exports.hasScrollbars = hasScrollbars;
//# sourceMappingURL=style.js.map