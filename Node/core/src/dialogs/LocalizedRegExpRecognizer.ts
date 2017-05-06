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
import { RegExpRecognizer } from './RegExpRecognizer';

export class LocalizedRegExpRecognizer implements IIntentRecognizer {
    private recognizers = <IRecognizerMap>{};

    constructor(private intent: string, private key: string, private namespace?: string) {
    }

    public recognize(context: IRecognizeContext, callback: (err: Error, result: IIntentRecognizerResult) => void): void {
        // Create recognizer for locale on first message
        var locale = context.preferredLocale();
        var recognizer = this.recognizers[locale];
        if (!recognizer) {
            var pattern = context.localizer.trygettext(locale, this.key, this.namespace);
            if (pattern) {
                var exp = new RegExp(pattern, 'i');
                this.recognizers[locale] = recognizer = new RegExpRecognizer(this.intent, exp);
            }
        }

        // Recognize utterance
        if (recognizer) {
            recognizer.recognize(context, callback);
        } else {
            callback(null, { score: 0.0, intent: null });
        }
    }
}

interface IRecognizerMap {
    [locale: string]: IIntentRecognizer;
}