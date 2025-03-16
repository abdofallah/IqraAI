"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.createSensor = void 0;
const object_1 = require("./object");
const observer_1 = require("./observer");
exports.createSensor = typeof ResizeObserver !== 'undefined'
    ? observer_1.createSensor
    : object_1.createSensor;
//# sourceMappingURL=index.js.map