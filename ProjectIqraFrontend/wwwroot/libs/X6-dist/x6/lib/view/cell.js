"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
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
exports.CellView = void 0;
/* eslint-disable @typescript-eslint/no-unused-vars */
const x6_geometry_1 = require("@antv/x6-geometry");
const x6_common_1 = require("@antv/x6-common");
const registry_1 = require("../registry/registry");
const view_1 = require("./view");
const cache_1 = require("./cache");
const markup_1 = require("./markup");
const tool_1 = require("./tool");
const attr_1 = require("./attr");
const flag_1 = require("./flag");
const util_1 = require("../util");
class CellView extends view_1.View {
    static getDefaults() {
        return this.defaults;
    }
    static config(options) {
        this.defaults = this.getOptions(options);
    }
    static getOptions(options) {
        const mergeActions = (arr1, arr2) => {
            if (arr2 != null) {
                return x6_common_1.ArrayExt.uniq([
                    ...(Array.isArray(arr1) ? arr1 : [arr1]),
                    ...(Array.isArray(arr2) ? arr2 : [arr2]),
                ]);
            }
            return Array.isArray(arr1) ? [...arr1] : [arr1];
        };
        const ret = x6_common_1.ObjectExt.cloneDeep(this.getDefaults());
        const { bootstrap, actions, events, documentEvents } = options, others = __rest(options, ["bootstrap", "actions", "events", "documentEvents"]);
        if (bootstrap) {
            ret.bootstrap = mergeActions(ret.bootstrap, bootstrap);
        }
        if (actions) {
            Object.entries(actions).forEach(([key, val]) => {
                const raw = ret.actions[key];
                if (val && raw) {
                    ret.actions[key] = mergeActions(raw, val);
                }
                else if (val) {
                    ret.actions[key] = mergeActions(val);
                }
            });
        }
        if (events) {
            ret.events = Object.assign(Object.assign({}, ret.events), events);
        }
        if (options.documentEvents) {
            ret.documentEvents = Object.assign(Object.assign({}, ret.documentEvents), documentEvents);
        }
        return x6_common_1.ObjectExt.merge(ret, others);
    }
    get [Symbol.toStringTag]() {
        return CellView.toStringTag;
    }
    constructor(cell, options = {}) {
        super();
        this.cell = cell;
        this.options = this.ensureOptions(options);
        this.graph = this.options.graph;
        this.attr = new attr_1.AttrManager(this);
        this.flag = new flag_1.FlagManager(this, this.options.actions, this.options.bootstrap);
        this.cache = new cache_1.Cache(this);
        this.setContainer(this.ensureContainer());
        this.setup();
        this.init();
    }
    init() { }
    onRemove() {
        this.removeTools();
    }
    get priority() {
        return this.options.priority;
    }
    get rootSelector() {
        return this.options.rootSelector;
    }
    getConstructor() {
        return this.constructor;
    }
    ensureOptions(options) {
        return this.getConstructor().getOptions(options);
    }
    getContainerTagName() {
        return this.options.isSvgElement ? 'g' : 'div';
    }
    getContainerStyle() { }
    getContainerAttrs() {
        return {
            'data-cell-id': this.cell.id,
            'data-shape': this.cell.shape,
        };
    }
    getContainerClassName() {
        return this.prefixClassName('cell');
    }
    ensureContainer() {
        return view_1.View.createElement(this.getContainerTagName(), this.options.isSvgElement);
    }
    setContainer(container) {
        if (this.container !== container) {
            this.undelegateEvents();
            this.container = container;
            if (this.options.events != null) {
                this.delegateEvents(this.options.events);
            }
            const attrs = this.getContainerAttrs();
            if (attrs != null) {
                this.setAttrs(attrs, container);
            }
            const style = this.getContainerStyle();
            if (style != null) {
                this.setStyle(style, container);
            }
            const className = this.getContainerClassName();
            if (className != null) {
                this.addClass(className, container);
            }
        }
        return this;
    }
    isNodeView() {
        return false;
    }
    isEdgeView() {
        return false;
    }
    render() {
        return this;
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    confirmUpdate(flag, options = {}) {
        return 0;
    }
    getBootstrapFlag() {
        return this.flag.getBootstrapFlag();
    }
    getFlag(actions) {
        return this.flag.getFlag(actions);
    }
    hasAction(flag, actions) {
        return this.flag.hasAction(flag, actions);
    }
    removeAction(flag, actions) {
        return this.flag.removeAction(flag, actions);
    }
    handleAction(flag, action, handle, additionalRemovedActions) {
        if (this.hasAction(flag, action)) {
            handle();
            const removedFlags = [action];
            if (additionalRemovedActions) {
                if (typeof additionalRemovedActions === 'string') {
                    removedFlags.push(additionalRemovedActions);
                }
                else {
                    removedFlags.push(...additionalRemovedActions);
                }
            }
            return this.removeAction(flag, removedFlags);
        }
        return flag;
    }
    setup() {
        this.cell.on('changed', this.onCellChanged, this);
    }
    onCellChanged({ options }) {
        this.onAttrsChange(options);
    }
    onAttrsChange(options) {
        let flag = this.flag.getChangedFlag();
        if (options.updated || !flag) {
            return;
        }
        if (options.dirty && this.hasAction(flag, 'update')) {
            flag |= this.getFlag('render'); // eslint-disable-line no-bitwise
        }
        // tool changes should be sync render
        if (options.toolId) {
            options.async = false;
        }
        if (this.graph != null) {
            this.graph.renderer.requestViewUpdate(this, flag, options);
        }
    }
    parseJSONMarkup(markup, rootElem) {
        const result = markup_1.Markup.parseJSONMarkup(markup);
        const selectors = result.selectors;
        const rootSelector = this.rootSelector;
        if (rootElem && rootSelector) {
            if (selectors[rootSelector]) {
                throw new Error('Invalid root selector');
            }
            selectors[rootSelector] = rootElem;
        }
        return result;
    }
    can(feature) {
        let interacting = this.graph.options.interacting;
        if (typeof interacting === 'function') {
            interacting = x6_common_1.FunctionExt.call(interacting, this.graph, this);
        }
        if (typeof interacting === 'object') {
            let val = interacting[feature];
            if (typeof val === 'function') {
                val = x6_common_1.FunctionExt.call(val, this.graph, this);
            }
            return val !== false;
        }
        if (typeof interacting === 'boolean') {
            return interacting;
        }
        return false;
    }
    cleanCache() {
        this.cache.clean();
        return this;
    }
    getCache(elem) {
        return this.cache.get(elem);
    }
    getDataOfElement(elem) {
        return this.cache.getData(elem);
    }
    getMatrixOfElement(elem) {
        return this.cache.getMatrix(elem);
    }
    getShapeOfElement(elem) {
        return this.cache.getShape(elem);
    }
    getBoundingRectOfElement(elem) {
        return this.cache.getBoundingRect(elem);
    }
    getBBoxOfElement(elem) {
        const rect = this.getBoundingRectOfElement(elem);
        const matrix = this.getMatrixOfElement(elem);
        const rm = this.getRootRotatedMatrix();
        const tm = this.getRootTranslatedMatrix();
        return util_1.Util.transformRectangle(rect, tm.multiply(rm).multiply(matrix));
    }
    getUnrotatedBBoxOfElement(elem) {
        const rect = this.getBoundingRectOfElement(elem);
        const matrix = this.getMatrixOfElement(elem);
        const tm = this.getRootTranslatedMatrix();
        return util_1.Util.transformRectangle(rect, tm.multiply(matrix));
    }
    getBBox(options = {}) {
        let bbox;
        if (options.useCellGeometry) {
            const cell = this.cell;
            const angle = cell.isNode() ? cell.getAngle() : 0;
            bbox = cell.getBBox().bbox(angle);
        }
        else {
            bbox = this.getBBoxOfElement(this.container);
        }
        return this.graph.coord.localToGraphRect(bbox);
    }
    getRootTranslatedMatrix() {
        const cell = this.cell;
        const pos = cell.isNode() ? cell.getPosition() : { x: 0, y: 0 };
        return x6_common_1.Dom.createSVGMatrix().translate(pos.x, pos.y);
    }
    getRootRotatedMatrix() {
        let matrix = x6_common_1.Dom.createSVGMatrix();
        const cell = this.cell;
        const angle = cell.isNode() ? cell.getAngle() : 0;
        if (angle) {
            const bbox = cell.getBBox();
            const cx = bbox.width / 2;
            const cy = bbox.height / 2;
            matrix = matrix.translate(cx, cy).rotate(angle).translate(-cx, -cy);
        }
        return matrix;
    }
    findMagnet(elem = this.container) {
        return this.findByAttr('magnet', elem);
    }
    updateAttrs(rootNode, attrs, options = {}) {
        if (options.rootBBox == null) {
            options.rootBBox = new x6_geometry_1.Rectangle();
        }
        if (options.selectors == null) {
            options.selectors = this.selectors;
        }
        this.attr.update(rootNode, attrs, options);
    }
    isEdgeElement(magnet) {
        return this.cell.isEdge() && (magnet == null || magnet === this.container);
    }
    // #region highlight
    prepareHighlight(elem, options = {}) {
        const magnet = elem || this.container;
        options.partial = magnet === this.container;
        return magnet;
    }
    highlight(elem, options = {}) {
        const magnet = this.prepareHighlight(elem, options);
        this.notify('cell:highlight', {
            magnet,
            options,
            view: this,
            cell: this.cell,
        });
        if (this.isEdgeView()) {
            this.notify('edge:highlight', {
                magnet,
                options,
                view: this,
                edge: this.cell,
                cell: this.cell,
            });
        }
        else if (this.isNodeView()) {
            this.notify('node:highlight', {
                magnet,
                options,
                view: this,
                node: this.cell,
                cell: this.cell,
            });
        }
        return this;
    }
    unhighlight(elem, options = {}) {
        const magnet = this.prepareHighlight(elem, options);
        this.notify('cell:unhighlight', {
            magnet,
            options,
            view: this,
            cell: this.cell,
        });
        if (this.isNodeView()) {
            this.notify('node:unhighlight', {
                magnet,
                options,
                view: this,
                node: this.cell,
                cell: this.cell,
            });
        }
        else if (this.isEdgeView()) {
            this.notify('edge:unhighlight', {
                magnet,
                options,
                view: this,
                edge: this.cell,
                cell: this.cell,
            });
        }
        return this;
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    notifyUnhighlight(magnet, options) { }
    // #endregion
    getEdgeTerminal(magnet, x, y, edge, type) {
        const cell = this.cell;
        const portId = this.findAttr('port', magnet);
        const selector = magnet.getAttribute('data-selector');
        const terminal = { cell: cell.id };
        if (selector != null) {
            terminal.magnet = selector;
        }
        if (portId != null) {
            terminal.port = portId;
            if (cell.isNode()) {
                if (!cell.hasPort(portId) && selector == null) {
                    // port created via the `port` attribute (not API)
                    terminal.selector = this.getSelector(magnet);
                }
            }
        }
        else if (selector == null && this.container !== magnet) {
            terminal.selector = this.getSelector(magnet);
        }
        return terminal;
    }
    getMagnetFromEdgeTerminal(terminal) {
        const cell = this.cell;
        const root = this.container;
        const portId = terminal.port;
        let selector = terminal.magnet;
        let magnet;
        if (portId != null && cell.isNode() && cell.hasPort(portId)) {
            magnet = this.findPortElem(portId, selector) || root;
        }
        else {
            if (!selector) {
                selector = terminal.selector;
            }
            if (!selector && portId != null) {
                selector = `[port="${portId}"]`;
            }
            magnet = this.findOne(selector, root, this.selectors);
        }
        return magnet;
    }
    hasTools(name) {
        const tools = this.tools;
        if (tools == null) {
            return false;
        }
        if (name == null) {
            return true;
        }
        return tools.name === name;
    }
    addTools(config) {
        this.removeTools();
        if (config) {
            if (!this.can('toolsAddable')) {
                return this;
            }
            const tools = tool_1.ToolsView.isToolsView(config)
                ? config
                : new tool_1.ToolsView(config);
            this.tools = tools;
            tools.config({ view: this });
            tools.mount();
        }
        return this;
    }
    updateTools(options = {}) {
        if (this.tools) {
            this.tools.update(options);
        }
        return this;
    }
    removeTools() {
        if (this.tools) {
            this.tools.remove();
            this.tools = null;
        }
        return this;
    }
    hideTools() {
        if (this.tools) {
            this.tools.hide();
        }
        return this;
    }
    showTools() {
        if (this.tools) {
            this.tools.show();
        }
        return this;
    }
    renderTools() {
        const tools = this.cell.getTools();
        this.addTools(tools);
        return this;
    }
    notify(name, args) {
        this.trigger(name, args);
        this.graph.trigger(name, args);
        return this;
    }
    getEventArgs(e, x, y) {
        const view = this; // eslint-disable-line @typescript-eslint/no-this-alias
        const cell = view.cell;
        if (x == null || y == null) {
            return { e, view, cell };
        }
        return { e, x, y, view, cell };
    }
    onClick(e, x, y) {
        this.notify('cell:click', this.getEventArgs(e, x, y));
    }
    onDblClick(e, x, y) {
        this.notify('cell:dblclick', this.getEventArgs(e, x, y));
    }
    onContextMenu(e, x, y) {
        this.notify('cell:contextmenu', this.getEventArgs(e, x, y));
    }
    onMouseDown(e, x, y) {
        if (this.cell.model) {
            this.cachedModelForMouseEvent = this.cell.model;
            this.cachedModelForMouseEvent.startBatch('mouse');
        }
        this.notify('cell:mousedown', this.getEventArgs(e, x, y));
    }
    onMouseUp(e, x, y) {
        this.notify('cell:mouseup', this.getEventArgs(e, x, y));
        if (this.cachedModelForMouseEvent) {
            this.cachedModelForMouseEvent.stopBatch('mouse', { cell: this.cell });
            this.cachedModelForMouseEvent = null;
        }
    }
    onMouseMove(e, x, y) {
        this.notify('cell:mousemove', this.getEventArgs(e, x, y));
    }
    onMouseOver(e) {
        this.notify('cell:mouseover', this.getEventArgs(e));
    }
    onMouseOut(e) {
        this.notify('cell:mouseout', this.getEventArgs(e));
    }
    onMouseEnter(e) {
        this.notify('cell:mouseenter', this.getEventArgs(e));
    }
    onMouseLeave(e) {
        this.notify('cell:mouseleave', this.getEventArgs(e));
    }
    onMouseWheel(e, x, y, delta) {
        this.notify('cell:mousewheel', Object.assign({ delta }, this.getEventArgs(e, x, y)));
    }
    onCustomEvent(e, name, x, y) {
        this.notify('cell:customevent', Object.assign({ name }, this.getEventArgs(e, x, y)));
        this.notify(name, Object.assign({}, this.getEventArgs(e, x, y)));
    }
    onMagnetMouseDown(e, magnet, x, y) { }
    onMagnetDblClick(e, magnet, x, y) { }
    onMagnetContextMenu(e, magnet, x, y) { }
    onLabelMouseDown(e, x, y) { }
    checkMouseleave(e) {
        const target = this.getEventTarget(e, { fromPoint: true });
        const view = this.graph.findViewByElem(target);
        if (view === this) {
            return;
        }
        // Leaving the current view
        this.onMouseLeave(e);
        if (!view) {
            return;
        }
        // Entering another view
        view.onMouseEnter(e);
    }
    dispose() {
        this.cell.off('changed', this.onCellChanged, this);
    }
}
CellView.defaults = {
    isSvgElement: true,
    rootSelector: 'root',
    priority: 0,
    bootstrap: [],
    actions: {},
};
__decorate([
    CellView.dispose()
], CellView.prototype, "dispose", null);
exports.CellView = CellView;
(function (CellView) {
    CellView.Flag = flag_1.FlagManager;
    CellView.Attr = attr_1.AttrManager;
})(CellView = exports.CellView || (exports.CellView = {}));
(function (CellView) {
    CellView.toStringTag = `X6.${CellView.name}`;
    function isCellView(instance) {
        if (instance == null) {
            return false;
        }
        if (instance instanceof CellView) {
            return true;
        }
        const tag = instance[Symbol.toStringTag];
        const view = instance;
        if ((tag == null || tag === CellView.toStringTag) &&
            typeof view.isNodeView === 'function' &&
            typeof view.isEdgeView === 'function' &&
            typeof view.confirmUpdate === 'function') {
            return true;
        }
        return false;
    }
    CellView.isCellView = isCellView;
})(CellView = exports.CellView || (exports.CellView = {}));
// decorators
// ----
(function (CellView) {
    function priority(value) {
        return function (ctor) {
            ctor.config({ priority: value });
        };
    }
    CellView.priority = priority;
    function bootstrap(actions) {
        return function (ctor) {
            ctor.config({ bootstrap: actions });
        };
    }
    CellView.bootstrap = bootstrap;
})(CellView = exports.CellView || (exports.CellView = {}));
(function (CellView) {
    CellView.registry = registry_1.Registry.create({
        type: 'view',
    });
})(CellView = exports.CellView || (exports.CellView = {}));
//# sourceMappingURL=cell.js.map