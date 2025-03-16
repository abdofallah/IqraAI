"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const object_1 = require("../../object");
describe('Object', () => {
    const obj = {
        name: 'x6',
        age: 1,
        gender: null,
        tall: true,
    };
    describe('#getValue', () => {
        it('should return a value of a object', () => {
            expect(object_1.ObjectExt.getValue(obj, 'name')).toBe('x6');
            expect(object_1.ObjectExt.getValue(obj, 'age')).toBe(1);
            expect(object_1.ObjectExt.getValue(obj, 'gender')).toBe(null);
            expect(object_1.ObjectExt.getValue(obj, 'count')).toBe(undefined);
        });
        it('should return a defaultValue of a object', () => {
            expect(object_1.ObjectExt.getValue(obj, 'gender', 'male')).toBe('male');
            expect(object_1.ObjectExt.getValue(obj, 'count', 100)).toBe(100);
        });
    });
    describe('#getNumber', () => {
        it('should return a number value of a object', () => {
            expect(object_1.ObjectExt.getNumber(obj, 'age', 2)).toBe(1);
        });
        it('should return a defaultValue of a object', () => {
            expect(object_1.ObjectExt.getNumber(obj, 'name', 10)).toBe(10);
            expect(object_1.ObjectExt.getNumber(obj, 'count', 20)).toBe(20);
        });
    });
    describe('#getBoolean', () => {
        it('should return a boolean value of a object', () => {
            expect(object_1.ObjectExt.getBoolean(obj, 'tall', false)).toBe(true);
            expect(object_1.ObjectExt.getBoolean(obj, 'name', false)).toBe(true);
            expect(object_1.ObjectExt.getBoolean(obj, 'age', false)).toBe(true);
        });
        it('should return a defaultValue of a object', () => {
            expect(object_1.ObjectExt.getBoolean(obj, 'gender', false)).toBe(false);
            expect(object_1.ObjectExt.getBoolean(obj, 'count', true)).toBe(true);
        });
    });
    describe('#getByPath#setByPath', () => {
        const project = {
            name: 'x6',
            version: ['0.1', '0.2', '0.3'],
            attr: {
                node: {
                    fontSize: 14,
                },
                edge: {
                    color: 'red',
                },
            },
        };
        it('should set or get object value by path', () => {
            expect(object_1.ObjectExt.getByPath(project, 'version/1')).toBe('0.2');
            expect(object_1.ObjectExt.getByPath(project, 'attr/node/fontSize')).toBe(14);
            expect(object_1.ObjectExt.getByPath(project, 'attr/node/color')).toBe(undefined);
            object_1.ObjectExt.setByPath(project, 'version/1', '0.8');
            object_1.ObjectExt.setByPath(project, 'attr/node/fontSize', 16);
            object_1.ObjectExt.setByPath(project, 'attr/node/color', 'green');
            expect(object_1.ObjectExt.getByPath(project, 'version/1')).toBe('0.8');
            expect(object_1.ObjectExt.getByPath(project, 'attr/node/fontSize')).toBe(16);
            expect(object_1.ObjectExt.getByPath(project, 'attr/node/color')).toBe('green');
            object_1.ObjectExt.unsetByPath(project, 'version/1');
            object_1.ObjectExt.unsetByPath(project, 'attr/node/fontSize');
            expect(object_1.ObjectExt.getByPath(project, 'version/1')).toBe(undefined);
            expect(object_1.ObjectExt.getByPath(project, 'attr/node/fontSize')).toBe(undefined);
        });
    });
    describe('#flatten', () => {
        const project = {
            name: 'x6',
            version: ['0.1', '0.2', '0.3'],
            attr: {
                node: {
                    fontSize: 14,
                },
                edge: {
                    color: 'red',
                },
            },
        };
        it('should return flatten object', () => {
            expect(object_1.ObjectExt.flatten(project)).toEqual({
                name: 'x6',
                'version/0': '0.1',
                'version/1': '0.2',
                'version/2': '0.3',
                'attr/node/fontSize': 14,
                'attr/edge/color': 'red',
            });
        });
    });
});
//# sourceMappingURL=object.test.js.map