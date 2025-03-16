import { Util } from './util';
export class EventObject {
    constructor(e, props) {
        this.isDefaultPrevented = Util.returnFalse;
        this.isPropagationStopped = Util.returnFalse;
        this.isImmediatePropagationStopped = Util.returnFalse;
        this.isSimulated = false;
        this.preventDefault = () => {
            const e = this.originalEvent;
            this.isDefaultPrevented = Util.returnTrue;
            if (e && !this.isSimulated) {
                e.preventDefault();
            }
        };
        this.stopPropagation = () => {
            const e = this.originalEvent;
            this.isPropagationStopped = Util.returnTrue;
            if (e && !this.isSimulated) {
                e.stopPropagation();
            }
        };
        this.stopImmediatePropagation = () => {
            const e = this.originalEvent;
            this.isImmediatePropagationStopped = Util.returnTrue;
            if (e && !this.isSimulated) {
                e.stopImmediatePropagation();
            }
            this.stopPropagation();
        };
        if (typeof e === 'string') {
            this.type = e;
        }
        else if (e.type) {
            this.originalEvent = e;
            this.type = e.type;
            // Events bubbling up the document may have been marked as prevented
            // by a handler lower down the tree; reflect the correct value.
            this.isDefaultPrevented = e.defaultPrevented
                ? Util.returnTrue
                : Util.returnFalse;
            // Create target properties
            this.target = e.target;
            this.currentTarget = e.currentTarget;
            this.relatedTarget = e.relatedTarget;
            this.timeStamp = e.timeStamp;
        }
        // Put explicitly provided properties onto the event object
        if (props) {
            Object.assign(this, props);
        }
        // Create a timestamp if incoming event doesn't have one
        if (!this.timeStamp) {
            this.timeStamp = Date.now();
        }
    }
}
(function (EventObject) {
    function create(originalEvent) {
        return originalEvent instanceof EventObject
            ? originalEvent
            : new EventObject(originalEvent);
    }
    EventObject.create = create;
})(EventObject || (EventObject = {}));
(function (EventObject) {
    function addProperty(name, hook) {
        Object.defineProperty(EventObject.prototype, name, {
            enumerable: true,
            configurable: true,
            get: typeof hook === 'function'
                ? // eslint-disable-next-line
                    function () {
                        if (this.originalEvent) {
                            return hook(this.originalEvent);
                        }
                    }
                : // eslint-disable-next-line
                    function () {
                        if (this.originalEvent) {
                            return this.originalEvent[name];
                        }
                    },
            set(value) {
                Object.defineProperty(this, name, {
                    enumerable: true,
                    configurable: true,
                    writable: true,
                    value,
                });
            },
        });
    }
    EventObject.addProperty = addProperty;
})(EventObject || (EventObject = {}));
(function (EventObject) {
    // Common event props including KeyEvent and MouseEvent specific props.
    const commonProps = {
        bubbles: true,
        cancelable: true,
        eventPhase: true,
        detail: true,
        view: true,
        button: true,
        buttons: true,
        clientX: true,
        clientY: true,
        offsetX: true,
        offsetY: true,
        pageX: true,
        pageY: true,
        screenX: true,
        screenY: true,
        toElement: true,
        pointerId: true,
        pointerType: true,
        char: true,
        code: true,
        charCode: true,
        key: true,
        keyCode: true,
        touches: true,
        changedTouches: true,
        targetTouches: true,
        which: true,
        altKey: true,
        ctrlKey: true,
        metaKey: true,
        shiftKey: true,
    };
    Object.keys(commonProps).forEach((name) => EventObject.addProperty(name, commonProps[name]));
})(EventObject || (EventObject = {}));
//# sourceMappingURL=object.js.map