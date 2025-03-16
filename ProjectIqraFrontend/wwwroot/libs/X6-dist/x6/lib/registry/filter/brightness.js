"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.brightness = void 0;
const util_1 = require("./util");
function brightness(args = {}) {
    const amount = (0, util_1.getNumber)(args.amount, 1);
    return `
    <filter>
      <feComponentTransfer>
        <feFuncR type="linear" slope="${amount}"/>
        <feFuncG type="linear" slope="${amount}"/>
        <feFuncB type="linear" slope="${amount}"/>
      </feComponentTransfer>
    </filter>
  `.trim();
}
exports.brightness = brightness;
//# sourceMappingURL=brightness.js.map