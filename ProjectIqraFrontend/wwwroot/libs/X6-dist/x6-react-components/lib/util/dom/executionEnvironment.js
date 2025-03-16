"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.isInWorker = exports.canUseViewport = exports.canUseEventListeners = exports.canUseWorkers = exports.canUseDOM = void 0;
exports.canUseDOM = !!(typeof window !== 'undefined' &&
    window.document &&
    window.document.createElement);
exports.canUseWorkers = typeof Worker !== 'undefined';
exports.canUseEventListeners = exports.canUseDOM && !!(window.addEventListener || window.attachEvent);
exports.canUseViewport = exports.canUseDOM && !!window.screen;
exports.isInWorker = !exports.canUseDOM;
//# sourceMappingURL=executionEnvironment.js.map