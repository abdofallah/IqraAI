var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { Basecoat, CssLoader } from '@antv/x6';
import { TransformImpl } from './transform';
import { content } from './style/raw';
import './api';
export class Transform extends Basecoat {
    constructor(options = {}) {
        super();
        this.name = 'transform';
        this.widgets = new Map();
        this.disabled = false;
        this.options = options;
        CssLoader.ensure(this.name, content);
    }
    init(graph) {
        this.graph = graph;
        if (this.disabled) {
            return;
        }
        this.startListening();
    }
    startListening() {
        this.graph.on('node:click', this.onNodeClick, this);
        this.graph.on('blank:mousedown', this.onBlankMouseDown, this);
    }
    stopListening() {
        this.graph.off('node:click', this.onNodeClick, this);
        this.graph.off('blank:mousedown', this.onBlankMouseDown, this);
    }
    enable() {
        if (this.disabled) {
            this.disabled = false;
            this.startListening();
        }
    }
    disable() {
        if (!this.disabled) {
            this.disabled = true;
            this.stopListening();
        }
    }
    isEnabled() {
        return !this.disabled;
    }
    createWidget(node) {
        this.clearWidgets();
        const widget = this.createTransform(node);
        if (widget) {
            this.widgets.set(node, widget);
            widget.on('*', (name, args) => {
                this.trigger(name, args);
                this.graph.trigger(name, args);
            });
        }
    }
    onNodeClick({ node }) {
        this.createWidget(node);
    }
    onBlankMouseDown() {
        this.clearWidgets();
    }
    createTransform(node) {
        const options = this.getTransformOptions(node);
        if (options.resizable || options.rotatable) {
            return new TransformImpl(options, node, this.graph);
        }
        return null;
    }
    getTransformOptions(node) {
        if (!this.options.resizing) {
            this.options.resizing = {
                enabled: false,
            };
        }
        if (!this.options.rotating) {
            this.options.rotating = {
                enabled: false,
            };
        }
        if (typeof this.options.resizing === 'boolean') {
            this.options.resizing = {
                enabled: this.options.resizing,
            };
        }
        if (typeof this.options.rotating === 'boolean') {
            this.options.rotating = {
                enabled: this.options.rotating,
            };
        }
        const resizing = Transform.parseOptionGroup(this.graph, node, this.options.resizing);
        const rotating = Transform.parseOptionGroup(this.graph, node, this.options.rotating);
        const options = {
            resizable: !!resizing.enabled,
            minWidth: resizing.minWidth || 0,
            maxWidth: resizing.maxWidth || Number.MAX_SAFE_INTEGER,
            minHeight: resizing.minHeight || 0,
            maxHeight: resizing.maxHeight || Number.MAX_SAFE_INTEGER,
            orthogonalResizing: typeof resizing.orthogonal === 'boolean' ? resizing.orthogonal : true,
            restrictedResizing: !!resizing.restrict,
            autoScrollOnResizing: typeof resizing.autoScroll === 'boolean' ? resizing.autoScroll : true,
            preserveAspectRatio: !!resizing.preserveAspectRatio,
            allowReverse: typeof resizing.allowReverse === 'boolean'
                ? resizing.allowReverse
                : true,
            rotatable: !!rotating.enabled,
            rotateGrid: rotating.grid || 15,
        };
        return options;
    }
    clearWidgets() {
        this.widgets.forEach((widget, node) => {
            if (this.graph.getCellById(node.id)) {
                widget.dispose();
            }
        });
        this.widgets.clear();
    }
    dispose() {
        this.clearWidgets();
        this.stopListening();
        this.off();
        CssLoader.clean(this.name);
    }
}
__decorate([
    Basecoat.dispose()
], Transform.prototype, "dispose", null);
(function (Transform) {
    function parseOptionGroup(graph, arg, options) {
        const result = {};
        Object.keys(options || {}).forEach((key) => {
            const val = options[key];
            result[key] = typeof val === 'function' ? val.call(graph, arg) : val;
        });
        return result;
    }
    Transform.parseOptionGroup = parseOptionGroup;
})(Transform || (Transform = {}));
//# sourceMappingURL=index.js.map