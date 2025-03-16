"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const x6_1 = require("@antv/x6");
x6_1.Graph.prototype.toSVG = function (callback, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.toSVG(callback, options);
    }
};
x6_1.Graph.prototype.toPNG = function (callback, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.toPNG(callback, options);
    }
};
x6_1.Graph.prototype.toJPEG = function (callback, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.toJPEG(callback, options);
    }
};
x6_1.Graph.prototype.exportPNG = function (fileName, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.exportPNG(fileName, options);
    }
};
x6_1.Graph.prototype.exportJPEG = function (fileName, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.exportJPEG(fileName, options);
    }
};
x6_1.Graph.prototype.exportSVG = function (fileName, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.exportSVG(fileName, options);
    }
};
//# sourceMappingURL=api.js.map