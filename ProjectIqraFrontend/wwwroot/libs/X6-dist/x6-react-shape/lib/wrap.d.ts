import React from 'react';
import { Graph } from '@antv/x6';
import { ReactShape } from './node';
export declare class Wrap extends React.PureComponent<Wrap.Props, Wrap.State> {
    constructor(props: Wrap.Props);
    componentDidMount(): void;
    clone(elem: React.ReactElement): React.ReactElement<any, string | React.JSXElementConstructor<any>>;
    render(): React.ReactElement<any, string | React.JSXElementConstructor<any>> | null;
}
export declare namespace Wrap {
    interface State {
        tick: number;
    }
    interface Props {
        node: ReactShape;
        graph: Graph;
    }
}
