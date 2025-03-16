import { FunctionExt } from '@antv/x6-common';
import { router } from './router';
import { defaults } from './options';
export const manhattan = function (vertices, options, edgeView) {
    return FunctionExt.call(router, this, vertices, Object.assign(Object.assign({}, defaults), options), edgeView);
};
//# sourceMappingURL=index.js.map