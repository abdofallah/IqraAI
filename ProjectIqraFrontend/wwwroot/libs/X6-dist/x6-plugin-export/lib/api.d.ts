import { Export } from './index';
declare module '@antv/x6/lib/graph/graph' {
    interface Graph {
        toSVG: (callback: Export.ToSVGCallback, options?: Export.ToSVGOptions) => void;
        toPNG: (callback: Export.ToSVGCallback, options?: Export.ToImageOptions) => void;
        toJPEG: (callback: Export.ToSVGCallback, options?: Export.ToImageOptions) => void;
        exportPNG: (fileName?: string, options?: Export.ToImageOptions) => void;
        exportJPEG: (fileName?: string, options?: Export.ToImageOptions) => void;
        exportSVG: (fileName?: string, options?: Export.ToSVGOptions) => void;
    }
}
