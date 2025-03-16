"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const vector_1 = require("../../vector");
const dom_1 = require("../../dom");
describe('Dom', () => {
    describe('class', () => {
        describe('hasClass', () => {
            it('should return `false` when element or selector is null', () => {
                expect(dom_1.Dom.hasClass(null, null)).toBe(false);
            });
            it('should return `false` for invalid element', () => {
                const text = document.createTextNode('');
                dom_1.Dom.addClass(text, 'test');
                expect(dom_1.Dom.hasClass(text, 'test')).toBe(false);
                const vc = vector_1.Vector.create(document.createComment(''));
                vc.addClass('test');
                expect(vc.hasClass('test')).toBe(false);
            });
        });
        describe('#addClass', () => {
            let div;
            let vel;
            beforeEach(() => {
                div = vector_1.Vector.create('div');
                vel = vector_1.Vector.create('g');
            });
            it('should add class to HTMLDivElement', () => {
                div.addClass('test').addClass(null);
                const cls = div.node.getAttribute('class');
                expect(div.hasClass('test')).toBe(true);
                expect(cls.indexOf('test') !== -1).toBe(true);
                expect(div.attr('class')).toEqual('test');
            });
            it('should add class to SVGGElement', () => {
                vel.addClass('test');
                const cls = vel.node.getAttribute('class');
                expect(vel.hasClass('test')).toBe(true);
                expect(cls.indexOf('test') !== -1).toBe(true);
                expect(vel.attr('class')).toEqual('test');
            });
            it('should append to class list', () => {
                vel.attr('class', 'foo');
                vel.addClass('test');
                const cls = vel.node.getAttribute('class');
                expect(vel.hasClass('test')).toBe(true);
                expect(cls.indexOf('test') !== -1).toBe(true);
                expect(vel.attr('class')).toEqual('foo test');
                vel.addClass('foo bar baz');
                expect(vel.attr('class')).toEqual('foo test bar baz');
            });
            it('should not add the same class twice in same element', () => {
                div.addClass('foo').addClass('foo');
                expect(div.attr('class')).toEqual('foo');
                vel.addClass('foo foo');
                expect(vel.attr('class')).toEqual('foo');
            });
            it('should not add empty string', () => {
                vel.addClass('test');
                vel.addClass(' ');
                expect(vel.attr('class')).toEqual('test');
            });
            it('should call hook', () => {
                vel.addClass('test');
                vel.addClass(' ');
                dom_1.Dom.addClass(vel.node, (cls) => `${cls} foo`);
                expect(vel.attr('class')).toEqual('test foo');
            });
        });
        describe('#removeClass', () => {
            const vel = vector_1.Vector.create('g');
            it('should remove one', () => {
                vel.removeClass();
                vel.addClass('foo bar');
                vel.removeClass('foo test');
                expect(vel.attr('class')).toEqual('bar');
            });
            it('should remove all', () => {
                vel.removeClass();
                vel.addClass('foo bar');
                vel.removeClass();
                expect(vel.attr('class')).toEqual('');
            });
            it('should call hook', () => {
                vel.removeClass();
                vel.addClass('foo bar');
                dom_1.Dom.removeClass(vel.node, (cls) => cls.split(' ')[1]);
                expect(vel.attr('class')).toEqual('foo');
            });
            it('should do nothing for invalid element or selector', () => {
                dom_1.Dom.removeClass(null);
                dom_1.Dom.removeClass(null, null);
            });
        });
        describe('#toggleClass', () => {
            const vel = vector_1.Vector.create('g');
            it('should do nothing for invalid element or selector', () => {
                dom_1.Dom.toggleClass(null, 'foo');
                dom_1.Dom.toggleClass(null, null);
            });
            it('should toggle class', () => {
                vel.removeClass();
                vel.toggleClass('foo bar');
                expect(vel.attr('class')).toEqual('foo bar');
                vel.toggleClass('foo');
                expect(vel.attr('class')).toEqual('bar');
                vel.toggleClass('foo');
                expect(vel.attr('class')).toEqual('bar foo');
            });
            it('should not toggle empty strings', () => {
                vel.removeClass();
                vel.toggleClass('foo bar');
                expect(vel.attr('class')).toEqual('foo bar');
                vel.toggleClass(' ');
                expect(vel.attr('class')).toEqual('foo bar');
                vel.toggleClass(' ');
                expect(vel.attr('class')).toEqual('foo bar');
            });
            it('should work with the specified next state', () => {
                vel.removeClass();
                vel.toggleClass('foo bar');
                expect(vel.attr('class')).toEqual('foo bar');
                vel.toggleClass('foo', true);
                expect(vel.attr('class')).toEqual('foo bar');
                vel.toggleClass('foo', true);
                expect(vel.attr('class')).toEqual('foo bar');
                vel.toggleClass('foo', false);
                expect(vel.attr('class')).toEqual('bar');
            });
            it('should call hook', () => {
                vel.removeClass();
                dom_1.Dom.toggleClass(vel.node, () => 'foo bar');
                expect(vel.attr('class')).toEqual('foo bar');
                dom_1.Dom.toggleClass(vel.node, () => 'foo', false);
                expect(vel.attr('class')).toEqual('bar');
            });
        });
    });
});
//# sourceMappingURL=class.test.js.map