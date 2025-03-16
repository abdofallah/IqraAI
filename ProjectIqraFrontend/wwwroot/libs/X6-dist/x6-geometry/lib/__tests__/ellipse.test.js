"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const __1 = require("..");
describe('ellipse', () => {
    describe('#constructor', () => {
        it('should create an ellipse instance', () => {
            expect(new __1.Ellipse()).toBeInstanceOf(__1.Ellipse);
            expect(new __1.Ellipse(1)).toBeInstanceOf(__1.Ellipse);
            expect(new __1.Ellipse(1, 2)).toBeInstanceOf(__1.Ellipse);
            expect(new __1.Ellipse(1, 2, 3)).toBeInstanceOf(__1.Ellipse);
            expect(new __1.Ellipse(1, 2, 3, 4)).toBeInstanceOf(__1.Ellipse);
            expect(new __1.Ellipse(1, 2, 3, 4).x).toEqual(1);
            expect(new __1.Ellipse(1, 2, 3, 4).y).toEqual(2);
            expect(new __1.Ellipse(1, 2, 3, 4).a).toEqual(3);
            expect(new __1.Ellipse(1, 2, 3, 4).b).toEqual(4);
            expect(new __1.Ellipse().equals(new __1.Ellipse(0, 0, 0, 0)));
        });
        it('should return the center point', () => {
            const ellipse = new __1.Ellipse(1, 2, 3, 4);
            expect(ellipse.getCenter().equals({ x: 1, y: 2 }));
            expect(ellipse.center.equals({ x: 1, y: 2 }));
        });
    });
    describe('#Ellipse.create', () => {
        it('should create an ellipse from number', () => {
            const ellipse = __1.Ellipse.create(1, 2, 3, 4);
            expect(ellipse.x).toEqual(1);
            expect(ellipse.y).toEqual(2);
            expect(ellipse.a).toEqual(3);
            expect(ellipse.b).toEqual(4);
        });
        it('should create an ellipse from Ellipse instance', () => {
            const ellipse = __1.Ellipse.create(new __1.Ellipse(1, 2, 3, 4));
            expect(ellipse.x).toEqual(1);
            expect(ellipse.y).toEqual(2);
            expect(ellipse.a).toEqual(3);
            expect(ellipse.b).toEqual(4);
        });
        it('should create an ellipse from EllipseLike', () => {
            const ellipse = __1.Ellipse.create({ x: 1, y: 2, a: 3, b: 4 });
            expect(ellipse.x).toEqual(1);
            expect(ellipse.y).toEqual(2);
            expect(ellipse.a).toEqual(3);
            expect(ellipse.b).toEqual(4);
        });
        it('should create an ellipse from EllipseData', () => {
            const ellipse = __1.Ellipse.create([1, 2, 3, 4]);
            expect(ellipse.x).toEqual(1);
            expect(ellipse.y).toEqual(2);
            expect(ellipse.a).toEqual(3);
            expect(ellipse.b).toEqual(4);
        });
    });
    describe('#Ellipse.fromRect', () => {
        it('should create an ellipse from a rectangle instance', () => {
            const ellipse = __1.Ellipse.fromRect(new __1.Rectangle(1, 2, 3, 4));
            expect(ellipse.x).toEqual(2.5);
            expect(ellipse.y).toEqual(4);
            expect(ellipse.a).toEqual(1.5);
            expect(ellipse.b).toEqual(2);
        });
    });
    describe('#bbox', () => {
        it('should return the bbox', () => {
            expect(new __1.Ellipse(1, 2, 3, 4).bbox().toJSON()).toEqual({
                x: -2,
                y: -2,
                width: 6,
                height: 8,
            });
        });
    });
    describe('#inflate', () => {
        it('should inflate with the given `amount`', () => {
            expect(new __1.Ellipse(1, 2, 3, 4).inflate(2).toJSON()).toEqual({
                x: 1,
                y: 2,
                a: 7,
                b: 8,
            });
        });
        it('should inflate with the given `dx` and `dy`', () => {
            expect(new __1.Ellipse(1, 2, 3, 4).inflate(2, 3).toJSON()).toEqual({
                x: 1,
                y: 2,
                a: 7,
                b: 10,
            });
        });
    });
    describe('#normalizedDistance', () => {
        it('should return a normalized distance', () => {
            const ellipse = new __1.Ellipse(0, 0, 3, 4);
            expect(ellipse.normalizedDistance(1, 1) < 1).toBeTruthy();
            expect(ellipse.normalizedDistance({ x: 5, y: 5 }) > 1).toBeTruthy();
            expect(ellipse.normalizedDistance([0, 4]) === 1).toBeTruthy();
        });
    });
    describe('#containsPoint', () => {
        const ellipse = new __1.Ellipse(0, 0, 3, 4);
        it('shoule return true when ellipse contains the given point', () => {
            expect(ellipse.containsPoint(1, 1)).toBeTruthy();
        });
        it('shoule return true when the given point is on the boundary of the ellipse', () => {
            expect(ellipse.containsPoint([0, 4])).toBeTruthy();
        });
        it('shoule return true when ellipse not contains the given point', () => {
            expect(ellipse.containsPoint({ x: 5, y: 5 })).toBeFalsy();
        });
    });
    describe('#intersectsWithLine', () => {
        const ellipse = new __1.Ellipse(0, 0, 3, 4);
        it('should return the intersections with line', () => {
            expect(__1.Point.equalPoints(ellipse.intersectsWithLine(new __1.Line(0, -5, 0, 5)), [
                { x: 0, y: -4 },
                { x: 0, y: 4 },
            ])).toBeTruthy();
            expect(__1.Point.equalPoints(ellipse.intersectsWithLine(new __1.Line(0, 0, 0, 5)), [
                { x: 0, y: 4 },
            ])).toBeTruthy();
            expect(__1.Point.equalPoints(ellipse.intersectsWithLine(new __1.Line(3, 0, 3, 4)), [
                { x: 3, y: 0 },
            ])).toBeTruthy();
        });
        it('should return null when not intersections', () => {
            expect(ellipse.intersectsWithLine(new __1.Line(0, 0, 1, 1))).toBeNull();
            expect(ellipse.intersectsWithLine(new __1.Line(3, 5, 3, 6))).toBeNull();
            expect(ellipse.intersectsWithLine(new __1.Line(-6, -6, -6, 100))).toBeNull();
            expect(ellipse.intersectsWithLine(new __1.Line(6, 6, 100, 100))).toBeNull();
        });
    });
    describe('#intersectsWithLineFromCenterToPoint', () => {
        const ellipse = new __1.Ellipse(0, 0, 3, 4);
        it('should return the intersection point when the given point is outside of the ellipse', () => {
            expect(ellipse
                .intersectsWithLineFromCenterToPoint({ x: 0, y: 6 })
                .round()
                .toJSON()).toEqual({ x: 0, y: 4 });
            expect(ellipse
                .intersectsWithLineFromCenterToPoint({ x: 6, y: 0 })
                .round()
                .toJSON()).toEqual({ x: 3, y: 0 });
            expect(ellipse
                .intersectsWithLineFromCenterToPoint({ x: 0, y: 6 }, 90)
                .round()
                .toJSON()).toEqual({ x: 0, y: 3 });
        });
        it('should return the intersection point when the given point is inside of the ellipse', () => {
            expect(ellipse
                .intersectsWithLineFromCenterToPoint({ x: 0, y: 0 }, 90)
                .round()
                .toJSON()).toEqual({ x: -0, y: -3 });
            expect(ellipse
                .intersectsWithLineFromCenterToPoint({ x: 0, y: 2 })
                .round()
                .toJSON()).toEqual({ x: 0, y: 4 });
            expect(ellipse
                .intersectsWithLineFromCenterToPoint({ x: 0, y: 2 }, 90)
                .round()
                .toJSON()).toEqual({ x: 0, y: 3 });
            expect(ellipse
                .intersectsWithLineFromCenterToPoint({ x: 2, y: 0 }, 90)
                .round()
                .toJSON()).toEqual({ x: 4, y: -0 });
        });
    });
    describe('#tangentTheta', () => {
        it('should return the tangent theta', () => {
            const ellipse = new __1.Ellipse(0, 0, 3, 4);
            expect(ellipse.tangentTheta({ x: 3, y: 0 })).toEqual(270);
            expect(ellipse.tangentTheta({ x: -3, y: 0 })).toEqual(90);
            expect(ellipse.tangentTheta({ x: 0, y: 4 })).toEqual(180);
            expect(ellipse.tangentTheta({ x: 0, y: -4 })).toEqual(-0);
        });
    });
    describe('#scale', () => {
        it('should scale the ellipse with the given `sx` and `sy`', () => {
            const ellipse = new __1.Ellipse(1, 2, 3, 4).scale(3, 4);
            expect(ellipse.x).toEqual(1);
            expect(ellipse.y).toEqual(2);
            expect(ellipse.a).toEqual(9);
            expect(ellipse.b).toEqual(16);
        });
    });
    describe('#rotate', () => {
        it('should rotate the ellipse', () => {
            const ellipse = new __1.Ellipse(0, 0, 3, 4).rotate(90);
            expect(ellipse.x).toEqual(0);
            expect(ellipse.y).toEqual(0);
            expect(ellipse.a).toEqual(4);
            expect(Math.round(ellipse.b)).toEqual(3);
        });
    });
    describe('#translate', () => {
        it('should translate the ellipse with the given `dx` and `dy`', () => {
            const ellipse = new __1.Ellipse(1, 2, 3, 4).translate(3, 4);
            expect(ellipse.x).toEqual(4);
            expect(ellipse.y).toEqual(6);
            expect(ellipse.a).toEqual(3);
            expect(ellipse.b).toEqual(4);
        });
    });
    describe('#clone', () => {
        it('should return the cloned ellipse', () => {
            const ellipse = new __1.Ellipse(1, 2, 3, 4).clone();
            expect(ellipse.x).toEqual(1);
            expect(ellipse.y).toEqual(2);
            expect(ellipse.a).toEqual(3);
            expect(ellipse.b).toEqual(4);
        });
    });
    describe('#serialize', () => {
        it('should return the serialized string', () => {
            expect(new __1.Ellipse(1, 2, 3, 4).serialize()).toEqual('1 2 3 4');
        });
    });
});
//# sourceMappingURL=ellipse.test.js.map