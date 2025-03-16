"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.highlight = void 0;
const util_1 = require("./util");
function highlight(args = {}) {
    const color = (0, util_1.getString)(args.color, 'red');
    const blur = (0, util_1.getNumber)(args.blur, 0);
    const width = (0, util_1.getNumber)(args.width, 1);
    const opacity = (0, util_1.getNumber)(args.opacity, 1);
    return `
      <filter>
        <feFlood flood-color="${color}" flood-opacity="${opacity}" result="colored"/>
        <feMorphology result="morphed" in="SourceGraphic" operator="dilate" radius="${width}"/>
        <feComposite result="composed" in="colored" in2="morphed" operator="in"/>
        <feGaussianBlur result="blured" in="composed" stdDeviation="${blur}"/>
        <feBlend in="SourceGraphic" in2="blured" mode="normal"/>
      </filter>
    `.trim();
}
exports.highlight = highlight;
//# sourceMappingURL=highlight.js.map