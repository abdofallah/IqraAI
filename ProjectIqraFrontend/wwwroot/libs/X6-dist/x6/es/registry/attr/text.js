import { ObjectExt, NumberExt, Dom, FunctionExt, } from '@antv/x6-common';
export const text = {
    qualify(text, { attrs }) {
        return attrs.textWrap == null || !ObjectExt.isPlainObject(attrs.textWrap);
    },
    set(text, { view, elem, attrs }) {
        const cacheName = 'x6-text';
        const cache = Dom.data(elem, cacheName);
        const json = (str) => {
            try {
                return JSON.parse(str);
            }
            catch (error) {
                return str;
            }
        };
        const options = {
            x: attrs.x,
            eol: attrs.eol,
            annotations: json(attrs.annotations),
            textPath: json(attrs['text-path'] || attrs.textPath),
            textVerticalAnchor: (attrs['text-vertical-anchor'] ||
                attrs.textVerticalAnchor),
            displayEmpty: (attrs['display-empty'] || attrs.displayEmpty) === 'true',
            lineHeight: (attrs['line-height'] || attrs.lineHeight),
        };
        const fontSize = (attrs['font-size'] || attrs.fontSize);
        const textHash = JSON.stringify([text, options]);
        if (fontSize) {
            elem.setAttribute('font-size', fontSize);
        }
        // Updates the text only if there was a change in the string
        // or any of its attributes.
        if (cache == null || cache !== textHash) {
            // Text Along Path Selector
            const textPath = options.textPath;
            if (textPath != null && typeof textPath === 'object') {
                const selector = textPath.selector;
                if (typeof selector === 'string') {
                    const pathNode = view.find(selector)[0];
                    if (pathNode instanceof SVGPathElement) {
                        Dom.ensureId(pathNode);
                        options.textPath = Object.assign({ 'xlink:href': `#${pathNode.id}` }, textPath);
                    }
                }
            }
            Dom.text(elem, `${text}`, options);
            Dom.data(elem, cacheName, textHash);
        }
    },
};
export const textWrap = {
    qualify: ObjectExt.isPlainObject,
    set(val, { view, elem, attrs, refBBox }) {
        const info = val;
        // option `width`
        const width = info.width || 0;
        if (NumberExt.isPercentage(width)) {
            refBBox.width *= parseFloat(width) / 100;
        }
        else if (width <= 0) {
            refBBox.width += width;
        }
        else {
            refBBox.width = width;
        }
        // option `height`
        const height = info.height || 0;
        if (NumberExt.isPercentage(height)) {
            refBBox.height *= parseFloat(height) / 100;
        }
        else if (height <= 0) {
            refBBox.height += height;
        }
        else {
            refBBox.height = height;
        }
        // option `text`
        let wrappedText;
        let txt = info.text;
        if (txt == null) {
            // the edge of the label is assigned to txt
            txt = attrs.text || (elem === null || elem === void 0 ? void 0 : elem.textContent);
        }
        if (txt != null) {
            wrappedText = Dom.breakText(`${txt}`, refBBox, {
                'font-weight': attrs['font-weight'] || attrs.fontWeight,
                'font-size': attrs['font-size'] || attrs.fontSize,
                'font-family': attrs['font-family'] || attrs.fontFamily,
                lineHeight: attrs.lineHeight,
            }, {
                // svgDocument: view.graph.view.svg,
                ellipsis: info.ellipsis,
                // hyphen: info.hyphen as string,
                // breakWord: info.breakWord as boolean,
            });
        }
        else {
            wrappedText = '';
        }
        FunctionExt.call(text.set, this, wrappedText, {
            view,
            elem,
            attrs,
            refBBox,
            cell: view.cell,
        });
    },
};
const isTextInUse = (val, { attrs }) => {
    return attrs.text !== undefined;
};
export const lineHeight = {
    qualify: isTextInUse,
};
export const textVerticalAnchor = {
    qualify: isTextInUse,
};
export const textPath = {
    qualify: isTextInUse,
};
export const annotations = {
    qualify: isTextInUse,
};
export const eol = {
    qualify: isTextInUse,
};
export const displayEmpty = {
    qualify: isTextInUse,
};
//# sourceMappingURL=text.js.map