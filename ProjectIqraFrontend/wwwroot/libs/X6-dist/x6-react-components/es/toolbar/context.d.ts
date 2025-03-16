import React from 'react';
export declare const ToolbarContext: React.Context<ToolbarContext.Contexts>;
export declare namespace ToolbarContext {
    interface Contexts {
        prefixCls: string;
        onClick: (key: string, value?: any) => void;
    }
}
