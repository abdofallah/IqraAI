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
import { ObjectExt } from '@antv/x6-common';
import { Config } from '../config';
import { Edge as StandardEdge } from '../shape';
export var Options;
(function (Options) {
    function get(options) {
        const { grid, panning, mousewheel, embedding } = options, others = __rest(options
        // size
        // ----
        , ["grid", "panning", "mousewheel", "embedding"]);
        // size
        // ----
        const container = options.container;
        if (container != null) {
            if (others.width == null) {
                others.width = container.clientWidth;
            }
            if (others.height == null) {
                others.height = container.clientHeight;
            }
        }
        else {
            throw new Error(`Ensure the container of the graph is specified and valid`);
        }
        const result = ObjectExt.merge({}, Options.defaults, others);
        // grid
        // ----
        const defaultGrid = { size: 10, visible: false };
        if (typeof grid === 'number') {
            result.grid = { size: grid, visible: false };
        }
        else if (typeof grid === 'boolean') {
            result.grid = Object.assign(Object.assign({}, defaultGrid), { visible: grid });
        }
        else {
            result.grid = Object.assign(Object.assign({}, defaultGrid), grid);
        }
        // booleas
        // -------
        const booleas = [
            'panning',
            'mousewheel',
            'embedding',
        ];
        booleas.forEach((key) => {
            const val = options[key];
            if (typeof val === 'boolean') {
                result[key].enabled = val;
            }
            else {
                result[key] = Object.assign(Object.assign({}, result[key]), val);
            }
        });
        return result;
    }
    Options.get = get;
})(Options || (Options = {}));
(function (Options) {
    Options.defaults = {
        x: 0,
        y: 0,
        scaling: {
            min: 0.01,
            max: 16,
        },
        grid: {
            size: 10,
            visible: false,
        },
        background: false,
        panning: {
            enabled: false,
            eventTypes: ['leftMouseDown'],
        },
        mousewheel: {
            enabled: false,
            factor: 1.2,
            zoomAtMousePosition: true,
        },
        highlighting: {
            default: {
                name: 'stroke',
                args: {
                    padding: 3,
                },
            },
            nodeAvailable: {
                name: 'className',
                args: {
                    className: Config.prefix('available-node'),
                },
            },
            magnetAvailable: {
                name: 'className',
                args: {
                    className: Config.prefix('available-magnet'),
                },
            },
        },
        connecting: {
            snap: false,
            allowLoop: true,
            allowNode: true,
            allowEdge: false,
            allowPort: true,
            allowBlank: true,
            allowMulti: true,
            highlight: false,
            anchor: 'center',
            edgeAnchor: 'ratio',
            connectionPoint: 'boundary',
            router: 'normal',
            connector: 'normal',
            validateConnection({ type, sourceView, targetView }) {
                const view = type === 'target' ? targetView : sourceView;
                return view != null;
            },
            createEdge() {
                return new StandardEdge();
            },
        },
        translating: {
            restrict: false,
        },
        embedding: {
            enabled: false,
            findParent: 'bbox',
            frontOnly: true,
            validate: () => true,
        },
        moveThreshold: 0,
        clickThreshold: 0,
        magnetThreshold: 0,
        preventDefaultDblClick: true,
        preventDefaultMouseDown: false,
        preventDefaultContextMenu: true,
        preventDefaultBlankAction: true,
        interacting: {
            edgeLabelMovable: false,
        },
        async: true,
        virtual: false,
        guard: () => false,
    };
})(Options || (Options = {}));
//# sourceMappingURL=options.js.map