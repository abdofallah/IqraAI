var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { Events } from '../event';
import { ObjectExt } from '../object';
import { Disposable } from './disposable';
export class Basecoat extends Events {
    dispose() {
        this.off();
    }
}
__decorate([
    Disposable.dispose()
], Basecoat.prototype, "dispose", null);
(function (Basecoat) {
    Basecoat.dispose = Disposable.dispose;
})(Basecoat || (Basecoat = {}));
ObjectExt.applyMixins(Basecoat, Disposable);
//# sourceMappingURL=basecoat.js.map