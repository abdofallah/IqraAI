import React, { MouseEvent } from 'react';
export declare const MenuContext: React.Context<MenuContext.Contexts>;
export declare namespace MenuContext {
    interface Contexts {
        prefixCls: string;
        onClick: (name: string, e?: MouseEvent) => void;
        registerHotkey: (hotkey: string, handler: () => any) => void;
        unregisterHotkey: (hotkey: string, handler: () => any) => void;
    }
}
