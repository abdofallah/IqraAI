import { Graph } from '@antv/x6';
Graph.prototype.isClipboardEnabled = function () {
    const clipboard = this.getPlugin('clipboard');
    if (clipboard) {
        return clipboard.isEnabled();
    }
    return false;
};
Graph.prototype.enableClipboard = function () {
    const clipboard = this.getPlugin('clipboard');
    if (clipboard) {
        clipboard.enable();
    }
    return this;
};
Graph.prototype.disableClipboard = function () {
    const clipboard = this.getPlugin('clipboard');
    if (clipboard) {
        clipboard.disable();
    }
    return this;
};
Graph.prototype.toggleClipboard = function (enabled) {
    const clipboard = this.getPlugin('clipboard');
    if (clipboard) {
        clipboard.toggleEnabled(enabled);
    }
    return this;
};
Graph.prototype.isClipboardEmpty = function (options) {
    const clipboard = this.getPlugin('clipboard');
    if (clipboard) {
        return clipboard.isEmpty(options);
    }
    return true;
};
Graph.prototype.getCellsInClipboard = function () {
    const clipboard = this.getPlugin('clipboard');
    if (clipboard) {
        return clipboard.getCellsInClipboard();
    }
    return [];
};
Graph.prototype.cleanClipboard = function () {
    const clipboard = this.getPlugin('clipboard');
    if (clipboard) {
        clipboard.clean();
    }
    return this;
};
Graph.prototype.copy = function (cells, options) {
    const clipboard = this.getPlugin('clipboard');
    if (clipboard) {
        clipboard.copy(cells, options);
    }
    return this;
};
Graph.prototype.cut = function (cells, options) {
    const clipboard = this.getPlugin('clipboard');
    if (clipboard) {
        clipboard.cut(cells, options);
    }
    return this;
};
Graph.prototype.paste = function (options, graph) {
    const clipboard = this.getPlugin('clipboard');
    if (clipboard) {
        return clipboard.paste(options, graph);
    }
    return [];
};
//# sourceMappingURL=api.js.map