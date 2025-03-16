import { Dom } from '@antv/x6-common';
import { Config } from '../../config';
const defaultClassName = Config.prefix('highlighted');
export const className = {
    highlight(cellView, magnet, options) {
        const cls = (options && options.className) || defaultClassName;
        Dom.addClass(magnet, cls);
    },
    unhighlight(cellView, magnet, options) {
        const cls = (options && options.className) || defaultClassName;
        Dom.removeClass(magnet, cls);
    },
};
//# sourceMappingURL=class.js.map