"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.SplitBox = void 0;
const react_1 = __importDefault(require("react"));
const clamp_1 = __importDefault(require("clamp"));
const classnames_1 = __importDefault(require("classnames"));
const box_1 = require("./box");
const resizer_1 = require("./resizer");
class SplitBox extends react_1.default.PureComponent {
    constructor(props) {
        super(props);
        this.onMouseDown = () => {
            const { maxSize, minSize } = this.getRange();
            this.minSize = minSize;
            this.maxSize = maxSize;
            this.curSize = this.getPrimarySize();
            this.rawSize = this.curSize;
            this.resizing = true;
            this.createMask();
            this.updateCursor(this.curSize, minSize, maxSize);
        };
        this.onMouseMove = (deltaX, deltaY) => {
            if (this.props.resizable && this.resizing) {
                const delta = this.getDelta(deltaX, deltaY);
                if (delta === 0) {
                    return;
                }
                if (this.rawSize < this.minSize || this.rawSize > this.maxSize) {
                    this.rawSize -= delta;
                    return;
                }
                this.rawSize -= delta;
                this.curSize = this.rawSize;
                this.curSize = (0, clamp_1.default)(this.curSize, this.minSize, this.maxSize);
                this.setPrimarySize(this.curSize);
                this.updateCursor(this.curSize, this.minSize, this.maxSize);
                if (this.props.onResizing) {
                    this.props.onResizing(this.curSize);
                }
            }
        };
        this.onMouseMoveEnd = () => {
            if (this.props.resizable && this.resizing) {
                if (this.props.onResizeEnd) {
                    this.props.onResizeEnd(this.curSize);
                }
                if (this.props.refresh) {
                    const isPrimaryFirst = this.isPrimaryFirst();
                    this.setState({
                        box1Size: isPrimaryFirst ? this.curSize : undefined,
                        box2Size: isPrimaryFirst ? undefined : this.curSize,
                    });
                }
                this.resizing = false;
                this.removeMask();
            }
        };
        this.refContainer = (container) => {
            this.container = container;
        };
        this.refResizer = (elem) => {
            this.resizerElem = elem;
        };
        this.state = this.getNextState(props);
    }
    UNSAFE_componentWillReceiveProps(nextProps) {
        const nextState = this.getNextState(nextProps);
        this.setState(nextState);
    }
    getNextState(props) {
        const { size, defaultSize, primary } = props;
        const initialSize = size != null ? size : defaultSize != null ? defaultSize : '25%';
        return {
            box1Size: primary === 'first' ? initialSize : undefined,
            box2Size: primary === 'second' ? initialSize : undefined,
        };
    }
    isVertical() {
        return this.props.split === 'vertical';
    }
    isPrimaryFirst() {
        return this.props.primary === 'first';
    }
    getDelta(deltaX, deltaY) {
        const { step } = this.props;
        const isVertical = this.isVertical();
        const isPrimaryFirst = this.isPrimaryFirst();
        let delta = isVertical ? deltaX : deltaY;
        if (delta === 0) {
            return 0;
        }
        if (step && Math.abs(delta) >= step) {
            delta = ~~(delta / step) * step; // eslint-disable-line
        }
        delta = isPrimaryFirst ? -delta : delta;
        const primaryBox = isPrimaryFirst ? this.box1Elem : this.box2Elem;
        const secondBox = isPrimaryFirst ? this.box2Elem : this.box1Elem;
        const box1Order = parseInt(window.getComputedStyle(primaryBox).order, 10);
        const box2Order = parseInt(window.getComputedStyle(secondBox).order, 10);
        if (box1Order > box2Order) {
            delta = -delta;
        }
        return delta;
    }
    getRange() {
        const { maxSize, minSize } = this.props;
        const containerRect = this.container.getBoundingClientRect();
        const containerSize = this.isVertical()
            ? containerRect.width
            : containerRect.height;
        let newMinSize = minSize !== undefined ? minSize : 0;
        let newMaxSize = maxSize !== undefined ? maxSize : 0;
        while (newMinSize < 0) {
            newMinSize = containerSize + newMinSize;
        }
        while (newMaxSize <= 0) {
            newMaxSize = containerSize + newMaxSize;
        }
        return {
            minSize: (0, clamp_1.default)(newMinSize, 0, containerSize),
            maxSize: (0, clamp_1.default)(newMaxSize, 0, containerSize),
        };
    }
    getPrimarySize() {
        const primaryBox = this.isPrimaryFirst() ? this.box1Elem : this.box2Elem;
        return this.isVertical()
            ? primaryBox.getBoundingClientRect().width
            : primaryBox.getBoundingClientRect().height;
    }
    setPrimarySize(size) {
        const isFirstPrimary = this.isPrimaryFirst();
        const primaryBox = isFirstPrimary ? this.box1Elem : this.box2Elem;
        const secondBox = isFirstPrimary ? this.box2Elem : this.box1Elem;
        const resizerElem = this.resizerElem;
        const value = `${size}px`;
        if (this.isVertical()) {
            primaryBox.style.width = value;
            if (isFirstPrimary) {
                secondBox.style.left = value;
                resizerElem.style.left = value;
            }
            else {
                secondBox.style.right = value;
                resizerElem.style.right = value;
            }
        }
        else {
            primaryBox.style.height = value;
            if (isFirstPrimary) {
                secondBox.style.top = value;
                resizerElem.style.top = value;
            }
            else {
                secondBox.style.bottom = value;
                resizerElem.style.bottom = value;
            }
        }
    }
    updateCursor(size, minSize, maxSize) {
        let cursor = '';
        if (this.isVertical()) {
            if (size === minSize) {
                cursor = this.isPrimaryFirst() ? 'e-resize' : 'w-resize';
            }
            else if (size === maxSize) {
                cursor = this.isPrimaryFirst() ? 'w-resize' : 'e-resize';
            }
            else {
                cursor = 'col-resize';
            }
        }
        else if (size === minSize) {
            cursor = this.isPrimaryFirst() ? 's-resize' : 'n-resize';
        }
        else if (size === maxSize) {
            cursor = this.isPrimaryFirst() ? 'n-resize' : 's-resize';
        }
        else {
            cursor = 'row-resize';
        }
        this.maskElem.style.cursor = cursor;
    }
    createMask() {
        const mask = document.createElement('div');
        mask.style.position = 'absolute';
        mask.style.top = '0';
        mask.style.right = '0';
        mask.style.bottom = '0';
        mask.style.left = '0';
        mask.style.zIndex = '9999';
        document.body.appendChild(mask);
        this.maskElem = mask;
    }
    removeMask() {
        if (this.maskElem.parentNode) {
            this.maskElem.parentNode.removeChild(this.maskElem);
        }
    }
    renderBox(baseCls, index) {
        const primary = index === 1 ? 'first' : 'second';
        const isPrimary = this.props.primary === primary;
        const currentSize = index === 1 ? this.state.box1Size : this.state.box2Size;
        const oppositeSize = index === 1 ? this.state.box2Size : this.state.box1Size;
        const style = Object.assign(Object.assign({}, this.props.boxStyle), (isPrimary ? this.props.primaryBoxStyle : this.props.secondBoxStyle));
        const classes = (0, classnames_1.default)(`${baseCls}-item`, isPrimary ? `${baseCls}-item-primary` : `${baseCls}-item-second`);
        const ref = (elem) => {
            if (index === 1) {
                this.box1Elem = elem;
            }
            else {
                this.box2Elem = elem;
            }
        };
        const children = this.props.children;
        return (react_1.default.createElement(box_1.Box, { key: `box${index}`, refIt: ref, style: style, index: index, className: classes, currentSize: currentSize, oppositeSize: oppositeSize, vertical: this.isVertical(), isPrimary: isPrimary }, children[index - 1]));
    }
    renderResizer(baseCls) {
        const style = Object.assign({}, this.props.resizerStyle);
        style.position = 'absolute';
        style.zIndex = 2;
        if (this.isVertical()) {
            style.top = 0;
            style.bottom = 0;
            if (this.props.resizable === true) {
                style.cursor = 'col-resize';
            }
            if (this.isPrimaryFirst()) {
                style.left = this.state.box1Size;
            }
            else {
                style.right = this.state.box2Size;
            }
        }
        else {
            style.left = 0;
            style.right = 0;
            if (this.props.resizable === true) {
                style.cursor = 'row-resize';
            }
            if (this.isPrimaryFirst()) {
                style.top = this.state.box1Size;
            }
            else {
                style.bottom = this.state.box2Size;
            }
        }
        return (react_1.default.createElement(resizer_1.Resizer, { key: "resizer", style: style, className: `${baseCls}-resizer`, refIt: this.refResizer, onClick: this.props.onResizerClick, onMouseDown: this.onMouseDown, onMouseMove: this.onMouseMove, onMouseMoveEnd: this.onMouseMoveEnd, onDoubleClick: this.props.onResizerDoubleClick }));
    }
    render() {
        const style = Object.assign(Object.assign({}, this.props.style), { overflow: 'hidden', position: 'relative', width: '100%', height: '100%' });
        const baseCls = `${this.props.prefixCls}-split-box`;
        const classes = (0, classnames_1.default)(baseCls, `${baseCls}-${this.props.split}`, {
            [`${baseCls}-disabled`]: !this.props.resizable,
        });
        return (react_1.default.createElement("div", { style: style, className: classes, ref: this.refContainer },
            this.renderBox(baseCls, 1),
            this.renderResizer(baseCls),
            this.renderBox(baseCls, 2)));
    }
}
exports.SplitBox = SplitBox;
(function (SplitBox) {
    SplitBox.defaultProps = {
        resizable: true,
        split: 'vertical',
        primary: 'first',
        prefixCls: 'x6',
        defaultSize: '25%',
    };
})(SplitBox = exports.SplitBox || (exports.SplitBox = {}));
//# sourceMappingURL=splitbox.js.map