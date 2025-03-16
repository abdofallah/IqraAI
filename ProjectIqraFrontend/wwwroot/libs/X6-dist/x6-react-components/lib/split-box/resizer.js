"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Resizer = void 0;
const react_1 = __importDefault(require("react"));
const MouseMoveTracker_1 = require("../util/dom/MouseMoveTracker");
class Resizer extends react_1.default.PureComponent {
    constructor() {
        super(...arguments);
        this.onMouseDown = (e) => {
            this.mouseMoveTracker.capture(e);
            this.props.onMouseDown(e);
        };
        this.onMouseMove = (deltaX, deltaY, pos) => {
            if (this.props.onMouseMove != null) {
                this.props.onMouseMove(deltaX, deltaY, pos);
            }
        };
        this.onMouseMoveEnd = () => {
            this.mouseMoveTracker.release();
            if (this.props.onMouseMoveEnd != null) {
                this.props.onMouseMoveEnd();
            }
        };
        this.onClick = (e) => {
            if (this.props.onClick) {
                this.props.onClick(e);
            }
        };
        this.onDoubleClick = (e) => {
            if (this.props.onDoubleClick) {
                this.props.onDoubleClick(e);
            }
        };
    }
    UNSAFE_componentWillMount() {
        this.mouseMoveTracker = new MouseMoveTracker_1.MouseMoveTracker({
            onMouseMove: this.onMouseMove,
            onMouseMoveEnd: this.onMouseMoveEnd,
        });
    }
    componentWillUnmount() {
        this.mouseMoveTracker.release();
    }
    render() {
        const { className, style } = this.props;
        return (
        // eslint-disable-next-line
        react_1.default.createElement("div", { role: "button", style: style, className: className, onClick: this.onClick, ref: this.props.refIt, onMouseDown: this.onMouseDown, onDoubleClick: this.onDoubleClick }));
    }
}
exports.Resizer = Resizer;
//# sourceMappingURL=resizer.js.map