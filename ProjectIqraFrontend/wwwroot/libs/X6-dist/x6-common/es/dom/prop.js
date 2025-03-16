const propMap = {
    /* GENERAL */
    class: 'className',
    contenteditable: 'contentEditable',
    /* LABEL */
    for: 'htmlFor',
    /* INPUT */
    readonly: 'readOnly',
    maxlength: 'maxLength',
    tabindex: 'tabIndex',
    /* TABLE */
    colspan: 'colSpan',
    rowspan: 'rowSpan',
    /* IMAGE */
    usemap: 'useMap',
};
export function prop(elem, props, value) {
    if (!props) {
        return;
    }
    if (typeof props === 'string') {
        props = propMap[props] || props; // eslint-disable-line
        if (arguments.length < 3) {
            return elem[props];
        }
        ;
        elem[props] = value;
        return;
    }
    // eslint-disable-next-line
    for (const key in props) {
        prop(elem, key, props[key]);
    }
}
//# sourceMappingURL=prop.js.map