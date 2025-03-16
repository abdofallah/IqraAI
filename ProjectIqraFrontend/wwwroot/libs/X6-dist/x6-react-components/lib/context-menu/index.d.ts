import React, { PropsWithChildren } from 'react';
import { Dropdown } from '../dropdown';
export declare class ContextMenu extends React.PureComponent<PropsWithChildren<ContextMenu.Props>> {
    render(): React.JSX.Element;
}
export declare namespace ContextMenu {
    interface Props extends Dropdown.Props {
        menu: string | React.ReactNode;
    }
}
