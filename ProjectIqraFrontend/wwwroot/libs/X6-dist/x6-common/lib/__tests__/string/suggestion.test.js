"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const string_1 = require("../../string");
describe('String', () => {
    describe('#getSpellingSuggestion', () => {
        it('should return the best suggestion', () => {
            let candidates = ['asurance', 'assurance', 'assurances'];
            expect(string_1.StringExt.getSpellingSuggestion('assurance', candidates, (candidate) => candidate)).toBe('asurance');
            candidates = ['aransue', 'assurance', 'assurances'];
            expect(string_1.StringExt.getSpellingSuggestion('assurance', candidates, (candidate) => candidate)).toBe('assurances');
        });
    });
});
//# sourceMappingURL=suggestion.test.js.map