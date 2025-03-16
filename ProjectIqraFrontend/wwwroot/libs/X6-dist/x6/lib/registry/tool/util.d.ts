import { Point } from '@antv/x6-geometry';
import { Edge } from '../../model/edge';
import { CellView } from '../../view/cell';
import { EdgeView } from '../../view/edge';
export declare function getAnchor(this: EdgeView, pos: Point.PointLike, terminalView: CellView, terminalMagnet: Element, type: Edge.TerminalType): Edge.NodeAnchorItem | undefined;
export declare function getViewBBox(view: CellView, quick?: boolean): import("@antv/x6-geometry").Rectangle;
