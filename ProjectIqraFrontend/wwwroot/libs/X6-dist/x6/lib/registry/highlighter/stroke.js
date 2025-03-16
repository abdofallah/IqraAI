"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.stroke = void 0;
const x6_common_1 = require("@antv/x6-common");
const config_1 = require("../../config");
const util_1 = require("../../util");
const defaultOptions = {
    padding: 3,
    rx: 0,
    ry: 0,
    attrs: {
        'stroke-width': 3,
        stroke: '#FEB663',
    },
};
exports.stroke = {
    highlight(cellView, magnet, options) {
        const id = Private.getHighlighterId(magnet, options);
        if (Private.hasCache(id)) {
            return;
        }
        // eslint-disable-next-line
        options = x6_common_1.ObjectExt.defaultsDeep({}, options, defaultOptions);
        const magnetVel = x6_common_1.Vector.create(magnet);
        let pathData;
        let magnetBBox;
        try {
            pathData = magnetVel.toPathData();
        }
        catch (error) {
            // Failed to get path data from magnet element.
            // Draw a rectangle around the entire cell view instead.
            magnetBBox = util_1.Util.bbox(magnetVel.node, true);
            pathData = x6_common_1.Dom.rectToPathData(Object.assign(Object.assign({}, options), magnetBBox));
        }
        const path = x6_common_1.Dom.createSvgElement('path');
        x6_common_1.Dom.attr(path, Object.assign({ d: pathData, 'pointer-events': 'none', 'vector-effect': 'non-scaling-stroke', fill: 'none' }, (options.attrs ? x6_common_1.Dom.kebablizeAttrs(options.attrs) : null)));
        // const highlightVel = v.create('path').attr()
        if (cellView.isEdgeElement(magnet)) {
            x6_common_1.Dom.attr(path, 'd', cellView.getConnectionPathData());
        }
        else {
            let highlightMatrix = magnetVel.getTransformToElement(cellView.container);
            // Add padding to the highlight element.
            const padding = options.padding;
            if (padding) {
                if (magnetBBox == null) {
                    magnetBBox = util_1.Util.bbox(magnetVel.node, true);
                }
                const cx = magnetBBox.x + magnetBBox.width / 2;
                const cy = magnetBBox.y + magnetBBox.height / 2;
                magnetBBox = util_1.Util.transformRectangle(magnetBBox, highlightMatrix);
                const width = Math.max(magnetBBox.width, 1);
                const height = Math.max(magnetBBox.height, 1);
                const sx = (width + padding) / width;
                const sy = (height + padding) / height;
                const paddingMatrix = x6_common_1.Dom.createSVGMatrix({
                    a: sx,
                    b: 0,
                    c: 0,
                    d: sy,
                    e: cx - sx * cx,
                    f: cy - sy * cy,
                });
                highlightMatrix = highlightMatrix.multiply(paddingMatrix);
            }
            x6_common_1.Dom.transform(path, highlightMatrix);
        }
        x6_common_1.Dom.addClass(path, config_1.Config.prefix('highlight-stroke'));
        const cell = cellView.cell;
        const removeHandler = () => Private.removeHighlighter(id);
        cell.on('removed', removeHandler);
        if (cell.model) {
            cell.model.on('reseted', removeHandler);
        }
        cellView.container.appendChild(path);
        Private.setCache(id, path);
    },
    unhighlight(cellView, magnet, opt) {
        Private.removeHighlighter(Private.getHighlighterId(magnet, opt));
    },
};
var Private;
(function (Private) {
    function getHighlighterId(magnet, options) {
        x6_common_1.Dom.ensureId(magnet);
        return magnet.id + JSON.stringify(options);
    }
    Private.getHighlighterId = getHighlighterId;
    const cache = {};
    function setCache(id, elem) {
        cache[id] = elem;
    }
    Private.setCache = setCache;
    function hasCache(id) {
        return cache[id] != null;
    }
    Private.hasCache = hasCache;
    function removeHighlighter(id) {
        const elem = cache[id];
        if (elem) {
            x6_common_1.Dom.remove(elem);
            delete cache[id];
        }
    }
    Private.removeHighlighter = removeHighlighter;
})(Private || (Private = {}));
//# sourceMappingURL=stroke.js.map