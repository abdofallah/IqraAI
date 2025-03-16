"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Box = void 0;
const react_1 = __importDefault(require("react"));
class Box extends react_1.default.PureComponent {
    render() {
        const { refIt, className, index, currentSize, oppositeSize, vertical } = this.props;
        const style = Object.assign(Object.assign({}, this.props.style), { overflow: 'hidden', position: 'absolute', zIndex: 1 });
        if (vertical) {
            style.bottom = 0;
            style.top = 0;
        }
        else {
            style.left = 0;
            style.right = 0;
        }
        if (currentSize != null) {
            if (vertical) {
                style.width = currentSize;
                if (index === 1) {
                    style.left = 0;
                }
                else {
                    style.right = 0;
                }
            }
            else {
                style.height = currentSize;
                if (index === 1) {
                    style.top = 0;
                }
                else {
                    style.bottom = 0;
                }
            }
        }
        else if (vertical) {
            if (index === 1) {
                style.left = 0;
                style.right = oppositeSize;
            }
            else {
                style.left = oppositeSize;
                style.right = 0;
            }
        }
        else if (index === 1) {
            style.top = 0;
            style.bottom = oppositeSize;
        }
        else {
            style.top = oppositeSize;
            style.bottom = 0;
        }
        return (react_1.default.createElement("div", { ref: refIt, style: style, className: className }, this.props.children));
    }
}
exports.Box = Box;
//# sourceMappingURL=box.js.map