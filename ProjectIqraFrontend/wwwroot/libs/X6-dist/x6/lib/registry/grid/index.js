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
exports.Grid = void 0;
const x6_common_1 = require("@antv/x6-common");
const registry_1 = require("../registry");
const patterns = __importStar(require("./main"));
class Grid {
    constructor() {
        this.patterns = {};
        this.root = x6_common_1.Vector.create(x6_common_1.Dom.createSvgDocument(), {
            width: '100%',
            height: '100%',
        }, [x6_common_1.Dom.createSvgElement('defs')]).node;
    }
    add(id, elem) {
        const firstChild = this.root.childNodes[0];
        if (firstChild) {
            firstChild.appendChild(elem);
        }
        this.patterns[id] = elem;
        x6_common_1.Vector.create('rect', {
            width: '100%',
            height: '100%',
            fill: `url(#${id})`,
        }).appendTo(this.root);
    }
    get(id) {
        return this.patterns[id];
    }
    has(id) {
        return this.patterns[id] != null;
    }
}
exports.Grid = Grid;
(function (Grid) {
    Grid.presets = patterns;
    Grid.registry = registry_1.Registry.create({
        type: 'grid',
    });
    Grid.registry.register(Grid.presets, true);
})(Grid = exports.Grid || (exports.Grid = {}));
//# sourceMappingURL=index.js.map