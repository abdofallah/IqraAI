import React from 'react';
export declare namespace Portal {
    function connect(id: string, portal: React.ReactPortal): void;
    function disconnect(id: string): void;
    function isActive(): boolean;
    function getProvider(): () => React.FunctionComponentElement<{
        children?: React.ReactNode;
    }>;
}
