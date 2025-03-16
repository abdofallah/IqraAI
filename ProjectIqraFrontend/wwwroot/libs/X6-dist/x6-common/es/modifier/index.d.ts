import { Dom } from '../dom';
export type ModifierKey = 'alt' | 'ctrl' | 'meta' | 'shift';
export declare namespace ModifierKey {
    function parse(modifiers: string | ModifierKey[]): {
        or: ModifierKey[];
        and: ModifierKey[];
    };
    function equals(modifiers1?: string | ModifierKey[] | null, modifiers2?: string | ModifierKey[] | null): boolean;
    function isMatch(e: Dom.EventObject | WheelEvent, modifiers?: string | ModifierKey[] | null, strict?: boolean): boolean;
}
