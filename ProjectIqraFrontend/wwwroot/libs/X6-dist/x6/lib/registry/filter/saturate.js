"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.saturate = void 0;
const util_1 = require("./util");
function saturate(args = {}) {
    const amount = (0, util_1.getNumber)(args.amount, 1);
    return `
      <filter>
        <feColorMatrix type="saturate" values="${1 - amount}"/>
      </filter>
    `.trim();
}
exports.saturate = saturate;
//# sourceMappingURL=saturate.js.map