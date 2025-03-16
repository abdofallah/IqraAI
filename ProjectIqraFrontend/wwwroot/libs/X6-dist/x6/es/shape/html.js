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
import { Dom } from '@antv/x6-common';
import { Markup } from '../view';
import { Node } from '../model/node';
import { NodeView } from '../view/node';
import { Graph } from '../graph/graph';
export class HTML extends Node {
}
(function (HTML) {
    class View extends NodeView {
        init() {
            super.init();
            this.cell.on('change:*', this.onCellChangeAny, this);
        }
        onCellChangeAny({ key }) {
            const content = HTML.shapeMaps[this.cell.shape];
            if (content) {
                const { effect } = content;
                if (!effect || effect.includes(key)) {
                    this.renderHTMLComponent();
                }
            }
        }
        confirmUpdate(flag) {
            const ret = super.confirmUpdate(flag);
            return this.handleAction(ret, View.action, () => this.renderHTMLComponent());
        }
        renderHTMLComponent() {
            const container = this.selectors && this.selectors.foContent;
            if (container) {
                Dom.empty(container);
                const content = HTML.shapeMaps[this.cell.shape];
                if (!content) {
                    return;
                }
                let { html } = content;
                if (typeof html === 'function') {
                    html = html(this.cell);
                }
                if (html) {
                    if (typeof html === 'string') {
                        container.innerHTML = html;
                    }
                    else {
                        Dom.append(container, html);
                    }
                }
            }
        }
        dispose() {
            this.cell.off('change:*', this.onCellChangeAny, this);
        }
    }
    __decorate([
        View.dispose()
    ], View.prototype, "dispose", null);
    HTML.View = View;
    (function (View) {
        View.action = 'html';
        View.config({
            bootstrap: [View.action],
            actions: {
                html: View.action,
            },
        });
        NodeView.registry.register('html-view', View, true);
    })(View = HTML.View || (HTML.View = {}));
})(HTML || (HTML = {}));
(function (HTML) {
    HTML.config({
        view: 'html-view',
        markup: [
            {
                tagName: 'rect',
                selector: 'body',
            },
            Object.assign({}, Markup.getForeignObjectMarkup()),
            {
                tagName: 'text',
                selector: 'label',
            },
        ],
        attrs: {
            body: {
                fill: 'none',
                stroke: 'none',
                refWidth: '100%',
                refHeight: '100%',
            },
            fo: {
                refWidth: '100%',
                refHeight: '100%',
            },
        },
    });
    Node.registry.register('html', HTML, true);
})(HTML || (HTML = {}));
(function (HTML) {
    HTML.shapeMaps = {};
    function register(config) {
        const { shape, html, effect, inherit } = config, others = __rest(config, ["shape", "html", "effect", "inherit"]);
        if (!shape) {
            throw new Error('should specify shape in config');
        }
        HTML.shapeMaps[shape] = {
            html,
            effect,
        };
        Graph.registerNode(shape, Object.assign({ inherit: inherit || 'html' }, others), true);
    }
    HTML.register = register;
})(HTML || (HTML = {}));
//# sourceMappingURL=html.js.map