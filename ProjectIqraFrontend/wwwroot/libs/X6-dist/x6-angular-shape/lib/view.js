"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.AngularShapeView = void 0;
const core_1 = require("@angular/core");
const x6_1 = require("@antv/x6");
const registry_1 = require("./registry");
class AngularShapeView extends x6_1.NodeView {
    getNodeContainer() {
        return this.selectors && this.selectors.foContent;
    }
    confirmUpdate(flag) {
        const ret = super.confirmUpdate(flag);
        return this.handleAction(ret, AngularShapeView.action, () => this.renderAngularContent());
    }
    getNgArguments() {
        var _a;
        const input = ((_a = this.cell.data) === null || _a === void 0 ? void 0 : _a.ngArguments) || {};
        return input;
    }
    /** 当执行 node.setData() 时需要对实例设置新的输入值 */
    setInstanceInput(content, ref) {
        const ngArguments = this.getNgArguments();
        if (content instanceof core_1.TemplateRef) {
            const embeddedViewRef = ref;
            embeddedViewRef.context = { ngArguments };
        }
        else {
            const componentRef = ref;
            Object.keys(ngArguments).forEach((v) => componentRef.setInput(v, ngArguments[v]));
            componentRef.changeDetectorRef.detectChanges();
        }
    }
    renderAngularContent() {
        const container = this.getNodeContainer();
        if (container) {
            this.unmountAngularContent();
            const node = this.cell;
            const { injector, content } = registry_1.registerInfo.get(node.shape);
            const viewContainerRef = injector.get(core_1.ViewContainerRef);
            if (content instanceof core_1.TemplateRef) {
                const ngArguments = this.getNgArguments();
                const embeddedViewRef = viewContainerRef.createEmbeddedView(content, {
                    ngArguments,
                });
                embeddedViewRef.rootNodes.forEach((node) => container.appendChild(node));
                embeddedViewRef.detectChanges();
                node.on('change:data', () => this.setInstanceInput(content, embeddedViewRef));
            }
            else {
                const componentRef = viewContainerRef.createComponent(content, {
                    injector,
                });
                const insertNode = componentRef.hostView
                    .rootNodes[0];
                container.appendChild(insertNode);
                this.setInstanceInput(content, componentRef);
                node.on('change:data', () => this.setInstanceInput(content, componentRef));
                node.on('removed', () => componentRef.destroy());
            }
        }
    }
    unmountAngularContent() {
        const container = this.getNodeContainer();
        container.innerHTML = '';
        return container;
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
        this.unmountAngularContent();
        super.unmount();
        return this;
    }
}
exports.AngularShapeView = AngularShapeView;
(function (AngularShapeView) {
    AngularShapeView.action = 'angular';
    AngularShapeView.config({
        bootstrap: [AngularShapeView.action],
        actions: {
            component: AngularShapeView.action,
        },
    });
    x6_1.NodeView.registry.register('angular-shape-view', AngularShapeView, true);
})(AngularShapeView = exports.AngularShapeView || (exports.AngularShapeView = {}));
//# sourceMappingURL=view.js.map