import { Graph } from '@antv/x6';
Graph.prototype.toSVG = function (callback, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.toSVG(callback, options);
    }
};
Graph.prototype.toPNG = function (callback, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.toPNG(callback, options);
    }
};
Graph.prototype.toJPEG = function (callback, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.toJPEG(callback, options);
    }
};
Graph.prototype.exportPNG = function (fileName, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.exportPNG(fileName, options);
    }
};
Graph.prototype.exportJPEG = function (fileName, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.exportJPEG(fileName, options);
    }
};
Graph.prototype.exportSVG = function (fileName, options) {
    const instance = this.getPlugin('export');
    if (instance) {
        instance.exportSVG(fileName, options);
    }
};
//# sourceMappingURL=api.js.map