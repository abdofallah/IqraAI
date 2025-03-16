import { NodeView } from '@antv/x6';
import { isVue2, isVue3, createApp, h, Vue2 } from 'vue-demi';
import { shapeMaps } from './registry';
import { isActive, connect, disconnect } from './teleport';
export class VueShapeView extends NodeView {
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
            const { component } = shapeMaps[node.shape];
            if (component) {
                if (isVue2) {
                    const Vue = Vue2;
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
                else if (isVue3) {
                    if (isActive()) {
                        connect(this.targetId(), component, root, node, graph);
                    }
                    else {
                        this.vm = createApp({
                            render() {
                                return h(component, { node, graph });
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
            isVue2 && this.vm.$destroy();
            isVue3 && this.vm.unmount();
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
        if (isActive()) {
            disconnect(this.targetId());
        }
        this.unmountVueComponent();
        super.unmount();
        return this;
    }
}
(function (VueShapeView) {
    VueShapeView.action = 'vue';
    VueShapeView.config({
        bootstrap: [VueShapeView.action],
        actions: {
            component: VueShapeView.action,
        },
    });
    NodeView.registry.register('vue-shape-view', VueShapeView, true);
})(VueShapeView || (VueShapeView = {}));
//# sourceMappingURL=view.js.map