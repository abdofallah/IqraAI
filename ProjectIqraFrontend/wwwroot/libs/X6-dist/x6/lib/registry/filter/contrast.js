"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.contrast = void 0;
const util_1 = require("./util");
function contrast(args = {}) {
    const amount = (0, util_1.getNumber)(args.amount, 1);
    const amount2 = 0.5 - amount / 2;
    return `
    <filter>
     <feComponentTransfer>
        <feFuncR type="linear" slope="${amount}" intercept="${amount2}"/>
        <feFuncG type="linear" slope="${amount}" intercept="${amount2}"/>
        <feFuncB type="linear" slope="${amount}" intercept="${amount2}"/>
      </feComponentTransfer>
    </filter>
  `.trim();
}
exports.contrast = contrast;
//# sourceMappingURL=contrast.js.map