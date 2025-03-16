"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.opacity = void 0;
const x6_common_1 = require("@antv/x6-common");
const config_1 = require("../../config");
const className = config_1.Config.prefix('highlight-opacity');
exports.opacity = {
    highlight(cellView, magnet) {
        x6_common_1.Dom.addClass(magnet, className);
    },
    unhighlight(cellView, magnetEl) {
        x6_common_1.Dom.removeClass(magnetEl, className);
    },
};
//# sourceMappingURL=opacity.js.map