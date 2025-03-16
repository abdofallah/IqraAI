import { NumberExt, Rectangle, Size, Basecoat, Graph } from '@antv/x6';
import './api';
export declare class Export extends Basecoat<Export.EventArgs> implements Graph.Plugin {
    name: string;
    private graph;
    constructor();
    get view(): Graph.View;
    init(graph: Graph): void;
    exportPNG(fileName?: string, options?: Export.ToImageOptions): void;
    exportJPEG(fileName?: string, options?: Export.ToImageOptions): void;
    exportSVG(fileName?: string, options?: Export.ToSVGOptions): void;
    toSVG(callback: Export.ToSVGCallback, options?: Export.ToSVGOptions): void;
    toDataURL(callback: Export.ToSVGCallback, options: Export.ToDataURLOptions): void;
    toPNG(callback: Export.ToSVGCallback, options?: Export.ToImageOptions): void;
    toJPEG(callback: Export.ToSVGCallback, options?: Export.ToImageOptions): void;
    protected notify<K extends keyof Export.EventArgs>(name: K, args: Export.EventArgs[K]): void;
    dispose(): void;
}
export declare namespace Export {
    interface EventArgs {
        'before:export': Export.ToSVGOptions;
        'after:export': Export.ToSVGOptions;
    }
    type ToSVGCallback = (dataUri: string) => any;
    interface ToSVGOptions {
        /**
         * By default, the resulting SVG has set width and height to `100%`.
         * If you'd like to have the dimensions to be set to the actual content
         * width and height, set `preserveDimensions` to `true`. An object with
         * `width` and `height` properties can be also used here if you need to
         * define the export size explicitely.
         */
        preserveDimensions?: boolean | Size;
        viewBox?: Rectangle.RectangleLike;
        /**
         * When set to `true` all the styles from external stylesheets are copied
         * to the resulting SVG export. Note this requires a lot of computations
         * and it might significantly affect the export time.
         */
        copyStyles?: boolean;
        stylesheet?: string;
        /**
         * Converts all contained images into Data URI format.
         */
        serializeImages?: boolean;
        /**
         * A function called before the XML serialization. It may be used to
         * modify the exported SVG before it is converted to a string. The
         * function can also return a new SVGDocument.
         */
        beforeSerialize?: (this: Graph, svg: SVGSVGElement) => any;
    }
    interface ToImageOptions extends ToSVGOptions {
        /**
         * The width of the image in pixels.
         */
        width?: number;
        /**
         * The height of the image in pixels.
         */
        height?: number;
        ratio?: string;
        backgroundColor?: string;
        padding?: NumberExt.SideOptions;
        quality?: number;
    }
    interface ToDataURLOptions extends ToImageOptions {
        type: 'image/png' | 'image/jpeg';
    }
}
