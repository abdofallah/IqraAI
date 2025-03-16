"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.outline = void 0;
const util_1 = require("./util");
function outline(args = {}) {
    const color = (0, util_1.getString)(args.color, 'blue');
    const width = (0, util_1.getNumber)(args.width, 1);
    const margin = (0, util_1.getNumber)(args.margin, 2);
    const opacity = (0, util_1.getNumber)(args.opacity, 1);
    const innerRadius = margin;
    const outerRadius = margin + width;
    return `
    <filter>
      <feFlood flood-color="${color}" flood-opacity="${opacity}" result="colored"/>
      <feMorphology in="SourceAlpha" result="morphedOuter" operator="dilate" radius="${outerRadius}" />
      <feMorphology in="SourceAlpha" result="morphedInner" operator="dilate" radius="${innerRadius}" />
      <feComposite result="morphedOuterColored" in="colored" in2="morphedOuter" operator="in"/>
      <feComposite operator="xor" in="morphedOuterColored" in2="morphedInner" result="outline"/>
      <feMerge>
        <feMergeNode in="outline"/>
        <feMergeNode in="SourceGraphic"/>
      </feMerge>
    </filter>
  `.trim();
}
exports.outline = outline;
//# sourceMappingURL=outline.js.map