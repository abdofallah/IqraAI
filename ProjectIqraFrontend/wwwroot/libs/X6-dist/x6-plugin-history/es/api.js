import { Graph } from '@antv/x6';
Graph.prototype.isHistoryEnabled = function () {
    const history = this.getPlugin('history');
    if (history) {
        return history.isEnabled();
    }
    return false;
};
Graph.prototype.enableHistory = function () {
    const history = this.getPlugin('history');
    if (history) {
        history.enable();
    }
    return this;
};
Graph.prototype.disableHistory = function () {
    const history = this.getPlugin('history');
    if (history) {
        history.disable();
    }
    return this;
};
Graph.prototype.toggleHistory = function (enabled) {
    const history = this.getPlugin('history');
    if (history) {
        history.toggleEnabled(enabled);
    }
    return this;
};
Graph.prototype.undo = function (options) {
    const history = this.getPlugin('history');
    if (history) {
        history.undo(options);
    }
    return this;
};
Graph.prototype.redo = function (options) {
    const history = this.getPlugin('history');
    if (history) {
        history.redo(options);
    }
    return this;
};
Graph.prototype.undoAndCancel = function (options) {
    const history = this.getPlugin('history');
    if (history) {
        history.cancel(options);
    }
    return this;
};
Graph.prototype.canUndo = function () {
    const history = this.getPlugin('history');
    if (history) {
        return history.canUndo();
    }
    return false;
};
Graph.prototype.canRedo = function () {
    const history = this.getPlugin('history');
    if (history) {
        return history.canRedo();
    }
    return false;
};
Graph.prototype.cleanHistory = function (options) {
    const history = this.getPlugin('history');
    if (history) {
        history.clean(options);
    }
    return this;
};
Graph.prototype.getHistoryStackSize = function () {
    const history = this.getPlugin('history');
    return history.getSize();
};
Graph.prototype.getUndoStackSize = function () {
    const history = this.getPlugin('history');
    return history.getUndoSize();
};
Graph.prototype.getRedoStackSize = function () {
    const history = this.getPlugin('history');
    return history.getRedoSize();
};
Graph.prototype.getUndoRemainSize = function () {
    const history = this.getPlugin('history');
    return history.getUndoRemainSize();
};
//# sourceMappingURL=api.js.map