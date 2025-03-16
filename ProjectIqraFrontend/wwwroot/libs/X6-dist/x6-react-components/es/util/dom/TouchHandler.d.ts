export declare class TouchHandler {
    private deltaX;
    private deltaY;
    private lastTouchX;
    private lastTouchY;
    private velocityX;
    private velocityY;
    private accumulatedDeltaX;
    private accumulatedDeltaY;
    private lastFrameTimestamp;
    private autoScrollTimestamp;
    private trackerId;
    private dragAnimationId;
    private handleScrollX;
    private handleScrollY;
    private callback;
    private stopPropagation;
    constructor(options: TouchHandler.Options);
    onTouchStart(e: TouchEvent): void;
    onTouchEnd(e: TouchEvent): void;
    onTouchCancel(e: TouchEvent): void;
    onTouchMove(e: TouchEvent): void;
    didTouchMove: () => void;
    track: () => void;
    startAutoScroll: () => void;
    autoScroll: () => void;
}
export declare namespace TouchHandler {
    const MOVE_AMPLITUDE = 1.6;
    const DECELERATION_AMPLITUDE = 1.6;
    const DECELERATION_FACTOR = 325;
    const TRACKER_TIMEOUT = 100;
    interface Options {
        onTouchScroll: (deltaX: number, deltaY: number) => void;
        shouldHandleScrollX: boolean | ((deltaX: number, deltaY: number) => boolean);
        shouldHandleScrollY: boolean | ((deltaX: number, deltaY: number) => boolean);
        stopPropagation?: boolean | (() => boolean);
    }
}
