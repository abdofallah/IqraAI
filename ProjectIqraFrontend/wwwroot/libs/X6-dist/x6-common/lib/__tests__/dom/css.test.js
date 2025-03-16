"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const vector_1 = require("../../vector");
const dom_1 = require("../../dom");
describe('Dom', () => {
    describe('css', () => {
        it('should set right style property for element', () => {
            const vel = vector_1.Vector.create('rect');
            const node = vel.node;
            dom_1.Dom.css(node, {
                stroke: 'red',
                userDrag: 'auto',
            });
            expect(dom_1.Dom.css(node, 'stroke')).toEqual('red');
            expect(dom_1.Dom.css(node, 'webkitUserDrag')).toEqual('auto');
        });
    });
});
//# sourceMappingURL=css.test.js.map