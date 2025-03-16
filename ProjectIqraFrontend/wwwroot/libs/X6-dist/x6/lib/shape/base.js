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
exports.Base = void 0;
const x6_common_1 = require("@antv/x6-common");
const model_1 = require("../model");
class Base extends model_1.Node {
    get label() {
        return this.getLabel();
    }
    set label(val) {
        this.setLabel(val);
    }
    getLabel() {
        return this.getAttrByPath('text/text');
    }
    setLabel(label, options) {
        if (label == null) {
            this.removeLabel();
        }
        else {
            this.setAttrByPath('text/text', label, options);
        }
        return this;
    }
    removeLabel() {
        this.removeAttrByPath('text/text');
        return this;
    }
}
exports.Base = Base;
(function (Base) {
    Base.bodyAttr = {
        fill: '#ffffff',
        stroke: '#333333',
        strokeWidth: 2,
    };
    Base.labelAttr = {
        fontSize: 14,
        fill: '#000000',
        refX: 0.5,
        refY: 0.5,
        textAnchor: 'middle',
        textVerticalAnchor: 'middle',
        fontFamily: 'Arial, helvetica, sans-serif',
    };
    Base.config({
        attrs: { text: Object.assign({}, Base.labelAttr) },
        propHooks(metadata) {
            const { label } = metadata, others = __rest(metadata, ["label"]);
            if (label) {
                x6_common_1.ObjectExt.setByPath(others, 'attrs/text/text', label);
            }
            return others;
        },
        visible: true,
    });
})(Base = exports.Base || (exports.Base = {}));
//# sourceMappingURL=base.js.map