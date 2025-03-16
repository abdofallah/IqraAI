"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.dropShadow = void 0;
const util_1 = require("./util");
function dropShadow(args = {}) {
    const dx = (0, util_1.getNumber)(args.dx, 0);
    const dy = (0, util_1.getNumber)(args.dy, 0);
    const color = (0, util_1.getString)(args.color, 'black');
    const blur = (0, util_1.getNumber)(args.blur, 4);
    const opacity = (0, util_1.getNumber)(args.opacity, 1);
    return 'SVGFEDropShadowElement' in window
        ? `<filter>
         <feDropShadow stdDeviation="${blur}" dx="${dx}" dy="${dy}" flood-color="${color}" flood-opacity="${opacity}" />
       </filter>`.trim()
        : `<filter>
         <feGaussianBlur in="SourceAlpha" stdDeviation="${blur}" />
         <feOffset dx="${dx}" dy="${dy}" result="offsetblur" />
         <feFlood flood-color="${color}" />
         <feComposite in2="offsetblur" operator="in" />
         <feComponentTransfer>
           <feFuncA type="linear" slope="${opacity}" />
         </feComponentTransfer>
         <feMerge>
           <feMergeNode/>
           <feMergeNode in="SourceGraphic"/>
         </feMerge>
       </filter>`.trim();
}
exports.dropShadow = dropShadow;
//# sourceMappingURL=drop-shadow.js.map