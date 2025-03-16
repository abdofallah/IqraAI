"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Special = void 0;
const hook_1 = require("./hook");
const util_1 = require("./util");
// Prevent triggered image.load events from bubbling to window.load
var Special;
(function (Special) {
    hook_1.EventHook.register('load', {
        noBubble: true,
    });
})(Special = exports.Special || (exports.Special = {}));
// Support: Chrome <=73+
// Chrome doesn't alert on `event.preventDefault()`
// as the standard mandates.
(function (Special) {
    hook_1.EventHook.register('beforeunload', {
        postDispatch(elem, event) {
            if (event.result !== undefined && event.originalEvent) {
                event.originalEvent.returnValue = event.result;
            }
        },
    });
})(Special = exports.Special || (exports.Special = {}));
// For mouseenter/leave call the handler if related is outside the target.
// NB: No relatedTarget if the mouse left/entered the browser window
(function (Special) {
    hook_1.EventHook.register('mouseenter', {
        delegateType: 'mouseover',
        bindType: 'mouseover',
        handle(target, event) {
            let ret;
            const related = event.relatedTarget;
            const handleObj = event.handleObj;
            if (!related || (related !== target && !util_1.Util.contains(target, related))) {
                event.type = handleObj.originType;
                ret = handleObj.handler.call(target, event);
                event.type = 'mouseover';
            }
            return ret;
        },
    });
    hook_1.EventHook.register('mouseleave', {
        delegateType: 'mouseout',
        bindType: 'mouseout',
        handle(target, event) {
            let ret;
            const related = event.relatedTarget;
            const handleObj = event.handleObj;
            if (!related || (related !== target && !util_1.Util.contains(target, related))) {
                event.type = handleObj.originType;
                ret = handleObj.handler.call(target, event);
                event.type = 'mouseout';
            }
            return ret;
        },
    });
})(Special = exports.Special || (exports.Special = {}));
//# sourceMappingURL=special.js.map