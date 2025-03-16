import { Store } from './store';
import { EventRaw } from './alias';
export declare class EventObject<TDelegateTarget = any, TData = any, TCurrentTarget = any, TTarget = any, TEvent extends Event = Event> implements EventObject.Event {
    isDefaultPrevented: () => boolean;
    isPropagationStopped: () => boolean;
    isImmediatePropagationStopped: () => boolean;
    type: string;
    originalEvent: TEvent;
    target: TTarget | null;
    currentTarget: TCurrentTarget | null;
    delegateTarget: TDelegateTarget | null;
    relatedTarget?: EventTarget | null;
    data: TData;
    result: any;
    timeStamp: number;
    handleObj: Store.HandlerObject;
    namespace?: string;
    rnamespace?: RegExp | null;
    isSimulated: boolean;
    constructor(e: TEvent | string, props?: Record<string, any> | null);
    preventDefault: () => void;
    stopPropagation: () => void;
    stopImmediatePropagation: () => void;
}
export interface EventObject extends EventObject.Event {
}
export declare namespace EventObject {
    function create(originalEvent: EventRaw | EventObject | string): EventObject<any, any, any, any, globalThis.Event>;
}
export declare namespace EventObject {
    function addProperty(name: string, hook?: any | ((e: EventRaw) => any)): void;
}
export declare namespace EventObject {
}
export declare namespace EventObject {
    interface Event {
        bubbles: boolean | undefined;
        cancelable: boolean | undefined;
        eventPhase: number | undefined;
        detail: number | undefined;
        view: Window | undefined;
        button: number | undefined;
        buttons: number | undefined;
        clientX: number | undefined;
        clientY: number | undefined;
        offsetX: number | undefined;
        offsetY: number | undefined;
        pageX: number | undefined;
        pageY: number | undefined;
        screenX: number | undefined;
        screenY: number | undefined;
        /** @deprecated */
        toElement: Element | undefined;
        pointerId: number | undefined;
        pointerType: string | undefined;
        /** @deprecated */
        char: string | undefined;
        /** @deprecated */
        charCode: number | undefined;
        key: string | undefined;
        /** @deprecated */
        keyCode: number | undefined;
        touches: TouchList | undefined;
        targetTouches: TouchList | undefined;
        changedTouches: TouchList | undefined;
        which: number | undefined;
        altKey: boolean | undefined;
        ctrlKey: boolean | undefined;
        metaKey: boolean | undefined;
        shiftKey: boolean | undefined;
        type: string;
        timeStamp: number;
        isDefaultPrevented(): boolean;
        isImmediatePropagationStopped(): boolean;
        isPropagationStopped(): boolean;
        preventDefault(): void;
        stopImmediatePropagation(): void;
        stopPropagation(): void;
    }
}
