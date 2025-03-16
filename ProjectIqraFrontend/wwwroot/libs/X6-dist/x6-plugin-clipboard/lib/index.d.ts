import { Cell, Graph, Basecoat } from '@antv/x6';
import { ClipboardImpl } from './clipboard';
import './api';
export declare class Clipboard extends Basecoat<Clipboard.EventArgs> implements Graph.Plugin {
    name: string;
    private clipboardImpl;
    private graph;
    options: Clipboard.Options;
    get disabled(): boolean;
    get cells(): Cell<Cell.Properties>[];
    constructor(options?: Clipboard.Options);
    init(graph: Graph): void;
    isEnabled(): boolean;
    enable(): void;
    disable(): void;
    toggleEnabled(enabled?: boolean): this;
    isEmpty(options?: Clipboard.Options): boolean;
    getCellsInClipboard(): Cell<Cell.Properties>[];
    clean(force?: boolean): this;
    copy(cells: Cell[], options?: Clipboard.CopyOptions): this;
    cut(cells: Cell[], options?: Clipboard.CopyOptions): this;
    paste(options?: Clipboard.PasteOptions, graph?: Graph): Cell<Cell.Properties>[];
    protected get commonOptions(): {
        useLocalStorage?: boolean | undefined;
    };
    protected notify<K extends keyof Clipboard.EventArgs>(name: K, args: Clipboard.EventArgs[K]): void;
    dispose(): void;
}
export declare namespace Clipboard {
    interface EventArgs {
        'clipboard:changed': {
            cells: Cell[];
        };
    }
    interface Options extends ClipboardImpl.Options {
        enabled?: boolean;
    }
    interface CopyOptions extends ClipboardImpl.CopyOptions {
    }
    interface PasteOptions extends ClipboardImpl.PasteOptions {
    }
}
