"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Basecoat = void 0;
const event_1 = require("../event");
const object_1 = require("../object");
const disposable_1 = require("./disposable");
class Basecoat extends event_1.Events {
    dispose() {
        this.off();
    }
}
__decorate([
    disposable_1.Disposable.dispose()
], Basecoat.prototype, "dispose", null);
exports.Basecoat = Basecoat;
(function (Basecoat) {
    Basecoat.dispose = disposable_1.Disposable.dispose;
})(Basecoat = exports.Basecoat || (exports.Basecoat = {}));
object_1.ObjectExt.applyMixins(Basecoat, disposable_1.Disposable);
//# sourceMappingURL=basecoat.js.map