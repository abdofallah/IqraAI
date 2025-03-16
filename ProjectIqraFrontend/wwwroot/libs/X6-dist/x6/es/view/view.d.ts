import { Dom, KeyValue, Basecoat } from '@antv/x6-common';
import { EventArgs } from '@antv/x6-common/lib/event/types';
import { Markup } from './markup';
import { Attr } from '../registry';
export declare abstract class View<A extends EventArgs = any> extends Basecoat<A> {
    readonly cid: string;
    container: Element;
    protected selectors: Markup.Selectors;
    get priority(): number;
    /** If need remove `this.container` DOM */
    protected get disposeContainer(): boolean;
    constructor();
    confirmUpdate(flag: number, options: any): number;
    empty(elem?: Element): this;
    unmount(elem?: Element): this;
    remove(elem?: Element): this;
    protected onRemove(): void;
    setClass(className: string | string[], elem?: Element): void;
    addClass(className: string | string[], elem?: Element): this;
    removeClass(className: string | string[], elem?: Element): this;
    setStyle(style: Record<string, string | number>, elem?: Element): this;
    setAttrs(attrs?: Attr.SimpleAttrs | null, elem?: Element): this;
    /**
     * Returns the value of the specified attribute of `node`.
     *
     * If the node does not set a value for attribute, start recursing up
     * the DOM tree from node to lookup for attribute at the ancestors of
     * node. If the recursion reaches CellView's root node and attribute
     * is not found even there, return `null`.
     */
    findAttr(attrName: string, elem?: Element): string | null;
    find(selector?: string, rootElem?: Element, selectors?: Markup.Selectors): Element[];
    findOne(selector?: string, rootElem?: Element, selectors?: Markup.Selectors): Element | null;
    findByAttr(attrName: string, elem?: Element): Element | null;
    getSelector(elem: Element, prevSelector?: string): string | undefined;
    prefixClassName(className: string): string;
    delegateEvents(events: View.Events, append?: boolean): this;
    undelegateEvents(): this;
    delegateDocumentEvents(events: View.Events, data?: KeyValue): this;
    undelegateDocumentEvents(): this;
    protected delegateEvent(eventName: string, selector: string | Record<string, unknown>, listener: any): this;
    protected undelegateEvent(eventName: string, selector: string, listener: any): this;
    protected undelegateEvent(eventName: string): this;
    protected undelegateEvent(eventName: string, listener: any): this;
    protected addEventListeners(elem: Element | Document, events: View.Events, data?: KeyValue): this;
    protected removeEventListeners(elem: Element | Document): this;
    protected getEventNamespace(): string;
    protected getEventHandler(handler: string | Function): Function | undefined;
    getEventTarget(e: Dom.EventObject, options?: {
        fromPoint?: boolean;
    }): any;
    stopPropagation(e: Dom.EventObject): this;
    isPropagationStopped(e: Dom.EventObject): boolean;
    getEventData<T extends KeyValue>(e: Dom.EventObject): T;
    setEventData<T extends KeyValue>(e: Dom.EventObject, data: T): T;
    protected eventData<T extends KeyValue>(e: Dom.EventObject, data?: T): T;
    normalizeEvent<T extends Dom.EventObject>(evt: T): T;
    dispose(): void;
}
export declare namespace View {
    type Events = KeyValue<string | Function>;
}
export declare namespace View {
    function createElement(tagName?: string, isSvgElement?: boolean): SVGElement | HTMLElement;
    function find(selector: string | null | undefined, rootElem: Element, selectors: Markup.Selectors): {
        isCSSSelector?: boolean;
        elems: Element[];
    };
    function normalizeEvent<T extends Dom.EventObject>(evt: T): T;
}
export declare namespace View {
    const views: {
        [cid: string]: View;
    };
    function getView(cid: string): View<any>;
}
