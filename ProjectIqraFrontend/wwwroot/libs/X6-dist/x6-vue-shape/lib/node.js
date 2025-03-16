"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.VueShape = void 0;
const x6_1 = require("@antv/x6");
class VueShape extends x6_1.Node {
}
exports.VueShape = VueShape;
(function (VueShape) {
    function getMarkup(primer) {
        const markup = [];
        const content = x6_1.Markup.getForeignObjectMarkup();
        if (primer) {
            markup.push(...[
                {
                    tagName: primer,
                    selector: 'body',
                },
                content,
            ]);
        }
        else {
            markup.push(content);
        }
        return markup;
    }
    VueShape.config({
        view: 'vue-shape-view',
        markup: getMarkup(),
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
        propHooks(metadata) {
            if (metadata.markup == null) {
                const primer = metadata.primer;
                if (primer) {
                    metadata.markup = getMarkup(primer);
                    let attrs = {};
                    switch (primer) {
                        case 'circle':
                            attrs = {
                                refCx: '50%',
                                refCy: '50%',
                                refR: '50%',
                            };
                            break;
                        case 'ellipse':
                            attrs = {
                                refCx: '50%',
                                refCy: '50%',
                                refRx: '50%',
                                refRy: '50%',
                            };
                            break;
                        default:
                            break;
                    }
                    metadata.attrs = x6_1.ObjectExt.merge({}, {
                        body: Object.assign({ refWidth: null, refHeight: null }, attrs),
                    }, metadata.attrs || {});
                }
            }
            return metadata;
        },
    });
    x6_1.Node.registry.register('vue-shape', VueShape, true);
})(VueShape || (exports.VueShape = VueShape = {}));
//# sourceMappingURL=node.js.map