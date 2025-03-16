import { EventHandler } from './types';
export declare namespace Store {
    type EventTarget = Element | Record<string, unknown>;
    interface HandlerObject {
        guid: number;
        type: string;
        originType: string;
        handler: EventHandler<any, any>;
        data?: any;
        selector?: string;
        namespace?: string;
    }
    interface Data {
        handler?: EventHandler<any, any>;
        events: {
            [type: string]: {
                handlers: HandlerObject[];
                delegateCount: number;
            };
        };
    }
    function ensure(target: EventTarget): Data;
    function get(target: EventTarget): Data | undefined;
    function remove(target: EventTarget): boolean;
}
