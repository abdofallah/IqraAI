import { ObjectExt } from '@antv/x6-common';
export const filter = {
    qualify: ObjectExt.isPlainObject,
    set(filter, { view }) {
        return `url(#${view.graph.defineFilter(filter)})`;
    },
};
//# sourceMappingURL=filter.js.map