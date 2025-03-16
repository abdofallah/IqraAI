import { Platform } from '../platform';
const cssModules = [];
export function ensure(name, content) {
    const cssModule = cssModules.find((m) => m.name === name);
    if (cssModule) {
        cssModule.loadTimes += 1;
        if (cssModule.loadTimes > 1) {
            return;
        }
    }
    if (!Platform.isApplyingHMR()) {
        const styleElement = document.createElement('style');
        styleElement.setAttribute('type', 'text/css');
        styleElement.textContent = content;
        const head = document.querySelector('head');
        if (head) {
            head.insertBefore(styleElement, head.firstChild);
        }
        cssModules.push({
            name,
            loadTimes: 1,
            styleElement,
        });
    }
}
export function clean(name) {
    const index = cssModules.findIndex((m) => m.name === name);
    if (index > -1) {
        const cssModule = cssModules[index];
        cssModule.loadTimes -= 1;
        if (cssModule.loadTimes > 0) {
            return;
        }
        let styleElement = cssModule.styleElement;
        if (styleElement && styleElement.parentNode) {
            styleElement.parentNode.removeChild(styleElement);
        }
        styleElement = null;
        cssModules.splice(index, 1);
    }
}
//# sourceMappingURL=loader.js.map