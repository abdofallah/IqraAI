export { debounce, throttle } from 'lodash-es';
type Fn = (...args: any[]) => any;
export declare function apply<T extends Fn>(fn: T, ctx: ThisParameterType<T>, args?: Parameters<T>): ReturnType<T>;
export declare function call<T extends Fn>(fn: T, ctx: ThisParameterType<T>, ...args: Parameters<T>): ReturnType<T>;
