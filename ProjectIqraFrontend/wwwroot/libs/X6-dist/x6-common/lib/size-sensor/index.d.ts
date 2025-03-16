import { Listener } from './sensors/types';
export declare namespace SizeSensor {
    const bind: (element: Element, cb: Listener) => () => void;
    const clear: (element: Element) => void;
}
