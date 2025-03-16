"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const string_1 = require("../../string");
describe('string', () => {
    describe('#uuid', () => {
        it('should generate uuids with RFC-4122 format', () => {
            for (let i = 0; i < 10000; i += 1) {
                expect(string_1.StringExt.uuid()).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/);
            }
        });
    });
});
//# sourceMappingURL=uuid.test.js.map