"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.ReactShapeView = void 0;
const react_1 = __importDefault(require("react"));
const react_dom_1 = require("react-dom");
const client_1 = require("react-dom/client");
const x6_1 = require("@antv/x6");
const portal_1 = require("./portal");
const wrap_1 = require("./wrap");
class ReactShapeView extends x6_1.NodeView {
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
            const elem = react_1.default.createElement(wrap_1.Wrap, { node, graph: this.graph });
            if (portal_1.Portal.isActive()) {
                const portal = (0, react_dom_1.createPortal)(elem, container, node.id);
                portal_1.Portal.connect(this.targetId(), portal);
            }
            else {
                this.root = (0, client_1.createRoot)(container);
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
        if (portal_1.Portal.isActive()) {
            portal_1.Portal.disconnect(this.targetId());
        }
        this.unmountReactComponent();
        super.unmount();
        return this;
    }
}
exports.ReactShapeView = ReactShapeView;
(function (ReactShapeView) {
    ReactShapeView.action = 'react';
    ReactShapeView.config({
        bootstrap: [ReactShapeView.action],
        actions: {
            component: ReactShapeView.action,
        },
    });
    x6_1.NodeView.registry.register('react-shape-view', ReactShapeView, true);
})(ReactShapeView = exports.ReactShapeView || (exports.ReactShapeView = {}));
//# sourceMappingURL=view.js.map