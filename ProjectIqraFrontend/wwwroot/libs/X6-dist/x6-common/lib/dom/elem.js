"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.children = exports.isHTMLElement = exports.isElement = exports.appendTo = exports.after = exports.before = exports.prepend = exports.append = exports.empty = exports.remove = exports.contains = exports.findParentByClass = exports.findOne = exports.find = exports.index = exports.tagName = exports.parseXML = exports.createSvgDocument = exports.createSvgElement = exports.createElementNS = exports.createElement = exports.svgVersion = exports.ns = exports.isSVGGraphicsElement = exports.ensureId = exports.uniqueId = void 0;
const class_1 = require("./class");
let idCounter = 0;
function uniqueId() {
    idCounter += 1;
    return `v${idCounter}`;
}
exports.uniqueId = uniqueId;
function ensureId(elem) {
    if (elem.id == null || elem.id === '') {
        elem.id = uniqueId();
    }
    return elem.id;
}
exports.ensureId = ensureId;
/**
 * Returns true if object is an instance of SVGGraphicsElement.
 * @see https://developer.mozilla.org/en-US/docs/Web/API/SVGGraphicsElement
 */
function isSVGGraphicsElement(elem) {
    if (elem == null) {
        return false;
    }
    return typeof elem.getScreenCTM === 'function' && elem instanceof SVGElement;
}
exports.isSVGGraphicsElement = isSVGGraphicsElement;
exports.ns = {
    svg: 'http://www.w3.org/2000/svg',
    xmlns: 'http://www.w3.org/2000/xmlns/',
    xml: 'http://www.w3.org/XML/1998/namespace',
    xlink: 'http://www.w3.org/1999/xlink',
    xhtml: 'http://www.w3.org/1999/xhtml',
};
exports.svgVersion = '1.1';
function createElement(tagName, doc = document) {
    return doc.createElement(tagName);
}
exports.createElement = createElement;
function createElementNS(tagName, namespaceURI = exports.ns.xhtml, doc = document) {
    return doc.createElementNS(namespaceURI, tagName);
}
exports.createElementNS = createElementNS;
function createSvgElement(tagName, doc = document) {
    return createElementNS(tagName, exports.ns.svg, doc);
}
exports.createSvgElement = createSvgElement;
function createSvgDocument(content) {
    if (content) {
        const xml = `<svg xmlns="${exports.ns.svg}" xmlns:xlink="${exports.ns.xlink}" version="${exports.svgVersion}">${content}</svg>`; // lgtm[js/html-constructed-from-input]
        const { documentElement } = parseXML(xml, { async: false });
        return documentElement;
    }
    const svg = document.createElementNS(exports.ns.svg, 'svg');
    svg.setAttributeNS(exports.ns.xmlns, 'xmlns:xlink', exports.ns.xlink);
    svg.setAttribute('version', exports.svgVersion);
    return svg;
}
exports.createSvgDocument = createSvgDocument;
function parseXML(data, options = {}) {
    let xml;
    try {
        const parser = new DOMParser();
        if (options.async != null) {
            const instance = parser;
            instance.async = options.async;
        }
        xml = parser.parseFromString(data, options.mimeType || 'text/xml');
    }
    catch (error) {
        xml = undefined;
    }
    if (!xml || xml.getElementsByTagName('parsererror').length) {
        throw new Error(`Invalid XML: ${data}`);
    }
    return xml;
}
exports.parseXML = parseXML;
function tagName(node, lowercase = true) {
    const nodeName = node.nodeName;
    return lowercase ? nodeName.toLowerCase() : nodeName.toUpperCase();
}
exports.tagName = tagName;
function index(elem) {
    let index = 0;
    let node = elem.previousSibling;
    while (node) {
        if (node.nodeType === 1) {
            index += 1;
        }
        node = node.previousSibling;
    }
    return index;
}
exports.index = index;
function find(elem, selector) {
    return elem.querySelectorAll(selector);
}
exports.find = find;
function findOne(elem, selector) {
    return elem.querySelector(selector);
}
exports.findOne = findOne;
function findParentByClass(elem, className, terminator) {
    const ownerSVGElement = elem.ownerSVGElement;
    let node = elem.parentNode;
    while (node && node !== terminator && node !== ownerSVGElement) {
        if ((0, class_1.hasClass)(node, className)) {
            return node;
        }
        node = node.parentNode;
    }
    return null;
}
exports.findParentByClass = findParentByClass;
function contains(parent, child) {
    const bup = child && child.parentNode;
    return (parent === bup ||
        !!(bup && bup.nodeType === 1 && parent.compareDocumentPosition(bup) & 16) // eslint-disable-line no-bitwise
    );
}
exports.contains = contains;
function remove(elem) {
    if (elem) {
        const elems = Array.isArray(elem) ? elem : [elem];
        elems.forEach((item) => {
            if (item.parentNode) {
                item.parentNode.removeChild(item);
            }
        });
    }
}
exports.remove = remove;
function empty(elem) {
    while (elem.firstChild) {
        elem.removeChild(elem.firstChild);
    }
}
exports.empty = empty;
function append(elem, elems) {
    const arr = Array.isArray(elems) ? elems : [elems];
    arr.forEach((child) => {
        if (child != null) {
            elem.appendChild(child);
        }
    });
}
exports.append = append;
function prepend(elem, elems) {
    const child = elem.firstChild;
    return child ? before(child, elems) : append(elem, elems);
}
exports.prepend = prepend;
function before(elem, elems) {
    const parent = elem.parentNode;
    if (parent) {
        const arr = Array.isArray(elems) ? elems : [elems];
        arr.forEach((child) => {
            if (child != null) {
                parent.insertBefore(child, elem);
            }
        });
    }
}
exports.before = before;
function after(elem, elems) {
    const parent = elem.parentNode;
    if (parent) {
        const arr = Array.isArray(elems) ? elems : [elems];
        arr.forEach((child) => {
            if (child != null) {
                parent.insertBefore(child, elem.nextSibling);
            }
        });
    }
}
exports.after = after;
function appendTo(elem, target) {
    if (target != null) {
        target.appendChild(elem);
    }
}
exports.appendTo = appendTo;
function isElement(x) {
    return !!x && x.nodeType === 1;
}
exports.isElement = isElement;
// Determines whether a node is an HTML node
function isHTMLElement(elem) {
    try {
        // Using W3 DOM2 (works for FF, Opera and Chrome)
        return elem instanceof HTMLElement;
    }
    catch (e) {
        // Browsers not supporting W3 DOM2 don't have HTMLElement and
        // an exception is thrown and we end up here. Testing some
        // properties that all elements have (works on IE7)
        return (typeof elem === 'object' &&
            elem.nodeType === 1 &&
            typeof elem.style === 'object' &&
            typeof elem.ownerDocument === 'object');
    }
}
exports.isHTMLElement = isHTMLElement;
function children(parent, className) {
    const matched = [];
    let elem = parent.firstChild;
    for (; elem; elem = elem.nextSibling) {
        if (elem.nodeType === 1) {
            if (!className || (0, class_1.hasClass)(elem, className)) {
                matched.push(elem);
            }
        }
    }
    return matched;
}
exports.children = children;
//# sourceMappingURL=elem.js.map