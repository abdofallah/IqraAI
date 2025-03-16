import { getVendorPrefix } from './getVendorPrefix';
import { hasCSSTransforms, hasCSS3DTransforms } from './browserSupport';
const transform = getVendorPrefix('transform');
const backfaceVisibility = getVendorPrefix('backfaceVisibility');
export const translate = (() => {
    if (hasCSSTransforms()) {
        const ua = window ? window.navigator.userAgent : '';
        const isSafari = /Safari\//.test(ua) && !/Chrome\//.test(ua);
        // It appears that Safari messes up the composition order
        // of GPU-accelerated layers
        // (see bug https://bugs.webkit.org/show_bug.cgi?id=61824).
        // Use 2D translation instead.
        if (!isSafari && hasCSS3DTransforms()) {
            return (style, x, y) => {
                style[transform] = `translate3d(${x}px,${y}px,0)`;
                style[backfaceVisibility] = 'hidden';
            };
        }
        return (style, x, y) => {
            style[transform] = `translate(${x}px,${y}px)`;
        };
    }
    return (style, x, y) => {
        style.left = `${x}px`;
        style.top = `${y}px`;
    };
})();
//# sourceMappingURL=translate.js.map