var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { CssLoader } from '@antv/x6-common';
import { Config } from '../config';
import { content } from '../style/raw';
import { Base } from './base';
export class CSSManager extends Base {
    init() {
        if (Config.autoInsertCSS) {
            CssLoader.ensure('core', content);
        }
    }
    dispose() {
        CssLoader.clean('core');
    }
}
__decorate([
    CSSManager.dispose()
], CSSManager.prototype, "dispose", null);
//# sourceMappingURL=css.js.map