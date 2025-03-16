import { Size } from '../types';
import { Text } from '../text';
export interface TextOptions {
    /** Should we allow the text to be selected? */
    displayEmpty?: boolean;
    /** End of Line character */
    eol?: string;
    textPath?: string | {
        d?: string;
        'xlink:href'?: string;
    };
    textVerticalAnchor?: 'middle' | 'bottom' | 'top' | number;
    x?: number | string;
    /** auto, 1.25em */
    lineHeight?: string;
    includeAnnotationIndices?: boolean;
    annotations?: Text.Annotation | Text.Annotation[];
}
export declare function text(elem: SVGElement, content: string, options?: TextOptions): void;
export declare function measureText(text: string, styles?: any): TextMetrics | {
    width: number;
};
export declare function splitTextByLength(text: string, splitWidth: number, totalWidth: number, style?: any): string[];
export declare function breakText(text: string, size: Size, styles?: any, options?: {
    ellipsis?: string;
    eol?: string;
}): string;
