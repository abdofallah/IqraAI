"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const dom_1 = require("../../dom");
describe('Dom', () => {
    describe('#setPrefixedStyle', () => {
        it('should return prefixed attr', () => {
            const style = {};
            dom_1.Dom.setPrefixedStyle(style, 'userDrag', 'true');
            expect(style).toEqual({
                userDrag: 'true',
                webkitUserDrag: 'true',
            });
        });
        describe('#hasScrollbars', () => {
            const container = document.createElement('div');
            beforeAll(() => {
                container.style.overflow = 'auto';
                document.body.appendChild(container);
            });
            afterAll(() => {
                document.body.removeChild(container);
            });
            it('should return true with an elem has scrollbar', () => {
                expect(dom_1.Dom.hasScrollbars(container)).toBeTruthy();
                container.style.overflow = 'scroll';
                expect(dom_1.Dom.hasScrollbars(container)).toBeTruthy();
                container.style.overflow = 'hidden';
                expect(dom_1.Dom.hasScrollbars(container)).toBeFalsy();
            });
        });
    });
});
//# sourceMappingURL=style.test.js.map