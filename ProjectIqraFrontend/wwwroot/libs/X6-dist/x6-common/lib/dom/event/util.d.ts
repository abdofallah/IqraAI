import { Store } from './store';
import { EventObject } from './object';
export declare namespace Util {
    const returnTrue: () => boolean;
    const returnFalse: () => boolean;
    function stopPropagationCallback(e: Event): void;
    function addEventListener<TElement extends Element>(elem: TElement, type: string, handler: EventListener): void;
    function removeEventListener<TElement extends Element>(elem: TElement, type: string, handler: EventListener): void;
}
export declare namespace Util {
    function splitType(types: string): RegExpMatchArray;
    function normalizeType(type: string): {
        originType: string;
        namespaces: string[];
    };
    function isValidTarget(target: Element | Record<string, any>): boolean;
    function isValidSelector(elem: Store.EventTarget, selector?: string): boolean;
}
export declare namespace Util {
    type Handler = (...args: any[]) => void;
    export function ensureHandlerId(handler: Handler): number;
    export function getHandlerId(handler: Handler): number | undefined;
    export function removeHandlerId(handler: Handler): boolean;
    export function setHandlerId(handler: Handler, id: number): WeakMap<Handler, number>;
    export {};
}
export declare namespace Util {
    function getHandlerQueue(elem: Store.EventTarget, event: EventObject): ({
        elem: Node;
        handlers: Store.HandlerObject[];
    } | {
        elem: Store.EventTarget;
        handlers: Store.HandlerObject[];
    })[];
}
export declare namespace Util {
    function isWindow(obj: any): obj is Window;
}
export declare namespace Util {
    function contains(a: any, b: any): boolean;
}
