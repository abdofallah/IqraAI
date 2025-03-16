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
exports.router = void 0;
const x6_common_1 = require("@antv/x6-common");
const x6_geometry_1 = require("@antv/x6-geometry");
const sorted_set_1 = require("./sorted-set");
const obstacle_map_1 = require("./obstacle-map");
const util = __importStar(require("./util"));
const options_1 = require("./options");
/**
 * Finds the route between two points (`from`, `to`).
 */
function findRoute(edgeView, from, to, map, options) {
    const precision = options.precision;
    let sourceEndpoint;
    let targetEndpoint;
    if (x6_geometry_1.Rectangle.isRectangle(from)) {
        sourceEndpoint = util.round(util.getSourceEndpoint(edgeView, options).clone(), precision);
    }
    else {
        sourceEndpoint = util.round(from.clone(), precision);
    }
    if (x6_geometry_1.Rectangle.isRectangle(to)) {
        targetEndpoint = util.round(util.getTargetEndpoint(edgeView, options).clone(), precision);
    }
    else {
        targetEndpoint = util.round(to.clone(), precision);
    }
    // Get grid for this route.
    const grid = util.getGrid(options.step, sourceEndpoint, targetEndpoint);
    // Get pathfinding points.
    // -----------------------
    const startPoint = sourceEndpoint;
    const endPoint = targetEndpoint;
    let startPoints;
    let endPoints;
    if (x6_geometry_1.Rectangle.isRectangle(from)) {
        startPoints = util.getRectPoints(startPoint, from, options.startDirections, grid, options);
    }
    else {
        startPoints = [startPoint];
    }
    if (x6_geometry_1.Rectangle.isRectangle(to)) {
        endPoints = util.getRectPoints(targetEndpoint, to, options.endDirections, grid, options);
    }
    else {
        endPoints = [endPoint];
    }
    // take into account only accessible rect points (those not under obstacles)
    startPoints = startPoints.filter((p) => map.isAccessible(p));
    endPoints = endPoints.filter((p) => map.isAccessible(p));
    // There is an accessible route point on both sides.
    if (startPoints.length > 0 && endPoints.length > 0) {
        const openSet = new sorted_set_1.SortedSet();
        // Keeps the actual points for given nodes of the open set.
        const points = {};
        // Keeps the point that is immediate predecessor of given element.
        const parents = {};
        // Cost from start to a point along best known path.
        const costs = {};
        for (let i = 0, n = startPoints.length; i < n; i += 1) {
            // startPoint is assumed to be aligned already
            const startPoint = startPoints[i];
            const key = util.getKey(startPoint);
            openSet.add(key, util.getCost(startPoint, endPoints));
            points[key] = startPoint;
            costs[key] = 0;
        }
        const previousRouteDirectionAngle = options.previousDirectionAngle;
        // undefined for first route
        const isPathBeginning = previousRouteDirectionAngle === undefined;
        // directions
        let direction;
        let directionChange;
        const directions = util.getGridOffsets(grid, options);
        const numDirections = directions.length;
        const endPointsKeys = endPoints.reduce((res, endPoint) => {
            const key = util.getKey(endPoint);
            res.push(key);
            return res;
        }, []);
        // main route finding loop
        const sameStartEndPoints = x6_geometry_1.Point.equalPoints(startPoints, endPoints);
        let loopsRemaining = options.maxLoopCount;
        while (!openSet.isEmpty() && loopsRemaining > 0) {
            // Get the closest item and mark it CLOSED
            const currentKey = openSet.pop();
            const currentPoint = points[currentKey];
            const currentParent = parents[currentKey];
            const currentCost = costs[currentKey];
            const isStartPoint = currentPoint.equals(startPoint);
            const isRouteBeginning = currentParent == null;
            let previousDirectionAngle;
            if (!isRouteBeginning) {
                previousDirectionAngle = util.getDirectionAngle(currentParent, currentPoint, numDirections, grid, options);
            }
            else if (!isPathBeginning) {
                // a vertex on the route
                previousDirectionAngle = previousRouteDirectionAngle;
            }
            else if (!isStartPoint) {
                // beginning of route on the path
                previousDirectionAngle = util.getDirectionAngle(startPoint, currentPoint, numDirections, grid, options);
            }
            else {
                previousDirectionAngle = null;
            }
            // Check if we reached any endpoint
            const skipEndCheck = isRouteBeginning && sameStartEndPoints;
            if (!skipEndCheck && endPointsKeys.indexOf(currentKey) >= 0) {
                options.previousDirectionAngle = previousDirectionAngle;
                return util.reconstructRoute(parents, points, currentPoint, startPoint, endPoint);
            }
            // Go over all possible directions and find neighbors
            for (let i = 0; i < numDirections; i += 1) {
                direction = directions[i];
                const directionAngle = direction.angle;
                directionChange = util.getDirectionChange(previousDirectionAngle, directionAngle);
                // Don't use the point changed rapidly.
                if (!(isPathBeginning && isStartPoint) &&
                    directionChange > options.maxDirectionChange) {
                    continue;
                }
                const neighborPoint = util.align(currentPoint
                    .clone()
                    .translate(direction.gridOffsetX || 0, direction.gridOffsetY || 0), grid, precision);
                const neighborKey = util.getKey(neighborPoint);
                // Closed points were already evaluated.
                if (openSet.isClose(neighborKey) || !map.isAccessible(neighborPoint)) {
                    continue;
                }
                // Neighbor is an end point.
                if (endPointsKeys.indexOf(neighborKey) >= 0) {
                    const isEndPoint = neighborPoint.equals(endPoint);
                    if (!isEndPoint) {
                        const endDirectionAngle = util.getDirectionAngle(neighborPoint, endPoint, numDirections, grid, options);
                        const endDirectionChange = util.getDirectionChange(directionAngle, endDirectionAngle);
                        if (endDirectionChange > options.maxDirectionChange) {
                            continue;
                        }
                    }
                }
                // The current direction is ok.
                // ----------------------------
                const neighborCost = direction.cost;
                const neighborPenalty = isStartPoint
                    ? 0
                    : options.penalties[directionChange];
                const costFromStart = currentCost + neighborCost + neighborPenalty;
                // Neighbor point has not been processed yet or the cost of
                // the path from start is lower than previously calculated.
                if (!openSet.isOpen(neighborKey) ||
                    costFromStart < costs[neighborKey]) {
                    points[neighborKey] = neighborPoint;
                    parents[neighborKey] = currentPoint;
                    costs[neighborKey] = costFromStart;
                    openSet.add(neighborKey, costFromStart + util.getCost(neighborPoint, endPoints));
                }
            }
            loopsRemaining -= 1;
        }
    }
    if (options.fallbackRoute) {
        return x6_common_1.FunctionExt.call(options.fallbackRoute, this, startPoint, endPoint, options);
    }
    return null;
}
function snap(vertices, gridSize = 10) {
    if (vertices.length <= 1) {
        return vertices;
    }
    for (let i = 0, len = vertices.length; i < len - 1; i += 1) {
        const first = vertices[i];
        const second = vertices[i + 1];
        if (first.x === second.x) {
            const x = gridSize * Math.round(first.x / gridSize);
            if (first.x !== x) {
                first.x = x;
                second.x = x;
            }
        }
        else if (first.y === second.y) {
            const y = gridSize * Math.round(first.y / gridSize);
            if (first.y !== y) {
                first.y = y;
                second.y = y;
            }
        }
    }
    return vertices;
}
const router = function (vertices, optionsRaw, edgeView) {
    const options = (0, options_1.resolveOptions)(optionsRaw);
    const sourceBBox = util.getSourceBBox(edgeView, options);
    const targetBBox = util.getTargetBBox(edgeView, options);
    const sourceEndpoint = util.getSourceEndpoint(edgeView, options);
    // pathfinding
    const map = new obstacle_map_1.ObstacleMap(options).build(edgeView.graph.model, edgeView.cell);
    const oldVertices = vertices.map((p) => x6_geometry_1.Point.create(p));
    const newVertices = [];
    // The origin of first route's grid, does not need snapping
    let tailPoint = sourceEndpoint;
    let from;
    let to;
    for (let i = 0, len = oldVertices.length; i <= len; i += 1) {
        let partialRoute = null;
        from = to || sourceBBox;
        to = oldVertices[i];
        // This is the last iteration
        if (to == null) {
            to = targetBBox;
            // If the target is a point, we should use dragging route
            // instead of main routing method if it has been provided.
            const edge = edgeView.cell;
            const isEndingAtPoint = edge.getSourceCellId() == null || edge.getTargetCellId() == null;
            if (isEndingAtPoint && typeof options.draggingRouter === 'function') {
                const dragFrom = from === sourceBBox ? sourceEndpoint : from;
                const dragTo = to.getOrigin();
                partialRoute = x6_common_1.FunctionExt.call(options.draggingRouter, edgeView, dragFrom, dragTo, options);
            }
        }
        // Find the partial route
        if (partialRoute == null) {
            partialRoute = findRoute(edgeView, from, to, map, options);
        }
        // Cannot found the partial route.
        if (partialRoute === null) {
            // eslint-next-line
            console.warn(`Unable to execute manhattan algorithm, use orth instead`);
            return x6_common_1.FunctionExt.call(options.fallbackRouter, this, vertices, options, edgeView);
        }
        // Remove the first point if the previous partial route has
        // the same point as last.
        const leadPoint = partialRoute[0];
        if (leadPoint && leadPoint.equals(tailPoint)) {
            partialRoute.shift();
        }
        // Save tailPoint for next iteration
        tailPoint = partialRoute[partialRoute.length - 1] || tailPoint;
        newVertices.push(...partialRoute);
    }
    if (options.snapToGrid) {
        return snap(newVertices, edgeView.graph.grid.getGridSize());
    }
    return newVertices;
};
exports.router = router;
//# sourceMappingURL=router.js.map