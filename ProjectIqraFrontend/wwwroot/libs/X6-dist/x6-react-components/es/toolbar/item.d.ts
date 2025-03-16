import React from 'react';
import { TooltipProps } from 'antd/es/tooltip';
import { Dropdown } from '../dropdown';
export declare const ToolbarItem: React.FC<ToolbarItem.Props>;
export declare namespace ToolbarItem {
    interface Props {
        className?: string;
        name?: string;
        icon?: React.ReactNode;
        text?: string | React.ReactNode;
        hidden?: boolean;
        disabled?: boolean;
        active?: boolean;
        children?: React.ReactNode;
        tooltip?: string;
        tooltipProps?: TooltipProps;
        tooltipAsTitle?: boolean;
        dropdown?: any;
        dropdownArrow?: boolean;
        dropdownProps?: Dropdown.Props;
        onClick?: (name?: string) => void;
    }
}
