import { Dom, Nilable } from '@antv/x6-common';
import { Rectangle } from '@antv/x6-geometry';
import { Graph } from '../graph';
import { GridManager } from './grid';
import { BackgroundManager } from './background';
import { PanningManager } from './panning';
import { MouseWheel } from './mousewheel';
import { Model, Cell, Node, Edge } from '../model';
import { CellView, NodeView, EdgeView, Markup } from '../view';
import { Router, Connector, NodeAnchor, EdgeAnchor, ConnectionPoint } from '../registry';
import { HighlightManager } from './highlight';
import { PortManager } from '../model/port';
export declare namespace Options {
    interface Common {
        container: HTMLElement;
        model?: Model;
        x: number;
        y: number;
        width: number;
        height: number;
        autoResize?: boolean | Element | Document;
        background?: false | BackgroundManager.Options;
        scaling: {
            min?: number;
            max?: number;
        };
        moveThreshold: number;
        clickThreshold: number;
        magnetThreshold: number | 'onleave';
        preventDefaultDblClick: boolean;
        preventDefaultContextMenu: boolean | ((this: Graph, { view }: {
            view: CellView | null;
        }) => boolean);
        preventDefaultMouseDown: boolean;
        preventDefaultBlankAction: boolean;
        interacting: CellView.Interacting;
        async?: boolean;
        virtual?: boolean;
        guard: (e: Dom.EventObject, view?: CellView | null) => boolean;
        onPortRendered?: (args: OnPortRenderedArgs) => void;
        onEdgeLabelRendered?: (args: OnEdgeLabelRenderedArgs) => undefined | ((args: OnEdgeLabelRenderedArgs) => void);
        createCellView?: (this: Graph, cell: Cell) => typeof CellView | (new (...args: any[]) => CellView) | null | undefined;
    }
    export interface ManualBooleans {
        panning: boolean | Partial<PanningManager.Options>;
        mousewheel: boolean | Partial<MouseWheel.Options>;
        embedding: boolean | Partial<Embedding>;
    }
    export interface Manual extends Partial<Common>, Partial<ManualBooleans> {
        grid?: boolean | number | (Partial<GridManager.CommonOptions> & GridManager.DrawGridOptions);
        connecting?: Partial<Connecting>;
        translating?: Partial<Translating>;
        highlighting?: Partial<Highlighting>;
    }
    export interface Definition extends Common {
        grid: GridManager.Options;
        panning: PanningManager.Options;
        mousewheel: MouseWheel.Options;
        embedding: Embedding;
        connecting: Connecting;
        translating: Translating;
        highlighting: Highlighting;
    }
    export {};
}
export declare namespace Options {
    type OptionItem<T, S> = S | ((this: Graph, arg: T) => S);
    type NodeAnchorOptions = string | NodeAnchor.NativeItem | NodeAnchor.ManaualItem;
    type EdgeAnchorOptions = string | EdgeAnchor.NativeItem | EdgeAnchor.ManaualItem;
    type ConnectionPointOptions = string | ConnectionPoint.NativeItem | ConnectionPoint.ManaualItem;
    export interface Connecting {
        /**
         * Snap edge to the closest node/port in the given radius on dragging.
         */
        snap: boolean | {
            radius: number;
            anchor?: 'center' | 'bbox';
        };
        /**
         * Specify whether connect to point on the graph is allowed.
         */
        allowBlank: boolean | ((this: Graph, args: ValidateConnectionArgs) => boolean);
        /**
         * When set to `false`, edges can not be connected to the same node,
         * meaning the source and target of the edge can not be the same node.
         */
        allowLoop: boolean | ((this: Graph, args: ValidateConnectionArgs) => boolean);
        /**
         * Specify whether connect to node(not the port on the node) is allowed.
         */
        allowNode: boolean | ((this: Graph, args: ValidateConnectionArgs) => boolean);
        /**
         * Specify whether connect to edge is allowed.
         */
        allowEdge: boolean | ((this: Graph, args: ValidateConnectionArgs) => boolean);
        /**
         * Specify whether connect to port is allowed.
         */
        allowPort: boolean | ((this: Graph, args: ValidateConnectionArgs) => boolean);
        /**
         * Specify whether more than one edge connected to the same source and
         * target node is allowed.
         */
        allowMulti: boolean | 'withPort' | ((this: Graph, args: ValidateConnectionArgs) => boolean);
        /**
         * Highlights all the available magnets or nodes when a edge is
         * dragging(reconnecting). This gives a hint to the user to what
         * other nodes/ports this edge can be connected. What magnets/cells
         * are available is determined by the `validateConnection` function.
         */
        highlight: boolean;
        anchor: NodeAnchorOptions;
        sourceAnchor?: NodeAnchorOptions;
        targetAnchor?: NodeAnchorOptions;
        edgeAnchor: EdgeAnchorOptions;
        sourceEdgeAnchor?: EdgeAnchorOptions;
        targetEdgeAnchor?: EdgeAnchorOptions;
        connectionPoint: ConnectionPointOptions;
        sourceConnectionPoint?: ConnectionPointOptions;
        targetConnectionPoint?: ConnectionPointOptions;
        router: string | Router.NativeItem | Router.ManaualItem;
        connector: string | Connector.NativeItem | Connector.ManaualItem;
        createEdge?: (this: Graph, args: {
            sourceCell: Cell;
            sourceView: CellView;
            sourceMagnet: Element;
        }) => Nilable<Edge> | void;
        /**
         * Check whether to add a new edge to the graph when user clicks
         * on an a magnet.
         */
        validateMagnet?: (this: Graph, args: {
            cell: Cell;
            view: CellView;
            magnet: Element;
            e: Dom.MouseDownEvent | Dom.MouseEnterEvent;
        }) => boolean;
        /**
         * Custom validation on stop draggin the edge arrowhead(source/target).
         * If the function returns `false`, the edge is either removed(edges
         * which are created during the interaction) or reverted to the state
         * before the interaction.
         */
        validateEdge?: (this: Graph, args: {
            edge: Edge;
            type: Edge.TerminalType;
            previous: Edge.TerminalData;
        }) => boolean;
        /**
         * Check whether to allow or disallow the edge connection while an
         * arrowhead end (source/target) being changed.
         */
        validateConnection: (this: Graph, args: ValidateConnectionArgs) => boolean;
    }
    export interface ValidateConnectionArgs {
        type?: Edge.TerminalType | null;
        edge?: Edge | null;
        edgeView?: EdgeView | null;
        sourceCell?: Cell | null;
        targetCell?: Cell | null;
        sourceView?: CellView | null;
        targetView?: CellView | null;
        sourcePort?: string | null;
        targetPort?: string | null;
        sourceMagnet?: Element | null;
        targetMagnet?: Element | null;
    }
    export interface Translating {
        /**
         * Restrict the translation (movement) of nodes by a given bounding box.
         * If set to `true`, the user will not be able to move nodes outside the
         * boundary of the graph area.
         */
        restrict: boolean | OptionItem<CellView | null, Rectangle.RectangleLike | number | null>;
    }
    export interface Embedding {
        enabled?: boolean;
        /**
         * Determines the way how a cell finds a suitable parent when it's dragged
         * over the graph. The cell with the highest z-index (visually on the top)
         * will be chosen.
         */
        findParent?: 'bbox' | 'center' | 'topLeft' | 'topRight' | 'bottomLeft' | 'bottomRight' | ((this: Graph, args: {
            node: Node;
            view: NodeView;
        }) => Cell[]);
        /**
         * If enabled only the node on the very front is taken into account for the
         * embedding. If disabled the nodes under the dragged view are tested one by
         * one (from front to back) until a valid parent found.
         */
        frontOnly?: boolean;
        /**
         * Check whether to allow or disallow the node embedding while it's being
         * translated. By default, all nodes can be embedded into all other nodes.
         */
        validate: (this: Graph, args: {
            child: Node;
            parent: Node;
            childView: CellView;
            parentView: CellView;
        }) => boolean;
    }
    /**
     * Configure which highlighter to use (and with which options) for
     * each type of interaction.
     */
    export interface Highlighting {
        /**
         * The default highlighter to use (and options) when none is specified
         */
        default: HighlightManager.Options;
        /**
         * When a cell is dragged over another cell in embedding mode.
         */
        embedding?: HighlightManager.Options | null;
        /**
         * When showing all nodes to which a valid connection can be made.
         */
        nodeAvailable?: HighlightManager.Options | null;
        /**
         * When showing all magnets to which a valid connection can be made.
         */
        magnetAvailable?: HighlightManager.Options | null;
        /**
         * When a valid edge connection can be made to an node.
         */
        magnetAdsorbed?: HighlightManager.Options | null;
    }
    export {};
}
export declare namespace Options {
    function get(options: Partial<Manual>): Definition;
}
export declare namespace Options {
    interface OnPortRenderedArgs {
        node: Node;
        port: PortManager.Port;
        container: Element;
        selectors?: Markup.Selectors;
        labelContainer?: Element;
        labelSelectors?: Markup.Selectors | null;
        contentContainer: Element;
        contentSelectors?: Markup.Selectors;
    }
    interface OnEdgeLabelRenderedArgs {
        edge: Edge;
        label: Edge.Label;
        container: Element;
        selectors: Markup.Selectors;
    }
}
export declare namespace Options {
    const defaults: Partial<Definition>;
}
