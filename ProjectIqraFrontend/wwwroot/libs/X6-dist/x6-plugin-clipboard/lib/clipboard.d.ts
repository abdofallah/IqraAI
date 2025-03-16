import { Graph, Cell, Node, Edge, Model } from '@antv/x6';
export declare class ClipboardImpl {
    protected options: ClipboardImpl.Options;
    cells: Cell[];
    copy(cells: Cell[], graph: Graph | Model, options?: ClipboardImpl.CopyOptions): void;
    cut(cells: Cell[], graph: Graph | Model, options?: ClipboardImpl.CopyOptions): void;
    paste(graph: Graph | Model, options?: ClipboardImpl.PasteOptions): Cell<Cell.Properties>[];
    serialize(options: ClipboardImpl.PasteOptions): void;
    deserialize(options: ClipboardImpl.PasteOptions): void;
    isEmpty(options?: ClipboardImpl.Options): boolean;
    clean(): void;
}
export declare namespace ClipboardImpl {
    interface Options {
        useLocalStorage?: boolean;
    }
    interface CopyOptions extends Options {
        deep?: boolean;
    }
    interface PasteOptions extends Options {
        /**
         * Set of properties to be set on each copied node on every `paste()` call.
         * It is defined as an object. e.g. `{ zIndex: 1 }`.
         */
        nodeProps?: Node.Properties;
        /**
         * Set of properties to be set on each copied edge on every `paste()` call.
         * It is defined as an object. e.g. `{ zIndex: 1 }`.
         */
        edgeProps?: Edge.Properties;
        /**
         * An increment that is added to the pasted cells position on every
         * `paste()` call. It can be either a number or an object with `dx`
         * and `dy` attributes. It defaults to `{ dx: 20, dy: 20 }`.
         */
        offset?: number | {
            dx: number;
            dy: number;
        };
    }
}
