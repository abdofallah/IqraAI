"use strict";
var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                t[p[i]] = s[p[i]];
        }
    return t;
};
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.ContextMenu = void 0;
const react_1 = __importDefault(require("react"));
const dropdown_1 = require("../dropdown");
class ContextMenu extends react_1.default.PureComponent {
    render() {
        const _a = this.props, { children, menu, overlay } = _a, props = __rest(_a, ["children", "menu", "overlay"]);
        return (react_1.default.createElement(dropdown_1.Dropdown, Object.assign({}, props, { overlay: menu || overlay, trigger: "contextMenu" }), children));
    }
}
exports.ContextMenu = ContextMenu;
//# sourceMappingURL=index.js.map