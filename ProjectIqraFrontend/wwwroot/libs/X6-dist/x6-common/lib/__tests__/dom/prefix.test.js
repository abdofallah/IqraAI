"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const dom_1 = require("../../dom");
describe('Dom', () => {
    describe('#prefix', () => {
        it('should return prefixed name with compatibility name', () => {
            expect(dom_1.Dom.getVendorPrefixedName('userDrag')).toBe('webkitUserDrag');
        });
    });
});
//# sourceMappingURL=prefix.test.js.map