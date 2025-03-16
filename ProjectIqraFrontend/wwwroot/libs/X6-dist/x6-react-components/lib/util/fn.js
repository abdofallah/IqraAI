"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.debounce = exports.translatePosition = exports.getJudgeFunction = void 0;
const translate_1 = require("./dom/translate");
const functionReturnTrue = () => true;
const functionReturnFalse = () => false;
function getJudgeFunction(fn) {
    if (typeof fn !== 'function') {
        return fn ? functionReturnTrue : functionReturnFalse;
    }
    return fn;
}
exports.getJudgeFunction = getJudgeFunction;
function translatePosition(style, x, y, initialRender = false) {
    if (initialRender) {
        style.left = `${x}px`;
        style.top = `${y}px`;
    }
    else {
        (0, translate_1.translate)(style, x, y);
    }
}
exports.translatePosition = translatePosition;
function debounce(func, wait, context, setTimeoutFunc = window.setTimeout, clearTimeoutFunc = window.clearTimeout) {
    let timeout;
    const debouncer = (...args) => {
        debouncer.reset();
        const callback = () => {
            func.apply(context, args);
        };
        timeout = setTimeoutFunc(callback, wait);
    };
    debouncer.reset = () => {
        clearTimeoutFunc(timeout);
    };
    return debouncer;
}
exports.debounce = debounce;
//# sourceMappingURL=fn.js.map