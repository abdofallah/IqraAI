var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { Rectangle, FunctionExt, Dom, Cell, Collection, View, } from '@antv/x6';
export class SelectionImpl extends View {
    get graph() {
        return this.options.graph;
    }
    get boxClassName() {
        return this.prefixClassName(Private.classNames.box);
    }
    get $boxes() {
        return Dom.children(this.container, this.boxClassName);
    }
    get handleOptions() {
        return this.options;
    }
    constructor(options) {
        super();
        this.options = options;
        if (this.options.model) {
            this.options.collection = this.options.model.collection;
        }
        if (this.options.collection) {
            this.collection = this.options.collection;
        }
        else {
            this.collection = new Collection([], {
                comparator: Private.depthComparator,
            });
            this.options.collection = this.collection;
        }
        this.boxCount = 0;
        this.createContainer();
        this.startListening();
    }
    startListening() {
        const graph = this.graph;
        const collection = this.collection;
        this.delegateEvents({
            [`mousedown .${this.boxClassName}`]: 'onSelectionBoxMouseDown',
            [`touchstart .${this.boxClassName}`]: 'onSelectionBoxMouseDown',
        }, true);
        graph.on('scale', this.onGraphTransformed, this);
        graph.on('translate', this.onGraphTransformed, this);
        graph.model.on('updated', this.onModelUpdated, this);
        collection.on('added', this.onCellAdded, this);
        collection.on('removed', this.onCellRemoved, this);
        collection.on('reseted', this.onReseted, this);
        collection.on('updated', this.onCollectionUpdated, this);
        collection.on('node:change:position', this.onNodePositionChanged, this);
        collection.on('cell:changed', this.onCellChanged, this);
    }
    stopListening() {
        const graph = this.graph;
        const collection = this.collection;
        this.undelegateEvents();
        graph.off('scale', this.onGraphTransformed, this);
        graph.off('translate', this.onGraphTransformed, this);
        graph.model.off('updated', this.onModelUpdated, this);
        collection.off('added', this.onCellAdded, this);
        collection.off('removed', this.onCellRemoved, this);
        collection.off('reseted', this.onReseted, this);
        collection.off('updated', this.onCollectionUpdated, this);
        collection.off('node:change:position', this.onNodePositionChanged, this);
        collection.off('cell:changed', this.onCellChanged, this);
    }
    onRemove() {
        this.stopListening();
    }
    onGraphTransformed() {
        this.updateSelectionBoxes();
    }
    onCellChanged() {
        this.updateSelectionBoxes();
    }
    onNodePositionChanged({ node, options, }) {
        const { showNodeSelectionBox, pointerEvents } = this.options;
        const { ui, selection, translateBy, snapped } = options;
        const allowTranslating = (showNodeSelectionBox !== true || (pointerEvents && this.getPointerEventsValue(pointerEvents) === 'none')) &&
            !this.translating &&
            !selection;
        const translateByUi = ui && translateBy && node.id === translateBy;
        if (allowTranslating && (translateByUi || snapped)) {
            this.translating = true;
            const current = node.position();
            const previous = node.previous('position');
            const dx = current.x - previous.x;
            const dy = current.y - previous.y;
            if (dx !== 0 || dy !== 0) {
                this.translateSelectedNodes(dx, dy, node, options);
            }
            this.translating = false;
        }
    }
    onModelUpdated({ removed }) {
        if (removed && removed.length) {
            this.unselect(removed);
        }
    }
    isEmpty() {
        return this.length <= 0;
    }
    isSelected(cell) {
        return this.collection.has(cell);
    }
    get length() {
        return this.collection.length;
    }
    get cells() {
        return this.collection.toArray();
    }
    select(cells, options = {}) {
        options.dryrun = true;
        const items = this.filter(Array.isArray(cells) ? cells : [cells]);
        this.collection.add(items, options);
        return this;
    }
    unselect(cells, options = {}) {
        // dryrun to prevent cell be removed from graph
        options.dryrun = true;
        this.collection.remove(Array.isArray(cells) ? cells : [cells], options);
        return this;
    }
    reset(cells, options = {}) {
        if (cells) {
            if (options.batch) {
                const filterCells = this.filter(Array.isArray(cells) ? cells : [cells]);
                this.collection.reset(filterCells, Object.assign(Object.assign({}, options), { ui: true }));
                return this;
            }
            const prev = this.cells;
            const next = this.filter(Array.isArray(cells) ? cells : [cells]);
            const prevMap = {};
            const nextMap = {};
            prev.forEach((cell) => (prevMap[cell.id] = cell));
            next.forEach((cell) => (nextMap[cell.id] = cell));
            const added = [];
            const removed = [];
            next.forEach((cell) => {
                if (!prevMap[cell.id]) {
                    added.push(cell);
                }
            });
            prev.forEach((cell) => {
                if (!nextMap[cell.id]) {
                    removed.push(cell);
                }
            });
            if (removed.length) {
                this.unselect(removed, Object.assign(Object.assign({}, options), { ui: true }));
            }
            if (added.length) {
                this.select(added, Object.assign(Object.assign({}, options), { ui: true }));
            }
            if (removed.length === 0 && added.length === 0) {
                this.updateContainer();
            }
            return this;
        }
        return this.clean(options);
    }
    clean(options = {}) {
        if (this.length) {
            if (options.batch === false) {
                this.unselect(this.cells, options);
            }
            else {
                this.collection.reset([], Object.assign(Object.assign({}, options), { ui: true }));
            }
        }
        return this;
    }
    setFilter(filter) {
        this.options.filter = filter;
    }
    setContent(content) {
        this.options.content = content;
    }
    startSelecting(evt) {
        // Flow: startSelecting => adjustSelection => stopSelecting
        evt = this.normalizeEvent(evt); // eslint-disable-line
        this.clean();
        let x;
        let y;
        const graphContainer = this.graph.container;
        if (evt.offsetX != null &&
            evt.offsetY != null &&
            graphContainer.contains(evt.target)) {
            x = evt.offsetX;
            y = evt.offsetY;
        }
        else {
            const offset = Dom.offset(graphContainer);
            const scrollLeft = graphContainer.scrollLeft;
            const scrollTop = graphContainer.scrollTop;
            x = evt.clientX - offset.left + window.pageXOffset + scrollLeft;
            y = evt.clientY - offset.top + window.pageYOffset + scrollTop;
        }
        Dom.css(this.container, {
            top: y,
            left: x,
            width: 1,
            height: 1,
        });
        this.setEventData(evt, {
            action: 'selecting',
            clientX: evt.clientX,
            clientY: evt.clientY,
            offsetX: x,
            offsetY: y,
            scrollerX: 0,
            scrollerY: 0,
            moving: false,
        });
        this.delegateDocumentEvents(Private.documentEvents, evt.data);
    }
    filter(cells) {
        const filter = this.options.filter;
        return cells.filter((cell) => {
            if (Array.isArray(filter)) {
                return filter.some((item) => {
                    if (typeof item === 'string') {
                        return cell.shape === item;
                    }
                    return cell.id === item.id;
                });
            }
            if (typeof filter === 'function') {
                return FunctionExt.call(filter, this.graph, cell);
            }
            return true;
        });
    }
    stopSelecting(evt) {
        const graph = this.graph;
        const eventData = this.getEventData(evt);
        const action = eventData.action;
        switch (action) {
            case 'selecting': {
                let width = Dom.width(this.container);
                let height = Dom.height(this.container);
                const offset = Dom.offset(this.container);
                const origin = graph.pageToLocal(offset.left, offset.top);
                const scale = graph.transform.getScale();
                width /= scale.sx;
                height /= scale.sy;
                const rect = new Rectangle(origin.x, origin.y, width, height);
                const cells = this.getCellViewsInArea(rect).map((view) => view.cell);
                this.reset(cells, { batch: true });
                this.hideRubberband();
                break;
            }
            case 'translating': {
                const client = graph.snapToGrid(evt.clientX, evt.clientY);
                if (!this.options.following) {
                    const data = eventData;
                    this.updateSelectedNodesPosition({
                        dx: data.clientX - data.originX,
                        dy: data.clientY - data.originY,
                    });
                }
                this.graph.model.stopBatch('move-selection');
                this.notifyBoxEvent('box:mouseup', evt, client.x, client.y);
                break;
            }
            default: {
                this.clean();
                break;
            }
        }
    }
    onMouseUp(evt) {
        const action = this.getEventData(evt).action;
        if (action) {
            this.stopSelecting(evt);
            this.undelegateDocumentEvents();
        }
    }
    onSelectionBoxMouseDown(evt) {
        if (!this.options.following) {
            evt.stopPropagation();
        }
        const e = this.normalizeEvent(evt);
        if (this.options.movable) {
            this.startTranslating(e);
        }
        const activeView = this.getCellViewFromElem(e.target);
        this.setEventData(e, { activeView });
        const client = this.graph.snapToGrid(e.clientX, e.clientY);
        this.notifyBoxEvent('box:mousedown', e, client.x, client.y);
        this.delegateDocumentEvents(Private.documentEvents, e.data);
    }
    startTranslating(evt) {
        this.graph.model.startBatch('move-selection');
        const client = this.graph.snapToGrid(evt.clientX, evt.clientY);
        this.setEventData(evt, {
            action: 'translating',
            clientX: client.x,
            clientY: client.y,
            originX: client.x,
            originY: client.y,
        });
    }
    getRestrictArea() {
        const restrict = this.graph.options.translating.restrict;
        const area = typeof restrict === 'function'
            ? FunctionExt.call(restrict, this.graph, null)
            : restrict;
        if (typeof area === 'number') {
            return this.graph.transform.getGraphArea().inflate(area);
        }
        if (area === true) {
            return this.graph.transform.getGraphArea();
        }
        return area || null;
    }
    getSelectionOffset(client, data) {
        let dx = client.x - data.clientX;
        let dy = client.y - data.clientY;
        const restrict = this.getRestrictArea();
        if (restrict) {
            const cells = this.collection.toArray();
            const totalBBox = Cell.getCellsBBox(cells, { deep: true }) || Rectangle.create();
            const minDx = restrict.x - totalBBox.x;
            const minDy = restrict.y - totalBBox.y;
            const maxDx = restrict.x + restrict.width - (totalBBox.x + totalBBox.width);
            const maxDy = restrict.y + restrict.height - (totalBBox.y + totalBBox.height);
            if (dx < minDx) {
                dx = minDx;
            }
            if (dy < minDy) {
                dy = minDy;
            }
            if (maxDx < dx) {
                dx = maxDx;
            }
            if (maxDy < dy) {
                dy = maxDy;
            }
            if (!this.options.following) {
                const offsetX = client.x - data.originX;
                const offsetY = client.y - data.originY;
                dx = offsetX <= minDx || offsetX >= maxDx ? 0 : dx;
                dy = offsetY <= minDy || offsetY >= maxDy ? 0 : dy;
            }
        }
        return {
            dx,
            dy,
        };
    }
    updateElementPosition(elem, dLeft, dTop) {
        const strLeft = Dom.css(elem, 'left');
        const strTop = Dom.css(elem, 'top');
        const left = strLeft ? parseFloat(strLeft) : 0;
        const top = strTop ? parseFloat(strTop) : 0;
        Dom.css(elem, 'left', left + dLeft);
        Dom.css(elem, 'top', top + dTop);
    }
    updateSelectedNodesPosition(offset) {
        const { dx, dy } = offset;
        if (dx || dy) {
            if ((this.translateSelectedNodes(dx, dy), this.boxesUpdated)) {
                if (this.collection.length > 1) {
                    this.updateSelectionBoxes();
                }
            }
            else {
                const scale = this.graph.transform.getScale();
                for (let i = 0, $boxes = this.$boxes, len = $boxes.length; i < len; i += 1) {
                    this.updateElementPosition($boxes[i], dx * scale.sx, dy * scale.sy);
                }
                this.updateElementPosition(this.selectionContainer, dx * scale.sx, dy * scale.sy);
            }
        }
    }
    autoScrollGraph(x, y) {
        const scroller = this.graph.getPlugin('scroller');
        if (scroller) {
            return scroller.autoScroll(x, y);
        }
        return { scrollerX: 0, scrollerY: 0 };
    }
    adjustSelection(evt) {
        const e = this.normalizeEvent(evt);
        const eventData = this.getEventData(e);
        const action = eventData.action;
        switch (action) {
            case 'selecting': {
                const data = eventData;
                if (data.moving !== true) {
                    Dom.appendTo(this.container, this.graph.container);
                    this.showRubberband();
                    data.moving = true;
                }
                const { scrollerX, scrollerY } = this.autoScrollGraph(e.clientX, e.clientY);
                data.scrollerX += scrollerX;
                data.scrollerY += scrollerY;
                const dx = e.clientX - data.clientX + data.scrollerX;
                const dy = e.clientY - data.clientY + data.scrollerY;
                const left = parseInt(Dom.css(this.container, 'left') || '0', 10);
                const top = parseInt(Dom.css(this.container, 'top') || '0', 10);
                Dom.css(this.container, {
                    left: dx < 0 ? data.offsetX + dx : left,
                    top: dy < 0 ? data.offsetY + dy : top,
                    width: Math.abs(dx),
                    height: Math.abs(dy),
                });
                break;
            }
            case 'translating': {
                const client = this.graph.snapToGrid(e.clientX, e.clientY);
                const data = eventData;
                const offset = this.getSelectionOffset(client, data);
                if (this.options.following) {
                    this.updateSelectedNodesPosition(offset);
                }
                else {
                    this.updateContainerPosition(offset);
                }
                if (offset.dx) {
                    data.clientX = client.x;
                }
                if (offset.dy) {
                    data.clientY = client.y;
                }
                this.notifyBoxEvent('box:mousemove', evt, client.x, client.y);
                break;
            }
            default:
                break;
        }
        this.boxesUpdated = false;
    }
    translateSelectedNodes(dx, dy, exclude, otherOptions) {
        const map = {};
        const excluded = [];
        if (exclude) {
            map[exclude.id] = true;
        }
        this.collection.toArray().forEach((cell) => {
            cell.getDescendants({ deep: true }).forEach((child) => {
                map[child.id] = true;
            });
        });
        if (otherOptions && otherOptions.translateBy) {
            const currentCell = this.graph.getCellById(otherOptions.translateBy);
            if (currentCell) {
                map[currentCell.id] = true;
                currentCell.getDescendants({ deep: true }).forEach((child) => {
                    map[child.id] = true;
                });
                excluded.push(currentCell);
            }
        }
        this.collection.toArray().forEach((cell) => {
            if (!map[cell.id]) {
                const options = Object.assign(Object.assign({}, otherOptions), { selection: this.cid, exclude: excluded });
                cell.translate(dx, dy, options);
                this.graph.model.getConnectedEdges(cell).forEach((edge) => {
                    if (!map[edge.id]) {
                        edge.translate(dx, dy, options);
                        map[edge.id] = true;
                    }
                });
            }
        });
    }
    getCellViewsInArea(rect) {
        const graph = this.graph;
        const options = {
            strict: this.options.strict,
        };
        let views = [];
        if (this.options.rubberNode) {
            views = views.concat(graph.model
                .getNodesInArea(rect, options)
                .map((node) => graph.renderer.findViewByCell(node))
                .filter((view) => view != null));
        }
        if (this.options.rubberEdge) {
            views = views.concat(graph.model
                .getEdgesInArea(rect, options)
                .map((edge) => graph.renderer.findViewByCell(edge))
                .filter((view) => view != null));
        }
        return views;
    }
    notifyBoxEvent(name, e, x, y) {
        const data = this.getEventData(e);
        const view = data.activeView;
        this.trigger(name, { e, view, x, y, cell: view.cell });
    }
    getSelectedClassName(cell) {
        return this.prefixClassName(`${cell.isNode() ? 'node' : 'edge'}-selected`);
    }
    addCellSelectedClassName(cell) {
        const view = this.graph.renderer.findViewByCell(cell);
        if (view) {
            view.addClass(this.getSelectedClassName(cell));
        }
    }
    removeCellUnSelectedClassName(cell) {
        const view = this.graph.renderer.findViewByCell(cell);
        if (view) {
            view.removeClass(this.getSelectedClassName(cell));
        }
    }
    destroySelectionBox(cell) {
        this.removeCellUnSelectedClassName(cell);
        if (this.canShowSelectionBox(cell)) {
            Dom.remove(this.container.querySelector(`[data-cell-id="${cell.id}"]`));
            if (this.$boxes.length === 0) {
                this.hide();
            }
            this.boxCount = Math.max(0, this.boxCount - 1);
        }
    }
    destroyAllSelectionBoxes(cells) {
        cells.forEach((cell) => this.removeCellUnSelectedClassName(cell));
        this.hide();
        Dom.remove(this.$boxes);
        this.boxCount = 0;
    }
    hide() {
        Dom.removeClass(this.container, this.prefixClassName(Private.classNames.rubberband));
        Dom.removeClass(this.container, this.prefixClassName(Private.classNames.selected));
    }
    showRubberband() {
        Dom.addClass(this.container, this.prefixClassName(Private.classNames.rubberband));
    }
    hideRubberband() {
        Dom.removeClass(this.container, this.prefixClassName(Private.classNames.rubberband));
    }
    showSelected() {
        Dom.removeAttribute(this.container, 'style');
        Dom.addClass(this.container, this.prefixClassName(Private.classNames.selected));
    }
    createContainer() {
        this.container = document.createElement('div');
        Dom.addClass(this.container, this.prefixClassName(Private.classNames.root));
        if (this.options.className) {
            Dom.addClass(this.container, this.options.className);
        }
        this.selectionContainer = document.createElement('div');
        Dom.addClass(this.selectionContainer, this.prefixClassName(Private.classNames.inner));
        this.selectionContent = document.createElement('div');
        Dom.addClass(this.selectionContent, this.prefixClassName(Private.classNames.content));
        Dom.append(this.selectionContainer, this.selectionContent);
        Dom.attr(this.selectionContainer, 'data-selection-length', this.collection.length);
        Dom.prepend(this.container, this.selectionContainer);
    }
    updateContainerPosition(offset) {
        if (offset.dx || offset.dy) {
            this.updateElementPosition(this.selectionContainer, offset.dx, offset.dy);
        }
    }
    updateContainer() {
        const origin = { x: Infinity, y: Infinity };
        const corner = { x: 0, y: 0 };
        const cells = this.collection
            .toArray()
            .filter((cell) => this.canShowSelectionBox(cell));
        cells.forEach((cell) => {
            const view = this.graph.renderer.findViewByCell(cell);
            if (view) {
                const bbox = view.getBBox({
                    useCellGeometry: true,
                });
                origin.x = Math.min(origin.x, bbox.x);
                origin.y = Math.min(origin.y, bbox.y);
                corner.x = Math.max(corner.x, bbox.x + bbox.width);
                corner.y = Math.max(corner.y, bbox.y + bbox.height);
            }
        });
        Dom.css(this.selectionContainer, {
            position: 'absolute',
            pointerEvents: 'none',
            left: origin.x,
            top: origin.y,
            width: corner.x - origin.x,
            height: corner.y - origin.y,
        });
        Dom.attr(this.selectionContainer, 'data-selection-length', this.collection.length);
        const boxContent = this.options.content;
        if (boxContent) {
            if (typeof boxContent === 'function') {
                const content = FunctionExt.call(boxContent, this.graph, this, this.selectionContent);
                if (content) {
                    this.selectionContent.innerHTML = content;
                }
            }
            else {
                this.selectionContent.innerHTML = boxContent;
            }
        }
        if (this.collection.length > 0 && !this.container.parentNode) {
            Dom.appendTo(this.container, this.graph.container);
        }
        else if (this.collection.length <= 0 && this.container.parentNode) {
            this.container.parentNode.removeChild(this.container);
        }
    }
    canShowSelectionBox(cell) {
        return ((cell.isNode() && this.options.showNodeSelectionBox === true) ||
            (cell.isEdge() && this.options.showEdgeSelectionBox === true));
    }
    getPointerEventsValue(pointerEvents) {
        return typeof pointerEvents === 'string'
            ? pointerEvents
            : pointerEvents(this.cells);
    }
    createSelectionBox(cell) {
        this.addCellSelectedClassName(cell);
        if (this.canShowSelectionBox(cell)) {
            const view = this.graph.renderer.findViewByCell(cell);
            if (view) {
                const bbox = view.getBBox({
                    useCellGeometry: true,
                });
                const className = this.boxClassName;
                const box = document.createElement('div');
                const pointerEvents = this.options.pointerEvents;
                Dom.addClass(box, className);
                Dom.addClass(box, `${className}-${cell.isNode() ? 'node' : 'edge'}`);
                Dom.attr(box, 'data-cell-id', cell.id);
                Dom.css(box, {
                    position: 'absolute',
                    left: bbox.x,
                    top: bbox.y,
                    width: bbox.width,
                    height: bbox.height,
                    pointerEvents: pointerEvents
                        ? this.getPointerEventsValue(pointerEvents)
                        : 'auto',
                });
                Dom.appendTo(box, this.container);
                this.showSelected();
                this.boxCount += 1;
            }
        }
    }
    updateSelectionBoxes() {
        if (this.collection.length > 0) {
            this.boxesUpdated = true;
            this.confirmUpdate();
            // this.graph.renderer.requestViewUpdate(this as any, 1, options)
        }
    }
    confirmUpdate() {
        if (this.boxCount) {
            this.hide();
            for (let i = 0, $boxes = this.$boxes, len = $boxes.length; i < len; i += 1) {
                const box = $boxes[i];
                const cellId = Dom.attr(box, 'data-cell-id');
                Dom.remove(box);
                this.boxCount -= 1;
                const cell = this.collection.get(cellId);
                if (cell) {
                    this.createSelectionBox(cell);
                }
            }
            this.updateContainer();
        }
        return 0;
    }
    getCellViewFromElem(elem) {
        const id = elem.getAttribute('data-cell-id');
        if (id) {
            const cell = this.collection.get(id);
            if (cell) {
                return this.graph.renderer.findViewByCell(cell);
            }
        }
        return null;
    }
    onCellRemoved({ cell }) {
        this.destroySelectionBox(cell);
        this.updateContainer();
    }
    onReseted({ previous, current }) {
        this.destroyAllSelectionBoxes(previous);
        current.forEach((cell) => {
            this.listenCellRemoveEvent(cell);
            this.createSelectionBox(cell);
        });
        this.updateContainer();
    }
    onCellAdded({ cell }) {
        // The collection do not known the cell was removed when cell was
        // removed by interaction(such as, by "delete" shortcut), so we should
        // manually listen to cell's remove event.
        this.listenCellRemoveEvent(cell);
        this.createSelectionBox(cell);
        this.updateContainer();
    }
    listenCellRemoveEvent(cell) {
        cell.off('removed', this.onCellRemoved, this);
        cell.on('removed', this.onCellRemoved, this);
    }
    onCollectionUpdated({ added, removed, options, }) {
        added.forEach((cell) => {
            this.trigger('cell:selected', { cell, options });
            if (cell.isNode()) {
                this.trigger('node:selected', { cell, options, node: cell });
            }
            else if (cell.isEdge()) {
                this.trigger('edge:selected', { cell, options, edge: cell });
            }
        });
        removed.forEach((cell) => {
            this.trigger('cell:unselected', { cell, options });
            if (cell.isNode()) {
                this.trigger('node:unselected', { cell, options, node: cell });
            }
            else if (cell.isEdge()) {
                this.trigger('edge:unselected', { cell, options, edge: cell });
            }
        });
        const args = {
            added,
            removed,
            options,
            selected: this.cells.filter((cell) => !!this.graph.getCellById(cell.id)),
        };
        this.trigger('selection:changed', args);
    }
    // #endregion
    dispose() {
        this.clean();
        this.remove();
        this.off();
    }
}
__decorate([
    View.dispose()
], SelectionImpl.prototype, "dispose", null);
// private
// -------
var Private;
(function (Private) {
    const base = 'widget-selection';
    Private.classNames = {
        root: base,
        inner: `${base}-inner`,
        box: `${base}-box`,
        content: `${base}-content`,
        rubberband: `${base}-rubberband`,
        selected: `${base}-selected`,
    };
    Private.documentEvents = {
        mousemove: 'adjustSelection',
        touchmove: 'adjustSelection',
        mouseup: 'onMouseUp',
        touchend: 'onMouseUp',
        touchcancel: 'onMouseUp',
    };
    function depthComparator(cell) {
        return cell.getAncestors().length;
    }
    Private.depthComparator = depthComparator;
})(Private || (Private = {}));
//# sourceMappingURL=selection.js.map