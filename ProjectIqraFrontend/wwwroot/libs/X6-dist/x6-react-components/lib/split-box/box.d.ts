import React, { PropsWithChildren } from 'react';
export declare class Box extends React.PureComponent<PropsWithChildren<Box.Props>> {
    render(): React.JSX.Element;
}
export declare namespace Box {
    interface Props {
        style?: React.CSSProperties;
        className?: string;
        currentSize?: number | string;
        oppositeSize?: number | string;
        index: 1 | 2;
        vertical: boolean;
        isPrimary: boolean;
        refIt: React.LegacyRef<HTMLDivElement>;
    }
}
