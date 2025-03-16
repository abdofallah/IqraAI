import { Cell } from '@antv/x6';
import { Clipboard } from './index';
declare module '@antv/x6/lib/graph/graph' {
    interface Graph {
        isClipboardEnabled: () => boolean;
        enableClipboard: () => Graph;
        disableClipboard: () => Graph;
        toggleClipboard: (enabled?: boolean) => Graph;
        isClipboardEmpty: (options?: Clipboard.Options) => boolean;
        getCellsInClipboard: () => Cell[];
        cleanClipboard: () => Graph;
        copy: (cells: Cell[], options?: Clipboard.CopyOptions) => Graph;
        cut: (cells: Cell[], options?: Clipboard.CopyOptions) => Graph;
        paste: (options?: Clipboard.PasteOptions, graph?: Graph) => Cell[];
    }
}
declare module '@antv/x6/lib/graph/events' {
    interface EventArgs {
        'clipboard:changed': {
            cells: Cell[];
        };
    }
}
