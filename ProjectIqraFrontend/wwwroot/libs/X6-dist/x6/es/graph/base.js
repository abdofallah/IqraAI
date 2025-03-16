import { Disposable } from '@antv/x6-common';
export class Base extends Disposable {
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
//# sourceMappingURL=base.js.map