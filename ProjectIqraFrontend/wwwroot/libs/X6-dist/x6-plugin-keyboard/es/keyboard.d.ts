import Mousetrap from 'mousetrap';
import { Dom, Disposable, IDisablable, Graph } from '@antv/x6';
export declare class KeyboardImpl extends Disposable implements IDisablable {
    private readonly options;
    readonly target: HTMLElement | Document;
    private readonly container;
    private readonly mousetrap;
    private get graph();
    constructor(options: KeyboardImpl.Options & {
        graph: Graph;
    });
    get disabled(): boolean;
    enable(): void;
    disable(): void;
    on(keys: string | string[], callback: KeyboardImpl.Handler, action?: KeyboardImpl.Action): void;
    off(keys: string | string[], action?: KeyboardImpl.Action): void;
    clear(): void;
    trigger(key: string, action?: KeyboardImpl.Action): void;
    private focus;
    private getKeys;
    protected formatkey(key: string): string;
    protected isGraphEvent(e: KeyboardEvent): boolean;
    isInputEvent(e: KeyboardEvent | Dom.MouseUpEvent): boolean;
    isEnabledForEvent(e: KeyboardEvent): boolean;
    dispose(): void;
}
export declare namespace KeyboardImpl {
    type Action = 'keypress' | 'keydown' | 'keyup';
    type Handler = (e: KeyboardEvent) => void;
    interface Options {
        enabled?: boolean;
        /**
         * Specifies if keyboard event should bind on docuemnt or on container.
         *
         * Default is `false` that will bind keyboard event on the container.
         */
        global?: boolean;
        format?: (this: Graph, key: string) => string;
        guard?: (this: Graph, e: KeyboardEvent) => boolean;
    }
}
export declare namespace KeyboardImpl {
    function createMousetrap(keyboard: KeyboardImpl): Mousetrap.MousetrapInstance;
}
