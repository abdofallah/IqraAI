"use strict";
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
exports.TextBlock = void 0;
const x6_common_1 = require("@antv/x6-common");
const registry_1 = require("../registry");
const base_1 = require("./base");
exports.TextBlock = base_1.Base.define({
    shape: 'text-block',
    markup: [
        {
            tagName: 'rect',
            selector: 'body',
        },
        x6_common_1.Platform.SUPPORT_FOREIGNOBJECT
            ? {
                tagName: 'foreignObject',
                selector: 'foreignObject',
                children: [
                    {
                        tagName: 'div',
                        ns: x6_common_1.Dom.ns.xhtml,
                        selector: 'label',
                        style: {
                            width: '100%',
                            height: '100%',
                            position: 'static',
                            backgroundColor: 'transparent',
                            textAlign: 'center',
                            margin: 0,
                            padding: '0px 5px',
                            boxSizing: 'border-box',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                        },
                    },
                ],
            }
            : {
                tagName: 'text',
                selector: 'label',
                attrs: {
                    textAnchor: 'middle',
                },
            },
    ],
    attrs: {
        body: Object.assign(Object.assign({}, base_1.Base.bodyAttr), { refWidth: '100%', refHeight: '100%' }),
        foreignObject: {
            refWidth: '100%',
            refHeight: '100%',
        },
        label: {
            style: {
                fontSize: 14,
            },
        },
    },
    propHooks(metadata) {
        const { text } = metadata, others = __rest(metadata, ["text"]);
        if (text) {
            x6_common_1.ObjectExt.setByPath(others, 'attrs/label/text', text);
        }
        return others;
    },
    attrHooks: {
        text: {
            set(text, { cell, view, refBBox, elem, attrs }) {
                if (elem instanceof HTMLElement) {
                    elem.textContent = text;
                }
                else {
                    // No foreign object
                    const style = attrs.style || {};
                    const wrapValue = { text, width: -5, height: '100%' };
                    const wrapAttrs = Object.assign({ textVerticalAnchor: 'middle' }, style);
                    const textWrap = registry_1.Attr.presets.textWrap;
                    x6_common_1.FunctionExt.call(textWrap.set, this, wrapValue, {
                        cell,
                        view,
                        elem,
                        refBBox,
                        attrs: wrapAttrs,
                    });
                    return { fill: style.color || null };
                }
            },
            position(text, { refBBox, elem }) {
                if (elem instanceof SVGElement) {
                    return refBBox.getCenter();
                }
            },
        },
    },
});
//# sourceMappingURL=text-block.js.map