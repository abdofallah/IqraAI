"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.className = void 0;
const x6_common_1 = require("@antv/x6-common");
const config_1 = require("../../config");
const defaultClassName = config_1.Config.prefix('highlighted');
exports.className = {
    highlight(cellView, magnet, options) {
        const cls = (options && options.className) || defaultClassName;
        x6_common_1.Dom.addClass(magnet, cls);
    },
    unhighlight(cellView, magnet, options) {
        const cls = (options && options.className) || defaultClassName;
        x6_common_1.Dom.removeClass(magnet, cls);
    },
};
//# sourceMappingURL=class.js.map