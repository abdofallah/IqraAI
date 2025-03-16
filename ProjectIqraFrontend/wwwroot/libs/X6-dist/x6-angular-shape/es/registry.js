var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                t[p[i]] = s[p[i]];
        }
    return t;
};
import { Graph } from '@antv/x6';
export const registerInfo = new Map();
export function register(config) {
    const { shape, injector, content } = config, others = __rest(config, ["shape", "injector", "content"]);
    registerInfo.set(shape, { injector, content });
    Graph.registerNode(shape, Object.assign({ inherit: 'angular-shape' }, others), true);
}
//# sourceMappingURL=registry.js.map