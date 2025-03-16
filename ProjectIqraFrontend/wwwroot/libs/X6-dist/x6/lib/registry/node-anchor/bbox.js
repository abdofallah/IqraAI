"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.bottomRight = exports.bottomLeft = exports.topRight = exports.topLeft = exports.right = exports.left = exports.bottom = exports.top = exports.center = void 0;
const x6_common_1 = require("@antv/x6-common");
exports.center = createBBoxAnchor('center');
exports.top = createBBoxAnchor('topCenter');
exports.bottom = createBBoxAnchor('bottomCenter');
exports.left = createBBoxAnchor('leftMiddle');
exports.right = createBBoxAnchor('rightMiddle');
exports.topLeft = createBBoxAnchor('topLeft');
exports.topRight = createBBoxAnchor('topRight');
exports.bottomLeft = createBBoxAnchor('bottomLeft');
exports.bottomRight = createBBoxAnchor('bottomRight');
function createBBoxAnchor(method) {
    return function (view, magnet, ref, options = {}) {
        let bbox;
        if (view.cell.visible) {
            bbox = options.rotate
                ? view.getUnrotatedBBoxOfElement(magnet)
                : view.getBBoxOfElement(magnet);
        }
        else {
            bbox = view.cell.getBBox();
        }
        const result = bbox[method];
        result.x += x6_common_1.NumberExt.normalizePercentage(options.dx, bbox.width);
        result.y += x6_common_1.NumberExt.normalizePercentage(options.dy, bbox.height);
        const cell = view.cell;
        return options.rotate
            ? result.rotate(-cell.getAngle(), cell.getBBox().getCenter())
            : result;
    };
}
//# sourceMappingURL=bbox.js.map