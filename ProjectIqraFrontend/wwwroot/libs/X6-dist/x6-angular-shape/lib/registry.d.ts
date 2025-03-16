import { Injector, TemplateRef, Type } from '@angular/core';
import { Node } from '@antv/x6';
export type Content = TemplateRef<any> | Type<any>;
export type AngularShapeConfig = Node.Properties & {
    shape: string;
    injector: Injector;
    content: Content;
};
export declare const registerInfo: Map<string, {
    injector: Injector;
    content: Content;
}>;
export declare function register(config: AngularShapeConfig): void;
