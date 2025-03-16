"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.NodeView = void 0;
const x6_common_1 = require("@antv/x6-common");
const x6_geometry_1 = require("@antv/x6-geometry");
const config_1 = require("../config");
const cell_1 = require("../model/cell");
const cell_2 = require("./cell");
const markup_1 = require("./markup");
class NodeView extends cell_2.CellView {
    constructor() {
        super(...arguments);
        this.portsCache = {};
        // #endregion
    }
    get [Symbol.toStringTag]() {
        return NodeView.toStringTag;
    }
    getContainerClassName() {
        const classList = [
            super.getContainerClassName(),
            this.prefixClassName("node"),
        ];
        if (!this.can("nodeMovable")) {
            classList.push(this.prefixClassName("node-immovable"));
        }
        return classList.join(" ");
    }
    updateClassName(e) {
        const target = e.target;
        if (target.hasAttribute("magnet")) {
            // port
            const className = this.prefixClassName("port-unconnectable");
            if (this.can("magnetConnectable")) {
                x6_common_1.Dom.removeClass(target, className);
            }
            else {
                x6_common_1.Dom.addClass(target, className);
            }
        }
        else {
            // node
            const className = this.prefixClassName("node-immovable");
            if (this.can("nodeMovable")) {
                this.removeClass(className);
            }
            else {
                this.addClass(className);
            }
        }
    }
    isNodeView() {
        return true;
    }
    confirmUpdate(flag, options = {}) {
        let ret = flag;
        if (this.hasAction(ret, "ports")) {
            this.removePorts();
            this.cleanPortsCache();
        }
        if (this.hasAction(ret, "render")) {
            this.render();
            ret = this.removeAction(ret, [
                "render",
                "update",
                "resize",
                "translate",
                "rotate",
                "ports",
                "tools",
            ]);
        }
        else {
            ret = this.handleAction(ret, "resize", () => this.resize(), "update");
            ret = this.handleAction(ret, "update", () => this.update(), 
            // `update()` will render ports when useCSSSelectors are enabled
            config_1.Config.useCSSSelector ? "ports" : null);
            ret = this.handleAction(ret, "translate", () => this.translate());
            ret = this.handleAction(ret, "rotate", () => this.rotate());
            ret = this.handleAction(ret, "ports", () => this.renderPorts());
            ret = this.handleAction(ret, "tools", () => {
                if (this.getFlag("tools") === flag) {
                    this.renderTools();
                }
                else {
                    this.updateTools(options);
                }
            });
        }
        return ret;
    }
    update(partialAttrs) {
        this.cleanCache();
        // When CSS selector strings are used, make sure no rule matches port nodes.
        if (config_1.Config.useCSSSelector) {
            this.removePorts();
        }
        const node = this.cell;
        const size = node.getSize();
        const attrs = node.getAttrs();
        this.updateAttrs(this.container, attrs, {
            attrs: partialAttrs === attrs ? null : partialAttrs,
            rootBBox: new x6_geometry_1.Rectangle(0, 0, size.width, size.height),
            selectors: this.selectors,
        });
        if (config_1.Config.useCSSSelector) {
            this.renderPorts();
        }
    }
    renderMarkup() {
        const markup = this.cell.markup;
        if (markup) {
            if (typeof markup === "string") {
                throw new TypeError("Not support string markup.");
            }
            return this.renderJSONMarkup(markup);
        }
        throw new TypeError("Invalid node markup.");
    }
    renderJSONMarkup(markup) {
        const ret = this.parseJSONMarkup(markup, this.container);
        this.selectors = ret.selectors;
        this.container.appendChild(ret.fragment);
    }
    render() {
        this.empty();
        this.renderMarkup();
        this.resize();
        this.updateTransform();
        if (!config_1.Config.useCSSSelector) {
            this.renderPorts();
        }
        this.renderTools();
        return this;
    }
    resize() {
        if (this.cell.getAngle()) {
            this.rotate();
        }
        this.update();
    }
    translate() {
        this.updateTransform();
    }
    rotate() {
        this.updateTransform();
    }
    getTranslationString() {
        const position = this.cell.getPosition();
        return `translate(${position.x},${position.y})`;
    }
    getRotationString() {
        const angle = this.cell.getAngle();
        if (angle) {
            const size = this.cell.getSize();
            return `rotate(${angle},${size.width / 2},${size.height / 2})`;
        }
    }
    updateTransform() {
        let transform = this.getTranslationString();
        const rot = this.getRotationString();
        if (rot) {
            transform += ` ${rot}`;
        }
        this.container.setAttribute("transform", transform);
    }
    // #region ports
    findPortElem(portId, selector) {
        const cache = portId ? this.portsCache[portId] : null;
        if (!cache) {
            return null;
        }
        const portRoot = cache.portContentElement;
        const portSelectors = cache.portContentSelectors || {};
        return this.findOne(selector, portRoot, portSelectors);
    }
    cleanPortsCache() {
        this.portsCache = {};
    }
    removePorts() {
        Object.values(this.portsCache).forEach((cached) => {
            x6_common_1.Dom.remove(cached.portElement);
        });
    }
    renderPorts() {
        const container = this.container;
        // References to rendered elements without z-index
        const references = [];
        container.childNodes.forEach((child) => {
            references.push(child);
        });
        const parsedPorts = this.cell.getParsedPorts();
        const portsGropsByZ = x6_common_1.ArrayExt.groupBy(parsedPorts, "zIndex");
        const autoZIndexKey = "auto";
        // render non-z first
        if (portsGropsByZ[autoZIndexKey]) {
            portsGropsByZ[autoZIndexKey].forEach((port) => {
                const portElement = this.getPortElement(port);
                container.append(portElement);
                references.push(portElement);
            });
        }
        Object.keys(portsGropsByZ).forEach((key) => {
            if (key !== autoZIndexKey) {
                const zIndex = parseInt(key, 10);
                this.appendPorts(portsGropsByZ[key], zIndex, references);
            }
        });
        this.updatePorts();
    }
    appendPorts(ports, zIndex, refs) {
        const elems = ports.map((p) => this.getPortElement(p));
        if (refs[zIndex] || zIndex < 0) {
            x6_common_1.Dom.before(refs[Math.max(zIndex, 0)], elems);
        }
        else {
            x6_common_1.Dom.append(this.container, elems);
        }
    }
    getPortElement(port) {
        const cached = this.portsCache[port.id];
        if (cached) {
            return cached.portElement;
        }
        return this.createPortElement(port);
    }
    createPortElement(port) {
        let renderResult = markup_1.Markup.renderMarkup(this.cell.getPortContainerMarkup());
        const portElement = renderResult.elem;
        if (portElement == null) {
            throw new Error("Invalid port container markup.");
        }
        renderResult = markup_1.Markup.renderMarkup(this.getPortMarkup(port));
        const portContentElement = renderResult.elem;
        const portContentSelectors = renderResult.selectors;
        if (portContentElement == null) {
            throw new Error("Invalid port markup.");
        }
        this.setAttrs({
            port: port.id,
            "port-group": port.group,
        }, portContentElement);
        let portClass = "x6-port";
        if (port.group) {
            portClass += ` x6-port-${port.group}`;
        }
        x6_common_1.Dom.addClass(portElement, portClass);
        x6_common_1.Dom.addClass(portElement, "x6-port");
        x6_common_1.Dom.addClass(portContentElement, "x6-port-body");
        portElement.appendChild(portContentElement);
        let portSelectors = portContentSelectors;
        let portLabelElement;
        let portLabelSelectors;
        const existLabel = this.existPortLabel(port);
        if (existLabel) {
            renderResult = markup_1.Markup.renderMarkup(this.getPortLabelMarkup(port.label));
            portLabelElement = renderResult.elem;
            portLabelSelectors = renderResult.selectors;
            if (portLabelElement == null) {
                throw new Error("Invalid port label markup.");
            }
            if (portContentSelectors && portLabelSelectors) {
                // eslint-disable-next-line
                for (const key in portLabelSelectors) {
                    if (portContentSelectors[key] && key !== this.rootSelector) {
                        throw new Error("Selectors within port must be unique.");
                    }
                }
                portSelectors = Object.assign(Object.assign({}, portContentSelectors), portLabelSelectors);
            }
            x6_common_1.Dom.addClass(portLabelElement, "x6-port-label");
            portElement.appendChild(portLabelElement);
        }
        this.portsCache[port.id] = {
            portElement,
            portSelectors,
            portLabelElement,
            portLabelSelectors,
            portContentElement,
            portContentSelectors,
        };
        if (this.graph.options.onPortRendered) {
            this.graph.options.onPortRendered({
                port,
                node: this.cell,
                container: portElement,
                selectors: portSelectors,
                labelContainer: portLabelElement,
                labelSelectors: portLabelSelectors,
                contentContainer: portContentElement,
                contentSelectors: portContentSelectors,
            });
        }
        return portElement;
    }
    updatePorts() {
        const groups = this.cell.getParsedGroups();
        const groupList = Object.keys(groups);
        if (groupList.length === 0) {
            this.updatePortGroup();
        }
        else {
            groupList.forEach((groupName) => this.updatePortGroup(groupName));
        }
    }
    updatePortGroup(groupName) {
        const bbox = x6_geometry_1.Rectangle.fromSize(this.cell.getSize());
        const metrics = this.cell.getPortsLayoutByGroup(groupName, bbox);
        for (let i = 0, n = metrics.length; i < n; i += 1) {
            const metric = metrics[i];
            const portId = metric.portId;
            const cached = this.portsCache[portId] || {};
            const portLayout = metric.portLayout;
            this.applyPortTransform(cached.portElement, portLayout);
            if (metric.portAttrs != null) {
                const options = {
                    selectors: cached.portSelectors || {},
                };
                if (metric.portSize) {
                    options.rootBBox = x6_geometry_1.Rectangle.fromSize(metric.portSize);
                }
                this.updateAttrs(cached.portElement, metric.portAttrs, options);
            }
            const labelLayout = metric.labelLayout;
            if (labelLayout && cached.portLabelElement) {
                this.applyPortTransform(cached.portLabelElement, labelLayout, -(portLayout.angle || 0));
                if (labelLayout.attrs) {
                    const options = {
                        selectors: cached.portLabelSelectors || {},
                    };
                    if (metric.labelSize) {
                        options.rootBBox = x6_geometry_1.Rectangle.fromSize(metric.labelSize);
                    }
                    this.updateAttrs(cached.portLabelElement, labelLayout.attrs, options);
                }
            }
        }
    }
    applyPortTransform(element, layout, initialAngle = 0) {
        const angle = layout.angle;
        const position = layout.position;
        const matrix = x6_common_1.Dom.createSVGMatrix()
            .rotate(initialAngle)
            .translate(position.x || 0, position.y || 0)
            .rotate(angle || 0);
        x6_common_1.Dom.transform(element, matrix, { absolute: true });
    }
    getPortMarkup(port) {
        return port.markup || this.cell.portMarkup;
    }
    getPortLabelMarkup(label) {
        return label.markup || this.cell.portLabelMarkup;
    }
    existPortLabel(port) {
        return port.attrs && port.attrs.text;
    }
    getEventArgs(e, x, y) {
        const view = this; // eslint-disable-line
        const node = view.cell;
        const cell = node;
        if (x == null || y == null) {
            return { e, view, node, cell };
        }
        return { e, x, y, view, node, cell };
    }
    getPortEventArgs(e, port, pos) {
        const view = this; // eslint-disable-line
        const node = view.cell;
        const cell = node;
        if (pos) {
            return {
                e,
                x: pos.x,
                y: pos.y,
                view,
                node,
                cell,
                port,
            };
        }
        return { e, view, node, cell, port };
    }
    notifyMouseDown(e, x, y) {
        super.onMouseDown(e, x, y);
        this.notify("node:mousedown", this.getEventArgs(e, x, y));
    }
    notifyMouseMove(e, x, y) {
        super.onMouseMove(e, x, y);
        this.notify("node:mousemove", this.getEventArgs(e, x, y));
    }
    notifyMouseUp(e, x, y) {
        super.onMouseUp(e, x, y);
        this.notify("node:mouseup", this.getEventArgs(e, x, y));
    }
    notifyPortEvent(name, e, pos) {
        const port = this.findAttr("port", e.target);
        if (port) {
            const originType = e.type;
            if (name === "node:port:mouseenter") {
                e.type = "mouseenter";
            }
            else if (name === "node:port:mouseleave") {
                e.type = "mouseleave";
            }
            this.notify(name, this.getPortEventArgs(e, port, pos));
            e.type = originType;
        }
    }
    onClick(e, x, y) {
        super.onClick(e, x, y);
        this.notify("node:click", this.getEventArgs(e, x, y));
        this.notifyPortEvent("node:port:click", e, { x, y });
    }
    onDblClick(e, x, y) {
        super.onDblClick(e, x, y);
        this.notify("node:dblclick", this.getEventArgs(e, x, y));
        this.notifyPortEvent("node:port:dblclick", e, { x, y });
    }
    onContextMenu(e, x, y) {
        super.onContextMenu(e, x, y);
        this.notify("node:contextmenu", this.getEventArgs(e, x, y));
        this.notifyPortEvent("node:port:contextmenu", e, { x, y });
    }
    onMouseDown(e, x, y) {
        const isImmovable = this.isElementOrParentImmovable(e.target);
        if (isImmovable) {
            return;
        }
        if (this.isPropagationStopped(e)) {
            return;
        }
        this.notifyMouseDown(e, x, y);
        this.notifyPortEvent("node:port:mousedown", e, { x, y });
        this.startNodeDragging(e, x, y);
    }
    isElementOrParentImmovable(element) {
        let currentElement = element;
        while (currentElement) {
            // Check if the current element is immovable
            if (currentElement.classList.contains("html-shape-immovable")) {
                return true;
            }
            // Stop traversing if we hit the x6-cell or x6-node parent
            if (currentElement.classList.contains("x6-cell") ||
                currentElement.classList.contains("x6-node")) {
                break;
            }
            currentElement = currentElement.parentElement;
        }
        return false;
    }
    onMouseMove(e, x, y) {
        const data = this.getEventData(e);
        const action = data.action;
        if (action === "magnet") {
            this.dragMagnet(e, x, y);
        }
        else {
            if (action === "move") {
                const meta = data;
                const view = meta.targetView || this;
                view.dragNode(e, x, y);
                view.notify("node:moving", {
                    e,
                    x,
                    y,
                    view,
                    cell: view.cell,
                    node: view.cell,
                });
            }
            this.notifyMouseMove(e, x, y);
            this.notifyPortEvent("node:port:mousemove", e, { x, y });
        }
        this.setEventData(e, data);
    }
    onMouseUp(e, x, y) {
        const data = this.getEventData(e);
        const action = data.action;
        if (action === "magnet") {
            this.stopMagnetDragging(e, x, y);
        }
        else {
            this.notifyMouseUp(e, x, y);
            this.notifyPortEvent("node:port:mouseup", e, { x, y });
            if (action === "move") {
                const meta = data;
                const view = meta.targetView || this;
                view.stopNodeDragging(e, x, y);
            }
        }
        const magnet = data.targetMagnet;
        if (magnet) {
            this.onMagnetClick(e, magnet, x, y);
        }
        this.checkMouseleave(e);
    }
    onMouseOver(e) {
        super.onMouseOver(e);
        this.notify("node:mouseover", this.getEventArgs(e));
        // mock mouseenter event,so we can get correct trigger time when move mouse from node to port
        // wo also need to change e.type for use get correct event args
        this.notifyPortEvent("node:port:mouseenter", e);
        this.notifyPortEvent("node:port:mouseover", e);
    }
    onMouseOut(e) {
        super.onMouseOut(e);
        this.notify("node:mouseout", this.getEventArgs(e));
        // mock mouseleave event,so we can get correct trigger time when move mouse from port to node
        // wo also need to change e.type for use get correct event args
        this.notifyPortEvent("node:port:mouseleave", e);
        this.notifyPortEvent("node:port:mouseout", e);
    }
    onMouseEnter(e) {
        this.updateClassName(e);
        super.onMouseEnter(e);
        this.notify("node:mouseenter", this.getEventArgs(e));
    }
    onMouseLeave(e) {
        super.onMouseLeave(e);
        this.notify("node:mouseleave", this.getEventArgs(e));
    }
    onMouseWheel(e, x, y, delta) {
        super.onMouseWheel(e, x, y, delta);
        this.notify("node:mousewheel", Object.assign({ delta }, this.getEventArgs(e, x, y)));
    }
    onMagnetClick(e, magnet, x, y) {
        const graph = this.graph;
        const count = graph.view.getMouseMovedCount(e);
        if (count > graph.options.clickThreshold) {
            return;
        }
        this.notify("node:magnet:click", Object.assign({ magnet }, this.getEventArgs(e, x, y)));
    }
    onMagnetDblClick(e, magnet, x, y) {
        this.notify("node:magnet:dblclick", Object.assign({ magnet }, this.getEventArgs(e, x, y)));
    }
    onMagnetContextMenu(e, magnet, x, y) {
        this.notify("node:magnet:contextmenu", Object.assign({ magnet }, this.getEventArgs(e, x, y)));
    }
    onMagnetMouseDown(e, magnet, x, y) {
        this.startMagnetDragging(e, x, y);
    }
    onCustomEvent(e, name, x, y) {
        this.notify("node:customevent", Object.assign({ name }, this.getEventArgs(e, x, y)));
        super.onCustomEvent(e, name, x, y);
    }
    prepareEmbedding(e) {
        const graph = this.graph;
        const data = this.getEventData(e);
        const node = data.cell || this.cell;
        const view = graph.findViewByCell(node);
        const localPoint = graph.snapToGrid(e.clientX, e.clientY);
        this.notify("node:embed", {
            e,
            node,
            view,
            cell: node,
            x: localPoint.x,
            y: localPoint.y,
            currentParent: node.getParent(),
        });
    }
    processEmbedding(e, data) {
        const cell = data.cell || this.cell;
        const graph = data.graph || this.graph;
        const options = graph.options.embedding;
        const findParent = options.findParent;
        let candidates = typeof findParent === "function"
            ? x6_common_1.FunctionExt.call(findParent, graph, {
                view: this,
                node: this.cell,
            }).filter((c) => {
                return (cell_1.Cell.isCell(c) &&
                    this.cell.id !== c.id &&
                    !c.isDescendantOf(this.cell));
            })
            : graph.model.getNodesUnderNode(cell, {
                by: findParent,
            });
        // Picks the node with the highest `z` index
        if (options.frontOnly) {
            if (candidates.length > 0) {
                const zIndexMap = x6_common_1.ArrayExt.groupBy(candidates, "zIndex");
                const maxZIndex = x6_common_1.ArrayExt.max(Object.keys(zIndexMap).map((z) => parseInt(z, 10)));
                if (maxZIndex) {
                    candidates = zIndexMap[maxZIndex];
                }
            }
        }
        // Filter the nodes which is invisiable
        candidates = candidates.filter((candidate) => candidate.visible);
        let newCandidateView = null;
        const prevCandidateView = data.candidateEmbedView;
        const validateEmbeding = options.validate;
        for (let i = candidates.length - 1; i >= 0; i -= 1) {
            const candidate = candidates[i];
            if (prevCandidateView && prevCandidateView.cell.id === candidate.id) {
                // candidate remains the same
                newCandidateView = prevCandidateView;
                break;
            }
            else {
                const view = candidate.findView(graph);
                if (validateEmbeding &&
                    x6_common_1.FunctionExt.call(validateEmbeding, graph, {
                        child: this.cell,
                        parent: view.cell,
                        childView: this,
                        parentView: view,
                    })) {
                    // flip to the new candidate
                    newCandidateView = view;
                    break;
                }
            }
        }
        this.clearEmbedding(data);
        if (newCandidateView) {
            newCandidateView.highlight(null, { type: "embedding" });
        }
        data.candidateEmbedView = newCandidateView;
        const localPoint = graph.snapToGrid(e.clientX, e.clientY);
        this.notify("node:embedding", {
            e,
            cell,
            node: cell,
            view: graph.findViewByCell(cell),
            x: localPoint.x,
            y: localPoint.y,
            currentParent: cell.getParent(),
            candidateParent: newCandidateView ? newCandidateView.cell : null,
        });
    }
    clearEmbedding(data) {
        const candidateView = data.candidateEmbedView;
        if (candidateView) {
            candidateView.unhighlight(null, { type: "embedding" });
            data.candidateEmbedView = null;
        }
    }
    finalizeEmbedding(e, data) {
        this.graph.startBatch("embedding");
        const cell = data.cell || this.cell;
        const graph = data.graph || this.graph;
        const view = graph.findViewByCell(cell);
        const parent = cell.getParent();
        const candidateView = data.candidateEmbedView;
        if (candidateView) {
            // Candidate view is chosen to become the parent of the node.
            candidateView.unhighlight(null, { type: "embedding" });
            data.candidateEmbedView = null;
            if (parent == null || parent.id !== candidateView.cell.id) {
                candidateView.cell.insertChild(cell, undefined, { ui: true });
            }
        }
        else if (parent) {
            parent.unembed(cell, { ui: true });
        }
        graph.model.getConnectedEdges(cell, { deep: true }).forEach((edge) => {
            edge.updateParent({ ui: true });
        });
        if (view && candidateView) {
            const localPoint = graph.snapToGrid(e.clientX, e.clientY);
            view.notify("node:embedded", {
                e,
                cell,
                x: localPoint.x,
                y: localPoint.y,
                node: cell,
                view: graph.findViewByCell(cell),
                previousParent: parent,
                currentParent: cell.getParent(),
            });
        }
        this.graph.stopBatch("embedding");
    }
    getDelegatedView() {
        let cell = this.cell;
        let view = this; // eslint-disable-line
        while (view) {
            if (cell.isEdge()) {
                break;
            }
            if (!cell.hasParent() || view.can("stopDelegateOnDragging")) {
                return view;
            }
            cell = cell.getParent();
            view = this.graph.findViewByCell(cell);
        }
        return null;
    }
    validateMagnet(cellView, magnet, e) {
        if (magnet.getAttribute("magnet") !== "passive") {
            const validate = this.graph.options.connecting.validateMagnet;
            if (validate) {
                return x6_common_1.FunctionExt.call(validate, this.graph, {
                    e,
                    magnet,
                    view: cellView,
                    cell: cellView.cell,
                });
            }
            return true;
        }
        return false;
    }
    startMagnetDragging(e, x, y) {
        if (!this.can("magnetConnectable")) {
            return;
        }
        e.stopPropagation();
        const magnet = e.currentTarget;
        const graph = this.graph;
        this.setEventData(e, {
            targetMagnet: magnet,
        });
        if (this.validateMagnet(this, magnet, e)) {
            if (graph.options.magnetThreshold <= 0) {
                this.startConnectting(e, magnet, x, y);
            }
            this.setEventData(e, {
                action: "magnet",
            });
            this.stopPropagation(e);
        }
        else {
            this.onMouseDown(e, x, y);
        }
        graph.view.delegateDragEvents(e, this);
    }
    startConnectting(e, magnet, x, y) {
        this.graph.model.startBatch("add-edge");
        const edgeView = this.createEdgeFromMagnet(magnet, x, y);
        edgeView.setEventData(e, edgeView.prepareArrowheadDragging("target", {
            x,
            y,
            isNewEdge: true,
            fallbackAction: "remove",
        }));
        this.setEventData(e, { edgeView });
        edgeView.notifyMouseDown(e, x, y);
    }
    getDefaultEdge(sourceView, sourceMagnet) {
        let edge;
        const create = this.graph.options.connecting.createEdge;
        if (create) {
            edge = x6_common_1.FunctionExt.call(create, this.graph, {
                sourceMagnet,
                sourceView,
                sourceCell: sourceView.cell,
            });
        }
        return edge;
    }
    createEdgeFromMagnet(magnet, x, y) {
        const graph = this.graph;
        const model = graph.model;
        const edge = this.getDefaultEdge(this, magnet);
        edge.setSource(Object.assign(Object.assign({}, edge.getSource()), this.getEdgeTerminal(magnet, x, y, edge, "source")));
        edge.setTarget(Object.assign(Object.assign({}, edge.getTarget()), { x, y }));
        edge.addTo(model, { async: false, ui: true });
        return edge.findView(graph);
    }
    dragMagnet(e, x, y) {
        const data = this.getEventData(e);
        const edgeView = data.edgeView;
        if (edgeView) {
            edgeView.onMouseMove(e, x, y);
            this.autoScrollGraph(e.clientX, e.clientY);
        }
        else {
            const graph = this.graph;
            const magnetThreshold = graph.options.magnetThreshold;
            const currentTarget = this.getEventTarget(e);
            const targetMagnet = data.targetMagnet;
            // magnetThreshold when the pointer leaves the magnet
            if (magnetThreshold === "onleave") {
                if (targetMagnet === currentTarget ||
                    targetMagnet.contains(currentTarget)) {
                    return;
                }
                // eslint-disable-next-line no-lonely-if
            }
            else {
                // magnetThreshold defined as a number of movements
                if (graph.view.getMouseMovedCount(e) <= magnetThreshold) {
                    return;
                }
            }
            this.startConnectting(e, targetMagnet, x, y);
        }
    }
    stopMagnetDragging(e, x, y) {
        const data = this.eventData(e);
        const edgeView = data.edgeView;
        if (edgeView) {
            edgeView.onMouseUp(e, x, y);
            this.graph.model.stopBatch("add-edge");
        }
    }
    notifyUnhandledMouseDown(e, x, y) {
        this.notify("node:unhandled:mousedown", {
            e,
            x,
            y,
            view: this,
            cell: this.cell,
            node: this.cell,
        });
    }
    notifyNodeMove(name, e, x, y, cell) {
        let cells = [cell];
        const selection = this.graph.getPlugin("selection");
        if (selection && selection.isSelectionMovable()) {
            const selectedCells = selection.getSelectedCells();
            if (selectedCells.includes(cell)) {
                cells = selectedCells.filter((c) => c.isNode());
            }
        }
        cells.forEach((c) => {
            this.notify(name, {
                e,
                x,
                y,
                cell: c,
                node: c,
                view: c.findView(this.graph),
            });
        });
    }
    getRestrictArea(view) {
        const restrict = this.graph.options.translating.restrict;
        const area = typeof restrict === "function"
            ? x6_common_1.FunctionExt.call(restrict, this.graph, view)
            : restrict;
        if (typeof area === "number") {
            return this.graph.transform.getGraphArea().inflate(area);
        }
        if (area === true) {
            return this.graph.transform.getGraphArea();
        }
        return area || null;
    }
    startNodeDragging(e, x, y) {
        const targetView = this.getDelegatedView();
        if (targetView == null || !targetView.can("nodeMovable")) {
            return this.notifyUnhandledMouseDown(e, x, y);
        }
        this.setEventData(e, {
            targetView,
            action: "move",
        });
        const position = x6_geometry_1.Point.create(targetView.cell.getPosition());
        targetView.setEventData(e, {
            moving: false,
            offset: position.diff(x, y),
            restrict: this.getRestrictArea(targetView),
        });
    }
    dragNode(e, x, y) {
        const node = this.cell;
        const graph = this.graph;
        const gridSize = graph.getGridSize();
        const data = this.getEventData(e);
        const offset = data.offset;
        const restrict = data.restrict;
        if (!data.moving) {
            data.moving = true;
            this.addClass("node-moving");
            this.notifyNodeMove("node:move", e, x, y, this.cell);
        }
        this.autoScrollGraph(e.clientX, e.clientY);
        const posX = x6_geometry_1.GeometryUtil.snapToGrid(x + offset.x, gridSize);
        const posY = x6_geometry_1.GeometryUtil.snapToGrid(y + offset.y, gridSize);
        node.setPosition(posX, posY, {
            restrict,
            deep: true,
            ui: true,
        });
        if (graph.options.embedding.enabled) {
            if (!data.embedding) {
                this.prepareEmbedding(e);
                data.embedding = true;
            }
            this.processEmbedding(e, data);
        }
    }
    stopNodeDragging(e, x, y) {
        const data = this.getEventData(e);
        if (data.embedding) {
            this.finalizeEmbedding(e, data);
        }
        if (data.moving) {
            this.removeClass("node-moving");
            this.notifyNodeMove("node:moved", e, x, y, this.cell);
        }
        data.moving = false;
        data.embedding = false;
    }
    // eslint-disable-next-line
    autoScrollGraph(x, y) {
        const scroller = this.graph.getPlugin("scroller");
        if (scroller) {
            scroller.autoScroll(x, y);
        }
    }
}
exports.NodeView = NodeView;
(function (NodeView) {
    NodeView.toStringTag = `X6.${NodeView.name}`;
    function isNodeView(instance) {
        if (instance == null) {
            return false;
        }
        if (instance instanceof NodeView) {
            return true;
        }
        const tag = instance[Symbol.toStringTag];
        const view = instance;
        if ((tag == null || tag === NodeView.toStringTag) &&
            typeof view.isNodeView === "function" &&
            typeof view.isEdgeView === "function" &&
            typeof view.confirmUpdate === "function" &&
            typeof view.update === "function" &&
            typeof view.findPortElem === "function" &&
            typeof view.resize === "function" &&
            typeof view.rotate === "function" &&
            typeof view.translate === "function") {
            return true;
        }
        return false;
    }
    NodeView.isNodeView = isNodeView;
})(NodeView = exports.NodeView || (exports.NodeView = {}));
NodeView.config({
    isSvgElement: true,
    priority: 0,
    bootstrap: ["render"],
    actions: {
        view: ["render"],
        markup: ["render"],
        attrs: ["update"],
        size: ["resize", "ports", "tools"],
        angle: ["rotate", "tools"],
        position: ["translate", "tools"],
        ports: ["ports"],
        tools: ["tools"],
    },
});
NodeView.registry.register("node", NodeView, true);
//# sourceMappingURL=node.js.map