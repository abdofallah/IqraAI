import { KeyValue } from '@antv/x6';
import { History } from './index';
declare module '@antv/x6/lib/graph/graph' {
    interface Graph {
        isHistoryEnabled: () => boolean;
        enableHistory: () => Graph;
        disableHistory: () => Graph;
        toggleHistory: (enabled?: boolean) => Graph;
        undo: (options?: KeyValue) => Graph;
        redo: (options?: KeyValue) => Graph;
        undoAndCancel: (options?: KeyValue) => Graph;
        canUndo: () => boolean;
        canRedo: () => boolean;
        getHistoryStackSize: () => number;
        getUndoStackSize: () => number;
        getRedoStackSize: () => number;
        getUndoRemainSize: () => number;
        cleanHistory: (options?: KeyValue) => Graph;
    }
}
declare module '@antv/x6/lib/graph/events' {
    interface EventArgs {
        'history:undo': History.Args;
        'history:redo': History.Args;
        'history:cancel': History.Args;
        'history:add': History.Args;
        'history:clean': History.Args<null>;
        'history:change': History.Args<null>;
        'history:batch': {
            cmd: History.Command;
            options: KeyValue;
        };
    }
}
