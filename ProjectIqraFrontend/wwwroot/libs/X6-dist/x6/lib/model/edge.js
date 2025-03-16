"use strict";
var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                t[p[i]] = s[p[i]];
        }
    return t;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Edge = void 0;
const x6_common_1 = require("@antv/x6-common");
const x6_geometry_1 = require("@antv/x6-geometry");
const registry_1 = require("../registry/registry");
const markup_1 = require("../view/markup");
const registry_2 = require("./registry");
const cell_1 = require("./cell");
class Edge extends cell_1.Cell {
    get [Symbol.toStringTag]() {
        return Edge.toStringTag;
    }
    constructor(metadata = {}) {
        super(metadata);
    }
    preprocess(metadata, ignoreIdCheck) {
        const { source, sourceCell, sourcePort, sourcePoint, target, targetCell, targetPort, targetPoint } = metadata, others = __rest(metadata, ["source", "sourceCell", "sourcePort", "sourcePoint", "target", "targetCell", "targetPort", "targetPoint"]);
        const data = others;
        const isValidId = (val) => typeof val === 'string' || typeof val === 'number';
        if (source != null) {
            if (cell_1.Cell.isCell(source)) {
                data.source = { cell: source.id };
            }
            else if (isValidId(source)) {
                data.source = { cell: source };
            }
            else if (x6_geometry_1.Point.isPoint(source)) {
                data.source = source.toJSON();
            }
            else if (Array.isArray(source)) {
                data.source = { x: source[0], y: source[1] };
            }
            else {
                const cell = source.cell;
                if (cell_1.Cell.isCell(cell)) {
                    data.source = Object.assign(Object.assign({}, source), { cell: cell.id });
                }
                else {
                    data.source = source;
                }
            }
        }
        if (sourceCell != null || sourcePort != null) {
            let terminal = data.source;
            if (sourceCell != null) {
                const id = isValidId(sourceCell) ? sourceCell : sourceCell.id;
                if (terminal) {
                    terminal.cell = id;
                }
                else {
                    terminal = data.source = { cell: id };
                }
            }
            if (sourcePort != null && terminal) {
                terminal.port = sourcePort;
            }
        }
        else if (sourcePoint != null) {
            data.source = x6_geometry_1.Point.create(sourcePoint).toJSON();
        }
        if (target != null) {
            if (cell_1.Cell.isCell(target)) {
                data.target = { cell: target.id };
            }
            else if (isValidId(target)) {
                data.target = { cell: target };
            }
            else if (x6_geometry_1.Point.isPoint(target)) {
                data.target = target.toJSON();
            }
            else if (Array.isArray(target)) {
                data.target = { x: target[0], y: target[1] };
            }
            else {
                const cell = target.cell;
                if (cell_1.Cell.isCell(cell)) {
                    data.target = Object.assign(Object.assign({}, target), { cell: cell.id });
                }
                else {
                    data.target = target;
                }
            }
        }
        if (targetCell != null || targetPort != null) {
            let terminal = data.target;
            if (targetCell != null) {
                const id = isValidId(targetCell) ? targetCell : targetCell.id;
                if (terminal) {
                    terminal.cell = id;
                }
                else {
                    terminal = data.target = { cell: id };
                }
            }
            if (targetPort != null && terminal) {
                terminal.port = targetPort;
            }
        }
        else if (targetPoint != null) {
            data.target = x6_geometry_1.Point.create(targetPoint).toJSON();
        }
        return super.preprocess(data, ignoreIdCheck);
    }
    setup() {
        super.setup();
        this.on('change:labels', (args) => this.onLabelsChanged(args));
        this.on('change:vertices', (args) => this.onVertexsChanged(args));
    }
    isEdge() {
        return true;
    }
    // #region terminal
    disconnect(options = {}) {
        this.store.set({
            source: { x: 0, y: 0 },
            target: { x: 0, y: 0 },
        }, options);
        return this;
    }
    get source() {
        return this.getSource();
    }
    set source(data) {
        this.setSource(data);
    }
    getSource() {
        return this.getTerminal('source');
    }
    getSourceCellId() {
        return this.source.cell;
    }
    getSourcePortId() {
        return this.source.port;
    }
    setSource(source, args, options = {}) {
        return this.setTerminal('source', source, args, options);
    }
    get target() {
        return this.getTarget();
    }
    set target(data) {
        this.setTarget(data);
    }
    getTarget() {
        return this.getTerminal('target');
    }
    getTargetCellId() {
        return this.target.cell;
    }
    getTargetPortId() {
        return this.target.port;
    }
    setTarget(target, args, options = {}) {
        return this.setTerminal('target', target, args, options);
    }
    getTerminal(type) {
        return Object.assign({}, this.store.get(type));
    }
    setTerminal(type, terminal, args, options = {}) {
        // `terminal` is a cell
        if (cell_1.Cell.isCell(terminal)) {
            this.store.set(type, x6_common_1.ObjectExt.merge({}, args, { cell: terminal.id }), options);
            return this;
        }
        // `terminal` is a point-like object
        const p = terminal;
        if (x6_geometry_1.Point.isPoint(terminal) || (p.x != null && p.y != null)) {
            this.store.set(type, x6_common_1.ObjectExt.merge({}, args, { x: p.x, y: p.y }), options);
            return this;
        }
        // `terminal` is an object
        this.store.set(type, x6_common_1.ObjectExt.cloneDeep(terminal), options);
        return this;
    }
    getSourcePoint() {
        return this.getTerminalPoint('source');
    }
    getTargetPoint() {
        return this.getTerminalPoint('target');
    }
    getTerminalPoint(type) {
        const terminal = this[type];
        if (x6_geometry_1.Point.isPointLike(terminal)) {
            return x6_geometry_1.Point.create(terminal);
        }
        const cell = this.getTerminalCell(type);
        if (cell) {
            return cell.getConnectionPoint(this, type);
        }
        return new x6_geometry_1.Point();
    }
    getSourceCell() {
        return this.getTerminalCell('source');
    }
    getTargetCell() {
        return this.getTerminalCell('target');
    }
    getTerminalCell(type) {
        if (this.model) {
            const cellId = type === 'source' ? this.getSourceCellId() : this.getTargetCellId();
            if (cellId) {
                return this.model.getCell(cellId);
            }
        }
        return null;
    }
    getSourceNode() {
        return this.getTerminalNode('source');
    }
    getTargetNode() {
        return this.getTerminalNode('target');
    }
    getTerminalNode(type) {
        let cell = this; // eslint-disable-line
        const visited = {};
        while (cell && cell.isEdge()) {
            if (visited[cell.id]) {
                return null;
            }
            visited[cell.id] = true;
            cell = cell.getTerminalCell(type);
        }
        return cell && cell.isNode() ? cell : null;
    }
    // #endregion
    // #region router
    get router() {
        return this.getRouter();
    }
    set router(data) {
        if (data == null) {
            this.removeRouter();
        }
        else {
            this.setRouter(data);
        }
    }
    getRouter() {
        return this.store.get('router');
    }
    setRouter(name, args, options) {
        if (typeof name === 'object') {
            this.store.set('router', name, args);
        }
        else {
            this.store.set('router', { name, args }, options);
        }
        return this;
    }
    removeRouter(options = {}) {
        this.store.remove('router', options);
        return this;
    }
    // #endregion
    // #region connector
    get connector() {
        return this.getConnector();
    }
    set connector(data) {
        if (data == null) {
            this.removeConnector();
        }
        else {
            this.setConnector(data);
        }
    }
    getConnector() {
        return this.store.get('connector');
    }
    setConnector(name, args, options) {
        if (typeof name === 'object') {
            this.store.set('connector', name, args);
        }
        else {
            this.store.set('connector', { name, args }, options);
        }
        return this;
    }
    removeConnector(options = {}) {
        return this.store.remove('connector', options);
    }
    // #endregion
    // #region labels
    getDefaultLabel() {
        const ctor = this.constructor;
        const defaults = this.store.get('defaultLabel') || ctor.defaultLabel || {};
        return x6_common_1.ObjectExt.cloneDeep(defaults);
    }
    get labels() {
        return this.getLabels();
    }
    set labels(labels) {
        this.setLabels(labels);
    }
    getLabels() {
        return [...this.store.get('labels', [])].map((item) => this.parseLabel(item));
    }
    setLabels(labels, options = {}) {
        this.store.set('labels', Array.isArray(labels) ? labels : [labels], options);
        return this;
    }
    insertLabel(label, index, options = {}) {
        const labels = this.getLabels();
        const len = labels.length;
        let idx = index != null && Number.isFinite(index) ? index : len;
        if (idx < 0) {
            idx = len + idx + 1;
        }
        labels.splice(idx, 0, this.parseLabel(label));
        return this.setLabels(labels, options);
    }
    appendLabel(label, options = {}) {
        return this.insertLabel(label, -1, options);
    }
    getLabelAt(index) {
        const labels = this.getLabels();
        if (index != null && Number.isFinite(index)) {
            return this.parseLabel(labels[index]);
        }
        return null;
    }
    setLabelAt(index, label, options = {}) {
        if (index != null && Number.isFinite(index)) {
            const labels = this.getLabels();
            labels[index] = this.parseLabel(label);
            this.setLabels(labels, options);
        }
        return this;
    }
    removeLabelAt(index, options = {}) {
        const labels = this.getLabels();
        const idx = index != null && Number.isFinite(index) ? index : -1;
        const removed = labels.splice(idx, 1);
        this.setLabels(labels, options);
        return removed.length ? removed[0] : null;
    }
    parseLabel(label) {
        if (typeof label === 'string') {
            const ctor = this.constructor;
            return ctor.parseStringLabel(label);
        }
        return label;
    }
    onLabelsChanged({ previous, current, }) {
        const added = previous && current
            ? current.filter((label1) => {
                if (!previous.find((label2) => label1 === label2 || x6_common_1.ObjectExt.isEqual(label1, label2))) {
                    return label1;
                }
                return null;
            })
            : current
                ? [...current]
                : [];
        const removed = previous && current
            ? previous.filter((label1) => {
                if (!current.find((label2) => label1 === label2 || x6_common_1.ObjectExt.isEqual(label1, label2))) {
                    return label1;
                }
                return null;
            })
            : previous
                ? [...previous]
                : [];
        if (added.length > 0) {
            this.notify('labels:added', { added, cell: this, edge: this });
        }
        if (removed.length > 0) {
            this.notify('labels:removed', { removed, cell: this, edge: this });
        }
    }
    // #endregion
    // #region vertices
    get vertices() {
        return this.getVertices();
    }
    set vertices(vertices) {
        this.setVertices(vertices);
    }
    getVertices() {
        return [...this.store.get('vertices', [])];
    }
    setVertices(vertices, options = {}) {
        const points = Array.isArray(vertices) ? vertices : [vertices];
        this.store.set('vertices', points.map((p) => x6_geometry_1.Point.toJSON(p)), options);
        return this;
    }
    insertVertex(vertice, index, options = {}) {
        const vertices = this.getVertices();
        const len = vertices.length;
        let idx = index != null && Number.isFinite(index) ? index : len;
        if (idx < 0) {
            idx = len + idx + 1;
        }
        vertices.splice(idx, 0, x6_geometry_1.Point.toJSON(vertice));
        return this.setVertices(vertices, options);
    }
    appendVertex(vertex, options = {}) {
        return this.insertVertex(vertex, -1, options);
    }
    getVertexAt(index) {
        if (index != null && Number.isFinite(index)) {
            const vertices = this.getVertices();
            return vertices[index];
        }
        return null;
    }
    setVertexAt(index, vertice, options = {}) {
        if (index != null && Number.isFinite(index)) {
            const vertices = this.getVertices();
            vertices[index] = vertice;
            this.setVertices(vertices, options);
        }
        return this;
    }
    removeVertexAt(index, options = {}) {
        const vertices = this.getVertices();
        const idx = index != null && Number.isFinite(index) ? index : -1;
        vertices.splice(idx, 1);
        return this.setVertices(vertices, options);
    }
    onVertexsChanged({ previous, current, }) {
        const added = previous && current
            ? current.filter((p1) => {
                if (!previous.find((p2) => x6_geometry_1.Point.equals(p1, p2))) {
                    return p1;
                }
                return null;
            })
            : current
                ? [...current]
                : [];
        const removed = previous && current
            ? previous.filter((p1) => {
                if (!current.find((p2) => x6_geometry_1.Point.equals(p1, p2))) {
                    return p1;
                }
                return null;
            })
            : previous
                ? [...previous]
                : [];
        if (added.length > 0) {
            this.notify('vertexs:added', { added, cell: this, edge: this });
        }
        if (removed.length > 0) {
            this.notify('vertexs:removed', { removed, cell: this, edge: this });
        }
    }
    // #endregion
    // #region markup
    getDefaultMarkup() {
        return this.store.get('defaultMarkup') || markup_1.Markup.getEdgeMarkup();
    }
    getMarkup() {
        return super.getMarkup() || this.getDefaultMarkup();
    }
    // #endregion
    // #region transform
    /**
     * Translate the edge vertices (and source and target if they are points)
     * by `tx` pixels in the x-axis and `ty` pixels in the y-axis.
     */
    translate(tx, ty, options = {}) {
        options.translateBy = options.translateBy || this.id;
        options.tx = tx;
        options.ty = ty;
        return this.applyToPoints((p) => ({
            x: (p.x || 0) + tx,
            y: (p.y || 0) + ty,
        }), options);
    }
    /**
     * Scales the edge's points (vertices) relative to the given origin.
     */
    scale(sx, sy, origin, options = {}) {
        return this.applyToPoints((p) => {
            return x6_geometry_1.Point.create(p).scale(sx, sy, origin).toJSON();
        }, options);
    }
    applyToPoints(worker, options = {}) {
        const attrs = {};
        const source = this.getSource();
        const target = this.getTarget();
        if (x6_geometry_1.Point.isPointLike(source)) {
            attrs.source = worker(source);
        }
        if (x6_geometry_1.Point.isPointLike(target)) {
            attrs.target = worker(target);
        }
        const vertices = this.getVertices();
        if (vertices.length > 0) {
            attrs.vertices = vertices.map(worker);
        }
        this.store.set(attrs, options);
        return this;
    }
    // #endregion
    // #region common
    getBBox() {
        return this.getPolyline().bbox();
    }
    getConnectionPoint() {
        return this.getPolyline().pointAt(0.5);
    }
    getPolyline() {
        const points = [
            this.getSourcePoint(),
            ...this.getVertices().map((vertice) => x6_geometry_1.Point.create(vertice)),
            this.getTargetPoint(),
        ];
        return new x6_geometry_1.Polyline(points);
    }
    updateParent(options) {
        let newParent = null;
        const source = this.getSourceCell();
        const target = this.getTargetCell();
        const prevParent = this.getParent();
        if (source && target) {
            if (source === target || source.isDescendantOf(target)) {
                newParent = target;
            }
            else if (target.isDescendantOf(source)) {
                newParent = source;
            }
            else {
                newParent = cell_1.Cell.getCommonAncestor(source, target);
            }
        }
        // Unembeds the edge if source and target has no common
        // ancestor or common ancestor changed
        if (prevParent && newParent && newParent.id !== prevParent.id) {
            prevParent.unembed(this, options);
        }
        // Embeds the edge if source and target are not same
        if (newParent && (!prevParent || prevParent.id !== newParent.id)) {
            newParent.embed(this, options);
        }
        return newParent;
    }
    hasLoop(options = {}) {
        const source = this.getSource();
        const target = this.getTarget();
        const sourceId = source.cell;
        const targetId = target.cell;
        if (!sourceId || !targetId) {
            return false;
        }
        let loop = sourceId === targetId;
        // Note that there in the deep mode a edge can have a loop,
        // even if it connects only a parent and its embed.
        // A loop "target equals source" is valid in both shallow and deep mode.
        // eslint-disable-next-line
        if (!loop && options.deep && this._model) {
            const sourceCell = this.getSourceCell();
            const targetCell = this.getTargetCell();
            if (sourceCell && targetCell) {
                loop =
                    sourceCell.isAncestorOf(targetCell, options) ||
                        targetCell.isAncestorOf(sourceCell, options);
            }
        }
        return loop;
    }
    getFragmentAncestor() {
        const cells = [this, this.getSourceNode(), this.getTargetNode()].filter((item) => item != null);
        return this.getCommonAncestor(...cells);
    }
    isFragmentDescendantOf(cell) {
        const ancestor = this.getFragmentAncestor();
        return (!!ancestor && (ancestor.id === cell.id || ancestor.isDescendantOf(cell)));
    }
}
exports.Edge = Edge;
Edge.defaults = {};
(function (Edge) {
    function equalTerminals(a, b) {
        const a1 = a;
        const b1 = b;
        if (a1.cell === b1.cell) {
            return a1.port === b1.port || (a1.port == null && b1.port == null);
        }
        return false;
    }
    Edge.equalTerminals = equalTerminals;
})(Edge = exports.Edge || (exports.Edge = {}));
(function (Edge) {
    Edge.defaultLabel = {
        markup: [
            {
                tagName: 'rect',
                selector: 'body',
            },
            {
                tagName: 'text',
                selector: 'label',
            },
        ],
        attrs: {
            text: {
                fill: '#000',
                fontSize: 14,
                textAnchor: 'middle',
                textVerticalAnchor: 'middle',
                pointerEvents: 'none',
            },
            rect: {
                ref: 'label',
                fill: '#fff',
                rx: 3,
                ry: 3,
                refWidth: 1,
                refHeight: 1,
                refX: 0,
                refY: 0,
            },
        },
        position: {
            distance: 0.5,
        },
    };
    function parseStringLabel(text) {
        return {
            attrs: { label: { text } },
        };
    }
    Edge.parseStringLabel = parseStringLabel;
})(Edge = exports.Edge || (exports.Edge = {}));
(function (Edge) {
    Edge.toStringTag = `X6.${Edge.name}`;
    function isEdge(instance) {
        if (instance == null) {
            return false;
        }
        if (instance instanceof Edge) {
            return true;
        }
        const tag = instance[Symbol.toStringTag];
        const edge = instance;
        if ((tag == null || tag === Edge.toStringTag) &&
            typeof edge.isNode === 'function' &&
            typeof edge.isEdge === 'function' &&
            typeof edge.prop === 'function' &&
            typeof edge.attr === 'function' &&
            typeof edge.disconnect === 'function' &&
            typeof edge.getSource === 'function' &&
            typeof edge.getTarget === 'function') {
            return true;
        }
        return false;
    }
    Edge.isEdge = isEdge;
})(Edge = exports.Edge || (exports.Edge = {}));
(function (Edge) {
    Edge.registry = registry_1.Registry.create({
        type: 'edge',
        process(shape, options) {
            if (registry_2.ShareRegistry.exist(shape, false)) {
                throw new Error(`Edge with name '${shape}' was registered by anthor Node`);
            }
            if (typeof options === 'function') {
                options.config({ shape });
                return options;
            }
            let parent = Edge;
            // default inherit from 'dege'
            const { inherit = 'edge' } = options, others = __rest(options, ["inherit"]);
            if (typeof inherit === 'string') {
                const base = this.get(inherit || 'edge');
                if (base == null && inherit) {
                    this.onNotFound(inherit, 'inherited');
                }
                else {
                    parent = base;
                }
            }
            else {
                parent = inherit;
            }
            if (others.constructorName == null) {
                others.constructorName = shape;
            }
            const ctor = parent.define.call(parent, others);
            ctor.config({ shape });
            return ctor;
        },
    });
    registry_2.ShareRegistry.setEdgeRegistry(Edge.registry);
})(Edge = exports.Edge || (exports.Edge = {}));
(function (Edge) {
    let counter = 0;
    function getClassName(name) {
        if (name) {
            return x6_common_1.StringExt.pascalCase(name);
        }
        counter += 1;
        return `CustomEdge${counter}`;
    }
    function define(config) {
        const { constructorName, overwrite } = config, others = __rest(config, ["constructorName", "overwrite"]);
        const ctor = x6_common_1.ObjectExt.createClass(getClassName(constructorName || others.shape), this);
        ctor.config(others);
        if (others.shape) {
            Edge.registry.register(others.shape, ctor, overwrite);
        }
        return ctor;
    }
    Edge.define = define;
    function create(options) {
        const shape = options.shape || 'edge';
        const Ctor = Edge.registry.get(shape);
        if (Ctor) {
            return new Ctor(options);
        }
        return Edge.registry.onNotFound(shape);
    }
    Edge.create = create;
})(Edge = exports.Edge || (exports.Edge = {}));
(function (Edge) {
    const shape = 'basic.edge';
    Edge.config({
        shape,
        propHooks(metadata) {
            const { label, vertices } = metadata, others = __rest(metadata, ["label", "vertices"]);
            if (label) {
                if (others.labels == null) {
                    others.labels = [];
                }
                const formated = typeof label === 'string' ? Edge.parseStringLabel(label) : label;
                others.labels.push(formated);
            }
            if (vertices) {
                if (Array.isArray(vertices)) {
                    others.vertices = vertices.map((item) => x6_geometry_1.Point.create(item).toJSON());
                }
            }
            return others;
        },
    });
    Edge.registry.register(shape, Edge);
})(Edge = exports.Edge || (exports.Edge = {}));
//# sourceMappingURL=edge.js.map