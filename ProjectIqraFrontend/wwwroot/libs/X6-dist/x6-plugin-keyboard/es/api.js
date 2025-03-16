import { Graph } from '@antv/x6';
Graph.prototype.isKeyboardEnabled = function () {
    const keyboard = this.getPlugin('keyboard');
    if (keyboard) {
        return keyboard.isEnabled();
    }
    return false;
};
Graph.prototype.enableKeyboard = function () {
    const keyboard = this.getPlugin('keyboard');
    if (keyboard) {
        keyboard.enable();
    }
    return this;
};
Graph.prototype.disableKeyboard = function () {
    const keyboard = this.getPlugin('keyboard');
    if (keyboard) {
        keyboard.disable();
    }
    return this;
};
Graph.prototype.toggleKeyboard = function (enabled) {
    const keyboard = this.getPlugin('keyboard');
    if (keyboard) {
        keyboard.toggleEnabled(enabled);
    }
    return this;
};
Graph.prototype.bindKey = function (keys, callback, action) {
    const keyboard = this.getPlugin('keyboard');
    if (keyboard) {
        keyboard.bindKey(keys, callback, action);
    }
    return this;
};
Graph.prototype.unbindKey = function (keys, action) {
    const keyboard = this.getPlugin('keyboard');
    if (keyboard) {
        keyboard.unbindKey(keys, action);
    }
    return this;
};
Graph.prototype.clearKeys = function () {
    const keyboard = this.getPlugin('keyboard');
    if (keyboard) {
        keyboard.clear();
    }
    return this;
};
Graph.prototype.triggerKey = function (key, action) {
    const keyboard = this.getPlugin('keyboard');
    if (keyboard) {
        keyboard.trigger(key, action);
    }
    return this;
};
//# sourceMappingURL=api.js.map