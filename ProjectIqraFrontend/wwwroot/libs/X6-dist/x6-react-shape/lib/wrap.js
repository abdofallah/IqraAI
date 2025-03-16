"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Wrap = void 0;
const react_1 = __importDefault(require("react"));
const registry_1 = require("./registry");
class Wrap extends react_1.default.PureComponent {
    constructor(props) {
        super(props);
        this.state = { tick: 0 };
    }
    componentDidMount() {
        const { node } = this.props;
        node.on('change:*', ({ key }) => {
            // eslint-disable-next-line react/no-access-state-in-setstate
            const content = registry_1.shapeMaps[node.shape];
            if (content) {
                const { effect } = content;
                if (!effect || effect.includes(key)) {
                    this.setState({ tick: this.state.tick + 1 });
                }
            }
        });
    }
    clone(elem) {
        const { node, graph } = this.props;
        return typeof elem.type === 'string'
            ? react_1.default.cloneElement(elem)
            : react_1.default.cloneElement(elem, { node, graph });
    }
    render() {
        const { node } = this.props;
        const content = registry_1.shapeMaps[node.shape];
        if (!content) {
            return null;
        }
        const { component } = content;
        if (react_1.default.isValidElement(component)) {
            return this.clone(component);
        }
        const FC = component;
        return this.clone(react_1.default.createElement(FC, null));
    }
}
exports.Wrap = Wrap;
//# sourceMappingURL=wrap.js.map