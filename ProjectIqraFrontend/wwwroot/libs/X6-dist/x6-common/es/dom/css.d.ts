export declare function isCSSVariable(prop: string): boolean;
export declare function computeStyle(elem: Element, prop: string, isVariable?: boolean): any;
export declare function computeStyleInt(elem: Element, prop: string): number;
export declare function css(elem: Element, prop: string): string | undefined;
export declare function css(elem: Element, prop: string, value: number | string): void;
export declare function css(elem: Element, prop: Record<string, number | string>): void;
