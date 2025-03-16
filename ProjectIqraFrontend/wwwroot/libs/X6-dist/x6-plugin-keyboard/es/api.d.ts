import { KeyboardImpl } from './keyboard';
declare module '@antv/x6/lib/graph/graph' {
    interface Graph {
        isKeyboardEnabled: () => boolean;
        enableKeyboard: () => Graph;
        disableKeyboard: () => Graph;
        toggleKeyboard: (enabled?: boolean) => Graph;
        bindKey: (keys: string | string[], callback: KeyboardImpl.Handler, action?: KeyboardImpl.Action) => Graph;
        unbindKey: (keys: string | string[], action?: KeyboardImpl.Action) => Graph;
        clearKeys: () => Graph;
        triggerKey: (key: string, action: KeyboardImpl.Action) => Graph;
    }
}
