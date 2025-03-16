"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Portal = void 0;
const react_1 = __importStar(require("react"));
var Portal;
(function (Portal) {
    let active = false;
    let dispatch;
    const reducer = (state, action) => {
        const payload = action.payload;
        switch (action.type) {
            case 'add': {
                const index = state.findIndex((item) => item.id === payload.id);
                if (index >= 0) {
                    state[index] = payload;
                    return [...state];
                }
                return [...state, payload];
            }
            case 'remove': {
                const index = state.findIndex((item) => item.id === payload.id);
                if (index >= 0) {
                    const result = [...state];
                    result.splice(index, 1);
                    return result;
                }
                break;
            }
            default: {
                break;
            }
        }
        return state;
    };
    function connect(id, portal) {
        if (active) {
            dispatch({ type: 'add', payload: { id, portal } });
        }
    }
    Portal.connect = connect;
    function disconnect(id) {
        if (active) {
            dispatch({ type: 'remove', payload: { id } });
        }
    }
    Portal.disconnect = disconnect;
    function isActive() {
        return active;
    }
    Portal.isActive = isActive;
    function getProvider() {
        // eslint-disable-next-line react/display-name
        return () => {
            active = true;
            const [items, mutate] = (0, react_1.useReducer)(reducer, []);
            dispatch = mutate;
            // eslint-disable-next-line react/no-children-prop
            return react_1.default.createElement(react_1.default.Fragment, {
                children: items.map((item) => item.portal),
            });
        };
    }
    Portal.getProvider = getProvider;
})(Portal = exports.Portal || (exports.Portal = {}));
//# sourceMappingURL=portal.js.map