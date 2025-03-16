"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.hueRotate = void 0;
const util_1 = require("./util");
function hueRotate(args = {}) {
    const angle = (0, util_1.getNumber)(args.angle, 0);
    return `
      <filter>
        <feColorMatrix type="hueRotate" values="${angle}"/>
      </filter>
    `.trim();
}
exports.hueRotate = hueRotate;
//# sourceMappingURL=hue-rotate.js.map