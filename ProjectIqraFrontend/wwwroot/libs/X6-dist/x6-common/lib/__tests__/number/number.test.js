"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const number_1 = require("../../number");
describe('NumberExt', () => {
    describe('#mod', () => {
        it('should work with positive numbers', () => {
            expect(number_1.NumberExt.mod(12, 5)).toEqual(2);
        });
        it('should work with negative numbers', () => {
            expect(number_1.NumberExt.mod(-12, 5)).toEqual(3);
            expect(number_1.NumberExt.mod(12, -5)).toEqual(-3);
            expect(number_1.NumberExt.mod(-12, -5)).toEqual(-2);
        });
    });
    describe('#random', () => {
        it('should return random number between lower and upper', () => {
            const rnd = number_1.NumberExt.random(2, 5);
            expect(rnd).toBeGreaterThanOrEqual(2);
            expect(rnd).toBeLessThanOrEqual(5);
        });
        it('should automatically reverses the order of parameters', () => {
            const rnd = number_1.NumberExt.random(5, 2);
            expect(rnd).toBeGreaterThanOrEqual(2);
            expect(rnd).toBeLessThanOrEqual(5);
        });
    });
    describe('#parseCssNumeric', () => {
        it('should work with valid params', () => {
            expect(number_1.NumberExt.parseCssNumeric('12px')).toEqual({
                unit: 'px',
                value: 12,
            });
            expect(number_1.NumberExt.parseCssNumeric('12px', ['px'])).toEqual({
                unit: 'px',
                value: 12,
            });
            expect(number_1.NumberExt.parseCssNumeric('12px', 'px')).toEqual({
                unit: 'px',
                value: 12,
            });
        });
        it('should return null with invalid params', () => {
            expect(number_1.NumberExt.parseCssNumeric('abc')).toBe(null);
            expect(number_1.NumberExt.parseCssNumeric('12px', [])).toBe(null);
        });
    });
    describe('#normalizePercentage', () => {
        it('should return 0 when input is invalid', () => {
            expect(number_1.NumberExt.normalizePercentage(null, 1)).toBe(0);
            expect(number_1.NumberExt.normalizePercentage(Infinity, 1)).toBe(0);
        });
        it('should work with valid input', () => {
            expect(number_1.NumberExt.normalizePercentage('500%', 1)).toBe(5);
            expect(number_1.NumberExt.normalizePercentage(0.1, 10)).toBe(1);
        });
    });
    describe('#normalizeSides', () => {
        it('should return the same property', () => {
            expect(number_1.NumberExt.normalizeSides(10)).toEqual({
                top: 10,
                right: 10,
                bottom: 10,
                left: 10,
            });
        });
        it('should work with object', () => {
            let slides = {
                left: 0,
                right: 10,
                top: 20,
                bottom: 30,
            };
            expect(number_1.NumberExt.normalizeSides(slides)).toEqual(slides);
            slides = {
                left: 0,
                right: 10,
                vertical: 20,
            };
            expect(number_1.NumberExt.normalizeSides(slides)).toEqual({
                left: 0,
                right: 10,
                top: 20,
                bottom: 20,
            });
            slides = {
                horizontal: 30,
                vertical: 20,
            };
            expect(number_1.NumberExt.normalizeSides(slides)).toEqual({
                left: 30,
                right: 30,
                top: 20,
                bottom: 20,
            });
        });
    });
});
//# sourceMappingURL=number.test.js.map