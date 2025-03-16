"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.isEventSupported = void 0;
const executionEnvironment_1 = require("./executionEnvironment");
let useHasFeature;
if (executionEnvironment_1.canUseDOM) {
    useHasFeature =
        document.implementation &&
            document.implementation.hasFeature &&
            // always returns true in newer browsers as per the standard.
            // @see http://dom.spec.whatwg.org/#dom-domimplementation-hasfeature
            document.implementation.hasFeature('', '') !== true;
}
/**
 * Checks if an event is supported in the current execution environment.
 *
 * NOTE: This will not work correctly for non-generic events such as `change`,
 * `reset`, `load`, `error`, and `select`.
 *
 * Borrows from Modernizr.
 *
 * @param {string} eventNameSuffix Event name, e.g. "click".
 * @param {?boolean} capture Check if the capture phase is supported.
 * @return {boolean} True if the event is supported.
 */
function isEventSupported(eventNameSuffix, capture) {
    if ((!executionEnvironment_1.canUseDOM || capture) && !('addEventListener' in document)) {
        return false;
    }
    const eventName = `on${eventNameSuffix}`;
    let isSupported = eventName in document;
    if (!isSupported) {
        const element = document.createElement('div');
        element.setAttribute(eventName, 'return;');
        isSupported = typeof element[eventName] === 'function';
    }
    if (!isSupported && useHasFeature && eventNameSuffix === 'wheel') {
        // This is the only way to test support for the `wheel` event in IE9+.
        isSupported = document.implementation.hasFeature('Events.wheel', '3.0');
    }
    return isSupported;
}
exports.isEventSupported = isEventSupported;
//# sourceMappingURL=isEventSupported.js.map