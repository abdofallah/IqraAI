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
exports.createShape = exports.getImageUrlHook = exports.getMarkup = void 0;
const x6_common_1 = require("@antv/x6-common");
const base_1 = require("./base");
function getMarkup(tagName, selector = 'body') {
    return [
        {
            tagName,
            selector,
        },
        {
            tagName: 'text',
            selector: 'label',
        },
    ];
}
exports.getMarkup = getMarkup;
function getImageUrlHook(attrName = 'xlink:href') {
    const hook = (metadata) => {
        const { imageUrl, imageWidth, imageHeight } = metadata, others = __rest(metadata, ["imageUrl", "imageWidth", "imageHeight"]);
        if (imageUrl != null || imageWidth != null || imageHeight != null) {
            const apply = () => {
                if (others.attrs) {
                    const image = others.attrs.image;
                    if (imageUrl != null) {
                        image[attrName] = imageUrl;
                    }
                    if (imageWidth != null) {
                        image.width = imageWidth;
                    }
                    if (imageHeight != null) {
                        image.height = imageHeight;
                    }
                    others.attrs.image = image;
                }
            };
            if (others.attrs) {
                if (others.attrs.image == null) {
                    others.attrs.image = {};
                }
                apply();
            }
            else {
                others.attrs = {
                    image: {},
                };
                apply();
            }
        }
        return others;
    };
    return hook;
}
exports.getImageUrlHook = getImageUrlHook;
function createShape(shape, config, options = {}) {
    const defaults = {
        constructorName: shape,
        markup: getMarkup(shape, options.selector),
        attrs: {
            [shape]: Object.assign({}, base_1.Base.bodyAttr),
        },
    };
    const base = options.parent || base_1.Base;
    return base.define(x6_common_1.ObjectExt.merge(defaults, config, { shape }));
}
exports.createShape = createShape;
//# sourceMappingURL=util.js.map