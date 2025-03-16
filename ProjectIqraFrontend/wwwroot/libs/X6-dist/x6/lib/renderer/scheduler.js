"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Scheduler = void 0;
const x6_common_1 = require("@antv/x6-common");
const view_1 = require("../view");
const queueJob_1 = require("./queueJob");
class Scheduler extends x6_common_1.Disposable {
    get model() {
        return this.graph.model;
    }
    get container() {
        return this.graph.view.stage;
    }
    constructor(graph) {
        super();
        this.views = {};
        this.willRemoveViews = {};
        this.queue = new queueJob_1.JobQueue();
        this.graph = graph;
        this.init();
    }
    init() {
        this.startListening();
        this.renderViews(this.model.getCells());
    }
    startListening() {
        this.model.on('reseted', this.onModelReseted, this);
        this.model.on('cell:added', this.onCellAdded, this);
        this.model.on('cell:removed', this.onCellRemoved, this);
        this.model.on('cell:change:zIndex', this.onCellZIndexChanged, this);
        this.model.on('cell:change:visible', this.onCellVisibleChanged, this);
    }
    stopListening() {
        this.model.off('reseted', this.onModelReseted, this);
        this.model.off('cell:added', this.onCellAdded, this);
        this.model.off('cell:removed', this.onCellRemoved, this);
        this.model.off('cell:change:zIndex', this.onCellZIndexChanged, this);
        this.model.off('cell:change:visible', this.onCellVisibleChanged, this);
    }
    onModelReseted({ options }) {
        this.queue.clearJobs();
        this.removeZPivots();
        this.resetViews();
        const cells = this.model.getCells();
        this.renderViews(cells, Object.assign(Object.assign({}, options), { queue: cells.map((cell) => cell.id) }));
    }
    onCellAdded({ cell, options }) {
        this.renderViews([cell], options);
    }
    onCellRemoved({ cell }) {
        this.removeViews([cell]);
    }
    onCellZIndexChanged({ cell, options, }) {
        const viewItem = this.views[cell.id];
        if (viewItem) {
            this.requestViewUpdate(viewItem.view, Scheduler.FLAG_INSERT, options, queueJob_1.JOB_PRIORITY.Update, true);
        }
    }
    onCellVisibleChanged({ cell, current, }) {
        this.toggleVisible(cell, !!current);
    }
    requestViewUpdate(view, flag, options = {}, priority = queueJob_1.JOB_PRIORITY.Update, flush = true) {
        const id = view.cell.id;
        const viewItem = this.views[id];
        if (!viewItem) {
            return;
        }
        viewItem.flag = flag;
        viewItem.options = options;
        const priorAction = view.hasAction(flag, ['translate', 'resize', 'rotate']);
        if (priorAction || options.async === false) {
            priority = queueJob_1.JOB_PRIORITY.PRIOR; // eslint-disable-line
            flush = false; // eslint-disable-line
        }
        this.queue.queueJob({
            id,
            priority,
            cb: () => {
                this.renderViewInArea(view, flag, options);
                const queue = options.queue;
                if (queue) {
                    const index = queue.indexOf(view.cell.id);
                    if (index >= 0) {
                        queue.splice(index, 1);
                    }
                    if (queue.length === 0) {
                        this.graph.trigger('render:done');
                    }
                }
            },
        });
        const effectedEdges = this.getEffectedEdges(view);
        effectedEdges.forEach((edge) => {
            this.requestViewUpdate(edge.view, edge.flag, options, priority, false);
        });
        if (flush) {
            this.flush();
        }
    }
    setRenderArea(area) {
        this.renderArea = area;
        this.flushWaitingViews();
    }
    isViewMounted(view) {
        if (view == null) {
            return false;
        }
        const viewItem = this.views[view.cell.id];
        if (!viewItem) {
            return false;
        }
        return viewItem.state === Scheduler.ViewState.MOUNTED;
    }
    renderViews(cells, options = {}) {
        cells.sort((c1, c2) => {
            if (c1.isNode() && c2.isEdge()) {
                return -1;
            }
            return 0;
        });
        cells.forEach((cell) => {
            const id = cell.id;
            const views = this.views;
            let flag = 0;
            let viewItem = views[id];
            if (viewItem) {
                flag = Scheduler.FLAG_INSERT;
            }
            else {
                const cellView = this.createCellView(cell);
                if (cellView) {
                    cellView.graph = this.graph;
                    flag = Scheduler.FLAG_INSERT | cellView.getBootstrapFlag();
                    viewItem = {
                        view: cellView,
                        flag,
                        options,
                        state: Scheduler.ViewState.CREATED,
                    };
                    this.views[id] = viewItem;
                }
            }
            if (viewItem) {
                this.requestViewUpdate(viewItem.view, flag, options, this.getRenderPriority(viewItem.view), false);
            }
        });
        this.flush();
    }
    renderViewInArea(view, flag, options = {}) {
        const cell = view.cell;
        const id = cell.id;
        const viewItem = this.views[id];
        if (!viewItem) {
            return;
        }
        let result = 0;
        if (this.isUpdatable(view)) {
            result = this.updateView(view, flag, options);
            viewItem.flag = result;
        }
        else {
            if (viewItem.state === Scheduler.ViewState.MOUNTED) {
                result = this.updateView(view, flag, options);
                viewItem.flag = result;
            }
            else {
                viewItem.state = Scheduler.ViewState.WAITING;
            }
        }
        if (result) {
            if (cell.isEdge() &&
                (result & view.getFlag(['source', 'target'])) === 0) {
                this.queue.queueJob({
                    id,
                    priority: queueJob_1.JOB_PRIORITY.RenderEdge,
                    cb: () => {
                        this.updateView(view, flag, options);
                    },
                });
            }
        }
    }
    removeViews(cells) {
        cells.forEach((cell) => {
            const id = cell.id;
            const viewItem = this.views[id];
            if (viewItem) {
                this.willRemoveViews[id] = viewItem;
                delete this.views[id];
                this.queue.queueJob({
                    id,
                    priority: this.getRenderPriority(viewItem.view),
                    cb: () => {
                        this.removeView(viewItem.view);
                    },
                });
            }
        });
        this.flush();
    }
    flush() {
        this.graph.options.async
            ? this.queue.queueFlush()
            : this.queue.queueFlushSync();
    }
    flushWaitingViews() {
        Object.values(this.views).forEach((viewItem) => {
            if (viewItem && viewItem.state === Scheduler.ViewState.WAITING) {
                const { view, flag, options } = viewItem;
                this.requestViewUpdate(view, flag, options, this.getRenderPriority(view), false);
            }
        });
        this.flush();
    }
    updateView(view, flag, options = {}) {
        if (view == null) {
            return 0;
        }
        if (view_1.CellView.isCellView(view)) {
            if (flag & Scheduler.FLAG_REMOVE) {
                this.removeView(view.cell);
                return 0;
            }
            if (flag & Scheduler.FLAG_INSERT) {
                this.insertView(view);
                flag ^= Scheduler.FLAG_INSERT; // eslint-disable-line
            }
        }
        if (!flag) {
            return 0;
        }
        return view.confirmUpdate(flag, options);
    }
    insertView(view) {
        const viewItem = this.views[view.cell.id];
        if (viewItem) {
            const zIndex = view.cell.getZIndex();
            const pivot = this.addZPivot(zIndex);
            this.container.insertBefore(view.container, pivot);
            if (!view.cell.isVisible()) {
                this.toggleVisible(view.cell, false);
            }
            viewItem.state = Scheduler.ViewState.MOUNTED;
            this.graph.trigger('view:mounted', { view });
        }
    }
    resetViews() {
        this.willRemoveViews = Object.assign(Object.assign({}, this.views), this.willRemoveViews);
        Object.values(this.willRemoveViews).forEach((viewItem) => {
            if (viewItem) {
                this.removeView(viewItem.view);
            }
        });
        this.views = {};
        this.willRemoveViews = {};
    }
    removeView(view) {
        const cell = view.cell;
        const viewItem = this.willRemoveViews[cell.id];
        if (viewItem && view) {
            viewItem.view.remove();
            delete this.willRemoveViews[cell.id];
            this.graph.trigger('view:unmounted', { view });
        }
    }
    toggleVisible(cell, visible) {
        const edges = this.model.getConnectedEdges(cell);
        for (let i = 0, len = edges.length; i < len; i += 1) {
            const edge = edges[i];
            if (visible) {
                const source = edge.getSourceCell();
                const target = edge.getTargetCell();
                if ((source && !source.isVisible()) ||
                    (target && !target.isVisible())) {
                    continue;
                }
                this.toggleVisible(edge, true);
            }
            else {
                this.toggleVisible(edge, false);
            }
        }
        const viewItem = this.views[cell.id];
        if (viewItem) {
            x6_common_1.Dom.css(viewItem.view.container, {
                display: visible ? 'unset' : 'none',
            });
        }
    }
    addZPivot(zIndex = 0) {
        if (this.zPivots == null) {
            this.zPivots = {};
        }
        const pivots = this.zPivots;
        let pivot = pivots[zIndex];
        if (pivot) {
            return pivot;
        }
        pivot = pivots[zIndex] = document.createComment(`z-index:${zIndex + 1}`);
        let neighborZ = -Infinity;
        // eslint-disable-next-line
        for (const key in pivots) {
            const currentZ = +key;
            if (currentZ < zIndex && currentZ > neighborZ) {
                neighborZ = currentZ;
                if (neighborZ === zIndex - 1) {
                    continue;
                }
            }
        }
        const layer = this.container;
        if (neighborZ !== -Infinity) {
            const neighborPivot = pivots[neighborZ];
            layer.insertBefore(pivot, neighborPivot.nextSibling);
        }
        else {
            layer.insertBefore(pivot, layer.firstChild);
        }
        return pivot;
    }
    removeZPivots() {
        if (this.zPivots) {
            Object.values(this.zPivots).forEach((elem) => {
                if (elem && elem.parentNode) {
                    elem.parentNode.removeChild(elem);
                }
            });
        }
        this.zPivots = {};
    }
    createCellView(cell) {
        const options = { graph: this.graph };
        const createViewHook = this.graph.options.createCellView;
        if (createViewHook) {
            const ret = x6_common_1.FunctionExt.call(createViewHook, this.graph, cell);
            if (ret) {
                return new ret(cell, options); // eslint-disable-line new-cap
            }
            if (ret === null) {
                // null means not render
                return null;
            }
        }
        const view = cell.view;
        if (view != null && typeof view === 'string') {
            const def = view_1.CellView.registry.get(view);
            if (def) {
                return new def(cell, options); // eslint-disable-line new-cap
            }
            return view_1.CellView.registry.onNotFound(view);
        }
        if (cell.isNode()) {
            return new view_1.NodeView(cell, options);
        }
        if (cell.isEdge()) {
            return new view_1.EdgeView(cell, options);
        }
        return null;
    }
    getEffectedEdges(view) {
        const effectedEdges = [];
        const cell = view.cell;
        const edges = this.model.getConnectedEdges(cell);
        for (let i = 0, n = edges.length; i < n; i += 1) {
            const edge = edges[i];
            const viewItem = this.views[edge.id];
            if (!viewItem) {
                continue;
            }
            const edgeView = viewItem.view;
            if (!this.isViewMounted(edgeView)) {
                continue;
            }
            const flagLabels = ['update'];
            if (edge.getTargetCell() === cell) {
                flagLabels.push('target');
            }
            if (edge.getSourceCell() === cell) {
                flagLabels.push('source');
            }
            effectedEdges.push({
                id: edge.id,
                view: edgeView,
                flag: edgeView.getFlag(flagLabels),
            });
        }
        return effectedEdges;
    }
    isUpdatable(view) {
        if (view.isNodeView()) {
            if (this.renderArea) {
                return this.renderArea.isIntersectWithRect(view.cell.getBBox());
            }
            return true;
        }
        if (view.isEdgeView()) {
            const edge = view.cell;
            const sourceCell = edge.getSourceCell();
            const targetCell = edge.getTargetCell();
            if (this.renderArea && sourceCell && targetCell) {
                return (this.renderArea.isIntersectWithRect(sourceCell.getBBox()) ||
                    this.renderArea.isIntersectWithRect(targetCell.getBBox()));
            }
        }
        return true;
    }
    getRenderPriority(view) {
        return view.cell.isNode()
            ? queueJob_1.JOB_PRIORITY.RenderNode
            : queueJob_1.JOB_PRIORITY.RenderEdge;
    }
    dispose() {
        this.stopListening();
        // clear views
        Object.keys(this.views).forEach((id) => {
            this.views[id].view.dispose();
        });
        this.views = {};
    }
}
__decorate([
    x6_common_1.Disposable.dispose()
], Scheduler.prototype, "dispose", null);
exports.Scheduler = Scheduler;
(function (Scheduler) {
    Scheduler.FLAG_INSERT = 1 << 30;
    Scheduler.FLAG_REMOVE = 1 << 29;
    Scheduler.FLAG_RENDER = (1 << 26) - 1;
})(Scheduler = exports.Scheduler || (exports.Scheduler = {}));
(function (Scheduler) {
    let ViewState;
    (function (ViewState) {
        ViewState[ViewState["CREATED"] = 0] = "CREATED";
        ViewState[ViewState["MOUNTED"] = 1] = "MOUNTED";
        ViewState[ViewState["WAITING"] = 2] = "WAITING";
    })(ViewState = Scheduler.ViewState || (Scheduler.ViewState = {}));
})(Scheduler = exports.Scheduler || (exports.Scheduler = {}));
//# sourceMappingURL=scheduler.js.map