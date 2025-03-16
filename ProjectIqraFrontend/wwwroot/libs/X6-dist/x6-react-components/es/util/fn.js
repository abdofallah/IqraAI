import { translate } from './dom/translate';
const functionReturnTrue = () => true;
const functionReturnFalse = () => false;
export function getJudgeFunction(fn) {
    if (typeof fn !== 'function') {
        return fn ? functionReturnTrue : functionReturnFalse;
    }
    return fn;
}
export function translatePosition(style, x, y, initialRender = false) {
    if (initialRender) {
        style.left = `${x}px`;
        style.top = `${y}px`;
    }
    else {
        translate(style, x, y);
    }
}
export function debounce(func, wait, context, setTimeoutFunc = window.setTimeout, clearTimeoutFunc = window.clearTimeout) {
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
//# sourceMappingURL=fn.js.map