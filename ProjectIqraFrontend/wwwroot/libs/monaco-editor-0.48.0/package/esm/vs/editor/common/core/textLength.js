/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
import { Position } from './position.js';
import { Range } from './range.js';
/**
 * Represents a non-negative length of text in terms of line and column count.
*/
export class TextLength {
    static ofText(text) {
        let line = 0;
        let column = 0;
        for (const c of text) {
            if (c === '\n') {
                line++;
                column = 0;
            }
            else {
                column++;
            }
        }
        return new TextLength(line, column);
    }
    constructor(lineCount, columnCount) {
        this.lineCount = lineCount;
        this.columnCount = columnCount;
    }
    createRange(startPosition) {
        if (this.lineCount === 0) {
            return new Range(startPosition.lineNumber, startPosition.column, startPosition.lineNumber, startPosition.column + this.columnCount);
        }
        else {
            return new Range(startPosition.lineNumber, startPosition.column, startPosition.lineNumber + this.lineCount, this.columnCount + 1);
        }
    }
    addToPosition(position) {
        if (this.lineCount === 0) {
            return new Position(position.lineNumber, position.column + this.columnCount);
        }
        else {
            return new Position(position.lineNumber + this.lineCount, this.columnCount + 1);
        }
    }
    toString() {
        return `${this.lineCount},${this.columnCount}`;
    }
}
TextLength.zero = new TextLength(0, 0);
