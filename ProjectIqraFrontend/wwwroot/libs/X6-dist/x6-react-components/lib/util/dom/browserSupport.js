"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.hasCSS3DTransforms = exports.hasCSSTransforms = exports.hasCSSTransitions = exports.hasCSSAnimations = void 0;
const getVendorPrefix_1 = require("./getVendorPrefix");
const hasCSSAnimations = () => !!(0, getVendorPrefix_1.getVendorPrefix)('animationName');
exports.hasCSSAnimations = hasCSSAnimations;
const hasCSSTransitions = () => !!(0, getVendorPrefix_1.getVendorPrefix)('transition');
exports.hasCSSTransitions = hasCSSTransitions;
const hasCSSTransforms = () => !!(0, getVendorPrefix_1.getVendorPrefix)('transform');
exports.hasCSSTransforms = hasCSSTransforms;
const hasCSS3DTransforms = () => !!(0, getVendorPrefix_1.getVendorPrefix)('perspective');
exports.hasCSS3DTransforms = hasCSS3DTransforms;
//# sourceMappingURL=browserSupport.js.map