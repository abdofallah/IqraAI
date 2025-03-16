"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Util = void 0;
const store_1 = require("./store");
var Util;
(function (Util) {
    Util.returnTrue = () => true;
    Util.returnFalse = () => false;
    function stopPropagationCallback(e) {
        e.stopPropagation();
    }
    Util.stopPropagationCallback = stopPropagationCallback;
    function addEventListener(elem, type, handler) {
        if (elem.addEventListener != null) {
            elem.addEventListener(type, handler);
        }
    }
    Util.addEventListener = addEventListener;
    function removeEventListener(elem, type, handler) {
        if (elem.removeEventListener != null) {
            elem.removeEventListener(type, handler);
        }
    }
    Util.removeEventListener = removeEventListener;
})(Util = exports.Util || (exports.Util = {}));
(function (Util) {
    const rNotHTMLWhite = /[^\x20\t\r\n\f]+/g;
    const rNamespace = /^([^.]*)(?:\.(.+)|)/;
    function splitType(types) {
        return (types || '').match(rNotHTMLWhite) || [''];
    }
    Util.splitType = splitType;
    function normalizeType(type) {
        const parts = rNamespace.exec(type) || [];
        return {
            originType: parts[1] ? parts[1].trim() : parts[1],
            namespaces: parts[2]
                ? parts[2]
                    .split('.')
                    .map((ns) => ns.trim())
                    .sort()
                : [],
        };
    }
    Util.normalizeType = normalizeType;
    function isValidTarget(target) {
        // Accepts only:
        //  - Node
        //    - Node.ELEMENT_NODE
        //    - Node.DOCUMENT_NODE
        //  - Object
        //    - Any
        return target.nodeType === 1 || target.nodeType === 9 || !+target.nodeType;
    }
    Util.isValidTarget = isValidTarget;
    function isValidSelector(elem, selector) {
        if (selector) {
            const node = elem;
            return node.querySelector != null && node.querySelector(selector) != null;
        }
        return true;
    }
    Util.isValidSelector = isValidSelector;
})(Util = exports.Util || (exports.Util = {}));
(function (Util) {
    let seed = 0;
    const cache = new WeakMap();
    function ensureHandlerId(handler) {
        if (!cache.has(handler)) {
            cache.set(handler, seed);
            seed += 1;
        }
        return cache.get(handler);
    }
    Util.ensureHandlerId = ensureHandlerId;
    function getHandlerId(handler) {
        return cache.get(handler);
    }
    Util.getHandlerId = getHandlerId;
    function removeHandlerId(handler) {
        return cache.delete(handler);
    }
    Util.removeHandlerId = removeHandlerId;
    function setHandlerId(handler, id) {
        return cache.set(handler, id);
    }
    Util.setHandlerId = setHandlerId;
})(Util = exports.Util || (exports.Util = {}));
(function (Util) {
    function getHandlerQueue(elem, event) {
        const queue = [];
        const store = store_1.Store.get(elem);
        const bag = store && store.events && store.events[event.type];
        const handlers = (bag && bag.handlers) || [];
        const delegateCount = bag ? bag.delegateCount : 0;
        if (delegateCount > 0 &&
            // Support: Firefox <=42 - 66+
            // Suppress spec-violating clicks indicating a non-primary pointer button (trac-3861)
            // https://www.w3.org/TR/DOM-Level-3-Events/#event-type-click
            // Support: IE 11+
            // ...but not arrow key "clicks" of radio inputs, which can have `button` -1 (gh-2343)
            !(event.type === 'click' &&
                typeof event.button === 'number' &&
                event.button >= 1)) {
            for (let curr = event.target; curr !== elem; curr = curr.parentNode || elem) {
                // Don't check non-elements
                // Don't process clicks on disabled elements
                if (curr.nodeType === 1 &&
                    !(event.type === 'click' && curr.disabled === true)) {
                    const matchedHandlers = [];
                    const matchedSelectors = {};
                    for (let i = 0; i < delegateCount; i += 1) {
                        const handleObj = handlers[i];
                        const selector = handleObj.selector;
                        if (selector != null && matchedSelectors[selector] == null) {
                            const node = elem;
                            const nodes = [];
                            node.querySelectorAll(selector).forEach((child) => {
                                nodes.push(child);
                            });
                            matchedSelectors[selector] = nodes.includes(curr);
                        }
                        if (matchedSelectors[selector]) {
                            matchedHandlers.push(handleObj);
                        }
                    }
                    if (matchedHandlers.length) {
                        queue.push({ elem: curr, handlers: matchedHandlers });
                    }
                }
            }
        }
        // Add the remaining (directly-bound) handlers
        if (delegateCount < handlers.length) {
            queue.push({ elem, handlers: handlers.slice(delegateCount) });
        }
        return queue;
    }
    Util.getHandlerQueue = getHandlerQueue;
})(Util = exports.Util || (exports.Util = {}));
(function (Util) {
    function isWindow(obj) {
        return obj != null && obj === obj.window;
    }
    Util.isWindow = isWindow;
})(Util = exports.Util || (exports.Util = {}));
(function (Util) {
    function contains(a, b) {
        const adown = a.nodeType === 9 ? a.documentElement : a;
        const bup = b && b.parentNode;
        return (a === bup ||
            !!(bup &&
                bup.nodeType === 1 &&
                // Support: IE 9 - 11+
                // IE doesn't have `contains` on SVG.
                (adown.contains
                    ? adown.contains(bup)
                    : a.compareDocumentPosition && a.compareDocumentPosition(bup) & 16)));
    }
    Util.contains = contains;
})(Util = exports.Util || (exports.Util = {}));
//# sourceMappingURL=util.js.map