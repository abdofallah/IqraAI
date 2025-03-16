/// <reference types="react" />
export declare class MouseMoveTracker {
    private elem;
    private clientX;
    private clientY;
    private deltaX;
    private deltaY;
    private dragging;
    private captured;
    private animationFrameID;
    private removeMouseMoveEvent;
    private removeMouseUpEvent;
    private onMouseMoveCallback;
    private onMouseMoveEndCallback;
    constructor(options: MouseMoveTracker.Options);
    capture(e: React.MouseEvent): void;
    release(): void;
    isDragging(): boolean;
    onMouseMove: (e: MouseEvent) => void;
    onMouseUp: () => void;
    triggerOnMouseMoveCallback: () => void;
    triggerOnMouseMoveEndCallback: (cancel: boolean) => void;
}
export declare namespace MouseMoveTracker {
    interface ClientPosition {
        clientX: number;
        clientY: number;
    }
    interface Options {
        elem?: HTMLElement;
        onMouseMove: (deltaX: number, deltaY: number, pos?: ClientPosition) => void;
        onMouseMoveEnd: (cancel: boolean) => void;
    }
}
