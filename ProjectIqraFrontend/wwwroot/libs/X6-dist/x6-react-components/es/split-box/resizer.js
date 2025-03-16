import React from 'react';
import { MouseMoveTracker } from '../util/dom/MouseMoveTracker';
export class Resizer extends React.PureComponent {
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
        this.mouseMoveTracker = new MouseMoveTracker({
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
        React.createElement("div", { role: "button", style: style, className: className, onClick: this.onClick, ref: this.props.refIt, onMouseDown: this.onMouseDown, onDoubleClick: this.onDoubleClick }));
    }
}
//# sourceMappingURL=resizer.js.map