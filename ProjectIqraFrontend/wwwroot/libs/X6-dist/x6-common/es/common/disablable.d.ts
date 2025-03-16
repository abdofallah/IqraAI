import { EventArgs } from '../event/types';
import { Basecoat } from './basecoat';
export interface IDisablable {
    readonly disabled: boolean;
    enable(): void;
    disable(): void;
}
export declare abstract class Disablable<A extends EventArgs = any> extends Basecoat<A> implements IDisablable {
    private _disabled?;
    get disabled(): boolean;
    enable(): void;
    disable(): void;
}
