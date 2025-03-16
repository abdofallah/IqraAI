"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const x6_1 = require("@antv/x6");
x6_1.Graph.prototype.isHistoryEnabled = function () {
    const history = this.getPlugin('history');
    if (history) {
        return history.isEnabled();
    }
    return false;
};
x6_1.Graph.prototype.enableHistory = function () {
    const history = this.getPlugin('history');
    if (history) {
        history.enable();
    }
    return this;
};
x6_1.Graph.prototype.disableHistory = function () {
    const history = this.getPlugin('history');
    if (history) {
        history.disable();
    }
    return this;
};
x6_1.Graph.prototype.toggleHistory = function (enabled) {
    const history = this.getPlugin('history');
    if (history) {
        history.toggleEnabled(enabled);
    }
    return this;
};
x6_1.Graph.prototype.undo = function (options) {
    const history = this.getPlugin('history');
    if (history) {
        history.undo(options);
    }
    return this;
};
x6_1.Graph.prototype.redo = function (options) {
    const history = this.getPlugin('history');
    if (history) {
        history.redo(options);
    }
    return this;
};
x6_1.Graph.prototype.undoAndCancel = function (options) {
    const history = this.getPlugin('history');
    if (history) {
        history.cancel(options);
    }
    return this;
};
x6_1.Graph.prototype.canUndo = function () {
    const history = this.getPlugin('history');
    if (history) {
        return history.canUndo();
    }
    return false;
};
x6_1.Graph.prototype.canRedo = function () {
    const history = this.getPlugin('history');
    if (history) {
        return history.canRedo();
    }
    return false;
};
x6_1.Graph.prototype.cleanHistory = function (options) {
    const history = this.getPlugin('history');
    if (history) {
        history.clean(options);
    }
    return this;
};
x6_1.Graph.prototype.getHistoryStackSize = function () {
    const history = this.getPlugin('history');
    return history.getSize();
};
x6_1.Graph.prototype.getUndoStackSize = function () {
    const history = this.getPlugin('history');
    return history.getUndoSize();
};
x6_1.Graph.prototype.getRedoStackSize = function () {
    const history = this.getPlugin('history');
    return history.getRedoSize();
};
x6_1.Graph.prototype.getUndoRemainSize = function () {
    const history = this.getPlugin('history');
    return history.getUndoRemainSize();
};
//# sourceMappingURL=api.js.map