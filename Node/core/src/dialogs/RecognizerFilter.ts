// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Bot Framework: http://botframework.com
// 
// Bot Builder SDK Github:
// https://github.com/Microsoft/BotBuilder
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

import { IRecognizeContext } from './IntentRecognizerSet';
import { IIntentRecognizer, IIntentRecognizerResult } from './IntentRecognizerSet';

export interface IRecognizerFilterOnEnabled {
    (context: IRecognizeContext, callback: (err: Error, enabled: boolean) => void): void;
}

export interface IRecognizerFilterOnRecognized {
    (context: IRecognizeContext, result: IIntentRecognizerResult, callback: (err: Error, result: IIntentRecognizerResult) => void): void;
}

export class RecognizerFilter implements IIntentRecognizer {
    private _onEnabled: IRecognizerFilterOnEnabled;
    private _onRecognized: IRecognizerFilterOnRecognized;

    constructor(private recognizer: IIntentRecognizer) {
    }

    public recognize(context: IRecognizeContext, callback: (err: Error, result: IIntentRecognizerResult) => void): void {
        this.isEnabled(context, (err, enabled) => {
            if (!err && enabled) {
                this.recognizer.recognize(context, (err, result) => {
                    if (!err && result && result.score > 0 && this._onRecognized) {
                        this._onRecognized(context, result, callback);
                    } else {
                        callback(err, result);
                    }
                });
            } else {
                callback(err, { score: 0.0, intent: null });
            }
        });
    }

    public onEnabled(handler: IRecognizerFilterOnEnabled): this {
        this._onEnabled = handler;
        return this;
    }

    public onRecognized(handler: IRecognizerFilterOnRecognized): this {
        this._onRecognized = handler;
        return this;
    }

    private isEnabled(context: IRecognizeContext, cb: (err: Error, enabled: boolean) => void): void {
        if (this._onEnabled) {
            this._onEnabled(context, cb);
        } else {
            cb(null, true);
        }
    }
}

