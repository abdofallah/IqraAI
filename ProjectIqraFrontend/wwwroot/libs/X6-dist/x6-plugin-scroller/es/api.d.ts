declare module '@antv/x6/lib/graph/graph' {
    interface Graph {
        lockScroller: () => Graph;
        unlockScroller: () => Graph;
        updateScroller: () => Graph;
        getScrollbarPosition: () => {
            left: number;
            top: number;
        };
        setScrollbarPosition: (left?: number, top?: number) => Graph;
    }
}
export {};
