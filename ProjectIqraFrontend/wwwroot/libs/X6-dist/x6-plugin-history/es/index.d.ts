import { KeyValue, Basecoat, Cell, Model, Graph } from '@antv/x6';
import './api';
export declare class History extends Basecoat<History.EventArgs> implements Graph.Plugin {
    name: string;
    graph: Graph;
    model: Model;
    readonly options: History.CommonOptions;
    readonly validator: History.Validator;
    protected redoStack: History.Commands[];
    protected undoStack: History.Commands[];
    protected batchCommands: History.Command[] | null;
    protected batchLevel: number;
    protected lastBatchIndex: number;
    protected freezed: boolean;
    protected stackSize: number;
    protected readonly handlers: (<T extends History.ModelEvents>(event: T, args: Model.EventArgs[T]) => any)[];
    constructor(options?: History.Options);
    init(graph: Graph): void;
    isEnabled(): boolean;
    enable(): void;
    disable(): void;
    toggleEnabled(enabled?: boolean): this;
    undo(options?: KeyValue): this;
    redo(options?: KeyValue): this;
    /**
     * Same as `undo()` but does not store the undo-ed command to the
     * `redoStack`. Canceled command therefore cannot be redo-ed.
     */
    cancel(options?: KeyValue): this;
    getSize(): number;
    getUndoRemainSize(): number;
    getUndoSize(): number;
    getRedoSize(): number;
    canUndo(): boolean;
    canRedo(): boolean;
    clean(options?: KeyValue): this;
    get disabled(): boolean;
    protected validate(events: string | string[], ...callbacks: History.Validator.Callback[]): this;
    protected startListening(): void;
    protected stopListening(): void;
    protected createCommand(options?: {
        batch: boolean;
    }): History.Command;
    protected revertCommand(cmd: History.Commands, options?: KeyValue): void;
    protected applyCommand(cmd: History.Commands, options?: KeyValue): void;
    protected executeCommand(cmd: History.Command, revert: boolean, options: KeyValue): void;
    protected addCommand<T extends keyof Model.EventArgs>(event: T, args: Model.EventArgs[T]): void;
    /**
     * Gather multiple changes into a single command. These commands could
     * be reverted with single `undo()` call. From the moment the function
     * is called every change made on model is not stored into the undoStack.
     * Changes are temporarily kept until `storeBatchCommand()` is called.
     */
    protected initBatchCommand(options: KeyValue): void;
    /**
     * Store changes temporarily kept in the undoStack. You have to call this
     * function as many times as `initBatchCommand()` been called.
     */
    protected storeBatchCommand(options: KeyValue): void;
    protected filterBatchCommand(batchCommands: History.Command[]): History.Command[];
    protected notify(event: keyof History.EventArgs, cmd: History.Commands | null, options: KeyValue): void;
    protected push(cmd: History.Command, options: KeyValue): void;
    /**
     * Conditionally combine multiple undo items into one.
     *
     * Currently this is only used combine a `cell:changed:position` event
     * followed by multiple `cell:change:parent` and `cell:change:children`
     * events, such that a "move + embed" action can be undone in one step.
     *
     * See https://github.com/antvis/X6/issues/2421
     *
     * This is an ugly WORKAROUND. It does not solve deficiencies in the batch
     * system itself.
     */
    protected consolidateCommands(): void;
    protected undoStackPush(cmd: History.Commands): void;
    protected ensureUndefinedAttrs(newAttrs: Record<string, any>, oldAttrs: Record<string, any>): boolean;
    dispose(): void;
}
export declare namespace History {
    export type ModelEvents = keyof Model.EventArgs;
    export interface CommonOptions {
        enabled?: boolean;
        ignoreAdd?: boolean;
        ignoreRemove?: boolean;
        ignoreChange?: boolean;
        eventNames?: (keyof Model.EventArgs)[];
        /**
         * A function evaluated before any command is added. If the function
         * returns `false`, the command does not get stored. This way you can
         * control which commands do not get registered for undo/redo.
         */
        beforeAddCommand?: <T extends ModelEvents>(this: History, event: T, args: Model.EventArgs[T]) => any;
        afterAddCommand?: <T extends ModelEvents>(this: History, event: T, args: Model.EventArgs[T], cmd: Command) => any;
        executeCommand?: (this: History, cmd: Command, revert: boolean, options: KeyValue) => any;
        /**
         * An array of options property names that passed in undo actions.
         */
        revertOptionsList?: string[];
        /**
         * An array of options property names that passed in redo actions.
         */
        applyOptionsList?: string[];
        /**
         * Determine whether to cancel an invalid command or not.
         */
        cancelInvalid?: boolean;
    }
    export interface Options extends Partial<CommonOptions> {
        stackSize?: number;
    }
    interface Data {
        id?: string;
    }
    export interface CreationData extends Data {
        edge?: boolean;
        node?: boolean;
        props: Cell.Properties;
    }
    export interface ChangingData extends Data {
        key: string;
        prev: KeyValue;
        next: KeyValue;
    }
    export interface Command {
        batch: boolean;
        modelChange?: boolean;
        event?: ModelEvents;
        data: CreationData | ChangingData;
        options?: KeyValue;
    }
    export type Commands = History.Command[] | History.Command;
    export {};
}
export declare namespace History {
    interface Args<T = never> {
        cmds: Command[] | T;
        options: KeyValue;
    }
    interface EventArgs extends Validator.EventArgs {
        /**
         * Triggered when a command was undone.
         */
        undo: Args;
        /**
         * Triggered when a command were redone.
         */
        redo: Args;
        /**
         * Triggered when a command was canceled.
         */
        cancel: Args;
        /**
         * Triggered when command(s) were added to the stack.
         */
        add: Args;
        /**
         * Triggered when all commands were clean.
         */
        clean: Args<null>;
        /**
         * Triggered when any change was made to stacks.
         */
        change: Args<null>;
        /**
         * Triggered when a batch command received.
         */
        batch: {
            cmd: Command;
            options: KeyValue;
        };
    }
}
export declare namespace History {
    /**
     * Runs a set of callbacks to determine if a command is valid. This is
     * useful for checking if a certain action in your application does
     * lead to an invalid state of the graph.
     */
    class Validator extends Basecoat<Validator.EventArgs> {
        protected readonly command: History;
        protected readonly cancelInvalid: boolean;
        protected readonly map: {
            [event: string]: Validator.Callback[][];
        };
        constructor(options: Validator.Options);
        protected onCommandAdded({ cmds }: History.EventArgs['add']): boolean;
        protected isValidCommand(cmd: History.Command): boolean;
        validate(events: string | string[], ...callbacks: Validator.Callback[]): this;
        dispose(): void;
    }
    namespace Validator {
        interface Options {
            history: History;
            /**
             * To cancel (= undo + delete from redo stack) a command if is not valid.
             */
            cancelInvalid?: boolean;
        }
        type Callback = (err: Error | null, cmd: History.Command, next: (err: Error | null) => any) => any;
        interface EventArgs {
            invalid: {
                err: Error;
            };
        }
    }
}
