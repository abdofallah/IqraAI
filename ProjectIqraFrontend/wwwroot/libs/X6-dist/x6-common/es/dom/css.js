import { getVendorPrefixedName } from './prefix';
const numericProps = {
    animationIterationCount: true,
    columnCount: true,
    flexGrow: true,
    flexShrink: true,
    fontWeight: true,
    gridArea: true,
    gridColumn: true,
    gridColumnEnd: true,
    gridColumnStart: true,
    gridRow: true,
    gridRowEnd: true,
    gridRowStart: true,
    lineHeight: true,
    opacity: true,
    order: true,
    orphans: true,
    widows: true,
    zIndex: true,
};
export function isCSSVariable(prop) {
    return /^--/.test(prop);
}
export function computeStyle(elem, prop, isVariable) {
    const style = window.getComputedStyle(elem, null);
    return isVariable
        ? style.getPropertyValue(prop) || undefined
        : style[prop] || elem.style[prop];
}
export function computeStyleInt(elem, prop) {
    return parseInt(computeStyle(elem, prop), 10) || 0;
}
function getSuffixedValue(prop, value) {
    return !numericProps[prop] && typeof value === 'number' ? `${value}px` : value;
}
export function css(elem, prop, value) {
    if (typeof prop === 'string') {
        const isVariable = isCSSVariable(prop);
        if (!isVariable) {
            prop = getVendorPrefixedName(prop); // eslint-disable-line
        }
        if (value === undefined) {
            return computeStyle(elem, prop, isVariable);
        }
        if (!isVariable) {
            value = getSuffixedValue(prop, value); // eslint-disable-line
        }
        const style = elem.style;
        if (isVariable) {
            style.setProperty(prop, value);
        }
        else {
            style[prop] = value;
        }
        return;
    }
    // eslint-disable-next-line
    for (const key in prop) {
        css(elem, key, prop[key]);
    }
}
//# sourceMappingURL=css.js.map