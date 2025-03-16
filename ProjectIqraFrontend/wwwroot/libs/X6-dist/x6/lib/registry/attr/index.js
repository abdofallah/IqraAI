"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Attr = void 0;
const x6_common_1 = require("@antv/x6-common");
const registry_1 = require("../registry");
const raw_1 = require("./raw");
const attrs = __importStar(require("./main"));
var Attr;
(function (Attr) {
    function isValidDefinition(def, val, options) {
        if (def != null) {
            if (typeof def === 'string') {
                return true;
            }
            if (typeof def.qualify !== 'function' ||
                x6_common_1.FunctionExt.call(def.qualify, this, val, options)) {
                return true;
            }
        }
        return false;
    }
    Attr.isValidDefinition = isValidDefinition;
})(Attr = exports.Attr || (exports.Attr = {}));
(function (Attr) {
    Attr.presets = Object.assign(Object.assign({}, raw_1.raw), attrs);
    Attr.registry = registry_1.Registry.create({
        type: 'attribute definition',
    });
    Attr.registry.register(Attr.presets, true);
})(Attr = exports.Attr || (exports.Attr = {}));
//# sourceMappingURL=index.js.map