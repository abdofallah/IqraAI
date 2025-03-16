import React from 'react';
import { MouseMoveTracker } from '../util/dom/MouseMoveTracker';
export declare class Resizer extends React.PureComponent<Resizer.Props> {
    private mouseMoveTracker;
    UNSAFE_componentWillMount(): void;
    componentWillUnmount(): void;
    onMouseDown: (e: React.MouseEvent) => void;
    onMouseMove: (deltaX: number, deltaY: number, pos: MouseMoveTracker.ClientPosition) => void;
    onMouseMoveEnd: () => void;
    onClick: (e: React.MouseEvent) => void;
    onDoubleClick: (e: React.MouseEvent) => void;
    render(): React.JSX.Element;
}
export declare namespace Resizer {
    interface Props {
        className?: string;
        style?: React.CSSProperties;
        refIt: React.LegacyRef<HTMLDivElement>;
        onClick?: (e: React.MouseEvent) => void;
        onDoubleClick?: (e: React.MouseEvent) => void;
        onMouseDown: (e: React.MouseEvent) => void;
        onMouseMove?: (deltaX: number, deltaY: number, pos: MouseMoveTracker.ClientPosition) => void;
        onMouseMoveEnd?: () => void;
    }
}
