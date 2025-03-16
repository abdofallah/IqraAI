"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Stencil = void 0;
const x6_1 = require("@antv/x6");
const x6_plugin_dnd_1 = require("@antv/x6-plugin-dnd");
const grid_1 = require("./grid");
const raw_1 = require("./style/raw");
class Stencil extends x6_1.View {
    get targetScroller() {
        const target = this.options.target;
        const scroller = target.getPlugin('scroller');
        return scroller;
    }
    get targetGraph() {
        return this.options.target;
    }
    get targetModel() {
        return this.targetGraph.model;
    }
    constructor(options = {}) {
        super();
        this.name = 'stencil';
        x6_1.CssLoader.ensure(this.name, raw_1.content);
        this.graphs = {};
        this.groups = {};
        this.options = Object.assign(Object.assign({}, Stencil.defaultOptions), options);
        this.init();
    }
    init() {
        this.dnd = new x6_plugin_dnd_1.Dnd(this.options);
        this.onSearch = x6_1.FunctionExt.debounce(this.onSearch, 200);
        this.initContainer();
        this.initSearch();
        this.initContent();
        this.initGroups();
        this.setTitle();
        this.startListening();
    }
    load(data, groupName) {
        if (Array.isArray(data)) {
            this.loadGroup(data, groupName);
        }
        else if (this.options.groups) {
            Object.keys(this.options.groups).forEach((groupName) => {
                if (data[groupName]) {
                    this.loadGroup(data[groupName], groupName);
                }
            });
        }
        return this;
    }
    unload(data, groupName) {
        if (Array.isArray(data)) {
            this.loadGroup(data, groupName, true);
        }
        else if (this.options.groups) {
            Object.keys(this.options.groups).forEach((groupName) => {
                if (data[groupName]) {
                    this.loadGroup(data[groupName], groupName, true);
                }
            });
        }
        return this;
    }
    toggleGroup(groupName) {
        if (this.isGroupCollapsed(groupName)) {
            this.expandGroup(groupName);
        }
        else {
            this.collapseGroup(groupName);
        }
        return this;
    }
    collapseGroup(groupName) {
        if (this.isGroupCollapsable(groupName)) {
            const group = this.groups[groupName];
            if (group && !this.isGroupCollapsed(groupName)) {
                this.trigger('group:collapse', { name: groupName });
                x6_1.Dom.addClass(group, 'collapsed');
            }
        }
        return this;
    }
    expandGroup(groupName) {
        if (this.isGroupCollapsable(groupName)) {
            const group = this.groups[groupName];
            if (group && this.isGroupCollapsed(groupName)) {
                this.trigger('group:expand', { name: groupName });
                x6_1.Dom.removeClass(group, 'collapsed');
            }
        }
        return this;
    }
    isGroupCollapsable(groupName) {
        const group = this.groups[groupName];
        return x6_1.Dom.hasClass(group, 'collapsable');
    }
    isGroupCollapsed(groupName) {
        const group = this.groups[groupName];
        return group && x6_1.Dom.hasClass(group, 'collapsed');
    }
    collapseGroups() {
        Object.keys(this.groups).forEach((groupName) => this.collapseGroup(groupName));
        return this;
    }
    expandGroups() {
        Object.keys(this.groups).forEach((groupName) => this.expandGroup(groupName));
        return this;
    }
    resizeGroup(groupName, size) {
        const graph = this.graphs[groupName];
        if (graph) {
            graph.resize(size.width, size.height);
        }
        return this;
    }
    addGroup(group) {
        const groups = Array.isArray(group) ? group : [group];
        if (this.options.groups) {
            this.options.groups.push(...groups);
        }
        else {
            this.options.groups = groups;
        }
        groups.forEach((group) => this.initGroup(group));
    }
    removeGroup(groupName) {
        const groupNames = Array.isArray(groupName) ? groupName : [groupName];
        if (this.options.groups) {
            this.options.groups = this.options.groups.filter((group) => !groupNames.includes(group.name));
            groupNames.forEach((groupName) => {
                const graph = this.graphs[groupName];
                this.unregisterGraphEvents(graph);
                graph.dispose();
                delete this.graphs[groupName];
                const elem = this.groups[groupName];
                x6_1.Dom.remove(elem);
                delete this.groups[groupName];
            });
        }
    }
    // #endregion
    initContainer() {
        this.container = document.createElement('div');
        x6_1.Dom.addClass(this.container, this.prefixClassName(ClassNames.base));
        x6_1.Dom.attr(this.container, 'data-not-found-text', this.options.notFoundText || 'No matches found');
    }
    initContent() {
        this.content = document.createElement('div');
        x6_1.Dom.addClass(this.content, this.prefixClassName(ClassNames.content));
        x6_1.Dom.appendTo(this.content, this.container);
    }
    initSearch() {
        if (this.options.search) {
            x6_1.Dom.addClass(this.container, 'searchable');
            x6_1.Dom.append(this.container, this.renderSearch());
        }
    }
    initGroup(group) {
        const globalGraphOptions = this.options.stencilGraphOptions || {};
        const groupElem = document.createElement('div');
        x6_1.Dom.addClass(groupElem, this.prefixClassName(ClassNames.group));
        x6_1.Dom.attr(groupElem, 'data-name', group.name);
        if ((group.collapsable == null && this.options.collapsable) ||
            group.collapsable !== false) {
            x6_1.Dom.addClass(groupElem, 'collapsable');
        }
        x6_1.Dom.toggleClass(groupElem, 'collapsed', group.collapsed === true);
        const title = document.createElement('h3');
        x6_1.Dom.addClass(title, this.prefixClassName(ClassNames.groupTitle));
        title.innerHTML = group.title || group.name;
        const content = document.createElement('div');
        x6_1.Dom.addClass(content, this.prefixClassName(ClassNames.groupContent));
        const graphOptionsInGroup = group.graphOptions;
        const graph = new x6_1.Graph(Object.assign(Object.assign(Object.assign({}, globalGraphOptions), graphOptionsInGroup), { container: document.createElement('div'), model: globalGraphOptions.model || new x6_1.Model(), width: group.graphWidth || this.options.stencilGraphWidth, height: group.graphHeight || this.options.stencilGraphHeight, interacting: false, preventDefaultBlankAction: false }));
        this.registerGraphEvents(graph);
        x6_1.Dom.append(content, graph.container);
        x6_1.Dom.append(groupElem, [title, content]);
        x6_1.Dom.appendTo(groupElem, this.content);
        this.groups[group.name] = groupElem;
        this.graphs[group.name] = graph;
    }
    initGroups() {
        this.clearGroups();
        this.setCollapsableState();
        if (this.options.groups && this.options.groups.length) {
            this.options.groups.forEach((group) => {
                this.initGroup(group);
            });
        }
        else {
            const globalGraphOptions = this.options.stencilGraphOptions || {};
            const graph = new x6_1.Graph(Object.assign(Object.assign({}, globalGraphOptions), { container: document.createElement('div'), model: globalGraphOptions.model || new x6_1.Model(), width: this.options.stencilGraphWidth, height: this.options.stencilGraphHeight, interacting: false, preventDefaultBlankAction: false }));
            x6_1.Dom.append(this.content, graph.container);
            this.graphs[Private.defaultGroupName] = graph;
        }
    }
    setCollapsableState() {
        this.options.collapsable =
            this.options.collapsable &&
                this.options.groups &&
                this.options.groups.some((group) => group.collapsable !== false);
        if (this.options.collapsable) {
            x6_1.Dom.addClass(this.container, 'collapsable');
            const collapsed = this.options.groups &&
                this.options.groups.every((group) => group.collapsed || group.collapsable === false);
            if (collapsed) {
                x6_1.Dom.addClass(this.container, 'collapsed');
            }
            else {
                x6_1.Dom.removeClass(this.container, 'collapsed');
            }
        }
        else {
            x6_1.Dom.removeClass(this.container, 'collapsable');
        }
    }
    setTitle() {
        const title = document.createElement('div');
        x6_1.Dom.addClass(title, this.prefixClassName(ClassNames.title));
        title.innerHTML = this.options.title;
        x6_1.Dom.appendTo(title, this.container);
    }
    renderSearch() {
        const elem = document.createElement('div');
        x6_1.Dom.addClass(elem, this.prefixClassName(ClassNames.search));
        const input = document.createElement('input');
        x6_1.Dom.attr(input, {
            type: 'search',
            placeholder: this.options.placeholder || 'Search',
        });
        x6_1.Dom.addClass(input, this.prefixClassName(ClassNames.searchText));
        x6_1.Dom.append(elem, input);
        return elem;
    }
    startListening() {
        const title = this.prefixClassName(ClassNames.title);
        const searchText = this.prefixClassName(ClassNames.searchText);
        const groupTitle = this.prefixClassName(ClassNames.groupTitle);
        this.delegateEvents({
            [`click .${title}`]: 'onTitleClick',
            [`touchstart .${title}`]: 'onTitleClick',
            [`click .${groupTitle}`]: 'onGroupTitleClick',
            [`touchstart .${groupTitle}`]: 'onGroupTitleClick',
            [`input .${searchText}`]: 'onSearch',
            [`focusin .${searchText}`]: 'onSearchFocusIn',
            [`focusout .${searchText}`]: 'onSearchFocusOut',
        });
    }
    stopListening() {
        this.undelegateEvents();
    }
    registerGraphEvents(graph) {
        graph.on('cell:mousedown', this.onDragStart, this);
    }
    unregisterGraphEvents(graph) {
        graph.off('cell:mousedown', this.onDragStart, this);
    }
    loadGroup(cells, groupName, reverse) {
        const model = this.getModel(groupName);
        if (model) {
            const nodes = cells.map((cell) => x6_1.Node.isNode(cell) ? cell : x6_1.Node.create(cell));
            if (reverse === true) {
                model.removeCells(nodes);
            }
            else {
                model.resetCells(nodes);
            }
        }
        const group = this.getGroup(groupName);
        let height = this.options.stencilGraphHeight;
        if (group && group.graphHeight != null) {
            height = group.graphHeight;
        }
        const layout = (group && group.layout) || this.options.layout;
        if (layout && model) {
            x6_1.FunctionExt.call(layout, this, model, group);
        }
        if (!height) {
            const graph = this.getGraph(groupName);
            graph.fitToContent({
                minWidth: graph.options.width,
                gridHeight: 1,
                padding: (group && group.graphPadding) ||
                    this.options.stencilGraphPadding ||
                    10,
            });
        }
        return this;
    }
    onDragStart(args) {
        const { e, node } = args;
        const group = this.getGroupByNode(node);
        if (group && group.nodeMovable === false) {
            return;
        }
        this.dnd.start(node, e);
    }
    filter(keyword, filter) {
        const found = Object.keys(this.graphs).reduce((memo, groupName) => {
            const graph = this.graphs[groupName];
            const name = groupName === Private.defaultGroupName ? null : groupName;
            const items = graph.model.getNodes().filter((cell) => {
                let matched = false;
                if (typeof filter === 'function') {
                    matched = x6_1.FunctionExt.call(filter, this, cell, keyword, name, this);
                }
                else if (typeof filter === 'boolean') {
                    matched = filter;
                }
                else {
                    matched = this.isCellMatched(cell, keyword, filter, keyword.toLowerCase() !== keyword);
                }
                const view = graph.renderer.findViewByCell(cell);
                if (view) {
                    x6_1.Dom.toggleClass(view.container, 'unmatched', !matched);
                }
                return matched;
            });
            const found = items.length > 0;
            const options = this.options;
            const model = new x6_1.Model();
            model.resetCells(items);
            if (options.layout) {
                x6_1.FunctionExt.call(options.layout, this, model, this.getGroup(groupName));
            }
            if (this.groups[groupName]) {
                x6_1.Dom.toggleClass(this.groups[groupName], 'unmatched', !found);
            }
            graph.fitToContent({
                gridWidth: 1,
                gridHeight: 1,
                padding: options.stencilGraphPadding || 10,
            });
            return memo || found;
        }, false);
        x6_1.Dom.toggleClass(this.container, 'not-found', !found);
    }
    isCellMatched(cell, keyword, filters, ignoreCase) {
        if (keyword && filters) {
            return Object.keys(filters).some((shape) => {
                if (shape === '*' || cell.shape === shape) {
                    const filter = filters[shape];
                    if (typeof filter === 'boolean') {
                        return filter;
                    }
                    const paths = Array.isArray(filter) ? filter : [filter];
                    return paths.some((path) => {
                        let val = cell.getPropByPath(path);
                        if (val != null) {
                            val = `${val}`;
                            if (!ignoreCase) {
                                val = val.toLowerCase();
                            }
                            return val.indexOf(keyword) >= 0;
                        }
                        return false;
                    });
                }
                return false;
            });
        }
        return true;
    }
    onSearch(evt) {
        this.filter(evt.target.value, this.options.search);
    }
    onSearchFocusIn() {
        x6_1.Dom.addClass(this.container, 'is-focused');
    }
    onSearchFocusOut() {
        x6_1.Dom.removeClass(this.container, 'is-focused');
    }
    onTitleClick() {
        if (this.options.collapsable) {
            x6_1.Dom.toggleClass(this.container, 'collapsed');
            if (x6_1.Dom.hasClass(this.container, 'collapsed')) {
                this.collapseGroups();
            }
            else {
                this.expandGroups();
            }
        }
    }
    onGroupTitleClick(evt) {
        const group = evt.target.closest(`.${this.prefixClassName(ClassNames.group)}`);
        if (group) {
            this.toggleGroup(x6_1.Dom.attr(group, 'data-name') || '');
        }
        const allCollapsed = Object.keys(this.groups).every((name) => {
            const group = this.getGroup(name);
            const groupElem = this.groups[name];
            return ((group && group.collapsable === false) ||
                x6_1.Dom.hasClass(groupElem, 'collapsed'));
        });
        x6_1.Dom.toggleClass(this.container, 'collapsed', allCollapsed);
    }
    getModel(groupName) {
        const graph = this.getGraph(groupName);
        return graph ? graph.model : null;
    }
    getGraph(groupName) {
        return this.graphs[groupName || Private.defaultGroupName];
    }
    getGroup(groupName) {
        const groups = this.options.groups;
        if (groupName != null && groups && groups.length) {
            return groups.find((group) => group.name === groupName);
        }
        return null;
    }
    getGroupByNode(node) {
        const groups = this.options.groups;
        if (groups) {
            return groups.find((group) => {
                const model = this.getModel(group.name);
                if (model) {
                    return model.has(node.id);
                }
                return false;
            });
        }
        return null;
    }
    clearGroups() {
        Object.keys(this.graphs).forEach((groupName) => {
            const graph = this.graphs[groupName];
            this.unregisterGraphEvents(graph);
            graph.dispose();
        });
        Object.keys(this.groups).forEach((groupName) => {
            const elem = this.groups[groupName];
            x6_1.Dom.remove(elem);
        });
        this.graphs = {};
        this.groups = {};
    }
    onRemove() {
        this.clearGroups();
        this.dnd.remove();
        this.stopListening();
        this.undelegateDocumentEvents();
    }
    dispose() {
        this.remove();
        x6_1.CssLoader.clean(this.name);
    }
}
__decorate([
    x6_1.View.dispose()
], Stencil.prototype, "dispose", null);
exports.Stencil = Stencil;
(function (Stencil) {
    Stencil.defaultOptions = Object.assign({ stencilGraphWidth: 200, stencilGraphHeight: 800, title: 'Stencil', collapsable: false, placeholder: 'Search', notFoundText: 'No matches found', layout(model, group) {
            const options = {
                columnWidth: this.options.stencilGraphWidth / 2 - 10,
                columns: 2,
                rowHeight: 80,
                resizeToFit: false,
                dx: 10,
                dy: 10,
            };
            (0, grid_1.grid)(model, Object.assign(Object.assign(Object.assign({}, options), this.options.layoutOptions), (group ? group.layoutOptions : {})));
        } }, x6_plugin_dnd_1.Dnd.defaults);
})(Stencil = exports.Stencil || (exports.Stencil = {}));
var ClassNames;
(function (ClassNames) {
    ClassNames.base = 'widget-stencil';
    ClassNames.title = `${ClassNames.base}-title`;
    ClassNames.search = `${ClassNames.base}-search`;
    ClassNames.searchText = `${ClassNames.search}-text`;
    ClassNames.content = `${ClassNames.base}-content`;
    ClassNames.group = `${ClassNames.base}-group`;
    ClassNames.groupTitle = `${ClassNames.group}-title`;
    ClassNames.groupContent = `${ClassNames.group}-content`;
})(ClassNames || (ClassNames = {}));
var Private;
(function (Private) {
    Private.defaultGroupName = '__default__';
})(Private || (Private = {}));
//# sourceMappingURL=index.js.map