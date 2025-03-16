"use strict";
/* eslint-disable no-underscore-dangle */
Object.defineProperty(exports, "__esModule", { value: true });
exports.Disablable = void 0;
const basecoat_1 = require("./basecoat");
class Disablable extends basecoat_1.Basecoat {
    get disabled() {
        return this._disabled === true;
    }
    enable() {
        delete this._disabled;
    }
    disable() {
        this._disabled = true;
    }
}
exports.Disablable = Disablable;
//# sourceMappingURL=disablable.js.map