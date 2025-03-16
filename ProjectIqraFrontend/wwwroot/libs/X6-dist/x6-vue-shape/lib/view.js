"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.VueShapeView = void 0;
const x6_1 = require("@antv/x6");
const vue_demi_1 = require("vue-demi");
const registry_1 = require("./registry");
const teleport_1 = require("./teleport");
class VueShapeView extends x6_1.NodeView {
    getComponentContainer() {
        return this.selectors && this.selectors.foContent;
    }
    confirmUpdate(flag) {
        const ret = super.confirmUpdate(flag);
        return this.handleAction(ret, VueShapeView.action, () => {
            this.renderVueComponent();
        });
    }
    targetId() {
        return `${this.graph.view.cid}:${this.cell.id}`;
    }
    renderVueComponent() {
        this.unmountVueComponent();
        const root = this.getComponentContainer();
        const node = this.cell;
        const graph = this.graph;
        if (root) {
            const { component } = registry_1.shapeMaps[node.shape];
            if (component) {
                if (vue_demi_1.isVue2) {
                    const Vue = vue_demi_1.Vue2;
                    this.vm = new Vue({
                        el: root,
                        render(h) {
                            return h(component, { node, graph });
                        },
                        provide() {
                            return {
                                getNode: () => node,
                                getGraph: () => graph,
                            };
                        },
                    });
                }
                else if (vue_demi_1.isVue3) {
                    if ((0, teleport_1.isActive)()) {
                        (0, teleport_1.connect)(this.targetId(), component, root, node, graph);
                    }
                    else {
                        this.vm = (0, vue_demi_1.createApp)({
                            render() {
                                return (0, vue_demi_1.h)(component, { node, graph });
                            },
                            provide() {
                                return {
                                    getNode: () => node,
                                    getGraph: () => graph,
                                };
                            },
                        });
                        this.vm.mount(root);
                    }
                }
            }
        }
    }
    unmountVueComponent() {
        const root = this.getComponentContainer();
        if (this.vm) {
            vue_demi_1.isVue2 && this.vm.$destroy();
            vue_demi_1.isVue3 && this.vm.unmount();
            this.vm = null;
        }
        if (root) {
            root.innerHTML = '';
        }
        return root;
    }
    onMouseDown(e, x, y) {
        const target = e.target;
        const tagName = target.tagName.toLowerCase();
        if (tagName === 'input') {
            const type = target.getAttribute('type');
            if (type == null ||
                [
                    'text',
                    'password',
                    'number',
                    'email',
                    'search',
                    'tel',
                    'url',
                ].includes(type)) {
                return;
            }
        }
        super.onMouseDown(e, x, y);
    }
    unmount() {
        if ((0, teleport_1.isActive)()) {
            (0, teleport_1.disconnect)(this.targetId());
        }
        this.unmountVueComponent();
        super.unmount();
        return this;
    }
}
exports.VueShapeView = VueShapeView;
(function (VueShapeView) {
    VueShapeView.action = 'vue';
    VueShapeView.config({
        bootstrap: [VueShapeView.action],
        actions: {
            component: VueShapeView.action,
        },
    });
    x6_1.NodeView.registry.register('vue-shape-view', VueShapeView, true);
})(VueShapeView || (exports.VueShapeView = VueShapeView = {}));
//# sourceMappingURL=view.js.map