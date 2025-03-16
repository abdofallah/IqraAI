import { Graph } from '@antv/x6';
Graph.prototype.createTransformWidget = function (node) {
    const transform = this.getPlugin('transform');
    if (transform) {
        transform.createWidget(node);
    }
    return this;
};
Graph.prototype.clearTransformWidgets = function () {
    const transform = this.getPlugin('transform');
    if (transform) {
        transform.clearWidgets();
    }
    return this;
};
//# sourceMappingURL=api.js.map