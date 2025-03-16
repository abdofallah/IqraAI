import React from 'react';
import { PopoverProps } from 'antd/es/popover';
import { SketchPickerProps, RGBColor, ColorResult } from 'react-color';
export { ColorResult } from 'react-color';
export declare class ColorPicker extends React.Component<ColorPicker.Props, ColorPicker.State> {
    private removeDocClickEvent;
    private container;
    constructor(props: ColorPicker.Props);
    componentWillUnmount(): void;
    onDocumentClick: (e: React.MouseEvent) => void;
    unbindDocEvent(): void;
    handleChange: (value: ColorResult, event: React.ChangeEvent<HTMLInputElement>) => void;
    handleClick: (e: React.MouseEvent) => void;
    renderPicker(): React.JSX.Element;
    render(): React.JSX.Element;
}
export declare namespace ColorPicker {
    interface Props extends SketchPickerProps {
        prefixCls?: string;
        disabled?: boolean;
        overlayProps?: PopoverProps;
        style?: React.CSSProperties;
        color: string | RGBColor;
    }
    interface State {
        active: boolean;
        color: string | RGBColor;
    }
    const defaultProps: Props;
}
