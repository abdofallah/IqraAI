"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const dom_1 = require("../../dom");
const vector_1 = require("../../vector");
describe('Dom', () => {
    describe('matrix', () => {
        const fixture = document.createElement('div');
        const svgContainer = vector_1.Vector.create('svg').node;
        fixture.appendChild(svgContainer);
        document.body.appendChild(fixture);
        afterAll(() => {
            var _a;
            (_a = fixture.parentNode) === null || _a === void 0 ? void 0 : _a.removeChild(fixture);
        });
        describe('createSVGTransform', () => {
            it('should return SVG transform object', () => {
                const svgDocument = dom_1.Dom.createSvgElement('svg');
                const matrix = svgDocument.createSVGMatrix();
                expect(dom_1.Dom.createSVGTransform(matrix)).toBeInstanceOf(SVGTransform);
                expect(dom_1.Dom.createSVGTransform({
                    a: 1,
                    b: 0,
                    c: 0,
                    d: 1,
                    e: 0,
                    f: 0,
                })).toBeInstanceOf(SVGTransform);
            });
        });
        describe('#parseTransformString', () => {
            it('should parse scale, rotate, translate', () => {
                const parsed = dom_1.Dom.parseTransformString('scale(3) rotate(6) translate(9) xxx(11)');
                expect(parsed.scale).toEqual({ sx: 3, sy: 3 });
                expect(parsed.rotation).toEqual({
                    angle: 6,
                    cx: undefined,
                    cy: undefined,
                });
                expect(parsed.translation).toEqual({ tx: 9, ty: 0 });
            });
            it('should parse martix', () => {
                const parsed = dom_1.Dom.parseTransformString('matrix(1,0,0,1,30,30)');
                expect(parsed.scale).toEqual({ sx: 1, sy: 1 });
                expect(parsed.rotation).toEqual({
                    angle: 0,
                    cx: undefined,
                    cy: undefined,
                });
                expect(parsed.translation).toEqual({ tx: 30, ty: 30 });
            });
        });
        describe('#transformStringToMatrix', () => {
            let svgTestGroup;
            beforeEach(() => {
                svgTestGroup = vector_1.Vector.create('g');
                svgContainer.appendChild(svgTestGroup.node);
            });
            afterEach(() => {
                svgTestGroup.remove();
            });
            const arr = [
                '',
                'scale(2)',
                'scale(2,3)',
                'scale(2.5,3.1)',
                'translate(10, 10)',
                'translate(10,10)',
                'translate(10.2,11.6)',
                'rotate(10)',
                'rotate(10,100,100)',
                'skewX(40)',
                'skewY(60)',
                'scale(2,2) matrix(1 0 0 1 10 10)',
                'matrix(1 0 0 1 10 10) scale(2,2)',
                'rotate(10,100,100) matrix(1 0 0 1 10 10) scale(2,2) translate(10,20)',
            ];
            arr.forEach((transformString) => {
                it(`should convert "${transformString}" to matrix`, () => {
                    svgTestGroup.attr('transform', transformString);
                    expect(dom_1.Dom.transformStringToMatrix(transformString)).toEqual(svgTestGroup.node.getCTM());
                });
            });
        });
        describe('#matrixToTransformString', () => {
            it('should return correct transformation string', () => {
                expect(dom_1.Dom.matrixToTransformString()).toEqual('matrix(1,0,0,1,0,0)');
                expect(dom_1.Dom.matrixToTransformString({ a: 2, d: 2 })).toEqual('matrix(2,0,0,2,0,0)');
                expect(dom_1.Dom.matrixToTransformString({ a: 1, b: 2, c: 3, d: 4, e: 5, f: 6 })).toEqual('matrix(1,2,3,4,5,6)');
                expect(dom_1.Dom.matrixToTransformString(dom_1.Dom.createSVGMatrix({ a: 1, b: 2, c: 3, d: 4, e: 5, f: 6 }))).toEqual('matrix(1,2,3,4,5,6)');
                expect(dom_1.Dom.matrixToTransformString({ a: 0, b: 1, c: 1, d: 0, e: 0, f: 0 })).toEqual('matrix(0,1,1,0,0,0)');
            });
        });
        describe('#matrixTo[Transformation]', () => {
            function roundObject(obj) {
                // eslint-disable-next-line
                for (const i in obj) {
                    if (Object.prototype.hasOwnProperty.call(obj, i)) {
                        obj[i] = Math.round(obj[i]);
                    }
                }
                return obj;
            }
            it('should convert matrix to rotation metadata', () => {
                let angle;
                angle = dom_1.Dom.matrixToRotation(dom_1.Dom.createSVGMatrix().rotate(45));
                expect(roundObject(angle)).toEqual({ angle: 45 });
                angle = dom_1.Dom.matrixToRotation(dom_1.Dom.createSVGMatrix().translate(50, 50).rotate(15));
                expect(roundObject(angle)).toEqual({ angle: 15 });
                angle = dom_1.Dom.matrixToRotation(dom_1.Dom.createSVGMatrix().translate(50, 50).rotate(60).scale(2));
                expect(roundObject(angle)).toEqual({ angle: 60 });
                angle = dom_1.Dom.matrixToRotation(dom_1.Dom.createSVGMatrix().rotate(60).rotate(60));
                expect(roundObject(angle)).toEqual({ angle: 120 });
            });
            it('should convert matrix to translation medata', () => {
                let translate;
                translate = dom_1.Dom.matrixToTranslation(dom_1.Dom.createSVGMatrix().translate(10, 20));
                expect(roundObject(translate)).toEqual({ tx: 10, ty: 20 });
                translate = dom_1.Dom.matrixToTranslation(dom_1.Dom.createSVGMatrix().translate(10, 20).rotate(10, 20).scale(2));
                expect(roundObject(translate)).toEqual({ tx: 10, ty: 20 });
                translate = dom_1.Dom.matrixToTranslation(dom_1.Dom.createSVGMatrix().translate(10, 20).translate(30, 40));
                expect(roundObject(translate)).toEqual({ tx: 40, ty: 60 });
            });
            it('should convert matrix to scaling metadata', () => {
                let scale;
                scale = dom_1.Dom.matrixToScale(dom_1.Dom.createSVGMatrix().scale(2));
                expect(roundObject(scale)).toEqual({ sx: 2, sy: 2 });
                scale = dom_1.Dom.matrixToScale(dom_1.Dom.createSVGMatrix()
                    .translate(15, 15)
                    .scaleNonUniform(2, 3)
                    .rotate(10, 20));
                expect(roundObject(scale)).toEqual({ sx: 2, sy: 3 });
                scale = dom_1.Dom.matrixToScale(dom_1.Dom.createSVGMatrix().scale(2, 2).scale(3, 3));
                expect(roundObject(scale)).toEqual({ sx: 6, sy: 6 });
            });
        });
    });
});
//# sourceMappingURL=matrix.test.js.map