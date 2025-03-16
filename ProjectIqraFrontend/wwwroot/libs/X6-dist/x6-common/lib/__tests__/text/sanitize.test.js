"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const text_1 = require("../../text");
describe('Text', () => {
    describe('#sanitize', () => {
        it('should sanitize text', () => {
            expect(text_1.Text.sanitize('hell o')).toBe('hell\u00A0o');
        });
    });
});
//# sourceMappingURL=sanitize.test.js.map