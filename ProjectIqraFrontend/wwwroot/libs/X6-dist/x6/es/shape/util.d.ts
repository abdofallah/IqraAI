import { Cell, Node } from '../model';
import { Markup } from '../view';
import { Base } from './base';
export declare function getMarkup(tagName: string, selector?: string): Markup;
export declare function getImageUrlHook(attrName?: string): Cell.PropHook<Cell.Metadata, Cell<Cell.Properties>>;
export declare function createShape(shape: string, config: Node.Config, options?: {
    selector?: string;
    parent?: Node.Definition | typeof Base;
}): typeof Base;
