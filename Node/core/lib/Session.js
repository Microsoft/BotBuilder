"use strict";
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var Dialog_1 = require("./dialogs/Dialog");
var Message_1 = require("./Message");
var consts = require("./consts");
var sprintf = require("sprintf-js");
var events = require("events");
var async = require("async");
var Session = (function (_super) {
    __extends(Session, _super);
    function Session(options) {
        var _this = _super.call(this) || this;
        _this.options = options;
        _this.msgSent = false;
        _this._isReset = false;
        _this.lastSendTime = new Date().getTime();
        _this.batch = [];
        _this.batchStarted = false;
        _this.sendingBatch = false;
        _this.inMiddleware = false;
        _this._locale = null;
        _this.localizer = null;
        _this.logger = null;
        _this.library = options.library;
        _this.localizer = options.localizer;
        _this.logger = options.logger;
        if (typeof _this.options.autoBatchDelay !== 'number') {
            _this.options.autoBatchDelay = 250;
        }
        return _this;
    }
    Session.prototype.toRecognizeContext = function () {
        var _this = this;
        return {
            message: this.message,
            userData: this.userData,
            conversationData: this.conversationData,
            privateConversationData: this.privateConversationData,
            dialogData: this.dialogData,
            localizer: this.localizer,
            logger: this.logger,
            dialogStack: function () { return _this.dialogStack(); },
            preferredLocale: function () { return _this.preferredLocale(); },
            gettext: function () {
                var args = [];
                for (var _i = 0; _i < arguments.length; _i++) {
                    args[_i] = arguments[_i];
                }
                return Session.prototype.gettext.call(_this, args);
            },
            ngettext: function () {
                var args = [];
                for (var _i = 0; _i < arguments.length; _i++) {
                    args[_i] = arguments[_i];
                }
                return Session.prototype.ngettext.call(_this, args);
            },
            locale: this.preferredLocale()
        };
    };
    Session.prototype.dispatch = function (sessionState, message, done) {
        var _this = this;
        var index = 0;
        var session = this;
        var now = new Date().getTime();
        var middleware = this.options.middleware || [];
        var next = function () {
            var handler = index < middleware.length ? middleware[index] : null;
            if (handler) {
                index++;
                handler(session, next);
            }
            else {
                _this.inMiddleware = false;
                _this.sessionState.lastAccess = now;
                done();
            }
        };
        this.sessionState = sessionState || { callstack: [], lastAccess: now, version: 0.0 };
        var cur = this.curDialog();
        if (cur) {
            this.dialogData = cur.state;
        }
        this.inMiddleware = true;
        this.message = (message || { text: '' });
        if (!this.message.type) {
            this.message.type = consts.messageType;
        }
        var locale = this.preferredLocale();
        this.localizer.load(locale, function (err) {
            if (err) {
                _this.error(err);
            }
            else {
                next();
            }
        });
        return this;
    };
    Session.prototype.error = function (err) {
        var m = err.toString();
        err = err instanceof Error ? err : new Error(m);
        this.emit('error', err);
        this.logger.error(this.dialogStack(), err);
        if (this.options.dialogErrorMessage) {
            this.endConversation(this.options.dialogErrorMessage);
        }
        else {
            var locale = this.preferredLocale();
            this.endConversation(this.localizer.gettext(locale, 'default_error', consts.Library.system));
        }
        return this;
    };
    Session.prototype.preferredLocale = function (locale, callback) {
        if (locale) {
            this._locale = locale;
            if (this.userData) {
                this.userData[consts.Data.PreferredLocale] = locale;
            }
            if (this.localizer) {
                this.localizer.load(locale, callback);
            }
        }
        else if (!this._locale) {
            if (this.userData && this.userData[consts.Data.PreferredLocale]) {
                this._locale = this.userData[consts.Data.PreferredLocale];
            }
            else if (this.message && this.message.textLocale) {
                this._locale = this.message.textLocale;
            }
            else if (this.localizer) {
                this._locale = this.localizer.defaultLocale();
            }
        }
        return this._locale;
    };
    Session.prototype.gettext = function (messageid) {
        var args = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            args[_i - 1] = arguments[_i];
        }
        return this.vgettext(this.curLibraryName(), messageid, args);
    };
    Session.prototype.ngettext = function (messageid, messageid_plural, count) {
        var tmpl;
        if (this.localizer && this.message) {
            tmpl = this.localizer.ngettext(this.preferredLocale(), messageid, messageid_plural, count, this.curLibraryName());
        }
        else if (count == 1) {
            tmpl = messageid;
        }
        else {
            tmpl = messageid_plural;
        }
        return sprintf.sprintf(tmpl, count);
    };
    Session.prototype.save = function () {
        this.logger.log(this.dialogStack(), 'Session.save()');
        this.startBatch();
        return this;
    };
    Session.prototype.send = function (message) {
        var args = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            args[_i - 1] = arguments[_i];
        }
        args.unshift(this.curLibraryName(), message);
        return Session.prototype.sendLocalized.apply(this, args);
    };
    Session.prototype.sendLocalized = function (localizationNamespace, message) {
        var args = [];
        for (var _i = 2; _i < arguments.length; _i++) {
            args[_i - 2] = arguments[_i];
        }
        this.msgSent = true;
        if (message) {
            var m;
            if (typeof message == 'string' || Array.isArray(message)) {
                m = this.createMessage(localizationNamespace, message, args);
            }
            else if (message.toMessage) {
                m = message.toMessage();
            }
            else {
                m = message;
            }
            this.prepareMessage(m);
            this.batch.push(m);
            this.logger.log(this.dialogStack(), 'Session.send()');
        }
        this.startBatch();
        return this;
    };
    Session.prototype.sendTyping = function () {
        this.msgSent = true;
        var m = { type: 'typing' };
        this.prepareMessage(m);
        this.batch.push(m);
        this.logger.log(this.dialogStack(), 'Session.sendTyping()');
        this.sendBatch();
        return this;
    };
    Session.prototype.messageSent = function () {
        return this.msgSent;
    };
    Session.prototype.beginDialog = function (id, args) {
        this.logger.log(this.dialogStack(), 'Session.beginDialog(' + id + ')');
        var id = this.resolveDialogId(id);
        var dialog = this.findDialog(id);
        if (!dialog) {
            throw new Error('Dialog[' + id + '] not found.');
        }
        this.pushDialog({ id: id, state: {} });
        this.startBatch();
        dialog.begin(this, args);
        return this;
    };
    Session.prototype.replaceDialog = function (id, args) {
        this.logger.log(this.dialogStack(), 'Session.replaceDialog(' + id + ')');
        var id = this.resolveDialogId(id);
        var dialog = this.findDialog(id);
        if (!dialog) {
            throw new Error('Dialog[' + id + '] not found.');
        }
        this.popDialog();
        this.pushDialog({ id: id, state: {} });
        this.startBatch();
        dialog.begin(this, args);
        return this;
    };
    Session.prototype.endConversation = function (message) {
        var args = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            args[_i - 1] = arguments[_i];
        }
        var m;
        if (message) {
            if (typeof message == 'string' || Array.isArray(message)) {
                m = this.createMessage(this.curLibraryName(), message, args);
            }
            else if (message.toMessage) {
                m = message.toMessage();
            }
            else {
                m = message;
            }
            this.msgSent = true;
            this.prepareMessage(m);
            this.batch.push(m);
        }
        this.privateConversationData = {};
        this.logger.log(this.dialogStack(), 'Session.endConversation()');
        var ss = this.sessionState;
        ss.callstack = [];
        this.sendBatch();
        return this;
    };
    Session.prototype.endDialog = function (message) {
        var args = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            args[_i - 1] = arguments[_i];
        }
        if (typeof message === 'object' && (message.hasOwnProperty('response') || message.hasOwnProperty('resumed') || message.hasOwnProperty('error'))) {
            console.warn('Returning results via Session.endDialog() is deprecated. Use Session.endDialogWithResult() instead.');
            return this.endDialogWithResult(message);
        }
        var cur = this.curDialog();
        if (cur) {
            var m;
            if (message) {
                if (typeof message == 'string' || Array.isArray(message)) {
                    m = this.createMessage(this.curLibraryName(), message, args);
                }
                else if (message.toMessage) {
                    m = message.toMessage();
                }
                else {
                    m = message;
                }
                this.msgSent = true;
                this.prepareMessage(m);
                this.batch.push(m);
            }
            this.logger.log(this.dialogStack(), 'Session.endDialog()');
            var childId = cur.id;
            cur = this.popDialog();
            this.startBatch();
            if (cur) {
                var dialog = this.findDialog(cur.id);
                if (dialog) {
                    dialog.dialogResumed(this, { resumed: Dialog_1.ResumeReason.completed, response: true, childId: childId });
                }
                else {
                    this.error(new Error("Can't resume missing parent dialog '" + cur.id + "'."));
                }
            }
        }
        return this;
    };
    Session.prototype.endDialogWithResult = function (result) {
        var cur = this.curDialog();
        if (cur) {
            result = result || {};
            if (!result.hasOwnProperty('resumed')) {
                result.resumed = Dialog_1.ResumeReason.completed;
            }
            result.childId = cur.id;
            this.logger.log(this.dialogStack(), 'Session.endDialogWithResult()');
            cur = this.popDialog();
            this.startBatch();
            if (cur) {
                var dialog = this.findDialog(cur.id);
                if (dialog) {
                    dialog.dialogResumed(this, result);
                }
                else {
                    this.error(new Error("Can't resume missing parent dialog '" + cur.id + "'."));
                }
            }
        }
        return this;
    };
    Session.prototype.cancelDialog = function (dialogId, replaceWithId, replaceWithArgs) {
        var childId = typeof dialogId === 'number' ? this.sessionState.callstack[dialogId].id : dialogId;
        var cur = this.deleteDialogs(dialogId);
        if (replaceWithId) {
            this.logger.log(this.dialogStack(), 'Session.cancelDialog(' + replaceWithId + ')');
            var id = this.resolveDialogId(replaceWithId);
            var dialog = this.findDialog(id);
            this.pushDialog({ id: id, state: {} });
            this.startBatch();
            dialog.begin(this, replaceWithArgs);
        }
        else {
            this.logger.log(this.dialogStack(), 'Session.cancelDialog()');
            this.startBatch();
            if (cur) {
                var dialog = this.findDialog(cur.id);
                if (dialog) {
                    dialog.dialogResumed(this, { resumed: Dialog_1.ResumeReason.canceled, response: null, childId: childId });
                }
                else {
                    this.error(new Error("Can't resume missing parent dialog '" + cur.id + "'."));
                }
            }
        }
        return this;
    };
    Session.prototype.reset = function (dialogId, dialogArgs) {
        this.logger.log(this.dialogStack(), 'Session.reset()');
        this._isReset = true;
        this.sessionState.callstack = [];
        if (!dialogId) {
            dialogId = this.options.dialogId;
            dialogArgs = this.options.dialogArgs;
        }
        this.beginDialog(dialogId, dialogArgs);
        return this;
    };
    Session.prototype.isReset = function () {
        return this._isReset;
    };
    Session.prototype.sendBatch = function (callback) {
        var _this = this;
        this.logger.log(this.dialogStack(), 'Session.sendBatch() sending ' + this.batch.length + ' message(s)');
        if (this.sendingBatch) {
            return;
        }
        if (this.batchTimer) {
            clearTimeout(this.batchTimer);
            this.batchTimer = null;
        }
        this.batchTimer = null;
        var batch = this.batch;
        this.batch = [];
        this.batchStarted = false;
        this.sendingBatch = true;
        var cur = this.curDialog();
        if (cur) {
            cur.state = this.dialogData;
        }
        this.onSave(function (err) {
            if (!err) {
                _this.onSend(batch, function (err) {
                    _this.onFinishBatch(function () {
                        if (_this.batchStarted) {
                            _this.startBatch();
                        }
                        if (callback) {
                            callback(err);
                        }
                    });
                });
            }
            else {
                _this.onFinishBatch(function () {
                    if (callback) {
                        callback(err);
                    }
                });
            }
        });
    };
    Session.prototype.dialogStack = function (newStack) {
        var stack;
        if (newStack) {
            stack = this.sessionState.callstack = newStack;
            this.dialogData = stack.length > 0 ? stack[stack.length - 1].state : null;
        }
        else {
            stack = this.sessionState.callstack || [];
            if (stack.length > 0) {
                stack[stack.length - 1].state = this.dialogData || {};
            }
        }
        return stack.slice(0);
    };
    Session.prototype.clearDialogStack = function () {
        this.sessionState.callstack = [];
        this.dialogData = null;
        return this;
    };
    Session.forEachDialogStackEntry = function (stack, reverse, fn) {
        var step = reverse ? -1 : 1;
        var l = stack ? stack.length : 0;
        for (var i = step > 0 ? 0 : l - 1; i >= 0 && i < l; i += step) {
            fn(stack[i], i);
        }
    };
    Session.findDialogStackEntry = function (stack, dialogId, reverse) {
        if (reverse === void 0) { reverse = false; }
        var step = reverse ? -1 : 1;
        var l = stack ? stack.length : 0;
        for (var i = step > 0 ? 0 : l - 1; i >= 0 && i < l; i += step) {
            if (stack[i].id === dialogId) {
                return i;
            }
        }
        return -1;
    };
    Session.activeDialogStackEntry = function (stack) {
        return stack && stack.length > 0 ? stack[stack.length - 1] : null;
    };
    Session.pushDialogStackEntry = function (stack, entry) {
        if (!entry.state) {
            entry.state = {};
        }
        stack = stack || [];
        stack.push(entry);
        return entry;
    };
    Session.popDialogStackEntry = function (stack) {
        if (stack && stack.length > 0) {
            stack.pop();
        }
        return Session.activeDialogStackEntry(stack);
    };
    Session.pruneDialogStack = function (stack, start) {
        if (stack && stack.length > 0) {
            stack.splice(start);
        }
        return Session.activeDialogStackEntry(stack);
    };
    Session.validateDialogStack = function (stack, root) {
        var valid = true;
        Session.forEachDialogStackEntry(stack, false, function (entry) {
            var pair = entry.id.split(':');
            if (!root.findDialog(pair[0], pair[1])) {
                valid = false;
            }
        });
        return valid;
    };
    Session.prototype.routeToActiveDialog = function (recognizeResult) {
        var dialogStack = this.dialogStack();
        if (Session.validateDialogStack(dialogStack, this.library)) {
            var active = Session.activeDialogStackEntry(dialogStack);
            if (active) {
                var dialog = this.findDialog(active.id);
                dialog.replyReceived(this, recognizeResult);
            }
            else {
                this.beginDialog(this.options.dialogId, this.options.dialogArgs);
            }
        }
        else {
            this.error(new Error('Invalid Dialog Stack.'));
        }
    };
    Session.prototype.watch = function (variable, enable) {
        if (enable === void 0) { enable = true; }
        var name = variable.toLowerCase();
        if (!this.userData.hasOwnProperty(consts.Data.DebugWatches)) {
            this.userData[consts.Data.DebugWatches] = {};
        }
        if (watchableHandlers.hasOwnProperty(name)) {
            var entry = watchableHandlers[name];
            this.userData[consts.Data.DebugWatches][entry.name] = enable;
        }
        else {
            throw new Error("Invalid watch statement. '" + variable + "' isn't watchable");
        }
        return this;
    };
    Session.prototype.watchList = function () {
        var watches = [];
        if (this.userData.hasOwnProperty(consts.Data.DebugWatches)) {
            for (var name_1 in this.userData[consts.Data.DebugWatches]) {
                if (this.userData[consts.Data.DebugWatches][name_1]) {
                    watches.push(name_1);
                }
            }
        }
        return watches;
    };
    Session.watchable = function (variable, handler) {
        if (handler) {
            watchableHandlers[variable.toLowerCase()] = { name: variable, handler: handler };
        }
        else {
            var entry = watchableHandlers[variable.toLowerCase()];
            if (entry) {
                handler = entry.handler;
            }
        }
        return handler;
    };
    Session.watchableList = function () {
        var variables = [];
        for (var name_2 in watchableHandlers) {
            if (watchableHandlers.hasOwnProperty(name_2)) {
                variables.push(watchableHandlers[name_2].name);
            }
        }
        return variables;
    };
    Session.prototype.onSave = function (cb) {
        var _this = this;
        this.options.onSave(function (err) {
            if (err) {
                _this.logger.error(_this.dialogStack(), err);
                switch (err.code || '') {
                    case consts.Errors.EBADMSG:
                    case consts.Errors.EMSGSIZE:
                        _this.userData = {};
                        _this.batch = [];
                        _this.endConversation(_this.options.dialogErrorMessage || 'Oops. Something went wrong and we need to start over.');
                        break;
                }
            }
            cb(err);
        });
    };
    Session.prototype.onSend = function (batch, cb) {
        var _this = this;
        if (batch && batch.length > 0) {
            this.options.onSend(batch, function (err) {
                if (err) {
                    _this.logger.error(_this.dialogStack(), err);
                }
                cb(err);
            });
        }
        else {
            cb(null);
        }
    };
    Session.prototype.onFinishBatch = function (cb) {
        var _this = this;
        var ctx = this.toRecognizeContext();
        async.each(this.watchList(), function (variable, cb) {
            var entry = watchableHandlers[variable.toLowerCase()];
            if (entry && entry.handler) {
                try {
                    entry.handler(ctx, function (err, value) {
                        if (!err) {
                            _this.logger.dump(variable, value);
                        }
                        cb(err);
                    });
                }
                catch (e) {
                    cb(e);
                }
            }
            else {
                cb(new Error("'" + variable + "' isn't watchable."));
            }
        }, function (err) {
            if (err) {
                _this.logger.error(_this.dialogStack(), err);
            }
            _this.logger.flush(function (err) {
                _this.sendingBatch = false;
                if (err) {
                    console.error(err);
                }
                cb();
            });
        });
    };
    Session.prototype.startBatch = function () {
        var _this = this;
        this.batchStarted = true;
        if (!this.sendingBatch) {
            if (this.batchTimer) {
                clearTimeout(this.batchTimer);
            }
            this.batchTimer = setTimeout(function () {
                _this.sendBatch();
            }, this.options.autoBatchDelay);
        }
    };
    Session.prototype.createMessage = function (localizationNamespace, text, args) {
        var message = new Message_1.Message(this)
            .text(this.vgettext(localizationNamespace, Message_1.Message.randomPrompt(text), args));
        return message.toMessage();
    };
    Session.prototype.prepareMessage = function (msg) {
        if (!msg.type) {
            msg.type = 'message';
        }
        if (!msg.address) {
            msg.address = this.message.address;
        }
        if (!msg.textLocale && this.message.textLocale) {
            msg.textLocale = this.message.textLocale;
        }
    };
    Session.prototype.vgettext = function (localizationNamespace, messageid, args) {
        var tmpl;
        if (this.localizer && this.message) {
            tmpl = this.localizer.gettext(this.preferredLocale(), messageid, localizationNamespace);
        }
        else {
            tmpl = messageid;
        }
        return args && args.length > 0 ? sprintf.vsprintf(tmpl, args) : tmpl;
    };
    Session.prototype.validateCallstack = function () {
        var ss = this.sessionState;
        for (var i = 0; i < ss.callstack.length; i++) {
            var id = ss.callstack[i].id;
            if (!this.findDialog(id)) {
                return false;
            }
        }
        return true;
    };
    Session.prototype.resolveDialogId = function (id) {
        return id.indexOf(':') >= 0 ? id : this.curLibraryName() + ':' + id;
    };
    Session.prototype.curLibraryName = function () {
        var cur = this.curDialog();
        return cur && !this.inMiddleware ? cur.id.split(':')[0] : this.library.name;
    };
    Session.prototype.findDialog = function (id) {
        var parts = id.split(':');
        return this.library.findDialog(parts[0] || this.library.name, parts[1]);
    };
    Session.prototype.pushDialog = function (ds) {
        var ss = this.sessionState;
        var cur = this.curDialog();
        if (cur) {
            cur.state = this.dialogData || {};
        }
        ss.callstack.push(ds);
        this.dialogData = ds.state || {};
        return ds;
    };
    Session.prototype.popDialog = function () {
        var ss = this.sessionState;
        if (ss.callstack.length > 0) {
            ss.callstack.pop();
        }
        var cur = this.curDialog();
        this.dialogData = cur ? cur.state : null;
        return cur;
    };
    Session.prototype.deleteDialogs = function (dialogId) {
        var ss = this.sessionState;
        var index = -1;
        if (typeof dialogId === 'string') {
            for (var i = ss.callstack.length - 1; i >= 0; i--) {
                if (ss.callstack[i].id == dialogId) {
                    index = i;
                    break;
                }
            }
        }
        else {
            index = dialogId;
        }
        if (index < 0 && index < ss.callstack.length) {
            throw new Error('Unable to cancel dialog. Dialog[' + dialogId + '] not found.');
        }
        ss.callstack.splice(index);
        var cur = this.curDialog();
        this.dialogData = cur ? cur.state : null;
        return cur;
    };
    Session.prototype.curDialog = function () {
        var cur;
        var ss = this.sessionState;
        if (ss.callstack.length > 0) {
            cur = ss.callstack[ss.callstack.length - 1];
        }
        return cur;
    };
    Session.prototype.getMessageReceived = function () {
        console.warn("Session.getMessageReceived() is deprecated. Use Session.message.sourceEvent instead.");
        return this.message.sourceEvent;
    };
    return Session;
}(events.EventEmitter));
exports.Session = Session;
var watchableHandlers = {
    'userdata': { name: 'userData', handler: function (ctx, cb) { return cb(null, ctx.userData); } },
    'conversationdata': { name: 'conversationData', handler: function (ctx, cb) { return cb(null, ctx.conversationData); } },
    'privateconversationdata': { name: 'privateConversationData', handler: function (ctx, cb) { return cb(null, ctx.privateConversationData); } },
    'dialogdata': { name: 'dialogData', handler: function (ctx, cb) { return cb(null, ctx.dialogData); } },
    'dialogstack': { name: 'dialogStack', handler: function (ctx, cb) { return cb(null, ctx.dialogStack()); } },
    'preferredlocale': { name: 'preferredLocale', handler: function (ctx, cb) { return cb(null, ctx.preferredLocale()); } },
    'libraryname': { name: 'libraryName', handler: function (ctx, cb) { return cb(null, ctx.libraryName); } }
};
