"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Unit = void 0;
let millimeterSize;
const supportedUnits = {
    px(val) {
        return val;
    },
    mm(val) {
        return millimeterSize * val;
    },
    cm(val) {
        return millimeterSize * val * 10;
    },
    in(val) {
        return millimeterSize * val * 25.4;
    },
    pt(val) {
        return millimeterSize * ((25.4 * val) / 72);
    },
    pc(val) {
        return millimeterSize * ((25.4 * val) / 6);
    },
};
// eslint-disable-next-line
var Unit;
(function (Unit) {
    function measure(cssWidth, cssHeight, unit) {
        const div = document.createElement('div');
        const style = div.style;
        style.display = 'inline-block';
        style.position = 'absolute';
        style.left = '-15000px';
        style.top = '-15000px';
        style.width = cssWidth + (unit || 'px');
        style.height = cssHeight + (unit || 'px');
        document.body.appendChild(div);
        const rect = div.getBoundingClientRect();
        const size = {
            width: rect.width || 0,
            height: rect.height || 0,
        };
        document.body.removeChild(div);
        return size;
    }
    Unit.measure = measure;
    function toPx(val, unit) {
        if (millimeterSize == null) {
            millimeterSize = measure('1', '1', 'mm').width;
        }
        const convert = unit ? supportedUnits[unit] : null;
        if (convert) {
            return convert(val);
        }
        return val;
    }
    Unit.toPx = toPx;
})(Unit = exports.Unit || (exports.Unit = {}));
//# sourceMappingURL=index.js.map