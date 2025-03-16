"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.SizeSensor = void 0;
const sensors_1 = require("./sensors");
var SizeSensor;
(function (SizeSensor) {
    const cache = new WeakMap();
    function get(element) {
        let sensor = cache.get(element);
        if (sensor) {
            return sensor;
        }
        sensor = (0, sensors_1.createSensor)(element);
        cache.set(element, sensor);
        return sensor;
    }
    function remove(sensor) {
        sensor.destroy();
        cache.delete(sensor.element);
    }
    SizeSensor.bind = (element, cb) => {
        const sensor = get(element);
        sensor.bind(cb);
        return () => sensor.unbind(cb);
    };
    SizeSensor.clear = (element) => {
        const sensor = get(element);
        remove(sensor);
    };
})(SizeSensor = exports.SizeSensor || (exports.SizeSensor = {}));
//# sourceMappingURL=index.js.map