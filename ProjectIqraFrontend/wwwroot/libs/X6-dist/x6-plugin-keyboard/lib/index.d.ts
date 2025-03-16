import { Disposable, Graph } from '@antv/x6';
import { KeyboardImpl } from './keyboard';
import './api';
export declare class Keyboard extends Disposable implements Graph.Plugin {
    name: string;
    private keyboardImpl;
    options: KeyboardImpl.Options;
    constructor(options?: KeyboardImpl.Options);
    init(graph: Graph): void;
    isEnabled(): boolean;
    enable(): void;
    disable(): void;
    toggleEnabled(enabled?: boolean): this;
    bindKey(keys: string | string[], callback: KeyboardImpl.Handler, action?: KeyboardImpl.Action): this;
    trigger(key: string, action?: KeyboardImpl.Action): this;
    clear(): this;
    unbindKey(keys: string | string[], action?: KeyboardImpl.Action): this;
    dispose(): void;
}
