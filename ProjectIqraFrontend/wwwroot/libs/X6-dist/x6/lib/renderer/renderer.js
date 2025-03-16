"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Renderer = void 0;
/* eslint-disable @typescript-eslint/no-unused-vars */
const x6_geometry_1 = require("@antv/x6-geometry");
const base_1 = require("../graph/base");
const model_1 = require("../model");
const scheduler_1 = require("./scheduler");
const util_1 = require("../util");
class Renderer extends base_1.Base {
    constructor() {
        super(...arguments);
        this.schedule = new scheduler_1.Scheduler(this.graph);
    }
    requestViewUpdate(view, flag, options = {}) {
        this.schedule.requestViewUpdate(view, flag, options);
    }
    isViewMounted(view) {
        return this.schedule.isViewMounted(view);
    }
    setRenderArea(area) {
        this.schedule.setRenderArea(area);
    }
    findViewByElem(elem) {
        if (elem == null) {
            return null;
        }
        const container = this.options.container;
        const target = typeof elem === 'string'
            ? container.querySelector(elem)
            : elem instanceof Element
                ? elem
                : elem[0];
        if (target) {
            const id = this.graph.view.findAttr('data-cell-id', target);
            if (id) {
                const views = this.schedule.views;
                if (views[id]) {
                    return views[id].view;
                }
            }
        }
        return null;
    }
    findViewByCell(cell) {
        if (cell == null) {
            return null;
        }
        const id = model_1.Cell.isCell(cell) ? cell.id : cell;
        const views = this.schedule.views;
        if (views[id]) {
            return views[id].view;
        }
        return null;
    }
    findViewsFromPoint(p) {
        const ref = { x: p.x, y: p.y };
        return this.model
            .getCells()
            .map((cell) => this.findViewByCell(cell))
            .filter((view) => {
            if (view != null) {
                return util_1.Util.getBBox(view.container, {
                    target: this.view.stage,
                }).containsPoint(ref);
            }
            return false;
        });
    }
    findEdgeViewsFromPoint(p, threshold = 5) {
        return this.model
            .getEdges()
            .map((edge) => this.findViewByCell(edge))
            .filter((view) => {
            if (view != null) {
                const point = view.getClosestPoint(p);
                if (point) {
                    return point.distance(p) <= threshold;
                }
            }
            return false;
        });
    }
    findViewsInArea(rect, options = {}) {
        const area = x6_geometry_1.Rectangle.create(rect);
        return this.model
            .getCells()
            .map((cell) => this.findViewByCell(cell))
            .filter((view) => {
            if (view) {
                if (options.nodeOnly && !view.isNodeView()) {
                    return false;
                }
                const bbox = util_1.Util.getBBox(view.container, {
                    target: this.view.stage,
                });
                if (bbox.width === 0) {
                    bbox.inflate(1, 0);
                }
                else if (bbox.height === 0) {
                    bbox.inflate(0, 1);
                }
                return options.strict
                    ? area.containsRect(bbox)
                    : area.isIntersectWithRect(bbox);
            }
            return false;
        });
    }
    dispose() {
        this.schedule.dispose();
    }
}
__decorate([
    base_1.Base.dispose()
], Renderer.prototype, "dispose", null);
exports.Renderer = Renderer;
//# sourceMappingURL=renderer.js.map