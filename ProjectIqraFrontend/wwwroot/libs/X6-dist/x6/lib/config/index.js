"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Config = void 0;
exports.Config = {
    prefixCls: 'x6',
    autoInsertCSS: true,
    useCSSSelector: true,
    prefix(suffix) {
        return `${exports.Config.prefixCls}-${suffix}`;
    },
};
//# sourceMappingURL=index.js.map