import { Graph } from '@antv/x6';
Graph.prototype.isSelectionEnabled = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        return selection.isEnabled();
    }
    return false;
};
Graph.prototype.enableSelection = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.enable();
    }
    return this;
};
Graph.prototype.disableSelection = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.disable();
    }
    return this;
};
Graph.prototype.toggleSelection = function (enabled) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.toggleEnabled(enabled);
    }
    return this;
};
Graph.prototype.isMultipleSelection = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        return selection.isMultipleSelection();
    }
    return false;
};
Graph.prototype.enableMultipleSelection = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.enableMultipleSelection();
    }
    return this;
};
Graph.prototype.disableMultipleSelection = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.disableMultipleSelection();
    }
    return this;
};
Graph.prototype.toggleMultipleSelection = function (multiple) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.toggleMultipleSelection(multiple);
    }
    return this;
};
Graph.prototype.isSelectionMovable = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        return selection.isSelectionMovable();
    }
    return false;
};
Graph.prototype.enableSelectionMovable = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.enableSelectionMovable();
    }
    return this;
};
Graph.prototype.disableSelectionMovable = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.disableSelectionMovable();
    }
    return this;
};
Graph.prototype.toggleSelectionMovable = function (movable) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.toggleSelectionMovable(movable);
    }
    return this;
};
Graph.prototype.isRubberbandEnabled = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        return selection.isRubberbandEnabled();
    }
    return false;
};
Graph.prototype.enableRubberband = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.enableRubberband();
    }
    return this;
};
Graph.prototype.disableRubberband = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.disableRubberband();
    }
    return this;
};
Graph.prototype.toggleRubberband = function (enabled) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.toggleRubberband(enabled);
    }
    return this;
};
Graph.prototype.isStrictRubberband = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        return selection.isStrictRubberband();
    }
    return false;
};
Graph.prototype.enableStrictRubberband = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.enableStrictRubberband();
    }
    return this;
};
Graph.prototype.disableStrictRubberband = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.disableStrictRubberband();
    }
    return this;
};
Graph.prototype.toggleStrictRubberband = function (strict) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.toggleStrictRubberband(strict);
    }
    return this;
};
Graph.prototype.setRubberbandModifiers = function (modifiers) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.setRubberbandModifiers(modifiers);
    }
    return this;
};
Graph.prototype.setSelectionFilter = function (filter) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.setSelectionFilter(filter);
    }
    return this;
};
Graph.prototype.setSelectionDisplayContent = function (content) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.setSelectionDisplayContent(content);
    }
    return this;
};
Graph.prototype.isSelectionEmpty = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        return selection.isEmpty();
    }
    return true;
};
Graph.prototype.cleanSelection = function (options) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.clean(options);
    }
    return this;
};
Graph.prototype.resetSelection = function (cells, options) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.reset(cells, options);
    }
    return this;
};
Graph.prototype.getSelectedCells = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        return selection.getSelectedCells();
    }
    return [];
};
Graph.prototype.getSelectedCellCount = function () {
    const selection = this.getPlugin('selection');
    if (selection) {
        return selection.getSelectedCellCount();
    }
    return 0;
};
Graph.prototype.isSelected = function (cell) {
    const selection = this.getPlugin('selection');
    if (selection) {
        return selection.isSelected(cell);
    }
    return false;
};
Graph.prototype.select = function (cells, options) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.select(cells, options);
    }
    return this;
};
Graph.prototype.unselect = function (cells, options) {
    const selection = this.getPlugin('selection');
    if (selection) {
        selection.unselect(cells, options);
    }
    return this;
};
//# sourceMappingURL=api.js.map