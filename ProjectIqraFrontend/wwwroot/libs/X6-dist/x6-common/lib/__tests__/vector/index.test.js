"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const vector_1 = require("../../vector");
describe('Vector', () => {
    describe('#create', () => {
        it('should create a vector instance with tagName', () => {
            const vel = vector_1.Vector.create('rect');
            expect(vector_1.Vector.isVector(vel)).toBe(true);
            expect(vel.node).toBeInstanceOf(SVGRectElement);
        });
        it('should create a vector instance with SVGElement', () => {
            const old = vector_1.Vector.create('rect');
            const vel = vector_1.Vector.create(old.node);
            expect(vector_1.Vector.isVector(vel)).toBe(true);
            expect(vel.node).toBeInstanceOf(SVGRectElement);
        });
        it('should create a vector instance with another vector', () => {
            const old = vector_1.Vector.create('rect');
            const vel = vector_1.Vector.create(old);
            expect(vector_1.Vector.isVector(vel)).toBe(true);
            expect(vel.node).toBeInstanceOf(SVGRectElement);
        });
        it('should create a vector instance with markup', () => {
            const vel = vector_1.Vector.create('<rect width="100%" height="100%" fill="red" />');
            expect(vel.node).toBeInstanceOf(SVGRectElement);
            expect(vel.getAttribute('width')).toEqual('100%');
            expect(vel.getAttribute('height')).toEqual('100%');
            expect(vel.getAttribute('fill')).toEqual('red');
        });
        it('should throw an error with invalid markup', () => {
            const fn = () => vector_1.Vector.create('<invalid markup>');
            expect(fn).toThrowError();
        });
        it('should throw an error with empty markup', () => {
            const fn = () => vector_1.Vector.create('');
            expect(fn).toThrowError();
        });
    });
    describe('#createVectors', () => {
        it('should return an array of vectors', () => {
            const vels = vector_1.Vector.createVectors('<path id="svg-path" d="M10 10"/>' +
                '<!-- comment -->' +
                '<g id="svg-group">' +
                '  <ellipse id="svg-ellipse" x="10" y="10" rx="30" ry="30"/>' +
                '  <circle id="svg-circle" cx="10" cy="10" r="2" fill="red"/>' +
                '</g>' +
                '<polygon id="svg-polygon" points="200,10 250,190 160,210"/>' +
                '<text id="svg-text" x="0" y="15" fill="red">Test</text>' +
                '<rect id="svg-rectangle" x="100" y="100" width="50" height="100"/>' +
                '<g id="svg-group-1" class="group-1">' +
                '  <g id="svg-group-2" class="group-2">' +
                '    <g id="svg-group-3" class="group3">' +
                '      <path id="svg-path-2" d="M 100 100 C 100 100 0 150 100 200 Z"/>' +
                '    </g>' +
                '  </g>' +
                '</g>' +
                '<path id="svg-path-3"/>' +
                '<linearGradient id= "svg-linear-gradient"><stop/></linearGradient>');
            expect(Array.isArray(vels)).toBe(true);
            expect(vels.length).toEqual(9);
            vels.forEach((vel) => {
                expect(vector_1.Vector.isVector(vel)).toBe(true);
            });
        });
        it('should fall back to create a vector', () => {
            expect(vector_1.Vector.createVectors('rect').length).toEqual(1);
        });
    });
});
//# sourceMappingURL=index.test.js.map