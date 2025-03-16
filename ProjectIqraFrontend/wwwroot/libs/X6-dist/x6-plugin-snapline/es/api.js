import { Graph } from '@antv/x6';
Graph.prototype.isSnaplineEnabled = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        return snapline.isEnabled();
    }
    return false;
};
Graph.prototype.enableSnapline = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.enable();
    }
    return this;
};
Graph.prototype.disableSnapline = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.disable();
    }
    return this;
};
Graph.prototype.toggleSnapline = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.toggleEnabled();
    }
    return this;
};
Graph.prototype.hideSnapline = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.hide();
    }
    return this;
};
Graph.prototype.setSnaplineFilter = function (filter) {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.setFilter(filter);
    }
    return this;
};
Graph.prototype.isSnaplineOnResizingEnabled = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        return snapline.isOnResizingEnabled();
    }
    return false;
};
Graph.prototype.enableSnaplineOnResizing = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.enableOnResizing();
    }
    return this;
};
Graph.prototype.disableSnaplineOnResizing = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.disableOnResizing();
    }
    return this;
};
Graph.prototype.toggleSnaplineOnResizing = function (enableOnResizing) {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.toggleOnResizing(enableOnResizing);
    }
    return this;
};
Graph.prototype.isSharpSnapline = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        return snapline.isSharp();
    }
    return false;
};
Graph.prototype.enableSharpSnapline = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.enableSharp();
    }
    return this;
};
Graph.prototype.disableSharpSnapline = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.disableSharp();
    }
    return this;
};
Graph.prototype.toggleSharpSnapline = function (sharp) {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.toggleSharp(sharp);
    }
    return this;
};
Graph.prototype.getSnaplineTolerance = function () {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        return snapline.getTolerance();
    }
};
Graph.prototype.setSnaplineTolerance = function (tolerance) {
    const snapline = this.getPlugin('snapline');
    if (snapline) {
        snapline.setTolerance(tolerance);
    }
    return this;
};
//# sourceMappingURL=api.js.map