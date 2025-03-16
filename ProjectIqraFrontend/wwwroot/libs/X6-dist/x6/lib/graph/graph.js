"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Graph = void 0;
const x6_common_1 = require("@antv/x6-common");
const x6_geometry_1 = require("@antv/x6-geometry");
const model_1 = require("../model");
const view_1 = require("../view");
const Registry = __importStar(require("../registry"));
const view_2 = require("./view");
const css_1 = require("./css");
const options_1 = require("./options");
const grid_1 = require("./grid");
const transform_1 = require("./transform");
const background_1 = require("./background");
const panning_1 = require("./panning");
const mousewheel_1 = require("./mousewheel");
const virtual_render_1 = require("./virtual-render");
const renderer_1 = require("../renderer");
const defs_1 = require("./defs");
const coord_1 = require("./coord");
const highlight_1 = require("./highlight");
const size_1 = require("./size");
class Graph extends x6_common_1.Basecoat {
    get container() {
        return this.options.container;
    }
    get [Symbol.toStringTag]() {
        return Graph.toStringTag;
    }
    constructor(options) {
        super();
        this.installedPlugins = new Set();
        this.options = options_1.Options.get(options);
        this.css = new css_1.CSSManager(this);
        this.view = new view_2.GraphView(this);
        this.defs = new defs_1.DefsManager(this);
        this.coord = new coord_1.CoordManager(this);
        this.transform = new transform_1.TransformManager(this);
        this.highlight = new highlight_1.HighlightManager(this);
        this.grid = new grid_1.GridManager(this);
        this.background = new background_1.BackgroundManager(this);
        if (this.options.model) {
            this.model = this.options.model;
        }
        else {
            this.model = new model_1.Model();
            this.model.graph = this;
        }
        this.renderer = new renderer_1.Renderer(this);
        this.panning = new panning_1.PanningManager(this);
        this.mousewheel = new mousewheel_1.MouseWheel(this);
        this.virtualRender = new virtual_render_1.VirtualRenderManager(this);
        this.size = new size_1.SizeManager(this);
    }
    // #region model
    isNode(cell) {
        return cell.isNode();
    }
    isEdge(cell) {
        return cell.isEdge();
    }
    resetCells(cells, options = {}) {
        this.model.resetCells(cells, options);
        return this;
    }
    clearCells(options = {}) {
        this.model.clear(options);
        return this;
    }
    toJSON(options = {}) {
        return this.model.toJSON(options);
    }
    parseJSON(data) {
        return this.model.parseJSON(data);
    }
    fromJSON(data, options = {}) {
        this.model.fromJSON(data, options);
        return this;
    }
    getCellById(id) {
        return this.model.getCell(id);
    }
    addNode(node, options = {}) {
        return this.model.addNode(node, options);
    }
    addNodes(nodes, options = {}) {
        return this.addCell(nodes.map((node) => (model_1.Node.isNode(node) ? node : this.createNode(node))), options);
    }
    createNode(metadata) {
        return this.model.createNode(metadata);
    }
    removeNode(node, options = {}) {
        return this.model.removeCell(node, options);
    }
    addEdge(edge, options = {}) {
        return this.model.addEdge(edge, options);
    }
    addEdges(edges, options = {}) {
        return this.addCell(edges.map((edge) => (model_1.Edge.isEdge(edge) ? edge : this.createEdge(edge))), options);
    }
    removeEdge(edge, options = {}) {
        return this.model.removeCell(edge, options);
    }
    createEdge(metadata) {
        return this.model.createEdge(metadata);
    }
    addCell(cell, options = {}) {
        this.model.addCell(cell, options);
        return this;
    }
    removeCell(cell, options = {}) {
        return this.model.removeCell(cell, options);
    }
    removeCells(cells, options = {}) {
        return this.model.removeCells(cells, options);
    }
    removeConnectedEdges(cell, options = {}) {
        return this.model.removeConnectedEdges(cell, options);
    }
    disconnectConnectedEdges(cell, options = {}) {
        this.model.disconnectConnectedEdges(cell, options);
        return this;
    }
    hasCell(cell) {
        return this.model.has(cell);
    }
    getCells() {
        return this.model.getCells();
    }
    getCellCount() {
        return this.model.total();
    }
    /**
     * Returns all the nodes in the graph.
     */
    getNodes() {
        return this.model.getNodes();
    }
    /**
     * Returns all the edges in the graph.
     */
    getEdges() {
        return this.model.getEdges();
    }
    /**
     * Returns all outgoing edges for the node.
     */
    getOutgoingEdges(cell) {
        return this.model.getOutgoingEdges(cell);
    }
    /**
     * Returns all incoming edges for the node.
     */
    getIncomingEdges(cell) {
        return this.model.getIncomingEdges(cell);
    }
    /**
     * Returns edges connected with cell.
     */
    getConnectedEdges(cell, options = {}) {
        return this.model.getConnectedEdges(cell, options);
    }
    /**
     * Returns an array of all the roots of the graph.
     */
    getRootNodes() {
        return this.model.getRoots();
    }
    /**
     * Returns an array of all the leafs of the graph.
     */
    getLeafNodes() {
        return this.model.getLeafs();
    }
    /**
     * Returns `true` if the node is a root node, i.e.
     * there is no  edges coming to the node.
     */
    isRootNode(cell) {
        return this.model.isRoot(cell);
    }
    /**
     * Returns `true` if the node is a leaf node, i.e.
     * there is no edges going out from the node.
     */
    isLeafNode(cell) {
        return this.model.isLeaf(cell);
    }
    /**
     * Returns all the neighbors of node in the graph. Neighbors are all
     * the nodes connected to node via either incoming or outgoing edge.
     */
    getNeighbors(cell, options = {}) {
        return this.model.getNeighbors(cell, options);
    }
    /**
     * Returns `true` if `cell2` is a neighbor of `cell1`.
     */
    isNeighbor(cell1, cell2, options = {}) {
        return this.model.isNeighbor(cell1, cell2, options);
    }
    getSuccessors(cell, options = {}) {
        return this.model.getSuccessors(cell, options);
    }
    /**
     * Returns `true` if `cell2` is a successor of `cell1`.
     */
    isSuccessor(cell1, cell2, options = {}) {
        return this.model.isSuccessor(cell1, cell2, options);
    }
    getPredecessors(cell, options = {}) {
        return this.model.getPredecessors(cell, options);
    }
    /**
     * Returns `true` if `cell2` is a predecessor of `cell1`.
     */
    isPredecessor(cell1, cell2, options = {}) {
        return this.model.isPredecessor(cell1, cell2, options);
    }
    getCommonAncestor(...cells) {
        return this.model.getCommonAncestor(...cells);
    }
    /**
     * Returns an array of cells that result from finding nodes/edges that
     * are connected to any of the cells in the cells array. This function
     * loops over cells and if the current cell is a edge, it collects its
     * source/target nodes; if it is an node, it collects its incoming and
     * outgoing edges if both the edge terminal (source/target) are in the
     * cells array.
     */
    getSubGraph(cells, options = {}) {
        return this.model.getSubGraph(cells, options);
    }
    /**
     * Clones the whole subgraph (including all the connected links whose
     * source/target is in the subgraph). If `options.deep` is `true`, also
     * take into account all the embedded cells of all the subgraph cells.
     *
     * Returns a map of the form: { [original cell ID]: [clone] }.
     */
    cloneSubGraph(cells, options = {}) {
        return this.model.cloneSubGraph(cells, options);
    }
    cloneCells(cells) {
        return this.model.cloneCells(cells);
    }
    getNodesFromPoint(x, y) {
        return this.model.getNodesFromPoint(x, y);
    }
    getNodesInArea(x, y, w, h, options) {
        return this.model.getNodesInArea(x, y, w, h, options);
    }
    getNodesUnderNode(node, options = {}) {
        return this.model.getNodesUnderNode(node, options);
    }
    searchCell(cell, iterator, options = {}) {
        this.model.search(cell, iterator, options);
        return this;
    }
    /** *
     * Returns an array of IDs of nodes on the shortest
     * path between source and target.
     */
    getShortestPath(source, target, options = {}) {
        return this.model.getShortestPath(source, target, options);
    }
    /**
     * Returns the bounding box that surrounds all cells in the graph.
     */
    getAllCellsBBox() {
        return this.model.getAllCellsBBox();
    }
    /**
     * Returns the bounding box that surrounds all the given cells.
     */
    getCellsBBox(cells, options = {}) {
        return this.model.getCellsBBox(cells, options);
    }
    startBatch(name, data = {}) {
        this.model.startBatch(name, data);
    }
    stopBatch(name, data = {}) {
        this.model.stopBatch(name, data);
    }
    batchUpdate(arg1, arg2, arg3) {
        const name = typeof arg1 === 'string' ? arg1 : 'update';
        const execute = typeof arg1 === 'string' ? arg2 : arg1;
        const data = typeof arg2 === 'function' ? arg3 : arg2;
        this.startBatch(name, data);
        const result = execute();
        this.stopBatch(name, data);
        return result;
    }
    updateCellId(cell, newId) {
        return this.model.updateCellId(cell, newId);
    }
    // #endregion
    // #region view
    findView(ref) {
        if (model_1.Cell.isCell(ref)) {
            return this.findViewByCell(ref);
        }
        return this.findViewByElem(ref);
    }
    findViews(ref) {
        if (x6_geometry_1.Rectangle.isRectangleLike(ref)) {
            return this.findViewsInArea(ref);
        }
        if (x6_geometry_1.Point.isPointLike(ref)) {
            return this.findViewsFromPoint(ref);
        }
        return [];
    }
    findViewByCell(cell) {
        return this.renderer.findViewByCell(cell);
    }
    findViewByElem(elem) {
        return this.renderer.findViewByElem(elem);
    }
    findViewsFromPoint(x, y) {
        const p = typeof x === 'number' ? { x, y: y } : x;
        return this.renderer.findViewsFromPoint(p);
    }
    findViewsInArea(x, y, width, height, options) {
        const rect = typeof x === 'number'
            ? {
                x,
                y: y,
                width: width,
                height: height,
            }
            : x;
        const localOptions = typeof x === 'number'
            ? options
            : y;
        return this.renderer.findViewsInArea(rect, localOptions);
    }
    matrix(mat) {
        if (typeof mat === 'undefined') {
            return this.transform.getMatrix();
        }
        this.transform.setMatrix(mat);
        return this;
    }
    resize(width, height) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.resize(width, height);
        }
        else {
            this.transform.resize(width, height);
        }
        return this;
    }
    scale(sx, sy = sx, cx = 0, cy = 0) {
        if (typeof sx === 'undefined') {
            return this.transform.getScale();
        }
        this.transform.scale(sx, sy, cx, cy);
        return this;
    }
    zoom(factor, options) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            if (typeof factor === 'undefined') {
                return scroller.zoom();
            }
            scroller.zoom(factor, options);
        }
        else {
            if (typeof factor === 'undefined') {
                return this.transform.getZoom();
            }
            this.transform.zoom(factor, options);
        }
        return this;
    }
    zoomTo(factor, options = {}) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.zoom(factor, Object.assign(Object.assign({}, options), { absolute: true }));
        }
        else {
            this.transform.zoom(factor, Object.assign(Object.assign({}, options), { absolute: true }));
        }
        return this;
    }
    zoomToRect(rect, options = {}) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.zoomToRect(rect, options);
        }
        else {
            this.transform.zoomToRect(rect, options);
        }
        return this;
    }
    zoomToFit(options = {}) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.zoomToFit(options);
        }
        else {
            this.transform.zoomToFit(options);
        }
        return this;
    }
    rotate(angle, cx, cy) {
        if (typeof angle === 'undefined') {
            return this.transform.getRotation();
        }
        this.transform.rotate(angle, cx, cy);
        return this;
    }
    translate(tx, ty) {
        if (typeof tx === 'undefined') {
            return this.transform.getTranslation();
        }
        this.transform.translate(tx, ty);
        return this;
    }
    translateBy(dx, dy) {
        const ts = this.translate();
        const tx = ts.tx + dx;
        const ty = ts.ty + dy;
        return this.translate(tx, ty);
    }
    getGraphArea() {
        return this.transform.getGraphArea();
    }
    getContentArea(options = {}) {
        return this.transform.getContentArea(options);
    }
    getContentBBox(options = {}) {
        return this.transform.getContentBBox(options);
    }
    fitToContent(gridWidth, gridHeight, padding, options) {
        return this.transform.fitToContent(gridWidth, gridHeight, padding, options);
    }
    scaleContentToFit(options = {}) {
        this.transform.scaleContentToFit(options);
        return this;
    }
    /**
     * Position the center of graph to the center of the viewport.
     */
    center(options) {
        return this.centerPoint(options);
    }
    centerPoint(x, y, options) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.centerPoint(x, y, options);
        }
        else {
            this.transform.centerPoint(x, y);
        }
        return this;
    }
    centerContent(options) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.centerContent(options);
        }
        else {
            this.transform.centerContent(options);
        }
        return this;
    }
    centerCell(cell, options) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.centerCell(cell, options);
        }
        else {
            this.transform.centerCell(cell);
        }
        return this;
    }
    positionPoint(point, x, y, options = {}) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.positionPoint(point, x, y, options);
        }
        else {
            this.transform.positionPoint(point, x, y);
        }
        return this;
    }
    positionRect(rect, direction, options) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.positionRect(rect, direction, options);
        }
        else {
            this.transform.positionRect(rect, direction);
        }
        return this;
    }
    positionCell(cell, direction, options) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.positionCell(cell, direction, options);
        }
        else {
            this.transform.positionCell(cell, direction);
        }
        return this;
    }
    positionContent(pos, options) {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.positionContent(pos, options);
        }
        else {
            this.transform.positionContent(pos, options);
        }
        return this;
    }
    snapToGrid(x, y) {
        return this.coord.snapToGrid(x, y);
    }
    pageToLocal(x, y, width, height) {
        if (x6_geometry_1.Rectangle.isRectangleLike(x)) {
            return this.coord.pageToLocalRect(x);
        }
        if (typeof x === 'number' &&
            typeof y === 'number' &&
            typeof width === 'number' &&
            typeof height === 'number') {
            return this.coord.pageToLocalRect(x, y, width, height);
        }
        return this.coord.pageToLocalPoint(x, y);
    }
    localToPage(x, y, width, height) {
        if (x6_geometry_1.Rectangle.isRectangleLike(x)) {
            return this.coord.localToPageRect(x);
        }
        if (typeof x === 'number' &&
            typeof y === 'number' &&
            typeof width === 'number' &&
            typeof height === 'number') {
            return this.coord.localToPageRect(x, y, width, height);
        }
        return this.coord.localToPagePoint(x, y);
    }
    clientToLocal(x, y, width, height) {
        if (x6_geometry_1.Rectangle.isRectangleLike(x)) {
            return this.coord.clientToLocalRect(x);
        }
        if (typeof x === 'number' &&
            typeof y === 'number' &&
            typeof width === 'number' &&
            typeof height === 'number') {
            return this.coord.clientToLocalRect(x, y, width, height);
        }
        return this.coord.clientToLocalPoint(x, y);
    }
    localToClient(x, y, width, height) {
        if (x6_geometry_1.Rectangle.isRectangleLike(x)) {
            return this.coord.localToClientRect(x);
        }
        if (typeof x === 'number' &&
            typeof y === 'number' &&
            typeof width === 'number' &&
            typeof height === 'number') {
            return this.coord.localToClientRect(x, y, width, height);
        }
        return this.coord.localToClientPoint(x, y);
    }
    localToGraph(x, y, width, height) {
        if (x6_geometry_1.Rectangle.isRectangleLike(x)) {
            return this.coord.localToGraphRect(x);
        }
        if (typeof x === 'number' &&
            typeof y === 'number' &&
            typeof width === 'number' &&
            typeof height === 'number') {
            return this.coord.localToGraphRect(x, y, width, height);
        }
        return this.coord.localToGraphPoint(x, y);
    }
    graphToLocal(x, y, width, height) {
        if (x6_geometry_1.Rectangle.isRectangleLike(x)) {
            return this.coord.graphToLocalRect(x);
        }
        if (typeof x === 'number' &&
            typeof y === 'number' &&
            typeof width === 'number' &&
            typeof height === 'number') {
            return this.coord.graphToLocalRect(x, y, width, height);
        }
        return this.coord.graphToLocalPoint(x, y);
    }
    clientToGraph(x, y, width, height) {
        if (x6_geometry_1.Rectangle.isRectangleLike(x)) {
            return this.coord.clientToGraphRect(x);
        }
        if (typeof x === 'number' &&
            typeof y === 'number' &&
            typeof width === 'number' &&
            typeof height === 'number') {
            return this.coord.clientToGraphRect(x, y, width, height);
        }
        return this.coord.clientToGraphPoint(x, y);
    }
    // #endregion
    // #region defs
    defineFilter(options) {
        return this.defs.filter(options);
    }
    defineGradient(options) {
        return this.defs.gradient(options);
    }
    defineMarker(options) {
        return this.defs.marker(options);
    }
    // #endregion
    // #region grid
    getGridSize() {
        return this.grid.getGridSize();
    }
    setGridSize(gridSize) {
        this.grid.setGridSize(gridSize);
        return this;
    }
    showGrid() {
        this.grid.show();
        return this;
    }
    hideGrid() {
        this.grid.hide();
        return this;
    }
    clearGrid() {
        this.grid.clear();
        return this;
    }
    drawGrid(options) {
        this.grid.draw(options);
        return this;
    }
    // #endregion
    // #region background
    updateBackground() {
        this.background.update();
        return this;
    }
    drawBackground(options, onGraph) {
        const scroller = this.getPlugin('scroller');
        if (scroller != null && (this.options.background == null || !onGraph)) {
            scroller.drawBackground(options, onGraph);
        }
        else {
            this.background.draw(options);
        }
        return this;
    }
    clearBackground(onGraph) {
        const scroller = this.getPlugin('scroller');
        if (scroller != null && (this.options.background == null || !onGraph)) {
            scroller.clearBackground(onGraph);
        }
        else {
            this.background.clear();
        }
        return this;
    }
    // #endregion
    // #region virtual-render
    enableVirtualRender() {
        this.virtualRender.enableVirtualRender();
        return this;
    }
    disableVirtualRender() {
        this.virtualRender.disableVirtualRender();
        return this;
    }
    // #endregion
    // #region mousewheel
    isMouseWheelEnabled() {
        return !this.mousewheel.disabled;
    }
    enableMouseWheel() {
        this.mousewheel.enable();
        return this;
    }
    disableMouseWheel() {
        this.mousewheel.disable();
        return this;
    }
    toggleMouseWheel(enabled) {
        if (enabled == null) {
            if (this.isMouseWheelEnabled()) {
                this.disableMouseWheel();
            }
            else {
                this.enableMouseWheel();
            }
        }
        else if (enabled) {
            this.enableMouseWheel();
        }
        else {
            this.disableMouseWheel();
        }
        return this;
    }
    // #endregion
    // #region panning
    isPannable() {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            return scroller.isPannable();
        }
        return this.panning.pannable;
    }
    enablePanning() {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.enablePanning();
        }
        else {
            this.panning.enablePanning();
        }
        return this;
    }
    disablePanning() {
        const scroller = this.getPlugin('scroller');
        if (scroller) {
            scroller.disablePanning();
        }
        else {
            this.panning.disablePanning();
        }
        return this;
    }
    togglePanning(pannable) {
        if (pannable == null) {
            if (this.isPannable()) {
                this.disablePanning();
            }
            else {
                this.enablePanning();
            }
        }
        else if (pannable !== this.isPannable()) {
            if (pannable) {
                this.enablePanning();
            }
            else {
                this.disablePanning();
            }
        }
        return this;
    }
    // #endregion
    // #region plugin
    use(plugin, ...options) {
        if (!this.installedPlugins.has(plugin)) {
            this.installedPlugins.add(plugin);
            plugin.init(this, ...options);
        }
        return this;
    }
    getPlugin(pluginName) {
        return Array.from(this.installedPlugins).find((plugin) => plugin.name === pluginName);
    }
    getPlugins(pluginName) {
        return Array.from(this.installedPlugins).filter((plugin) => pluginName.includes(plugin.name));
    }
    enablePlugins(plugins) {
        let postPlugins = plugins;
        if (!Array.isArray(postPlugins)) {
            postPlugins = [postPlugins];
        }
        const aboutToChangePlugins = this.getPlugins(postPlugins);
        aboutToChangePlugins === null || aboutToChangePlugins === void 0 ? void 0 : aboutToChangePlugins.forEach((plugin) => {
            var _a;
            (_a = plugin === null || plugin === void 0 ? void 0 : plugin.enable) === null || _a === void 0 ? void 0 : _a.call(plugin);
        });
        return this;
    }
    disablePlugins(plugins) {
        let postPlugins = plugins;
        if (!Array.isArray(postPlugins)) {
            postPlugins = [postPlugins];
        }
        const aboutToChangePlugins = this.getPlugins(postPlugins);
        aboutToChangePlugins === null || aboutToChangePlugins === void 0 ? void 0 : aboutToChangePlugins.forEach((plugin) => {
            var _a;
            (_a = plugin === null || plugin === void 0 ? void 0 : plugin.disable) === null || _a === void 0 ? void 0 : _a.call(plugin);
        });
        return this;
    }
    isPluginEnabled(pluginName) {
        var _a;
        const pluginIns = this.getPlugin(pluginName);
        return (_a = pluginIns === null || pluginIns === void 0 ? void 0 : pluginIns.isEnabled) === null || _a === void 0 ? void 0 : _a.call(pluginIns);
    }
    disposePlugins(plugins) {
        let postPlugins = plugins;
        if (!Array.isArray(postPlugins)) {
            postPlugins = [postPlugins];
        }
        const aboutToChangePlugins = this.getPlugins(postPlugins);
        aboutToChangePlugins === null || aboutToChangePlugins === void 0 ? void 0 : aboutToChangePlugins.forEach((plugin) => {
            plugin.dispose();
            this.installedPlugins.delete(plugin);
        });
        return this;
    }
    // #endregion
    // #region dispose
    dispose(clean = true) {
        if (clean) {
            this.model.dispose();
        }
        this.css.dispose();
        this.defs.dispose();
        this.grid.dispose();
        this.coord.dispose();
        this.transform.dispose();
        this.highlight.dispose();
        this.background.dispose();
        this.mousewheel.dispose();
        this.panning.dispose();
        this.view.dispose();
        this.renderer.dispose();
        this.installedPlugins.forEach((plugin) => {
            plugin.dispose();
        });
    }
}
__decorate([
    x6_common_1.Basecoat.dispose()
], Graph.prototype, "dispose", null);
exports.Graph = Graph;
(function (Graph) {
    /* eslint-disable @typescript-eslint/no-unused-vars */
    Graph.View = view_2.GraphView;
    Graph.Renderer = renderer_1.Renderer;
    Graph.MouseWheel = mousewheel_1.MouseWheel;
    Graph.DefsManager = defs_1.DefsManager;
    Graph.GridManager = grid_1.GridManager;
    Graph.CoordManager = coord_1.CoordManager;
    Graph.TransformManager = transform_1.TransformManager;
    Graph.HighlightManager = highlight_1.HighlightManager;
    Graph.BackgroundManager = background_1.BackgroundManager;
    Graph.PanningManager = panning_1.PanningManager;
})(Graph = exports.Graph || (exports.Graph = {}));
(function (Graph) {
    Graph.toStringTag = `X6.${Graph.name}`;
    function isGraph(instance) {
        if (instance == null) {
            return false;
        }
        if (instance instanceof Graph) {
            return true;
        }
        const tag = instance[Symbol.toStringTag];
        if (tag == null || tag === Graph.toStringTag) {
            return true;
        }
        return false;
    }
    Graph.isGraph = isGraph;
})(Graph = exports.Graph || (exports.Graph = {}));
(function (Graph) {
    function render(options, data) {
        const graph = options instanceof HTMLElement
            ? new Graph({ container: options })
            : new Graph(options);
        if (data != null) {
            graph.fromJSON(data);
        }
        return graph;
    }
    Graph.render = render;
})(Graph = exports.Graph || (exports.Graph = {}));
(function (Graph) {
    Graph.registerNode = model_1.Node.registry.register;
    Graph.registerEdge = model_1.Edge.registry.register;
    Graph.registerView = view_1.CellView.registry.register;
    Graph.registerAttr = Registry.Attr.registry.register;
    Graph.registerGrid = Registry.Grid.registry.register;
    Graph.registerFilter = Registry.Filter.registry.register;
    Graph.registerNodeTool = Registry.NodeTool.registry.register;
    Graph.registerEdgeTool = Registry.EdgeTool.registry.register;
    Graph.registerBackground = Registry.Background.registry.register;
    Graph.registerHighlighter = Registry.Highlighter.registry.register;
    Graph.registerPortLayout = Registry.PortLayout.registry.register;
    Graph.registerPortLabelLayout = Registry.PortLabelLayout.registry.register;
    Graph.registerMarker = Registry.Marker.registry.register;
    Graph.registerRouter = Registry.Router.registry.register;
    Graph.registerConnector = Registry.Connector.registry.register;
    Graph.registerAnchor = Registry.NodeAnchor.registry.register;
    Graph.registerEdgeAnchor = Registry.EdgeAnchor.registry.register;
    Graph.registerConnectionPoint = Registry.ConnectionPoint.registry.register;
})(Graph = exports.Graph || (exports.Graph = {}));
(function (Graph) {
    Graph.unregisterNode = model_1.Node.registry.unregister;
    Graph.unregisterEdge = model_1.Edge.registry.unregister;
    Graph.unregisterView = view_1.CellView.registry.unregister;
    Graph.unregisterAttr = Registry.Attr.registry.unregister;
    Graph.unregisterGrid = Registry.Grid.registry.unregister;
    Graph.unregisterFilter = Registry.Filter.registry.unregister;
    Graph.unregisterNodeTool = Registry.NodeTool.registry.unregister;
    Graph.unregisterEdgeTool = Registry.EdgeTool.registry.unregister;
    Graph.unregisterBackground = Registry.Background.registry.unregister;
    Graph.unregisterHighlighter = Registry.Highlighter.registry.unregister;
    Graph.unregisterPortLayout = Registry.PortLayout.registry.unregister;
    Graph.unregisterPortLabelLayout = Registry.PortLabelLayout.registry.unregister;
    Graph.unregisterMarker = Registry.Marker.registry.unregister;
    Graph.unregisterRouter = Registry.Router.registry.unregister;
    Graph.unregisterConnector = Registry.Connector.registry.unregister;
    Graph.unregisterAnchor = Registry.NodeAnchor.registry.unregister;
    Graph.unregisterEdgeAnchor = Registry.EdgeAnchor.registry.unregister;
    Graph.unregisterConnectionPoint = Registry.ConnectionPoint.registry.unregister;
})(Graph = exports.Graph || (exports.Graph = {}));
//# sourceMappingURL=graph.js.map