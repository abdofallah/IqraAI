"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.radialOriented = exports.radial = void 0;
const x6_geometry_1 = require("@antv/x6-geometry");
const util_1 = require("./util");
const radial = (portPosition, elemBBox, args) => radialLayout(portPosition.diff(elemBBox.getCenter()), false, args);
exports.radial = radial;
const radialOriented = (portPosition, elemBBox, args) => radialLayout(portPosition.diff(elemBBox.getCenter()), true, args);
exports.radialOriented = radialOriented;
function radialLayout(portCenterOffset, autoOrient, args) {
    const offset = args.offset != null ? args.offset : 20;
    const origin = new x6_geometry_1.Point(0, 0);
    const angle = -portCenterOffset.theta(origin);
    const pos = portCenterOffset
        .clone()
        .move(origin, offset)
        .diff(portCenterOffset)
        .round();
    let y = '.3em';
    let textAnchor;
    let orientAngle = angle;
    if ((angle + 90) % 180 === 0) {
        textAnchor = autoOrient ? 'end' : 'middle';
        if (!autoOrient && angle === -270) {
            y = '0em';
        }
    }
    else if (angle > -270 && angle < -90) {
        textAnchor = 'start';
        orientAngle = angle - 180;
    }
    else {
        textAnchor = 'end';
    }
    return (0, util_1.toResult)({
        position: pos.round().toJSON(),
        angle: autoOrient ? orientAngle : 0,
        attrs: {
            '.': {
                y,
                'text-anchor': textAnchor,
            },
        },
    }, args);
}
//# sourceMappingURL=radial.js.map