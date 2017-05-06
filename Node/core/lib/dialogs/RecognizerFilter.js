"use strict";
var RecognizerFilter = (function () {
    function RecognizerFilter(recognizer) {
        this.recognizer = recognizer;
    }
    RecognizerFilter.prototype.recognize = function (context, callback) {
        var _this = this;
        this.isEnabled(context, function (err, enabled) {
            if (!err && enabled) {
                _this.recognizer.recognize(context, function (err, result) {
                    if (!err && result && result.score > 0 && _this._onRecognized) {
                        _this._onRecognized(context, result, callback);
                    }
                    else {
                        callback(err, result);
                    }
                });
            }
            else {
                callback(err, { score: 0.0, intent: null });
            }
        });
    };
    RecognizerFilter.prototype.onEnabled = function (handler) {
        this._onEnabled = handler;
        return this;
    };
    RecognizerFilter.prototype.onRecognized = function (handler) {
        this._onRecognized = handler;
        return this;
    };
    RecognizerFilter.prototype.isEnabled = function (context, cb) {
        if (this._onEnabled) {
            this._onEnabled(context, cb);
        }
        else {
            cb(null, true);
        }
    };
    return RecognizerFilter;
}());
exports.RecognizerFilter = RecognizerFilter;
