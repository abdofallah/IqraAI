"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const sinon_1 = __importDefault(require("sinon"));
const function_1 = require("../../function");
describe('FunctionExt', () => {
    describe('#call', () => {
        it('should invoke function with empty args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            function_1.FunctionExt.call(spy, ctx);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith()).toBeTruthy();
        });
        it('should invoke function with one args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            function_1.FunctionExt.call(spy, ctx, 1);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1)).toBeTruthy();
        });
        it('should invoke function with two args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            function_1.FunctionExt.call(spy, ctx, 1, '2');
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2')).toBeTruthy();
        });
        it('should invoke function with three args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            function_1.FunctionExt.call(spy, ctx, 1, '2', true);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2', true)).toBeTruthy();
        });
        it('should invoke function with four args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            const obj = {};
            function_1.FunctionExt.call(spy, ctx, 1, '2', true, obj);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2', true, obj)).toBeTruthy();
        });
        it('should invoke function with five args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            const obj = {};
            const reg = /a/g;
            function_1.FunctionExt.call(spy, ctx, 1, '2', true, obj, reg);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2', true, obj, reg)).toBeTruthy();
        });
        it('should invoke function with six args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            const obj = {};
            const reg = /a/g;
            const arr = [1, 2, 3];
            function_1.FunctionExt.call(spy, ctx, 1, '2', true, obj, reg, arr);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2', true, obj, reg, arr)).toBeTruthy();
        });
        it('should invoke function with more args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            const obj = {};
            const reg = /a/g;
            const arr = [1, 2, 3];
            function_1.FunctionExt.call(spy, ctx, 1, '2', true, obj, reg, arr, 'more');
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2', true, obj, reg, arr, 'more')).toBeTruthy();
        });
    });
    describe('#apply', () => {
        it('should invoke function with empty args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            function_1.FunctionExt.apply(spy, ctx);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith()).toBeTruthy();
        });
        it('should invoke function with one args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            function_1.FunctionExt.apply(spy, ctx, [1]);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1)).toBeTruthy();
        });
        it('should invoke function with two args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            function_1.FunctionExt.apply(spy, ctx, [1, '2']);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2')).toBeTruthy();
        });
        it('should invoke function with three args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            function_1.FunctionExt.apply(spy, ctx, [1, '2', true]);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2', true)).toBeTruthy();
        });
        it('should invoke function with four args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            const obj = {};
            function_1.FunctionExt.apply(spy, ctx, [1, '2', true, obj]);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2', true, obj)).toBeTruthy();
        });
        it('should invoke function with five args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            const obj = {};
            const reg = /a/g;
            function_1.FunctionExt.apply(spy, ctx, [1, '2', true, obj, reg]);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2', true, obj, reg)).toBeTruthy();
        });
        it('should invoke function with six args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            const obj = {};
            const reg = /a/g;
            const arr = [1, 2, 3];
            function_1.FunctionExt.apply(spy, ctx, [1, '2', true, obj, reg, arr]);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2', true, obj, reg, arr)).toBeTruthy();
        });
        it('should invoke function with more args', () => {
            const spy = sinon_1.default.spy();
            const ctx = {};
            const obj = {};
            const reg = /a/g;
            const arr = [1, 2, 3];
            function_1.FunctionExt.apply(spy, ctx, [1, '2', true, obj, reg, arr, 'more']);
            expect(spy.calledOn(ctx)).toBeTruthy();
            expect(spy.calledWith(1, '2', true, obj, reg, arr, 'more')).toBeTruthy();
        });
    });
});
//# sourceMappingURL=function.test.js.map