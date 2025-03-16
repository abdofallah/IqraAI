"use strict";
/* eslint-disable @typescript-eslint/ban-types */
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
exports.Background = void 0;
const registry_1 = require("../registry");
const patterns = __importStar(require("./main"));
var Background;
(function (Background) {
    Background.presets = Object.assign({}, patterns);
    Background.presets['flip-x'] = patterns.flipX;
    Background.presets['flip-y'] = patterns.flipY;
    Background.presets['flip-xy'] = patterns.flipXY;
    Background.registry = registry_1.Registry.create({
        type: 'background pattern',
    });
    Background.registry.register(Background.presets, true);
})(Background = exports.Background || (exports.Background = {}));
//# sourceMappingURL=index.js.map