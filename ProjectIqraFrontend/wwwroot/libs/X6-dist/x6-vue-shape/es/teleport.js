import { defineComponent, h, reactive, isVue3, Teleport, markRaw, Fragment } from 'vue-demi';
let active = false;
const items = reactive({});
export function connect(id, component, container, node, graph) {
    if (active) {
        items[id] = markRaw(defineComponent({
            render: () => h(Teleport, { to: container }, [h(component, { node, graph })]),
            provide: () => ({
                getNode: () => node,
                getGraph: () => graph,
            }),
        }));
    }
}
export function disconnect(id) {
    if (active) {
        delete items[id];
    }
}
export function isActive() {
    return active;
}
export function getTeleport() {
    if (!isVue3) {
        throw new Error('teleport is only available in Vue3');
    }
    active = true;
    return defineComponent({
        setup() {
            return () => h(Fragment, {}, Object.keys(items).map((id) => h(items[id])));
        },
    });
}
//# sourceMappingURL=teleport.js.map