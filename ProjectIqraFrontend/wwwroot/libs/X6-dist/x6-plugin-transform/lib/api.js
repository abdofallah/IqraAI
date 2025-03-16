"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const x6_1 = require("@antv/x6");
x6_1.Graph.prototype.createTransformWidget = function (node) {
    const transform = this.getPlugin('transform');
    if (transform) {
        transform.createWidget(node);
    }
    return this;
};
x6_1.Graph.prototype.clearTransformWidgets = function () {
    const transform = this.getPlugin('transform');
    if (transform) {
        transform.clearWidgets();
    }
    return this;
};
//# sourceMappingURL=api.js.map