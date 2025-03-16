import { ObjectExt } from '@antv/x6-common';
export const fill = {
    qualify: ObjectExt.isPlainObject,
    set(fill, { view }) {
        return `url(#${view.graph.defineGradient(fill)})`;
    },
};
//# sourceMappingURL=fill.js.map