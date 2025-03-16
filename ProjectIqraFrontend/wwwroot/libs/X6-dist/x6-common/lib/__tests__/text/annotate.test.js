"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const text_1 = require("../../text");
describe('Text', () => {
    describe('#annotate', () => {
        it('should cut string into pieces and attributed according to the spans', () => {
            const annotations = text_1.Text.annotate('This is a text that goes on multiple lines.', [
                { start: 2, end: 5, attrs: { fill: 'red' } },
                { start: 4, end: 8, attrs: { fill: 'blue' } },
            ]);
            expect(annotations).toEqual([
                'Th',
                { t: 'is', attrs: { fill: 'red' } },
                { t: ' is ', attrs: { fill: 'blue' } },
                'a text that goes on multiple lines.',
            ]);
        });
        it('should cut string into pieces and attributed according to the annotations including concatenated classes', () => {
            const annotations = text_1.Text.annotate('abcdefgh', [
                { start: 1, end: 3, attrs: { class: 'one' } },
                { start: 2, end: 5, attrs: { class: 'two', fill: 'blue' } },
            ]);
            expect(annotations).toEqual([
                'a',
                { t: 'b', attrs: { class: 'one' } },
                { t: 'c', attrs: { class: 'one two', fill: 'blue' } },
                { t: 'de', attrs: { class: 'two', fill: 'blue' } },
                'fgh',
            ]);
        });
        it('should cut string into pieces and attributed according to the annotations including concatenated classes', () => {
            const annotations = text_1.Text.annotate('abcdefgh', [
                { start: 1, end: 3, attrs: { class: 'one' } },
                { start: 2, end: 5, attrs: { class: 'two', fill: 'blue' } },
            ], { includeAnnotationIndices: true });
            expect(annotations).toEqual([
                'a',
                { t: 'b', attrs: { class: 'one' }, annotations: [0] },
                {
                    t: 'c',
                    attrs: { class: 'one two', fill: 'blue' },
                    annotations: [0, 1],
                },
                { t: 'de', attrs: { class: 'two', fill: 'blue' }, annotations: [1] },
                'fgh',
            ]);
        });
    });
    describe('#shiftAnnotations', () => {
        it('should add offset when position less than index', () => {
            expect(text_1.Text.shiftAnnotations([
                {
                    start: 1,
                    end: 8,
                    attrs: {},
                },
                {
                    start: 8,
                    end: 8,
                    attrs: {},
                },
            ], 5, 2)).toEqual([
                {
                    start: 1,
                    end: 10,
                    attrs: {},
                },
                {
                    start: 10,
                    end: 10,
                    attrs: {},
                },
            ]);
        });
    });
});
//# sourceMappingURL=annotate.test.js.map