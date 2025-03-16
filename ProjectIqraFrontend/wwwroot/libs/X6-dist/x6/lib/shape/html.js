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
exports.HTML = void 0;
const x6_common_1 = require("@antv/x6-common");
const view_1 = require("../view");
const node_1 = require("../model/node");
const node_2 = require("../view/node");
const graph_1 = require("../graph/graph");
class HTML extends node_1.Node {
}
exports.HTML = HTML;
(function (HTML) {
    class View extends node_2.NodeView {
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
                x6_common_1.Dom.empty(container);
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
                        x6_common_1.Dom.append(container, html);
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
        node_2.NodeView.registry.register('html-view', View, true);
    })(View = HTML.View || (HTML.View = {}));
})(HTML = exports.HTML || (exports.HTML = {}));
(function (HTML) {
    HTML.config({
        view: 'html-view',
        markup: [
            {
                tagName: 'rect',
                selector: 'body',
            },
            Object.assign({}, view_1.Markup.getForeignObjectMarkup()),
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
    node_1.Node.registry.register('html', HTML, true);
})(HTML = exports.HTML || (exports.HTML = {}));
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
        graph_1.Graph.registerNode(shape, Object.assign({ inherit: inherit || 'html' }, others), true);
    }
    HTML.register = register;
})(HTML = exports.HTML || (exports.HTML = {}));
//# sourceMappingURL=html.js.map