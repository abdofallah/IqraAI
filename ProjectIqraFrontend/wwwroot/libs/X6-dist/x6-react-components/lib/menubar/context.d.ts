import React from 'react';
export declare const MenubarContext: React.Context<MenubarContext.Contexts>;
export declare namespace MenubarContext {
    interface Contexts {
        prefixCls: string;
        activeMenubar: () => void;
        menubarActived: boolean;
    }
}
