"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Dijkstra = void 0;
const priorityqueue_1 = require("./priorityqueue");
var Dijkstra;
(function (Dijkstra) {
    function run(adjacencyList, source, weight = (u, v) => 1) {
        const dist = {};
        const previous = {};
        const scanned = {};
        const queue = new priorityqueue_1.PriorityQueue();
        dist[source] = 0;
        Object.keys(adjacencyList).forEach((v) => {
            if (v !== source) {
                dist[v] = Infinity;
            }
            queue.insert(dist[v], v, v);
        });
        while (!queue.isEmpty()) {
            const u = queue.remove();
            scanned[u] = true;
            const neighbours = adjacencyList[u] || [];
            for (let i = 0; i < neighbours.length; i += 1) {
                const v = neighbours[i];
                if (!scanned[v]) {
                    const alt = dist[u] + weight(u, v);
                    if (alt < dist[v]) {
                        dist[v] = alt;
                        previous[v] = u;
                        queue.updatePriority(v, alt);
                    }
                }
            }
        }
        return previous;
    }
    Dijkstra.run = run;
})(Dijkstra = exports.Dijkstra || (exports.Dijkstra = {}));
//# sourceMappingURL=dijkstra.js.map