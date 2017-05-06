"use strict";
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var Library_1 = require("./Library");
var Session_1 = require("../Session");
var DefaultLocalizer_1 = require("../DefaultLocalizer");
var BotStorage_1 = require("../storage/BotStorage");
var SessionLogger_1 = require("../SessionLogger");
var RemoteSessionLogger_1 = require("../RemoteSessionLogger");
var consts = require("../consts");
var utils = require("../utils");
var async = require("async");
var UniversalBot = (function (_super) {
    __extends(UniversalBot, _super);
    function UniversalBot(connector, defaultDialog, libraryName) {
        var _this = _super.call(this, libraryName || consts.Library.default) || this;
        _this.settings = {
            processLimit: 4,
            persistUserData: true,
            persistConversationData: false
        };
        _this.connectors = {};
        _this.mwReceive = [];
        _this.mwSend = [];
        _this.mwSession = [];
        _this.localePath('./locale/');
        _this.library(Library_1.systemLib);
        if (defaultDialog) {
            if (typeof defaultDialog === 'function' || Array.isArray(defaultDialog)) {
                _this.dialog('/', defaultDialog);
            }
            else {
                var settings = defaultDialog;
                for (var name in settings) {
                    if (settings.hasOwnProperty(name)) {
                        _this.set(name, settings[name]);
                    }
                }
            }
        }
        if (connector) {
            _this.connector(consts.defaultConnector, connector);
        }
        return _this;
    }
    UniversalBot.prototype.clone = function (copyTo, newName) {
        var obj = copyTo || new UniversalBot(null, null, newName || this.name);
        for (var name in this.settings) {
            if (this.settings.hasOwnProperty(name)) {
                this.set(name, this.settings[name]);
            }
        }
        for (var channel in this.connectors) {
            obj.connector(channel, this.connectors[channel]);
        }
        obj.mwReceive = this.mwReceive.slice(0);
        obj.mwSession = this.mwSession.slice(0);
        obj.mwSend = this.mwSend.slice(0);
        return _super.prototype.clone.call(this, obj);
    };
    UniversalBot.prototype.set = function (name, value) {
        this.settings[name] = value;
        if (value && name === 'localizerSettings') {
            var settings = value;
            if (settings.botLocalePath) {
                this.localePath(settings.botLocalePath);
            }
        }
        return this;
    };
    UniversalBot.prototype.get = function (name) {
        return this.settings[name];
    };
    UniversalBot.prototype.connector = function (channelId, connector) {
        var _this = this;
        var c;
        if (connector) {
            this.connectors[channelId || consts.defaultConnector] = c = connector;
            c.onEvent(function (events, cb) { return _this.receive(events, cb); });
            var asStorage = connector;
            if (!this.settings.storage &&
                typeof asStorage.getData === 'function' &&
                typeof asStorage.saveData === 'function') {
                this.settings.storage = asStorage;
            }
        }
        else if (this.connectors.hasOwnProperty(channelId)) {
            c = this.connectors[channelId];
        }
        else if (this.connectors.hasOwnProperty(consts.defaultConnector)) {
            c = this.connectors[consts.defaultConnector];
        }
        return c;
    };
    UniversalBot.prototype.use = function () {
        var _this = this;
        var args = [];
        for (var _i = 0; _i < arguments.length; _i++) {
            args[_i] = arguments[_i];
        }
        args.forEach(function (mw) {
            var added = 0;
            if (mw.receive) {
                Array.prototype.push.apply(_this.mwReceive, Array.isArray(mw.receive) ? mw.receive : [mw.receive]);
                added++;
            }
            if (mw.send) {
                Array.prototype.push.apply(_this.mwSend, Array.isArray(mw.send) ? mw.send : [mw.send]);
                added++;
            }
            if (mw.botbuilder) {
                Array.prototype.push.apply(_this.mwSession, Array.isArray(mw.botbuilder) ? mw.botbuilder : [mw.botbuilder]);
                added++;
            }
            if (added < 1) {
                console.warn('UniversalBot.use: no compatible middleware hook found to install.');
            }
        });
        return this;
    };
    UniversalBot.prototype.receive = function (events, done) {
        var _this = this;
        var list = Array.isArray(events) ? events : [events];
        async.eachLimit(list, this.settings.processLimit, function (message, cb) {
            message.agent = consts.agent;
            message.type = message.type || consts.messageType;
            _this.lookupUser(message.address, function (user) {
                if (user) {
                    message.user = user;
                }
                _this.emit('receive', message);
                _this.eventMiddleware(message, _this.mwReceive, function () {
                    if (_this.isMessage(message)) {
                        _this.emit('incoming', message);
                        var userId = message.user.id;
                        var conversationId = message.address.conversation ? message.address.conversation.id : null;
                        var storageCtx = {
                            userId: userId,
                            conversationId: conversationId,
                            address: message.address,
                            persistUserData: _this.settings.persistUserData,
                            persistConversationData: _this.settings.persistConversationData
                        };
                        _this.dispatch(storageCtx, message, _this.settings.defaultDialogId || '/', _this.settings.defaultDialogArgs, cb);
                    }
                    else {
                        _this.emit(message.type, message);
                        cb(null);
                    }
                }, cb);
            }, cb);
        }, this.errorLogger(done));
    };
    UniversalBot.prototype.beginDialog = function (address, dialogId, dialogArgs, done) {
        var _this = this;
        this.lookupUser(address, function (user) {
            var msg = {
                type: consts.messageType,
                agent: consts.agent,
                source: address.channelId,
                sourceEvent: {},
                address: utils.clone(address),
                text: '',
                user: user
            };
            _this.ensureConversation(msg.address, function (adr) {
                msg.address = adr;
                var conversationId = msg.address.conversation ? msg.address.conversation.id : null;
                var storageCtx = {
                    userId: msg.user.id,
                    conversationId: conversationId,
                    address: msg.address,
                    persistUserData: _this.settings.persistUserData,
                    persistConversationData: _this.settings.persistConversationData
                };
                _this.dispatch(storageCtx, msg, dialogId, dialogArgs, _this.errorLogger(done), true);
            }, _this.errorLogger(done));
        }, this.errorLogger(done));
    };
    UniversalBot.prototype.send = function (messages, done) {
        var _this = this;
        var list;
        if (Array.isArray(messages)) {
            list = messages;
        }
        else if (messages.toMessage) {
            list = [messages.toMessage()];
        }
        else {
            list = [messages];
        }
        async.eachLimit(list, this.settings.processLimit, function (message, cb) {
            _this.ensureConversation(message.address, function (adr) {
                message.address = adr;
                _this.emit('send', message);
                _this.eventMiddleware(message, _this.mwSend, function () {
                    _this.emit('outgoing', message);
                    cb(null);
                }, cb);
            }, cb);
        }, this.errorLogger(function (err) {
            if (!err && list.length > 0) {
                _this.tryCatch(function () {
                    var channelId = list[0].address.channelId;
                    var connector = _this.connector(channelId);
                    if (!connector) {
                        throw new Error("Invalid channelId='" + channelId + "'");
                    }
                    connector.send(list, _this.errorLogger(done));
                }, _this.errorLogger(done));
            }
            else if (done) {
                done(err);
            }
        }));
    };
    UniversalBot.prototype.isInConversation = function (address, cb) {
        var _this = this;
        this.lookupUser(address, function (user) {
            var conversationId = address.conversation ? address.conversation.id : null;
            var storageCtx = {
                userId: user.id,
                conversationId: conversationId,
                address: address,
                persistUserData: false,
                persistConversationData: false
            };
            _this.getStorageData(storageCtx, function (data) {
                var lastAccess;
                if (data && data.privateConversationData && data.privateConversationData.hasOwnProperty(consts.Data.SessionState)) {
                    var ss = data.privateConversationData[consts.Data.SessionState];
                    if (ss && ss.lastAccess) {
                        lastAccess = new Date(ss.lastAccess);
                    }
                }
                cb(null, lastAccess);
            }, _this.errorLogger(cb));
        }, this.errorLogger(cb));
    };
    UniversalBot.prototype.onDisambiguateRoute = function (handler) {
        this._onDisambiguateRoute = handler;
    };
    UniversalBot.prototype.loadSession = function (address, done) {
        var _this = this;
        this.lookupUser(address, function (user) {
            var msg = {
                type: consts.messageType,
                agent: consts.agent,
                source: address.channelId,
                sourceEvent: {},
                address: utils.clone(address),
                text: '',
                user: user
            };
            _this.ensureConversation(msg.address, function (adr) {
                msg.address = adr;
                var conversationId = msg.address.conversation ? msg.address.conversation.id : null;
                var storageCtx = {
                    userId: msg.user.id,
                    conversationId: conversationId,
                    address: msg.address,
                    persistUserData: _this.settings.persistUserData,
                    persistConversationData: _this.settings.persistConversationData
                };
                _this.createSession(storageCtx, msg, _this.settings.defaultDialogId || '/', _this.settings.defaultDialogArgs, done);
            }, _this.errorLogger(done));
        }, this.errorLogger(done));
    };
    UniversalBot.prototype.dispatch = function (storageCtx, message, dialogId, dialogArgs, done, newStack) {
        var _this = this;
        if (newStack === void 0) { newStack = false; }
        this.createSession(storageCtx, message, dialogId, dialogArgs, function (err, session) {
            if (!err) {
                _this.emit('routing', session);
                _this.routeMessage(session, done);
            }
            else {
                done(err);
            }
        }, newStack);
    };
    UniversalBot.prototype.createSession = function (storageCtx, message, dialogId, dialogArgs, done, newStack) {
        var _this = this;
        if (newStack === void 0) { newStack = false; }
        var loadedData;
        this.getStorageData(storageCtx, function (data) {
            if (!_this.localizer) {
                var defaultLocale = _this.settings.localizerSettings ? _this.settings.localizerSettings.defaultLocale : null;
                _this.localizer = new DefaultLocalizer_1.DefaultLocalizer(_this, defaultLocale);
            }
            var logger;
            if (message.source == consts.emulatorChannel) {
                logger = new RemoteSessionLogger_1.RemoteSessionLogger(_this.connector(consts.emulatorChannel), message.address, message.address);
            }
            else if (data.privateConversationData && data.privateConversationData.hasOwnProperty(consts.Data.DebugAddress)) {
                var debugAddress = data.privateConversationData[consts.Data.DebugAddress];
                logger = new RemoteSessionLogger_1.RemoteSessionLogger(_this.connector(consts.emulatorChannel), debugAddress, message.address);
            }
            else {
                logger = new SessionLogger_1.SessionLogger();
            }
            var session = new Session_1.Session({
                localizer: _this.localizer,
                logger: logger,
                autoBatchDelay: _this.settings.autoBatchDelay,
                library: _this,
                middleware: _this.mwSession,
                dialogId: dialogId,
                dialogArgs: dialogArgs,
                dialogErrorMessage: _this.settings.dialogErrorMessage,
                onSave: function (cb) {
                    var finish = _this.errorLogger(cb);
                    loadedData.userData = utils.clone(session.userData);
                    loadedData.conversationData = utils.clone(session.conversationData);
                    loadedData.privateConversationData = utils.clone(session.privateConversationData);
                    loadedData.privateConversationData[consts.Data.SessionState] = session.sessionState;
                    _this.saveStorageData(storageCtx, loadedData, finish, finish);
                },
                onSend: function (messages, cb) {
                    _this.send(messages, cb);
                }
            });
            session.on('error', function (err) { return _this.emitError(err); });
            var sessionState;
            session.userData = data.userData || {};
            session.conversationData = data.conversationData || {};
            session.privateConversationData = data.privateConversationData || {};
            if (session.privateConversationData.hasOwnProperty(consts.Data.SessionState)) {
                sessionState = newStack ? null : session.privateConversationData[consts.Data.SessionState];
                delete session.privateConversationData[consts.Data.SessionState];
            }
            loadedData = data;
            session.dispatch(sessionState, message, function () { return done(null, session); });
        }, done);
    };
    UniversalBot.prototype.routeMessage = function (session, done) {
        var _this = this;
        var entry = 'UniversalBot("' + this.name + '") routing ';
        if (session.message.text) {
            entry += '"' + session.message.text + '"';
        }
        else if (session.message.attachments && session.message.attachments.length > 0) {
            entry += session.message.attachments.length + ' attachment(s)';
        }
        else {
            entry += '<null>';
        }
        entry += ' from "' + session.message.source + '"';
        session.logger.log(null, entry);
        var context = session.toRecognizeContext();
        this.recognize(context, function (err, topIntent) {
            if (session.message.entities) {
                session.message.entities.forEach(function (entity) {
                    if (entity.type === consts.intentEntityType &&
                        entity.score > topIntent.score) {
                        topIntent = entity;
                    }
                });
            }
            context.intent = topIntent;
            context.libraryName = _this.name;
            var results = Library_1.Library.addRouteResult({ score: 0.0, libraryName: _this.name });
            async.each(_this.libraryList(), function (lib, cb) {
                lib.findRoutes(context, function (err, routes) {
                    if (!err && routes) {
                        routes.forEach(function (r) { return results = Library_1.Library.addRouteResult(r, results); });
                    }
                    cb(err);
                });
            }, function (err) {
                if (!err) {
                    var disambiguateRoute = function (session, routes) {
                        var route = Library_1.Library.bestRouteResult(results, session.dialogStack(), _this.name);
                        if (route) {
                            _this.library(route.libraryName).selectRoute(session, route);
                        }
                        else {
                            session.routeToActiveDialog();
                        }
                    };
                    if (_this._onDisambiguateRoute) {
                        disambiguateRoute = _this._onDisambiguateRoute;
                    }
                    disambiguateRoute(session, results);
                    done(null);
                }
                else {
                    session.error(err);
                    done(err);
                }
            });
        });
    };
    UniversalBot.prototype.eventMiddleware = function (event, middleware, done, error) {
        var i = -1;
        var _that = this;
        function next() {
            if (++i < middleware.length) {
                _that.tryCatch(function () {
                    middleware[i](event, next);
                }, function () { return next(); });
            }
            else {
                _that.tryCatch(function () { return done(); }, error);
            }
        }
        next();
    };
    UniversalBot.prototype.isMessage = function (message) {
        return (message && message.type && message.type.toLowerCase() == consts.messageType);
    };
    UniversalBot.prototype.ensureConversation = function (address, done, error) {
        var _this = this;
        this.tryCatch(function () {
            if (!address.conversation) {
                var connector = _this.connector(address.channelId);
                if (!connector) {
                    throw new Error("Invalid channelId='" + address.channelId + "'");
                }
                connector.startConversation(address, function (err, adr) {
                    if (!err) {
                        _this.tryCatch(function () { return done(adr); }, error);
                    }
                    else if (error) {
                        error(err);
                    }
                });
            }
            else {
                _this.tryCatch(function () { return done(address); }, error);
            }
        }, error);
    };
    UniversalBot.prototype.lookupUser = function (address, done, error) {
        var _this = this;
        this.tryCatch(function () {
            _this.emit('lookupUser', address);
            if (_this.settings.lookupUser) {
                _this.settings.lookupUser(address, function (err, user) {
                    if (!err) {
                        _this.tryCatch(function () { return done(user || address.user); }, error);
                    }
                    else if (error) {
                        error(err);
                    }
                });
            }
            else {
                _this.tryCatch(function () { return done(address.user); }, error);
            }
        }, error);
    };
    UniversalBot.prototype.getStorageData = function (storageCtx, done, error) {
        var _this = this;
        this.tryCatch(function () {
            _this.emit('getStorageData', storageCtx);
            var storage = _this.getStorage();
            storage.getData(storageCtx, function (err, data) {
                if (!err) {
                    _this.tryCatch(function () { return done(data || {}); }, error);
                }
                else if (error) {
                    error(err);
                }
            });
        }, error);
    };
    UniversalBot.prototype.saveStorageData = function (storageCtx, data, done, error) {
        var _this = this;
        this.tryCatch(function () {
            _this.emit('saveStorageData', storageCtx);
            var storage = _this.getStorage();
            storage.saveData(storageCtx, data, function (err) {
                if (!err) {
                    if (done) {
                        _this.tryCatch(function () { return done(); }, error);
                    }
                }
                else if (error) {
                    error(err);
                }
            });
        }, error);
    };
    UniversalBot.prototype.getStorage = function () {
        if (!this.settings.storage) {
            this.settings.storage = new BotStorage_1.MemoryBotStorage();
        }
        return this.settings.storage;
    };
    UniversalBot.prototype.tryCatch = function (fn, error) {
        try {
            fn();
        }
        catch (e) {
            try {
                if (error) {
                    error(e);
                }
            }
            catch (e2) {
                this.emitError(e2);
            }
        }
    };
    UniversalBot.prototype.errorLogger = function (done) {
        var _this = this;
        return function (err) {
            if (err) {
                _this.emitError(err);
            }
            if (done) {
                done(err);
                done = null;
            }
        };
    };
    UniversalBot.prototype.emitError = function (err) {
        var m = err.toString();
        var e = err instanceof Error ? err : new Error(m);
        if (this.listenerCount('error') > 0) {
            this.emit('error', e);
        }
        else {
            console.error(e.stack);
        }
    };
    return UniversalBot;
}(Library_1.Library));
exports.UniversalBot = UniversalBot;
