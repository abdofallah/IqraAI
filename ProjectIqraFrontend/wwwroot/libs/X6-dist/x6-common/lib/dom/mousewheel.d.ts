export declare class MouseWheelHandle {
    private target;
    private onWheelCallback;
    private onWheelGuard?;
    private animationFrameId;
    private deltaX;
    private deltaY;
    private eventName;
    constructor(target: HTMLElement | Document, onWheelCallback: MouseWheelHandle.OnWheelCallback, onWheelGuard?: MouseWheelHandle.OnWheelGuard);
    enable(): void;
    disable(): void;
    private onWheel;
    private didWheel;
}
export declare namespace MouseWheelHandle {
    type OnWheelGuard = (e: WheelEvent) => boolean;
    type OnWheelCallback = (e: WheelEvent, deltaX?: number, deltaY?: number) => void;
}
