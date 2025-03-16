import { ModifierKey, Dom } from '@antv/x6-common';
import { Base } from './base';
export declare class PanningManager extends Base {
    private panning;
    private clientX;
    private clientY;
    private mousewheelHandle;
    private isSpaceKeyPressed;
    protected get widgetOptions(): PanningManager.Options;
    get pannable(): boolean;
    protected init(): void;
    protected startListening(): void;
    protected stopListening(): void;
    allowPanning(e: Dom.MouseDownEvent, strict?: boolean): boolean;
    protected startPanning(evt: Dom.MouseDownEvent): void;
    protected pan(evt: Dom.MouseMoveEvent): void;
    protected stopPanning(e: Dom.MouseUpEvent): void;
    protected updateClassName(): void;
    protected onMouseDown({ e }: {
        e: Dom.MouseDownEvent;
    }): void;
    protected onRightMouseDown(e: Dom.MouseDownEvent): void;
    protected onMouseWheel(e: WheelEvent, deltaX: number, deltaY: number): void;
    protected onSpaceKeyDown(e: Dom.KeyDownEvent): void;
    protected onSpaceKeyUp(e: Dom.KeyUpEvent): void;
    protected allowBlankMouseDown(e: Dom.MouseDownEvent): boolean | undefined;
    protected allowMouseWheel(e: WheelEvent): boolean | undefined;
    autoPanning(x: number, y: number): void;
    enablePanning(): void;
    disablePanning(): void;
    dispose(): void;
}
export declare namespace PanningManager {
    type EventType = 'leftMouseDown' | 'rightMouseDown' | 'mouseWheel' | 'mouseWheelDown';
    export interface Options {
        enabled?: boolean;
        modifiers?: string | Array<ModifierKey | 'space'> | null;
        eventTypes?: EventType[];
    }
    export {};
}
