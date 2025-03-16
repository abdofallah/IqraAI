import { Snapline } from './index';
declare module '@antv/x6/lib/graph/graph' {
    interface Graph {
        isSnaplineEnabled: () => boolean;
        enableSnapline: () => Graph;
        disableSnapline: () => Graph;
        toggleSnapline: (enabled?: boolean) => Graph;
        hideSnapline: () => Graph;
        setSnaplineFilter: (filter?: Snapline.Filter) => Graph;
        isSnaplineOnResizingEnabled: () => boolean;
        enableSnaplineOnResizing: () => Graph;
        disableSnaplineOnResizing: () => Graph;
        toggleSnaplineOnResizing: (enableOnResizing?: boolean) => Graph;
        isSharpSnapline: () => boolean;
        enableSharpSnapline: () => Graph;
        disableSharpSnapline: () => Graph;
        toggleSharpSnapline: (sharp?: boolean) => Graph;
        getSnaplineTolerance: () => number | undefined;
        setSnaplineTolerance: (tolerance: number) => Graph;
    }
}
