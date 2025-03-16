"use strict";
var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                t[p[i]] = s[p[i]];
        }
    return t;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Core = void 0;
const util_1 = require("./util");
const hook_1 = require("./hook");
const store_1 = require("./store");
const object_1 = require("./object");
require("./special");
var Core;
(function (Core) {
    let triggered;
    function on(elem, types, handler, data, selector) {
        if (!util_1.Util.isValidTarget(elem)) {
            return;
        }
        // Caller can pass in an object of custom data in lieu of the handler
        let handlerData;
        if (typeof handler !== 'function') {
            const { handler: h, selector: s } = handler, others = __rest(handler, ["handler", "selector"]);
            handler = h; // eslint-disable-line
            selector = s; // eslint-disable-line
            handlerData = others;
        }
        // Ensure that invalid selectors throw exceptions at attach time
        // if (!Util.isValidSelector(elem, selector)) {
        //   throw new Error('Delegate event with invalid selector.')
        // }
        const store = store_1.Store.ensure(elem);
        // Ensure the main handle
        let mainHandler = store.handler;
        if (mainHandler == null) {
            mainHandler = store.handler = function (e, ...args) {
                return triggered !== e.type ? dispatch(elem, e, ...args) : undefined;
            };
        }
        // Make sure that the handler has a unique ID, used to find/remove it later
        const guid = util_1.Util.ensureHandlerId(handler);
        // Handle multiple events separated by a space
        util_1.Util.splitType(types).forEach((item) => {
            const { originType, namespaces } = util_1.Util.normalizeType(item);
            // There *must* be a type, no attaching namespace-only handlers
            if (!originType) {
                return;
            }
            let type = originType;
            let hook = hook_1.EventHook.get(type);
            // If selector defined, determine special event type, otherwise given type
            type = (selector ? hook.delegateType : hook.bindType) || type;
            // Update hook based on newly reset type
            hook = hook_1.EventHook.get(type);
            // handleObj is passed to all event handlers
            const handleObj = Object.assign({ type,
                originType,
                data,
                selector,
                guid, handler: handler, namespace: namespaces.join('.') }, handlerData);
            // Init the event handler queue if we're the first
            const events = store.events;
            let bag = events[type];
            if (!bag) {
                bag = events[type] = { handlers: [], delegateCount: 0 };
                // Only use addEventListener if the `hook.steup` returns false
                if (!hook.setup ||
                    hook.setup(elem, data, namespaces, mainHandler) === false) {
                    util_1.Util.addEventListener(elem, type, mainHandler);
                }
            }
            if (hook.add) {
                util_1.Util.removeHandlerId(handleObj.handler);
                hook.add(elem, handleObj);
                util_1.Util.setHandlerId(handleObj.handler, guid);
            }
            // Add to the element's handler list, delegates in front
            if (selector) {
                bag.handlers.splice(bag.delegateCount, 0, handleObj);
                bag.delegateCount += 1;
            }
            else {
                bag.handlers.push(handleObj);
            }
        });
    }
    Core.on = on;
    function off(elem, types, handler, selector, mappedTypes) {
        const store = store_1.Store.get(elem);
        if (!store) {
            return;
        }
        const events = store.events;
        if (!events) {
            return;
        }
        // Once for each type.namespace in types; type may be omitted
        util_1.Util.splitType(types).forEach((item) => {
            const { originType, namespaces } = util_1.Util.normalizeType(item);
            // Unbind all events (on this namespace, if provided) for the element
            if (!originType) {
                Object.keys(events).forEach((key) => {
                    off(elem, key + item, handler, selector, true);
                });
                return;
            }
            let type = originType;
            const hook = hook_1.EventHook.get(type);
            type = (selector ? hook.delegateType : hook.bindType) || type;
            const bag = events[type];
            if (!bag) {
                return;
            }
            const rns = namespaces.length > 0
                ? new RegExp(`(^|\\.)${namespaces.join('\\.(?:.*\\.|)')}(\\.|$)`)
                : null;
            // Remove matching events
            const originHandlerCount = bag.handlers.length;
            for (let i = bag.handlers.length - 1; i >= 0; i -= 1) {
                const handleObj = bag.handlers[i];
                if ((mappedTypes || originType === handleObj.originType) &&
                    (!handler || util_1.Util.getHandlerId(handler) === handleObj.guid) &&
                    (rns == null ||
                        (handleObj.namespace && rns.test(handleObj.namespace))) &&
                    (selector == null ||
                        selector === handleObj.selector ||
                        (selector === '**' && handleObj.selector))) {
                    bag.handlers.splice(i, 1);
                    if (handleObj.selector) {
                        bag.delegateCount -= 1;
                    }
                    if (hook.remove) {
                        hook.remove(elem, handleObj);
                    }
                }
            }
            if (originHandlerCount && bag.handlers.length === 0) {
                if (!hook.teardown ||
                    hook.teardown(elem, namespaces, store.handler) === false) {
                    util_1.Util.removeEventListener(elem, type, store.handler);
                }
                delete events[type];
            }
        });
        // Remove data and the expando if it's no longer used
        if (Object.keys(events).length === 0) {
            store_1.Store.remove(elem);
        }
    }
    Core.off = off;
    function dispatch(elem, evt, ...args) {
        const event = object_1.EventObject.create(evt);
        event.delegateTarget = elem;
        const hook = hook_1.EventHook.get(event.type);
        if (hook.preDispatch && hook.preDispatch(elem, event) === false) {
            return;
        }
        const handlerQueue = util_1.Util.getHandlerQueue(elem, event);
        // Run delegates first; they may want to stop propagation beneath us
        for (let i = 0, l = handlerQueue.length; i < l && !event.isPropagationStopped(); i += 1) {
            const matched = handlerQueue[i];
            event.currentTarget = matched.elem;
            for (let j = 0, k = matched.handlers.length; j < k && !event.isImmediatePropagationStopped(); j += 1) {
                const handleObj = matched.handlers[j];
                // If event is namespaced, then each handler is only invoked if it is
                // specially universal or its namespaces are a superset of the event's.
                if (event.rnamespace == null ||
                    (handleObj.namespace && event.rnamespace.test(handleObj.namespace))) {
                    event.handleObj = handleObj;
                    event.data = handleObj.data;
                    const hookHandle = hook_1.EventHook.get(handleObj.originType).handle;
                    const result = hookHandle
                        ? hookHandle(matched.elem, event, ...args)
                        : handleObj.handler.call(matched.elem, event, ...args);
                    if (result !== undefined) {
                        event.result = result;
                        if (result === false) {
                            event.preventDefault();
                            event.stopPropagation();
                        }
                    }
                }
            }
        }
        // Call the postDispatch hook for the mapped type
        if (hook.postDispatch) {
            hook.postDispatch(elem, event);
        }
        return event.result;
    }
    Core.dispatch = dispatch;
    function trigger(event, eventArgs, elem, onlyHandlers) {
        let eventObj = event;
        let type = typeof event === 'string' ? event : event.type;
        let namespaces = typeof event === 'string' || eventObj.namespace == null
            ? []
            : eventObj.namespace.split('.');
        const node = elem;
        // Don't do events on text and comment nodes
        if (node.nodeType === 3 || node.nodeType === 8) {
            return;
        }
        if (type.indexOf('.') > -1) {
            // Namespaced trigger; create a regexp to match event type in handle()
            namespaces = type.split('.');
            type = namespaces.shift();
            namespaces.sort();
        }
        const ontype = type.indexOf(':') < 0 && `on${type}`;
        // Caller can pass in a EventObject, Object, or just an event type string
        eventObj =
            event instanceof object_1.EventObject
                ? event
                : new object_1.EventObject(type, typeof event === 'object' ? event : null);
        eventObj.namespace = namespaces.join('.');
        eventObj.rnamespace = eventObj.namespace
            ? new RegExp(`(^|\\.)${namespaces.join('\\.(?:.*\\.|)')}(\\.|$)`)
            : null;
        // Clean up the event in case it is being reused
        eventObj.result = undefined;
        if (!eventObj.target) {
            eventObj.target = node;
        }
        const args = [eventObj];
        if (Array.isArray(eventArgs)) {
            args.push(...eventArgs);
        }
        else {
            args.push(eventArgs);
        }
        const hook = hook_1.EventHook.get(type);
        if (!onlyHandlers &&
            hook.trigger &&
            hook.trigger(node, eventObj, eventArgs) === false) {
            return;
        }
        let bubbleType;
        // Determine event propagation path in advance, per W3C events spec.
        // Bubble up to document, then to window; watch for a global ownerDocument
        const eventPath = [node];
        if (!onlyHandlers && !hook.noBubble && !util_1.Util.isWindow(node)) {
            bubbleType = hook.delegateType || type;
            let last = node;
            let curr = node.parentNode;
            while (curr != null) {
                eventPath.push(curr);
                last = curr;
                curr = curr.parentNode;
            }
            // Only add window if we got to document
            const doc = node.ownerDocument || document;
            if (last === doc) {
                const win = last.defaultView || last.parentWindow || window;
                eventPath.push(win);
            }
        }
        let lastElement = node;
        // Fire handlers on the event path
        for (let i = 0, l = eventPath.length; i < l && !eventObj.isPropagationStopped(); i += 1) {
            const currElement = eventPath[i];
            lastElement = currElement;
            eventObj.type = i > 1 ? bubbleType : hook.bindType || type;
            // Custom handler
            const store = store_1.Store.get(currElement);
            if (store) {
                if (store.events[eventObj.type] && store.handler) {
                    store.handler.call(currElement, ...args);
                }
            }
            // Native handler
            const handle = (ontype && currElement[ontype]) || null;
            if (handle && util_1.Util.isValidTarget(currElement)) {
                eventObj.result = handle.call(currElement, ...args);
                if (eventObj.result === false) {
                    eventObj.preventDefault();
                }
            }
        }
        eventObj.type = type;
        // If nobody prevented the default action, do it now
        if (!onlyHandlers && !eventObj.isDefaultPrevented()) {
            const preventDefault = hook.preventDefault;
            if ((preventDefault == null ||
                preventDefault(eventPath.pop(), eventObj, eventArgs) === false) &&
                util_1.Util.isValidTarget(node)) {
                // Call a native DOM method on the target with the same name as the
                // event. Don't do default actions on window.
                if (ontype &&
                    typeof node[type] === 'function' &&
                    !util_1.Util.isWindow(node)) {
                    // Don't re-trigger an onFOO event when we call its FOO() method
                    const tmp = node[ontype];
                    if (tmp) {
                        node[ontype] = null;
                    }
                    // Prevent re-triggering of the same event, since we already bubbled it above
                    triggered = type;
                    if (eventObj.isPropagationStopped()) {
                        lastElement.addEventListener(type, util_1.Util.stopPropagationCallback);
                    }
                    node[type]();
                    if (eventObj.isPropagationStopped()) {
                        lastElement.removeEventListener(type, util_1.Util.stopPropagationCallback);
                    }
                    triggered = undefined;
                    if (tmp) {
                        node[ontype] = tmp;
                    }
                }
            }
        }
        return eventObj.result;
    }
    Core.trigger = trigger;
})(Core = exports.Core || (exports.Core = {}));
//# sourceMappingURL=core.js.map