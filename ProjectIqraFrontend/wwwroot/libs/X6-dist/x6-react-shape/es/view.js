import React from 'react';
import { createPortal } from 'react-dom';
import { createRoot } from 'react-dom/client';
import { NodeView } from '@antv/x6';
import { Portal } from './portal';
import { Wrap } from './wrap';
export class ReactShapeView extends NodeView {
    targetId() {
        return `${this.graph.view.cid}:${this.cell.id}`;
    }
    getComponentContainer() {
        return this.selectors && this.selectors.foContent;
    }
    confirmUpdate(flag) {
        const ret = super.confirmUpdate(flag);
        return this.handleAction(ret, ReactShapeView.action, () => {
            this.renderReactComponent();
        });
    }
    renderReactComponent() {
        this.unmountReactComponent();
        const container = this.getComponentContainer();
        const node = this.cell;
        if (container) {
            const elem = React.createElement(Wrap, { node, graph: this.graph });
            if (Portal.isActive()) {
                const portal = createPortal(elem, container, node.id);
                Portal.connect(this.targetId(), portal);
            }
            else {
                this.root = createRoot(container);
                this.root.render(elem);
            }
        }
    }
    unmountReactComponent() {
        const container = this.getComponentContainer();
        if (container && this.root) {
            this.root.unmount();
            this.root = undefined;
        }
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
        if (Portal.isActive()) {
            Portal.disconnect(this.targetId());
        }
        this.unmountReactComponent();
        super.unmount();
        return this;
    }
}
(function (ReactShapeView) {
    ReactShapeView.action = 'react';
    ReactShapeView.config({
        bootstrap: [ReactShapeView.action],
        actions: {
            component: ReactShapeView.action,
        },
    });
    NodeView.registry.register('react-shape-view', ReactShapeView, true);
})(ReactShapeView || (ReactShapeView = {}));
//# sourceMappingURL=view.js.map