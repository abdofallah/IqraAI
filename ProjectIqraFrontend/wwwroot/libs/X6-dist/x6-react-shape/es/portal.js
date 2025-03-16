import React, { useReducer } from 'react';
export var Portal;
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
            const [items, mutate] = useReducer(reducer, []);
            dispatch = mutate;
            // eslint-disable-next-line react/no-children-prop
            return React.createElement(React.Fragment, {
                children: items.map((item) => item.portal),
            });
        };
    }
    Portal.getProvider = getProvider;
})(Portal || (Portal = {}));
//# sourceMappingURL=portal.js.map