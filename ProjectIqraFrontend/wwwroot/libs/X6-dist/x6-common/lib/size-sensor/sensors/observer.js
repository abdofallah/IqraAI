"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.createSensor = void 0;
const util_1 = require("./util");
function createSensor(element) {
    let sensor = null;
    let listeners = [];
    const trigger = (0, util_1.debounce)(() => {
        listeners.forEach((listener) => {
            listener(element);
        });
    });
    const create = () => {
        const s = new ResizeObserver(trigger);
        s.observe(element);
        trigger();
        return s;
    };
    const bind = (listener) => {
        if (!sensor) {
            sensor = create();
        }
        if (listeners.indexOf(listener) === -1) {
            listeners.push(listener);
        }
    };
    const destroy = () => {
        if (sensor) {
            sensor.disconnect();
            listeners = [];
            sensor = null;
        }
    };
    const unbind = (listener) => {
        const idx = listeners.indexOf(listener);
        if (idx !== -1) {
            listeners.splice(idx, 1);
        }
        // no listener, and sensor is exist then destroy the sensor
        if (listeners.length === 0 && sensor) {
            destroy();
        }
    };
    return {
        element,
        bind,
        destroy,
        unbind,
    };
}
exports.createSensor = createSensor;
//# sourceMappingURL=observer.js.map