"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const sinon_1 = __importDefault(require("sinon"));
const event_1 = require("../../event");
describe('events', () => {
    it('should trigger with context', () => {
        const obj = new event_1.Events();
        const spy = sinon_1.default.spy();
        const ctx = {};
        obj.on('a', spy, ctx);
        obj.trigger('a');
        expect(spy.calledOn(ctx)).toBeTruthy();
    });
    it('should trigger with arguments', () => {
        const obj = new event_1.Events();
        const spy1 = sinon_1.default.spy();
        const spy2 = sinon_1.default.spy();
        obj.on('a', spy1);
        obj.on('b', spy2);
        const data = { a: 1 };
        obj.trigger('a', 1, 2, 3);
        obj.trigger('b', data);
        expect(spy1.calledWith(1, 2, 3)).toBeTruthy();
        expect(spy2.calledWith(data)).toBeTruthy();
    });
    it('should trigger event multi times', () => {
        const obj = new event_1.Events();
        const spy = sinon_1.default.spy();
        obj.on('a', spy);
        obj.trigger('a', 1, 2);
        expect(spy.callCount).toBe(1);
        obj.trigger('a');
        obj.trigger('a');
        obj.trigger('b');
        expect(spy.callCount).toBe(3);
    });
    it('should trigger once', () => {
        const spy1 = sinon_1.default.spy();
        const spy2 = sinon_1.default.spy();
        const obj = new event_1.Events();
        const context = {};
        obj.once('a', spy1, context);
        obj.once('b', spy2, context);
        obj.trigger('a', 1);
        obj.trigger('b', 1, 2);
        obj.trigger('a', 1);
        obj.trigger('b', 1);
        obj.trigger('c');
        expect(spy1.withArgs(1).calledOnce).toBeTruthy();
        expect(spy1.calledOn(context)).toBeTruthy();
        expect(spy2.withArgs(1, 2).calledOnce).toBeTruthy();
        expect(spy2.calledOn(context)).toBeTruthy();
    });
    it('should returns callback status', () => {
        const obj = new event_1.Events();
        const stub1 = sinon_1.default.stub();
        const stub2 = sinon_1.default.stub();
        obj.on('a', stub1);
        obj.on('a', stub2);
        stub1.returns(false);
        stub2.returns(true);
        expect(obj.trigger('a')).toBe(false);
        stub1.returns(undefined);
        stub2.returns(null);
        expect(obj.trigger('a')).toBe(true);
        stub1.returns(true);
        stub2.returns(true);
        expect(obj.trigger('a')).toBe(true);
    });
    it('should not bind invalid callbacks', () => {
        const obj = new event_1.Events();
        const spy = sinon_1.default.spy();
        obj.on('a', null);
        obj.trigger('a');
        expect(spy.callCount).toBe(0);
    });
    it('should bind a callback with a specific context', () => {
        const obj = new event_1.Events();
        const context = {};
        const spy = sinon_1.default.spy();
        obj.on('a', spy, context);
        obj.trigger('a');
        expect(spy.calledOn(context));
    });
    it('should unbind event', () => {
        const obj = new event_1.Events();
        const spy = sinon_1.default.spy();
        obj.on('a', spy);
        obj.trigger('a');
        expect(spy.callCount).toBe(1);
        obj.off('a');
        obj.off('b');
        obj.trigger('a');
        expect(spy.callCount).toBe(1);
    });
    it('should unbind specified event callback', () => {
        const obj = new event_1.Events();
        const spyA = sinon_1.default.spy();
        const spyB = sinon_1.default.spy();
        obj.on('a', spyA);
        obj.on('a', spyB);
        obj.trigger('a');
        expect(spyA.callCount).toBe(1);
        expect(spyB.callCount).toBe(1);
        obj.off('a', spyA);
        obj.trigger('a');
        expect(spyA.callCount).toBe(1);
        expect(spyB.callCount).toBe(2);
    });
    it('should unbind a callback in the midst of it trigger', () => {
        const obj = new event_1.Events();
        const spy = sinon_1.default.spy();
        function callback() {
            spy();
            obj.off('a', callback);
        }
        obj.on('a', callback);
        obj.trigger('a');
        obj.trigger('a');
        expect(spy.callCount).toBe(1);
    });
    it('should remove all events', () => {
        const obj = new event_1.Events();
        const spy1 = sinon_1.default.spy();
        const spy2 = sinon_1.default.spy();
        obj.on('x', spy1);
        obj.on('y', spy2);
        obj.trigger('x');
        obj.trigger('y');
        obj.off();
        obj.trigger('x');
        obj.trigger('y');
        expect(spy1.callCount).toBe(1);
        expect(spy2.callCount).toBe(1);
    });
    it('should remove all events for a specific event', () => {
        const obj = new event_1.Events();
        const spyA = sinon_1.default.spy();
        const spyB = sinon_1.default.spy();
        obj.on('x', spyA);
        obj.on('y', spyA);
        obj.on('x', spyB);
        obj.on('y', spyB);
        obj.trigger('x');
        obj.off('x');
        obj.off('y');
        obj.trigger('x');
        expect(spyA.callCount).toBe(1);
        expect(spyB.callCount).toBe(1);
    });
    it('should remove all events for a specific context', () => {
        const obj = new event_1.Events();
        const spyA = sinon_1.default.spy();
        const spyB = sinon_1.default.spy();
        const context = {};
        obj.on('x', spyA);
        obj.on('y', spyA);
        obj.on('x', spyB, context);
        obj.on('y', spyB, context);
        obj.off(null, null, context);
        obj.trigger('x');
        obj.trigger('y');
        expect(spyA.callCount).toBe(2);
        expect(spyB.callCount).toBe(0);
    });
    it('should remove all events for a specific callback', () => {
        const obj = new event_1.Events();
        const success = sinon_1.default.spy();
        const fail = sinon_1.default.spy();
        obj.on('x', success);
        obj.on('y', success);
        obj.on('x', fail);
        obj.on('y', fail);
        obj.off(null, fail);
        obj.trigger('x');
        obj.trigger('y');
        expect(success.callCount).toBe(2);
        expect(fail.callCount).toBe(0);
    });
});
//# sourceMappingURL=events.test.js.map