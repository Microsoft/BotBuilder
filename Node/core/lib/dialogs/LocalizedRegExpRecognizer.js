"use strict";
var RegExpRecognizer_1 = require("./RegExpRecognizer");
var LocalizedRegExpRecognizer = (function () {
    function LocalizedRegExpRecognizer(intent, key, namespace) {
        this.intent = intent;
        this.key = key;
        this.namespace = namespace;
        this.recognizers = {};
    }
    LocalizedRegExpRecognizer.prototype.recognize = function (context, callback) {
        var locale = context.preferredLocale();
        var recognizer = this.recognizers[locale];
        if (!recognizer) {
            var pattern = context.localizer.trygettext(locale, this.key, this.namespace);
            if (pattern) {
                var exp = new RegExp(pattern, 'i');
                this.recognizers[locale] = recognizer = new RegExpRecognizer_1.RegExpRecognizer(this.intent, exp);
            }
        }
        if (recognizer) {
            recognizer.recognize(context, callback);
        }
        else {
            callback(null, { score: 0.0, intent: null });
        }
    };
    return LocalizedRegExpRecognizer;
}());
exports.LocalizedRegExpRecognizer = LocalizedRegExpRecognizer;
