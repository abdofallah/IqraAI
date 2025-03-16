"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Base = void 0;
const x6_common_1 = require("@antv/x6-common");
class Base extends x6_common_1.Disposable {
    get options() {
        return this.graph.options;
    }
    get model() {
        return this.graph.model;
    }
    get view() {
        return this.graph.view;
    }
    constructor(graph) {
        super();
        this.graph = graph;
        this.init();
    }
    init() { }
}
exports.Base = Base;
//# sourceMappingURL=base.js.map