import { StringExt } from '../../string';
describe('String', () => {
    describe('#getSpellingSuggestion', () => {
        it('should return the best suggestion', () => {
            let candidates = ['asurance', 'assurance', 'assurances'];
            expect(StringExt.getSpellingSuggestion('assurance', candidates, (candidate) => candidate)).toBe('asurance');
            candidates = ['aransue', 'assurance', 'assurances'];
            expect(StringExt.getSpellingSuggestion('assurance', candidates, (candidate) => candidate)).toBe('assurances');
        });
    });
});
//# sourceMappingURL=suggestion.test.js.map