"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const __1 = require("../..");
describe('Unit', () => {
    describe('#toPx', () => {
        it('should return correct px', () => {
            expect(Math.floor(__1.Unit.toPx(10, 'mm'))).toBe(37);
            expect(Math.floor(__1.Unit.toPx(10, 'pt'))).toBe(13);
            expect(Math.floor(__1.Unit.toPx(10, 'pc'))).toBe(159);
        });
    });
});
//# sourceMappingURL=index.test.js.map