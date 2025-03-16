export declare class WheelHandler {
    private deltaX;
    private deltaY;
    private callback;
    private shouldHandleScrollX;
    private shouldHandleScrollY;
    private stopPropagation;
    private animationFrameID;
    constructor(options: WheelHandler.Options);
    onWheel: (e: React.WheelEvent) => void;
    private didWheel;
}
export declare namespace WheelHandler {
    interface Options {
        onWheel: (deltaX: number, deltaY: number) => void;
        shouldHandleScrollX: boolean | ((deltaX: number, deltaY: number) => boolean);
        shouldHandleScrollY: boolean | ((deltaX: number, deltaY: number) => boolean);
        stopPropagation?: boolean | (() => boolean);
    }
}
