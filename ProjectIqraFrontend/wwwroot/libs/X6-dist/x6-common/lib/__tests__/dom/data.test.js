"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const vector_1 = require("../../vector");
const dom_1 = require("../../dom");
const object_1 = require("../../object");
describe('Dom', () => {
    describe('data', () => {
        it('should get empty data for a new element', () => {
            const vel = vector_1.Vector.create('rect');
            const node = vel.node;
            const data = dom_1.Dom.data(node);
            expect(object_1.ObjectExt.isEmpty(data)).toEqual(true);
        });
        it('should set/get all data for a element', () => {
            const vel = vector_1.Vector.create('rect');
            const node = vel.node;
            const key = 'dataKey';
            const value = { foo: 'foo', bar: 20 };
            dom_1.Dom.data(node, key, value);
            const data = dom_1.Dom.data(node);
            expect(data[key].foo).toEqual(value.foo);
            expect(data[key].bar).toEqual(value.bar);
        });
        it('should set/get correct data for a element', () => {
            const vel = vector_1.Vector.create('rect');
            const node = vel.node;
            const key = 'data-key';
            const value = { foo: 'foo', bar: 20 };
            dom_1.Dom.data(node, key, value);
            const data = dom_1.Dom.data(node, key);
            expect(data.foo).toEqual(value.foo);
            expect(data.bar).toEqual(value.bar);
        });
    });
});
//# sourceMappingURL=data.test.js.map