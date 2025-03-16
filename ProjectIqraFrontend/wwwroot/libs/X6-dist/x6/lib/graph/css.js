"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.CSSManager = void 0;
const x6_common_1 = require("@antv/x6-common");
const config_1 = require("../config");
const raw_1 = require("../style/raw");
const base_1 = require("./base");
class CSSManager extends base_1.Base {
    init() {
        if (config_1.Config.autoInsertCSS) {
            x6_common_1.CssLoader.ensure('core', raw_1.content);
        }
    }
    dispose() {
        x6_common_1.CssLoader.clean('core');
    }
}
__decorate([
    CSSManager.dispose()
], CSSManager.prototype, "dispose", null);
exports.CSSManager = CSSManager;
//# sourceMappingURL=css.js.map