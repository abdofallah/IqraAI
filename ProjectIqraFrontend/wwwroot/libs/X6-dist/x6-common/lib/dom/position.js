"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.position = exports.height = exports.width = exports.offset = void 0;
const css_1 = require("./css");
const elem_1 = require("./elem");
function offset(elem) {
    const rect = elem.getBoundingClientRect();
    const win = elem.ownerDocument.defaultView;
    return {
        top: rect.top + win.pageYOffset,
        left: rect.left + win.pageXOffset,
    };
}
exports.offset = offset;
function width(elem) {
    const rect = elem.getBoundingClientRect();
    return rect.width;
}
exports.width = width;
function height(elem) {
    const rect = elem.getBoundingClientRect();
    return rect.height;
}
exports.height = height;
function position(elem) {
    const isFixed = (0, css_1.computeStyle)(elem, 'position') === 'fixed';
    let offsetValue;
    if (isFixed) {
        const rect = elem.getBoundingClientRect();
        offsetValue = { left: rect.left, top: rect.top };
    }
    else {
        offsetValue = offset(elem);
    }
    if (!isFixed) {
        const doc = elem.ownerDocument;
        let offsetParent = elem.offsetParent || doc.documentElement;
        while ((offsetParent === doc.body || offsetParent === doc.documentElement) &&
            (0, css_1.computeStyle)(offsetParent, 'position') === 'static') {
            offsetParent = offsetParent.parentNode;
        }
        if (offsetParent !== elem && (0, elem_1.isElement)(offsetParent)) {
            const parentOffset = offset(offsetParent);
            offsetValue.top -=
                parentOffset.top + (0, css_1.computeStyleInt)(offsetParent, 'borderTopWidth');
            offsetValue.left -=
                parentOffset.left + (0, css_1.computeStyleInt)(offsetParent, 'borderLeftWidth');
        }
    }
    return {
        top: offsetValue.top - (0, css_1.computeStyleInt)(elem, 'marginTop'),
        left: offsetValue.left - (0, css_1.computeStyleInt)(elem, 'marginLeft'),
    };
}
exports.position = position;
//# sourceMappingURL=position.js.map