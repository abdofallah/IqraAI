import { Graph } from '@antv/x6';
Graph.prototype.lockScroller = function () {
    const scroller = this.getPlugin('scroller');
    if (scroller) {
        scroller.lockScroller();
    }
    return this;
};
Graph.prototype.unlockScroller = function () {
    const scroller = this.getPlugin('scroller');
    if (scroller) {
        scroller.unlockScroller();
    }
    return this;
};
Graph.prototype.updateScroller = function () {
    const scroller = this.getPlugin('scroller');
    if (scroller) {
        scroller.updateScroller();
    }
    return this;
};
Graph.prototype.getScrollbarPosition = function () {
    const scroller = this.getPlugin('scroller');
    if (scroller) {
        return scroller.getScrollbarPosition();
    }
    return {
        left: 0,
        top: 0,
    };
};
Graph.prototype.setScrollbarPosition = function (left, top) {
    const scroller = this.getPlugin('scroller');
    if (scroller) {
        scroller.setScrollbarPosition(left, top);
    }
    return this;
};
//# sourceMappingURL=api.js.map