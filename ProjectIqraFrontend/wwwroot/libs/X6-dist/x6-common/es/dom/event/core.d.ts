import { Store } from './store';
import { EventObject } from './object';
import { EventHandler } from './types';
import './special';
export declare namespace Core {
    function on(elem: Store.EventTarget, types: string, handler: EventHandler<any, any> | ({
        handler: EventHandler<any, any>;
        selector?: string;
    } & Partial<Store.HandlerObject>), data?: any, selector?: string): void;
    function off(elem: Store.EventTarget, types: string, handler?: EventHandler<any, any>, selector?: string, mappedTypes?: boolean): void;
    function dispatch(elem: Store.EventTarget, evt: Event | EventObject | string, ...args: any[]): any;
    function trigger(event: (Partial<EventObject.Event> & {
        type: string;
    }) | EventObject | string, eventArgs: any, elem: Store.EventTarget, onlyHandlers?: boolean): any;
}
