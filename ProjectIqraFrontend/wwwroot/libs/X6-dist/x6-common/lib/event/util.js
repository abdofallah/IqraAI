"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.call = void 0;
const function_1 = require("../function");
function call(list, args) {
    const results = [];
    for (let i = 0; i < list.length; i += 2) {
        const handler = list[i];
        const context = list[i + 1];
        const params = Array.isArray(args) ? args : [args];
        const ret = function_1.FunctionExt.apply(handler, context, params);
        results.push(ret);
    }
    return function_1.FunctionExt.toAsyncBoolean(results);
}
exports.call = call;
//# sourceMappingURL=util.js.map