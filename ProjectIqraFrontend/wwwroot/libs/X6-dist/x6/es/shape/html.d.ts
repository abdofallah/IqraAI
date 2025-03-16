import { Cell } from '../model/cell';
import { Node } from '../model/node';
import { NodeView } from '../view/node';
export declare class HTML<Properties extends HTML.Properties = HTML.Properties> extends Node<Properties> {
}
export declare namespace HTML {
    interface Properties extends Node.Properties {
    }
}
export declare namespace HTML {
    class View extends NodeView<HTML> {
        protected init(): void;
        protected onCellChangeAny({ key }: Cell.EventArgs['change:*']): void;
        confirmUpdate(flag: number): number;
        protected renderHTMLComponent(): void;
        dispose(): void;
    }
    namespace View {
        const action: any;
    }
}
export declare namespace HTML {
}
export declare namespace HTML {
    type HTMLComponent = string | HTMLElement | ((cell: Cell) => HTMLElement | string);
    export type HTMLShapeConfig = Node.Properties & {
        shape: string;
        html: HTMLComponent;
        effect?: (keyof Node.Properties)[];
        inherit?: string;
    };
    export const shapeMaps: Record<string, {
        html: string | HTMLElement | ((cell: Cell) => HTMLElement | string);
        effect?: (keyof Node.Properties)[];
    }>;
    export function register(config: HTMLShapeConfig): void;
    export {};
}
